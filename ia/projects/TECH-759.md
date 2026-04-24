---
purpose: "TECH-759 — Invalid tint path."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T3.2.3"
---
# TECH-759 — Invalid tint path

> **Issue:** [TECH-759](../../BACKLOG.md)
> **Status:** Draft | In Review | In Progress | Final
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Invalid placement path: red tint on ghost + `PlacementFailReason` enum propagated to
UI layer for tooltip consumption (TECH-760). Reuses tint utility (TECH-758).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Red tint on invalid result using same utility as valid path.
2. `PlacementFailReason` enum value forwarded to UI / tooltip consumer.
3. `sortingOrder` preserved (invariant with TECH-758).
4. No new `Collider2D` on world tiles.

### 2.2 Non-Goals (Out of Scope)

1. Tooltip text rendering (TECH-760 owns that).
2. Different tint utilities per reason type — single red for all invalid states.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Preview ghost turns red when placement is invalid | Ghost sprite shows red tint; tooltip shows reason |
| 2 | Developer | Reason enum propagates to tooltip layer | PlacementFailReason forwarded to TECH-760 consumer |

## 4. Current State

### 4.1 Domain behavior

Ghost preview exists but no invalid tint feedback. `PlacementValidator` returns reason enum (TECH-689); no UI consumption yet.

### 4.2 Systems map

- Same sprite tint utility as TECH-758.
- `PlacementValidator` result tuple carries reason (TECH-689).
- UI tooltip consumer (TECH-760) — downstream of enum forward.

### 4.3 Implementation investigation notes (optional)

- Confirm tint utility works with red color.
- Determine enum forward mechanism (event, direct call, or property).

## 5. Proposed Design

### 5.1 Target behavior (product)

When `PlacementValidator.CanPlace` returns `false` (reason ≠ None), apply red tint to ghost sprite; forward reason enum to TECH-760.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Reuse tint utility from TECH-758; apply red color on invalid result.
- Emit reason enum event or forward to direct consumer (TECH-760).
- Preserve layer/sorting as per TECH-758.

### 5.3 Method / algorithm notes (optional)

```
OnValidationResult(isValid, reason):
  if !isValid:
    ghostRenderer.color = Color.red  // or existing utility method
    tooltipConsumer.OnReasonChanged(reason)
  else:
    // handled by TECH-758
```

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| TBD | Reason forward mechanism | TBD | TBD |

## 7. Implementation Plan

### Phase 1 — Invalid branch + reason forward

- [ ] Apply red tint; emit reason event / direct call to tooltip layer.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Red tint applied on invalid placement | Play Mode manual | Cursor over invalid cell; observe ghost color | Screenshot or operator observation |
| PlacementFailReason enum forwarded | Code review | Reason parameter passed to TECH-760 consumer | Verify enum type and values match |
| sortingOrder preserved | Code/Play Mode review | Compare ghost layer before/after tint | Invariant with TECH-758 |

## 8. Acceptance Criteria

- [ ] Red tint applies to ghost sprite on invalid placement (reason ≠ None).
- [ ] `PlacementFailReason` enum value forwarded to UI / tooltip consumer.
- [ ] `sortingOrder` preserved (invariant with TECH-758).
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

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._

## Open Questions (resolve before / during implementation)

1. Reason enum forward — event, property, or direct consumer call?
2. Red tint color — exact `Color.red` or custom RGB matching game palette?
3. Tint revert — same as TECH-758, or different trigger?

---
