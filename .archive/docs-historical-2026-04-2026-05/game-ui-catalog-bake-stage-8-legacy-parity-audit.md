# Stage 8 — Legacy UI parity audit

TECH-14099 deliverable. Diffs main-branch UI inventory against current catalog seed coverage. Anchor for Stage 8 closeout gate.

## §Method

Inventory sources:

- **Main reference:** `git show main:Assets/Scenes/MainScene.unity` GameObject `m_Name` enumeration (236 unique names, 47 UI-suffixed candidates) + `git show main:Assets/UI/Prefabs/*.prefab` (4 hand-authored shells).
- **Current catalog seed:** `Assets/UI/Prefabs/Generated/*.prefab` (39 catalog-baked prefabs, kebab-case D1 slugs) + 4 unchanged shell prefabs at `Assets/UI/Prefabs/*.prefab` (`UI_ModalShell`, `UI_ScrollListShell`, `UI_StatRow`, `UI_ToolButton`).
- **Catalog tables (Postgres):** `catalog_panel_list`, `catalog_button_list`, `catalog_sprite_list`, `catalog_archetype_list`, `catalog_list` — all return `items=[]` on current branch (catalog rows seeded inline by bake handlers, not persisted to Postgres catalog tables; consistent with Stage 6 demotion of IR JSON to sketchpad-only).

Tag enum (3 values per Plan Digest §Pending Decisions):

- `covered` — catalog seed exists (Generated prefab OR shell prefab + scene wiring).
- `gap` — main-branch entity has no current-branch equivalent. Triggers escalation per gap rule (≤2 files = inline T8.x.y; >2 files = new stage).
- `retired` — intentional drop with documented decision (TECH-10500 collapse, Stage 6 IR demotion, etc.).

Dedup pattern (reusable from T8.2 hud-bar dedup):

1. `unity_bridge_command` `delete_gameobject` for scene refs.
2. `unity_bridge_command` `delete_asset` for prefab + .meta on disk.
3. `bash rm -rf` for empty stub dirs surviving prior bake iterations.
4. `unity_bridge_command` `save_scene` then `refresh_asset_database`.
5. EditMode test asserting survivor + dead-guid scrub.

## §Inventory

### HUD entities

| Main-branch entity | Current coverage | Tag | Rationale |
|---|---|---|---|
| `MoneyPanel` (CityStats child) | `Generated/hud-bar.prefab` (HUD bar carries money chip) + `Generated/city-stats.prefab` | covered | hud-bar dedup landed T8.2; D1 single source = `hud-bar.prefab` |
| `PopulationPanel` | `Generated/hud-bar.prefab` (population chip) | covered | bar-chip composition |
| `HappinessPanel` | `Generated/hud-bar.prefab` (happiness chip) | covered | bar-chip composition |
| `DatePanel` + `DateText` | `Generated/hud-bar.prefab` (date chip) | covered | bar-chip composition |
| `CityNameText` + `CityCategoryText` | `Generated/hud-bar.prefab` (city-name chip) | covered | bar-chip composition |
| `SpeedButtonsPanel` (4 speed buttons) | `Generated/time-controls.prefab` | covered | catalog-baked replacement |
| `MiniMapPanel` | `Generated/mini-map.prefab` | covered | catalog-baked replacement |
| `NotificationPanel` | `Generated/alerts-panel.prefab` | covered | catalog-baked replacement |
| `DebugPanel` (cell coordinates + height + zone + water) | scene `CellDataPanel` wired via T8.5 (TECH-14101 binding test) | covered | T8.5 EditMode test pins binding contract |

### Stats / data popups

| Main-branch entity | Current coverage | Tag | Rationale |
|---|---|---|---|
| `StatsPanel` (CityStats popup) | `Generated/city-stats.prefab` + `Generated/city-stats-handoff.prefab` | covered | catalog-baked replacement |
| `DetailsPanel` + `DetailsPopupController` | `Generated/building-info.prefab` | covered | catalog-baked replacement |
| `DataPopupController` + `DataPanelButtons` | `Generated/info-panel.prefab` | covered | catalog-baked replacement |

