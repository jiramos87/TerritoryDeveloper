### Stage 13 — Overlays + HUD Parity / District Info Panel

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship district info panel (click-to-open, shows `DistrictSignalCache` aggregates for all 12 signals); `DistrictSignalCache.GetAll` Bucket 8 facade; end-to-end smoke test confirming all signal chains produce expected values.

**Exit:**

- Clicking district cell (or in district overlay mode) opens info panel.
- Panel shows all 12 signal aggregate values for selected district.
- `DistrictSignalCache.GetAll(districtId)` returns 12-entry dictionary; no throw on empty district.
- Smoke test: 30-day city shows non-zero PollutionAir, Crime, ServicePolice, LandValue in district panel.
- Phase 1 — District info panel UI + district click selection.
- Phase 2 — Bucket 8 GetAll facade + end-to-end smoke test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T13.1 | DistrictInfoPanel UI | _pending_ | _pending_ | `DistrictInfoPanel` UI component — popup panel following `UIManager.PopupStack` pattern; shows district name + scrollable table of 12 signal rows (signal name, aggregate value, unit label from `OverlayConfig`); `Show(int districtId)` populates from `DistrictSignalCache.GetAll(districtId)`; NaN shown as "—"; `Hide()` via popup stack `Pop`. |
| T13.2 | DistrictSelector + click handler | _pending_ | _pending_ | `DistrictSelector` component on camera/input system — on left-click when `OverlayMode == DistrictBoundary`, call `DistrictManager.GetDistrictId(gridX, gridY)` → `DistrictInfoPanel.Show(id)`; highlight selected district boundary in `SignalOverlayRenderer`; deselect on second click same cell or Escape. |
| T13.3 | Bucket 8 GetAll facade | _pending_ | _pending_ | Finalize `DistrictSignalCache.GetAll(int districtId)` — return `Dictionary<SimulationSignal, float>` (12 entries; NaN for empty district, not missing key); add `GetAllDistricts()` returning `List<(int districtId, Dictionary<SimulationSignal, float> values)>` — read-model API surface Bucket 8 CityStats overhaul will consume without per-feature glue code. |
| T13.4 | End-to-end smoke test | _pending_ | _pending_ | EditMode (or minimal PlayMode) smoke test — place 3 I-heavy + 5 R-medium + 1 police station; advance 30 in-game days via tick loop; assert `DistrictSignalCache.GetAll(0)[PollutionAir]` > 0, `[Crime]` > 0, `[ServicePolice]` > 0, `[LandValue]` > 0; assert `DistrictInfoPanel.Show(0)` does not throw; assert overlay `Render(PollutionAir)` produces non-zero texture pixel at industry location. |

---
