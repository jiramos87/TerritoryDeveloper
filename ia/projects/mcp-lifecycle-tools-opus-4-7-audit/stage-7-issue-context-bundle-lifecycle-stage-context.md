### Stage 7 — Composite Bundles + Graph Freshness / `issue_context_bundle` + `lifecycle_stage_context`


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement the two most-used composite tools. `issue_context_bundle` fans out to 5 sub-fetches and aggregates under one envelope call. `lifecycle_stage_context` wraps `issue_context_bundle` with a stage-specific extras dispatch map.

**Exit:**

- `issue_context_bundle({ issue_id: "TECH-301" })` returns full bundle per §7.2 example.
- `depends_on` references archived issue → resolves from `ia/backlog-archive/`.
- Journal `db_unconfigured` → `ok: true`, `recent_journal: []`, `meta.partial.failed: 1`.
- `lifecycle_stage_context` all 4 stages return enriched bundles; partial failures propagate.
- Tests green.
- Phase 1 — `issue_context_bundle` implementation + tests.
- Phase 2 — `lifecycle_stage_context` implementation + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | issue_context_bundle | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/issue-context-bundle.ts`: core sub-fetch = `backlog_issue` (fail → `ok: false`); optional = `router_for_task` + `spec_section × N` + `invariants_summary` + `project_spec_journal_search` (each fail → `meta.partial.failed++`). Search both `ia/backlog/` and `ia/backlog-archive/` for `depends_on` chain. Return `{ issue, depends_chain, routed_specs, invariant_guardrails, recent_journal }` under `wrapTool`. |
| T7.2 | issue_context_bundle tests | _pending_ | _pending_ | Tests: happy path (all 5 sub-fetches succeed); `depends_on` references archived issue → resolved; `project_spec_journal_search` unconfigured (`db_unconfigured`) → `ok: true`, `recent_journal: []`, `meta.partial: {succeeded:4, failed:1}`; `backlog_issue` not found → `ok: false, error.code = "issue_not_found"`. |
| T7.3 | lifecycle_stage_context | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/lifecycle-stage-context.ts`: `stage ∈ {author, implement, verify, close}` → stage map dispatches `issue_context_bundle` + stage-specific extras: `author` adds glossary anchors + Stage 1×N bulk-author context (replaces the retired `kickoff` stage per M6 collapse); `implement` adds per-phase domain prep + invariants; `verify` adds bridge preflight hints; `close` adds closeout digest + journal search. `meta.partial` aggregates across all sub-fetches. |
| T7.4 | lifecycle_stage_context tests | _pending_ | _pending_ | Tests: all 4 stage values (`author` / `implement` / `verify` / `close`) return enriched bundles; unknown `stage` value → `{ ok: false, error: { code: "invalid_input" } }`; optional stage-extra sub-fetch failure → `ok: true`, `meta.partial.failed++`; `stage: "close"` + db unconfigured → graceful degradation on journal + digest. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
