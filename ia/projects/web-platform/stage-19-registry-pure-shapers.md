### Stage 19 — Release-scoped progress view / Registry + pure shapers


**Status:** Final (4 tasks filed 2026-04-17 — TECH-339..TECH-342; all archived 2026-04-18)

**Backlog state (2026-04-18):** 4 / 4 tasks filed; all closed.

**Objectives:** Author the hand-maintained release registry, pure filtering shaper, default-expand predicate, and plan-tree builder. No routes, no UI, no auth changes. Self-contained data layer consumed by Stage 7.2 pages.

**Exit:**

- `web/lib/releases.ts`: `Release` interface + `resolveRelease()` + seeded `full-game-mvp` row; header comment cites **Rollout tracker** doc as source of truth.
- `web/lib/releases/resolve.ts`: `getReleasePlans()` pure filter; silently drops missing-on-disk children; imports `PlanData` from `web/lib/plan-loader-types.ts`.
- `web/lib/releases/default-expand.ts`: `deriveDefaultExpandedStepId()` predicate; JSDoc "tasks are ground truth; stale step-header Status prose ignored" + `'blocked'` unreachable note.
- `web/lib/plan-tree.ts`: `buildPlanTree()` + `TreeNodeData` union; phase nodes from `task.phase` groupBy, NOT `Stage.phases` checklist; JSDoc NB1.
- Unit tests for all four modules under `web/lib/**/__tests__/`; `npm run validate:web` green.
- Phase 1 — Registry + resolve shaper (`releases.ts` + `releases/resolve.ts` + tests).
- Phase 2 — Default-expand + plan-tree shapers (`releases/default-expand.ts` + `plan-tree.ts` + tests).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T19.1 | **TECH-339** | Done (archived) | Author `web/lib/releases.ts` — `Release` interface (`id`, `label`, `umbrellaMasterPlan`, `children: string[]`) + `resolveRelease(id: string): Release | null` + seeded `releases` const array with `full-game-mvp` row (9 children from extensions doc Examples block); header comment cites `docs/full-game-mvp-rollout-tracker.md` as source of truth for `children[]` drift warning. |
| T19.2 | **TECH-340** | Done (archived) | Author `web/lib/releases/resolve.ts` — `getReleasePlans(release: Release, allPlans: PlanData[]): PlanData[]` pure filter; matches `plan.filename` basename against `release.children`; silently drops missing-on-disk entries. Author `web/lib/__tests__/releases.test.ts` — unit tests: `resolveRelease` found/not-found, `getReleasePlans` filter + missing-child drop + umbrella self-inclusion edge case. |
| T19.3 | **TECH-341** | Done (archived) | Author `web/lib/releases/default-expand.ts` — `deriveDefaultExpandedStepId(plan: PlanData, metrics: PlanMetrics): string | null`; iterates `plan.steps` in order; returns first step id where `metrics.stepCounts[step.id]?.done < metrics.stepCounts[step.id]?.total`; returns `null` if all done or steps empty; JSDoc: "tasks are ground truth; stale step-header Status prose ignored" + `'blocked'` unreachable note. Author `web/lib/__tests__/default-expand.test.ts` — unit tests: first-non-done, all-done null, all-pending returns first, stale-header ignored, empty-steps null. |
| T19.4 | **TECH-342** | Done (archived) | Author `web/lib/plan-tree.ts` — `TreeNodeData` discriminated union (kind: `step | stage | phase | task`; id, label, status, counts, children); `buildPlanTree(plan: PlanData, metrics: PlanMetrics): TreeNodeData[]`; synthesizes phase nodes by `groupBy(task.phase)` within each stage (NOT conflated with `Stage.phases` checklist; JSDoc NB1); per-node status from `BadgeChip` Status union (`done | in-progress | pending | blocked`). Author `web/lib/__tests__/plan-tree.test.ts` — unit tests: stage-node counts, phase synthesis from tasks, status derivation, all-done propagation. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
