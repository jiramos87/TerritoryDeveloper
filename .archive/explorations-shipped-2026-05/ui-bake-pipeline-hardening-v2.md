---
slug: ui-bake-pipeline-hardening-v2
parent_plan_id: null
target_version: 1
notes: >-
  V2 hardening — picks up where `bake-pipeline-hardening` (v1, closed 2026-05-09)
  left off. V1 shipped surfaces (KindRendererMatrix / SlotAnchorResolver /
  validate:bake-handler-kind-coverage / validate:ui-id-consistency /
  validate_panel_blueprint / task_kind=ui_from_db blueprint loader / Settings
  tracer) but cityscene-mainmenu-panel-rollout Stages 6.0–9.0 shipped WITHOUT
  consuming them — the "Stage 5.5 prologue" recommended in
  docs/cityscene-rollout-bake-hardening-impact.md never ran. Result: stages
  6.0–9.0 closed PASS but Editor QA found legacy GOs still active, wrong
  panels opened by HUD buttons, sub-views with no controls, dead pause-menu
  buttons, hidden notifications, theme drift. Six-layer hardening plan to
  close every gap v1 left open — author-time gates, bake-time correctness,
  scene-wire DB-driven (biggest gap), runtime contract tests, visual /
  functional verify, auditability. Final stage = retrospective + v3 repair
  extension trigger task for cityscene-mainmenu-panel-rollout.
