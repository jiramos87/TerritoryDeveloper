---
name: ui-hud-row-theme
description: Add or adjust a HUD/menu row using UiTheme and the UI design system. Use when touching MainMenu strip, shared colors, or font sizes for new uGUI rows.
---

# UI row + theme

## When to use

- New **HUD** or **menu** **Button** / **Text** row where literals would diverge from **`UiTheme`**.
- **Main menu** changes under **`MainMenu.unity`** / **`MainMenuController`**.

## Recipe

1. Read **`.cursor/specs/ui-design-system.md`** **§1**, **§3.0**, **§4.3**, and **§5.2** (theme / prefab paths).
2. Prefer **`Territory.UI.UiTheme`** (`Assets/UI/Theme/DefaultUiTheme.asset`) for **menu** strip colors and sizes; extend **`UiTheme.cs`** fields if you need new token groups (keep **XML** summary accurate).
3. For **city** **HUD**, migrate incrementally — **§1.2** **typography** policy applies (**legacy `Text`** vs **TMP**); do not mix stacks on the same row without a **Decision Log** row in the relevant **BACKLOG** issue or a follow-up issue.
4. After **`.unity`** hierarchy edits, refresh **`docs/reports/ui-inventory-as-built-baseline.json`** via **Territory Developer → Reports → Export UI Inventory (JSON)** (see **`docs/reports/README.md`**).
5. Optional: **Territory Developer → Reports → Validate UI Theme asset** (`UiThemeValidationMenu.cs`).
6. For **v0** building blocks (**tool button**, **stat row**, **scroll shell**, **modal shell**): run **Territory Developer → UI → Scaffold UI Prefab Library v0** (`UiPrefabLibraryScaffoldMenu.cs`) so **`Assets/UI/Prefabs/UI_*.prefab`** exist before wiring instances.

## Do not

- Add **`FindObjectOfType`** in **`Update`** for UI wiring (**invariants**).
- Introduce new **singletons** for UI.

## Trace

- **BACKLOG / specs:** **UI-as-code program** — **glossary**; **`ui-design-system.md`** **§5.2** + **Codebase inventory (uGUI)**
- **Theme asset:** `Assets/UI/Theme/DefaultUiTheme.asset`
- **Prefab v0:** `Assets/UI/Prefabs/` — scaffold menu above
