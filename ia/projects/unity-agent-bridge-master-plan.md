# Unity IDE Agent Bridge — Master Plan (Post–Phase 1 program)

> **Last updated:** 2026-04-24
>
> **Status:** In Progress — Stage 2.1
>
> **Scope:** Tiered hardening + transport + optional depth on top of shipped **Postgres** **`agent_bridge_job`** + **`unity_bridge_command`** / **`unity_bridge_get`** + **`AgentBridgeCommandRunner`** (`docs/unity-ide-agent-bridge-analysis.md` **Design Expansion**). **Out of program:** headless CI, `-batchmode` / Test Framework as delivery goals, file-only queue replacing **`agent_bridge_job`**, rewrite of **`ia/specs/unity-development-context.md`** §10 JSON contracts. Optional deferrals → recommend companion `docs/unity-agent-bridge-post-mvp-extensions.md` (not authored by this pass).
>
> **Exploration source:** `docs/unity-ide-agent-bridge-analysis.md` (**§ Design Expansion** — Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Examples).
>
> **Locked decisions (do not reopen in this plan):**
>
> - **Phase 1 already shipped:** queue + MCP + Editor runner — this plan is **§10-B → §10-C → §10-D** tiers, not a second MVP bridge choice.
> - Keep **`agent_bridge_job`** + existing **`kind`** surface; additive migrations only when new **`kind`** values need DB contract.
> - **Developer machine + Unity Editor open**; glossary-aligned command names; **`DATABASE_URL`** + migration **0008** remain the persistence path.
> - Reuse existing **`[MenuItem]`** export bodies — dispatch-only changes; no duplicate grid read logic.
> - **Grid reads:** **`GridManager.GetCell`** only where bridge touches cells — **invariant #5**.
>
> **Hierarchy rules:** `ia/projects/MASTER-PLAN-STRUCTURE.md` (canonical Stage > Task 2-level shape — authoritative) · `ia/rules/project-hierarchy.md` · `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable) · `ia/rules/plan-apply-pair-contract.md`.
>
> **Read first if landing cold:**
>
> - `docs/unity-ide-agent-bridge-analysis.md` — full analysis + **Design Expansion** (ground truth).
> - `ia/specs/unity-development-context.md` §10 — Editor agent diagnostics, **`editor_export_*`**, **`agent_bridge_job`**.
> - `docs/mcp-ia-server.md` — MCP tool catalog + bridge tools.
> - `ia/skills/ide-bridge-evidence/SKILL.md` — evidence / **`debug_context_bundle`** contract.
> - `ia/projects/MASTER-PLAN-STRUCTURE.md` + `ia/rules/project-hierarchy.md` — doc semantics + Stage / Task cardinality rule (≥2 Tasks per Stage).
> - `ia/rules/invariants.md` — **#5** (no direct **`gridArray`** / **`cellArray`** outside **`GridManager`**), **#3** (no hot-loop **`FindObjectOfType`** — bridge polling stays Editor update), **#6** (do not grow **`GridManager`** — extract helpers if new play-mode probes).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Stage `Status:` uses enum `Draft | In Review | In Progress | Final` (per `ia/projects/MASTER-PLAN-STRUCTURE.md` §6.2). Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file-apply` → task rows gain `Issue` id + `Draft` status; `plan-author` / `plan-digest` → `In Review`; `spec-implementer` → `In Progress`; `plan-applier` Mode stage-closeout → `Done (archived)` + Stage `Final` rollup.

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

### Stage 1.2 — MCP sugar tools + catalog

**Status:** Final

**Notes:** archived 2026-04-24 — 6 Tasks Done (TECH-571..576)

**Backlog state (Stage 1.2):** 6 filed → archived

**Objectives:** Register thin **`unity_export_*`** tools that wrap **`unity_bridge_command`** + poll **`unity_bridge_get`** for common flows. Keep surface small to avoid tool sprawl (analysis **Phase A** risk).

**Exit criteria:**

- At least **two** sugar tools shipped (e.g. **`unity_export_cell_chunk`**, **`unity_export_sorting_debug`**) with shared helper for poll/backoff.
- **`docs/mcp-ia-server.md`** documents sugar vs raw bridge; **`kind`** table updated.
- MCP integration tests cover happy path + timeout/error.

**Art:** None.

**Relevant surfaces (load when stage opens):**

