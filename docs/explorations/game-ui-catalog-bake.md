---
slug: game-ui-catalog-bake
target_version: 3
stages:
  - id: "9.9"
    title: "AUTO-mode fix + growth-budget panel re-introduction"
    exit: "Hud-bar AUTO button toggles `cityStats.simulateGrowth` (matches legacy `SimulateGrowthToggle.OnToggleClick`), illuminates from same bool. New BUDGET button (left of AUTO) opens `growth-budget-panel` (catalog-baked, anchored slide-out under hud-bar) with 1 total slider + 2 weight sliders (zoning ↔ roads, sum=100). Sliders drive `GrowthBudgetManager.SetGrowthBudgetPercent` + `SetCategoryPercent(Zoning|Roads, _)`. Energy/Water frozen at 0 v1. Save + load round-trips slider values via existing `growthBudgetData`. PlayMode tracer: AUTO click flips `simulateGrowth`, BUDGET click toggles panel, slider edit mutates manager, save/load preserves."
    red_stage_proof: |
      # Tracer: AUTO toggles simulateGrowth; BUDGET opens slide-out panel; sliders drive manager; save+load round-trips.
      # Pre: hud-bar baked with 10 buttons (BUDGET inserted left of AUTO); growth-budget-panel row + slider-row-2 archetype + hud_bar_icon_budget sprite seeded.
      seed_panel_row("growth_budget_panel", anchor=(1,1,1,1), pivot=(1,1), sizeDelta=(360,220), layout="vstack", padding=(16,16,16,16), gap=12)
      seed_archetype("slider-row-2", geom={"row_height":40, "label_width":110, "value_width":48}, motion={"hover":"tint"})
      seed_sprite("hud_bar_icon_budget", png_32x32)
      ir = bake_asset_registry()
      assert ir["panels"]["growth_budget_panel"]["sizeDelta"] == [360, 220]
      assert ir["archetypes"]["slider-row-2"]["row_height"] == 40
      assert "hud_bar_icon_budget" in ir["sprites"]

      # Bug 1 — AUTO toggles simulateGrowth (NOT manager.enabled)
      adapter   = scene.GetComponent("HudBarDataAdapter")
      cityStats = scene.GetComponent("CityStats")
      assert cityStats.simulateGrowth == False
      adapter._autoButton.OnClick.Invoke()
      assert cityStats.simulateGrowth == True
      assert adapter._autoButton.IlluminationAlpha == 1.0     # illumination mirrors simulateGrowth, not manager.enabled
      adapter._autoButton.OnClick.Invoke()
      assert cityStats.simulateGrowth == False
      assert adapter._autoButton.IlluminationAlpha == 0.0

      # Bug 2 — BUDGET toggles slide-out panel
      panel_root = scene.Find("GrowthBudgetPanelRoot")
      assert panel_root.activeSelf == False
      adapter._budgetButton.OnClick.Invoke()
      assert panel_root.activeSelf == True
      ctrl = panel_root.GetComponent("GrowthBudgetPanelController")
      mgr  = scene.GetComponent("GrowthBudgetManager")

      # Total slider drives manager pct
      ctrl.totalSlider.value = 30
      assert mgr.GetGrowthBudgetPercent() == 30

      # Weight sliders sum=100 invariant — moving zoning auto-redistributes roads
      ctrl.zoningWeightSlider.value = 70
      assert ctrl.roadWeightSlider.value == 30
      assert mgr.GetCategoryPercent(GrowthCategory.Zoning) == 70
      assert mgr.GetCategoryPercent(GrowthCategory.Roads)  == 30
      # Energy + Water frozen at 0 v1
      assert mgr.GetCategoryPercent(GrowthCategory.Energy) == 0
      assert mgr.GetCategoryPercent(GrowthCategory.Water)  == 0

      # Save+load round-trip via existing growthBudgetData path
      save_game()
      load_game()
      ctrl_after = scene.Find("GrowthBudgetPanelRoot").GetComponent("GrowthBudgetPanelController")
      assert ctrl_after.totalSlider.value == 30
      assert ctrl_after.zoningWeightSlider.value == 70

      # Universal-rule: BUDGET re-click closes panel (toggle); click-outside also closes
      adapter._budgetButton.OnClick.Invoke()
      assert panel_root.activeSelf == False
    tasks:
      - id: "9.9.1"
        title: "Fix HudBarDataAdapter.HandleAutoClick — toggle cityStats.simulateGrowth + illumination read from same bool"
        prefix: BUG
        kind: code
        depends_on: []
        digest_outline: "Replace `HandleAutoClick` body: drop `_autoZoningManager.enabled = enable` + `_autoRoadBuilder.enabled = enable` (dead — sim gate is `cityStats.simulateGrowth` per `AutoZoningManager.cs:159` + `AutoRoadBuilder.cs:186`). New body: `if (cityStats == null) return; cityStats.simulateGrowth = !cityStats.simulateGrowth;`. Update line 338 (illumination tick): replace `_autoZoningManager.enabled` read with `cityStats.simulateGrowth`. Inject `[SerializeField] private CityStats _cityStats` + Awake `FindObjectOfType<CityStats>` fallback (rule 4). Pattern mirrors `SimulateGrowthToggle.OnToggleClick`."
        touched_paths:
          - "Assets/Scripts/UI/HUD/HudBarDataAdapter.cs"
      - id: "9.9.2"
        title: "Seed catalog rows — growth_budget_panel + slider-row-2 archetype + hud_bar_icon_budget sprite + new hud-bar button row"
        prefix: TECH
        kind: code
        depends_on: []
        digest_outline: "New migration `00XX_seed_growth_budget_panel.sql`. (a) Sprite: `catalog_entity` slug=`hud_bar_icon_budget` kind=sprite + `sprite_detail` row + published `entity_version`; PNG drop at `Assets/UI/Sprites/HUD/hud_bar_icon_budget.png` auto-registers via Stage 9.6 AssetPostprocessor (idempotent). (b) Button: `catalog_entity` slug=`hud_bar_btn_budget` kind=button + `button_detail` (size_variant=md, action_id=`hud_bar_action_budget`, sprite_icon_entity_id→`hud_bar_icon_budget`). (c) Panel: `catalog_entity` slug=`growth_budget_panel` kind=panel + `panel_detail` (layout_template=vstack, gap_px=12, padding_json={top:16,right:16,bottom:16,left:16}, anchor jsonb=top-right pivot=(1,1) sizeDelta=(360,220)). (d) Archetype: `catalog_entity` slug=`slider-row-2` kind=archetype + archetype_detail (row_height=40, label_width=110, value_width=48, motion={hover:tint}). (e) `panel_child` rows: 3 slider-row instances under growth_budget_panel (ord 1=total, 2=zoning-weight, 3=roads-weight). (f) Re-seed `panel_child` rows for hud_bar — insert `hud_bar_btn_budget` at ord position immediately left of AUTO (current AUTO ord shifts +1; total panel children grow 9→10). Idempotent ON CONFLICT DO NOTHING."
        touched_paths:
          - "db/migrations/"
          - "Assets/UI/Sprites/HUD/"
      - id: "9.9.3"
        title: "Author GrowthBudgetPanelController — catalog-driven build + 1 total + 2 weight sliders + manager wiring"
        prefix: FEAT
        kind: code
        depends_on: ["TECH-9.9.2"]
        digest_outline: "New file `Assets/Scripts/UI/HUD/GrowthBudgetPanelController.cs`. Class `GrowthBudgetPanelController : MonoBehaviour`. Awake: `[SerializeField] private GrowthBudgetManager _manager` + `FindObjectOfType` fallback (rule 4); cache `UiAssetCatalog` accessor (rule 3 — no FindObjectOfType per-frame). Build: read `growth_budget_panel` row from catalog → instantiate `panelRoot` under hud-bar root with anchor/pivot/sizeDelta from catalog (NOT hardcoded — mirrors Stage 9.7 pattern). Read `slider-row-2` archetype → instantiate 3 slider rows (total, zoning-weight, roads-weight) with row_height + label_width + value_width from archetype. Total slider OnValueChanged → `_manager.SetGrowthBudgetPercent((int)value)`. Zoning weight OnValueChanged → set `Zoning=v`, `Roads=100-v`, call `_manager.SetCategoryPercent(GrowthCategory.Zoning, v)` + `SetCategoryPercent(GrowthCategory.Roads, 100-v)`, mirror to `roadWeightSlider.value` (suppress callback re-entry). Roads weight OnValueChanged → mirror inverse. Energy + Water hidden — `_manager.SetCategoryPercent(GrowthCategory.Energy, 0)` + `SetCategoryPercent(GrowthCategory.Water, 0)` on Show() (frozen v1; defer 4-cat surface to economy v2). Show()/Hide() flip panelRoot.SetActive. Refresh slider values from `_manager.GetGrowthBudgetPercent` + `GetCategoryPercent` on Show (save/load reload path). Click-outside dismiss via invisible `Graphic` raycast catcher (mirror `MoneyReadoutBudgetToggle` pattern — `Assets/Scripts/UI/HUD/MoneyReadoutBudgetToggle.cs:9`). Caveman XML doc — single line."
        touched_paths:
          - "Assets/Scripts/UI/HUD/GrowthBudgetPanelController.cs"
          - "Assets/Scripts/UI/TokenCatalog.cs"
      - id: "9.9.4"
        title: "Wire HudBarDataAdapter — _budgetButton + _growthBudgetPanelRoot refs + HandleBudgetClick toggler"
        prefix: FEAT
        kind: code
        depends_on: ["TECH-9.9.2", "FEAT-9.9.3"]
        digest_outline: "Add to `HudBarDataAdapter`: `[SerializeField] private IlluminatedButton _budgetButton` + `[SerializeField] private GameObject _growthBudgetPanelRoot` + `[SerializeField] private GrowthBudgetPanelController _budgetPanelController`. Awake/wire: `_budgetButton.OnClick.AddListener(HandleBudgetClick)`. New `HandleBudgetClick`: if controller != null → `controller.Toggle()` (Show if hidden, Hide if visible); fallback toggle `_growthBudgetPanelRoot.SetActive(!activeSelf)`. Illumination tick (Update): `_budgetButton.IlluminationAlpha = _growthBudgetPanelRoot.activeSelf ? 1f : 0f`. Inspector slot order updated to match catalog ord (BUDGET left of AUTO)."
        touched_paths:
          - "Assets/Scripts/UI/HUD/HudBarDataAdapter.cs"
      - id: "9.9.5"
        title: "Tracer — PlayMode test: AUTO toggles simulateGrowth, BUDGET opens panel, sliders drive manager, save+load round-trips"
        prefix: TECH
        kind: code
        depends_on: ["BUG-9.9.1", "FEAT-9.9.3", "FEAT-9.9.4"]
        digest_outline: "PlayMode test `Assets/Tests/PlayMode/AutoModeAndGrowthBudgetTracerTest.cs`. Pre: seed catalog rows (or run migration), bake hud-bar. Bug 1: invoke `_autoButton.OnClick`, assert `cityStats.simulateGrowth` flips, assert `_autoButton.IlluminationAlpha` mirrors. Bug 2: invoke `_budgetButton.OnClick`, assert `growthBudgetPanelRoot.activeSelf == true`, set `totalSlider.value=30`, assert `mgr.GetGrowthBudgetPercent()==30`, set `zoningWeightSlider.value=70`, assert `roadWeightSlider.value==30` AND `mgr.GetCategoryPercent(Zoning)==70` AND `mgr.GetCategoryPercent(Roads)==30` AND `Energy==0` AND `Water==0`. Save: `SaveGameManager.Save`; reload scene; assert sliders restore from `growthBudgetData`. Re-click BUDGET → panel hidden. Hard-fails if controller falls back to legacy 4-slider topology or if AUTO flips `manager.enabled` instead of `simulateGrowth`."
        touched_paths:
          - "Assets/Tests/PlayMode/AutoModeAndGrowthBudgetTracerTest.cs"
