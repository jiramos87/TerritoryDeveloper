# UI / UX Design System — Context and Discovery

## Overview

Territory Developer’s player-facing UI is built in **Unity** using **Unity UI (uGUI)** — `Canvas`, `Graphic` components (`Image`, `Text`, etc.), and `EventSystem` for input. The main orchestrator is **`UIManager`**, which holds many serialized references to texts, images, popups, and tool state. Controllers under `Assets/Scripts/Controllers/` handle focused interactions (buttons, popups, sliders).

This document captures **current context** so the design system spec and backlog issues are grounded in the real codebase. Update it when architecture or major screens change.

## Architectural placement

From [ARCHITECTURE.md](../ARCHITECTURE.md):

- **UI layer** — `UIManager`, `CursorManager`, `GameNotificationManager`, controllers.
- **Input** — `GridManager` and others often gate world input when the pointer is over UI (`EventSystem` / `IsPointerOverGameObject`); scroll vs camera behavior is a known UX area (see backlog **BUG-19**).

## Primary entry points

| File | Role |
|------|------|
| `Assets/Scripts/Managers/GameManagers/UIManager.cs` | Main HUD, popups (`PopupType`: load game, details, building selector, stats, tax), toolbar / zone and tool selection, demand visualization, many `Text` / `Image` references |
| `Assets/Scripts/Managers/GameManagers/CursorManager.cs` | Cursor state coordinated with tools and UI |
| `Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs` | Notifications (singleton with `DontDestroyOnLoad`) |

Namespace: `UIManager` lives in `Territory.UI` (`namespace Territory.UI` in `UIManager.cs`).

## Controllers (inventory)

### `Assets/Scripts/Controllers/GameControllers/`

| File | Role |
|------|------|
| `CameraController.cs` | Camera movement, zoom (scroll wheel interacts with UI in some cases) |
| `CityStatsUIController.cs` | City stats presentation |
| `MiniMapController.cs` | Mini-map UI and layers |

### `Assets/Scripts/Controllers/UnitControllers/`

| File | Role |
|------|------|
| `BuildingSelectorMenuController.cs` | Building selection menu |
| `CameraButtonsController.cs` | Camera control buttons |
| `CommercialZoningSelectorButton.cs` | Commercial zone tool |
| `DataPopupController.cs` | Data popup |
| `DetailsPopupController.cs` | Tile / building details popup |
| `EnviromentalSelectorButton.cs` | Environmental / forest tool |
| `GrowthBudgetSlidersController.cs` | Growth budget sliders |
| `IndustrialZoningSelectorButton.cs` | Industrial zone tool |
| `MiniMapLayerButton.cs` | Mini-map layer toggle |
| `PowerBuildingsSelectorButton.cs` | Power building tool |
| `ResidentialZoningSelectorButton.cs` | Residential zone tool |
| `RoadsSelectorButton.cs` | Road tool |
| `ShowMiniMapButton.cs` | Show mini-map |
| `ShowStatsButton.cs` | Stats panel |
| `ShowTaxes.cs` | Tax panel |
| `SimulateGrowthToggle.cs` | Growth simulation toggle |
| `SpeedButtonsController.cs` | Time speed controls |
| `WaterBuildingSelectorButton.cs` | Water building tool |

Other managers consume UI indirectly: e.g. `StatisticsManager`, `EconomyManager`, `TimeManager` interact with systems that surface data in HUD.

## Scene and prefab notes

- Primary layout lives on **`MainScene`** (and related) — **Canvas** hierarchy, panels, and assigned references are edited in Unity; this file does not duplicate scene object names (they drift). When documenting a pilot screen, add a subsection here: *Canvas path, key prefabs, fonts/materials.*

### Toolbar — `ControlPanel` (MainScene)

- **GameObject:** `ControlPanel` (under the main HUD Canvas in `Assets/Scenes/MainScene.unity`).
- **Role:** Primary construction **toolbar**: demolition, RCI zoning, power/water, roads, forest/environment tools. Tool state is driven by `UIManager` (`Territory.UI`); toolbar buttons use `UnitControllers/*SelectorButton.cs` and related scripts wired in the Inspector.
- **Layout (planned):** Migrate from **bottom-centered horizontal ribbon** to a **left-docked vertical** panel with **category rows** and **horizontal button groups per row** — see backlog **TECH-07** and **§3.3** of the [design system spec](../.cursor/specs/ui-design-system.md). After implementation, update this subsection with the concrete Canvas path (e.g. `Canvas/ControlPanel/...`) and any `LayoutGroup` setup.

## Technical constraints (typical for this project)

- **Resolution / Canvas** — Scale mode and reference resolution affect layout; document chosen approach in the [design system spec](../.cursor/specs/ui-design-system.md) once decided.
- **EventSystem** — UI must consume pointer events where appropriate so world tools (e.g. camera zoom) do not fire through panels (**BUG-19** pattern).
- **Performance** — Avoid `FindObjectOfType` per frame in UI code (**BUG-14** pattern); cache references in `Start` / serialized fields.
- **Coupling** — `UIManager` is large (~1200+ lines); new work should prefer small controllers or shared helpers over growing a single class (align with project anti-patterns in `AGENTS.md`).

## Known pain points (from backlog and code shape)

- Scroll wheel over UI lists also affecting camera (**BUG-19**).
- `FindObjectOfType` in `Update` paths for UI-related managers (**BUG-14** and related).
- Happiness and other stats display inconsistencies (**BUG-12** — example of logic/UI coupling).

Add rows as discovery finds more.

## References

- [ARCHITECTURE.md](../ARCHITECTURE.md) — Layering and dependency map
- [.cursor/specs/ui-design-system.md](../.cursor/specs/ui-design-system.md) — Normative spec for the design system
- [BACKLOG.md](../BACKLOG.md) — Issues to link from the program charter

---

*Update this document when major UI surfaces or constraints change.*
