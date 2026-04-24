### Stage 14 — Critical Misses: Authoring Parity + Atomicity + Dry-run (post-plan review addendum) / Master-plan Authoring Tools


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement three authoring tools mirroring the glossary/spec/rule authorship pattern from Stage 4.2, targeted at the highest-churn IA surface (master-plan orchestrators). All three validate cardinality + `caller_agent` gate + path-under-`ia/projects/` guard per invariant #12. Reuses Stage 3.2 orchestrator-parser via a new `serialize()` inverse.

**Exit:**

- `master_plan_create({ slug, metadata, steps, caller_agent: "master-plan-new" })` writes `ia/projects/{slug}-master-plan.md` via orchestrator-parser serializer; rejects if file exists (hint: use `master_plan_step_append`).
- `master_plan_step_append({ slug, step, caller_agent: "master-plan-extend" })` appends new Step to existing orchestrator; preserves existing Steps verbatim (never rewrites).
- `stage_decompose_apply({ slug, step_id, decomposition, caller_agent: "stage-decompose" })` expands one skeleton step into stages → phases → tasks in-place; preserves step header + Objectives + Exit criteria.
- Cardinality validator rejects: `< 2 phases per stage`, `< 2 tasks per phase` (unless `justification` field present), duplicate stage ids, duplicate task ids within stage.
- Unauthorized caller → `unauthorized_caller`.
- Tests green.
- Phase 1 — Serializer + cardinality validator + create/append tools.
- Phase 2 — Stage-decompose tool + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T14.1 | Orchestrator serializer + cardinality validator | _pending_ | _pending_ | Extend `tools/mcp-ia-server/src/parser/orchestrator-parser.ts` with `serialize(snapshot): string` inverse of the Stage 3.2 parser — emits master-plan Markdown (header block, Steps, Stages, phase checkboxes, task tables, orchestration guardrails footer). Author `tools/mcp-ia-server/src/parser/cardinality-validator.ts` — enforces `project-hierarchy` rules: ≥2 phases/stage, ≥2 tasks/phase (allow `justification?: string` override), unique stage ids, unique task ids within stage; returns `{ ok, violations: [{ stage_id, level, message }] }`. |
| T14.2 | master_plan_create + master_plan_step_append | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/master-plan-create.ts` + `master-plan-step-append.ts` via `wrapTool` + `checkCaller`. `create`: path guard (invariant #12 — under `ia/projects/`), file-exists guard, cardinality-validate input, `serialize()` to markdown, atomic temp-file swap write. `step_append`: parse existing orchestrator, validate new step cardinality, splice new Step block before `## Orchestration guardrails` footer, re-`serialize()`, atomic swap — never modify existing Steps prose. |
| T14.3 | stage_decompose_apply | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/stage-decompose-apply.ts` via `wrapTool` + `checkCaller` (allowlist: `stage-decompose`). Parse orchestrator; locate step by `step_id`; detect skeleton marker (missing stages section OR `_pending decomposition_` placeholder); replace with generated stages → phases → tasks structure; preserve step header + Objectives + Exit criteria + Relevant surfaces verbatim; cardinality-validate before write; target step already decomposed → `invalid_input` (hint: edit in place or rerun `stage-decompose` skill). |
| T14.4 | Authoring tool tests | _pending_ | _pending_ | Tests in `tools/mcp-ia-server/tests/tools/master-plan-authoring.test.ts`: happy paths for all three tools; cardinality violations (phase with 1 task, stage with 1 phase, duplicate ids) → `invalid_input` with violation list; path outside `ia/projects/` → `invalid_input`; unauthorized caller → `unauthorized_caller`; `master_plan_create` on existing file → `invalid_input` (hint: `master_plan_step_append`); `stage_decompose_apply` on already-decomposed step → `invalid_input`; `master_plan_step_append` snapshot test confirms existing Steps byte-identical post-append. |

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
