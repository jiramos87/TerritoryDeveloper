---
slug: cityscene-mainmenu-panel-rollout
parent_plan_id: null
parent_rationale: >
  v3 extension of cityscene-mainmenu-panel-rollout (same slug, new version).
  v1/v2 shipped PASS but Editor QA revealed four structural gaps —
  wrong-target HUD buttons, live legacy GOs, pause-menu theme divergence,
  canvas z-order bug. v3 fixes all four. Gated by ui-bake-pipeline-hardening-v2
  Layers 3+5 — v3 cannot ship green unless scene-wire-plan drift detector +
  B.7c/d/e gates pass on every stage commit. Lineage parent: slug
  `cityscene-mainmenu-panel-rollout` version=2 (closed 2026-05-11).
target_version: 3
stages:
  - id: "10.0"
    title: "HUD button rewire — every button fires DB-declared target panel"
    exit: >-
      All HUD buttons dispatch to panel declared in catalog_entity.params_json
      (target_panel_slug). scene-wire-plan.yaml is authoritative target list.
      scene-drift-detector reports zero wrong-target entries.
      Layer 5 B.7d functional smoke passes end-to-end.
    red_stage_proof: |
      Layer 3 drift detector (TECH-28366) already fires on wrong-target HUD buttons.
      Pre-10.0: at least one HUD button opens wrong panel. Post-10.0: drift detector
      reports zero wrong-target entries AND B.7d functional smoke passes every button.
    red_stage_proof_block:
      red_test_anchor: "tests/cityscene-v3-repair/stage10-hud-rewire.test.mjs::HudButton_FiresDB_DeclaredTarget"
      target_kind: "unit"
      proof_artifact_id: "stage10-hud-rewire.test.mjs"
      proof_status: "failed_as_expected"
    tasks:
      - id: "10.0.1"
        title: "Read scene-wire-plan.yaml; emit corrected button binding list"
        prefix: TECH
        digest_outline: >-
          Parse scene-wire-plan.yaml emitted by Layer 3 bake. Build corrected
          (button_go, target_panel_slug) mapping from DB declarations. Output
          as migration patch artifact for T10.0.2.
        kind: doc-only
      - id: "10.0.2"
        title: "Patch UiActionDispatcher targets to match DB declarations"
        prefix: TECH
        depends_on: ["10.0.1"]
        digest_outline: >-
          Update every HUD button GO in CityScene so its UiActionDispatcher
          target_panel_slug matches catalog_entity.params_json. Patch driven by
          artifact from T10.0.1.
        kind: code
        touched_paths:
          - Assets/Scenes/CityScene.unity
      - id: "10.0.3"
        title: "scene-drift-detector clean run (zero wrong-target)"
        prefix: TECH
        depends_on: ["10.0.2"]
        digest_outline: >-
          Run scene-drift-detector (Layer 3 TECH-28366) against patched scene.
          Assert output reports zero wrong-target entries. Fix any remaining
          targets until clean.
        kind: code
      - id: "10.0.4"
        title: "Stage 10 test file GREEN — B.7d functional smoke passes"
        prefix: TECH
        depends_on: ["10.0.3"]
        digest_outline: >-
          Create tests/cityscene-v3-repair/stage10-hud-rewire.test.mjs.
          Assert HudButton_FiresDB_DeclaredTarget for every published HUD button.
          B.7d functional smoke must pass as part of ship-cycle Pass B.
        kind: code
        touched_paths:
          - tests/cityscene-v3-repair/stage10-hud-rewire.test.mjs

  - id: "11.0"
    title: "Legacy-GO purge — remove SubtypePickerRoot / GrowthBudgetPanelRoot / city-stats-handoff"
    exit: >-
      SubtypePickerRoot, GrowthBudgetPanelRoot, city-stats-handoff GOs absent from CityScene.
      catalog_legacy_gos table empty (all rows purged + soft-deleted).
      Layer 5 B.7e legacy-drift sweep reports zero live legacy GOs.
    red_stage_proof: |
      Layer 3 purge planner (TECH-28369) marks GOs for retirement but does not delete them.
      Pre-11.0: catalog_legacy_gos has active rows. Post-11.0: every row retired + GOs
      absent from scene. B.7e sweep reports zero live legacy entries.
    red_stage_proof_block:
      red_test_anchor: "tests/cityscene-v3-repair/stage11-legacy-purge.test.mjs::LegacyGO_AbsentFromScene"
      target_kind: "unit"
      proof_artifact_id: "stage11-legacy-purge.test.mjs"
      proof_status: "failed_as_expected"
    tasks:
      - id: "11.0.1"
        title: "Delete SubtypePickerRoot GO from CityScene"
        prefix: TECH
        digest_outline: >-
          Remove SubtypePickerRoot GameObject and all child GOs from CityScene.
          Confirm no remaining scripts reference it by name. Update catalog_legacy_gos
          row to status=retired.
        kind: code
        touched_paths:
          - Assets/Scenes/CityScene.unity
      - id: "11.0.2"
        title: "Delete GrowthBudgetPanelRoot GO from CityScene"
        prefix: TECH
        depends_on: ["11.0.1"]
        digest_outline: >-
          Remove GrowthBudgetPanelRoot and children from CityScene. Update
          catalog_legacy_gos row to status=retired.
        kind: code
        touched_paths:
          - Assets/Scenes/CityScene.unity
      - id: "11.0.3"
        title: "Delete city-stats-handoff GO from CityScene"
        prefix: TECH
        depends_on: ["11.0.2"]
        digest_outline: >-
          Remove city-stats-handoff and children from CityScene. Update
          catalog_legacy_gos row to status=retired.
        kind: code
        touched_paths:
          - Assets/Scenes/CityScene.unity
      - id: "11.0.4"
        title: "Purge catalog_legacy_gos rows; B.7e sweep green"
        prefix: TECH
        depends_on: ["11.0.3"]
        digest_outline: >-
          Confirm all catalog_legacy_gos rows are retired. Create
          tests/cityscene-v3-repair/stage11-legacy-purge.test.mjs.
          B.7e legacy-drift sweep must report zero live entries.
        kind: code
        touched_paths:
          - tests/cityscene-v3-repair/stage11-legacy-purge.test.mjs

  - id: "12.0"
    title: "Theme conformance — pause-menu matches MainMenu aesthetic"
    exit: >-
      pause-menu panel screenshot SSIM >= 0.95 vs new MainMenu-aligned baseline.
      Every pause-menu DS-token resolves to ui_design_tokens row (Layer 1 gate passes).
      B.7c visual diff gate passes on stage commit.
    red_stage_proof: |
      pause-menu params_json references tokens not in DefaultUiTheme set or missing
      entries that MainMenu uses. Pre-12.0: SSIM below threshold vs MainMenu baseline.
      Post-12.0: all tokens resolve; SSIM >= 0.95; B.7c passes.
    red_stage_proof_block:
      red_test_anchor: "tests/cityscene-v3-repair/stage12-theme-conformance.test.mjs::PauseMenu_TokensResolve_MainMenuAligned"
      target_kind: "unit"
      proof_artifact_id: "stage12-theme-conformance.test.mjs"
      proof_status: "failed_as_expected"
    tasks:
      - id: "12.0.1"
        title: "Audit pause-menu params_json vs DefaultUiTheme token set"
        prefix: TECH
        digest_outline: >-
          Run Layer 1 token-reference gate against pause-menu catalog entry.
          Identify which DS-tokens are missing, misnamed, or diverge from
          MainMenu aesthetic. Output diff list for T12.0.2.
        kind: doc-only
      - id: "12.0.2"
        title: "Update pause-menu catalog entry — apply MainMenu-aligned tokens"
        prefix: TECH
        depends_on: ["12.0.1"]
        digest_outline: >-
          Update pause-menu catalog_entity.params_json to use DefaultUiTheme
          token set matching MainMenu. Layer 1 token-reference gate must pass
          after update.
        kind: code
      - id: "12.0.3"
        title: "Re-bake pause-menu; reset visual diff baseline"
        prefix: TECH
        depends_on: ["12.0.2"]
        digest_outline: >-
          Trigger UiBakeHandler.Apply for pause-menu. Reset Layer 2 golden
          baseline to new bake output. Capture new screenshot baseline for
          Layer 5 B.7c SSIM check.
        kind: code
      - id: "12.0.4"
        title: "Stage 12 test file GREEN — B.7c passes"
        prefix: TECH
        depends_on: ["12.0.3"]
        digest_outline: >-
          Create tests/cityscene-v3-repair/stage12-theme-conformance.test.mjs.
          Assert PauseMenu_TokensResolve_MainMenuAligned. B.7c visual diff
          must pass as part of ship-cycle Pass B.
        kind: code
        touched_paths:
          - tests/cityscene-v3-repair/stage12-theme-conformance.test.mjs

  - id: "13.0"
    title: "Canvas layering — notifications above subtype-picker"
    exit: >-
      notifications canvas sortingOrder > subtype-picker canvas sortingOrder in scene.
      Layer 3 canvas layering audit reports no hierarchy violations.
      B.7c visual diff passes with notifications-above-picker baseline.
    red_stage_proof: |
      Layer 3 canvas layering audit (TECH-28367) already detects sortingOrder violations.
      Pre-13.0: audit reports notifications canvas sortingOrder <= subtype-picker.
      Post-13.0: audit reports zero hierarchy violations; B.7c passes.
    red_stage_proof_block:
      red_test_anchor: "tests/cityscene-v3-repair/stage13-canvas-layering.test.mjs::Notifications_SortingOrder_AboveSubtypePicker"
      target_kind: "unit"
      proof_artifact_id: "stage13-canvas-layering.test.mjs"
      proof_status: "failed_as_expected"
    tasks:
      - id: "13.0.1"
        title: "Read canvas layering audit report; identify sortingOrder fix"
        prefix: TECH
        digest_outline: >-
          Run Layer 3 canvas layering audit (TECH-28367) output. Identify
          notifications canvas and subtype-picker canvas current sortingOrder
          values. Determine correct assignments so notifications > subtype-picker.
        kind: doc-only
      - id: "13.0.2"
        title: "Assign corrected sortingOrder values in CityScene"
        prefix: TECH
        depends_on: ["13.0.1"]
        digest_outline: >-
          Update Canvas sortingOrder on notifications GO and subtype-picker GO
          in CityScene so notifications renders above subtype-picker. Match
          scene-wire-plan.yaml declared hierarchy.
        kind: code
        touched_paths:
          - Assets/Scenes/CityScene.unity
      - id: "13.0.3"
        title: "Re-run canvas layering audit — zero violations"
        prefix: TECH
        depends_on: ["13.0.2"]
        digest_outline: >-
          Re-run Layer 3 canvas layering audit against patched scene. Assert
          zero hierarchy violations. Fix any remaining issues until clean.
        kind: code
      - id: "13.0.4"
        title: "Stage 13 test file GREEN — B.7c passes; master plan ships"
        prefix: TECH
        depends_on: ["13.0.3"]
        digest_outline: >-
          Create tests/cityscene-v3-repair/stage13-canvas-layering.test.mjs.
          Assert Notifications_SortingOrder_AboveSubtypePicker. B.7c visual
          diff must pass. This is the final stage — master plan ships on close.
        kind: code
        touched_paths:
          - tests/cityscene-v3-repair/stage13-canvas-layering.test.mjs
