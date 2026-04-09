---
name: agent-test-mode-verify
description: >
  Run after project-spec-implement (or standalone) when agent-led test mode verification is required:
  gate (run vs skip), Path A (glossary Agent test mode batch) or Path B (glossary IDE agent bridge hybrid
  with .queued-test-scenario-id), bounded iterate with validate:all / compile gates, structured handoff for
  human normal-game QA. Triggers: "agent test mode loop", "verify in test mode without opening Unity",
  "batchmode scenario check", "post-implement Play Mode suite". Design trace: projects/TECH-31a3-agent-test-mode-verify-skill.md
  (TECH-31 stage 31a3).
---

# Agent test-mode verification loop

**Design trace (program stage):** [`projects/TECH-31a3-agent-test-mode-verify-skill.md`](../../../projects/TECH-31a3-agent-test-mode-verify-skill.md) (**TECH-31** — [`BACKLOG.md`](../../../BACKLOG.md)). **Does not** replace human issue verification per [`AGENTS.md`](../../../AGENTS.md).

**Vocabulary:** **glossary** **Agent test mode batch** (headless **Editor** **`npm run unity:testmode-batch`**, no **Postgres** queue). **glossary** **IDE agent bridge** (**territory-ia** **`unity_bridge_command`**, **Postgres** **`agent_bridge_job`**, **Unity Editor** on **REPO_ROOT**). **Fixtures** and **Load pipeline** semantics: **persistence-system** — scenario JSON is **`GameSaveData`**-shaped; runtime load goes through **`GameSaveManager.LoadGame`** only (same entry as normal load), not ad-hoc grid mutation. Operator matrix and flags: [`tools/fixtures/scenarios/README.md`](../../../tools/fixtures/scenarios/README.md).

**Related:** **[`project-spec-implement`](../project-spec-implement/SKILL.md)** (optional phase exit). **[`close-dev-loop`](../close-dev-loop/SKILL.md)** — **compose** for compile gate order and rich **`debug_context_bundle`** before/after diffs; **do not** duplicate its **Moore**-neighborhood diff prose here. **[`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md)** (one-off logs/screenshots). **[`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md)** (**Postgres** / table checks). **[`project-implementation-validation`](../project-implementation-validation/SKILL.md)** (**`validate:all`**). **Program tracker:** [`projects/TECH-31-agent-scenario-generator-program.md`](../../../projects/TECH-31-agent-scenario-generator-program.md).

**Owner policy (durable):** [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md) — during agent implementation, **Unity** is a **test** surface; **attempt** **Path A** and **Path B** when tools and environment allow; do not skip bridge work only for convenience. **Bridge** **`timeout_ms`:** use **`40000`** (40s) for agent-led verification unless a shorter bound is justified. **IA autoreference:** [`docs/information-architecture-overview.md`](../../../docs/information-architecture-overview.md).

## Prerequisites by path

| Path | Requires |
|------|----------|
| **Path A** (**Agent test mode batch**) | Repo root; **`UNITY_EDITOR_PATH`** or **macOS** Hub inference (see **`tools/scripts/unity-compile-check.sh`** / **`unity-testmode-batch.sh`**); Unity **not** holding **project lock** (or use **`--quit-editor-first`**). No **Postgres**. |
| **Path B** (**IDE agent bridge**) | **`DATABASE_URL`** or **`config/postgres-dev.json`**; migration **`0008_agent_bridge_job.sql`**; **Unity Editor** open on **REPO_ROOT** (if not running, run **`npm run unity:ensure-editor`** — macOS; exit 0 = ready); **territory-ia** **`unity_bridge_command`**. Run **`npm run db:bridge-preflight`** before the first bridge call in a session ([**`bridge-environment-preflight`**](../bridge-environment-preflight/SKILL.md)). On bridge timeout, follow the **timeout escalation protocol** in [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md). |

## Gate — run vs skip

If **none** of the following apply, **skip** this loop, state **why** in the handoff, and rely on **`validate:all`** / normal review only.

| Run the loop when the diff or project spec **§7b** / **§8** touches… | Notes |
|------------------------------------------------------------------------|--------|
| **`GameSaveManager`** / **`GameSaveData`** / save-shaped fixtures | **Load pipeline** risk |
| **Test mode** bootstrap, **`TestModeCommandLineBootstrap`**, scenario resolution | Entry and flags |
| **Committed** or **agent-generated** scenario JSON under **`tools/fixtures/scenarios/`** | Fixture contract |
| **`GridManager`** init / **simulation** tick harness (**`SimulationManager`**, **`ProcessSimulationTick`**) | Batch runner exercises these |
| **HUD** / **Play Mode** assertions, or explicit **§7b** row requiring batch or bridge | Product ask |

## Tool recipe (ordered)

1. **Gate** — Apply the table above; if skip, document and stop (or run **`validate:all`** only if the diff still warrants it).
2. **`npm run validate:all`** (repo root) when the diff touches **MCP** / schemas / **IA** indexes / **fixtures** / **`.cursor/skills/`** / **`.cursor/specs/`** bodies that feed indexes — same policy as **[`project-implementation-validation`](../project-implementation-validation/SKILL.md)**.
3. **Compile gate** — After **`Assets/`** **C#** changes: prefer **`unity_bridge_command`** **`kind`:** **`get_compilation_status`** or **`unity_compile`** when the **Editor** is open for **Path B**; otherwise **`npm run unity:compile-check`** from repo root (**do not** skip because **`$UNITY_EDITOR_PATH`** is unset — dotenv + **macOS** Hub fallback). **Never** run **`unity:compile-check`** while the **Editor** holds the same **projectPath** lock. Full preference order: **[`close-dev-loop`](../close-dev-loop/SKILL.md)** § **Compile gate**.
4. **Scenario** — **v1:** committed id (e.g. **`reference-flat-32x32`**) or **`tools/fixtures/scenarios/agent-generated/{run-id}/save.json`** with **`--scenario-path`** (absolute path). **v2:** when **program** stage **31b** ships, prefer the builder output path documented in [`projects/TECH-31b-scenario-builder.md`](../../../projects/TECH-31b-scenario-builder.md) (stable artifact location).
5. **Path A** or **Path B** (below).
6. **Iterate** — On failure, fix code or environment, then repeat from step 2 up to **`{MAX_ITERATIONS}`** (default **2**, same as **`close-dev-loop`**).
7. **Handoff** — English verdict, artifact paths, exit codes; request **human** **normal-game** **QA** (no **test mode** CLI flags / no reliance on agent-only queue file in player builds).

## Path A — **Agent test mode batch**

From **repository root**:

```bash
npm run unity:testmode-batch
```

