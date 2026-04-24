### Stage 6 — Live dashboard / Plan loader + typed schema


**Status:** Done (archived 2026-04-15 — TECH-200 / TECH-201 / TECH-202 / TECH-203 closed; loader + types + RSC stub + README §Dashboard + JSDoc all landed)

**Objectives:** Author `web/lib/plan-loader.ts` as a read-only wrapper around `tools/progress-tracker/parse.mjs`, exporting `loadAllPlans(): Promise<PlanData[]>` for RSC consumption. Pin the parse.mjs output schema as TypeScript interfaces so downstream consumers are type-safe and `parse.mjs` itself stays untouched.

**Exit:**

- `web/lib/plan-loader-types.ts` exports `TaskStatus`, `HierarchyStatus`, `TaskRow`, `PhaseEntry`, `Stage`, `Step`, `PlanData` TypeScript interfaces mirroring the parse.mjs JSDoc output schema exactly.
- `web/lib/plan-loader.ts` exports `loadAllPlans(): Promise<PlanData[]>` — globs `ia/projects/*-master-plan.md` from repo root, reads each file, calls `parseMasterPlan(content, filename)` via dynamic ESM import, returns typed array.
- `parse.mjs` has zero modifications — wrapper-only contract upheld.
- `validate:all` green; `loadAllPlans()` resolves with ≥1 plan against current repo state (confirmed in T3.1.3).
- `web/README.md` §Dashboard documents loader contract, `PlanData` shape, and "parse.mjs is authoritative" invariant.
- Phase 1 — Types + loader implementation.
- Phase 2 — Build integration + smoke + docs.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T6.1 | **TECH-200** | Done (archived) | Author `web/lib/plan-loader-types.ts` — TypeScript interfaces: `TaskStatus` (union literal), `HierarchyStatus` (union literal), `TaskRow { id, phase, issue, status, intent }`, `PhaseEntry { checked, label }`, `Stage { id, title, status, statusDetail, phases, tasks }`, `Step { id, title, status, statusDetail, stages }`, `PlanData { title, overallStatus, overallStatusDetail, siblingWarnings, steps, allTasks }` — mirroring parse.mjs JSDoc schema exactly. |
| T6.2 | **TECH-201** | Done (archived) | Author `web/lib/plan-loader.ts` — `loadAllPlans(): Promise<PlanData[]>`: globs `ia/projects/*-master-plan.md` from repo root via `fs.promises` + path resolution; reads each file; calls `parseMasterPlan(content, filename)` via dynamic `import()` of `../../tools/progress-tracker/parse.mjs`; returns typed `PlanData[]`. `parse.mjs` untouched. |
| T6.3 | **TECH-202** | Done (archived) | Verify Next.js RSC can call `loadAllPlans()` at build time without bundler errors — confirm dynamic `import()` of `parse.mjs` resolves in Node 20+ ESM context (server component, no `"use client"`); stub `web/app/dashboard/page.tsx` (bare RSC calling `loadAllPlans()` + logging plan count); `validate:all` green. |
| T6.4 | **TECH-203** | Done (archived) | Extend `web/README.md` with §Dashboard section — documents `loadAllPlans()` contract, `PlanData` shape key fields, "parse.mjs is authoritative — plan-loader is read-only wrapper" invariant, and consumption pattern for RSC callers; add inline JSDoc to `plan-loader.ts` with glob-path note + invariant comment. |

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
