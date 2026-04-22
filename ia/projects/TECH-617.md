---
purpose: "TECH-617 — Optional pool seed smoke or documented deferral."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T1.1.6
---
# TECH-617 — Pool seed smoke optional

> **Issue:** [TECH-617](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Prove optional pool membership insert or explicitly defer with recorded rationale.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Either minimal pool+member seed exists OR §Findings states deferral with empty tables OK.
2. No broken FK references to seeded assets.

### 2.2 Non-Goals (Out of Scope)

1. Full spawn simulation coverage.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Pool write path verified or deferred | §Findings records outcome |

## 4. Current State

### 4.1 Domain behavior

`0012` may be empty; TECH-616 may supply asset ids for FK targets.

### 4.2 Systems map

- `db/migrations/0012_catalog_spawn_pools.sql`
- Seed artifact from T1.1.5 if reused

## 5. Proposed Design

### 5.1 Target behavior (product)

Optional proof that `catalog_pool_member` accepts weighted rows; MVP may defer.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Minimal one-pool / one-member insert vs explicit deferral note.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Stub from stage-file | Parent Stage 1.1 | — |

## 7. Implementation Plan

### Phase 1 — Optional pool smoke

- [ ] Insert minimal pool/member rows OR document deferral.
- [ ] Note outcome in §Findings for Step 2 consumers.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| SQL insert or N/A | SQL / prose | §Findings | Deferral explicit |

## 8. Acceptance Criteria

- [ ] Either minimal pool+member seed exists OR §Findings states deferral with empty tables OK.
- [ ] No broken FK references to seeded assets.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | Pool smoke | MVP scope | `0014_catalog_pool_smoke.sql` — pool `smoke_zone_s_tool` + member (asset 0, weight 100) |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Either insert minimal pool+member rows referencing seeded assets, or document explicit deferral while keeping pool tables empty.

### §Acceptance

- [ ] Minimal seed exists **or** §Findings states deferral with rationale.
- [ ] No orphan FK references.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| optional_insert | SQL | one member row **or** deferral note | sql / prose |

### §Examples

| outcome | §Findings text |
|---------|----------------|
| defer | "Pools deferred to Step 2; tables empty." |

### §Mechanical Steps

#### Step 1 — Pool smoke or deferral

**Goal:** Execute optional SQL against `catalog_spawn_pool` / `catalog_pool_member` **or** edit §Findings.

**Edits:**
- If implementing smoke: run SQL (documented in §7) inserting one pool + one member with `weight`, referencing existing `catalog_asset` id from TECH-616 seed.
- If deferring: `ia/projects/TECH-617.md` — **before**:
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
| 1 | Pool smoke | MVP scope | Deferred to Step 2; empty pool tables per Stage exit |
```

**Gate:**
```bash
npm run db:migrate
```

**STOP:** Non-zero → fix SQL or seed order; re-run.

**MCP hints:** `plan_digest_resolve_anchor`

#### Step 2 — Validator

**Goal:** IA consistency.

**Edits:**
- None additional when deferral path only updates §9 table.

**Gate:**
```bash
npm run validate:dead-project-specs
```

**STOP:** Non-zero → fix; re-run.

**MCP hints:** none

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
