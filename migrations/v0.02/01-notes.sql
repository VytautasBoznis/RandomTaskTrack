-- ─────────────────────────────────────────────────────────────────────────────
-- Notes — free-form markdown, deliberately attached to nothing.
--
-- Not a domain and not a task: a note has no schedule, no completion and no
-- planned-vs-actual story, so hanging it off task_tasks would mean nullable
-- due dates and a status that never changes. It is its own small table instead.
--
-- `content` is markdown, stored verbatim. Rendering is the frontend's problem —
-- the server never parses it, so switching renderer is not a migration.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.note_notes
(
    id         uuid        NOT NULL PRIMARY KEY,
    title      text        NOT NULL,
    content    text        NOT NULL DEFAULT '',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

-- The list is ordered most-recently-touched first, which is the only read there
-- is.
CREATE INDEX IF NOT EXISTS ix_note_notes_updated
    ON tracker.note_notes (updated_at DESC);