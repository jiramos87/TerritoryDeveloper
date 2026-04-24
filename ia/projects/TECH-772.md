---
purpose: "TECH-772 — Add save fields."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T3.3.1"
---
# TECH-772 — Add save fields

> **Issue:** [TECH-772](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

Extend `CellData` / building DTO with `assetId` (int, default 0 = legacy pre-catalog marker); bump `GameSaveData.schemaVersion`; wire `MigrateLoadedSaveData` when field added. Respect **persistence-system** Load pipeline order — no reorder.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `CellData` carries `assetId` (int, default 0) serialized into save.
2. `GameSaveData.schemaVersion` bumped iff field addition changes binary compatibility.
3. `MigrateLoadedSaveData` handles legacy saves deterministically (default `assetId` = 0 = legacy pre-catalog).
4. Save round-trip test covers v0 → v{new} upgrade path.

### 2.2 Non-Goals (Out of Scope)

1. Runtime asset remapping — handled by TECH-773.
2. GC implementation — handled by TECH-774.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want to persist asset catalog ids in saves | Round-trip test passes; legacy saves default `assetId=0` |
| 2 | Developer | I want schema bumps to respect Load pipeline order | Migration code runs in correct sequence per **persistence-system** spec |

## 4. Current State

### 4.1 Domain behavior

Saves currently do NOT persist asset catalog ids; asset identity lost on reload. Schema version locked.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — `CellData` DTO, `MigrateLoadedSaveData`
- `Assets/Scripts/Managers/GameManagers/GridManager.cs` — `CellData` write/read sites
- `ia/specs/persistence-system.md` §Save + §Load pipeline

### 4.3 Implementation investigation notes

None — straightforward DTO extension.

## 5. Proposed Design

### 5.1 Target behavior (product)

`CellData` DTO expanded with `assetId` field. Serialized into save binary / JSON. Legacy saves (pre-catalog) default to `assetId=0`.

### 5.2 Architecture / implementation

Add `int assetId = 0` field to `CellData`; bump `GameSaveData.CurrentSchemaVersion` if binary layout changes; extend `MigrateLoadedSaveData` to treat missing field as 0.

### 5.3 Method / algorithm notes

None — straightforward.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Default `assetId=0` signals legacy | Avoids null checks; 0 = pre-catalog era | Sentinel value like -1 (less clear) |

## 7. Implementation Plan

### Phase 1 — DTO + schema bump

- [ ] Add `assetId` field to `CellData` (default 0).
- [ ] Bump `GameSaveData.schemaVersion` if field touches binary layout; else document no-bump rationale.
- [ ] Extend `MigrateLoadedSaveData` to treat missing `assetId` as 0 (legacy marker).
- [ ] Round-trip test: save v_prev → load → resave matches.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Save round-trip (v0 → v{new}) | EditMode | `npm run unity:testmode-batch` (if test exists) or manual verify | DTO field persists + deserializes correctly |
| Schema version bump correctness | Code review | `GameSaveData.CurrentSchemaVersion` compared to binary changes | Bump only if binary layout changed |

## 8. Acceptance Criteria

- [ ] `CellData` carries `assetId` (int, default 0) serialized into save.
- [ ] `GameSaveData.schemaVersion` bumped iff field addition changes binary compatibility.
- [ ] `MigrateLoadedSaveData` handles legacy saves deterministically (default `assetId` = 0).
- [ ] Save round-trip test covers v0 → v{new} upgrade path.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- None yet.

## §Plan Digest

```yaml
mechanicalization_score:
  anchors: ok
  picks: ok
  invariants: ok
  validators: ok
  escalation_enum: ok
  overall: fully_mechanical
```

### §Goal

Persist a stable **`assetId`** (int) on every `CellData` so save → load round-trips preserve `GridAssetCatalog` identity for buildings + terrain; JsonUtility-additive change — `GameSaveData.CurrentSchemaVersion` stays `4` (no binary-layout break); `MigrateLoadedSaveData` stays a no-op for the new field (JSON-absent → default `0` = legacy pre-catalog marker).

### §Acceptance

- [ ] `CellData` class exposes public `int assetId` field (declared next to `sortingOrder` / `height` — existing grid-position block).
- [ ] `CellData.SetDefaults()` sets `this.assetId = 0;`.
- [ ] `CellData.Clone()` copies `assetId`.
- [ ] `GameSaveData.CurrentSchemaVersion` stays `4` (JSON-additive — documented in Decision Log with no-bump rationale).
- [ ] `MigrateLoadedSaveData` does NOT alter `assetId` (JsonUtility defaults absent-field to `0`; matches legacy-marker contract).
- [ ] EditMode test fixture in TECH-775 covers `Save_AssetId_RoundTrip` + `Save_AssetId_LegacyDefaultsZero` + `Migrate_Idempotent` — authored by TECH-775, depends on this Task's DTO shape.
- [ ] `npm run unity:compile-check` exits 0 after edits.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `Save_AssetId_RoundTrip` | `CellData{assetId=7}` save → load | `loaded.assetId == 7` | unity-batch |
| `Save_AssetId_LegacyDefaultsZero` | save JSON with `assetId` field absent | `loaded.assetId == 0` | unity-batch |
| `Migrate_LegacySave_SetsAssetIdZero` | `GameSaveData{schemaVersion=0}` → `MigrateLoadedSaveData` | every `CellData.assetId == 0`; final `schemaVersion == CurrentSchemaVersion` | unity-batch |
| `Migrate_Idempotent` | call `MigrateLoadedSaveData` twice | second call = no-op; `assetId` unchanged | unity-batch |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| Fresh save (`assetId = 7`) | Round-trip: load → resave → `assetId == 7` | Happy path |
| Legacy JSON (no `assetId` field) | `loaded.assetId == 0` | JsonUtility absent-default = 0 = legacy marker |
| Schema 0 → 4 migrate | Migrate runs; `assetId` untouched (already 0 via JsonUtility); final `schemaVersion == 4` | Idempotent |
| Second migrate call | Same `schemaVersion`, same `assetId` | No-op |

### §Mechanical Steps

#### Step 1 — Add `assetId` field to `CellData` DTO

**Goal:** Add public `int assetId` field to `CellData` in the grid-position block (next to `sortingOrder` / `height`); JsonUtility serializes it into save JSON.

**Edits:**
- `Assets/Scripts/Managers/UnitManagers/CellData.cs` — **operation**: edit
  **before**:
  ```csharp
      public int sortingOrder;
      public int height;
  ```
  **after**:
  ```csharp
      public int sortingOrder;
      public int height;

      /// <summary>
      /// Stable <see cref="GridAssetCatalog"/> asset id persisted in save.
      /// 0 = legacy pre-catalog marker (TECH-772). Remap on load handled by TECH-773.
      /// </summary>
      public int assetId;
  ```
- `invariant_touchpoints`:
  - id: `persistence-load-order`
    gate: `rule_content persistence-system § Load pipeline`
    expected: unchanged
  - id: `save-schema-compat`
    gate: `grep -n "CurrentSchemaVersion = 4" Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
    expected: pass
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** Compile fails → re-open Step 1 anchor; field added outside the class body = misplaced. If field position drift breaks `CellData.Clone()` ordering expectations → proceed to Step 3 immediately.

**MCP hints:** `plan_digest_resolve_anchor`, `backlog_issue TECH-772`, `glossary_lookup CellData`.

#### Step 2 — Default `assetId` in `SetDefaults()`

**Goal:** Explicit default `this.assetId = 0;` inside `SetDefaults()` — mirrors symbolic zero for every other numeric field; no throw path in `MigrateLoadedSaveData`.

**Edits:**
- `Assets/Scripts/Managers/UnitManagers/CellData.cs` — **operation**: edit
  **before**:
  ```csharp
          this.closeForestCount = 0;
          this.closeWaterCount = 0;
          this.prefab = null;
  ```
  **after**:
  ```csharp
          this.closeForestCount = 0;
          this.closeWaterCount = 0;
          this.assetId = 0;
          this.prefab = null;
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** Compile fails → re-open Step 2 anchor. If default not emitted in round-trip test → ensure `assetId` declared before `SetDefaults()` call site.

**MCP hints:** `plan_digest_resolve_anchor`.

#### Step 3 — Copy `assetId` in `CellData.Clone()`

**Goal:** Clone semantics must copy `assetId` so in-memory cell duplication (grid hydration, restore flows) preserves asset identity.

**Edits:**
- `Assets/Scripts/Managers/UnitManagers/CellData.cs` — **operation**: edit
  **before**:
  ```csharp
          clone.closeForestCount = closeForestCount;
          clone.closeWaterCount = closeWaterCount;
  ```
  **after**:
  ```csharp
          clone.closeForestCount = closeForestCount;
          clone.closeWaterCount = closeWaterCount;
          clone.assetId = assetId;
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** Compile fails → re-open Step 3 anchor. Clone test in TECH-775 asserts `clone.assetId == orig.assetId`; failure → re-open this step.

**MCP hints:** `plan_digest_resolve_anchor`.

#### Step 4 — Document no-bump rationale in Decision Log

**Goal:** Decision Log records that `CurrentSchemaVersion` stays at `4` — JsonUtility is additive-safe (absent field → type default); no migration branch needed.

**Edits:**
- `ia/projects/TECH-772.md` — **operation**: edit
  **before**:
  ```
  | 2026-04-24 | Default `assetId=0` signals legacy | Avoids null checks; 0 = pre-catalog era | Sentinel value like -1 (less clear) |
  ```
  **after**:
  ```
  | 2026-04-24 | Default `assetId=0` signals legacy | Avoids null checks; 0 = pre-catalog era | Sentinel value like -1 (less clear) |
  | 2026-04-24 | No `CurrentSchemaVersion` bump | JsonUtility defaults absent int field to 0 — legacy JSON loads cleanly; no binary-layout break | Bump to 5 + migrate branch (unnecessary churn) |
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** Validator red on Decision Log table shape → re-open Step 4 with correct pipe-column count.

**MCP hints:** `plan_digest_resolve_anchor`.

### §Findings

- Glossary gap candidate: `Grid asset catalog` (referenced in stub + master plan, no canonical row). Raised in §Open Questions.
- `replaced_by` column confirmed at `Assets/Scripts/Managers/GameManagers/GridAssetCatalog.Dto.cs:86` — consumed by sibling TECH-773.
- JsonUtility additive-safe confirmed: absent int field deserializes to `0`; no migration branch needed in `MigrateLoadedSaveData` for `assetId`.

## Open Questions (resolve before / during implementation)

- **Glossary candidate (defer to glossary curator):** `Grid asset catalog` — referenced across Step 3 master plan + this spec but absent from `ia/specs/glossary.md`. Propose row under category `Zones & Buildings` with `specReference` = `docs/grid-asset-visual-registry-exploration.md §1.4`.
- **Decision: binary vs JSON-additive bump.** Pre-implementation: inspect current serializer (BinaryFormatter vs JSON) — if JSON-additive, skip `CurrentSchemaVersion` bump + document in Decision Log.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor | critical._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