notes:
  - "Plan extension: parent = game-ui-catalog-bake v2 (slug-keyed table — no integer id). target_version=3."
  - "Stage order: 9.5 (done) → 9.6 (done) → 9.7 (done) → 9.8 (done) → 9.9 (this stage) → 7 (MVP closeout, pending)."
  - "Stage 9.9 inserted between Stage 9.8 (subtype expansion done) and Stage 7 (closeout). 9.9 is the final 'additional polish' stage in the 9.x series."
  - "Consumes DEC-A25 asset-pipeline-standard-v1 — no new arch decision (catalog-bake pattern reused for new panel + button + sprite + archetype rows)."
  - "Backend `GrowthCategory` enum kept 4-cat (Roads, Energy, Water, Zoning) — UI surfaces only Zoning + Roads as weight sliders; Energy + Water frozen at 0 v1. Cleaner backend > UI façade per rule 6 (don't bloat manager with parallel enum)."
  - "Naming: `growth_budget_panel` (NEW) ≠ `BudgetPanel` (existing tax-envelope panel — `Assets/Scripts/Managers/GameManagers/BudgetPanel.cs`, opens via `MoneyReadoutBudgetToggle` on money readout). Two distinct panels, do NOT collide on PopupType enum."
  - "§Visibility Delta — Player gains: (1) AUTO button actually starts city growth (was no-op), (2) new BUDGET button (left of AUTO, slider icon) opens slide-out under hud-bar with 3 sliders (total budget %, zoning weight, roads weight) — adjustable mid-game, persists in save."
---

# Auto-mode fix + growth-budget panel re-introduction

## Why this exploration

Two regressions surfaced post-Stage-9.8, both block MVP closeout (Stage 7):

1. **AUTO button no-op.** Hud-bar AUTO button (right cluster) does not start auto-mode. Both auto-zoning + auto-road systems stay idle even after click.
2. **No growth-budget panel.** Need a **new** panel where the player sets (a) total % of city budget routed to auto-growth, (b) weighted split between zoning and roads. Authored fresh through the catalog-bake pipeline (Stage-9 conformance). Opened by a **separate hud-bar toggler button** — AUTO stays a pure on/off toggle.

Goal: append both as one new stage `9.9` of `game-ui-catalog-bake`, sequenced **before** Stage 7 closeout.

## Problem 1 — AUTO button no-op

### Surface

`Assets/Scripts/UI/HUD/HudBarDataAdapter.cs:268`

```csharp
private void HandleAutoClick()
{
    bool enable = _autoZoningManager == null ? true : !_autoZoningManager.enabled;
    if (_autoZoningManager != null) _autoZoningManager.enabled = enable;
    if (_autoRoadBuilder != null) _autoRoadBuilder.enabled = enable;
}
```

### Root cause

Handler flips `MonoBehaviour.enabled` on the two managers. Actual sim gate is **`cityStats.simulateGrowth`** inside `ProcessTick`:

- `Assets/Scripts/Managers/GameManagers/AutoZoningManager.cs` — `if (!cityStats.simulateGrowth) return;`
- `Assets/Scripts/Managers/GameManagers/AutoRoadBuilder.cs:186` — `if (!cityStats.simulateGrowth) return;`

Reference for correct behavior — legacy `Assets/Scripts/Controllers/UnitControllers/SimulateGrowthToggle.cs`:

```csharp
void OnToggleClick()
{
    if (cityStats == null) return;
    cityStats.simulateGrowth = !cityStats.simulateGrowth;
    RefreshVisual();
}
```

### Fix

`HandleAutoClick` must toggle `cityStats.simulateGrowth`. Manager-`enabled` flip can stay as belt-and-suspenders, OR drop it — design question: does anything else read `_autoZoningManager.enabled` other than ProcessTick? If no, drop the redundant flip.

Visual feedback: AUTO button must reflect on/off state (illuminated styling). Already an `IlluminatedButton` — needs a SetIlluminated call wired to the new bool.

## Problem 2 — New growth-budget panel

### Backend that already exists (reference, not source)

`GrowthBudgetManager` + `CityStats.growthBudgetData` already track:

- `monthlyBudgetPercent` (total budget % of projected income)
- `GrowthCategory { Roads, Energy, Water, Zoning }` per-cat percent splits + spent counters

Legacy `GrowthBudgetSlidersController.cs` exists but **NOT being revived** — built for a different HUD layout, 4-cat full-expose topology. New panel authored fresh.

