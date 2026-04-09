# TECH-31d — City metrics and test mode (**TECH-82** Phase 1)

**Program:** [TECH-31-agent-scenario-generator-program.md](TECH-31-agent-scenario-generator-program.md) **Stage 31d**.  
**Backlog (authoritative for schema/MCP):** [TECH-82](../BACKLOG.md) — Phase 1 **`city_metrics_history`**, **`MetricsRecorder`**, **`city_metrics_query`**.  
**Consumer:** [TECH-31](../BACKLOG.md) **test mode** **city history** assertions.  
**Technical spec:** [`.cursor/projects/TECH-82.md`](../.cursor/projects/TECH-82.md).  
**Prerequisite stages:** **31c** recommended so file-based verification exists before adding DB assertions; **31a** minimum for **test mode** runs.

## Summary

This stage is the **program checkpoint** where **TECH-82** Phase 1 lands (migrations, **Unity** **`MetricsRecorder`**, **Postgres** bridge writes, **MCP** query) and **TECH-31** **test mode** opts into recording so agents can assert **city history** over **N** **simulation ticks** when **`DATABASE_URL`** is set. **Scenarios** remain **save**-authoritative; DB rows are **verification** only.

## Goals (**TECH-82** Phase 1 — see **TECH-82** spec for detail)

- Per-tick snapshots into **`city_metrics_history`** after **`SimulationManager`** tick processing.
- **`city_metrics_query`** (or equivalent) documented for agents.
- Game fully playable without **Postgres** (fire-and-forget when configured).

## Goals (**TECH-31**-specific slice)

- **Test mode** run tags or correlates metrics with **scenario id** when both systems integrate (exact shape: **TECH-82** **Open Questions** + **Implementation investigation** in [`.cursor/projects/TECH-31.md`](../.cursor/projects/TECH-31.md) / **TECH-82** spec).
- Example assertion: load scenario → **N** ticks → query metrics / enrich **`debug_context_bundle`** (if spec’d).
- **Degradation:** when DB absent, **31c**-style file assertions still pass.

## Non-goals

- **TECH-82** Phases 2–4 (events, grid snapshots, buildings table)—unless pulled forward by explicit **Decision Log** on **TECH-82**.
- **MCP** scenario resolver (31e).

## Implementation checklist

- [ ] Complete **TECH-82** Phase 1 per [`.cursor/projects/TECH-82.md`](../.cursor/projects/TECH-82.md) **Implementation Plan**.
- [ ] Wire **test mode** / **MetricsRecorder** so scenario runs populate history when DB configured.
- [ ] Document example **city history** assertion for one reference scenario.
- [ ] Resolve or document **scenario id** correlation (metadata column vs run id—**TECH-82** + program investigation notes).

## Test contracts (stage)

| Goal | Check | Notes |
|------|--------|--------|
| Metrics after **N** ticks | **MCP** / SQL / agent | Requires **Postgres** + bridge |
| No-DB regression | **31c** checks still green | Degradation path |

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
|  |  |  |

## Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |
