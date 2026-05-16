---
slug: zone-s-economy-mvp
status: seed-stub
supersedes_master_plan: zone-s-economy (closed 2026-05-16 — pre-arch drift, unstarted)
parent_exploration: docs/mvp-scope.md §3.5 (D9 lock — 7 S-zone subtypes)
related_master_plans:
  - region-scene-prototype (closed)
  - city-region-zoom-transition (open)
  - utilities-pool-mvp (seed — services consume utilities + S-zone consumer roll-up)
  - full-game-mvp (umbrella)
related_specs:
  - docs/mvp-scope.md §3.5 City services (D9 lock — 7 S-zone subtypes)
  - docs/mvp-scope.md §3.15 Budget + finance (D24 — per-service allocation, S maintenance)
  - docs/mvp-scope.md §3.31 Toolbar (D23 + D32 — Service tool, 7 cards)
  - docs/mvp-scope.md §3.19 Notifications (D34 — service-driven warnings)
arch_decisions_inherited:
  - DEC-A22 (prototype-first-methodology)
  - DEC-A23 (tdd-red-green-methodology)
  - DEC-A29 (iso-scene-core-shared-foundation)
  - DEC-A30 (corescene-persistent-shell — service overlay toggles in minimap UI in CoreScene)
  - DEC-A24 (game-ui-catalog-bake — S-zone subtype-picker cards via catalog)
  - DEC-A18 (db-lifecycle-extensions — service coverage state persistence)
arch_surfaces_touched:
  - data-flows/persistence (per-cell service coverage state in save)
  - data-flows/initialization (service manager wire order)
  - layers/system-layers (Territory.Services new layer + Territory.Economy budget integration)
---

# S-Zone Economy + 7 Services — Exploration Seed Stub (MVP)

**Status:** Seed stub. `/design-explore` to expand.

**Replaces:** Closed master plan `zone-s-economy` (9 stages, never started, drifted).

---

## Problem Statement

D9 (2026-05-07) locked S-zone to **7 sub-types** split into two behavioral classes:

- **5 coverage services** (police, fire, education, healthcare, parks) — each ships an overlay; coverage radius drives effect per cell.
- **2 capacity services** (public-housing, public-offices) — no overlay; effect is global/budget-side, not per-cell. Surface in budget + demographics panels.

D24 (2026-05-07) locked **per-service budget allocation** into the shared `budget-panel`. D34 (2026-05-07) added service-driven warning toasts (debounced 30 in-game days per service when coverage drops below 40%). D32 (2026-05-07) collapsed Service into a single toolbar tool with 7 picker cards (no density tier — kind subtypes per D9).

Current code has partial S-zone infrastructure (`ZoneManager`, `BuildingFactory`) but no per-service registry, no overlay rendering, no budget allocation surface. Closed plan pre-dated D9 (7 subtypes vs older 5), D24 (budget integration shape), D34 (warning debounce), D32 (toolbar+picker contract), DEC-A30 (CoreScene-owned overlays).

---

## Open Questions (resolve at /design-explore Phase 1)

### Service mechanics

1. **Coverage radius per service.** Police/fire/edu/health/parks each have a coverage radius. Same radius for all 5, or differentiated per service (e.g. fire = larger radius than parks)?
2. **Coverage decay shape.** Linear falloff from building center, or step function within radius? Stacked coverage from multiple buildings (additive cap or max-only)?
3. **Capacity service effects.** Public-housing = low-income R alternative — how does it interact with private R demand? Public-offices = budget knob unlock + tax tolerance — what's "tax tolerance" formula?
4. **Service quality vs coverage.** Just coverage % or also a quality dimension (staffed/funded/etc.)? D24 per-service budget allocation implies quality dial — how does underfunding manifest?

### Architecture

5. **Per-service manager vs single ServicesManager.** Each service its own MonoBehaviour (PoliceManager / FireManager / etc.) or single hub? Atomization Strategy γ default = single hub + Services/services/{name}.
6. **Coverage map data structure.** Per-cell per-service float (5 floats per cell × 64×64 cells = ~20k floats) or sparse per-service grid?
7. **Tick order.** Coverage recompute = every tick, every day, every month, or event-driven (on placement / demolish / staff change)?
8. **Service vs utility consumer split.** Services consume utilities (police HQ needs power + water + sewage). Wire as `IUtilityConsumer` per utilities-pool-mvp seed? Service buildings register with `UtilityContributorRegistry` as consumers.

### UI surface

9. **Subtype-picker for Service tool.** 7 cards (D32). Card layout — single column, 2-column grid, scroll list? Capacity services (public-housing, public-offices) visually distinguished from coverage services?
10. **Overlay rendering.** 5 service overlays on minimap (D26 — overlays embedded inside minimap panel, not on gridmap per D6). Color gradient per service? Stacking model when multiple overlays toggled (additive blend, last-on-top, layered tint)?
11. **Service info panel.** Click on S-zone building → info-panel (D20 single adaptive panel) shows: sub-type / coverage radius / staffed status / maintenance cost. What does "staffed status" mean — boolean or %?
12. **Budget panel per-service allocation.** D24 says "per-service allocation". 7 sliders (one per service) or grouped (5 coverage + 2 capacity)? Slider range = budget % or absolute $ allocation?

### Persistence

13. **Save schema delta.** Per-cell coverage map (sparse) + per-service budget allocation + service-building registry. Slot into v3 envelope or own sub-object?

### Economy integration

14. **Maintenance cost flow.** Each S-zone building has monthly maintenance cost. Flows through `EconomyManager.ProcessDailyEconomy` (monthly close per D33). Underfunding (budget can't cover all S maintenance) → coverage degrades? Service closes? Notification fires?
15. **Tax revenue tie-in.** Public-offices raises "tax tolerance" — what does that mean numerically? Higher tax rate before R/C/I demand crashes?

---

## Scope NOT in this seed

- **Per-staff / per-officer NPC sim** (no per-NPC pop sim — §5 hard exclusion).
- **Service-quality random events** (no random events — D34).
- **Workforce skill tracking outside education service** (D34 demographics has 4-bin education chart only).
- **Per-service policy knobs beyond budget allocation** (no "police-strategy" picker, no per-service hiring/firing).
- **Cross-service synergies** (e.g. "police near education raises both") — flat per-service effects.

## Pre-conditions for `/design-explore`

- `utilities-pool-mvp` seed expanded → S-zone buildings register as utility consumers via shared contract.
- `game-ui-catalog-bake` closed (yes) — catalog rows for S-zone subtype-picker cards.
- `game-ui-design-system` closed (yes) — info-panel + budget-panel chrome exists.

## Next step

`/design-explore docs/explorations/zone-s-economy-mvp.md`
