---
purpose: "TECH-685 — Map ZoneSubTypeEntry.id 0..6 to grid catalog asset_id (PK) for seven Zone S rows."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T2.3.2
phases:
  - "Phase 1 — Static map + TryResolve API"
---
# TECH-685 — Map subTypeId to asset_id

> **Issue:** [TECH-685](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Provide a single authoritative `int` → `int` map from legacy `ZoneSubTypeEntry.id` (`0..6`) to catalog `asset_id` primary keys for the seven Zone S rows seeded in Postgres + export. Document JSON-era vs snapshot-era in Decision Log. Consumed by registry + callers; TECH-687 locks values.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Expose `bool TryGetAssetIdForSubType(int subTypeId, out int assetId)` (or project naming) for ids `0..6`; invalid id returns `false`.
2. Map values match `GridAssetCatalog` published snapshot for Zone S category rows.
3. `///` + Decision Log line on migration from `Resources/.../zone-sub-types.json` cost fields.

### 2.2 Non-Goals (Out of Scope)

1. No DB migration (already landed in Step 1).
2. No change to `zone-sub-types.json` file content in this task unless required to keep tests green during transition (prefer catalog path first).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|------------------------|
| 1 | Developer | I need a stable subTypeId → asset_id join for catalog reads | API returns true for `0..6` with ids matching export |

## 4. Current State

### 4.1 Domain behavior

`ZoneSubTypeEntry.id` matches JSON; catalog rows use `asset_id` and economy columns from snapshot. Without map, code cannot call `GridAssetCatalog` TryGet by PK from sub-type id.

### 4.2 Systems map

- `ZoneSubTypeRegistry.cs` — map can live in partial class or `ZoneSubTypeAssetIdMap.cs` in same feature folder
- `GridAssetCatalog` — `TryGetAsset` / `TryGet` by int id
- `docs/grid-asset-visual-registry-exploration.md` — seed alignment

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A — internal id join.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Static `readonly` array of seven pairs or `ReadOnlySpan` lookup table; no allocation on hot path. Validate length `7` at domain load with `LogError` on mismatch in debug.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-22 | Hard-coded table from seed | MVP seven rows; no data-driven file for map | ScriptableObject (deferred) |

## 7. Implementation Plan

### Phase 1 — Map + API

- [ ] Add table `0..6` → `asset_id` matching snapshot / seed.
- [ ] Expose `TryGetAssetIdForSubType` (name per codebase).
- [ ] Call sites in TECH-686 use this API; tests in TECH-687 assert table.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Map covered | Unity EditMode | TECH-687 suite | Drift on catalog seed |

## 8. Acceptance Criteria

- [ ] One authoritative map; seven rows.
- [ ] `///` block documenting JSON vs catalog.
- [ ] `TryGet` false for `id` outside `0..6` or `-1` sentinel as designed.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

A single `TryGet` API maps `subTypeId` in `0..6` to catalog `asset_id` PKs for all seven Zone S rows; table values are copied from `db/` seed + `web` export, recorded in **Decision Log**. TECH-686 calls this API; TECH-687 asserts values.

### §Acceptance

- [ ] `bool TryGetAssetIdForSubType(int subTypeId, out int assetId)` (name aligned to codebase) returns `true` for `0..6` and `false` for other ids.
- [ ] `///` on method + one block comment on migration from `Resources/.../zone-sub-types` JSON to catalog-backed economy fields.
- [ ] `npm run unity:compile-check` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| seven_row_lock | map + `GridAssetCatalog` seven rows | assertions pass | TECH-687 EditMode |
| compile | C# | exit 0 | `npm run unity:compile-check` |

### §Examples

| subTypeId | asset_id source |
|-----------|-----------------|
| 0..6 | Seven PKs in same order as `0..6` from `db/migrations` catalog insert for Zone S rows; cross-check `web` export |

### §Mechanical Steps

#### Step 1 — Author `asset_id` table and API on `ZoneSubTypeRegistry`

**Goal:** O(1) `int[7]` table + `TryGet` method; no per-call allocation beyond array index (static readonly array, seven entries).

**Edits:**

- `Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs` — **before** (unique anchor — last 8 lines of file):
  ```
        foreach (var entry in _entries)
            if (entry.id == id) return entry;
        return null;
    }
  }
  ```
  **after**:
  ```
        foreach (var entry in _entries)
            if (entry.id == id) return entry;
        return null;
    }

    // TECH-685: subTypeId 0..6 -> catalog asset_id (literals must match db seed + web export; see Decision Log in ia/projects/TECH-685.md)
    private static readonly int[] SubTypeIdToAssetId = new int[7]
    {
        0, 0, 0, 0, 0, 0, 0
    };

    /// <summary>Maps JSON-era subTypeId to grid catalog <c>asset_id</c> for Zone S seven rows; false when id outside 0..6.</summary>
    public bool TryGetAssetIdForSubType(int subTypeId, out int assetId)
    {
        assetId = 0;
        if (subTypeId < 0 || subTypeId > 6) return false;
        assetId = SubTypeIdToAssetId[subTypeId];
        return assetId > 0;
    }
  }
  ```
  Implementer replaces the seven `0` entries with the published PKs from the catalog seed; commit only when all seven are non-zero and match `db/` + `web` export.

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run unity:compile-check
```

**STOP:** Replace placeholder array with seven real PKs; re-run `rg` on `db/migrations` + export script output until numbers match; document source row in **Decision Log** table in this spec.

**MCP hints:** `backlog_issue` (TECH-685), `glossary_lookup` (asset_id), `invariants_summary` (unity + economy subset).

## Open Questions (resolve before / during implementation)

Lock the seven `asset_id` values against `db/` seed + `web` export; surface mismatch in Decision Log if PK order no longer matches legacy JSON ordering.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
