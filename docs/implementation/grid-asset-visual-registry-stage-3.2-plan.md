# grid-asset-visual-registry — Stage 3.2 Plan Digest

Compiled 2026-04-23 from 5 task spec(s).

---

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

---
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

Subscribe `CursorManager` to its own `PlacementResultChanged` event and apply a soft-green tint (`new Color(0.4f, 1f, 0.4f, 0.5f)`) to `previewInstance.GetComponentInChildren<SpriteRenderer>().color` when `result.IsAllowed == true`. Capture `_originalPreviewColor` on each preview spawn, restore on `RemovePreview`. Idempotent via `_lastTintState` field. `sortingOrder` and `sortingLayerID` untouched.

### §Acceptance

- [ ] Private `ApplyPreviewTint(PlacementResult)` method on `CursorManager` writes `SpriteRenderer.color = PreviewTintGreen` only when `result.IsAllowed == true` AND `_lastTintState != PreviewTintState.Valid`.
- [ ] `_originalPreviewColor` (Color, default `new Color(1, 1, 1, 0.5f)` per existing `ShowBuildingPreview` line 99) captured immediately after each `previewInstance = Instantiate(...)` site (3 sites: line 88, line 145).
- [ ] `RemovePreview` restores no color (preview is destroyed); `_lastTintState` resets to `None`; next preview re-captures `_originalPreviewColor`.
- [ ] `sortingOrder` and `sortingLayerID` untouched by `ApplyPreviewTint` (test asserts pre == post).
- [ ] Idempotent: 5 consecutive `Allowed()` results trigger exactly 1 `SpriteRenderer.color =` write (counter spy via subclass test double).
- [ ] No new `Collider2D` on world tiles or preview instance — existing `Destroy(col)` loops at lines 109-112 + 149-150 stay intact (no replacement, no enable).

### §Test Blueprint

| test_name | inputs | expected | harness |
|---|---|---|---|
| `CursorTint_AppliesGreenOnValid` | Spawn preview, raise `PlacementResultChanged(PlacementResult.Allowed())` | `previewInstance` SpriteRenderer `color` equals `new Color(0.4f, 1f, 0.4f, 0.5f)` | unity-batch (EditMode) |
| `CursorTint_PreservesSortingOrder` | Snapshot `sortingOrder` + `sortingLayerID` before, raise valid result, snapshot after | both fields unchanged | unity-batch (EditMode) |
| `CursorTint_Idempotent` | Raise `Allowed()` 5 times consecutively | `SpriteRenderer.color` setter called once (counter spy on `MaterialPropertyBlock` swap or test double subclass) | unity-batch (EditMode) |
| `CursorTint_NoColliderRegression` | After tint apply, query `previewInstance.GetComponentsInChildren<Collider2D>()` | length == 0 | unity-batch (EditMode) |
| `CursorTint_NoTintOnNullPreview` | Raise `Allowed()` while `previewInstance == null` | no exception; no state mutation | unity-batch (EditMode) |
| Play Mode smoke: green visible on valid cell | Operator moves cursor onto a valid residential cell | Ghost sprite visibly soft-green; no layer pop-in | manual (folded into TECH-761) |

### §Examples

| Input (event payload) | `_lastTintState` (before) | Action | `_lastTintState` (after) |
|---|---|---|---|
| `PlacementResult.Allowed()` | `None` | capture `_originalPreviewColor` (if not yet); `SpriteRenderer.color = new Color(0.4f, 1f, 0.4f, 0.5f)` | `Valid` |
| `PlacementResult.Allowed()` | `Valid` | no-op | `Valid` |
| `PlacementResult.Fail(Occupied, …)` | `Valid` | delegated to TECH-759 (red branch sets `Invalid`) | `Invalid` |
| `RemovePreview()` invoked | `Valid` | preview destroyed; `_lastTintState = PreviewTintState.None` | `None` |
| New `ShowBuildingPreview` call | `None` | re-capture `_originalPreviewColor` after `Instantiate` | `None` |

### §Mechanical Steps

#### Step 1 — Add tint-state enum + color constant + cached _originalPreviewColor

