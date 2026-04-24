### Stage 12 — Flagship HUD + Toolbar + overlay polish / Toolbar + overlay migration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Migrate toolbar rows + overlay toggle row. Bucket 2 signal scalars bound via `IStudioControl.BindSignalSource` into LED indicators. Tool-select pulse closes the flagship polish loop for Step 5.

**Exit:**

- Toolbar rows → `ThemedPanel` + `IlluminatedButton` clusters; tool-category switch raises `PulseOnEvent`.
- Overlay toggle row: one `ThemedOverlayToggleRow` per Bucket 2 signal (6 signals). Each row carries `ThemedIcon` + `ThemedLabel` + `LED` active-state indicator.
- `LED.BindSignalSource(() => overlayManager.GetSignalIntensity(signalKind))` wired in `ToolbarController.Awake`.
- Toolbar bg `ThemedPanel` + `ShadowDepth` per tier.
- `npm run validate:all` + `npm run unity:compile-check` green.
- Phase 1 — Toolbar button migration + tool-select pulse.
- Phase 2 — Overlay toggle row + Bucket 2 signal binding.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | Toolbar cluster migration | _pending_ | _pending_ | Existing toolbar zoning / service / transport buttons → `IlluminatedButton` clusters under `ThemedPanel` rows. Active-state LED bound to `ToolbarController.currentTool` matcher. Row bg `ThemedPanel` + `ShadowDepth` tier 1. |
| T12.2 | Tool-category pulse | _pending_ | _pending_ | On `ToolbarController.SetCategory(newCategory)` event → `PulseOnEvent.Trigger()` on newly-active cluster + inverse pulse on deactivating cluster. Duration from `theme.motion.alertPulse`. No per-frame scan; event-driven only. |
| T12.3 | Overlay toggle rows (6 signals) | _pending_ | _pending_ | Instantiate 6 `ThemedOverlayToggleRow` instances under toolbar overlay area — one per Bucket 2 signal (pollution / crime / traffic / happiness / zone / desirability). Each row: icon + label + `LED`. Toggle click → `OverlayRenderer.SetSignalEnabled(kind, bool)`. |
| T12.4 | LED signal-intensity binding | _pending_ | _pending_ | In `ToolbarController.Awake`, each overlay row's `LED` gets `BindSignalSource(() => overlayManager.GetSignalIntensity(kind))`. Verify zero per-frame alloc across 1000 frames. Confirms invariant #3 + completes flagship polish exit criteria. |

---
