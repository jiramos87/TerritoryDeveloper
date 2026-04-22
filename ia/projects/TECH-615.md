---
purpose: "TECH-615 — Author 0012 pool DDL for spawn pools."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T1.1.4
---
# TECH-615 — Author 0012 pool DDL

> **Issue:** [TECH-615](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Introduce spawn pool tables and membership graph for weighted random rolls later in Step 1 consumers.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Pool + member tables with `weight` column and FK to assets.
2. Migration ordering after `0011` enforced in filename chain.
3. Ready for optional seed smoke in T1.1.6.

### 2.2 Non-Goals (Out of Scope)

1. Runtime draw logic in Unity.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Pool schema exists | `0012` applies after `0011` |

## 4. Current State

### 4.1 Domain behavior

Exploration §8.1 defines pool/member shape; core assets table required from `0011`.

### 4.2 Systems map

- `db/migrations/0012_catalog_spawn_pools.sql` (new)
- `docs/grid-asset-visual-registry-exploration.md` §8.1 pool bullets

## 5. Proposed Design

### 5.1 Target behavior (product)

Weighted membership referencing `catalog_asset` rows.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Empty tables at rest OK until TECH-617 optional seed.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Stub from stage-file | Parent Stage 1.1 | — |

## 7. Implementation Plan

### Phase 1 — Pool DDL

- [x] Author `0012` with constraints + indexes for pool lookups.
- [x] Empty tables acceptable at Stage exit; Step 2 consumer validates writes.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Migrate chain | Shell | `npm run db:migrate` | After 0012 |

## 8. Acceptance Criteria

- [ ] Pool + member tables with `weight` column and FK to assets.
- [ ] Migration ordering after `0011` enforced in filename chain.
- [ ] Ready for optional seed smoke in T1.1.6.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Add `0012` migration for `catalog_spawn_pool` and `catalog_pool_member` with `weight` and FK to assets.

### §Acceptance

- [ ] Pool tables exist; FK targets `catalog_asset` (or chosen PK column).
- [ ] Filename sorts after `0011` migration in `db/migrations/`.
- [ ] `npm run db:migrate` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| migrate_chain | DB with 0011 applied | 0012 applies | shell |

### §Examples

| column | type |
|--------|------|
| weight | numeric or int per exploration |

### §Mechanical Steps

#### Step 1 — Author pool migration

**Goal:** Create `0012` SQL file after `0011` file in migration directory.

**Edits:**
- Add new file under `db/migrations/` with name pattern `0012_catalog_spawn_pools.sql` containing `CREATE TABLE` for pool + member, `weight` column, FK to asset table from TECH-612.

**Gate:**
```bash
npm run db:migrate
```

**STOP:** Non-zero → fix ordering or FK target; re-run.

**MCP hints:** `plan_digest_verify_paths`

#### Step 2 — Note deferral if unused

**Goal:** Record empty-table OK for MVP if no consumer yet.

**Edits:**
- `ia/projects/TECH-615.md` — **before**:
```
### Phase 1 — Pool DDL

- [ ] Author `0012` with constraints + indexes for pool lookups.
- [ ] Note deferral if pools unused until Step 2 (tables still exist).
```
  **after**:
```
### Phase 1 — Pool DDL

- [ ] Author `0012` with constraints + indexes for pool lookups.
- [ ] Empty tables acceptable at Stage exit; Step 2 consumer validates writes.
```

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
