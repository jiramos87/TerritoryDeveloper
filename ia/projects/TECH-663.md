---
purpose: "TECH-663 — Snapshot JSON schema + version."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T2.1.2
phases:
  - "Phase 1 — Types + serializer"
---
# TECH-663 — Snapshot JSON schema + version

> **Issue:** [TECH-663](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Define canonical snapshot JSON shape consumed by Unity loader: top-level metadata plus arrays; enforce stable sort; bump schemaVersion when breaking.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Top-level schemaVersion + generatedAt ISO-8601.
2. Arrays for assets, sprites, bindings, economy with stable sort keys.
3. Human-readable schema note or JSON Schema file under tools/docs for agents.

### 2.2 Non-Goals (Out of Scope)

1. Runtime parse in Unity (Stage 2.2).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Serialize catalog rows to versioned JSON | schemaVersion + stable ordering verified by golden test |

## 4. Current State

### 4.1 Domain behavior

Migrations and DTOs exist; snapshot envelope not yet fixed.

### 4.2 Systems map

tools/catalog-export (serializer), docs/grid-asset-visual-registry-exploration.md §8.2, web/types/api/catalog*.ts field parity.

## 5. Proposed Design

### 5.1 Target behavior (product)

Single file format Unity and tools agree on; version increments on breaking layout changes.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

TypeScript types + explicit sort comparator before stringify.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Scope from Stage 2.1 orchestrator | Filed via stage-file | — |
| 2026-04-22 | `schemaVersion` — bump `CATALOG_SNAPSHOT_SCHEMA_VERSION` in `web/lib/catalog/build-catalog-snapshot.ts` on breaking JSON | Aligns with Unity `GridAssetCatalog` | Ad-hoc file fork |

## 7. Implementation Plan

### Phase 1 — Types + serializer

- [ ] TypeScript interfaces matching `web/types/api/catalog*.ts`; explicit `stableJsonStringify` before file write; golden test under `web/lib/catalog/`.
- [ ] Document `schemaVersion` bump protocol in §6 Decision Log when layout breaks.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tooling | Node | `npm run validate:all` when touching IA/package | |
| Golden JSON | Node | Fixture diff test | Stable key order |

## 8. Acceptance Criteria

- [ ] Top-level schemaVersion + generatedAt ISO-8601.
- [ ] Arrays for assets, sprites, bindings, economy with stable sort keys.
- [ ] Human-readable schema note or JSON Schema file under tools/docs for agents.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Freeze snapshot JSON envelope: `schemaVersion`, `generatedAt`, ordered arrays matching catalog DTOs so Unity loader (Stage 2.2) compiles against one contract.

### §Acceptance

- [ ] Top-level `schemaVersion` + `generatedAt` ISO-8601 in output object.
- [ ] Arrays sorted by stable keys documented in §7.
- [ ] Golden test or fixture proves byte-stable serialization for fixed input rows.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| snapshot_sort | two logical row orderings | same JSON bytes after sort | node |
| schema_bump_doc | n/a | §7 records bump rule | manual |

### §Examples

| Field | Example | Notes |
|-------|---------|-------|
| schemaVersion | `1` | integer bump on breaking change |
| generatedAt | `2026-04-22T12:00:00.000Z` | UTC ISO |

### §Mechanical Steps

#### Step 1 — extend §7 serializer bullets

**Goal:** Bind serializer work to typed rows from TECH-662 reader.

**Edits:**

- `ia/projects/TECH-663.md` — **before**:

```
- [ ] TypeScript interfaces matching DTOs; JSON.stringify with ordered keys; golden fixture test for sort stability.
```

  **after**:

```
- [ ] TypeScript interfaces matching `web/types/api/catalog*.ts`; explicit sort functions before `JSON.stringify`; golden fixture under `tools/mcp-ia-server/tests/fixtures/` or sibling `tools/` test dir.
- [ ] Document `schemaVersion` bump protocol in §6 Decision Log when layout breaks.
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Restore §7 text if validator flags spec links.

**MCP hints:** `plan_digest_resolve_anchor`

#### Step 2 — cross-link exploration snapshot prose

**Goal:** Keep human-readable contract next to §8.2 diagram in exploration doc.

**Edits:**

- `docs/grid-asset-visual-registry-exploration.md` — **before**:

```
### 8.2 Architecture
```

  **after**:

```
### 8.2 Architecture

<!-- catalog-snapshot-schema-TECH-663: top-level `{ schemaVersion, generatedAt, assets[], ... }` matches hand-written DTOs; stable sorts defined in Stage 2.1 export code. -->
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** If anchor matches twice, narrow **before** block with extra blank line context from file HEAD.

**MCP hints:** `plan_digest_resolve_anchor`

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
