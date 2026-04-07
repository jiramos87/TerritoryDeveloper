# TECH-75 — Close Dev Loop: agent-driven Play Mode verification without human data shuttle

> **Orchestration spec** — no umbrella issue. Sub-issues **TECH-75a** / **TECH-75b** / **TECH-75c** live in [`BACKLOG.md`](../../BACKLOG.md) **§ Agent ↔ Unity & MCP context lane**.
> **Status:** Draft
> **Created:** 2026-04-07
> **Last updated:** 2026-04-07

## 1. Summary

Today every visual bug fix requires **3–8 manual round-trips** where the developer enters Play Mode, runs QA, digests data, explains it to the agent, watches the agent code, then repeats. The developer is a "data shuttle" between Unity and the IDE agent. **Close Dev Loop** eliminates that shuttle: the agent enters Play Mode, collects runtime evidence (cell data + screenshot + console), applies the fix, re-verifies, and only presents the human with a final "approve / reject" decision.

**Shipped foundation:** Phase 1 bridge (`unity_bridge_command` / `unity_bridge_get` via Postgres `agent_bridge_job`) with `export_agent_context`, `capture_screenshot`, `get_console_logs`. **Charter:** [`docs/unity-ide-agent-bridge-analysis.md`](../../docs/unity-ide-agent-bridge-analysis.md).

**MVP exit criteria:** An agent can — in a single chat turn — enter Play Mode, collect cell data + screenshot at a seed cell, exit Play Mode, and present the result to the human. No manual Unity interaction required between "implement fix" and "verify result."

## 2. Goals and Non-Goals

### 2.1 Goals

1. Agent can **enter and exit Play Mode** via bridge commands, without human clicking Play.
2. Agent can collect **cell data + screenshot + console logs in one round-trip** ("context bundle").
3. Agent can **detect terrain anomalies** (void cells, missing cliffs, HeightMap/Cell.height mismatch) from exported data without relying on human visual inspection.
4. A **Cursor Skill** orchestrates the fix → verify → report cycle end-to-end.

### 2.2 Non-Goals (Out of Scope)

1. HTTP bridge (Phase 2 of charter) — file/Postgres queue is sufficient for MVP.
2. Headless CI / `-batchmode` runs — MVP targets developer machines with Unity Editor open.
3. Visual pixel-diff automation — structural anomaly detection is enough for MVP; pixel comparison is a follow-up.
4. Changes to water prefabs, simulation logic, or save/load — tooling only.
5. Performance optimization of geography init (**TECH-15**) or sim ticks (**TECH-16**) — beneficial but not a blocker; those issues remain independent.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Agent | I enter Play Mode, wait for grid init, capture evidence at specific cells, and exit — no human needed. | Bridge commands `enter_play_mode` / `exit_play_mode` work; agent polls readiness. |
| 2 | Agent | I collect cell neighborhood + screenshot + console in one tool call. | MCP sugar tool returns combined payload; one round-trip instead of 3–4. |
| 3 | Agent | I can tell if a border cliff is missing without a human describing the screenshot. | Anomaly detector flags cells where `height > MIN_HEIGHT` with no cliff children on visible border faces. |
| 4 | Developer | After the agent implements and verifies a fix, I only review a before/after summary. | Cursor Skill presents diff: cell child counts changed, screenshot paths, anomaly count delta. |

## 4. Current State

### 4.1 Shipped infrastructure

| Component | Status | Gaps for MVP |
|-----------|--------|-------------|
| `AgentBridgeCommandRunner.cs` | Dispatches `export_agent_context`, `capture_screenshot`, `get_console_logs` | No `enter_play_mode` / `exit_play_mode`; no readiness signal |
| `unity_bridge_command` MCP | Sends command, polls result | Each evidence type is a separate round-trip |
| `AgentBridgeScreenshotCapture.cs` | Captures Game view PNG in Play Mode | Requires Game view visible + focused |
| Postgres `agent_bridge_job` | Queue + dequeue + complete | Works; no schema changes needed for new `kind` values |
| `ide-bridge-evidence` Skill | Orchestrates bridge calls for one-off evidence capture | Does not loop fix → verify; does not enter/exit Play Mode |

### 4.2 Absorbed issues

The following **existing backlog** items are **absorbed** into this orchestration (their scope is subsumed or delivered by the sub-issues below). Original issue ids are retired from `BACKLOG.md`:

| Former issue | What it covered | Absorbed into |
|-------------|-----------------|---------------|
| **TECH-59** | MCP staging for Editor export registry payload | **TECH-75a** — staging concept is simpler when the agent drives Play Mode directly; registry staging becomes a secondary concern once the bridge can enter Play Mode and export without manual menu clicks |

Issues that remain **independent** (useful for closed loop but not MVP-blocking):

| Issue | Why independent |
|-------|----------------|
| **TECH-15** | Init performance — makes the loop faster but not functionally required |
| **TECH-16** | Sim tick performance — same rationale |
| **TECH-33** | Asset introspection — useful for prefab debugging but not part of the verify loop |
| **TECH-43** | Event log / telemetry — future observability; MVP anomaly detection is simpler |
| **TECH-31** | Scenario fixtures — deterministic replay is post-MVP |
| **TECH-18** | IA → Postgres — orthogonal to the runtime bridge |

## 5. Proposed Design

### 5.1 Target behavior (product)

Agent workflow for a visual bug fix:

```
1. Agent reads issue, analyzes code, implements fix
2. Agent calls: enter_play_mode → waits for ready signal
3. Agent calls: debug_context_bundle(seed_cell="62,0") →
   receives: { cells: [...], screenshot_path: "...", console_lines: [...], anomalies: [...] }
4. Agent analyzes: "0 anomalies at border; cliff children count matches height"
5. Agent calls: exit_play_mode
6. Agent presents to human: "Fix verified. Before: 2 anomalies. After: 0. Screenshot attached."
7. Human: approves or requests iteration
```

### 5.2 Architecture

```
┌────────────────────┐                              ┌─────────────────────┐
│  Cursor Agent       │                              │  Unity Editor       │
│  (IDE + MCP)        │                              │                     │
│                     │  enter_play_mode              │  AgentBridge        │
│  close-dev-loop     │ ────────────────────────→    │  CommandRunner      │
│  Skill              │  ready: { grid_size: 128 }   │                     │
│                     │ ←────────────────────────    │  EditorApplication  │
│                     │  debug_context_bundle         │  .EnterPlaymode()   │
│                     │ ────────────────────────→    │                     │
│                     │  { cells, screenshot, logs,  │  export + screenshot│
│                     │    anomalies }               │  + anomaly scan     │
│                     │ ←────────────────────────    │                     │
│                     │  exit_play_mode               │                     │
│                     │ ────────────────────────→    │  EditorApplication  │
│                     │                              │  .ExitPlaymode()    │
└────────────────────┘                              └─────────────────────┘
```

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-07 | 3 sub-issues (75a/b/c), no umbrella row | Product owner prefers orchestration spec over umbrella issue; sub-issues are sequential | One monolithic issue; 5+ micro-issues |
| 2026-04-07 | Absorb **TECH-59** | TECH-59's MCP staging for registry payload is subsumed by the agent driving Play Mode directly — the agent no longer needs to pre-stage export params if it can enter Play Mode and call exports itself | Keep TECH-59 independent |
| 2026-04-07 | Keep **TECH-15**, **TECH-16**, **TECH-33**, **TECH-43**, **TECH-31**, **TECH-18** independent | These improve speed or depth of the loop but are not functionally required for the MVP | Absorb everything |
| 2026-04-07 | Anomaly detection in Unity (C#), not in MCP/Node | Runtime has access to `Cell`, `HeightMap`, `WaterMap`, cliff children — can scan efficiently without serializing the whole grid | Post-process JSON in Node |

## 7. Implementation Plan — MVP Sub-Issues

### TECH-75a — Play Mode bridge commands + readiness signal

**Goal:** Agent can enter and exit Play Mode programmatically.

**Scope:**
- New bridge `kind` values: `enter_play_mode`, `exit_play_mode`, `get_play_mode_status`
- `AgentBridgeCommandRunner.cs`: dispatch `EditorApplication.EnterPlaymode()` / `ExitPlaymode()`
- Readiness signal: after entering Play Mode, poll until `GridManager` is initialized (grid width > 0), then mark the bridge job as completed with `{ ready: true, grid_width: N, grid_height: N }`
- MCP: `unity_bridge_command` already dispatches by `kind` — no new tool registration needed, just new `kind` strings
- Guard: if already in Play Mode, `enter_play_mode` returns success immediately; if not in Play Mode, `exit_play_mode` returns success immediately

**Deliverables:**
- [ ] `AgentBridgeCommandRunner.cs` — `enter_play_mode` / `exit_play_mode` / `get_play_mode_status` dispatch
- [ ] Readiness polling (coroutine or `EditorApplication.update` callback waiting for `GridManager.width > 0`)
- [ ] MCP `unity_bridge_command` documentation for new `kind` values
- [ ] Test: agent can round-trip `enter_play_mode` → `get_play_mode_status` → `exit_play_mode`

**Depends on:** none (extends shipped Phase 1 bridge)

### TECH-75b — Context bundle + anomaly detection

**Goal:** One bridge call returns cell data + screenshot + console + anomaly flags.

**Scope:**
- New bridge `kind`: `debug_context_bundle`
- Params: `seed_cell` (required), `include_screenshot` (default true), `include_console` (default true), `include_anomaly_scan` (default true), `filename_stem` (optional)
- Response JSON combines: Moore neighborhood cell data (same as `export_agent_context`), screenshot path, filtered console lines, and an `anomalies` array
- **Anomaly scanner** (`AgentBridgeAnomalyScanner.cs`): scans cells in the exported neighborhood for:
  - `height > MIN_HEIGHT` on south/east map border with no `CliffSouth`/`CliffEast` children → "missing_border_cliff"
  - `HeightMap[x,y] != cell.height` → "heightmap_cell_desync"
  - Water-shore primary prefab with unexpected cliff children toward off-grid → "redundant_shore_cliff"
  - (Extensible: add rules as bugs are fixed)
- MCP: same `unity_bridge_command` tool, new `kind` string; sugar MCP wrapper `unity_debug_bundle` (optional, reduces token cost)

**Deliverables:**
- [ ] `AgentBridgeCommandRunner.cs` — `debug_context_bundle` dispatch combining existing export + screenshot + console + new anomaly scan
- [ ] `AgentBridgeAnomalyScanner.cs` — rule-based cell scanner with typed anomaly results
- [ ] MCP documentation for `debug_context_bundle` response schema
- [ ] Optional: `unity_debug_bundle` sugar tool in `tools/mcp-ia-server/`

**Depends on:** TECH-75a (needs Play Mode to be active for meaningful data)

### TECH-75c — Close-dev-loop Cursor Skill

**Goal:** End-to-end Skill that orchestrates: pre-fix capture → implement → post-fix capture → diff → report.

**Scope:**
- Cursor Skill `.cursor/skills/close-dev-loop/SKILL.md`
- **Tool recipe:**
  1. Read backlog issue + project spec (existing MCP tools)
  2. `enter_play_mode` → wait ready
  3. `debug_context_bundle` at repro cells → save as "before" baseline
  4. `exit_play_mode`
  5. Implement fix (agent edits C#)
  6. Wait for Unity recompilation (poll `get_console_logs` for compilation messages, or simple delay)
  7. `enter_play_mode` → wait ready
  8. `debug_context_bundle` at same cells → save as "after"
  9. `exit_play_mode`
  10. Compare: anomaly count delta, child name changes, height changes
  11. Present to human: before/after summary + screenshot paths + "anomalies resolved: N/M"
- Skill triggers: "close dev loop", "verify fix in play mode", "agent-driven QA"
- Integrates with `project-spec-implement` (optional post-phase verification step)

**Deliverables:**
- [ ] `.cursor/skills/close-dev-loop/SKILL.md` — Skill with tool recipe
- [ ] Update `.cursor/skills/README.md` — add skill to index
- [ ] Update `AGENTS.md` — pointer to skill for visual bug workflows
- [ ] Optional: update `project-spec-implement` Skill to reference close-dev-loop as a verification step

**Depends on:** TECH-75b (needs bundle + anomaly detection for meaningful diffs)

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Bridge `enter_play_mode` / `exit_play_mode` work | MCP / dev machine | `unity_bridge_command` with new `kind` values | Postgres `agent_bridge_job` + Unity on REPO_ROOT |
| Context bundle returns combined payload | MCP / dev machine | `unity_bridge_command` `kind: debug_context_bundle` | Play Mode; `seed_cell` required |
| Anomaly scanner flags known-bad cell | MCP / dev machine | Bundle at a map border cell with intentional void | Manual setup or test map |
| Skill orchestrates full cycle | Dev machine | Run close-dev-loop Skill in Cursor Agent chat | End-to-end human confirmation |
| MCP / IA touched | Node | `npm run validate:all` | After MCP tool docs/registration changes |

## 8. Acceptance Criteria

- [ ] Agent can **enter Play Mode**, **wait for grid ready**, **collect evidence**, and **exit Play Mode** — zero human Unity interaction.
- [ ] Single bridge call (`debug_context_bundle`) returns cell data + screenshot + console + anomalies.
- [ ] Anomaly scanner catches at least: missing border cliffs, HeightMap/cell desync.
- [ ] Cursor Skill presents before/after diff with anomaly count delta and screenshot paths.
- [ ] MCP docs (`docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`) updated for new `kind` values.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | … | … | … |

## 10. Lessons Learned

- TBD at closeout.

## Open Questions (tooling and agent workflow)

1. **Recompilation wait:** After the agent edits C# and Unity recompiles, how does the agent know compilation finished? Options: (a) poll `get_console_logs` for "Compilation completed" pattern; (b) dedicated `get_compilation_status` bridge kind; (c) simple configurable delay. Product preference?
2. **Game view focus:** `capture_screenshot` requires the Game view to be visible. Should `enter_play_mode` also force Game view to front? (EditorApplication API can do this.)
3. **Multiple seed cells:** Should `debug_context_bundle` accept an array of seed cells for a single pass, or should the Skill call it multiple times?

**N/A (game logic)** — no HeightMap, Save data, or simulation changes.
