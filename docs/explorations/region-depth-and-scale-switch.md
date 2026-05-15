---
slug: region-depth-and-scale-switch
status: seed
parent_exploration: region-scene-prototype
depends_on_prototype_close: region-scene-prototype Stage 5.0 (Sim-lite exit gate)
related_master_plans:
  - multi-scale (closed; rescue source)
  - region-scene-prototype (active; this exploration extends it)
companion_explorations:
  - docs/explorations/assets/city-scene-loading-research.md
arch_decisions_inherited:
  - DEC-A29 (iso-scene-core-shared-foundation)
  - DEC-A28 (ui-renderer-strangler-uitoolkit)
  - DEC-A22 (prototype-first-methodology)
  - DEC-A23 (tdd-red-green-methodology)
---

# RegionScene Depth + Scale Switch — Exploration Seed

**Status:** Seed (problem statement + open questions only). Do NOT run `/design-explore` until `region-scene-prototype` Stage 5.0 closes green.
**Gate:** After region-scene-prototype Sim-lite exit gate (Stage 5.0 done).
**Rescue source:** `multi-scale-master-plan` Stages 8.0 (tile catalog), 9.0 (Compact/Expand snapshot), 11.0 (flow channels + treasury), 12.0 (scale switch), 13.0 (build strokes), 14.0 migration question, 15.0 verify gate. Old plan based on pre-DEC-A29 architecture (parent-id-on-city-save + cell-type subclass + single-scene multi-camera + uGUI/UiTheme); restate ideas under IsoSceneCore + UI Toolkit + per-scene FS save model.

---

## Problem Statement

The `region-scene-prototype` ships a Sim-lite RegionScene: 64×64 heightful terrain, click panels, pop + urban-area evolution per global tick, basic FS save, unlock gate from CityScene. That validates the iso-core foundation but leaves the region a thin map view. To make the region a real second-tier scene the player will keep playing, four depth axes still need design + plumbing:

1. **Region economy depth.** The prototype evolves only `population` + `urban_area` per cell. The actual multi-scale fiction needs cross-cell flow signals (goods, money, policy, traffic, pollution, tourism, geography) feeding a region-scale treasury that the player can steer via policy sliders. Without flows + treasury, the region has no consequence.

2. **Scale switch UX.** The prototype accesses RegionScene only from the main menu, no transition mechanics — that companion problem lives in `city-scene-loading-research.md`. The product vision is mouse-wheel zoom from CityScene → RegionScene (and back) with a dissolve VFX placeholder + dormant-city math running between scenes so the player sees their dormant cities visibly evolve on re-entry. Without scale switch, the two scenes feel like separate apps.

3. **Region build mode.** Founding a new city is the prototype's Stage 5.0 tool, but the region also needs infrastructure strokes — highways, rail, canals, bridges drawn cell-to-cell — to make the region read as a developed map rather than an empty hexagonal canvas. Build mode also needs viability rules (terrain + adjacency + treasury cost) and a hover preview.

4. **Dormant-city fidelity contract.** When the player leaves CityScene the city must continue evolving in the region's idle frame. Old multi-scale Stage 9.0 sketched a `Compact / Expand` snapshot round-trip + `ICityEvolver` interface with locked fidelity tolerances (≤2% population drift, ≤5% treasury drift, ≤10% pollution drift over a 30-day dormant evolve). That math contract is the heart of the multi-scale illusion — re-entering a city the player left an hour ago should feel like it kept living without exposing the snapshotting seam.

These four axes were all in the deprecated `multi-scale` plan but framed against architecture that DEC-A29 (shared IsoSceneCore) + DEC-A28 (UI Toolkit strangler) replaces. This exploration re-frames them on top of the prototype's foundation: shared iso-core for camera + culling + tick + UI shell, per-scene FS save linkage, hand-authored UI Toolkit panels, plugin/registration UI slots.

---

## Known Design Decisions (locked, do not re-grill)

- **DEC-A29** — Shared `IsoSceneCore` foundation. Camera + chunk culling + tick + UI shell + tooling infra (toolbar + subtype picker + zone-paint host + modal host + toast surface) are shared between CityScene + RegionScene. Per-scene plugins register into core slots. New scale tiers (country, world) extend by incremental generification.
- **Hub-preservation rule (DEC-A29 addendum).** Unity Inspector-wired hub scripts (`GameManager`, `GridManager`, `GeographyInitService`, `UIManager`, `RegionManager`, future hubs) NEVER renamed/moved/deleted. New logic extracted into `Assets/Scripts/Domains/IsoSceneCore/Services/*` and per-scene service folders; hubs become facades delegating to services.
- **Save model.** Region save = new FS file linking region ↔ N cities. Player loads any city → traverses to region independent of which city loaded (city must belong to region). Save model extends naturally to country tier in the future.
- **Global game tick.** One tick driver shared CityScene + RegionScene. Cities emit on global tick; region evolves on the same tick via its own tick-derived behavior; dormant cities evolve via region's tick handler when their scene isn't loaded.
- **UI Toolkit only.** All new UI hand-authored UIDocument + UXML + USS + C# controller per parity-recovery patterns (DEC-A28). DB-driven UI baking deferred indefinitely.
- **Region cell representation locked.** 1 region cell = 32×32 city cells (locked in `region-scene-prototype` Phase 0.5). Sets the math contract for Compact/Expand snapshots — a city covers 1 region cell footprint.
- **Region grid size.** 64×64 fixed for prototype; same fixed size carries forward to depth + scale switch (revisit for country tier exploration).

