# TECH-31c — Verification pipeline (file-based)

**Stage status:** **Closed** (2026-04-10). This file remains the **reference** for **31c** acceptance, **Test contracts**, and the **`reference-flat-32x32`** **close-dev-loop** recipe. Normative operator detail is also in [`tools/fixtures/scenarios/README.md`](../tools/fixtures/scenarios/README.md) and **glossary** **Agent test mode batch**. Next program stage: **31d** (**TECH-82** Phase 1) when scheduled.

> **Program issue:** [TECH-31](../BACKLOG.md) — stage **31c** (child spec; aggregate **Open Questions** and stub **Test contracts** live in [`.cursor/projects/TECH-31.md`](../.cursor/projects/TECH-31.md)).

**Program:** [TECH-31-agent-scenario-generator-program.md](TECH-31-agent-scenario-generator-program.md) **Stage 31c**.  
**Backlog:** [TECH-31](../BACKLOG.md).  
**Prerequisite stages:** **31a**, **31b** (shipped — **glossary** **scenario_descriptor_v1**, [`tools/fixtures/scenarios/BUILDER.md`](../tools/fixtures/scenarios/BUILDER.md); or a hand-authored committed save if you intentionally bypass the builder).

## Current state

- **Closed** (2026-04-10): implementation plan and acceptance criteria satisfied; **Agent test mode batch** report **`schema_version`:** **2** with optional **`city_stats`**; optional **`-testGoldenPath`** / **`--golden-path`** (exit **8** on mismatch); committed **`agent-testmode-golden-ticks*.json`** under **`reference-flat-32x32/`**; **CI** tick cap and **RNG** notes in scenarios **README**.

## Summary

Automate: load a committed scenario through **`GameSaveManager.LoadGame`** (**persistence-system** **Load pipeline**) in **test mode** → run **N** **simulation ticks** (each tick follows **simulation-system** **Tick execution order** via **`SimulationManager.ProcessSimulationTick()`**) → assert or emit report JSON using **`debug_context_bundle`**, golden files, and/or **UTF**—**without** **TECH-82** **Postgres** **city_metrics_query**. Document a **bounded** **N** for **CI**. Compose with [`.cursor/skills/close-dev-loop/SKILL.md`](../.cursor/skills/close-dev-loop/SKILL.md) and [`.cursor/skills/agent-test-mode-verify/SKILL.md`](../.cursor/skills/agent-test-mode-verify/SKILL.md) (**Path A** / **Path B** when applicable).

## Goals

- **UTF** and/or scripted **Play Mode** path with one-command local run; optional **CI** when driver exists (**TECH-15** / **TECH-16** — soft).
- **close-dev-loop** recipe for the reference scenario (seed cells, bundle fields).
- Determinism: document **RNG** seed and tick count for any golden JSON.

## Non-goals

- **`city_metrics_query`** / **TECH-82** time-series (31d). **MCP** scenario orchestration (31e).

## Risks

| Risk | Mitigation |
|------|------------|
| Flaky goldens | Prefer stable integer **CityStats** fields; document float tolerance if unavoidable. |
| **Three sources of truth** | Align expected values across **§7b** below, committed **`agent-testmode-golden-*.json`**, and the parent program row **Acceptance** in [BACKLOG.md](../BACKLOG.md) (open **TECH-31**). |

## 7. Implementation Plan

### Phase 1 — Harness (load → ticks → assert or report)

- [x] Scripted path: **test mode** load → **N** **simulation ticks** → structured assert or JSON report (golden diff and/or emitted artifact).
- [x] Wire to committed **Save data** fixtures (and/or **scenario_descriptor_v1**-built saves per **31b**); loads only through **`GameSaveManager.LoadGame`**, respecting **Load pipeline** order.

**Delivered:** **`AgentTestModeBatchRunner`** emits report **`schema_version`:** **2** with optional **`city_stats`** (integer **CityStats** slice). **`-testGoldenPath`** / **`npm run unity:testmode-batch -- --golden-path …`** compares against committed JSON; mismatch → exit **8**. Reference goldens: **`tools/fixtures/scenarios/reference-flat-32x32/agent-testmode-golden-ticks0.json`**, **`…-ticks3.json`**.

### Phase 2 — Reference scenario + **close-dev-loop**

- [x] Document **close-dev-loop** steps for the reference scenario: **seed cells**, relevant **`debug_context_bundle`** / bridge fields, and how they relate to golden expectations.
- [x] Cross-link [`docs/agent-led-verification-policy.md`](../docs/agent-led-verification-policy.md) (**Verification** block expectations) where useful.

**Reference scenario (`reference-flat-32x32`) — Path B (**close-dev-loop**):**

1. Preflight: **`npm run db:bridge-preflight`** (see [`.cursor/skills/bridge-environment-preflight/SKILL.md`](../.cursor/skills/bridge-environment-preflight/SKILL.md)).
2. Queue load: one line **`reference-flat-32x32`** in **`tools/fixtures/scenarios/.queued-test-scenario-id`** (or **Player** **`-testScenarioId`**).
3. **`unity_bridge_command`** **`enter_play_mode`** → poll **`get_play_mode_status`** until the grid is ready.
4. **Seed cells (32×32 map):** use **`"0,0"`** (origin corner), **`"16,16"`** (interior), and optionally **`"31,31"`** (opposite corner) as **`debug_context_bundle`** **`seed_cell`** values — same **Moore** neighborhood export as **close-dev-loop**.
5. Inspect **`bundle.anomaly_count`**, **`bundle.anomalies`**, **`bundle.cell_export`**, and optional screenshot paths per [`.cursor/skills/ide-bridge-evidence/SKILL.md`](../.cursor/skills/ide-bridge-evidence/SKILL.md).
6. **`exit_play_mode`**.

