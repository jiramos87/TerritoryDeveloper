---
slug: cityscene-mainmenu-panel-rollout
parent_plan_id: null
parent_rationale: >
  No live master plan owns this rollout. game-ui-catalog-bake is the upstream
  infra plan (DB-first bake + bake handler + 2 panels published). This rollout
  CONSUMES that infra. New plan = peer of game-ui-catalog-bake, not a child stage.
  Note: 9 stages exceeds the soft ≤6-stage cap for new plans (Q1 sequential lock
  forbids splitting into MainMenu + CityScene plans). Cap break is an explicit
  authoring decision, NOT a parent_plan_id grandfather; downstream /ship-final
  validators do not gate on stage count.
target_version: 2
tracks:
  - id: A
    name: MainMenu
    sequence: first
    waves: [A0, A1, A2, A3]
    stage_ids: [1.0, 2.0, 3.0, 4.0]
  - id: B
    name: CityScene
    sequence: after-MainMenu-complete
    waves: [B1, B2, B3, B4, B5]
    stage_ids: [5.0, 6.0, 7.0, 8.0, 9.0]
stages:
  - id: "1.0"
    title: "Wave A0 — MainMenu-only action+bind registry + bake-time validator"
    exit: >-
      UiActionRegistry + UiBindRegistry C# classes ship; 32 MainMenu-only actions + 12 binds
      registered; bake-time drift validator runs on validate:all and exits 1 on registry drift;
      action_registry_list + bind_registry_list MCP slices return live registration log.
    red_stage_proof: |
      Editor-side bake-time drift validator walks every published `panel_child.params_json`
      and asserts each `action` / `bind` / `visible_bind` / `enabled_bind` / `slot_bind`
      reference resolves against UiActionRegistry / UiBindRegistry. Pre-A0: no validator
      exists; drift undetected. Post-A0: deliberate typo in a panel_child action ref
      (e.g. `mainmenu.continueX`) → `validate:all` exits 1 with the unresolved ref name.
    red_stage_proof_block:
      red_test_anchor: "n/a"
      target_kind: "design_only"
      proof_artifact_id: "n/a"
      proof_status: "not_applicable"
    tasks:
      - id: "1.0.1"
        title: "UiActionRegistry + UiBindRegistry C# classes"
        prefix: TECH
        digest_outline: >-
          Add Assets/Scripts/UI/Registry/UiActionRegistry.cs + UiBindRegistry.cs MonoBehaviours
          per Wave A0 lock shape (Register / Dispatch / Set / Get / Subscribe / ListRegistered).
          ServiceLocator-registered. No external consumers yet — registry shell only.
        kind: code
        touched_paths:
          - Assets/Scripts/UI/Registry/UiActionRegistry.cs
          - Assets/Scripts/UI/Registry/UiBindRegistry.cs
      - id: "1.0.2"
        title: "Bake-time action+bind drift validator (Editor menu + validate:all hook)"
        prefix: TECH
        depends_on: ["1.0.1"]
        digest_outline: >-
          Add Editor menu `Territory > UI > Validate Action+Bind Drift` walking published
          panel_child.params_json. Wire into validate:all so drift exits 1. Reuses agent_bridge
          command surface for headless invocation.
        kind: code
        touched_paths:
          - Assets/Scripts/Editor/UI/ActionBindDriftValidator.cs
          - tools/scripts/validate-action-bind-drift.mjs
          - package.json
      - id: "1.0.3"
        title: "action_registry_list + bind_registry_list MCP slices"
        prefix: TECH
        depends_on: ["1.0.1"]
        digest_outline: >-
          Register two read-only MCP tools backed by Editor bridge command + DB-backed
          registration log table. Returns action ids + handler-bound flag, bind ids + values
          + subscriber counts.
        kind: mcp-only
        touched_paths:
          - tools/mcp-ia-server/src/index.ts
          - db/migrations/0114_ui_registry_log.sql
      - id: "1.0.4"
        title: "Enumerate 32 MainMenu actions + 12 binds in registry seed"
        prefix: TECH
        depends_on: ["1.0.1"]
        digest_outline: >-
          Lock action ids (main-menu 7 + new-game-form 4 + settings-view 12 + save-load-view 9)
          + bind ids (mainmenu 6 + newgame 3 + saveload 3) per Implementation Points body.
          Stub handlers raise `NotImplementedException` — wired in Wave A1+.
        kind: code
        touched_paths:
          - Assets/Scripts/UI/Registry/MainMenuRegistrySeed.cs

  - id: "2.0"
    title: "Wave A1 — main-menu root panel (5 buttons + view-slot + branding)"
    exit: >-
      main-menu panel published in catalog (1 panel + 10 children). MainMenuController refactored
      to bind-dispatcher consumer. Player launches game → 5 functional buttons render; Continue
      greys with no save; Quit shows 3-second confirm; sub-views show "coming next wave"
      placeholder via view-slot.
    red_stage_proof: |
      Pre-A1: no baked main-menu panel; legacy `loadCityPanel` / `optionsPanel` serialized slots
      drive UI. Post-A1: visibility delta — boot scene `MainMenu.unity`, observe baked
      `MainMenuPanelRoot` GameObject under Canvas; click each of 5 buttons; Continue button
      `disabled` state matches `GameSaveManager.HasAnySave()` return; Quit confirm-button
      counts down 3 seconds.
    red_stage_proof_block:
      red_test_anchor: "n/a"
      target_kind: "design_only"
      proof_artifact_id: "n/a"
      proof_status: "not_applicable"
    tasks:
      - id: "2.0.1"
        title: "DB seed migration — main-menu panel + 10 panel_child rows"
        prefix: TECH
        depends_on: ["1.0.4"]
        digest_outline: >-
          Reserve next migration index (0115). Seed catalog_entity(kind=panel,slug=main-menu) +
          panel_detail(layout_template=fullscreen-stack, params_json carries bg_color_token) +
          10 panel_child rows (5 buttons + 1 confirm-button + 1 back-button + 3 labels +
          1 view-slot). Action / bind refs resolve against Wave A0 registry.
        kind: code
        touched_paths:
          - db/migrations/0115_seed_main_menu.sql
      - id: "2.0.2"
        title: "view-slot + confirm-button archetypes (catalog + bake-handler)"
        prefix: TECH
        depends_on: ["2.0.1"]
        digest_outline: >-
          Add 2 archetype catalog rows (view-slot, confirm-button) + 2 case arms in
          UiBakeHandler.Archetype.cs. view-slot renders one of N declared sub-views by
          enum-bind; confirm-button is button variant with N-second inline countdown.
        kind: code
        touched_paths:
          - db/migrations/0116_seed_view_slot_confirm_button_archetypes.sql
          - Assets/Scripts/Editor/Bridge/UiBakeHandler.Archetype.cs
      - id: "2.0.3"
        title: "MainMenuController refactor — drop legacy slots; subscribe contentScreen"
        prefix: TECH
        depends_on: ["2.0.2"]
        digest_outline: >-
          Drop loadCityPanel + optionsPanel SerializeField slots; subscribe to
          `mainmenu.contentScreen` enum bind; register handlers for 7 actions; drive
          `mainmenu.continue.disabled` from GameSaveManager.HasAnySave().
        kind: code
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/MainMenuController.cs
      - id: "2.0.4"
        title: "GameSaveManager APIs — HasAnySave + GetMostRecentSave"
        prefix: TECH
        digest_outline: >-
          Add HasAnySave() : bool + GetMostRecentSave() : SaveFileMeta? to GameSaveManager.
          Drives Continue button disabled-state + auto-load target. Existing save discovery
          logic factored without behavior change.
        kind: code
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/GameSaveManager.cs
      - id: "2.0.5"
        title: "Scene-wire MainMenu.unity + new tokens (color.bg.menu + size.text.title-display)"
        prefix: TECH
        depends_on: ["2.0.3"]
        digest_outline: >-
          Add MainMenuPanelRoot GameObject under existing Canvas in MainMenu.unity; baked
          prefab parented. Publish 2 ui_tokens. Keep useBakedUi flag for transition (flip
          after Wave A3 verification).
        kind: code
        touched_paths:
          - Assets/Scenes/MainMenu.unity
          - db/migrations/0117_seed_main_menu_tokens.sql

  - id: "3.0"
    title: "Wave A2 — new-game-form + settings-view (functional day-one)"
    exit: >-
      new-game-form + settings-view panels published; both mount into main-menu view-slot.
      Q3 functional day-one lock: audio sliders persist via PlayerPrefs/audio mixer; Reset
      restores defaults; Start launches CityScene with chosen budget + cityName + seed.
    red_stage_proof: |
      Pre-A2: NewGameScreenDataAdapter exposes 2 numeric sliders + scenario toggles;
      SettingsScreenDataAdapter ships 6 controls. Post-A2: visibility delta — open
      MainMenu, click New Game → 3-card map picker + 3-chip budget picker + name-input
      pre-filled with random Spanish name; click Settings → 9 controls all functional
      (drag SFX slider → AudioMixer level changes immediately + persists across launch).
    red_stage_proof_block:
      red_test_anchor: "n/a"
      target_kind: "design_only"
      proof_artifact_id: "n/a"
      proof_status: "not_applicable"
    tasks:
      - id: "3.0.1"
        title: "DB seed migration — new-game-form + settings-view panels + city-name-pool-es"
        prefix: TECH
        depends_on: ["2.0.5"]
        digest_outline: >-
          Reserve next migration index. Seed 2 panels with host_slots wiring (settings-view
          host_slots=[main-menu-content-slot, pause-menu-content-slot]). Author 100 fictional
          Spanish names into NEW pool kind `string-pool` (lang=es).
        kind: code
        touched_paths:
          - db/migrations/0118_seed_new_game_settings_panels.sql
          - db/migrations/0119_seed_city_name_pool_es.sql
      - id: "3.0.2"
        title: "7 new archetypes — card-picker, chip-picker, text-input, toggle-row, slider-row, dropdown-row, section-header"
        prefix: TECH
        depends_on: ["3.0.1"]
        digest_outline: >-
          7 archetype catalog rows + 7 case arms in UiBakeHandler.Archetype.cs. Each archetype
          ships archetype-catalog row + IR DTO + JsonUtility round-trip test.
        kind: code
        touched_paths:
          - db/migrations/0120_seed_form_archetypes.sql
          - Assets/Scripts/Editor/Bridge/UiBakeHandler.Archetype.cs
      - id: "3.0.3"
        title: "NewGameScreenDataAdapter + SettingsScreenDataAdapter refactor"
        prefix: TECH
        depends_on: ["3.0.2"]
        digest_outline: >-
          NewGameScreenDataAdapter: drop sliders + scenarios; add card/chip/input bind
          subscriptions. SettingsScreenDataAdapter: add SFX slider + monthly-budget-notif
          toggle + auto-save toggle + Reset-to-defaults footer. Volume mapping
          LinearToDecibel(percent/100f) for 3 sliders. Add NEW PlayerPrefs keys
          MonthlyBudgetNotificationsKey + AutoSaveKey.
        kind: code
        touched_paths:
          - Assets/Scripts/UI/Modals/NewGameScreenDataAdapter.cs
          - Assets/Scripts/UI/Modals/SettingsScreenDataAdapter.cs
      - id: "3.0.4"
        title: "MainMenuController.StartNewGame signature change + EconomyManager + CityStats wiring"
        prefix: TECH
        depends_on: ["3.0.3"]
        digest_outline: >-
          Refactor StartNewGame to (mapSize, startingBudget, cityName, seed). Mark legacy
          (mapSize, seed, scenarioIndex) [Obsolete] for one ship cycle. Wire
          EconomyManager.SetStartingFunds(startingBudget) + CityStats.SetCityName(cityName);
          add SetCityName method to CityStats.
        kind: code
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/MainMenuController.cs
          - Assets/Scripts/Managers/GameManagers/EconomyManager.cs
          - Assets/Scripts/Managers/GameManagers/CityStats.cs
      - id: "3.0.5"
        title: "Scene-wire + new tokens (selected colors + section-header size + muted text)"
        prefix: TECH
        depends_on: ["3.0.4"]
        digest_outline: >-
          Mount baked panels into MainMenu.unity main-menu-content-slot. Publish 5 ui_tokens
          (color.bg.selected, color.border.selected, color.text.dark, size.text.section-header,
          color.text.muted).
        kind: code
        touched_paths:
          - Assets/Scenes/MainMenu.unity
          - db/migrations/0121_seed_form_tokens.sql

  - id: "4.0"
    title: "Wave A3 — save-load-view (load-only mode when MainMenu host)"
    exit: >-
      save-load-view panel published with mode bind. Mounts into main-menu-content-slot
      with mode forced to `load`. Player picks Load → real saves render newest-first; row
      click highlights; Load button loads CityScene; trash → 3-second confirm → delete.
    red_stage_proof: |
      Pre-A3: no Load surface in baked MainMenu (legacy slot still wired). Post-A3:
      visibility delta — write 3 saves with distinct timestamps via test scenario; click
      Load on baked main-menu → list shows 3 rows ordered newest-first; click middle row
      → highlight + Load enables; Load → CityScene loads correct save. Trash icon → 3-second
      countdown then GameSaveManager.DeleteSave fires.
    red_stage_proof_block:
      red_test_anchor: "n/a"
      target_kind: "design_only"
      proof_artifact_id: "n/a"
      proof_status: "not_applicable"
    tasks:
      - id: "4.0.1"
        title: "DB seed migration — save-load-view panel"
        prefix: TECH
        depends_on: ["3.0.5"]
        digest_outline: >-
          Seed save-load-view catalog_entity + panel_detail + panel_child rows. host_slots=[
          main-menu-content-slot, pause-menu-content-slot]. Mode bind drives save vs load
          variant.
        kind: code
        touched_paths:
          - db/migrations/0122_seed_save_load_view.sql
      - id: "4.0.2"
        title: "save-controls-strip + save-list archetypes"
        prefix: TECH
        depends_on: ["4.0.1"]
        digest_outline: >-
          2 archetype catalog rows + 2 case arms in UiBakeHandler.Archetype.cs. save-list
          carries row-action map for trash + select.
        kind: code
        touched_paths:
          - db/migrations/0123_seed_save_view_archetypes.sql
          - Assets/Scripts/Editor/Bridge/UiBakeHandler.Archetype.cs
      - id: "4.0.3"
        title: "SaveLoadScreenDataAdapter refactor — mode bind + list selection + trash"
        prefix: TECH
        depends_on: ["4.0.2"]
        digest_outline: >-
          Add saveload.mode bind subscription, list-selection highlight, sort newest-first,
          name-input + auto-name format <cityName>-YYYY-MM-DD-HHmm, per-row trash + 3s confirm,
          footer Load button.
        kind: code
        touched_paths:
          - Assets/Scripts/UI/Modals/SaveLoadScreenDataAdapter.cs
      - id: "4.0.4"
        title: "GameSaveManager APIs — GetSaveFiles + DeleteSave"
        prefix: TECH
        depends_on: ["2.0.4"]
        digest_outline: >-
          Add GetSaveFiles() : SaveFileMeta[] (sorted newest-first) + DeleteSave(name) APIs.
          Preserve Inv 7 save-migration policy. Wire to AGENT_BRIDGE for closed-loop testing.
        kind: code
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/GameSaveManager.cs
      - id: "4.0.5"
        title: "Scene-wire MainMenu.unity load mode + cutover legacy useBakedUi flag"
        prefix: TECH
        depends_on: ["4.0.3", "4.0.4"]
        digest_outline: >-
          Mount baked save-load-view into main-menu-content-slot with mode=load forced.
          Flip useBakedUi flag permanently; remove legacy loadCityPanel/optionsPanel
          SerializeField hookups from MainMenuController + Inspector wiring.
        kind: code
        touched_paths:
          - Assets/Scenes/MainMenu.unity
          - Assets/Scripts/Managers/GameManagers/MainMenuController.cs

  - id: "5.0"
    title: "Wave B1 — toolbar revisit-refine + tool-subtype-picker"
    exit: >-
      toolbar action ids reconciled with Wave A0 registry; tool-subtype-picker published
      and mounts under toolbar root in CityScene. Player clicks R toolbar slot → picker
      pops with 3 density variants; click chooses → tool active.
    red_stage_proof: |
      Pre-B1: tool-subtype-picker drafted in defs only (not in catalog); toolbar actions
      not registered against Wave A0 registry. Post-B1: visibility delta — boot CityScene,
      click R slot → picker overlay renders with 3 density variants; click I-density →
      ToolManager.ActiveTool ∈ ResidentialIndustrialDensity-encoded slug; pick another
      density → tool re-arms.
    red_stage_proof_block:
      red_test_anchor: "n/a"
      target_kind: "design_only"
      proof_artifact_id: "n/a"
      proof_status: "not_applicable"
    tasks:
      - id: "5.0.1"
        title: "DB seed migration — tool-subtype-picker panel publish (if not already)"
        prefix: TECH
        depends_on: ["1.0.4"]
        digest_outline: >-
          Confirm tool-subtype-picker not in catalog; if absent, seed catalog_entity +
          panel_detail + panel_child rows per docs/ui-element-definitions.md ###
          tool-subtype-picker.
        kind: code
        touched_paths:
          - db/migrations/0124_seed_tool_subtype_picker.sql
      - id: "5.0.2"
        title: "tool-subtype-picker archetype (catalog row + bake-handler case arm)"
        prefix: TECH
        depends_on: ["5.0.1"]
        digest_outline: >-
          1 archetype catalog row encoding R/C/I density-evolution semantics. 1 case arm
          in UiBakeHandler.Archetype.cs.
        kind: code
        touched_paths:
          - db/migrations/0125_seed_tool_subtype_picker_archetype.sql
          - Assets/Scripts/Editor/Bridge/UiBakeHandler.Archetype.cs
      - id: "5.0.3"
        title: "Toolbar action refinements + tool-subtype-picker subscription"
        prefix: TECH
        depends_on: ["5.0.2"]
        digest_outline: >-
          Reconcile existing toolbar adapter action ids against Wave A0 registry. Extend
          with picker subscription. ~6 actions added/refined.
        kind: code
        touched_paths:
          - Assets/Scripts/UI/Toolbar/ToolbarDataAdapter.cs
      - id: "5.0.4"
        title: "Scene-wire CityScene.unity picker mount under toolbar root"
        prefix: TECH
        depends_on: ["5.0.3"]
        digest_outline: >-
          Mount baked tool-subtype-picker prefab under existing toolbar root GameObject in
          CityScene.unity.
        kind: code
        touched_paths:
          - Assets/Scenes/CityScene.unity

  - id: "6.0"
    title: "Wave B2 — stats-panel (HUD-triggered modal, 3 tabs) + ModalCoordinator first build"
    exit: >-
      stats-panel published (1 panel + 21 children); ModalCoordinator C# class lands;
      TimeManager pause-owner APIs added; StatsHistoryRecorder service ticks monthly aggregates.
      Editor menu fires `stats.open` action → modal opens, sim pauses, 3 tabs render.
    red_stage_proof: |
      Pre-B2: no ModalCoordinator; no TimeManager pause-owner stack; stats UI is
      script-driven prefabs. Post-B2: visibility delta — Editor menu Territory > UI >
      Open Stats → baked modal renders, ModalCoordinator._openModals contains stats-panel,
      TimeManager.IsPaused()==true with owner=stats-panel; close modal → sim resumes.
      HUD trigger deferred (game-ui-catalog-bake Stage 9.15 BLOCK-CALL-OUT).
    red_stage_proof_block:
      red_test_anchor: "n/a"
      target_kind: "design_only"
      proof_artifact_id: "n/a"
      proof_status: "not_applicable"
    tasks:
      - id: "6.0.1"
        title: "DB seed migration — stats-panel + 21 children"
        prefix: TECH
        depends_on: ["5.0.4"]
        digest_outline: >-
          Seed stats-panel catalog_entity + panel_detail + 21 panel_child rows (header +
          close + tab-strip + range-tabs + 3 line-charts + 3 stacked-bar-rows + 11 service-rows).
        kind: code
        touched_paths:
          - db/migrations/0126_seed_stats_panel.sql
      - id: "6.0.2"
        title: "5 new archetypes — tab-strip, chart, range-tabs, stacked-bar-row, service-row"
        prefix: TECH
        depends_on: ["6.0.1"]
        digest_outline: >-
          5 archetype catalog rows + 5 case arms in UiBakeHandler.Archetype.cs.
        kind: code
        touched_paths:
          - db/migrations/0127_seed_stats_archetypes.sql
          - Assets/Scripts/Editor/Bridge/UiBakeHandler.Archetype.cs
      - id: "6.0.3"
        title: "ModalCoordinator + TimeManager pause-owner APIs"
        prefix: TECH
        digest_outline: >-
          Add Assets/Scripts/UI/Modals/ModalCoordinator.cs (TryOpen / Close /
          IsAnyExclusiveOpen). Add TimeManager.SetModalPauseOwner(string) +
          ClearModalPauseOwner(string) (single-owner stack initially).
        kind: code
        touched_paths:
          - Assets/Scripts/UI/Modals/ModalCoordinator.cs
          - Assets/Scripts/Managers/GameManagers/TimeManager.cs
      - id: "6.0.4"
        title: "StatsHistoryRecorder service + stats-panel adapter wiring"
        prefix: TECH
        depends_on: ["6.0.3"]
        digest_outline: >-
          Add StatsHistoryRecorder snapshotting monthly aggregates into 3mo / 12mo / all-time
          ring buffer. Wire stats-panel adapter to recorder + ~25 binds. Stub `stats.open`
          Editor menu trigger (HUD wire deferred).
        kind: code
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/StatsHistoryRecorder.cs
          - Assets/Scripts/UI/Modals/StatsPanelAdapter.cs
      - id: "6.0.5"
        title: "Scene-wire CityScene.unity + Editor menu stats.open stub"
        prefix: TECH
        depends_on: ["6.0.4"]
        digest_outline: >-
          Mount baked stats-panel prefab under CityScene.unity Canvas. Add Territory > UI >
          Open Stats Editor menu firing `stats.open` for closed-loop testing until 9.15
          hud-bar-stats-button addition lands.
        kind: code
        touched_paths:
          - Assets/Scenes/CityScene.unity
          - Assets/Scripts/Editor/UI/StatsPanelMenu.cs

  - id: "7.0"
    title: "Wave B3 — budget-panel (HUD-triggered modal, 4 quadrants) + ModalCoordinator reuse"
    exit: >-
      budget-panel published (1 panel + 25 children); BudgetForecaster service computes
      3-month forecast on slider edit; HUD trigger wires `budget.open` to existing
      hud-bar-budget-readout. Player clicks budget readout → modal opens; drag tax-R →
      forecast recomputes live; close → sim resumes.
    red_stage_proof: |
      Pre-B3: budget UI is script-driven prefab; no live forecast. Post-B3: visibility delta —
      click hud-bar-budget-readout → baked budget-panel modal renders with sim paused; drag
      tax-R slider from 7% to 12% → forecast bind updates within next frame; close →
      EconomyManager applies new tax rate.
    red_stage_proof_block:
      red_test_anchor: "n/a"
      target_kind: "design_only"
      proof_artifact_id: "n/a"
      proof_status: "not_applicable"
    tasks:
      - id: "7.0.1"
        title: "DB seed migration — budget-panel + 25 children"
        prefix: TECH
        depends_on: ["6.0.5"]
        digest_outline: >-
          Seed budget-panel catalog_entity + panel_detail + 25 panel_child rows (header +
          close + 4 sections + 4 tax slider-rows + 11 expense-rows + 2 readout-blocks +
          chart + range-tabs).
        kind: code
        touched_paths:
          - db/migrations/0128_seed_budget_panel.sql
      - id: "7.0.2"
        title: "3 new archetypes — slider-row-numeric, expense-row, readout-block"
        prefix: TECH
        depends_on: ["7.0.1"]
        digest_outline: >-
          3 archetype catalog rows + 3 case arms in UiBakeHandler.Archetype.cs (chart +
          range-tabs + section reuse Wave B2).
        kind: code
        touched_paths:
          - db/migrations/0129_seed_budget_archetypes.sql
          - Assets/Scripts/Editor/Bridge/UiBakeHandler.Archetype.cs
      - id: "7.0.3"
        title: "BudgetForecaster service + budget-panel adapter wiring"
        prefix: TECH
        depends_on: ["6.0.3", "7.0.2"]
        digest_outline: >-
          Add BudgetForecaster recomputing 3-month forecast on slider edit. Wire ~40 binds
          (taxes 4 + funding 11 + spent 11 + last-month 4 + forecast 3 + history matrix +
          range + header). ModalCoordinator reused.
        kind: code
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/BudgetForecaster.cs
          - Assets/Scripts/UI/Modals/BudgetPanelAdapter.cs
      - id: "7.0.4"
        title: "HUD trigger wire — hud-bar-budget-readout → budget.open"
        prefix: TECH
        depends_on: ["7.0.3"]
        digest_outline: >-
          Confirm hud-bar-budget-readout child in baked hud-bar (already 14/19 published)
          and assign action=`budget.open`. Coordinate with game-ui-catalog-bake plan if
          action wiring requires upstream change.
        kind: code
        touched_paths:
          - db/migrations/0130_wire_hud_bar_budget_readout_action.sql
      - id: "7.0.5"
        title: "Scene-wire CityScene.unity"
        prefix: TECH
        depends_on: ["7.0.3"]
        digest_outline: >-
          Mount baked budget-panel prefab under CityScene.unity Canvas.
        kind: code
        touched_paths:
          - Assets/Scenes/CityScene.unity

  - id: "8.0"
    title: "Wave B4 — pause-menu (ESC modal hub) + host-adapter pattern"
    exit: >-
      pause-menu published (1 panel + 7 children); modal-card archetype lands; PauseMenuDataAdapter
      refactored to host-adapter pattern embedding settings-view + save-load-view via slot mounts.
      ESC stack registers pause-menu as bottom of LIFO (TECH-14102 preserved). 4 close paths
      (Resume / ESC / backdrop / terminal action) work; ModalCoordinator mutual-exclusion enforced.
    red_stage_proof: |
      Pre-B4: pause-menu is script-driven prefab; no host-adapter pattern; settings + save-load
      duplicated. Post-B4: visibility delta — in CityScene press ESC → baked pause-menu renders
      with 6 buttons; click Settings → settings-view mounts in slot with mode locked
      (`pause.contentScreen=settings`); click Save → save-load-view mounts with mode=save; click
      Main-menu → 3-second confirm → quits to MainMenu; Resume + backdrop + ESC all close.
    red_stage_proof_block:
      red_test_anchor: "n/a"
      target_kind: "design_only"
      proof_artifact_id: "n/a"
      proof_status: "not_applicable"
    tasks:
      - id: "8.0.1"
        title: "DB seed migration — pause-menu + 7 children"
        prefix: TECH
        depends_on: ["7.0.5"]
        digest_outline: >-
          Seed pause-menu catalog_entity + panel_detail + 7 panel_child rows (title + 6 buttons).
          Sub-views mounted via slot reuse settings-view + save-load-view from MainMenu track.
        kind: code
        touched_paths:
          - db/migrations/0131_seed_pause_menu.sql
      - id: "8.0.2"
        title: "modal-card archetype (catalog row + bake-handler case arm)"
        prefix: TECH
        depends_on: ["8.0.1"]
        digest_outline: >-
          1 archetype catalog row encoding root container with backdrop + center-anchor +
          content-replace slot. 1 case arm in UiBakeHandler.Archetype.cs.
        kind: code
        touched_paths:
          - db/migrations/0132_seed_modal_card_archetype.sql
          - Assets/Scripts/Editor/Bridge/UiBakeHandler.Archetype.cs
      - id: "8.0.3"
        title: "PauseMenuDataAdapter refactor — host-adapter pattern"
        prefix: TECH
        depends_on: ["8.0.2", "4.0.5"]
        digest_outline: >-
          Refactor to embed settings-view + save-load-view via slot mounts. Settings mode
          locked. Save-load mode driven by which button clicked (Save → mode=save; Load →
          mode=load). Route Main-menu + Quit through confirm-button.
        kind: code
        touched_paths:
          - Assets/Scripts/UI/Modals/PauseMenuDataAdapter.cs
      - id: "8.0.4"
        title: "ESC stack registration + ModalCoordinator integration"
        prefix: TECH
        depends_on: ["8.0.3", "6.0.3"]
        digest_outline: >-
          Register pause-menu at bottom of UIManager.HandleEscapePress LIFO stack
          (TECH-14102 preserved). Pause-menu joins ModalCoordinator exclusive group with
          budget-panel + stats-panel.
        kind: code
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/UIManager.cs
      - id: "8.0.5"
        title: "Scene-wire CityScene.unity"
        prefix: TECH
        depends_on: ["8.0.3"]
        digest_outline: >-
          Mount baked pause-menu prefab under CityScene.unity Canvas. Coexists with existing
          pause-menu prefab until cutover then deletes legacy.
        kind: code
        touched_paths:
          - Assets/Scenes/CityScene.unity

  - id: "9.0"
    title: "Wave B5 — HUD widgets bundle (info-panel + map-panel + notifications-toast)"
    exit: >-
      3 panels published; info-panel auto-opens on world-click; HUD map button toggles
      minimap with drag-pan + layer toggles; GameNotificationManager Milestone tier
      extension (sticky queue + camera-jump click + 3 SFX) ships in place.
    red_stage_proof: |
      Pre-B5: DetailsPopupController + script-driven minimap + 4-tier notifications.
      Post-B5: visibility delta — click zoned cell → info dock opens right edge with
      population + jobs + Demolish (3-second confirm); HUD map button toggles minimap;
      drag minimap → camera pans; population crosses 1 000 → gold-pulse milestone toast
      sticks until clicked; click toast → camera jumps to milestone cell.
    red_stage_proof_block:
      red_test_anchor: "n/a"
      target_kind: "design_only"
      proof_artifact_id: "n/a"
      proof_status: "not_applicable"
    tasks:
      - id: "9.0.1"
        title: "DB seed migration — info-panel + map-panel + notifications-toast (3 panels)"
        prefix: TECH
        depends_on: ["8.0.5"]
        digest_outline: >-
          Seed 3 panels + ~23 children total. info-panel docks right edge; map-panel
          bottom-right minimap; notifications-toast top-right transient stack.
        kind: code
        touched_paths:
          - db/migrations/0133_seed_hud_widgets_bundle.sql
      - id: "9.0.2"
        title: "5 new archetypes — info-dock, field-list, minimap-canvas, toast-stack, toast-card"
        prefix: TECH
        depends_on: ["9.0.1"]
        digest_outline: >-
          5 archetype catalog rows + 5 case arms in UiBakeHandler.Archetype.cs.
        kind: code
        touched_paths:
          - db/migrations/0134_seed_hud_archetypes.sql
          - Assets/Scripts/Editor/Bridge/UiBakeHandler.Archetype.cs
      - id: "9.0.3"
        title: "info-panel adapter + WorldSelectionResolver + GridManager.DemolishAt"
        prefix: TECH
        depends_on: ["9.0.2"]
        digest_outline: >-
          Extract WorldSelectionResolver returning {type, fields[]} per click + 6 per-type
          field-set builders. Replace DetailsPopupController + OnCellInfoShown event. Wire
          Alt+Click inspect modifier in GridManager.Update. Add GridManager.DemolishAt(grid)
          direct API (tool-mode-independent) reusing onUrbanCellsBulldozed Action.
        kind: code
        touched_paths:
          - Assets/Scripts/UI/HUD/InfoPanelAdapter.cs
          - Assets/Scripts/Managers/GameManagers/WorldSelectionResolver.cs
          - Assets/Scripts/Managers/GameManagers/GridManager.cs
      - id: "9.0.4"
        title: "map-panel adapter + MiniMapController extensions + CameraController.PanCameraTo"
        prefix: TECH
        depends_on: ["9.0.2"]
        digest_outline: >-
          Extend MiniMapController — SetVisible(bool); OnDrag handler + drag-pan wiring;
          header-strip layer-toggle button forwarding; size enforcement update (render
          shrinks to 360×324 to make room for 36px header). Add
          CameraController.PanCameraTo(grid).
        kind: code
        touched_paths:
          - Assets/Scripts/UI/HUD/MapPanelAdapter.cs
          - Assets/Scripts/Controllers/GameControllers/MiniMapController.cs
          - Assets/Scripts/Controllers/GameControllers/CameraController.cs
      - id: "9.0.5"
        title: "GameNotificationManager Milestone tier extension + CityStats milestone emitter"
        prefix: TECH
        depends_on: ["9.0.2"]
        digest_outline: >-
          Q5 lock — extend GameNotificationManager in place. Add Milestone to
          NotificationType enum + PostMilestone(title, subtitle?, cellRef?) sticky variant +
          sticky-queue semantics + camera-jump on cellRef click + 3 SFX serialized fields.
          Add CityStats.OnPopulationMilestone Action<int> firing once per threshold from
          [1000, 5000, 10000, 25000, 50000, 100000]; per-service threshold-crosser util
          with 30-day debounce.
        kind: code
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs
          - Assets/Scripts/Managers/GameManagers/CityStats.cs

