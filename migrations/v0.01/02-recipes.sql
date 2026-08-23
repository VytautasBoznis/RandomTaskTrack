-- ─────────────────────────────────────────────────────────────────────────────
-- Recipes — the "cook something new every week" scope.
--
-- Cuisine families are rows rather than an enum for the same reason domains
-- are: the weekly pick rotates to whichever family has gone longest without a
-- dish, so retiring a cuisine is `is_active = false`, not a schema change.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.recipe_families
(
    -- `code` is the source's own cuisine vocabulary, sent to the API verbatim.
    -- A second source would need its own mapping anyway, so there is no
    -- pretence of a neutral taxonomy here.
    id         int         NOT NULL PRIMARY KEY,
    code       text        NOT NULL,
    name       text        NOT NULL,
    is_active  boolean     NOT NULL DEFAULT true,
    sort_order int         NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_recipe_families_code
    ON tracker.recipe_families (code);

-- Every dish ever pulled. This doubles as the "already seen" list — the whole
-- point of the scope is new dishes, so anything already in here is never
-- offered a second time.
CREATE TABLE IF NOT EXISTS tracker.recipe_recipes
(
    id            uuid        NOT NULL PRIMARY KEY,
    source        text        NOT NULL,
    external_id   text        NOT NULL,
    family_id     int         NULL REFERENCES tracker.recipe_families (id),
    title         text        NOT NULL,
    image_url     text        NULL,
    source_url    text        NULL,
    ready_minutes int         NULL,
    servings      int         NULL,

    -- [{"item": "Pork chops", "amount": "4"}] — the shopping checklist.
    ingredients   jsonb       NOT NULL DEFAULT '[]'::jsonb,
    -- ["Bash the loins…", "Heat the oil…"] — ordered.
    steps         jsonb       NOT NULL DEFAULT '[]'::jsonb,

    pulled_at     timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_recipe_recipes_external
    ON tracker.recipe_recipes (source, external_id);

-- One dish per week. week_of is the Monday of the ISO week in the scheduler's
-- timezone, which makes "this week's dish" a plain equality lookup.
--
--   status  1 = current, 2 = rerolled (superseded, kept so it is not re-offered)
CREATE TABLE IF NOT EXISTS tracker.recipe_picks
(
    id         uuid        NOT NULL PRIMARY KEY,
    recipe_id  uuid        NOT NULL REFERENCES tracker.recipe_recipes (id),
    week_of    date        NOT NULL,
    status     int         NOT NULL DEFAULT 1,

    -- Set when the dish is put on the board. ON DELETE SET NULL so binning the
    -- task does not take the pick with it.
    task_id    uuid        NULL REFERENCES tracker.task_tasks (id) ON DELETE SET NULL,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_recipe_picks_status CHECK (status IN (1, 2))
);

-- Same trick as ux_task_tasks_recurrence_due: a partial unique index is what
-- lets "ensure this week has a dish" run on every page load, concurrently,
-- with ON CONFLICT DO NOTHING instead of a lock.
CREATE UNIQUE INDEX IF NOT EXISTS ux_recipe_picks_current
    ON tracker.recipe_picks (week_of) WHERE status = 1;

CREATE INDEX IF NOT EXISTS ix_recipe_picks_history
    ON tracker.recipe_picks (created_at DESC);
