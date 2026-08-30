-- ─────────────────────────────────────────────────────────────────────────────
-- Learning — the "where am I going, and what do I have to do to get there"
-- scope. Career paths, the certifications they need, the certifications
-- already held, and the courses, labs and projects in between.
--
-- Everything in here needs researching before it can be planned, so the shape
-- is the one the plants scope arrived at: the AI's whole answer as a jsonb
-- blob the UI renders, and a second table for the lines the user actually
-- committed to. `plant_plants.profile` suggests care tasks;
-- CreatePlantScheduleOperation promotes the chosen ones into recurrences. Here
-- `learn_goals.plan` suggests phases, certs, resources and projects, and
-- CreateLearningStepsOperation promotes the chosen ones into learn_steps.
--
-- Why not one table with a `parent_id`: a drafted plan is read whole and
-- replaced whole on every re-draft, while a step is edited one at a time and
-- has to survive that re-draft. They have different lifetimes, so they are
-- different tables.
-- ─────────────────────────────────────────────────────────────────────────────

-- ─────────────────────────────────────────────────────────────────────────────
-- Goals — the paths. There are only ever a handful of these, which is the
-- point: `tier` is what keeps them honest about priority instead of letting a
-- fifth "nice to have" quietly compete with the promotion.
--
--   tier    1 = primary, 2 = secondary, 3 = tertiary, 4 = nice to have
--   status  1 = active, 2 = achieved, 3 = parked
--
-- `why` and `benefits` are not decoration. They are the reason the tab is
-- opened on a wall tablet at 7am, so they are columns rather than something
-- buried in the notes, and the card leads with them.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.learn_goals
(
    id             uuid        NOT NULL PRIMARY KEY,

    title          text        NOT NULL,
    tier           int         NOT NULL DEFAULT 4,
    status         int         NOT NULL DEFAULT 1,

    -- The motivation, in the user's own words. Never written by a lookup.
    why            text        NOT NULL DEFAULT '',

    -- What they expect to get out of it — the promotion, the pay, the door it
    -- opens. Read back later to decide whether the path is still worth walking.
    benefits       text        NOT NULL DEFAULT '',

    -- "Prepared by". Deliberately rough: this is a direction, not a deadline
    -- anything is enforced against, and nothing here goes overdue.
    target_on      date        NULL,

    -- What the user told the AI: where they are now, hours a week, constraints.
    -- Kept for the same reason plant_plants.description is — a re-draft asks
    -- the same question, and a bad plan is traceable to what was actually asked.
    context        text        NOT NULL DEFAULT '',

    -- The drafted path, whole. '{}' until the first successful draft; a goal
    -- can be added with no AI configured at all. Shape is LearningPlan, and
    -- Business/Learning/LearningMapper is the only place that knows it.
    plan           jsonb       NOT NULL DEFAULT '{}'::jsonb,
    researched_at  timestamptz NULL,
    research_model text        NULL,

    notes          text        NOT NULL DEFAULT '',

    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_learn_goals_tier CHECK (tier BETWEEN 1 AND 4),
    CONSTRAINT ck_learn_goals_status CHECK (status IN (1, 2, 3))
);

-- The list is by priority and that is the only read there is.
CREATE INDEX IF NOT EXISTS ix_learn_goals_tier
    ON tracker.learn_goals (tier, created_at);

-- ─────────────────────────────────────────────────────────────────────────────
-- Steps — the checklist under a goal. One row per thing that was actually
-- committed to, whether it came off the drafted plan or was typed by hand.
--
--   kind    1 = study, 2 = certification/exam, 3 = project, 4 = course,
--           5 = assignment, 6 = licence, 7 = milestone
--   status  1 = planned, 2 = doing, 3 = done, 4 = dropped
--
-- `notes` is the plan for the step — what to do. `outcome` is what happened
-- afterwards: the grade, the mark breakdown, "failed the lab section, retake
-- booked 12 Jan". Two columns rather than one because they are written at
-- different times and only one of them is worth surfacing as a badge on the
-- row. That is what makes an MSc assignment trackable without an assignments
-- table: kind 5, a target date, and the result typed into `outcome`.
--
-- `provider`, `url` and `cost` carry a course or a tool: "Udemy", the exact
-- title in `title`, and what it costs. `cost` is text on purpose — "€14.99 on
-- sale", "free with the subscription" — it is displayed and never summed, and
-- a numeric would force a currency column onto a scope that has no money in it.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.learn_steps
(
    id         uuid        NOT NULL PRIMARY KEY,
    goal_id    uuid        NOT NULL REFERENCES tracker.learn_goals (id) ON DELETE CASCADE,

    title      text        NOT NULL,
    kind       int         NOT NULL DEFAULT 1,
    status     int         NOT NULL DEFAULT 1,

    target_on  date        NULL,

    -- What to do.
    notes      text        NOT NULL DEFAULT '',

    -- What happened. Grades, comments, retakes.
    outcome    text        NOT NULL DEFAULT '',

    provider   text        NULL,
    url        text        NULL,
    cost       text        NULL,

    -- Rough effort, for sequencing a path against the hours actually available.
    hours      int         NULL,

    sort_order int         NOT NULL DEFAULT 0,

    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_learn_steps_kind CHECK (kind BETWEEN 1 AND 7),
    CONSTRAINT ck_learn_steps_status CHECK (status BETWEEN 1 AND 4),
    CONSTRAINT ck_learn_steps_hours CHECK (hours IS NULL OR hours > 0)
);

