# PostgreSQL — IA dev setup

**Scope:** Local or shared **dev** database for **Information Architecture** tables (`glossary`, `spec_sections`, `invariants`, `relationships`), the **IA project spec journal** (`ia_project_spec_journal` — **glossary** **IA project spec journal**), the **dev repro bundle registry** (`dev_repro_bundle`, **E1** in [`postgres-interchange-patterns.md`](postgres-interchange-patterns.md)), the **per-export Editor Reports registry** (`editor_export_*` tables), the **IDE agent bridge** queue (`agent_bridge_job` — **glossary** **IDE agent bridge**), and optional **gameplay verification** time-series (`city_metrics_history` — per-tick snapshots from Unity **`MetricsRecorder`**; **not** authoritative **Save data**). This is **not** player **Save data** or **Load pipeline** input — see [`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md). **Charter trace:** [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md).

## Prerequisites

- **PostgreSQL** 14+ (server)
- **`psql`** client on `PATH` (for `apply-migrations.mjs`)
- **Node.js** 18+ (for migration runner and read/seed scripts)

## Environment

**Connection (Node / **territory-ia**):** when **`CI`** / **`GITHUB_ACTIONS`** are unset, tooling loads repo-root **`.env`** then **`.env.local`** (keys only fill empty `process.env` entries), then uses **`DATABASE_URL`** if set, then [`config/postgres-dev.json`](../config/postgres-dev.json), then the same dev URI as that file if the JSON is missing. In **CI**, only **`DATABASE_URL`** applies (no dotenv file load). **`REPO_ROOT`** is the repository root (where **`config/`** and **`ProjectSettings/`** live); scripts resolve it automatically unless you override with **`REPO_ROOT`** in **`.env`**.

**Default host port (5434):** [`config/postgres-dev.json`](../config/postgres-dev.json) uses **host port `5434`** so this database can run **alongside** another PostgreSQL on **`5432`** (common when other tools use Docker or a second stack). **Your server must listen on the same port as the URI** — if nothing is bound to **5434**, clients return **connection refused**. This project **does not require Docker**; use Postgres.app, Homebrew, or any local install (see below). Docker is **optional** for contributors who prefer a container.

Put your full URI (including password when **SCRAM** requires it) in **`postgres-dev.json`** for shared dev defaults, or set **`DATABASE_URL`** in a gitignored repo-root **`.env`** so credentials never land in version control.

Example env override:

```bash
export DATABASE_URL='postgresql://postgres:postgres@localhost:5434/territory_ia_dev'
```

Optional: keep a repo-root **`.env`** (gitignored) with local overrides; Node tools and **`npm run unity:compile-check`** load it automatically (see **`tools/scripts/load-repo-env.inc.sh`** and **`tools/mcp-ia-server/src/ia-db/repo-dotenv.ts`**). **`.env.example`** is only a pointer to create **`.env`** — variable documentation lives in your **`.env`**.

## Local PostgreSQL (Postgres.app, Homebrew — no Docker)

1. **Create** database **`territory_ia_dev`** (and a login role if needed). The committed [`config/postgres-dev.json`](../config/postgres-dev.json) expects user **`postgres`** with password **`postgres`** — change either the database role or the JSON so they match.
2. **Port:** If your local server still uses **`5432`** but another process already owns **`5432`**, configure **this** server to use **`5434`**:
   - **Postgres.app:** open the app → select your server → **Server Settings…** (or open the data directory from the menu) → edit **`postgresql.conf`** → set **`port = 5434`** → restart the server. Ensure **`listen_addresses`** allows TCP to **`localhost`** (default is often enough).
   - **Homebrew:** edit **`postgresql.conf`** in your data directory (e.g. under **`opt/postgresql@…/`** or **`/usr/local/var/postgres`**), set **`port = 5434`**, then **`brew services restart postgresql@…`** (match your version).
3. **Verify** before DBeaver or MCP:

```bash
export DATABASE_URL="$(node -p "JSON.parse(require('fs').readFileSync('config/postgres-dev.json','utf8')).database_url")"
psql "$DATABASE_URL" -c 'SELECT 1 AS ok;'
```

On macOS you can confirm something is listening: **`lsof -nP -iTCP:5434 -sTCP:LISTEN`**.

**Bash helper (Postgres.app port + database + migrate):** from the repository root, **`npm run db:setup-local -- help`** — commands **`configure-port`**, **`server-start`**, **`server-restart`** (`pg_ctl` on the latest **`var-*`** data dir, or **`POSTGRES_APP_PGDATA`**), **`init-db`**, **`migrate`**, **`all`** (see **`tools/scripts/setup-territory-ia-postgres.sh`**). Reads host, port, user, password, and database name from [`config/postgres-dev.json`](../config/postgres-dev.json). **`init-db`** requires a superuser-style login (the default **`postgres`** role is fine).

If you truly have **no** conflict on **`5432`**, you may point **`postgres-dev.json`** at **`localhost:5432`** instead — keep the file in sync with the server.

## Optional: Quick start (Docker)

For contributors who want a throwaway server on **host port 5434** (maps container **5432**). Remove or rename an existing container named **`territory-ia-pg`** if it already exists.

```bash
docker run --name territory-ia-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=territory_ia_dev -p 5434:5432 -d postgres:16
export DATABASE_URL='postgresql://postgres:postgres@localhost:5434/territory_ia_dev'
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
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f db/migrations/0006_editor_export_ui_inventory.sql
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f db/migrations/0007_ia_project_spec_journal.sql
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f db/migrations/0008_agent_bridge_job.sql
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f db/migrations/0009_city_metrics_history.sql
```

If you use manual `psql` on a fresh DB, insert migration rows so the Node runner does not re-apply:

```sql
INSERT INTO schema_migrations (version) VALUES ('0001_ia_tables'), ('0002_ia_read_surface'), ('0003_dev_repro_bundle'), ('0004_editor_export_tables'), ('0005_editor_export_document'), ('0006_editor_export_ui_inventory'), ('0007_ia_project_spec_journal'), ('0008_agent_bridge_job'), ('0009_city_metrics_history');
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

## Dev repro bundle registry (**E1**)

Registers **metadata** for **Editor** exports under **`tools/reports/`** (paths are **gitignored** — see [`.cursor/specs/unity-development-context.md`](../.cursor/specs/unity-development-context.md) **§10**): **Agent context** JSON (`agent-context-*.json`), optional **Sorting debug** Markdown (`sorting-debug-*.md`). Rows use **B1** shape: scalars + **`payload jsonb`** with **Interchange JSON**–style keys **`artifact`**: `dev_repro_bundle`, **`schema_version`**: `1` (see [`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md)).

**Table:** `dev_repro_bundle` — columns `backlog_issue_id`, `git_sha`, `exported_at_utc` (defaults to **INSERT** time), `interchange_revision` (mirrors **`schema_version`** for this artifact), `payload`.

**`backlog_issue_id`:** Store the canonical form matching **territory-ia** `normalizeIssueId` (case-insensitive input normalized to **`BUG-`/`FEAT-`/`TECH-`-** style). The **`register-dev-repro.mjs`** script applies the same rules.

**Example INSERT** (adjust paths to your export files):

```sql
INSERT INTO dev_repro_bundle (backlog_issue_id, git_sha, interchange_revision, payload)
VALUES (
  'TECH-00',
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
SELECT * FROM dev_repro_list_by_issue('TECH-00', 10);
```

**CLI registration** (uses `git rev-parse HEAD` for SHA unless `--sha` is set):

```bash
npm run db:register-repro -- --issue TECH-00 \
  --agent-context tools/reports/agent-context-2026-04-04T12-00-00Z.json
```

## Editor export registry (per-export **Postgres** history)

**Postgres-only:** when **`DATABASE_URL`** resolves (process environment, **EditorPrefs** `TerritoryDeveloper.EditorExportRegistry.DatabaseUrl`, or repo-root **`.env.local`** `DATABASE_URL=…`), **Unity** runs **`register-editor-export.mjs`**: the full export body is stored in column **`document jsonb`** (plus **`payload jsonb`** metadata). **GIN** indexes support **`jsonb_path_ops`** queries on **`document`**. There is **no** workspace fallback under **`tools/reports/`** for these menus — see [`.cursor/specs/unity-development-context.md`](../.cursor/specs/unity-development-context.md) **§10**. **Staging** for **`--document-file`** uses a temp directory or an absolute path (see **`register-editor-export.mjs`**).

Migrations: **`0004_editor_export_tables.sql`**, **`0005_editor_export_document.sql`** (`backlog_issue_id` nullable; **`document`** required on new inserts), **`0006_editor_export_ui_inventory.sql`**, **`0008_agent_bridge_job.sql`** (**IDE agent bridge** queue — **`agent_bridge_job`**).

| Table | Menu item |
|-------|-----------|
| `editor_export_agent_context` | **Export Agent Context** |
| `editor_export_sorting_debug` | **Export Sorting Debug (Markdown)** |
| `editor_export_terrain_cell_chunk` | **Export Cell Chunk (Interchange)** (Play Mode) |
| `editor_export_world_snapshot_dev` | **Export World Snapshot (Dev Interchange)** (Play Mode) |
| `editor_export_ui_inventory` | **Export UI Inventory (JSON)** (Edit Mode; **`ui-design-system.md`** / **UI** inventory baseline) |

**Settings:** **Territory Developer → Reports → Postgres registry — settings…** — optional **`backlog_issue_id`** (metadata for SQL filters; may be empty), optional **`DATABASE_URL`** (local only), optional **Node executable** path, optional verbose logging after successful inserts.

**Node / Unity on macOS:** If the Console shows `Registry invocation failed` with **Cannot find the specified file** and **`ApplicationName='node'`**, Unity’s process **PATH** usually lacks **Volta**, **nvm**, or **fnm** (common when Unity is opened from the Dock). The Editor auto-tries **`~/.volta/bin/node`**, Homebrew **`/opt/homebrew/bin/node`** / **`/usr/local/bin/node`**, and **`NODE_BINARY`** in the environment; override with **Node executable** in settings (use `which node` from Terminal), or launch Unity from a shell where **`node`** is on **PATH**.

**Apply schema:** `npm run db:migrate`.

**CLI** (body from a UTF-8 file — repo-relative or absolute; **`--issue`** optional):

```bash
npm run db:register-editor-export -- --kind agent_context \
  --document-file path/to/body-example.json \
  --issue TECH-00
```

`--kind`: `agent_context`, `sorting_debug`, `terrain_cell_chunk`, `world_snapshot_dev`, `ui_inventory`. **Sorting debug** files are Markdown text; the script wraps them as `{"format":"markdown","body":"…"}` in **`document`**.

**Example `document` queries:**

```sql
SELECT id, exported_at_utc, document->'schema_version' AS sv
FROM editor_export_agent_context
ORDER BY exported_at_utc DESC
LIMIT 5;

SELECT id, document->'artifact' AS artifact
FROM editor_export_terrain_cell_chunk
WHERE backlog_issue_id = 'TECH-00'
ORDER BY exported_at_utc DESC
LIMIT 10;

SELECT id, left(document->>'body', 200)
FROM editor_export_sorting_debug
WHERE document->>'format' = 'markdown'
ORDER BY exported_at_utc DESC
LIMIT 5;
```

**Unlabeled rows:** `WHERE backlog_issue_id IS NULL`.

**Manual bundle** (**E1**) remains available via **`npm run db:register-repro`** — it inserts a single **`dev_repro_bundle`** row that can point at both **Agent context** and **Sorting debug** paths. The **`editor_export_*`** tables add **per-export** history (**`document jsonb`**) in separate tables.

## Bridge environment preflight

Before relying on **`unity_bridge_command`** or **`unity_compile`**, run the preflight from the repository root:

```bash
npm run db:bridge-preflight
```

The script reuses the same **`resolveIaDatabaseUrl`** logic as **territory-ia** MCP: **`DATABASE_URL`** env wins, else **`config/postgres-dev.json`** when not in CI.

**Exit codes (stable — agents branch on these):**

| Code | Meaning | Suggested repair |
|------|---------|------------------|
| `0` | OK — Postgres reachable, **`agent_bridge_job`** present | — |
| `1` | No URL — neither **`DATABASE_URL`** nor **`config/postgres-dev.json`** `database_url` resolved | Set **`DATABASE_URL`** or add `database_url` to **`config/postgres-dev.json`** |
| `2` | Connection refused / timeout | Start Postgres (`npm run db:setup-local` or `pg_ctl start`) |
| `3` | Table missing — **`agent_bridge_job`** not found | `npm run db:migrate` (migration **0008**) |
| `4` | Unexpected SQL error | Check stderr; manual fix |

**Unity vs MCP URL alignment:** **Unity Editor** may resolve its database URL via **EditorPrefs** (`TerritoryDeveloper.EditorExportRegistry.DatabaseUrl`) or repo-root **`.env.local`**. The preflight script and **territory-ia** MCP do **not** read those Unity-only sources. If **`unity_bridge_command`** returns **`timeout`** while preflight shows exit `0`, the most likely cause is **Unity** targeting a different database — align the **Editor** value with the URI in **`config/postgres-dev.json`** or **`DATABASE_URL`** (see **Agent bridge job queue** troubleshooting below).

**Cursor Skill:** [`.cursor/skills/bridge-environment-preflight/SKILL.md`](../.cursor/skills/bridge-environment-preflight/SKILL.md) — bounded repair policy for agents.

## Agent bridge job queue

**Table:** **`agent_bridge_job`** — **`command_id`** (**uuid**), **`kind`**, **`status`** (`pending` → `processing` → `completed` / `failed`), **`request`**, **`response`**, **`error`**. **territory-ia** MCP **`unity_bridge_command`** inserts **`pending`** rows and polls; **Unity** **`AgentBridgeCommandRunner`** runs **`tools/postgres-ia/agent-bridge-dequeue.mjs`** (claim + **`processing`**) and **`agent-bridge-complete.mjs`** (write **`response`** / **`failed`**). Requires the same **`DATABASE_URL`** as **`register-editor-export.mjs`**. See [`docs/mcp-ia-server.md`](mcp-ia-server.md) and **unity-development-context** §10.

**Troubleshooting — MCP `timeout` while jobs stay `pending`:** If **`unity_bridge_command`** (or **`unity_compile`**) returns **`timeout`** but **Postgres** accepts connections, **Unity** is likely dequeuing against a **different** connection string than the **MCP** host. Align **`DATABASE_URL`** (or **EditorPrefs** / **`.env.local`**) in the **Unity Editor** with the value **Cursor** / **`territory-ia`** uses (see **Connection** at the top of this doc). **MCP** and **Unity** must target the **same** database and schema (**migration `0008`** applied).

## IA project spec journal

**Table:** **`ia_project_spec_journal`** — append-only rows for **Decision Log** and **Lessons learned** Markdown bodies copied from `.cursor/projects/{ISSUE_ID}.md` at **project-spec-close**. Columns include **`backlog_issue_id`**, **`entry_kind`** (`decision_log` \| `lessons_learned`), **`body_markdown`**, **`keywords`** (`text[]` for overlap search), **`source_spec_path`**, **`recorded_at`**, optional **`git_sha`**, and generated **`body_tsv`** (**GIN** full-text).

**Write:** **territory-ia** MCP **`project_spec_journal_persist`** or from repo root **`npm run db:persist-project-journal -- --issue TECH-58`** (optional **`--git-sha`**). Requires migration **`0007`** applied (`npm run db:migrate`).

**Read / search:** MCP **`project_spec_journal_search`**, **`project_spec_journal_get`**, **`project_spec_journal_update`**. Example **SQL**:

```sql
SELECT id, backlog_issue_id, entry_kind, left(body_markdown, 200), recorded_at
FROM ia_project_spec_journal
ORDER BY recorded_at DESC
LIMIT 10;
```

## CI note

Spinning **Postgres** in **CI** for this milestone remains **optional** (developer **Docker** / local **Homebrew** + documented **`npm run db:migrate`**).

## Shipped decisions (first Postgres **IA** milestone — durable record)

| Topic | Choice |
|-------|--------|
| **Migrations** | Versioned SQL under **`db/migrations/`**; `tools/postgres-ia/apply-migrations.mjs` runs each file with **`psql -f`** and records versions in **`schema_migrations`** via **`pg`** (avoids multi-statement splitting in **node-postgres**). |
| **Milestone 1 shape** | Normalized columns only — **no JSONB**; follow [`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md) when **JSONB** is added later. |
| **MCP** | **Normative** spec tools stay **file-backed**; optional **`project_spec_journal_*`** tools use **`pg`** when **`DATABASE_URL`** is set — see [`docs/mcp-ia-server.md`](mcp-ia-server.md). |

**Archive:** First **Postgres** **IA** milestone completed 2026-04-03 — trace in [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md) (**2026-04-04** batch).

## Related

- **Program / patterns:** [`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md) (including **Program extension mapping (E1–E3)**); [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md) (closed **Postgres** charter)
- **Future DB-backed MCP:** [`docs/mcp-ia-server.md`](mcp-ia-server.md) — **PostgreSQL IA (dev schema) and future DB-backed retrieval**
- **Tooling README:** [`tools/postgres-ia/README.md`](../tools/postgres-ia/README.md)
