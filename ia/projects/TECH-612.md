---
purpose: "TECH-612 — Author 0011 core DDL for grid asset catalog."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T1.1.1
---
# TECH-612 — Author 0011 core DDL

> **Issue:** [TECH-612](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Land `0011_catalog_core.sql` with four core tables, uniqueness on `(category, slug)`, cents columns,
and revision column for later PATCH 409 behavior.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `catalog_asset` row carries identity, status, category, slug, `updated_at`.
2. `catalog_sprite` + `catalog_asset_sprite` bind sprites to assets with slot rules.
3. `catalog_economy` holds Zone S / price fields in integer cents.
4. DDL is idempotent on fresh DB and matches exploration §8.1 naming.

### 2.2 Non-Goals (Out of Scope)

1. Drizzle TS mirror (Stage 1.2).
2. HTTP / MCP routes (Stages 1.3–1.4).
3. Unity snapshot consumer (Step 2).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Core catalog tables exist in Postgres | Migration applies; columns match exploration §8.1 |

## 4. Current State

### 4.1 Domain behavior

No authoritative catalog tables yet; exploration `docs/grid-asset-visual-registry-exploration.md` §8.1 defines target schema.

### 4.2 Systems map

- `db/migrations/0011_catalog_core.sql` (new)
- `docs/grid-asset-visual-registry-exploration.md` §8.1
- `ia/specs/economy-system.md` — Zone S vocabulary

## 5. Proposed Design

### 5.1 Target behavior (product)

Published/draft/retired assets with stable `(category, slug)` and cents-backed economy fields for Zone S.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Single SQL migration file; trigger vs app-managed `updated_at` recorded in Decision Log.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Stub from stage-file | Parent Stage 1.1 | — |
| 2026-04-22 | `updated_at` | Application-owned: INSERT uses DEFAULT now(); PATCH bumps row | DB trigger (not in MVP) |

## 7. Implementation Plan

### Phase 1 — Core DDL

- [ ] Author `0011` with enums/checks, FK stubs, UNIQUE `(category, slug)`, NOT NULL cents.
- [ ] Document trigger vs app-owned `updated_at` in §Implementation / Decision Log.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Migration applies clean DB | Shell | `npm run db:migrate` | After 0011 authored |
| Schema lint | Node | `npm run validate:backlog-yaml` | IA-only touch |

## 8. Acceptance Criteria

- [ ] `catalog_asset` row carries identity, status, category, slug, `updated_at`.
- [ ] `catalog_sprite` + `catalog_asset_sprite` bind sprites to assets with slot rules.
- [ ] `catalog_economy` holds Zone S / price fields in integer cents.
- [ ] DDL is idempotent on fresh DB and matches exploration §8.1 naming.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Add core catalog migration with four tables matching exploration §8.1.

### §Acceptance

- [ ] Four core tables created per exploration.
- [ ] `(category, slug)` UNIQUE enforced.
- [ ] Required cents columns NOT NULL.
- [ ] `updated_at` present; ownership in Decision Log.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| migrate_fresh | clean DB + migrate | exit 0 | shell |
| reject_dup_slug | second insert same pair | SQL error | sql |

### §Examples

| artifact | note |
|----------|------|
| exploration §8.1 | column names source of truth |

### §Mechanical Steps

#### Step 1 — Write core DDL

**Goal:** Land `0011` migration file with catalog tables.

**Edits:**
- Create new SQL migration under `db/migrations/` immediately after `0010` series files: filename pattern `0011_catalog_core.sql`. Body: `CREATE TABLE` / `CREATE INDEX` statements for `catalog_asset`, `catalog_sprite`, `catalog_asset_sprite`, `catalog_economy`; UNIQUE on `(category, slug)`; NOT NULL on required economy cents; `updated_at` on asset.

**Gate:**
```bash
npm run db:migrate
```

**STOP:** Gate non-zero → repair SQL; repeat until exit 0 on fresh database.

**MCP hints:** `plan_digest_verify_paths`

#### Step 2 — Decision log row

**Goal:** Record `updated_at` trigger vs application ownership.

**Edits:**
- `ia/projects/TECH-612.md` — **before**:
```
| 2026-04-22 | Stub from stage-file | Parent Stage 1.1 | — |
```
  **after**:
```
| 2026-04-22 | Stub from stage-file | Parent Stage 1.1 | — |
| 2026-04-22 | updated_at | Documented trigger or app-owned | none |
```

**Gate:**
```bash
npm run validate:dead-project-specs
```

**STOP:** Non-zero → fix spec links or paths; re-run.

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