---

## Open Questions (to be grilled by `/design-explore`)

The single biggest fork is which axis to scope into the first depth pass. The four axes can ship independently or bundled. Below: per-axis questions, each numbered. `/design-explore` Phase 0.5 should grill these one by one until concrete commitment.

### Axis 1 — Region economy (flow channels + treasury + policy)

1. **Channel inventory.** Multi-scale Stage 11.0 listed 7 channels: goods, money, policy, traffic, pollution, tourism, geography. Which subset survives under the new architecture? Is the channel set the same, or does the IsoSceneCore tick-bus + per-cell evolution suggest a different decomposition (e.g. per-cell flow vectors vs region-scoped aggregate channels)?
2. **Treasury ledger split.** Old plan had a 4-channel treasury (cash, debt, policy debt, ???). What are the 4 channels in the new model? Or does the region treasury reduce to 1-2 channels with policy bound separately?
3. **Policy sliders.** What policies, mapped to what channels, with what tick-cadence (instant, per-tick, per-day)?
4. **City ↔ region flow plumbing.** When a city is loaded (player in CityScene), does the city's evolution feed flows back into the region treasury directly, or via a pull-on-tick reconciliation when the player exits the city? Same question for dormant cities.
5. **UI surface.** Where do flows + treasury render? In the IsoSceneCore HUD (always visible at region zoom), in a dedicated dashboard panel (Stage 11.0 old design), in cell inspectors? Pick one + justify.

### Axis 2 — Scale switch UX (CityScene ↔ RegionScene transition)

This axis overlaps `city-scene-loading-research.md` companion exploration. The depth-and-scale-switch exploration should NOT redo the technical research; it should commit to which mechanic ships first.