locked_decisions:
  Q1: MainMenu first complete, then CityScene (sequential, NOT parallel, NOT shared-plumbing-first).
  Q2: CityScene custom 5-wave (toolbar+picker / stats / budget / pause / HUD-bundle).
  Q3: MainMenu functional day-one — sliders persist via PlayerPrefs/audio mixer + Load slots load real saves.
  Q4: MainMenu-only action+bind registry first (~32 actions + ~12 binds); CityScene actions added per-wave later.
  Q5: notifications-toast plumbing — extend GameNotificationManager in place (Milestone tier + sticky queue + camera-jump click + 3 new SFX).
dependencies:
  - kind: block-call-out
    source: game-ui-catalog-bake Stage 9.15
    state: 5 unimplemented tasks; hud-bar at 14/19 children published; toolbar published
    impact: do-not-fix-here; this rollout consumes hud-bar+toolbar as-baked
  - kind: db-published
    source: catalog_entity (kind=panel)
    state: hud-bar (partial 14/19) + toolbar (full) published
  - kind: c-sharp-existing
    source: GameSaveManager + SettingsScreenDataAdapter + SaveLoadScreenDataAdapter + NewGameScreenDataAdapter + MainMenuController + GameNotificationManager + MiniMapController + GridManager + EconomyManager + CityStats + UIManager.HandleEscapePress
  - kind: scenes-existing
    source: Assets/Scenes/MainMenu.unity (build idx 0) + Assets/Scenes/CityScene.unity (build idx 1)
  - kind: bake-handler
    source: Assets/Scripts/Editor/Bridge/UiBakeHandler{.cs,.Frame.cs,.Archetype.cs}
    state: covers tokens + panels + archetypes; new kinds (view-slot / card-picker / chip-picker / text-input / save-list / etc.) need bake-handler additions per wave
