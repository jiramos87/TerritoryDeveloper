# TECH-31a2 — Batch **test mode** tooling (shell + **executeMethod**)

**Program:** [TECH-31-agent-scenario-generator-program.md](TECH-31-agent-scenario-generator-program.md) **Stage 31a2**.  
**Backlog:** [TECH-31](../BACKLOG.md).  
**Prerequisite stages:** **31a** (**test mode** entry, **`GameSaveManager`** load path, fixtures layout).

## Summary

Deliver the **machine-facing** prerequisites for agent-driven **test mode** runs **without** an interactive **Editor**: **shell** helpers to **quit** a **Unity Editor** instance that holds the **project lock** (best-effort, **macOS** **v1**), a **`unity-testmode-batch`** entry point that invokes the **Unity** binary with **`-batchmode`** **`-nographics`** **`-quit`**, **test mode** CLI args (**`-testScenarioId`** / **`-testScenarioPath`**), and a static **Editor** **`executeMethod`** that completes a deterministic **load** → optional **bounded** **simulation** work → **report** file under **`tools/reports/`**. Stage **31a3** (**agent-test-mode-verify** skill) **consumes** these tools; **31b** (**scenario builder**) assumes they exist for **CI** / agent **smoke** on generated artifacts.

## Goals

- **`tools/scripts/`** script(s) sourcing **`load-repo-env.inc.sh`** / **`UNITY_EDITOR_PATH`** resolution consistent with **`unity-compile-check.sh`** (**macOS** **v1**; document **Windows/Linux** gaps).
- **Best-effort** **quit** of **Unity** when it blocks a second **batchmode** instance (e.g. **`osascript`** **AppleScript** or documented **`pkill`** policy with **safety** checks — **never** kill unrelated **Unity** projects without **projectPath** match where feasible).
- **`npm run unity:testmode-batch`** (or equivalent documented alias) passing **`-projectPath`**, **scenario** args, and **`-executeMethod Namespace.Class.Method`**.
- **Editor** **C#**: one public **static** **`Run`** method (name/namespace TBD) that uses **`GameSaveManager.LoadGame`** / **`GameStartInfo`**-compatible flow — **no** second **deserializer**; **English** logs; writes **JSON** or structured text to **`tools/reports/`** (policy: **gitignored** artifacts).
- **Exit-code** table documented in script **`--help`** and in [`tools/fixtures/scenarios/README.md`](../tools/fixtures/scenarios/README.md) **batch** subsection (cross-link).

## Non-goals

- Final **Cursor Skill** prose, **`AGENTS.md`** narrative, or **`project-spec-implement`** hook (**31a3**).
- **Guaranteed** **headless** **GPU**-free success in **CI** for all platforms in **v1**.
- **New** gameplay features or **`GridManager`** responsibilities (**invariants**).

## Risks

| Risk | Mitigation |
|------|------------|
| **Double Unity** on same **projectPath** | **Quit** first or fail fast with clear **exit code** |
| **Batch** **Play Mode** init order | Align with **MainScene** bootstrap; bounded waits; no **`FindObjectOfType`** in **Update** |

## Implementation checklist

- [ ] **`tools/scripts/unity-testmode-batch.sh`** (or split **quit** + **launch**): **`-h`** / **`--help`**, exit codes.
- [ ] **macOS** **quit** helper (integrated or **`unity-quit-project.sh`**).
- [ ] **Editor** **`AgentTestModeBatchRunner`** (name TBD) **`executeMethod`** entry.
- [ ] Root **`package.json`** **`npm run unity:testmode-batch`**.
- [ ] **`npm run unity:compile-check`** after **C#**; **`npm run validate:all`** if **package.json** / docs touch **IA**.

## Implementation plan (condensed)

### Phase A — Editor entry point

- [ ] Add **`Territory.Testing.Editor`** (or existing **Editor** asm) **static** method: parse **command-line** / **static** args Unity passes into **batchmode**; load scenario; optional **N** ticks; write **`tools/reports/agent-testmode-batch-*.json`** (schema documented in file header or **31a3** skill).

### Phase B — Shell + **npm**

- [ ] Resolve **`UNITY_EDITOR_PATH`** (same strategy as **`unity-compile-check.sh`**).
- [ ] **Quit** path: target process holding **`REPO_ROOT`** / **`-projectPath`** match.
- [ ] **Launch** path: full **Unity** arg list documented in **`tools/fixtures/scenarios/README.md`**.

## Test contracts (stage)

| Goal | Check type | Command or artifact | Notes |
|------|------------|---------------------|--------|
| Scripts + **Editor** compile | Batch | `npm run unity:compile-check` | No **Editor** lock |
| **Batch** smoke | Manual / dev machine | `npm run unity:testmode-batch` + report file | **macOS** **v1** |
| **IA** parity | Node | `npm run validate:all` | If **package.json** / durable docs change |

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-09 | Stage **31a2** split from former standalone **backlog** skill issue | **Tooling** ships before **skill** orchestration (**31a3**); **31b** prereq clarity |

## Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## Open Questions (resolve before / during implementation)

**N/A** (tooling only). **Game** **scenario** rules remain in **`.cursor/projects/TECH-31.md`**.
