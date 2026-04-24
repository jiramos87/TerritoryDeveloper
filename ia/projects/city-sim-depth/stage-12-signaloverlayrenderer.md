### Stage 12 — Overlays + HUD Parity / SignalOverlayRenderer

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `SignalOverlayRenderer` (per-cell color gradient texture per signal + district boundary mode) and HUD toggle.

**Exit:**

- `SignalOverlayRenderer` renders visible per-cell color gradient for active signal.
- District boundary mode renders `DistrictMap` assignments as distinct per-district colors.
- HUD toggle cycles overlay modes (off → signals 0–11 → DistrictBoundary → off).
- No per-frame `FindObjectOfType` — invariant #3 verified.
- Phase 1 — SignalOverlayRenderer MonoBehaviour + OverlayConfig ScriptableObject.
- Phase 2 — Auto-normalization + district boundary mode + HUD toggle.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | SignalOverlayRenderer MonoBehaviour | _pending_ | _pending_ | `SignalOverlayRenderer` MonoBehaviour — `Texture2D overlayTex` sized `gridWidth × gridHeight`; `Render(SimulationSignal s)` iterates cells, maps `SignalField.Get(x,y)` through `OverlayConfig.colorRamp` gradient, sets texture pixel; `RenderDistricts()` path maps `DistrictMap.Get(x,y)` to palette color; `SetActive(bool)` shows/hides overlay quad; `[SerializeField] SignalFieldRegistry` + `FindObjectOfType`. |
| T12.2 | OverlayConfig ScriptableObject | _pending_ | _pending_ | `OverlayConfig` ScriptableObject — fields: `SimulationSignal signal`, `Gradient colorRamp` (seed: green→yellow→red for pollution signals, blue gradient for service signals, grey for traffic/waste), `bool autoNormalize`, `float fixedMax`; one asset per signal (12 total); referenced array on `SignalOverlayRenderer`. |
| T12.3 | Auto-normalization + district boundary | _pending_ | _pending_ | When `OverlayConfig.autoNormalize = true`: compute `fieldMax = SignalField.Max()` per render call; normalize before gradient lookup; clamp `fieldMax` to `epsilon` floor to avoid divide-by-zero. District boundary `RenderDistricts()`: iterate `DistrictMap`, map `districtId % paletteSize` to `Color[] districtPalette` array (hardcoded 8-color seed). |
| T12.4 | HUD overlay toggle | _pending_ | _pending_ | Add overlay toggle button to HUD (follow `UIManager.Hud.cs` pattern); `OverlayMode` enum (`Off`, then 12 signal entries, `DistrictBoundary`); `UIManager.CycleOverlayMode()` increments enum mod length, calls `SignalOverlayRenderer.Render(signal)` or `RenderDistricts()` accordingly; integrate with `UIManager.Theme.cs` button style. |

---
