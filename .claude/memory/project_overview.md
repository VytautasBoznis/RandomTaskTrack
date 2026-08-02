# Project overview — RandomTaskTrack

Personal task tracker running permanently on a wall tablet, self-hosted and
reachable over the public internet via the owner's own DNS/hardware.

## What it is

One task engine plus per-domain AI "generators", **not** four separate apps.
Domains (fitness, house cleaning, plants, cooking) are rows in
`tracker.task_domains` — adding a new tracker is an INSERT plus a system-prompt
tweak, never a schema change.

The load-bearing idea: the **completion log** records what *actually* happened
(`actual_data`) next to what was planned (`planned_data`). That difference is
what makes progress charts and AI plan-adjustment possible; without it the app
is just checkboxes.

## Stack

- **Backend** — C# / .NET 10, four projects: `.API` / `.Business` / `.Data` /
  `.Interfaces`. Patterns lifted wholesale from `CartFees-admin` v2 (see
  [reference_cartfees_springboard.md](reference_cartfees_springboard.md)).
- **Data** — PostgreSQL + **Dapper**. No EF, deliberately. Schema-first: SQL in
  `migrations/` is the source of truth, C# models are hand-written to match.
- **Migrations** — yuniql. Never edit an applied `vX.XX` directory (checksummed);
  add the next version instead.
- **AI** — swappable behind `IAiProvider`. Anthropic adapter ships;
  `NullAiProvider` is registered when no API key is set so the app still boots.
- **Frontend** — React + TypeScript. Not built yet.

## Layout

```
migrations/            yuniql workspace (_init, _pre, v0.00, _post, _draft, _erase)
server/
  RandomTaskTrack.API/         controllers, filters, DI extensions, Program.cs
  RandomTaskTrack.Business/    operations, repositories, Ai/, Services/, Base/
  RandomTaskTrack.Data/        models, DTOs, requests, responses, validators
  RandomTaskTrack.Interfaces/  IUnitOfWork, repository + AI interfaces
docker-compose.yml     db → migrate (one-shot yuniql) → api
```

## Schema (`tracker` schema)

| Table | Purpose |
|---|---|
| `user_users` | Auth. Single user in practice; role enum kept for admin-gated endpoints. |
| `task_domains` | The trackers. Seeded with fitness/house/plants/cooking/general. |
| `task_recurrences` | Rule + payload template that spawns instances. |
| `task_tasks` | Dated instances, ad-hoc and materialized alike — one dashboard query. |
| `task_completions` | Append-only. planned vs actual. Never updated in place. |
| `chat_conversations` / `chat_messages` | Provider-neutral chat history. |

Domain-specific payloads live in `jsonb` `data` columns (sets/reps/weight, water
ml, recipe id) rather than in per-domain tables.

## Endpoints

`/api/auth` (login, register, change-password) · `/api/domains` ·
`/api/tasks` (+ `/dashboard`, `/completions`, `/{id}/complete`) ·
`/api/recurrences` · `/api/chat` (conversations, messages) · `/health`

## Not built yet

React frontend; any domain-specific UI (fitness charts, recipe view, spin-the-
wheel meal picker); confirmation round-trip for the destructive AI tools
(`RequiresConfirmation` is set on the definitions but the API always auto-executes).
