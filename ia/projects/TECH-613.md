---
purpose: "TECH-613 — Indexes and FK policy for catalog core tables."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T1.1.2
---
# TECH-613 — Indexes FKs and status filters

> **Issue:** [TECH-613](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Add indexes and FK actions on `0011` tables so list/get routes and joins stay bounded; align delete
semantics with soft-retire story.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Index columns used in published/draft filters and FK joins.
2. `ON DELETE` / `ON UPDATE` choices documented (no silent orphan rows).
3. Behavior matches exploration retire / `replaced_by` narrative.

### 2.2 Non-Goals (Out of Scope)

1. Pool tables (TECH-615).
2. API error mapping.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Queries on status and joins use indexes | EXPLAIN or convention documented |

## 4. Current State

### 4.1 Domain behavior

Core DDL lands in TECH-612; this task layers access paths and referential actions.

### 4.2 Systems map

- `db/migrations/0011_catalog_core.sql`
- `docs/grid-asset-visual-registry-exploration.md` §8.1–8.2

## 5. Proposed Design

### 5.1 Target behavior (product)

Consistent visibility filters and safe lifecycle when rows retire or remap.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

**FK policy (matches exploration §8.2 / soft-retire):** `replaced_by` is **ON DELETE SET NULL** (clear pointer if old row removed). `catalog_asset_sprite` / `catalog_economy` use **ON DELETE RESTRICT** so hard-deletes never orphan the soft-retire story — retire uses `status` + `replaced_by`, not DELETE. `catalog_sprite` rows **RESTRICT** if still referenced. Secondary indexes: `db/migrations/0011_catalog_core_indexes.sql` (`status`, `category`, `sprite_id`, partial `replaced_by`).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Stub from stage-file | Parent Stage 1.1 | — |

## 7. Implementation Plan

### Phase 1 — Indexes + FK policy

- [ ] Add indexes for `status`, join keys; set FK actions.
- [ ] Capture policy prose in project spec §7 + §Findings if edge case.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Migrate green | Shell | `npm run db:migrate` | After edits |

## 8. Acceptance Criteria

- [ ] Index columns used in published/draft filters and FK joins.
- [ ] `ON DELETE` / `ON UPDATE` choices documented (no silent orphan rows).
- [ ] Behavior matches exploration retire / `replaced_by` narrative.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Add indexes and FK actions on core catalog tables for filter and join paths; document delete semantics for soft-retire.

### §Acceptance

- [ ] Indexes on `status` and join keys as per Intent.
- [ ] FK `ON DELETE` / `ON UPDATE` documented in §7 and match exploration retire story.
- [ ] `npm run db:migrate` exit 0 after change.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| migrate_after_indexes | clean DB | exit 0 | shell |
| explain_status | `EXPLAIN` list query | uses index when rows grow | sql |

### §Examples

| policy | note |
|--------|------|
| soft-retire | avoid hard `CASCADE` that drops economy rows unexpectedly |

### §Mechanical Steps

#### Step 1 — Patch core migration

**Goal:** Extend `0011` migration body with indexes + FK clauses aligned to exploration.

**Edits:**
- Open the migration file created for TECH-612 under `db/migrations/` matching prefix `0011_catalog_core` and extension `.sql`. Append or integrate `CREATE INDEX` for `status` and foreign-key columns; set FK actions consistent with exploration §8.2 retire / `replaced_by` narrative.

**Gate:**
```bash
npm run db:migrate
```

**STOP:** Non-zero → fix SQL; re-run until clean DB succeeds.

**MCP hints:** `plan_digest_verify_paths`

#### Step 2 — Document policy in spec

**Goal:** Capture FK behavior in implementation narrative.

**Edits:**
- `ia/projects/TECH-613.md` — **before**:
```
### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Prefer RESTRICT/SET NULL patterns that match soft-delete; document in §7.
```
  **after**:
```
### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Document exact FK actions chosen (RESTRICT / SET NULL / NO ACTION) per table edge; align with soft-retire + `replaced_by` from exploration §8.2.
```

**Gate:**
```bash
npm run validate:dead-project-specs
```

**STOP:** Non-zero → fix markdown; re-run gate.

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
