---
slug: cityscene-mainmenu-panel-rollout
parent_plan_slug: null
parent_rationale: >
  No live master plan owns this rollout. game-ui-catalog-bake is the upstream
  infra plan (DB-first bake + bake handler + 2 panels published). This rollout
  CONSUMES that infra. New plan = peer of game-ui-catalog-bake, not a child stage.
target_version: 1
tracks:
  - id: A
    name: MainMenu
    sequence: first
    waves: [A0, A1, A2, A3]
  - id: B
    name: CityScene
    sequence: after-MainMenu-complete
    waves: [B1, B2, B3, B4, B5]
waves:
  - id: A0
    track: A
    title: MainMenu-only action+bind registry + bake-time validator
    panels: []
    actions_added: ~32
    binds_added: ~12
    new_archetypes: []
  - id: A1
    track: A
    title: main-menu root panel (5 buttons + view-slot + back-arrow + branding strips)
    panels: [main-menu]
    actions_added: 7
    binds_added: 6
    new_archetypes: [view-slot, confirm-button]
  - id: A2
    track: A
    title: new-game-form sub-view + settings-view sub-view (functional day-one)
    panels: [new-game-form, settings-view]
    actions_added: 11
    binds_added: 12
    new_archetypes: [card-picker, chip-picker, text-input, toggle-row, slider-row, dropdown-row, section-header]
  - id: A3
    track: A
    title: save-load-view sub-view (load-only mode for MainMenu host) + functional load
    panels: [save-load-view]
    actions_added: 9
    binds_added: 7
    new_archetypes: [save-controls-strip, save-list]
  - id: B1
    track: B
    title: toolbar revisit-refine + tool-subtype-picker
    panels: [toolbar, tool-subtype-picker]
    actions_added: ~6
    binds_added: ~4
    new_archetypes: [tool-subtype-picker]
  - id: B2
    track: B
    title: stats-panel (HUD-triggered modal, 3 tabs) + ModalCoordinator first build
    panels: [stats-panel]
    actions_added: ~4
    binds_added: ~25
    new_archetypes: [tab-strip, chart, range-tabs, stacked-bar-row, service-row]
  - id: B3
    track: B
    title: budget-panel (HUD-triggered modal, 4 quadrants) + ModalCoordinator reuse
    panels: [budget-panel]
    actions_added: ~5
    binds_added: ~40
    new_archetypes: [slider-row-numeric, expense-row, readout-block]
  - id: B4
    track: B
    title: pause-menu (ESC modal, 6 buttons) + host-adapter pattern reuses settings-view + save-load-view
    panels: [pause-menu]
    actions_added: ~6
    binds_added: ~3
    new_archetypes: [modal-card]
  - id: B5
    track: B
    title: HUD widgets bundle — info-panel + map-panel + notifications-toast (Milestone tier extension)
    panels: [info-panel, map-panel, notifications-toast]
    actions_added: ~8
    binds_added: ~20
    new_archetypes: [info-dock, field-list, minimap-canvas, toast-stack, toast-card]
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
