### Stage 2.1 — Localhost HTTP bridge

**Status:** Draft

**Notes:** tasks _pending_ — not yet filed

**Backlog state (Stage 2.1):** 0 filed

**Objectives:** Add **localhost** **`HttpListener`** transport (same JSON envelope as **`agent_bridge_job`** commands) for sub-second round-trips. Implement on loopback only; marshal command execution to Unity main thread; share dispatch with existing job runner. **Editor-only assembly** (`Assets/Scripts/Editor/`); no runtime / player-build code path (analysis §4 line 52). Consistent with `docs/db-boundaries.md` — Unity runtime never talks to Postgres directly; bridge stays Editor-dev surface.

**Exit criteria:**

- **`POST`** **`localhost:{port}/...`** accepts bridge JSON; **`AgentBridgeCommandRunner`** (or sibling static class) executes on main thread via **`EditorApplication.update`** queue (**§10-C** risk: marshaling).
- Default port **7780** (configurable **`EditorPrefs`**) with conflict detection.
- Same command envelope as **`unity_bridge_command`** **`request`** jsonb.
- Automated or scripted smoke: **`curl`** POST → **`completed`** response when Editor idle.
- **Transport policy (locked):** MCP **`unity_bridge_command`** + **`unity_export_*`** sugar tools stay on the **`agent_bridge_job`** Postgres queue (multi-process durability, MCP stdio). HTTP transport = agent-machine escape hatch for sub-second interactive loops; never a replacement for the DB queue.

**Art:** None.

**Relevant surfaces (load when stage opens):**

- `docs/unity-ide-agent-bridge-analysis.md` — §4.5 HTTP upgrade + **Design Expansion** **Phase B**
- `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` — shared dispatch extraction target
- `Assets/Scripts/Editor/AgentBridgeConsoleBuffer.cs` (exists)
- `tools/mcp-ia-server/` — optional HTTP client tool or documented **`curl`** recipe

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T2.1.1 | HttpListener Editor class | _pending_ | _pending_ | New Editor static (e.g. **`AgentBridgeHttpHost`**) placed under **`Assets/Scripts/Editor/`** only; never referenced from runtime assemblies. Registers **`localhost`** prefix only; rejects non-loopback; start/stop tied to Editor play mode preference (documented). |
| T2.1.2 | Main-thread command queue | _pending_ | _pending_ | Queue **`BridgeCommand`** payloads from listener thread; drain on **`EditorApplication.update`** (same pump pattern as screenshot deferral). |
| T2.1.3 | Shared dispatch extraction | _pending_ | _pending_ | Refactor **`AgentBridgeCommandRunner`** so dequeue + HTTP paths call single **`ExecuteBridgeCommand`** internal API — no duplicate switch bodies. |
| T2.1.4 | HTTP integration smoke | _pending_ | _pending_ | Repo script under **`tools/scripts/`** or MCP test: POST sample **`get_play_mode_status`** → JSON **`completed`**; document **`curl`** in **`docs/mcp-ia-server.md`**. |
| T2.1.5 | EditorPrefs port + enable flag | _pending_ | _pending_ | **`EditorPrefs`** keys for port, enable HTTP; log clear error on **`HttpListenerException`** (address in use). |
| T2.1.6 | Security note in docs | _pending_ | _pending_ | Document localhost-only binding, no secrets in payloads, **`DATABASE_URL`** stays env — analysis §4.1. |

#### §Stage File Plan

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

_pending — populated by `/audit` when all Tasks reach Done post-verify (pre-closeout)._

#### §Stage Closeout Plan

_pending — populated by `/closeout ia/projects/unity-agent-bridge-master-plan.md Stage 2.1` when all Tasks reach `Done`._

---
