---
purpose: Use when you need Unity Play Mode evidence (Console logs or Game view screenshots) via territory-ia unity_bridge_command for issue acceptance or debugging.
audience: agent
loaded_by: skill:ide-bridge-evidence
slices_via: none
name: ide-bridge-evidence
description: >
  Use when you need Unity Play Mode evidence (Console logs or Game view screenshots) via territory-ia
  unity_bridge_command for issue acceptance or debugging. Requires Postgres agent_bridge_job (migration 0008),
  DATABASE_URL, and Unity Editor on REPO_ROOT with AgentBridgeCommandRunner. Triggers: "bridge screenshot",
  "get unity logs from MCP", "capture_screenshot include_ui", "enter_play_mode", "exit_play_mode",
  "get_play_mode_status", "get_compilation_status", "unity_compile", "debug_context_bundle", "IDE agent bridge evidence".
---

# IDE agent bridge — Play Mode evidence (logs + screenshots)

Optional, dev-machine-only `unity_bridge_command` / `unity_bridge_get` (glossary IDE agent bridge). Does not replace CI or `npm run validate:all`.

**Policy:** [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md) — `timeout_ms: 40000` initial; on timeout → `npm run unity:ensure-editor` → retry 60 s. Ceiling: 120 s (`UNITY_BRIDGE_TIMEOUT_MS_MAX`).

**Related:** [`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md) (run before first bridge call) · [`plan-author`](../plan-author/SKILL.md) (§7b rows — retired `project-spec-kickoff` folded here per M6 collapse) · [`project-spec-implement`](../project-spec-implement/SKILL.md) (phase verification) · [`close-dev-loop`](../close-dev-loop/SKILL.md) (before/after `debug_context_bundle`) · [`project-implementation-validation`](../project-implementation-validation/SKILL.md) (Node checks). **Normative:** [`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md), unity-development-context §10.

## Prerequisites

| Requirement | Notes |
|---|---|
| `DATABASE_URL` or `config/postgres-dev.json` | Same as Editor export registry |
| Migrations `0008_agent_bridge_job.sql` + `0010_agent_bridge_lease.sql` | `npm run db:migrate` |
| Unity Editor on repo root | `AgentBridgeCommandRunner` polls dequeue. Missing → `npm run unity:ensure-editor` |
| Play Mode for `capture_screenshot` | Edit Mode → `ok: false` error |

## Play Mode lease (multi-agent concurrency)

When multiple agents or sessions may be active on the same machine, acquire the Play Mode lease before `enter_play_mode` and release it after `exit_play_mode`. Non-Play-Mode commands (`export_agent_context`, `get_compilation_status`, `get_console_logs`) do not require a lease.

```
unity_bridge_lease(action: acquire, agent_id: "{ISSUE_ID or session tag}", kind: play_mode)
  → store lease_id
  → [Play Mode commands]
unity_bridge_lease(action: release, lease_id: "{lease_id}")
```

**On `lease_unavailable`:** retry with 60 s backoff up to 10 min total. If still busy, skip Play Mode evidence and emit `play_mode_lease: skipped_busy` in the Verification block. TTL is 8 min: a crashed agent's lease auto-expires; call `status` to confirm.

## Agent-led verification (Play Mode smoke)

Agents run acceptance via bridge instead of asking human to click Play/Stop. Order:

0b. `unity_bridge_lease(acquire)` — claim Play Mode lease (store `lease_id`).
1. `get_play_mode_status` — baseline.
2. `enter_play_mode` — expect `ok: true`, `ready: true`, `play_mode_state: play_mode_ready`, grid dimensions when `has_grid_dimensions`.
3. `get_play_mode_status` — confirm active.
4. Optional: `debug_context_bundle` with `seed_cell "x,y"` — `response.bundle` (cell export, screenshot, console, anomalies). Deferred completion for PNG.
5. `exit_play_mode` — expect `play_mode_state: edit_mode`.
6. `unity_bridge_lease(release, lease_id)` — release lease.

Record `command_id` values in chat/spec. For Game view: `capture_screenshot` with `include_ui: true`, or `debug_context_bundle` (uses Game view `ScreenCapture` when `include_screenshot` true).

## MCP tools

| Tool | Role |
|---|---|
| `unity_bridge_command` | Insert `agent_bridge_job`, poll until completed/failed or `timeout_ms` (default 30000, max 120000 — use 40000 initial; on timeout → escalation protocol) |
| `unity_bridge_get` | Read response by `command_id` (optional `wait_ms`) |
| `unity_compile` | Alias: `unity_bridge_command` with `kind: get_compilation_status` |

## `kind` values

### `enter_play_mode`
`EditorApplication.EnterPlaymode()` (Game view focus). Completes when `GridManager.isInitialized` (~24 s max). Response: `ok`, `ready`, `play_mode_state` (`play_mode_ready`), `grid_width`/`grid_height` when `has_grid_dimensions`, `already_playing` if active. Uses `SessionState` to survive domain reload.

### `exit_play_mode`
Exits Play Mode; completes in Edit Mode. `already_stopped` if not playing.

### `get_play_mode_status`
Immediate (no transition). `play_mode_state`: `edit_mode` | `play_mode_loading` | `play_mode_ready`; optional grid dimensions.

### `get_compilation_status` / `unity_compile`
Synchronous compile snapshot (Edit Mode). `compilation_status`: `compiling`, `compilation_failed`, `last_error_excerpt`, `recent_error_messages`. For batchmode without Editor lock: `npm run unity:compile-check`.

### `export_agent_context`
Reports → Export Agent Context; optional `seed_cell "x,y"` for Moore center.

### `get_console_logs`
Buffered Console lines (`response.log_lines`). Optional: `since_utc`, `severity_filter` (all|log|warning|error), `tag_filter`, `max_lines` (1–2000).

### `capture_screenshot`
Writes `tools/reports/bridge-screenshots/*.png` (gitignored). Optional: `filename_stem`, `camera`.
- `include_ui: false` (default): Camera render (world + Screen Space - Camera); not Overlay.
- `include_ui: true`: `ScreenCapture` of Game view (includes Overlay HUD). Ignores `camera`. Game tab must be visible; ~15 s timeout → `ok: false`.

### `debug_context_bundle`
Play Mode + initialized GridManager. Required: `seed_cell "x,y"`. Optional: `include_screenshot`, `include_console`, `include_anomaly_scan` (defaults true); `filename_stem`, console filters. Response: `ok` reflects export + screenshot; `bundle` sub-results: `cell_export`, `screenshot`, `console`, `anomalies`, `anomaly_count`. Screenshot uses Game view `ScreenCapture`; deferred completion — PNG path available only when job row shows `completed`.

## Operational limits

- `timeout_ms`: 30000 default, 120000 max. Use 40000 initial; on timeout → escalation protocol in [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md).
- Stuck `processing`: fail row with `agent-bridge-complete.mjs --failed`.

## CLI equivalent (no MCP)

One-shot `export_agent_context`: `npm run db:bridge-agent-context` (`BRIDGE_TIMEOUT_MS`, default 30000). For other kinds, `cd tools/mcp-ia-server && npx tsx -e` with `runUnityBridgeCommand` from `src/tools/unity-bridge-command.ts`.

## Seed prompt

```markdown
Use **ide-bridge-evidence** (`ia/skills/ide-bridge-evidence/SKILL.md`): call **territory-ia** **`unity_bridge_command`** for {KIND} with {PARAMS}. For screenshot/export/bundle in Play Mode, `enter_play_mode` first unless already playing. Attach artifact paths/bundle/log summary for {ISSUE_ID} acceptance.
```
