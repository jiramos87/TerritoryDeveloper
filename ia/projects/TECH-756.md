---
purpose: "TECH-756 — Catalog API behavior gaps + doc/ref reconciliation."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T1.3.2"
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

Six concrete gaps in shipped `/api/catalog/*` routes (Stage 1.1–1.2):
1. `GET /assets/:id` returns 200 for retired rows (should 404).
2. `POST /assets` lacks slot uniqueness pre-check (can 500 on dup).
3. `PATCH /assets/:id` accepts unknown fields (should reject).
4. `PATCH /assets/:id` accepts empty body (should 400).
5. `POST /preview-diff` throws bare error (inconsistent envelope).
6. `POST /assets/:id/retire` lacks validation on `replaced_by` (should 409 on invalid).

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

_pending — populated by `/plan-digest {MASTER_PLAN_PATH} {STAGE_ID}`. Sub-sections: §Goal / §Acceptance / §Test Blueprint / §Examples / §Mechanical Steps (each step carries Goal / Edits / Gate / STOP / MCP hints). Template: `ia/templates/plan-digest-section.md`._

### §Goal

Fix six concrete behavior gaps in shipped catalog routes + reconcile rule doc + JSDoc refs.

### §Acceptance

- [ ] All six bugs fixed with matching regression test (Red → Green pattern).
- [ ] Error envelope consistent across all catalog routes.

### §Test Blueprint

TBD.

### §Examples

TBD.

### §Mechanical Steps

TBD.

## Open Questions

- `catalogJsonError` helper location — confirm whether it exists already or needs extraction in Bug 5.
- Pagination contract: does shipped list route already implement cursor vs offset? Doc the as-built.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