**Goal:** introduce `private enum PreviewTintState { None, Valid, Invalid }`, a `private PreviewTintState _lastTintState`, the green `static readonly Color PreviewTintGreen`, and `private Color _originalPreviewColor` field on `CursorManager`.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
      private UIManager cachedUIManager;
  ```
  **after**:
  ```csharp
      private UIManager cachedUIManager;

      private enum PreviewTintState { None, Valid, Invalid }
      private PreviewTintState _lastTintState = PreviewTintState.None;
      private Color _originalPreviewColor = new Color(1f, 1f, 1f, 0.5f);
      private static readonly Color PreviewTintGreen = new Color(0.4f, 1f, 0.4f, 0.5f);
  ```

**invariant_touchpoints:**
- id: "no new MonoBehaviour, no SerializeField"
  gate: "git grep -nE '\\[SerializeField\\]' Assets/Scripts/Managers/GameManagers/CursorManager.cs | wc -l"
  expected: "unchanged"   # TECH-757 added 1 SerializeField; this Step adds 0 more

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if extra `[SerializeField]` count appears, re-open this Step; this Task ships internal tint state only.

**MCP hints:** `plan_digest_resolve_anchor`, `glossary_lookup`.

#### Step 2 — Capture _originalPreviewColor at each Instantiate site

**Goal:** snapshot original `SpriteRenderer.color` immediately after the existing `Instantiate(buildingPrefab)` (line 88) and `Instantiate(roadPrefab)` (line 145) sites, before the existing transparency write.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
              if (spriteRenderer != null)
              {
                  spriteRenderer.color = new Color(1, 1, 1, 0.5f); // Set transparency
                  // Set a high sorting order to ensure preview appears on top
                  spriteRenderer.sortingOrder = 10000;
              }
  ```
  **after**:
  ```csharp
              if (spriteRenderer != null)
              {
                  _originalPreviewColor = new Color(1f, 1f, 1f, 0.5f);
                  spriteRenderer.color = _originalPreviewColor; // Set transparency
                  // Set a high sorting order to ensure preview appears on top
                  spriteRenderer.sortingOrder = 10000;
                  _lastTintState = PreviewTintState.None;
              }
  ```

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
                      SpriteRenderer sr = previewInstance.GetComponent<SpriteRenderer>();
                      if (sr == null) sr = previewInstance.GetComponentInChildren<SpriteRenderer>();
                      if (sr != null) sr.color = new Color(1, 1, 1, 0.5f);
                      foreach (var col in previewInstance.GetComponentsInChildren<Collider2D>())
                          Destroy(col);
  ```
  **after**:
  ```csharp
                      SpriteRenderer sr = previewInstance.GetComponent<SpriteRenderer>();
                      if (sr == null) sr = previewInstance.GetComponentInChildren<SpriteRenderer>();
                      if (sr != null)
                      {
                          _originalPreviewColor = new Color(1f, 1f, 1f, 0.5f);
                          sr.color = _originalPreviewColor;
                      }
                      _lastTintState = PreviewTintState.None;
                      foreach (var col in previewInstance.GetComponentsInChildren<Collider2D>())
                          Destroy(col);
  ```

**invariant_touchpoints:**
- id: "Stage Exit — no new Collider2D on world tiles"
  gate: "git grep -nE 'AddComponent<Collider2D>|new BoxCollider2D|new CircleCollider2D' Assets/Scripts/Managers/GameManagers/CursorManager.cs"
  expected: "none"
- id: "preserve sortingOrder write"
  gate: "git grep -n 'spriteRenderer.sortingOrder = 10000;' Assets/Scripts/Managers/GameManagers/CursorManager.cs"
  expected: "pass"

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if either grep diverges, revert and re-anchor before Step 3; the Stage Exit collider invariant is non-negotiable.

**MCP hints:** `plan_digest_resolve_anchor`, `rule_content unity-invariants`, `invariant_preflight`.

#### Step 3 — Hook ApplyPreviewTint to PlacementResultChanged in Start

**Goal:** subscribe to `PlacementResultChanged` once (in `Start`); private handler `ApplyPreviewTint(PlacementResult result)` switches on `result.IsAllowed`. Valid branch lives here; invalid branch reserved for TECH-759.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
          cachedUIManager = FindObjectOfType<UIManager>();
          if (placementValidator == null)
              placementValidator = FindObjectOfType<PlacementValidator>();
          Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
  ```
  **after**:
  ```csharp
          cachedUIManager = FindObjectOfType<UIManager>();
          if (placementValidator == null)
              placementValidator = FindObjectOfType<PlacementValidator>();
          PlacementResultChanged += ApplyPreviewTint;
          Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
  ```

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
      private bool IsPointerOverUI()
      {
          return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
      }
  ```
  **after**:
  ```csharp
      private bool IsPointerOverUI()
      {
          return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
      }

      private void ApplyPreviewTint(PlacementResult result)
      {
          if (previewInstance == null) return;
          SpriteRenderer renderer = previewInstance.GetComponent<SpriteRenderer>();
          if (renderer == null) renderer = previewInstance.GetComponentInChildren<SpriteRenderer>();
          if (renderer == null) return;

          if (result.IsAllowed)
          {
              if (_lastTintState == PreviewTintState.Valid) return;
              renderer.color = PreviewTintGreen;
              _lastTintState = PreviewTintState.Valid;
          }
          // Invalid branch authored in TECH-759.
      }
  ```

**invariant_touchpoints:**
- id: "idempotent tint write"
  gate: "git grep -nE 'if \\(_lastTintState == PreviewTintState\\.Valid\\) return;' Assets/Scripts/Managers/GameManagers/CursorManager.cs"
  expected: "pass"
- id: "sortingOrder/sortingLayerID untouched in handler"
  gate: "git grep -nE 'sortingOrder|sortingLayerID' Assets/Scripts/Managers/GameManagers/CursorManager.cs | grep -v 'sortingOrder = 10000;' | grep -v 'sr.sortingOrder = sortingOrder'"
  expected: "none"

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if sorting fields appear inside `ApplyPreviewTint`, revert that line; sorting writes belong only to existing `ShowBuildingPreview` and road-ghost branches.

**MCP hints:** `plan_digest_resolve_anchor`, `glossary_lookup` (`SpriteRenderer`), `rule_content unity-invariants`.

#### Step 4 — EditMode test for valid tint path

**Goal:** add green-tint test cases under `Assets/Tests/EditMode/GridAssetCatalog/CursorPlacementPreviewTests.cs` (created in TECH-757 Step 5). Replace 1 inconclusive stub with a green-tint assertion + sortingOrder snapshot.

**Edits:**

- `Assets/Tests/EditMode/GridAssetCatalog/CursorPlacementPreviewTests.cs` — **before**:
  ```csharp
          [Test]
          public void PlacementResultChanged_FiresWithStructPayload() { Assert.Inconclusive("authored in /implement"); }
  ```
  **after**:
  ```csharp
          [Test]
          public void PlacementResultChanged_FiresWithStructPayload() { Assert.Inconclusive("authored in /implement"); }

          [Test]
          public void ValidResult_AppliesGreenTint_PreservesSortingOrder() { Assert.Inconclusive("authored in /implement (TECH-758)"); }

          [Test]
          public void ValidResult_Idempotent_OneColorWritePerTransition() { Assert.Inconclusive("authored in /implement (TECH-758)"); }
  ```

**invariant_touchpoints:**
- id: "test placement under Assets/Tests/EditMode/GridAssetCatalog"
  gate: "test -f Assets/Tests/EditMode/GridAssetCatalog/CursorPlacementPreviewTests.cs && echo OK"
  expected: "pass"

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if file does not exist, halt and route to TECH-757 Step 5 — TECH-758 cannot file tests before TECH-757 lands the host file.

**MCP hints:** `plan_digest_verify_paths`, `unity_bridge_command` (`run_test_mode`).

---
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

Extend `CursorManager.ApplyPreviewTint` (added by TECH-758) with the invalid branch: write soft-red `new Color(1f, 0.4f, 0.4f, 0.5f)` when `result.IsAllowed == false`, gate writes via `_lastTintState != PreviewTintState.Invalid` for idempotency, and raise a new `public event Action<PlacementFailReason> PlacementReasonChanged` carrying the enum value (no enum→string mapping here — that lives in TECH-760).

### §Acceptance

- [ ] Invalid branch in `ApplyPreviewTint` writes `SpriteRenderer.color = PreviewTintRed` (`new Color(1f, 0.4f, 0.4f, 0.5f)`) when `result.IsAllowed == false`.
- [ ] Idempotent: 5 consecutive `Fail(Occupied)` results trigger exactly 1 color write (gated via `_lastTintState`).
- [ ] `public event Action<PlacementFailReason> PlacementReasonChanged` declared on `CursorManager`; raised on every `PlacementResultChanged` (both valid and invalid: `result.Reason` value forwarded — including `None` for the valid branch so TECH-760 can hide tooltip).
- [ ] All 5 invalid `PlacementFailReason` values (`Footprint, Zoning, Locked, Unaffordable, Occupied`) plus `None` route through the event without a `switch` statement on the enum (one event, no enum-to-color map — single red constant).
- [ ] `sortingOrder` and `sortingLayerID` untouched in invalid branch (test asserts pre == post for an invalid transition).
- [ ] No new `Collider2D` on world tiles or preview instance (existing collider-destroy loops at lines 109-112, 149-150 untouched).

### §Test Blueprint

| test_name | inputs | expected | harness |
|---|---|---|---|
| `CursorTint_AppliesRedOnInvalid` | Spawn preview, raise `PlacementResultChanged(PlacementResult.Fail(PlacementFailReason.Occupied, "x"))` | `previewInstance` SpriteRenderer `color` equals `new Color(1f, 0.4f, 0.4f, 0.5f)` | unity-batch (EditMode) |
| `CursorTint_RedPreservesSortingOrder` | Snapshot before, raise invalid, snapshot after | `sortingOrder` + `sortingLayerID` unchanged | unity-batch (EditMode) |
| `CursorTint_ForwardsAllReasonValues` | Iterate `Enum.GetValues(typeof(PlacementFailReason))` (6 values), raise each | Subscribed `PlacementReasonChanged` handler receives matching enum value 6 times in order | unity-batch (EditMode) |
| `CursorTint_RedToGreenTransition` | Raise `Fail(Occupied)` then `Allowed()` | `SpriteRenderer.color` sequence: red, then green; event payloads: `Occupied`, `None` | unity-batch (EditMode) |
| `CursorTint_InvalidIdempotent_OneWritePerTransition` | Raise `Fail(Occupied)` 5 times | `SpriteRenderer.color` setter invoked once (counter spy) | unity-batch (EditMode) |
| `CursorTint_NoColliderRegression_Invalid` | After red tint, query `previewInstance.GetComponentsInChildren<Collider2D>()` | length == 0 | unity-batch (EditMode) |
| `CursorTint_NoSwitchOnPlacementFailReason_InCursorManager` | `git grep -nE 'switch \\(.*Reason.*\\)' Assets/Scripts/Managers/GameManagers/CursorManager.cs` | zero matches | manual (grep guard) |
| Play Mode smoke: each reason → red ghost + reason event | Operator triggers each `PlacementFailReason` via cursor moves | Ghost red on each invalid branch; tooltip (TECH-760) shows matching string | manual (folded into TECH-761) |

### §Examples

| Input (event payload) | `_lastTintState` (before) | Action | `PlacementReasonChanged` payload |
|---|---|---|---|
| `Fail(Occupied, …)` | `None` | `SpriteRenderer.color = PreviewTintRed`; `_lastTintState = Invalid` | `Occupied` |
| `Fail(Footprint, …)` | `Valid` | switch green → red; `_lastTintState = Invalid` | `Footprint` |
| `Fail(Zoning, …)` | `Invalid` | no color write (idempotent); event still raised with new enum value | `Zoning` |
| `Fail(Unaffordable, …)` | `Invalid` | no color write; event with new value | `Unaffordable` |
| `Fail(Locked, …)` | `None` | red write; `_lastTintState = Invalid` | `Locked` |
| `Allowed()` (after invalid) | `Invalid` | delegated to TECH-758 valid branch (green write) | `None` |
| `RemovePreview()` invoked | any | preview destroyed (TECH-758 already resets `_lastTintState`) | n/a (no event raised here) |

### §Mechanical Steps

#### Step 1 — Add red color constant + reason event

**Goal:** add `static readonly Color PreviewTintRed` and `public event Action<PlacementFailReason> PlacementReasonChanged` to `CursorManager`.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
      private static readonly Color PreviewTintGreen = new Color(0.4f, 1f, 0.4f, 0.5f);
  ```
  **after**:
  ```csharp
      private static readonly Color PreviewTintGreen = new Color(0.4f, 1f, 0.4f, 0.5f);
      private static readonly Color PreviewTintRed = new Color(1f, 0.4f, 0.4f, 0.5f);
      public event Action<PlacementFailReason> PlacementReasonChanged;
  ```