panel_inventory_full:
  mainmenu_track:
    - main-menu (root panel; 5 buttons + view-slot)
    - new-game-form (sub-view; card-picker + chip-picker + text-input)
    - settings-view (sub-view; 9 controls = 3 toggles + 3 sliders + 1 dropdown + 1 reset-button)
    - save-load-view (sub-view; mode-driven save / load; load-only when MainMenu host)
  cityscene_track:
    - toolbar (revisit; already published — refine actions only)
    - tool-subtype-picker (already drafted in defs; check wave-1 for catalog publish)
    - stats-panel (3-tab modal)
    - budget-panel (4-quadrant modal)
    - pause-menu (6-button modal hub)
    - info-panel (right-edge inspect dock)
    - map-panel (bottom-right minimap)
    - notifications-toast (top-right transient stack)
---

# CityScene + MainMenu Panel Rollout — Exploration

## Problem

`docs/ui-element-definitions.md` locks 11 panel definitions across MainMenu (4) + CityScene (8). game-ui-catalog-bake landed DB-first bake infra + bake-handler + 2 panels published (`hud-bar` partial 14/19 + `toolbar` full). Remaining: seed catalog rows + bake snapshot + scene-wire + functional adapters for the 9 unbuilt panels + revisit toolbar refinements. Need an authoring sequence that ships product-visible value at each step without rebuilding infra and without unblocking scopes that the upstream plan still owns.

