---
purpose: "TECH-758 — Valid tint path."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T3.2.2"
---
# TECH-758 — Valid tint path

> **Issue:** [TECH-758](../../BACKLOG.md)
> **Status:** Draft | In Review | In Progress | Final
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Valid placement path: green tint applied to ghost sprite via existing tint utility;
`sortingOrder` preserved; no new world-tile collider. Consumed by TECH-757 hook.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Green tint uses existing sprite tint utility pattern; no new `SpriteRenderer` writes.
2. `sortingOrder` and sorting group preserved across tint transitions.
3. Tint state reverts on cursor leave / placement commit.
4. No new `Collider2D` on world tiles (Stage Exit).

### 2.2 Non-Goals (Out of Scope)

1. New tooltip prefab or UI layer changes (tooltip is TECH-760).
2. Animation or tint fade — instant or snap tint.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Preview ghost turns green when placement is valid | Ghost sprite shows green tint; no collider pop-in |
| 2 | Developer | Reuse existing sprite tint utility | No new `SpriteRenderer` component or tint method |

## 4. Current State

### 4.1 Domain behavior

Ghost preview exists but no tint feedback. Existing sprite tint utilities (likely on `SpriteRenderer`) available for reuse.

### 4.2 Systems map

- Existing sprite tint utility (locate via `SpriteRenderer.color` touch sites in
  Managers/GameManagers).
- `CursorManager` / preview hook (TECH-757) — caller.
- `PlacementValidator` result (`PlacementFailReason.None`) — trigger.

### 4.3 Implementation investigation notes (optional)

- Identify existing sprite tint utility method/pattern.
- Confirm `sortingOrder` preservation across tint calls.

## 5. Proposed Design

### 5.1 Target behavior (product)

When `PlacementValidator.CanPlace` returns `true` (reason = None), apply green tint to ghost sprite; preserve layer/sorting hierarchy.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Locate existing tint utility (likely `SpriteRenderer.color = Color.green`).
- Wire TECH-757 valid result to tint utility call.
- Implement revert path (on cursor leave or placement commit).

### 5.3 Method / algorithm notes (optional)

```
OnValidationResult(isValid, reason):
  if isValid:
    ghostRenderer.color = Color.green  // or existing utility method
  else:
    // revert handled by TECH-759
```

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| TBD | Tint utility method | TBD | TBD |

## 7. Implementation Plan

### Phase 1 — Tint apply/revert path

- [ ] Wire valid result → tint utility; revert on cursor leave / commit.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Green tint applied on valid placement | Play Mode manual | Cursor over valid cell; observe ghost color | Screenshot or operator observation |
| sortingOrder preserved | Code/Play Mode review | Compare ghost layer before/after tint | No layer change or collider appearance |
| Tint reverts on cursor leave | Play Mode manual | Move cursor away; verify ghost revert | Color return to original |

## 8. Acceptance Criteria

- [ ] Green tint applies to ghost sprite on valid placement (reason = None).
- [ ] `sortingOrder` and sorting group preserved across tint transitions.
- [ ] Tint reverts on cursor leave / placement commit.
- [ ] No new `Collider2D` on world tiles.

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

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._

## Open Questions (resolve before / during implementation)

1. Existing sprite tint utility — method signature and location?
2. Revert trigger — on cursor leave only, or also on placement commit?
3. Tint color value — exact `Color.green` or custom RGB?

---
