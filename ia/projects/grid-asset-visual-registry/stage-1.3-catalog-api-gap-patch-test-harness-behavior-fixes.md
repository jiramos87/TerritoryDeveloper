### Stage 1.3 — Catalog API gap-patch: test harness + behavior fixes

**Status:** Final

**Objectives:** Patch concrete gaps found in shipped `/api/catalog/*` routes (TECH-640..645 surface): build integration test harness; fix 6 behavior bugs; reconcile doc/refs.

**Exit:**

- Integration test suite green; all happy-path routes covered.
- 6 behavior gaps fixed; each with paired regression test.
- `ia/rules/web-backend-logic.md` updated (pagination + error contract + retire idempotency); JSDoc `@see` refs reconciled.
- `npm run validate:all` + `npm run validate:web` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T1.3.1 | Catalog API test harness + happy-path coverage | **TECH-755** | Done (archived) | Integration test infra for `/api/catalog/*`: DB setup/teardown per test, transactional rollback, HTTP helper, seed fixture. Happy-path tests: GET list published-default, GET by id joined shape snapshot, POST create 201, PATCH 200, retire 200, preview-diff 200 stable JSON. |
| T1.3.2 | Catalog API behavior gaps + doc/ref reconciliation | **TECH-756** | Done (archived) | Six bug fixes with paired regression tests: GET-by-id retired-asset 404 filter; POST slot uniqueness pre-check; PATCH unknown-field reject; PATCH no-op-body reject; preview-diff swap `throw e` → `catalogJsonError`; retire 409 on invalid `replaced_by`. Doc: pagination + error contract + retire idempotency in `web-backend-logic.md`; fix JSDoc `@see` refs in 4 route files. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
mechanicalization_score:
  anchors: ok
  picks: ok
  invariants: ok
  validators: ok
  escalation_enum: ok
  overall: fully_mechanical

# Decision Log — cardinality:
# Stage 1.3 has 2 phases × 1 task each (sub-≥2/phase rule). Justified:
# post-rewrite intentional grouping — T1.3.1 bundles harness+happy-path as one
# coherent unit; T1.3.2 bundles 6 small bug fixes + doc/ref reconciliation as
# one coherent follow-up. Splitting further creates artificial fragmentation
# (single-file test setup / single-PR bug batch). User confirmed Stage 1.3
# rewrite intent. Proceed with 2-task emission.

- reserved_id: "TECH-755"
  title: "Catalog API test harness + happy-path coverage"
  priority: high
  issue_type: "infrastructure / web"
  notes: |
    Build integration test harness for `/api/catalog/*` routes under `web/`. Per-test DB setup/teardown w/
    transactional rollback, HTTP test client helper, seed fixture matching Zone S seven-row shape.
    Happy-path coverage: `GET /api/catalog/assets` (published-default filter), `GET /api/catalog/assets/:id`
    (joined shape snapshot), `POST` create 201, `PATCH` 200, `POST /:id/retire` 200, `POST /preview-diff`
    200 with stable JSON ordering. Test runner wired into `npm run validate:web` or sibling script per repo
    pattern. No behavior changes to routes themselves — harness-only.
    Aligns grid-asset-visual-registry master plan Step 1 Stage 1.3 Phase 1.
  depends_on: []
  related:
    - TECH-626
    - TECH-627
    - TECH-628
  stub_body:
    summary: |
      Land reusable integration test harness for catalog API routes so Stage 1.3 Phase 2 bug fixes and
      future catalog route work ship with paired regression tests. Harness handles DB lifecycle, HTTP call
      ergonomics, and seed fixtures; happy-path suite locks shipped 200/201 responses as snapshots.
    goals: |
      1. Per-test DB isolation (transactional rollback or truncate strategy) — no cross-test leakage.
      2. HTTP helper covers GET/POST/PATCH with JSON body + status assertion; matches Next.js App Router handler signatures.
      3. Seed fixture produces deterministic seven Zone S rows + minimal sprite/economy bindings.
      4. Happy-path suite green: list, get-by-id joined, create, patch, retire, preview-diff.
      5. Test script wired so `npm run validate:web` (or documented sibling) picks it up.
    systems_map: |
      - New: `web/` test suite under agreed path (e.g. `web/tests/api/catalog/*.test.ts`) per existing test conventions.
      - Existing routes: `web/app/api/catalog/assets/route.ts`, `web/app/api/catalog/assets/[id]/route.ts`, `web/app/api/catalog/assets/[id]/retire/route.ts`, `web/app/api/catalog/preview-diff/route.ts`.
      - DTOs: `web/types/api/catalog*.ts` (from TECH-626/627/628 archived).
      - Migrations: `db/migrations/0011_catalog_core.sql`, `db/migrations/0012_catalog_spawn_pools.sql`.
      - Rule: `ia/rules/web-backend-logic.md` (consume as read; Phase 2 task TECH-756 edits it).
    impl_plan_sketch: |
      ### Phase 1 — Harness + happy-path
      - [ ] Pick test framework matching repo convention (vitest/jest) + discover existing web test setup if any.
      - [ ] Implement DB lifecycle helper (transactional rollback preferred; fallback truncate of catalog_* tables).
      - [ ] Implement HTTP call helper that wraps Next handlers or hits dev server deterministically.
      - [ ] Implement seed fixture loader (re-use Stage 1.1 seed SQL or inline factory).
      - [ ] Author six happy-path tests (list, get-by-id, create, patch, retire, preview-diff).
      - [ ] Wire npm script; document in `web/README.md` if new entry.
    open_questions: |
      - Framework choice: align with whatever `web/` already uses — confirm in first PR commit.
      - Fixture strategy: SQL seed vs TS factory — pick per repo consistency with existing DB tests.

