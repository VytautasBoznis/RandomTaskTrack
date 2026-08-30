-- The tracker learning tasks are filed under. Adding a domain is an INSERT,
-- not a schema change — see task_domains in v0.00.
--
-- Sorted between plants/cooking and general: it is a real tracker, not the
-- catch-all. Idempotent so the migration is safe to re-run.
INSERT INTO tracker.task_domains (id, code, name, sort_order)
VALUES (6, 'learning', 'Learning', 50)
ON CONFLICT (id) DO NOTHING;
