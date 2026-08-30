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
everything else works — a plant added without it is saved unidentified, with the
lookup offered again on its card.

`AI_WEB_SEARCH` (default true) lets the plant lookup use Anthropic's server-side
search. It is billed per search on top of tokens; set it false for a key whose
organisation has not enabled the tool, and the lookup answers from what the
model already knows.

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
  RandomTaskTrack.Business/    operations, repositories, Ai/, Finance/, Services/
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
| DELETE | `/api/recipes/pick` | clear the week's dish without picking another |
| GET | `/api/recipes/history` | the cookbook — filter by `search`, `tags`, `cooked` |
| PUT | `/api/recipes/{id}` | rating, notes, tags |
| GET | `/api/recipes/catalog` | bulk catalog status — loaded, running, progress |
| POST | `/api/recipes/catalog/import` | starts the bulk load in the background |
| GET | `/api/plants` | every plant, its photos, care schedule and pending tasks — one call |
| POST | `/api/plants` | add a plant or a seed packet; a photo and/or a description identifies it |
| PUT | `/api/plants/{id}` · DELETE `/api/plants/{id}` | delete takes the photos, schedules and pending tasks with it |
| POST | `/api/plants/{id}/research` | ask again, with a better description and the newest photo |
| POST | `/api/plants/{id}/photos` | add a photo, which is also a stage — the AI labels it |
| GET | `/api/plants/photos/{id}` · DELETE | the image bytes · remove a stage |
| POST | `/api/plants/{id}/schedule` | turn chosen care lines into recurrences |
| POST | `/api/plants/{id}/sowing` | date a seed packet's plan from the day it gets sown |
| GET | `/api/finance/overview` | accounts, cash, deposits, positions, flows, targets — one call |
| GET | `/api/finance/projection` | the monthly series; `months`, `historyMonths`, `stockGrowth` |
| POST | `/api/finance/prices/refresh` | pull share prices and FX rates |
| GET/POST/PUT/DELETE | `/api/finance/entries` | the cash ledger |
| POST/PUT/DELETE | `/api/finance/flows` | recurring income and expenses |
| POST/PUT/DELETE | `/api/finance/accounts` | the pots the money sits in |
| POST | `/api/finance/accounts/{id}/balance` | type the balance you can see; logs the difference |
| POST/PUT/DELETE | `/api/finance/holdings` · `/trades` · `/dividends` · `/deposits` · `/targets` | |
| GET | `/api/learning` | every path with its steps, and every credential held — one call |
| POST/PUT/DELETE | `/api/learning/goals[/{id}]` | a path: why, expected benefits, "prepared by" |
| POST | `/api/learning/goals/{id}/plan` | draft or re-draft the route; committed steps survive it |
| POST | `/api/learning/goals/{id}/steps` | commit chosen lines of the plan to the path |
| PUT/DELETE | `/api/learning/steps/{id}` | status, dates, and the result — the grade or the retake |
| POST | `/api/learning/steps/{id}/task` | put a step on the board as a dated task |
| POST/PUT/DELETE | `/api/learning/credentials[/{id}]` | what you already hold; PUT is also how a renewal is recorded |
| POST | `/api/learning/credentials/{id}/renewal` | look up whether it expires, and how it renews |
| POST | `/api/learning/credentials/{id}/reminder` | a dated renewal on the board; rejected for a permanent one |
| GET/POST/PUT/DELETE | `/api/notes` | markdown notes, newest edit first |
| POST | `/api/chat/messages` | one agent turn |
| GET | `/api/chat/conversations`, `/api/chat/conversations/{id}` | |

Tokens last **30 days** — the tablet is a permanently signed-in kiosk.

## Things worth knowing before changing anything

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

**The rotation and search use different backends, on purpose.**
`HybridRecipeSource` sends `PullAsync` to Spoonacular and `SearchAsync` to the
local catalog, because they are good at different things. Spoonacular has
cuisine labels, images, cook times and servings — everything the weekly rotation
picks by and the dish card is built from. What it does not have is breadth:
measured on the live API, `ramen` is 5 dishes, `sushi` 3, `pad thai` and
`bibimbap` 0. No paid tier changes that; their plans sell quota, not recipes.
The same searches against `tracker.recipe_catalog` return 1,061, 996, 490 and
93. Search falls back to the API when the catalog is empty, so the tab works
before anyone presses Load.

The catalog is **two** feeds, because one could not do the job alone. RecipeNLG
(2.2M) has the breadth but stores nothing except title, ingredients and method —
so a search for "chicken ramen" returned three dishes all called "Chicken Ramen"
with no photo, no timing and no way to choose between them. AllRecipes (32,722,
77% with a photo, all with times and servings) has the opposite problem: lovely
cards, thin coverage. Both are imported and search sorts `image_url IS NULL`
last, so pictured dishes come first and the long tail is still there underneath.
`feed` on each row is what lets "check for new" add the second corpus without
re-downloading the first.

