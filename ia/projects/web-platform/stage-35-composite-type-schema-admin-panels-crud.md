### Stage 35 — Catalog composite authoring / Composite-type schema admin + panels CRUD


**Status:** Draft (tasks _pending_ — not yet filed; opens only when Stage 34 Final + grid-asset-visual-registry Step 5 Stage 5.2 Final — panels + buttons tables shipped)

**Objectives:** Ship (a) read-only composite-type listing at `/admin/catalog/types` (types are seeded via migration not via web UI at MVP; admin view is diagnostic only) and (b) full CRUD for panels at `/admin/catalog/panels` consuming `/api/catalog/panels` + `/api/catalog/composite-types`. Panel forms resolve the composite-type JSON schema dynamically via ajv and render per-type fields (L5).

**Exit:**

- `web/app/admin/catalog/types/page.tsx` RSC: list `catalog_composite_type` rows with `slug` + `props_schema` preview (collapsible JSON); read-only — no create/edit surface at MVP (seed-only).
- `web/app/admin/catalog/panels/page.tsx` RSC: list view with `composite_type` filter, status filter; wraps rows in `<DataTable>`; `<Rack>` + `<Breadcrumb>` Dashboard › Admin › Catalog › Panels.
- `web/app/admin/catalog/panels/[id]/page.tsx` RSC + Client island: joined panel + inline-buttons view; edit form rebuilds per-type props fields dynamically from `props_schema` (via `<DynamicFormFromSchema>` helper); PATCH with `updated_at` optimistic lock (L13).
- `web/app/admin/catalog/panels/new/page.tsx` Client island: two-step form (1) pick `composite_type` (2) fill schema-driven fields + buttons[]; POST transactional (panel + inline buttons in one call — matches Stage 5.2 transactional endpoint).
- `<DynamicFormFromSchema>` helper in `web/components/catalog/DynamicFormFromSchema.tsx`: consumes a JSON-schema object + initial values + onChange; renders primitives (string/number/boolean/enum/array-of-primitives); ajv-compiled client validator runs on submit; errors surface inline.
- Sidebar `LINKS` entries added: `Panels`, `Types (diagnostic)` under Admin group.
- `npm run validate:web` green; Playwright spec covers panel list + create (with schema-driven form) + edit + 409 conflict.
- Phase 1 — Composite-type diagnostic list + `<DynamicFormFromSchema>` helper.
- Phase 2 — Panels list + detail RSC + edit Client island.
- Phase 3 — Panels create (two-step) + playwright coverage.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T35.1 | _pending_ | _pending_ | Author `web/components/catalog/DynamicFormFromSchema.tsx` Client helper — props: `{ schema: JSONSchema7, value: Record<string, unknown>, onChange: (v) => void, errors?: Record<string, string[]> }`; recursively render fields: `string` → `<input type=text>`, `number`/`integer` → `<input type=number>`, `boolean` → checkbox, `enum` → `<select>`, `array` of primitives → chip list, `object` → nested group; compile ajv validator on schema change (memoized); emit validation errors to parent on submit. Unit test table: 6 shape variants (flat string, enum, numeric range, boolean, array-of-string, nested object). |
| T35.2 | _pending_ | _pending_ | Author `web/app/admin/catalog/types/page.tsx` RSC — fetch `GET /api/catalog/composite-types`; render `<DataTable>` with columns: `slug` (e.g. `button.fire`, `panel.weapon`), `kind` (button \| panel \| prefab), `updated_at`, collapsible `props_schema` preview (pretty-printed JSON via `<Surface tone="sunk">`); read-only — emit banner "Types are seeded via migration; contact ops to add a new type" (L5 per-type schema is infra-owned at MVP). Add sidebar link under Admin group. |
| T35.3 | _pending_ | _pending_ | Author `web/app/admin/catalog/panels/page.tsx` RSC — list view; `<DataTable>` columns: `slug`, `composite_type`, `buttons_count`, `status`, `updated_at`; filter bar: `composite_type` dropdown (fetched from `/api/catalog/composite-types`), status filter (`published` default). `<Rack>` frame + `<Breadcrumb>`; sidebar link. |
| T35.4 | _pending_ | _pending_ | Author `web/app/admin/catalog/panels/[id]/page.tsx` RSC + `[id]/edit/page.tsx` Client island — detail view lists joined panel + inline buttons (rendered via `<DataTable>` sub-section); edit form uses `<DynamicFormFromSchema>` bound to the resolved `composite_type.props_schema`; buttons[] editor is a repeatable sub-form (add / remove / reorder); PATCH with `updated_at`; 409 → conflict UI (keep local / reload server); preview-diff button calls `POST /api/catalog/panels/:id/preview-diff` before commit. |
| T35.5 | _pending_ | _pending_ | Author `web/app/admin/catalog/panels/new/page.tsx` Client island — two-step form: (Step 1) pick `composite_type` from combobox → loads `props_schema` via `/api/catalog/composite-types/:slug`; (Step 2) render `<DynamicFormFromSchema>` + inline buttons[] repeatable sub-form; submit POST transactional (panel + buttons in one call — matches Stage 5.2 endpoint); redirect to detail on 201. |
| T35.6 | _pending_ | _pending_ | Author Playwright spec `web/tests/e2e/admin-catalog-panels.spec.ts` — covers: (a) types diagnostic list renders with schema preview (b) panels list with type filter (c) create panel: step-1 pick type, step-2 schema-driven form + add 2 buttons, submit (d) edit panel: field-level validation errors surface from ajv (e) 409 conflict replay on edit. `npm run validate:web` + `npm run validate:e2e` green. |

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
