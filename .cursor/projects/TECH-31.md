# TECH-31 — Agent scenario generator (orchestrator stub)

> **Issue:** [TECH-31](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-09

**Program tracker (stages, progress, lessons):** [`projects/TECH-31-agent-scenario-generator-program.md`](../../projects/TECH-31-agent-scenario-generator-program.md).  
**Implementation specs (sequential):** [31a — test mode + load](../../projects/TECH-31a-test-mode-and-load.md) → [31a2 — batch tooling](../../projects/TECH-31a2-batch-testmode-tooling.md) → [31a3 — agent test-mode verify skill](../../projects/TECH-31a3-agent-test-mode-verify-skill.md) → [31b — builder](../../projects/TECH-31b-scenario-builder.md) → [31c — verification](../../projects/TECH-31c-verification-pipeline.md) → [31d — **TECH-82** Phase 1 + metrics](../../projects/TECH-31d-city-metrics-TECH-82-phase1.md) → [31e — MCP](../../projects/TECH-31e-mcp-tool-and-workflows.md).

Normative detail for each stage lives in those **`projects/TECH-31*.md`** files. This stub retains **BACKLOG** **`Spec:`** resolution, **Open Questions** (game logic), and aggregate **Test contracts** for **MCP** / closeout tools that read `.cursor/projects/TECH-31.md` only.

**Related:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md); [`.cursor/skills/close-dev-loop/SKILL.md`](../skills/close-dev-loop/SKILL.md); **glossary** **IDE agent bridge**, **`debug_context_bundle`**; [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md). **Spec-pipeline:** glossary **territory-ia spec-pipeline program** — [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md). **TECH-82** backlog + [`.cursor/projects/TECH-82.md`](TECH-82.md) for **31d** / **city metrics**.

## Summary

Build an **agent-facing** **scenario generator**: structured intent → **`GameSaveData`-compatible** artifact + **scenario id**; **test mode** on **32×32**; bounded **simulation** work; exports/assertions; final **MCP** tool. **Not in scope:** player-facing editors, **TECH-35** property-based generation.

## Test contracts (aggregate)

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|--------|
| Reference scenario loads in **test mode** | Unity **UTF** or scripted run | One-command local + optional **CI** | Stages **31a** / **31a2** / **31c**; **`npm run unity:testmode-batch`** when **31a2** ships |
| Project compiles after tooling | Batch compile | `npm run unity:compile-check` (repo root) | Per **AGENTS.md** |
| Bridge-assisted spot check (dev) | Manual / agent | **close-dev-loop** + **`debug_context_bundle`** | **Postgres** + **Editor** on **REPO_ROOT** |
| **City history** spot check (dev) | Agent / SQL / MCP | **TECH-82** **`city_metrics_query`** after **N** ticks | Stage **31d** |
| Descriptor / fixture schema valid | Node / **CI** | **`validate:fixtures`** or sibling | Stage **31b** when schema exists |
| **MCP** scenario tool | **MCP** + docs | **`docs/mcp-ia-server.md`** | Stage **31e** |

## Acceptance criteria (roll-up)

See program tracker **Roll-up acceptance** and [BACKLOG.md](../../BACKLOG.md) **TECH-31** row.

## Open Questions (resolve before / during implementation)

Canonical vocabulary; **game behavior** for scenarios—not tooling (tooling notes live in stage specs and **TECH-82**).

- For the **reference scenario** (and templates): what **in-game time**, **treasury**, **RCI** demand, and **CityStats** snapshots are **valid frozen preconditions**—may a scenario start from economically “stressed” or demand-inconsistent states that normal **New game** never produces, as long as load succeeds and ticks run?
- When a descriptor requests **terrain** / **water** / **road stroke** layout, should the **product** rule be **strict** (reject impossible combinations) or **best-effort repair** (adjust heights/water for **shore** and **road** rules)—and is repair allowed outside the declared edit region?

## Issues Found During Development

Prefer per-stage tables in **`projects/TECH-31*.md`**; record cross-stage items in the [program tracker](../../projects/TECH-31-agent-scenario-generator-program.md) **Issues found** table.

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## Lessons Learned

Capture during work in the [program tracker](../../projects/TECH-31-agent-scenario-generator-program.md); migrate to glossary/specs at **TECH-31** closeout per **project-spec-close** skill.

## Decision Log (high level)

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-02 | Spec created | Roadmap |
| 2026-04-09 | Staged specs under **`projects/`**; this file = stub + OQ + aggregate contracts | Human order + **BACKLOG** **`Spec:`** stability |
