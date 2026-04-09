# TECH-31 program — Agent scenario generator (tracker)

**Backlog:** [TECH-31](../BACKLOG.md) (open row under **Agent ↔ Unity & MCP context lane**).  
**Cursor spec (stub + Open Questions):** [`.cursor/projects/TECH-31.md`](../.cursor/projects/TECH-31.md).  
**Related:** [TECH-82](../BACKLOG.md) (**city metrics** / **city history** — Stage **31d**). **Spec-pipeline** prerequisite: glossary **territory-ia spec-pipeline program**; exploration [spec-pipeline-exploration.md](spec-pipeline-exploration.md).

This file is the **human-oriented orchestrator**: stage order, links to **implementation specs**, progress checkboxes, and **lessons learned**. Detailed requirements per stage live in **`projects/TECH-31*.md`** below—not in this table alone.

## Program intent (one paragraph)

Deliver **test mode**, **save**-shaped **scenarios**, construction tooling, verification (**UTF** / **`debug_context_bundle`** / optional **TECH-82** **city history**), and finally a **territory-ia** **MCP** tool—so agents get reproducible **game-logic** checks without hand-built cities. **Scenarios** stay **save**-authoritative; **Postgres** metrics are observability for assertions when configured.

## Stages (sequential)

Complete stages in order unless the **Decision Log** records a deliberate parallel exception (e.g. spike). **Stage ids** (`31a`–`31e`) are program labels only—they are **not** separate **BACKLOG** issues.

| Stage | Focus | Implementation spec | Primary BACKLOG anchor |
|-------|--------|---------------------|-------------------------|
| **31a** | **Test mode** + load path + **32×32** policy + fixture layout + driver matrix | [TECH-31a-test-mode-and-load.md](TECH-31a-test-mode-and-load.md) | **TECH-31** |
| **31b** | Descriptor → **builder** → **`GameSaveData`** artifact (invariants-safe) | [TECH-31b-scenario-builder.md](TECH-31b-scenario-builder.md) | **TECH-31** |
| **31c** | Verification without DB metrics: **N** ticks, **UTF**/scripted run, **close-dev-loop** recipe | [TECH-31c-verification-pipeline.md](TECH-31c-verification-pipeline.md) | **TECH-31** |
| **31d** | **City history** for scenarios: **TECH-82** Phase 1 (`city_metrics_history`, **`MetricsRecorder`**, MCP query) + **test mode** correlation (**scenario id** metadata — see **TECH-82** Open Questions) | [TECH-31d-city-metrics-TECH-82-phase1.md](TECH-31d-city-metrics-TECH-82-phase1.md) | **TECH-82** (Phase 1); **TECH-31** consumes |
| **31e** | **MCP** tool + docs + skill cross-links | [TECH-31e-mcp-tool-and-workflows.md](TECH-31e-mcp-tool-and-workflows.md) | **TECH-31** |

**Soft dependencies (from specs):** **TECH-15** / **TECH-16** (**UTF** / harness naming for **CI**); stable bridge **`kind`** values (**unity-development-context**, **close-dev-loop**). **TECH-83** remains optional for parameter sweeps after load.

## Progress tracker

Update this section as stages complete (owner / date optional).

- [ ] **31a** — Test mode + load + contracts
- [ ] **31b** — Scenario builder + reference descriptor/artifact
- [ ] **31c** — File-based verification pipeline + documented **close-dev-loop** recipe
- [ ] **31d** — **TECH-82** Phase 1 + **test mode** metrics path (see **TECH-82** acceptance)
- [ ] **31e** — **MCP** `scenario_*` tool + **`docs/mcp-ia-server.md`**

## Roll-up acceptance (maps to **TECH-31** BACKLOG **Acceptance**)

Satisfied when all relevant stage specs are done and **TECH-82** Phase 1 meets its own row if **31d** is in scope for the release:

- At least one automated Unity run on a committed scenario; **test mode** launch documented (**scenario id** or path, **32×32**).
- Builder docs: **AUTO** + at least one non-**AUTO** pattern.
- **`npm run unity:compile-check`** after tooling changes; **MCP** tool registered in **`docs/mcp-ia-server.md`**.
- Program **Test contracts** table in [`.cursor/projects/TECH-31.md`](../.cursor/projects/TECH-31.md) (or migrated here if desired) lists driver args and **CI** tick bound.
- Optional: **BUG-52** **Notes** link when first **AUTO** scenario lands.

## Decision Log (program-level)

| Date | Decision | Notes |
|------|----------|--------|
| 2026-04-02 | Program chartered as **TECH-31** | Original monolithic spec |
| 2026-04-09 | Staged implementation specs under **`projects/`**; **31d** = **TECH-82** Phase 1 alignment | Human-readable sequence; **BACKLOG** rows remain **TECH-31** + **TECH-82** |

*(Per-stage decisions belong in each **`TECH-31*.md`** Decision Log or **Issues Found**.)*

## Lessons learned (migrate at **TECH-31** closeout)

| Date | Stage | Lesson | Where to persist (if durable) |
|------|-------|--------|------------------------------|
|  |  |  |  |

## Issues found (program-wide)

Track cross-stage blockers here; stage-local bugs stay in the stage spec **Issues Found** table.

| # | Stage | Description | Resolution |
|---|-------|-------------|------------|
|  |  |  |  |