stages:
  - id: "1.0"
    title: "Layer 1 — Author-time DB gates (archetype×kind coverage + action-id sink uniqueness + bind contract + token graph + view-slot required-by)"
    exit: "MCP write to catalog tables blocked when archetype lacks renderer / action-id sink missing / bind-id contract violated / token reference dangling / view-slot anchor missing required-by edge. Stage 1 test file stays red until task 1.0.5 lands."
    red_stage_proof: |
      # gate runs at catalog_panel_publish time
      row = {"slug": "test-panel", "children": [{"kind": "unknown-kind"}]}
      result = mcp.catalog_panel_publish(row)
      assert result.ok is False
      assert result.errors[0].code == "archetype_no_renderer"

      row2 = {"slug": "p2", "children": [{"kind": "button", "action_id": "duplicate.id"}]}
      mcp.catalog_panel_publish(row2)
      row3 = {"slug": "p3", "children": [{"kind": "button", "action_id": "duplicate.id"}]}
      result3 = mcp.catalog_panel_publish(row3)
      assert result3.ok is False
      assert result3.errors[0].code == "action_id_sink_collision"
    red_stage_proof_block:
      red_test_anchor: "unit-test:tests/ui-bake-hardening-v2/stage1-author-gates.test.mjs::publish_FailsOnArchetypeNoRenderer"
      target_kind: tracer_verb
      proof_artifact_id: tests/ui-bake-hardening-v2/stage1-author-gates.test.mjs
      proof_status: failed_as_expected
    tasks:
      - id: 1.0.1
        title: "archetype×kind renderer coverage gate (catalog_panel_publish MCP)"
        prefix: TECH
        depends_on: []
        digest_outline: "Wire `validate-bake-handler-kind-coverage.mjs` invariant into `catalog_panel_publish` mutation. Every child.kind in DB row must resolve to KindRendererMatrix entry OR archetype with renderer hint. Publish errors on miss. Stage 1 test file created red — publish_FailsOnArchetypeNoRenderer asserts publish rejects child.kind=unknown-kind."
        touched_paths:
          - "tools/mcp-ia-server/src/tools/catalog-panel-publish.ts"
          - "tools/mcp-ia-server/src/ia-db/mutations/catalog-panel.ts"
          - "tests/ui-bake-hardening-v2/stage1-author-gates.test.mjs"
        kind: code
      - id: 1.0.2
        title: "action-id sink uniqueness gate (UiActionRegistry contract)"
        prefix: TECH
        depends_on: ["TECH-1.0.1"]
        digest_outline: "Add `ia_ui_action_sinks` table (action_id PK, owner_panel_slug, registered_at). On `catalog_panel_publish`, every child.action_id must be unique across ALL published panels. Publish errors on collision. Test extends with publish_FailsOnActionIdSinkCollision."
        touched_paths:
          - "db/migrations/0147_ia_ui_action_sinks.sql"
          - "tools/mcp-ia-server/src/ia-db/mutations/catalog-panel.ts"
          - "tests/ui-bake-hardening-v2/stage1-author-gates.test.mjs"
        kind: code
      - id: 1.0.3
        title: "bind-id contract gate (bind reads/writes resolve to registered binds)"
        prefix: TECH
        depends_on: ["TECH-1.0.2"]
        digest_outline: "Reuse `ia_ui_bind_registry` (existing). On publish, every child.bind_id must exist in registry OR be marked declare-on-publish (auto-registers). Test extends with publish_FailsOnUnknownBindId."
        touched_paths:
          - "tools/mcp-ia-server/src/ia-db/mutations/catalog-panel.ts"
          - "tests/ui-bake-hardening-v2/stage1-author-gates.test.mjs"
        kind: code
      - id: 1.0.4
        title: "token reference graph gate (DS-token usage must resolve to ui_design_tokens row)"
        prefix: TECH
        depends_on: ["TECH-1.0.3"]
        digest_outline: "Add `catalog_token_resolve` mutation check. Every params_json.token-* reference must resolve to published ui_design_tokens row OR alias. Stale token refs error. Test extends with publish_FailsOnDanglingToken."
        touched_paths:
          - "tools/mcp-ia-server/src/ia-db/mutations/catalog-panel.ts"
          - "tools/mcp-ia-server/src/ia-db/mutations/catalog-token.ts"
          - "tests/ui-bake-hardening-v2/stage1-author-gates.test.mjs"
        kind: code
      - id: 1.0.5
        title: "view-slot anchor required-by gate (panel declaring views[] must declare anchor required-by edge)"
        prefix: TECH
        depends_on: ["TECH-1.0.4"]
        digest_outline: "On publish, panel.views[] must have matching anchor row in `catalog_panel_anchors` (new table) declaring `slot_name + required_by_panels[]`. Prevents F2-class (slot drift). Test extends with publish_FailsOnUnanchoredView → file flips green."
        touched_paths:
          - "db/migrations/0148_catalog_panel_anchors.sql"
          - "tools/mcp-ia-server/src/ia-db/mutations/catalog-panel.ts"
          - "tests/ui-bake-hardening-v2/stage1-author-gates.test.mjs"
        kind: code

  - id: "2.0"
    title: "Layer 2 — Bake-time correctness (non-empty assert + bake handler plugin pattern + bake diff baseline + meta-file write proof)"
    exit: "bake produces non-empty prefab for every declared child OR errors. Bake-handler dispatch externalized to `IBakeHandler[]` plugin list. Per-panel bake diff stored vs golden baseline → drift flagged. .meta file write proven post-bake. Stage 2 EditMode test file flips green at task 2.0.4."
    red_stage_proof: |
      panel = catalog.get_panel("settings")
      prefab = bake.run(panel)
      assert prefab.transform.childCount == len(panel.children)
      for child in prefab.children:
        assert child.has_component_matching(child.expected_component_type)

      baseline = bake.load_baseline("settings")
      diff = bake.diff(prefab, baseline)
      assert diff.added == [] and diff.removed == [] and diff.changed == []
    red_stage_proof_block:
      red_test_anchor: "unit-test:Assets/Tests/EditMode/UiBakeHardeningV2/Stage2BakeCorrectness.cs::Bake_FailsOnEmptyChild"
      target_kind: visibility_delta
      proof_artifact_id: Assets/Tests/EditMode/UiBakeHardeningV2/Stage2BakeCorrectness.cs
      proof_status: failed_as_expected
    tasks:
      - id: 2.0.1
        title: "non-empty child assert in UiBakeHandler.Apply"
        prefix: TECH
        depends_on: ["TECH-1.0.5"]
        digest_outline: "After every child render, assert `child.gameObject != null AND child.transform.childCount > 0 OR child.has_required_component()`. Empty stub → `BakeException(\"empty_child:{kind}:{slug}\")`. Stage 2 EditMode test file created red — Bake_FailsOnEmptyChild bakes panel with stub child + asserts exception."
        touched_paths:
          - "Assets/Scripts/Editor/Bridge/UiBakeHandler.cs"
          - "Assets/Tests/EditMode/UiBakeHardeningV2/Stage2BakeCorrectness.cs"
        kind: code
      - id: 2.0.2
        title: "bake-handler plugin pattern (IBakeHandler[] dispatch)"
        prefix: TECH
        depends_on: ["TECH-2.0.1"]
        digest_outline: "Externalize hardcoded `BakeChildByKind` switch into `IBakeHandler[]` plugin list. Each plugin declares `SupportedKinds` + `Bake(child, parent)`. Bake handler iterates plugins in priority order. New kind = new plugin class, no switch edit. Test extends with BakeHandler_DispatchesToFirstMatchingPlugin."
        touched_paths:
          - "Assets/Scripts/Editor/Bridge/UiBakeHandler.cs"
          - "Assets/Scripts/Editor/UiBake/IBakeHandler.cs"
          - "Assets/Scripts/Editor/UiBake/Plugins/ButtonBakeHandler.cs"
          - "Assets/Scripts/Editor/UiBake/Plugins/RowBakeHandler.cs"
          - "Assets/Tests/EditMode/UiBakeHardeningV2/Stage2BakeCorrectness.cs"
        kind: code
      - id: 2.0.3
        title: "bake diff baseline + golden manifest per panel"
        prefix: TECH
        depends_on: ["TECH-2.0.2"]
        digest_outline: "Add `bake.diff(prefab, baseline)` → returns `{added[], removed[], changed[]}`. Golden manifest = JSON snapshot of last-approved bake under `Assets/Resources/UI/Generated/Baselines/{panel}.json`. CI gate flags drift. First-author of new panel snapshots baseline. Test extends with BakeDiff_FlagsDrift."
        touched_paths:
          - "Assets/Scripts/Editor/UiBake/BakeDiffer.cs"
          - "Assets/Resources/UI/Generated/Baselines/.gitkeep"
          - "Assets/Tests/EditMode/UiBakeHardeningV2/Stage2BakeCorrectness.cs"
        kind: code
      - id: 2.0.4
        title: ".meta-file write proof post-bake"
        prefix: TECH
        depends_on: ["TECH-2.0.3"]
        digest_outline: "After AssetDatabase.SaveAssets, assert `File.Exists($\"{prefab_path}.meta\")` AND meta GUID matches stable expected (prevents F4-class — Inspector OnClick empty because bake wrote prefab but didn't import). Test extends with Bake_WritesMetaWithStableGuid → Stage 2 file flips green."
        touched_paths:
          - "Assets/Scripts/Editor/Bridge/UiBakeHandler.cs"
          - "Assets/Tests/EditMode/UiBakeHardeningV2/Stage2BakeCorrectness.cs"
        kind: code

  - id: "3.0"
    title: "Layer 3 — Scene-wire DB-driven (biggest v1 gap — bake emits scene-wire-plan + scene drift detector + canvas layering audit + adapter↔panel binding fixture)"
    exit: "bake emits `scene-wire-plan.yaml` declaring every (scene, canvas, slot) target + (controller, adapter, panel) binding. Scene drift detector flags hand-wired GO that contradicts plan. Canvas-layering audit enforces sortingOrder hierarchy. Adapter↔panel binding test fixture proves HUD buttons open the panel DB says. Stage 3 EditMode + PlayMode files flip green at task 3.0.5."
    red_stage_proof: |
      bake.run_all_panels()
      plan = load_yaml("Assets/Resources/UI/Generated/scene-wire-plan.yaml")
      assert plan["pause-menu"]["scene"] == "MainScene"
      assert plan["pause-menu"]["canvas"] == "MainMenuCanvas"
      assert plan["pause-menu"]["controller_bind"] == "PauseMenuController"

      scene = unity.load_scene("MainScene")
      drift = scene_drift_detector.scan(scene, plan)
      assert drift.legacy_gos == []
      assert drift.unwired_controllers == []
      assert drift.wrong_panel_targets == []

      hud_test = adapter_panel_binding.assert("hud-bar-budget-button", target_panel="budget-panel")
      assert hud_test.ok is True
    red_stage_proof_block:
      red_test_anchor: "unit-test:Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs::SceneWirePlan_DriftDetectorCatchesLegacyGO"
      target_kind: visibility_delta
      proof_artifact_id: Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs
      proof_status: failed_as_expected
    tasks:
      - id: 3.0.1
        title: "scene-wire-plan.yaml emit from bake"
        prefix: TECH
        depends_on: ["TECH-2.0.4"]
        digest_outline: "Add `catalog_panel_scene_targets` rows declaring (scene_name, canvas_path, slot_anchor, controller_type, adapter_type) per panel. Bake emits `Assets/Resources/UI/Generated/scene-wire-plan.yaml` consolidating all rows. Plan is canonical scene-wire source. Stage 3 test file created red — ScenePlan_EmitsFullManifest asserts plan covers every published panel."
        touched_paths:
          - "db/migrations/0149_catalog_panel_scene_targets.sql"
          - "Assets/Scripts/Editor/UiBake/SceneWirePlanEmitter.cs"
          - "Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs"
        kind: code
      - id: 3.0.2
        title: "scene drift detector — legacy GO + unwired controller + wrong-target sweep"
        prefix: TECH
        depends_on: ["TECH-3.0.1"]
        digest_outline: "Author `tools/scripts/validate-scene-wire-drift.mjs` reading scene YAML + scene-wire-plan.yaml. Flags: (a) legacy GOs in scene NOT in plan (e.g. SubtypePickerRoot, GrowthBudgetPanelRoot, city-stats-handoff), (b) plan-declared controller missing from scene, (c) HUD button onClick wired to wrong panel. Wire into validate:all. Test extends with SceneWirePlan_DriftDetectorCatchesLegacyGO + WrongPanelTarget_Flagged."
        touched_paths:
          - "tools/scripts/validate-scene-wire-drift.mjs"
          - "package.json"
          - "Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs"
        kind: code
      - id: 3.0.3
        title: "canvas layering audit — sortingOrder hierarchy enforced"
        prefix: TECH
        depends_on: ["TECH-3.0.2"]
        digest_outline: "Add `canvas_sorting_layers` column to scene-wire-plan. Audit script asserts: HUD < SubViews < Modals < Notifications < Cursor. Mitigates 'notifications hidden by subtype-picker' class. Test extends with CanvasLayering_NotificationsAboveHud."
        touched_paths:
          - "Assets/Scripts/Editor/UiBake/SceneWirePlanEmitter.cs"
          - "tools/scripts/validate-scene-wire-drift.mjs"
          - "Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs"
        kind: code
      - id: 3.0.4
        title: "adapter↔panel binding test fixture"
        prefix: TECH
        depends_on: ["TECH-3.0.3"]
        digest_outline: "Add `Assets/Tests/PlayMode/UiBakeHardeningV2/AdapterPanelBindingFixture.cs`. Per HUD button id, asserts onClick handler dispatches to DB-declared target panel. Catches 'hud-bar-budget-button opens GrowthBudgetPanelRoot instead of BudgetPanel'. Test extends with AdapterPanelBinding_HudButtonsHitDbDeclaredTargets."
        touched_paths:
          - "Assets/Tests/PlayMode/UiBakeHardeningV2/AdapterPanelBindingFixture.cs"
          - "Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs"
        kind: code
      - id: 3.0.5
        title: "legacy-GO purge planner + retire markers"
        prefix: TECH
        depends_on: ["TECH-3.0.4"]
        digest_outline: "Add `catalog_legacy_gos` rows declaring (scene, hierarchy_path, retired_by_panel, retire_after_stage). Drift detector flips legacy-GO-still-active to ERROR after retire_after_stage closes. Forces v3 repair extension to delete SubtypePickerRoot etc. Test extends with LegacyGoRetirement_BlocksAfterRetireStage → Stage 3 files flip green."
        touched_paths:
          - "db/migrations/0150_catalog_legacy_gos.sql"
          - "tools/scripts/validate-scene-wire-drift.mjs"
          - "Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs"
        kind: code

  - id: "4.0"
    title: "Layer 4 — Runtime contract tests + telemetry + bridge read_panel_state"
    exit: "PlayMode runtime tests assert: every published panel mounts to declared anchor, every action-id dispatches to registered handler, every bind subscriber count > 0 on mount. New bridge kind `read_panel_state(panel_slug)` returns live (mounted, child_count, bind_count, action_count). Action-fire telemetry logs every dispatch with marker. Stage 4 PlayMode file flips green at task 4.0.4."
    red_stage_proof: |
      unity.start_play_mode()
      for panel in catalog.list_published_panels():
        state = bridge.read_panel_state(panel.slug)
        assert state.mounted is True
        assert state.child_count == len(panel.children)
        assert state.bind_count >= panel.expected_bind_count

      result = bridge.dispatch_action("budget.open")
      assert result.handler_fired is True
      assert result.handler_class == "BudgetPanelController"
      log = bridge.get_action_log(since=test_start)
      assert any(e.action_id == "budget.open" and e.marker == "fired" for e in log)
    red_stage_proof_block:
      red_test_anchor: "unit-test:Assets/Tests/PlayMode/UiBakeHardeningV2/Stage4RuntimeContract.cs::EveryPanel_MountsToDeclaredAnchor"
      target_kind: tracer_verb
      proof_artifact_id: Assets/Tests/PlayMode/UiBakeHardeningV2/Stage4RuntimeContract.cs
      proof_status: failed_as_expected
    tasks:
      - id: 4.0.1
        title: "bridge kind: read_panel_state(panel_slug)"
        prefix: TECH
        depends_on: ["TECH-3.0.5"]
        digest_outline: "Add `read_panel_state` to AgentBridgeCommandRunner.Queries.cs. Returns `{mounted, anchor_path, child_count, bind_count, action_count, controller_alive}`. Register MCP tool. Stage 4 PlayMode test file created red — PanelState_ReturnsLiveCounts."
        touched_paths:
          - "Assets/Scripts/Editor/Bridge/AgentBridgeCommandRunner.Queries.cs"
          - "tools/mcp-ia-server/src/server-registrations.ts"
          - "Assets/Tests/PlayMode/UiBakeHardeningV2/Stage4RuntimeContract.cs"
        kind: code
      - id: 4.0.2
        title: "DB-derived contract test — every panel mounts + binds + dispatches"
        prefix: TECH
        depends_on: ["TECH-4.0.1"]
        digest_outline: "PlayMode test iterates `catalog.list_published_panels()` + asserts each panel's runtime state matches DB declaration via `read_panel_state`. Catches stage-9-class 'info-panel only shows black screen' (mounted=true but child_count=0). Test extends with EveryPanel_MountsToDeclaredAnchor + EveryPanel_HasNonZeroBindCount."
        touched_paths:
          - "Assets/Tests/PlayMode/UiBakeHardeningV2/Stage4RuntimeContract.cs"
        kind: code
      - id: 4.0.3
        title: "action-fire telemetry — every dispatch logged with handler class + ts"
        prefix: TECH
        depends_on: ["TECH-4.0.2"]
        digest_outline: "Patch `UiActionRegistry.Dispatch` to log `{action_id, handler_class, ts}` to `Diagnostics/action-fire.log`. Bridge kind `get_action_log(since)` exposes. Test extends with ActionDispatch_LogsHandlerClass."
        touched_paths:
          - "Assets/Scripts/Runtime/UI/UiActionRegistry.cs"
          - "Assets/Scripts/Editor/Bridge/AgentBridgeCommandRunner.Queries.cs"
          - "Assets/Tests/PlayMode/UiBakeHardeningV2/Stage4RuntimeContract.cs"
        kind: code
      - id: 4.0.4
        title: "synthetic click harness — bridge dispatch_action triggers full pipeline"
        prefix: TECH
        depends_on: ["TECH-4.0.3"]
        digest_outline: "Add `dispatch_action(action_id)` bridge kind that simulates click without OS event. Routes to `UiActionRegistry.Dispatch`. Test extends with SyntheticClick_FiresHandlerAndOpensTarget — clicks every HUD button via bridge, asserts target panel mounted via read_panel_state → Stage 4 file flips green."
        touched_paths:
          - "Assets/Scripts/Editor/Bridge/AgentBridgeCommandRunner.Mutations.cs"
          - "tools/mcp-ia-server/src/server-registrations.ts"
          - "Assets/Tests/PlayMode/UiBakeHardeningV2/Stage4RuntimeContract.cs"
        kind: code

  - id: "5.0"
    title: "Layer 5 — Visual diff + functional smoke + legacy-drift gates (B.7c/d/e ship-cycle hooks)"
    exit: "Per-panel screenshot baseline with SSIM tolerance. Functional smoke = synthetic click trace through every HUD button + sub-view. Three new sync ship-cycle gates B.7c (visual diff), B.7d (functional smoke), B.7e (legacy-drift sweep) hard-fail before stage commit. Stage 5 PlayMode file flips green at task 5.0.4."
    red_stage_proof: |
      bake.run_all_panels()
      unity.start_play_mode()
      for panel in catalog.list_published_panels():
        screenshot = unity.capture(panel.slug)
        baseline = load_baseline(panel.slug)
        diff = ssim(screenshot, baseline)
        assert diff > 0.95

      smoke = functional_smoke.run()
      assert smoke.dead_buttons == []
      assert smoke.wrong_targets == []

      drift = legacy_drift_sweep.run()
      assert drift.active_legacy_gos == []
    red_stage_proof_block:
      red_test_anchor: "unit-test:Assets/Tests/PlayMode/UiBakeHardeningV2/Stage5VisualFunctional.cs::FunctionalSmoke_NoDeadButtons"
      target_kind: tracer_verb
      proof_artifact_id: Assets/Tests/PlayMode/UiBakeHardeningV2/Stage5VisualFunctional.cs
      proof_status: failed_as_expected
    tasks:
      - id: 5.0.1
        title: "visual diff harness — per-panel screenshot + SSIM tolerance"
        prefix: TECH
        depends_on: ["TECH-4.0.4"]
        digest_outline: "Add `Assets/Resources/UI/Generated/VisualBaselines/{panel}.png`. PlayMode test captures each panel + SSIM-compares to baseline. Tolerance 0.95. First-author of new panel snapshots baseline. Test extends with VisualDiff_TolerableUnderTinyJitter."
        touched_paths:
          - "Assets/Scripts/Editor/UiBake/VisualBaselineCapture.cs"
          - "Assets/Tests/PlayMode/UiBakeHardeningV2/Stage5VisualFunctional.cs"
        kind: code
      - id: 5.0.2
        title: "functional smoke harness — synthetic click trace through every HUD button"
        prefix: TECH
        depends_on: ["TECH-5.0.1"]
        digest_outline: "PlayMode test loads MainScene + Play Mode + iterates HUD action ids + `dispatch_action(id)` + asserts `read_panel_state(declared_target).mounted=true` within 500ms. Flags dead-button (handler null) + wrong-target (different panel mounted). Test extends with FunctionalSmoke_NoDeadButtons + FunctionalSmoke_NoWrongTargets."
        touched_paths:
          - "Assets/Tests/PlayMode/UiBakeHardeningV2/Stage5VisualFunctional.cs"
        kind: code
      - id: 5.0.3
        title: "legacy-drift sweep — every catalog_legacy_gos row retired"
        prefix: TECH
        depends_on: ["TECH-5.0.2"]
        digest_outline: "PlayMode sweep — for every `catalog_legacy_gos` row past retire_after_stage, asserts `GameObject.Find(hierarchy_path) == null` in MainScene. Test extends with LegacyDrift_AllRetiredGOsAbsent (Stage 5 file only — Stage 3 file already green at 3.0.5)."
        touched_paths:
          - "Assets/Tests/PlayMode/UiBakeHardeningV2/Stage5VisualFunctional.cs"
        kind: code
      - id: 5.0.4
        title: "ship-cycle Pass B gates B.7c (visual diff) + B.7d (functional smoke) + B.7e (legacy drift)"
        prefix: TECH
        depends_on: ["TECH-5.0.3"]
        digest_outline: "Add 3 recipe steps to `tools/recipes/ship-cycle-pass-b.yaml` between B.7b (wait_asset_recompile) and B.8 (stage_commit). Each is SYNC gate — hard fail aborts before commit. Gates run only when `Assets/Resources/UI/Generated/**` touched in stage diff. Test extends with ShipCycleGates_AbortOnVisualMismatch → Stage 5 file flips green."
        touched_paths:
          - "tools/recipes/ship-cycle-pass-b.yaml"
          - "tools/scripts/recipe-engine/ship-cycle/run-visual-diff.sh"
          - "tools/scripts/recipe-engine/ship-cycle/run-functional-smoke.sh"
          - "tools/scripts/recipe-engine/ship-cycle/run-legacy-drift-sweep.sh"
          - "Assets/Tests/PlayMode/UiBakeHardeningV2/Stage5VisualFunctional.cs"
        kind: code

  - id: "6.0"
    title: "Layer 6 — Auditability (ia_ui_bake_history + ia_bake_diffs + per-panel web dashboard)"
    exit: "every bake writes audit row with (panel_slug, ts, bake_handler_version, diff_summary, commit_sha). Bake diffs persisted per panel for trend. web/ dashboard at /admin/ui-bake-history shows last N bakes per panel + drift over time. Stage 6 test file flips green at task 6.0.3."
    red_stage_proof: |
      bake.run("settings")
      history = db.query("SELECT * FROM ia_ui_bake_history WHERE panel_slug='settings' ORDER BY baked_at DESC LIMIT 1")
      assert history[0].bake_handler_version is not None
      assert history[0].diff_summary is not None
      assert history[0].commit_sha is not None

      resp = web.get("/admin/ui-bake-history?panel=settings")
      assert resp.status == 200
      assert "bake_handler_version" in resp.body
    red_stage_proof_block:
      red_test_anchor: "unit-test:tests/ui-bake-hardening-v2/stage6-auditability.test.mjs::BakeAudit_PersistsRowOnEveryBake"
      target_kind: tracer_verb
      proof_artifact_id: tests/ui-bake-hardening-v2/stage6-auditability.test.mjs
      proof_status: failed_as_expected
    tasks:
      - id: 6.0.1
        title: "ia_ui_bake_history table + ia_bake_diffs table + bake writer"
        prefix: TECH
        depends_on: ["TECH-5.0.4"]
        digest_outline: "Migration adds `ia_ui_bake_history (id PK, panel_slug, baked_at, bake_handler_version, diff_summary jsonb, commit_sha)` + `ia_bake_diffs (id PK, history_id FK, change_kind, child_kind, slug, before jsonb, after jsonb)`. Bake handler writes both per panel. Stage 6 test file created red — BakeAudit_PersistsRowOnEveryBake."
        touched_paths:
          - "db/migrations/0151_ia_ui_bake_history.sql"
          - "Assets/Scripts/Editor/Bridge/UiBakeHandler.cs"
          - "tests/ui-bake-hardening-v2/stage6-auditability.test.mjs"
        kind: code
      - id: 6.0.2
        title: "MCP read tool: ui_bake_history_query"
        prefix: TECH
        depends_on: ["TECH-6.0.1"]
        digest_outline: "Add `ui_bake_history_query(panel_slug, limit)` MCP read tool returning recent bakes + diffs. Test extends with MCPQuery_ReturnsRecentBakes."
        touched_paths:
          - "tools/mcp-ia-server/src/tools/ui-bake-history-query.ts"
          - "tools/mcp-ia-server/src/server-registrations.ts"
          - "tests/ui-bake-hardening-v2/stage6-auditability.test.mjs"
        kind: code
      - id: 6.0.3
        title: "web dashboard route /admin/ui-bake-history"
        prefix: TECH
        depends_on: ["TECH-6.0.2"]
        digest_outline: "Add Next.js route `web/app/admin/ui-bake-history/page.tsx` rendering bake history table + per-panel drift trend chart. Reuses existing admin auth. Test extends with WebDashboard_RendersHistoryRows → Stage 6 file flips green."
        touched_paths:
          - "web/app/admin/ui-bake-history/page.tsx"
          - "web/app/api/ui-bake-history/route.ts"
          - "tests/ui-bake-hardening-v2/stage6-auditability.test.mjs"
        kind: code

  - id: "7.0"
    title: "Retrospective + v3 repair extension trigger for cityscene-mainmenu-panel-rollout"
    exit: "Retrospective doc lists what each layer actually delivered vs designed + open follow-ups. Final task fires `/ship-plan` for cityscene-mainmenu-panel-rollout v3 repair extension (Stage 10.0–13.0 covering panel rewire, legacy purge, theme conformance, notifications layering). Stage 7 test file flips green at task 7.0.2."
    red_stage_proof: |
      retro = read("docs/ui-bake-pipeline-hardening-v2-retrospective.md")
      for layer in [1,2,3,4,5,6]:
        assert f"Layer {layer}" in retro
        assert "Delivered:" in retro
        assert "Open:" in retro

      backlog_row = backlog.get("TECH-CITYSCENE-V3-TRIGGER")
      assert backlog_row.task_kind == "tooling"
      assert "ship-plan cityscene-mainmenu-panel-rollout" in backlog_row.notes
    red_stage_proof_block:
      red_test_anchor: "unit-test:tests/ui-bake-hardening-v2/stage7-retrospective.test.mjs::Retrospective_CoversAllSixLayers"
      target_kind: tracer_verb
      proof_artifact_id: tests/ui-bake-hardening-v2/stage7-retrospective.test.mjs
      proof_status: failed_as_expected
    tasks:
      - id: 7.0.1
        title: "v2 retrospective doc — layer-by-layer delivered vs open"
        prefix: TECH
        depends_on: ["TECH-6.0.3"]
        digest_outline: "Author `docs/ui-bake-pipeline-hardening-v2-retrospective.md`. Sections per layer (1–6): `Delivered: {surfaces}`, `Tests: {file paths}`, `Open: {follow-ups}`, `Consumed by: {downstream plans}`. Stage 7 test file created red — Retrospective_CoversAllSixLayers."
        touched_paths:
          - "docs/ui-bake-pipeline-hardening-v2-retrospective.md"
          - "tests/ui-bake-hardening-v2/stage7-retrospective.test.mjs"
        kind: doc-only
      - id: 7.0.2
        title: "cityscene v3 repair extension trigger task (handoff to /ship-plan)"
        prefix: TECH
        depends_on: ["TECH-7.0.1"]
        digest_outline: "Author `docs/explorations/cityscene-mainmenu-panel-rollout-v3-repair.md` with handoff YAML frontmatter for `/ship-plan`. v3 stages: 10.0 (panel rewire — HUD buttons → correct DB-declared targets), 11.0 (legacy-GO purge — SubtypePickerRoot / GrowthBudgetPanelRoot / city-stats-handoff), 12.0 (theme conformance — pause-menu matches MainMenu aesthetic), 13.0 (canvas layering — notifications above subtype-picker). Each stage gated by v2 Layers 3+5 hooks. Test extends with V3TriggerTask_PointsAtShipPlan → Stage 7 file flips green → master plan ships."
        touched_paths:
          - "docs/explorations/cityscene-mainmenu-panel-rollout-v3-repair.md"
          - "tests/ui-bake-hardening-v2/stage7-retrospective.test.mjs"
        kind: doc-only
