# uGUI deletion sweep — exploration seed

**Status:** seed (TECH-32931 — ui-toolkit-migration Stage 6.0)
**Follows:** ui-toolkit-migration final stage (stage-6-0) — all 51 prefabs migrated to UI Toolkit.
**Zero-gate:** `npm run validate:no-legacy-ugui-refs` returns TOTAL = 0 before ship.

---

## Goal

Hard-delete all legacy uGUI / quarantined primitives after zero-gate is confirmed green:

1. Delete `UiBindRegistry.cs` + consumers migrate to native `INotifyValueChanged` / `ChangeEvent<T>`.
2. Delete `UiTheme.cs` + `.asset` (if ever created) — runtime consumes USS `var(--ds-*)` only.
3. Delete `ThemedPrimitiveBase` + full Themed ring:
   - Simple: `ThemedLabel`, `ThemedDivider`, `ThemedBadge`, `ThemedIcon`, `ThemedSectionHeader`, `ThemedIlluminationLayer`, `ThemedOverlayToggleRow`
   - Complex (alongside UxmlElement successor): `ThemedButton`, `ThemedFrame`, `ThemedList`, `ThemedTabBar`, `ThemedTabCell`, `ThemedToggle`, `ThemedSlider`, `ThemedTooltip`, `ThemedPanel`
   - Renderers: `ThemedPrimitiveRendererBase`, `ThemedListRenderer`, `ThemedTabBarRenderer`, `ThemedToggleRenderer`, `ThemedSliderRenderer`, `ThemedTooltipRenderer`, `ThemedOverlayToggleRowRenderer`
   - Interface: `IThemed`, struct: `SlotSpec`
4. Move retired wrappers to `.archive/` (if any remain with lingering Inspector refs).
5. Delete `UiBakeHandler` prefab-bake paths that reference uGUI `Image` / `RectTransform` directly (coordinate with `ui-bake-handler-atomization` plan).
6. Delete legacy Canvas refs in scene wires (validate via `validate:scene-wire-drift`).

---

## Baseline (as of Stage 6.0 quarantine)

Captured by `npm run validate:no-legacy-ugui-refs` at stage-6-0 close:

| Category | Count |
|---|---|
| Canvas (uGUI) | 2 |
| CanvasRenderer | 4 |
| RectTransform | 30 |
| UiBindRegistry | 32 |
| UiTheme | 51 |
| **TOTAL** | **119** |

Zero-gate = all rows must read 0 before deletion plan ships.

---

## Deletion criteria

- `validate:no-legacy-ugui-refs` → TOTAL = 0 (upgrade validator from advisory → hard-fail when ship starts).
- `validate:all` green.
- `unity:compile-check` clean.
- `validate:scene-wire-drift` green (no dangling Canvas refs in scene wire YAML).
- All 51 prefabs emitting `UIDocument` + UXML + USS confirmed by `validate:catalog-panel-coverage`.

---

## Suggested plan shape

Stage N+1 of a new `ugui-deletion-sweep` master plan:

1. Migrate residual `UiBindRegistry` consumers to native binding (each consumer = 1 task).
2. Migrate residual `UiTheme` read paths to USS `var(--ds-*)`.
3. Delete Themed ring + renderers + IThemed + SlotSpec.
4. Delete `UiBindRegistry.cs` + `UiTheme.cs`.
5. Upgrade `validate:no-legacy-ugui-refs` exit code → 1 on non-zero (hard-gate).
6. Final validate:all + compile-check green confirm.

---

## Related docs

- `docs/explorations/ui-toolkit-migration.md` — strangler pattern history (51 prefabs).
- `docs/explorations/ui-bake-handler-atomization.md` — bake handler split plan.
- `ia/specs/glossary.md §Retired terms` — quarantined terms + replacements.
