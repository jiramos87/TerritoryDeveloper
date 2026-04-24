### Stage 7 — Live dashboard / Dashboard RSC + filters


**Status:** Done (closed 2026-04-15 — TECH-205…TECH-208 archived)

**Objectives:** Ship `/dashboard` RSC consuming `loadAllPlans()`, rendering per-plan task tables via `DataTable`, and wiring `FilterChips` for per-plan / per-status / per-phase filter via URL query params (SSR-only). Apply Q14 obscure-URL gate: route unlinked from public nav, `robots.txt` disallows, "internal" banner displayed.

**Exit:**

- `web/app/dashboard/page.tsx` RSC renders all plans from `loadAllPlans()`; each plan section: title + overall-status `BadgeChip` + `DataTable` with columns `id | phase | issue | status | intent` consuming `plan.allTasks`.
- Step/stage grouping visible via plan heading + `statusDetail`; step heading rows show `HierarchyStatus` badge.
- `FilterChips` for plan / status / phase wired; active state read from `searchParams`; filtering applied server-side before passing rows to `DataTable`.
- "Internal" banner at top of `/dashboard` (full-English user-facing text per caveman-exception).
- `web/app/robots.ts` disallow list extended to include `/dashboard`; route not linked from `web/app/layout.tsx` or any nav component; absent from `web/app/sitemap.ts`.
- `validate:all` green.
- Phase 1 — RSC core (page + DataTable + plan-loader wiring).
- Phase 2 — Filter chips + access gate.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T7.1 | **TECH-205** | Done (archived) | Build out `web/app/dashboard/page.tsx` RSC — import `loadAllPlans()`, render per-plan sections; each section: plan title heading + `BadgeChip` for `overallStatus`; `DataTable` consuming `plan.allTasks` w/ typed columns `id | phase | issue | status | intent`; "internal" banner paragraph at page top (full-English caveman-exception text). |
| T7.2 | **TECH-206** | Done (archived) | Add plan-grouped visual hierarchy — step heading rows (`Step {id} — {title}` + `HierarchyStatus` badge via `BadgeChip`) above per-stage task groups; `statusDetail` in muted text; task rows prefixed by `stage.id` so stage breakdown is scannable within each plan's `DataTable`. |
| T7.3 | **TECH-207** | Done (archived) | Wire `FilterChips` for per-plan / per-status / per-phase — read `searchParams: { plan?, status?, phase? }` in RSC; filter `PlanData[]` + task rows server-side before render; chip `<a href>` links emit query-param URLs; active chip state derived from `searchParams` match against chip value; uses existing `FilterChips` `active` prop from Stage 1.2. |
| T7.4 | **TECH-208** | Done (archived) | Apply Q14 access gate — extend `web/app/robots.ts` disallow array to include `/dashboard`; confirm `/dashboard` absent from `web/app/layout.tsx` nav and `web/app/sitemap.ts`; `validate:all` green. |

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
