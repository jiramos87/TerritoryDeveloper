### Stage 10 — Super-utility bridge + UI surface + spec closeout / Super-utility contributor bridge

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Wire the narrow catalog bridge to sibling Bucket 4-a `UtilityContributorRegistry`. On `LandmarkBuildCompleted` with non-null `utilityContributorRef`, call `Register`. On load-path re-registration via sidecar restore, same call. **Hard sequencing dep:** utilities Stage 1.3 closed before this stage files.

**Exit:**

- `LandmarkPlacementService` adds optional `[SerializeField] private UtilityContributorRegistry utilityRegistry` (Bucket 4-a type). `Awake` fallback + nullable handling (bucket-4-a not always loaded in test scenes).
- `Place` method — after cell-tag write, if `row.utilityContributorRef != null && utilityRegistry != null`, call `utilityRegistry.Register(row.utilityContributorRef, row.contributorScalingFactor)`. Log if registry missing but row has non-null ref (misconfiguration).
- `RestoreCellTag` (load-path) — same conditional re-registration. Ensures re-register on load so utility pools rebuild correctly.
- `Unregister` path — v1 has no in-game landmark destruction, but scaffolded for future: new `Demolish(string id)` method clears cell-tag + sidecar row + if super-utility calls `utilityRegistry.Unregister(id)`. Exposed but not invoked by any UI in v1.
- EditMode bridge test — fake `UtilityContributorRegistry`, place `big_power_plant`, assert `Register("contributors/coal_plant", 10.0f)` called once. Place `regional_plocks` (null ref) — assert `Register` NOT called.
- Load-path bridge test — restore sidecar w/ 1 super-utility row, assert `Register` called during `RestoreCellTag`.
- Phase 1 — Registry ref + place-path bridge.
- Phase 2 — Load-path bridge + Demolish scaffold.
- Phase 3 — Bridge EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | UtilityContributorRegistry ref | _pending_ | _pending_ | Edit `LandmarkPlacementService.cs` — `[SerializeField] private UtilityContributorRegistry utilityRegistry` (nullable). `Awake` — `FindObjectOfType` fallback; log info if still null (OK for test scenes). |
| T10.2 | Place-path super-utility register | _pending_ | _pending_ | In `Place` method, after cell-tag write: `if (row.utilityContributorRef != null) { if (utilityRegistry != null) utilityRegistry.Register(row.utilityContributorRef, row.contributorScalingFactor); else Debug.LogWarning($"Landmark {row.id} has utilityContributorRef but no registry wired"); }`. |
| T10.3 | Load-path re-register | _pending_ | _pending_ | `RestoreCellTag` — same conditional registry.Register call. Ensures load-path rebuilds utility pool contributors. XML doc note: "idempotent — utility registry dedupes by id." |
| T10.4 | Demolish scaffold | _pending_ | _pending_ | Add `public void Demolish(string id)` — look up sidecar row, clear cell-tag at `(row.cell.x, row.cell.y).landmarkId = null`, remove sidecar row, if `row.utilityContributorRef != null` call `utilityRegistry.Unregister(id)`. Not invoked by UI in v1; scaffolded for post-MVP destructibility. |
| T10.5 | Place-path bridge test | _pending_ | _pending_ | Add `Assets/Tests/EditMode/Landmarks/SuperUtilityBridgeTests.cs` — fake `UtilityContributorRegistry`, place `big_power_plant`, assert `Register("contributors/coal_plant", 10.0f)` called once; place `regional_plocks` (null ref), assert no register call. |
| T10.6 | Load-path bridge test | _pending_ | _pending_ | Add test: construct sidecar DTO w/ 1 super-utility row + 1 non-utility row. Call `RestoreCellTag` for each. Assert fake registry has exactly one `Register` call w/ correct ref + multiplier. |
