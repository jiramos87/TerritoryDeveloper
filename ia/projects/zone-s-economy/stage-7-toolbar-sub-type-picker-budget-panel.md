### Stage 7 — UI surfaces + CityStats integration + economy-system reference spec / Toolbar + sub-type picker + budget panel

**Status:** Final

**Objectives:** Player can click S button, pick a sub-type, place a building, tune envelope sliders. End-to-end flow with visible feedback on envelope draws + overspend blocks.

**Exit:**

- S zoning button visible in toolbar, correct icon, enters S placement mode on click.
- Sub-type picker opens over placement mode; 7 buttons; click commits sub-type + resumes cursor placement.
- Picker cancel (ESC or outside-click) closes picker without cost or placement (N3).
- Budget panel reachable from HUD; 7 sliders sum-locked + commit normalizes to 1.0; global cap slider + remaining readouts live-update.
- Overspend-blocked notification visible when `TryDraw` returns false.
- Phase 1 — S zoning button + placement mode entry.
- Phase 2 — Sub-type picker modal.
- Phase 3 — Budget panel with envelope sliders + global cap.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | S zoning button in `UIManager.ToolbarChrome` | **TECH-553** | Done (archived) | Add 4th button to zoning cluster in `UIManager.ToolbarChrome.cs` alongside R/C/I. Icon: placeholder "S" glyph. Click handler sets `ZoneManager.activeZoneType = ZoneType.StateServiceLightZoning` + opens sub-type picker (next task). Toolbar layout respects existing theme spacing. |
| T7.2 | Placement-mode routing through `ZoneSService` | **TECH-554** | Done (archived) | When S placement active + user clicks on grid cell, route through `ZoneSService.PlaceStateServiceZone(cell, currentSubTypeId)` instead of direct `ZoneManager.PlaceZone`. `currentSubTypeId` carried in transient placement state (set by picker). Guard: if `currentSubTypeId < 0`, reopen picker. |
| T7.3 | Sub-type picker modal UI | **TECH-555** | Done (archived) | New `SubTypePickerModal.cs` under `Assets/Scripts/Managers/GameManagers/UI/` (or existing UI dir). Uses `UIManager.PopupStack` to present 7 buttons (icon + displayName + baseCost) sourced from `ZoneSubTypeRegistry`. Click commits `currentSubTypeId` + closes modal + signals placement mode ready. |
| T7.4 | Picker cancel UX (N3) | **TECH-556** | Done (archived) | ESC key or outside-click dismisses picker with no cost + no placement + `currentSubTypeId = -1` + exits placement mode. Documented in `SubTypePickerModal` XML docs referencing Review Note N3. |
| T7.5 | Budget panel UI with sliders | **TECH-557** | Done (archived) | New `BudgetPanel.cs` + Unity UI prefab. 7 horizontal sliders (one per sub-type, labeled from `ZoneSubTypeRegistry`), 1 global cap slider, 7 remaining-this-month readouts. Open via HUD button (add to `UIManager.Hud`). Slider commit calls `budgetAllocation.SetEnvelopePct(i, pct)` which auto-normalizes; UI re-reads values post-normalize so sliders reflect actual stored state. |
| T7.6 | Overspend-blocked notification wiring | **TECH-558** | Done (archived) | Hook `GameNotificationManager` event raised by `BudgetAllocationService.TryDraw` failure. Display a transient HUD badge: "{sub-type} envelope exhausted" for 3s. Matches Example 2 user-facing feedback. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-553"
  title: "S zoning button in `UIManager.ToolbarChrome`"
  priority: "medium"
  notes: |
    Fourth toolbar zoning control for **Zone S**; enters placement + opens sub-type picker. Touches `UIManager.ToolbarChrome`, theme spacing, `ZoneManager.activeZoneType`.
  depends_on: []
  related: ["TECH-554", "TECH-555", "TECH-556", "TECH-557", "TECH-558"]
  stub_body:
    summary: |
      Add **StateServiceLightZoning** (S) button to zoning cluster in `UIManager.ToolbarChrome`; click sets active zone type and opens sub-type picker path for next task.
    goals: |
      - Insert 4th button beside R/C/I with placeholder S glyph and existing theme spacing.
      - Click sets `ZoneManager.activeZoneType = ZoneType.StateServiceLightZoning` and triggers picker flow (wired when T7.3 lands).
      - No regression to R/C/I toolbar behavior.
    systems_map: |
      - `Assets/Scripts/Managers/GameManagers/UI/UIManager.ToolbarChrome.cs` (or path from repo)
      - `ZoneManager`, `ZoneType`, `UiTheme` / toolbar layout
    impl_plan_sketch: |
      Phase 1 — Locate zoning cluster + duplicate pattern for S. Phase 2 — Wire click → zone type + picker hook.

