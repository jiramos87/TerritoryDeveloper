### Stage 11 — Super-utility bridge + UI surface + spec closeout / UI surface (progress panel + commission dialog)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship minimum-viable UI — progress panel listing state + commission dialog. No tooltip / onboarding polish (Bucket 6 owns). **Hard dep:** Bucket 6 `UiTheme` must land (Tier B' exit).

**Exit:**

- `LandmarkProgressPanel.cs` MonoBehaviour — constructs UGUI list, rows categorized (Unlocked-available / In-progress / Locked). Row shows `displayName`, cost, build months, state badge. Click-to-open commission dialog for available rows.
- Refresh triggers: `LandmarkProgressionService.LandmarkUnlocked`, `BigProjectService.LandmarkBuildCompleted`, per-game-month (progress bar for in-progress rows).
- `CommissionDialog.cs` — modal confirms cost + build months + target cell (default = player-selected cell via existing placement-mode UI OR scale-capital fallback); on confirm invokes `BigProjectService.TryCommission`. Renders result enum.
- Toolbar entry — new "Landmarks" button in existing UI toolbar opens progress panel. Reuse `UIManager.Toolbar.cs` pattern.
- `Awake` caches all service refs per invariant #3 (no per-frame `FindObjectOfType`).
- PlayMode smoke — open progress panel, confirm commission, advance months via debug hook, assert landmark placed + panel reflects state.
- Phase 1 — `LandmarkProgressPanel` layout + list rendering.
- Phase 2 — `CommissionDialog` modal + confirm flow.
- Phase 3 — Toolbar entry + live-binding refresh + PlayMode smoke.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | LandmarkProgressPanel layout | _pending_ | _pending_ | Add `Assets/Scripts/UI/LandmarkProgressPanel.cs` MonoBehaviour. `Awake` caches LandmarkCatalogStore, LandmarkProgressionService, BigProjectService refs (invariant #3). Build UGUI vertical list w/ three sections (Available / In progress / Locked). |
| T11.2 | Row rendering + state badge | _pending_ | _pending_ | Row prefab shows `displayName`, commission cost, build months, state badge (colour per section). In-progress rows show progress bar (`monthsElapsed / buildMonths`). Uses existing `UiTheme` palette (Bucket 6 dep). |
| T11.3 | CommissionDialog modal | _pending_ | _pending_ | Add `Assets/Scripts/UI/CommissionDialog.cs` MonoBehaviour — modal w/ cost + months + target-cell readout + Confirm/Cancel. Confirm invokes `BigProjectService.TryCommission(id, cell, scale)`; renders `CommissionResult` outcome (toast or inline status). |
| T11.4 | Target-cell selection | _pending_ | _pending_ | Commission dialog — integrates with existing placement-mode cell-pick flow OR falls back to scale-capital cell. Add `ScaleTierController.GetCapitalCell(tier)` helper if missing (see Stage 3.2 T3.2.6). |
| T11.5 | Toolbar entry | _pending_ | _pending_ | Edit `UIManager.Toolbar.cs` — add "Landmarks" button opening `LandmarkProgressPanel`. Icon = placeholder (Bucket 5 coordination). |
| T11.6 | Live-binding refresh | _pending_ | _pending_ | `LandmarkProgressPanel` subscribes to `LandmarkProgressionService.LandmarkUnlocked` + `BigProjectService.LandmarkBuildCompleted` + `TimeManager.OnGameMonth` (for progress bar). Unsubscribes in `OnDisable`. |
| T11.7 | PlayMode commission smoke | _pending_ | _pending_ | Add `Assets/Tests/PlayMode/Landmarks/LandmarkCommissionSmoke.cs` — open panel, confirm commission on `big_power_plant`, advance 18 game-months via debug hook, assert landmark placed at cell, panel row moved from In-progress to Unlocked section. |