### What we're building

- New panel slug (TBD) authored as **catalog row** baked via Stage-9 pipeline.
- 1 total slider → drives `GrowthBudgetManager.monthlyBudgetPercent` (or equivalent).
- 2 weight sliders: zoning ↔ roads, sum=100, mutual redistribution.
- Energy + Water: backend-only, hidden from UI (auto-balanced or frozen — design Q).
- New toggler button in hud-bar opens panel; click-outside / re-click closes.
- AUTO button stays as pure on/off toggle for `cityStats.simulateGrowth`.

### Open design questions

1. **Slider topology backend wiring.** Reuse 4-cat `GrowthCategory` enum + just hide energy/water in UI (frozen at 0 or split evenly), OR introduce a 2-cat façade on top of the manager? Cleaner enum vs cleaner backend.
2. **Hud-bar slot.** Where does new toggler button land in cluster order? Right cluster has AUTO/zoom-in/zoom-out/stats/mini-map. Insert "BUDGET" between AUTO and zoom-in? Or left of AUTO?
3. **Panel shape.** Full popup (PopupType + UIManager.OpenPopup) or anchored slide-out from hud-bar (like stats panel toggle)?
4. **Catalog authoring.** Panel slug? Archetype reuse vs new archetype? Rows go in DB seed migration like Stage-9.7 picker rows.
5. **Persistence.** Slider values persist with save game via existing `CityStats.growthBudgetData` path — confirm new controller reads/writes through same SO.
6. **Sprite for budget toggler button.** New IlluminatedButton needs sprite slug → sprite-catalog row (Stage 9.6 AssetPostprocessor handles registration if PNG dropped under `Assets/UI/Sprites/HUD/`).
7. **New controller class.** Author new `GrowthBudgetPanelController` (or similar) bound to baked panel — don't extend legacy 4-slider controller.

## Architecture impact

- **`HudBarDataAdapter`** — add `_budgetToggleButton` IlluminatedButton ref + handler; add `_growthBudgetPanelRoot` GameObject ref. Wire `cityStats.simulateGrowth` into `HandleAutoClick`. Add `SetIlluminated(cityStats.simulateGrowth)` refresh on Update or via CityStats event.
- **New `GrowthBudgetPanelController`** (name TBD) — authored fresh, bound to baked panel root, drives `GrowthBudgetManager` via 2-slider + 1-total topology. Legacy `GrowthBudgetSlidersController` left in tree as historical reference (or retired in cleanup pass).
- **Hud-bar prefab + bake** — add new button slot in catalog row; re-bake hud-bar via Stage-9 pipeline.
- **UI catalog (`UiAssetCatalog` rows)** — new panel row `growth-budget-panel` + new button slug for toggler.
- **Sprite-catalog** — 1 new HUD sprite for budget toggler.
- **Save/load** — verify `growthBudgetData` still serializes; no schema change expected.

## Implementation points (preliminary)

- HudBarDataAdapter HandleAutoClick fix → trivial 3-line patch.
- HudBarDataAdapter add budget-toggle handler + panel-root toggle → ~15 lines.
- IlluminatedButton AUTO state refresh on Update tick (read cityStats.simulateGrowth).
- New `GrowthBudgetPanelController` — 1 total slider + 2 weight sliders (zoning + roads), redistribute on change so zoning% + road% = 100. Energy/Water backend cats frozen at 0 (or split fixed) for v1.
- Hud-bar catalog row update + bake → 1 panel row + 1 button row + 1 sprite row.
- New panel catalog row authored to standard panel archetype, baked via Stage-9 pipeline.
- PlayMode tracer: AUTO click flips simulateGrowth, sliders adjust manager state, save+load persists.

## Tracer (Red-Stage Proof seed)

```python
# Pre: hud-bar baked; growth-budget-panel row + budget-toggle button row + sprite row in catalog.
adapter = scene.GetComponent("HudBarDataAdapter")
cityStats = scene.GetComponent("CityStats")

# Bug 1 verify
assert cityStats.simulateGrowth == False
adapter._autoButton.OnClick.Invoke()
assert cityStats.simulateGrowth == True
adapter._autoButton.OnClick.Invoke()
assert cityStats.simulateGrowth == False

# Bug 2 verify
panel_root = scene.Find("growth-budget-panel-root")
assert panel_root.activeSelf == False
adapter._budgetToggleButton.OnClick.Invoke()
assert panel_root.activeSelf == True
ctrl = panel_root.GetComponent("GrowthBudgetPanelController")  # new class
ctrl.totalSlider.value = 30
assert ctrl.growthBudgetManager.monthlyBudgetPercent == 30
ctrl.zoningWeightSlider.value = 70
# road auto-redistributes to 30
assert ctrl.roadWeightSlider.value == 30
```

## Next phase

Hand off to `design-explore` subagent — phases compare → select → architect → subsystem-impact → impl-points → review → emit handoff yaml frontmatter for `master-plan-extend`.

---

## Design Expansion

### Approaches surveyed

#### A — Single combined fix + panel stage (this stage 9.9 — recommended)

- **Pros:** Both regressions land together → one ship-cycle pass, one closeout, one tracer covers AUTO + BUDGET. Catalog-bake conformance (panel + button + sprite + archetype rows) reuses Stage 9.7/9.8 pattern → mechanical authoring. Bug 1 fix (3 lines) + Bug 2 build share `HudBarDataAdapter` edit surface — single PR.
- **Cons:** 5 tasks (fix + catalog seed + controller + adapter wire + tracer) — under ship-cycle 80k cap. None.
- **Effort:** medium — controller is new (~120 LoC), catalog seed migration is mechanical, fix is trivial.