- `docs/unity-ide-agent-bridge-analysis.md` — **Design Expansion** Implementation Points **Phase A**
- `tools/mcp-ia-server/` (exists) — `registerTool` + tests
- `docs/agent-led-verification-policy.md` — bridge timeout guidance (40 s initial)
- `ia/specs/unity-development-context.md` §10 — bridge kinds + params

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T1.2.1 | Sugar tool registration | **TECH-571** | Done (archived) | Add **`unity_export_cell_chunk`** + **`unity_export_sorting_debug`** (names per glossary / existing patterns) in **`tools/mcp-ia-server/src/`**; thin wrappers — call **`unity_bridge_command`**, poll **`unity_bridge_get`** by **`command_id`**, return parsed body / path refs. |
| T1.2.2 | Shared poll helper + limits | **TECH-572** | Done (archived) | Extract shared TypeScript helper: timeout aligned with agent-led verification policy (40 s initial / escalation documented in tool description); surface **`BRIDGE_TIMEOUT`** env if already used elsewhere. |
| T1.2.3 | mcp-ia-server.md catalog update | **TECH-573** | Done (archived) | Document sugar tools, params, and when to prefer raw **`unity_bridge_command`**; link **`agent_bridge_job`** migration + dequeue scripts. |
| T1.2.4 | §10 cross-link from spec | **TECH-574** | Done (archived) | Add short pointer in **`ia/specs/unity-development-context.md`** §10 "See also" to MCP catalog section for sugar tools (minimal edit — no contract rewrite). |
| T1.2.5 | MCP integration tests | **TECH-575** | Done (archived) | Extend **`tools/mcp-ia-server`** tests: mock or stub bridge responses if needed; assert Zod + tool handler paths for sugar tools. |
| T1.2.6 | validate:all + index | **TECH-576** | Done (archived) | Run **`npm run validate:all`**; update **`generate:ia-indexes`** if tool catalog indexed; fix any **`registerTool`** descriptor drift. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "Sugar tool registration"
  priority: "medium"
  notes: |
    Add unity_export_cell_chunk + unity_export_sorting_debug in tools/mcp-ia-server/src/.
    Thin wrappers: unity_bridge_command enqueue, unity_bridge_get poll by command_id, return parsed body / path refs.
    Align names with glossary + existing registerTool patterns.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Register two MCP sugar tools that wrap unity_bridge_command + unity_bridge_get for cell chunk and sorting debug exports.
      Handlers return parsed JSON body and artifact path refs agents need without raw poll loops every call.
    goals: |
      1. unity_export_cell_chunk + unity_export_sorting_debug registered with Zod params matching bridge request shapes.
      2. Each tool enqueues bridge job, polls get until completed/failed/timeout, returns structured result.
      3. Tool descriptions document when to prefer raw unity_bridge_command.
    systems_map: |
      - tools/mcp-ia-server/src/ — registerTool, handlers, Zod
      - docs/agent-led-verification-policy.md — bridge timeout guidance (40 s initial)
      - docs/mcp-ia-server.md — catalog surface (updated in sibling task)
      - ia/specs/unity-development-context.md §10 — bridge kinds + params
    impl_plan_sketch: |
      - Add registerTool entries + Zod for both tools
      - Implement enqueue + poll loop using shared helper (T1.2.2) or inline first pass per plan
      - Unit-test handler shape with mocked bridge responses

- reserved_id: ""
  title: "Shared poll helper + limits"
  priority: "medium"
  notes: |
    Extract shared TypeScript helper for unity_bridge_get polling: timeout aligned with agent-led verification policy (40 s initial, escalation in tool docs).
    Surface BRIDGE_TIMEOUT env if repo already uses it for MCP bridge tools.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Centralize poll/backoff + timeout caps for sugar tools and any future bridge wrappers so behavior matches verification policy.
    goals: |
      1. Single helper used by sugar tools for poll loop + deadline.
      2. Default timeout 40 s; document escalation path in tool description.
      3. Optional BRIDGE_TIMEOUT env override wired if pattern exists elsewhere in mcp-ia-server.
    systems_map: |
      - tools/mcp-ia-server/src/ — shared bridge poll utility
      - docs/agent-led-verification-policy.md — canonical timeout semantics
    impl_plan_sketch: |
      - Add poll_until_terminal(command_id, options) helper
      - Wire sugar tools to helper; add tests for timeout + completed paths

