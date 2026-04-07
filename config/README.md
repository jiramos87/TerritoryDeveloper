# Repository config (versioned)

## `postgres-dev.json`

Default **local** PostgreSQL URI for **Information Architecture** dev tools (`npm run db:migrate`, **territory-ia** `project_spec_journal_*` when the MCP process has no `DATABASE_URL`, scripts under `tools/postgres-ia/`).

**Port:** the committed URI uses **host port `5434`** so Territory IA can coexist with another Postgres on **`5432`**. Your local server (Postgres.app, Homebrew, etc.) must **listen on that port**, or change this file to match your real port. Docker is **not** required — see **Local PostgreSQL** in [`docs/postgres-ia-dev-setup.md`](../docs/postgres-ia-dev-setup.md).

**Precedence:** environment variable **`DATABASE_URL`** overrides this file when set.

**CI:** when **`CI=true`** or **`GITHUB_ACTIONS`** is set, Node tools and **territory-ia** **do not** read this file (no accidental `localhost` connections in GitHub Actions). Set **`DATABASE_URL`** explicitly in a job only when you run integration tests against a service container.

**Scope:** local dev URI for IA tooling. **`DATABASE_URL`** overrides this file. Prefer env-only credentials in teams that avoid committing passwords; CI never reads this file.

**Bridge preflight:** `npm run db:bridge-preflight` uses **`resolveIaDatabaseUrl`** (same **`DATABASE_URL`** → `postgres-dev.json` fallback). Exit codes 0–4 — see [`docs/postgres-ia-dev-setup.md`](../docs/postgres-ia-dev-setup.md) (**Bridge environment preflight**).

See [`docs/postgres-ia-dev-setup.md`](../docs/postgres-ia-dev-setup.md).

### Shell and `psql`

From the repository root, load the URI into the environment (same string **territory-ia** / Node tools use when `DATABASE_URL` is unset):

```bash
export DATABASE_URL="$(node -p "JSON.parse(require('fs').readFileSync('config/postgres-dev.json','utf8')).database_url")"
psql "$DATABASE_URL" -c "SELECT id, git_sha, exported_at_utc FROM editor_export_ui_inventory ORDER BY id DESC LIMIT 5;"
```

To mirror that in a **gitignored** env file (e.g. for tools that only read `.env`), append:

```bash
echo "DATABASE_URL=$(node -p "JSON.parse(require('fs').readFileSync('config/postgres-dev.json','utf8')).database_url")" >> .env.local
```

**UI inventory exports** store the tree under `document → scenes → canvases → nodes` (not a top-level `document.nodes`). Example: total sampled nodes for the latest row:

```bash
psql "$DATABASE_URL" -c "
WITH expanded AS (
  SELECT e.id, jsonb_array_elements(s.scene->'canvases') AS canvas
  FROM editor_export_ui_inventory e,
       LATERAL jsonb_array_elements(e.document->'scenes') AS s(scene)
  WHERE e.id = (SELECT MAX(id) FROM editor_export_ui_inventory)
)
SELECT id, SUM(jsonb_array_length(canvas->'nodes'))::int AS total_sampled_nodes
FROM expanded GROUP BY id;
"
```