#### B — Split into 9.9a (AUTO fix) + 9.9b (budget panel)

- **Pros:** AUTO fix ships in hours (3-line patch + tracer); budget panel can take its own cycle.
- **Cons:** 2 stages = 2 closeouts = 2 PRs. AUTO fix without illumination read-source change = half-done (illumination still reads `manager.enabled`). Tracer redundancy.
- **Effort:** medium — same total work, doubled ceremony.

#### C — AUTO fix only, defer budget panel to v3

- **Pros:** Fastest path to MVP closeout. Bug 1 unblocks player from clicking AUTO. Stage 7 ships.
- **Cons:** User explicitly asked for budget panel now ("captures 7 design questions"). Deferral re-creates the same orphaned-debt pattern Phase B postmortem flagged. Budget panel surface (catalog rows + controller) is mechanical work — no design debt left to resolve.
- **Effort:** low for fix, but high deferral cost (recurrence).

#### D — Revive legacy `GrowthBudgetSlidersController` instead of new controller

- **Pros:** Reuses existing class.
- **Cons:** 4-slider topology mismatches new 2-weight + 1-total UX; legacy was hand-built (not catalog-driven), violates Stage 9 conformance mandate; class is coupled to old HUD layout. Rewrite ≈ new class with extra dep churn.
- **Effort:** high — refactor cost > new class.

### Decision matrix

| Approach | Constraint fit | Effort | Output control | Maintainability | Dep risk |
|---|---|---|---|---|---|
| A — Combined 9.9 | High (bug fix + new panel both blocking 7) | Medium (5 tasks, ≤80k cap) | High (single tracer asserts both) | High (catalog-driven, no hand-wired) | Low |
| B — Split 9.9a/b | Medium (doubled ceremony) | Medium (same work) | Medium (split tracers) | High | Low |
| C — Fix only, defer panel | Low (user reversed deferral once already) | Low | Low (deferred surface) | Low (recurring debt) | Medium (re-explore later) |
| D — Revive legacy controller | Low (4-slider topology mismatch) | High (refactor) | Low (not catalog-conformant) | Low | Medium |

### Chosen Approach

**Approach A — single combined Stage 9.9 (AUTO fix + new growth-budget panel).** Both regressions block Stage 7. Catalog-bake authoring (panel + button + sprite + archetype + 3 child rows) is mechanical; controller is the only net-new code (~120 LoC). Single tracer asserts both fixes. 5 tasks fit ship-cycle cap.

Stage order: `9.5 (done) → 9.6 (done) → 9.7 (done) → 9.8 (done) → 9.9 (this) → 7 (MVP closeout, pending)`.

### Architecture Decision

Consumes **DEC-A25 `asset-pipeline-standard-v1`** (existing — covers `catalog_entity` / `panel_detail` / `button_detail` / `sprite_detail` / `panel_child` spine). Stage 9.9 = consumer of existing decision, NOT a new arch decision. No `arch_decision_write` / `arch_changelog_append` needed. New `slider-row-2` archetype is a row, not an architectural surface.

### Architecture

| Component | Layer | Role |
|---|---|---|
| `HudBarDataAdapter.HandleAutoClick` (refactored) | runtime C# | Toggles `cityStats.simulateGrowth` (single source of truth for sim gate per `AutoZoningManager.cs:159` + `AutoRoadBuilder.cs:186`). Drops dead `manager.enabled` flip. Mirrors `SimulateGrowthToggle.OnToggleClick` pattern. |
| `HudBarDataAdapter` illumination tick | runtime C# | Update line 338: `_autoButton.IlluminationAlpha = cityStats.simulateGrowth ? 1f : 0f` (was `_autoZoningManager.enabled`). |
| `growth_budget_panel` panel row | catalog (`panel_detail`) | New panel: layout=vstack, gap_px=12, padding=16/16/16/16, anchor top-right pivot=(1,1), sizeDelta=(360,220). Anchored slide-out below hud-bar. |
| `slider-row-2` archetype row | catalog (`archetype_detail`) | Tile shape: row_height=40, label_width=110, value_width=48. `motion: { hover: "tint" }`. Reused for all 3 sliders in the panel. |
| `hud_bar_btn_budget` button row | catalog (`button_detail`) | New hud-bar button. size_variant=md, action_id=`hud_bar_action_budget`, sprite_icon→`hud_bar_icon_budget`. Insert at panel_child ord position immediately left of AUTO. |
| `hud_bar_icon_budget` sprite row | catalog (`sprite_detail`) | Hand-authored 32×32 PNG dial/sliders glyph. PNG drop at `Assets/UI/Sprites/HUD/hud_bar_icon_budget.png` → Stage 9.6 AssetPostprocessor auto-registers. |
| `GrowthBudgetPanelController` (NEW) | runtime C# | Catalog-driven build — reads panel row + slider archetype from `UiAssetCatalog` (rule 3 cache in Awake). Exposes `totalSlider`, `zoningWeightSlider`, `roadWeightSlider`. Drives `GrowthBudgetManager` via `SetGrowthBudgetPercent` + `SetCategoryPercent(Zoning|Roads)`. Energy/Water frozen at 0 v1 on Show(). Click-outside dismiss via invisible `Graphic` raycast catcher (mirrors `MoneyReadoutBudgetToggle.cs:9`). |
| `HudBarDataAdapter._budgetButton` + `_growthBudgetPanelRoot` | runtime C# | New `[SerializeField]` slots wired via Inspector (rule 4) + Awake `FindObjectOfType` fallbacks. `HandleBudgetClick` toggles panel via controller; illumination mirrors panel `activeSelf`. |
| `GrowthCategory` enum | runtime C# | UNCHANGED — keeps 4-cat (Roads, Energy, Water, Zoning). UI hides Energy + Water (frozen at 0 v1). Cleaner backend > UI façade per rule 6 (don't bloat manager with parallel 2-cat enum). |
| Save/load path | runtime C# (existing) | UNCHANGED — `CityStats.growthBudgetData` already serializes via SO save path. Controller reads/writes through `GrowthBudgetManager` getters/setters → no schema change. |

### Red-Stage Proof — Stage 9.9

```python
# Tracer: AUTO toggles simulateGrowth (NOT manager.enabled); BUDGET opens slide-out; sliders drive manager; save+load round-trips
# Pre: hud-bar re-baked with 10 buttons (BUDGET inserted left of AUTO); growth_budget_panel + slider-row-2 + hud_bar_icon_budget seeded
seed_panel_row("growth_budget_panel", anchor=(1,1,1,1), pivot=(1,1), sizeDelta=(360,220), layout="vstack", padding=(16,16,16,16), gap=12)
seed_archetype("slider-row-2", row_height=40, label_width=110, value_width=48, motion={"hover":"tint"})
seed_sprite("hud_bar_icon_budget", png_32x32)
ir = bake_asset_registry()
assert ir["panels"]["growth_budget_panel"]["sizeDelta"] == [360, 220]
assert ir["archetypes"]["slider-row-2"]["row_height"] == 40
assert "hud_bar_icon_budget" in ir["sprites"]

# Bug 1 — AUTO toggles simulateGrowth
adapter   = scene.GetComponent("HudBarDataAdapter")
cityStats = scene.GetComponent("CityStats")
assert cityStats.simulateGrowth == False
adapter._autoButton.OnClick.Invoke()
assert cityStats.simulateGrowth == True
assert adapter._autoButton.IlluminationAlpha == 1.0    # mirrors simulateGrowth, NOT manager.enabled
adapter._autoButton.OnClick.Invoke()
assert cityStats.simulateGrowth == False

# Bug 2 — BUDGET opens panel + sliders drive manager
panel_root = scene.Find("GrowthBudgetPanelRoot")
assert panel_root.activeSelf == False
adapter._budgetButton.OnClick.Invoke()
assert panel_root.activeSelf == True
ctrl = panel_root.GetComponent("GrowthBudgetPanelController")
mgr  = scene.GetComponent("GrowthBudgetManager")

ctrl.totalSlider.value = 30
assert mgr.GetGrowthBudgetPercent() == 30

ctrl.zoningWeightSlider.value = 70
assert ctrl.roadWeightSlider.value == 30                            # auto-redistribute, sum=100
assert mgr.GetCategoryPercent(GrowthCategory.Zoning) == 70
assert mgr.GetCategoryPercent(GrowthCategory.Roads)  == 30
assert mgr.GetCategoryPercent(GrowthCategory.Energy) == 0           # frozen v1
assert mgr.GetCategoryPercent(GrowthCategory.Water)  == 0           # frozen v1

# Save+load round-trips through existing growthBudgetData
save_game(); load_game()
ctrl_after = scene.Find("GrowthBudgetPanelRoot").GetComponent("GrowthBudgetPanelController")
assert ctrl_after.totalSlider.value == 30
assert ctrl_after.zoningWeightSlider.value == 70

# BUDGET re-click closes panel
adapter._budgetButton.OnClick.Invoke()
assert panel_root.activeSelf == False
```

### Subsystem Impact

Touched (count = 6):

1. **`Assets/Scripts/UI/HUD/HudBarDataAdapter.cs`** — `HandleAutoClick` rewrite (3 lines), illumination tick line 338 read-source swap, new `_budgetButton` + `_growthBudgetPanelRoot` + `_budgetPanelController` `[SerializeField]` slots + `HandleBudgetClick`. Invariants: rule 3 (no `FindObjectOfType` per-frame — cache `_cityStats` in Awake), rule 4 (Inspector + Awake `FindObjectOfType` fallback for new refs).
2. **`Assets/Scripts/UI/HUD/GrowthBudgetPanelController.cs`** (NEW) — catalog-driven build, slider-redistribute logic, manager wiring, click-outside dismiss. Invariants: rule 3 (cache `UiAssetCatalog` accessor in Awake — no per-frame catalog reads), rule 4 (Inspector + Awake fallback for `_manager`), rule 6 (extract responsibility — new class, do NOT extend legacy `GrowthBudgetSlidersController`).
3. **`Assets/Scripts/UI/TokenCatalog.cs`** — small add: lookup helpers for `slider-row-2` archetype tokens (row_height, label_width, value_width, motion.hover).
4. **`db/migrations/00XX_seed_growth_budget_panel.sql`** (NEW) — 1 sprite row + 1 button row + 1 panel row + 1 archetype row + 3 panel_child rows (sliders) + 1 panel_child row (BUDGET button into hud-bar). Idempotent ON CONFLICT DO NOTHING (mirrors migration 0064 pattern).
5. **`Assets/UI/Sprites/HUD/hud_bar_icon_budget.png`** (NEW) — hand-authored 32×32 PNG. Stage 9.6 AssetPostprocessor auto-registers sprite-catalog row on import.
6. **`Assets/Tests/PlayMode/AutoModeAndGrowthBudgetTracerTest.cs`** (NEW) — PlayMode tracer covers Bug 1 + Bug 2 + save/load + universal toggle behavior.

Invariant flags: rule 3, rule 4, rule 6 (all addressed in `GrowthBudgetPanelController` + `HudBarDataAdapter` patches). NO HeightMap / road-modify / water-place / cliff-face touch points → invariants 1, 2, 7, 8, 9, 10 not triggered.

### Implementation Points

**Stage 9.9 tasks (5):**

1. **9.9.1 — BUG-9.9.1 — AUTO fix.** Rewrite `HudBarDataAdapter.HandleAutoClick` to flip `cityStats.simulateGrowth`. Drop dead `manager.enabled` flips (sim gate is `simulateGrowth`, not `MonoBehaviour.enabled`). Update illumination tick at line 338 to read `cityStats.simulateGrowth`. Add `[SerializeField] private CityStats _cityStats` + Awake fallback. ~6 line net diff.
2. **9.9.2 — TECH-9.9.2 — Catalog seed.** New SQL migration: sprite (`hud_bar_icon_budget`) + button (`hud_bar_btn_budget`) + panel (`growth_budget_panel`) + archetype (`slider-row-2`) + 3 panel_child slider rows + 1 panel_child for BUDGET button into hud-bar (left of AUTO). Drop PNG at `Assets/UI/Sprites/HUD/`. Idempotent ON CONFLICT.
3. **9.9.3 — FEAT-9.9.3 — `GrowthBudgetPanelController`.** New file under `Assets/Scripts/UI/HUD/`. Catalog-driven build (read panel row + archetype from `UiAssetCatalog`, rule 3 cache). 3 sliders: total drives `SetGrowthBudgetPercent`; zoning/roads weight sliders sum=100 mutual redistribute via callback suppression; Energy/Water frozen at 0 on Show. Click-outside dismiss via invisible `Graphic` raycast catcher. Show/Hide toggles `panelRoot.SetActive`.
4. **9.9.4 — FEAT-9.9.4 — Hud-bar adapter wire.** Add `_budgetButton` + `_growthBudgetPanelRoot` + `_budgetPanelController` slots to `HudBarDataAdapter`. New `HandleBudgetClick` calls `controller.Toggle()`. Illumination tick mirrors `panelRoot.activeSelf`. Inspector slot order matches catalog ord (BUDGET left of AUTO).
5. **9.9.5 — TECH-9.9.5 — Tracer.** PlayMode test asserts: AUTO flips `simulateGrowth` + illumination, BUDGET opens panel, sliders drive manager (total + zoning/roads sum=100 + Energy/Water=0), save/load round-trips, BUDGET re-click closes. Hard-fails on regression to legacy 4-slider or `manager.enabled` flip.

### Examples

**`HudBarDataAdapter.HandleAutoClick` (refactored):**

```csharp
// Before — flipped MonoBehaviour.enabled (dead — sim gate is simulateGrowth):
//   bool enable = _autoZoningManager == null ? true : !_autoZoningManager.enabled;
//   if (_autoZoningManager != null) _autoZoningManager.enabled = enable;
//   if (_autoRoadBuilder  != null) _autoRoadBuilder.enabled  = enable;

private void HandleAutoClick()
{
    if (_cityStats == null) return;
    _cityStats.simulateGrowth = !_cityStats.simulateGrowth;
    // Mirrors SimulateGrowthToggle.OnToggleClick — single source of truth for ProcessTick gate.
}

// Update tick illumination (was line 338 reading _autoZoningManager.enabled):
private void Update()
{
    // ...existing money / population / happiness channels...
    if (_autoButton != null && _cityStats != null)
        _autoButton.IlluminationAlpha = _cityStats.simulateGrowth ? 1f : 0f;
}
```

**`GrowthBudgetPanelController` slider-redistribute (suppress callback re-entry):**

```csharp
private bool _suppressCallbacks;

private void OnZoningWeightChanged(float v)
{
    if (_suppressCallbacks) return;
    int zoning = (int)v;
    int roads  = 100 - zoning;
    _manager.SetCategoryPercent(GrowthCategory.Zoning, zoning);
    _manager.SetCategoryPercent(GrowthCategory.Roads,  roads);

    _suppressCallbacks = true;
    roadWeightSlider.value = roads;
    _suppressCallbacks = false;
}

private void OnRoadsWeightChanged(float v)
{
    if (_suppressCallbacks) return;
    int roads  = (int)v;
    int zoning = 100 - roads;
    _manager.SetCategoryPercent(GrowthCategory.Roads,  roads);
    _manager.SetCategoryPercent(GrowthCategory.Zoning, zoning);

    _suppressCallbacks = true;
    zoningWeightSlider.value = zoning;
    _suppressCallbacks = false;
}

public void Show()
{
    // Refresh from manager (save/load reload + Energy/Water freeze v1).
    _suppressCallbacks = true;
    totalSlider.value         = _manager.GetGrowthBudgetPercent();
    zoningWeightSlider.value  = _manager.GetCategoryPercent(GrowthCategory.Zoning);
    roadWeightSlider.value    = _manager.GetCategoryPercent(GrowthCategory.Roads);
    _suppressCallbacks = false;

    _manager.SetCategoryPercent(GrowthCategory.Energy, 0);   // frozen v1 — UI hides Energy/Water
    _manager.SetCategoryPercent(GrowthCategory.Water,  0);
    panelRoot.SetActive(true);
}
```