## Approaches surveyed

| ID | Name | Shape | Pro | Con |
| --- | --- | --- | --- | --- |
| A | Sequential MainMenu→CityScene, custom 5-wave CityScene | MainMenu fully ships first as one self-contained track; then CityScene 5 waves grouped by modal-coordinator dependency | Maximum learning compounding (registry + sub-view archetypes shake out on MainMenu before CityScene multiplies); each wave is independently shippable + visible to player; minimal cross-wave merge conflict; ModalCoordinator built once on Wave B2 then reused B3+B4 | Slowest calendar — no parallelism; CityScene ships later than parallel approach |
| B | Parallel MainMenu + CityScene tracks | Two devs / two waves at once | Faster calendar | Action+bind registry forks (Q4 lock says single MainMenu-first registry); bake-handler additions collide; new sub-view archetypes (`card-picker` etc.) needed by both → race; ModalCoordinator either built twice or blocks both |
| C | Shared-plumbing-first (registry + ModalCoordinator + ConfirmButton + 5 archetypes), then panels | Infra wave 0 → all 11 panels parallel after | Cleanest dependency graph in theory | Wave-0 ships zero player-visible value (~2 weeks of dark work); registry shape can't be locked without first real consumer (MainMenu); leads to either over-engineering or rework |
| D | Scene-bucketed (MainMenu = 1 wave / CityScene = 1 wave) | 2 mega-waves | Fewest planning artifacts | Each wave too large; fails prototype-first §Visibility Delta cadence; impossible to verify-loop incrementally |

