### Stage 11 — Dashboard improvements + UI polish / D3.js data visualization


**Status:** Done — TECH-239 + TECH-240 + TECH-241 + TECH-242 all closed 2026-04-16 (archived)

**Objectives:** Install `d3` + `@types/d3`; author `PlanChart` `'use client'` component with grouped-bar status-breakdown chart per plan; wire `dynamic()` with `{ ssr: false }` to avoid hydration errors; integrate into dashboard page with data aggregation from `PlanData`; validate no SSR build errors.

**Exit:**

- `d3` + `@types/d3` added to `web/package.json`.
- `web/components/PlanChart.tsx` (new) — `'use client'` SVG chart; D3 `scaleBand` + `scaleLinear` + `axisBottom` + `axisLeft`; grouped bars (pending / in-progress / done per step); fills via `var(--color-*)` CSS vars; axis labels + color legend; empty-state `<p>` when 0 tasks.
- Dashboard page imports `PlanChart` via `next/dynamic({ ssr: false })`; one chart per plan with loading skeleton; no SSR / hydration errors in `next build` output.
- `web/README.md §Components` PlanChart entry added; `validate:all` green.
- Phase 1 — D3 install + PlanChart component (chart + axes + legend).
- Phase 2 — Dashboard integration + ssr-bypass + validation.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T11.1 | **TECH-239** | Done (archived) | Install `d3` + `@types/d3` into `web/package.json`; author `web/components/PlanChart.tsx` (new) — `'use client'`; props `{ data: { label: string; pending: number; inProgress: number; done: number }[] }`; `useRef<SVGSVGElement>` + `useEffect` for D3 draw; `scaleBand` (step labels) + `scaleLinear` (count); 3 grouped bars per step using nested `scaleBand`; fills via `var(--color-bg-status-pending)` / `var(--color-bg-status-progress)` / `var(--color-bg-status-done)` real `@theme` aliases; static `480×220` SVG; empty-state `<p>` when `data.length === 0`. |
| T11.2 | **TECH-240** | Done (archived) | Extend `PlanChart.tsx` — add `axisBottom` (step label ticks, truncated at 12 chars via `.text(d => d.length > 12 ? d.slice(0,11) + '…' : d)`); `axisLeft` (count integer ticks, `tickFormat(d3.format('d'))`); inline SVG `<text>` legend (3 color swatches + "Pending / In Progress / Done" labels); handle 0-task plan (data array empty → render placeholder `<p className="text-text-muted text-sm">No tasks</p>` instead of SVG). |
| T11.3 | **TECH-241** | Done (archived) | Integrate `PlanChart` into `web/app/dashboard/page.tsx` — `const PlanChart = dynamic(() => import('@/components/PlanChart'), { ssr: false, loading: () => <div className="h-[220px] bg-bg-panel animate-pulse rounded" /> })`; for each plan derive chart data: `plan.steps.map(step => ({ label: step.title, pending: plan.allTasks.filter(t => t.id.startsWith('T' + step.id + '.') && t.status === '_pending_').length, inProgress: …'In Progress'…, done: …'Done (archived)'… }))`; render one `<PlanChart data={chartData} />` per plan below its `DataTable`. |
| T11.4 | **TECH-242** | Done (archived) | Smoke chart in dev + build — run `cd web && npm run build`; confirm zero `ReferenceError: window is not defined` or `document` SSR errors in build output; `validate:all` green; add `web/README.md §Components` PlanChart entry (dynamic import pattern, `ssr: false` rationale, data aggregation shape, fill CSS var names). |

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
