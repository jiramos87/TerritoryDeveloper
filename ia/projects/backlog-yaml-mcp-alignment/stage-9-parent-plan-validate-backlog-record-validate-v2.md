### Stage 9 — yaml schema v2 + backfill + validator MVP (locator fields) / `parent_plan_validate` + `backlog_record_validate` v2

**Status:** Final

**Backlog state (Stage 3.3):** 5 filed (TECH-406, TECH-407, TECH-408, TECH-409, TECH-410 Done (archived)).

**Objectives:** Ship the `parent_plan_validate` MCP tool + matching `tools/validate-parent-plan-locator.mjs` CLI validator (dual-mode: advisory default + `--strict` flag). Extend the existing `backlog_record_validate` shared lint core (`backlog-record-schema.ts`) with schema-v2 awareness (new-field regex / type checks). Keep advisory-default through Step 6; strict-flip lives in Step 6 late-hardening.

**Exit:**

- `tools/validate-parent-plan-locator.mjs (new)` — scans `ia/backlog/*.yaml` + `ia/backlog-archive/*.yaml`; checks: `parent_plan` path resolves on disk; `task_key` matches `^T\d+\.\d+(\.\d+)?$`; `task_key` present as row in `parent_plan` (line match); plan row `Issue: **{id}**` back-references yaml id. Dual-mode per source doc Phase 6 Step 1.
- `tools/mcp-ia-server/src/tools/parent-plan-validate.ts (new)` — input `{ strict?: boolean = false }`, output `{ errors: string[], warnings: string[], exit_code: 0|1 }`. Delegates to shared validator core.
- `backlog-record-schema.ts` schema-v2 awareness — regex guard on `task_key`; type guards on arrays (`surfaces`, `mcp_slices`, `skill_hints`); `parent_plan` path-string format check (no existence check here — that lives in `parent_plan_validate`).
- Fixtures under `tools/scripts/test-fixtures/parent-plan-validate/` — plan-exists-pass, plan-missing-fail, task-key-bad-regex-fail, task-key-drift-warn, issue-back-ref-missing-warn.
- Advisory run emits drift count line when drift exists; silent when clean. `--strict` (CLI) / `strict: true` (MCP) escalates to errors + exit 1.
- Phase 1 — Shared validator core + CLI dual-mode.
- Phase 2 — MCP tool wrapper + schema-v2 lint extensions + fixtures.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | Author shared validator core | **TECH-406** | Done (archived) | Create `tools/mcp-ia-server/src/parser/parent-plan-validator.ts (new)` exporting `validateParentPlanLocator({ yamlDirs: string[], planGlob: string, strict: boolean }): { errors, warnings, exit_code }`. Implements the 4 checks (path resolve / regex / task_key-in-plan / back-ref). Pure function; no process exit. |
| T9.2 | CLI wrapper + dual-mode flag | **TECH-407** | Done (archived) | `tools/validate-parent-plan-locator.mjs (new)` — wraps core from T3.3.1; `--strict` / `--advisory` flag parsing; default advisory; prints drift count on advisory; full errors on strict. Exit 0 advisory (always) or 1 on strict + error. Add `npm run validate:parent-plan-locator` script to root `package.json`; chain into `validate:all` as advisory (non-blocking) for now. |
| T9.3 | MCP tool wrapper | **TECH-408** | Done (archived) | `tools/mcp-ia-server/src/tools/parent-plan-validate.ts (new)` — input schema `{ strict?: boolean }`, calls `validateParentPlanLocator` with repo-relative paths, returns `{ errors, warnings, exit_code }`. Register in `tools/mcp-ia-server/src/index.ts` tool registry. Tool descriptor notes schema-cache restart (N4). |
| T9.4 | Extend `backlog_record_schema.ts` | **TECH-409** | Done (archived) | In `tools/mcp-ia-server/src/parser/backlog-record-schema.ts`, add: `task_key` regex check when present; `surfaces` / `mcp_slices` / `skill_hints` must be `string[]` when present; `parent_plan` must be non-empty string when present (existence check deferred to `parent_plan_validate`). Shared by CLI + `backlog_record_validate` MCP tool. |
| T9.5 | Fixtures for validator | **TECH-410** | Done (archived) | Under `tools/scripts/test-fixtures/parent-plan-validate/`: `plan-exists-pass/`, `plan-missing-fail/`, `task-key-bad-regex-fail/`, `task-key-drift-warn/` (plan exists but no row matches), `issue-back-ref-missing-warn/` (plan has row but `Issue:` points elsewhere). Harness under `tools/mcp-ia-server/tests/tools/parent-plan-validate.test.ts` asserts advisory vs strict outputs per fixture. |

---
