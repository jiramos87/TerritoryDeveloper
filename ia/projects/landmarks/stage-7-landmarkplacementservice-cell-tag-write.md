### Stage 7 — BigProjectService + LandmarkPlacementService + sidecar save / LandmarkPlacementService + cell-tag write

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Land the placement helper (under `GameManagers/*Service.cs` carve-out). `Place(id, cell, scale)` writes sprite tile + cell-tag. No sidecar yet (Stage 3.3), no commission yet (Stage 3.2) — just the placement atom that both tracks call.

**Exit:**

- `LandmarkPlacementService.cs` MonoBehaviour — `[SerializeField] private GridManager grid`, `[SerializeField] private LandmarkCatalogStore catalog`. `FindObjectOfType` fallback per invariant #4.
- `Place(string landmarkId, CellCoord cell, ScaleTag scale)` — looks up row, writes sprite tile at `(cell.x, cell.y)` via existing tile API, sets `grid.GetCell(cell.x, cell.y).landmarkId = landmarkId` (new `Cell.landmarkId` string field, nullable). Invariant #1 safe — no height write.
- `Cell.landmarkId` field added to existing `Cell` struct (check file at stage-file time — likely `Assets/Scripts/Grid/Cell.cs`).
- `event Action<string, CellCoord, ScaleTag> LandmarkPlaced` fires after write (consumed by sidecar + future CityStats feed).
- EditMode test — drive `Place("regional_plocks", (42,88), Region)`, assert cell-tag set, assert sprite tile registered, assert `LandmarkPlaced` event fired once.
- Phase 1 — Service scaffold + `Cell.landmarkId` field.
- Phase 2 — Place method + event emission + EditMode test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | LandmarkPlacementService scaffold | _pending_ | _pending_ | Add `Assets/Scripts/Managers/GameManagers/LandmarkPlacementService.cs` MonoBehaviour. `[SerializeField]` GridManager + LandmarkCatalogStore refs + `FindObjectOfType` fallback. XML doc cites invariant #5 + #6 carve-out (service under GameManagers allowed cellArray access; no GridManager responsibility creep). |
| T7.2 | Cell.landmarkId field | _pending_ | _pending_ | Edit `Assets/Scripts/Grid/Cell.cs` (or canonical cell struct location — verify at stage-file time). Add `public string landmarkId` nullable field. XML doc: "denormalized index of sidecar row; null when no landmark placed; rebuilt from sidecar on load." |
| T7.3 | Place method + invariant #1 guard | _pending_ | _pending_ | Implement `Place(landmarkId, cell, scale)` — catalog lookup, sprite tile write via existing BuildingPlacementService tile API pattern, `grid.GetCell(cell.x, cell.y).landmarkId = landmarkId`. Code comment: `// Invariant #1 — placement is tile-sprite only, no HeightMap mutation`. |
| T7.4 | LandmarkPlaced event | _pending_ | _pending_ | Add `public event Action<string, CellCoord, ScaleTag> LandmarkPlaced`; invoke after cell-tag write in `Place`. Consumers wired in Stage 3.3 (sidecar) + Step 4 (utilities bridge). |
| T7.5 | Placement EditMode test | _pending_ | _pending_ | Add `Assets/Tests/EditMode/Landmarks/LandmarkPlacementServiceTests.cs` — fixture scene w/ GridManager + Store + PlacementService. Call `Place("regional_plocks", (42,88), Region)`; assert `grid.GetCell(42,88).landmarkId == "regional_plocks"`, assert event fired once, assert height untouched (invariant #1 check). |
