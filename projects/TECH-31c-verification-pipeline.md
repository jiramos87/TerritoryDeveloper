# TECH-31c — Verification pipeline (file-based)

**Program:** [TECH-31-agent-scenario-generator-program.md](TECH-31-agent-scenario-generator-program.md) **Stage 31c**.  
**Backlog:** [TECH-31](../BACKLOG.md).  
**Prerequisite stages:** **31a**, **31b** (or a hand-authored committed save if **31b** is not yet merged—document exception in **Decision Log**).

## Summary

Automate: load scenario in **test mode** → run **N** **simulation ticks** → assert or emit report JSON using **`debug_context_bundle`**, golden files, and/or **UTF**—**without** requiring **TECH-82** **Postgres** metrics. Document a **bounded** **N** for **CI**. Compose with [`.cursor/skills/close-dev-loop/SKILL.md`](../.cursor/skills/close-dev-loop/SKILL.md).

## Goals

- **UTF** and/or scripted **Play Mode** path with one-command local run; optional **CI** when driver exists (**TECH-15** / **TECH-16**).
- **close-dev-loop** recipe for the reference scenario (seed cells, bundle fields).
- Determinism: document **RNG** seed and tick count for any golden JSON.

## Non-goals

- **`city_metrics_query`** / **TECH-82** time-series (31d). **MCP** orchestration (31e).

## Risks

| Risk | Mitigation |
|------|------------|
| Flaky goldens | Prefer stable integer **CityStats** fields; document float tolerance if unavoidable. |
| **Three sources of truth** | Align expected values across stage **Test contracts**, descriptor `expected`, and [TECH-31 BACKLOG Acceptance](../BACKLOG.md). |

## Implementation checklist

- [ ] Scripted load → **N** ticks → assert or report.
- [ ] Document **close-dev-loop** + **`debug_context_bundle`** for reference scenario.
- [ ] Document max **N** and CLI args for **CI**.

## Test contracts (stage)

| Goal | Check | Notes |
|------|--------|--------|
| Automated run on clean tree | **UTF** or scripted | Per driver chosen in **31a** |
| Compile | `npm run unity:compile-check` |  |

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
|  |  |  |

## Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |
