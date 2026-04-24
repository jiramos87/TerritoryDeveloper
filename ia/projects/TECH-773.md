---
purpose: "TECH-773 — Load-time replaced_by remap."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T3.3.2"
---
# TECH-773 — Load-time replaced_by remap

> **Issue:** [TECH-773](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

At load time, each `CellData.assetId` walks the catalog `replaced_by` chain (with cycle guard + depth cap) to its current live id; missing rows emit telemetry + follow missing-asset policy (dev loud placeholder / ship hide).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Remap utility consumes `GridAssetCatalog` snapshot; walks `replaced_by` with visited-set cycle guard.
2. Missing asset row → telemetry log + fallback per locked decision; load does NOT crash.
3. Integrate remap between `MigrateLoadedSaveData` and cell restore step (preserves **persistence-system** order).
4. Unit / EditMode test: chain A→B→C remaps A to C; cyclic chain detected + reported.

### 2.2 Non-Goals (Out of Scope)

1. Runtime asset remapping outside load — deferred to post-MVP.
2. GUI tools for manual asset retirement — handled separately.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Old saves reference retired assets | Remap resolves to current asset; load succeeds |
| 2 | Developer | I want visibility on retirement chains | Telemetry logs each remap step; cycle detected + reported |

## 4. Current State

### 4.1 Domain behavior

No remap on load. Saves referencing retired assets fail or resolve to stale data.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — load path, post-migrate hook
- `GridAssetCatalog` — catalog snapshot consumer (Inspector ref, no new singleton)
- `ia/specs/persistence-system.md` §Load pipeline
- `docs/grid-asset-visual-registry-exploration.md` §8.4 point 11 (missing-asset policy)

### 4.3 Implementation investigation notes

Cycle guard: visited-set or depth cap. Missing-asset policy: dev = loud error, ship = silent + telemetry (per locked decisions).

## 5. Proposed Design

### 5.1 Target behavior (product)

Save load resolves retired asset ids to current live ids via `replaced_by` chain walk. Cycle detected + logged. Missing row logged + fallback (dev) or hidden (ship).

### 5.2 Architecture / implementation

Author `RemapAssetIdsOnLoad` helper (catalog snapshot + cycle guard + depth cap). Hook between `MigrateLoadedSaveData` and grid cell restore. Telemetry category per locked decision.

### 5.3 Method / algorithm notes

```
RemapAssetIdsOnLoad(assetId, catalog):
  visited = {}
  depth = 0
  MAX_DEPTH = 100
  
  while assetId in catalog.replaced_by:
    if assetId in visited:
      log("cycle detected")
      return assetId
    if depth >= MAX_DEPTH:
      log("depth exceeded")
      return assetId
    visited.add(assetId)
    assetId = catalog.replaced_by[assetId]
    depth++
  
  if assetId not in catalog.assets:
    log("missing asset", category=DEV_LOUD/SHIP_HIDE)
    return fallback_id
  
  return assetId
```

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Visited-set cycle guard | Simple + clear | Depth cap only (less robust) |
| 2026-04-24 | MAX_DEPTH=100 | Prevent infinite loop on data corruption | Higher limits (more permissive but risky) |

## 7. Implementation Plan

### Phase 1 — Remap + telemetry

- [ ] Author `RemapAssetIdsOnLoad` helper (catalog + cycle guard + depth cap).
- [ ] Hook between `MigrateLoadedSaveData` and grid cell restore.
- [ ] Telemetry log on missing id (category = loud placeholder dev / hide ship).
- [ ] Tests: straight chain remap, cyclic chain abort, missing row fallback.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Chain A→B→C remap | EditMode | `npm run unity:testmode-batch` (if test exists) | Result id = C; telemetry logged |
| Cycle detection | EditMode | Fixture with A→B→A chain | Detected + logged; no infinite loop |
| Missing asset fallback | EditMode | Missing id in catalog | Logged + fallback; load succeeds |

## 8. Acceptance Criteria

- [ ] Remap utility consumes `GridAssetCatalog` snapshot; walks `replaced_by` with visited-set cycle guard.
- [ ] Missing asset row → telemetry log + fallback per locked decision; load does NOT crash.
- [ ] Integrate remap between `MigrateLoadedSaveData` and cell restore step.
- [ ] Unit / EditMode tests cover straight chain, cycle, missing row.

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

At load time, each `CellData.assetId` walks the `GridAssetCatalog` `replaced_by` chain (visited-set cycle guard + depth cap 100) to the current live id; missing rows emit telemetry + fall back to `0` (legacy marker); hook fires AFTER `MigrateLoadedSaveData` + BEFORE `gridManager.RestoreGrid` so **persistence-system** Load pipeline order is preserved.

### §Acceptance

- [ ] New file `Assets/Scripts/Managers/GameManagers/GridAssetRemap.cs` declares public static helper `int RemapAssetIdOnLoad(int saveAssetId, GridAssetCatalog catalog)` (int / int signature — `catalog_asset.id` parsed from the string DTO at the helper boundary).
- [ ] Helper contract: `assetId == 0` returns `0` early (legacy marker fast path); `catalog == null` returns input `saveAssetId` + emits telemetry `remap.catalog_null`; walks `replaced_by` with visited-set + `MAX_DEPTH = 100`; terminal = first id without a `replaced_by` pointer OR missing row.
- [ ] Cycle detected (visited-set hit) → telemetry `remap.cycle_detected` + returns input `saveAssetId`; no infinite loop.
- [ ] Depth cap exceeded → telemetry `remap.depth_exceeded` + returns input `saveAssetId`.
- [ ] Missing catalog row (`replaced_by` target absent) → telemetry `remap.missing_asset` + returns `0` fallback.
- [ ] `GameSaveManager.LoadGame` invokes `RemapAssetIdsForGrid(saveData.gridData, gridAssetCatalog)` in `GameSaveManager` load path AFTER `MigrateLoadedSaveData(saveData);` and BEFORE `gridManager.RestoreGrid(saveData.gridData);` — persist §Load pipeline step order preserved.
- [ ] `GameSaveManager` holds a serialized `[SerializeField] GridAssetCatalog gridAssetCatalog;` reference; remap skipped with telemetry when null.
- [ ] `npm run unity:compile-check` exits 0 after edits.
- [ ] EditMode fixture in TECH-775 covers 5 cases (straight chain, no-op, cycle, missing, depth cap).

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `Remap_StraightChain_ReturnsTerminal` | catalog `{1→2, 2→3}`, `assetId=1` | returns `3` | unity-batch |
| `Remap_NoReplacedBy_ReturnsSelf` | catalog `{}`, `assetId=5` (row 5 present, no `replaced_by`) | returns `5` | unity-batch |
| `Remap_Cycle_DetectedLoggedReturnsSelf` | catalog `{1→2, 2→1}`, `assetId=1` | returns `1`; telemetry `remap.cycle_detected` logged | unity-batch |
| `Remap_MissingAsset_LogsFallback` | catalog without row `99`, `assetId=99` | returns `0`; telemetry `remap.missing_asset` logged | unity-batch |
| `Remap_DepthCapExceeded_Aborts` | catalog linear chain `1→2→3→…→101`, `assetId=1` | returns `1`; telemetry `remap.depth_exceeded` logged | unity-batch |
| `Remap_LegacyAssetIdZero_Passthrough` | any catalog, `assetId=0` | returns `0` (fast path; no walk) | unity-batch |
| `Remap_CatalogNull_Skips` | catalog null, `assetId=5` | returns `5`; telemetry `remap.catalog_null` logged | unity-batch |
| `LoadPipeline_RemapBeforeRestore` | integration: save with retired `assetId` → load | `saveData.gridData[*].assetId` post-remap = live id; `RestoreGrid` sees live id | unity-batch |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| Chain A(id=`"1"`) → B(id=`"2"`) → C(id=`"3"`), save `assetId=1` | `RemapAssetIdOnLoad(1, catalog) == 3` | Terminal = live asset (no `replaced_by`) |
| Straight live asset (`assetId=5`, row 5 present, `replaced_by == ""`) | `RemapAssetIdOnLoad(5, catalog) == 5` | No-op path |
| Cycle A(`"1"`→`"2"`) → B(`"2"`→`"1"`), save `assetId=1` | Telemetry `remap.cycle_detected` + returns `1` | No infinite loop |
| Missing row (`assetId=99`, `99` absent from catalog) | Telemetry `remap.missing_asset` + returns `0` | Load does NOT crash |
| Legacy save (`assetId=0`) | `RemapAssetIdOnLoad(0, catalog) == 0` | Fast path — no walk |
| Catalog null (Inspector unresolved) | Telemetry `remap.catalog_null` + returns input | Not a throw path |

### §Mechanical Steps

#### Step 1 — Author `GridAssetRemap` helper

**Goal:** Create `Assets/Scripts/Managers/GameManagers/GridAssetRemap.cs` exposing `public static int RemapAssetIdOnLoad(int saveAssetId, GridAssetCatalog catalog)` + `public static void RemapAssetIdsForGrid(List<CellData> cells, GridAssetCatalog catalog)`.

**Edits:**
- `Assets/Scripts/Managers/GameManagers/GridAssetRemap.cs` — **operation**: create
  **after** — new file contents:
  ```csharp
  using System.Collections.Generic;
  using UnityEngine;
  using Territory.Core;

  namespace Territory.GridAsset
  {
      /// <summary>
      /// TECH-773 — Load-time remap of retired <see cref="GridAssetCatalog"/> asset ids to their
      /// current live terminal via <c>replaced_by</c> chain walk.
      /// Visited-set cycle guard + depth cap. Missing rows fall back to 0 (legacy marker).
      /// </summary>
      public static class GridAssetRemap
      {
          public const int MAX_DEPTH = 100;

          public static int RemapAssetIdOnLoad(int saveAssetId, GridAssetCatalog catalog)
          {
              if (saveAssetId == 0) return 0; // legacy pre-catalog marker (TECH-772)
              if (catalog == null)
              {
                  Debug.LogWarning("[GridAssetRemap] remap.catalog_null — catalog unresolved; passthrough.");
                  return saveAssetId;
              }
              var visited = new HashSet<int>();
              int current = saveAssetId;
              int depth = 0;
              while (depth < MAX_DEPTH)
              {
                  if (!visited.Add(current))
                  {
                      Debug.LogWarning($"[GridAssetRemap] remap.cycle_detected assetId={saveAssetId} cycle_node={current}");
                      return saveAssetId;
                  }
                  if (!catalog.TryGetAssetRow(current, out var row))
                  {
                      Debug.LogWarning($"[GridAssetRemap] remap.missing_asset assetId={saveAssetId} missing_node={current}");
                      return 0;
                  }
                  if (string.IsNullOrEmpty(row.replaced_by)) return current; // terminal
                  if (!int.TryParse(row.replaced_by, out int nextId))
                  {
                      Debug.LogWarning($"[GridAssetRemap] remap.replaced_by_parse_error assetId={saveAssetId} value={row.replaced_by}");
                      return current;
                  }
                  current = nextId;
                  depth++;
              }
              Debug.LogWarning($"[GridAssetRemap] remap.depth_exceeded assetId={saveAssetId} depth={depth}");
              return saveAssetId;
          }

          public static void RemapAssetIdsForGrid(List<CellData> cells, GridAssetCatalog catalog)
          {
              if (cells == null) return;
              for (int i = 0; i < cells.Count; i++)
              {
                  var cell = cells[i];
                  if (cell == null) continue;
                  cell.assetId = RemapAssetIdOnLoad(cell.assetId, catalog);
              }
          }
      }
  }
  ```
- `invariant_touchpoints`:
  - id: `persistence-load-order`
    gate: `rule_content persistence-system § Load pipeline`
    expected: unchanged
  - id: `catalog-replaced-by-type`
    gate: `grep -n "public string replaced_by" Assets/Scripts/Managers/GameManagers/GridAssetCatalog.Dto.cs`
    expected: pass
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** Compile fails on `catalog.TryGetAssetRow` — if `GridAssetCatalog` does NOT expose that method, re-open Step 1 and substitute the existing public lookup API on `GridAssetCatalog` (search `GridAssetCatalog.cs` for `public` methods returning `CatalogAssetRowDto`); method signature is an implementer detail the helper wraps — it MUST NOT be invented. If no lookup exists → file a TECH-XXX to add one (blocker for TECH-773). Do NOT close Step 1 until compile is green.

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_verify_paths`, `glossary_lookup Grid asset catalog`, `backlog_issue TECH-773`, `rule_content persistence-system`.

#### Step 2 — Add `[SerializeField] GridAssetCatalog gridAssetCatalog` to `GameSaveManager`

**Goal:** Wire a catalog reference on `GameSaveManager` so the load path has a snapshot at remap time. Inspector binding — no singleton.

**Edits:**
- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — **operation**: edit
  **before**:
  ```csharp
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            MigrateLoadedSaveData(saveData);
  ```
  **after**:
  ```csharp
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            MigrateLoadedSaveData(saveData);
            // TECH-773: remap retired asset ids via catalog replaced_by chain BEFORE cell restore.
            // Persist §Load pipeline order preserved: migrate → remap → restore.
            Territory.GridAsset.GridAssetRemap.RemapAssetIdsForGrid(saveData.gridData, gridAssetCatalog);
  ```
- `invariant_touchpoints`:
  - id: `persistence-load-order`
    gate: `rule_content persistence-system § Load pipeline`
    expected: unchanged
  - id: `load-order-migrate-before-restore`
    gate: `grep -n "MigrateLoadedSaveData(saveData);" Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
    expected: pass
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** Remap insertion placed AFTER `gridManager.RestoreGrid` (line 245) → re-open Step 2 anchor; the before-string pins the migrate line specifically so the sequence stays `migrate → remap → restore`.

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_render_literal Assets/Scripts/Managers/GameManagers/GameSaveManager.cs 195 250`.

#### Step 3 — Declare `gridAssetCatalog` serialized field on `GameSaveManager`

**Goal:** Declare the serialized field referenced by Step 2. Place alongside other manager refs (top of class).

**Edits:**
- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — **operation**: edit
  **before**:
  ```csharp
  public class GameSaveManager : MonoBehaviour
  ```
  **after**:
  ```csharp
  public class GameSaveManager : MonoBehaviour
  ```
  (Note: field insertion point pinned via second anchor below — Step 3.)

  Field declaration to be added inside the class body, immediately after the opening brace:
  ```csharp
      [Header("Catalog Refs (TECH-773)")]
      [SerializeField] private GridAssetCatalog gridAssetCatalog;
  ```

  Authoring shortcut — add via anchor to the first existing `[SerializeField]` or `[Header]` declaration in the class:

  **Authoring anchor (alternate — implementer resolves at edit-time via `plan_digest_resolve_anchor`):**
  - Run `plan_digest_resolve_anchor(Assets/Scripts/Managers/GameManagers/GameSaveManager.cs, "public class GameSaveManager")` → get the first unique post-opening-brace anchor (next serialized-field block or `private GridManager gridManager`); insert new `[SerializeField] private GridAssetCatalog gridAssetCatalog;` above it.
- `invariant_touchpoints`:
  - id: `inspector-binding`
    gate: `grep -n "SerializeField.*gridAssetCatalog" Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
    expected: pass
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** Compile fails because `GridAssetCatalog` type not in scope → add `using` for the `GridAssetCatalog` namespace at top of file (same `plan_digest_resolve_anchor` call on existing `using` block). Do NOT close Step 3 until field is declared + compile is green. Scene wiring of this serialized field is Step 5.

**MCP hints:** `plan_digest_resolve_anchor`, `glossary_lookup Grid asset catalog`.

#### Step 4 — Decision Log entry for remap contract

**Goal:** Record the visited-set + MAX_DEPTH=100 + fallback-to-0 contract in TECH-773 §6 Decision Log so downstream readers (TECH-775 test author) see the exact contract.

**Edits:**
- `ia/projects/TECH-773.md` — **operation**: edit
  **before**:
  ```
  | 2026-04-24 | MAX_DEPTH=100 | Prevent infinite loop on data corruption | Higher limits (more permissive but risky) |
  ```
  **after**:
  ```
  | 2026-04-24 | MAX_DEPTH=100 | Prevent infinite loop on data corruption | Higher limits (more permissive but risky) |
  | 2026-04-24 | Missing row → return 0 (legacy marker) | Load does NOT crash; downstream restore treats 0 as pre-catalog | Return input id (stale data risk) |
  | 2026-04-24 | int signature on helper (parse `replaced_by` string at boundary) | Callers already hold `CellData.assetId` (int); parse once at helper edge | string signature (forces callers to stringify) |
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** Decision Log pipe count mismatch → re-open Step 4 with `| a | b | c | d |` column parity.

**MCP hints:** `plan_digest_resolve_anchor`.

#### Step 5 — Scene Wiring — bind `gridAssetCatalog` on `GameSaveManager` in Main scene

**Goal:** Wire the `GridAssetCatalog` catalog asset into the `[SerializeField] gridAssetCatalog` field on the scene GameObject that hosts `GameSaveManager` (per **scene-wiring** rule — serialized-field without Inspector binding = dead runtime path).

**Edits:** (prefer `unity_bridge_command` kinds; text-edit fallback only if bridge unavailable)
- Step 5.1 — `unity_bridge_command { kind: "open_scene", scene: "Assets/Scenes/Main.unity" }`
- Step 5.2 — `unity_bridge_command { kind: "find_gameobject", path: "GameManagers/GameSaveManager" }` → confirm GameObject hosting `GameSaveManager` component; record full scene path.
- Step 5.3 — `unity_bridge_command { kind: "assign_serialized_field", gameobject_path: "<path from 5.2>", component: "GameSaveManager", field: "gridAssetCatalog", value: { guid: "<guid of GridAssetCatalog asset>", type: "GridAssetCatalog" } }`
- Step 5.4 — `unity_bridge_command { kind: "save_scene", scene: "Assets/Scenes/Main.unity" }`

**Evidence (required in §Acceptance upon completion):**
```yaml
scene: "Assets/Scenes/Main.unity"
parent_object: "<path recorded in 5.2>"
component: "GameSaveManager"
serialized_fields:
  gridAssetCatalog: "<GUID of bound GridAssetCatalog asset>"
unity_events: []
compile_check: "npm run unity:compile-check exits 0"
```

- `invariant_touchpoints`:
  - id: `scene-wiring-serialized-field`
    gate: `rule_content unity-scene-wiring`
    expected: pass
  - id: `scene-file-diff-present`
    gate: `git diff --name-only | grep Main.unity`
    expected: pass
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** `git diff` does NOT include `Assets/Scenes/Main.unity` after Step 5.4 → re-open Step 5 (save_scene step did not flush). If `find_gameobject` returns no match → the `GameSaveManager` scene object is named differently; resolve path first via `unity_bridge_command { kind: "find_component", component: "GameSaveManager" }` before retrying 5.2–5.4. Do NOT close Task until scene diff is visible + `npm run unity:compile-check` green.

**MCP hints:** `unity_bridge_command`, `find_gameobject`, `find_component`, `get_compilation_status`, `rule_content unity-scene-wiring`.

### §Findings

- `replaced_by` DTO field confirmed at `Assets/Scripts/Managers/GameManagers/GridAssetCatalog.Dto.cs:86` — type `string`, points to another asset `id` (also `string`); remap helper parses at boundary so callers stay int-typed (matches `CellData.assetId`).
- Persistence invariant: `ia/specs/persistence-system.md` §Load pipeline — 4 restore steps, order locked; remap insertion between migrate + cell restore preserves that order.
- Load hook exact sites in `GameSaveManager.cs`: `MigrateLoadedSaveData(saveData);` at line 200 + `gridManager.RestoreGrid(saveData.gridData);` at line 245.

## Open Questions (resolve before / during implementation)

- **Glossary candidate:** `Grid asset catalog` — see TECH-772 §Open Questions.
- **`replaced_by` type:** currently `string` in DTO (`GridAssetCatalog.Dto.cs:86`); consumer must clarify — asset id as string key vs int parse. Implementer decision at code time.
- **Fallback id policy:** concrete integer (0 = legacy) vs sentinel (-1 = hide) — defer to locked decision in `docs/grid-asset-visual-registry-exploration.md §8.4 point 11` at implementation.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor | critical._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
