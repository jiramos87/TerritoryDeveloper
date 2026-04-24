### Stage 10 — Dashboard improvements + UI polish / UI primitives polish + dashboard percentages


**Status:** Done (TECH-231 + TECH-232 + TECH-233 + TECH-234 archived 2026-04-16)

**Objectives:** Author `Button` primitive with variant + size props consuming design tokens; extend `DataTable` with optional `pctColumn` prop rendering `StatBar` inline; compute and display per-plan and per-step completion percentages on the dashboard derived from `PlanData`; `plan-loader.ts` + `plan-loader-types.ts` untouched.

**Exit:**

- `web/components/Button.tsx` (new) — `variant: 'primary' | 'secondary' | 'ghost'`; `size: 'sm' | 'md' | 'lg'`; polymorphic (`<button>` default, `<a>` when `href` present); design token classes; `disabled` state; exports `ButtonProps`.
- `web/components/DataTable.tsx` extended — optional `pctColumn?: { dataKey: keyof T; label?: string; max?: number }` prop; renders `StatBar` inline for named key; existing column contract unchanged.
- Dashboard renders per-plan `StatBar` (done / total tasks) in plan section heading and per-step compact `StatBar` rows below each step heading; both computed from `PlanData.allTasks` — `plan-loader.ts` untouched.
- `web/README.md §Components` Button + DataTable `pctColumn` entries added; `validate:all` green.
- Phase 1 — Button + DataTable pctColumn primitives.
- Phase 2 — Dashboard percentage rendering + docs.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T10.1 | **TECH-231** | Done (archived) | Author `web/components/Button.tsx` (new) — polymorphic: renders `<button>` (default) or `<a>` when `href` prop present; `variant: 'primary' \ | 'secondary' \ | 'ghost'` mapped to corrected token utility classes (`bg-bg-status-progress text-text-status-progress-fg` / `bg-bg-panel text-text-primary border border-text-muted/40` / `bg-transparent text-text-muted hover:text-text-primary` — phantom `accent-info` / `border-border` names from spec draft replaced during kickoff with real `globals.css @theme` aliases); `size: 'sm' \ | 'md' \ | 'lg'` mapped to `px-/py-/text-` scale; `disabled` → `opacity-50 cursor-not-allowed pointer-events-none`; named-exports `Button` + `ButtonProps`. |
| T10.2 | **TECH-232** | Done (archived) | Extend `web/components/DataTable.tsx` — add optional `pctColumn?: { dataKey: keyof T; label?: string; max?: number }` prop; when provided, appends an extra column rendering `<StatBar value={(row[dataKey] as number) / (pctColumn.max ?? 100) * 100} />` with `label ?? 'Progress'` header; all existing column definitions, generic types, and sort contract unchanged; import `StatBar` from `./StatBar`. |
| T10.3 | **TECH-233** | Done (archived) | Add per-plan completion `StatBar` to `web/app/dashboard/page.tsx` — for each plan, compute `completedCount` (`allTasks.filter(t => DONE_STATUSES.has(t.status)).length`, `DONE_STATUSES = {'Done (archived)', 'Done'}`) + `totalCount`; render `<StatBar label="{completedCount} / {totalCount} done" value={completedCount} max={totalCount} />` in plan section heading row next to `BadgeChip`; `plan-loader.ts` + `plan-loader-types.ts` untouched. |
| T10.4 | **TECH-234** | Done (archived) | Add per-step completion stats to dashboard — for each `step` in `plan.steps`, derive step tasks from `plan.allTasks.filter(t => t.id.startsWith('T' + step.id + '.'))` (done / total); render compact `<StatBar>` row below each step heading; add `web/README.md §Components` Button + DataTable `pctColumn` entries; `validate:all` green. |

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
