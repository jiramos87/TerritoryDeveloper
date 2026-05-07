---
purpose: "Reference spec for UI / UX Design System — Spec."
audience: agent
loaded_by: router
slices_via: spec_section
---
# UI / UX Design System — Spec

## Overview

This spec defines **foundations**, **components**, and **patterns** for Territory Developer’s in-game UI so that backlog issues can reference concrete sections. The **UI-as-code program** (IDE- and agent-friendly **UI** workflows) is **§ Completed** — trace [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) **Recent archive**; **codebase inventory (uGUI)** lives in **this spec** (**Codebase inventory (uGUI)** below). **Executable issues:** [`BACKLOG.md`](../../BACKLOG.md). **As-built** tables in **§1**, **§4**, and major **§2–§3** surfaces are sourced from the committed machine snapshot [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) (refresh when scenes change; see [`docs/reports/README.md`](../../docs/reports/README.md)); the JSON was last promoted from **Postgres** **`editor_export_ui_inventory`** (export row **id** **8**, repo **git** **`2245403e3531b5779c52b3480be6bd0ba085946c`**). **Scenes:** **UI** spans **`MainMenu`**, **`CityScene`** (future **`CityScene`**), and future surfaces (e.g. **`RegionScene`**) — exports and prose are **per scene**.

### As-built vs target

- **As-built (current):** What the game **actually** uses today — **Canvas** settings, **colors**, **fonts** / sizes, **margins**, **anchors**, **HUD** / **toolbar** / **popup** layout, and representative **UX** behaviors. Primary **city** snapshot: **`CityScene.unity`** → **`UI/City/Canvas`** (paths in JSON are relative to that **Canvas** root, e.g. `Canvas/ControlPanel`). **Main menu:** **`MainMenu.unity`** ships a serialized **`MainMenuCanvas`** (**Screen Space Overlay**, **1280×720** scaler) with **`MainMenuController`** on **`MenuBootstrap`**; overlay panels (**Load City**, **Options**) are still created at runtime when their **Inspector** references are **null** — see **`MainMenuController`**. If **all** **Button** references are **null**, **`BuildUI()`** remains the dev fallback (**§3.0**).
- **Target (planned):** Future layout or tokens defined by **BACKLOG** issues (e.g. **FEAT-** polish rows). Keep **Target** subsections or labeled rows **alongside** **as-built** so refactors stay traceable.

### Domain vocabulary

Backlog items and player-facing copy that name gameplay systems should use [`glossary.md`](glossary.md) terms (**Game notification**, **street**, **interstate**, **map border**, **road validation pipeline**, etc.) so wording matches reference specs and **territory-ia** tools (`glossary_discover`, `glossary_lookup`).

**Status:** Draft — **§1**–**§4** and **§2**–**§3** carry **as-built** rows from the **UI** inventory baseline (committed JSON + **`ui-design-system.md`** prose); **§1.4** / **§1.5** remain light until an issue scopes icon / motion work. **Glossary:** **UI design system (reference spec)**.

## Related files (code and assets)

| Area | Location / notes |
|------|------------------|
| Main UI orchestration | `UIManager.cs` + **`UIManager.PopupStack.cs`**, **`UIManager.Hud.cs`**, **`UIManager.Toolbar.cs`**, **`UIManager.Utilities.cs`** |
| Main menu | `Assets/Scripts/Managers/GameManagers/MainMenuController.cs` — serialized **`MainMenuCanvas`** + **`MenuBootstrap`**; `BuildUI()` fallback when buttons unassigned |
| **UiTheme** (tokens) | `Assets/Scripts/Managers/GameManagers/UiTheme.cs` — default asset **`Assets/UI/Theme/DefaultUiTheme.asset`** |
| Popup controllers | `Assets/Scripts/Controllers/UnitControllers/*Popup*.cs`, `DetailsPopupController.cs`, `DataPopupController.cs`, etc. |
| HUD / stats | `CityStatsUIController.cs`, `UIManager.cs` (many legacy `Text` references) |
| Input vs UI | `EventSystem`, `CameraController.cs`, `GridManager.cs` (`IsPointerOverGameObject` patterns) |
| **City** scene asset | `Assets/Scenes/CityScene.unity` — **`UI/City/Canvas`** hierarchy (authoritative in **Editor**) |
| **Main menu** scene asset | `Assets/Scenes/MainMenu.unity` — serialized **`MainMenuCanvas`** / **`MainMenuRoot`** / menu **Buttons** + **`EventSystem`**; **`MenuBootstrap`** holds **`MainMenuController`** |
| **As-built** JSON (committed) | [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) — bounded **`scenes[]`** sample |

Add prefab paths under `Assets/` as they are standardized.

### Codebase inventory (uGUI)

*Scene object names and **Inspector** wiring can drift — verify in **Unity** when updating **as-built** docs or refactors. Update this subsection when hierarchies or roles change.*

**Stack:** **Unity UI (uGUI)** — **Canvas**, **Graphic** (**Image**, **Text** / **TMP**, etc.), **EventSystem**. Primary orchestrator: **`UIManager`** (`Territory.UI`) — **`partial`** across **`UIManager.cs`** (fields, lifecycle) + **`UIManager.PopupStack.cs`**, **`UIManager.Hud.cs`**, **`UIManager.Toolbar.cs`**, **`UIManager.Utilities.cs`**. **`CursorManager`**, **`GameNotificationManager`**, and **UnitControllers** handle focused interactions.