## Recommendation

**Locked: Approach A** — Sequential MainMenu→CityScene with custom 5-wave CityScene split. Q1 + Q2 lock the shape; Q3+Q4+Q5 lock the per-track scopes. Registry built MainMenu-only first (Wave A0) so CityScene picks up a battle-tested action+bind dispatcher; ModalCoordinator built lazily on Wave B2 (stats-panel = first city modal) and reused on B3+B4.

## Open questions

All 5 polling decisions resolved at Phase 1 entry. No open questions for this exploration. Forward-looking flags surface inside `### Implementation Points` per wave (e.g. `card-picker` archetype shape lock, `string-pool` resolver, drag-pan API) — those become per-task spec drift items when `/ship-plan` decomposes.

---

## Design Expansion

### Chosen Approach

Sequential rollout, MainMenu track first (Waves A0-A3), then CityScene track (Waves B1-B5 per Q2 custom shape). Single MainMenu-only action+bind registry shipped Wave A0 sets the dispatcher contract; CityScene waves extend the registry per-panel-need. ModalCoordinator deferred to Wave B2 (first CityScene modal). Existing C# adapters refactored in place rather than rewritten — `MainMenuController` / `NewGameScreenDataAdapter` / `SettingsScreenDataAdapter` / `SaveLoadScreenDataAdapter` / `PauseMenuDataAdapter` / `MiniMapController` / `GameNotificationManager` all carry production logic; bake-driven UI is wrapper layer only.

### Architecture

#### Pipeline contract (per panel)

```
docs/ui-element-definitions.md (locked def)
  ↓
DB seed migration (db/migrations/00XX_seed_<panel>.sql)
  catalog_entity(kind='panel', slug, status='published') row
  + panel_detail(entity_id, layout_template, layout, params_json) row
  + panel_child(panel_id, child_id, position, layout_json) rows × N
  ↓
bake snapshot exporter (Editor menu → panels.json under Assets/UI/Snapshots/)
  ↓
UiBakeHandler.BakePanelSnapshotChildren(IrPanel) — Editor-only, JsonUtility round-trip
  ↓
Generated prefab under Assets/UI/Prefabs/Generated/<slug>.prefab
  ↓
Scene-wire (MainMenu.unity OR CityScene.unity root GameObject)
  ↓
Adapter (existing C# class, refactored to bind-dispatcher consumer)
```

Pipeline is identical for every wave; new kind (`view-slot`, `card-picker`, `chip-picker`, `text-input`, `save-list`, `tab-strip`, `chart`, `slider-row`, `expense-row`, `service-row`, `stacked-bar-row`, `info-dock`, `field-list`, `minimap-canvas`, `toast-stack`, `toast-card`, `confirm-button`, `modal-card`) only adds a `case` arm in `UiBakeHandler.Archetype.cs` + an archetype catalog row.

#### Action / bind registry shape (Wave A0 lock)

```csharp
// Assets/Scripts/UI/Registry/UiActionRegistry.cs (NEW)
public sealed class UiActionRegistry : MonoBehaviour
{
    private readonly Dictionary<string, Action<JsonValue?>> _handlers = new();
    public void Register(string actionId, Action<JsonValue?> handler) { ... }
    public bool Dispatch(string actionId, JsonValue? payload) { ... }
    public IReadOnlyList<string> ListRegistered() { ... }
}

// Assets/Scripts/UI/Registry/UiBindRegistry.cs (NEW)
public sealed class UiBindRegistry : MonoBehaviour
{
    private readonly Dictionary<string, BindEntry> _binds = new();
    public void Set<T>(string bindId, T value) { ... }
    public T Get<T>(string bindId) { ... }
    public IDisposable Subscribe<T>(string bindId, Action<T> onChange) { ... }
    public IReadOnlyList<string> ListRegistered() { ... }
}
```

Bake-time validator (Editor menu + agent-led check) walks `panel_child.params_json` for every published panel + asserts every `action` / `bind` / `visible_bind` / `enabled_bind` / `slot_bind` referenced exists in registry. Drift → exit 1 + names listed.

MCP slices added on Wave A0:

- `action_registry_list` — returns all registered action ids + handler-bound flag.
- `bind_registry_list` — returns all registered bind ids + current values + subscriber counts.

Both consumed by validate:all + by the bake-time validator C# side via Editor bridge command.

#### ModalCoordinator (Wave B2 first build, B3+B4 reuse)

```csharp
// Assets/Scripts/UI/Modals/ModalCoordinator.cs (NEW, Wave B2)
public sealed class ModalCoordinator : MonoBehaviour
{
    private readonly HashSet<string> _openModals = new();
    public bool TryOpen(string modalSlug)
    {
        // exclusive group: budget-panel / stats-panel / pause-menu
        if (IsExclusiveGroup(modalSlug) && _openModals.Overlaps(ExclusiveGroup))
            CloseAllInGroup();
        _openModals.Add(modalSlug);
        TimeManager.SetModalPauseOwner(modalSlug);
        return true;
    }
    public void Close(string modalSlug) { ... TimeManager.ClearModalPauseOwner(modalSlug); }
    public bool IsAnyExclusiveOpen() { ... }
}
```

`TimeManager.SetModalPauseOwner(string)` / `ClearModalPauseOwner(string)` — flagged as missing today; Wave B2 ships the API addition (single-owner stack initially; extend if 9.15 hud-bar adds re-entrancy).

#### ESC stack ordering (existing TECH-14102 LIFO discipline preserved)

```
SubTypePicker  >  ToolSelected  >  {budget-panel, stats-panel, info-panel}  >  pause-menu (fallback)
```

`UIManager.HandleEscapePress` lines 383-415 already implements. Each new modal registers itself on open, deregisters on close. info-panel ESC handled only when no modal active.

### Architecture Decision

Skip — touched arch surfaces are existing (`UiBakeHandler`, `GameSaveManager`, `MainMenuController`, `GameNotificationManager`, `UIManager`, `MiniMapController`, `GridManager`, `EconomyManager`, `CityStats`). No new system layer added; new C# classes (`UiActionRegistry`, `UiBindRegistry`, `ModalCoordinator`) are orchestration helpers within `Territory.UI` namespace already covered by DEC-A27 catalog architecture + DEC-A1x UI-design-system decisions. No `arch_decision_write` row needed for this rollout — per-wave drift items file under `arch_changelog` via standard ship-cycle closeout.

### Subsystem Impact

| Subsystem | Touch | Invariant flag | Mitigation |
| --- | --- | --- | --- |
| `Assets/Scenes/MainMenu.unity` | Edit — add baked panel root GameObjects under existing Canvas; remove legacy serialized panel slots from `MainMenuController` (loadCityPanel / optionsPanel) once baked equivalents wire | Inv 9 (Inspector wiring) | Keep both wired during Wave A1-A3 transition; switch via `[SerializeField] bool useBakedUi` flag; flip permanently after Wave A3 verification |
| `Assets/Scenes/CityScene.unity` | Edit — add baked panel root GameObjects per Wave (B1: tool-subtype-picker; B2: stats-panel; B3: budget-panel; B4: pause-menu; B5: info-panel + map-panel + notifications-toast) | Inv 9 | One scene-edit task per wave; coexists with existing modal prefabs until cutover |
| `UiBakeHandler.Archetype.cs` | Add ~18 new `case` arms across waves | Inv 11 (single-bake-pass deterministic) | Each new kind ships with archetype-catalog row + IR DTO + JsonUtility round-trip test |
| `UiBakeHandler.cs` | Extend `IrPanel` DTO if `host` / `host_slot` fields needed for sub-view embedding | Inv 11 | Defer until Wave A2 reveals actual need; preserve forward-compat empty defaults |
| MCP catalog slices (existing) | Reuse `ui_panel_publish` / `ui_panel_get` / `ui_archetype_publish` / `ui_token_publish` | — | No schema change |
| MCP slices (NEW Wave A0) | `action_registry_list` + `bind_registry_list` (read-only) | — | Editor bridge command + DB-backed handler-registration log |
| Calibration corpus (`ia/state/ui-calibration-corpus.jsonl`) | Append per-panel grilling decisions during seed authoring | — | Existing append-only pattern |
| `GameSaveManager` | Add `HasAnySave()` + `GetSaveFiles()` (sorted) + `GetMostRecentSave()` + `DeleteSave(name)` APIs | Inv 7 (save migration policy) | Wave A1 (HasAnySave + GetMostRecentSave drive Continue button); Wave A3 (GetSaveFiles sorted + DeleteSave drive save-load-view) |
| `SettingsScreenDataAdapter` | Add SFX slider + monthly-budget-notifications toggle + auto-save toggle + Reset-to-defaults button + bind-dispatch wiring | Inv 9 | Preserve existing 6 PlayerPrefs slots; add 3 NEW keys (`SfxVolumeKey` reflowing existing dB key, `MonthlyBudgetNotificationsKey`, `AutoSaveKey`) |
| `SaveLoadScreenDataAdapter` | Add mode bind, name input, per-row trash + 3s confirm, footer Load button, list selection highlight, sort newest-first | Inv 7 | Wave A3 |
| `NewGameScreenDataAdapter` | Replace 2 sliders + scenario toggles with 3-card map picker + 3-chip budget picker + city-name input; refactor `MainMenuController.StartNewGame` signature `(mapSize, seed, scenarioIndex)` → `(mapSize, startingBudget, cityName, seed)` | Inv 9 | Wave A2; keep old signature deprecated with `[Obsolete]` for one ship cycle |
| `MainMenuController` | Drop legacy `loadCityPanel` / `optionsPanel` serialized slots; route Quit through `confirm-button` archetype; drive sub-view via `mainmenu.contentScreen` bind | Inv 9 | Waves A1+A2 |
| `PauseMenuDataAdapter` | Refactor to host-adapter pattern — embed settings-view + save-load-view via slot mounts; route Main-menu + Quit through `confirm-button` | Inv 9 | Wave B4; reuses MainMenu shipped sub-views |
| `MiniMapController` | Add `SetVisible(bool)` API + `OnDrag` handler + drag-pan wiring; add header-strip layer-toggle button forwarding | Inv 9 + Inv 11 | Wave B5 |
| `GameNotificationManager` (Q5 lock) | Extend in place — add `Milestone` to `NotificationType` enum + `PostMilestone(title, subtitle?, cellRef?)` method + sticky queue logic + camera-jump click handler + 3 new SFX serialized fields | Inv 9 | Wave B5; preserves existing 4-tier callers; existing emitters untouched |
| `GridManager` | Add `DemolishAt(grid)` direct API (tool-mode-independent) for info-panel inline demolish; preserve `HandleBulldozerMode` | Inv 1 + Inv 8 (grid coord rules) | Wave B5; reuse existing onUrbanCellsBulldozed Action |
| `EconomyManager` | Confirm `SetStartingFunds(int)` mutable pre-game-start | Inv 13 | Wave A2; if not mutable, expose via `EconomyManager.Initialize(startingTreasury)` called by MainMenuController.StartNewGame |
| `CityStats` | Add `SetCityName(string)` + `OnPopulationMilestone` Action<int> + emitter wiring on monthly tick | — | Wave A2 (cityName) + Wave B5 (milestone emitter) |
| `string-pool` resolver + `city-name-pool-es` (100 names) | NEW catalog kind + content-authoring pass | — | Wave A2 — 100 fictional Spanish names; pool-row `kind=string-pool, lang=es` |

### Implementation Points (per wave)

#### Wave A0 — MainMenu-only registry + validator

- New: `Assets/Scripts/UI/Registry/UiActionRegistry.cs` + `UiBindRegistry.cs`.
- New: Editor menu `Territory > UI > Validate Action+Bind Drift` walks published `panel_child.params_json` + asserts each referenced id exists.
- New MCP slices: `action_registry_list` + `bind_registry_list` (Editor bridge → DB-backed registration log).
- 32 actions enumerated (locked):
  - main-menu (7): `mainmenu.continue`, `mainmenu.openNewGame`, `mainmenu.openLoad`, `mainmenu.openSettings`, `mainmenu.back`, `mainmenu.quit.confirm`, `mainmenu.quit`.
  - new-game-form (4): `newgame.mapSize.set`, `newgame.budget.set`, `newgame.cityName.reroll`, `mainmenu.startNewGame`.
  - settings-view (12): `settings.scrollEdgePan.set`, `settings.monthlyBudgetNotif.set`, `settings.autoSave.set`, `settings.master.set`, `settings.music.set`, `settings.sfx.set`, `settings.resolution.set`, `settings.fullscreen.set`, `settings.vsync.set`, `settings.resetDefaults.confirm`, `settings.resetDefaults`, `settings.back`.
  - save-load-view (9): `saveload.save.confirm`, `saveload.save`, `saveload.overwrite.confirm`, `saveload.overwrite`, `saveload.selectSlot`, `saveload.load`, `saveload.delete.confirm`, `saveload.delete`, `saveload.back`.
- 12 binds enumerated (locked):
  - `mainmenu.contentScreen` (enum), `mainmenu.continue.disabled` (bool), `mainmenu.back.visible` (bool), `mainmenu.title.text`, `mainmenu.version.text`, `mainmenu.studio.text`.
  - `newgame.mapSize` (enum), `newgame.budget` (enum), `newgame.cityName` (string).
  - `saveload.mode` (enum), `saveload.list` (array), `saveload.selectedSlot` (string|null).
  - settings.* values (9 separately registered but bundled under `settings.*.value` family — 1 family slot in the 12-count; concrete 9 slots are derived per spec).
- Visibility Delta — bake-time validator runs on `validate:all`; agent-led check fails CI if registry drift.

#### Wave A1 — main-menu root panel

- DB seed: `db/migrations/00XX_seed_main_menu.sql` — 1 catalog_entity(kind=panel, slug=`main-menu`, status=published) + 1 panel_detail(layout_template=`fullscreen-stack`) + 10 panel_child rows (5 buttons + 1 confirm-button + 1 back-button + 3 labels + 1 view-slot).
- New archetypes: `view-slot` (renders one of N declared sub-views by enum-bind value) + `confirm-button` (button variant with N-second inline countdown).
- Bake-handler: 2 new `case` arms in `UiBakeHandler.Archetype.cs`.
- Scene-wire: `MainMenu.unity` — add root GameObject `MainMenuPanelRoot` under existing Canvas; baked prefab parent.
- Adapter: refactor `MainMenuController` — drop `loadCityPanel` + `optionsPanel` serialized slots; subscribe to `mainmenu.contentScreen` enum bind; register handlers for 7 actions; call `GameSaveManager.HasAnySave()` to drive `mainmenu.continue.disabled`.
- New GameSaveManager APIs (Wave-A1 subset): `HasAnySave()` + `GetMostRecentSave()`.
- New tokens: `color.bg.menu` + `size.text.title-display`.
- Visibility Delta — player launches game, sees title screen with 5 functional buttons; Continue greys when no save; Quit shows 3-second confirm; New Game / Load / Settings show "coming next wave" view-slot placeholder.

#### Wave A2 — new-game-form + settings-view (functional day-one Q3 lock)

- DB seed: 2 panels seeded (`new-game-form` host=main-menu, `settings-view` host_slots=[main-menu-content-slot, pause-menu-content-slot]) + new pool `city-name-pool-es` (100 names — content-authoring task).
- New archetypes (7): `card-picker`, `chip-picker`, `text-input`, `toggle-row`, `slider-row`, `dropdown-row`, `section-header`.
- Bake-handler: 7 new case arms.
- Scene-wire: `MainMenu.unity` — both panels mount into `main-menu-content-slot` view-slot baked Wave A1.
- Adapter: refactor `NewGameScreenDataAdapter` (drop sliders + scenarios; add card/chip/input bind subscriptions); refactor `SettingsScreenDataAdapter` (add SFX slider + monthly-budget-notif toggle + auto-save toggle + Reset-to-defaults footer button + Volume mapping `LinearToDecibel(percent / 100f)` for all 3 sliders).
- New PlayerPrefs keys: `MonthlyBudgetNotificationsKey`, `AutoSaveKey`. Reuse existing `MasterVolumeKey`, `MusicVolumeKey`, `SfxVolumeDbKey` (surface to UI), `ResolutionIndexKey`, `FullscreenKey`, `VSyncKey`, `ScrollEdgePanKey`.
- Refactor `MainMenuController.StartNewGame` signature: `(mapSize, startingBudget, cityName, seed)`. Wire `EconomyManager.SetStartingFunds(startingBudget)` + `CityStats.SetCityName(cityName)`.
- New tokens: `color.bg.selected`, `color.border.selected`, `color.text.dark`, `size.text.section-header`, `color.text.muted`.
- Visibility Delta — player picks New Game → form shows 3 cards + 3 chips + name input pre-filled with random Spanish name; Start launches CityScene with chosen budget + name. Player picks Settings → 9 controls all functional (audio sliders move volume immediately + persist; resolution dropdown applies; toggles persist + apply on next-game-load).

#### Wave A3 — save-load-view (load-only when MainMenu host)

- DB seed: 1 panel (`save-load-view`) with mode bind. host_slots=[main-menu-content-slot, pause-menu-content-slot].
- New archetypes (2): `save-controls-strip` + `save-list` (with row-action map).
- Bake-handler: 2 new case arms.
- Scene-wire: mounts into `main-menu-content-slot` (mode forced to `load` when MainMenu host).
- Adapter: refactor `SaveLoadScreenDataAdapter` — add mode bind, list-selection highlight, sort newest-first, name-input + auto-name format `<cityName>-YYYY-MM-DD-HHmm`, per-row trash + 3s confirm, footer Load button.
- New GameSaveManager APIs: `GetSaveFiles()` (sorted newest-first metadata array) + `DeleteSave(name)`.
- Visibility Delta — player picks Load → list shows real saves sorted newest-first; click row highlights; click footer Load → CityScene loads that save. Trash icon → 3-second confirm → delete.

#### Wave B1 — toolbar revisit + tool-subtype-picker

- DB: toolbar already published — refine actions only (action ids may change post-revisit; coordinate with action-registry from A0). tool-subtype-picker drafted in defs; publish if not already (`docs/ui-element-definitions.md ### tool-subtype-picker`).
- New archetype: `tool-subtype-picker` (R/C/I density-evolution semantics).
- Bake-handler: 1 case arm.
- Scene-wire: `CityScene.unity` — picker mounts under existing toolbar root.
- Adapter: existing toolbar adapter; extend with picker subscription.
- ~6 actions added to registry.
- Visibility Delta — player clicks R toolbar slot → picker pops with 3 density variants; click chooses; tool active.

