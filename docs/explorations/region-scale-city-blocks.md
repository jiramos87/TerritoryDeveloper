---
slug: region-scale-city-blocks
status: seed
parent_exploration: city-region-zoom-transition
depends_on_prototype_close: city-region-zoom-transition shipped (parent seed; transition must define integration event)
related_master_plans:
  - region-scene-prototype (closed)
companion_explorations:
  - docs/explorations/city-region-zoom-transition.md
  - docs/explorations/region-depth-and-scale-switch.md
  - docs/explorations/assets/city-scene-loading-research.md
arch_decisions_inherited:
  - DEC-A29 (iso-scene-core-shared-foundation)
  - DEC-A28 (ui-renderer-strangler-uitoolkit)
  - DEC-A22 (prototype-first-methodology)
  - DEC-A23 (tdd-red-green-methodology)
---

# Region-Scale City Blocks — Exploration Seed

**Status:** Seed (problem statement + vision + open questions). Ready for `/design-explore` after `city-region-zoom-transition` lands its design expansion + defines the integration event that fires composition recompute.
**Gate:** `city-region-zoom-transition` shipped (parent seed). Placeholder brown-diamond tiles ship there; this seed defines their replacement.
**Parent deferral:** Parent seed locked placeholder brown diamonds for the player's 2×2 region-cell footprint + neighbor cities. This seed scopes the procedural composition feature that replaces them.

---

## Problem Statement

At region zoom the player sees a 64×64 cell map. Each cell = 32×32 city-cells of world space. The player's own city occupies a 2×2 region-cell footprint anchored at its origin; neighbor cities sit elsewhere on the grid. Today the parent transition seed renders each city footprint as a flat brown diamond — readable enough to validate the zoom segue but visually mute. A region full of identical brown patches cannot communicate which cities are downtowns, which are farms, which are factory towns.

Cities must visually parse as cities from far away. A high-population commercial city should read as a high-rise downtown silhouette. A low-pop rural city should read as scattered greenery + farm plots. Mid-size industrial cities should read as a band of warehouses + smokestacks. The visual composition must be derived from each city's evolved sim state (population, urban_area, zone mix, growth rate, density distribution) so the region map mirrors what the player actually built across all their cities.

This is render-pipeline work + art-deliverable work + composition-algorithm work fused into one feature. None of the three pieces can ship independently — the algorithm needs the catalog, the catalog needs the art, and the art only reads at region scale if the render pipeline can compose multiple visual building blocks per cell.

---

## Vision

- **Block taxonomy.** Small set of visual building blocks tagged by district function:
  - High-rise blocks (downtown / city center)
  - Mid-rise residential
  - Low-rise residential
  - Industrial blocks
  - Commercial / mixed-use blocks
  - Greenery / parks / forested patches
  - Scattered farm / agricultural / light-industrial outskirts
- **Per-city composition derived from evolved state.** Composition algorithm reads city sim fields (population, urban_area, zone counts by type, growth rate, density centroids) + emits an ordered set of blocks placed across the city's region-cell footprint.
- **Sub-cell layout.** Player city 2×2 footprint = 4 sub-cells minimum. Each sub-cell may hold one block or blocks may span multiple sub-cells. Finer subdivision possible if blocks themselves carry internal grid.
- **Recompose at meaningful triggers.** Zoom-out entry triggers a recompute. Evolution threshold crossings (city pop crosses 10k, downtown forms, industrial zone exceeds N cells) may trigger live recompute while player is in region view. Exact cadence is open.
- **Art assets are first-class deliverables.** Sprite catalog ships with the feature; not an afterthought tracked separately.
- **Same algorithm for neighbor cities.** Neighbors driven by the simulated growth model from `region-depth-and-scale-switch` Axis 4 produce the same shape of sim state; composition algorithm consumes it identically.
- **Coherent style.** Block sprites match the existing city-scale isometric style so the zoom seam reads as zoom, not as scene swap.

---

## Known Design Decisions (locked, do not re-grill)

