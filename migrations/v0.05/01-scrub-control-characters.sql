-- ─────────────────────────────────────────────────────────────────────────────
-- Scrub control characters out of recipe text written before the importer
-- learned to strip them.
--
-- What this is NOT for: the NUL that aborted the catalog import with
-- "22P05 unsupported Unicode escape sequence". Postgres refuses a NUL in a text
-- column and refuses one inside jsonb, so that character has never been in this
-- database and cannot be — the crash *was* the enforcement.
--
-- What it IS for: the quieter neighbours. A BELL, an ESC, or the U+FFFD that a
-- broken surrogate pair decays into are all perfectly storable, so anything
-- inserted before the fix may be carrying them. They render as blanks or boxes
-- on the tablet rather than breaking anything, which is why nothing complained.
--
-- recipe_catalog could just be re-imported instead. recipe_recipes could not:
-- it holds ratings, notes and tags that exist nowhere else, so it is scrubbed
-- rather than rebuilt.
--
-- Tab, newline and carriage return are deliberately kept — they are ordinary
-- punctuation in a method.
-- ─────────────────────────────────────────────────────────────────────────────

-- Built from chr() rather than escapes so the pattern itself contains no
-- literal control characters, which do not survive being copied between
-- editors and would make this file unreviewable.
CREATE OR REPLACE FUNCTION tracker.scrub_pattern() RETURNS text
    LANGUAGE sql IMMUTABLE AS
$$
SELECT '[' || chr(1) || '-' || chr(8) || chr(11) || chr(12)
           || chr(14) || '-' || chr(31) || chr(65533) || ']'
$$;

CREATE OR REPLACE FUNCTION tracker.scrub_controls(value text) RETURNS text
    LANGUAGE sql IMMUTABLE STRICT AS
$$
SELECT regexp_replace(value, tracker.scrub_pattern(), '', 'g')
$$;

-- ── the library ──────────────────────────────────────────────────────────────
-- WITH ORDINALITY throughout: jsonb_agg has no inherent order, and silently
-- reshuffling the steps of a recipe would be a far worse bug than the one being
-- fixed here.
UPDATE tracker.recipe_recipes
SET title       = tracker.scrub_controls(title),
    notes       = tracker.scrub_controls(notes),
    tags        = ARRAY(SELECT tracker.scrub_controls(tag) FROM unnest(tags) AS tag),
    ingredients = (
        SELECT coalesce(jsonb_agg(
                   jsonb_build_object(
                       'item',   tracker.scrub_controls(e ->> 'item'),
                       -- to_jsonb(NULL) is SQL NULL, not JSON null, so an
                       -- ingredient with no amount needs saying explicitly.
                       'amount', coalesce(to_jsonb(tracker.scrub_controls(e ->> 'amount')), 'null'::jsonb))
                   ORDER BY ord), '[]'::jsonb)
        FROM jsonb_array_elements(ingredients) WITH ORDINALITY AS t(e, ord)),
    steps       = (
        SELECT coalesce(jsonb_agg(tracker.scrub_controls(value) ORDER BY ord), '[]'::jsonb)
        FROM jsonb_array_elements_text(steps) WITH ORDINALITY AS t(value, ord))
WHERE title ~ tracker.scrub_pattern()
   OR notes ~ tracker.scrub_pattern()
   OR EXISTS (SELECT 1 FROM unnest(tags) AS tag WHERE tag ~ tracker.scrub_pattern())
   OR EXISTS (SELECT 1 FROM jsonb_array_elements_text(steps) AS s WHERE s ~ tracker.scrub_pattern())
   OR EXISTS (SELECT 1 FROM jsonb_array_elements(ingredients) AS e
              WHERE e ->> 'item' ~ tracker.scrub_pattern()
                 OR e ->> 'amount' ~ tracker.scrub_pattern());

-- ── the catalog ──────────────────────────────────────────────────────────────
UPDATE tracker.recipe_catalog
SET title       = tracker.scrub_controls(title),
    ingredients = (
        SELECT coalesce(jsonb_agg(
                   jsonb_build_object(
                       'item',   tracker.scrub_controls(e ->> 'item'),
                       'amount', coalesce(to_jsonb(tracker.scrub_controls(e ->> 'amount')), 'null'::jsonb))
                   ORDER BY ord), '[]'::jsonb)
        FROM jsonb_array_elements(ingredients) WITH ORDINALITY AS t(e, ord)),
    steps       = (
        SELECT coalesce(jsonb_agg(tracker.scrub_controls(value) ORDER BY ord), '[]'::jsonb)
        FROM jsonb_array_elements_text(steps) WITH ORDINALITY AS t(value, ord))
WHERE title ~ tracker.scrub_pattern()
   OR EXISTS (SELECT 1 FROM jsonb_array_elements_text(steps) AS s WHERE s ~ tracker.scrub_pattern())
   OR EXISTS (SELECT 1 FROM jsonb_array_elements(ingredients) AS e
              WHERE e ->> 'item' ~ tracker.scrub_pattern());

DROP FUNCTION tracker.scrub_controls(text);
DROP FUNCTION tracker.scrub_pattern();
