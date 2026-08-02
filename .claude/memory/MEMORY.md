# RandomTaskTrack — Project Memory

This file indexes all memory files for the RandomTaskTrack project. One line per
memory; the file itself holds the detail. See `README.md` for how recall links to
this folder.

## Files

- [project_overview.md](project_overview.md) — What the app is (one task engine + per-domain AI generators, not four apps), stack (.NET 10 + Dapper + Postgres + yuniql, no EF), the `tracker` schema, project layout and endpoint map
- [project_core_engine_2026_08.md](project_core_engine_2026_08.md) — 2026-08-02 core engine build: materialize-don't-compute recurrences + the `ON CONFLICT` idempotency trick, `anchor_mode` from_schedule vs from_completion, timezone handling, the hand-written agent loop and why not `BetaToolRunner`; known gaps (tool confirmation unenforced, no frontend, no first-user bootstrap)
- [reference_cartfees_springboard.md](reference_cartfees_springboard.md) — CartFees-admin v2 as the pattern source: what was copied verbatim (UnitOfWork, BaseOperation, filters, JWT/BCrypt, naming) and the three deliberate deviations (transaction opt-in, typed validation errors, CORS allow-list)
