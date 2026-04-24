### Stage 1.2 — MCP server split (B1)


**Status:** Final

**Objectives:** Extract Unity-bridge + compute tools from the single `territory-ia` MCP server into a dedicated `territory-ia-bridge` server behind a feature flag. IA-authoring sessions load the lean core; verify/implement stages opt-in to the bridge. Flag default off in this Stage; flip default in Stage 1.3 post-sweep.

**Exit:**

- `tools/mcp-ia-server/src/index-ia.ts` (new): registers all non-bridge tools (≥22 IA-authoring tools).
- `tools/mcp-ia-server/src/index-bridge.ts` (new): registers Unity-bridge + compute tools (`unity_bridge_command`, `unity_bridge_get`, `unity_bridge_lease`, `unity_compile`, `unity_callers_of`, `unity_subscribers_of`, `findobjectoftype_scan`, `city_metrics_query`, `desirability_top_cells`, `geography_init_params_validate`, `grid_distance`, `growth_ring_classify`, `isometric_world_to_grid`, `pathfinding_cost_preview`).
- `tools/mcp-ia-server/src/index.ts` retained as default entry (backward compat, imports both).
- `.mcp.json` `territory-ia-bridge` server entry added; `MCP_SPLIT_SERVERS=0` default.
- Integration test `tools/mcp-ia-server/tests/server-split.test.ts` passes: `MCP_SPLIT_SERVERS=1` + design-explore dispatch → bridge tools absent from `tools/list`; spec-implementer dispatch + bridge prefix → bridge tools present.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T1.2.1 | Extract IA-core + bridge servers | **TECH-524** | Done | In `tools/mcp-ia-server/src/`: author `index-ia.ts` registering all IA-authoring tools (backlog, router, glossary, spec, rules, invariants, journal, reserve, materialize surfaces); author `index-bridge.ts` registering Unity-bridge + compute tools (14 tools). Original `index.ts` retained as backward-compat default importing both. Add `MCP_SPLIT_SERVERS` env check to `index.ts`: when `=1`, `index-ia.ts` standalone path loads. |
| T1.2.2 | .mcp.json split config | **TECH-525** | Done | Add `territory-ia-bridge` entry to `.mcp.json` pointing to `index-bridge.ts`; add `"MCP_SPLIT_SERVERS": "0"` to existing `territory-ia` env block (alongside existing `DEBUG_MCP_COMPUTE`). Document `MCP_SPLIT_SERVERS=1` flag semantics in `docs/mcp-ia-server.md` (new §Server split architecture section). |
| T1.2.3 | Integration test fixture | **TECH-526** | Done | Author `tools/mcp-ia-server/tests/server-split.test.ts`: assert `MCP_SPLIT_SERVERS=1` + design-explore-style dispatch → `tools/list` response excludes `unity_bridge_command`; assert spec-implementer-style dispatch with bridge server prefix declared → bridge tools present. Add `npm run test:mcp-split` script to `package.json`. |
| T1.2.4 | Flag-flip timeline doc | **TECH-527** | Done | Document `MCP_SPLIT_SERVERS` flag-flip timeline in Stage 1.3 header (flip from `0` to `1` after post-stage sweep confirms correctness per NB-6 resolution). Update `docs/session-token-latency-audit-exploration.md` §Open questions to mark B1 primary decision closed. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  task_key: "T1.2.1"
  title: "Extract IA-core + bridge servers"
  priority: "high"
  issue_type: "TECH"
  notes: |
    In tools/mcp-ia-server/src/: author index-ia.ts registering all IA-authoring tools
    (backlog, router, glossary, spec, rules, invariants, journal, reserve, materialize
    surfaces); author index-bridge.ts registering Unity-bridge + compute tools (14 tools:
    unity_bridge_command, unity_bridge_get, unity_bridge_lease, unity_compile,
    unity_callers_of, unity_subscribers_of, findobjectoftype_scan, city_metrics_query,
    desirability_top_cells, geography_init_params_validate, grid_distance,
    growth_ring_classify, isometric_world_to_grid, pathfinding_cost_preview). Original
    index.ts retained as backward-compat default importing both. Add MCP_SPLIT_SERVERS env
    check to index.ts: when =1, index-ia.ts standalone path loads. Foundation for B1
    server split — T1.2.2/T1.2.3/T1.2.4 consume.
  depends_on: []
  related:
    - "T1.2.2"
    - "T1.2.3"
    - "T1.2.4"
  stub_body:
    summary: |
      Extract single territory-ia MCP server into IA-core + bridge dual-server shape behind
      MCP_SPLIT_SERVERS feature flag. IA-authoring sessions load lean core; verify/implement
      stages opt-in to bridge. Flag default off in this Stage; flip in Stage 1.3 post-sweep.
    goals: |
      - New file tools/mcp-ia-server/src/index-ia.ts registers ≥22 IA-authoring tools.
      - New file tools/mcp-ia-server/src/index-bridge.ts registers 14 Unity-bridge + compute tools.
      - index.ts retains backward-compat default; importing both server modules.
      - MCP_SPLIT_SERVERS env check selects standalone path when =1.
    systems_map: |
      New: tools/mcp-ia-server/src/index-ia.ts.
      New: tools/mcp-ia-server/src/index-bridge.ts.
      Touches: tools/mcp-ia-server/src/index.ts (env-check + dual-import).
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 1 — Server extraction.
      1. Inventory current index.ts tool registrations (≥36 total).
      2. Bucket tools: IA-authoring (backlog/router/glossary/spec/rules/invariants/journal/reserve/materialize) vs Unity-bridge + compute (14).
      3. Author index-ia.ts: import shared registration helpers + register IA-authoring tools.
      4. Author index-bridge.ts: register the 14 bridge tools.
      5. Edit index.ts: add MCP_SPLIT_SERVERS env check; default path imports both; =1 path loads index-ia.ts only.
      6. Run npm run validate:all.
