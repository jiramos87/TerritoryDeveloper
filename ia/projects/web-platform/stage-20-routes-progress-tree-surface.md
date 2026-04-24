### Stage 20 — Release-scoped progress view / Routes + progress tree surface


**Status:** Final — TECH-351, TECH-352, TECH-353, TECH-354 archived 2026-04-18

**Objectives:** Author `TreeNode` + `PlanTree` Client components; ship the release picker RSC page (`/dashboard/releases`) and progress tree RSC page (`/dashboard/releases/[releaseId]/progress`). Relies on Stage 7.1 shapers.

**Exit:**

- `web/components/TreeNode.tsx`: recursive render; status glyph + label + count summary; `<button aria-expanded aria-controls>` for non-leaf (a11y); leaf tasks show Issue id when not `_pending_`.
- `web/components/PlanTree.tsx` (`'use client'`): `useState<Set<string>>` expanded ids seeded from `props.initialExpanded`; chevron toggle; ONLY Client island on this surface.
- `web/app/dashboard/releases/page.tsx` RSC: registry list with links; `Breadcrumb`; existing primitives only.
- `web/app/dashboard/releases/[releaseId]/progress/page.tsx` RSC: `resolveRelease` → `notFound()` on null; calls `loadAllPlans` + `getReleasePlans` + per-plan `computePlanMetrics` + `buildPlanTree` + `deriveDefaultExpandedStepId`; renders `<PlanTree>` per plan; reserved comment for future `/rollout` sibling (no filesystem stub per B1).
- `npm run validate:web` green.
- Phase 1 — Client components (`TreeNode.tsx` + `PlanTree.tsx`).
- Phase 2 — RSC pages (picker + progress page).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T20.1 | **TECH-351** | Done (archived) | Author `web/components/TreeNode.tsx` — recursive render of `TreeNodeData`; status-colored glyph (chevron for branches, `●` for task leaves); label + `{done}/{total}` count; `<button aria-expanded={isExpanded} aria-controls={childListId}>` for non-leaf toggles (a11y); leaf tasks show Issue id when present (not `_pending_`); consumes existing `BadgeChip` status token CSS classes; props: `node: TreeNodeData, expanded: Set<string>, onToggle: (id: string) => void`. |
| T20.2 | **TECH-352** | Done (archived) | Author `web/components/PlanTree.tsx` — `'use client'`; `useState<Set<string>>(new Set(props.initialExpanded))`; renders root `TreeNodeData[]` list; `onToggle = id => setExpanded(prev => { const next = new Set(prev); next.has(id) ? next.delete(id) : next.add(id); return next; })`; passes `expanded` + `onToggle` to each `<TreeNode>`; props: `{ nodes: TreeNodeData[], initialExpanded: Set<string> }`. ONLY Client island on this surface — progress `page.tsx` stays RSC. |
| T20.3 | **TECH-353** | Done (archived) | Author `web/app/dashboard/releases/page.tsx` (RSC) — imports `releases` registry from `web/lib/releases.ts`; renders `Breadcrumb` (Dashboard › Releases) + list/`DataTable` of release rows, each linking to `/dashboard/releases/{release.id}/progress`; full-English user-facing labels (caveman exception — CLAUDE.md §6); `npm run validate:web` green. |
| T20.4 | **TECH-354** | Done (archived) | Author `web/app/dashboard/releases/[releaseId]/progress/page.tsx` (RSC) — `resolveRelease(params.releaseId)` → `notFound()` on null; `loadAllPlans()` + `getReleasePlans` + per-plan `computePlanMetrics` + `buildPlanTree` + `deriveDefaultExpandedStepId`; render `Breadcrumb` (Dashboard › Releases › {release.label} › Progress) + `<PlanTree nodes={tree} initialExpanded={new Set(defaultId ? [defaultId] : [])} />` per plan; reserved comment `// /dashboard/releases/:releaseId/rollout — reserved; URL 404s by default; no filesystem stub (B1)`; full-English headings. |

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
