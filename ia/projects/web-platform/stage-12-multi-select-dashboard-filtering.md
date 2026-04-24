### Stage 12 — Dashboard improvements + UI polish / Multi-select dashboard filtering


**Status:** Done (TECH-247 + TECH-248 + TECH-249 + TECH-250 archived 2026-04-16)

**Objectives:** Upgrade `FilterChips` with per-chip `href` override for multi-select callers; author `web/lib/dashboard/filter-params.ts` URL helpers (`toggleFilterParam`, `parseFilterValues`); update dashboard `searchParams` parsing to multi-value arrays (OR within dimension, AND across); add per-value de-select and "clear all filters" ghost `Button` control.

**Exit:**

- `web/components/FilterChips.tsx` extended — `Chip` interface gains optional `href?: string` (explicit URL override); `active?: boolean` per-chip; backward-compatible (chips without `href` unchanged).
- `web/lib/dashboard/filter-params.ts` (new) — exports `toggleFilterParam(search, key, value): string`; `parseFilterValues(params, key): string[]` (handles comma-delimited + repeated params); `clearFiltersHref` constant `'/dashboard'`.
- Dashboard `searchParams` parsed via `parseFilterValues`; `PlanData[]` + `TaskRow[]` filtered server-side (OR within dimension, AND across); each chip `href` from `toggleFilterParam`.
- "Clear filters" ghost `Button` visible when `searchParams` non-empty; full-English "Clear filters" text (caveman-exception); `validate:all` green.
- Phase 1 — FilterChips extension + URL helper module.
- Phase 2 — Dashboard wiring + clear-filters control + validation.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T12.1 | **TECH-247** | Done (archived) | Extend `web/components/FilterChips.tsx` — update `Chip` interface: add `href?: string` (when present, chip renders `<a href={href}>` directly instead of computing href internally); `active?: boolean` stays per-chip; remove any assumption of exactly one active chip; existing single-select callers (no `href` in chips) fall back to `href="#"` — backward-compatible; no `'use client'` conversion needed (chips are purely declarative). |
| T12.2 | **TECH-248** | Done (archived) | Author `web/lib/dashboard/filter-params.ts` (new) — exports: `parseFilterValues(params: URLSearchParams \ | ReadonlyURLSearchParams, key: string): string[]` — splits comma-delimited value + collects repeated params, deduplicates, returns sorted array; `toggleFilterParam(currentSearch: string, key: string, value: string): string` — parses `currentSearch` into `URLSearchParams`, adds `value` if absent or removes if present (comma-delimited representation), returns new query string; `clearFiltersHref = '/dashboard'` constant. |
| T12.3 | **TECH-249** | Done (archived) | Update `web/app/dashboard/page.tsx` `searchParams` parsing — replace single-value reads with `parseFilterValues(new URLSearchParams(searchParams as Record<string, string>), 'plan')` etc. for each dimension; filter `PlanData[]` (OR within dimension, AND across): `plan` filter on `plan.title`, `status` filter on `task.status`, `phase` filter on `task.phase`; pass per-chip `href` from `toggleFilterParam(new URLSearchParams(searchParams as Record<string, string>).toString(), key, chipValue)` to `FilterChips` chips array. |
| T12.4 | **TECH-250** | Done (archived) | Add "clear all filters" control to dashboard page — conditionally render `<Button variant="ghost" href="/dashboard">Clear filters</Button>` (full-English caveman-exception) when `Object.values(searchParams ?? {}).some(Boolean)`; smoke multi-select: `?status=Draft,In+Progress` narrows rows; each chip individually de-selectable; single-chip round-trip `toggleFilterParam` adds then removes cleanly; `validate:all` green. |

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
