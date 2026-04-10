---
description: Agent-led Unity/bridge verification — attempt batch + IDE bridge; report full Verification block
alwaysApply: true
---

# Agent-led verification (Territory Developer)

**IA context:** [`docs/information-architecture-overview.md`](../../docs/information-architecture-overview.md) (stack overview). **Full policy:** [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md).

## Directives

1. **Unity during agent work** is a **test** surface (not production). Prefer **proving** integration (**glossary** **Agent test mode batch**, **IDE agent bridge**) over skipping for convenience.
2. **Bridge timeout:** use **`unity_bridge_command`** **`timeout_ms`:** **`40000`** (40 s initial) for agent-led verification. On timeout, follow the **timeout escalation protocol** in [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md) (`npm run unity:ensure-editor` → retry 60 s). Ceiling: **120 s** (`UNITY_BRIDGE_TIMEOUT_MS_MAX`).
3. **Editor launch:** If the Unity Editor is not running, run **`npm run unity:ensure-editor`** (macOS; exit 0 = ready) before concluding "human needed".
4. **Path A — project lock:** **`unity:testmode-batch`** fails if the **Editor** already has **`REPO_ROOT`** open. Before Path A, release the lock: prefer **`npm run unity:testmode-batch -- --quit-editor-first …`** (see [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md)). When running **Path A** and **Path B** in one session, run **Path A** first (with **`--quit-editor-first`** when needed), then **`unity:ensure-editor`** before **Path B**.
5. **Completion messages — Verification block** must include, when applicable:
   - **`npm run validate:all`** — exit code.
   - **`npm run unity:compile-check`** — exit code if **`Assets/`** **C#** changed; else **N/A** + reason.
   - **Path A:** **`npm run unity:testmode-batch`** (note if **`--quit-editor-first`** was used) — exit code + newest **`agent-testmode-batch-*.json`** summary (**`ok`**, **`exit_code`**).
   - **Path B:** **`db:bridge-preflight`** then bridge attempt(s) with **`timeout_ms` 40000** initial (escalation protocol on timeout) — **`ok`/`error`/`timeout`** (+ **`command_id`** if any). If not run, state why.
6. **Skills:** [`.cursor/skills/agent-test-mode-verify/SKILL.md`](../skills/agent-test-mode-verify/SKILL.md), [`.cursor/skills/ide-bridge-evidence/SKILL.md`](../skills/ide-bridge-evidence/SKILL.md).
