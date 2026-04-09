# TECH-31a3 — **Agent test-mode verify** skill (orchestration + human **QA** handoff)

> **Program issue:** [TECH-31](../BACKLOG.md) — stage **31a3** (child spec; aggregate **Open Questions** / **§7b** for the program live in [`.cursor/projects/TECH-31.md`](../.cursor/projects/TECH-31.md)).

**Program:** [TECH-31-agent-scenario-generator-program.md](TECH-31-agent-scenario-generator-program.md) **Stage 31a3**.  
**Backlog:** [TECH-31](../BACKLOG.md).  
**Prerequisite stages:** **31a** (runtime **test mode** + fixtures); **31a2** (shipped — **glossary** **Agent test mode batch**: **`npm run unity:testmode-batch`**, [`ARCHITECTURE.md`](../ARCHITECTURE.md) **Local verification**, [`tools/fixtures/scenarios/README.md`](../tools/fixtures/scenarios/README.md)).

## Summary

Finalize **`.cursor/skills/agent-test-mode-verify/SKILL.md`** as the **normative** agent workflow: **gate** (run vs skip), **Path A** (**Agent test mode batch** / **`npm run unity:testmode-batch`**), **Path B** (**IDE agent bridge** **hybrid** with **`.queued-test-scenario-id`**), bounded **iterate-until-green** with **`validate:all`** / **compile** gates, and a **structured handoff** requesting **human** **normal-game** **QA** only at the end. Integrate with **`project-spec-implement`** (already cross-linked) and **`close-dev-loop`** (**compose**, do not fork). **Does not** replace **human** **issue** verification per **`AGENTS.md`**.

## Goals

