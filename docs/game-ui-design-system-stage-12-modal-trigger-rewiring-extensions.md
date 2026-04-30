# Game UI Design System — Stage 12 Modal Trigger Rewiring Extensions

> **Source type:** Extensions doc for existing `game-ui-design-system` master plan.
> **Companion to:** `docs/game-ui-design-system-exploration.md` + `docs/game-ui-design-system-stage-9-split-extensions.md` + `docs/game-ui-design-system-render-layer-extensions.md`.
> **Why this doc exists:** Stage 8 shipped 5 themed modal prefabs + 5 `*DataAdapter.cs` MonoBehaviours + scene wiring under `UI Canvas`, but trigger paths still point to LEGACY surfaces. Player presses Esc → nothing. Alt+click cell → legacy `DetailsPopupController` UI shows (Occupancy / Happiness / Power Output / Power Consumption — old style, NOT new themed `info-panel`). MainMenu → "Options" → legacy options screen (SFX Volume + Mute + Back), NOT new themed `settings-screen`. MainMenu → "New Game" → legacy flow, NOT new themed `new-game-screen`. PauseMenu → "Save" / "Load" → never opens because pause menu itself never opens. Stage 11 (already decomposed, half-B surface adapters) covers splash / onboarding / glossary / city-stats handoff but does NOT touch modal trigger paths. This Stage appends one new Stage 12 covering the 5 trigger rewires + Esc-stack regression smoke + lessons-learned migration.

---

## Decision — extension scope

Append one new Stage (Stage 12) covering trigger-path rewiring from legacy surfaces to the 5 themed modals shipped in Stage 8. Numbered append-only per `master-plan-extend` boundary; logical execution order = 6→10→7→8→9→11→12. After Stage 12 ships, the visual MVP completion gate fires: Esc opens themed pause menu / Alt+click on grid cell opens themed info-panel / MainMenu Options opens themed settings-screen / MainMenu New Game opens themed new-game-screen / Pause → Save+Load opens themed save-load-screen.

Why a separate Stage rather than fold into Stage 11:

- Stage 11 scope is locked to half-B surface adapters (splash / onboarding / glossary / city-stats handoff) per `docs/game-ui-design-system-stage-9-split-extensions.md`. Adding trigger rewiring would mix concerns + break sizing gate (3 → 7 tasks).
- Trigger rewiring is INDEPENDENT of Stage 11 surface adapters — only depends on Stage 8 outputs already shipped. Can land before Stage 11 (faster visual payoff for player).
- Each rewire is small (1 file edit + scene-wiring delta + 1 PlayMode test step) — clean Task granularity.

## Locked decisions

- **Trigger path pattern:** legacy caller → call `UIManager.OpenPopup(PopupType.{Modal})` instead of legacy method/UI activation. The 5 `*DataAdapter.cs` already subscribe their producer/consumer events; once `OpenPopup` activates the modal root, the adapter populates content automatically.
- **Legacy preservation:** legacy surfaces (`DetailsPopupController` UI, MainMenu options screen, MainMenu legacy new-game flow) DELETED in this Stage — co-existence ends. Lessons-learned migrate to canonical docs before delete (per IF→THEN guardrail in `invariants.md`).
- **Esc stack reuse:** `UIManager.cs` line 437 (`Input.GetKeyDown(KeyCode.Escape)`) already drives the popup stack pop. Stage 12 only adds a "Esc with empty stack → push `PopupType.PauseMenu`" branch. No new input handler.
- **Smoke test = single PlayMode test** (`ModalTriggerPathsSmokeTest.cs`) covering Esc → pause / Alt+click → info-panel / MainMenu Options → settings / MainMenu New Game → new-game / Pause Save+Load → save-load + Esc-stack close-last-first regression. Runs via `unity:testmode-batch`.

## Architecture — Stage 12 component map

```
Assets/Scripts/Managers/GameManagers/UIManager.cs
  └─ Update(): line 437 Esc branch
      ├─ existing: pop top of popup stack if non-empty
      └─ NEW: if popup stack empty AND game-running state → OpenPopup(PopupType.PauseMenu)

Assets/Scripts/Managers/GameManagers/MainMenuController.cs
  ├─ OnOptionsClicked() / OpenSettings()
  │     └─ legacy: activate legacy options screen GameObject
  │     └─ NEW: UIManager.Instance.OpenPopup(PopupType.SettingsScreen)
  ├─ OnNewGameClicked() (or equivalent main-menu New Game button handler)
  │     └─ legacy: legacy new-game flow
  │     └─ NEW: UIManager.Instance.OpenPopup(PopupType.NewGameScreen)
  ├─ SaveGame() / LoadGame()                                       (called by PauseMenuDataAdapter)
  │     └─ legacy: directly invoke GameSaveManager
  │     └─ NEW: UIManager.Instance.OpenPopup(PopupType.SaveLoadScreen)
  └─ ResumeGame() — body remains empty; PauseMenuDataAdapter Resume button calls
                     UIManager.Instance.ClosePopup(PopupType.PauseMenu) via OnResume forward

Assets/Scripts/Controllers/UnitControllers/DetailsPopupController.cs
  ├─ existing OnCellInfoShown event (consumed by InfoPanelDataAdapter)
  ├─ legacy popup UI activation logic
  │     └─ DELETE legacy UI activation; keep ONLY OnCellInfoShown event firing
  └─ AFTER firing OnCellInfoShown: UIManager.Instance.OpenPopup(PopupType.InfoPanel)

Assets/Scripts/UI/Modals/PauseMenuDataAdapter.cs
  ├─ OnSave() — currently `_mainMenu.SaveGame()` direct call
  │     └─ NEW: replace with UIManager.Instance.OpenPopup(PopupType.SaveLoadScreen)
  ├─ OnLoad() — currently `_mainMenu.LoadGame()` direct call
  │     └─ NEW: replace with UIManager.Instance.OpenPopup(PopupType.SaveLoadScreen)
  └─ OnResume() — already calls `_mainMenu.ResumeGame()`; ALSO call
                  UIManager.Instance.ClosePopup(PopupType.PauseMenu)

Assets/Scripts/Tests/UI/Modals/ModalTriggerPathsSmokeTest.cs   (NEW)
  └─ PlayMode test: Esc → pause / Alt+click → info-panel / MainMenu Options → settings /
                    MainMenu New Game → new-game / Pause Save → save-load /
                    Esc-stack close-last-first regression

LEGACY DELETIONS (pattern parallels Stage 6 UIManager.Hud.cs / Stage 11 UIManager.CityStats.cs):
  ├─ MainMenuController legacy options-screen GameObject + handler partial (if isolatable)
  ├─ MainMenuController legacy new-game flow (if isolatable)
  └─ DetailsPopupController legacy popup UI (Image/TMP_Text children + Show/Hide methods that
                                              activate legacy GameObject; keep event-firing logic only)
```

## Subsystem Impact

- **UIManager** — single Esc branch addition; `OpenPopup` / `ClosePopup` already work (Stage 8 wired modal root fields).
- **MainMenuController** — 3 button-handler bodies rewritten; legacy options + legacy new-game UI deleted; lessons-learned migrate to `docs/agent-lifecycle.md` if process-relevant or to glossary if domain-relevant.
- **DetailsPopupController** — legacy UI activation deleted; event-firing logic preserved; `OpenPopup(PopupType.InfoPanel)` call added after event fire.
- **PauseMenuDataAdapter** — `OnSave` / `OnLoad` rewritten; `OnResume` adds `ClosePopup` call.
- **MainScene wiring** — NO new PrefabInstances. May need to re-bind any Inspector slot that still references deleted legacy GameObjects (audit + fix in T12.5).
- **Test infra** — `ModalTriggerPathsSmokeTest.cs` covers all 5 trigger paths + Esc-stack regression in one PlayMode run.
- **Invariants flagged:** #4 (Inspector first, FindObjectOfType fallback) — adapters already comply; ensure new `UIManager.Instance` lookups follow the same pattern.
- **Out of scope:** Any new modal prefab / DataAdapter (Stage 8 owns); any new IR / renderer (Stage 9 / 10 own); any half-B surface adapter (Stage 11 owns).

## Implementation Points — staged skeleton

Single Stage 12; 6 tasks. Order: Esc binding → DetailsPopup → MainMenu Options → MainMenu New Game → Pause Save+Load → smoke + close.

1. **T12.1 — Esc → pause menu binding.** Edit `UIManager.cs` line ~437 Esc branch. Add "if popup stack empty AND not in main-menu state → `OpenPopup(PopupType.PauseMenu)`". Do NOT modify existing pop-on-Esc behavior. Compile-check + manual smoke (Esc in PlayMode → themed pause menu visible).

2. **T12.2 — DetailsPopupController → InfoPanel rewire.** Read `Assets/Scripts/Controllers/UnitControllers/DetailsPopupController.cs`. Identify legacy popup UI activation (GameObject SetActive + child Image/TMP_Text writes). Migrate any non-event lessons-learned to glossary. DELETE the legacy UI activation block. Keep the `OnCellInfoShown` event firing logic intact. Add `UIManager.Instance.OpenPopup(PopupType.InfoPanel)` after event fire. Compile-check + smoke (Alt+click cell → themed info-panel visible, NOT legacy).

3. **T12.3 — MainMenuController Options button rewire.** Edit `MainMenuController.cs` `OnOptionsClicked()` / `OpenSettings()`. Replace legacy options-screen activation with `UIManager.Instance.OpenPopup(PopupType.SettingsScreen)`. Migrate lessons (if any). DELETE legacy options-screen GameObject from scene + any field references. Compile-check + smoke (MainMenu → Options → themed settings-screen visible).

4. **T12.4 — MainMenuController New Game button rewire.** Edit `MainMenuController.cs` New Game button handler. Replace legacy new-game flow with `UIManager.Instance.OpenPopup(PopupType.NewGameScreen)`. NewGameScreenDataAdapter already calls `MainMenuController.StartNewGame(mapSize, seed, scenarioIndex)` on confirm — leave that path intact. DELETE legacy new-game UI from scene + any field references. Compile-check + smoke (MainMenu → New Game → themed new-game-screen visible; confirm → game starts).

5. **T12.5 — PauseMenu Save+Load rewire + scene audit.** Edit `PauseMenuDataAdapter.cs` `OnSave` and `OnLoad`. Replace direct `_mainMenu.SaveGame()` / `_mainMenu.LoadGame()` with `UIManager.Instance.OpenPopup(PopupType.SaveLoadScreen)`. Edit `OnResume` to ALSO call `UIManager.Instance.ClosePopup(PopupType.PauseMenu)`. Audit MainScene Inspector slots — fix any reference to GameObjects deleted in T12.3 / T12.4 (broken refs would fail Editor open / serialize). Compile-check + smoke (Pause → Save → themed save-load visible; Pause → Resume → pause-menu closes).

6. **T12.6 — Modal trigger paths smoke test + Stage close.** Author `Assets/Scripts/Tests/UI/Modals/ModalTriggerPathsSmokeTest.cs`. PlayMode test scenarios:
   - Esc with empty stack → pause-menu modal visible.
   - Alt+click on grid cell → info-panel modal visible (legacy popup NOT active).
   - MainMenu Options → settings-screen modal visible.
   - MainMenu New Game → new-game-screen modal visible.
   - Pause → Save → save-load-screen modal visible.
   - Esc-stack close-last-first regression: open settings-screen, then info-panel; press Esc → info-panel closes, settings-screen still visible; press Esc again → settings-screen closes.

   Asserts at each step: relevant modal root `activeInHierarchy == true`, themed primitive `Image.color.a > 0`, `TMP_Text.text` non-empty, no console errors, no missing Inspector references. Run via `unity:testmode-batch` scenario.

## Relevant surfaces (for Stage 12 authoring)