CREATE INDEX IF NOT EXISTS ix_learn_steps_goal
    ON tracker.learn_steps (goal_id, sort_order, created_at);

-- ─────────────────────────────────────────────────────────────────────────────
-- Credentials — what is already held. Separate from steps because a cert
-- changes job the day it is earned: it stops being work to do and becomes an
-- asset to keep, and keeping it is a renewal schedule, not a checklist item.
--
-- **Not everything expires.** An older MCSD is permanent; a current Azure
-- credential renews yearly; a pre-2011 CompTIA is good for life. So the expiry
-- is a tri-state, not a nullable date:
--
--   renewal_kind  1 = permanent, 2 = expires (expires_on set), 3 = unknown
--
-- A nullable expires_on on its own cannot tell "never expires" from "nobody
-- has checked yet", and conflating them would either nag forever about a
-- permanent cert or quietly let a real expiry pass unwatched. The CHECK is what
-- keeps the two columns agreeing with each other.
--
-- `renewal` is what the lookup found: how renewal works, what it costs, when
-- the window opens, what happens if it lapses. A blob for the usual reason —
-- the UI renders it and nothing queries it. There is deliberately no table of
-- provider rules anywhere in the code: Microsoft moved to annual free renewals
-- in 2022 while leaving the older certifications permanent, and a hardcoded
-- table would have gone quietly wrong that day.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.learn_credentials
(
    id             uuid        NOT NULL PRIMARY KEY,

    -- Optional: a cert earned on the way to a goal, or one that predates all
    -- of them. SET NULL rather than CASCADE — deleting a path must never
    -- delete the evidence that something was passed.
    goal_id        uuid        NULL REFERENCES tracker.learn_goals (id) ON DELETE SET NULL,

    name           text        NOT NULL,
    issuer         text        NOT NULL DEFAULT '',

    -- "AZ-305". The thing you actually search for.
    code           text        NULL,

    earned_on      date        NOT NULL,

    renewal_kind   int         NOT NULL DEFAULT 3,
    expires_on     date        NULL,

    credential_id  text        NULL,
    url            text        NULL,

    -- Shape is CredentialRenewal. '{}' until looked up.
    renewal        jsonb       NOT NULL DEFAULT '{}'::jsonb,
    researched_at  timestamptz NULL,
    research_model text        NULL,

    notes          text        NOT NULL DEFAULT '',

    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_learn_credentials_renewal CHECK (
        (renewal_kind = 1 AND expires_on IS NULL)
            OR (renewal_kind = 2 AND expires_on IS NOT NULL)
            OR (renewal_kind = 3)
        )
);

-- "What is expiring soon" is the only query on this table that is not a plain
-- list, and permanent rows are NULL here so they sort out of the way for free.
CREATE INDEX IF NOT EXISTS ix_learn_credentials_expires
    ON tracker.learn_credentials (expires_on);

-- ─────────────────────────────────────────────────────────────────────────────
-- Steps and credentials reach the board as ordinary tasks in the `learning`
-- domain, tied back by {"learnStepId": "…"} / {"credentialId": "…"} in
-- task_tasks.data rather than by a foreign key — the same bargain the plants
-- scope documents, and for the same two reasons: the payload propagates
-- through the materializer for free, and the two busiest tables in the schema
-- stay ignorant of a scope they know nothing about.
--
-- The cost is the same too: nothing stops a task outliving the step that made
-- it, so DeleteGoalOperation and DeleteStepOperation sweep up their own
-- pending tasks on the way out.
--
-- Renewal reminders are one-off dated tasks, never recurrences. A cert renewed
-- early moves its own expiry, so a yearly recurrence would drift away from the
-- truth within one cycle and start reminding about the wrong date.
-- ─────────────────────────────────────────────────────────────────────────────