**invariant_touchpoints:**
- id: "no PlacementFailReason switch in CursorManager (table-driven map lives in TECH-760 / UIManager)"
  gate: "git grep -nE 'switch \\(.*Reason.*\\)' Assets/Scripts/Managers/GameManagers/CursorManager.cs"
  expected: "none"

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if a `switch` on `PlacementFailReason` appears inside `CursorManager`, revert and route the mapping to TECH-760 surface; this Task only forwards the enum.

**MCP hints:** `plan_digest_resolve_anchor`, `glossary_lookup` (`PlacementFailReason`).

#### Step 2 — Extend ApplyPreviewTint with invalid branch + raise reason event

**Goal:** complete the `else` branch of `ApplyPreviewTint`; raise `PlacementReasonChanged` exactly once per `PlacementResultChanged` (covers both valid and invalid — `None` for valid lets TECH-760 hide tooltip).

**Edits:**

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
      private void ApplyPreviewTint(PlacementResult result)
      {
          if (previewInstance == null) return;
          SpriteRenderer renderer = previewInstance.GetComponent<SpriteRenderer>();
          if (renderer == null) renderer = previewInstance.GetComponentInChildren<SpriteRenderer>();
          if (renderer == null) return;

          if (result.IsAllowed)
          {
              if (_lastTintState == PreviewTintState.Valid) return;
              renderer.color = PreviewTintGreen;
              _lastTintState = PreviewTintState.Valid;
          }
          // Invalid branch authored in TECH-759.
      }
  ```
  **after**:
  ```csharp
      private void ApplyPreviewTint(PlacementResult result)
      {
          if (previewInstance == null) return;
          SpriteRenderer renderer = previewInstance.GetComponent<SpriteRenderer>();
          if (renderer == null) renderer = previewInstance.GetComponentInChildren<SpriteRenderer>();
          if (renderer == null) return;

          if (result.IsAllowed)
          {
              if (_lastTintState != PreviewTintState.Valid)
              {
                  renderer.color = PreviewTintGreen;
                  _lastTintState = PreviewTintState.Valid;
              }
          }
          else
          {
              if (_lastTintState != PreviewTintState.Invalid)
              {
                  renderer.color = PreviewTintRed;
                  _lastTintState = PreviewTintState.Invalid;
              }
          }

          PlacementReasonChanged?.Invoke(result.Reason);
      }
  ```

**invariant_touchpoints:**
- id: "Stage Exit — no new Collider2D on world tiles"
  gate: "git grep -nE 'AddComponent<Collider2D>|new BoxCollider2D|new CircleCollider2D' Assets/Scripts/Managers/GameManagers/CursorManager.cs"
  expected: "none"
- id: "sortingOrder/sortingLayerID untouched in handler"
  gate: "git grep -nE 'sortingOrder|sortingLayerID' Assets/Scripts/Managers/GameManagers/CursorManager.cs | grep -v 'sortingOrder = 10000;' | grep -v 'sr.sortingOrder = sortingOrder'"
  expected: "none"

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if either grep diverges, revert ApplyPreviewTint and re-anchor before Step 3.

**MCP hints:** `plan_digest_resolve_anchor`, `rule_content unity-invariants`, `glossary_lookup` (`SpriteRenderer`).

#### Step 3 — Clear PlacementReasonChanged subscribers in OnDestroy

**Goal:** extend the `OnDestroy` (added by TECH-757 Step 4) to null out the new event so subscribers do not receive stale fan-out post scene unload.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
      private void OnDestroy()
      {
          PlacementResultChanged = null;
      }
  ```
  **after**:
  ```csharp
      private void OnDestroy()
      {
          PlacementResultChanged = null;
          PlacementReasonChanged = null;
      }
  ```