- **Region grid size.** 64×64 region cells (locked in `region-scene-prototype`).
- **Region cell scale.** 1 region cell = 32×32 city cells (locked in `region-scene-prototype`).
- **Player city footprint.** 2×2 region-cell anchor at city origin (locked in `region-scene-prototype`).
- **DEC-A29.** Shared `IsoSceneCore` foundation. Region renderer + camera + culling + tick + UI shell stay shared with CityScene. Composition + block rendering plug into the existing region renderer slots.
- **DEC-A28.** UI Toolkit only for any new UI surface (block inspector tooltip, composition debug panel, etc). No uGUI.
- **DEC-A22.** Prototype-first methodology. First stage of the resulting plan ships a Stage 1.0 tracer slice: one block type + one composition rule + one sub-cell render path replacing the brown diamond for the player city only.
- **DEC-A23.** TDD red→green protocol per stage. One test file per stage, red on first task, green on last.
- **Hub-preservation rule (DEC-A29 addendum).** Inspector-wired hubs never renamed / moved / deleted. Any new persistent coordinator (working title `RegionBlockCompositionManager` or similar) becomes Inspector-wired from creation + falls under the same never-rename rule. New logic otherwise goes into `Domains/{X}/Services/` under existing hubs.
- **Render-only replacement.** This feature ships strictly as a render replacement for the placeholder brown diamonds. It does NOT touch the zoom transition mechanic itself, the camera tween, the welcome stats panel, the save format, the sim tick, or any other parent-seed surface.
- **Parent seed dependency.** `city-region-zoom-transition` must land + define the integration event ("city evolved state changed; recompose composition for this city") before this seed can ship. That event is the contract surface.

---

## Open Questions (to be grilled by `/design-explore`)

### Block taxonomy

1. **Exact block-type count.** Vision lists 7 categories. `/design-explore` Phase 0.5 must pick a concrete count for v1 (4? 6? 8? 12?) — fewer = easier to author + ship, more = richer visual variety per city.
2. **Sub-variations per type.** Each block type ships with N sprite variants for visual variety (one high-rise sprite would tile-repeat ugly across multiple downtown cells). Lock variant count per type. Affects sprite-catalog budget.
3. **Orientation awareness.** Are blocks orientation-aware — do they rotate to face the city center, or align to road grid neighbors, or are they fixed-orientation isometric tiles? Affects sprite count multiplier (×4 if 4 rotations needed).
4. **Density tiers.** Low-rise residential vs mid-rise vs high-rise residential — three separate block types, or one residential type with a density parameter that picks sprite variant? Same question for commercial + industrial.
5. **Special blocks.** Landmark (player-named monument), port (coastal cities), ruined (disaster aftermath), festival (event-driven) — in v1 catalog or later expansions?
6. **Empty / fallback block.** What renders for an empty sub-cell (city footprint area with no zone activity yet)? Empty greenery tile? Bare terrain? Affects the composition algorithm's null branch.

### Composition algorithm

7. **Input fields from city state.** Lock the field list the algorithm consumes: population, urban_area, per-zone-type cell counts (residential / commercial / industrial / mixed), growth rate, city age, density centroid coordinates within the city, building-type histogram? Each field added = more grill on how the algorithm weights it.
8. **Algorithm shape.** Rule-based decision table (deterministic, easy to debug, may feel mechanical), weighted-random draw (varied, less predictable), learned model (NN trained on hand-curated examples), or hand-tuned heuristic? Or hybrid — rule-table for typical cities + override slots for special cases.
9. **Composition stability.** Does a small input change produce a small visual change (perceptual continuity), or can blocks flip dramatically on tiny sim deltas? Stability matters for player trust — "my city looked like a downtown last visit, why is it suddenly farms?".
10. **Seed determinism.** Same city state + same seed → same composition always? Required for save-replay + reproducibility + future multiplayer? Or stochastic with per-session RNG?
11. **Algorithm versioning.** If algorithm changes between game versions, do existing saves recompose against new algorithm or keep their old composition pinned? Affects save semantics question below.

### Update cadence

12. **Recompute triggers.** Every region tick / every game-day / every player zoom-out / only on evolution threshold crossings (city crossed 10k pop, downtown formed, etc)? Lock the trigger list.
13. **Live recompose while in region view.** Player sits in region view for minutes — do their visible cities animate to new compositions as sim ticks under them, or stay frozen until next zoom-out + zoom-in cycle?
14. **Animated transitions vs hard swap.** When blocks change, does the new block crossfade in over N seconds, or hard-swap on the tick boundary? Affects visual polish budget.

### Sub-cell layout

15. **Sub-cell count per region cell.** Player city = 2×2 region cells = 4 sub-cells minimum. Does each region cell internally subdivide further (4×4 = 16 sub-cells per region cell)? Trade-off: more sub-cells = finer composition + higher draw cost.
16. **One block per sub-cell vs multi-cell blocks.** Does each sub-cell hold exactly one block, or can a single large block (e.g. one downtown skyscraper cluster) span 2×2 sub-cells inside a region cell?
17. **Sub-cell render scale.** Sub-cell sprite footprint relative to region-cell sprite footprint. Affects pixel-grid math + sprite source-resolution requirements.
18. **Non-player cities.** Neighbor cities = single 1×1 region-cell footprint or do some span 2×2 like the player city? If 1×1, fewer sub-cells = simpler composition. If variable, algorithm must handle both footprint shapes.

### Visual coherence

