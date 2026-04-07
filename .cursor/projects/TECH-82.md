# TECH-82 — Entity model for gameplay database (time-series, events, snapshots)

> **Issue:** [TECH-82](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-07
> **Last updated:** 2026-04-07

## 1. Summary

Extend the Postgres infrastructure from agent/debug use to gameplay data persistence: time-series city metrics, financial event sourcing, grid state snapshots, and building identity tracking. Enables the game data dashboard (FEAT-51), financial audit trails (FEAT-21), simulation analysis, and building lifecycle tracking (FEAT-08) — all with graceful degradation when Postgres is unavailable.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `city_metrics_history` table: per-tick snapshots of key city metrics (population, money, happiness, demand R/C/I, employment, forest coverage)
2. `city_events` table: event-sourced financial transactions (tax income, road expense, building expense, service expense, demolition refund)
3. `grid_snapshots` table: periodic compressed grid state for diffing and analysis
4. `buildings` table: individual building identity with construction date, upgrade history, and demolition tracking
5. MCP tools for querying gameplay data: `city_metrics_query`, `city_events_query`, `grid_snapshot_diff`
6. Unity-side `MetricsRecorder` helper class that writes data via the existing Node.js bridge pattern
7. All gameplay database features are optional enrichment — the game must remain fully playable without Postgres

### 2.2 Non-Goals (Out of Scope)

1. Replacing the existing `GameSaveData` save/load pipeline (this runs alongside it)
2. Real-time gameplay that depends on database reads (all game logic stays in Unity C#)
3. Multiplayer or networked database access
4. Production deployment infrastructure (this is dev/analytics tooling that enriches gameplay analysis)

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | As a player viewing the game data dashboard (FEAT-51), I want to see population growth, income trends, and demand history as charts | `city_metrics_query(metric: "population", last_n_ticks: 100)` returns time-series data for chart rendering |
| 2 | Player | As a player, I want to see where my money goes: "Last month: $5000 tax income, $3000 road maintenance, $1500 services" | `city_events_query(kind: "all", last_n_days: 30)` returns categorized financial events |
| 3 | Developer | As a developer diagnosing BUG-52 (AUTO zoning gaps), I want to diff grid state between tick 50 and tick 100 to see which cells were zoned | `grid_snapshot_diff(tick_a: 50, tick_b: 100)` returns changed cells with before/after zone types |
| 4 | Developer | As a developer tuning FEAT-43 (growth rings), I want to see how development spreads over 200 simulation ticks | Query `city_metrics_history` for zone count progression + `grid_snapshots` for spatial distribution |
| 5 | AI agent | As an agent debugging a simulation anomaly, I want `debug_context_bundle` to include recent metric trends and event history for the seed cell's neighborhood | Bundle includes last 10 ticks of metrics + recent events near the seed cell |

## 4. Current State

### 4.1 Domain behavior

**What gets persisted today:** CellData (per-cell state), CityStatsData (aggregate counts), WaterMapData, GrowthBudgetData, InGameTime — all in a single `GameSaveData` JSON blob.

**What's lost on save/load:**
- StatisticsManager trend history (30-value rolling windows, runtime only)
- Financial transaction details (only current balance persisted)
- Building construction/upgrade/demolition timestamps
- Grid state history (no diffing possible)
- DemandManager per-cycle building deltas

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/SimulationManager.cs` — tick orchestration (hook point for MetricsRecorder)
- `Assets/Scripts/Managers/GameManagers/EconomyManager.cs` — SpendMoney/AddMoney (hook point for events)
- `Assets/Scripts/Managers/GameManagers/CityStats.cs` — aggregate city metrics
- `Assets/Scripts/Managers/UnitManagers/Cell.cs` / `CellData.cs` — per-cell state
- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — save/load pipeline
- `tools/postgres-ia/` — existing Node.js bridge scripts
- `db/migrations/` — migration infrastructure

## 5. Proposed Design

### 5.1 Target behavior (product)

**Phase 1 — Time-series metrics (enables FEAT-51 dashboard):**

After each simulation tick, a lightweight `MetricsRecorder` captures key values and writes them via the Postgres bridge. The dashboard reads this data for charts.

Example data flow:
```
SimulationManager.ProcessSimulationTick()
  → MetricsRecorder.RecordTick()
    → { game_date: "0001-03-15", population: 1250, money: 45000, happiness: 72.3,
        demand_r: 35, demand_c: 20, demand_i: -5, employment: 0.85, forest_coverage: 0.12 }
    → INSERT into city_metrics_history (fire-and-forget)
```

**Phase 2 — Financial events (enables FEAT-21 expenses):**

EconomyManager wraps money operations to emit categorized events:

```
EconomyManager.SpendMoney(3000, "road_maintenance", { road_count: 150 })
  → INSERT into city_events (kind: "road_expense", amount: -3000, details: {...})
```

**Phase 3 — Grid snapshots (enables simulation analysis):**

Periodic snapshots (every N ticks or on save) capture compressed grid state:

```
MetricsRecorder.CaptureSnapshot(every: 10 ticks)
  → INSERT into grid_snapshots (game_date, snapshot_kind: "periodic", data: compressed CellData[])
```

**Phase 4 — Building identity (enables FEAT-08 density evolution):**

New `buildingId` field on Cell/CellData. Building lifecycle tracked:

```
ZoneManager.PlaceBuilding(cell, type, density)
  → buildings INSERT (building_id: 42, cell: [5,10], zone_type: "residential", density: "light", constructed_at: game_date)
GrowthManager.UpgradeBuilding(cell, new_density)
  → buildings UPDATE (upgraded_at: game_date, density: "medium")
```

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Implementation approach left to the implementing agent. Key design constraint: all database writes must be fire-and-forget. The game never waits for a DB response during gameplay. Buffer in memory if DB is unavailable; discard gracefully if buffer fills.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-07 | Optional enrichment, not required for gameplay | Game must work without Postgres; this is analytics/dashboard infrastructure | Required dependency; SQLite embedded |
| 2026-04-07 | Use existing Node.js bridge pattern | Proven infrastructure (agent_bridge_job); avoids new C#→Postgres dependency | Direct C# Npgsql; REST API; file-based logging |
| 2026-04-07 | Four phases, each independently valuable | Each phase enables specific backlog items without waiting for the full system | Ship all at once; minimal viable only |

## 7. Implementation Plan

### Phase 1 — Time-series infrastructure

- [ ] Design `city_metrics_history` table and migration
- [ ] MetricsRecorder helper class (C#) with bridge write
- [ ] `city_metrics_query` MCP tool
- [ ] Integration with SimulationManager tick

### Phase 2 — Financial events

- [ ] Design `city_events` table and migration
- [ ] EconomyManager event emission wrapper
- [ ] `city_events_query` MCP tool

### Phase 3 — Grid snapshots

- [ ] Design `grid_snapshots` table and migration
- [ ] Periodic snapshot capture logic
- [ ] `grid_snapshot_diff` MCP tool

### Phase 4 — Building identity

- [ ] Design `buildings` table and migration
- [ ] Add `buildingId` to Cell/CellData (save/load compatible)
- [ ] Building lifecycle tracking in ZoneManager/GrowthManager

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tables created and migrations run | Node | Migration scripts | `npm run db:migrate` |
| MCP tools registered and functional | Node | `npm run verify` + `npm run test:ia` | Repo root |
| Game playable without Postgres | Manual | Start game without DB configured; verify no errors | Critical acceptance gate |
| MetricsRecorder does not block simulation tick | Manual / profiling | Measure tick time with/without recording | Performance acceptance gate |

## 8. Acceptance Criteria

- [ ] Phase 1: `city_metrics_history` populated per simulation tick; `city_metrics_query` MCP tool returns time-series data
- [ ] Phase 2: `city_events` records financial transactions; `city_events_query` returns categorized events
- [ ] Phase 3: `grid_snapshots` captured periodically; `grid_snapshot_diff` returns cell-level changes
- [ ] Phase 4: `buildings` table tracks individual building lifecycle with construction/upgrade/demolition dates
- [ ] All phases: graceful degradation when Postgres unavailable — game fully playable
- [ ] All phases: fire-and-forget writes, no gameplay blocking
- [ ] Documented in `docs/mcp-ia-server.md` and `docs/postgres-ia-dev-setup.md`

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## Open Questions (resolve before / during implementation)

1. What is the optimal snapshot frequency for grid state? Every 10 ticks? Every 50? Should it be configurable?
2. Should building identity survive across save/load cycles, or is it session-scoped?
3. How much metric history should be retained? Pruning policy (e.g., keep last 1000 ticks, aggregate older data)?