---

# UI bake-pipeline hardening v2 — closing the gaps v1 left open

Exploration. Seeds master plan `ui-bake-pipeline-hardening-v2`. Six-layer hardening covering every gap surfaced by `cityscene-mainmenu-panel-rollout` Stages 6.0–9.0 Editor QA findings (2026-05-10). Final stage = retrospective + trigger task for cityscene v3 repair extension.

Lineage: directly downstream of `bake-pipeline-hardening` (v1, closed 2026-05-09). V1 shipped infrastructure but cityscene tasks 6.0–9.0 NEVER consumed it — "Stage 5.5 prologue" recommended in `docs/cityscene-rollout-bake-hardening-impact.md` never ran. Result: Stages 6.0–9.0 shipped PASS but Editor QA found cascading failures (see Part 1).

Trigger: user verbatim 2026-05-10 — "Overall this is a very poorly implemented master plan. UI definitions surfaced the actual UI, and much of the claimed results are not visible or functional. […] File a new master plan ui-bake-pipeline-hardening-v2 covering Layers 1–6."

---

## Part 1 — Why v1 was insufficient (cityscene-mainmenu-panel-rollout Stage 6.0–9.0 QA findings)

User Editor-opened every closed stage 2026-05-10. Findings verbatim → root-cause map:

