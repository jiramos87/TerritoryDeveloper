### Stage 8 — BigProjectService + LandmarkPlacementService + sidecar save / BigProjectService commission pipeline

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship the commission pipeline — `TryCommission` opens bond + ledger row; `Tick()` advances monthly progress; completion fires `LandmarkBuildCompleted` which invokes placement. Pause / resume supported; cancel NOT supported (v1 locked).

**Exit:**

- `BigProjectService.cs` MonoBehaviour — refs to `ServiceBudgetService`, `LandmarkCatalogStore`, `LandmarkProgressionService`, `LandmarkPlacementService`, `TimeManager`. `FindObjectOfType` fallback per invariant #4.
- `CommissionLedgerRow` class — `string landmarkId`, `int principal`, `int monthsElapsed`, `int buildMonths`, `bool paused`, `BondRef bondRef`, `CellCoord targetCell`, `ScaleTag targetScale`.
- `CommissionResult` enum — `Ok`, `NotUnlocked`, `AlreadyCommissioned`, `BondDeclined`, `UnknownLandmark`.
- `TryCommission(string id, CellCoord cell, ScaleTag scale)` — checks unlock flag via `progression.IsUnlocked`, checks no existing row for same id, calls `serviceBudget.OpenBond(row.commissionCost, this)` (this = `IBondConsumer` impl), appends ledger row. Returns result.
- `OnGameMonth` handler advances `monthsElapsed` on non-paused rows; on `monthsElapsed >= buildMonths` fires `event Action<string, CellCoord, ScaleTag> LandmarkBuildCompleted` + removes row from active ledger.
- Tier-defining bypass — subscribes to `progression.LandmarkUnlocked`; when row has `buildMonths == 0 && commissionCost == 0`, fires `LandmarkBuildCompleted` immediately at default cell (scale-capital cell lookup via `ScaleTierController` OR fallback grid center).
- `Pause(string id)` / `Resume(string id)` flip `paused` flag on matching ledger row.
- EditMode tests — commission flow, pause blocks progress, tier-defining bypass, bond decline path, not-unlocked reject.
- Phase 1 — Data scaffolds + `BigProjectService` MonoBehaviour + `TryCommission`.
- Phase 2 — Monthly tick + completion event.
- Phase 3 — Tier-defining bypass + pause/resume.
- Phase 4 — EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | CommissionLedgerRow + CommissionResult | _pending_ | _pending_ | Add `Assets/Scripts/Data/Landmarks/CommissionLedgerRow.cs` serializable class (fields listed in Stage exit) + `CommissionResult.cs` enum. |
| T8.2 | BigProjectService scaffold + refs | _pending_ | _pending_ | Add `Assets/Scripts/Managers/GameManagers/BigProjectService.cs` MonoBehaviour. `[SerializeField]` slots for ServiceBudgetService, LandmarkCatalogStore, LandmarkProgressionService, LandmarkPlacementService, TimeManager. Inventory `List<CommissionLedgerRow>` (active). `FindObjectOfType` fallback per invariant #4. |
| T8.3 | TryCommission + bond open | _pending_ | _pending_ | Implement `TryCommission(id, cell, scale)` — validate unlock via `progression.IsUnlocked(id)`, check no duplicate ledger row, call `serviceBudget.OpenBond(row.commissionCost, this)`. On success append ledger row. Return `CommissionResult`. `IBondConsumer` impl = this service (Bucket 3 contract stub). |
| T8.4 | Monthly tick + completion event | _pending_ | _pending_ | Add `OnGameMonth` handler — iterate non-paused rows, `monthsElapsed++`. On `monthsElapsed >= buildMonths` fire `event Action<string, CellCoord, ScaleTag> LandmarkBuildCompleted` + remove row. Subscribe to `TimeManager.OnGameMonth`. |
| T8.5 | LandmarkBuildCompleted → placement wiring | _pending_ | _pending_ | Subscribe `placement.Place` to `LandmarkBuildCompleted` event in `OnEnable` / unsubscribe in `OnDisable`. Callback — `placement.Place(id, cell, scale)`. |
| T8.6 | Tier-defining bypass | _pending_ | _pending_ | Subscribe to `progression.LandmarkUnlocked` — if `catalog.GetById(id).buildMonths == 0 && commissionCost == 0`, skip commission, fire `LandmarkBuildCompleted` immediately. Default cell = scale-capital cell (add `ScaleTierController.GetCapitalCell(tier)` or fallback grid-center helper). |
| T8.7 | Pause + resume API | _pending_ | _pending_ | Add `public void Pause(string id)` / `Resume(string id)` — look up ledger row by id, flip `paused` flag. No-op on unknown id. |
| T8.8 | Commission flow EditMode tests | _pending_ | _pending_ | Add `Assets/Tests/EditMode/Landmarks/BigProjectServiceTests.cs` — fake ServiceBudget + fixture catalog. Core flow: commission→tick×18→complete→placement invoked; pause→tick×N→no progress; resume→tick→progress resumes. |
| T8.9 | Commission reject-path tests | _pending_ | _pending_ | Extend test suite — `TryCommission` returns `NotUnlocked` when progression flag false; returns `AlreadyCommissioned` on duplicate id; returns `BondDeclined` when fake budget refuses bond; returns `UnknownLandmark` on catalog miss. Tier-defining bypass test — `LandmarkUnlocked` event w/ `buildMonths==0` fires `LandmarkBuildCompleted` same tick. |
