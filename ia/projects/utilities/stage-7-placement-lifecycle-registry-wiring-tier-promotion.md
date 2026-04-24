### Stage 7 — Infrastructure buildings + terrain-sensitive placement / Placement lifecycle + registry wiring + tier promotion

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** On successful placement: instantiate contributor, register with `UtilityContributorRegistry`; on demolition: deregister. Tier promotion driven by output threshold (not player input, not tech tree). Integrates with `EconomyManager` for construction cost + per-day maintenance.

**Exit:**

- `InfrastructureBuildingService.Place(def, x, y)` — instantiates prefab, attaches `InfrastructureContributor` component implementing `IUtilityContributor`, calls `registry.Register(contributor)`. Returns placed building ref.
- `InfrastructureContributor : MonoBehaviour, IUtilityContributor` — reads def, reports current tier's `ProductionRate = base × tierMultiplier[currentTier]`. Tier recomputed on each game-day when service calls `PromoteIfEligible`.
- `InfrastructureBuildingService.Demolish(building)` deregisters contributor before destroying GameObject.
- `EconomyManager` deducts `def.constructionCost` on place + `def.dailyMaintenance` on `OnGameDay`. Tracked under separate infrastructure line (not Zone S budget).
- EditMode tests: place→registered, demolish→deregistered, tier promotes at threshold, maintenance deducted per day.
- Phase 1 — `InfrastructureContributor` component + place/demolish hooks.
- Phase 2 — Tier promotion logic.
- Phase 3 — `EconomyManager` cost/maintenance line.
- Phase 4 — Lifecycle EditMode tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | InfrastructureContributor component | _pending_ | _pending_ | Add `Assets/Scripts/Buildings/Infrastructure/InfrastructureContributor.cs` — MonoBehaviour implementing `IUtilityContributor`. Reads `InfrastructureBuildingDef def` + `int currentTier`. `ProductionRate` getter computes `def.baseProductionRate × def.tierMultipliers[currentTier]`. |
| T7.2 | Place + register hook | _pending_ | _pending_ | Implement `InfrastructureBuildingService.Place(def, x, y)` — instantiate prefab, attach `InfrastructureContributor`, call `registry.Register(contributor)`. Wire into existing `BuildingPlacementService` dispatch. |
| T7.3 | Demolish + deregister hook | _pending_ | _pending_ | Implement `InfrastructureBuildingService.Demolish(building)` — `registry.Deregister(contributor)` before `Destroy(go)`. Wire into demolition entry point. |
| T7.4 | Tier promotion | _pending_ | _pending_ | Implement `PromoteIfEligible(contributor)` called on `OnGameDay` — compare accumulated output vs `def.tierThresholds[currentTier]`; increment `currentTier` (clamped to `tierCount - 1`) when exceeded. No demotion. |
| T7.5 | EconomyManager infrastructure line | _pending_ | _pending_ | Edit `EconomyManager.cs` — new `infrastructureBudget` line. `DeductConstruction(def.constructionCost)` on place; `DeductMaintenance(Σ def.dailyMaintenance)` on `OnGameDay`. Distinct from Zone S budget. |
| T7.6 | Lifecycle EditMode tests | _pending_ | _pending_ | Add tests: place → registered in registry; demolish → deregistered; tier promotes at threshold; maintenance deducted daily; construction cost deducted on place. |