**Relation to goldens:** Path A goldens assert **aggregate** **CityStats** integers after load + **N** ticks. Path B exports **per-cell** terrain / sorting context — use both when a change could affect either aggregates or local **Moore** neighborhoods.

### Phase 3 — **CI** bounds and determinism

- [x] Document maximum **N** and CLI arguments for **CI** (and local parity).
- [x] Document **RNG** seed(s) and **N** for each golden file; describe regeneration when **`GameSaveData`** / schema changes.

**Delivered:** [`tools/fixtures/scenarios/README.md`](../tools/fixtures/scenarios/README.md) sections **Golden CityStats snapshots**, **CI simulation tick bound and RNG**, and extended **Driver matrix**.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Fixtures / descriptor schema valid | Node / **CI** | `npm run validate:fixtures` (repo root) | **glossary** **scenario_descriptor_v1**; [`docs/schemas/README.md`](../docs/schemas/README.md) |
| IA / tooling index parity when schemas or MCP-adjacent docs change | Node / **CI** | `npm run validate:all` | Chains dead spec paths, **test:ia**, **validate:fixtures**, **generate:ia-indexes --check** |
| **Agent test mode batch** on clean tree | Unity / **CI** (when enabled) | `npm run unity:testmode-batch` | **Chosen driver** for **31c**. Args: **`--scenario-id`**, **`--scenario-path`**, **`--simulation-ticks N`** (CI cap **10000** = C# clamp), **`--golden-path`** (optional assert), **`--quit-editor-first`**. Newest **`tools/reports/agent-testmode-batch-*.json`**: **`ok`**, **`exit_code`**, optional **`city_stats`**. |
| Golden regression (**reference-flat-32x32**) | Unity / local | `npm run unity:testmode-batch -- --scenario-id reference-flat-32x32 --simulation-ticks 0 --golden-path "$(pwd)/tools/fixtures/scenarios/reference-flat-32x32/agent-testmode-golden-ticks0.json"` | Same for **`ticks3`** + **`agent-testmode-golden-ticks3.json`**. |
| Project compiles after **Assets/** **C#** or batch driver changes | Unity / **CI** | `npm run unity:compile-check` | Per **AGENTS.md** |
| Agent-led verification handoff (optional dev) | Agent report | **`validate:all`** + **`unity:compile-check`** (if **C#**) + Path A / B per [`.cursor/skills/agent-test-mode-verify/SKILL.md`](../.cursor/skills/agent-test-mode-verify/SKILL.md) | **`unity_bridge_command`** **`timeout_ms`:** **40000** initial; [`docs/agent-led-verification-policy.md`](../docs/agent-led-verification-policy.md) |
| **Play Mode** / console / HUD spot check (optional) | MCP / dev machine | **territory-ia** **`unity_bridge_command`**: **`get_console_logs`**, **`capture_screenshot`** (`include_ui` when **Overlay** matters) | **N/A** in **CI**; [`.cursor/skills/ide-bridge-evidence/SKILL.md`](../.cursor/skills/ide-bridge-evidence/SKILL.md); **unity-development-context** (Editor reports) |

## 8. Acceptance Criteria

- [x] At least one automated Unity path on a clean tree (**UTF** and/or **Agent test mode batch**) runs a committed scenario through **Load pipeline** and executes a **documented** **N** **simulation ticks** with assert or report output.
- [x] **§7b** lists the **chosen driver**, CLI args, and **CI** tick bound (**N** max); local and **CI** behavior is consistent where **CI** is enabled.
- [x] **close-dev-loop** + **`debug_context_bundle`** (or equivalent bridge export) is documented for the **reference scenario** (seed cells, fields checked).
- [x] Determinism for goldens: **RNG** seed and **N** are documented; regeneration policy is documented when **Save data** changes.
- [x] Expected values stay aligned: **§7b**, committed **`agent-testmode-golden-*.json`**, and parent program **Acceptance** / **Notes** on the open backlog row.
- [x] `npm run unity:compile-check` passes after **C#** or batch driver changes tied to this stage.

## Open Questions (resolve before / during implementation)

None — tooling-only. Frozen preconditions for scenarios and goldens (economy, **CityStats**, descriptor strictness) are owned by [`.cursor/projects/TECH-31.md`](../.cursor/projects/TECH-31.md) **Open Questions**.

## Decision Log

| Date | Decision | Rationale |
|------|----------|------------|
| 2026-04-10 | Golden v1 = integer **CityStats** fields only; report **`schema_version`:** **2** | Avoid float flakes; **JsonUtility**-friendly nested DTO; optional **`-testGoldenPath`**. |
| 2026-04-10 | Exit code **8** for golden mismatch | Distinct from load/sim failures (**6**) and argument errors (**4**). |
| 2026-04-10 | **Stage closed** | All **Acceptance** criteria met; durable IA migrated to **glossary**, **`ARCHITECTURE.md`**, **`docs/agent-led-verification-policy.md`**, **unity-development-context**, **agent-test-mode-verify** skill, stub **Test contracts**, **BACKLOG** **TECH-31** **Notes**; keep this doc as the **31c** trace. |

## Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |
