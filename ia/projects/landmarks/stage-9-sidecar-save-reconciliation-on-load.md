### Stage 9 — BigProjectService + LandmarkPlacementService + sidecar save / Sidecar save + reconciliation on load

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Persist placed landmarks + commission ledger to `landmarks.json` sidecar; extend main-save per-scale cell-tag map w/ `landmarkId`; atomic write with main save; reconcile on load (sidecar wins). Schema piggybacks on Bucket 3 v3 envelope — no mid-tier bump here.

**Exit:**

- `LandmarkSidecarDto` serializable class mirroring sidecar schema (`schemaVersion: 1`, `landmarks[]`, `commissionLedger[]`).
- `GameSaveManager` writes + reads sidecar — `landmarks.json` path = `{persistentDataPath}/{saveSlot}/landmarks.json`. Atomic write: temp file + atomic rename, paired with main-save temp+rename per Review Notes Phase C.
- Main-save `GameSaveData` v3 envelope additions — `regionCells[].landmarkId` + `cityCells[].landmarkId` (nullable). Code comment: `// schemaVersion bump owned by Bucket 3 (zone-s-economy); landmarks adds fields to v3 envelope only`.
- Write path — `GameSaveManager.Save` extension: walk `LandmarkPlacementService` sidecar row inventory + `BigProjectService.activeLedger`; serialize to DTO; atomic write.
- Read path — `GameSaveManager.Load` extension, after grid cell restore (existing step): (1) load sidecar first (sidecar = truth); (2) walk sidecar `landmarks[]` — for each row, call `placement.RestoreCellTag(row)` idempotent helper; (3) walk main-save cell tags; for each tag w/o matching sidecar row, clear tag + log diagnostic (dangling); (4) restore `BigProjectService.activeLedger` from `commissionLedger[]`.
- Load guard — when `saveData.schemaVersion < 3`, skip cell-tag read; when sidecar missing, treat as empty list (new save OR pre-landmark save).
- Diagnostic channel — `Debug.Log` reconciliation delta (count of restored tags, count of dangling tags cleared).
- PlayMode round-trip test — place `regional_plocks` + commission `big_power_plant` mid-build (months=5/18), save, reload, assert sidecar + cell-tag restored + ledger progress preserved + pause flag preserved.
- Divergence test — hand-craft save where sidecar has `big_power_plant @ (17,33)` but main-save cell-tag absent → reload → assert tag restored + diagnostic logged.
- Dangling test — hand-craft save where main-save has cell-tag but sidecar row missing → reload → assert tag cleared + diagnostic logged.
- Phase 1 — Sidecar DTO + write path + main-save cell-tag field.
- Phase 2 — Load + reconciliation pipeline + atomic-write pairing.
- Phase 3 — PlayMode round-trip + divergence tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | LandmarkSidecarDto | _pending_ | _pending_ | Add `Assets/Scripts/Data/Landmarks/LandmarkSidecarDto.cs` — `[Serializable]` class w/ `int schemaVersion = 1`, `List<LandmarkSidecarRow> landmarks`, `List<CommissionLedgerRow> commissionLedger`. Nested `LandmarkSidecarRow` w/ `id`, `cell`, `footprint`, `placedTick`, `active`, `counters`. |
| T9.2 | GameSaveManager sidecar write | _pending_ | _pending_ | Edit `GameSaveManager.cs` — add `WriteSidecar(saveSlot)` private method. Walks `LandmarkPlacementService` inventory + `BigProjectService.activeLedger`; serializes to DTO; writes to `{persistentDataPath}/{saveSlot}/landmarks.json.tmp`. Called from Save pipeline after main-save serialization. |
| T9.3 | Main-save cell-tag field | _pending_ | _pending_ | Edit main-save `GameSaveData` DTO (verify exact location at stage-file time). Add nullable `string landmarkId` to `regionCells[]` + `cityCells[]` DTO. Comment: `// v3 envelope — Bucket 3 owns schemaVersion bump; landmarks additive`. |
| T9.4 | Sidecar read + reconciliation | _pending_ | _pending_ | Edit `GameSaveManager.Load` — after grid cell restore: (1) load sidecar if exists; (2) foreach sidecar row call `placement.RestoreCellTag(row)`; (3) walk cell-tags, clear dangling + log diagnostic; (4) restore `BigProjectService.activeLedger` from `commissionLedger`. Guard `if (saveData.schemaVersion < 3) skip`. |
| T9.5 | LandmarkPlacementService.RestoreCellTag | _pending_ | _pending_ | Add `public void RestoreCellTag(LandmarkSidecarRow row)` — idempotent write to `grid.GetCell(row.cell.x, row.cell.y).landmarkId = row.id`. Does NOT re-emit `LandmarkPlaced` event (load-path, not place-path). |
| T9.6 | Atomic write pairing | _pending_ | _pending_ | Refactor `GameSaveManager.Save` — write both main-save + sidecar to `.tmp`, then atomic-rename both as a pair (File.Move). Fail-safe: if rename fails mid-pair, leave `.tmp` files for next save recovery. Doc cites Review Notes Phase C sidecar bundling. |
| T9.7 | Save round-trip PlayMode test | _pending_ | _pending_ | Add `Assets/Tests/PlayMode/Landmarks/LandmarkSaveRoundTripTests.cs` — place `regional_plocks`, commission `big_power_plant` to months=5/18, pause, save, reload. Assert: sidecar row restored, cell-tag restored, ledger row restored w/ `paused == true` + `monthsElapsed == 5`. |
| T9.8 | Reconciliation divergence tests | _pending_ | _pending_ | Add tests: (a) sidecar has row, main-save cell-tag absent → reload → tag restored + diagnostic count 1; (b) main-save has tag, sidecar missing row → reload → tag cleared + diagnostic count 1; (c) both present + matching → no diagnostic. |
