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

- Derivation, aggregation, parsing, status-inference, any non-trivial transformation → **backend modules** — `web/lib/**`, route handlers (`web/app/**/route.ts`), server components' data-loading paths, server-only utilities.
- **Frontend / client components** (`'use client'` files, presentational `web/components/**`, page-body JSX) consume already-shaped props + render. No inline reducers, no status computation, no markdown re-parsing, no task-state aggregation at render time.
- Server components may call backend libs directly but must not re-implement logic — call canonical lib function.

## Why

- Drift: same derivation re-implemented in parser + component diverges silently (stale Status lines in markdown kept showing Pending because badge mapper lived downstream of parser ignoring task-state truth).
- Testability: lib functions = pure + unit-testable; components = not.
- ISR: backend runs per-revalidate, frontend per-request — derivation in frontend wastes CPU per page view.
- Consistency: dashboard, feed, wiki, API all share one source of truth when derivation lives in `lib/`.

## How to apply

- New dashboard column / badge / metric → extend relevant `web/lib/*-parser.ts` or `web/lib/*-loader.ts` to emit pre-computed field on typed return object; component reads field.
- Status badges, progress counts, rollup percentages, "done / total" figures → computed in loader/parser, not renderer.
- Component needs data loader doesn't yet expose → **add field to loader's return type**; do not compute it in-component.
- Client-side reactive state (form inputs, expand/collapse, modal open) fine in client components — UI state, not business logic.

## Boundary examples

| Backend (allowed in lib/) | Frontend (render only) |
|---|---|
| `deriveHierarchyStatus(steps)` | `<BadgeChip status={stage.status} />` |
| Parse markdown → typed `PlanData` | Map `plan.steps` over JSX |
| Count tasks by status → totals | Display `done / total` from props |
| Aggregate cross-plan metrics | Render a number passed in |

## Pagination contract

`GET /api/catalog/assets` uses keyset (cursor) pagination on `bigserial id`, ascending:

- Query params: `limit` (default 200, range 1–500), `cursor` (numeric id string; scans `id > cursor`).
- Response: `{ assets: CatalogAsset[], next_cursor: string | null, limit: number }`.
- `next_cursor` = last row id when `assets.length === limit`, else `null` (exhausted).
- See `web/lib/catalog/parse-list-query.ts` + `web/app/api/catalog/assets/route.ts`.

## Error-response envelope

All `/api/catalog/*` routes emit errors via `web/lib/catalog/catalog-api-errors.ts` helpers:

- `catalogJsonError(status, code, message, { details?, current?, logContext? })`.
- `responseFromPostgresError(e, fallback)` maps `23505 → 409 unique_violation`, `23503 → 400 foreign_key_violation`, `22P02 → 400 bad_request`, else `500 internal`.
- Response body: `{ error: string, code: CatalogErrorCode, details?: unknown, current?: unknown }` — no stack traces in body.
- `CatalogErrorCode` enum: `bad_request | not_found | conflict | internal | unique_violation | foreign_key_violation`.

## Retire idempotency

`POST /api/catalog/assets/:id/retire`:

- Empty body valid: retires asset with `replaced_by = null`.
- `replaced_by` missing id → 409 `conflict` (not 404 — 404 reserved for primary `:id`).
- `replaced_by` references retired asset → 409 `conflict`.
- `replaced_by === :id` (self) → 400 `bad_request`.
- Re-retiring already-retired asset returns 200 with current composite (idempotent).

## Relation to other rules

- Caveman / English boundary: `ia/rules/agent-output-caveman.md` §exceptions — user-facing copy under `web/content/**` + page-body strings stay full English.
- Web orchestrator (permanent master plan): `ia/projects/web-platform-master-plan.md`.