**Catalog seed migration shape (mirrors `0064_seed_hud_bar_panel.sql`):**

```sql
-- Sprite
INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('sprite', 'hud_bar_icon_budget', 'Hud Bar Icon Budget')
ON CONFLICT (kind, slug) DO NOTHING;

INSERT INTO sprite_detail (entity_id, assets_path, pixels_per_unit, provenance)
SELECT ce.id, 'Assets/UI/Sprites/HUD/hud_bar_icon_budget.png', 100, 'hand'
FROM catalog_entity ce WHERE ce.kind='sprite' AND ce.slug='hud_bar_icon_budget'
ON CONFLICT (entity_id) DO NOTHING;

-- Panel growth_budget_panel + 3 slider children (ord 1=total, 2=zoning, 3=roads)
INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('panel', 'growth_budget_panel', 'Growth Budget Panel')
ON CONFLICT (kind, slug) DO NOTHING;

INSERT INTO panel_detail (entity_id, layout_template, layout, padding_json, gap_px)
SELECT ce.id, 'vstack', 'vstack',
       '{"top":16,"right":16,"bottom":16,"left":16}'::jsonb, 12
FROM catalog_entity ce WHERE ce.kind='panel' AND ce.slug='growth_budget_panel'
ON CONFLICT (entity_id) DO UPDATE SET layout=EXCLUDED.layout, gap_px=EXCLUDED.gap_px;

-- BUDGET button inserted into hud_bar at ord = (current AUTO ord); existing AUTO ord shifts +1
-- (handled by adjusting panel_child rows on hud_bar — re-seed with new ord layout).
```

### Review Notes

Reviewer pass identified 0 BLOCKING + 3 NON-BLOCKING + 2 SUGGESTIONS. Blocking risks resolved inline before persist:

**Resolved during synthesis (would have been BLOCKING):**

- **R1 — Naming collision with existing `BudgetPanel`.** Existing `Assets/Scripts/Managers/GameManagers/BudgetPanel.cs` is the **tax-envelope** panel (7 sub-type sliders), opened via `MoneyReadoutBudgetToggle` from money readout, registered as `PopupType.BudgetPanel`. Resolution: new panel slug = `growth_budget_panel` (DB-side) + new controller class = `GrowthBudgetPanelController` (runtime-side). Distinct from `BudgetPanel` — no PopupType enum collision (slide-out, not popup-stack-managed). Documented in §notes lock.
- **R2 — `manager.enabled` dead flip.** Verified `_autoZoningManager.enabled` read at `HudBarDataAdapter.cs:338` (illumination tick). Resolution: replace illumination read-source with `_cityStats.simulateGrowth`. Drops `manager.enabled` write entirely → no consumer left, safe to remove.
- **R3 — `GrowthCategory` 4-cat vs 2-cat façade.** Resolution: keep 4-cat backend per rule 6 (don't bloat manager with parallel enum). UI surfaces only Zoning + Roads weight sliders; Energy + Water frozen at 0 v1. Future utilities-economy-v2 can re-surface Energy + Water on the same panel — schema admits.

**NON-BLOCKING (carry into Stage 9.9 author phase):**

- **N1 — Click-outside dismiss raycast catcher.** Pattern is `MoneyReadoutBudgetToggle` invisible `Graphic` (zero vertex cost). Confirm full-screen catcher behind `growthBudgetPanelRoot` doesn't block other HUD interactions when panel is hidden — disable catcher GameObject when panel hidden.
- **N2 — Slider value-readout text.** Each slider row has a value readout (right side of row, 48 px wide per archetype). v1: integer % display ("30%"). Defer formatting polish (color tinting, locale-aware separator) to UX polish stage.
- **N3 — Save/load reload Show timing.** `SaveGameManager.Load` rebuilds scene; if panel was open before save, it should restore visible. Decision: panel always opens hidden post-load; user re-opens via BUDGET button. Avoids complex save/restore of UI visibility state.

**SUGGESTIONS (not gating):**

- **S1 — Energy + Water surface.** v1 freezes both at 0. Once utilities economy ships, re-surface as expandable section in same panel ("Show advanced" disclosure). Schema admits 4 sliders without archetype change.
- **S2 — Per-slider tooltip.** Hover tile on each slider explains effect ("% of monthly budget routed to growth", "Weight zoning vs roads"). Couples to Stage 9.5 motion.hover enum (`tint` extends to tooltip-anchor).
- **S3 — BUDGET button position re-evaluation.** Locked left of AUTO. If player feedback shows "set then go" left→right scan doesn't read, swap to right of AUTO. One catalog seed migration line — cheap to revisit.

### Expansion metadata

- **Date:** 2026-05-06
- **Model:** claude-opus-4-7
- **Approach selected:** A (combined Stage 9.9 — AUTO fix + new growth-budget panel)
- **Mode:** standard (Approaches synthesized inline from 7 design questions; user delegated all phases)
- **Phases run:** 0, 1, 2, 3, 4, 5, 6, 7, 8, 9
- **Phases skipped:** 0.5 (interview synthesized from chat-provided locks + seed doc context), 2.5 (consumes existing DEC-A25, no new arch decision)
- **BLOCKING items resolved:** 3 (R1 naming, R2 dead flip, R3 4-cat vs 2-cat)
- **NON-BLOCKING carried:** 3 (N1, N2, N3)
- **Suggestions:** 3 (S1, S2, S3)