**Plants are joined to their tasks by payload, not by a foreign key.** A care
task carries `{"plantId": …, "careTitle": …}` in `data`, and the materializer
already copies a recurrence's `data` verbatim onto every instance it spawns —
so one schedule keeps labelling its tasks for free, and neither `task_tasks` nor
`task_recurrences` grows a column for a scope they otherwise know nothing about.
The price is that nothing in the database stops a watering task outliving the
plant, which is why `DeletePlantOperation` sweeps up the schedules and the
pending tasks itself. `careTitle` is there so the tab can tell an
already-scheduled suggestion from a new one without reproducing the
`"Water — the big one"` title format the dashboard needs.

Care schedules are anchored `from_completion`. Watering three days late means
the next one is three days later too — with `from_schedule` a holiday comes back
as a column of overdue rows nobody can act on.

**The plant lookup is one completion, and it is allowed to fail.** No tools of
ours, no conversation, no stored chat: `IPlantResearcher` asks the model to
answer in JSON and parses what comes back. Failure is not fatal on the way in —
running with `AI_API_KEY` empty is supported, so a plant added without a lookup
is still a plant and the card offers to retry. Only pressing "look it up"
reports the failure as an error, because that button does exactly one thing.

**A photo is worth more than the description, and a seed packet is mostly a
name.** The lookup takes an image, and the image outranks the words when they
disagree — people misremember what the garden centre told them. A packet is the
harder case and gets its own prompt: a phone photo of foil small print is not
readable and the prompt says so, so the model is told to get the *variety* off
the front and look the rest up. That is why the lookup has web search on
(`AiRequest.AllowWebSearch`, vetoable per deployment with `AI_WEB_SEARCH`) —
sowing depth and days-to-harvest are cultivar-specific, published, and not
reliably in a model's memory.

Web search is a *server-side* tool, so nothing here executes anything, but it
can come back with `stop_reason: pause_turn`. Resuming is confined to
`AnthropicAiProvider`: it hands the assistant's partial turn back and asks
again, up to four times, so `CompleteAsync` still returns a finished turn to
everything above it. The response blocks are converted to request blocks through
JSON, which is the only conversion the SDK offers — and the one that keeps each
search result's `encrypted_content`, without which the resumed request is
rejected.

**Every photo is a stage.** There is no separate "mark a stage" anywhere:
photographing something is how a change gets recorded, so an upload runs a
second, cheaper AI call that says what the picture shows ("first true leaves",
"looking leggy") and that becomes the label. Best-effort, like the lookup — a
photo is worth keeping whether or not anything could be said about it. A
hand-typed stage skips the call entirely.

The bytes live in Postgres. It is the only thing in this app that already has
storage in both compose and Kubernetes, a household's plants are a few
megabytes, and a volume mount plus a second backup story would cost more than it
buys. The browser downscales to 1568px — the size the model reduces images to
anyway — before uploading, so a row is a couple of hundred KB. `plant_photos`
cascades on delete, unlike the care tasks, which have no foreign key to cascade
from. The list queries never select the `image` column; the UI fetches each
photo separately, with the bearer token, because an `<img>` tag cannot send a
header.

**A seed packet is a plant you do not have yet** — same table, `kind = 2`. When
it comes up it becomes a plant with one UPDATE, keeping the photos and the tasks
it already has; a separate seeds table would have made sprouting a copy that
loses both. What differs is the schedule: a packet's plan is *dated one-off
tasks* (sow → germinate → pot on → harden off → plant out → first harvest),
generated as day-offsets from whichever day it actually gets sown, while care is
intervals. A repeating "sow" would be nonsense, and sowing a fortnight late
should move the whole chain a fortnight.

The profile is stored as one jsonb blob rather than fifteen columns, for the
reason `recipe_recipes.ingredients` is: the UI renders it, nothing queries it,
and a prompt that learns to return one more field should not be a migration.
`species` and `latin_name` are the exception — lifted out into columns because
they are what a human corrects by hand, and a re-lookup keeps a hand-typed
correction rather than overwriting it.

**Finance computes, it does not materialize** — the deliberate reverse of the
task engine above. Task instances are written into `task_tasks` ahead of time
because each one has to be individually editable over a 21-day horizon. A
30-year projection is hundreds of monthly buckets nobody edits that change
wholesale the moment one flow changes, so `FinanceProjector` derives them on
read and no future row is ever stored. Financial cadence is its own four-value
enum rather than `task_recurrences`: that table carries a `domain_id`, an
`anchor_mode` defined in terms of task *completion*, and a materializer, none of
which mean anything to a salary on the 25th.