```

```yaml
- reserved_id: ""
  task_key: "T1.2.2"
  title: ".mcp.json split config"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Add territory-ia-bridge entry to .mcp.json pointing to index-bridge.ts; add
    "MCP_SPLIT_SERVERS": "0" to existing territory-ia env block (alongside existing
    DEBUG_MCP_COMPUTE). Document MCP_SPLIT_SERVERS=1 flag semantics in
    docs/mcp-ia-server.md (new §Server split architecture section). Activates B1 dual-server
    surface for opt-in consumption.
  depends_on: []
  related:
    - "T1.2.1"
    - "T1.2.3"
    - "T1.2.4"
  stub_body:
    summary: |
      Wire dual-server config in .mcp.json: register territory-ia-bridge alongside existing
      territory-ia entry; default flag off. Document flag semantics + flip timeline in
      docs/mcp-ia-server.md.
    goals: |
      - .mcp.json carries territory-ia-bridge server entry pointing to index-bridge.ts.
      - territory-ia env block carries MCP_SPLIT_SERVERS=0 default.
      - docs/mcp-ia-server.md gains §Server split architecture section documenting flag.
      - npm run validate:all green.
    systems_map: |
      Touches: .mcp.json (root).
      Touches: docs/mcp-ia-server.md (new §Server split architecture section).
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 1 — Config + docs.
      1. Edit .mcp.json: add territory-ia-bridge server block (mirrors territory-ia shape, command targets index-bridge.ts).
      2. Edit territory-ia env block: add "MCP_SPLIT_SERVERS": "0" alongside DEBUG_MCP_COMPUTE.
      3. Author docs/mcp-ia-server.md §Server split architecture: rationale + flag semantics + flip timeline pointer (Stage 1.3 sweep).
      4. Run npm run validate:all.
```

```yaml
- reserved_id: ""
  task_key: "T1.2.3"
  title: "Integration test fixture"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Author tools/mcp-ia-server/tests/server-split.test.ts: assert MCP_SPLIT_SERVERS=1 +
    design-explore-style dispatch → tools/list response excludes unity_bridge_command;
    assert spec-implementer-style dispatch with bridge server prefix declared → bridge
    tools present. Add npm run test:mcp-split script to package.json. Locks B1 split
    semantics behind a CI gate.
  depends_on: []
  related:
    - "T1.2.1"
    - "T1.2.2"
    - "T1.2.4"
  stub_body:
    summary: |
      Ship integration test asserting B1 server-split semantics. Two dispatches: lean
      IA-core path excludes bridge tools; bridge-prefix path exposes them. Wired via
      npm run test:mcp-split.
    goals: |
      - New file tools/mcp-ia-server/tests/server-split.test.ts.
      - Test asserts MCP_SPLIT_SERVERS=1 + IA-core dispatch hides 14 bridge tools.
      - Test asserts bridge-prefix dispatch exposes 14 bridge tools.
      - package.json scripts gain test:mcp-split entry.
    systems_map: |
      New: tools/mcp-ia-server/tests/server-split.test.ts.
      Touches: package.json (scripts.test:mcp-split).
      Reads: tools/mcp-ia-server/src/index-ia.ts + index-bridge.ts (T1.2.1 output).
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 2 — Integration test.
      1. Author server-split.test.ts: spawn server with MCP_SPLIT_SERVERS=1; query tools/list; assert bridge tools absent.
      2. Add bridge-prefix branch: query tools/list with bridge config; assert 14 bridge tools present.
      3. Add test:mcp-split script to package.json.
      4. Run npm run test:mcp-split locally; confirm green.
      5. Run npm run validate:all.
```

```yaml
- reserved_id: ""
  task_key: "T1.2.4"
  title: "Flag-flip timeline doc"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Document MCP_SPLIT_SERVERS flag-flip timeline in Stage 1.3 header (flip from 0 to 1
    after post-stage sweep confirms correctness per NB-6 resolution). Update
    docs/session-token-latency-audit-exploration.md §Open questions to mark B1 primary
    decision closed. Closes Stage 1.2 paper trail.
  depends_on: []
  related:
    - "T1.2.1"
    - "T1.2.2"
    - "T1.2.3"
  stub_body:
    summary: |
      Doc-only task: cross-reference flag-flip timeline in Stage 1.3 header + close NB-6
      open question on B1 in exploration doc. No code touched.
    goals: |
      - Stage 1.3 header in master plan carries MCP_SPLIT_SERVERS flip timeline note.
      - docs/session-token-latency-audit-exploration.md §Open questions B1 entry marked Closed.
      - npm run validate:all green.
    systems_map: |
      Touches: ia/projects/session-token-latency-master-plan.md (Stage 1.3 header).
      Touches: docs/session-token-latency-audit-exploration.md (§Open questions).
      No code / runtime surface touched.
    impl_plan_sketch: |
      Phase 2 — Doc closeout.
      1. Edit Stage 1.3 header: add inline note pointing to MCP_SPLIT_SERVERS flip step (T1.3.6).
      2. Edit exploration §Open questions: flip B1 row to Closed with resolution pointer (Stage 1.2 + Stage 1.3 sweep).
      3. Run npm run validate:all.
```

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
