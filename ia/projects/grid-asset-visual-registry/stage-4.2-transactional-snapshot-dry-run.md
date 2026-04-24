### Stage 4.2 — Transactional snapshot + dry-run

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** **Snapshot scene** before composite; **rollback** restores prior state on any failure; **`dry_run`** prints plan only.

**Exit:**

- Failed run leaves scene unchanged (Edit Mode test).
- Telemetry fields include **`recipe_id`** + **`caller_agent`** passthrough if available.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T4.2.1 | Pre-snapshot hook | _pending_ | _pending_ | Serialize relevant `GameObject` hierarchy or use Unity `Undo` stack if compatible with bridge — pick one pattern and document limits. |
| T4.2.2 | Rollback on failure | _pending_ | _pending_ | Ensure exceptions in any sub-step trigger restore; return `partial` metadata for agents. |
| T4.2.3 | dry_run plan JSON | _pending_ | _pending_ | No prefab instance persists; output lists intended creates + property sets. |
| T4.2.4 | EditMode bridge tests | _pending_ | _pending_ | If repo has Editor test asmdef, cover success + rollback; else document **`verify-loop`** manual path. |

#### §Stage File Plan

_pending — populated by `/stage-file ia/projects/grid-asset-visual-registry-master-plan.md Stage 4.2` planner pass._

#### §Plan Fix

_pending — populated by `/plan-review ia/projects/grid-asset-visual-registry-master-plan.md Stage 4.2` when fixes are needed._

#### §Stage Audit

_pending — populated by `/audit ia/projects/grid-asset-visual-registry-master-plan.md Stage 4.2` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

_pending — populated by `/closeout ia/projects/grid-asset-visual-registry-master-plan.md Stage 4.2` planner pass when all Tasks reach `Done`._
