-- Fitness is no longer one of the trackers.
--
-- Domains are referenced by tasks, recurrences and completions, so the row can
-- only be deleted when nothing points at it. On a database that already has
-- fitness history the DELETE matches nothing and the UPDATE takes over:
-- is_active = false hides the domain everywhere (GetAllAsync filters on it)
-- while leaving the completion log readable.
DELETE
FROM tracker.task_domains d
WHERE d.code = 'fitness'
  AND NOT EXISTS (SELECT 1 FROM tracker.task_tasks t WHERE t.domain_id = d.id)
  AND NOT EXISTS (SELECT 1 FROM tracker.task_recurrences r WHERE r.domain_id = d.id)
  AND NOT EXISTS (SELECT 1 FROM tracker.task_completions c WHERE c.domain_id = d.id);

UPDATE tracker.task_domains
SET is_active = false
WHERE code = 'fitness';
