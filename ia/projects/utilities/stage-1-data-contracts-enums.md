### Stage 1 — Pool core + contributor registry / Data contracts + enums

**Status:** In Progress — 4 tasks filed (TECH-331..TECH-334, all Draft)

**Objectives:** Define the five value types + two interfaces the services operate on. No runtime logic — just typed scaffolding other stages consume.

**Exit:**

- `UtilityKind` enum (`Water`, `Power`, `Sewage`), `ScaleTag` enum (`City`, `Region`, `Country`), `PoolStatus` enum (`Healthy`, `Warning`, `Deficit`).
- `PoolState` struct w/ `float net`, `float ema`, `PoolStatus status`, `int consecutiveNegativeEmaTicks`, `int consecutivePositiveEmaTicks` (hysteresis counters).
- `IUtilityContributor` interface: `UtilityKind Kind`, `float ProductionRate`, `ScaleTag Scale`.
- `IUtilityConsumer` interface: `UtilityKind Kind`, `float ConsumptionRate`, `ScaleTag Scale`.
- Files compile clean (`npm run unity:compile-check`); no references to the new types from runtime code yet.
- Phase 1 — Enum + struct scaffolding.
- Phase 2 — Interface contracts + assembly wiring.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Utility enums | **TECH-331** | Draft | Add `Assets/Scripts/Data/Utilities/UtilityKind.cs`, `ScaleTag.cs`, `PoolStatus.cs` — plain enums, no behavior. XML doc each value (e.g. `Water` → "potable supply pool"). |
| T1.2 | PoolState struct | **TECH-332** | Draft | Add `Assets/Scripts/Data/Utilities/PoolState.cs` — blittable struct w/ `net`, `ema`, `status`, `consecutiveNegativeEmaTicks`, `consecutivePositiveEmaTicks`. Default ctor sets `Healthy` + zeros. |
| T1.3 | Contributor/consumer interfaces | **TECH-333** | Draft | Add `IUtilityContributor.cs` + `IUtilityConsumer.cs` under `Assets/Scripts/Data/Utilities/`. Read-only properties; implementations land in Step 2. |
| T1.4 | Assembly + compile check | **TECH-334** | Draft | Add `Utilities.asmdef` under `Assets/Scripts/Data/Utilities/` (if repo uses asmdefs) OR ensure types live in main asm; run `npm run unity:compile-check` green. |

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 1 Task specs (TECH-331..334) aligned w/ Stage block + §Plan Author + backlog yaml `parent_plan` / `task_key` mirror. No fix tuples. Downstream: `/ship-stage` Pass 1 per task.
