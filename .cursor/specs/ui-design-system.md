# UI / UX Design System — Spec

## Overview

This spec defines **foundations**, **components**, and **patterns** for Territory Developer’s in-game UI so that backlog issues can reference concrete sections. **Program charter**, **codebase inventory**, **backlog bridge**, and **roadmap** live in [`.cursor/projects/TECH-67.md`](../projects/TECH-67.md) (**UI-as-code program** umbrella — not a reference spec). **Executable issues:** [`BACKLOG.md`](../../BACKLOG.md). **First spec milestone:** document **as-built** (**shipped**) **UI** in **§1–§4** and major **§2–§3** surfaces — see **TECH-68** (`.cursor/projects/TECH-68.md`). **Scenes:** **UI** spans **`MainMenu`**, **`MainScene`** (future **`CityScene`**), and future surfaces (e.g. **`RegionScene`**) — **TECH-68** exports and prose are **per scene**.

### As-built vs target

- **As-built (current):** What the game **actually** uses today — **Canvas** settings, **colors**, **fonts** / sizes, **margins**, **anchors**, **HUD** / **toolbar** / **popup** layout, and representative **UX** behaviors. Sourced from **`MainMenu.unity`**, **`MainScene.unity`** (or renamed **city** scene), **prefabs**, **`UIManager`**, and **controllers**. This is the **default** meaning of tables in **§1** until a row is explicitly marked **Target**.
- **Target (planned):** Future layout or tokens defined by **BACKLOG** issues (e.g. **TECH-07** **ControlPanel**). Keep **Target** subsections or labeled rows **alongside** **as-built** so refactors stay traceable.

### Domain vocabulary

Backlog items and player-facing copy that name gameplay systems should use [`glossary.md`](glossary.md) terms (**Game notification**, **street**, **interstate**, **map border**, **road validation pipeline**, etc.) so wording matches reference specs and **territory-ia** tools (`glossary_discover`, `glossary_lookup`).

**Status:** Draft — **§1** and related tables transition from **TBD** to **as-built** under **TECH-68**; then **target** states are updated as issues ship. **Glossary:** **UI design system (reference spec)**.

## Related files (code and assets)

| Area | Location / notes |
|------|------------------|
| Main UI orchestration | `Assets/Scripts/Managers/GameManagers/UIManager.cs` |
| Popup controllers | `Assets/Scripts/Controllers/UnitControllers/*Popup*.cs`, `DetailsPopupController.cs`, etc. |
| HUD / stats | `CityStatsUIController.cs`, `UIManager.cs` (many `Text` fields) |
| Input vs UI | `EventSystem`, `CameraController.cs`, `GridManager.cs` (`IsPointerOverGameObject` patterns) |
| Scene | `Assets/Scenes/MainScene.unity` — Canvas hierarchy (authoritative in Editor) |

Add prefab paths under `Assets/` as they are standardized.

---

## 1. Foundations

### 1.1 Color

| Token name | Usage | Value / reference |
|------------|--------|-------------------|
| *e.g. `ui-primary`* | Primary actions, key highlights | *TBD* |
| *e.g. `ui-surface`* | Panels, cards | *TBD* |
| *e.g. `ui-text-primary`* | Main labels | *TBD* |
| *e.g. `ui-danger`* | Destructive / critical | *TBD* |

Document whether colors are **Unity `Color`**, **Sprite** tinting, or **UI material** based.

### 1.2 Typography

| Style | Font asset | Size | Weight | Usage |
|-------|------------|------|--------|--------|
| *e.g. HUD title* | *TBD* | *TBD* | *TBD* | Top bar, screen titles |
| *e.g. Body* | *TBD* | *TBD* | *TBD* | Popups, descriptions |

### 1.3 Spacing and layout

- **Grid:** e.g. 4px or 8px base unit (TBD).
- **Safe area / margins:** TBD for HUD edges and popups.
- **Anchors:** Prefer consistent anchor presets per pattern (e.g. full-stretch panels vs centered modals).

### 1.4 Iconography

- Source / style (line vs filled), sizes, and tint rules — TBD.

### 1.5 Motion (optional)

- Duration and easing for show/hide of panels — TBD; keep scope small unless an issue explicitly covers animation.

---

## 2. Components

Each component should list **variants**, **states**, and **when to use**. Link prefabs when they exist.

### 2.1 Button — primary

- **Variants:** TBD (primary, secondary, ghost, icon-only).
- **States:** normal, highlighted, pressed, disabled.
- **Specs:** min height, padding, corner radius (if applicable).

### 2.2 Button — secondary

- TBD — contrast vs primary; de-emphasized actions.

### 2.3 Panel / card

- Background, border, padding, optional header slot.

### 2.4 List / scroll

- Row height, hover/selection, scrollbar styling.
- **Input:** ensure scroll does not propagate to game camera where inappropriate (verify against **BACKLOG** if a regression is suspected).

### 2.5 Tooltip

- Delay, max width, placement relative to cursor or widget.

### 2.6 Modal / dialog

- Overlay dimming, close affordances, primary/secondary action order.

*Add components (sliders, toggles, tabs) as the library grows.*

---

## 3. Patterns

### 3.1 HUD information density

- Priority of stats (population, money, date, demands) — align with `UIManager` layout groups.

### 3.2 Popups

- Map to `PopupType` and controllers: load game, details, building selector, stats, tax.
- Consistent header, close, and primary action placement.

### 3.3 Tool selection / toolbar

- Selected vs unselected tool buttons; connection to `CursorManager` and mode flags on `UIManager`.
- **Scene:** Primary toolbar lives in `MainScene.unity` (GameObject **`ControlPanel`**). Inventory and constraints: [`.cursor/projects/TECH-67.md`](../projects/TECH-67.md) **§4.4** (**Codebase inventory** — **ControlPanel**).
- **Layout variants (document the active one in context and verify in Play Mode):**
  - **Current (legacy):** horizontal strip, **bottom-center** dock — category groups as columns in one row.
  - **Target layout:** **left** dock, **vertical** panel — **one row per category** (stacked vertically), **buttons within each row remain horizontal**. Use consistent spacing (`§1.3`) and anchors so overlays (e.g. zoning density options) re-anchor to the sidebar instead of the old bottom bar. Avoid overlapping the mini-map and corner HUD.
- **Implementation:** Prefer `VerticalLayoutGroup` (categories) + `HorizontalLayoutGroup` (buttons per row) or an equivalent documented hierarchy; confirm `Canvas Scaler` (`§4.3`) at reference resolutions.

### 3.4 Feedback and errors

- Insufficient funds, invalid placement — reuse a single feedback component or text style where possible.

### 3.5 World vs UI input

- Document expectation: pointer over UI blocks or passes events consistently; reference `EventSystem` checks.

---

## 4. Unity mapping

### 4.1 Naming

- Prefab naming convention — TBD (e.g. `UI_Button_Primary`, `UI_Panel_Standard`).
- Controllers remain focused; avoid duplicating styling logic across many `MonoBehaviour`s — prefer shared prefab variants or a small theme helper if introduced in a dedicated **BACKLOG** tech row.

### 4.2 Scripting

- Cache component references in `Awake`/`Start`; no `FindObjectOfType` in `Update` for UI.
- New UI features: document public API in XML per project conventions.

### 4.3 Canvas

- Document target **Canvas Scaler** mode and reference resolution once locked.

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
| 2026-04-04 | **As-built vs target** subsection; **TECH-68** as first **UI-as-code** spec milestone (**glossary** **UI design system (reference spec)**) |
