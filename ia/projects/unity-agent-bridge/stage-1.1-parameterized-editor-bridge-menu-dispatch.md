### Stage 1.1 — Parameterized Editor bridge + menu dispatch

**Status:** Final

**Notes:** archived 2026-04-24 — 6 Tasks Done (TECH-559..564)

**Backlog state (Stage 1.1):** 6 filed → archived

**Objectives:** Extend **`AgentBridgeCommandRunner`** + menu exports so MCP **`request.params`** drives bounded export parameters without duplicating export bodies. Harden **`failed`** status when Play Mode / grid preconditions fail (**invariant #5** on any new cell reads).

**Exit criteria:**

- **`BridgeCommand`** / DTO path parses **`params`** for **`export_cell_chunk`**, **`export_sorting_debug`**, **`export_agent_context`** (and documents defaults when omitted).
- **`AgentDiagnosticsReportsMenu`** / **`InterchangeJsonReportsMenu`** expose parameterized static entry points (or thin wrappers) callable from runner — existing menu items remain human baseline.
- Integration smoke: enqueue job → **`unity_bridge_get`** returns **`completed`** or **`failed`** with stable error string for uninitialized grid.

**Art:** None.

**Relevant surfaces (load when stage opens):**

- `docs/unity-ide-agent-bridge-analysis.md` — **Design Expansion** Implementation Points **Phase A**
- `ia/specs/unity-development-context.md` §10 (lines ~141–185) — Reports + bridge artifacts
- `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` (exists) — dispatch extension
- `Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs` (exists)
- `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs` (exists) — sorting + agent context
- `Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs` (exists) — cell chunk + world snapshot
- `tools/mcp-ia-server/` (exists) — `registerTool` + tests

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T1.1.1 | Runner params DTO + dispatch | **TECH-559** | Done (archived) | Extend **`AgentBridgeCommandRunner`** (and shared DTOs) to deserialize **`params`** for **`export_cell_chunk`** (**`origin_*`**, **`width`**, **`height`**), **`export_sorting_debug`** / **`export_agent_context`** optional **`seed_cell`**; route to menu statics without new **`gridArray`** reads (**invariant #5**). |
| T1.1.2 | MCP Zod alignment for new params | **TECH-560** | Done (archived) | Update **`tools/mcp-ia-server`** **`unity_bridge_command`** / job **`request`** Zod so enqueued rows match Unity DTOs; add fixture or unit test for param round-trip. |
| T1.1.3 | Menu parameterized entry points | **TECH-561** | Done (archived) | Refactor **`AgentDiagnosticsReportsMenu`** + **`InterchangeJsonReportsMenu`** so bridge calls **`Export*`** methods with explicit parameter structs; preserve existing **`MenuItem`** behavior via defaults. |
| T1.1.4 | Menu regression pass | **TECH-562** | Done (archived) | Manual or automated check: **Territory Developer → Reports** still runs for all §10 items; no duplicate file writes; **`TryPersistReport`** paths unchanged for registry exports. |
| T1.1.5 | Play Mode + grid gate errors | **TECH-563** | Done (archived) | Before Play-only exports, verify **`GridManager.isInitialized`** (and documented **`TerrainManager`** needs); return **`failed`** + human-readable **`error`** field; align with analysis §8.3 risk table. |
| T1.1.6 | Bridge response contract tests | **TECH-564** | Done (archived) | Add EditMode or MCP-side tests asserting **`completed`** / **`failed`** shapes for **`export_cell_chunk`** + sorting debug when grid absent — snapshot keys only, not full JSON bodies. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "Runner params DTO + dispatch"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Extend AgentBridgeCommandRunner + shared DTOs to deserialize params for
    export_cell_chunk (origin_x, origin_y, width, height), export_sorting_debug /
    export_agent_context optional seed_cell. Route to menu statics without new
    gridArray reads (invariant #5). Touches Assets/Scripts/Editor/AgentBridgeCommandRunner.cs,
    AgentBridgeCommandRunner.Mutations.cs.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Extend AgentBridgeCommandRunner dispatch to deserialize request.params
      for export_cell_chunk, export_sorting_debug, export_agent_context kinds.
      New DTO structs carry bounded parameters per unity-development-context §10.
    goals: |
      1. BridgeCommand / DTO path parses params for export_cell_chunk (origin, width, height).
      2. export_sorting_debug + export_agent_context accept optional seed_cell param.
      3. Runner switch-dispatch routes parameterized requests to menu statics.
      4. No new gridArray / cellArray reads — invariant #5 respected.
    systems_map: |
      - Assets/Scripts/Editor/AgentBridgeCommandRunner.cs — dispatch extension
      - Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs — mutation helpers
      - ia/specs/unity-development-context.md §10 — Reports + bridge artifacts
    impl_plan_sketch: |
      - Define param structs (ExportCellChunkParams, ExportSortingDebugParams, ExportAgentContextParams)
      - Extend BridgeCommand deserialization to populate params from request JSON
      - Route parameterized requests through runner switch to menu statics

- reserved_id: ""
  title: "MCP Zod alignment for new params"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Update tools/mcp-ia-server unity_bridge_command / job request Zod schemas
    so enqueued rows match Unity DTO param shapes. Add fixture or unit test
    for param round-trip. Touches tools/mcp-ia-server/src/.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Align MCP unity_bridge_command Zod request schema with new Unity DTO
      param shapes (export_cell_chunk, export_sorting_debug, export_agent_context).
      Add fixture or unit test proving param round-trip fidelity.
    goals: |
      1. Zod request schema accepts params matching Unity DTO structs.
      2. Fixture or unit test validates param round-trip (enqueue → dequeue shape match).
      3. Existing non-parameterized kinds still pass Zod validation.
    systems_map: |
      - tools/mcp-ia-server/src/ — registerTool, Zod schemas
      - tools/mcp-ia-server/tests/ — test surface
      - ia/specs/unity-development-context.md §10 — artifact table
    impl_plan_sketch: |
      - Extend unity_bridge_command request Zod with optional params sub-object
      - Add fixture covering each new kind + param shape
      - Confirm existing non-parameterized kinds pass unchanged

- reserved_id: ""
  title: "Menu parameterized entry points"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Refactor AgentDiagnosticsReportsMenu + InterchangeJsonReportsMenu so bridge
    calls Export* methods with explicit parameter structs. Preserve existing
    MenuItem behavior via defaults. Touches Assets/Scripts/Editor/ menu classes.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Refactor AgentDiagnosticsReportsMenu and InterchangeJsonReportsMenu so
      bridge runner calls Export* methods with explicit parameter structs.
      Existing MenuItem paths remain as zero-param defaults.
    goals: |
      1. Export methods accept parameter structs (chunk bounds, seed_cell).
      2. MenuItem wrappers call same methods with default (null/empty) params.
      3. Bridge runner routes parameterized requests through these entry points.
    systems_map: |
      - Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs — sorting + agent context exports
      - Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs — cell chunk + world snapshot exports
      - ia/specs/unity-development-context.md §10 — artifact table
    impl_plan_sketch: |
      - Add parameter-accepting overloads to Export methods
      - Wire MenuItem wrappers to overloads with default params
      - Confirm bridge runner dispatch reaches parameterized path

- reserved_id: ""
  title: "Menu regression pass"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Manual or automated check: Territory Developer → Reports still runs for
    all §10 items. No duplicate file writes. TryPersistReport paths unchanged
    for registry exports. Verification-focused task.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Verify all Territory Developer → Reports menu items still work after
      parameterized refactor. No duplicate file writes. TryPersistReport
      paths unchanged for Postgres registry exports.
    goals: |
      1. Every MenuItem under Reports menu produces expected output.
      2. No duplicate file writes from bridge vs menu paths.
      3. TryPersistReport paths + Postgres registry export unchanged.
    systems_map: |
      - Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs — Reports menu
      - Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs — Reports menu
      - Assets/Scripts/Editor/EditorPostgresExportRegistrar.cs — registry
    impl_plan_sketch: |
      - Enumerate all Reports menu items; confirm each invocation succeeds
      - Check file output paths for duplicates
      - Verify Postgres registry export rows match pre-refactor baseline

- reserved_id: ""
  title: "Play Mode + grid gate errors"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Before Play-only exports, verify GridManager.isInitialized + TerrainManager
    readiness. Return failed + human-readable error field when preconditions
    unmet. Align with analysis §8.3 risk table. Touches AgentBridgeCommandRunner
    precondition path.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Add precondition gates in bridge export path: GridManager.isInitialized
      and TerrainManager readiness checks before Play-only exports. Return
      failed status + human-readable error string when preconditions not met.
    goals: |
      1. Play-only export kinds check GridManager.isInitialized before execution.
      2. TerrainManager readiness verified where documented needs exist.
      3. Failed response includes structured error field (human-readable string).
      4. No new gridArray / cellArray reads — invariant #5 respected.
    systems_map: |
      - Assets/Scripts/Editor/AgentBridgeCommandRunner.cs — precondition gate
      - Assets/Scripts/Managers/GameManagers/GridManager.cs — isInitialized check
      - docs/unity-ide-agent-bridge-analysis.md §8.3 — risk table
    impl_plan_sketch: |
      - Add isInitialized / TerrainManager readiness check before Play-only dispatch
      - Return failed + error JSON on precondition failure
      - Align error strings with analysis §8.3 risk categories

- reserved_id: ""
  title: "Bridge response contract tests"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Add EditMode or MCP-side tests asserting completed / failed shapes for
    export_cell_chunk + sorting debug when grid absent. Snapshot keys only,
    not full JSON bodies. Touches Assets/Tests/EditMode/ or tools/mcp-ia-server/tests/.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Add EditMode or MCP-side tests asserting completed / failed response
      shapes for export_cell_chunk + export_sorting_debug when grid absent.
      Snapshot keys only — not full JSON body comparison.
    goals: |
      1. Test asserts completed response shape has expected top-level keys.
      2. Test asserts failed response shape has error field when grid uninitialized.
      3. Key-only snapshots (no full body) for stability across content changes.
    systems_map: |
      - Assets/Tests/EditMode/ — Unity EditMode test surface
      - tools/mcp-ia-server/tests/ — MCP-side test surface
      - ia/specs/unity-development-context.md §10 — artifact table
    impl_plan_sketch: |
      - Add EditMode test: enqueue export_cell_chunk → assert completed keys
      - Add EditMode test: export when grid absent → assert failed + error key
      - Add MCP-side test if tooling coverage needed
```

#### §Plan Fix

> plan-review exit 0 — all 6 Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

_retroactive-skip — Stage 1.1 closed pre-canonical-§Stage-Audit (2026-04-24 structure refactor). No audit paragraphs persisted at close time. Tasks TECH-559..564 archive records + spec-journal entries remain source of truth._

#### §Stage Closeout Plan

<!-- mechanicalization_score: overall=fully_mechanical; unresolved_anchors=0; conditional_ops=0; free_text_ops=0; assessed=2026-04-24 -->

> stage-closeout-plan — 6 Tasks (0 shared migration ops + 24 per-Task ops = 24 tuples total). Applied 2026-04-24.

```yaml
# No shared migration ops — no new glossary rows, no rule edits, no doc paragraph edits cited by ≥2 Tasks.

# Per-Task ops — TECH-559 (T1.1.1)
- operation: archive_record
  target_path: ia/backlog-archive/TECH-559.yaml
  target_anchor: "id: \"TECH-559\""
  payload:
    completed: "2026-04-24"

- operation: delete_file
  target_path: ia/projects/TECH-559.md
  target_anchor: "file:TECH-559.md"
  payload: null

- operation: replace_section
  target_path: ia/projects/unity-agent-bridge-master-plan.md
  target_anchor: "task_key:T1.1.1"
  payload: |
    | T1.1.1 | Runner params DTO + dispatch | **TECH-559** | Done (archived) | Extend **`AgentBridgeCommandRunner`** (and shared DTOs) to deserialize **`params`** for **`export_cell_chunk`** (**`origin_*`**, **`width`**, **`height`**), **`export_sorting_debug`** / **`export_agent_context`** optional **`seed_cell`**; route to menu statics without new **`gridArray`** reads (**invariant #5**). |

- operation: digest_emit
  target_path: ia/backlog-archive/TECH-559.yaml
  target_anchor: "TECH-559"
  payload:
    tool: stage_closeout_digest
    mode: per_task

# Per-Task ops — TECH-560 (T1.1.2)
- operation: archive_record
  target_path: ia/backlog-archive/TECH-560.yaml
  target_anchor: "id: \"TECH-560\""
  payload:
    completed: "2026-04-24"

- operation: delete_file
  target_path: ia/projects/TECH-560.md
  target_anchor: "file:TECH-560.md"
  payload: null

- operation: replace_section
  target_path: ia/projects/unity-agent-bridge-master-plan.md
  target_anchor: "task_key:T1.1.2"
  payload: |
    | T1.1.2 | MCP Zod alignment for new params | **TECH-560** | Done (archived) | Update **`tools/mcp-ia-server`** **`unity_bridge_command`** / job **`request`** Zod so enqueued rows match Unity DTOs; add fixture or unit test for param round-trip. |

- operation: digest_emit
  target_path: ia/backlog-archive/TECH-560.yaml
  target_anchor: "TECH-560"
  payload:
    tool: stage_closeout_digest
    mode: per_task

# Per-Task ops — TECH-561 (T1.1.3)
- operation: archive_record
  target_path: ia/backlog-archive/TECH-561.yaml
  target_anchor: "id: \"TECH-561\""
  payload:
    completed: "2026-04-24"

- operation: delete_file
  target_path: ia/projects/TECH-561.md
  target_anchor: "file:TECH-561.md"
  payload: null

- operation: replace_section
  target_path: ia/projects/unity-agent-bridge-master-plan.md
  target_anchor: "task_key:T1.1.3"
  payload: |
    | T1.1.3 | Menu parameterized entry points | **TECH-561** | Done (archived) | Refactor **`AgentDiagnosticsReportsMenu`** + **`InterchangeJsonReportsMenu`** so bridge calls **`Export*`** methods with explicit parameter structs; preserve existing **`MenuItem`** behavior via defaults. |

- operation: digest_emit
  target_path: ia/backlog-archive/TECH-561.yaml
  target_anchor: "TECH-561"
  payload:
    tool: stage_closeout_digest
    mode: per_task

# Per-Task ops — TECH-562 (T1.1.4)
- operation: archive_record
  target_path: ia/backlog-archive/TECH-562.yaml
  target_anchor: "id: \"TECH-562\""
  payload:
    completed: "2026-04-24"

- operation: delete_file
  target_path: ia/projects/TECH-562.md
  target_anchor: "file:TECH-562.md"
  payload: null

- operation: replace_section
  target_path: ia/projects/unity-agent-bridge-master-plan.md
  target_anchor: "task_key:T1.1.4"
  payload: |
    | T1.1.4 | Menu regression pass | **TECH-562** | Done (archived) | Manual or automated check: **Territory Developer → Reports** still runs for all §10 items; no duplicate file writes; **`TryPersistReport`** paths unchanged for registry exports. |

- operation: digest_emit
  target_path: ia/backlog-archive/TECH-562.yaml
  target_anchor: "TECH-562"
  payload:
    tool: stage_closeout_digest
    mode: per_task

# Per-Task ops — TECH-563 (T1.1.5)
- operation: archive_record
  target_path: ia/backlog-archive/TECH-563.yaml
  target_anchor: "id: \"TECH-563\""
  payload:
    completed: "2026-04-24"

- operation: delete_file
  target_path: ia/projects/TECH-563.md
  target_anchor: "file:TECH-563.md"
  payload: null

- operation: replace_section
  target_path: ia/projects/unity-agent-bridge-master-plan.md
  target_anchor: "task_key:T1.1.5"
  payload: |
    | T1.1.5 | Play Mode + grid gate errors | **TECH-563** | Done (archived) | Before Play-only exports, verify **`GridManager.isInitialized`** (and documented **`TerrainManager`** needs); return **`failed`** + human-readable **`error`** field; align with analysis §8.3 risk table. |

- operation: digest_emit
  target_path: ia/backlog-archive/TECH-563.yaml
  target_anchor: "TECH-563"
  payload:
    tool: stage_closeout_digest
    mode: per_task

# Per-Task ops — TECH-564 (T1.1.6)
- operation: archive_record
  target_path: ia/backlog-archive/TECH-564.yaml
  target_anchor: "id: \"TECH-564\""
  payload:
    completed: "2026-04-24"

- operation: delete_file
  target_path: ia/projects/TECH-564.md
  target_anchor: "file:TECH-564.md"
  payload: null

- operation: replace_section
  target_path: ia/projects/unity-agent-bridge-master-plan.md
  target_anchor: "task_key:T1.1.6"
  payload: |
    | T1.1.6 | Bridge response contract tests | **TECH-564** | Done (archived) | Add EditMode or MCP-side tests asserting **`completed`** / **`failed`** shapes for **`export_cell_chunk`** + sorting debug when grid absent — snapshot keys only, not full JSON bodies. |

- operation: digest_emit
  target_path: ia/backlog-archive/TECH-564.yaml
  target_anchor: "TECH-564"
  payload:
    tool: stage_closeout_digest
    mode: per_task
```

---