**invariant_touchpoints:**
- id: "lifecycle — both events cleared"
  gate: "git grep -nE 'PlacementResultChanged = null;|PlacementReasonChanged = null;' Assets/Scripts/Managers/GameManagers/CursorManager.cs | wc -l"
  expected: "pass"   # exactly 2 lines

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if `wc -l` returns anything other than `2`, re-open Step 3.

**MCP hints:** `plan_digest_resolve_anchor`, `unity_bridge_command` (`get_compilation_status`).

#### Step 4 — EditMode tests for invalid tint + reason fan-out

**Goal:** flip 3 inconclusive stubs in `Assets/Tests/EditMode/GridAssetCatalog/CursorPlacementPreviewTests.cs` into red-tint, sortingOrder, and reason-fan-out cases. Adds 2 new tests.

**Edits:**

- `Assets/Tests/EditMode/GridAssetCatalog/CursorPlacementPreviewTests.cs` — **before**:
  ```csharp
          [Test]
          public void ValidResult_Idempotent_OneColorWritePerTransition() { Assert.Inconclusive("authored in /implement (TECH-758)"); }
  ```
  **after**:
  ```csharp
          [Test]
          public void ValidResult_Idempotent_OneColorWritePerTransition() { Assert.Inconclusive("authored in /implement (TECH-758)"); }

          [Test]
          public void InvalidResult_AppliesRedTint_PreservesSortingOrder() { Assert.Inconclusive("authored in /implement (TECH-759)"); }

          [Test]
          public void InvalidResult_Idempotent_OneColorWritePerTransition() { Assert.Inconclusive("authored in /implement (TECH-759)"); }

          [Test]
          public void PlacementReasonChanged_ForwardsAllSixEnumValues() { Assert.Inconclusive("authored in /implement (TECH-759)"); }
  ```

**invariant_touchpoints:**
- id: "test placement under Assets/Tests/EditMode/GridAssetCatalog"
  gate: "test -f Assets/Tests/EditMode/GridAssetCatalog/CursorPlacementPreviewTests.cs && echo OK"
  expected: "pass"

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if file missing, halt and route to TECH-757 Step 5; cannot file invalid-branch tests before host file lands.

**MCP hints:** `plan_digest_verify_paths`, `unity_bridge_command` (`run_test_mode`).

---
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

Add a table-driven `PlacementFailReason → string` map and two new methods (`ShowPlacementReasonTooltip(PlacementFailReason)` + `HidePlacementReasonTooltip()`) to the `UIManager.Utilities.cs` partial. Map covers the 5 non-`None` enum values; `None` early-returns to `HidePlacementReasonTooltip`. Reuses the existing `insufficientFundsPanel` + `insufficientFundsText` surface — no new `[SerializeField]`, no auto-hide coroutine. EditMode test iterates `Enum.GetValues(typeof(PlacementFailReason))` and fails on missing entry.

### §Acceptance

- [ ] `private static readonly Dictionary<PlacementFailReason, string> PlacementReasonStringMap` declared inside `UIManager` partial with exactly 5 entries (one per non-`None` enum value).
- [ ] `public void ShowPlacementReasonTooltip(PlacementFailReason reason)` method early-returns to `HidePlacementReasonTooltip` when `reason == PlacementFailReason.None`; otherwise looks up via `TryGetValue` and writes `insufficientFundsText.text = msg`.
- [ ] `public void HidePlacementReasonTooltip()` reuses the same surface (`insufficientFundsPanel.SetActive(false)`), no fade coroutine, no `tooltipDisplayTime`.
- [ ] Zero new `[SerializeField]` fields added to `UIManager` partial.
- [ ] Zero `switch (reason)` or `switch (PlacementFailReason)` statement in `Assets/Scripts/Managers/GameManagers/UIManager*.cs`.
- [ ] EditMode test asserts every `PlacementFailReason` value except `None` is a key in the map; test red on enum drift.
- [ ] Each map entry carries `// TODO: localize` comment (no localization framework exists today — verified spot check).

