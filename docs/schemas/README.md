# JSON Schemas (interchange)

Machine-readable contracts for **interchange** JSON (tools, **MCP**, fixtures)—**not** player **Save data** (`GameSaveData` / **CellData**), which stays on the **persistence-system** path.

## Conventions

- **JSON Schema** uses **Draft 2020-12** (`$schema`: `https://json-schema.org/draft/2020-12/schema`).
- **Schema files**: prefer version in the basename, e.g. `geography-init-params.v1.schema.json`, plus a stable **`$id`** URL or `territory-developer:` URI for tooling.
- **Payloads** must include string **`artifact`** (logical model id). Optional integer **`schema_version`** when a consumer must branch without loading a schema file (see [`projects/TECH-21-json-use-cases-brainstorm.md`](../../projects/TECH-21-json-use-cases-brainstorm.md) **FAQ** and [`.cursor/projects/TECH-40.md`](../../.cursor/projects/TECH-40.md)).

## Layout

| Path | Role |
|------|------|
| `*.v*.schema.json` | Schema definitions |
| `fixtures/` | **Good** / **bad** JSON for **CI** (`npm run validate:fixtures` from repo root) |

## Pilot

- **`geography-init-params.v1.schema.json`** — **`artifact`**: `geography_init_params`; aligns with **TECH-21** brainstorm **G4** and **TECH-41** / **TECH-39** naming goals (field names shared with **compute-lib** **Zod** when that schema lands).
