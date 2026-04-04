# UI / UX Design System — Spec

## Overview

This spec defines **foundations**, **components**, and **patterns** for Territory Developer’s in-game UI so that backlog issues can reference concrete sections. **Program charter**, **codebase inventory**, **backlog bridge**, and **roadmap** live in [`.cursor/projects/TECH-67.md`](../projects/TECH-67.md) (**UI-as-code program** umbrella — not a reference spec). **Executable issues:** [`BACKLOG.md`](../../BACKLOG.md). **As-built** tables in **§1**, **§4**, and major **§2–§3** surfaces are sourced from the committed machine snapshot [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) (refresh when scenes change; see [`docs/reports/README.md`](../../docs/reports/README.md)). **Scenes:** **UI** spans **`MainMenu`**, **`MainScene`** (future **`CityScene`**), and future surfaces (e.g. **`RegionScene`**) — exports and prose are **per scene**.

### As-built vs target

- **As-built (current):** What the game **actually** uses today — **Canvas** settings, **colors**, **fonts** / sizes, **margins**, **anchors**, **HUD** / **toolbar** / **popup** layout, and representative **UX** behaviors. Primary **city** snapshot: **`MainScene.unity`** → **`UI/City/Canvas`** (paths in JSON are relative to that **Canvas** root, e.g. `Canvas/ControlPanel`). **Main menu:** **`MainMenu.unity`** has **no** serialized **Canvas** in the scene file; when **`MainMenuController`** builds UI at runtime (`BuildUI`), it creates **`Canvas`** + **`CanvasScaler`** as documented in **§3.0** and **§4.3**.
- **Target (planned):** Future layout or tokens defined by **BACKLOG** issues (e.g. **TECH-69** theme / prefab work). Keep **Target** subsections or labeled rows **alongside** **as-built** so refactors stay traceable.

### Domain vocabulary

Backlog items and player-facing copy that name gameplay systems should use [`glossary.md`](glossary.md) terms (**Game notification**, **street**, **interstate**, **map border**, **road validation pipeline**, etc.) so wording matches reference specs and **territory-ia** tools (`glossary_discover`, `glossary_lookup`).

**Status:** Draft — **§1**–**§4** and **§2**–**§3** carry **as-built** rows from the **UI** inventory baseline (committed JSON + **`ui-design-system.md`** prose); **§1.4** / **§1.5** remain light until an issue scopes icon / motion work. **Glossary:** **UI design system (reference spec)**.

## Related files (code and assets)

| Area | Location / notes |
|------|------------------|
| Main UI orchestration | `Assets/Scripts/Managers/GameManagers/UIManager.cs` |
| Main menu (runtime UI builder) | `Assets/Scripts/Managers/GameManagers/MainMenuController.cs` — optional **Inspector**-wired UI vs `BuildUI()` |
| Popup controllers | `Assets/Scripts/Controllers/UnitControllers/*Popup*.cs`, `DetailsPopupController.cs`, `DataPopupController.cs`, etc. |
| HUD / stats | `CityStatsUIController.cs`, `UIManager.cs` (many legacy `Text` references) |
| Input vs UI | `EventSystem`, `CameraController.cs`, `GridManager.cs` (`IsPointerOverGameObject` patterns) |
| **City** scene asset | `Assets/Scenes/MainScene.unity` — **`UI/City/Canvas`** hierarchy (authoritative in **Editor**) |
| **Main menu** scene asset | `Assets/Scenes/MainMenu.unity` — scene file contains **no** **Canvas** YAML; menu **Canvas** may be **runtime**-created (**§3.0**) |
| **As-built** JSON (committed) | [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) — bounded **`scenes[]`** sample |

Add prefab paths under `Assets/` as they are standardized.

---

## 1. Foundations

**Traceability:** Color and typography frequency tables below are deduplicated from [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) (**MainScene** **`canvases[0].nodes`**). Row **Usage** cites representative **`Canvas/…`** paths (relative to **`UI/City/Canvas`** in the scene).

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

