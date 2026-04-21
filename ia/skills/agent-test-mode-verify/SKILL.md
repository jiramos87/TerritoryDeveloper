---
purpose: "Run after project-spec-implement (or standalone) when agent-led test mode verification is required: gate (run vs skip), Path A (glossary Agent test mode batch) or Path B (glossary IDE agent bridge hybrid with…"
audience: agent
loaded_by: skill:agent-test-mode-verify
slices_via: none
name: agent-test-mode-verify
description: >
  Run after project-spec-implement (or standalone) when agent-led test mode verification is required:
  gate (run vs skip), Path A (glossary Agent test mode batch) or Path B (glossary IDE agent bridge hybrid
  with runtime_state / queue file), bounded iterate with validate:all / compile gates, structured handoff for
  human normal-game QA. Triggers: "agent test mode loop", "verify in test mode without opening Unity",
  "batchmode scenario check", "post-implement Play Mode suite". Design trace: projects/TECH-31a3-agent-test-mode-verify-skill.md
  (TECH-31 stage 31a3).
model: inherit
---

Start: fetch `mcp__territory-ia__runtime_state` (fallback: read `ia/state/runtime-state.json`) to honor last verify / bridge state + queued scenario.

# Agent test-mode verification loop

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). **Additional exceptions:** structured batch report contents (`tools/reports/agent-testmode-batch-*.json`), MCP `unity_bridge_command` JSON inputs/outputs.

Does not replace human issue verification per AGENTS.md.

**Vocabulary:** Agent test mode batch = headless Editor `npm run unity:testmode-batch` (no Postgres). IDE agent bridge = `unity_bridge_command` + Postgres `agent_bridge_job` + Editor on REPO_ROOT. Fixtures: `GameSaveData`-shaped; load via `GameSaveManager.LoadGame` only. Operator matrix: [`tools/fixtures/scenarios/README.md`](../../../tools/fixtures/scenarios/README.md).

**Related:** [`project-spec-implement`](../project-spec-implement/SKILL.md) · [`close-dev-loop`](../close-dev-loop/SKILL.md) (compile gate + `debug_context_bundle` diffs) · [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) · [`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md) · [`project-implementation-validation`](../project-implementation-validation/SKILL.md).

**Policy:** [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md) — attempt Path A and Path B when tools allow; do not skip bridge for convenience. Bridge `timeout_ms`: `40000` default.

## Prerequisites

| Path | Requires |
|------|----------|
| **Path A** (batch) | Repo root; `UNITY_EDITOR_PATH` or macOS Hub inference. No other Unity process on REPO_ROOT — **must** pass `--quit-editor-first`. No Postgres needed. |
| **Path B** (bridge) | `DATABASE_URL` or `config/postgres-dev.json`; migration `0008`; Editor open on REPO_ROOT (`npm run unity:ensure-editor`); `unity_bridge_command`. Run `npm run db:bridge-preflight` first session call. Timeout → escalation protocol in verification policy. |

## Gate — run vs skip

None apply → **skip** loop, state why in handoff; rely on `validate:all` + normal review.

| Run the loop when the diff or project spec **§7b** / **§8** touches… | Notes |
|------------------------------------------------------------------------|--------|
| **`GameSaveManager`** / **`GameSaveData`** / save-shaped fixtures | **Load pipeline** risk |
| **Test mode** bootstrap, **`TestModeCommandLineBootstrap`**, scenario resolution | Entry and flags |
| **Committed** or **agent-generated** scenario JSON under **`tools/fixtures/scenarios/`** | Fixture contract |
| **`GridManager`** init / **simulation** tick harness (**`SimulationManager`**, **`ProcessSimulationTick`**) | Batch runner exercises these |
| **HUD** / **Play Mode** assertions, or explicit **§7b** row requiring batch or bridge | Product ask |

## Tool recipe (ordered)

1. **Gate** — Apply table above; skip → document + stop.
2. **`validate:all`** — When diff touches MCP/schemas/IA indexes/fixtures/specs.
3. **Compile gate** — After C# changes: prefer `unity_bridge_command` `get_compilation_status`/`unity_compile` (Editor open, Path B); else `npm run unity:compile-check`. Never run compile-check while Editor holds projectPath lock. Full order: [`close-dev-loop`](../close-dev-loop/SKILL.md) § Compile gate.
4. **Scenario** — Committed id (e.g. `reference-flat-32x32`) or agent-generated path with `--scenario-path`. Prefer `scenario_descriptor_v1` layout from [`BUILDER.md`](../../../tools/fixtures/scenarios/BUILDER.md).
5. **Path A or B** (below). Both in one session → Path A first (`--quit-editor-first`), then `unity:ensure-editor` before Path B.
6. **Iterate** — On failure, fix + repeat from step 2 up to `{MAX_ITERATIONS}` (default 2).
7. **Handoff** — Verdict, artifact paths, exit codes; request human normal-game QA.

## Path A — Agent test mode batch

Project lock: batchmode needs REPO_ROOT exclusively. Use `--quit-editor-first` when Editor might be running.

```bash
npm run unity:testmode-batch -- --quit-editor-first --scenario-id reference-flat-32x32
```

- Invokes `unity-testmode-batch.sh`: Unity `-batchmode -nographics -executeMethod Territory.Testing.AgentTestModeBatchRunner.Run`.
- Default `--scenario-id`: `reference-flat-32x32`. Optional `--scenario-path` for ad-hoc JSON (absolute path).
- `--quit-editor-first` → `unity-quit-project.sh` (Lockfile/lsof) to release lock.
- Load pipeline: `GameSaveManager.LoadGame` only; optional `-testSimulationTicks` → `ProcessSimulationTick`.
- `--golden-path` / `-testGoldenPath`: asserts CityStats fields vs committed JSON — mismatch → exit 8.
- Artifacts: `tools/reports/agent-testmode-batch-*.json` (schema_version 2), `unity-testmode-batch-*.log`.
- Optional Postgres: `DATABASE_URL` + migration `0009` → `MetricsRecorder` appends `city_metrics_history`; query via `city_metrics_query`. Does not replace golden CityStats JSON.

## Path B — IDE agent bridge hybrid

Use when batch CLI unavailable or need `debug_context_bundle`/screenshots in open Editor.

1. Write scenario id (single line) to `tools/fixtures/scenarios/.queued-test-scenario-id` (gitignored) for Unity consumption. Prefer `mcp__territory-ia__runtime_state` `action: write`, `patch: { "queued_test_scenario_id": "<id>" }` for harness-visible state. Queue file is id-only — path-based loads use `-testScenarioPath`.
2. `npm run db:bridge-preflight` — exit codes per `bridge-environment-preflight` (0 proceed; 1 no URL; 2 server; 3 migrate; 4 SQL error).
3. `unity_bridge_command` `enter_play_mode`, `timeout_ms: 40000` → poll `get_play_mode_status` until ready.
4. `unity_bridge_command` `debug_context_bundle`, `timeout_ms: 40000`, `seed_cell: "x,y"`. Optionally `get_console_logs`, `capture_screenshot` (`include_ui: true`) per `ide-bridge-evidence`.
5. `unity_bridge_command` `exit_play_mode`, `timeout_ms: 40000`.

Load pipeline: `TestModeCommandLineBootstrap` consumes queue file; load remains `GameSaveManager.LoadGame` only.

## Exit codes

| Source | Code | Meaning |
|--------|------|---------|
| `unity-testmode-batch.sh` | 0 | Success |
| | 1 | Bad args or both id+path set |
| | 2 | Unity binary missing — set `UNITY_EDITOR_PATH` |
| | 3 | Lock still held after quit attempt |
| | Other | Propagated Unity/`EditorApplication.Exit` code |
| `AgentTestModeBatchRunner` | 4 | Bad/missing scenario; golden file missing; MainScene failure |
| | 6 | Play Mode/grid wait failure; LoadGame/simulation exception |
| | 7 | Timed out waiting for Play Mode stop |
| | 8 | Golden CityStats mismatch |
| | 9 | `HeightMap[x,y] != CityCell.height` invariant #1 violation (post-load or post-tick sweep) |
| MCP/bridge | `db_unconfigured` | No `DATABASE_URL` |
| | `timeout` | Unity did not complete job |
| `get_compilation_status` | `compilation_failed` | See `close-dev-loop` compile gate |

## Gotchas

- **Stale `UnityLockfile` after Editor crash / SIGTERM** — lockfile can survive as orphan; `lsof` check before assuming lock is live. Remove if no live process holds it, else `--quit-editor-first` fails with exit 3.
- **Sim tick requires explicit `--simulation-ticks N`** — default run applies 0 ticks; report `simulation_ticks_applied: 0` only validates load pipeline, not sim harness. Regression gates that must exercise tick path need `--simulation-ticks 3` (or higher).
- **Golden path assertion (stricter gate)** — `reference-flat-32x32` ships committed `agent-testmode-golden-ticks3.json`. Add `--golden-path <file>` to upgrade from smoke (exit 0) to value assertion (exit 8 on mismatch).

## Placeholders

| Key | Default / meaning |
|-----|-------------------|
| `{MAX_ITERATIONS}` | 2 (fix→re-verify cycles) |
| `{SCENARIO_ID}` | Kebab-case id under `tools/fixtures/scenarios/` |
| `{SEED_CELL}` | `"x,y"` for `debug_context_bundle` (Path B) |

## Seed prompt (parameterize)

```markdown
Run the agent-test-mode-verify workflow for the completed spec work.
Follow ia/skills/agent-test-mode-verify/SKILL.md (Path A: glossary Agent test mode batch; Path B: glossary IDE agent bridge).
Use territory-ia unity_bridge_command when Path B applies (timeout_ms: 40000); use npm run unity:testmode-batch -- --quit-editor-first (plus scenario flags) when Path A applies if an Editor might hold REPO_ROOT.
Fixtures must stay GameSaveData-shaped; load only through GameSaveManager.LoadGame (persistence-system Load pipeline).
End with a Verification section per docs/agent-led-verification-policy.md (validate:all, compile if C#, batch JSON, bridge outcome).
Max iterations: {MAX_ITERATIONS}.
```

## Handoff (required shape)

- **Verdict:** pass / fail / skipped (with reason if skipped).
- **Path used:** **A** and/or **B**.
- **Artifacts:** newest **`agent-testmode-batch-*.json`** path; bridge **`bundle`** / screenshot paths if any.
- **Human ask:** confirm behavior in **normal** game (no **test mode** flags).

## Verification block

Include per [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md): `validate:all` (exit code); `unity:compile-check` if C# changed (N/A + reason otherwise); Path A exit code + report JSON `ok`/`exit_code`; Path B `db:bridge-preflight` + bridge calls (`timeout_ms: 40000`) — outcome. If Path B not run, state why.
