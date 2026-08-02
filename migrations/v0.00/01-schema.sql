-- Baseline schema for RandomTaskTrack.
-- Everything lives under the `tracker` schema. Table names are prefixed by
-- group (user_, task_, chat_) following the CartFees-admin convention.

CREATE SCHEMA IF NOT EXISTS tracker;

-- ─────────────────────────────────────────────────────────────────────────────
-- Users
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.user_users
(
    id         uuid        NOT NULL PRIMARY KEY,
    email      text        NOT NULL,
    password   text        NOT NULL,
    role       int         NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_user_users_email
    ON tracker.user_users (lower(email));

-- ─────────────────────────────────────────────────────────────────────────────
-- Domains — the "tracker" a task belongs to (fitness, house, plants, cooking…).
-- Adding a new domain is an INSERT, not a schema change.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.task_domains
(
    id         int         NOT NULL PRIMARY KEY,
    code       text        NOT NULL,
    name       text        NOT NULL,
    is_active  boolean     NOT NULL DEFAULT true,
    sort_order int         NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_task_domains_code
    ON tracker.task_domains (code);

-- ─────────────────────────────────────────────────────────────────────────────
-- Recurrences — the template + schedule that spawns task instances.
--
--   rule_type    1 = interval_days, 2 = days_of_week, 3 = day_of_month
--   anchor_mode  1 = from_schedule   (next due = previous DUE date + interval)
--                2 = from_completion (next due = actual COMPLETION + interval)
--
-- anchor_mode is the "I cleaned the bathroom on day 9 of a 7-day cycle" knob:
-- from_schedule keeps the original cadence, from_completion resets the clock.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.task_recurrences
(
    id            uuid        NOT NULL PRIMARY KEY,
    domain_id     int         NOT NULL REFERENCES tracker.task_domains (id),
    title         text        NOT NULL,
    notes         text        NULL,
    data          jsonb       NOT NULL DEFAULT '{}'::jsonb,
    rule_type     int         NOT NULL,
    interval_days int         NULL,
    days_of_week  int[]       NULL,
    day_of_month  int         NULL,
    anchor_mode   int         NOT NULL DEFAULT 1,
    time_of_day   time        NULL,
    starts_on     date        NOT NULL,
    ends_on       date        NULL,
    is_active     boolean     NOT NULL DEFAULT true,
    last_due_on   date        NULL,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_task_recurrences_rule_type CHECK (rule_type IN (1, 2, 3)),
    CONSTRAINT ck_task_recurrences_anchor CHECK (anchor_mode IN (1, 2)),

    -- Every branch is wrapped in COALESCE on purpose. A CHECK that evaluates to
    -- NULL is treated as satisfied, so `array_length('{}', 1) > 0` (NULL for an
    -- empty array) and `NULL BETWEEN 1 AND 31` would both let a malformed rule
    -- straight through.
    CONSTRAINT ck_task_recurrences_shape CHECK (
        (rule_type = 1 AND COALESCE(interval_days, 0) > 0)
            OR (rule_type = 2 AND COALESCE(array_length(days_of_week, 1), 0) > 0)
            OR (rule_type = 3 AND COALESCE(day_of_month, 0) BETWEEN 1 AND 31)
        )
);

CREATE INDEX IF NOT EXISTS ix_task_recurrences_active
    ON tracker.task_recurrences (is_active, domain_id);

-- ─────────────────────────────────────────────────────────────────────────────
-- Tasks — a single dated instance. Both ad-hoc (recurrence_id IS NULL) and
-- materialized-from-recurrence tasks live here so the dashboard is one query.
--
--   status  1 = pending, 2 = done, 3 = skipped
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.task_tasks
(
    id            uuid        NOT NULL PRIMARY KEY,
    domain_id     int         NOT NULL REFERENCES tracker.task_domains (id),
    recurrence_id uuid        NULL REFERENCES tracker.task_recurrences (id) ON DELETE SET NULL,
    title         text        NOT NULL,
    notes         text        NULL,
    data          jsonb       NOT NULL DEFAULT '{}'::jsonb,
    due_on        date        NOT NULL,
    due_time      time        NULL,
    status        int         NOT NULL DEFAULT 1,
    completed_at  timestamptz NULL,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_task_tasks_status CHECK (status IN (1, 2, 3))
);

CREATE INDEX IF NOT EXISTS ix_task_tasks_due
    ON tracker.task_tasks (due_on, status);
CREATE INDEX IF NOT EXISTS ix_task_tasks_domain
    ON tracker.task_tasks (domain_id, due_on);
CREATE INDEX IF NOT EXISTS ix_task_tasks_data
    ON tracker.task_tasks USING gin (data);

-- One materialized instance per recurrence per date. This is what makes the
-- materializer safe to run repeatedly (it relies on ON CONFLICT DO NOTHING).
CREATE UNIQUE INDEX IF NOT EXISTS ux_task_tasks_recurrence_due
    ON tracker.task_tasks (recurrence_id, due_on)
    WHERE recurrence_id IS NOT NULL;

-- ─────────────────────────────────────────────────────────────────────────────
-- Completions — the append-only record of what ACTUALLY happened.
-- This is the table that makes progress charts and AI adjustment possible:
-- planned_data is the snapshot of what was asked for, actual_data is what got
-- done. Never updated in place.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.task_completions
(
    id           uuid        NOT NULL PRIMARY KEY,
    task_id      uuid        NOT NULL REFERENCES tracker.task_tasks (id) ON DELETE CASCADE,
    domain_id    int         NOT NULL REFERENCES tracker.task_domains (id),
    status       int         NOT NULL,
    planned_data jsonb       NOT NULL DEFAULT '{}'::jsonb,
    actual_data  jsonb       NOT NULL DEFAULT '{}'::jsonb,
    note         text        NULL,
    due_on       date        NOT NULL,
    completed_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_task_completions_status CHECK (status IN (2, 3))
);

CREATE INDEX IF NOT EXISTS ix_task_completions_task
    ON tracker.task_completions (task_id);
CREATE INDEX IF NOT EXISTS ix_task_completions_domain_date
    ON tracker.task_completions (domain_id, completed_at DESC);
CREATE INDEX IF NOT EXISTS ix_task_completions_actual
    ON tracker.task_completions USING gin (actual_data);

-- ─────────────────────────────────────────────────────────────────────────────
-- Chat — conversations with the AI. Messages are the provider-neutral shape
-- (role + content + optional tool_calls / tool_result payloads), so switching
-- AI provider does not invalidate stored history.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.chat_conversations
(
    id         uuid        NOT NULL PRIMARY KEY,
    title      text        NOT NULL,
    domain_id  int         NULL REFERENCES tracker.task_domains (id),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_chat_conversations_updated
    ON tracker.chat_conversations (updated_at DESC);

CREATE TABLE IF NOT EXISTS tracker.chat_messages
(
    id              uuid        NOT NULL PRIMARY KEY,
    conversation_id uuid        NOT NULL REFERENCES tracker.chat_conversations (id) ON DELETE CASCADE,
    seq             int         NOT NULL,
    role            text        NOT NULL,
    content         text        NULL,
    tool_calls      jsonb       NULL,
    tool_results    jsonb       NULL,
    model           text        NULL,
    input_tokens    int         NULL,
    output_tokens   int         NULL,
    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_chat_messages_role CHECK (role IN ('user', 'assistant', 'tool'))
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_chat_messages_seq
    ON tracker.chat_messages (conversation_id, seq);
