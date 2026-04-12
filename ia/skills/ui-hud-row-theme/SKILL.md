---
purpose: Add or adjust a HUD/menu row using UiTheme and the UI design system.
audience: agent
loaded_by: skill:ui-hud-row-theme
slices_via: none
name: ui-hud-row-theme
description: Add or adjust a HUD/menu row using UiTheme and the UI design system. Use when touching MainMenu strip, shared colors, or font sizes for new uGUI rows.
---

# UI row + theme

## When to use

- New HUD/menu Button/Text row where literals would diverge from `UiTheme`.
- Main menu changes under `MainMenu.unity` / `MainMenuController`.

## Recipe

1. Read `ia/specs/ui-design-system.md` §1, §3.0, §4.3, §5.2 (theme / prefab paths).
2. Prefer `Territory.UI.UiTheme` (`Assets/UI/Theme/DefaultUiTheme.asset`) for menu strip colors/sizes. Extend `UiTheme.cs` fields for new token groups (keep XML summary accurate).
3. City HUD — migrate incrementally. §1.2 typography policy (legacy `Text` vs TMP); do NOT mix stacks on one row without Decision Log row in BACKLOG issue or follow-up issue.
4. After `.unity` hierarchy edits, refresh `docs/reports/ui-inventory-as-built-baseline.json` via Territory Developer → Reports → Export UI Inventory (JSON) (`docs/reports/README.md`).
5. Optional: Territory Developer → Reports → Validate UI Theme asset (`UiThemeValidationMenu.cs`).
6. For v0 blocks (tool button, stat row, scroll shell, modal shell): Territory Developer → UI → Scaffold UI Prefab Library v0 (`UiPrefabLibraryScaffoldMenu.cs`) before wiring instances.

## Do not

- `FindObjectOfType` in `Update` for UI wiring (invariants).
- New singletons for UI.

## Trace

- BACKLOG/specs: UI-as-code program — glossary; `ui-design-system.md` §5.2 + Codebase inventory (uGUI)
- Theme asset: `Assets/UI/Theme/DefaultUiTheme.asset`
- Prefab v0: `Assets/UI/Prefabs/` — scaffold menu above
