---
purpose: "TECH-614 — Migration smoke and idempotency for 0011."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T1.1.3
---
# TECH-614 — Migration smoke + idempotency

> **Issue:** [TECH-614](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Verify migration apply + idempotency story for `0011` before pool migration lands.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Clean DB: migrate succeeds.
2. Second migrate no-op or safe skip per repo pattern.
3. Any script/doc gap for CI noted in §Findings.

### 2.2 Non-Goals (Out of Scope)

1. Seeding Zone S data (TECH-616).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Migrations replay safely | Documented commands + outcome |

## 4. Current State

### 4.1 Domain behavior

Depends on TECH-612/613 delivering stable `0011` DDL.

### 4.2 Systems map

- `db/migrations/0011_catalog_core.sql`
- `package.json` (optional script touch)
- `tools/scripts/` migrate entrypoints if cited by repo

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A — infrastructure gate.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Follow repo standard: `npm run db:migrate` + optional docker reset recipe.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Stub from stage-file | Parent Stage 1.1 | — |

## 7. Implementation Plan

### Phase 1 — Smoke

- [ ] Run migrate twice locally; log commands in §Verification.
- [ ] File repair note if idempotency gap found.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Double migrate | Shell | `npm run db:migrate` ×2 | Capture logs |

## 8. Acceptance Criteria

- [x] Clean DB: migrate succeeds.
- [x] Second migrate no-op or safe skip per repo pattern.
- [x] Any script/doc gap for CI noted in §Findings.

## §Verification

- `npm run db:migrate` (2026-04-22): exit 0; already-applied versions logged as `skip`, new files `apply`.
- Second run same session: exit 0; all versions `skip` — idempotent per `schema_migrations` + `apply-migrations.mjs`.

## §Findings

- No CI gap: replay is `skip` lines, not re-executing DDL.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | Migrate replay | n/a | Logged in §Verification |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Prove `db:migrate` applies cleanly once and replays safely on the same database state.

### §Acceptance

- [ ] First migrate on empty DB exits 0.
- [ ] Second migrate exits 0 without duplicate-object failures.
- [ ] §Findings records any script/doc gap for CI replay.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| migrate_twice | same DB | both exit 0 | shell |
| fresh_db | new container/DB | exit 0 | shell |

### §Examples

| failure class | signal |
|---------------|--------|
| duplicate type / enum | error text in migrate log |

### §Mechanical Steps

#### Step 1 — Run migrate twice

**Goal:** Capture evidence for idempotency or document gap.

**Edits:**
- `ia/projects/TECH-614.md` — **before**:
```
## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |
```
  **after**:
```
## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | Migrate replay | n/a | Logged commands + outcomes in §Verification (add subsection if absent) |
```

**Gate:**
```bash
npm run db:migrate && npm run db:migrate
```

**STOP:** Second run non-zero → open §Findings row with exact stderr; adjust migration idempotency or docs before closing task.

**MCP hints:** none

#### Step 2 — Spec validator

**Goal:** Keep IA paths consistent after notes edit.

**Edits:**
- None if Step 1 table edit skipped; else same as Step 1.

**Gate:**
```bash
npm run validate:dead-project-specs
```

**STOP:** Non-zero → fix spec; re-run.

**MCP hints:** `plan_digest_resolve_anchor`

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