### 1.2 Typography

**Product stack (target):** **TBD** — **TECH-69** **Phase D** records **TextMeshPro** migration vs continuing **legacy `UnityEngine.UI.Text`**. Until then, treat **as-built** rows below and the **UI** inventory export as authoritative.

**As-built:** The **city** scene uses **legacy `UnityEngine.UI.Text`** heavily (`UIManager` serialized fields). **`TextMeshProUGUI`** appears sporadically (e.g. some `Text (Legacy)` sibling naming in hierarchy; export shows **`LiberationSans SDF`** / **`LiberationSans`** on a small number of nodes).

| Style (as-built) | Font asset (exported `.name`) | Size (px) | Weight | Usage |
|------------------|-------------------------------|-----------|--------|--------|
| **HUD / panel key** | `LegacyRuntime` | 10 | Normal | Stat keys — `…/PopulationKey`, `…/MoneyKey`, demand keys, etc. |
| **HUD / panel value** | `LegacyRuntime` | 36 | Normal | Large numeric readouts — `…/PopulationValue`, `…/MoneyValue`, demand values |
| **City name / title** | `LegacyRuntime` | 12–14 | Normal | `PlayerCityName`, panel titles |
| **TMP occasional** | `LiberationSans SDF`, `LiberationSans` | 8, 28 | Normal | Rare nodes — prefer one stack per new work (**TECH-67** **§4.9**) |

### 1.3 Spacing and layout

- **Main menu (code-built):** When `MainMenuController.BuildUI()` runs, vertical stack uses **button** size **200×40**, **spacing 10** px, centered anchor — see `MainMenuController.cs`.
- **City scene:** **Toolbar** **`Canvas/ControlPanel`** is a **left**-docked **vertical** panel: **one row per category** (demolition, roads, utilities, **RCI** zoning, environment, etc.) with **horizontal** **Button** groups per row (**LayoutGroup** as authored in **`MainScene.unity`**). **LayoutGroups** also appear under scroll views (**BuildingSelector**, **LoadGame**).
- **Grid / token spacing:** No single **4px/8px** token is enforced globally — treat **as-built** as **per-panel** until a **theme** helper or **TECH-69**-scoped tokens land.
- **Anchors:** **Stats** and **date** clusters live under **`Canvas/DataPanelButtons`** with mixed anchors (see JSON **`anchor_min` / `anchor_max`** per node).

### 1.4 Iconography