19. **Adjacent-block edge blending.** When a high-rise block sits next to a greenery block, do they blend via transition tile / gradient overlay, or are cell edges discrete? Trade visual polish vs sprite-count blowup.
20. **Sorting order within a sub-cell.** Each block may layer multiple sprites (building base + roof + foliage + roads). Pin the sort order convention so block authors don't fight the renderer.
21. **Day/night / weather variation.** Do blocks switch sprite variants on day cycle or weather state? In scope v1 or deferred?
22. **Camera angle.** Region scene uses isometric projection. Do blocks share the same isometric angle as city-scale sprites (continuity at zoom seam) or have their own region-specific projection?

### Neighbor cities

23. **Neighbor sim source.** Neighbors run no real city sim — their growth comes from the simulated growth model defined in `region-depth-and-scale-switch` Axis 4. Lock the data contract: which sim fields does that model emit + which does this composition algorithm consume?
24. **Neighbor footprint shape.** Single 1×1 region cell, or some neighbors evolve into 2×2 (founding cities that grew large)? Affects algorithm input shape.
25. **Neighbor visual distinction.** Should neighbor cities visually differ from the player city (tint, border ring, name label) or render identically by composition?
26. **Pre-game neighbor seed.** New-game initialization seeds N neighbor cities with starting state. What state? Random within plausible range, hand-authored, or generated from a region-archetype profile (forest region = more farm-heavy neighbors, coastal region = more port-heavy)?

### Player feedback

27. **Real-time visible change.** When player returns to region view after city play, do they see their city visibly changed (different block composition reflecting growth)? Or static-until-next-event?
28. **HUD cue.** Toast / banner / minimap pulse on visible change ("Your downtown grew" / "Industrial zone expanded")? Or silent — visible composition change is its own feedback?
29. **Inspector tooltip.** Hover over a city in region view → tooltip shows current composition summary (e.g. "Downtown core + 2 residential bands + industrial belt")? Adds UI surface scope.

### Performance

30. **Draw call budget at region zoom.** 64×64 cells × K blocks/cell × M sprite layers/block. Lock max acceptable draw calls per frame at region zoom. Drives atlas + batching strategy.
31. **Texture atlas plan.** One atlas per block-type family (all high-rise variants together)? One atlas per district style? One mega-atlas? Trade memory footprint vs batching efficiency.
32. **LOD.** Do far-from-camera region cells render simpler (one composite sprite per city instead of N block sprites per sub-cell)? Affects whether the algorithm produces a sprite-tree or a flat tile output.
33. **Composition compute cost.** Algorithm cost per city × N visible cities. Budget per frame on recompute path? Cache + invalidate strategy?
34. **Initial region-scene load.** First zoom-out fires composition for all visible cities. Lock max load-time impact on the parent seed's tween budget.

### Save semantics

35. **Composed blocks persistence.** Composed block layout persists in region save (snapshot of last computed result) or always re-derived from city state on load (deterministic regen)?
36. **Seed pinning.** If deterministic regen, is the RNG seed + algorithm version pinned per save? Required for cross-version save compatibility.
37. **Save schema growth.** If snapshot stored, what's the byte cost per city × N cities? Affects RegionSaveService schema budget.

### Art deliverables

38. **Sprite count estimate.** Per block type × sub-variations × rotations × density tiers × day-night variants → total sprite count. Lock target. Drives art-pipeline cost.
39. **Pixel-grid spec.** Does the existing region tile spec govern projection / pixel grid / source resolution, or does this seed introduce a new spec for block sprites?
40. **Art pipeline choice.** Hand-authored sprites only? Procedural generation via the sprite-gen pipeline? Mix (key blocks hand-authored, variants procedural)? Affects timeline + cost.
41. **Palette + style coherence.** How is style continuity enforced at the zoom seam (city sprites at min zoom should match region block sprites at max zoom)? Reference palette + style guide doc deliverable?
42. **Iteration loop.** Sprite authoring → in-game preview → composition test → reauthor cycle. Lock the tooling — sprite-gen calibration axis, editor preview window, in-game debug overlay?

### Glossary + IA integration

43. **New domain terms.** Block, composition, density tier, downtown core, residential band, industrial belt, district function. Each needs a glossary row.
44. **Block catalog storage.** DB rows via `catalog_archetype_*` MCP slice (same pattern as existing archetype catalog), static ScriptableObject, or generated JSON? Affects authoring workflow + runtime load path.
45. **Composition rule storage.** If rule-based algorithm, rules live in code, in DB, in JSON? Authoring workflow for designers vs developers.

### Scope cutline

