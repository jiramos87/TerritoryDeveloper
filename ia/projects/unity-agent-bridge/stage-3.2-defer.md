### Stage 3.2 — Deterministic replay + visual diff (spike / defer)

**Status:** Draft

**Notes:** tasks _pending_ — not yet filed

**Backlog state (Stage 3.2):** 0 filed

**Objectives:** Time-box spike for seed + action log capture + visual diff automation; explicit gate whether to fold into backlog or extensions-only.

**Exit criteria:**

- Spike doc section: **feasible / not feasible** + rough effort.
- No **headless CI** language introduced — analysis §4.1 guardrail.

**Art:** None.

**Relevant surfaces (load when stage opens):**

- `docs/unity-ide-agent-bridge-analysis.md` — **Design Expansion** **Phase C**, **Deferred / out of scope**
- `docs/unity-agent-bridge-post-mvp-extensions.md` **(recommended, not required)**
- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — seed source
- `tools/reports/` — gitignored artifact target

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T3.2.1 | Replay capture scope | _pending_ | _pending_ | Identify minimal hooks: **`GameSaveManager`** seed + input queue vs full action log — document data written to **`tools/reports/`** gitignored paths. **Boundary (locked):** replay artifacts write to Editor filesystem (`tools/reports/`) only — never Postgres, never shipped player. Shipped-game save format stays filesystem-only per `docs/distribution-exploration.md` + `docs/db-boundaries.md`. |
| T3.2.2 | Replay spike prototype | _pending_ | _pending_ | Optional throwaway Editor script: load fixture + N ticks — **not** CI — prove deterministic snapshot equality for one scenario. |
| T3.2.3 | Visual diff automation assessment | _pending_ | _pending_ | Compare **`ScreenCapture`** pairs + structural diff from Stage 3.1; decide ship vs **`post-mvp`** bucket. |
| T3.2.4 | Gate decision + extensions pointer | _pending_ | _pending_ | Write **Decision** paragraph in exploration doc OR extensions doc; if defer-only, no production code requirement. |

#### §Stage File Plan

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

_pending — populated by `/audit` when all Tasks reach Done post-verify (pre-closeout)._

#### §Stage Closeout Plan

_pending — populated by `/closeout ia/projects/unity-agent-bridge-master-plan.md Stage 3.2` when all Tasks reach `Done`._

---