### §Test Blueprint

| test_name | inputs | expected | harness |
|---|---|---|---|
| `PlacementReasonMap_CoversAllEnumValuesExceptNone` | `Enum.GetValues(typeof(PlacementFailReason)).Where(v => v != PlacementFailReason.None)` | Every enumerated value is a key in `PlacementReasonStringMap`; `None` absent | unity-batch (EditMode) |
| `PlacementReasonMap_NotTableDriven_GrepGuard` | `git grep -nE 'switch \\(.*PlacementFailReason.*\\)|switch \\(reason\\)' Assets/Scripts/Managers/GameManagers/UIManager*.cs` | zero matches | manual (grep) |
| `ShowPlacementReasonTooltip_RendersOnInvalid` | Call with `PlacementFailReason.Footprint` | `insufficientFundsPanel.activeSelf == true`; `insufficientFundsText.text == map[Footprint]` | unity-batch (EditMode) |
| `ShowPlacementReasonTooltip_HidesOnNone` | Call with `PlacementFailReason.None` | `insufficientFundsPanel.activeSelf == false`; no map lookup attempted | unity-batch (EditMode) |
| `PlacementReasonTooltip_NoAutoHide` | Show tooltip; advance test scheduler 5 seconds | Panel still `activeSelf == true` (no `WaitForSecondsRealtime(tooltipDisplayTime)` coroutine attached) | unity-batch (EditMode) |
| `UIManager_NoNewSerializeFieldForPlacementTooltip` | `git diff HEAD -- Assets/Scripts/Managers/GameManagers/UIManager.Utilities.cs \| grep '+\\[SerializeField\\]'` | zero added lines | manual (grep over diff) |
| Play Mode smoke: each reason renders matching string | Operator triggers each enum branch via cursor + state | Tooltip text per row of map; hides on valid cell / cursor leave | manual (folded into TECH-761) |

### §Examples

| Input enum | Map lookup | Action |
|---|---|---|
| `PlacementFailReason.None` | (not queried — early return) | `HidePlacementReasonTooltip()` |
| `PlacementFailReason.Footprint` | `"Out of bounds or unsupported footprint."` | `insufficientFundsText.text = msg`; `insufficientFundsPanel.SetActive(true)` |
| `PlacementFailReason.Occupied` | `"Cell already occupied."` | same as above |
| `PlacementFailReason.Zoning` | `"Wrong zone for this asset."` | same |
| `PlacementFailReason.Locked` | `"Asset locked — research required."` | same |
| `PlacementFailReason.Unaffordable` | `"Insufficient funds."` | same |
| Future enum value (Stage 3.3+) before map updated | `TryGetValue` returns false | `Debug.LogWarning` + `HidePlacementReasonTooltip()`; CI test catches missing row |

### §Mechanical Steps

#### Step 1 — Declare PlacementReasonStringMap in UIManager.Utilities.cs

**Goal:** add the static dictionary above the `ShowInsufficientFundsTooltip` method, with verbatim 5 entries + `// TODO: localize` markers.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/UIManager.Utilities.cs` — **before**:
  ```csharp
      public void ShowInsufficientFundsTooltip(string itemType, int cost)
  ```
  **after**:
  ```csharp
      private static readonly System.Collections.Generic.Dictionary<PlacementFailReason, string> PlacementReasonStringMap =
          new System.Collections.Generic.Dictionary<PlacementFailReason, string>
          {
              { PlacementFailReason.Footprint, "Out of bounds or unsupported footprint." }, // TODO: localize
              { PlacementFailReason.Zoning, "Wrong zone for this asset." }, // TODO: localize
              { PlacementFailReason.Locked, "Asset locked — research required." }, // TODO: localize
              { PlacementFailReason.Unaffordable, "Insufficient funds." }, // TODO: localize
              { PlacementFailReason.Occupied, "Cell already occupied." }, // TODO: localize
          };

      public void ShowInsufficientFundsTooltip(string itemType, int cost)
  ```

**invariant_touchpoints:**
- id: "no new SerializeField added"
  gate: "git diff HEAD -- Assets/Scripts/Managers/GameManagers/UIManager.Utilities.cs | grep -E '^\\+\\s*\\[SerializeField\\]' | wc -l"
  expected: "pass"   # 0 lines

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if added `[SerializeField]` lines appear, revert the dictionary write; this Task only adds non-Inspector state.

**MCP hints:** `plan_digest_resolve_anchor`, `glossary_lookup` (`PlacementFailReason`), `invariant_preflight`.

#### Step 2 — Add ShowPlacementReasonTooltip + HidePlacementReasonTooltip methods

**Goal:** insert two new public methods after `HideInsufficientFundsFadeRoutine`. Reuse the `insufficientFundsPanel` + `insufficientFundsText` Inspector-bound surface; no fade coroutine; cancel any in-flight `hideTooltipCoroutine` from the funds-tooltip path before showing.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/UIManager.Utilities.cs` — **before**:
  ```csharp
      private IEnumerator HideInsufficientFundsFadeRoutine()
      {
          CanvasGroup cg = insufficientFundsPanel.GetComponent<CanvasGroup>();
          if (cg != null)
              yield return UiCanvasGroupUtility.FadeUnscaled(cg, cg.alpha, 0f, PopupFadeDurationSeconds);
          insufficientFundsPanel.SetActive(false);
      }
  ```
  **after**:
  ```csharp
      private IEnumerator HideInsufficientFundsFadeRoutine()
      {
          CanvasGroup cg = insufficientFundsPanel.GetComponent<CanvasGroup>();
          if (cg != null)
              yield return UiCanvasGroupUtility.FadeUnscaled(cg, cg.alpha, 0f, PopupFadeDurationSeconds);
          insufficientFundsPanel.SetActive(false);
      }

      public void ShowPlacementReasonTooltip(PlacementFailReason reason)
      {
          if (reason == PlacementFailReason.None)
          {
              HidePlacementReasonTooltip();
              return;
          }

          if (insufficientFundsPanel == null || insufficientFundsText == null)
              return;

          if (!PlacementReasonStringMap.TryGetValue(reason, out string msg))
          {
              Debug.LogWarning($"PlacementReasonStringMap missing entry for {reason}; tooltip suppressed.");
              HidePlacementReasonTooltip();
              return;
          }

          if (hideTooltipCoroutine != null)
          {
              StopCoroutine(hideTooltipCoroutine);
              hideTooltipCoroutine = null;
          }

          insufficientFundsText.text = msg;

          CanvasGroup cg = UiCanvasGroupUtility.EnsureCanvasGroup(insufficientFundsPanel);
          cg.blocksRaycasts = false;
          cg.interactable = false;
          cg.alpha = 1f;
          insufficientFundsPanel.SetActive(true);
      }

      public void HidePlacementReasonTooltip()
      {
          if (hideTooltipCoroutine != null)
          {
              StopCoroutine(hideTooltipCoroutine);
              hideTooltipCoroutine = null;
          }

          if (insufficientFundsPanel == null || !insufficientFundsPanel.activeSelf)
              return;
          insufficientFundsPanel.SetActive(false);
      }
  ```

