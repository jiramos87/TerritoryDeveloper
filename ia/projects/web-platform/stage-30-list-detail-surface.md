### Stage 30 — Catalog admin CRUD views / List + detail surface


**Status:** Draft (tasks _pending_ — not yet filed; Step 9 opens only when Step 8 Final + grid-asset-visual-registry Step 1.3 shipped)

**Objectives:** Ship the catalog list page + single-asset detail view under `/admin/catalog/**`, consuming `GET /api/catalog/assets` and `GET /api/catalog/assets/:id` from grid-asset-visual-registry Step 1.3. All surfaces built on `--ds-*` primitives + console chrome (Rack / Bezel / Screen) per D5 lock.

**Exit:**

- `web/app/admin/catalog/assets/page.tsx` RSC: list view with `status` filter (`published` default), category filter, pagination; wraps rows in `<DataTable>` (post-Stage-8 token-migrated); `<Rack>` frame + `<Breadcrumb>` Dashboard › Admin › Catalog.
- `web/app/admin/catalog/assets/[id]/page.tsx` RSC: joined asset + economy + sprite-slot view; read-only at this stage; `<Surface tone="raised">` for each subsystem panel.
- Auth gate via `web/proxy.ts` matcher widen to `/admin/:path*`; unauthenticated → 302 `/auth/login`.
- Sidebar `LINKS` entry added: `{ href: '/admin/catalog/assets', label: 'Catalog', Icon: Boxes }`.
- `npm run validate:web` green; Playwright route spec covers list + detail 200.
- Phase 1 — List RSC + auth matcher + sidebar link.
- Phase 2 — Detail RSC + playwright coverage.

**Tasks:** _pending_ — materialize via `/stage-decompose` when Step 9 opens.

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
