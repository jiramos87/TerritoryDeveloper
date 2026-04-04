# PostgreSQL — IA dev setup (TECH-44b + TECH-44c + TECH-55 / TECH-55b)

**Scope:** Local or shared **dev** database for **Information Architecture** tables (`glossary`, `spec_sections`, `invariants`, `relationships`), the **dev repro bundle registry** (`dev_repro_bundle`, **TECH-44c**), and the **per-export Editor Reports registry** (**TECH-55** — `editor_export_*` tables). This is **not** player **Save data** or **Load pipeline** input — see [`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md).

## Prerequisites

- **PostgreSQL** 14+ (server)
- **`psql`** client on `PATH` (for `apply-migrations.mjs`)
- **Node.js** 18+ (for migration runner and read/seed scripts)

## Environment

Set **`DATABASE_URL`** to a PostgreSQL connection URI (never commit secrets). Names only — see repository **`.env.example`**.

Example:

```bash
export DATABASE_URL='postgresql://postgres:postgres@127.0.0.1:5432/territory_ia_dev'
```

Optional: create a `.env` file at the repo root (gitignored) and load it before running scripts, e.g. `set -a && source .env && set +a` (shell-dependent).

## Quick start (Docker)

One-off server (adjust password/port as needed):

```bash
docker run --name territory-ia-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=territory_ia_dev -p 5432:5432 -d postgres:16
export DATABASE_URL='postgresql://postgres:postgres@127.0.0.1:5432/territory_ia_dev'
```

## Apply migrations

From the repository root:

```bash
npm --prefix tools/postgres-ia install
npm --prefix tools/postgres-ia run migrate
```

Equivalent **manual** path (no Node migration bookkeeping for `schema_migrations` — prefer the script above):

```bash
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f db/migrations/0001_ia_tables.sql
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f db/migrations/0002_ia_read_surface.sql
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f db/migrations/0003_dev_repro_bundle.sql
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f db/migrations/0004_editor_export_tables.sql
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f db/migrations/0005_editor_export_document.sql
```

If you use manual `psql` on a fresh DB, insert migration rows so the Node runner does not re-apply:

```sql
INSERT INTO schema_migrations (version) VALUES ('0001_ia_tables'), ('0002_ia_read_surface'), ('0003_dev_repro_bundle'), ('0004_editor_export_tables'), ('0005_editor_export_document');
```

## Optional glossary seed

Upserts the first **20** parsed rows from [`.cursor/specs/glossary.md`](../.cursor/specs/glossary.md) (override with **`SEED_GLOSSARY_MAX`**):

```bash
npm --prefix tools/postgres-ia run seed:glossary
```

## Read path smoke test (glossary by key)

After seeding, **`heightmap`** should resolve (derived from **HeightMap** — slug is lowercase alphanumerics):

```bash
npm --prefix tools/postgres-ia run glossary-by-key -- heightmap
```

**`psql`** equivalent:

```sql
SELECT * FROM ia_glossary_row_by_key('heightmap');
```

## Dev repro bundle registry (**TECH-44c**, **E1**)

Registers **metadata** for **Editor** exports under **`tools/reports/`** (paths are **gitignored** — see [`.cursor/specs/unity-development-context.md`](../.cursor/specs/unity-development-context.md) **§10**): **Agent context** JSON (`agent-context-*.json`), optional **Sorting debug** Markdown (`sorting-debug-*.md`). Rows use **B1** shape: scalars + **`payload jsonb`** with **Interchange JSON**–style keys **`artifact`**: `dev_repro_bundle`, **`schema_version`**: `1` (see [`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md)).

**Table:** `dev_repro_bundle` — columns `backlog_issue_id`, `git_sha`, `exported_at_utc` (defaults to **INSERT** time), `interchange_revision` (mirrors **`schema_version`** for this artifact), `payload`.

**`backlog_issue_id`:** Store the canonical form matching **territory-ia** `normalizeIssueId` (e.g. `bug-37` → `BUG-37`, `FEAT-37B` → `FEAT-37b`). The **`register-dev-repro.mjs`** script applies the same rules.

**Example INSERT** (adjust paths to your export files):

```sql
INSERT INTO dev_repro_bundle (backlog_issue_id, git_sha, interchange_revision, payload)
VALUES (
  'TECH-44c',
  'a1b2c3d4e5f678901234567890abcdef12345678',
  1,
  '{
    "artifact": "dev_repro_bundle",
    "schema_version": 1,
    "agent_context_relative_path": "tools/reports/agent-context-2026-04-04T12-00-00Z.json",
    "sorting_debug_relative_path": "tools/reports/sorting-debug-2026-04-04T12-00-00Z.md",
    "notes": "optional"
  }'::jsonb
);
```

**Example SELECT** (latest rows for an issue):

```sql
SELECT * FROM dev_repro_list_by_issue('TECH-44c', 10);
```

**CLI registration** (uses `git rev-parse HEAD` for SHA unless `--sha` is set):

```bash
npm run db:register-repro -- --issue TECH-44c \
  --agent-context tools/reports/agent-context-2026-04-04T12-00-00Z.json
```

## Editor export registry (**TECH-55** + **TECH-55b**)