**invariant_touchpoints:**
- id: "no switch on PlacementFailReason in UIManager*.cs"
  gate: "git grep -nE 'switch \\(.*PlacementFailReason.*\\)|switch \\(reason\\)' Assets/Scripts/Managers/GameManagers/UIManager*.cs"
  expected: "none"
- id: "no auto-hide coroutine on placement-reason tooltip"
  gate: "git grep -nE 'WaitForSecondsRealtime\\(tooltipDisplayTime\\)' Assets/Scripts/Managers/GameManagers/UIManager.Utilities.cs | wc -l"
  expected: "unchanged"   # baseline = 1 (existing funds tooltip), must stay 1

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if either grep diverges, revert the method-add hunk; the placement tooltip MUST stay manually controlled (TECH-757 hides via `None` event payload).

**MCP hints:** `plan_digest_resolve_anchor`, `glossary_lookup`, `rule_content unity-invariants`, `unity_bridge_command` (`get_compilation_status`).

#### Step 3 — EditMode tests for map coverage + show/hide behavior

**Goal:** create `Assets/Tests/EditMode/GridAssetCatalog/PlacementReasonTooltipTests.cs` covering enum-coverage assertion, switch-grep guard, and show/hide semantics.

**Edits:**

- `Assets/Tests/EditMode/GridAssetCatalog/PlacementReasonTooltipTests.cs` — **create** (verbatim test skeleton):
  ```csharp
  using System;
  using NUnit.Framework;
  using UnityEngine;
  using Territory.Core;
  using Territory.UI;

  namespace Territory.Tests.GridAssetCatalog
  {
      public class PlacementReasonTooltipTests
      {
          [Test]
          public void Map_CoversAllEnumValuesExceptNone() { Assert.Inconclusive("authored in /implement (TECH-760)"); }

          [Test]
          public void Show_RendersTextOnInvalidReason() { Assert.Inconclusive("authored in /implement (TECH-760)"); }

          [Test]
          public void Show_NoneEarlyReturnsToHide() { Assert.Inconclusive("authored in /implement (TECH-760)"); }

          [Test]
          public void Show_NoAutoHideAfterFiveSeconds() { Assert.Inconclusive("authored in /implement (TECH-760)"); }

          [Test]
          public void Hide_DeactivatesPanel_NoCoroutine() { Assert.Inconclusive("authored in /implement (TECH-760)"); }
      }
  }
  ```

**invariant_touchpoints:**
- id: "test placement under Assets/Tests/EditMode/GridAssetCatalog"
  gate: "test -f Assets/Tests/EditMode/GridAssetCatalog/PlacementReasonTooltipTests.cs && echo OK"
  expected: "pass"

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if create fails (e.g. asmdef issue), reuse existing `Assets/Tests/EditMode/GridAssetCatalog/` `.asmdef` (proven by `GridAssetCatalogParseTests.cs`); halt and route to `/plan-review` only if asmdef truly missing.

**MCP hints:** `plan_digest_verify_paths`, `unity_bridge_command` (`run_test_mode`), `glossary_lookup`.

#### Step 4 — Wire CursorManager.PlacementReasonChanged → UIManager tooltip

**Goal:** subscribe `CursorManager` to its own `PlacementReasonChanged` (added by TECH-759) inside `Start`, routing every payload to `cachedUIManager.ShowPlacementReasonTooltip(reason)` (which internally hides on `None`). Guard on `cachedUIManager != null`.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
          PlacementResultChanged += ApplyPreviewTint;
          Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
  ```
  **after**:
  ```csharp
          PlacementResultChanged += ApplyPreviewTint;
          PlacementReasonChanged += OnPlacementReasonForwardToTooltip;
          Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
  ```

- `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — **before**:
  ```csharp
      private void OnDestroy()
      {
          PlacementResultChanged = null;
          PlacementReasonChanged = null;
      }
  ```
  **after**:
  ```csharp
      private void OnDestroy()
      {
          PlacementResultChanged = null;
          PlacementReasonChanged = null;
      }

      private void OnPlacementReasonForwardToTooltip(PlacementFailReason reason)
      {
          if (cachedUIManager == null) return;
          cachedUIManager.ShowPlacementReasonTooltip(reason);
      }
  ```

**invariant_touchpoints:**
- id: "single tooltip route — CursorManager.cs has exactly one ShowPlacementReasonTooltip call"
  gate: "git grep -n 'ShowPlacementReasonTooltip' Assets/Scripts/Managers/GameManagers/CursorManager.cs | wc -l"
  expected: "pass"   # exactly 1
- id: "no PlacementFailReason switch in CursorManager (still mapping-free)"
  gate: "git grep -nE 'switch \\(.*PlacementFailReason.*\\)' Assets/Scripts/Managers/GameManagers/CursorManager.cs"
  expected: "none"

**validator_gate:** `npm run unity:compile-check` exits 0.

