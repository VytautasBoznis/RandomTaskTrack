# Project overview — RandomTaskTrack

Personal task tracker running permanently on a wall tablet, self-hosted and
reachable over the public internet via the owner's own DNS/hardware.

## What it is

One task engine plus per-domain AI "generators", **not** several separate apps.
Domains (house cleaning, plants, cooking) are rows in `tracker.task_domains` —
adding a new tracker is an INSERT plus a system-prompt tweak, never a schema
change. Fitness was dropped in `v0.01`.

The one scope that is *not* just a domain is **recipes**: it pulls a dish a week
from Spoonacular, rotating cuisine families, and can put that dish on the board
as an ordinary cooking task.

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
- **Recipes** — same shape behind `IRecipeSource`, but registered as
  `HybridRecipeSource`: Spoonacular drives the weekly cuisine rotation (it has
  the cuisine labels, images and timings), the local `recipe_catalog` drives
  targeted search (Spoonacular's catalogue is thin outside Western cooking).
  `NullRecipeSource` when `Recipes:ApiKey` is empty — search still works.
- **Frontend** — React + TypeScript, no router: `App.tsx` switches six tabs
  (Today / Recurring / Recipes / Notes / Log / Chat) and each remounts on switch
  so it re-reads. Note bodies are markdown, rendered with `react-markdown` +
  `remark-gfm` (raw HTML left disabled, so no sanitizer).

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
| `recipe_families` | Cuisine families, in the source's own vocabulary. The weekly pick rotates to whichever has gone longest unused. |
| `recipe_recipes` | The library — every dish ever pulled or saved, plus rating, notes and tags. Mostly uncooked; it is a pool, not an exclusion list. The `not picked` tag takes one out of the rotation without hiding it. |
| `recipe_picks` | One dish per ISO week, and the "already cooked" list. Partial unique index on `(week_of) WHERE status = 1`. |
| `recipe_catalog` | Opt-in bulk corpus (RecipeNLG, 2.2M) that targeted search reads. Read-only reference — never picked directly, copied into `recipe_recipes` when saved. |
| `note_notes` | Free-form markdown, attached to nothing — no domain, no schedule, no completion. |

Domain-specific payloads live in `jsonb` `data` columns (sets/reps/weight, water
ml, recipe id) rather than in per-domain tables.

## Endpoints

`/api/auth` (login, register, change-password) · `/api/domains` ·
`/api/tasks` (+ `/dashboard`, `/completions`, `/{id}/complete`) ·
`/api/recurrences` · `/api/recipes` (weekly, reroll, task, search, library, pick,
history, `PUT /{id}`) · `/api/notes` ·
`/api/chat` (conversations, messages) · `/health`

## Not built yet

Domain-specific payload UI (sets/reps, water ml) — `data` jsonb is written by
the AI and read by nothing in the frontend; confirmation round-trip for the
destructive AI tools (`RequiresConfirmation` is set on the definitions but the
API always auto-executes); clearing an optional field through a partial update
(null means "leave alone", so due time and end dates can be set but not removed).
