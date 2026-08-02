# RandomTaskTrack

Personal task tracker for a wall tablet. One task engine, several AI-driven
trackers (fitness, house cleaning, plants, cooking) on top of it.

Backend patterns are lifted from `CartFees-admin` v2: four-project split,
`UnitOfWork` + Dapper, the operation/validator pattern, JWT + BCrypt auth,
Serilog. **No EF** — the SQL under `migrations/` is the source of truth and the
C# models are written to match it.

## Running it

```bash
cp .env.example .env      # then fill in DB_PASSWORD, JWT_SECRET_KEY, AI_API_KEY
docker compose up -d
```

Compose ordering is `db` → `migrate` (one-shot yuniql) → `api`, so the API can
never start against an un-migrated database. Health check on
`http://localhost:5080/health`.

Leaving `AI_API_KEY` empty is supported: chat returns a clear error and
everything else works.

### First user

`/api/auth/register` is admin-gated, so the first account is inserted by hand:

```sql
INSERT INTO tracker.user_users (id, email, password, role)
VALUES (gen_random_uuid(), 'you@example.com', '<bcrypt hash, work factor 12>', 999);
```

## Layout

```
migrations/   yuniql workspace — see migrations/README.md
server/
  RandomTaskTrack.API/         controllers, filters, DI wiring, Program.cs
  RandomTaskTrack.Business/    operations, repositories, Ai/, Services/
  RandomTaskTrack.Data/        models, DTOs, requests/responses, validators
  RandomTaskTrack.Interfaces/  IUnitOfWork, repository + AI interfaces
```

## API

| Method | Route | |
|---|---|---|
| POST | `/api/auth/login` | |
| POST | `/api/auth/register` | admin only |
| POST | `/api/auth/change-password` | |
| GET | `/api/domains` | the trackers |
| GET | `/api/tasks/dashboard` | overdue / today / upcoming / done today / streaks — one call |
| GET | `/api/tasks` | filter by domain, date range, status, title |
| GET | `/api/tasks/completions` | the planned-vs-actual log |
| POST | `/api/tasks` · PUT `/api/tasks/{id}` · DELETE `/api/tasks/{id}` | |
| POST | `/api/tasks/{id}/complete` | tick + log + chain the next occurrence |
| GET/POST/PUT/DELETE | `/api/recurrences` | |
| POST | `/api/chat/messages` | one agent turn |
| GET | `/api/chat/conversations`, `/api/chat/conversations/{id}` | |

Tokens last **30 days** — the tablet is a permanently signed-in kiosk.

## Two things worth knowing before changing anything

**Recurrences are materialized, not computed.** Instances are written into
`task_tasks` ahead of time (21-day horizon, hourly background sweep), so the
dashboard is a plain indexed query and any single instance can be edited. The
partial unique index on `(recurrence_id, due_on)` makes the sweep idempotent.

**`anchor_mode` decides what "late" means.** `from_schedule` holds a fixed
cadence; `from_completion` restarts the interval from the actual completion date
— clean the bathroom on day 9 of a 7-day cycle and the next one is day 16, not
day 14. Chosen per recurrence, because there is no right default.

## Swapping the AI provider

`IAiProvider` is one method: `CompleteAsync(AiRequest, CancellationToken)`. Tool
definitions are raw JSON Schema, so they pass to any provider unchanged. Adding
one means a class in `Business/Ai/Providers/` and a case in
`ServiceCollectionExtensions.AddAiServices`.

Provider-specific settings (Anthropic effort/thinking, etc.) live in
`Ai:ProviderOptions` and are read only by the adapter that understands them —
deliberately not on the interface, since forcing them into a shared shape would
reduce it to a lowest common denominator.
