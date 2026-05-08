---
purpose: "TECH-292 — MetricsRecorder integration per TECH-82 Phase 1."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-292 — MetricsRecorder integration per TECH-82 Phase 1

> **Issue:** [TECH-292](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Stage 2.2 scope-slice of **TECH-82** Phase 1. Land `MetricsRecorder.cs` that fires fire-and-forget per `SimulationManager` **tick**; apply `city_metrics_history` migration; ship `mcp__territory-ia__city_metrics_query` tool returning time-series. Game fully playable w/o Postgres. Does NOT subsume TECH-82 — TECH-82 retains Phases 2–4 (`city_events`, `grid_snapshots`, `buildings`).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `MetricsRecorder.cs` (new) — MonoBehaviour scene component per invariant guardrail (new manager rule); fires-and-forgets per tick.
2. `city_metrics_history` Postgres table migration applied via `db/migrations/`.
3. Bridge scripts in `tools/postgres-ia/` write per-tick rows w/o blocking main thread.
4. MCP tool `city_metrics_query` in `tools/mcp-ia-server/src/` returns time-series.
5. Game playable w/o Postgres — connection failure must not crash tick path.
6. Per-tick metrics exposed: population, employment rate, treasury, demand R/C/I (subset per TECH-82 §entity model — FEAT-51 primary consumer).

### 2.2 Non-Goals

1. TECH-82 Phases 2–4 (events / snapshots / buildings).
2. FEAT-51 dashboard rendering — Stage 2.3 owns.
3. Postgres installer scripts — `docs/postgres-ia-dev-setup.md` covers.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Query city metrics time-series via MCP | `city_metrics_query {city_id, from, to}` returns rows |
| 2 | Player | Game runs w/o Postgres installed | No crash; no lag spikes |

## 4. Current State

### 4.1 Domain behavior

**TECH-82** spec describes four-phase entity model (see `ia/projects/TECH-82.md`). No runtime recorder wired yet. FEAT-51 dashboard blocked on Phase 1 data.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/SimulationManager.cs` — tick call site.
- `Assets/Scripts/Managers/MetricsRecorder.cs` — new helper MonoBehaviour.
- `db/migrations/` — SQL migration files.
- `tools/postgres-ia/` — bridge + preflight scripts.
- `tools/mcp-ia-server/src/` — `city_metrics_query` tool registration.
- Reference spec: `ia/projects/TECH-82.md` §Phase 1 acceptance; `docs/postgres-ia-dev-setup.md`.
- Invariants: #3 (cache refs in `Awake`), #6 (helper class — not `GridManager`), new-manager rule (Inspector + `FindObjectOfType` fallback).

## 5. Proposed Design

### 5.1 Target behavior (product)

Per-tick metrics written to Postgres when available. Game tick unaffected by DB connection state. MCP query returns time-series for dashboard + agent introspection.

### 5.2 Architecture / implementation

1. `MetricsRecorder` MonoBehaviour — `[SerializeField]` refs to `SimulationManager`, `DemandManager`, `EmploymentManager`, `StatisticsManager`; `FindObjectOfType` fallback in `Awake`.
2. Subscribe to `SimulationManager.OnTick` (add event if absent; light-touch).
3. On tick → enqueue sample → async bridge call → swallow + log on failure.
4. Fire-and-forget queue bounded (drop oldest if DB slow); zero allocation per enqueue (pooled struct).
5. `db/migrations/NNNN_city_metrics_history.sql` — schema per TECH-82 §entity model.
6. MCP tool `city_metrics_query` — params: `city_id`, `from_tick`, `to_tick`, `metric_names[]`; returns rows.
7. Preflight check: Postgres unreachable → recorder disables itself; log once; no per-tick log spam.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Fire-and-forget bounded queue | Game playable w/o Postgres per TECH-82 constraint | Sync write — blocks tick; unbounded queue — memory leak risk |
| 2026-04-17 | Scope-slice TECH-82 Phase 1 into TECH-292 | Stage 2.2 requires integration under its own id for tracking | File under TECH-82 directly — conflates Phase 2–4 scope |

## 7. Implementation Plan

### Phase 1 — Recorder + schema + MCP tool

- [ ] `db/migrations/NNNN_city_metrics_history.sql` lands; `npm run db:migrate` applies.
- [ ] `tools/postgres-ia/` bridge script for recorder rows.
- [ ] `Assets/Scripts/Managers/MetricsRecorder.cs` (new) — tick hook, bounded queue, async flush.
- [ ] `SimulationManager` emits `OnTick` event (if missing).
- [ ] `tools/mcp-ia-server/src/` registers `city_metrics_query` tool + tests.
- [ ] Preflight failure path — recorder disables + single log line.
- [ ] Verify: Postgres off → game runs; Postgres on → rows land; MCP tool returns time-series.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Migration applies | Node | `npm run db:migrate` | Dev Postgres :5434 |
| MCP tool returns rows | Node | `tools/mcp-ia-server/tests/` | Unit test on `city_metrics_query` |
| Game playable w/o DB | Testmode | `npm run unity:testmode-batch` | Stop Postgres; run smoke |
| Compile clean | Unity | `npm run unity:compile-check` | |
| IA consistency | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] Per-tick rows land in `city_metrics_history` when Postgres up.
- [ ] Game runs zero-crash, zero-stutter w/ Postgres down.
- [ ] MCP `city_metrics_query` returns time-series per `city_id` + range.
- [ ] TECH-82 Phase 1 acceptance bullets all met.
- [ ] Invariant #3 + #6 + new-manager rule preserved.
- [ ] `validate:all` + `unity:compile-check` clean.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. Exact metric set for Phase 1 — population / employment rate / treasury / demand R-C-I confirmed minimum? Add cell count / tax rate? Lock w/ FEAT-51 author before schema lands.
