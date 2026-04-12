# postgres-ia-tools (TECH-44b / TECH-44c)

Small **Node** helpers for the game-owned **PostgreSQL** **IA** schema, **E1** **dev repro bundle** registry (**TECH-44c**), **TECH-55** **Editor** export registry, and optional **city metrics** inserts from Unity: apply ordered SQL under [`db/migrations/`](../../db/migrations/), seed sample **glossary** rows, **glossary-by-key** smoke read, **register-dev-repro**, **register-editor-export**, and **insert-city-metrics** (payload JSON file → **`city_metrics_history`**).

**Connection URI resolution:** **`DATABASE_URL`** env, else committed [`config/postgres-dev.json`](../../config/postgres-dev.json) (skipped when **`CI=true`** / **`GITHUB_ACTIONS`**). Implemented in **`resolve-database-url.mjs`**.

**Canonical setup** (Postgres.app / Homebrew, Docker optional, overrides): [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md). From repo root: **`npm run db:setup-local -- help`** (`tools/scripts/setup-territory-ia-postgres.sh`).

## Scripts

| npm script | Purpose |
|------------|---------|
| `migrate` | Run `apply-migrations.mjs` — requires **`psql`** on `PATH` |
| `glossary-by-key` | `node glossary-by-key.mjs <term_key>` — uses `ia_glossary_row_by_key` |
| `seed:glossary` | Parse first N rows from `ia/specs/glossary.md` and upsert |
| `register-repro` | `node register-dev-repro.mjs --issue …` — inserts **`dev_repro_bundle`** row |
| `register-editor-export` | `node register-editor-export.mjs --kind … --document-file …` — **TECH-55b** **`document jsonb`** insert |
| `insert-city-metrics` | `node insert-city-metrics.mjs --payload-file …` — one row in **`city_metrics_history`** (spawned by Unity **`MetricsRecorder`**) |

From repo root:

```bash
npm --prefix tools/postgres-ia install
export DATABASE_URL='postgresql://...'
npm --prefix tools/postgres-ia run migrate
```

Root shortcuts (optional): `npm run db:migrate`, `npm run db:seed:glossary`, `npm run db:glossary -- <term_key>`, `npm run db:register-repro -- --issue TECH-44c …`, `npm run db:register-editor-export -- --kind agent_context --document-file path/to/body.json`, `npm run db:persist-project-journal -- --issue FEAT-44` (after migration **`0007_ia_project_spec_journal`** — see [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) §IA project spec journal).