**Cash and assets must never double-count.** `fin_entries` is a cash ledger and
nothing else — income received, expenses paid — and current cash is derived from
it rather than typed into a box. Deposits and holdings are assets valued
separately, and their *past* cash movements are already inside that balance:
buying a share three years ago is why the cash is lower now, so subtracting it
again would count it twice. The only future cash an asset produces is a deposit
maturing and a dividend landing. The corollary that bit once already: a deposit
maturing inside a month must drop out of the deposits column in the same month
its value lands in cash, or net worth jumps by a whole deposit for one month —
that is what `FinanceProjector.StillHeld` is for.

**Balances are derived too, per account.** `fin_accounts` has no balance column.
An account's balance is its entries plus what its deposits have moved, computed
in `FinanceProjector.BuildAccounts`, and "Set balance" writes one *Balance
adjustment* entry for the difference rather than storing the number. A stored
total would be the single figure in the scope that could disagree with the
ledger under it, and it would start disagreeing the first time an old entry was
corrected. Every entry and every holding names an account; a symbol is unique
per account rather than globally, so the same ETF in a brokerage and a pension
is two positions.

**A deposit moves its own money.** `source_account_id` and `target_account_id`
mean the principal leaves the source while the deposit is open and principal
plus interest lands in the target once `matures_on` has passed — both derived,
so nothing runs on the maturity date and deleting the deposit undoes both
halves. Never log an entry for either leg. Both columns are nullable because
deposits predating `v0.10` had their transfer logged by hand, and attaching an
account retroactively would subtract the same money twice. The overlap in
`FinanceProjector` (`windowFrom`) is what stops a deposit that opens or matures
later *this* month falling into the gap between the anchor and the first
projected bucket.

Two consequences worth knowing. Net worth is projected **forward only**: valuing
holdings in the past would need historical prices this app does not store, so
the months behind today carry actual income and expenses from the ledger and no
balances at all. And prices come from Yahoo's chart endpoint — no key, no
account, one request per symbol with a browser User-Agent, which it needs. It is
an undocumented endpoint rather than a published API; `IStockPriceSource` is one
method, so replacing it is a class in `Business/Finance/Sources/` and a case in
`AddFinanceServices`. FX rides along, since a currency pair is just another
symbol (`EURUSD=X`).

**Learning splits what the AI said from what you agreed to.** `learn_goals.plan`
is a jsonb blob — phases, certifications worth sitting, courses and labs,
projects by level — that the UI renders and nothing queries, exactly as
`plant_plants.profile` does. A `learn_steps` row is a line you accepted. That
split is what makes re-drafting cheap: asking again replaces the suggestion and
leaves every commitment, date and grade untouched. They also have different
lifetimes — a plan is read whole and replaced whole, a step is edited one at a
time — which is why a single table with a `parent_id` would have been wrong.

A step carries `notes` (what to do) and `outcome` (what happened: the grade, the
mark breakdown, "failed the lab section, retake booked 12 Jan"). Those two
columns plus `kind = assignment` are the whole of coursework tracking; a
separate assignments table would have bought a weighted average and cost a
second CRUD stack.

**Not every credential expires, so the expiry is a tri-state.**
`learn_credentials.renewal_kind` is permanent, expires or unknown, and
`ck_learn_credentials_renewal` keeps it agreeing with `expires_on`. A nullable
date alone cannot tell "never expires" from "nobody has checked", and
conflating them either nags forever about an older MCSD — permanent, and
rightly invisible to every renewal list — or lets a real expiry pass unwatched.
The lookup fills this in only when neither a kind nor a date has been given:
the person holding the certificate knows what it says, and a search result
should not talk them out of it. An answer that claims permanence *and* a
validity period is stored as unknown rather than as a guessed date.

There is deliberately **no table of provider renewal rules** anywhere in the
code. Microsoft moved to annual free renewals in 2022 while leaving the older
certifications permanent; AWS and ISC2 differ again. A hardcoded table would
have gone quietly wrong that day, so the rules are looked up per credential,
web-search backed, stored with the date they were checked, and overridable by
hand.

**Renewals are derived, not materialized** — the same call Finance makes.
"Expires in 47 days" is computed on read; a reminder task exists only once you
press for it, as a one-off dated task rather than a yearly recurrence. Renewing
early moves the expiry, so a recurrence would drift off the real date within one
cycle, and automatic materialization would need a rule for finding and removing
the reminder it had already written.

The catalog is opt-in and loads from the Recipes tab — 2.2M recipes, ~2GB,
streamed straight into Postgres by `RecipeCatalogImporter` with no key and no
quota. It runs in the background and the tab polls. Re-running is incremental:
rows land in a temp table and cross over with `ON CONFLICT DO NOTHING`, so a
second run adds only what is new and a pod that dies mid-import leaves nothing
half-written. `Recipes:CatalogMaxRows` caps it if 2GB is too much.

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