**Architectural placement** (see also **`ia/specs/architecture/layers.md`**): **UI layer** — **`UIManager`**, **`CursorManager`**, **`GameNotificationManager`**, controllers. **Input** — **`GridManager`** and others gate world input when the pointer is over UI (**`IsPointerOverGameObject`**); scroll vs camera is a recurring UX area (**BACKLOG**).

**Primary entry points**

| File | Role |
|------|------|
| `UIManager.cs` + **`UIManager.*.cs` partials** | Main **HUD**, **popups** (**PopupType**: load game, details, building selector, stats, tax), **toolbar** / zone and tool selection, demand visualization |
| `Assets/Scripts/Managers/GameManagers/CursorManager.cs` | Cursor state with tools and UI |
| `Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs` | **Game notification** path (**singleton**, `DontDestroyOnLoad`) |

**Controllers (representative)** — **`GameControllers/`**: **`CameraController.cs`** (zoom vs UI scroll), **`CityStatsUIController.cs`**, **`MiniMapController.cs`**. **`UnitControllers/`**: **`BuildingSelectorMenuController`**, **`DetailsPopupController`**, **`DataPopupController`**, **`GrowthBudgetSlidersController`**, **`SpeedButtonsController`**, **`*SelectorButton.cs`**, **`MiniMapLayerButton`**, **`ShowStatsButton`**, **`ShowTaxes`**, **`SimulateGrowthToggle`**, etc. Other managers feed **HUD** data (**`StatisticsManager`**, **`EconomyManager`**, **`TimeManager`**) without owning every widget.

**City scene and `ControlPanel`:** Primary layout **`Assets/Scenes/CityScene.unity`** (or future **`CityScene.unity`**). **City** **Canvas** root in scene: **`UI/City/Canvas`** (**Screen Space Overlay**; **Canvas Scaler** reference **800×600** in **UI** inventory export). **`ControlPanel`**: **left**-docked **vertical** construction **toolbar** (category rows, **horizontal** tool groups per row); wired via **`UIManager`** and **`UnitControllers/*SelectorButton.cs`**. **Normative layout:** **§3.3**. **`SampleScene.unity`** also lives under **`Assets/Scenes/`** (default **Unity** template); it is **not** on **`UiInventoryReportsMenu`** **`SceneAllowlist`** — **as-built** docs and the committed baseline cover **CityScene** + **MainMenu** only.

**Main menu scene:** **`Assets/Scenes/MainMenu.unity`** — scene YAML may contain **no** serialized **Canvas** in older flows; **`MainMenuController`** wires **Inspector** **Button**s and/or **`MainMenuCanvas`**; **`BuildUI()`** remains a dev fallback when strip references are **null**. **Edit Mode** **UI** inventory export should include **`MainMenuCanvas`** when present; use **§3.0** + code for **as-built** menu **UI**.

**Technical constraints:** **Canvas Scaler** — **§4.3**. **EventSystem** — UI must consume pointer events so world tools (e.g. camera zoom) do not fire through panels. **Performance** — no **`FindObjectOfType`** in **`Update`** (**ia/rules/invariants.md**). **Coupling** — **`UIManager`** is large; prefer small controllers or shared helpers (**AGENTS.md**).

**Known pain points:** Scroll wheel over UI lists also moving **camera** (fixed — see **§3.5**); **`FindObjectOfType`** in hot paths (**BUG-14**); happiness / stats display inconsistencies (**BACKLOG**).

**Ongoing hygiene:** When **UI** hierarchies change, refresh **§1–§4** (as needed), this **Codebase inventory**, and the committed baseline JSON per [`docs/reports/README.md`](../../docs/reports/README.md). After **glossary** / **reference spec** body edits consumed by **territory-ia**, run `npm run generate:ia-indexes -- --check`. Extend **`UiInventoryReportsMenu`** allowlist when **`RegionScene`** / **`CityScene`** assets land or rename.

---

## 1. Foundations

**Traceability:** Color and typography frequency tables below are deduplicated from [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) (**CityScene** **`canvases[0].nodes`**). Row **Usage** cites representative **`Canvas/…`** paths (relative to **`UI/City/Canvas`** in the scene).

### 1.1 Color

Colors are **Unity `Graphic.color`** on **uGUI** **Image** / **Text** (legacy). Values below are **RGBA** channels **0–1** as exported.

| Token name (as-built) | Usage | Value / reference |
|------------------------|--------|-------------------|
| **ui-text-primary** | Default stat labels, keys, body copy on dark panels | `1.000,1.000,1.000,1.000` — e.g. `Canvas/DataPanelButtons/StatsPanel/*/…Key`, many **ControlPanel** labels |
| **ui-text-muted** | De-emphasized chrome | `1.000,1.000,1.000,0.392` — semi-transparent white on several widgets |
| **ui-surface-dark** | Button faces, dark chrome | `0.196,0.196,0.196,1.000` |
| **ui-overlay-dim** | Panel backdrops / tint | `0.498,0.498,0.498,0.392` |
| **ui-text-warm** | Alternate light text | `1.000,0.986,0.986,1.000` |
| **ui-accent-blue** | Accent (e.g. some chrome) | `0.165,0.275,0.447,1.000` / `0.133,0.227,0.373,1.000` (two close blues in sample) |

