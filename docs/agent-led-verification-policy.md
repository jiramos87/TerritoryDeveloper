# Agent-led verification policy

> **IA placement:** This document is part of the **Information Architecture** stack. Overview and layer diagram: [`docs/information-architecture-overview.md`](information-architecture-overview.md). Day-to-day workflow: [`AGENTS.md`](../AGENTS.md). **Cursor rule (always-on):** [`.cursor/rules/agent-verification-directives.mdc`](../.cursor/rules/agent-verification-directives.mdc).

## Purpose

During **implementation** and **fix** work with Cursor agents on this repository, the **Unity Editor**, **Play Mode**, and **save-shaped scenarios** are **test surfaces** — not production. The **owner** treats agent-driven runs as disposable: closing or restarting the Editor to unblock **Path A** (**Agent test mode batch**) or **Path B** (**IDE agent bridge**) is acceptable when pursuing a closed **implement → verify** loop. **Human normal-game QA** remains the final gate per [`AGENTS.md`](../AGENTS.md).

## Integration expectations

- **Prefer proving over assuming:** When **Postgres** and **Unity** are available, agents should **attempt** **glossary** **IDE agent bridge** commands after **`npm run db:bridge-preflight`**, not decline by default because of wall-clock cost.
- **Environment:** Use full agent capabilities (terminal, MCP, network) as allowed by the host. If a sandbox blocks a step, **retry** with broader permissions or report the **host** error — do not treat “might be slow” as a skip reason.
- **Timeouts:** **`unity_bridge_command`** supports **`timeout_ms`** up to **120000** (120 seconds; `UNITY_BRIDGE_TIMEOUT_MS_MAX`). Use **`40000`** for the **initial** agent-led call. On timeout, follow the **timeout escalation protocol** below. Waiting for **Unity** or **Play Mode** is normal (same idea as **E2E** tests).
- **Editor launch:** If the Unity Editor is not running, agents should run **`npm run unity:ensure-editor`** (macOS; exit 0 = ready, exit 2 = not macOS, exit 3 = binary not found) **before** concluding that the human must open Unity. The script launches the Editor on `REPO_ROOT` and waits up to 90 s for the lockfile.
- **Path A — project lock:** **`npm run unity:testmode-batch`** starts a **second** Unity process. If the **Unity Editor** already has **`REPO_ROOT`** open, batchmode aborts (*"another Unity instance is running"*, often exit **134**). **Before Path A**, agents **must** release the lock: preferred one-liner **`npm run unity:testmode-batch -- --quit-editor-first --scenario-id reference-flat-32x32`** (runs **`tools/scripts/unity-quit-project.sh`** first), or quit the Editor manually / run **`tools/scripts/unity-quit-project.sh`** then invoke batch without **`--quit-editor-first`**. **Both Path A and Path B in one session:** run **Path A** first (with **`--quit-editor-first`** when the Editor might be open), then **`npm run unity:ensure-editor`** (macOS) so **Path B** has an Editor on **`REPO_ROOT`** again.

## Verification block (required in agent completion messages)

When reporting **Verification** after substantive implementation (especially when **§7b** / **Load pipeline** / **test mode** applies), include **all** of the following that were run:

| Check | Report |
|-------|--------|
| **Node / IA** | `npm run validate:all` — exit code (and note if skipped with reason). |
| **Unity compile** | `npm run unity:compile-check` when **`Assets/`** **C#** changed — exit code; or **N/A** with reason. |
| **Path A — Agent test mode batch** | `npm run unity:testmode-batch` — exit code; path to newest **`tools/reports/agent-testmode-batch-*.json`** and **`ok` / `exit_code`**. Use **`--quit-editor-first`** when an Editor might hold **`REPO_ROOT`** (see **Path A — project lock** above). Example: **`npm run unity:testmode-batch -- --quit-editor-first --scenario-id reference-flat-32x32`**. |
| **Path B — IDE agent bridge** | After **`db:bridge-preflight`**: at least **`get_play_mode_status`** or full **`enter_play_mode`** → **`debug_context_bundle`** (optional) → **`exit_play_mode`** with **`timeout_ms`:** **`40000`** (initial; follow **timeout escalation protocol** on timeout) — **`ok`**, **`error`**, or **`timeout`** plus **`command_id`** if present. |

If **Path B** was not run, state **why** (e.g. no Editor, preflight non-zero) — do not omit the row.

**Skills:** [`.cursor/skills/agent-test-mode-verify/SKILL.md`](../.cursor/skills/agent-test-mode-verify/SKILL.md), [`.cursor/skills/ide-bridge-evidence/SKILL.md`](../.cursor/skills/ide-bridge-evidence/SKILL.md), [`.cursor/skills/close-dev-loop/SKILL.md`](../.cursor/skills/close-dev-loop/SKILL.md).

## Timeout escalation protocol

When a **`unity_bridge_command`** call returns **`timeout`**, follow this ordered recovery before concluding "human needed":

1. **First call** — use **`timeout_ms`:** **`40000`** (40 s, the recommended agent-led default).
2. **On timeout** — run **`npm run unity:ensure-editor`** (exit 0 = Editor running or just launched; exit 2 = not macOS; exit 3 = Unity binary not found). On exit 0, proceed to step 3. On non-zero, report the exit code and escalate to the human.
3. **Retry** — repeat the bridge command with **`timeout_ms`:** **`60000`** (60 s). This accommodates Editor startup + domain reload + `AgentBridgeCommandRunner` initialization.
4. **On second timeout** — run **`npm run db:bridge-preflight`** and check Console logs (**`get_console_logs`** if the Editor responds). Report findings and escalate to the human.

The ceiling is **120 s** (`UNITY_BRIDGE_TIMEOUT_MS_MAX`); the escalation protocol intentionally stops at **60 s** to avoid silent long waits. Do not retry more than once.

## territory-ia MCP and **`timeout_ms`**

The **`unity_bridge_command`** / **`unity_compile`** **120 s** ceiling is enforced in [`tools/mcp-ia-server/src/tools/unity-bridge-command.ts`](../tools/mcp-ia-server/src/tools/unity-bridge-command.ts) (`UNITY_BRIDGE_TIMEOUT_MS_MAX`). After pulling a change that adjusts this cap, **restart the territory-ia MCP server** (or reload the Cursor window) so the host picks up the new tool schema — otherwise the client may still validate **`timeout_ms`** against the old maximum.

## Cursor Memory (optional)

Paste the following into **Cursor → Memory** if you want the same policy across projects or sessions without opening this repo:

- Territory Developer: During agent implementation, Unity is a **test** environment; **attempt** **Agent test mode batch** and **IDE agent bridge** verification; for **Path A**, release the **project lock** first (**`npm run unity:testmode-batch -- --quit-editor-first …`** or quit Editor), then **`unity:ensure-editor`** before **Path B** if needed; use **`timeout_ms` 40000** initial for bridge commands, follow **timeout escalation protocol** on timeout (`npm run unity:ensure-editor` → retry 60 s); report **Verification** with **validate:all**, **compile-check** if C# changed, **batch JSON result**, and **bridge** outcome. **IA** overview: `docs/information-architecture-overview.md`; policy: `docs/agent-led-verification-policy.md`.