| Stage | User finding | Root cause | v1 surface that should have caught | Why didn't |
|---|---|---|---|---|
| 2.0 | Settings/Load sub-panels open but INCORRECTLY positioned + NO controls + NO back arrow | Slot anchor unwired in scene; widgets unrendered | SlotAnchorResolver + render-check (v1 Stage 2.0.2); navigation pattern outside v1 scope | Stage 5.5 prologue never ran → tasks 2.0+ didn't re-author through ui_from_db blueprint |
| 3.0 | Can't see ANY of the work | Scene wiring missing entirely | NONE — v1 had no scene-wire-plan concept | Layer 3 gap — scene wiring lived in agent's head, not DB |
| 4.0 | Can't see ANY of the work | Same as 3.0 | NONE | Same |
| 4.5 | Sub-views open but wrong-positioned + no controls | Same as 2.0 + canvas layering wrong | SlotAnchorResolver | Same — stale §Plan Digest, no v1 hooks consumed |
| 5.0 | Subtype-picker WHITE STRIP at bottom does nothing; legacy SubtypePickerRoot still active | Two GOs, no retire policy | NONE — v1 had no legacy-GO drift detector | Layer 3.5 gap — no `catalog_legacy_gos` retirement table |
| 6.0 | Stats panel doesn't open via hud button; city-stats-handoff legacy GO still in scene | HUD button onClick wired to wrong target; legacy GO not purged | NONE — v1 had no adapter↔panel binding fixture | Layer 3.4 + 3.5 gap combined |
| 7.0 | Budget panel doesn't open; hud-bar-budget-button opens GrowthBudgetPanelRoot (legacy); BudgetPanel exists but invisible even active | Wrong-target wiring + bake produced prefab but scene wire missed | NONE — v1 had no scene-wire-plan; no functional-smoke fixture | Layer 3 + Layer 5 gap |
| 8.0 | Pause-menu UI does NOT resemble MainMenu aesthetic; buttons DEAD | Theme tokens not applied; action handlers null | partial — v1 had token graph but no theme-conformance gate; UiActionRegistry contract not enforced | Layer 1.4 (token graph) + Layer 4 (runtime contract) gaps |
| 9.0 | Info-panel BLACK SCREEN; mini-map OK; notifications HIDDEN behind subtype-picker strip | Info-panel mounted but child_count=0; canvas sortingOrder wrong | NONE — v1 had no runtime contract test + no canvas-layering audit | Layer 3.3 + Layer 4 gap |

