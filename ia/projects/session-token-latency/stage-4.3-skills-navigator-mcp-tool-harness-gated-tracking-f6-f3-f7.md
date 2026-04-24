### Stage 4.3 — Skills navigator MCP tool + harness-gated tracking (F6 + F3 + F7)


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `skill_for_task(keywords, lifecycle_stage)` MCP tool replacing the pattern-match-yourself rule (`ia/skills/README.md` is 150 lines). File tracking issues for F3 (harness-level caveman enforcement, `PreCompletion` hook) and F7 (`defer_loading: true` rollout) — zero code change for tracking items; monitoring only pending Anthropic harness confirmation.

**Exit:**

- MCP tool `skill_for_task` registered in `tools/mcp-ia-server/src/index-ia.ts`; returns `{skill_name, skill_path, url, first_phase_body}` for keywords + lifecycle_stage query.
- Integration test: `skill_for_task("implement spec", "implement")` returns path matching `ia/skills/project-spec-implement/SKILL.md`.
- Tracking BACKLOG issues filed for F3 + F7 with `harness-gated` label and dependency note on Anthropic harness capability.
- `docs/session-token-latency-audit-exploration.md` §Open questions: F3 + F7 tracking issues linked.
- `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T4.3.1 | skill_for_task MCP tool | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/skill-for-task.ts`: `registerTool("skill_for_task", ...)` with inputs `keywords: string[]`, `lifecycle_stage?: string`; reads `ia/skills/README.md` index + each `ia/skills/*/SKILL.md` frontmatter (`title`, `phases`, `trigger` fields); computes keyword overlap score; returns top-1 match with `{skill_name, skill_path, url, first_phase_body}` (first phase body = first `### Phase 1` section text, ≤500 tokens). Register in `index-ia.ts`. |
| T4.3.2 | skill_for_task integration test | _pending_ | _pending_ | Author `tools/mcp-ia-server/tests/skill-for-task.test.ts`: assert `skill_for_task(["implement", "spec"], "implement")` returns path containing `project-spec-implement`; assert `skill_for_task(["stage", "file"], "stage-file")` returns path containing `stage-file-plan`. Add `npm run test:skill-for-task`. `npm run validate:all`. |
| T4.3.3 | F3 tracking issue | _pending_ | _pending_ | File `/project-new TECH-{id}: Track F3 harness-level caveman enforcement (PreCompletion hook)`: notes that `output-style: caveman` frontmatter in skill files requires `PreCompletion` hook support from Claude Code harness; links to `docs/session-token-latency-audit-exploration.md` §F3; blocked until Anthropic harness team confirms `PreCompletion` semantics. Zero code change. Link filed issue from exploration doc §Open questions Q3. |
| T4.3.4 | F7 tracking issue | _pending_ | _pending_ | File `/project-new TECH-{id}: Track F7 defer_loading: true MCP rollout`: monitors Claude Code release notes for `defer_loading: true` per-tool support; links to exploration §F7 + audit source; antidote to B1 two-server split when harness supports per-tool deferred loading. Zero code change until harness confirms. Link filed issue from exploration §Open questions. |

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
