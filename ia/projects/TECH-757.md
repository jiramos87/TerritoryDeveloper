---
purpose: "TECH-757 — Wire ghost preview to validator."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T3.2.1"
---
# TECH-757 — Wire ghost preview to validator

> **Issue:** [TECH-757](../../BACKLOG.md)
> **Status:** Draft | In Review | In Progress | Final
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Cursor preview hook calls `PlacementValidator.CanPlace(assetId, cell, rotation)` each
cell-change event; result drives tint path (TECH-758/759) and tooltip (TECH-760).
Throttle to cell-change, not per-frame.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Cursor move event resolves `PlacementValidator` reference once (cached, not
   `FindObjectOfType` per-frame).
2. `CanPlace` called on cell-change only; no per-frame allocation in hit-test path.
3. Result tuple (bool + reason) forwarded to tint + tooltip consumers via event or
   direct call.
4. `GridManager.GetCell(x,y)` only; no `cellArray` touch (invariant #5).

### 2.2 Non-Goals (Out of Scope)

1. Per-frame polling — cell-change event only.
2. New collision layer or `Collider2D` on preview ghost.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Preview validates placement on each cursor move | `PlacementValidator.CanPlace` called, result (bool + reason) available downstream |
| 2 | Developer | No per-frame `FindObjectOfType` in hot path | Validator ref cached on `Awake`; cell-change event throttles calls |

## 4. Current State

### 4.1 Domain behavior

Ghost preview sprite follows cursor but has no placement validation. TECH-688 (PlacementValidator API) and TECH-689 (PlacementFailReason enum) exist and ready to consume.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — preview hook entry.
- `Assets/Scripts/Managers/GameManagers/PlacementValidator.cs` (TECH-688) — consumer of
  `CanPlace`.
- `Assets/Scripts/Managers/GameManagers/GridManager.cs` — hit-test contract (unchanged).
- Possible new: `CursorPreviewController.cs` helper if extraction warranted (guardrail #3).

### 4.3 Implementation investigation notes (optional)

- Determine cursor cell-change event source (existing or new).
- Validate throttle strategy (event per cell vs per-frame filter).

## 5. Proposed Design

### 5.1 Target behavior (product)

Cursor move resolves grid cell; if cell changes, call `PlacementValidator.CanPlace` once per new cell; result tuple (bool + reason) propagates downstream.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Cache validator ref on `Awake` (no `FindObjectOfType` hot path).
- Subscribe to cursor cell-change event or hook into cursor move and filter by cell delta.
- Route result to tint consumers (TECH-758/759) and tooltip consumer (TECH-760) via event or direct call.

### 5.3 Method / algorithm notes (optional)

```
On CursorManager.OnCellChanged(newCell):
  if cachedValidator == null:
    cachedValidator = GetComponent<PlacementValidator>() or FindObjectOfType<PlacementValidator>()
  result = cachedValidator.CanPlace(currentAssetId, newCell, currentRotation)
  tintConsumer.OnValidationResult(result)
  tooltipConsumer.OnValidationResult(result)
```

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| TBD | Event vs direct call for result propagation | TBD | TBD |

## 7. Implementation Plan

### Phase 1 — Hook + throttle

- [ ] Cache validator ref on Awake; subscribe to cursor cell-change event.
- [ ] Call `CanPlace`; route result to tint + tooltip consumers.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Validator ref cached; no per-frame FindObjectOfType | Code review | Assets/Scripts/Managers/GameManagers/CursorManager.cs | Inspect Awake / OnDestroy lifecycle |
| Cell-change throttle implemented | Code review | Cursor event subscription logic | Verify event fires only on cell delta, not per-frame |
| Result tuple forwarded to tint + tooltip | Integration test (manual or testmode) | Play Mode + verify-loop scenario | Ghost tint + tooltip respond to validation result |

## 8. Acceptance Criteria

- [ ] Cursor move event resolves `PlacementValidator` reference once (cached).
- [ ] `CanPlace` called on cell-change only; no per-frame allocation.
- [ ] Result tuple (bool + reason) forwarded to tint + tooltip consumers.
- [ ] `GridManager.GetCell(x,y)` only; no `cellArray` touch.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| | | | |

## 10. Lessons Learned

- 

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

`CursorManager` calls `PlacementValidator.CanPlace` once per cursor cell change, caches the validator ref on `Start`, and forwards the resulting `PlacementResult` to tint + tooltip consumers via an internal `event Action<PlacementResult>`. No `FindObjectOfType` per frame; no `cellArray` touch.

### §Acceptance

- [ ] `CursorManager` exposes `[SerializeField] private PlacementValidator placementValidator` and resolves it on `Start` (with `FindObjectOfType` fallback when Inspector ref absent — runs once, not per frame).
- [ ] Cell-delta throttle: `CanPlace` invoked only when `mouseCell.x` or `mouseCell.y` differs from cached `_lastCellX` / `_lastCellY`.
- [ ] `PlacementResult` (struct: `IsAllowed` + `Reason` + `Detail`) forwarded via `public event Action<PlacementResult> PlacementResultChanged`; subscribers in TECH-758/759/760 read the struct.
- [ ] `GridManager.GetCell(x,y)` is the only grid read path; no `cellArray` token in `CursorManager.cs`.
- [ ] `OnDestroy` clears subscribers (`PlacementResultChanged = null`) to prevent stale fan-out after scene unload.
- [ ] Scene wiring evidence block emitted (target `Assets/Scenes/MainScene.unity`, parent `Game Managers`, component `CursorManager`, serialized field `placementValidator → PlacementValidator (fileID 4200001802)`, `unity_events: empty`, `compile_check: passed`).

### §Test Blueprint

| test_name | inputs | expected | harness |
|---|---|---|---|
| `CursorManager_ValidatorRefCachedOnStart` | Scene loads with `CursorManager` + `PlacementValidator` on `Game Managers`; ref assigned in Inspector | Reflection read of `placementValidator` non-null after `Start`; counter hook on `FindObjectOfType<PlacementValidator>` fires zero times in `Update` over 60 frames | unity-batch (EditMode) |
| `CursorManager_CellDeltaThrottle` | Drive `Update` 60 frames with `mouseCell` fixed at `(3, 5)` | Spy `PlacementValidator.CanPlace` invocation count == 1 | unity-batch (EditMode) |
| `CursorManager_CellChangeTriggersValidator` | Drive `Update` with cell sequence `(3,5)→(3,6)→(4,6)` | Spy receives 3 `CanPlace` calls with matching `(cellX, cellY)` args | unity-batch (EditMode) |
| `CursorManager_ForwardsResultToSubscribers` | Subscribe a stub handler to `PlacementResultChanged`; inject valid + invalid `PlacementResult` via spy | Stub captures `IsAllowed` and `Reason` matching injected struct | unity-batch (EditMode) |
| `CursorManager_OnDestroyClearsSubscribers` | Subscribe stub; call `OnDestroy`; raise event | Zero invocations on stub after `OnDestroy` | unity-batch (EditMode) |
| `CursorManager_NoCellArrayAccess` | `git grep -n cellArray Assets/Scripts/Managers/GameManagers/CursorManager.cs` | Zero matches | manual (grep guard) |
| Play Mode smoke: preview reacts to validator | Operator loads `MainScene`, moves cursor over valid + occupied cells | Ghost tint toggles per cell; no per-frame stutter (visual) | manual (folded into TECH-761 checklist) |

### §Examples

| Input (cursor event) | Validator call | `PlacementResult` | Event payload to subscribers |
|---|---|---|---|
| Cursor enters cell `(3, 5)` — first move | `CanPlace(assetId=42, 3, 5, 0, Zone.ZoneType.Residential)` | `Allowed()` | `IsAllowed=true, Reason=None` |
| Same `Update` frame, still `(3, 5)` | skipped (cell delta) | n/a | no event raised |
| Cursor moves to `(3, 6)` (occupied) | `CanPlace(42, 3, 6, 0, Residential)` | `Fail(Occupied, "Cell already has a building.")` | `IsAllowed=false, Reason=Occupied` |
| Cursor exits grid bounds | n/a (early `mouseCell == null` return — sets `previewInstance.SetActive(false)` and resets `_lastCellX/_lastCellY` to `int.MinValue`) | n/a | no event raised this frame |
| Catalog unseeded dev snapshot | `CanPlace(42, …)` hits missing row | `Allowed("Catalog row missing; skipped legality (unseeded snapshot).")` | `IsAllowed=true, Reason=None` |
| Scene unload (`OnDestroy`) | n/a | n/a | subscribers cleared; no fan-out post-unload |

### §Mechanical Steps

#### Step 1 — Add SerializeField + cell-delta state to CursorManager

**Goal:** introduce private `_lastCellX` / `_lastCellY` cell-delta fields, a `[SerializeField] private PlacementValidator placementValidator` Inspector reference, and `public event Action<PlacementResult> PlacementResultChanged`.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
  using UnityEngine;
  using UnityEngine.EventSystems;
  using Territory.Core;
  using Territory.Zones;
  using Territory.Buildings;
  ```
  **after**:
  ```csharp
  using System;
  using UnityEngine;
  using UnityEngine.EventSystems;
  using Territory.Core;
  using Territory.Zones;
  using Territory.Buildings;
  ```

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
      private GameObject previewInstance;
      public GridManager gridManager;
  ```
  **after**:
  ```csharp
      private GameObject previewInstance;
      public GridManager gridManager;
      [SerializeField] private PlacementValidator placementValidator;
      public event Action<PlacementResult> PlacementResultChanged;
      private int _lastCellX = int.MinValue;
      private int _lastCellY = int.MinValue;
      private int _currentAssetId;
      private int _currentRotation;
      private Zone.ZoneType _currentZoneType = Zone.ZoneType.None;
  ```

**invariant_touchpoints:**
- id: "perf — no FindObjectOfType per frame"
  gate: "git grep -n FindObjectOfType<PlacementValidator> Assets/Scripts/Managers/GameManagers/CursorManager.cs | grep -v Start"
  expected: "none"

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if compile-check fails, re-open this Step before touching Step 2; never push the missing field forward.

**MCP hints:** `plan_digest_resolve_anchor`, `glossary_lookup`, `invariant_preflight`.

#### Step 2 — Resolve validator ref on Start (one-time fallback)

**Goal:** lazy-resolve `placementValidator` on `Start` when Inspector ref absent — single `FindObjectOfType` call, not per frame.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
          cachedUIManager = FindObjectOfType<UIManager>();
          Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
  ```
  **after**:
  ```csharp
          cachedUIManager = FindObjectOfType<UIManager>();
          if (placementValidator == null)
              placementValidator = FindObjectOfType<PlacementValidator>();
          Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
  ```

**invariant_touchpoints:**
- id: "perf — Start runs once per scene load"
  gate: "git grep -nE 'FindObjectOfType<PlacementValidator>' Assets/Scripts/Managers/GameManagers/CursorManager.cs | wc -l"
  expected: "pass"   # exactly 1 hit, scoped to Start

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if `wc -l` returns anything other than `1`, re-open Step 2; do NOT add a second resolve site.

**MCP hints:** `plan_digest_resolve_anchor`, `unity_bridge_command` (`get_compilation_status`).

#### Step 3 — Cell-delta gate + validator call inside Update

**Goal:** when `mouseCell` is in-bounds, compare to `_lastCellX/_lastCellY`; on delta, call `CanPlace` and raise `PlacementResultChanged`. Reset cached cell on out-of-bounds branch.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
                  CityCell cell = gridManager.GetCell((int)gridPosition.x, (int)gridPosition.y);
                  if (cell == null)
                  {
                      previewInstance.SetActive(false);
                  }
                  else
                  {
                      Vector2 newWorldPos = gridManager.GetBuildingPlacementWorldPosition(gridPosition, buildingSize);
                      IBuilding selectedBuilding = cachedUIManager?.GetSelectedBuilding();
                      if (selectedBuilding is WaterPlant)
                          newWorldPos.y += gridManager.tileHeight / 4f;
                      previewInstance.transform.position = newWorldPos;
                  }
  ```
  **after**:
  ```csharp
                  CityCell cell = gridManager.GetCell((int)gridPosition.x, (int)gridPosition.y);
                  if (cell == null)
                  {
                      previewInstance.SetActive(false);
                      _lastCellX = int.MinValue;
                      _lastCellY = int.MinValue;
                  }
                  else
                  {
                      Vector2 newWorldPos = gridManager.GetBuildingPlacementWorldPosition(gridPosition, buildingSize);
                      IBuilding selectedBuilding = cachedUIManager?.GetSelectedBuilding();
                      if (selectedBuilding is WaterPlant)
                          newWorldPos.y += gridManager.tileHeight / 4f;
                      previewInstance.transform.position = newWorldPos;

                      if (placementValidator != null && (cell.x != _lastCellX || cell.y != _lastCellY))
                      {
                          _lastCellX = cell.x;
                          _lastCellY = cell.y;
                          PlacementResult result = placementValidator.CanPlace(
                              _currentAssetId,
                              cell.x,
                              cell.y,
                              _currentRotation,
                              _currentZoneType);
                          PlacementResultChanged?.Invoke(result);
                      }
                  }
  ```

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
              CityCell mouseCell = gridManager.GetMouseGridCell(mousePosition2);
              if (mouseCell == null)
              {
                  previewInstance.SetActive(false);
                  UpdateCursorForUIHover();
                  return;
              }
  ```
  **after**:
  ```csharp
              CityCell mouseCell = gridManager.GetMouseGridCell(mousePosition2);
              if (mouseCell == null)
              {
                  previewInstance.SetActive(false);
                  _lastCellX = int.MinValue;
                  _lastCellY = int.MinValue;
                  UpdateCursorForUIHover();
                  return;
              }
  ```

**invariant_touchpoints:**
- id: "Unity invariant #5 — GridManager.GetCell only; no cellArray"
  gate: "git grep -n cellArray Assets/Scripts/Managers/GameManagers/CursorManager.cs"
  expected: "none"
- id: "perf — no per-frame allocation in hot path"
  gate: "git grep -nE 'new (List|Dictionary|HashSet|Array)' Assets/Scripts/Managers/GameManagers/CursorManager.cs"
  expected: "unchanged"   # current count = 0 inside Update body

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if `cellArray` matches appear, revert this Step and route to `/plan-review`; the cell-delta gate must NEVER bypass `GridManager.GetCell`.

**MCP hints:** `plan_digest_resolve_anchor`, `glossary_lookup` (`HeightMap`, `cellArray`), `invariant_preflight`, `rule_content unity-invariants`.

#### Step 4 — Clear subscribers on OnDestroy + RemovePreview

**Goal:** zero stale fan-out after scene unload or preview clear; reset `_lastCellX/_lastCellY` so re-spawned previews trigger a fresh validator call.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
      public void RemovePreview()
      {
          currentRoadGhostPrefab = null;
          if (previewInstance != null)
          {
              Destroy(previewInstance);
              previewInstance = null;
          }
      }
  ```
  **after**:
  ```csharp
      public void RemovePreview()
      {
          currentRoadGhostPrefab = null;
          if (previewInstance != null)
          {
              Destroy(previewInstance);
              previewInstance = null;
          }
          _lastCellX = int.MinValue;
          _lastCellY = int.MinValue;
      }

      private void OnDestroy()
      {
          PlacementResultChanged = null;
      }
  ```

**invariant_touchpoints:**
- id: "lifecycle — no stale event fan-out"
  gate: "git grep -n 'PlacementResultChanged = null' Assets/Scripts/Managers/GameManagers/CursorManager.cs"
  expected: "pass"

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if `OnDestroy` body missing the null-out line, re-open Step 4; do NOT advance to test authoring.

**MCP hints:** `plan_digest_resolve_anchor`, `unity_bridge_command` (`get_compilation_status`).

#### Step 5 — EditMode tests under Assets/Tests/EditMode/GridAssetCatalog

**Goal:** create `CursorPlacementPreviewTests.cs` covering ref caching, cell-delta throttle, event fan-out, OnDestroy clear, and `cellArray` grep guard. Spy uses a test double (subclass `PlacementValidator` overriding `CanPlace`, or interface-extract — pick subclass to stay within Stage scope; record in Decision Log).

**Edits:**

- `Assets/Tests/EditMode/GridAssetCatalog/CursorPlacementPreviewTests.cs` — **create** (verbatim test skeleton):
  ```csharp
  using NUnit.Framework;
  using UnityEngine;
  using Territory.Core;
  using Territory.UI;
  using Territory.Zones;

  namespace Territory.Tests.GridAssetCatalog
  {
      public class CursorPlacementPreviewTests
      {
          [Test]
          public void ValidatorRef_Cached_OnStart_NoFindObjectOfTypePerFrame() { Assert.Inconclusive("authored in /implement"); }

          [Test]
          public void CellDelta_SameCell_DoesNotInvokeCanPlace() { Assert.Inconclusive("authored in /implement"); }

          [Test]
          public void CellDelta_NewCell_InvokesCanPlaceOnce() { Assert.Inconclusive("authored in /implement"); }

          [Test]
          public void PlacementResultChanged_FiresWithStructPayload() { Assert.Inconclusive("authored in /implement"); }

          [Test]
          public void OnDestroy_ClearsSubscribers_NoStaleFanout() { Assert.Inconclusive("authored in /implement"); }
      }
  }
  ```

**invariant_touchpoints:**
- id: "test placement — Assets/Tests/EditMode tree only"
  gate: "test -f Assets/Tests/EditMode/GridAssetCatalog/CursorPlacementPreviewTests.cs && echo OK"
  expected: "pass"

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if file create fails or path missing assembly definition coverage, re-open Step 5 and reuse existing `Assets/Tests/EditMode/GridAssetCatalog/` `.asmdef` (already present from Stage 2.2 — `GridAssetCatalogParseTests.cs` proves it).

**MCP hints:** `plan_digest_verify_paths`, `unity_bridge_command` (`run_test_mode`), `glossary_lookup` (`unity-batch`).

#### Step 6 — Scene Wiring (MainScene assign placementValidator on CursorManager)

**Goal:** wire the new `placementValidator` SerializeField on the existing `CursorManager` GameObject in `Assets/Scenes/MainScene.unity` to the existing `PlacementValidator` MonoBehaviour (fileID `4200001802`).

**Edits (bridge — preferred):**

```yaml
- kind: open_scene
  args: { scene_path: "Assets/Scenes/MainScene.unity" }
- kind: assign_serialized_field
  args:
    target_object: "Game Managers/CursorManager"
    component: "Territory.UI.CursorManager"
    field: "placementValidator"
    value_object_path: "Game Managers/PlacementValidator"
- kind: save_scene
  args: { scene_path: "Assets/Scenes/MainScene.unity" }
```

**Edits (text-edit fallback when bridge `assign_serialized_field` unavailable):**

- `Assets/Scenes/MainScene.unity` — **before**:
  ```yaml
    m_Script: {fileID: 11500000, guid: 14cc6fe9995e74810ab961005373c97f, type: 3}
    m_Name: 
    m_EditorClassIdentifier: 
    cursorTexture: {fileID: 2800000, guid: 4cc8ba484070847328594d0a49125900, type: 3}
    bulldozerTexture: {fileID: 2800000, guid: 86facc3aeb3f249ac86535afa0beb740, type: 3}
    detailsTexture: {fileID: 2800000, guid: 4cc8ba484070847328594d0a49125900, type: 3}
    hotSpot: {x: 0, y: 0}
    gridManager: {fileID: 1880690383}
  ```
  **after**:
  ```yaml
    m_Script: {fileID: 11500000, guid: 14cc6fe9995e74810ab961005373c97f, type: 3}
    m_Name: 
    m_EditorClassIdentifier: 
    cursorTexture: {fileID: 2800000, guid: 4cc8ba484070847328594d0a49125900, type: 3}
    bulldozerTexture: {fileID: 2800000, guid: 86facc3aeb3f249ac86535afa0beb740, type: 3}
    detailsTexture: {fileID: 2800000, guid: 4cc8ba484070847328594d0a49125900, type: 3}
    hotSpot: {x: 0, y: 0}
    gridManager: {fileID: 1880690383}
    placementValidator: {fileID: 4200001802}
  ```

**Evidence (paste verbatim into §Acceptance row "scene wiring evidence" + opus-code-review output):**

```
Scene wiring:
  scene: Assets/Scenes/MainScene.unity
  parent: Game Managers
  component: CursorManager (script guid 14cc6fe9995e74810ab961005373c97f)
  serialized_fields:
    placementValidator: "PlacementValidator (fileID 4200001802)"
  unity_events: empty
  compile_check: passed
```

**invariant_touchpoints:**
- id: "scene wiring — diff must include MainScene.unity"
  gate: "git diff --name-only HEAD -- Assets/Scenes/MainScene.unity"
  expected: "pass"   # path appears in diff after edit
- id: "MonoBehaviour script guid stable"
  gate: "grep -n 'guid: 14cc6fe9995e74810ab961005373c97f' Assets/Scripts/Managers/GameManagers/CursorManager.cs.meta"
  expected: "pass"

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if `git diff` shows zero `MainScene.unity` change after the bridge or text edit, re-open Step 6; per [`unity-scene-wiring.md`](../../ia/rules/unity-scene-wiring.md) the Stage cannot ship without the scene delta.

**MCP hints:** `unity_bridge_command` (`open_scene`, `assign_serialized_field`, `save_scene`), `find_gameobject` (verify parent `Game Managers/PlacementValidator` resolves), `get_compilation_status`, `plan_digest_render_literal`.

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._

## Open Questions (resolve before / during implementation)

1. Cell-change event source — existing cursor event or new event needed?
2. Validator reference — cache on `CursorManager` or inject via DI / static getter?
3. Result propagation — event, direct call, or both?

---