#### Wave B2 — stats-panel (HUD-triggered modal, 3 tabs) + ModalCoordinator first build

- DB seed: 1 panel + 21 children (header + close + tab-strip + range-tabs + 3 line-charts + 3 stacked-bar-rows + 11 service-rows).
- New archetypes (5): `tab-strip`, `chart`, `range-tabs`, `stacked-bar-row`, `service-row`.
- Bake-handler: 5 case arms.
- New: `Assets/Scripts/UI/Modals/ModalCoordinator.cs`.
- New: `TimeManager.SetModalPauseOwner(string)` + `ClearModalPauseOwner(string)`.
- New: `StatsHistoryRecorder` service (snapshots monthly aggregates into 3mo/12mo/all-time ring buffer).
- HUD trigger — depends on game-ui-catalog-bake Stage 9.15 hud-bar completion (currently 14/19 children); BLOCK-CALL-OUT — defer hud-bar-stats-button addition to upstream plan or coordinate amendment. Stub local action `stats.open` that fires from Editor menu until 9.15 closes.
- ~4 actions, ~25 binds.
- Visibility Delta — Editor menu fires `stats.open` → modal opens, sim pauses, 3 tabs render with placeholder series (until StatsHistoryRecorder ticks 1+ months).

#### Wave B3 — budget-panel (4-quadrant modal)

- DB seed: 1 panel + 25 children (header + close + 4 sections + 4 tax slider-rows + 11 expense-rows + 2 readout-blocks + chart + range-tabs).
- New archetypes (3): `slider-row-numeric` (with live readout left-aligned), `expense-row`, `readout-block`. (`chart` + `range-tabs` + `section` reused from Wave B2.)
- Bake-handler: 3 case arms.
- ModalCoordinator reused.
- New: `BudgetForecaster` service (recompute 3-month forecast on slider edit).
- HUD trigger — `hud-bar-budget-readout` already in baked hud-bar (current 14/19); confirm action wires to `budget.open`.
- ~5 actions, ~40 binds (taxes 4 + funding 11 + spent 11 + last-month 4 + forecast 3 + history matrix + range + header).
- Visibility Delta — player clicks budget readout → modal opens with sliders functional; drag tax-R → forecast recomputes live; close → sim resumes.

#### Wave B4 — pause-menu (ESC modal hub) + host-adapter pattern

- DB seed: 1 panel + 7 children (title + 6 buttons; sub-views mounted via slot — settings-view + save-load-view reused from MainMenu track).
- New archetype (1): `modal-card` (root container with backdrop + center-anchor + content-replace slot).
- Bake-handler: 1 case arm.
- Adapter: refactor `PauseMenuDataAdapter` — host-adapter pattern. embeds settings-view (mode locked; `pause.contentScreen=settings`) + save-load-view (mode driven by which button clicked: Save → mode=save, Load → mode=load). Routes 4 close paths (Resume, ESC, backdrop, terminal action).
- ESC stack — pause-menu registers as bottom of `UIManager.HandleEscapePress` LIFO stack (TECH-14102 preserved).
- ModalCoordinator reused (mutual exclusion with budget-panel + stats-panel).
- ~6 actions, ~3 binds.
- Visibility Delta — ESC opens pause-menu; 6 buttons functional; Settings mounts shared settings-view; Save/Load mount shared save-load-view in correct mode; Main-menu + Quit each show 3-second confirm; Resume + backdrop close + restore sim.

#### Wave B5 — HUD widgets bundle (info-panel + map-panel + notifications-toast)

- DB seed: 3 panels + ~23 children total.
- New archetypes (5): `info-dock`, `field-list`, `minimap-canvas`, `toast-stack`, `toast-card`.
- Bake-handler: 5 case arms.
- info-panel adapter: extract `WorldSelectionResolver` (returns `{type, fields[]}` per click) + 6 per-type field-set builders. Replaces `DetailsPopupController` + `OnCellInfoShown` event. Wire `Alt+Click` inspect modifier in `GridManager.Update`. Add `GridManager.DemolishAt(grid)` direct API.
- map-panel adapter: extend `MiniMapController` — `SetVisible(bool)` API; `OnDrag` handler + drag-pan wiring; header-strip layer-toggle button forwarding; size enforcement update (render shrinks to 360×324 to make room for 36px header). New `CameraController.PanCameraTo(grid)` (or per-tick `MoveCameraToMapCenter`).
- notifications-toast adapter (Q5 lock — extend in place): `GameNotificationManager` extension — add `Milestone` to `NotificationType` enum; add `PostMilestone(title, subtitle?, cellRef?)` method (sticky variant); add sticky-queue semantics (count non-sticky against max-visible, sticky always render in front); add camera-jump on `cellRef` click; add 3 new SFX serialized fields (`sfxSuccess`, `sfxWarning`, `sfxMilestone`).
- New emitters: `CityStats.OnPopulationMilestone` Action<int> (fires once per threshold from `[1000, 5000, 10000, 25000, 50000, 100000]`); per-service threshold-crosser util with 30-day debounce.
- HUD trigger — `hud-bar-map-button` exists in baked hud-bar (ord 9, center zone) but no documented action — assign `minimap.toggle`. `hud-bar-info-button` does NOT exist — info-panel auto-opens on world-click instead (no HUD trigger needed). notifications-toast has no trigger (lazy-create runtime).
- ~8 actions (info: `info.close`, `info.demolish.confirm`, `world.select`, `world.deselect`, `world.demolish`; map: `minimap.toggle`, `minimap.layer.set`, `minimap.click`, `minimap.drag`; toast: `notification.dismiss`, `notification.click`).
- ~20 binds.
- Visibility Delta — player clicks zoned building → info dock opens right edge, full card with population + jobs etc. + Demolish button (3s confirm). HUD map button toggles minimap; layer toggles + drag-pan functional. Population crosses 1 000 → gold-pulse milestone toast appears + sticks until clicked + camera jumps when clicked.

### Examples

#### Example 1 — Wave A1 panel_child seed row (main-menu Continue button)

```sql
-- db/migrations/0XXX_seed_main_menu.sql (excerpt)
INSERT INTO catalog_entity (id, kind, slug, status, display_name)
VALUES (gen_random_uuid(), 'panel', 'main-menu', 'published', 'Main menu');

INSERT INTO panel_detail (entity_id, layout_template, layout, params_json)
SELECT id, 'fullscreen-stack', 'fullscreen-stack',
       '{"bg_color_token":"color.bg.menu"}'::jsonb
FROM catalog_entity WHERE slug='main-menu';

-- Continue button child (ord 4)
INSERT INTO panel_child (panel_id, child_id, position, layout_json)
SELECT
  (SELECT id FROM catalog_entity WHERE slug='main-menu'),
  (SELECT id FROM catalog_entity WHERE slug='main-menu-continue-button'),
  4,
  '{"kind":"button","instance_slug":"main-menu-continue-button","params_json":{"kind":"primary-button","label":"Continue","action":"mainmenu.continue","disabled_bind":"mainmenu.continue.disabled","tooltip":"Resume your most recent city.","tooltip_override_when_disabled":"No save found.","zone":"center"}}'::jsonb;
```

#### Example 2 — Wave A0 action handler registration

```csharp
// MainMenuController.Start (refactored)
private void RegisterActionHandlers()
{
    var registry = ServiceLocator.Get<UiActionRegistry>();
    registry.Register("mainmenu.continue", _ => OnContinueClicked());
    registry.Register("mainmenu.openNewGame", _ => SetContentScreen("new-game-form"));
    registry.Register("mainmenu.openLoad", _ => SetContentScreen("load-list"));
    registry.Register("mainmenu.openSettings", _ => SetContentScreen("settings"));
    registry.Register("mainmenu.back", _ => SetContentScreen("root"));
    registry.Register("mainmenu.quit.confirm", _ => StartQuitConfirm());
    registry.Register("mainmenu.quit", _ => DoQuit());
}

private void SetContentScreen(string screen)
{
    var binds = ServiceLocator.Get<UiBindRegistry>();
    binds.Set("mainmenu.contentScreen", screen);
    binds.Set("mainmenu.back.visible", screen != "root");
}
```

#### Example 3 — Wave B5 GameNotificationManager Milestone tier extension

```csharp
// GameNotificationManager.cs (extension excerpt — Q5 lock)
public enum NotificationType { Info, Success, Warning, Error, Milestone }  // +Milestone

[Header("SFX clips (3 NEW)")]
[SerializeField] private AudioClip sfxSuccess;
[SerializeField] private AudioClip sfxWarning;
[SerializeField] private AudioClip sfxMilestone;

public void PostMilestone(string title, string subtitle = null, Vector2Int? cellRef = null)
{
    var n = new Notification { type = NotificationType.Milestone, title = title,
                               body = subtitle, cellRef = cellRef, sticky = true };
    EnqueueSticky(n);
    UiSfxPlayer.Play(sfxMilestone);
}

private void EnqueueSticky(Notification n)
{
    // sticky cards always render in front; count only non-sticky against maxVisible
    _queue.Insert(0, n);
    if (NonStickyVisibleCount() >= MaxVisible) AgeOutOldestNonSticky();
    LazyCreateUiCard(n);
}

private void OnCardClicked(Notification n)
{
    if (n.cellRef.HasValue && cameraController != null)
        cameraController.MoveCameraToCell(n.cellRef.Value);
    DismissCard(n);
}
```

