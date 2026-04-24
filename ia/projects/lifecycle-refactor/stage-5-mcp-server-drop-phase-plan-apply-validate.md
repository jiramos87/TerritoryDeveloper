### Stage 5 — Infrastructure + Execution Surface / MCP Server: Drop Phase + plan_apply_validate

**Status:** Final

**Objectives:** Remove Phase-aware parameters from MCP tool handlers. Add `plan_apply_validate` tool (validates §Plan anchor presence before applier runs). Update `router_for_task` lifecycle-stage enum to include pair-head + pair-tail stage names. Restart schema cache.

**Exit:**

- `router_for_task` `lifecycle_stage` enum: Phase-related values removed; new values added: `plan_review`, `plan_fix_apply`, `stage_file_plan`, `stage_file_apply`, `project_new_plan`, `project_new_apply`, `spec_enrich`, `opus_audit`, `opus_code_review`, `code_fix_apply`, `closeout_apply`.
- Phase params absent from `router_for_task`, `spec_section`, `backlog_issue`, `project_spec_closeout_digest`.
- `project_spec_closeout_digest` reads 4 new spec sections: `§Audit`, `§Code Review`, `§Code Fix Plan`, `§Closeout Plan`.
- `plan_apply_validate(section_header, target_path)` tool registered; handler validates that `§{section_header}` heading exists in `{target_path}` + tuple list is non-empty.
- MCP schema cache restarted (kill + respawn `territory-ia` process).
- MCP smoke tests pass (`npm run validate:mcp` if exists; else handler unit tests).
- Migration JSON M4 flipped to `done`.
- Phase 1 — Drop Phase params + enum update + closeout-digest new sections.
- Phase 2 — New plan_apply_validate tool + cache restart + validate.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | Drop Phase params + update enum | **TECH-458** | Done (archived) | In `tools/mcp-ia-server/src/`: find all tool handler files referencing `phase` or `parent_phase` as input params (grep `phase` across `tools/` + `schemas/` dirs); remove Phase-aware params from `router_for_task`, `spec_section`, `backlog_issue`; update `router_for_task` `lifecycle_stage` enum (add 11 pair/enrich values; remove Phase-era values); update `project_spec_closeout_digest` handler to read 4 new spec sections (`§Audit`, `§Code Review`, `§Code Fix Plan`, `§Closeout Plan`) in addition to existing reads. |
| T5.2 | Schema cache restart + smoke | **TECH-459** | Done (archived) | Restart `territory-ia` MCP process (kill PID or use project npm script); verify Claude Code reconnects to updated schema; run `npm run validate:mcp` (or targeted handler unit tests in `tools/mcp-ia-server/`); confirm `router_for_task` responds with new enum values when queried with `plan_review` stage name. |
| T5.3 | Author plan_apply_validate tool | **TECH-460** | Done (archived) | Add `plan_apply_validate` tool to MCP server: handler signature `(section_header: string, target_path: string) → {ok: boolean, found: boolean, tuple_count: number, error?: string}`; implementation reads `target_path`, searches for `## {section_header}` heading, counts `{operation, target_path, target_anchor, payload}` tuple lines below it; returns `found: false` if heading absent; registers tool in MCP index alongside `plan_apply_pair-contract.md` reference. |
| T5.4 | Register + validate + M4 flip | **TECH-461** | Done (archived) | Register `plan_apply_validate` in MCP tool index (`tools/mcp-ia-server/src/index.ts` or equivalent entry point); restart schema cache again; run smoke test calling `plan_apply_validate` with a valid spec path + known section header; flip migration JSON M4 `done`. |

---
