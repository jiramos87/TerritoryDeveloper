# TECH-31 program — Agent scenario generator (tracker)

**Backlog:** [TECH-31](../BACKLOG.md) (open row under **Agent ↔ Unity & MCP context lane**).  
**Cursor spec (stub + Open Questions):** [`.cursor/projects/TECH-31.md`](../.cursor/projects/TECH-31.md).  
**Related:** [TECH-82](../BACKLOG.md) (**city metrics** / **city history** — Stage **31d**). **Batch** **test mode** tooling (program stage **31a2**, shipped): **glossary** **Agent test mode batch** — [`ARCHITECTURE.md`](../ARCHITECTURE.md) **Local verification**, [`tools/fixtures/scenarios/README.md`](../tools/fixtures/scenarios/README.md). **Agent test-mode verify** skill (**31a3**): [`projects/TECH-31a3-agent-test-mode-verify-skill.md`](TECH-31a3-agent-test-mode-verify-skill.md). **Spec-pipeline** prerequisite: glossary **territory-ia spec-pipeline program**; exploration [spec-pipeline-exploration.md](spec-pipeline-exploration.md).

This file is the **human-oriented orchestrator**: stage order, links to **implementation specs**, progress checkboxes, and **lessons learned**. Detailed requirements per stage live in **`projects/TECH-31*.md`** when that stage file still exists (**31d** closed — trace **glossary** **City metrics history** and [`.cursor/projects/TECH-82.md`](../.cursor/projects/TECH-82.md))—not in this table alone.

## Program intent (one paragraph)

Deliver **test mode**, **save**-shaped **scenarios**, construction tooling, verification (**UTF** / **`debug_context_bundle`** / optional **TECH-82** **city history**), and finally a **territory-ia** **MCP** tool—so agents get reproducible **game-logic** checks without hand-built cities. **Scenarios** stay **save**-authoritative; **Postgres** metrics are observability for assertions when configured.

## Stages (sequential)

Complete stages in order unless the **Decision Log** records a deliberate parallel exception (e.g. spike). **Stage ids** (**31a**, **31a2**, **31a3**, **31b**–**31e**) are program labels only—they are **not** separate **BACKLOG** issues.

| Stage | Focus | Implementation spec | Primary BACKLOG anchor |
|-------|--------|---------------------|-------------------------|
| **31a** | **Test mode** + load path + **32×32** policy + fixture layout + driver matrix | [TECH-31a-test-mode-and-load.md](TECH-31a-test-mode-and-load.md) | **TECH-31** |
| **31a2** | **Batchmode** shell + **Unity** quit helper + **Editor** **`executeMethod`** + **`npm run unity:testmode-batch`** (shipped — **glossary** **Agent test mode batch**; [`ARCHITECTURE.md`](../ARCHITECTURE.md), [`tools/fixtures/scenarios/README.md`](../tools/fixtures/scenarios/README.md)) | *(stage spec removed after IA migration — 2026-04-09)* | **TECH-31** |
| **31a3** | **`agent-test-mode-verify`** **Cursor Skill** (orchestration, **Path A/B**, handoff to human **QA**) | [TECH-31a3-agent-test-mode-verify-skill.md](TECH-31a3-agent-test-mode-verify-skill.md) | **TECH-31** |
| **31b** | Descriptor → **builder** → **`GameSaveData`** artifact (invariants-safe) | *(stage spec removed after IA migration — 2026-04-10)* — **glossary** **scenario_descriptor_v1**, [`tools/fixtures/scenarios/BUILDER.md`](tools/fixtures/scenarios/BUILDER.md), [`docs/schemas/README.md`](../docs/schemas/README.md) | **TECH-31** |
| **31c** | Verification without DB metrics: **N** ticks, **UTF**/scripted run, **close-dev-loop** recipe (**closed** 2026-04-10 — spec retained) | [TECH-31c-verification-pipeline.md](TECH-31c-verification-pipeline.md) | **TECH-31** |
| **31d** | **City history** for scenarios: **TECH-82** Phase 1 (`city_metrics_history`, **`MetricsRecorder`**, **`city_metrics_query`**) + **test mode** **`scenario_id`** correlation (see **TECH-82** Decision Log) | *(stage spec removed after IA migration — 2026-04-10)* — **glossary** **City metrics history**, [`.cursor/projects/TECH-82.md`](../.cursor/projects/TECH-82.md) (**Phase 1**), [`docs/mcp-ia-server.md`](../docs/mcp-ia-server.md) (**`city_metrics_query`**), [`tools/fixtures/scenarios/README.md`](../tools/fixtures/scenarios/README.md), **managers-reference** (**MetricsRecorder**) | **TECH-82** (Phases 2–4 still open); **TECH-31** consumes |
| **31e** | **MCP** tool + docs + skill cross-links | [TECH-31e-mcp-tool-and-workflows.md](TECH-31e-mcp-tool-and-workflows.md) | **TECH-31** |

**Soft dependencies (from specs):** **TECH-15** / **TECH-16** (**UTF** / harness naming for **CI**); stable bridge **`kind`** values (**unity-development-context**, **close-dev-loop**). **TECH-83** remains optional for parameter sweeps after load.

## Progress tracker

Update this section as stages complete (owner / date optional).

- [x] **31a** — Test mode + load + contracts (2026-04-09)
- [x] **31a2** — Batch **test mode** tooling (shell + **executeMethod**) (2026-04-09)
- [x] **31a3** — **agent-test-mode-verify** skill (orchestration + docs) (2026-04-09)
- [x] **31b** — Scenario builder + reference descriptor/artifact (2026-04-10)
- [x] **31c** — File-based verification pipeline + documented **close-dev-loop** recipe (2026-04-10)
- [x] **31d** — **TECH-82** Phase 1 + **test mode** metrics path (2026-04-10)
- [ ] **31e** — **MCP** `scenario_*` tool + **`docs/mcp-ia-server.md`**

## Roll-up acceptance (maps to **TECH-31** BACKLOG **Acceptance**)

Satisfied when **31e** ships and **TECH-82** later phases meet the open **TECH-82** row (Phase 1 shipped with **31d** — 2026-04-10; see **TECH-31** **BACKLOG** **Notes**):

- At least one automated Unity run on a committed scenario; **test mode** launch documented (**scenario id** or path, **32×32**).
- **`npm run unity:testmode-batch`** runs **load** + report (**glossary** **Agent test mode batch**); optional **`--golden-path`** integer **CityStats** assert; **CI** simulation tick cap (**10000**) and operator matrix in [`tools/fixtures/scenarios/README.md`](../tools/fixtures/scenarios/README.md) (**31c**).
- **close-dev-loop** + **`debug_context_bundle`** recipe for **`reference-flat-32x32`** documented in [`projects/TECH-31c-verification-pipeline.md`](TECH-31c-verification-pipeline.md).
- Builder docs: **AUTO** + at least one non-**AUTO** pattern.
- **`npm run unity:compile-check`** after tooling changes; **MCP** tool registered in **`docs/mcp-ia-server.md`** (**31e**).
- Stub **Test contracts** in [`.cursor/projects/TECH-31.md`](../.cursor/projects/TECH-31.md) lists batch driver args, **CI** tick bound, and points to **31c** for normative tables.
- **TECH-82** **city history** composition for **test mode** assertions documented with **31d** / **TECH-82** spec (not required for **31c** file-only goldens).
- Optional: **BUG-52** **Notes** link when first **AUTO** scenario lands.

## Decision Log (program-level)

| Date | Decision | Notes |
|------|----------|-------|
| 2026-04-02 | Program chartered as **TECH-31** | Original monolithic spec |
| 2026-04-09 | Staged implementation specs under **`projects/`**; **31d** = **TECH-82** Phase 1 alignment | Human-readable sequence; **BACKLOG** rows remain **TECH-31** + **TECH-82** |
| 2026-04-09 | Insert **31a2** (batch tooling) + **31a3** (**agent-test-mode-verify** skill); former standalone row folded into **TECH-31** | **31b** prerequisites explicit |
| 2026-04-09 | Close **31a2** stage doc; normative operator detail → **glossary** **Agent test mode batch**, **`ARCHITECTURE.md`**, **unity-development-context** §10, scenarios **README** | **project-spec-close** (program stage; **TECH-31** row stays open) |
| 2026-04-10 | Close **31b** stage doc; **scenario_descriptor_v1** contract → **glossary**, **`ARCHITECTURE.md`**, **`BUILDER.md`**, **`docs/schemas/README.md`** | **project-spec-close** (program stage; **TECH-31** row stays open) |
| 2026-04-10 | **31c** complete: batch **golden** assert (**`-testGoldenPath`** / **`--golden-path`**, exit **8**), report **`city_stats`**, **close-dev-loop** recipe + **CI**/**RNG** docs; align **BACKLOG** **Notes**, stub **Test contracts**, roll-up acceptance | [`projects/TECH-31c-verification-pipeline.md`](TECH-31c-verification-pipeline.md) |
| 2026-04-10 | **31d** complete: **`city_metrics_history`**, **`MetricsRecorder`**, **`city_metrics_query`**, **`scenario_id`** for **test mode**; docs + fixtures **README** + **agent-test-mode-verify** skill; stage spec removed — trace **glossary** **City metrics history** + **TECH-82** Phase 1 | [`.cursor/projects/TECH-82.md`](../.cursor/projects/TECH-82.md) |

*(Per-stage decisions belong in each **`TECH-31*.md`** Decision Log or **Issues Found** — or the program **Decision Log** when the stage file was removed.)*

## Lessons learned (migrate at **TECH-31** closeout)

| Date | Stage | Lesson | Where to persist (if durable) |
|------|-------|--------|------------------------------|
| 2026-04-09 | **31a2** | **`SessionState`** alone did not carry batch **Play Mode** pump across **domain reload** in **`-batchmode`**; use a transient **`tools/reports/.agent-testmode-batch-state.json`** (or equivalent disk **Editor** state). Unity **`-quit`** after **`executeMethod`** returns can exit before **`EditorApplication.update`** runs — omit **`-quit`**; terminate with **`EditorApplication.Exit`**. | **glossary** **Agent test mode batch**, **`ARCHITECTURE.md`**, [`tools/fixtures/scenarios/README.md`](../tools/fixtures/scenarios/README.md) |
| 2026-04-10 | **31b** | **AJV** `strict: true` rejects some **`if`/`then` `required`** shapes on nested objects; prefer **`oneOf`** for mutually exclusive terrain shapes in **JSON Schema** when **CI** uses the same validator. | [`docs/schemas/scenario-descriptor.v1.schema.json`](../docs/schemas/scenario-descriptor.v1.schema.json) |

## Issues found (program-wide)

Track cross-stage blockers here; stage-local bugs stay in the stage spec **Issues Found** table.

| # | Stage | Description | Resolution |
|---|-------|-------------|------------|
|  |  |  |  |