1. **Trigger mechanic.** Mouse-wheel-past-max-zoom (old Stage 12.0 design)? Dedicated "Region" button in CityScene HUD? Esc menu → "Go to region"? Multiple triggers OK, but pick the primary.
2. **Transition visual.** Dissolve VFX placeholder? Camera zoom-out animation with progressive RegionScene composite (per loading research findings)? Hard cut to loading screen?
3. **Symmetry.** Is the return path RegionScene → CityScene identical to the forward path (zoom-in past min-zoom on region) or a discrete "Enter City" button click? Affects RegionCellInspectorPanel design.
4. **Camera continuity.** Does the camera position carry across scenes (city camera centered on origin city's region cell when returning to region) or reset?
5. **Tick freeze.** During the transition itself (sub-second), does the global tick pause or keep running? If keep running, who handles the half-loaded state?

### Axis 3 — Region build mode (infrastructure strokes + viability + cost)

1. **Stroke type inventory.** Old Stage 13.0 listed highway / rail / canal / bridge. Which subset for first depth pass? Each adds renderer + sim + cost complexity.
2. **Stroke representation.** Cell-to-cell path (Bresenham-style line from drag start to drag end) or click-each-cell? Affects input + preview UX.
3. **Viability rules.** Old Stage 13.0 had 4 rules (terrain compatible + adjacency + treasury cost + ???). What's the 4th + are all 4 still valid?
4. **Cost preview.** Hover during stroke draw shows running cost? Or commit-then-bill?
5. **Player feedback.** Red/green hover overlay (old design) — does that fit the UI Toolkit hover panel pattern, or does it need a separate sprite overlay layer?
6. **Persistence.** Stroke state in region save file as a list of segments? Per-cell flags? Affects RegionSaveService schema growth.

### Axis 4 — Dormant-city fidelity contract (Compact / Expand + ICityEvolver)

1. **Snapshot fields.** Old Stage 9.0 snapshot captured ??? city fields. With the prototype's lazy CityData model, which CityData fields are Compact-able vs which require full state (zone grids, building registry, etc.)?
2. **Evolver interface.** Multi-scale Stage 9.0 defined `ICityEvolver` w/ `Evolve(snapshot, days) → snapshot'`. Does the interface still hold under per-scene FS saves, or does dormant evolve happen directly on disk (write back compacted snapshot)?
3. **Fidelity tolerances.** Old plan locked ≤2% pop drift, ≤5% treasury, ≤10% pollution over 30 dormant days. Are these the right tolerances under the new economy model (Axis 1)? Different channels need different tolerances.
4. **Determinism.** Is dormant evolve deterministic (same input snapshot + same days → same output) or seeded by region tick? Affects save-replay + multiplayer-future.
5. **Visual surface.** When player returns to a dormant city, do they see a single "while you were away" summary screen, ambient growth animation on entry, or no UI at all (silent re-entry, math just reflects in HUD)?
6. **Cost budget.** Old plan locked ≤50 µs per dormant city per tick at N=20 cities. Under the new IsoSceneTickBus + cross-scene tick model, what's the realistic budget? Profile target?

### Axis 5 — Scope cutline

7. **Scope cutline for this exploration.** Pick ONE bundle:
   - **(a) Axis 1 only** — Region economy. Treasure + flow channels + policy. Defer scale switch + build + dormant evolve.
   - **(b) Axis 1 + 4** — Economy + dormant fidelity. Region depth + city continuity, no transition mechanic.
   - **(c) Axes 2 + 4** — Scale switch + dormant fidelity. The headline multi-scale UX (zoom-switch + cities living between visits), no new region depth content.
   - **(d) All four axes** — Full multi-scale depth pass. Maximum scope, slowest to ship.

This is the biggest decision — it shapes the entire plan + the stage count.

8. **Stage count target.** Once cutline locked, target stage count? 4–6 for one axis; 8–12 for bundled; 14+ for all four. Used as a soft cap during Phase 6 implementation roadmap.

---

## Approaches

*To be developed during `/design-explore` session.*

Likely fork shapes (do NOT decide here — `/design-explore` Phase 1):

- **A — Per-axis vertical slice.** Each axis ships as a self-contained stage cluster (3–4 stages per axis); 4-axis plan ≈ 14 stages. Maximum independence, max ship surface.
- **B — Bundled horizontal slice.** Each axis contributes a thin slice per stage (flows + transition + build + dormant all touched per stage). Tight integration, hard to verify.
- **C — Axis-priority cutline.** Pick 1–2 axes (per Open Question 7), ship them fully, defer remaining axes to future explorations.

`/design-explore` will rank these against constraint fit + effort + maintainability + scalability.

---

## Rescue Source — Multi-Scale Plan Tasks

The deprecated `multi-scale` master plan (slug: `multi-scale`, closed) carries these pending tasks that map to axes above. When `/design-explore` runs on this seed, `/ship-plan` can cite/migrate these tasks rather than re-author:

| Old task | Axis | Notes |
|---|---|---|
| `FEAT-51` (Stage 6.0 chart engine + HUD cards bound to TokenCatalog) | NOT in scope here — city-scale dashboard. File standalone or its own city-scale exploration. |  |
| `TECH-1804` (Stage 6.0 seed 5 chart token rows) | NOT in scope — companion to FEAT-51. |  |
| `TECH-1898..1902` (Stage 8.0 — 16 archetype rows + 16 sprite rows + region UI token rows + RegionTile data record + glossary) | Axis 1 (economy renders flows) / Axis 3 (build strokes need new sprites) | Asset-pipeline catalog spine rows for region tiles. Rescue after prototype proves visual surface. May spawn second follow-up `region-tile-asset-pipeline-integration`. |
| Stage 9.0 tasks T9.1–T9.6 (Snapshot struct + Compact/Expand + ICityEvolver + fidelity tests) | Axis 4 | Direct rescue — adapt to per-scene FS save model. |
| Stage 11.0 tasks T11.1–T11.6 (Flow solver + 4-channel treasury + policy store + UI manager + 3 panels + economy hook) | Axis 1 | Direct rescue — UI panels re-author under UI Toolkit hand-authored pattern. |
| Stage 12.0 tasks T12.1–T12.4 (Scale state machine + dissolve in/out + dormant evolve dispatcher + budget tests) | Axis 2 + Axis 4 | Direct rescue — depends on companion exploration `city-scene-loading-research.md` outcome. |
| Stage 13.0 tasks T13.1–T13.5 (Viability result type + viability check + city stub seed + build service strokes + toolbar + preview overlay) | Axis 3 | Direct rescue — overlay re-author under UI Toolkit. |
| Stage 14.0 tasks T14.1–T14.6 (RegionSaveV4 + v3→v4 migration + restore order + catalog reload + bridge wire) | Across all axes | Partial rescue — only the open product question "do existing v3 single-city saves need migration?" survives; rest baked into region-scene-prototype Stage 4.0.2. |
| Stage 15.0 tasks T15.1–T15.4 (EditMode + PlayMode + bridge smoke + glossary cleanup) | All axes | Pattern rescue — file as stage-final verify gate. |

---

## Notes

- This seed is INTENTIONALLY rich on open questions + light on solutions. `/design-explore` does the resolution work.
- Hub-preservation rule from `region-scene-prototype` applies recursively here — any new hub introduced (e.g. `RegionEconomyManager`) becomes Inspector-wired and falls under the same never-rename rule.
- The four axes are likely NOT all equally ready. Axis 2 (scale switch) blocks on `city-scene-loading-research.md` outcomes. Axis 4 (dormant fidelity) blocks on Axis 1 (economy model needs locking before fidelity tolerances make sense). `/design-explore` Phase 0.5 should sort dependency order before locking scope cutline.
- Cross-region commerce (was multi-scale Stage 3.0 neighbor-city stub) is OUT of scope here — that's a country-tier concern, future exploration.
- Track companion exploration progress: when `city-scene-loading-research.md` ships its design expansion + resolves scale-switch transition mechanics, this exploration's Axis 2 questions partially auto-resolve.

---
