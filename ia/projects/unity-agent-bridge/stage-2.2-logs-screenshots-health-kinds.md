### Stage 2.2 — Logs, screenshots, health kinds

**Status:** Draft

**Notes:** tasks _pending_ — not yet filed

**Backlog state (Stage 2.2):** 0 filed

**Objectives:** Close gaps for **§10-C** observability: log forwarding, screenshot / health automation **`kind`** values, MCP docs + tests.

**Exit criteria:**

- Forwarding path from **`logMessageReceived`** to bridge responses (or ring buffer merge) specified and shipped.
- Screenshot / health **`kind`** behavior matches **`unity-development-context`** §10 table; **`docs/mcp-ia-server.md`** updated.
- **`npm run validate:all`** green.
- **Scope boundary:** Editor Play-mode diagnostics only; not production telemetry, not shipped to players. Shipped-game observability stays on the distribution plan's `BuildInfo` + `/download/latest.json` surfaces (`docs/distribution-exploration.md`).

**Art:** None.

**Relevant surfaces (load when stage opens):**

- `docs/unity-ide-agent-bridge-analysis.md` — **Design Expansion** **Phase B**
- `Assets/Scripts/Editor/AgentBridgeConsoleBuffer.cs` (exists)
- `Assets/Scripts/Editor/AgentBridgeScreenshotCapture.cs` (exists)
- `ia/specs/unity-development-context.md` §10 — artifact table

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T2.2.1 | Log forwarding handler | _pending_ | _pending_ | Wire **`Application.logMessageReceived`** (Editor play) to **`AgentBridgeConsoleBuffer`** or parallel buffer; define ordering with existing dequeue (**invariant #3** — no per-frame heavy work). |
| T2.2.2 | Response merge rules | _pending_ | _pending_ | When **`kind`** requests logs in HTTP or job response, specify merge with **`since_utc`** / filters; document limits (max lines). |
| T2.2.3 | Screenshot / health kinds | _pending_ | _pending_ | Align **`capture_screenshot`** + health-check export **`kind`** with **`AgentBridgeScreenshotCapture`** deferred pump; update §10 artifact table rows. |
| T2.2.4 | Anomaly scanner hook | _pending_ | _pending_ | If **`AgentBridgeAnomalyScanner`** exposes new entry for health **`kind`**, wire without duplicating grid reads (**invariant #5**). |
| T2.2.5 | MCP + docs parity | _pending_ | _pending_ | Update tool descriptors + **`docs/mcp-ia-server.md`** for any new **`kind`** / HTTP discovery; link **IDE bridge evidence** skill. |
| T2.2.6 | Manual verify checklist | _pending_ | _pending_ | Short **`docs/`** or **`ia/skills`** pointer: steps for human to validate logs + screenshot in Play Mode (agent-led verification policy alignment). |

#### §Stage File Plan

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

_pending — populated by `/audit` when all Tasks reach Done post-verify (pre-closeout)._

#### §Stage Closeout Plan

_pending — populated by `/closeout ia/projects/unity-agent-bridge-master-plan.md Stage 2.2` when all Tasks reach `Done`._

---
