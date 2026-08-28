-- ─────────────────────────────────────────────────────────────────────────────
-- Recipes — recipe_recipes stops being the "already seen" list.
--
-- It was doing two jobs: the dish library and the exclusion list. Because a pull
-- asks the source for ten candidates and kept only one, nine were thrown away
-- every time and the excluded set grew until a random draw came back entirely
-- excluded — "no new Italian dish came back".
--
-- Now recipe_recipes is a library: everything ever pulled or saved, most of it
-- uncooked and waiting. "Already had it" moves to recipe_picks, which is what
-- actually records a dish going on the menu:
--
--   no qualifying pick        the pool — what the rotation draws from
--   status 1, past week       history — it was the weekly dish
--   status 1, current week    this week's dish
--   status 2                  rerolled; a rejection, not history, so it comes
--                             back to the pool once the week turns over
--
-- Nothing to backfill: every existing library row has exactly one pick, so the
-- old data lands as history, which is what it is.
-- ─────────────────────────────────────────────────────────────────────────────

-- A library is worth annotating. rating is NULL until it has been cooked and
-- judged; tags are free-form and entered comma-separated in the UI.
ALTER TABLE tracker.recipe_recipes
    ADD COLUMN IF NOT EXISTS rating int    NULL,
    ADD COLUMN IF NOT EXISTS notes  text   NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS tags   text[] NOT NULL DEFAULT '{}';

-- ADD CONSTRAINT has no IF NOT EXISTS, and _draft/_post re-runs would trip over
-- a bare ADD, so it is guarded by hand.
DO
$$
    BEGIN
        ALTER TABLE tracker.recipe_recipes
            ADD CONSTRAINT ck_recipe_recipes_rating
                CHECK (rating IS NULL OR rating BETWEEN 1 AND 5);
    EXCEPTION
        WHEN duplicate_object THEN NULL;
    END
$$;

-- Tag filtering is `tags && '{...}'`, which is an overlap operator — GIN is the
-- index that serves it.
CREATE INDEX IF NOT EXISTS ix_recipe_recipes_tags
    ON tracker.recipe_recipes USING gin (tags);

-- History search is a case-insensitive title match.
CREATE INDEX IF NOT EXISTS ix_recipe_recipes_title
    ON tracker.recipe_recipes (lower(title));

-- The pool predicate is an anti-join from recipe_recipes to recipe_picks, so
-- the lookup wants to go by recipe_id. ix_recipe_picks_history orders by
-- created_at and cannot serve it.
CREATE INDEX IF NOT EXISTS ix_recipe_picks_recipe
    ON tracker.recipe_picks (recipe_id);