**Aggregate diagnosis.** V1 shipped 5 stages of bake-pipeline surfaces. None of them addressed **scene wiring** (which Canvas → which slot → which controller binds which panel) — yet that's where every cityscene failure lives. Bake produced correct prefabs; the prefabs never made it onto the right scene targets. **Layer 3 (Scene-wire DB-driven) is the keystone gap.**

Secondary diagnosis. V1 trusted agents to consume v1 surfaces. They didn't — they reused stale §Plan Digests authored pre-v1. **Layer 5 (B.7c/d/e ship-cycle gates) closes this by making consumption MECHANICAL, not authorial.**

---

## Part 2 — Six-layer model

| Layer | Concern | v1 status | v2 stages |
|---|---|---|---|
| **1. Author-time DB gates** | Bad data rejected at `catalog_panel_publish` mutation time | Partial (validate_panel_blueprint) | Stage 1.0 (5 gates) |
| **2. Bake-time correctness** | Prefab matches DB intent | Partial (KindRendererMatrix) | Stage 2.0 (4 tasks) |
| **3. Scene-wire DB-driven** | DB declares which scene / canvas / slot / controller | **MISSING entirely** | Stage 3.0 (5 tasks) — keystone |
| **4. Runtime contract** | Live state matches DB declaration | MISSING | Stage 4.0 (4 tasks) |
| **5. Visual+functional verify** | Pixels right AND clicks fire AND legacy purged | MISSING | Stage 5.0 (4 tasks) — ship-cycle gates B.7c/d/e |
| **6. Auditability** | Every bake leaves audit trail + dashboard | MISSING | Stage 6.0 (3 tasks) |
| **7. Retrospective + v3 trigger** | Lessons + handoff to repair extension | n/a | Stage 7.0 (2 tasks) |

