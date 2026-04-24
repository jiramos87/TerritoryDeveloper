### Stage 6 — MEDIUM / LOW band (IP6–IP9) / MCP extensions (IP6 + IP9)

**Status:** Draft — tasks `_pending_`.

**Objectives:** Ship `backlog_record_create` MCP tool (atomic reserve → validate → write → materialize) + extend `backlog_search` with `priority` / `type` / `created_after` / `created_before` filters. Depends on Stage 1.1 (field extension), Stage 1.2 (reserve + validate tools), Stage 2.1 (flock-guarded materialize).

**Exit:**

- `tools/mcp-ia-server/src/tools/backlog-record-create.ts` — input `{ prefix, fields: Omit<ParsedBacklogIssue,"id"> }`, output `{ id, yaml_path }`. Flow: call `reserve_backlog_ids(count: 1)` → build yaml body → call `validateBacklogRecord` → tmp-file-then-rename write to `ia/backlog/{id}.yaml` → spawn `materialize-backlog.sh` (flock-guarded).
- `backlog-search.ts` accepts `priority?: string`, `type?: "BUG"|"FEAT"|"TECH"|"ART"|"AUDIO"`, `created_after?: string`, `created_before?: string`. Filters applied before scoring.
- Tests under `tools/mcp-ia-server/tests/tools/` — `backlog-record-create` happy path + validation-failure path + race (two parallel creates, distinct ids, both yaml files on disk, materialize ran). `backlog-search` filter combinations.
- Phase 1 — `backlog_record_create` implementation + atomicity test.
- Phase 2 — `backlog_search` filter extensions + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | Implement `backlog_record_create` tool | _pending_ | _pending_ | `tools/mcp-ia-server/src/tools/backlog-record-create.ts` — input `{ prefix, fields }`, flow per Stage 2.3 Exit. Use `reserve_backlog_ids` (IP3) + shared lint core (Stage 1.2) + flock-guarded materialize (Stage 2.1). Tmp-file-then-rename for the yaml write. Register in `tools/mcp-ia-server/src/index.ts`. |
| T6.2 | Happy / failure path tests | _pending_ | _pending_ | `tools/mcp-ia-server/tests/tools/backlog-record-create.test.ts` — happy path (record created, yaml on disk, BACKLOG.md regenerated); validation-failure path (bad field → no yaml on disk, no id consumed, counter unchanged); concurrent-create path (two parallel calls → two distinct ids, both yaml files, BACKLOG.md has both entries). |
| T6.3 | Extend `backlog_search` filter inputs | _pending_ | _pending_ | In `tools/mcp-ia-server/src/tools/backlog-search.ts`, add optional input fields `priority`, `type`, `created_after`, `created_before` (ISO date strings). Apply filters before scoring. Update tool descriptor + any exported schema. |
| T6.4 | Test `backlog_search` filter extensions | _pending_ | _pending_ | Extend `tools/mcp-ia-server/tests/tools/backlog-search.test.ts` with fixture set covering each filter dimension + combined filters + date-range edge cases. Assert ordering preserved after filter. |

---
