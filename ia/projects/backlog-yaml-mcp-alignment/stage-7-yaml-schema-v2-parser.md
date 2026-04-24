### Stage 7 — yaml schema v2 + backfill + validator MVP (locator fields) / yaml schema v2 + parser

**Status:** Final
**Backlog state (2026-04-18):** 4 tasks filed (TECH-363, TECH-364, TECH-365, TECH-366 all archived)

**Objectives:** Extend `ParsedBacklogIssue` + yaml loader to accept the 2 required + 7 optional locator fields. Regex-allowlist `task_key` per `^T\d+\.\d+(\.\d+)?$` (N1). Additive only — existing v1 records round-trip without the new fields.

**Exit:**

- `ParsedBacklogIssue` carries 9 new members (2 required on v2 writes; 7 optional throughout).
- Loader `yamlToIssue` populates new fields from yaml; absent = defaults (`null` / `[]`).
- `buildYaml` + writer path emit new fields when present; omit when absent (keep v1 records byte-identical on round-trip).
- Fixture set extends `tools/scripts/test-fixtures/` with full-v2 + missing-optional + missing-required examples.
- Phase 1 — Type + loader read-path extension.
- Phase 2 — Writer path + round-trip fixtures.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | Extend `ParsedBacklogIssue` v2 shape | **TECH-363** | Done (archived) | Add to `tools/mcp-ia-server/src/parser/types.ts` (or wherever `ParsedBacklogIssue` lives post-Step-1): `parent_plan: string \ | null`, `task_key: string \ | null`, `step: number \ | null`, `stage: string \ | null`, `phase: number \ | null`, `router_domain: string \ | null`, `surfaces: string[]`, `mcp_slices: string[]`, `skill_hints: string[]`. Null allowed on all to keep markdown-fallback path compilable. |
| T7.2 | Map new fields in yaml read path | **TECH-364** | Done (archived) | In `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts`, `yamlToIssue` sets all 9 fields from yaml record. Arrays default `[]`, scalars default `null`. Add regex guard on read for `task_key`: reject on parse if present + not matching `^T\d+\.\d+(\.\d+)?$`. |
| T7.3 | Emit new fields in writer path | **TECH-365** | Done (archived) | In `backlog-yaml-loader.ts` `buildYaml` (or the equivalent writer) + `tools/scripts/migrate-backlog-to-yaml.mjs`: emit `parent_plan`, `task_key`, optional scalars + arrays when present. Omit absent fields (no empty arrays or `null:` keys written). Preserve existing section order + block-literal style. |
| T7.4 | Round-trip fixtures for schema v2 | **TECH-366** | Done (archived) | Add `tools/scripts/test-fixtures/schema-v2-full.yaml` (all 9 fields) + `schema-v2-minimal.yaml` (only 2 required) + `schema-v1-legacy.yaml` (zero locator fields, proves back-compat). Load + round-trip test asserts byte-identical output per fixture. Hook into MCP tests folder too. |
