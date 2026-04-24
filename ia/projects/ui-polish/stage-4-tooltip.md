### Stage 4 — ThemedPrimitive ring / Primitives batch A (Panel / Button / Label / Icon / Tooltip)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship first 5 structural primitives — layout + typography + iconography focused. Each inherits `ThemedPrimitiveBase`, reads tokens in `ApplyTheme`, writes to uGUI `Image` / `TextMeshProUGUI` / child references. Allocation-free repaint path.

**Exit:**

- `ThemedPanel.cs` — `Image.color = theme.surfaceBase`; optional `ShadowDepth` slot (wired in Step 4).
- `ThemedButton.cs` — background + label color + hover / pressed variants from token accent ladder.
- `ThemedLabel.cs` — font / size / color from `UiTheme` typography fields.
- `ThemedIcon.cs` — tint from token accent ladder; sprite ref external.
- `ThemedTooltip.cs` — panel + label composition; delay / fade duration from `theme.motion.alertPulse` or dedicated tooltip entry.
- All under `Assets/Scripts/UI/Primitives/`.
- Phase 1 — Layout + typography primitives (Panel / Label / Icon).
- Phase 2 — Interactive + composite (Button / Tooltip).

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | ThemedPanel | _pending_ | _pending_ | `Assets/Scripts/UI/Primitives/ThemedPanel.cs` — `class ThemedPanel : ThemedPrimitiveBase`. Requires `Image` component. `ApplyTheme`: `image.color = theme.surfaceBase`; `image.sprite = theme.panelBackgroundSprite` (if field exists; else skip). Optional `[SerializeField] PanelElevation elevation` enum → maps to `theme.studioRack.shadowDepthStops[idx]`. |
| T4.2 | ThemedLabel | _pending_ | _pending_ | `ThemedLabel.cs` — wraps `TextMeshProUGUI`. `ApplyTheme`: `text.font = theme.primaryFont`; `text.color = theme.textPrimary`; `text.fontSize = theme.typography.bodySize` (or expose `LabelTier` enum mapping to h1/h2/body/caption sizes). |
| T4.3 | ThemedIcon | _pending_ | _pending_ | `ThemedIcon.cs` — wraps `Image` sized via token grid (`theme.iconGrid`). `ApplyTheme`: `image.color = theme.accentPrimary` (or enum-selected accent tier). Sprite ref Inspector-set per instance. |
| T4.4 | ThemedButton | _pending_ | _pending_ | `ThemedButton.cs` — requires `Button` + background `Image` + `ThemedLabel` child. `ApplyTheme`: sets base + hover + pressed + disabled color transitions on `Button.colors` from accent ladder. Hover pulse amplitude reads `theme.motion.alertPulse` (juice wires in Step 4). |
| T4.5 | ThemedTooltip | _pending_ | _pending_ | `ThemedTooltip.cs` — composite: `ThemedPanel` + `ThemedLabel`. `ApplyTheme` cascades to children (cached `Awake`). Fade in/out duration reads `theme.motion` dedicated tooltip entry (add to `MotionBlock` in Stage 1.1 if not present — cross-check before filing). |