- reserved_id: ""
  title: "mcp-ia-server.md catalog update"
  priority: "medium"
  notes: |
    Document sugar tools, params, prefer-raw-bridge guidance; link agent_bridge_job migration + dequeue scripts.
    Keep catalog aligned with registerTool descriptors.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Extend docs/mcp-ia-server.md with sugar tool entries, parameter tables, and operational links (Postgres queue, scripts).
    goals: |
      1. Catalog lists unity_export_cell_chunk + unity_export_sorting_debug with params + return shape summary.
      2. When to use raw unity_bridge_command vs sugar tools is explicit.
      3. Cross-links to migration 0008 / dequeue helpers where documented.
    systems_map: |
      - docs/mcp-ia-server.md — MCP catalog
      - tools/mcp-ia-server/ — tool names + kinds
    impl_plan_sketch: |
      - Add subsection for sugar tools + kind table updates
      - Link unity-development-context §10 from catalog

- reserved_id: ""
  title: "§10 cross-link from spec"
  priority: "medium"
  notes: |
    Minimal pointer in ia/specs/unity-development-context.md §10 See also → MCP catalog sugar tools section.
    No contract rewrite.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Add short See also cross-link from unity-development-context §10 to MCP catalog so agents discover sugar wrappers from spec.
    goals: |
      1. One See also line or bullet pointing at docs/mcp-ia-server.md sugar section.
      2. No change to JSON contracts or bridge DTO prose beyond pointer.
    systems_map: |
      - ia/specs/unity-development-context.md §10
      - docs/mcp-ia-server.md
    impl_plan_sketch: |
      - Insert See also cross-link under §10

- reserved_id: ""
  title: "MCP integration tests"
  priority: "medium"
  notes: |
    Extend tools/mcp-ia-server tests: mock/stub bridge responses; assert Zod + tool handler paths for sugar tools.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Add or extend integration tests so sugar tools validate input via Zod and exercise handler paths with stubbed unity_bridge_get progression.
    goals: |
      1. Tests cover happy path completed + failed + timeout stub paths.
      2. Zod rejects invalid params before enqueue.
      3. No live Postgres/Unity required in CI for these tests.
    systems_map: |
      - tools/mcp-ia-server/tests/
      - tools/mcp-ia-server/src/
    impl_plan_sketch: |
      - Add fixtures/mocks for bridge command + get polling
      - Assert tool output shape for each sugar tool

- reserved_id: ""
  title: "validate:all + index"
  priority: "medium"
  notes: |
    Run npm run validate:all; update generate:ia-indexes if tool catalog indexed; fix registerTool descriptor drift vs indexes.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Close Stage with repo-wide validators green and IA indexes consistent with new tools.
    goals: |
      1. npm run validate:all exits 0 after all prior tasks land.
      2. generate:ia-indexes --check passes; update sources if MCP tools appear in index rules.
      3. No stale registerTool / doc cross-ref failures.
    systems_map: |
      - package.json scripts — validate:all chain
      - tools/mcp-ia-server — tool registration
    impl_plan_sketch: |
      - Run validate:all; fix any failures
      - Patch index generation inputs if required