### Zone selector / picker

| Main-branch entity | Current coverage | Tag | Rationale |
|---|---|---|---|
| `BuildingSelectorMenuManager` | `Assets/Scripts/UI/SubtypePickerController.cs` (TECH-10500) | retired | TECH-10500 migration: single picker for all 4 ToolFamily values; manager indirection retired |
| `BuildingSelectorPopupController` | `Assets/Scripts/UI/SubtypePickerController.cs` | retired | TECH-10500 migration |
| `BuildingSelectorPopupPanel` | code-built panel (`SubtypePickerController.EnsureRuntimePanelRootIfNeeded`) | retired | TECH-10500 migration; runtime panel construction in C# |
| `SubTypePickerModal` | `Assets/Scripts/UI/SubtypePickerController.cs` | retired | TECH-10500 rename + namespace move (`Territory.Economy` → `Territory.UI`); contract pinned by T8.4 `SubTypePickerParityTest.cs` |
| `ResidentialZoningSelectorButton` | `ToolFamily.Residential` dispatch from `UIManager.Toolbar.cs` | covered | toolbar wiring intact |
| `CommercialZoningSelectorButton` | `ToolFamily.Commercial` dispatch | covered | toolbar wiring intact |
| `IndustrialZoningSelectorButton` | `ToolFamily.Industrial` dispatch | covered | toolbar wiring intact |
| `StateServiceZoningSelectorButton` | `ToolFamily.StateService` dispatch | covered | toolbar wiring intact |
| `PowerBuildingSelectorButton` | folded into `ToolFamily.StateService` catalog rows via `ZoneSubTypeRegistry` | retired | TECH-10500 collapse: 7 main categories → 4 ToolFamily values; power = StateService row |
| `WaterBuildingSelectorButton` | folded into `ToolFamily.StateService` | retired | TECH-10500 collapse |
| `RoadsSelectorButton` | folded into `ToolFamily.StateService` | retired | TECH-10500 collapse |
| `EnviromentalSelectorButton` (sic — main branch typo) | folded into `ToolFamily.StateService` | retired | TECH-10500 collapse + main-branch typo never carried forward |
| `BulldozerButton` | `UIManager.Toolbar.cs::OnBulldozeButtonClicked` + ToolSelected stack frame (T8.6) | covered | tool-button wiring + Esc-stack frame intact |

### Demand / economy / employment panels

| Main-branch entity | Current coverage | Tag | Rationale |
|---|---|---|---|
| `DemandResidentialPanel` + `DemandCommercialPanel` + `DemandIndustrialPanel` | `Generated/city-stats.prefab` (demand sub-rows) + `Generated/zone-overlay.prefab` (overlay strip) | covered | catalog-baked replacement |
| `DemandWarningPanel` + `DemandFeedbackPanel` | `Generated/alerts-panel.prefab` | covered | alert routing replaces dedicated demand-warning panel |
| `TaxPanel` (residential / commercial / industrial tax) | `Generated/city-stats.prefab` | covered | sub-rows folded into city-stats |
| `JobsTakenPanel` + `TotalJobsCreatedPanel` + `TotalJobsPanel` + `UnemploymentPanel` + `AvailableJobsPanel` | `Generated/city-stats.prefab` employment rows | covered | sub-rows folded into city-stats |
| `CityPowerConsumptionPanel` + `CityPowerOutputPanel` | `Generated/city-stats.prefab` power rows | covered | sub-rows folded into city-stats |
| `CityWaterConsumptionPanel` + `CityWaterOutputPanel` | `Generated/city-stats.prefab` water rows | covered | sub-rows folded into city-stats |
| `InsufficientFundsPanel` | `Generated/alerts-panel.prefab` (toast) | covered | alert routing |

### Menus / overlays

