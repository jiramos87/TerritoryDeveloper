### Stage 4 — Envelope Foundation (Breaking Cut) / Rewrite 32 Tool Handlers


**Status:** Done

**Backlog state (Stage 2.2):** 8 filed — 8 Done (archived) TECH-398, TECH-399, TECH-400, TECH-401, TECH-402, TECH-403, TECH-404, TECH-405

**Objectives:** Wrap all 32 tool handlers in `wrapTool()`; convert all error paths to typed `ErrorCode` values; add `payload.meta` to `spec_section` response. Handlers split by family across 4 phases for reviewability.

**Exit:**

- All 22 handler files use `wrapTool`; no bare `return { content: [...] }` at top level.
- `spec_section` response includes `payload.meta: { section_id, line_range, truncated, total_chars }`.
- `unity_bridge_command` timeout path includes `error.details: { command_id, last_output_preview }`.
- `db_unconfigured` returns `{ ok: false, error: { code: "db_unconfigured", ... } }` across all DB tools.
- Existing passing tests still pass after snapshot regen.
- Phase 1 — Read + spec + rule tools.
- Phase 2 — Glossary + invariant tools.
- Phase 3 — Backlog + DB-coupled tools.
- Phase 4 — Bridge + Unity analysis tools.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | Wrap spec tools | **TECH-398** | Done (archived) | Wrap `list-specs.ts`, `spec-outline.ts`, `spec-section.ts`, `spec-sections.ts` in `wrapTool`; add `payload.meta: { section_id, line_range, truncated, total_chars }` to `spec-section` success response; convert `spec_not_found` / `section_not_found` to typed `ErrorCode`. |
| T4.2 | Wrap rule + router tools | **TECH-399** | Done (archived) | Wrap `list-rules.ts`, `rule-content.ts`, `router-for-task.ts` in `wrapTool`; add new `rule_section` tool (symmetric to `spec_section`, canonical params `{ rule, section, max_chars }`) in `rule-content.ts` alongside existing `rule_content`; register in MCP server index. |
| T4.3 | Wrap glossary tools | **TECH-400** | Done (archived) | Wrap `glossary-discover.ts`, `glossary-lookup.ts` (including bulk-`terms` path from Stage 1.1) in `wrapTool`; ensure `meta.graph_generated_at` + `meta.graph_stale` preview fields flow through envelope `meta` (full freshness logic in Stage 3.3). |
| T4.4 | Wrap invariant tools | **TECH-401** | Done (archived) | Wrap `invariants-summary.ts` (structured response from Stage 1.2) and `invariant-preflight.ts` in `wrapTool`; convert hardcoded section-cap constants to `INVARIANT_PREFLIGHT_MAX_SECTIONS` / `INVARIANT_PREFLIGHT_MAX_CHARS` env vars with existing defaults. |
| T4.5 | Wrap backlog tools | **TECH-402** | Done (archived) | Wrap `backlog-issue.ts`, `backlog-search.ts` in `wrapTool`; `issue_not_found` → `{ ok: false, error: { code: "issue_not_found", hint: "Check ia/backlog/ and ia/backlog-archive/" } }`. |
| T4.6 | Wrap DB-coupled tools | **TECH-403** | Done (archived) | Wrap `city-metrics-query.ts`, `project-spec-closeout-digest.ts`, `project-spec-journal.ts` (all 4 journal ops) in `wrapTool`; `db_unconfigured` branch → `{ ok: false, error: { code: "db_unconfigured", hint: "Start Postgres on :5434" } }` uniformly across all four. |
| T4.7 | Wrap bridge tools | **TECH-404** | Done (archived) | Wrap `unity-bridge-command.ts`, `unity-bridge-lease.ts`, `unity-bridge-get.ts` (via `unity_bridge_get` / `unity_compile` kinds), `unity-compile.ts` in `wrapTool`; timeout path: inject `error.details = { command_id, last_output_preview }` before wrapping; `db_unconfigured` → `{ ok: false, error: { code: "db_unconfigured" } }`. |
| T4.8 | Wrap Unity analysis tools | **TECH-405** | Done (archived) | Wrap `findobjectoftype-scan.ts`, `unity-callers-of.ts`, `unity-subscribers-of.ts`, `csharp-class-summary.ts` in `wrapTool`; no-results path returns `ok: true, payload: { matches: [] }` (not error); parse failure → `ok: false, error: { code: "invalid_input" }`. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