```

#### §Plan Fix

> plan-review exit 0 — all 6 Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

_retroactive-skip — Stage 1.2 closed pre-canonical-§Stage-Audit (2026-04-24 structure refactor). No audit paragraphs persisted at close time._

#### §Stage Closeout Plan

> stage-closeout-plan — 6 Tasks (applied inline 2026-04-20). `plan-applier` Mode stage-closeout executed: archive backlog yaml **TECH-571**…**TECH-576** → `ia/backlog-archive/`; delete per-Task project specs for those ids; flip task rows → `Done (archived)`; Stage 1.2 **Status** → `Final`.

```yaml
closed_issue_ids: ["TECH-571","TECH-572","TECH-573","TECH-574","TECH-575","TECH-576"]
completed_iso: "2026-04-20"
validators: ["materialize-backlog.sh", "npm run validate:all"]
```

---

### Stage 1.3 — Cursor skill + narrative alignment

**Status:** Final

**Notes:** archived 2026-04-20 — 6 Tasks Done (TECH-587..592)

**Backlog state (Stage 1.3):** 6 filed → archived

**Objectives:** Ship **`.claude/skills/debug-sorting-order`** (Cursor-only). Patch **`ia/skills/ide-bridge-evidence`** only if Stage 1.1–1.2 changed evidence DTOs. Align **Close Dev Loop** / staging supersession text with exploration §7.1 / §10-B.

**Exit criteria:**

- **`.claude/skills/debug-sorting-order/SKILL.md`** committed with phases: bridge calls → **`spec_section`** **`geo`** §7 → compare → fix loop.
- **`ide-bridge-evidence`** updated OR explicit "no delta" note in Stage exit if DTOs unchanged.
- Docs note how **`debug_context_bundle`** relates to sugar tools (no contradiction with **`close-dev-loop`**).

**Art:** None.

**Relevant surfaces (load when stage opens):**

- `ia/specs/isometric-geography-system.md` §7 — sorting formula authority for debug-sorting skill
- `.claude/skills/debug-sorting-order/SKILL.md` **(new)**
- `ia/skills/ide-bridge-evidence/SKILL.md` (exists) — update only if response DTOs change
- `docs/mcp-ia-server.md` — MCP tool catalog

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T1.3.1 | debug-sorting-order SKILL body | **TECH-587** | Done (archived) | Author **`.claude/skills/debug-sorting-order/SKILL.md`**: triggers, prerequisites (**`DATABASE_URL`**, Unity on **`REPO_ROOT`**), recipe calling **`unity_export_sorting_debug`** + **`unity_export_cell_chunk`**, **`spec_section`** **`geo`** §7, comparison checklist (**BUG-28**-style). |
| T1.3.2 | Symlink + skill index | **TECH-588** | Done (archived) | If required by repo convention, symlink **`ia/skills/...`** → **`.claude/skills/...`**; add row to **`ia/skills/README.md`** only if this repo lists Cursor-packaged skills (minimal). |
| T1.3.3 | ide-bridge-evidence diff | **TECH-589** | Done (archived) | Read **`ia/skills/ide-bridge-evidence/SKILL.md`**; update tool names / bundle fields if Stage 1.1–1.2 changed responses; otherwise add single-line "no bridge DTO change" exit note in task report. |
| T1.3.4 | Glossary / router spot-check | **TECH-590** | Done (archived) | Verify **`glossary_lookup`** "IDE agent bridge" + **`router_for_task`** domains still accurate; no new glossary row unless new public term introduced (terminology rule). |
| T1.3.5 | Close Dev Loop doc alignment | **TECH-591** | Done (archived) | Update **`docs/agent-led-verification-policy.md`** or **`docs/mcp-ia-server.md`** short subsection: **`close-dev-loop`** + **`debug_context_bundle`** vs sugar tools — supersession of registry staging (per analysis). |
| T1.3.6 | Optional backlog spec pointer | **TECH-592** | Done (archived) | If **`ia/backlog/TECH-552.yaml`** (or successor) tracks bridge program, add **`spec:`** → this orchestrator path + **`npm run materialize-backlog.sh`** — only if issue record exists; do not invent issue id in orchestrator body. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "debug-sorting-order SKILL body"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Author .claude/skills/debug-sorting-order/SKILL.md. Triggers, DATABASE_URL + Unity on REPO_ROOT,
    unity_export_sorting_debug + unity_export_cell_chunk, spec_section geo §7, BUG-28-style comparison checklist.
    Touches .claude/skills/ only.
  depends_on: []
  related:
    - "TECH-588"
    - "TECH-589"
    - "TECH-590"
    - "TECH-591"
    - "TECH-592"
  stub_body:
    summary: |
      New Cursor skill documents end-to-end sorting-order debug: bridge exports, isometric geography §7
      authority via spec_section, and agent comparison loop.
    goals: |
      1. SKILL.md lists triggers and prerequisites (DATABASE_URL, Unity Editor, REPO_ROOT).
      2. Recipe covers unity_export_sorting_debug and unity_export_cell_chunk plus spec_section geo §7.
      3. Checklist matches close-dev-loop style (BUG-28 reference pattern) for before/after comparison.
      4. No ia/skills clone; Cursor path under .claude/skills per orchestrator header.
    systems_map: |
      - .claude/skills/debug-sorting-order/SKILL.md — new
      - docs/unity-ide-agent-bridge-analysis.md — Design Expansion cross-link optional
      - ia/specs/isometric-geography-system.md §7 — sorting formula authority
      - tools/mcp-ia-server — unity_export_* tool names
    impl_plan_sketch: |
      - Author SKILL.md sections (purpose, triggers, prerequisites, phased recipe)
      - Wire glossary terms: IDE agent bridge, unity_bridge_command, spec_section
      - Add symlink row in ia/skills README or note defer to TECH-588

- reserved_id: ""
  title: "Symlink + skill index"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Symlink ia/skills/debug-sorting-order to .claude/skills if repo convention requires; minimal
    ia/skills/README.md row when index lists Cursor-packaged skills.
  depends_on: []
  related:
    - "TECH-587"
    - "TECH-589"
    - "TECH-590"
    - "TECH-591"
    - "TECH-592"
  stub_body:
    summary: |
      Align repository skill wiring so debug-sorting-order is discoverable from both ia/skills and
      .claude/skills per existing symlink pattern.
    goals: |
      1. Symlink exists if and only if sibling skills use same pattern.
      2. README row added only when table already lists packaged skills; otherwise document skip in task report.
      3. No duplicate SKILL bodies — single source path documented.
    systems_map: |
      - .claude/skills/ — Cursor symlink targets
      - ia/skills/README.md — optional index row
      - ia/skills/debug-sorting-order/ — symlink target if created
    impl_plan_sketch: |
      - Compare existing .claude/skills → ia/skills symlinks
      - Add symlink or record explicit no-op with reason
      - Patch README minimally if required by convention

- reserved_id: ""
  title: "ide-bridge-evidence diff"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Diff ia/skills/ide-bridge-evidence/SKILL.md against Stage 1.1–1.2 bridge DTO changes; update tool names or
    bundle fields if needed; else one-line no-change note in findings.
  depends_on: []
  related:
    - "TECH-587"
    - "TECH-588"
    - "TECH-590"
    - "TECH-591"
    - "TECH-592"
  stub_body:
    summary: |
      Ensure ide-bridge-evidence skill text matches shipped bridge responses and MCP tool names after
      Stage 1.1–1.2 parameter work.
    goals: |
      1. Read ide-bridge-evidence SKILL end-to-end.
      2. If export kinds or response DTOs changed, update skill prose and examples.
      3. If no delta, capture explicit no DTO change note for audit trail.
    systems_map: |
      - ia/skills/ide-bridge-evidence/SKILL.md
      - docs/mcp-ia-server.md — tool catalog
      - ia/specs/unity-development-context.md §10
    impl_plan_sketch: |
      - Compare SKILL tool names vs MCP registerTool + §10 table
      - Edit SKILL or add no-change sentence to §Findings / report
      - npm run validate:all if MCP descriptors touched

- reserved_id: ""
  title: "Glossary / router spot-check"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Verify glossary_lookup IDE agent bridge + router_for_task domains; add glossary row only if new public term; terminology-consistency rule.
  depends_on: []
  related:
    - "TECH-587"
    - "TECH-588"
    - "TECH-589"
    - "TECH-591"
    - "TECH-592"
  stub_body:
    summary: |
      Spot-check MCP routing and glossary anchors for bridge vocabulary after Stage 1 work; no gratuitous new terms.
    goals: |
      1. glossary_lookup and router_for_task return coherent entries for bridge workflow.
      2. Document pass/fail in spec; new glossary row only if truly new domain term.
      3. No issue ids in durable specs per terminology rule.
    systems_map: |
      - ia/specs/glossary.md
      - tools/mcp-ia-server — router + glossary tools
      - docs/mcp-ia-server.md
    impl_plan_sketch: |
      - Run glossary_lookup + router_for_task probes (record outputs in §Verification later)
      - File gap as backlog only if tool broken; else narrative confirmation in spec

- reserved_id: ""
  title: "Close Dev Loop doc alignment"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Short subsection in docs/agent-led-verification-policy.md or docs/mcp-ia-server.md: close-dev-loop vs
    debug_context_bundle vs unity_export_* sugar; registry staging supersession narrative.
  depends_on: []
  related:
    - "TECH-587"
    - "TECH-588"
    - "TECH-589"
    - "TECH-590"
    - "TECH-592"
  stub_body:
    summary: |
      Align durable docs so agents understand when close-dev-loop, debug_context_bundle, and export sugar
      tools apply — consistent with unity-ide-agent-bridge analysis §7.1 / §10-B.
    goals: |
      1. One subsection links close-dev-loop skill to bridge evidence paths without contradiction.
      2. debug_context_bundle vs sugar tools relationship explicit.
      3. npm run validate:all green after doc edits.
    systems_map: |
      - docs/agent-led-verification-policy.md
      - docs/mcp-ia-server.md
      - docs/unity-ide-agent-bridge-analysis.md — §7.1 narrative
    impl_plan_sketch: |
      - Choose policy vs MCP doc anchor for subsection
      - Add cross-links to ide-bridge-evidence + close-dev-loop skills
      - validate:all

- reserved_id: ""
  title: "Optional backlog spec pointer"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    If TECH-552 (or successor) yaml tracks bridge program, set spec to this orchestrator; run materialize-backlog.sh.
    Skip if issue absent or out of scope.
  depends_on: []
  related:
    - "TECH-587"
    - "TECH-588"
    - "TECH-589"
    - "TECH-590"
    - "TECH-591"
  stub_body:
    summary: |
      Optional alignment between bridge umbrella backlog record and this master plan path when TECH-552 or
      successor exists.
    goals: |
      1. Confirm whether TECH-552.yaml (or listed successor) is active bridge tracker.
      2. If yes, set spec field to ia/projects/unity-agent-bridge-master-plan.md and materialize backlog.
      3. If no, document skip — do not invent ids in orchestrator body.
    systems_map: |
      - ia/backlog/TECH-552.yaml — conditional
      - BACKLOG.md — generated view
      - ia/projects/unity-agent-bridge-master-plan.md — orchestrator path
    impl_plan_sketch: |
      - backlog_issue TECH-552 (or successor) status check
      - Patch yaml spec field if appropriate
      - bash tools/scripts/materialize-backlog.sh when yaml changes
```

