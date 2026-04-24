---
purpose: "TECH-760 — Tooltip reason string."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T3.2.4"
---
# TECH-760 — Tooltip reason string

> **Issue:** [TECH-760](../../BACKLOG.md)
> **Status:** Draft | In Review | In Progress | Final
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Tooltip controller receives `PlacementFailReason` enum from TECH-759; maps to
human-readable string via table; renders in existing tooltip surface.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Table-driven enum → string map (one entry per `PlacementFailReason`); no nested switch.
2. Renders via existing `UIManager` tooltip surface pattern (no new tooltip prefab if avoidable).
3. Empty / None reason → tooltip hidden.
4. Localization hook-point reserved (string keys not hard-coded user strings if
   localization pattern exists).

### 2.2 Non-Goals (Out of Scope)

1. Animated tooltip appearance or fade-in.
2. Custom tooltip styling — reuse existing `UIManager` surface.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Invalid placement shows helpful reason text | Tooltip appears; human-readable reason clear |
| 2 | Developer | Reason enum maps to string via lookup table | No switch sprawl; table-driven design |

## 4. Current State

### 4.1 Domain behavior

`PlacementFailReason` enum exists (TECH-689); no UI consumption. Tooltips exist in UIManager.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/UIManager.cs` — tooltip host.
- `PlacementFailReason` enum (TECH-689) — input.
- Preview hook (TECH-757) — upstream caller.

### 4.3 Implementation investigation notes (optional)

- Enumerate all `PlacementFailReason` values.
- Locate UIManager tooltip method/surface.
- Check for existing localization pattern.

## 5. Proposed Design

### 5.1 Target behavior (product)

When invalid placement reason forwarded from TECH-759, display human-readable reason string in tooltip. Hide tooltip on valid or cursor leave.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Create enum → string lookup table (dict or switch).
- Hook to UIManager tooltip render method.
- Emit/clear tooltip on reason change.
- Reserve localization keys if pattern exists.

### 5.3 Method / algorithm notes (optional)

```
private Dictionary<PlacementFailReason, string> reasonStringMap = new()
{
  { PlacementFailReason.OutOfBounds, "Out of bounds" },
  { PlacementFailReason.Occupied, "Cell occupied" },
  // ... one entry per enum value
};

OnReasonChanged(reason):
  if reason == PlacementFailReason.None:
    uiManager.HideTooltip()
  else:
    string msg = reasonStringMap[reason]
    uiManager.ShowTooltip(msg)
```

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| TBD | Map structure — dict vs switch | TBD | TBD |

## 7. Implementation Plan

### Phase 1 — Enum→string table + render

- [ ] Define table; wire from invalid path; hide on valid / cursor leave.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Enum → string map complete | Code review | Table covers all PlacementFailReason values | No missing entries |
| Table-driven design (no switch sprawl) | Code review | Inspect lookup structure | Dict or enum array, not nested switch |
| Tooltip renders via existing UIManager | Play Mode manual | Observe tooltip text on invalid placement | Matches UIManager surface style |
| Empty/None reason hides tooltip | Play Mode manual | Move cursor to valid cell; tooltip disappears | Hidden state verified |

## 8. Acceptance Criteria

- [ ] Table-driven enum → string map (one entry per `PlacementFailReason`).
- [ ] Renders via existing `UIManager` tooltip surface pattern.
- [ ] Empty / None reason → tooltip hidden.
- [ ] Localization hook-point reserved (if pattern exists).

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

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._

## Open Questions (resolve before / during implementation)

1. Existing UIManager tooltip method — signature and usage pattern?
2. Localization pattern — string keys or hard-coded strings acceptable for MVP?
3. Tooltip lifetime — when does it auto-hide (cursor leave, placement commit)?

---
