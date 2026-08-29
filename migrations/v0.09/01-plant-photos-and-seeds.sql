-- ─────────────────────────────────────────────────────────────────────────────
-- Photos, stages, and seed packets.
--
-- Two changes that turn out to be the same change: a plant is no longer only a
-- thing you own and describe in words. It can start as a seed packet, and it
-- can be photographed — at the point of buying it, and again every time it does
-- something new.
-- ─────────────────────────────────────────────────────────────────────────────

--   kind  1 = a plant you have, 2 = a seed packet you have not sown yet
--
-- A column rather than a second table: a packet becomes a plant when it comes
-- up, and that should be one UPDATE, not a copy between tables that loses the
-- photos and the tasks. Everything else about the two is the same shape — a
-- name, a location, a researched profile, tasks on the board.
ALTER TABLE tracker.plant_plants
    ADD COLUMN IF NOT EXISTS kind int NOT NULL DEFAULT 1;

ALTER TABLE tracker.plant_plants
    DROP CONSTRAINT IF EXISTS ck_plant_plants_kind;

ALTER TABLE tracker.plant_plants
    ADD CONSTRAINT ck_plant_plants_kind CHECK (kind IN (1, 2));

-- ─────────────────────────────────────────────────────────────────────────────
-- Photos. Every one is a stage: adding a photo is how you record that something
-- changed, so there is no separate "stage" concept to keep in step with them.
-- The newest is the plant as it looks now; the rest are how it got there.
--
-- The bytes live in Postgres rather than on a disk or in a bucket. The database
-- is the only thing in this app that already has storage in both compose and
-- Kubernetes, and a household's plants are a few megabytes — a volume mount and
-- a second backup story would cost more than it buys. The UI downscales before
-- upload, so a row is a couple of hundred KB.
--
-- ON DELETE CASCADE, unlike the care tasks, which have no foreign key to cascade
-- from and are swept up by DeletePlantOperation instead.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.plant_photos
(
    id         uuid        NOT NULL PRIMARY KEY,
    plant_id   uuid        NOT NULL REFERENCES tracker.plant_plants (id) ON DELETE CASCADE,

    image      bytea       NOT NULL,
    media_type text        NOT NULL,

    -- What the photo shows: "sown", "first true leaves", "flowering". Free text
    -- rather than an enum — the stages of a chilli seedling and of a five-year
    -- old fig have nothing in common, and the lookup suggests one from the photo.
    stage      text        NOT NULL DEFAULT '',

    -- The lookup's read on this photo, or whatever the user typed instead.
    note       text        NOT NULL DEFAULT '',

    taken_on   date        NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

-- Newest first, per plant — the card's only read. created_at breaks ties within
-- a day, which is the common case when several are taken in one go.
CREATE INDEX IF NOT EXISTS ix_plant_photos_plant
    ON tracker.plant_photos (plant_id, taken_on DESC, created_at DESC);
