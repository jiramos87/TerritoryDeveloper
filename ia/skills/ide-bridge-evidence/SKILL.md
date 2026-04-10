---
name: ide-bridge-evidence
description: >
  Use when you need Unity Play Mode evidence (Console logs or Game view screenshots) via territory-ia
  unity_bridge_command for issue acceptance or debugging. Requires Postgres agent_bridge_job (migration 0008),
  DATABASE_URL, and Unity Editor on REPO_ROOT with AgentBridgeCommandRunner. Triggers: "bridge screenshot",
  "get unity logs from MCP", "capture_screenshot include_ui", "enter_play_mode", "exit_play_mode",
  "get_play_mode_status", "get_compilation_status", "unity_compile", "debug_context_bundle", "IDE agent bridge evidence".
---

# IDE agent bridge — Play Mode evidence (logs + screenshots)

This skill documents **optional**, **dev-machine-only** use of **territory-ia** **`unity_bridge_command`** / **`unity_bridge_get`** (glossary **IDE agent bridge**). It **does not** replace **CI** or **`npm run validate:all`** — there is no Unity in the **IA tools** workflow job.

**Owner policy:** [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md) — agents **attempt** bridge verification when **Postgres** + **Editor** can apply; use **`timeout_ms`:** **`40000`** (40 s initial) on **`unity_bridge_command`** / **`unity_compile`** for agent-led passes. On **timeout**, follow the **timeout escalation protocol** (`npm run unity:ensure-editor` → retry 60 s). Ceiling: **120 s** (`UNITY_BRIDGE_TIMEOUT_MS_MAX`).

**Related:** **[`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md)** — run before first bridge call in a session or when **`unity_bridge_command`** fails with DB errors. **[`project-spec-kickoff`](../project-spec-kickoff/SKILL.md)** (write **§7b** rows that reference these tools). **[`project-spec-implement`](../project-spec-implement/SKILL.md)** (optional verification after phases). **[`close-dev-loop`](../close-dev-loop/SKILL.md)** (full before/after **`debug_context_bundle`** cycle + compile gate). **[`project-implementation-validation`](../project-implementation-validation/SKILL.md)** (Node checks; bridge is a separate optional subsection). **Normative IA:** [`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md), **unity-development-context** §10, [`docs/unity-ide-agent-bridge-analysis.md`](../../../docs/unity-ide-agent-bridge-analysis.md).

## Prerequisites (all required)

| Requirement | Notes |
|-------------|--------|
| **`DATABASE_URL`** or **`config/postgres-dev.json`** | Same policy as **Editor export registry** |
| Migration **`0008_agent_bridge_job.sql`** applied | `npm run db:migrate` — see [`docs/postgres-ia-dev-setup.md`](../../../docs/postgres-ia-dev-setup.md) |
| **Unity Editor** open on **repository root** | **`AgentBridgeCommandRunner`** polls **`agent-bridge-dequeue.mjs`**. If not running, run **`npm run unity:ensure-editor`** (macOS; exit 0 = ready) |
| **Play Mode** for **`capture_screenshot`** | **Edit Mode** returns a **completed** job with **`ok: false`** and an English **`error`** |
| **Close Dev Loop** | Use **`enter_play_mode`** before evidence kinds if the agent should not click **Play** manually; then **`get_play_mode_status`**, **`debug_context_bundle`** (optional one-shot export + screenshot + console + **`bundle.anomalies`**), **`capture_screenshot`** / **`export_agent_context`** as needed; **`exit_play_mode`** when done |

## Agent-led verification (Play Mode smoke)