1. **Complete** **`SKILL.md`**: triggers, prerequisites, **exit-code** / failure-class table (mapping **Agent test mode batch** shell/C# codes + bridge failures), full **tool recipe** (**territory-ia** + **Node**/**shell** order), **seed prompt**.
2. **Gating heuristics** (document in **SKILL.md** as a short table): run the loop when the diff or **§7b** / **§8** touches **`GameSaveManager`** / **`GameSaveData`**, **test mode** bootstrap, **Load pipeline**-relevant saves, **GridManager** / **simulation** tick harness, or **HUD** / **Play Mode** assertions; otherwise **skip** and record **why** in the handoff.
3. **Scenario path** **v1**: reference **`reference-flat-32x32`** + **`tools/fixtures/scenarios/agent-generated/{run-id}/save.json`** conventions; **v2**: hook **31b** builder output when shipped.
4. **`AGENTS.md`** + **`ARCHITECTURE.md`** short subsections pointing to the skill.
5. **macOS** **E2E** documented once (**Path A** and **Path B**).

## Non-goals

- **Agent test mode batch** scripts (**prerequisite**, shipped — **glossary**).
- **Scenario builder** logic (**31b**).
- **Human** **closeout** / **archive** without confirmation.

## Current state

- **Stub** skill exists; **`project-spec-implement`** already references it.
- Program stage **31a2** delivered **`unity:testmode-batch`** + **`executeMethod`** + lock-based **quit**; this stage **documents consumption** and **polishes** orchestration prose.

## Proposed design (implementation-owned)

| Deliverable | Notes |
|-------------|--------|
| **`agent-test-mode-verify/SKILL.md`** | Remove “pending” language; link **Agent test mode batch** + **31a3** |
| **`AGENTS.md`**, **`ARCHITECTURE.md`** | One subsection each |
| **`.gitignore`** | **`tools/fixtures/scenarios/agent-generated/`** if not already |

## Implementation plan

### Phase 1 — Skill completion

- [x] Remove **pointer-only** / **pending** prose from [`.cursor/skills/agent-test-mode-verify/SKILL.md`](../.cursor/skills/agent-test-mode-verify/SKILL.md); keep YAML `description` triggers aligned with **Summary** above.
- [x] **Tool recipe** (ordered): **gate** → **`npm run validate:all`** when diff touches **MCP** / schemas / fixtures / skills → **`npm run unity:compile-check`** when **C#** under **`Assets/`** changed (or bridge **`get_compilation_status`** / **`unity_compile`** per **`close-dev-loop`**) → **Path A** or **Path B** → bounded **iterate** → **handoff**.
- [x] **Path A** — Document: repo-root **`npm run unity:testmode-batch`** (→ **`tools/scripts/unity-testmode-batch.sh`**); **`Territory.Testing.AgentTestModeBatchRunner.Run`**; optional **`tools/scripts/unity-quit-project.sh`** for project lock; artifact **`tools/reports/agent-testmode-batch-*.json`** (**glossary** **Agent test mode batch**; **Load pipeline** via **`GameSaveManager.LoadGame`** only).
- [x] **Path B** — Document: write **`.queued-test-scenario-id`** (see [`tools/fixtures/scenarios/README.md`](../tools/fixtures/scenarios/README.md)); **`npm run db:bridge-preflight`**; territory-ia **`unity_bridge_command`** (**`enter_play_mode`** → **`get_play_mode_status`** → **`debug_context_bundle`** with **`params.seed_cell`** / optional **`get_console_logs`**, **`capture_screenshot`**) → **`exit_play_mode`** (**glossary** **IDE agent bridge**).
- [x] **`{MAX_ITERATIONS}`** default **2**, same as [`.cursor/skills/close-dev-loop/SKILL.md`](../.cursor/skills/close-dev-loop/SKILL.md).
- [x] **Exit-code / failure-class** table: **shell** exit from **`unity-testmode-batch.sh`** / **`unity-quit-project.sh`**; **Unity** **`EditorApplication.Exit`** codes from batch runner; MCP / bridge **`db_unconfigured`**, job **`timeout`**, and **`get_compilation_status`** / **`compilation_failed`** (point to **`ide-bridge-evidence`** / **`bridge-environment-preflight`**).

### Phase 2 — Docs + **E2E**

- [x] **`AGENTS.md`**, **`ARCHITECTURE.md`**: one short subsection each linking **SKILL.md**, **Agent test mode batch**, and **Local verification**.
- [x] **E2E** in **skill** body: one worked example for **Path A** (fixture id **`reference-flat-32x32`**, expected log lines, report glob) and one for **Path B** (Postgres + Editor on **REPO_ROOT**, **seed_cell** example).
- [x] **`npm run validate:all`** after **`AGENTS.md`** / **`ARCHITECTURE.md`** edits.

### Phase 3 — **Scenario** contract until **31b**

- [x] Document **`tools/fixtures/scenarios/agent-generated/{run-id}/save.json`**; ensure [`.gitignore`](../.gitignore) ignores **`tools/fixtures/scenarios/agent-generated/`** if missing.
- [x] Cross-link program stage **31b** when **scenario builder** output path is stable.

## 7b. Test contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|--------|
| **Skill** normative | Manual | Read [`.cursor/skills/agent-test-mode-verify/SKILL.md`](../.cursor/skills/agent-test-mode-verify/SKILL.md) | Full recipe, exit table, **seed prompt**; no stub-only pointer |
| **IA / Node** | Node | `npm run validate:all` | After **`AGENTS.md`** / **`ARCHITECTURE.md`** / skill edits |
| **Unity compile** (if **C#** touched in same effort) | Node | `npm run unity:compile-check` | Per **`AGENTS.md`**; not skipped when **`UNITY_EDITOR_PATH`** unset in agent shell |
| **Path A** **E2E** | Manual / dev machine | `npm run unity:testmode-batch` + **`tools/reports/agent-testmode-batch-*.json`** | Committed scenario (e.g. **`reference-flat-32x32`**); **Load pipeline** exercised |
| **Path B** **E2E** | MCP / dev machine | `npm run db:bridge-preflight` → **`unity_bridge_command`** (**`timeout_ms`:** **40000**) **`enter_play_mode`** → **`debug_context_bundle`** → **`exit_play_mode`** | **`DATABASE_URL`** + Editor on **REPO_ROOT**; see **`ide-bridge-evidence`**; **Verification** block: [`docs/agent-led-verification-policy.md`](../docs/agent-led-verification-policy.md) |
| **Compile gate (bridge)** | MCP / dev machine | **`unity_bridge_command`** **`get_compilation_status`** or **`unity_compile`** | Same queue as **IDE agent bridge** |
| **Console / UI spot-check (optional)** | MCP / dev machine | **`get_console_logs`**, **`capture_screenshot`** (`include_ui` when overlay must show) | **`unity-development-context`** §10 |

## Acceptance criteria

- [x] **`SKILL.md`** is **normative** (this spec + **glossary** **Agent test mode batch** + **IDE agent bridge** vocabulary).
- [x] **`AGENTS.md`** + **`ARCHITECTURE.md`** updated with links to the skill and batch entry point.
- [ ] **Path A** and **Path B** each exercised once on **macOS** (owner sign-off); **`npm run validate:all`** green after doc edits.

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-09 | Consolidated former standalone **backlog** row into **TECH-31** as **31a3** | Single **program** trace; **31b** prereqs explicit |
| 2026-04-09 | **Compose** **`close-dev-loop`** | Avoid duplicate **Moore** **diff** prose |

## Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## Open Questions (resolve before / during implementation)

**N/A** (tooling / workflow). **Game** **scenario** **Open Questions** live in **`.cursor/projects/TECH-31.md`**.
