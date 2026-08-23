-- Spoonacular's cuisine vocabulary, verbatim — these strings go straight into
-- the API's include-tags parameter. The umbrella families (Asian, European,
-- Latin American, Mediterranean) overlap the country ones on purpose: they are
-- how the rotation reaches dishes that are not tagged with a single country.
--
-- Idempotent so the migration is safe to re-run.
INSERT INTO tracker.recipe_families (id, code, name, sort_order)
VALUES (1, 'Italian', 'Italian', 10),
       (2, 'Asian', 'Asian', 20),
       (3, 'Chinese', 'Chinese', 30),
       (4, 'Japanese', 'Japanese', 40),
       (5, 'Korean', 'Korean', 50),
       (6, 'Thai', 'Thai', 60),
       (7, 'Vietnamese', 'Vietnamese', 70),
       (8, 'Indian', 'Indian', 80),
       (9, 'French', 'French', 90),
       (10, 'Spanish', 'Spanish', 100),
       (11, 'Greek', 'Greek', 110),
       (12, 'Mediterranean', 'Mediterranean', 120),
       (13, 'Middle Eastern', 'Middle Eastern', 130),
       (14, 'African', 'African', 140),
       (15, 'Caribbean', 'Caribbean', 150),
       (16, 'Latin American', 'Latin American', 160),
       (17, 'Mexican', 'Mexican', 170),
       (18, 'American', 'American', 180),
       (19, 'Southern', 'Southern', 190),
       (20, 'Cajun', 'Cajun', 200),
       (21, 'British', 'British', 210),
       (22, 'Irish', 'Irish', 220),
       (23, 'German', 'German', 230),
       (24, 'Nordic', 'Nordic', 240),
       (25, 'Eastern European', 'Eastern European', 250),
       (26, 'European', 'European', 260),
       (27, 'Jewish', 'Jewish', 270)
ON CONFLICT (id) DO NOTHING;
