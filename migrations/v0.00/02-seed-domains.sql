-- Seed the initial domains. Idempotent so the migration is safe to re-run.
INSERT INTO tracker.task_domains (id, code, name, sort_order)
VALUES (1, 'fitness', 'Fitness', 10),
       (2, 'house', 'House Cleaning', 20),
       (3, 'plants', 'Plants & Herbs', 30),
       (4, 'cooking', 'Cooking', 40),
       (5, 'general', 'General', 99)
ON CONFLICT (id) DO NOTHING;
