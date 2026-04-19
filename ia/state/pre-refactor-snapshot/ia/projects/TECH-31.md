---
purpose: "Project spec for TECH-31 — Agent scenario generator (orchestrator stub)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-31 — Agent scenario generator (orchestrator stub)

> **Issue:** [TECH-31](../../BACKLOG.md)
> **Status:** In progress
> **Created:** 2026-04-02
> **Last updated:** 2026-04-11

**Program tracker (stages, progress, lessons):** [`projects/TECH-31-agent-scenario-generator-program.md`](../../projects/TECH-31-agent-scenario-generator-program.md).  
**Implementation specs (sequential):** [31a — test mode + load](../../projects/TECH-31a-test-mode-and-load.md) → **31a2** (**Agent test mode batch** — shipped; [`ARCHITECTURE.md`](../../ARCHITECTURE.md) **Local verification**, [`tools/fixtures/scenarios/README.md`](../../tools/fixtures/scenarios/README.md), **glossary**) → [31a3 — agent test-mode verify skill](../../projects/TECH-31a3-agent-test-mode-verify-skill.md) → **31b** (**scenario_descriptor_v1** / scenario builder — shipped; **glossary**, [`tools/fixtures/scenarios/BUILDER.md`](../../tools/fixtures/scenarios/BUILDER.md), [`docs/schemas/README.md`](../../docs/schemas/README.md)) → [31c — verification](../../projects/TECH-31c-verification-pipeline.md) (**closed** 2026-04-10; retained as reference) → **31d** (**TECH-82** Phase 1 + metrics — **closed** 2026-04-10; **glossary** **City metrics history**, [`ia/projects/TECH-82.md`](TECH-82.md), [`tools/fixtures/scenarios/README.md`](../../tools/fixtures/scenarios/README.md), **managers-reference**) → [31e — MCP](../../projects/TECH-31e-mcp-tool-and-workflows.md).

Normative detail for each stage lives in those **`projects/TECH-31*.md`** files where one exists (**31d** shipped: **glossary** **City metrics history**, [`ia/projects/TECH-82.md`](TECH-82.md)). This stub retains **BACKLOG** **`Spec:`** resolution, **Open Questions** (game logic), and aggregate **Test contracts** for **MCP** / closeout tools that read `ia/projects/TECH-31.md` only.

**Related:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md); [`ia/skills/close-dev-loop/SKILL.md`](../skills/close-dev-loop/SKILL.md); **glossary** **IDE agent bridge**, **`debug_context_bundle`**; [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md). **Spec-pipeline:** glossary **territory-ia spec-pipeline program** — [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md). **TECH-82** backlog + [`ia/projects/TECH-82.md`](TECH-82.md) for **city metrics** (Phase 1 shipped).

## Summary

Build an **agent-facing** **scenario generator**: structured intent → **`GameSaveData`-compatible** artifact + **scenario id**; **test mode** on **32×32**; bounded **simulation** work; exports/assertions; final **MCP** tool. **Not in scope:** player-facing editors, **TECH-35** property-based generation.

## Test contracts (aggregate)

Normative detail for batch driver, **golden** JSON, **CI** tick cap (**10000**), and **close-dev-loop** seed cells: [`projects/TECH-31c-verification-pipeline.md`](../../projects/TECH-31c-verification-pipeline.md) (stage **31c** complete). This table stays a short roll-up for tools that read only this stub.

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|--------|
| Reference scenario loads in **test mode** | Unity / optional **CI** | **`npm run unity:testmode-batch`** — args: **`--scenario-id`** / **`--scenario-path`**, **`--simulation-ticks N`** (max **10000**), **`--golden-path`** (optional), **`--quit-editor-first`** | **glossary** **Agent test mode batch**; [`tools/fixtures/scenarios/README.md`](../../tools/fixtures/scenarios/README.md); report **`schema_version`** **2** + optional **`city_stats`** |
| Project compiles after tooling | Batch compile | `npm run unity:compile-check` (repo root) | Per **AGENTS.md** |
| Bridge-assisted spot check (dev) | Manual / agent | **close-dev-loop** + **`debug_context_bundle`** | **Postgres** + **Editor** on **REPO_ROOT**; recipe for **`reference-flat-32x32`**: **31c** spec |
| **City history** spot check (dev) | Agent / SQL / MCP | **`city_metrics_query`** after **N** ticks | **glossary** **City metrics history**; [`ia/projects/TECH-82.md`](TECH-82.md) Phase 1 |
| Descriptor / fixture schema valid | Node / **CI** | **`validate:fixtures`** | **glossary** **scenario_descriptor_v1** (+ **`geography_init_params`**); see [`docs/schemas/README.md`](../../docs/schemas/README.md) |
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
