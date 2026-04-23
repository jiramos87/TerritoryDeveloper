---
purpose: "TECH-755 — Catalog API test harness + happy-path coverage."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T1.3.1"
---
# TECH-755 — Catalog API test harness + happy-path coverage

> **Issue:** [TECH-755](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Land reusable integration test harness for catalog API routes so Stage 1.3 Phase 2 bug fixes and
future catalog route work ship with paired regression tests. Harness handles DB lifecycle, HTTP call
ergonomics, and seed fixtures; happy-path suite locks shipped 200/201 responses as snapshots.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Per-test DB isolation (transactional rollback or truncate strategy) — no cross-test leakage.
2. HTTP helper covers GET/POST/PATCH with JSON body + status assertion; matches Next.js App Router handler signatures.
3. Seed fixture produces deterministic seven Zone S rows + minimal sprite/economy bindings.
4. Happy-path suite green: list, get-by-id joined, create, patch, retire, preview-diff.
5. Test script wired so `npm run validate:web` (or documented sibling) picks it up.

### 2.2 Non-Goals (Out of Scope)

1. Behavior fixes — Phase 2 task (TECH-756).
2. MCP tool tests — covered elsewhere (Stage 1.4).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Want reusable test harness for catalog API routes | Harness supports per-test DB isolation, HTTP call semantics, seed fixtures; happy-path suite green |
| 2 | QA | Want regression tests locked to shipped 200/201 responses | All six happy-path routes have snapshot tests wired to `validate:web` |

## 4. Current State

### 4.1 Domain behavior

Shipped `/api/catalog/*` routes (Stage 1.1–1.2: TECH-626–TECH-629, TECH-640–TECH-645) lack integration test coverage and harness. Phase 2 bug fixes (TECH-756) depend on test infra.

### 4.2 Systems map

- New: `web/` test suite under agreed path (e.g. `web/tests/api/catalog/*.test.ts`) per existing test conventions.
- Existing routes: `web/app/api/catalog/assets/route.ts`, `web/app/api/catalog/assets/[id]/route.ts`, `web/app/api/catalog/assets/[id]/retire/route.ts`, `web/app/api/catalog/preview-diff/route.ts`.
- DTOs: `web/types/api/catalog*.ts` (from TECH-626/627/628 archived).
- Migrations: `db/migrations/0011_catalog_core.sql`, `db/migrations/0012_catalog_spawn_pools.sql`.
- Rule: `ia/rules/web-backend-logic.md` (consume as read; Phase 2 task TECH-756 edits it).

### 4.3 Implementation investigation notes

Test framework alignment required (vitest/jest per repo convention). DB isolation strategy confirmed (transactional rollback preferred, truncate fallback). Seed fixture can reuse Stage 1.1 catalog seed SQL or inline TS factory.

## 5. Proposed Design

### 5.1 Target behavior (product)

Integration test harness provides:
- Per-test DB isolation preventing cross-test leakage.
- HTTP helper ergonomic for GET/POST/PATCH with assertion flow.
- Seed fixture producing deterministic seven Zone S rows + minimal sprite/economy bindings.
- Happy-path tests covering list (published-default filter), get-by-id (joined snapshot), create 201, patch 200, retire 200, preview-diff 200.

### 5.2 Architecture / implementation

Test suite structure: `web/tests/api/catalog/` directory housing harness modules + test files.

Harness modules:
- DB lifecycle helper (transactional rollback or truncate per scheme).
- HTTP call wrapper (matches Next.js App Router handler signatures).
- Seed fixture loader (deterministic Zone S asset rows).

Happy-path test file: `assets.test.ts` covering six routes (list, get-by-id, create, patch, retire, preview-diff).

### 5.3 Method / algorithm notes

DB lifecycle: wrap each test in transaction → rollback on teardown (PostgreSQL recommended), fallback truncate catalog tables.

HTTP helper: wrap Next handlers or dev server deterministically; support GET/POST/PATCH body + status assertion.

Seed fixture: reuse or inline Stage 1.1 catalog seed SQL (seven Zone S asset rows + sprite/economy bindings).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-23 | Defer framework + fixture strategy decision | Implementing agent confirms repo test convention in first commit | vitest vs jest; SQL seed vs TS factory |

## 7. Implementation Plan

### Phase 1 — DB setup + HTTP helper

- [ ] Pick test framework matching repo convention (vitest/jest).
- [ ] Implement DB lifecycle helper (transactional rollback preferred; fallback truncate of catalog_* tables).
- [ ] Implement HTTP call helper that wraps Next handlers or hits dev server deterministically.
- [ ] Document test runner integration (e.g. `npm run validate:web`).

### Phase 2 — Seed fixture + happy-path tests

- [ ] Implement seed fixture loader (re-use Stage 1.1 seed SQL or inline factory).
- [ ] Author six happy-path tests (list, get-by-id, create, patch, retire, preview-diff).
- [ ] Wire npm script; document in `web/README.md` if new entry.
- [ ] Verify `npm run validate:web` picks up suite.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Integration test suite green; all happy-path routes covered | Node | `npm run validate:web` | Harness + six happy-path tests; all 200/201 responses locked as snapshots |
| Test runner wired per repo convention | Node | `npm run validate:web` (or documented sibling) | Confirm in first commit message |

## 8. Acceptance Criteria

- [ ] Integration test harness green; all happy-path routes covered.
- [ ] Per-test DB isolation verified (no cross-test leakage).
- [ ] `npm run validate:web` picks up catalog test suite and passes.
- [ ] Six happy-path tests all green (list, get-by-id, create, patch, retire, preview-diff).
- [ ] Test code + harness modules documented in PR.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| TBD | … | … | … |

## 10. Lessons Learned

- …

## §Plan Digest

_pending — populated by `/plan-digest {MASTER_PLAN_PATH} {STAGE_ID}`. Sub-sections: §Goal / §Acceptance / §Test Blueprint / §Examples / §Mechanical Steps (each step carries Goal / Edits / Gate / STOP / MCP hints). Template: `ia/templates/plan-digest-section.md`._

### §Goal

Land reusable integration test harness for catalog API routes.

### §Acceptance

- [ ] Integration test harness green; all happy-path routes covered.

### §Test Blueprint

TBD.

### §Examples

TBD.

### §Mechanical Steps

TBD.

## Open Questions

- Framework choice: align with whatever `web/` already uses — confirm in first PR commit.
- Fixture strategy: SQL seed vs TS factory — pick per repo consistency with existing DB tests.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