When **`unity_bridge_command`** is callable, **agents** should run acceptance themselves instead of asking the human to click **Play**/**Stop** (same **Postgres** + **Unity** prerequisites as above). Suggested order:

1. **`get_play_mode_status`** — baseline (**`edit_mode`** or document if already in **Play Mode**).
2. **`enter_play_mode`** — expect **`ok: true`**, **`ready: true`**, **`play_mode_state`:** **`play_mode_ready`**, and grid dimensions when **`has_grid_dimensions`** (after **`GridManager.isInitialized`**).
3. **`get_play_mode_status`** again while **Play Mode** is active.
4. Optional: **`debug_context_bundle`** with **`seed_cell`** `"x,y"` — expect **`response.bundle`** (cell export path, screenshot path unless **`include_screenshot: false`**, console lines unless **`include_console: false`**, **`anomalies`** unless **`include_anomaly_scan: false`**). Completes only after the PNG exists when a screenshot is requested (same deferred pump as **`capture_screenshot`**).
5. **`exit_play_mode`** — expect **`play_mode_state`:** **`edit_mode`**.

Record **`command_id`** values in chat or the **project spec** **Lessons learned** when filing evidence. For **Game view** visibility, add **`capture_screenshot`** with **`include_ui: true`** if the issue requires it, or rely on **`debug_context_bundle`** (always uses **Game view** **`ScreenCapture`** when **`include_screenshot`** is true). **Normative:** [`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md) (**Implementation and operations**), **`AGENTS.md`** item **6**.

## MCP tools

| Tool | Role |
|------|------|
| **`unity_bridge_command`** | Inserts **`agent_bridge_job`**, polls until **`completed`** / **`failed`** or **`timeout_ms`** (default **30000**, max **120000** — use **`40000`** initial for agent-led verification; on timeout follow **escalation protocol**) |
| **`unity_bridge_get`** | Read **`response`** by **`command_id`** (optional **`wait_ms`**) |
| **`unity_compile`** | Shortcut: same queue path as **`unity_bridge_command`** with **`kind`:** **`get_compilation_status`** |

## `kind` values

### `enter_play_mode`

**Editor** calls **`EditorApplication.EnterPlaymode()`** (after best-effort **Game view** focus). The job completes when **`GridManager.isInitialized`** is true (**~24 s** max wait on the Unity side). **`response`:** **`ok`**, **`ready`**, **`play_mode_state`** (`play_mode_ready` on success), **`grid_width`** / **`grid_height`** when **`has_grid_dimensions`** is true, **`already_playing`** when **Play Mode** was already active with an initialized **grid**. Uses **`SessionState`** so the wait survives **domain reload**.

### `exit_play_mode`

Exits **Play Mode**; completes when the **Editor** is back in **Edit Mode**. **`already_stopped`** if **Play Mode** was not active.

### `get_play_mode_status`

Immediate completion (no **Play Mode** transition). **`play_mode_state`:** **`edit_mode`**, **`play_mode_loading`**, or **`play_mode_ready`**; optional **`grid_width`** / **`grid_height`** when the **grid** is initialized.

### `get_compilation_status` / `unity_compile`

Synchronous compile snapshot (**Edit Mode**). **`response.compilation_status`:** **`compiling`**, **`compilation_failed`** (**`EditorUtility.scriptCompilationFailed`**), **`last_error_excerpt`** (truncated), **`recent_error_messages`** (buffered **Console** **`error`** lines). Use after **C#** edits when the **Editor** is open. For **batchmode** compile-only smoke without an **Editor** lock on the project, run root **`npm run unity:compile-check`** (loads **`.env`** / **`.env.local`**; **do not** skip because **`$UNITY_EDITOR_PATH`** is unset in the agent shell).

### `export_agent_context`

**Reports → Export Agent Context** payload; optional **`seed_cell`** `"x,y"` for Moore neighborhood center.

### `get_console_logs`

Buffered **Unity Console** lines (**`response.log_lines`**). Optional: **`since_utc`**, **`severity_filter`** (`all` \| `log` \| `warning` \| `error`), **`tag_filter`**, **`max_lines`** (1–2000).

### `capture_screenshot`

Writes **`tools/reports/bridge-screenshots/*.png`** (gitignored). Optional: **`filename_stem`**, **`camera`** (GameObject name — synchronous camera render).

- **`include_ui: false` (default):** Renders via **Camera** (world + **Screen Space - Camera** UI on that camera); **not** **Screen Space - Overlay**.
- **`include_ui: true`:** **`ScreenCapture`** of the **Game view** (includes **Overlay** HUD). **Ignores** **`camera`**. Keep the **Game** tab **visible**; if the file does not appear within **~15 s** (Unity-side wall clock), the job completes with **`ok: false`** and an English error.

### `debug_context_bundle`

**Play Mode** + initialized **`GridManager`** only. **Required:** **`seed_cell`** (`"x,y"`). **Optional:** **`include_screenshot`**, **`include_console`**, **`include_anomaly_scan`** (defaults **true**); **`filename_stem`**, and the same console filters as **`get_console_logs`** (**`max_lines`**, **`since_utc`**, etc.). **Response:** top-level **`ok`** reflects cell export and (when included) screenshot success; structured sub-results under **`bundle`** (**`cell_export`**, **`screenshot`**, **`console`**, **`anomalies`**, **`anomaly_count`**, **`anomaly_scan_skipped`**). Screenshot uses **Game view** **`ScreenCapture`** (like **`capture_screenshot`** with **`include_ui: true`**); deferred completion reuses the same **`EditorApplication.update`** pump as **`capture_screenshot`** — do not expect an instant PNG path in the dequeue response until the job row shows **`completed`**.

## Operational limits

- **`timeout_ms`:** **30000** ms default; **120000** ms maximum (`UNITY_BRIDGE_TIMEOUT_MS_MAX`). Use **40000** for the initial agent-led call; on timeout, follow the **escalation protocol** in [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md).
- **Stuck `processing`:** Rare; if the Editor lost domain reload mid-defer, fail the row with **`agent-bridge-complete.mjs --failed`** (see **postgres-ia-dev-setup** **Agent bridge job queue**).

## CLI equivalent (no Cursor MCP)

From **repository root**, after `cd tools/mcp-ia-server` and with deps installed, you can invoke the same enqueue/poll logic as **`runUnityBridgeCommand`** (see **`src/tools/unity-bridge-command.ts`**). One-shot **export_agent_context** only: root **`npm run db:bridge-agent-context`** (**`BRIDGE_TIMEOUT_MS`**, default **30000**).

Example **`tsx -e`** pattern (logs — adjust **`kind`** / args):

```bash
cd tools/mcp-ia-server && npx tsx -e "
import { runUnityBridgeCommand } from './src/tools/unity-bridge-command.ts';
(async () => {
  const r = await runUnityBridgeCommand({
    kind: 'get_console_logs',
    max_lines: 100,
    severity_filter: 'all',
  });
  console.log(JSON.stringify(r, null, 2));
})();
"
```

## Seed prompt (parameterize)

```markdown
Use **ide-bridge-evidence** (`.cursor/skills/ide-bridge-evidence/SKILL.md`): with **Postgres** configured, call **territory-ia** **`unity_bridge_command`** for {KIND: enter_play_mode | exit_play_mode | get_play_mode_status | export_agent_context | get_console_logs | capture_screenshot | debug_context_bundle} with parameters {PARAMS}. For **`capture_screenshot`**, **`export_agent_context`**, or **`debug_context_bundle`** in **Play Mode**, prefer **`enter_play_mode`** first unless Unity is already playing. Attach **`artifact_paths`**, **`bundle`**, or summarize **`log_lines`** / **`play_mode_state`** in chat for {ISSUE_ID} acceptance.
```