**STOP:** if grep returns >1 call site, revert the wiring and centralize the route; UI tooltip must have a single CursorManager → UIManager edge.

**MCP hints:** `plan_digest_resolve_anchor`, `glossary_lookup`, `unity_bridge_command` (`get_compilation_status`).

---
## §Plan Digest

```yaml
mechanicalization_score:
  overall: fully_mechanical
  fields:
    edits_have_anchors: pass
    gates_present: pass
    invariant_touchpoints_present: pass
    stop_clauses_present: pass
    picks_resolved: pass
```

### §Goal

Author the Stage 3.2 manual Play Mode smoke checklist at `docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` so verify-loop operators can walk every `PlacementFailReason` branch + valid path + cursor-leave revert + sortingOrder + Collider2D invariant in one Play Mode session under 10 minutes. Doc IS the Stage Exit "manual smoke documented" deliverable.

### §Acceptance

- [ ] File `docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` exists.
- [ ] Doc body mentions all 6 `PlacementFailReason` enum names (`None`, `Footprint`, `Zoning`, `Locked`, `Unaffordable`, `Occupied`).
- [ ] Doc states verbatim: `Manual smoke per Stage Exit policy; no automated Play Mode test wired in CI.`
- [ ] `sortingOrder` visual checkpoint scenario row present.
- [ ] `Collider2D` invariant checkpoint scenario row present (zero new world-tile collider).
- [ ] Every scenario row carries `[ ]` checkbox + `**Result:**` placeholder for operator capture.
- [ ] Doc author runs the checklist once during `/implement` Play Mode pass-through; inline `**Result:**` lines captured + ambiguous wording revised.
- [ ] Full walk completes in one Play Mode session (target <10 min).

### §Test Blueprint

| test_name | inputs | expected | harness |
|---|---|---|---|
| Doc exists at canonical path | `test -f docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` | exit 0 | manual (path check) |
| Doc covers all `PlacementFailReason` values | `for r in None Footprint Zoning Locked Unaffordable Occupied; do grep -q "$r" docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md \|\| echo "MISSING: $r"; done` | no `MISSING:` output | manual (grep guard) |
| Doc states manual policy explicitly | `grep -c "Manual smoke per Stage Exit policy; no automated Play Mode test wired in CI." docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` | `1` | manual (grep) |
| Each scenario has checkbox + Result line | Visual review of every `### Scenario` block | Every scenario row carries `[ ]` and `**Result:**` placeholder | manual (review) |
| sortingOrder + Collider2D scenarios present | `grep -c "sortingOrder" docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md && grep -c "Collider2D" docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` | both `>= 1` | manual (grep) |
| End-to-end walk completes in one session | Operator follows checklist in Play Mode | Walk completes <10 min; all `[x]` boxes ticked or `**Result:**` captures failure | manual (operator-driven) |

### §Examples

Doc body section shape (one scenario block — repeated per row):

```markdown
### Scenario 2 — Occupied cell

- Setup: cell with existing building.
- Action: cursor over cell.
- Pass criterion: ghost red; tooltip text `Cell already occupied.`.
- [ ] **Result:** _operator pastes outcome here during verify-loop._
```

Scenario coverage matrix (verbatim authoring source for the 9 rows):

| # | Name | Setup | Action | Pass criterion |
|---|------|-------|--------|----------------|
| 1 | Valid placement (None) | Empty residential cell, treasury > base cost | Cursor over cell | Ghost green; tooltip hidden |
| 2 | Occupied | Cell with existing building | Cursor over cell | Ghost red; tooltip `Cell already occupied.` |
| 3 | Footprint (out of bounds) | Cursor at grid edge | Cursor crosses bound | Ghost red; tooltip `Out of bounds or unsupported footprint.` |
| 4 | Zoning mismatch | `state_service` asset, residential zone cell | Cursor over cell | Ghost red; tooltip `Wrong zone for this asset.` |
| 5 | Unaffordable | Treasury = 0, base_cost > 0 asset | Cursor over valid cell | Ghost red; tooltip `Insufficient funds.` |
| 6 | Locked (dormant in Stage 3.2) | Asset with `unlocks_after`, no tech | Cursor over valid cell | Ghost green per Stage 3.1 default; tooltip hidden. Doc note: `Locked path inactive in Stage 3.2; revisit when tech tree lands.` |
| 7 | Cursor leave revert | Ghost red on invalid cell | Move cursor off grid | Ghost destroyed; tooltip hidden |
| 8 | sortingOrder check | Ghost over building cluster, toggle red/green via cell move | Visual inspection | Ghost stays in correct draw order; no z-fighting |
| 9 | Collider2D invariant | After full smoke run | Inspect scene hierarchy + Physics2D | Zero new `Collider2D` on world tiles; preview colliders remain disabled per `CursorManager` |

### §Mechanical Steps

#### Step 1 — Create Stage 3.2 manual smoke checklist doc

**Goal:** create `docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` with header (Stage Exit policy + 6-enum coverage one-liner) + 9 scenario blocks (each carrying `Setup` / `Action` / `Pass criterion` / `[ ]` checkbox + `**Result:**` placeholder) + final closeout note pointing operator to capture transcript into verify-loop.

**invariant_touchpoints:** `none (utility)` — doc-only, no runtime surface, no `Assets/**/*.cs` touch.

**Edits:**