**Varies:** Full-screen tints, notification bars, and one-off sprites — grep the JSON or re-run **Export UI Inventory** after changes.

**Target (`UiTheme` / DefaultUiTheme):** Runtime HUD and menu polish read **`Assets/UI/Theme/DefaultUiTheme.asset`** via **`UIManager.hudUiTheme`** and **`MainMenuController.menuTheme`**. Canonical RGBA (0–1) ship on the asset:

| Token (code) | Role |
|----------------|------|
| `surfaceBase` | Deepest neutral base |
| `surfaceCardHud` | HUD / popup card (alpha ~0.88 for map bleed-through) |
| `surfaceToolbar` | **ControlPanel** strip (alpha ~0.94) |
| `surfaceElevated` | Elevated controls |
| `borderSubtle` | Dividers |
| `textPrimary` / `textSecondary` | Body vs muted labels |
| `accentPrimary` / `accentPositive` / `accentNegative` | Interactive / surplus / deficit |
| `modalDimmerColor` | Fullscreen popup dimmer |

Re-run **Export UI Inventory** after wide **Graphic.color** edits so **as-built** JSON stays aligned.

### 1.2 Typography

**Product stack (target):** **Shipped decision:** Keep **legacy `UnityEngine.UI.Text`** for **existing** **city** **HUD** / panels until a future issue scopes a **TMP** migration wave. **New** work may use **TMP** only when the issue explicitly chooses it (avoid mixed stacks on the same row). **Main menu** strip uses **legacy `Text`** + **`UiTheme`** font sizes.

**Target (`UiTheme` typography):** `fontSizeDisplay` (hero stats), `fontSizeHeading`, `fontSizeBody`, `fontSizeCaption` on **`DefaultUiTheme.asset`**; **`UIManager`** applies them on **Start** when **`hudUiTheme`** is assigned (**`CityScene`**).

**As-built:** The **city** scene uses **legacy `UnityEngine.UI.Text`** heavily (`UIManager` serialized fields). **`TextMeshProUGUI`** appears sporadically (e.g. some `Text (Legacy)` sibling naming in hierarchy; export shows **`LiberationSans SDF`** / **`LiberationSans`** on a small number of nodes).

| Style (as-built) | Font asset (exported `.name`) | Size (px) | Weight | Usage |
|------------------|-------------------------------|-----------|--------|--------|
| **HUD / panel key** | `LegacyRuntime` | 10 | Normal | Stat keys — `…/PopulationKey`, `…/MoneyKey`, demand keys, etc. |
| **HUD / panel value** | `LegacyRuntime` | 36 | Normal | Large numeric readouts — `…/PopulationValue`, `…/MoneyValue`, demand values |
| **City name / title** | `LegacyRuntime` | 12–14 | Normal | `PlayerCityName`, panel titles |
| **TMP occasional** | `LiberationSans SDF`, `LiberationSans` | 8, 28 | Normal | Rare nodes — prefer one stack per new work (**§1.2** policy) |

### 1.3 Spacing and layout

- **Main menu (code-built):** When `MainMenuController.BuildUI()` runs, vertical stack uses **button** size **200×40**, **spacing 10** px, centered anchor — see `MainMenuController.cs`.
- **City scene:** **Toolbar** **`Canvas/ControlPanel`** is a **left**-docked **vertical** panel: **one row per category** (demolition, roads, utilities, **RCI** zoning, environment, etc.) with **horizontal** **Button** groups per row (**LayoutGroup** as authored in **`CityScene.unity`**). **LayoutGroups** also appear under scroll views (**BuildingSelector**, **LoadGame**).
- **Grid / token spacing:** No single **4px/8px** token is enforced globally — treat **as-built** as **per-panel** until a **theme** helper or **§5.2** token work lands.
- **Anchors:** **Stats** and **date** clusters live under **`Canvas/DataPanelButtons`** with mixed anchors (see JSON **`anchor_min` / `anchor_max`** per node).

#### 1.3.1 HUD and uGUI hygiene (agents, **UI** inventory, **Edit Mode**)

Norms for **CityScene** / **MainMenu** hierarchies so **Editor** exports, **MCP** path references, and **Transform.Find** stay reliable. **New** work should follow these. Track **implementation** drift in [`BACKLOG.md`](../../BACKLOG.md) under **§ UI-as-code program** (open **TECH-** row with a linked `ia/projects/{ISSUE_ID}.md` when used). **Backlog id policy:** **TECH** numbers increase monotonically; **do not reuse** a **TECH** id that already appears in [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) for a different program — e.g. **TECH-60** there is the **completed** **spec pipeline & verification program** umbrella, not **HUD** hygiene work.

