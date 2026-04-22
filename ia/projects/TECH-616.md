---
purpose: "TECH-616 — Seed seven Zone S catalog reference assets."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T1.1.5
---
# TECH-616 — Seed seven Zone S assets

> **Issue:** [TECH-616](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Seed reference `catalog_asset` (+ economy rows) for seven Zone S sub-types so Unity / API consumers
can rely on stable ids 0–6.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Seven rows with correct slugs / categories per economy spec.
2. Cents / registry fields populated or explicitly defaulted.
3. Seed is repeatable (fixture SQL or idempotent upsert pattern).

### 2.2 Non-Goals (Out of Scope)

1. Final art sprites (nullable binds OK).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Zone S ids match `zone-sub-types.json` intent | Row count + slug check |

## 4. Current State

### 4.1 Domain behavior

**ZoneSubTypeRegistry** and `Assets/Resources/Economy/zone-sub-types.json` define seven sub-types.

### 4.2 Systems map

- `db/migrations/` or `tools/` seed artifact (per chosen approach)
- `Assets/Resources/Economy/zone-sub-types.json`
- `ia/specs/economy-system.md` — **ZoneSubTypeRegistry**

## 5. Proposed Design

### 5.1 Target behavior (product)

Stable numeric ids 0–6 aligned with existing JSON vocabulary for Zone S.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Prefer migration-bundled seed or documented repeatable script per repo norms.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Stub from stage-file | Parent Stage 1.1 | — |

## 7. Implementation Plan

### Phase 1 — Zone S seed

- [ ] Author seed SQL or runner; wire into migrate or documented one-shot.
- [ ] Verify row count + id range in §Verification.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Row count | SQL | `SELECT count(*)` | Seven Zone S rows |

## 8. Acceptance Criteria

- [ ] Seven rows with correct slugs / categories per economy spec.
- [ ] Cents / registry fields populated or explicitly defaulted.
- [ ] Seed is repeatable (fixture SQL or idempotent upsert pattern).

## §Verification

- After `npm run db:migrate`: `SELECT id, slug, display_name FROM catalog_asset WHERE category = 'zone_s' ORDER BY id` → seven rows, ids 0–6, slugs `police` … `public_offices`.
- Economy cents follow `zone-sub-types.json` scale ×100 (e.g. Police `base_cost_cents=50000`, `monthly_upkeep_cents=5000`).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | Seed verification | n/a | §Verification row count + slug check |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Insert seven Zone S reference rows (ids 0–6) aligned with `Assets/Resources/Economy/zone-sub-types.json` vocabulary.

### §Acceptance

- [ ] Seven rows in catalog tables with stable ids 0–6 (or documented mapping if DB uses serial — adjust spec if exploration requires fixed ids via `INSERT` with explicit keys).
- [ ] Slugs / categories match JSON intent.
- [ ] Sprite binds nullable; economy cents valid or defaulted.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| count_zone_s | SQL count | seven rows | sql |
| slug_match | JSON vs DB | names align | manual |

### §Examples

| source | path |
|--------|------|
| JSON vocabulary | `Assets/Resources/Economy/zone-sub-types.json` |

### §Mechanical Steps

#### Step 1 — Author seed artifact

**Goal:** Repeatable seed (SQL block in migration, or script under `tools/`).

**Edits:**
- Add seed SQL or script path referenced from `TECH-616` §7; inserts seven assets + economy rows; uses category vocabulary consistent with exploration and JSON file above.

**Gate:**
```bash
npm run db:migrate
```

**STOP:** Non-zero → fix seed ordering (0011/0012 must apply first); re-run.

**MCP hints:** `plan_digest_verify_paths`

#### Step 2 — Verification note in spec

**Goal:** Record verification query in §Verification or §9.

**Edits:**
- `ia/projects/TECH-616.md` — **before**:
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
| 1 | Seed verification | n/a | Row count + sample slugs captured after migrate |
```

**Gate:**
```bash
npm run validate:dead-project-specs
```

**STOP:** Non-zero → fix; re-run.

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