---

## Part 3 — Design Expansion

### Plan Shape
- Shape: flat
- Rationale: Layer 1 → 6 mostly dependency-linear (Layer 3 scene wire depends on Layer 2 plugin pattern; Layer 4 runtime contract reads scene-wire-plan; Layer 5 ship-cycle gates need Layer 4 read_panel_state; Layer 6 audits all prior). Stage 7 retrospective + v3 trigger naturally tail.

### Core Prototype
- `verb:` close the false-PASS window where v1 surfaces shipped but downstream plans never consumed them, by making consumption mechanical at ship-cycle gate level
- `hardcoded_scope:` 6 layers + retrospective + v3 trigger. No archetype-driven dispatch refactor (deferred). No single-source codegen for ids (lint-only path stays).
- `stubbed_systems:` Stage 6 dashboard reuses existing admin auth; Stage 5 SSIM tolerance hardcoded 0.95 (revisit after first run)
- `throwaway:` initial visual baselines (will rebaseline as panels iterate); retrospective doc itself (history)
- `forward_living:` `catalog_panel_anchors`, `catalog_panel_scene_targets`, `catalog_legacy_gos`, `ia_ui_action_sinks`, `ia_ui_bake_history`, `ia_bake_diffs` tables; `IBakeHandler` plugin interface; `read_panel_state` + `dispatch_action` + `ui_bake_history_query` MCP kinds; scene-wire-plan.yaml schema; ship-cycle gates B.7c/d/e