- **Canvas vs leaf graphics:** Keep **Canvas** + **CanvasScaler** on the **root** overlay (or a documented world-space root). Do **not** add **Canvas** + **CanvasScaler** on the same **GameObject** as ordinary **HUD** **Text** / **Image** leaves unless there is an explicit, documented reason.
- **`Transform.Find` depth:** **`Transform.Find`** only searches **immediate children**. Align sibling **HUD** widgets under the same parent (or cache a **hud root** reference) instead of assuming deep discovery by name.
- **Full-stretch anchors:** **`anchorMin` / `anchorMax`** `(0,0)`–`(1,1)` makes **rect height** depend on **parent height + `sizeDelta.y`**. Do not copy that pattern to small floating strips without applying the layout math; prefer explicit top/bottom anchors and fixed heights for agent-editable **HUD** blocks.
- **Corner anchors:** For **`anchorMin` = `anchorMax` = (0,0)**, **`anchoredPosition`** is relative to the parent **rect** corner—revalidate after parent size changes.
- **Stacking and overlap:** Keep a deliberate **vertical gap** between fixed **HUD** regions (e.g. debug readouts vs **MiniMapPanel**) unless overlap is intentional; pair semi-transparent chrome with an explicit stacking policy when layers coincide.
- **Naming:** No **trailing spaces** in **GameObject** names (they break **`Transform.Find`** and diffs). Prefer stable, unique names over **`Unity`** auto-suffixes such as **` (1)`** on nodes referenced from code or tooling.
- **Text stack:** Prefer **one** text stack (**legacy** **Text** *or* **TextMeshProUGUI**) per **surface** in **new** work; document known mixes until consolidated (**§1.2**).
- **UI Toolkit + uGUI:** **UIDocument** alongside **uGUI** under one panel (e.g. **`StatsPanel`**) is allowed but raises agent cost—keep **UXML** vs **`SerializeField`** boundaries obvious in code or **managers-reference** when extended.
- **Obsolete player flows:** The glossary **Urbanization proposal** is **obsolete**—do not treat **`ProposalUI`** as normal **HUD**; remove, hide, or disconnect it when implementation confirms the flow is inactive (**invariants**).
- **Components on panel roots:** Avoid unrelated **`MonoBehaviour`** types on fullscreen **panel** roots (e.g. core **managers** on **`LoadGameMenuPanel`**)—prefer dedicated **controller** types so **Inspector** and exports stay readable.
- **Inventory limits:** **UI** inventory export reflects the **serialized** hierarchy; **runtime-only** instances may be missing until the scene is saved (or a **Play Mode** capture exists). Re-run export and refresh the committed baseline after hierarchy edits ([`docs/reports/README.md`](../../docs/reports/README.md)).

### 1.4 Iconography

