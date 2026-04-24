### Stage 4.1 — Bridge composite implementation

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** **`wire_asset_from_catalog`** executes deterministic steps; respects **scene contract** paths from spec appendix (once landed, temporary constants OK in Stage 4.1 with TODO removed in 4.3).

**Exit:**

- Edit Mode run creates toolbar button wired to existing **`UIManager`** entry stub.
- Logs each sub-step for agent observability.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T4.1.1 | Kind enum + DTO fields | _pending_ | _pending_ | Extend mutation DTO with **`wire_asset_from_catalog`** payload: `assetId`, `dryRun`, `parentPath`, `uiThemeRef` strategy. |
| T4.1.2 | Dispatch switch case | _pending_ | _pending_ | Route in `AgentBridgeCommandRunner.Mutations.cs` per **bridge tooling patterns** (`unity-invariants` doc). |
| T4.1.3 | Resolve catalog row | _pending_ | _pending_ | Editor-only read of snapshot or DB bridge — choose one deterministic source for Edit Mode (document). |
| T4.1.4 | Instantiate + parent + bind | _pending_ | _pending_ | Reuse `instantiate_prefab`, `set_gameobject_parent`, `assign_serialized_field` primitives internally. |
| T4.1.5 | onClick wire + save_scene | _pending_ | _pending_ | Hook to existing inspector-exposed handler; call **`save_scene`**; return structured success object. |

#### §Stage File Plan

_pending — populated by `/stage-file ia/projects/grid-asset-visual-registry-master-plan.md Stage 4.1` planner pass._

#### §Plan Fix

_pending — populated by `/plan-review ia/projects/grid-asset-visual-registry-master-plan.md Stage 4.1` when fixes are needed._

#### §Stage Audit

_pending — populated by `/audit ia/projects/grid-asset-visual-registry-master-plan.md Stage 4.1` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

_pending — populated by `/closeout ia/projects/grid-asset-visual-registry-master-plan.md Stage 4.1` planner pass when all Tasks reach `Done`._