### Review Notes

Phase 7 self-review (read-only, doc-vs-source cross-check). 5 audits. 0 BLOCKING. 5 NON-BLOCKING drift items for `/ship-plan` to fold during plan-author.

| # | Severity | Surface | Finding | Carry-into |
| --- | --- | --- | --- | --- |
| R1 | NON-BLOCKING | `panel_inventory_full` | hud-bar absent from inventory enum although correctly excluded (block-call-out via game-ui-catalog-bake Stage 9.15). Reader may assume miss. | Add explicit `excluded:` sub-key listing hud-bar with `owner: game-ui-catalog-bake-9.15` reason. |
| R2 | NON-BLOCKING | YAML frontmatter | `waves[]` shape diverges from `/ship-plan` SKILL Phase 4 emitter contract which expects `stages[]` + `tasks[]` (per-task: prefix / depends_on / digest_outline / touched_paths / kind). | `/ship-plan` decomposes waves → stages+tasks during plan-author phase; or rename `waves[]` → `stages[]` upstream. Decision deferred to `/ship-plan` invocation. |
| R3 | NON-BLOCKING | `actions_added` accounting | A0 top-line `~32` vs sum of A1+A2+A3 = 7+11+9 = 27. 5-action delta from rounding + scope edge. | `/ship-plan` enumerates exact list per Wave A0 spec; reconcile to either "A0 ships registry only, A1-A3 add 27 actions" or "A0 ships registry + 27, A1-A3 register against pre-declared". Document chosen accounting in plan body. |
| R4 | NON-BLOCKING | Example 1 SQL | Illustrative not authoritative — `instance_slug` pattern (migration 0104) means catalog_entity rows for buttons are not pre-seeded; pattern needs alignment with current schema. | `/ship-plan` derives canonical SQL from migration 0108 / 0110 patterns + adapts per-panel. Treat exploration example as shape sketch only. |
| R5 | NON-BLOCKING | Migration numbering | First free migration index is `0113`. `0XXX_seed_main_menu.sql` placeholder in spec. | `/ship-plan` reserves explicit numbers per stage + writes them into task spec touched_paths. |

DB-current alignment: hud-bar (14/19 children, migration 0108) + toolbar (full, migration 0110) + ui_tokens (0111) + ui_components (0112) — all match doc claims. Q1-Q5 locks consistent across YAML + body + Examples. Pipeline contract identical for every wave (verified — no per-wave divergence). ESC-stack ordering preserves TECH-14102 LIFO. ModalCoordinator deferred to first consumer (Wave B2) — no dark-infra concern.

### Expansion metadata

- Date: 2026-05-08
- Model: claude-opus-4-7
- Approach selected: A (Sequential MainMenu→CityScene + custom 5-wave CityScene)
- Blocking items resolved: 0
- Non-blocking items carried into Review Notes: 5 (R1-R5)

## Post-Wave-A0 learnings — gates to re-inject into pending Stages 6.0–9.0

**Source**: 2026-05-09 main-menu post-bake runtime gap. 6 illuminated/confirm-buttons baked + visually correct, but **inert on click** until 3 latent gaps fixed. Recurrent surfaces — flag upstream for any UI-bake stage that emits clickable children.

### Lesson summary

| # | Surface | Symptom | Fix landed |
|---|---|---|---|
| L1 | Action-wire gap | `params_json.action` dropped on the floor; no `UiActionTrigger` MonoBehaviour subscribing `IlluminatedButton.OnClick → UiActionRegistry.Dispatch`. | New `UiActionTrigger` component + `AttachUiActionTrigger` helper called from both bake-handler switch cases (illuminated-button + confirm-button). |
| L2 | Action-id drift | `panels.json` (Wave A0 canonical) used `mainmenu.openSettings` / `openLoad` / `openNewGame` / `quit.confirm`; scene-side `MainMenuController` registered `mainmenu.settings` / `load` / `new-game` / `quit-confirmed`. Compile-clean, dispatch-silent. | Renamed register-time ids to match `panels.json` canonical; comment added flagging panels.json as one source of truth. |
| L3 | Second wire-site drift | Bake handler had two button-kind switch cases. First patch only fixed illuminated-button; quit-button (confirm-button case) remained inert until same `AttachUiActionTrigger` call landed in case 2. | Manual patch — formal validator pending. |

### Gates to inject into Stages 6.0–9.0 §Red-Stage Proof + §Work Items

For every stage that emits clickable surfaces (button / confirm-button / tab-strip tab / chart-segment / range-tab — anywhere `params_json.action` or `params_json.action_confirm` lives in seed migration), add the following gates:

1. **Action-wire conformance** (§Red-Stage Proof — runtime).
   - After bake, `prefab_inspect` must show one `UiActionTrigger` component per child whose seed row carries `params_json.action`. `_actionId` field value ≡ seed canonical.
   - Path A bridge or Play-Mode test: dispatch a synthetic click on each baked button → assert handler fires (e.g. ModalCoordinator opens, sim pauses, scene state mutates). DB-row + bake screenshot alone = NOT proof.

2. **Action-id canonical-source check** (§Work Items).
   - Diff seed migration `params_json.action` ids vs scene-side `actionRegistry.Register` calls. One source of truth = seed migration / `panels.json`. Controllers register against it, never the inverse.
   - Add to task touched_paths: corresponding `Assets/Scripts/Managers/**/*Controller.cs` or `**/*Adapter.cs` that owns the registry-register block.

3. **Bake-handler coverage** (§Work Items — only when stage adds a NEW button-kind case to `UiBakeHandler`).
   - When introducing `tab-strip` / `chart-segment` / `range-tab` / `service-row` / `modal-card` archetypes (Stages 6.0, 7.0, 8.0), each new switch case must include `AttachUiActionTrigger(childGo, pj?.action)` for any kind that emits dispatchable clicks. Validator stub `validate:bake-handler-action-coverage` (TBD) — until landed, code reviewer manually checks parity across cases.

### Per-stage profiling

| Stage | Wave | Clickable surfaces | Gates needed |
|---|---|---|---|
| **6.0** | B2 stats-panel | `tab-strip` (3 tabs → `stats.tab.{economy,demographics,services}`); `range-tabs` (1m/3m/12m); HUD-trigger (Editor menu `stats.open`). 3 NEW archetypes with click surfaces. | L1 + L2 + L3 — TECH-27083 (archetypes) must include bake-handler coverage check; TECH-27084 (ModalCoordinator) §Red-Stage Proof = synthetic `stats.open` dispatch fires modal + sim pauses; TECH-27086 (scene-wire) verifies all 3 tabs + range-tabs render UiActionTrigger via prefab_inspect. |
| **7.0** | B3 budget-panel | `quadrant-card` × 4 (R/C/I/services); `tax-slider` × 4 (continuous, not click — exempt L1); `lock-toggle` × 4 → `budget.lock.{r,c,i,services}`. | L1 + L2 — TECH-2709x archetypes (quadrant-card, lock-toggle) need bake-handler coverage; ModalCoordinator reuse = no new pause logic but `budget.open` dispatch test still required. Slider exempt from L1 (continuous value, separate wire pattern). |
| **8.0** | B4 pause-menu | 5 buttons (resume, settings, save-load, main-menu, quit) + ESC stack pop = 6th implicit click; `modal-card` archetype. | L1 + L2 + L3 — full button parity check, **identical to MainMenu Wave A1 surface area** (same 5-button stack pattern). TECH-27093 modal-card archetype = new bake case → coverage gate. TECH-27095 ESC integration = synthetic ESC keypress test ≡ click on Resume. **Highest L1 risk** — pause-menu mirrors main-menu defect surface 1:1. |
| **9.0** | B5 HUD widgets | info-panel (read-mostly, ~1-2 clickables); map-panel (zoom/center buttons → `map.{zoom_in,zoom_out,center}`); notifications-toast (dismiss × N → `notification.dismiss`). | L1 + L2 — map-panel buttons + toast dismiss need wire+id check. info-panel mostly bind-only (text fields), low click surface. |

### Handoff-yaml convention proposal (for any future `/ship-plan` extend on this slug)

Add per-stage key in handoff yaml frontmatter:

```yaml
stages:
  - id: 6.0
    action_wire_proof: required   # required | skip
    action_canonical_source: db.catalog_panel  # or panels.json
    new_bake_handler_cases: [tab-strip, range-tabs, chart-segment, stacked-bar-row, service-row]
```

`/ship-plan` Phase 5 (digest composer) reads these keys → emits §Red-Stage Proof clauses (synthetic-click-fires-handler) + §Work Items entries (id-canonical-diff + bake-handler coverage check) automatically. Pre-empts the Wave A0 → bake → runtime gap from recurring on every stage.

### Action items before re-extending the plan

1. Stage 6.0 task TECH-27084 (ModalCoordinator) → §Red-Stage Proof must add synthetic `stats.open` dispatch test (Path A bridge or Play-Mode).
2. Stage 6.0 task TECH-27083 (5 new archetypes) → §Work Items must list: each new switch case in `UiBakeHandler` includes `AttachUiActionTrigger` for click-emitting kinds.
3. Stage 8.0 task TECH-27095 (ESC stack) → §Red-Stage Proof must add ESC-keypress + 4-close-paths fires-handler test (mirrors MainMenu defect surface).
4. Land `validate:bake-handler-action-coverage` validator stub (separate TECH issue — flag for backlog).