| Main-branch entity | Current coverage | Tag | Rationale |
|---|---|---|---|
| `LoadGameMenuPanel` | `Generated/save-load.prefab` + `Generated/save-load-screen.prefab` | covered | catalog-baked replacement |
| `ControlPanel` (camera + game buttons) | `Generated/toolbar.prefab` + `Generated/time-controls.prefab` | covered | split into two panels |
| `EnergyGrowthSlider` + `RoadGrowthSlider` + `WaterGrowthSlider` + `ZoningGrowthSlider` + `TotalGrowthBudgetSlider` | `Generated/themed-slider.prefab` (template) | covered | slider template; per-axis instances code-instantiated |
| `Canvas` (3 hierarchy roots on main) | `UI Canvas` (single root, T8.1 D10) | retired | T8.1 D10 invariant: single Canvas root |
| pause / settings / new-game / splash / onboarding | `Generated/{pause,settings,new-game,splash,onboarding}{,-menu,-screen,-overlay}.prefab` | covered | catalog-baked replacements present |

### Theme / sketch primitives (non-functional, baked from claude-design IR)

| Generated prefab | Tag | Rationale |
|---|---|---|
| `themed-button.prefab`, `themed-label.prefab`, `themed-list.prefab`, `themed-overlay-toggle-row.prefab`, `themed-tab-bar.prefab`, `themed-toggle.prefab` | covered | atomic themed primitives |
| `illuminated-button.prefab`, `knob.prefab`, `led.prefab`, `oscilloscope.prefab`, `segmented-readout.prefab`, `vu-meter.prefab`, `detent-ring.prefab`, `fader.prefab`, `tooltip.prefab`, `glossary-panel.prefab`, `overlay-toggle-strip.prefab` | covered | sketchpad primitives (Stage 6 IR demotion — runtime-optional) |

## §Findings

- Zero `gap` rows. Every UI entity from main MainScene + `Assets/UI/Prefabs/` maps to either a Generated prefab (catalog-baked), a shell prefab + scene wiring, OR a retired entity with documented decision.
- TECH-10500 collapse drives the largest retired cluster: 7 main-branch zoning/state-service selector buttons → 4 ToolFamily values + StateService catalog dispatch via `ZoneSubTypeRegistry`. T8.4 `SubTypePickerParityTest.cs` pins the contract.
- T8.1 D10 single-Canvas invariant retires the 3 hand-authored Canvas roots from main; survivor = `"UI Canvas"`.
- T8.2 hud-bar dedup pattern (delete_gameobject + delete_asset + bash rm + save_scene + refresh_asset_database + EditMode test) reusable for any future duplicate prefab cleanup; documented under §Method.
- Catalog tables (`catalog_panel`, `catalog_button`, `catalog_sprite`, `catalog_archetype`, `catalog_asset`) carry zero rows on current branch. Bake handlers seed inline; no Postgres-persisted catalog. Confirms Stage 6 IR demotion intent (sketchpad-only). Future stage gate if/when catalog tables populate: re-run audit against DB rows.

## §Follow-ups

None. Zero `gap` rows ⇒ no inline T8.x.y tasks filed; no new stage proposed.

## Cross-references

- Anchor: `docs/game-ui-catalog-bake-post-mvp-extensions.md` Findings log (Stage 8 ship status section).
- Component contract pin: `Assets/Tests/EditMode/UI/SubTypePickerParityTest.cs` (T8.4).
- Single-canvas pin: `Assets/Tests/EditMode/UI/SingleRootCanvasTest.cs` (T8.1).
- Hud-bar dedup pin: `Assets/Tests/EditMode/UI/HudBarDedupTest.cs` (T8.2).
- CellData binding pin: `Assets/Tests/EditMode/UI/CellDataPanelBindingTest.cs` (T8.5).
- Esc-stack pin: `Assets/Tests/EditMode/UI/EscStackStateMachineTest.cs` (T8.6).
