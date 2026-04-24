### Stage 4.2 — Cache-breakpoint prescriptive tooling (F5)


**Status:** Draft (tasks _pending_ — not yet filed)

**Pre-condition:** `docs/prompt-caching-mechanics.md` §3 must define Tier 1 + Tier 2 anchors (authored by lifecycle-refactor T10.2 or earlier). Lifecycle-refactor T10.7 (20-block guardrail) recommended (F5 complements T10.7 prohibitive rule with prescriptive recipe).

**Objectives:** Add new MCP tool `cache_breakpoint_recommend(stage_id)` returning the 4 recommended breakpoint anchors for a given Stage. Author `npm run validate:cache-breakpoints` CI lint enforcing breakpoint annotations in skill preambles. Document 4-anchor layout prescriptively in `prompt-caching-mechanics.md` §F5.

**Exit:**

- MCP tool `cache_breakpoint_recommend` registered in `tools/mcp-ia-server/src/index-ia.ts`; returns `{tier1_end, tier2_bundle_end, spec_end, last_executor_mutable}` for a given `stage_id`.
- `npm run validate:cache-breakpoints` script: reads `ia/skills/*/SKILL.md` preambles; asserts breakpoint annotation present; exits non-zero if missing. Added to `validate:all`.
- `docs/prompt-caching-mechanics.md` §F5: 4-anchor layout documented as prescriptive recipe (not just prohibitive reference).
- `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T4.2.1 | cache_breakpoint_recommend MCP tool | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/cache-breakpoint-recommend.ts`: `registerTool("cache_breakpoint_recommend", ...)` with input `stage_id: string`; reads stage block from `ia/projects/*/master-plan.md` matching stage_id; returns 4 anchor objects `{name, anchor_type, location_hint}` per `prompt-caching-mechanics.md` §3 Tier 1/Tier 2 definitions + Tier 3 (spec end) + Tier 4 (last executor-mutable block). Register in `index-ia.ts`. |
| T4.2.2 | Skill preamble breakpoint annotation | _pending_ | _pending_ | Update `ia/skills/*/SKILL.md` preamble sections (lifecycle skills: `stage-file-plan`, `project-spec-implement`, `opus-code-review`, `stage-closeout-plan`, `plan-author`, `opus-audit`) to include a `cache_breakpoints:` frontmatter line listing the 4 anchor names. Use `cache_breakpoint_recommend` output to derive correct values per skill's lifecycle_stage. |
| T4.2.3 | validate:cache-breakpoints CI script | _pending_ | _pending_ | Author `tools/scripts/validate-cache-breakpoints.sh`: reads `ia/skills/*/SKILL.md` frontmatter; for skills with `phases:` key (progress-emit lifecycle skills), asserts `cache_breakpoints:` key present with ≥4 named anchors. Add `npm run validate:cache-breakpoints` to `package.json` + `validate:all` chain. |
| T4.2.4 | 4-anchor layout documentation | _pending_ | _pending_ | Append `## F5 — Prescriptive 4-anchor recipe` to `docs/prompt-caching-mechanics.md`: document Tier 1 (stable prefix end), Tier 2 (bundle end), Tier 3 (spec end), Tier 4 (last executor-mutable block); note 4-anchor Anthropic cap; note how this complements T10.7 prohibitive rule (forbids >1 stable-prefix block) with prescriptive layout guidance. `npm run validate:all`. |

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