#### §Plan Fix

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

_retroactive-skip — Stage 1.3 closed pre-canonical-§Stage-Audit (2026-04-24 structure refactor). No audit paragraphs persisted at close time._

#### §Stage Closeout Plan

> stage-closeout-plan — 6 Tasks (applied inline 2026-04-20). `plan-applier` Mode stage-closeout executed: archive backlog yaml **TECH-587**…**TECH-592** → `ia/backlog-archive/`; delete per-Task project specs for those ids; flip task rows → `Done (archived)`; Stage 1.3 **Status** → `Final`. No glossary/rule/doc shared migrations; no durable-doc id purge.

```yaml
closed_issue_ids: ["TECH-587","TECH-588","TECH-589","TECH-590","TECH-591","TECH-592"]
completed_iso: "2026-04-20"
validators: ["materialize-backlog.sh", "npm run validate:all"]
```

---

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

## Orchestration guardrails

**Do:**

- Open one Stage at a time. Next Stage opens only after current Stage's closeout runs.
- Run `/stage-file ia/projects/unity-agent-bridge-master-plan.md Stage N.M` to materialize `_pending_` Tasks.
- Update Stage + Task `Status` via lifecycle skills — do NOT edit by hand.
- Preserve locked decisions. Changes require explicit re-decision + sync edit to exploration + scope-boundary docs.
- Extend via `/master-plan-extend ia/projects/unity-agent-bridge-master-plan.md {source-doc}` — do NOT hand-insert new Stage blocks.
- Keep **`unity-development-context`** §10 authoritative for Reports + artifact tables — patch with cross-links only unless contract intentionally versioned.

**Do not:**

- Close the orchestrator via `/closeout` — orchestrators are permanent (`orchestrator-vs-spec.md`).
- Silently promote post-MVP items into Stage 1–2 — bucket to **`docs/unity-agent-bridge-post-mvp-extensions.md`**.
- Merge partial Stage state — every Stage lands on a green bar (`npm run validate:all` + `npm run unity:compile-check` when C# touched).
- Insert BACKLOG rows directly into this doc — only `stage-file-apply` materializes them.
- Hand-insert new Stages past the last persisted `### Stage N.M` block — run `/master-plan-extend`.
- Replace **`agent_bridge_job`** with file-only transport — locked out.
- Add **headless CI** or **`-batchmode`** delivery goals to this plan — analysis explicitly excludes.