**DB-first (TECH-55b):** when **`DATABASE_URL`** resolves (process environment, **EditorPrefs** `TerritoryDeveloper.EditorExportRegistry.DatabaseUrl`, or repo-root **`.env.local`** `DATABASE_URL=…`), **Unity** tries **Postgres** first: the full export body is stored in column **`document jsonb`** (plus **`payload jsonb`** metadata). **GIN** indexes support **`jsonb_path_ops`** queries on **`document`**. If the insert fails or no URL is set, the Editor writes the same content under **`tools/reports/`** (gitignored) with the usual filenames — see [`.cursor/specs/unity-development-context.md`](../.cursor/specs/unity-development-context.md) **§10**.

Migrations: **`0004_editor_export_tables.sql`**, **`0005_editor_export_document.sql`** (`backlog_issue_id` nullable; **`document`** required on new inserts).

| Table | Menu item |
|-------|-----------|
| `editor_export_agent_context` | **Export Agent Context** |
| `editor_export_sorting_debug` | **Export Sorting Debug (Markdown)** |
| `editor_export_terrain_cell_chunk` | **Export Cell Chunk (Interchange)** (Play Mode) |
| `editor_export_world_snapshot_dev` | **Export World Snapshot (Dev Interchange)** (Play Mode) |

**Settings:** **Territory Developer → Reports → Postgres registry — settings…** — optional **`backlog_issue_id`** (metadata for SQL filters; may be empty), optional **`DATABASE_URL`** (local only), optional **Node executable** path, optional verbose logging after successful inserts.

**Node / Unity on macOS:** If the Console shows `Registry invocation failed` with **Cannot find the specified file** and **`ApplicationName='node'`**, Unity’s process **PATH** usually lacks **Volta**, **nvm**, or **fnm** (common when Unity is opened from the Dock). The Editor auto-tries **`~/.volta/bin/node`**, Homebrew **`/opt/homebrew/bin/node`** / **`/usr/local/bin/node`**, and **`NODE_BINARY`** in the environment; override with **Node executable** in settings (use `which node` from Terminal), or launch Unity from a shell where **`node`** is on **PATH**.

**Apply schema:** `npm run db:migrate`.

**CLI** (body from a UTF-8 file under the repo; **`--issue`** optional):

```bash
npm run db:register-editor-export -- --kind agent_context \
  --document-file tools/reports/.staging/body-example.json \
  --issue BUG-37
```

`--kind`: `agent_context`, `sorting_debug`, `terrain_cell_chunk`, `world_snapshot_dev`. **Sorting debug** files are Markdown text; the script wraps them as `{"format":"markdown","body":"…"}` in **`document`**.

**Example `document` queries:**

```sql
SELECT id, exported_at_utc, document->'schema_version' AS sv
FROM editor_export_agent_context
ORDER BY exported_at_utc DESC
LIMIT 5;

SELECT id, document->'artifact' AS artifact
FROM editor_export_terrain_cell_chunk
WHERE backlog_issue_id = 'BUG-37'
ORDER BY exported_at_utc DESC
LIMIT 10;

SELECT id, left(document->>'body', 200)
FROM editor_export_sorting_debug
WHERE document->>'format' = 'markdown'
ORDER BY exported_at_utc DESC
LIMIT 5;
```

**Unlabeled rows:** `WHERE backlog_issue_id IS NULL`.

**Manual bundle** (**TECH-44c**) remains available via **`npm run db:register-repro`** — it inserts a single **`dev_repro_bundle`** row that can point at both **Agent context** and **Sorting debug** paths. **TECH-55** / **TECH-55b** add **per-export** history (**`document jsonb`**) in separate tables.

## CI note

Spinning **Postgres** in **CI** for this milestone remains **optional** (developer **Docker** / local **Homebrew** + documented **`npm run db:migrate`**).

## Shipped decisions (TECH-44b closure — durable record)

| Topic | Choice |
|-------|--------|
| **Migrations** | Versioned SQL under **`db/migrations/`**; `tools/postgres-ia/apply-migrations.mjs` runs each file with **`psql -f`** and records versions in **`schema_migrations`** via **`pg`** (avoids multi-statement splitting in **node-postgres**). |
| **Milestone 1 shape** | Normalized columns only — **no JSONB**; follow [`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md) when **JSONB** is added later. |
| **MCP** | **territory-ia** stays **file-backed** until **TECH-18**; see [`docs/mcp-ia-server.md`](mcp-ia-server.md). |

**Backlog:** **TECH-44b** completed 2026-04-03 — trace in [`BACKLOG.md`](../BACKLOG.md) **§ Completed (last 30 days)**.

## Related

- **Program / patterns:** [`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md) (including **Program extension mapping (E1–E3)**); [`BACKLOG.md`](../BACKLOG.md) **§ Completed** **TECH-44** (closed umbrella)
- **TECH-18 handoff:** [`docs/mcp-ia-server.md`](mcp-ia-server.md) — **PostgreSQL IA (TECH-44b) integration point for TECH-18**
- **Tooling README:** [`tools/postgres-ia/README.md`](../tools/postgres-ia/README.md)