- `Assets/Scripts/Managers/GameManagers/UIManager.cs` — Esc branch (line ~437); modal root fields + `OpenPopup` / `ClosePopup` methods (lines ~69–74).
- `Assets/Scripts/Managers/GameManagers/UIManager.PopupStack.cs` — `OpenPopup` / `ClosePopup` body + popup-stack push/pop logic.
- `Assets/Scripts/Managers/GameManagers/MainMenuController.cs` — `OpenSettings()` line 744 + `StartNewGame(int, int, int)` line 735 + `ResumeGame()` line 742 + Save/Load methods.
- `Assets/Scripts/Controllers/UnitControllers/DetailsPopupController.cs` — `OnCellInfoShown` event firing + legacy popup UI activation block (target for delete).
- `Assets/Scripts/UI/Modals/PauseMenuDataAdapter.cs` — `OnSave` / `OnLoad` / `OnResume` forwarders.
- `Assets/Scripts/UI/Modals/InfoPanelDataAdapter.cs` — already subscribes `DetailsPopupController.OnCellInfoShown`; populates content on event.
- `Assets/Scripts/UI/Modals/SettingsScreenDataAdapter.cs` — already wires sliders/toggles to PlayerPrefs; activates on `OpenPopup(PopupType.SettingsScreen)`.
- `Assets/Scripts/UI/Modals/NewGameScreenDataAdapter.cs` — already calls `MainMenuController.StartNewGame(mapSize, seed, scenarioIndex)` on confirm.
- `Assets/Scripts/UI/Modals/SaveLoadScreenDataAdapter.cs` — already drives slot-list populate + `GameSaveManager.SaveGame()` / `LoadGame(path)` on action.
- `Assets/Scenes/MainScene.unity` — modal roots already wired (Stage 8 outputs); audit for orphan refs after legacy deletes.
- `PopupType` enum — already includes `PauseMenu`, `InfoPanel`, `SettingsScreen`, `SaveLoadScreen`, `NewGameScreen` (Stage 8 outputs).

## Scope boundary

- **Out:** Any new modal prefab / DataAdapter (Stage 8 owns); any new IR row / renderer / tooltip-controller (Stage 9 / 10 own); any half-B surface adapter (Stage 11 owns); any new ScriptableObject / token edit; JuiceLayer behavior changes; new HUD / toolbar / overlay surface.
- **In:** 5 trigger-path rewires (Esc / DetailsPopup / MainMenu Options / MainMenu New Game / Pause Save+Load), legacy options-screen + legacy new-game-flow + legacy DetailsPopupController UI deletions, Esc-stack regression smoke + 5-trigger PlayMode smoke test.

## Locked decisions delta (for orchestrator header sync)

- Trigger rewiring follows pattern: legacy caller → `UIManager.Instance.OpenPopup(PopupType.{Modal})`. No new IR, no new adapter, no new prefab.
- Legacy surfaces (DetailsPopupController popup UI, MainMenu options screen, MainMenu legacy new-game flow) deleted in this Stage; lessons migrate to canonical docs per IF→THEN guardrail.
- `ModalTriggerPathsSmokeTest.cs` is the visual-MVP close gate — exercises all 5 trigger paths + Esc-stack regression in one PlayMode run.
- Logical execution order through plan: 1→2→3→4→5→6→10→7→8→9→11→12. Stage 12 runs LAST and gates "themed UI fully replaces legacy modal triggers" milestone.

## Player-visible checkpoint

After Stage 12 ships:

- **Esc in PlayMode** → themed pause menu visible (was: nothing).
- **Alt+click on grid cell** → themed info-panel visible (was: legacy `DetailsPopupController` UI).
- **MainMenu → Options** → themed settings-screen visible (was: legacy options screen with SFX Volume + Mute + Back).
- **MainMenu → New Game** → themed new-game-screen visible (was: legacy flow).
- **Pause → Save** / **Pause → Load** → themed save-load-screen visible (was: never opened).
- **Esc with stack non-empty** → close-last-first behavior preserved.

Combined with Stage 11 (legacy city-stats decommission), this completes the visual-MVP UI close gate.
