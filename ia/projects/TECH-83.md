# TECH-83 — Agent-driven simulation parameter tuning

> **Issue:** [TECH-83](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-07
> **Last updated:** 2026-04-07

## 1. Summary

Create MCP tools to read, modify, and evaluate simulation parameters at runtime — growth budget percentages, demand decay rates, desirability multipliers, ring boundary fractions, road extension probabilities — enabling agents to A/B test parameter changes, run N simulation ticks, measure outcomes, and recommend optimal values. Replaces manual magic-number tuning with data-driven agent experimentation.

## 2. Goals and Non-Goals

### 2.1 Goals

1. MCP tool `sim_params_read()` — returns all tunable simulation parameters with current values, ranges, and descriptions
2. MCP tool `sim_params_write(params)` — modify parameters at runtime via Unity bridge command
3. MCP tool `sim_experiment(params, ticks, metrics)` — set parameters, run N simulation ticks, measure specified metrics, return results
4. Parameter catalog: document all magic numbers in simulation managers with semantic names, valid ranges, and default values
5. Experiment results stored in Postgres for comparison across runs

### 2.2 Non-Goals (Out of Scope)

1. Player-facing parameter UI (this is agent/developer tooling)
2. Automatic parameter optimization (ML/evolutionary) — agents decide what to try based on results
3. Modifying non-simulation parameters (rendering, UI, audio)
4. Changing the simulation tick architecture or execution order

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | As an agent tuning FEAT-43 (growth rings), I want to try 3 different ring boundary fractions and compare the zone distribution after 100 ticks each | `sim_experiment({ ring_inner: 0.3, ring_mid: 0.6 }, ticks: 100, metrics: ["zone_r_count", "zone_c_count", "zone_i_count", "development_gradient"])` returns per-tick metrics; repeat with different fractions; compare |
| 2 | AI agent | As an agent debugging BUG-52 (AUTO zoning gaps), I want to increase `autoZoningCandidateRadius` from 2 to 3 and see if gaps disappear | `sim_params_write({ autoZoningCandidateRadius: 3 })` → run 50 ticks → compare gap cell count vs baseline |
| 3 | Developer | As a developer, I want to see all tunable parameters with their current values | `sim_params_read()` returns structured catalog with parameter name, current value, default, range, description, owning manager |
| 4 | Developer | As a developer tuning **monthly maintenance** or simulation balance, I want to experiment with costs without recompiling | `sim_params_write({ roadMaintenanceCostPerCell: 5.0 })` modifies at runtime |
| 5 | IA maintainer | As a maintainer, I want experiment results persisted so agents can reference prior tuning sessions | Postgres table `sim_experiments` stores params, tick count, metric snapshots, and comparison notes |

## 4. Current State

### 4.1 Domain behavior

Simulation parameters are hardcoded as constants or `[SerializeField]` fields in MonoBehaviour managers. Changing them requires code edits and recompilation. No systematic catalog exists — TECH-03 (extract magic numbers) tracks the technical debt but hasn't shipped. Agents tuning simulation behavior must guess-and-check via code edits, compile, Play Mode, observe.

Key parameters scattered across:
- `GrowthBudgetManager` — growth budget percentage
- `DemandManager` — demandSmoothingPerDay (0.2), demandDecayRate (0.1), desirabilityDemandMultiplier (0.1)
- `UrbanCentroidService` — ring boundary fractions, urban radius
- `AutoRoadBuilder` — road extension probability, max extensions per tick
- `AutoZoningManager` — zoning candidate radius, max zones per tick
- `AutoResourcePlanner` — resource planning thresholds
- `EconomyManager` — tax rate defaults, limits

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/SimulationManager.cs` — tick orchestration
- `Assets/Scripts/Managers/GameManagers/*.cs` — parameter-owning managers
- `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` — Unity bridge command dispatch (extension point)
- `tools/mcp-ia-server/src/tools/unity-bridge-command.ts` — MCP bridge tool
- `tools/postgres-ia/` — bridge scripts
- `db/migrations/` — migration infrastructure

## 5. Proposed Design

### 5.1 Target behavior (product)

**Parameter catalog (returned by `sim_params_read`):**

```json
{
  "params": [
    {
      "name": "demandSmoothingPerDay",
      "manager": "DemandManager",
      "current": 0.2,
      "default": 0.2,
      "range": [0.01, 1.0],
      "description": "How quickly demand adjusts per simulation day (higher = faster response)"
    },
    {
      "name": "ringInnerFraction",
      "manager": "UrbanCentroidService",
      "current": 0.33,
      "default": 0.33,
      "range": [0.1, 0.5],
      "description": "Fraction of urban radius that defines the inner growth ring boundary"
    }
  ]
}
```

**Experiment workflow:**

```
# Agent reads current params
sim_params_read()

# Agent designs experiment: try wider inner ring
sim_experiment({
  params: { ringInnerFraction: 0.4 },
  ticks: 100,
  metrics: ["population", "zone_r_count", "development_gradient"],
  baseline: true  // also run 100 ticks with current params for comparison
})
→ {
    experiment_id: "exp_001",
    baseline: { tick_100: { population: 450, zone_r_count: 85, development_gradient: 0.72 } },
    variant:  { tick_100: { population: 430, zone_r_count: 78, development_gradient: 0.81 } },
    delta: { population: -20, zone_r_count: -7, development_gradient: +0.09 },
    notes: "Wider inner ring produces smoother gradient but slightly less total growth"
  }
```

**Development gradient metric (example derived metric):**

```
development_gradient = correlation(ring_distance, zone_density) 
// -1.0 = perfect inverse (more development near center) = ideal
// 0.0 = no pattern
// +1.0 = more development far from center = inverted
```

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Implementation approach left to the implementing agent. Key considerations: C# parameter reflection or explicit catalog registration, Unity bridge command extensions for param read/write, experiment isolation (snapshot/restore state before/after), metric collection hooks in SimulationManager.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-07 | Agent-driven experimentation, not automated optimization | Agents have domain context to design meaningful experiments; black-box optimization might find degenerate solutions | Evolutionary parameter search; grid search |
| 2026-04-07 | Runtime modification via Unity bridge, not code generation | Faster iteration (no recompile); uses existing bridge infrastructure; parameters restored after experiment | Code generation + hot reload; ScriptableObject swap |

## 7. Implementation Plan

### Phase 1 — Parameter catalog

- [ ] Catalog all simulation parameters with names, ranges, defaults, owning managers
- [ ] `sim_params_read` MCP tool (bridge command → C# reflection or explicit registry → response)
- [ ] Documentation of all cataloged parameters

### Phase 2 — Runtime parameter modification

- [ ] `sim_params_write` MCP tool (bridge command → C# sets fields at runtime)
- [ ] Parameter validation (range checks, type checks)
- [ ] State snapshot/restore for experiment isolation

### Phase 3 — Experiment framework

- [ ] `sim_experiment` MCP tool: set params → run N ticks → collect metrics → return results
- [ ] Experiment results persisted in Postgres
- [ ] Baseline comparison mode

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Parameter catalog complete and accurate | Manual | Compare `sim_params_read` output with actual C# field values in Inspector | |
| Write → Read roundtrip | Play Mode / MCP | `sim_params_write` then `sim_params_read` returns new values | |
| Experiment doesn't corrupt game state | Play Mode | Run experiment, verify game state restored to pre-experiment values | Critical |
| Game compiles | MCP / dev machine | `unity_compile` | |

## 8. Acceptance Criteria

- [ ] `sim_params_read` returns catalog of all tunable simulation parameters with current values, defaults, ranges, descriptions
- [ ] `sim_params_write` modifies parameters at runtime without recompilation
- [ ] `sim_experiment` runs N ticks with specified parameters, collects metrics, returns structured results with optional baseline comparison
- [ ] Game state restored after experiment (no corruption)
- [ ] Experiment results stored in Postgres for cross-session comparison
- [ ] Graceful degradation when Postgres unavailable (experiment runs but results not persisted)
- [ ] Documented in `docs/mcp-ia-server.md`

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria. Developer policy question: should `sim_params_write` changes persist across Play Mode sessions, or reset on exit?
