---
purpose: Web workspace — business logic in backend, frontend renders pre-processed content only
audience: agent
loaded_by: on-demand
slices_via: none
description: Web dashboard / pages must not embed derivation logic in components; backend (lib/, route handlers, loaders) pre-processes and frontend renders.
alwaysApply: false
---

# Web workspace — backend logic, frontend render-only

Scope: `web/` Next.js workspace (App Router).

## Rule

- Derivation, aggregation, parsing, status-inference, and any non-trivial transformation live in **backend modules** — `web/lib/**`, route handlers (`web/app/**/route.ts`), server components' data-loading paths, or server-only utilities.
- **Frontend / client components** (`'use client'` files, presentational `web/components/**`, page-body JSX) consume already-shaped props and render. No inline reducers, no status computation, no markdown re-parsing, no task-state aggregation at render time.
- Server components may call backend libs directly but must not re-implement logic — call the canonical lib function.

## Why

- Drift: same derivation re-implemented in parser + component diverges silently (stale Status lines in markdown kept showing Pending because badge mapper lived downstream of a parser that ignored task-state truth).
- Testability: lib functions are pure + unit-testable; components are not.
- ISR: backend runs per-revalidate, frontend per-request — derivation in frontend wastes CPU per page view.
- Consistency: dashboard, feed, wiki, API all share one source of truth when derivation lives in `lib/`.

## How to apply

- New dashboard column / badge / metric → extend the relevant `web/lib/*-parser.ts` or `web/lib/*-loader.ts` to emit the pre-computed field on the typed return object; component reads the field.
- Status badges, progress counts, rollup percentages, "done / total" figures → computed in the loader/parser, not in the renderer.
- If a component needs data the loader does not yet expose, **add a field to the loader's return type**; do not compute it in-component.
- Client-side reactive state (form inputs, expand/collapse, modal open) is fine in client components — that is UI state, not business logic.

## Boundary examples

| Backend (allowed in lib/) | Frontend (render only) |
|---|---|
| `deriveHierarchyStatus(steps)` | `<BadgeChip status={stage.status} />` |
| Parse markdown → typed `PlanData` | Map `plan.steps` over JSX |
| Count tasks by status → totals | Display `done / total` from props |
| Aggregate cross-plan metrics | Render a number passed in |

## Relation to other rules

- Caveman / English boundary: `ia/rules/agent-output-caveman.md` §exceptions — user-facing copy under `web/content/**` + page-body strings stay full English.
- Web orchestrator (permanent master plan): `ia/projects/web-platform-master-plan.md`.
