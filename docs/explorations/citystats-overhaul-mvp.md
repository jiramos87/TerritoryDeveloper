---
slug: citystats-overhaul-mvp
status: seed-stub
supersedes_master_plan: citystats-overhaul (closed 2026-05-16 — pre-arch drift, unstarted)
parent_exploration: docs/mvp-scope.md §3.29 (D12 + D24 lock)
related_master_plans:
  - region-scene-prototype (closed — provides RegionScene shell for region-scale stats)
  - city-region-zoom-transition (open — CoreScene shell hosts stats-panel)
  - game-ui-design-system (closed — stats-panel chrome + tab pattern)
  - game-ui-catalog-bake (closed — stats-panel catalog rows)
  - full-game-mvp (umbrella)
related_specs:
  - docs/mvp-scope.md §3.29 In-game CityStats dashboard (D12 lock)
  - docs/mvp-scope.md §3.27 Graphs panel (D13 lock — 4 curves)
  - docs/mvp-scope.md §3.27a Demographics tab (D34 lock — full depth)
  - docs/mvp-scope.md §3.32 HUD strip (D29 + D36 — stats toggle button)
arch_decisions_inherited:
  - DEC-A22 (prototype-first-methodology)
  - DEC-A23 (tdd-red-green-methodology)
  - DEC-A24 (game-ui-catalog-bake — stats-panel rows via catalog)
  - DEC-A28 (ui-renderer-strangler-uitoolkit — stats-panel migration path)
  - DEC-A29 (iso-scene-core-shared-foundation — region-scale stats reuse)
  - DEC-A30 (corescene-persistent-shell — stats-panel in CoreScene)
arch_surfaces_touched:
  - data-flows/persistence (stat snapshot persistence per save tick)
  - layers/system-layers (Territory.CityStats refactor — reads sim signals)
---

# CityStats Overhaul — Exploration Seed Stub (MVP)

**Status:** Seed stub. `/design-explore` to expand.

**Replaces:** Closed master plan `citystats-overhaul` (10 stages, never started, drifted on 36 arch decisions).

---

## Problem Statement

Current `CityStats` MonoBehaviour (`Assets/Scripts/Managers/CityStats.cs`) computes city-level aggregates (population, happiness, pollution, budget summary) for the HUD readouts + day/month tick callbacks. The overhaul splits in three directions:

1. **D24 lock — CityStats moves into shared `stats-panel`** (one tabbed panel: Graphs + Demographics + CityStats). HUD readouts shrink to budget-only always-on; CityStats becomes a tab.
2. **D29 + D36 — HUD strip 19 → 9 cells.** Discrete CityStats readouts (population / happiness / pollution / etc.) removed from HUD; relocated to stats-panel CityStats tab.
3. **D34 — Demographics tab ships at full depth** (3 charts + ~6 readouts: population breakdown, age pyramid, education levels, income tiers, commute time, housing distribution). Updated monthly.

Closed plan pre-dated D24 (3-tab stats-panel collapse), D29 (HUD shrink), D34 (demographics depth), DEC-A29 (region-scale stats reuse), DEC-A30 (CoreScene-owned panel).

---

## Open Questions (resolve at /design-explore Phase 1)

### Architecture

1. **CityStats vs RegionStats split.** DEC-A29 says shared iso-scene-core; D34 demographics tab implies City-scope only. Does RegionStats reuse the same tab chrome with region-aggregated data, or is it a separate tab/panel?
2. **Data flow into stats-panel.** Push (CityStats publishes events; panel subscribes) or pull (panel queries CityStats on tab open + on monthly tick)?
3. **Monthly snapshot vs live read.** Demographics updated monthly (D33 budget close cadence). Are values snapshotted to a `stat_snapshot` struct at month-close, or recomputed on every tab open from current state?
4. **Where does CityStats live now?** Current code is a 2000+-line MonoBehaviour. Atomize per Strategy γ (Domains/CityStats/Services) or keep as hub + extract readonly query surface for stats-panel?

### Data model

5. **Stat definitions.** Inventory of all stats the panel needs (population/employed/unemployed/school-age/age-pyramid-3-bin/edu-4-bin/income-3-bin/commute-time/housing-3-bin/+ live happiness/pollution/budget). Where do they sourced from? (DemandManager, EconomyManager, ZoneManager, etc.)
6. **Graph data retention.** 4 curves (pop/happiness/pollution/budget) over last-week / last-month / all-time (D13). How much historical data persists? Daily samples in a ring buffer? Monthly aggregates only?

### Persistence

7. **Save schema delta.** Stat snapshots persist? Or recomputed on load from current state + replay? All-time graph data needs full history — does that persist in save? Save size budget concern.

### UI surface

8. **Tab layout per stats-panel.** 3 tabs (Graphs / Demographics / CityStats). Tab order? Default tab on open? Tab persistence across opens (resume on last-active tab)?
9. **Chart primitives.** D34 calls for 3 chart primitives (histogram, age-pyramid, bar-chart). New custom renderers via DEC-A28 (UI Toolkit strangler) or existing uGUI primitives?
10. **Stats-panel chrome integration.** Stats-panel already designed for game-ui-design-system (closed). Does CityStats tab inherit existing chrome or trigger a chrome update?

### CityScene vs RegionScene

11. **Region stats tab.** RegionScene needs region-aggregated stats (population sum across cities, regional happiness avg, etc.). Same stats-panel chrome? Switch via scale-switcher widget?

---

## Scope NOT in this seed

- **Web-dashboard parity** (D12 lock OUT for MVP).
- **Custom user-configurable stat dashboards** (no per-player customization).
- **Telemetry export** (auto-telemetry OUT per §5).
- **Historical stat replay** (no scrubbing back through time).

## Pre-conditions for `/design-explore`

- `game-ui-design-system` closed (yes) — stats-panel chrome exists.
- `game-ui-catalog-bake` closed (yes) — catalog rows for stats-panel + tabs.
- HUD shrink (19 → 9 cells) plan landed or in-flight — coordinates with this seed on which CityStats readouts leave HUD.

## Next step

`/design-explore docs/explorations/citystats-overhaul-mvp.md`