46. **Split decision.** This seed combines three concerns (render pipeline, algorithmic composition, art deliverable). `/design-explore` Phase 0 must decide:
    - **(a) Single combined plan.** Ship all three together as one master plan. Long timeline, tight integration, no intermediate state.
    - **(b) Split into two child explorations.** One explores render pipeline + composition algorithm (with placeholder art), one explores the art catalog separately. Smaller plans, parallel ship surface, art catalog can iterate independently.
    - **(c) Three-way split.** Render pipeline, algorithm, art catalog each own their seed + plan. Maximum independence, maximum coordination overhead.
47. **Stage count target.** Once cutline locked, target stage count? 4–6 for narrowest split; 8–12 for combined render + algorithm; 12+ for full bundled plan.

---

## Approaches

*To be developed during `/design-explore` session.*

Likely fork shapes for the composition algorithm (do NOT decide here — `/design-explore` Phase 1):

- **A — Rule-based decision table.** Deterministic. Per-city state mapped through a fixed rule set (pop > 50k + commercial-zone-count > 30 → downtown core block) → fixed block composition. Easy to debug + visualize, may feel mechanical / repetitive across many cities.
- **B — Weighted-random draw.** Per-city state defines weights over the block-type set; algorithm draws blocks per sub-cell using a seeded RNG. Varied visual results, less predictable, harder to reason about why a city looks a certain way.
- **C — Hand-tuned heuristic with override slots.** Rule-table base for typical cities + named override slots ("if this city has the 'capital' tag → downtown core in center sub-cell") for special / landmark cities. Mixes determinism with hand-curated standout cities.
- **D — Hybrid.** Rule-table baseline for the dominant district picks, weighted-random for sub-cell placement + variant selection within a chosen district. Compromise between determinism + variety.

Likely fork shapes for the render pipeline:

- **E — Per-sub-cell tile renderer.** Composition produces a 2D grid of block-id tiles; renderer draws one isometric sprite per sub-cell. Simplest.
- **F — Block-as-prefab compositor.** Each block is a prefab containing multiple sprite layers; renderer instantiates prefabs per sub-cell. More flexible per-block detail, higher instantiate cost.
- **G — Pre-composed RenderTexture per city.** Algorithm runs once per city + bakes the visual to a RenderTexture that the region renderer draws as a single quad per city footprint. Cheapest at region zoom, requires re-bake on recompute.

`/design-explore` will rank these against constraint fit (DEC-A29 + DEC-A28 + hub-preservation) + visual quality target + draw budget + ship effort.

---

## Rescue Source — Multi-Scale Stage 8.0 Tasks

The deprecated `multi-scale` master plan carried Stage 8.0 tasks that map directly to this seed's catalog spine:

| Old task | Notes |
|---|---|
| `TECH-1898..1902` (Stage 8.0 — 16 archetype rows + 16 sprite rows + region UI token rows + RegionTile data record + glossary) | Direct rescue source for the catalog spine. Asset-pipeline rows for region tiles. Adapt the 16-archetype count to the v1 block-type count locked by `/design-explore`. |

This rescue assumes the asset-pipeline catalog spine ships first (or co-ships) — block catalog rows live in `catalog_archetype_*` slice same as existing archetype catalog.

---

## Notes

- This seed is INTENTIONALLY rich on open questions + light on solutions. `/design-explore` does the resolution work. The open-questions list is the grill input.
- **May warrant a Phase 0 split.** Combined render pipeline + algorithmic composition + art deliverable scope is wide. If `/design-explore` Phase 0 shows scope too wide for one plan, split into two child explorations (render + algorithm in one, art catalog in another) — Q46 grill drives the decision.
- **Coupling with `region-depth-and-scale-switch` Axis 4.** The simulated growth model for dormant + neighbor cities lives in that exploration's Axis 4 (dormant-city fidelity contract). This seed consumes its output as the input to the composition algorithm. Q7 + Q23 lock that data contract.
- **Coupling with `city-region-zoom-transition`.** The integration event ("city evolved state changed; recompose composition for this city") is owned by the parent transition seed. This seed CANNOT ship until parent defines that event. Q12 trigger list grills directly against the parent's event surface.
- **Hub-preservation rule applies recursively.** Any new hub introduced by this work (e.g. `RegionBlockCompositionManager`, `RegionBlockRenderer`) becomes Inspector-wired + falls under the same never-rename rule from creation.
- **Art catalog timeline risk.** Sprite authoring is the slowest part of the work. If single combined plan picked (Q46a), art delivery becomes the critical path. Splitting (Q46b/c) lets render + algorithm ship with placeholder art early + iterate art independently.
- **Zoom-seam visual continuity is the hardest visual problem.** Q41 lives at the boundary between this seed's art deliverable + the parent seed's tween animation. Joint review with parent seed `/design-explore` outcome recommended.

---
