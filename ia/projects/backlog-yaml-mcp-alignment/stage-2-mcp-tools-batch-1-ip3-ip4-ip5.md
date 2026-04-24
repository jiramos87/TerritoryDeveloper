### Stage 2 — HIGH band (IP1–IP5) / MCP tools batch 1 (IP3 + IP4 + IP5)

**Status:** Final

**Backlog state (Stage 1.2):** 7 filed

**Objectives:** Ship the three new MCP tools: `reserve_backlog_ids` wrapping `reserve-id.sh`, `backlog_list` for structured filter queries, `backlog_record_validate` for pre-write lint. Extract the shared lint core (`backlog-record-schema.ts`) so `validate-backlog-yaml.mjs` and `backlog_record_validate` share logic.

**Exit:**

- `tools/mcp-ia-server/src/tools/reserve-backlog-ids.ts` — spawns `reserve-id.sh {PREFIX} {N}`, returns `{ ids: string[] }`.
- `tools/mcp-ia-server/src/tools/backlog-list.ts` — filters by `section` / `priority` / `type` / `status` / `scope`, returns ordered `{ issues, total }`.
- `tools/mcp-ia-server/src/tools/backlog-record-validate.ts` — input `{ yaml_body: string }`, output `{ ok, errors, warnings }`.
- `tools/mcp-ia-server/src/parser/backlog-record-schema.ts` — shared schema / lint core consumed by validator script + MCP tool.
- Tools registered in `tools/mcp-ia-server/src/index.ts` tool registry.
- Tests under `tools/mcp-ia-server/tests/tools/` cover: reserve concurrency (N parallel calls → zero dup ids), list filter combinations + empty result + scope switch, validator good + bad records (required fields, id format, status enum, soft-dep consistency).
- Tool descriptors match `mcp__territory-ia__*` naming convention.
- Phase 1 — Shared lint core extraction + `backlog_record_validate`.
- Phase 2 — `reserve_backlog_ids` tool + concurrency test.
- Phase 3 — `backlog_list` tool + filter combinations.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | Extract shared lint core | **TECH-323** | Done (archived) | Create `tools/mcp-ia-server/src/parser/backlog-record-schema.ts` exporting `validateBacklogRecord(yamlBody: string): { ok, errors, warnings }`. Move schema checks (required fields, id format, status enum, `depends_on_raw` non-empty when `depends_on: []` non-empty) out of `tools/validate-backlog-yaml.mjs`. Update the validator script to import + call the shared core. |
| T2.2 | Implement `backlog_record_validate` MCP tool | **TECH-324** | Done (archived) | `tools/mcp-ia-server/src/tools/backlog-record-validate.ts` — input schema `{ yaml_body: string }`, output `{ ok, errors, warnings }`. Delegate to shared core from T1.2.1. Register in `tools/mcp-ia-server/src/index.ts`. |
| T2.3 | Test `backlog_record_validate` against fixtures | **TECH-325** | Done (archived) | Add tests under `tools/mcp-ia-server/tests/tools/backlog-record-validate.test.ts` — good record passes; each bad-record fixture (missing required field, bad id format, invalid status, empty `depends_on_raw` with non-empty `depends_on`) returns the expected error. |
| T2.4 | Implement `reserve_backlog_ids` MCP tool | **TECH-326** | Done (archived) | `tools/mcp-ia-server/src/tools/reserve-backlog-ids.ts` — input `{ prefix: "TECH"\ | "FEAT"\ | "BUG"\ | "ART"\ | "AUDIO", count: 1..50 }`, spawn `tools/scripts/reserve-id.sh {prefix} {count}` via `child_process`, parse stdout, return `{ ids: string[] }`. Register in `tools/mcp-ia-server/src/index.ts`. |
| T2.5 | Concurrency test for `reserve_backlog_ids` | **TECH-327** | Done (archived) | Add `tools/mcp-ia-server/tests/tools/reserve-backlog-ids.test.ts` — spawn 8 parallel invocations of the tool (counts 2 each), assert 16 unique ids returned + counter advanced correctly. Mirrors `tools/scripts/test/reserve-id-concurrent.sh` at the MCP layer. |
| T2.6 | Implement `backlog_list` MCP tool | **TECH-328** | Done (archived) | `tools/mcp-ia-server/src/tools/backlog-list.ts` — input `{ section?, priority?, type?, status?, scope? (default "open") }`, load via `parseAllBacklogIssues`, apply filters in-memory, return `{ issues, total }` ordered by id desc. Register in `tools/mcp-ia-server/src/index.ts`. |
| T2.7 | Test `backlog_list` filter combinations | **TECH-329** | Done (archived) | Add `tools/mcp-ia-server/tests/tools/backlog-list.test.ts` — fixture set covering ≥2 sections, ≥2 priorities, ≥2 types, open + archive. Assert: scope switch, single-filter cases, multi-filter intersection, empty result, id desc ordering. |
