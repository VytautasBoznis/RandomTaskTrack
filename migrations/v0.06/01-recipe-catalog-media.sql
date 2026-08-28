-- ─────────────────────────────────────────────────────────────────────────────
-- Photos, timings and servings for the catalog — and a second feed to fill them.
--
-- The first corpus (RecipeNLG, 2.2M) is title + ingredients + method and nothing
-- else. In practice that made search unusable at the point of choosing: a search
-- for "chicken ramen" returns three dishes all called "Chicken Ramen",
-- distinguishable only by ingredient count. Nothing to pick with.
--
-- So a second, smaller corpus is added rather than swapping: AllRecipes, 32,722
-- recipes, 77% with a photo and all with times, servings and a description.
-- They are complementary, not competing — "ramen" is 1,061 in the big one and 36
-- in the rich one, of which 30 have pictures. Search ranks pictured dishes
-- first and falls through to the long tail.
--
-- `feed` is what stops "check for new" re-downloading two gigabytes to discover
-- it already has them: a feed with rows is a feed already imported.
-- ─────────────────────────────────────────────────────────────────────────────
ALTER TABLE tracker.recipe_catalog
    ADD COLUMN IF NOT EXISTS image_url     text NULL,
    ADD COLUMN IF NOT EXISTS ready_minutes int  NULL,
    ADD COLUMN IF NOT EXISTS servings      int  NULL,
    ADD COLUMN IF NOT EXISTS feed          text NOT NULL DEFAULT 'recipenlg';

-- Anything already loaded came from the first feed, which is also the default,
-- so existing rows are correct as they stand and only need the index.
CREATE INDEX IF NOT EXISTS ix_recipe_catalog_feed
    ON tracker.recipe_catalog (feed);

-- Search puts pictured dishes first, so the ordering column wants to be cheap
-- to test. Partial: the pictured rows are the minority and the only ones the
-- index needs to find.
CREATE INDEX IF NOT EXISTS ix_recipe_catalog_pictured
    ON tracker.recipe_catalog (external_id) WHERE image_url IS NOT NULL;
