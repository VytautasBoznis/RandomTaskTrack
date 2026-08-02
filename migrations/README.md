# Migrations (yuniql)

Schema-first. The SQL in this folder is the source of truth for the database —
the C# models are hand-written to match it, not the other way round. There is no
ORM-generated schema anywhere in this repo.

## Layout

| Directory | When it runs |
|---|---|
| `_init`  | Once, on a brand-new database, before any version |
| `_pre`   | Before every migration run |
| `vX.XX`  | Once each, in version order — the actual schema history |
| `_post`  | After every migration run (views/functions — must be idempotent) |
| `_draft` | Every run, untracked — scratch space while iterating |
| `_erase` | Only on an explicit `yuniql erase` |

## Adding a change

Never edit an already-applied `vX.XX` directory — yuniql records a checksum and
will refuse the run. Create the next version instead:

```bash
mkdir migrations/v0.01
# migrations/v0.01/01-add-whatever.sql
```

Numeric file prefixes (`01-`, `02-`) control order within a version.

## Running

Compose brings up a one-shot `migrate` service that runs before the API:

```bash
docker compose up migrate
```

Against a local database directly, either use the Docker CLI image:

```bash
docker run --rm -v "$(pwd)/migrations:/data" yuniql/cli:linux-x64-latest \
  run --platform postgresql -a \
  -c "Host=localhost;Port=5432;Database=random_task_track;Username=rtt;Password=rtt"
```

…or install the .NET global tool once and run it from the repo root:

```bash
dotnet tool install -g Yuniql.CLI
yuniql run -p ./migrations --platform postgresql -a -c "<connection string>"
yuniql list -p ./migrations --platform postgresql -c "<connection string>"
```

`-a` / `--auto-create-db` creates the database if it does not exist.
