### Stage 36 — Catalog composite authoring / Buttons + prefabs CRUD


**Status:** Draft (tasks _pending_ — not yet filed; opens only when Stage 35 Final + grid-asset-visual-registry Step 5 Stage 5.3 Final — prefabs table shipped)

**Objectives:** Ship standalone buttons management (for reusable buttons that aren't inline under a panel) at `/admin/catalog/buttons` and prefabs management at `/admin/catalog/prefabs`. Both surfaces reuse `<DynamicFormFromSchema>` (Stage 35 T35.1) for per-type field rendering. Prefabs bundle a panel-ref + button-refs + layout hints per Stage 5.3 schema.

**Exit:**

- `web/app/admin/catalog/buttons/page.tsx` RSC: list view with `composite_type` filter (`button.*` types only), status filter; `<DataTable>` rows; `<Rack>` + `<Breadcrumb>`.
- `web/app/admin/catalog/buttons/[id]/page.tsx` RSC + edit Client island: schema-driven form via `<DynamicFormFromSchema>` bound to button's `composite_type.props_schema`; PATCH with `updated_at`; 409 conflict UI.
- `web/app/admin/catalog/buttons/new/page.tsx` Client island: two-step form (pick button type → schema-driven fields); POST transactional; redirect to detail.
- `web/app/admin/catalog/prefabs/page.tsx` RSC: list view with `composite_type` filter (`prefab.*` types only), status filter; `<DataTable>` rows.
- `web/app/admin/catalog/prefabs/[id]/page.tsx` RSC + edit Client island: prefab detail shows referenced panel + referenced buttons[] + layout hints; edit form includes panel picker + button pickers (multi) + inline layout JSON editor; PATCH with `updated_at`.
- `web/app/admin/catalog/prefabs/new/page.tsx` Client island: two-step form (pick prefab type → schema-driven fields + panel ref + button refs); POST.
- Sidebar `LINKS` entries added: `Buttons`, `Prefabs` under Admin group.
- `npm run validate:web` green; Playwright spec covers button + prefab CRUD happy paths + 409.
- Phase 1 — Buttons list + detail + create.
- Phase 2 — Prefabs list + detail + create (with panel/button ref pickers).
- Phase 3 — Playwright coverage + cross-ref integrity checks.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T36.1 | _pending_ | _pending_ | Author `web/app/admin/catalog/buttons/page.tsx` RSC + `[id]/page.tsx` + `[id]/edit/page.tsx` Client island — list view with `composite_type` filter constrained to `button.*` slugs; detail + edit reuses `<DynamicFormFromSchema>` bound to button's `composite_type.props_schema`; PATCH with `updated_at` optimistic-lock; preview-diff panel. |
| T36.2 | _pending_ | _pending_ | Author `web/app/admin/catalog/buttons/new/page.tsx` Client island — two-step form (Step 1 pick `button.*` type; Step 2 schema-driven fields via `<DynamicFormFromSchema>`); POST transactional; redirect to detail on 201. Add sidebar link under Admin group. |
| T36.3 | _pending_ | _pending_ | Author `web/app/admin/catalog/prefabs/page.tsx` RSC + `[id]/page.tsx` + `[id]/edit/page.tsx` Client island — list view with `composite_type` filter constrained to `prefab.*` slugs; detail view resolves referenced `panel_id` + `button_ids[]` via joined fetch; edit form includes `<CatalogPanelPicker>` (combobox over `/api/catalog/panels`) + `<CatalogButtonPicker>` multi-select + inline layout hints JSON editor (monaco lite textarea with ajv validation against layout schema from composite type). PATCH with `updated_at`. |
| T36.4 | _pending_ | _pending_ | Author `web/app/admin/catalog/prefabs/new/page.tsx` Client island — two-step form (Step 1 pick `prefab.*` type; Step 2 schema-driven fields + `<CatalogPanelPicker>` + `<CatalogButtonPicker>` multi + layout JSON); POST transactional; redirect to detail on 201. |
| T36.5 | _pending_ | _pending_ | Author Playwright spec `web/tests/e2e/admin-catalog-buttons-prefabs.spec.ts` — covers: (a) buttons list + type filter (b) button create with schema-driven fields (c) prefab create with panel + button refs picked (d) prefab edit updates panel ref + layout JSON (e) 409 conflict replay on both surfaces. `npm run validate:web` + `npm run validate:e2e` green. |

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