### Iteration Roadmap

| Stage | Visibility delta |
|---|---|
| 1.0 | `catalog_panel_publish` rejects bad rows at MCP boundary — silent bake produces empty placeholders impossible |
| 2.0 | Bake fails loudly on empty child OR drift vs baseline; new kinds = new plugin classes, no switch edit |
| 3.0 | Scene-wire-plan emitted from DB → drift detector catches legacy GOs + wrong-target HUD buttons + missing controller binds |
| 4.0 | `read_panel_state` + synthetic click harness — agent verifies via bridge, not screenshot interpretation |
| 5.0 | Ship-cycle Pass B hard-fails before stage commit on visual mismatch / dead button / legacy-GO-still-active. Mechanical, not authorial. |
| 6.0 | Every bake leaves audit row; web dashboard shows drift over time |
| 7.0 | Retrospective doc lists what each layer delivered; v3 trigger task fires `/ship-plan cityscene-mainmenu-panel-rollout-v3-repair` so cityscene gets repair extension |

### Chosen Approach
**Six-layer keystone hardening** — Layer 3 (scene-wire DB-driven) is the keystone v1 missed. Without it, bake→prefab works but never reaches the right scene target. Layer 5 (ship-cycle B.7c/d/e gates) is the enforcement teeth — closes the false-PASS window by making layer consumption mechanical at recipe step level, not at agent authorship level. Layers 1, 2, 4, 6 reinforce. Stage 7 closes the lineage loop by triggering the cityscene repair extension that consumes all 6 layers.

### Architecture Decision
**DEC-A16-equivalent (deferred MCP write — Phase 2.5 inline record):**
- **Problem:** V1 hardening shipped surfaces but downstream cityscene stages didn't consume them; result = Editor QA found cascading failures (legacy GOs active, wrong-target HUD buttons, dead pause-menu, hidden notifications).
- **Chosen:** Six-layer model with Layer 3 (Scene-wire DB-driven) as keystone and Layer 5 (ship-cycle Pass B B.7c/d/e gates) as enforcement teeth. Consumption becomes mechanical at recipe-step level, not authorial.
- **Alternatives rejected:** (1) Re-run v1's Stage 5.5 prologue manually — fragile, requires agent discipline that v1 already proved unreliable. (2) Single mega-validator at validate:all — won't catch runtime-only defects (dead button / wrong-target / hidden notifications). (3) Defer scene-wire to v3 — keeps the keystone gap open.
- **Consequences:** New tables (catalog_panel_anchors, catalog_panel_scene_targets, catalog_legacy_gos, ia_ui_action_sinks, ia_ui_bake_history, ia_bake_diffs). New MCP kinds (read_panel_state, dispatch_action, ui_bake_history_query). Three new ship-cycle Pass B recipe steps (B.7c/d/e). New web admin route. Adds ~3–5 min per stage commit (visual diff + functional smoke + legacy drift sweep run pre-commit). False-PASS window closes — agent verdict via bridge, not screenshot interpretation.
- **Affected `arch_surfaces[]`:** `architecture/asset-pipeline-standard`, `architecture/interchange`, `architecture/data-flows`, `ui-design-system`, `catalog-architecture`, `ship-cycle-recipe`.
- **MCP write status:** deferred to ship-plan phase — `arch_decision_write` + `cron_arch_changelog_append_enqueue` + `arch_drift_scan` to fire when master plan author runs.

### Architecture

```mermaid
flowchart TD
  subgraph L1[Layer 1 — Author gates at catalog_panel_publish]
    A1[archetype×kind coverage]
    A2[action-id sink unique]
    A3[bind-id contract]
    A4[token graph resolve]
    A5[view-slot anchor required-by]
  end
  subgraph L2[Layer 2 — Bake-time]
    B1[non-empty child assert]
    B2[IBakeHandler[] plugins]
    B3[bake diff vs baseline]
    B4[meta write proof]
  end
  subgraph L3[Layer 3 — Scene-wire DB-driven keystone]
    C1[scene-wire-plan.yaml]
    C2[scene drift detector]
    C3[canvas layering audit]
    C4[adapter↔panel fixture]
    C5[legacy-GO retire]
  end
  subgraph L4[Layer 4 — Runtime contract]
    D1[read_panel_state bridge]
    D2[DB-derived contract tests]
    D3[action-fire telemetry]
    D4[dispatch_action synthetic click]
  end
  subgraph L5[Layer 5 — Visual + functional + drift gates]
    E1[visual SSIM diff]
    E2[functional smoke]
    E3[legacy drift sweep]
    E4[ship-cycle B.7c/d/e]
  end
  subgraph L6[Layer 6 — Audit]
    F1[ia_ui_bake_history]
    F2[ui_bake_history_query MCP]
    F3[web admin dashboard]
  end
  A1 --> B1
  A5 --> C1
  B3 --> E1
  C1 --> D1
  D1 --> D4
  D4 --> E2
  C5 --> E3
  E4 -.gate.-> shipcommit[(stage commit)]
  B1 --> F1
```

