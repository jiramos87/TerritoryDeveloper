### Stage 32 — Catalog admin CRUD views / Pool management surface


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship spawn-pool management UI at `/admin/catalog/pools`: list pools, view members, edit per-member `weight` inline, add/remove pool members via catalog-asset picker. Consumes Stage 1.4 MCP-backed pool routes (or direct drizzle calls if routes not exposed for pools at Step 1 close).

**Exit:**

- `web/app/admin/catalog/pools/page.tsx` RSC: pool list with member count + weight sum preview.
- `web/app/admin/catalog/pools/[id]/page.tsx` RSC + Client island for weight editing; drag-order optional (deferred if pool member order is catalog-authoritative).
- Add-member modal: catalog-asset search + weight input.
- `npm run validate:web` green; Playwright spec covers pool list + member add + weight edit.
- Phase 1 — Pool list RSC + detail RSC.
- Phase 2 — Weight editing + add/remove member flow + playwright.

**Tasks:** _pending_ — materialize via `/stage-decompose` when Step 9 opens.

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
