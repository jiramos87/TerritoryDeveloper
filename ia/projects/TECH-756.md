---
purpose: "TECH-756 — Catalog API behavior gaps + doc/ref reconciliation."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T1.3.2"
phases:
  - "Phase 1 — Bug fixes (6 × test-paired)"
  - "Phase 2 — Doc/ref reconciliation"
---
# TECH-756 — Catalog API behavior gaps + doc/ref reconciliation

> **Issue:** [TECH-756](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Fix six concrete behavior gaps in shipped catalog routes, each with paired regression test, then
reconcile rule doc + JSDoc refs so route behavior is canonically documented.

## 2. Goals and Non-Goals

### 2.1 Goals

1. All six bugs fixed with matching regression test (Red → Green pattern).
2. Error envelope consistent across all catalog routes (`catalogJsonError` helper, not raw throws).
3. `ia/rules/web-backend-logic.md` updated w/ pagination + error contract + retire idempotency sections.
4. JSDoc `@see` refs in four route files point at live spec sections (no 404 links).
5. `npm run validate:all` + `npm run validate:web` green; full harness suite green.

### 2.2 Non-Goals (Out of Scope)

1. New catalog route features — Phase 2 improvements deferred to future Stage.
2. Performance optimization — focused on behavior correctness + doc consistency.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Want correct behavior (404 on retired, 409 on invalid retire) | Six bug fixes verified by regression tests |
| 2 | QA | Want canonical doc of pagination + error + retire contracts | `web-backend-logic.md` sections added + JSDoc refs reconciled |

## 4. Current State

### 4.1 Domain behavior

Seven concrete gaps in shipped `/api/catalog/*` routes (Stage 1.1–1.2):
1. `GET /assets/:id` returns 200 for retired rows (should 404).
2. `POST /assets` lacks slot uniqueness pre-check (can 500 on dup).
3. `PATCH /assets/:id` accepts unknown fields (should reject).
4. `PATCH /assets/:id` accepts empty body (should 400).
5. `POST /preview-diff` throws bare error (inconsistent envelope).
6. `POST /assets/:id/retire` lacks validation on `replaced_by` (should 409 on invalid).
7. `PATCH /assets/:id` returns stale composite in 200 body — post-UPDATE re-read runs outside the enclosing txn (discovered during TECH-755 Pass 1, 2026-04-23).

JSDoc `@see` refs in four route files point at stale spec sections.

### 4.2 Systems map

- Routes: `web/app/api/catalog/assets/route.ts`, `web/app/api/catalog/assets/[id]/route.ts`, `web/app/api/catalog/assets/[id]/retire/route.ts`, `web/app/api/catalog/preview-diff/route.ts`.
- Helper: `web/lib/catalog/*` (error helper location per existing pattern).
- DTOs: `web/types/api/catalog*.ts`.
- Rule: `ia/rules/web-backend-logic.md` (edit target).
- Harness: Phase 1 suite from TECH-755.
- Migrations context: `db/migrations/0011_catalog_core.sql` (retired status + replaced_by).

### 4.3 Implementation investigation notes

Bug 1–4, 6 require route handler edits (validation, filtering, error handling). Bug 5 may require helper extraction or use of existing `catalogJsonError`. Depends on Phase 1 harness (TECH-755) for regression tests.

## 5. Proposed Design

### 5.1 Target behavior (product)

Route behavior after fixes:
1. `GET /assets/:id` — filter `WHERE status != 'retired'`; return 404 if not found.
2. `POST /assets` — pre-check slot uniqueness against `catalog_asset_sprite`; return 409 if dup.
3. `PATCH /assets/:id` — strict schema validation; reject unknown fields.
4. `PATCH /assets/:id` — reject empty or no-op body; return 400.
5. `POST /preview-diff` — consistent error envelope via `catalogJsonError` helper.
6. `POST /assets/:id/retire` — validate `replaced_by` existence + non-retired status; return 409 if invalid.
7. `PATCH /assets/:id` — return post-UPDATE composite (not stale pre-UPDATE row). Fix inside `patchCatalogAsset`: build composite inline from `tx` (not via `loadCatalogAssetById(idParam)` which opens a fresh pool connection outside the txn).

Error envelope consistent: all routes wrap errors in `catalogJsonError` helper.

### 5.2 Architecture / implementation

Per-bug approach:
- Bug 1: Add `status` filter to get-by-id handler.
- Bug 2: Add uniqueness check + 409 response.
- Bug 3–4: Strengthen PATCH validation (strict schema + non-empty body).
- Bug 5: Extract or use existing `catalogJsonError` helper.
- Bug 6: Add FK + status validation on `replaced_by`.

Doc updates:
- `web-backend-logic.md`: add pagination contract, error-response contract, retire idempotency sections.
- Four route files: fix JSDoc `@see` refs to point at live spec sections.

### 5.3 Method / algorithm notes

Bug 1: `SELECT ... WHERE id = ? AND status != 'retired'`.

Bug 2: `SELECT COUNT(*) FROM catalog_asset_sprite WHERE slot = ? AND asset_id != ?` (pre-check).

Bug 3–4: Zod schema with `.strict()` + `.min(1)` for patch body.

Bug 5: Confirm `catalogJsonError` helper signature; wrap errors consistently.

Bug 6: `SELECT id, status FROM catalog_asset WHERE id = ?` → if missing or status = 'retired', return 409.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-23 | Defer `catalogJsonError` helper location | Implementing agent confirms existence + location in first commit | Extract new helper vs use existing |
| 2026-04-23 | Confirm pagination contract as-built | List route may use cursor vs offset; doc as-built | Implement new contract vs doc shipped behavior |

## 7. Implementation Plan

### Phase 1 — Bug fixes (6 × test-paired)

- [ ] Bug 1: GET-by-id retired 404 — filter on `status`; add regression test.
- [ ] Bug 2: POST slot uniqueness pre-check — lookup + 409; add regression test.
- [ ] Bug 3: PATCH unknown-field reject — strict schema; add regression test.
- [ ] Bug 4: PATCH no-op-body reject — 400; add regression test.
- [ ] Bug 5: preview-diff `catalogJsonError` swap — consistent envelope; add regression test.
- [ ] Bug 6: retire 409 on invalid `replaced_by` — FK/existence check; add regression test.
- [ ] Bug 7: PATCH composite inline build from `tx` (no post-UPDATE out-of-txn reread); regression test asserts `response.asset.display_name === "patched"` directly (no GET round-trip).

### Phase 2 — Doc/ref reconciliation

- [ ] `web-backend-logic.md`: add pagination contract section.
- [ ] `web-backend-logic.md`: add error-response envelope section.
- [ ] `web-backend-logic.md`: add retire idempotency section.
- [ ] Fix JSDoc `@see` in 4 route files (verify each link resolves).
- [ ] Run `validate:all` + `validate:web`; attach logs to §Verification.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| All six bugs fixed with matching regression test | Node | `npm run validate:web` + TECH-755 harness | Red → Green pattern; six tests all passing |
| Rule doc updated; JSDoc refs reconciled | Validation | `npm run validate:all` | `web-backend-logic.md` updated; JSDoc links verified |

## 8. Acceptance Criteria

- [ ] All six bugs fixed with matching regression test (Red → Green pattern).
- [ ] Error envelope consistent across all catalog routes (`catalogJsonError` helper, not raw throws).
- [ ] `ia/rules/web-backend-logic.md` updated w/ pagination + error contract + retire idempotency sections.
- [ ] JSDoc `@see` refs in four route files point at live spec sections (no 404 links).
- [ ] `npm run validate:all` + `npm run validate:web` green; full harness suite green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| TBD | … | … | … |

## 10. Lessons Learned

- …

## §Plan Digest

```yaml
mechanicalization_score:
  anchors: ok
  picks: ok
  invariants: ok
  validators: ok
  escalation_enum: ok
  overall: fully_mechanical
```

### §Goal

Fix six concrete behavior gaps in shipped `/api/catalog/*` routes (each paired with a regression vitest spec via TECH-755 harness), then extend `ia/rules/web-backend-logic.md` with three new `##` sections (Pagination contract / Error-response envelope / Retire idempotency) and retarget JSDoc `@see` refs in four route files from archived project specs to live rule anchors.

### §Acceptance

- [ ] Bug 1: `loadCatalogAssetById` in `web/lib/catalog/fetch-asset-composite.ts` filters `status != 'retired'`; `GET /api/catalog/assets/:id` returns 404 for retired rows.
- [ ] Bug 2: Duplicate sprite_bind slot on `POST /assets` returns 409 `unique_violation` via existing `responseFromPostgresError` path (relies on DB unique index, not new pre-check).
- [ ] Bug 3: `PATCH /assets/:id` rejects unknown body fields with 400 `bad_request` + `details.unknown_fields` listing offending keys.
- [ ] Bug 4: `PATCH /assets/:id` with only `{ updated_at }` returns 400 with message "PATCH body must include at least one field to update" (narrower than current generic message).
- [ ] Bug 5: `web/app/api/catalog/preview-diff/route.ts:37` swaps bare `throw e` for `responseFromPostgresError(e, "Preview diff failed")`.
- [ ] Bug 6: `web/app/api/catalog/assets/[id]/retire/route.ts` returns 409 `conflict` (not 404) when `replaced_by` id missing OR points at a retired asset.
- [ ] 7 new vitest cases in `web/tests/api/catalog/` (one per bug; Bug 6 contributes 2 — missing + retired).
- [ ] `ia/rules/web-backend-logic.md` has three new `##` sections: `## Pagination contract`, `## Error-response envelope`, `## Retire idempotency`.
- [ ] All four route files (`assets/route.ts`, `assets/[id]/route.ts`, `assets/[id]/retire/route.ts`, `preview-diff/route.ts`) have JSDoc `@see` refs retargeted from `ia/backlog-archive/TECH-640.yaml` … `TECH-645.md` to `ia/rules/web-backend-logic.md` section anchors.
- [ ] `npm run validate:all` + `npm run validate:web` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `catalog_get_by_id_retired_returns_404` | seed retired asset; `GET /api/catalog/assets/{retired_id}` | status 404; `body.code === 'not_found'` | vitest (TECH-755 harness) |
| `catalog_post_duplicate_slot_returns_409` | `POST /api/catalog/assets` body w/ two sprite_binds same `slot` | status 409; `body.code === 'unique_violation'` | vitest |
| `catalog_patch_unknown_field_returns_400` | `PATCH /api/catalog/assets/{id}` w/ `{ updated_at, bogus: 1 }` | status 400; `body.code === 'bad_request'`; `body.details.unknown_fields` contains `"bogus"` | vitest |
| `catalog_patch_noop_body_returns_400_narrow_msg` | `PATCH /api/catalog/assets/{id}` w/ only `{ updated_at }` | status 400; `body.error` contains `"at least one field"` | vitest |
| `catalog_preview_diff_runtime_error_returns_500_envelope` | mock `computeCatalogAssetPreview` throwing generic `Error` | status 500; `body.code === 'internal'`; no raw stack in JSON | vitest |
| `catalog_retire_missing_replaced_by_returns_409` | `POST /api/catalog/assets/{id}/retire` w/ `{ replaced_by: 99999 }` | status 409; `body.code === 'conflict'` | vitest |
| `catalog_retire_retired_replaced_by_returns_409` | seed retired asset B; `POST /assets/{A}/retire` w/ `{ replaced_by: B.id }` | status 409; `body.code === 'conflict'`; `body.error` mentions "retired" | vitest |
| `web_backend_logic_rule_has_three_new_sections` | grep `ia/rules/web-backend-logic.md` | all three `## Pagination contract`, `## Error-response envelope`, `## Retire idempotency` present | node (grep) |
| `route_jsdoc_refs_point_at_rule_anchors` | grep `@see` from 4 route files | each `@see` targets `ia/rules/web-backend-logic.md#...`, zero archived-spec refs | node (grep) |

### §Examples

| Input | Expected output | Bug |
|-------|-----------------|-----|
| `GET /api/catalog/assets/{retired_id}` | `404` + `{ error, code: 'not_found' }` | 1 |
| `POST /assets` w/ duplicate `sprite_bind.slot` | `409` + `{ error, code: 'unique_violation' }` | 2 |
| `PATCH /assets/{id}` w/ `{ updated_at, unknown_field: "x" }` | `400` + `{ error, code: 'bad_request', details: { unknown_fields: ["unknown_field"] } }` | 3 |
| `PATCH /assets/{id}` w/ only `{ updated_at }` | `400` + `{ error: "PATCH body must include at least one field to update", code: 'bad_request' }` | 4 |
| `POST /preview-diff` that hits non-DATABASE_URL runtime error | `500` + `{ error, code: 'internal' }` (via `responseFromPostgresError`) | 5 |
| `POST /assets/{id}/retire` w/ `{ replaced_by: 99999 }` | `409` + `{ error, code: 'conflict' }` | 6 |
| `POST /assets/{id}/retire` w/ `{ replaced_by: {retired_id} }` | `409` + `{ error, code: 'conflict' }` w/ `error` mentioning "retired" | 6 |

### §Mechanical Steps

#### Step 1 — Bug 1: filter retired in `loadCatalogAssetById`

**Goal:** Add `status != 'retired'` to the primary asset SELECT so `GET /api/catalog/assets/:id` returns 404 for retired rows. Keep existing "notfound" path as-is (now covers retired too).

**Edits:**
- `web/lib/catalog/fetch-asset-composite.ts` — **before**:
  ```
  const ar = await sql`select * from catalog_asset where id = ${idNum} limit 1`;
  ```
  **after**:
  ```
  const ar = await sql`select * from catalog_asset where id = ${idNum} and status != 'retired' limit 1`;
  ```

**Gate:**
```bash
cd web && npx vitest run -t "catalog_get_by_id_retired_returns_404"
```
Expected: exit 0; test green.

**STOP:** If happy-path `catalog_get_by_id_returns_joined_snapshot` (TECH-755) turns red due to seed drift → revert this edit, re-verify seed has a published row, then re-apply. Do NOT move the filter into `[id]/route.ts` handler — `loadCatalogAssetById` is the one consumer of this SELECT and also feeds `preview-diff` which must also exclude retired.

**MCP hints:** `plan_digest_resolve_anchor` (confirm anchor is still unique after edit), `glossary_lookup` (`catalog asset`).

#### Step 2 — Bug 2: verify DB unique constraint covers duplicate slot

**Goal:** Confirm `catalog_asset_sprite` has a unique index on `(asset_id, slot)` in `0011_catalog_core.sql` so existing `responseFromPostgresError` path converts `23505` → 409 `unique_violation`. No handler edit if constraint exists.

**Edits:**
- none — constraint verification + regression test only.

**Gate:**
```bash
cd web && npx vitest run -t "catalog_post_duplicate_slot_returns_409"
```
Expected: vitest exits 0; regression test green via existing DB unique constraint.

**STOP:** If vitest test fails with 500 or missing-constraint symptom → grep `0011_catalog_core.sql` under `db/migrations/` for `unique.*catalog_asset_sprite`; if constraint missing, file a follow-up migration in a separate commit adding `unique (asset_id, slot)`, re-run gate. Do NOT add a handler pre-`SELECT` (race window, duplicates DB uniqueness).

**MCP hints:** `plan_digest_verify_paths` (migration path under `db/migrations/`), `glossary_lookup` (`slot uniqueness`).

#### Step 3 — Bug 3+4: strict-schema + narrow no-op msg in `patch-asset.ts`

**Goal:** Reject unknown PATCH body fields (Bug 3) with 400 `bad_request` + `details.unknown_fields`; narrow no-op-body error to `"PATCH body must include at least one field to update"` (Bug 4). Land via shared strict-schema allowlist inside `patchCatalogAsset`.

**Edits:**
- `web/lib/catalog/patch-asset.ts` — **before**:
  ```
  const { updated_at: _v, ...rest } = body;
  if (Object.keys(rest).length === 0) {
    return { ok: "badid" };
  }
  ```
  **after**:
  ```
  const ALLOWED_PATCH_KEYS = new Set([
    "updated_at",
    "display_name",
    "status",
    "replaced_by",
    "footprint_w",
    "footprint_h",
    "placement_mode",
    "unlocks_after",
    "has_button",
    "economy",
  ]);
  const unknownFields = Object.keys(body).filter((k) => !ALLOWED_PATCH_KEYS.has(k));
  if (unknownFields.length > 0) {
    return { ok: "unknown_fields", unknownFields };
  }
  const { updated_at: _v, ...rest } = body;
  if (Object.keys(rest).length === 0) {
    return { ok: "no_fields" };
  }
  ```
- `web/lib/catalog/patch-asset.ts` (PatchOutcome type) — **before**:
  ```
  type PatchOutcome =
    | PatchOk
    | { ok: "notfound" }
    | { ok: "badid" }
    | {
        ok: "conflict";
        current: NonNullable<Exclude<Awaited<ReturnType<typeof loadCatalogAssetById>>, "notfound" | "badid">>;
      };
  ```
  **after**:
  ```
  type PatchOutcome =
    | PatchOk
    | { ok: "notfound" }
    | { ok: "badid" }
    | { ok: "no_fields" }
    | { ok: "unknown_fields"; unknownFields: string[] }
    | {
        ok: "conflict";
        current: NonNullable<Exclude<Awaited<ReturnType<typeof loadCatalogAssetById>>, "notfound" | "badid">>;
      };
  ```
- `web/app/api/catalog/assets/[id]/route.ts` — **before**:
  ```
      if (out.ok === "badid") {
        return catalogJsonError(400, "bad_request", "Invalid id or body (need updated_at + one field to patch)");
      }
  ```
  **after**:
  ```
      if (out.ok === "badid") {
        return catalogJsonError(400, "bad_request", "Invalid id or updated_at (need valid numeric id + ISO updated_at)");
      }
      if (out.ok === "unknown_fields") {
        return catalogJsonError(400, "bad_request", "PATCH body contains unknown fields", {
          details: { unknown_fields: out.unknownFields },
        });
      }
      if (out.ok === "no_fields") {
        return catalogJsonError(400, "bad_request", "PATCH body must include at least one field to update");
      }
  ```

**Gate:**
```bash
cd web && npx vitest run -t "catalog_patch_unknown_field_returns_400|catalog_patch_noop_body_returns_400_narrow_msg"
```
Expected: exit 0; both tests green.

**STOP:** If TECH-755 `catalog_patch_returns_200_composite` turns red → check that happy-path body includes only allowlisted keys (`updated_at` + `display_name`) and retry. Do NOT widen `ALLOWED_PATCH_KEYS` beyond the 10 fields in `CatalogPatchAssetBody`. If `economy` sub-object needs its own strict-schema pass → defer to follow-up issue; scope Bug 3 to top-level keys only.

**MCP hints:** `plan_digest_resolve_anchor` (verify `"Invalid id or body"` anchor remains unique), `glossary_lookup` (`optimistic lock`).

#### Step 4 — Bug 5: preview-diff envelope swap

**Goal:** Replace bare `throw e` with `responseFromPostgresError` call for consistent error envelope.

**Edits:**
- `web/app/api/catalog/preview-diff/route.ts` — **before**:
  ```
      throw e;
  ```
  **after**:
  ```
      return responseFromPostgresError(e, "Preview diff failed");
  ```
- `web/app/api/catalog/preview-diff/route.ts` (import line) — **before**:
  ```
  import { catalogJsonError } from "@/lib/catalog/catalog-api-errors";
  ```
  **after**:
  ```
  import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
  ```

**Gate:**
```bash
cd web && npx vitest run -t "catalog_preview_diff_runtime_error_returns_500_envelope"
```
Expected: exit 0; test green; response `body.code === 'internal'`.

**STOP:** If `responseFromPostgresError` import typechecks red → verify export list in `web/lib/catalog/catalog-api-errors.ts:56` (`export function responseFromPostgresError`). If still red → revert import addition + re-open step. Do NOT catch the error inside `computeCatalogAssetPreview` — envelope belongs at the handler boundary.

**MCP hints:** `plan_digest_resolve_anchor` (`throw e;` anchor — confirm exactly 1 hit before edit), `glossary_lookup` (`error envelope`).

#### Step 5 — Bug 6: retire 409 on invalid `replaced_by`

**Goal:** Change retire handler to return 409 `conflict` (not 404) when `replaced_by` id missing OR points at a retired asset. Keep 404 reserved for the primary retire target (`id` not found).

**Edits:**
- `web/app/api/catalog/assets/[id]/retire/route.ts` — **before**:
  ```
      if (rep != null) {
        const ex = await sql`select 1 from catalog_asset where id = ${rep} limit 1`;
        if (ex.length === 0) {
          return catalogJsonError(404, "not_found", "replaced_by asset not found");
        }
      }
  ```
  **after**:
  ```
      if (rep != null) {
        const ex = await sql`select status from catalog_asset where id = ${rep} limit 1`;
        if (ex.length === 0) {
          return catalogJsonError(409, "conflict", "replaced_by asset not found");
        }
        if ((ex[0] as { status: string }).status === "retired") {
          return catalogJsonError(409, "conflict", "replaced_by asset is retired");
        }
      }
  ```

**Gate:**
```bash
cd web && npx vitest run -t "catalog_retire_missing_replaced_by_returns_409|catalog_retire_retired_replaced_by_returns_409"
```
Expected: exit 0; both tests green.

**STOP:** If TECH-755 `catalog_retire_returns_200_with_null_replaced_by` turns red → the `if (rep != null)` branch must still short-circuit for `replaced_by == null`; verify by re-reading surrounding lines at `retire/route.ts:37-42`. Do NOT change the 404 path on primary asset not-found (line 50 inside the update branch).

**MCP hints:** `plan_digest_resolve_anchor` (anchor on full 4-line before block — confirm unique hit).

#### Step 6 — Rule doc: three new sections in `web-backend-logic.md`

**Goal:** Append three canonical sections documenting shipped backend contracts (pagination / error envelope / retire idempotency) just above `## Relation to other rules` so JSDoc `@see` refs land on stable anchors.

**Edits:**
- `ia/rules/web-backend-logic.md` — **before**:
  ```
  ## Relation to other rules
  ```
  **after**:
  ```
  ## Pagination contract

  `GET /api/catalog/assets` uses keyset (cursor) pagination on `bigserial id`, ascending:

  - Query params: `limit` (default 200, range 1–500), `cursor` (numeric id string; scans `id > cursor`).
  - Response: `{ assets: CatalogAsset[], next_cursor: string | null, limit: number }`.
  - `next_cursor` is the last row id when `assets.length === limit`, else `null` (exhausted).
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
  - `replaced_by` missing id → 409 `conflict` (not 404 — 404 reserved for the primary `:id`).
  - `replaced_by` references a retired asset → 409 `conflict`.
  - `replaced_by === :id` (self) → 400 `bad_request`.
  - Re-retiring an already-retired asset returns 200 with the current composite (idempotent).

  ## Relation to other rules
  ```

**Gate:**
```bash
cd web && npx vitest run -t "web_backend_logic_rule_has_three_new_sections"
```
Expected: exit 0; rule-doc guard test green — three new H2 headings present.

**STOP:** If guard test fails → re-open this step and re-apply the insertion at the anchor. Do NOT re-order existing sections; append only.

**MCP hints:** `plan_digest_resolve_anchor` (`## Relation to other rules` must stay unique), `glossary_lookup` (`retire idempotency`, `error envelope`).

#### Step 7 — JSDoc `@see` ref retargeting (4 route files)

**Goal:** Retarget `@see` refs from archived `ia/backlog-archive/TECH-640.yaml` … `TECH-645.md` to live rule anchors in `ia/rules/web-backend-logic.md` (landed in Step 6).

**Edits:**
- `web/app/api/catalog/assets/route.ts` — **before**:
  ```
  /**
   * @see `ia/backlog-archive/TECH-640.yaml` — `GET /api/catalog/assets`
   */
  ```
  **after**:
  ```
  /**
   * @see `ia/rules/web-backend-logic.md#pagination-contract` — `GET /api/catalog/assets`
   */
  ```
- `web/app/api/catalog/assets/route.ts` — **before**:
  ```
  /**
   * @see `ia/backlog-archive/TECH-643.yaml` — `POST /api/catalog/assets`
   */
  ```
  **after**:
  ```
  /**
   * @see `ia/rules/web-backend-logic.md#error-response-envelope` — `POST /api/catalog/assets`
   */
  ```
- `web/app/api/catalog/assets/[id]/route.ts` — **before**:
  ```
  /**
   * @see `ia/backlog-archive/TECH-641.yaml` — `GET /api/catalog/assets/:id`
   */
  ```
  **after**:
  ```
  /**
   * @see `ia/rules/web-backend-logic.md#error-response-envelope` — `GET /api/catalog/assets/:id`
   */
  ```
- `web/app/api/catalog/assets/[id]/route.ts` — **before**:
  ```
  /**
   * @see `ia/backlog-archive/TECH-644.yaml` — `PATCH /api/catalog/assets/:id`
   */
  ```
  **after**:
  ```
  /**
   * @see `ia/rules/web-backend-logic.md#error-response-envelope` — `PATCH /api/catalog/assets/:id`
   */
  ```
- `web/app/api/catalog/assets/[id]/retire/route.ts` — **before**:
  ```
  /**
   * @see `ia/backlog-archive/TECH-645.yaml` — `POST /api/catalog/assets/:id/retire`
   */
  ```
  **after**:
  ```
  /**
   * @see `ia/rules/web-backend-logic.md#retire-idempotency` — `POST /api/catalog/assets/:id/retire`
   */
  ```
- `web/app/api/catalog/preview-diff/route.ts` — **before**:
  ```
  /**
   * @see `ia/backlog-archive/TECH-645.yaml` — `POST /api/catalog/preview-diff` (read-only; no `INSERT`/`UPDATE`).
   */
  ```
  **after**:
  ```
  /**
   * @see `ia/rules/web-backend-logic.md#error-response-envelope` — `POST /api/catalog/preview-diff` (read-only; no `INSERT`/`UPDATE`).
   */
  ```

**Gate:**
```bash
cd web && npx vitest run -t "route_jsdoc_refs_point_at_rule_anchors"
```
Expected: exit 0; JSDoc-ref guard test green — zero archived-spec refs, six rule-anchor refs.

**STOP:** If guard test fails → revisit the before/after blocks in this step; re-apply. Do NOT leave a dual-citation (`@see A; @see B`) — rule anchor is the canonical surface now.

**MCP hints:** `plan_digest_resolve_anchor` (confirm each before block unique), `glossary_lookup` (`error envelope`).

#### Step 8 — Final validator sweep

**Goal:** Confirm both root validators exit 0 after Steps 1–7 land.

**Edits:** none.

**Gate:**
```bash
npm run validate:all && npm run validate:web
```
Expected: both exit 0; vitest suite reports 7 new Bug-fix cases + 8 TECH-755 happy-path cases = 15 passing (or more if harness self-test added).

**STOP:** If `validate:all` fails on `validate:frontmatter` due to rule-doc changes → verify `ia/rules/web-backend-logic.md` frontmatter unchanged (still `loaded_by: on-demand`). If `validate:web` fails on typecheck after Step 3 (`PatchOutcome` widening) → re-open Step 3, confirm new variants destructured in the handler switch at `assets/[id]/route.ts`. Do NOT suppress lint errors with `// eslint-disable`.

**MCP hints:** `backlog_issue` (`TECH-756` for scope), `plan_digest_verify_paths` (root `package.json`).

## Open Questions

- `catalogJsonError` helper location — confirm whether it exists already or needs extraction in Bug 5. **Resolved during author pass:** lives at `web/lib/catalog/catalog-api-errors.ts` (exports `catalogJsonError` + `responseFromPostgresError`). Bug 5 swap: replace `throw e` w/ `responseFromPostgresError(e, "Preview diff failed")`.
- Pagination contract: does shipped list route already implement cursor vs offset? Doc the as-built. **Resolved during author pass:** **cursor** keyset pagination on `bigserial id`, `limit ∈ [1, 500]`, default 200 — see `parse-list-query.ts` + `assets/route.ts:40-56`. Doc as-built in rule.
- Bug 6 `replaced_by` cycle: should pointing at an already-retired asset be 409? Author pass proposed yes — confirm w/ product in first PR commit.
- Bug 2 pre-check scope: DB unique constraint on `catalog_asset_sprite(asset_id, slot)` already yields 23505 → 409. Should the Task add a handler-side pre-check anyway for a friendlier error message, or rely on DB path? Confirm in PR.
- Glossary candidates (surface terms not yet in `ia/specs/glossary.md`; author-pass flagged — do NOT edit glossary here): `catalog asset`, `optimistic lock`, `error envelope`, `retire idempotency`, `preview-diff`, `replaced_by`, `slot uniqueness`.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
