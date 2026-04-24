### Stage 34 — Catalog composite authoring / Pools tree CRUD (supersedes Stage 32)


**Status:** Draft (tasks _pending_ — not yet filed; Step 10 opens only when grid-asset-visual-registry Step 5 Stage 5.1 Final — pools self-ref tree + composite_type schemas shipped)

**Objectives:** Ship the pools-tree admin surface at `/admin/catalog/pools` that supersedes the Stage-32 flat pool list. Consumes grid-asset-visual-registry Stage 5.1 `/api/catalog/pools` endpoints (tree read, create, move, retire) + MCP `catalog_pool_*` authoring tools (read-only). Renders the self-ref `parent_pool_id` hierarchy with parent picker, cycle-prevention validator, and member-weight inline edit.

**Exit:**

- `web/app/admin/catalog/pools/page.tsx` RSC: recursive pools tree view (root pools + nested children via `parent_pool_id`); collapsible `<Surface tone="raised">` nodes; member count + weight sum preview per node. Supersedes Stage 32 flat-list layout (L9).
- `web/app/admin/catalog/pools/[id]/page.tsx` RSC + Client island: pool detail with parent picker (catalog-pool combobox, disallowing self + descendants via client-side cycle check), member list with inline `weight` edit + add/remove member flow (catalog-asset picker), retire action.
- Create-pool modal at `/admin/catalog/pools/new`: form fields (name, parent picker, description, initial members[]); POST transactional; optimistic-lock via `updated_at` round-trip (L13).
- Cycle-prevention validator: client-side precheck on parent picker (fetches full ancestors via `GET /api/catalog/pools/:id/ancestors`); server validates on PATCH and rejects with typed 422.
- Sidebar `LINKS` entry updated: `{ href: '/admin/catalog/pools', label: 'Pools', Icon: TreePine }` under Admin group (Stage 38).
- `npm run validate:web` green; Playwright route spec covers tree render + create child + move + cycle-reject + weight edit.
- Phase 1 — Tree RSC + ancestor resolver + sidebar link.
- Phase 2 — Detail RSC + parent picker + member editor + create modal.
- Phase 3 — Cycle-prevention + retire + playwright coverage.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T34.1 | _pending_ | _pending_ | Author `web/app/admin/catalog/pools/page.tsx` RSC — fetch `GET /api/catalog/pools?shape=tree` (returns nested `children[]`); render via `<PoolNode>` recursive client component in `web/components/catalog/PoolNode.tsx` (collapsible `<Surface tone="raised">` header with name + member count + weight sum; children render recursively); `<Rack>` frame + `<Breadcrumb>` Dashboard › Admin › Catalog › Pools; auth gate via `web/proxy.ts` matcher (already widened in Stage 30). Add sidebar `LINKS` entry under Admin group. `npm run validate:web` green. |
| T34.2 | _pending_ | _pending_ | Author `web/app/admin/catalog/pools/[id]/page.tsx` RSC — joined pool + members + parent_id view; members rendered in `<DataTable>` with inline `weight` edit Client island (PATCH `/api/catalog/pools/:id/members/:memberId` with `updated_at` round-trip; 409 surfaces conflict UI per L13); add/remove member flow via `<CatalogAssetPicker>` combobox modal; retire action modal (confirm + POST `/api/catalog/pools/:id/retire`). |
| T34.3 | _pending_ | _pending_ | Author `web/app/admin/catalog/pools/new/page.tsx` Client island — create form: name, parent picker (`<CatalogPoolPicker>` combobox, fetches `GET /api/catalog/pools?shape=flat`), description, initial members[] (optional); POST transactional; redirect to detail on 201; surface field-level validation errors. |
| T34.4 | _pending_ | _pending_ | Author `web/components/catalog/CatalogPoolPicker.tsx` Client combobox — async-search over `GET /api/catalog/pools?shape=flat&q=`; when used as parent picker, exclude self + descendants via client-side precheck (fetch `GET /api/catalog/pools/:id/ancestors` when editing; derive descendant set from cached tree when creating); render excluded items with tooltip "would create cycle". |
| T34.5 | _pending_ | _pending_ | Author Playwright spec `web/tests/e2e/admin-catalog-pools.spec.ts` — covers: (a) tree renders root + children (b) create child pool under existing parent (c) move pool to new parent via edit (d) cycle-reject: picking self or descendant as parent surfaces tooltip + disables submit (e) inline weight edit round-trips + 409 conflict replay (f) retire flow. `npm run validate:web` + `npm run validate:e2e` green. |

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

---
