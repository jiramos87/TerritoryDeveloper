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
