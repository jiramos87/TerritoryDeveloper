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