- Source / style (line vs filled), sizes, and tint rules — **varies** by panel; many stats use small **Image** children named `*Icon` next to **Text**. Consolidate in a future **FEAT-**/**TECH-** row if a library is introduced.

#### 1.4.1 Button iconography — human-authored mandate

**Rule.** Toolbar / panel button artwork is **human-authored**. `claude-design` output (and any LLM-driven generator) supplies **theme + layout only** — never button icon PNGs. Bake pipeline drives button icons from `IrPanelSlot.iconSpriteSlugs[]` parallel array, resolved at bake-time via `UiBakeHandler.ResolveButtonIconSprite` (Editor `AssetDatabase.LoadAssetAtPath<Sprite>`).

**Asset locations.** Resolver probes three paths in order per slug:
1. `Assets/Sprites/Buttons/{slug}-target.png` (preferred — keep new art here).
2. `Assets/Sprites/{slug}-target.png` (legacy root — tolerated).
3. `AssetDatabase.FindAssets("{slug}-target t:Sprite", new[]{ "Assets/Sprites" })` (recursive sibling-folder scan; tolerates `Assets/Sprites/Commercial/`, `Assets/Sprites/Residential/`, etc.).

Slug convention: `{Concept}-button-64` (e.g. `Residential-button-64`, `Commercial-button-64`, `Pause-button-1-64`). Suffix `-target.png` is the resolver-expected file.

**Authoring flow.**
1. Human / artist drops `{slug}-target.png` (+ optional `{slug}-pressed.png`) anywhere under `Assets/Sprites/**` — prefer `Assets/Sprites/Buttons/`.
2. IR parallel array in `web/design-refs/step-1-game-ui/ir.json` lists slug per button child of an `IlluminatedButton` parent slot (`slot.iconSpriteSlugs[c]` matches `slot.children[c]` index).
3. `bake_ui_from_ir` bridge mutation re-runs; bake handler spawns Image child + injects sprite ref.
4. Verify via `prefab_inspect` — every IlluminatedButton must have an `icon` child with non-null `m_Sprite`.

**LLM-out-of-scope.** Generators must not draft, suggest, or fall-back-synthesize button icons. Missing slug → flat-color body (legacy behaviour) is acceptable; never inject placeholder icons. New slugs added to IR must reference existing artist-authored PNGs.

### 1.5 Motion (optional)

- Duration and easing for show/hide of panels — **TBD**; keep scope small unless an issue explicitly covers animation.

---

## 2. Components

**As-built:** Shipped **city** UI still combines scene-authored **Button** + **Image** + legacy **Text**; **v0** reusable prefabs live under **`Assets/UI/Prefabs/`** after running **Territory Developer → UI → Scaffold UI Prefab Library v0** (**`UiPrefabLibraryScaffoldMenu`**). The following stays normative for **new** work where no issue overrides it.

### 2.1 Button — primary

- **As-built:** **Image** + **Button** on **ControlPanel** tools; colors per **§1.1**; text uses **LegacyRuntime** sizes **10–14** on labels.
- **States:** normal, highlighted, pressed, disabled — **Unity** defaults unless prefab overrides.

### 2.2 Button — secondary

- **As-built:** Same stack as primary with darker **`ui-surface-dark`** tints on many panels.

### 2.3 Panel / card

- **As-built:** **`DataPanelButtons`** hosts **Date**, **Stats**, **Details**, **Tax** sub-panels; **LoadGameMenuPanel**, **InsufficientFundsPanel**, **NotificationPanel** are separate roots under **`Canvas/`**.

### 2.4 List / scroll

- **As-built:** **`Scroll View` / `Viewport` / `Content`** under **`BuildingSelectorPopupPanel`** and **`LoadGameMenuPanel`**.
- **Input:** Scroll vs **camera** — see **§3.5**.

### 2.5 Tooltip

- **As-built:** `UIManager.tooltipDisplayTime` — behavior tied to manager; no separate **TMP** tooltip spec.

### 2.6 Modal / dialog

- **As-built:** Overlay-style panels (**LoadGame**, **InsufficientFunds**, **Notification**) plus **Details** / **Stats** / **Tax** data panels.

*Add components (sliders, toggles, tabs) as the library grows.*

---

## 3. Patterns

### 3.0 Main menu (**MainMenu** scene)

- **Flow:** **Continue**, **New Game**, **Load City**, **Options** — `MainMenuController` wires **Button** listeners; **Load City** / **Options** panels are created at runtime under the serialized **`Canvas`** when those **Inspector** references are **null** (`EnsureSerializedMenuPanels`).
- **As-built Canvas:** **`MainMenuCanvas`** in **`MainMenu.unity`** — **Screen Space Overlay**, **`CanvasScaler`**: **Scale With Screen Size**, **reference resolution 1280×720**, **match** **0.5**, **GraphicRaycaster**. **`EventSystem`** is a root **GameObject**. Optional **`UiTheme`** on **`MainMenuController`** tints the four menu **Buttons** on **`Start`**.
- **Fallback:** If **`continueButton`** (and the serialized strip) is **unassigned**, **`BuildUI()`** creates a full runtime tree (legacy path).
- **Export note:** **Edit Mode** **UI** inventory export includes **`MainMenuCanvas`**; refresh [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) after hierarchy edits.

### 3.1 HUD information density

- **As-built cluster:** Primary readouts live under **`Canvas/DataPanelButtons`**: **`DatePanel`** (city name, date), **`StatsPanel`** (population, money, happiness, power, water, jobs, demand bars, feedback), **`DetailsPanel`** (selection details), **`TaxPanel`** (taxes + growth sliders), **`GameButtons`** (stats/taxes toggles, zoom, mini-map, speed, simulate growth).
- **Orchestration:** **`UIManager`** owns references to **`Text`** fields; align copy and ordering with [`managers-reference.md`](managers-reference.md) when extending stats.

### 3.2 Popups

- **`PopupType`** (`UIManager.cs`): **LoadGame**, **Details**, **BuildingSelector**, **StatsPanel**, **TaxPanel**.
- **Shared modal contract:** Surfaces above that participate in the **Esc** stack call **`UIManager.RegisterPopupOpened(PopupType)`** when shown so **`UIManager`** closes **last-opened first** (see **`UIManager.PopupStack`**). Prefer full-screen or panel **Image** **`raycastTarget`** on dimmers so pointer hit tests reach **UI** before the **grid**. **InsufficientFunds** / **Notification** panels follow economy flows; add them to the **Esc** stack only if a future issue wires **`RegisterPopupOpened`** for them.
- **Representative Canvas paths (city scene):**
  - **LoadGame** → `Canvas/LoadGameMenuPanel` (+ **Scroll View**)
  - **BuildingSelector** → `Canvas/ControlPanel/BuildingSelectorPopupPanel`
  - **Details** → `Canvas/DataPanelButtons/DetailsPanel`
  - **StatsPanel** → `Canvas/DataPanelButtons/StatsPanel`
  - **TaxPanel** → `Canvas/DataPanelButtons/TaxPanel`
- **Feedback:** `Canvas/InsufficientFundsPanel`, `Canvas/NotificationPanel` — tied to economy / **Game notification** flows.
- **Controllers:** e.g. **`DetailsPopupController`**, **`BuildingSelectorMenuController`**, **`DataPopupController`** register with **`UIManager`**.

### 3.3 Tool selection / toolbar

- **Scene path:** **`Canvas/ControlPanel`** under **`UI/City/Canvas`** (**CityScene**).
- **Inventory and constraints:** **Codebase inventory (uGUI)** (this spec, **Related files**).
- **As-built (current):** **Left**-docked **vertical** **toolbar**: category **rows** (e.g. demolition, roads, utilities, **RCI** zoning, environment) with **horizontal** tool **`Button`** groups per row; dependent overlays (e.g. zoning density) re-anchored to the sidebar; avoid overlapping **mini-map** and corner **HUD** (**Editor**-authored layout in **`CityScene.unity`**).
- **Implementation note:** Prefer documented **LayoutGroup** hierarchy when refactoring; confirm **`Canvas Scaler`** (**§4.3**) at reference resolutions. Refresh [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) after hierarchy changes ([`docs/reports/README.md`](../../docs/reports/README.md)).

### 3.4 Feedback and errors

- **Insufficient funds** — `InsufficientFundsPanel` + text field.
- **Notifications** — `NotificationPanel` / **Game notification** path.

### 3.5 World vs UI input

- **Expectation:** Pointer over **UI** should consume events so **camera** / **grid** tools do not fire through panels; **`GridManager`** / **`CameraController`** use **`IsPointerOverGameObject`** patterns.
- **Scroll wheel vs zoom (checklist):**
  1. **`CameraController.HandleScrollZoom`** returns early when **`EventSystem.current.IsPointerOverGameObject()`** is true so list scroll does not change **orthographic** zoom.
  2. **Load Game** list: wheel over **`ScrollRect`** / **Viewport** / **Content** — list scrolls, **map** zoom does not.
  3. **Building selector** popup: same expectation over its **Scroll View** subtree.
  4. **Raycasts:** **Viewport** / list item **Graphic** components keep **`raycastTarget`** enabled where hit testing must see the **UI**.
- **Regression test:** **Play Mode** — open **Load Game**, scroll wheel over save list; open **Building Selector**, scroll over building list; pointer over **map** — zoom still steps.
- **Touch and keyboard:** **`EventSystem.current.IsPointerOverGameObject()`** without a **finger id** can miss **touch** over **`ScrollRect`**; **`CameraController`** should use the active touch’s **`fingerId`** when present. **WASD** (and right-drag) **camera** movement should also respect **UI** hit tests when a blocking overlay is up — same policy as scroll zoom.

---

## 4. Unity mapping

### 4.1 Naming

- **As-built:** Functional names on **ControlPanel** children (`*SelectorButton`, `*Panel`); **Tax** panel uses **`TaxGrowthBudgetPercentLabel`** (static caption for growth budget %) alongside **`TotalGrowthLabel`** (dynamic value). Prefer descriptive names on new objects; **§4.1** prefab prefix convention (**`UI_Button_Primary`**, etc.) remains **target** — not enforced globally yet.
- Controllers stay focused; avoid duplicating styling logic across many **`MonoBehaviour`**s — prefer shared prefab variants or a small theme helper if introduced in a dedicated **BACKLOG** tech row.

### 4.2 Scripting

- Cache component references in `Awake`/`Start`; no `FindObjectOfType` in `Update` for UI.
- New UI features: document public API in XML per project conventions.

### 4.3 Canvas

| Scene | Canvas path (scene / code) | `RenderMode` | **Canvas Scaler** (as-built) |
|-------|----------------------------|--------------|------------------------------|
| **CityScene** | `UI/City/Canvas` | **Screen Space Overlay** | **Scale With Screen Size**, reference **800×600**, **match** **0.5** — from **UI** inventory baseline export |
| **MainMenu** | Serialized **`MainMenuCanvas`** | **Screen Space Overlay** | **Scale With Screen Size**, reference **1280×720**, **match** **0.5** |
| **MainMenu** | Runtime: root **`Canvas`** (when `BuildUI()` only) | **Screen Space Overlay** | Same as above — dev fallback |

**Acceptance matrix (spot-check in Play Mode):**

| Resolution | **CityScene** (**HUD** + **ControlPanel**) | **MainMenu** |
|------------|--------------------------------------------|--------------|
| **800×600** | Toolbar clears **mini-map** / corners; readable **Stats** cluster | Menu stack centered; no clipped **Buttons** |
| **1280×720** | Baseline for **toolbar** layout (**§3.3**) | Reference resolution for scaler |
| **1920×1080** | No overlap regressions on **ControlPanel** / **DataPanelButtons** | Menu stack centered |

When changing **Canvas Scaler** or root anchors, re-run the checks above and refresh the **UI** inventory baseline if hierarchies change.

---

## 5. Acceptance criteria (per issue)

When opening a backlog issue for UI work, include:

1. **Spec section** this issue implements or updates.
2. **Screens affected** (HUD, which popup, etc.).
3. **Play Mode checks:** resolution sanity, hover/click, scroll not leaking to camera where applicable.
4. **Regression:** related systems (`UIManager`, listed controllers) still wire in Inspector.

### 5.2 Theme and prefab paths

| Asset | Path / notes |
|-------|----------------|
| **`UiTheme`** script | `Assets/Scripts/Managers/GameManagers/UiTheme.cs` |
| **Default theme** asset | `Assets/UI/Theme/DefaultUiTheme.asset` — assign on **`MainMenuController.menuTheme`** (optional) |
| **Prefab library v0** | `Assets/UI/Prefabs/UI_ToolButton.prefab`, `UI_StatRow.prefab`, `UI_ScrollListShell.prefab`, `UI_ModalShell.prefab` — generated by **Territory Developer → UI → Scaffold UI Prefab Library v0** (`UiPrefabLibraryScaffoldMenu.cs`); re-run to overwrite; then wire into scenes as needed |
| **`UIManager` partials** | `UIManager.PopupStack.cs`, `UIManager.Hud.cs`, `UIManager.Toolbar.cs`, `UIManager.Utilities.cs` alongside `UIManager.cs` |

**Editor:** **Territory Developer → Reports → Validate UI Theme asset** (`UiThemeValidationMenu.cs`).

### 5.3 Shipped polish patterns (implementation reference)

Normative behavior stays in **§1–§3**; the following are **consistency** notes for agents extending **`UiTheme`**-driven **HUD** without duplicating one-off **YAML** or coroutines:

- **Theme-first runtime chrome:** Runtime-created **HUD** pieces (**tax** section dividers, **RCI** demand gauge tracks, **welcome** briefing shell, **grid** coordinate readout backing) should read **`UIManager`**’s assigned **`hudUiTheme`** when present so **`DefaultUiTheme.asset`** edits propagate without large scene diffs.
- **Shared popup fade:** Prefer one utility (**`UiCanvasGroupUtility`**: **`EnsureCanvasGroup`** + **`FadeUnscaled`**) for **CanvasGroup** open/close on **popup** roots instead of per-controller coroutine copies.
- **Welcome vs **Esc** stack:** A **PlayerPrefs**-gated onboarding panel should **not** register on **`UIManager`**’s **popup** stack; show and dismiss it **before** **Esc** stack processing so **Load Game** / **Stats** ordering stays predictable.
- **Floating readouts:** Minimal **Text** + **Shadow** (no full-width chip) reduces noise when a label follows the cursor over the **map**.
- **Demand gauge tint:** Sample **heavy** **RCI** zoning prefab colors (e.g. **`ZoneManager`** lists) for **filled Image** tints so **HUD** matches **map** language, with bright **fallbacks** if lists are empty.
- **UI inventory sampling:** **`UiInventoryReportsMenu`** omits **RectTransforms** without **Graphic** / **LayoutGroup**; gaps in the committed baseline JSON are expected — validate with ancestor coverage rules, not a full scene tree listing.

---

### 3.6 Stats panel pattern (presenter-driven, scale-switchable)

City + Region stats render through a **shared presenter pipeline** baked from a JSX/IR archetype (`city-stats-handoff` v2, schema `tabs[] + rows[]`). The Stage 13.5–13.7 closeout settled the following:

| Decision | Pick | Notes |
|---|---|---|
| **D1 — Tabs** | `Money / People / Land / Infrastructure` | Default open tab = **Infrastructure**. Full labels, no abbreviations. Field→tab mapping authored in IR `tabs[].rows[]`. |
| **D2 — Region aggregation** | Same panel + same 4 tabs at Region scope | `population` + `money` = totals; `happiness` / `pollution` / `cityLandValueMean` = **population-weighted means** (null when Σpop=0); default = total. Region-only stats deferred. |
| **D3 — Iconography** | `icon-{happiness,population,money,bond,envelope}` added (27 icons total) | Tab + row icons everywhere. Bake handler resolves slug → 128×128 PNG via `tools/scripts/icons-svg-split.ts`. |
| **D4 — Row layout** | `[icon | label | value | vu? | delta?]`, 28 px row, 20 px icon | Label = 50% width. Sign-driven delta color. Row hover = MVP-scope only (no expansion). |
| **D5 — Schema cutover** | Hard cutover IR v1 → v2 | Per-panel approval gate at each Stage 14.* task. No back-compat. |
| **D6 — SVG export** | Per-id 128×128 PNG export at transcribe step | Invoked by `icons-svg-split.ts`; no runtime SVG import. |
| **D7 — Tab show/hide** | Hard show/hide via `SetActiveTab` | Pages[] flip; indicator snap; **no animation**. |
| **D8 — Presenter wiring** | `IStatsPresenter` interface + `CityStatsPresenter` + `RegionStatsPresenter` | Tick-driven (`CityStatsFacade.OnTickEnd` → `OnRefreshed`). Adapter gates writes on `IsReady` (guardrail #14 — manager-init race). |
| **D9 — Scale enum** | `City + Region` only — Country / World **hidden entirely** (NOT greyed) | `StatsScaleSwitcher.Scale` enum enforces; PlayMode test asserts cardinality + absence. |

**Wiring contract:**
- `CityStatsHandoffAdapter` subscribes to active presenter's `OnRefreshed`; `SetPresenter(IStatsPresenter)` swaps source on scale toggle (unsubscribe → swap → resubscribe → repaint).
- Inspector-first wiring per invariant #4; `FindObjectOfType` fallback in `Awake` only (guardrail #0). No runtime `AddComponent` on existing scene nodes (invariant #6).
- Same panel, same 4 tabs across scales — binding-key set is identical (`PlayMode RegionStatsPanelSmokeTests.Region_And_City_Bindings_HaveIdenticalKeySets`).
- No per-frame `Update` polling; refresh fires on facade tick end only (invariant #3).

**Architecture decision:** **DEC-A21** — Stats panel presenter-driven baking with scale-switchable adapter. Logged in `arch_decisions` (plan-scoped to `game-ui-design-system`).

### 3.7 RCIS subtype picker pattern

The legacy `Assets/Scripts/Managers/GameManagers/SubTypePickerModal.cs` is decommissioned post-Stage 13.7. The replacement is a **generalised RCIS** (Residential + Commercial + Industrial + Zone-S) picker baked from a new IR archetype `subtype-picker`. Same 4 paths (R / C / I / S) flow through the new modal; bake handler emits one prefab + one C# controller. Wiring follows the §3.6 presenter contract — Inspector-first, tick-agnostic (modal is event-driven on toolbar button click).

---

## 7. Target visual language (inferred — pending game UX/UI master plan)

These assertions are inferred from visual references provided by the developer (April 2026). They are **targets** — not yet enforced in as-built code. A future game UX/UI master plan will formalize them into backlog issues. Cross-reference: `ia/specs/web-ui-design-system.md` shares the same language for the web layer.

### 7.1 Dark-first palette

| Principle | Description |
|-----------|-------------|
| Base | Near-black canvas (`#0a0a0a`–`#181818`). No light-mode variant planned. |
| Surface elevation | Subtle step-up in brightness for cards/panels (~`#1a1a1a` → `#222`). |
| Text | Primary white (`#e8e8e8`), secondary muted (`rgba(255,255,255,0.55)`), de-emphasized (`rgba(255,255,255,0.35)`). |
| Accent — positive | Green (`#2d8a4e` / `#40bf72`) — growth, surplus, healthy stats. |
| Accent — negative | Red (`#bf4040` / `#e05555`) — deficit, damage, critical alerts. |
| Accent — neutral | Muted blue-gray (`#3a5080`) — interactive chrome, selected state. |
| Borders | Subtle `1px solid rgba(255,255,255,0.08)` — cards visible but not loud. |

### 7.2 Typography direction

| Principle | Description |
|-----------|-------------|
| Label keys | Small-caps or letter-spaced uppercase (`letter-spacing: 0.12em`) — sparse, precise. |
| Metric values | Large numeric readouts, tabular figures (`font-variant-numeric: tabular-nums`). |
| Body / annotations | Compact sans-serif (12–13px effective), tight line-height (1.3). |
| No decorative fonts | System UI or clean geometric sans — data legibility over personality. |

### 7.3 Information density

| Pattern | Description |
|---------|-------------|
| **Data tables** | Dense multi-column grids. Color-coded badges for numeric stats (green/red circles or chips). Sortable columns. Sticky headers. |
| **Filter chips** | Horizontal pill row above tables for category/metric filtering. |
| **Stat bars** | Thin horizontal progress bars (`height: 4–8px`) — proportion of max, colored by threshold. |
| **Entity cards** | Bordered dark cards: avatar/icon left, key stats right, tabbed sub-panels (Summary / Stats / History). |
| **Heat overlays** | Spatial data layers on map views — red/blue graduated color over dark base tile. No heavy border chrome on map surface. |
| **Proportional bubbles** | Circle size encodes magnitude on geographic/grid views. |
| **Metric badges** | Rounded rectangle chips with number + semantic color — used for ratings, ranks, deltas. |
| **Icon + value combos** | Small icon (16–20px) preceding or following a stat value — consistent pairing. |

### 7.4 Cross-surface note (game ↔ web)

These patterns apply to both the **in-game HUD** (Unity uGUI) and the **web platform** (`ia/specs/web-ui-design-system.md`). When a game UX/UI master plan is authored, it should reconcile the Unity token names in `UiTheme.cs` / `DefaultUiTheme.asset` with the target language above. The web layer has more freedom (CSS variables, Tailwind); the game layer must map through `UiTheme` tokens to maintain one-asset-update propagation.

---

## 6. Revision history

| Date | Change |
|------|--------|
| *YYYY-MM-DD* | Initial draft scaffold |
| 2026-03-20 | §3.3 — ControlPanel toolbar layout variants; cross-link **unity-development-context** |
| 2026-04-04 | Overview links → **`projects/ui-as-code-exploration.md`** (retired `docs/ui-design-system-project.md` / `docs/ui-design-system-context.md`) |
| 2026-04-06 | Program notes in closed umbrella project spec (later migrated here — **Codebase inventory (uGUI)**); delete **`projects/ui-as-code-exploration.md`** |
| 2026-04-10 | **UI-as-code program** umbrella **§ Completed**; **Codebase inventory (uGUI)** inlined from closed project spec |
| 2026-04-04 | **As-built vs target** subsection; **UI-as-code** program baseline for **`ui-design-system.md`** |
| 2026-04-04 | **§1**–**§4**, **§2**–**§3** **as-built** from [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json); **§3.0** **Main menu**; **§6** traceability note |
| 2026-04-04 | Serialized **`MainMenu`**, **`UiTheme`**, **§1.2** typography decision, **§4.3** resolution matrix, **§5.2** theme paths |
| 2026-04-04 | **`UIManager` `partial`** split; **§3.2** modal **Esc** contract; **§3.5** scroll-zoom checklist; **§5.2** prefab scaffold menu; **v0** prefabs via **`UiPrefabLibraryScaffoldMenu`** |
| 2026-04-11 | **§3.5** touch / **WASD** **UI** blocking note; **§5.3** shipped **UiTheme** / **HUD** polish implementation patterns (migrated from closed project spec) |
| 2026-04-14 | **§7** Target visual language (inferred from reference images) — dark-first, data-dense, semantic color, stat bars, entity cards, heat/bubble overlays. Cross-linked to `web-ui-design-system.md`. |
| 2026-05-04 | **§3.6** Stats panel pattern — D1-D9 closeout (4 tabs, region weighted-mean aggregation, IR v2 cutover, presenter pipeline, City+Region scale enum, DEC-A21). **§3.7** RCIS subtype picker pattern — legacy `SubTypePickerModal` decommissioned, IR-baked replacement. Migrated from `docs/game-ui-design-system-mvp-closeout-extensions.md` (preserved as historical record). |

### Machine-readable traceability (UI inventory baseline)

- **Committed snapshot:** [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) — refresh from **Postgres** `editor_export_ui_inventory` or **Territory Developer → Reports → Export UI Inventory (JSON)** when **UI** hierarchies change (see [`docs/reports/README.md`](../../docs/reports/README.md)).
- **Field mapping:** JSON node **`path`** is relative to the sampled **Canvas** root (`Canvas/…`); full scene path is **`UI/City/Canvas`** + **`path`** for **CityScene**.
