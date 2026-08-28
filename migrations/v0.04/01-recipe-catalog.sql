-- ─────────────────────────────────────────────────────────────────────────────
-- The local recipe catalog — a read-only reference corpus, imported in bulk.
--
-- Why a second table rather than more rows in recipe_recipes: they mean
-- different things. recipe_recipes is the *library* — dishes you pulled, rated,
-- annotated, and that the weekly rotation draws from. Putting two million
-- reference recipes in there would make the pool meaningless and the rotation
-- would spend the rest of its life offering strangers' casseroles.
--
-- So the catalog is what targeted search reads, and saving a hit copies it into
-- the library, exactly as saving a Spoonacular hit does. Nothing here is ever
-- picked directly.
--
-- Why it exists at all: Spoonacular's catalogue is heavily Western. Measured on
-- the live API — ramen 5, sushi 3, pad thai 0, bibimbap 0. The same searches
-- here return 1061, 996, 490 and 93. No paid Spoonacular tier changes that;
-- their tiers sell quota, not recipes.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.recipe_catalog
(
    -- md5(title || link), so a re-import lands on the same ids and a dish
    -- already copied into the library is not duplicated there.
    external_id text  NOT NULL PRIMARY KEY,
    title       text  NOT NULL,

    -- Same encodings recipe_recipes uses, so RecipeMapper reads both the same
    -- way: [{"item": "...", "amount": null}] and ["step", "step"].
    ingredients jsonb NOT NULL DEFAULT '[]'::jsonb,
    steps       jsonb NOT NULL DEFAULT '[]'::jsonb,

    link        text  NULL
);

-- Search is "I want ramen" — a title match, and plainto_tsquery's implicit AND
-- is what makes "chicken ramen" mean both words rather than either.
--
-- An expression index rather than a stored tsvector column: at two million rows
-- the generated column would be paid for on disk twice over, and the query only
-- ever needs the index.
CREATE INDEX IF NOT EXISTS ix_recipe_catalog_search
    ON tracker.recipe_catalog USING gin (to_tsvector('english', title));
