# JSON Schemas (interchange)

Machine-readable contracts for **interchange** JSON (tools, **MCP**, fixtures)—**not** player **Save data** (`GameSaveData` / **CellData**), which stays on the **persistence-system** path.

## Conventions

- **JSON Schema** uses **Draft 2020-12** (`$schema`: `https://json-schema.org/draft/2020-12/schema`).
- **Schema files**: prefer version in the basename, e.g. `geography-init-params.v1.schema.json`, plus a stable **`$id`** URL or `territory-developer:` URI for tooling.
- **Payloads** must include string **`artifact`** (logical model id). Optional integer **`schema_version`** when a consumer must branch without loading a schema file (see **glossary** **Interchange JSON (artifact)**; **IA index manifest** / fixtures **CI**: [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) + root **`package.json`** **`validate:fixtures`**).

## Layout

| Path | Role |
|------|------|
| `*.v*.schema.json` | Schema definitions |
| `fixtures/` | **Good** / **bad** JSON for **CI** (`npm run validate:fixtures` from repo root) |

## Pilot

- **`geography-init-params.v1.schema.json`** — **`artifact`**: `geography_init_params`; aligns with **glossary** **Geography initialization** / **Computational MCP tools** naming goals. Zod mirror: `tools/mcp-ia-server/src/schemas/geography-init-params-zod.ts` (CI via `validate:fixtures` + unit tests).

## Interchange schemas (tooling exports)

| Schema | `artifact` | Role |
|--------|------------|------|
| `cell-chunk-interchange.v1.schema.json` | `terrain_cell_chunk` | Axis-aligned **chunk** of **Cell** subset + height (Editor export, **Play Mode**) |
| `world-snapshot-dev.v1.schema.json` | `world_snapshot_dev` | **Water map** histogram + optional **HeightMap** raster (diagnostics only) |
| `scenario-descriptor.v1.schema.json` | `scenario_descriptor_v1` | **Agent scenario generator** descriptor (camelCase keys for Unity **JsonUtility**). Node terrain-only emit; Unity batch for **road stroke** via **road preparation** + **Apply**. **glossary** **scenario_descriptor_v1**; [`tools/fixtures/scenarios/BUILDER.md`](../../tools/fixtures/scenarios/BUILDER.md). |