- `docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` — **before**:
  ```
  (file does not exist)
  ```
  **after**:
  ```markdown
  # Stage 3.2 — Ghost preview validation Play Mode smoke checklist

  > **Project:** grid-asset-visual-registry
  > **Stage:** 3.2 — ghost preview validation tint + tooltip
  > **Policy:** Manual smoke per Stage Exit policy; no automated Play Mode test wired in CI.
  > **Coverage:** scenarios cover all 6 `PlacementFailReason` enum values from `Assets/Scripts/Managers/GameManagers/PlacementValidator.cs` (None + Footprint + Zoning + Locked + Unaffordable + Occupied) plus cursor-leave revert + sortingOrder visual + Collider2D invariant.

  Run end-to-end in one Play Mode session (target <10 min). Tick `[x]` per scenario after observation; paste outcome under `**Result:**` (operator transcript glues into closeout-digest).

  ## Scenarios

  ### Scenario 1 — Valid placement (None)

  - Setup: empty residential zone cell, treasury greater than asset `base_cost`.
  - Action: move cursor over cell.
  - Pass criterion: ghost tint green; tooltip hidden.
  - [ ] **Result:** _paste observed outcome here during verify-loop._

  ### Scenario 2 — Occupied

  - Setup: cell already holds a placed building.
  - Action: move cursor over the occupied cell.
  - Pass criterion: ghost tint red; tooltip text `Cell already occupied.`.
  - [ ] **Result:** _paste observed outcome here during verify-loop._

  ### Scenario 3 — Footprint (out of bounds)

  - Setup: cursor positioned at the grid edge.
  - Action: move cursor across the boundary.
  - Pass criterion: ghost tint red; tooltip text `Out of bounds or unsupported footprint.`.
  - [ ] **Result:** _paste observed outcome here during verify-loop._

  ### Scenario 4 — Zoning mismatch

  - Setup: select `state_service` channel asset; cursor over a residential zone cell.
  - Action: move cursor over the wrong-zone cell.
  - Pass criterion: ghost tint red; tooltip text `Wrong zone for this asset.`.
  - [ ] **Result:** _paste observed outcome here during verify-loop._

  ### Scenario 5 — Unaffordable

  - Setup: treasury at 0; select asset with `base_cost > 0`.
  - Action: move cursor over an otherwise-valid cell.
  - Pass criterion: ghost tint red; tooltip text `Insufficient funds.`.
  - [ ] **Result:** _paste observed outcome here during verify-loop._

  ### Scenario 6 — Locked (dormant in Stage 3.2)

  - Setup: asset with `unlocks_after` set; no tech research wired (Stage 3.1 default-allow stub).
  - Action: move cursor over an otherwise-valid cell.
  - Pass criterion: ghost tint green per Stage 3.1 default; tooltip hidden. Note: Locked path inactive in Stage 3.2; revisit when tech tree lands.
  - [ ] **Result:** _paste observed outcome here during verify-loop._

  ### Scenario 7 — Cursor leave revert

  - Setup: ghost currently red over an invalid cell.
  - Action: move cursor off the grid surface.
  - Pass criterion: ghost destroyed; tooltip hidden.
  - [ ] **Result:** _paste observed outcome here during verify-loop._

  ### Scenario 8 — sortingOrder check

  - Setup: ghost positioned over a building cluster; toggle between red/green via cell motion.
  - Action: visual inspection of draw order.
  - Pass criterion: ghost stays in correct sortingOrder; no z-fighting introduced.
  - [ ] **Result:** _paste observed outcome here during verify-loop._

  ### Scenario 9 — Collider2D invariant

  - Setup: after the full smoke run completes.
  - Action: inspect scene hierarchy + Physics2D collider list.
  - Pass criterion: zero new `Collider2D` on world tiles; preview colliders remain disabled per existing `CursorManager` behavior.
  - [ ] **Result:** _paste observed outcome here during verify-loop._

  ## Closeout

  Copy the populated `**Result:**` lines into the Stage 3.2 verify-loop transcript; transcript glues into closeout-digest. If any scenario fails, file the bug against the upstream Task (TECH-757..760) and re-run the smoke after fix.
  ```

**Gate:**
```bash
test -f docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md && echo OK
```
Expectation: prints `OK`.

**Secondary gate (coverage):**
```bash
for r in None Footprint Zoning Locked Unaffordable Occupied; do grep -q "$r" docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md || echo "MISSING: $r"; done
```
Expectation: no `MISSING:` line emitted.

**STOP:** if create fails (path conflict, parent dir missing, write blocked) → re-open the doc-author step, do NOT close the Task. Without this doc Stage 3.2 cannot ship per Stage Exit policy. If `PlacementFailReason` enum values drift between authoring + verify, escalate to `/plan-review` to extend the scenario list.

**MCP hints:** `plan_digest_verify_paths` (confirm `docs/implementation/` parent exists), `plan_digest_render_literal` (re-render `PlacementValidator.cs` enum block when verifying coverage one-liner).

#### Step 2 — Operator pass-through during `/implement`

**Goal:** doc author runs the checklist once in Play Mode immediately after Step 1 write; captures real outcomes inline under each `**Result:**` line; revises ambiguous wording in-place.

**invariant_touchpoints:** `none (utility)` — operator-driven verification, no code touch.

**Edits:**

- `docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md` — **before**:
  ```
  - [ ] **Result:** _paste observed outcome here during verify-loop._
  ```
  **after** (per-scenario, populated during the Play Mode pass):
  ```
  - [x] **Result:** observed: ghost tint <green|red>; tooltip text `<exact string or "hidden">`; sortingOrder OK; no new colliders.
  ```

**Gate:**
```bash
grep -c "**Result:** observed" docs/implementation/grid-asset-visual-registry-stage-3.2-smoke.md
```
Expectation: `>= 9` (one per scenario row).

**STOP:** if any scenario diverges from the authored Pass criterion → file a BUG-* against the upstream Task (TECH-757..760) before flipping this Task Done; do NOT mark `[x]` on a scenario whose `**Result:**` captures failure.

**MCP hints:** `unity_bridge_command` (Play Mode entry / cell-cursor placement when bridge supports it), `plan_digest_resolve_anchor` (locate the per-scenario `**Result:**` template line for in-place edit).

### §Implementer hand-off

- Order: Step 1 (write doc) → Step 2 (operator Play Mode pass + inline result capture). No code edits in this Task; no `npm run unity:compile-check` needed; gate set is `test -f` + `grep`.
- Cross-Task dependency: TECH-757..760 must be Done before Step 2 can produce non-failing `**Result:**` lines (Scenarios 2–7 depend on tint + tooltip wiring; Scenario 9 depends on absence of new `Collider2D` introduced by TECH-757..760 changes).
- This doc IS the Stage 3.2 Stage Exit "manual smoke documented" deliverable; without it ship-stage halts.


## Final gate

```bash
npm run validate:all
```
