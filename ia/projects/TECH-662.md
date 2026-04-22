---
purpose: "TECH-662 — Export reads published rows."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T2.1.1
phases:
  - "Phase 1 — Reader"
---
# TECH-662 — Export reads published rows

> **Issue:** [TECH-662](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Implement Node/TS export path that queries joined catalog tables through existing DB access layer, emits in-memory row set suitable for snapshot serialization, default published-only with optional draft inclusion for dev.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Published rows default; explicit dev mode for drafts.
2. Deterministic ordering (stable sort keys documented).
3. Column coverage matches asset/sprite/bind/economy contract from migrations 0011/0012.

### 2.2 Non-Goals (Out of Scope)

1. Unity C# loader (Stage 2.2).
2. MCP catalog mutation tools (prior stages).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Run catalog export against local DB | Published rows returned in stable order; optional drafts flag |

## 4. Current State

### 4.1 Domain behavior

Postgres holds authoritative catalog; HTTP API and MCP already expose rows; snapshot export not yet implemented.

### 4.2 Systems map

web/lib/db/, db/migrations/0011_catalog_core.sql + 0012_catalog_spawn_pools.sql, web/types/api/catalog*.ts DTOs, new tools/catalog-export or tools/scripts entry, package.json npm script alias catalog:export (stub OK until wired).

## 5. Proposed Design

### 5.1 Target behavior (product)

CLI or script reads same logical data the game will consume at boot, filtered per visibility rules.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Hand-written SQL or thin query module; no Drizzle in web per architecture audit; reuse connection patterns from existing web DB helpers where practical.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Scope from Stage 2.1 orchestrator | Filed via stage-file | — |

## 7. Implementation Plan

### Phase 1 — Reader

- [ ] Implement SQL or Drizzle-free query module against `getSql()` / `sql` from `web/lib/db/client.ts`; unit/integration smoke against fixture DB or mocked pool; document connection env (DATABASE_URL).
- [ ] Document default `published` filter + dev flag name in §7b Notes column.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tooling + IA | Node | `npm run validate:all` when touching scripts/package/IA | Per repo policy |
| Export reader | Node | Unit or integration test for query module | Optional fixture DB; default `published`; dev: `--include-drafts` |

## 8. Acceptance Criteria

- [ ] Published rows default; explicit dev mode for drafts.
- [ ] Deterministic ordering (stable sort keys documented).
- [ ] Column coverage matches asset/sprite/bind/economy contract from migrations 0011/0012.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Ship Postgres-backed reader for catalog export: published-default query path plus optional drafts, deterministic ordering, joins aligned to snapshot DTOs.

### §Acceptance

- [ ] Published-only default query wired; draft flag documented on CLI.
- [ ] Join covers asset + sprite + bind + economy columns needed for snapshot.
- [ ] Deterministic sort keys documented in §7 or task §Findings.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| export_query_smoke | local DB with Zone S seed | non-empty published set | node |
| ordering_stable | two runs same DB | identical row order | node |

### §Examples

| Mode | Filter | Expected |
|------|--------|----------|
| default | `status = published` | ship-visible rows only |
| dev flag | include draft | larger row set for editor |

### §Mechanical Steps

#### Step 1 — tighten implementation checklist

**Goal:** Lock reader deliverables inside spec §7 for reviewers.

**Edits:**

- `ia/projects/TECH-662.md` — **before**:

```
- [ ] Implement SQL or Drizzle-free query module, unit/integration smoke against fixture DB or mocked pool; document connection env (DATABASE_URL).
```

  **after**:

```
- [ ] Implement SQL or Drizzle-free query module against `getSql()` / `sql` from `web/lib/db/client.ts`; unit/integration smoke against fixture DB or mocked pool; document connection env (DATABASE_URL).
- [ ] Document default `published` filter + dev flag name in §7b Notes column.
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Re-open §7 bullet if validator reports spec path drift.

**MCP hints:** `backlog_issue`, `plan_digest_resolve_anchor`

#### Step 2 — wire npm entry (stub ok)

**Goal:** Expose `catalog:export` from repo root for later serializer wiring.

**Edits:**

- `package.json` — **before**:

```
    "validate:catalog-dto": "node tools/scripts/catalog-dto-migration-check.mjs",
    "validate:parent-plan-locator": "node tools/validate-parent-plan-locator.mjs",
```

  **after**:

```
    "validate:catalog-dto": "node tools/scripts/catalog-dto-migration-check.mjs",
    "catalog:export": "node -e \"process.exit(0)\"",
    "validate:parent-plan-locator": "node tools/validate-parent-plan-locator.mjs",
```

**Gate:**

```bash
node tools/validate-dead-project-spec-paths.mjs
```

**STOP:** If gate non-zero, fix JSON comma placement in `package.json`.

**MCP hints:** `plan_digest_resolve_anchor`

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 and Stage Exit for product-adjacent constraints.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
