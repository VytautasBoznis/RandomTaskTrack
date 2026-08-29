-- ─────────────────────────────────────────────────────────────────────────────
-- Plants — the "what is this thing and how do I keep it alive" scope.
--
-- The user describes a plant in their own words ("big glossy leaves, bought at
-- the market, lives by the north window") and the AI fills in the rest. So the
-- row has two halves: the few facts a human types and keeps editing, as
-- columns, and the whole researched care profile as one jsonb blob.
--
-- `profile` is a blob rather than fifteen columns for the same reason
-- recipe_recipes.ingredients is: the UI renders it, the database never filters
-- on it, and a prompt that learns to return one more field should not be a
-- migration. Its shape is PlantProfile, and Business/Plants/PlantMapper is the
-- only place that knows the encoding.
--
-- `species` and `latin_name` are lifted out of the profile into columns because
-- they are the two things a human corrects by hand when the identification is
-- wrong — and correcting them must survive the next re-lookup.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.plant_plants
(
    id             uuid        NOT NULL PRIMARY KEY,

    -- What the user calls it. Not the species: "the big one in the hall".
    name           text        NOT NULL,
    location       text        NULL,
    species        text        NULL,
    latin_name     text        NULL,
    acquired_on    date        NULL,

    -- The user's own running notes. Theirs, never overwritten by a lookup.
    notes          text        NOT NULL DEFAULT '',

    -- The free-text description the identification was made from. Kept so a
    -- re-lookup asks the same question, and so a wrong answer is traceable to
    -- what was actually asked.
    description    text        NOT NULL DEFAULT '',

    -- The AI's answer, whole. '{}' until the first successful lookup — a plant
    -- can be added with no AI configured at all.
    profile        jsonb       NOT NULL DEFAULT '{}'::jsonb,
    researched_at  timestamptz NULL,
    research_model text        NULL,

    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now()
);

-- The list is alphabetical and that is the only read there is.
CREATE INDEX IF NOT EXISTS ix_plant_plants_name
    ON tracker.plant_plants (lower(name));

-- ─────────────────────────────────────────────────────────────────────────────
-- Care tasks are ordinary tasks in the `plants` domain, tied to a plant by
-- {"plantId": "…"} in task_tasks.data / task_recurrences.data rather than by a
-- foreign key.
--
-- Two reasons. The materializer copies a recurrence's `data` verbatim onto
-- every instance it spawns, so the link propagates for free — a column would
-- need the materializer to know about plants. And it keeps a scope-specific
-- concern out of the two busiest tables in the schema, the same bargain
-- recipe_picks makes by pointing at a task instead of the reverse.
--
-- The cost is no referential integrity: nothing stops a task naming a plant
-- that has been deleted. DeletePlantOperation is what pays it, binning the
-- plant's recurrences and its pending tasks on the way out.
-- ─────────────────────────────────────────────────────────────────────────────
