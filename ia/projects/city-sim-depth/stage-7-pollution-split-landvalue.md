### Stage 7 — New Simulation Signals / Pollution Split + LandValue

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `PollutionLand` + `PollutionWater` producer/sink tables (completing 3-type pollution split from `PollutionAir` Step 2) and `LandValue` producer with tax-base wiring.

**Exit:**

- `PollutionLand` non-zero near chemical plants / landfills.
- `PollutionWater` non-zero near water-adjacent industry; diffusion bounded to water cells.
- `LandValue` rises near high-service, low-pollution, high-density zones.
- `CityStats` tax base receives non-zero land-value bonus in high-density city.
- EditMode smoke test per signal passes.
- Phase 1 — PollutionLand + PollutionWater producers/sinks.
- Phase 2 — LandValue producer + CityStats tax base wiring.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | PollutionLand producer/sink | _pending_ | _pending_ | Implement `ISignalProducer` for `PollutionLand` on industrial buildings (manufacturing/chemical sub-types) + landfill buildings; sinks: forest (lower weight than PollutionAir), parks; per-building weight table in `SignalMetadataRegistry` PollutionLand entry; diffusion radius=4, standard separable Gaussian. |
| T7.2 | PollutionWater producer/sink | _pending_ | _pending_ | Implement `ISignalProducer` for `PollutionWater` on water-adjacent industrial cells (checked via `WaterManager` membership); diffusion kernel gated to water cells only (skip dry cells in kernel traversal); sinks: wetland cells + water treatment buildings; diffusion radius=5 along water connectivity. |
| T7.3 | LandValue producer | _pending_ | _pending_ | `LandValueProducer` implements `ISignalProducer` — per-cell `LandValue = densityWeight * zoneLevel + serviceBonus - pollutionPenalty`; `serviceBonus` reads mean of 5 ServiceXxx signals; `pollutionPenalty` reads mean of 3 PollutionXxx signals; updates monthly tick only (not daily); weights in `SignalMetadataRegistry` LandValue entry. |
| T7.4 | LandValue tax base wiring + test | _pending_ | _pending_ | Edit `CityStats.cs` — monthly tax income += `SignalFieldRegistry.GetField(LandValue).Sum() * [SerializeField] float landValueTaxRate`; EditMode test: city with 5 high-service R-high cells after 30 days → monthly tax bonus > 0 vs baseline with no service coverage. |

---
