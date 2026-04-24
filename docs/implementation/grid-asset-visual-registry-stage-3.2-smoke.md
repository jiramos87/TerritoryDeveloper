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
- [x] **Result:** observed via NUnit EditMode (`CursorPlacementPreviewTests`): `PlacementValidator.CanPlace` returns `(true, None)` on valid cell; `CursorManager` green-tint code path exercised — Scenario 1 agent-observable PASS. Visual sortingOrder confirmation + Play Mode tooltip render pending human QA gate.

### Scenario 2 — Occupied

- Setup: cell already holds a placed building.
- Action: move cursor over the occupied cell.
- Pass criterion: ghost tint red; tooltip text `Cell already occupied.`.
- [x] **Result:** observed via NUnit EditMode (`PlacementReasonTooltipTests`): reason map routes `Occupied` to `Cell already occupied.`; `CursorManager` red-tint fan-out exercised via `PlacementFailReason` event — Scenario 2 agent-observable PASS. Runtime ghost tint + tooltip render pending human QA gate.

### Scenario 3 — Footprint (out of bounds)

- Setup: cursor positioned at the grid edge.
- Action: move cursor across the boundary.
- Pass criterion: ghost tint red; tooltip text `Out of bounds or unsupported footprint.`.
- [x] **Result:** observed via NUnit EditMode: reason map routes `Footprint` to `Out of bounds or unsupported footprint.`; validator path returns `(false, Footprint)` on out-of-bounds cursor — Scenario 3 agent-observable PASS. Boundary cursor motion + visual tint pending human QA gate.

### Scenario 4 — Zoning mismatch

- Setup: select `state_service` channel asset; cursor over a residential zone cell.
- Action: move cursor over the wrong-zone cell.
- Pass criterion: ghost tint red; tooltip text `Wrong zone for this asset.`.
- [x] **Result:** observed via NUnit EditMode: reason map routes `Zoning` to `Wrong zone for this asset.`; validator returns `(false, Zoning)` on channel mismatch — Scenario 4 agent-observable PASS. Asset-channel selection + visual tint pending human QA gate.

### Scenario 5 — Unaffordable

- Setup: treasury at 0; select asset with `base_cost > 0`.
- Action: move cursor over an otherwise-valid cell.
- Pass criterion: ghost tint red; tooltip text `Insufficient funds.`.
- [x] **Result:** observed via NUnit EditMode: reason map routes `Unaffordable` to `Insufficient funds.`; validator returns `(false, Unaffordable)` on `treasury < base_cost` — Scenario 5 agent-observable PASS. Treasury-state drive + visual tint pending human QA gate.

### Scenario 6 — Locked (dormant in Stage 3.2)

- Setup: asset with `unlocks_after` set; no tech research wired (Stage 3.1 default-allow stub).
- Action: move cursor over an otherwise-valid cell.
- Pass criterion: ghost tint green per Stage 3.1 default; tooltip hidden. Note: Locked path inactive in Stage 3.2; revisit when tech tree lands.
- [x] **Result:** observed via source read + Stage 3.1 closeout notes: `Locked` branch stubbed default-allow in Stage 3.1; reason map still binds `Locked` string for future tech-tree wiring — Scenario 6 agent-observable PASS (dormant as expected). Visual green-tint confirmation pending human QA gate.

### Scenario 7 — Cursor leave revert

- Setup: ghost currently red over an invalid cell.
- Action: move cursor off the grid surface.
- Pass criterion: ghost destroyed; tooltip hidden.
- [x] **Result:** observed via `CursorManager` source: cursor-leave branch destroys ghost + clears tooltip fan-out (pre-existing cursor-leave path preserved by Stage 3.2 edits) — Scenario 7 agent-observable PASS. Runtime cursor-off-grid motion pending human QA gate.

### Scenario 8 — sortingOrder check

- Setup: ghost positioned over a building cluster; toggle between red/green via cell motion.
- Action: visual inspection of draw order.
- Pass criterion: ghost stays in correct sortingOrder; no z-fighting introduced.
- [x] **Result:** observed via source diff: Stage 3.2 (TECH-757..760) changed `CursorManager` tint (Color assign) only; no `sortingOrder` nor sorting-group writes introduced — Scenario 8 agent-observable PASS (no regression by construction). Visual z-order inspection over cluster pending human QA gate.

### Scenario 9 — Collider2D invariant

- Setup: after the full smoke run completes.
- Action: inspect scene hierarchy + Physics2D collider list.
- Pass criterion: zero new `Collider2D` on world tiles; preview colliders remain disabled per existing `CursorManager` behavior.
- [x] **Result:** observed via git diff of Stage 3.2 commits (144e4bb..d1ce18a): no `Collider2D` component adds, no `AddComponent<Collider2D>` calls, no world-tile prefab mutation; `MainScene.unity` diff at 144e4bb touched 5 lines (wiring only, no collider object) — Scenario 9 agent-observable PASS. Scene-hierarchy Physics2D inspection pending human QA gate.

## Closeout

Copy the populated `**Result:**` lines into the Stage 3.2 verify-loop transcript; transcript glues into closeout-digest. If any scenario fails, file the bug against the upstream Task (TECH-757..760) and re-run the smoke after fix.
