### Stage 5 — Infrastructure buildings + terrain-sensitive placement / Infrastructure category + building def SO

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Data-model scaffolding for infrastructure buildings. Category tag, ScriptableObject def, five authored archetype assets. No runtime placement yet.

**Exit:**

- `InfrastructureCategory` enum distinct from Zone S classification.
- `InfrastructureBuildingDef` SO w/ fields listed in Step 2 exit criteria; `OnValidate` clamps `tierCount` to 2–3, `tierThresholds.Length == tierCount - 1`, `tierMultipliers.Length == tierCount`.
- Five archetype `.asset` files authored + fields populated (rates, thresholds, terrain requirements, costs).
- `TerrainRequirement` enum (`None`, `AdjacentWater`, `AdjacentWaterPollutesDownstream`, `Mountain`, `OpenTerrain`).
- Phase 1 — Category + terrain-requirement enums.
- Phase 2 — `InfrastructureBuildingDef` SO w/ `OnValidate`.
- Phase 3 — Author five archetype assets.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | InfrastructureCategory enum | _pending_ | _pending_ | Add `Assets/Scripts/Data/Buildings/InfrastructureCategory.cs` — enum distinct from existing Zone S tags. XML doc explains split rationale (own cost line, not Zone S budget). |
| T5.2 | TerrainRequirement enum | _pending_ | _pending_ | Add `TerrainRequirement.cs` — `None`, `AdjacentWater`, `AdjacentWaterPollutesDownstream`, `Mountain`, `OpenTerrain`. Consumed by placement validator in Stage 2.2. |
| T5.3 | InfrastructureBuildingDef SO | _pending_ | _pending_ | Add `InfrastructureBuildingDef.cs` ScriptableObject — `UtilityKind kind`, `float baseProductionRate`, `TerrainRequirement terrainReq`, `int tierCount`, `float[] tierThresholds`, `float[] tierMultipliers`, `int constructionCost`, `int dailyMaintenance`. `OnValidate` clamps tier arrays. |
| T5.4 | Author 5 archetype assets | _pending_ | _pending_ | Create `Assets/Data/Infrastructure/CoalPlant.asset`, `SolarFarm.asset`, `WindFarm.asset`, `WaterTreatment.asset`, `SewageTreatment.asset`. Populate rates + terrain reqs per Implementation Points §9. |