- Source / style (line vs filled), sizes, and tint rules — **varies** by panel; many stats use small **Image** children named `*Icon` next to **Text**. Consolidate in a future **FEAT-**/**TECH-** row if a library is introduced.

### 1.5 Motion (optional)

- Duration and easing for show/hide of panels — **TBD**; keep scope small unless an issue explicitly covers animation.

---

## 2. Components

**As-built:** There is **no** single shared **UI** prefab library yet; shipped UI combines **Unity** default **Button** + **Image** + legacy **Text**, **ScrollRect** patterns, and **Slider** under **`TaxPanel`**. The following stays normative for **new** work where no issue overrides it.

### 2.1 Button — primary

- **As-built:** **Image** + **Button** on **ControlPanel** tools; colors per **§1.1**; text uses **LegacyRuntime** sizes **10–14** on labels.
- **States:** normal, highlighted, pressed, disabled — **Unity** defaults unless prefab overrides.

### 2.2 Button — secondary

- **As-built:** Same stack as primary with darker **`ui-surface-dark`** tints on many panels.

### 2.3 Panel / card

- **As-built:** **`DataPanelButtons`** hosts **Date**, **Stats**, **Details**, **Tax** sub-panels; **LoadGameMenuPanel**, **InsufficientFundsPanel**, **NotificationPanel** are separate roots under **`Canvas/`**.

### 2.4 List / scroll

- **As-built:** **`Scroll View` / `Viewport` / `Content`** under **`BuildingSelectorPopupPanel`** and **`LoadGameMenuPanel`**.
- **Input:** Scroll vs **camera** — see **§3.5** and **BUG-19**.

### 2.5 Tooltip

- **As-built:** `UIManager.tooltipDisplayTime` — behavior tied to manager; no separate **TMP** tooltip spec.

### 2.6 Modal / dialog

- **As-built:** Overlay-style panels (**LoadGame**, **InsufficientFunds**, **Notification**) plus **Details** / **Stats** / **Tax** data panels.

*Add components (sliders, toggles, tabs) as the library grows.*

---

## 3. Patterns

### 3.0 Main menu (**MainMenu** scene)

- **Flow:** **Continue**, **New Game**, **Load City**, **Options** — `MainMenuController` wires **Button** listeners; **Load City** / **Options** use nested panels when built at runtime.
- **As-built Canvas:** If **Inspector** references are **null**, **`BuildUI()`** creates **`Canvas`** (**Screen Space Overlay**), **`CanvasScaler`**: **Scale With Screen Size**, **reference resolution 1280×720**, **match** **0.5**, plus **GraphicRaycaster** and **EventSystem** if missing.
- **Export note:** **Edit Mode** **UI** inventory export lists **zero** **`canvases`** for **`MainMenu.unity`** when the scene file has **no** serialized **Canvas**; **as-built** menu **UI** is defined by **`MainMenuController`** + **Play Mode** (or **Inspector**-assigned objects when used).

### 3.1 HUD information density

- **As-built cluster:** Primary readouts live under **`Canvas/DataPanelButtons`**: **`DatePanel`** (city name, date), **`StatsPanel`** (population, money, happiness, power, water, jobs, demand bars, feedback), **`DetailsPanel`** (selection details), **`TaxPanel`** (taxes + growth sliders), **`GameButtons`** (stats/taxes toggles, zoom, mini-map, speed, simulate growth).
- **Orchestration:** **`UIManager`** owns references to **`Text`** fields; align copy and ordering with [`managers-reference.md`](managers-reference.md) when extending stats.

### 3.2 Popups

- **`PopupType`** (`UIManager.cs`): **LoadGame**, **Details**, **BuildingSelector**, **StatsPanel**, **TaxPanel**.
- **Representative Canvas paths (city scene):**
  - **LoadGame** → `Canvas/LoadGameMenuPanel` (+ **Scroll View**)
  - **BuildingSelector** → `Canvas/ControlPanel/BuildingSelectorPopupPanel`
  - **Details** → `Canvas/DataPanelButtons/DetailsPanel`
  - **StatsPanel** → `Canvas/DataPanelButtons/StatsPanel`
  - **TaxPanel** → `Canvas/DataPanelButtons/TaxPanel`
- **Feedback:** `Canvas/InsufficientFundsPanel`, `Canvas/NotificationPanel` — tied to economy / **Game notification** flows.
- **Controllers:** e.g. **`DetailsPopupController`**, **`BuildingSelectorMenuController`**, **`DataPopupController`** register with **`UIManager`**.

### 3.3 Tool selection / toolbar

- **Scene path:** **`Canvas/ControlPanel`** under **`UI/City/Canvas`** (**MainScene**).
- **Inventory and constraints:** [`.cursor/projects/TECH-67.md`](../projects/TECH-67.md) **§4.4** (**Codebase inventory**).
- **As-built (current):** **Left**-docked **vertical** **toolbar**: category **rows** (e.g. demolition, roads, utilities, **RCI** zoning, environment) with **horizontal** tool **`Button`** groups per row; dependent overlays (e.g. zoning density) re-anchored to the sidebar; avoid overlapping **mini-map** and corner **HUD** (**Editor**-authored layout in **`MainScene.unity`**).
- **Implementation note:** Prefer documented **LayoutGroup** hierarchy when refactoring; confirm **`Canvas Scaler`** (**§4.3**) at reference resolutions. Refresh [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) after hierarchy changes ([`docs/reports/README.md`](../../docs/reports/README.md)).

### 3.4 Feedback and errors

- **Insufficient funds** — `InsufficientFundsPanel` + text field.
- **Notifications** — `NotificationPanel` / **Game notification** path.

### 3.5 World vs UI input

- **Expectation:** Pointer over **UI** should consume events so **camera** / **grid** tools do not fire through panels; **`GridManager`** / **`CameraController`** use **`IsPointerOverGameObject`** patterns.
- **Scroll:** **ScrollRect** over lists vs **camera** zoom — **BUG-19** class issues; test **LoadGame** and **BuildingSelector** scroll views in **Play Mode**.

---

## 4. Unity mapping

### 4.1 Naming

- **As-built:** Functional names on **ControlPanel** children (`*SelectorButton`, `*Panel`); some duplicate **Unity** auto-names (`TotalGrowthLabel (1)`). Prefer descriptive names on new objects; **§4.1** prefab prefix convention (**`UI_Button_Primary`**, etc.) remains **target** — not enforced globally yet.
- Controllers stay focused; avoid duplicating styling logic across many **`MonoBehaviour`**s — prefer shared prefab variants or a small theme helper if introduced in a dedicated **BACKLOG** tech row.

### 4.2 Scripting

- Cache component references in `Awake`/`Start`; no `FindObjectOfType` in `Update` for UI.
- New UI features: document public API in XML per project conventions.

### 4.3 Canvas

| Scene | Canvas path (scene / code) | `RenderMode` | **Canvas Scaler** (as-built) |
|-------|----------------------------|--------------|------------------------------|
| **MainScene** | `UI/City/Canvas` | **Screen Space Overlay** | **Scale With Screen Size**, reference **800×600**, **match** **0.5** — from **UI** inventory baseline export |
| **MainMenu** | Runtime: root object **`Canvas`** (when `BuildUI()`) | **Screen Space Overlay** | **Scale With Screen Size**, reference **1280×720**, **match** **0.5** — from `MainMenuController.cs` |
| **MainMenu** | **Inspector**-wired UI | *Per instance* | If **Canvas** is authored in scene later, re-run export and update this table |

**Acceptance testing:** Sanity-check **HUD** and **ControlPanel** at **800×600** and **1920×1080** (and **1280×720** for menu) when changing scaler or anchors.

---

## 5. Acceptance criteria (per issue)

When opening a backlog issue for UI work, include:

1. **Spec section** this issue implements or updates.
2. **Screens affected** (HUD, which popup, etc.).
3. **Play Mode checks:** resolution sanity, hover/click, scroll not leaking to camera where applicable.
4. **Regression:** related systems (`UIManager`, listed controllers) still wire in Inspector.

---

## 6. Revision history

| Date | Change |
|------|--------|
| *YYYY-MM-DD* | Initial draft scaffold |
| 2026-03-20 | §3.3 — ControlPanel toolbar layout variants; cross-link **unity-development-context** |
| 2026-04-04 | Overview links → **`projects/ui-as-code-exploration.md`** (retired `docs/ui-design-system-project.md` / `docs/ui-design-system-context.md`) |
| 2026-04-06 | Program notes → **`.cursor/projects/TECH-67.md`**; delete **`projects/ui-as-code-exploration.md`** (inventory **§4.4**) |
| 2026-04-04 | **As-built vs target** subsection; **UI-as-code** program baseline for **`ui-design-system.md`** (**glossary** **UI design system (reference spec)**) |
| 2026-04-04 | **§1**–**§4**, **§2**–**§3** **as-built** from [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json); **§3.0** **Main menu**; **§6** traceability note |

### Machine-readable traceability (UI inventory baseline)

- **Committed snapshot:** [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) — refresh from **Postgres** `editor_export_ui_inventory` or **Territory Developer → Reports → Export UI Inventory (JSON)** when **UI** hierarchies change (see [`docs/reports/README.md`](../../docs/reports/README.md)).
- **Field mapping:** JSON node **`path`** is relative to the sampled **Canvas** root (`Canvas/…`); full scene path is **`UI/City/Canvas`** + **`path`** for **MainScene**.
