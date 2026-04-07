---
name: ide-bridge-evidence
description: >
  Use when you need Unity Play Mode evidence (Console logs or Game view screenshots) via territory-ia
  unity_bridge_command for issue acceptance or debugging. Requires Postgres agent_bridge_job (migration 0008),
  DATABASE_URL, and Unity Editor on REPO_ROOT with AgentBridgeCommandRunner. Triggers: "bridge screenshot",
  "get unity logs from MCP", "capture_screenshot include_ui", "IDE agent bridge evidence".
---

# IDE agent bridge — Play Mode evidence (logs + screenshots)

This skill documents **optional**, **dev-machine-only** use of **territory-ia** **`unity_bridge_command`** / **`unity_bridge_get`** (glossary **IDE agent bridge**). It **does not** replace **CI** or **`npm run validate:all`** — there is no Unity in the **IA tools** workflow job.

**Related:** **[`project-spec-kickoff`](../project-spec-kickoff/SKILL.md)** (write **§7b** rows that reference these tools). **[`project-spec-implement`](../project-spec-implement/SKILL.md)** (optional verification after phases). **[`project-implementation-validation`](../project-implementation-validation/SKILL.md)** (Node checks; bridge is a separate optional subsection). **Normative IA:** [`docs/mcp-ia-server.md`](../../../docs/mcp-ia-server.md), **unity-development-context** §10, [`docs/unity-ide-agent-bridge-analysis.md`](../../../docs/unity-ide-agent-bridge-analysis.md).

## Prerequisites (all required)

| Requirement | Notes |
|-------------|--------|
| **`DATABASE_URL`** or **`config/postgres-dev.json`** | Same policy as **Editor export registry** |
| Migration **`0008_agent_bridge_job.sql`** applied | `npm run db:migrate` — see [`docs/postgres-ia-dev-setup.md`](../../../docs/postgres-ia-dev-setup.md) |
| **Unity Editor** open on **repository root** | **`AgentBridgeCommandRunner`** polls **`agent-bridge-dequeue.mjs`** |
| **Play Mode** for **`capture_screenshot`** | **Edit Mode** returns a **completed** job with **`ok: false`** and an English **`error`** |

## MCP tools

| Tool | Role |
|------|------|
| **`unity_bridge_command`** | Inserts **`agent_bridge_job`**, polls until **`completed`** / **`failed`** or **`timeout_ms`** (default **30000**, max **30000**) |
| **`unity_bridge_get`** | Read **`response`** by **`command_id`** (optional **`wait_ms`**) |

## `kind` values

### `get_console_logs`

Buffered **Unity Console** lines (**`response.log_lines`**). Optional: **`since_utc`**, **`severity_filter`** (`all` \| `log` \| `warning` \| `error`), **`tag_filter`**, **`max_lines`** (1–2000).

### `capture_screenshot`

Writes **`tools/reports/bridge-screenshots/*.png`** (gitignored). Optional: **`filename_stem`**, **`camera`** (GameObject name — synchronous camera render).

- **`include_ui: false` (default):** Renders via **Camera** (world + **Screen Space - Camera** UI on that camera); **not** **Screen Space - Overlay**.
- **`include_ui: true`:** **`ScreenCapture`** of the **Game view** (includes **Overlay** HUD). **Ignores** **`camera`**. Keep the **Game** tab **visible**; if the file does not appear within **~15 s** (Unity-side wall clock), the job completes with **`ok: false`** and an English error.

## Operational limits

- **`timeout_ms`:** **30000** ms default and **maximum** on the MCP tool — do not rely on longer waits.
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
Use **ide-bridge-evidence** (`.cursor/skills/ide-bridge-evidence/SKILL.md`): with Unity in **Play Mode** and **Postgres** configured, call **territory-ia** **`unity_bridge_command`** for {KIND: get_console_logs | capture_screenshot} with parameters {PARAMS}. Attach **`artifact_paths`** or summarize **`log_lines`** in chat for {ISSUE_ID} acceptance.
```
