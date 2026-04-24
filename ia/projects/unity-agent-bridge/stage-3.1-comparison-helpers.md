### Stage 3.1 — Streaming / comparison helpers

**Status:** Draft

**Notes:** tasks _pending_ — not yet filed

**Backlog state (Stage 3.1):** 0 filed

**Objectives:** Add structured before/after export comparison (MCP or Editor). Optional chunked / streaming responses for large payloads **without** breaking **`agent_bridge_job`** contract. Deliver **optional** **§10-D** items: richer streaming / comparison helpers gated to avoid scope creep. Prefer bucketing heavy deferrals to **`docs/unity-agent-bridge-post-mvp-extensions.md`**.

**Exit criteria:**

- Comparison helper (**`unity_validate_fix`**-class) either shipped as thin MCP wrapper or explicitly deferred with extensions-doc pointer.
- If streaming shipped: documented size limits + fallback to disk artifact paths.
- Comparison tool OR documented deferral + extensions appendix entry.

**Art:** None.

**Relevant surfaces (load when stage opens):**

- `docs/unity-ide-agent-bridge-analysis.md` — **Design Expansion** **Phase C**, **Deferred / out of scope**
- `docs/unity-agent-bridge-post-mvp-extensions.md` **(recommended, not required)** — bucket for deferrals
- `tools/mcp-ia-server/` — candidate wrapper surface

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T3.1.1 | Comparison DTO + diff algorithm | _pending_ | _pending_ | Define stable JSON diff for two **`editor_export_*`** **`document`** bodies or file paths (sorting debug + cell chunk) — ignore noisy fields (`exported_at_utc`). |
| T3.1.2 | unity_validate_fix wrapper | _pending_ | _pending_ | Thin MCP tool: enqueue two exports (before/after) or accept paths; return structured diff summary for agents. |
| T3.1.3 | Streaming spike | _pending_ | _pending_ | Evaluate chunked HTTP or job **`response`** fields for large artifacts; default stays disk path + **`document jsonb`**. |
| T3.1.4 | Extensions doc deferral row | _pending_ | _pending_ | If streaming not shipped, append deferral paragraph to **`docs/unity-agent-bridge-post-mvp-extensions.md`** (create file only if Stage 3 proceeds and file missing — coordinate with user). |

#### §Stage File Plan

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

_pending — populated by `/audit` when all Tasks reach Done post-verify (pre-closeout)._

#### §Stage Closeout Plan

_pending — populated by `/closeout ia/projects/unity-agent-bridge-master-plan.md Stage 3.1` when all Tasks reach `Done`._

---
