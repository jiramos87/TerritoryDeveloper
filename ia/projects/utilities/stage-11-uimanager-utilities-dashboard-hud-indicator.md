### Stage 11 — Deficit response + UI dashboard / UIManager utilities dashboard + HUD indicator

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Per-scale dashboard panel in `UIManager.Utilities.cs` — 3×3 grid (scale × kind) w/ net / EMA / status colour. HUD indicator top-bar glyph for any Deficit. Info panel on infrastructure building shows tier + production rate.

**Exit:**

- `UIManager.Utilities.cs` renders dashboard panel invoked from existing utilities toolbar entry; rows = Water / Power / Sewage, columns = City / Region / Country. Each cell shows `net` (signed float), `ema` (signed float), status dot color.
- `UIManager.Hud.cs` top-bar glyph flips colour Healthy→Warning→Deficit per worst pool status across all scales.
- Info panel (existing per-building path) reads `InfrastructureContributor` fields + renders tier label + production rate.
- PlayMode smoke test: debug command forces Deficit, dashboard + HUD reflect within one tick.
- Phase 1 — Dashboard panel render.
- Phase 2 — HUD indicator + info panel.
- Phase 3 — PlayMode smoke.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | Utilities dashboard panel layout | _pending_ | _pending_ | Edit `UIManager.Utilities.cs` — add `BuildUtilitiesDashboard()` creating UGUI grid (rows Water/Power/Sewage × cols City/Region/Country). Placeholder cell labels, no live data yet. |
| T11.2 | Dashboard live bindings | _pending_ | _pending_ | Wire dashboard cells to `UtilityPoolService[scale].pools[kind].{net, ema, status}`; refresh on `PoolStatusChanged` event + per-game-day. Cache service refs in `Awake` (invariant #3). |
| T11.3 | HUD deficit glyph | _pending_ | _pending_ | Edit `UIManager.Hud.cs` — add utility-status glyph; colour = worst status across all (scale, kind). Subscribe to `PoolStatusChanged`. |
| T11.4 | Info panel contributor readout | _pending_ | _pending_ | Edit existing building info-panel renderer — when target is `InfrastructureContributor`, show `def.kind`, `currentTier`, `ProductionRate`. |
| T11.5 | PlayMode deficit smoke | _pending_ | _pending_ | Add `Assets/Tests/PlayMode/Utilities/UtilityPlayModeSmoke.cs` — debug-command forces pool to Deficit; assert dashboard cell flips red, HUD glyph red, `ExpansionFrozen == true`. Recover → green. |
