### Stage 31 — Catalog admin CRUD views / Edit + create forms + retire action


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship authoring surfaces: edit form on detail page (PATCH with optimistic-lock + `preview-diff` preview), create form at `/admin/catalog/assets/new`, retire action with `replaced_by` picker. Forms consume `/api/catalog/assets` POST + PATCH + `/retire` + `/preview-diff` endpoints.

**Exit:**

- `web/app/admin/catalog/assets/[id]/edit/page.tsx` Client island: form fields bound to joined DTO; PATCH submits with `updated_at` optimistic-lock; 409 response renders conflict resolution UI.
- `web/app/admin/catalog/assets/new/page.tsx` Client island: blank-form create; POST transactional (asset + economy + sprite-slots in one call).
- Retire modal: confirm dialog + `replaced_by` combobox (catalog-lookup); POST `/api/catalog/assets/:id/retire`.
- Preview-diff panel: calls `POST /api/catalog/preview-diff` before commit; shows human-readable plan.
- `npm run validate:web` green; Playwright spec covers create + edit + 409 conflict + retire happy path.
- Phase 1 — Edit form + optimistic-lock UX.
- Phase 2 — Create form + retire modal + preview-diff panel + playwright.

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
