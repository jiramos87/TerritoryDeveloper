# Utilities — Exploration (stub)

> Pre-plan exploration stub for **Bucket 4-a** of the polished-ambitious MVP (per `docs/full-game-mvp-exploration.md` + `ia/projects/full-game-mvp-master-plan.md`). **Split** off from the original merged `utilities-landmarks-exploration.md` stub — sibling is `docs/landmarks-exploration.md`. Seeds a `/design-explore` pass that expands Approaches + Architecture + Subsystem impact + Implementation points. **Scope = utilities v1 (water / power / sewage as country-level resources + local contributors). NOT landmarks (sibling doc), NOT Zone S + per-service budgets (Bucket 3), NOT city-sim signals (Bucket 2), NOT CityStats (Bucket 8), NOT multi-scale core (Bucket 1). Those land in sibling buckets / docs.**

---

## Problem

Territory Developer has no utility sim. A polished ambitious MVP needs one:

- **Utilities absent.** No water / power / sewage production, distribution, or consumption. "Utility building" placeholders exist (BUG-20 restores visuals on load) but they contribute to nothing. A city can grow indefinitely without power or water — genre break.
- **No scale-level resource surface.** Natural wealth (forest, water body, mineral placeholder) has no economic dimension beyond pollution sink. No "country owns the watershed" pool that regions + cities draw from.
- **Deferred utility depth risks re-opening scope.** If utilities ship as local-only buildings, the cross-scale utility pool story (country wealth → region allocation → city consumption) has no home and gets filed as Bucket 4.5 later.

**Design goal (high-level):** utilities v1 = water / power / sewage framed as country-level resource pools derived from natural wealth + savings / assets, with local utility buildings (per-city / per-region) as contributors to those pools.

## Approaches surveyed

_(To be expanded by `/design-explore` — seed list only.)_

- **Approach A — Local-only utilities.** Utility buildings produce / consume per-cell or per-city only, no cross-scale pool. Minimal churn; fails the "country-level resource" framing.
- **Approach B — Country-pool first, local contributors feed it.** Define `UtilityPool` per scale per utility (3 utilities × 3 scales = 9 pool instances with rollup). Local contributors (power plants, water treatment, sewage treatment) register as producers. Cities consume against city-pool; city-pool draws from region-pool; region-pool draws from country-pool. Natural wealth seeds country-pool.
- **Approach C — Signal-integrated utilities.** Utilities become another signal in Bucket 2's signal contract (`UtilityWater`, `UtilityPower`, `UtilitySewage`). Reuses diffusion + consumer formula. Loses the country-pool "resource" framing (signals are per-cell, pools are per-scale aggregates).
- **Approach D — Defer utilities entirely.** File as Bucket 4.5 post-MVP. Trims scope at cost of the "genre table stakes" check.

## Recommendation

_TBD — `/design-explore` Phase 2 gate decides._ Author's prior lean: **Approach B** (country-pool utilities with local contributors). Matches bucket framing, keeps Bucket 2 signal surface free of cross-scale aggregates. Approach C collapses the cross-scale resource story into per-cell signals. Approach D defers and re-opens scope later.

## Locked decisions (prior interview)

- **Pool accounting = instantaneous flow-rate + soft warning.** Each tick: `net = production − consumption`. No stored capacity, no ring-buffer history in v1. A rolling average (EMA, ~5 ticks) of net balance drives a "running low" warning color before hard deficit fires. Deficit severity scales with EMA depth (not cliff-edge). Save schema: per-pool floats only, no storage state.
- **Explicit rejections:** no energy-storage buildings, no reserve-capacity mechanics, no ring-buffer history in v1 (plot the EMA directly if a sparkline is needed later).

## Open questions

- **Natural wealth surface.** Forest = renewable wood / carbon / air quality; water body = water supply; mineral placeholder = ore for industry. Which natural-wealth cells feed which utility pool? Authority: glossary rows + new `utility-system.md` spec vs extension of existing specs?
- **Contributor building archetypes.** Power plants (coal / solar / wind — sub-types?), water treatment, sewage treatment. Each as Zone S building (cost to budget) or separate utility-building category? Coordinate with Bucket 3 S classification.
- **Per-scale rollup rule.** Sum across cities → region pool; sum across regions → country pool? Loss on transfer (grid losses)? Country wealth decay (renewable regrowth vs extraction depletion)?
- **Deficit behaviour (concrete).** Once warning escalates: rolling blackouts (random cells lose power)? Happiness penalty? Desirability hit? Construction halts?
- **UI surface.** Utility pool dashboard (per scale). Which elements ship MVP? Coordinate with Bucket 6 UI polish.
- **Save schema impact.** Utility pool state per scale, contributor registrations. `schemaVersion` bump — coordinate with Bucket 3's bump.
- **Consumer-count inventory.** Which surfaces read utility pool state (HUD, info panels, CityStats Bucket 8, web dashboard)? Decide at exploration time for Bucket 8 parity contract.
- **Invariant compliance.** No new singletons. `UtilityPoolService` as MonoBehaviour + Inspector-wired. `GridManager` extraction carve-out if any (invariant #6).
- **BUG-20 interaction.** Existing utility-building visuals bug — does contributor registration inherently fix it, or is BUG-20 a separate save-restore bug that fires before utility pools land?
- **Hard deferrals re-check.** Climate / geographic base utility variation (rain → water pool modifier), renewable vs fossil granular mechanics, private utility operators — confirmed OUT at bucket level.
- **Interface with landmarks.** Some "super-utility" landmarks (e.g. 10× power plant) register as utility contributors via a scaling factor. Narrow catalog interface: landmark catalog row points at contributor registry entry; when placed, contributor registers as normal with a multiplier. Contract owned by sibling `docs/landmarks-exploration.md`; this doc owns the contributor registry contract it plugs into.

---

_Next step._ Run `/design-explore docs/utilities-exploration.md` to expand Approaches → selected approach → Architecture → Subsystem impact → Implementation points → subagent review. Then `/master-plan-new` to author `ia/projects/utilities-master-plan.md`.