- reserved_id: "TECH-756"
  title: "Catalog API behavior gaps + doc/ref reconciliation"
  priority: high
  issue_type: "bug / web"
  notes: |
    Six discrete bug fixes in shipped `/api/catalog/*` routes, each paired with regression test using TECH-755
    harness:
      1. `GET /assets/:id` — filter out retired assets (currently returns 200 for retired rows); return 404.
      2. `POST /assets` — pre-check slot uniqueness against `catalog_asset_sprite` to avoid 500 on dup.
      3. `PATCH /assets/:id` — reject unknown fields (strict body validation).
      4. `PATCH /assets/:id` — reject empty/no-op body (400 not 200 with no-op).
      5. `POST /preview-diff` — swap bare `throw e` for `catalogJsonError` helper (consistent error envelope).
      6. `POST /assets/:id/retire` — return 409 when `replaced_by` references non-existent or retired asset.
    Doc pass: extend `ia/rules/web-backend-logic.md` with pagination contract + error-response contract +
    retire idempotency semantics. Fix JSDoc `@see` refs in 4 route files pointing at stale spec sections.
    Depends on TECH-755 harness availability.
    Aligns grid-asset-visual-registry master plan Step 1 Stage 1.3 Phase 2.
  depends_on:
    - TECH-755
  related:
    - TECH-626
    - TECH-627
    - TECH-628
  stub_body:
    summary: |
      Fix six concrete behavior gaps in shipped catalog routes, each with paired regression test, then
      reconcile rule doc + JSDoc refs so route behavior is canonically documented.
    goals: |
      1. All six bugs fixed with matching regression test (Red → Green pattern).
      2. Error envelope consistent across all catalog routes (`catalogJsonError` helper, not raw throws).
      3. `ia/rules/web-backend-logic.md` updated w/ pagination + error contract + retire idempotency sections.
      4. JSDoc `@see` refs in four route files point at live spec sections (no 404 links).
      5. `npm run validate:all` + `npm run validate:web` green; full harness suite green.
    systems_map: |
      - Routes: `web/app/api/catalog/assets/route.ts`, `web/app/api/catalog/assets/[id]/route.ts`, `web/app/api/catalog/assets/[id]/retire/route.ts`, `web/app/api/catalog/preview-diff/route.ts`.
      - Helper: `web/lib/catalog/*` (error helper location per existing pattern).
      - DTOs: `web/types/api/catalog*.ts`.
      - Rule: `ia/rules/web-backend-logic.md` (edit target).
      - Harness: Phase 1 suite from TECH-755.
      - Migrations context: `db/migrations/0011_catalog_core.sql` (retired status + replaced_by).
    impl_plan_sketch: |
      ### Phase 1 — Bug fixes (6 × test-paired)
      - [ ] Bug 1: GET-by-id retired 404 — filter on `status`; regression test.
      - [ ] Bug 2: POST slot uniqueness pre-check — lookup + 409; regression test.
      - [ ] Bug 3: PATCH unknown-field reject — strict schema; regression test.
      - [ ] Bug 4: PATCH no-op-body reject — 400; regression test.
      - [ ] Bug 5: preview-diff `catalogJsonError` swap — consistent envelope; regression test.
      - [ ] Bug 6: retire 409 on invalid `replaced_by` — FK/existence check; regression test.
      ### Phase 2 — Doc/ref reconciliation
      - [ ] `web-backend-logic.md`: add pagination contract section.
      - [ ] `web-backend-logic.md`: add error-response envelope section.
      - [ ] `web-backend-logic.md`: add retire idempotency section.
      - [ ] Fix JSDoc `@see` in 4 route files (verify each link resolves).
      - [ ] Run `validate:all` + `validate:web`; attach logs to §Verification.
    open_questions: |
      - `catalogJsonError` helper location — confirm whether it exists already or needs extraction in Bug 5.
      - Pagination contract: does shipped list route already implement cursor vs offset? Doc the as-built.
```

#### §Plan Fix — PASS (no drift)

> plan-review recheck 2026-04-23 (Stage 1.3): TECH-755 / TECH-756 — §1/§2 vs task Intent; §7 phases; §8 acceptance; §Plan Digest; frontmatter `phases:`; cross-ref anchors; invariant compliance; glossary. Drift candidates: none.

#### §Stage Audit

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._
