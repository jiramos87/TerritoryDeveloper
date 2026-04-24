### Stage 5 — Happiness Migration + Warmup / DesirabilityComposer + FEAT-43 Toggle

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `DesirabilityComposer` with explicit FEAT-43 toggle; wire into `AutoZoningManager` and `GrowthManager`; create shell MonoBehaviours for `ConstructionStageController` + `DensityEvolutionTicker` with pre-wired Inspector refs (Step 4 fills in logic).

**Exit:**

- `DesirabilityComposer.CellValue(x,y)` returns float in `[0,1]` (clamp at composer boundary per Example 3).
- `AutoZoningManager` + `GrowthManager` use `DesirabilityComposer` when `useSignalDesirability = true`; toggle off = old path unchanged.
- `ConstructionStageController` + `DensityEvolutionTicker` shell files compile with no-op `SetDesirabilitySource` stub.
- `npm run validate:all` passes.
- Phase 1 — DesirabilityComposer MonoBehaviour + FEAT-43 toggle.
- Phase 2 — AutoZoningManager/GrowthManager wiring + Step 4 shell files.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | DesirabilityComposer MonoBehaviour | _pending_ | _pending_ | `DesirabilityComposer` MonoBehaviour — per-cell value = weighted sum of `LandValue` + `ServicePolice` + `ServiceFire` + `ServiceEducation` + `ServiceHealth` + `ServiceParks` signals (weights Inspector-tunable); `CellValue(x,y)` returns `Mathf.Clamp01(sum)` — clamp at composer boundary per Example 3 (not at consumer site); `[SerializeField] SignalFieldRegistry` + `FindObjectOfType` fallback. |
| T5.2 | FEAT-43 explicit toggle | _pending_ | _pending_ | Add `[SerializeField] public bool useSignalDesirability = false` to `AutoZoningManager` + `GrowthManager`; when true, replace existing desirability reads with `desirabilityComposer.CellValue(x,y)`; when false, old path unchanged. NOT parallel A/B (locked decision). Add `[SerializeField] DesirabilityComposer desirabilityComposer` + `FindObjectOfType` fallback to both. |
| T5.3 | AutoZoningManager + GrowthManager wiring | _pending_ | _pending_ | In `AutoZoningManager.ProcessTick` wrap desirability reads in `if (useSignalDesirability)` guard routing to `desirabilityComposer.CellValue(x,y)`. Same in `GrowthManager` growth-ring expansion. Wire `DesirabilityComposer` Inspector ref in both. Old path remains functional when toggle off. |
| T5.4 | ConstructionStageController + DensityEvolutionTicker shells | _pending_ | _pending_ | Create `ConstructionStageController.cs` + `DensityEvolutionTicker.cs` shell MonoBehaviours (new, `Assets/Scripts/Simulation/`) — each has `[SerializeField] DesirabilityComposer desirabilityComposer` + `FindObjectOfType` fallback + no-op `SetDesirabilitySource(DesirabilityComposer c)` stub; compiles cleanly; Step 4 stages fill in all logic. |

---