### Subsystem Impact

| Subsystem | Nature | Invariant risk | Breaking? | Mitigation |
|---|---|---|---|---|
| `catalog_panel_publish` MCP | Adds 5 publish-time gates | None | Additive (rejects bad rows only) | Backfill existing published rows pre-rollout |
| `UiBakeHandler.cs` | Plugin pattern + non-empty assert + meta proof | None | Additive (existing kinds wrapped) | `BakeException` only for new violations |
| New tables | 6 new tables (anchors, scene_targets, legacy_gos, action_sinks, bake_history, bake_diffs) | None | Additive | Migrations 0147–0151 |
| Scene wire (NEW concept) | Bake emits scene-wire-plan.yaml | None | Additive | Drift detector lint-only Stage 3.0.2 |
| Bridge MCP | New kinds: read_panel_state, dispatch_action, ui_bake_history_query, get_action_log | None | Additive | Register in server-registrations.ts |
| Ship-cycle Pass B recipe | 3 new gate steps B.7c/d/e | None | Additive (sync hard fail) | Gates run only when `Assets/Resources/UI/Generated/**` touched |
| web/ admin | New `/admin/ui-bake-history` route | None | Additive | Reuses existing admin auth |
| Unity invariants | Bake handler edits; new runtime contract tests | **Inv. #3** — no `FindObjectOfType` in `Update`/per-frame | Additive | Runtime contract test runs at PlayMode start only, never per-frame; read_panel_state queries at mount, caches |

### Implementation Points (collapsed — see frontmatter `tasks[]` for full digest outlines)

- **Stage 1.0** — 5 tasks wiring author-time gates into `catalog_panel_publish` mutation
- **Stage 2.0** — 4 tasks for bake-time correctness (non-empty + plugin pattern + diff + meta)
- **Stage 3.0** — 5 tasks for scene-wire-plan + drift detector + canvas layering + adapter fixture + legacy-GO retire
- **Stage 4.0** — 4 tasks for runtime contract (read_panel_state + contract tests + telemetry + synthetic click)
- **Stage 5.0** — 4 tasks for visual diff + functional smoke + legacy drift + ship-cycle B.7c/d/e gates
- **Stage 6.0** — 3 tasks for audit history + MCP query tool + web dashboard
- **Stage 7.0** — 2 tasks for retrospective doc + cityscene v3 trigger task

**Deferred / out of scope:** archetype-driven dispatch refactor (replace plugin pattern with catalog_entity row); single-source codegen for ids (lint-only path stays); pixel-perfect baselines (SSIM 0.95 tolerance suffices); cityscene v3 implementation (handoff via Stage 7.0.2 trigger task — separate master plan).

### TDD Spec — Incremental red→green per Stage

Same protocol as v1 — one test file per stage, grown task-by-task. Stage close = file fully green. Master-plan close = `npm run test:ui-bake-hardening-v2` (Node) + `unity:testmode-batch --filter UiBakeHardeningV2.*` (Unity) all green.

| Stage | Test file | Runner |
|---|---|---|
| 1.0 | `tests/ui-bake-hardening-v2/stage1-author-gates.test.mjs` | Node --test |
| 2.0 | `Assets/Tests/EditMode/UiBakeHardeningV2/Stage2BakeCorrectness.cs` | Unity EditMode |
| 3.0 | `Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs` | Unity EditMode |
| 4.0 | `Assets/Tests/PlayMode/UiBakeHardeningV2/Stage4RuntimeContract.cs` | Unity PlayMode |
| 5.0 | `Assets/Tests/PlayMode/UiBakeHardeningV2/Stage5VisualFunctional.cs` | Unity PlayMode |
| 6.0 | `tests/ui-bake-hardening-v2/stage6-auditability.test.mjs` | Node --test |
| 7.0 | `tests/ui-bake-hardening-v2/stage7-retrospective.test.mjs` | Node --test |

---

## Part 4 — Cityscene v3 repair extension (Stage 7.0.2 handoff)

After v2 ships, Stage 7.0.2 triggers `/ship-plan cityscene-mainmenu-panel-rollout-v3-repair`. v3 stages:

| Stage | Concern | Consumes v2 layer |
|---|---|---|
| 10.0 | HUD button rewire — every button → DB-declared target panel | L3 (scene-wire-plan) + L5 (functional smoke) |
| 11.0 | Legacy-GO purge — SubtypePickerRoot / GrowthBudgetPanelRoot / city-stats-handoff retired | L3 (legacy-GO retire) + L5 (drift sweep) |
| 12.0 | Theme conformance — pause-menu matches MainMenu (tokens + visual baseline) | L1 (token graph) + L5 (visual diff) |
| 13.0 | Canvas layering — notifications above subtype-picker; info-panel renders all children | L3 (canvas audit) + L4 (runtime contract) |

Each v3 stage is GREEN-AT-COMMIT only when v2 ship-cycle B.7c/d/e gates pass. False-PASS window closed by construction.

---

## Changelog

- 2026-05-10 — initial v2 draft. Captures cityscene Stage 6.0–9.0 Editor QA findings + six-layer model + handoff YAML for ship-plan + Stage 7.0.2 v3 trigger task. Lineage: v1 `bake-pipeline-hardening` closed 2026-05-09; cityscene-mainmenu-panel-rollout v2 closed 2026-05-09 but Editor QA 2026-05-10 found cascading failures because Stage 5.5 prologue never ran.