- reserved_id: "TECH-554"
  title: "Placement-mode routing through `ZoneSService`"
  priority: "medium"
  notes: |
    S placement clicks must use `ZoneSService.PlaceStateServiceZone(cell, subTypeId)` not raw `PlaceZone`. Transient `currentSubTypeId` from picker; guard reopen if -1.
  depends_on: []
  related: ["TECH-553", "TECH-555", "TECH-556", "TECH-557", "TECH-558"]
  stub_body:
    summary: |
      Route grid clicks during S placement through **ZoneSService** with committed sub-type id; invalid id reopens picker.
    goals: |
      - Hold `currentSubTypeId` in placement state (setter from picker).
      - On cell click: if id < 0 reopen picker; else call `PlaceStateServiceZone`.
      - Keep R/C/I placement paths unchanged.
    systems_map: |
      - `ZoneSService`, `ZoneManager`, placement-mode controller / input path
    impl_plan_sketch: |
      Phase 1 — Trace current zone placement click handler. Phase 2 — Branch for StateService zone type + service call.

- reserved_id: "TECH-555"
  title: "Sub-type picker modal UI"
  priority: "medium"
  notes: |
    New **SubTypePickerModal** on `UIManager.PopupStack`; 7 buttons from `ZoneSubTypeRegistry`; commit id + close + resume placement.
  depends_on: []
  related: ["TECH-553", "TECH-554", "TECH-556", "TECH-557", "TECH-558"]
  stub_body:
    summary: |
      Modal lists seven **Zone S** sub-types with icon, name, base cost; selection commits `currentSubTypeId` and closes.
    goals: |
      - Implement `SubTypePickerModal` under GameManagers UI folder.
      - Populate buttons via registry; click sets id and dismisses.
      - Integrate with toolbar S button entry and placement state.
    systems_map: |
      - `ZoneSubTypeRegistry`, `UIManager.PopupStack`, new `SubTypePickerModal.cs`
    impl_plan_sketch: |
      Phase 1 — Modal shell + stack push. Phase 2 — Bind registry + selection callback.

- reserved_id: "TECH-556"
  title: "Picker cancel UX (N3)"
  priority: "medium"
  notes: |
    ESC + outside-click dismiss picker without spend; `currentSubTypeId = -1`; exit placement per Review Note N3.
  depends_on: []
  related: ["TECH-553", "TECH-554", "TECH-555", "TECH-557", "TECH-558"]
  stub_body:
    summary: |
      Cancel paths clear sub-type selection and placement without charging player (exploration N3).
    goals: |
      - ESC closes modal and resets id + exits S placement mode.
      - Outside-click same behavior.
      - XML docs reference N3 on `SubTypePickerModal`.
    systems_map: |
      - `SubTypePickerModal`, input / graphic raycast for backdrop
    impl_plan_sketch: |
      Phase 1 — ESC + backdrop handlers. Phase 2 — Reset placement state via `ZoneManager` / coordinator.

- reserved_id: "TECH-557"
  title: "Budget panel UI with sliders"
  priority: "medium"
  notes: |
    **BudgetPanel** + prefab: 7 envelope sliders, global cap, remaining readouts; HUD open; commits via `budgetAllocation.SetEnvelopePct`; UI refresh post-normalize.
  depends_on: []
  related: ["TECH-553", "TECH-554", "TECH-555", "TECH-556", "TECH-558"]
  stub_body:
    summary: |
      HUD-driven panel edits **envelope** percentages with sum-lock and global cap; reflects allocator state after normalize.
    goals: |
      - Add `BudgetPanel.cs` + prefab; wire open from `UIManager.Hud`.
      - Seven sliders + cap + monthly remaining labels from registry / allocator.
      - On commit call `SetEnvelopePct`; re-read model to sync sliders.
    systems_map: |
      - `BudgetAllocationService` / `IBudgetAllocator`, `ZoneSubTypeRegistry`, `UIManager.Hud`
    impl_plan_sketch: |
      Phase 1 — Layout + bind sliders. Phase 2 — Allocator round-trip + live readouts.

- reserved_id: "TECH-558"
  title: "Overspend-blocked notification wiring"
  priority: "medium"
  notes: |
    Surface `TryDraw` failure via **GameNotificationManager**; transient HUD badge ~3s; aligns Example 2 feedback.
  depends_on: []
  related: ["TECH-553", "TECH-554", "TECH-555", "TECH-556", "TECH-557"]
  stub_body:
    summary: |
      When envelope draw fails, show short HUD message naming exhausted sub-type (Example 2).
    goals: |
      - Subscribe or hook notification raised from `BudgetAllocationService.TryDraw` false path.
      - Display badge text `"{displayName} envelope exhausted"` ~3s.
      - Match existing notification / HUD styling.
    systems_map: |
      - `GameNotificationManager`, `BudgetAllocationService`, HUD strip
    impl_plan_sketch: |
      Phase 1 — Event or callback from TryDraw failure. Phase 2 — HUD presenter + timer.
```

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.
