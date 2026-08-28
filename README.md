# RandomTaskTrack

Personal task tracker for a wall tablet. One task engine, several AI-driven
trackers (house cleaning, plants, cooking) on top of it.

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

### UI

```bash
cd ui && npm install && npm run dev   # :3000, proxies /api to :5080
```

A stub: sign in, then one `/api/tasks/dashboard` call rendered into buckets.
Creating and completing tasks is still API-only.

### First user

Register from the link on the sign-in screen. `/api/auth/register` is open —
this is a single-household app on a local network, not a public service. New
accounts get role `User` (1); nothing is admin-gated, so that is enough for
everything the app does.

## Layout

```
migrations/   yuniql workspace — see migrations/README.md
server/
  RandomTaskTrack.API/         controllers, filters, DI wiring, Program.cs
  RandomTaskTrack.Business/    operations, repositories, Ai/, Services/
  RandomTaskTrack.Data/        models, DTOs, requests/responses, validators
  RandomTaskTrack.Interfaces/  IUnitOfWork, repository + AI interfaces
ui/           React + TypeScript (Vite), served by nginx
deploy/helm/  the chart — api, ui, Postgres, migrate Job, ingress
Jenkinsfile   build → push → deploy
```

## API

| Method | Route | |
|---|---|---|
| POST | `/api/auth/login` | |
| POST | `/api/auth/register` | open — see "First user" |
| POST | `/api/auth/change-password` | |
| GET | `/api/domains` | the trackers |
| GET | `/api/tasks/dashboard` | overdue / today / upcoming / done today / streaks — one call |
| GET | `/api/tasks` | filter by domain, date range, status, title |
| GET | `/api/tasks/completions` | the planned-vs-actual log |
| POST | `/api/tasks` · PUT `/api/tasks/{id}` · DELETE `/api/tasks/{id}` | |
| POST | `/api/tasks/{id}/complete` | tick + log + chain the next occurrence |
| GET/POST/PUT/DELETE | `/api/recurrences` | |
| GET | `/api/recipes/weekly` | this week's dish; pulls one if the week has none |
| POST | `/api/recipes/reroll` · `/api/recipes/task` | swap the dish · put it on the board |
| GET | `/api/recipes/search?query=` | ask the source by name — saves nothing |
| POST | `/api/recipes/library` | bank the search results worth keeping |
| POST | `/api/recipes/pick` | cook a named library dish this week |
| GET | `/api/recipes/history` | the cookbook — filter by `search`, `tags`, `cooked` |
| PUT | `/api/recipes/{id}` | rating, notes, tags |
| GET/POST/PUT/DELETE | `/api/notes` | markdown notes, newest edit first |
| POST | `/api/chat/messages` | one agent turn |
| GET | `/api/chat/conversations`, `/api/chat/conversations/{id}` | |

Tokens last **30 days** — the tablet is a permanently signed-in kiosk.

## Three things worth knowing before changing anything

**Recurrences are materialized, not computed.** Instances are written into
`task_tasks` ahead of time (21-day horizon, hourly background sweep), so the
dashboard is a plain indexed query and any single instance can be edited. The
partial unique index on `(recurrence_id, due_on)` makes the sweep idempotent.

**`anchor_mode` decides what "late" means.** `from_schedule` holds a fixed
cadence; `from_completion` restarts the interval from the actual completion date
— clean the bathroom on day 9 of a 7-day cycle and the next one is day 16, not
day 14. Chosen per recurrence, because there is no right default.

**`recipe_recipes` is a library, not an already-seen list.** A pull asks the
source for ten dishes and banks all ten, so one unit of quota is worth ten
dishes and rerolling is usually free. What stops a repeat is `recipe_picks`: a
dish that was once the weekly dish (`status = 1`) is spent for good, a rerolled
one (`status = 2`) is only out until the week turns over, and anything with
neither is in the pool. That predicate lives in `RecipesRepository.InThePool`
and is the definition the rotation, the history filter and the cookbook all
share. It used to be the other way round — the library *was* the exclusion list,
and each pull threw nine dishes away — which is what made the weekly pick
eventually answer "every candidate has been cooked already".

Banking whole pulls means dishes arrive that nobody chose, so the `not picked`
tag (`RecipeTags.NotPicked`) takes one out of the rotation for good. It is a
plain tag, not a column, which is the point: a skipped dish still shows in
History, is still found by its other tags, and can still be cooked by naming it
outright — it just never comes back on a reroll.

## Kubernetes

`deploy/helm/randomtasktrack` deploys what compose does — API, UI, Postgres and
a one-shot yuniql migration — behind a single ingress host.

```bash
helm upgrade --install randomtasktrack deploy/helm/randomtasktrack \
  --namespace randomtasktrack --create-namespace \
  --set secrets.dbPassword=... \
  --set secrets.jwtSecretKey=... \
  --set ingress.host=tasks.example.com
```

`dbPassword` and `jwtSecretKey` have no defaults — the render fails without
them, the same way compose fails on an unset `${DB_PASSWORD}`. Set
`postgres.enabled=false` and `postgres.host` to use a database you run yourself.

Two things differ from compose, both forced by Kubernetes:

**The migrations arrive as an image.** There is no host directory to bind-mount
into `yuniql/cli`, so `migrations/Dockerfile` bakes the workspace in. Same CLI,
same arguments.

**Ordering is an init container, not a hook.** Compose gets "the API never
starts against an un-migrated database" from `depends_on`. A Helm `pre-install`
hook runs before Postgres exists and a `post-install` hook deadlocks `--wait`,
so the migrate Job is a plain resource and the API blocks on an init container
until `tracker.task_domains` answers.

The ingress sends `/api` and `/health` to the API and everything else to the
UI. They are same-origin there, so CORS never comes into play in a cluster.

## CI

`Jenkinsfile` is a multibranch pipeline. Every branch builds and type-checks the
server, the UI and the chart; `main` additionally builds the three images
tagged with the git SHA, pushes them, and runs `helm upgrade --install`.

It expects five credentials on the controller: `rtt-registry`,
`rtt-kubeconfig`, `rtt-db-password`, `rtt-jwt-secret-key`, `rtt-ai-api-key`.

## Swapping the AI provider

`IAiProvider` is one method: `CompleteAsync(AiRequest, CancellationToken)`. Tool
definitions are raw JSON Schema, so they pass to any provider unchanged. Adding
one means a class in `Business/Ai/Providers/` and a case in
`ServiceCollectionExtensions.AddAiServices`.

Provider-specific settings (Anthropic effort/thinking, etc.) live in
`Ai:ProviderOptions` and are read only by the adapter that understands them —
deliberately not on the interface, since forcing them into a shared shape would
reduce it to a lowest common denominator.