---

# cityscene MainMenu panel rollout v3 — repair extension

Handoff from `ui-bake-pipeline-hardening-v2` Stage 7.0.2 (TECH-28382).

See frontmatter for authoritative stage definitions, dependencies, and gate contract.

## Context

v1 (`cityscene-mainmenu-panel-rollout`) shipped Stages 1.0–9.0 with passing ship-cycle verdicts
but Editor QA revealed:

1. HUD buttons firing wrong-target panels (now detectable via Layer 3 drift detector).
2. Legacy GOs (`SubtypePickerRoot`, `GrowthBudgetPanelRoot`, `city-stats-handoff`) still live.
3. Pause-menu visual language diverged from MainMenu aesthetic.
4. Notifications canvas `sortingOrder` below subtype-picker — overlapping z-order bug.

v2 (`ui-bake-pipeline-hardening-v2`) added the detection machinery. v3 does the repair.

## Gate contract

Every stage in v3 must pass Layer 5 gates B.7c (visual diff), B.7d (functional smoke),
B.7e (legacy-drift sweep) before stage commit. These gates are hard-wired in ship-cycle
Pass B via ui-bake-pipeline-hardening-v2 Stage 5. v3 cannot ship green without v2 deployed.

## Execution order

Stages 10.0 → 11.0 → 12.0 → 13.0. Stages 11.0 (purge) safe only after 10.0 (rewire)
confirms buttons fire correctly — avoid purging a GO that a still-misconfigured button references.