- Invokes **`tools/scripts/unity-testmode-batch.sh`**: Unity **`-batchmode`** **`-nographics`** **`-executeMethod`** **`Territory.Testing.AgentTestModeBatchRunner.Run`** (no **`-quit`** on the shell — **`EditorApplication.Exit`** from C#).
- Default **`--scenario-id`**:**`reference-flat-32x32`** when omitted.
- Optional **`--scenario-path`** for ad-hoc **`GameSaveData`** JSON (prefer **absolute** path).
- Optional **`--quit-editor-first`** → **`tools/scripts/unity-quit-project.sh`** (**`Temp/UnityLockfile`** / **`lsof`**).
- **Load pipeline:** runner resolves the scenario file then calls **`GameSaveManager.LoadGame`** only; optional **`-testSimulationTicks`** → **`SimulationManager.ProcessSimulationTick`** (same tick entry as normal simulation).
- Artifacts: **`tools/reports/agent-testmode-batch-*.json`**, Unity log **`tools/reports/unity-testmode-batch-*.log`**. Transient **`tools/reports/.agent-testmode-batch-state.json`** may appear during the run (ignored with other report artifacts).

### Path A — worked example (**macOS**)

```bash
cd /path/to/territory-developer
npm run unity:testmode-batch -- --scenario-id reference-flat-32x32
```

Expect Unity log lines containing **`[AgentTestModeBatch]`** (e.g. state written, report path). Inspect the newest **`tools/reports/agent-testmode-batch-*.json`**: **`ok: true`**, **`exit_code: 0`**, **`scenario_id`** set.

## Path B — **IDE agent bridge** hybrid

Use when batch **Hub** CLI is unavailable or you need **`debug_context_bundle`** / screenshots in an open **Editor**.

1. Write a **single line** (scenario id) to **`tools/fixtures/scenarios/.queued-test-scenario-id`** (gitignored). Or use an ad-hoc save: there is no queue file for arbitrary paths — for **path**-based loads in **Editor**, use **Player** command-line **`-testScenarioPath`** or extend flow per [`tools/fixtures/scenarios/README.md`](../../../tools/fixtures/scenarios/README.md); the queue file is **id-only**.
2. **`npm run db:bridge-preflight`** — interpret exit codes per **`bridge-environment-preflight`** (0 proceed; 1 no URL; 2 server; 3 migrate; 4 SQL error).
3. **`unity_bridge_command`** **`kind`:** **`enter_play_mode`**, **`timeout_ms`:** **`40000`** → poll **`get_play_mode_status`** (**same `timeout_ms`**) until **`play_mode_ready`** / grid ready as needed.
4. **`unity_bridge_command`** **`kind`:** **`debug_context_bundle`**, **`timeout_ms`:** **`40000`**, **`seed_cell`:** **`"x,y"`** (required). Optionally **`get_console_logs`**, **`capture_screenshot`** (**`include_ui: true`** when overlay must show) per **`ide-bridge-evidence`**.
5. **`unity_bridge_command`** **`kind`:** **`exit_play_mode`**, **`timeout_ms`:** **`40000`**.

**Load pipeline:** **`TestModeCommandLineBootstrap`** consumes the queue file and drives the same **test mode** entry as CLI; load remains **`GameSaveManager.LoadGame`** only.

### Path B — worked example

With **Postgres** configured and **Editor** on **REPO_ROOT**:

1. `echo reference-flat-32x32 > tools/fixtures/scenarios/.queued-test-scenario-id` (one line, no extra newline if possible).
2. `npm run db:bridge-preflight`
3. **`unity_bridge_command`** **`enter_play_mode`** → **`get_play_mode_status`** until ready.
4. **`unity_bridge_command`** **`debug_context_bundle`** with **`seed_cell`:** **`"0,0"`** (adjust to your map).
5. **`unity_bridge_command`** **`exit_play_mode`**.

## Exit codes and failure classes

| Source | Code / class | Meaning / action |
|--------|----------------|------------------|
| **`unity-testmode-batch.sh`** | **0** | Success (**Unity** exit **0**). |
| | **1** | Bad script args or both id and path set. |
| | **2** | Unity binary missing / not executable — set **`UNITY_EDITOR_PATH`** or **macOS** Hub path. |
| | **3** | **`--quit-editor-first`** / **`unity-quit-project.sh`** — lock still held (**exit 3** from quit helper) or quit failed. |
| | **Other** | Propagated **Unity** / **`EditorApplication.Exit`** code. |
| **`unity-quit-project.sh`** | **0** | No lock or lock released. |
| | **1** | Invalid args. |
| | **3** | Lock still held after **SIGTERM**/**SIGKILL**. |
| **`AgentTestModeBatchRunner`** (**`EditorApplication.Exit`**) | **4** | Test mode disallowed; bad/missing **`-testScenarioId`**/**`-testScenarioPath`**; missing save; **MainScene** open failure. |
| | **6** | **Play Mode** / grid wait failure; **`LoadGame`** or simulation exception; unexpected **update** error. |
| | **7** | Timed out waiting for **Play Mode** to stop after load. |
| **MCP** / bridge | **`db_unconfigured`** / connection errors | No **`DATABASE_URL`** — configure per [`docs/postgres-ia-dev-setup.md`](../../../docs/postgres-ia-dev-setup.md). |
| | Job **`timeout`** / **`timeout_ms`** | **Unity** did not complete the job — see **`ide-bridge-evidence`**; check **Editor** logs. |
| **`get_compilation_status`** | **`compiling`**, **`compilation_failed`**, **`last_error_excerpt`** | Same queue as **IDE agent bridge**; see **`close-dev-loop`** compile gate. |

## Parameterize (replace before running)

| Placeholder | Meaning |
|-------------|---------|
| **`{MAX_ITERATIONS}`** | Fix → re-verify cycles (default **2**). |
| **`{SCENARIO_ID}`** | Kebab-case id under **`tools/fixtures/scenarios/`** (e.g. **`reference-flat-32x32`**). |
| **`{SEED_CELL}`** | **`"x,y"`** for **`debug_context_bundle`** on **Path B**. |

## Seed prompt (parameterize)

```markdown
Run the agent-test-mode-verify workflow for the completed spec work.
Follow .cursor/skills/agent-test-mode-verify/SKILL.md (Path A: glossary Agent test mode batch; Path B: glossary IDE agent bridge).
Use territory-ia unity_bridge_command when Path B applies (timeout_ms: 40000); use npm run unity:testmode-batch when Path A applies.
Fixtures must stay GameSaveData-shaped; load only through GameSaveManager.LoadGame (persistence-system Load pipeline).
End with a Verification section per docs/agent-led-verification-policy.md (validate:all, compile if C#, batch JSON, bridge outcome).
Max iterations: {MAX_ITERATIONS}.
```

## Handoff (required shape)

- **Verdict:** pass / fail / skipped (with reason if skipped).
- **Path used:** **A** and/or **B**.
- **Artifacts:** newest **`agent-testmode-batch-*.json`** path; bridge **`bundle`** / screenshot paths if any.
- **Human ask:** confirm behavior in **normal** game (no **test mode** flags).

## Verification block (agent completion messages)

After substantive work, include a **Verification** section matching [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md): **`npm run validate:all`** (exit code); **`npm run unity:compile-check`** if **`Assets/`** **C#** changed (**N/A** + reason otherwise); **Path A** — **`npm run unity:testmode-batch`** exit code + **`agent-testmode-batch-*.json`** **`ok`/`exit_code`**; **Path B** — **`db:bridge-preflight`** then bridge calls with **`timeout_ms`:** **`40000`** — outcome (**`ok`**, **`error`**, **`timeout`**, **`command_id`**). If **Path B** not run, state why.
