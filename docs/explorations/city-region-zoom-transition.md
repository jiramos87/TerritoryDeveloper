---
slug: city-region-zoom-transition
status: seed
parent_exploration: city-scene-loading-research (Approach C)
depends_on_prototype_close: region-scene-prototype shipped (closed)
related_master_plans:
  - region-scene-prototype (closed)
  - city-scene-loading-perf-quick-wins (separate; Approach A from same parent research)
companion_explorations:
  - docs/explorations/assets/city-scene-loading-research.md (parent research; Approach C source)
  - docs/explorations/region-depth-and-scale-switch.md (consumes this seed; Axis 2)
  - docs/explorations/region-scale-city-blocks.md (sibling seed; procedural city composition at region scale)
arch_decisions_inherited:
  - DEC-A29 (iso-scene-core-shared-foundation)
  - DEC-A28 (ui-renderer-strangler-uitoolkit)
  - DEC-A22 (prototype-first-methodology)
  - DEC-A23 (tdd-red-green-methodology)
---

# City → Region Zoom Transition — Exploration Seed

**Status:** Seed (product vision + locked decisions + open questions). Ready for `/design-explore` once sibling `region-scale-city-blocks` seed lands so the city-in-region rendering contract is at least scoped.
**Gate:** `region-scene-prototype` shipped (yes). CoreScene refactor is a hard pre-condition called out in §Open Questions.
**Parent research:** `docs/explorations/assets/city-scene-loading-research.md` Approach C (proposals #6, #7, #8, #15, #16) is the source-of-truth feasibility survey. This seed locks the product intent + the architecture envelope derived from it.

---

## Problem Statement

The game spans two scales: **CityScene** (player builds + manages one 64×64 city) and **RegionScene** (player browses a 64×64 region grid where 32×32 city-cells = 1 region-cell; the player city occupies a 2×2 anchor at its origin). Today the only way to cross scales is the main-menu route — quit the game, re-enter from a different menu path. That breaks the multi-scale fiction completely.

The product vision is a single segue: the player asks to leave the city, sees an animated zoom-out, lands inside the region map with their city visible as one anchored area, and can travel back via the symmetric zoom-in. The two scenes must feel like two depths of the same world, not two separate apps.

This seed is the product-facing companion to the technical loading research already done. The research surveys feasibility + algorithmic options; this seed locks the product intent + the architecture envelope so `/design-explore` can commit to the implementation.

---

## Vision (locked from product grill rounds 1–5)

1. Player initiates zoom-out from CityScene via a HUD button **OR** scroll-past-max-zoom; both routes show a confirmation panel ("Leave city for region view?"). Return path is symmetric: click player's city tile in RegionScene → confirmation → zoom-in.
2. On confirm: auto-save fires first; save failure → cancel transition + error toast, stay in city. On save success: city sim pauses; the zoom-out tween begins.
3. Tween shape: orthographic-size animation. City sprites stay rendered live as the camera pulls back; once the city footprint fits its 2×2 anchor area in region coordinates, the region map crossfades in around it. The architecture MUST support a nice animated effect — animation feel is product-grade, not utilitarian.
4. Tween duration is adaptive — targets a cinematic default (~1.5–2.0 s) but extends until the destination scene's required content is ready to display. No mid-tween cancel allowed once the player has confirmed.
5. On landing: camera always centers on the player's own city. The welcome stats panel + HUD + budget + minimap are usable from frame 1 because they live in a new persistent **CoreScene**, not in CityScene/RegionScene. Pan + zoom controls are disabled until cell content finishes streaming, then unlocked.
6. Algorithmic-growth handoff: on entry to the destination scene, a single one-shot catch-up step advances the destination world by the time elapsed on the other side. No background growth ticks while either scene is dormant — growth happens on entry only.
7. Audio: deferred for v1. Architecture must not foreclose music crossfade or transition SFX later.
8. City + region persist in two files paired by save id (`{saveId}.city`, `{saveId}.region`).

### What the player explicitly sees + does NOT see

| Moment | Sees | Does NOT see |
|---|---|---|
| Pre-confirm (HUD click or scroll past max) | Confirmation panel "Leave city for region view?" | Tween in progress |
| Post-confirm, save running | Saving indicator inside the panel | Half-loaded scene |
| Tween out, frame 1..N | City pulling away, region appearing underneath | Black screen / loading bar / modal dialog |
| Land in region | Welcome stats panel (right side), full HUD, minimap, player city centered, brown-diamond placeholder where own city is | Pan / zoom enabled (locked until stream done) |
| Stream-in tail | Cells popping in around the centered view | Camera moving on its own |
| Pan / zoom unlocked | Subtle UI tell (toast or panel state change) | Mode-change modal |
| Return click on city tile | Confirmation panel "Enter city?" | Instant teleport |
| Tween in, frame 1..N | Region pulling back, city emerging | — |
| Land in city | City restored at saved zoom + position, sim still paused | — |
| Sim resume | Auto-resume after stream-in done (or single resume beat) | — |

---

## Known Design Decisions (locked, do not re-grill)

### Locked in this seed's grill (rounds 1–5)

- **Trigger:** HUD button + scroll-past-max-zoom. BOTH routes require the same simple yes/no confirmation panel ("Leave city for region view?"). No "don't ask again" toggle in v1.
- **Return path:** symmetric zoom-in tween on player-city-tile click in region view, same confirmation panel pattern.
- **Sim state during transition:** city sim pauses on confirm; resumes on return-and-land. Neither scene runs real sim while dormant.
- **Algorithmic growth handoff:** runs on entry to the destination scene only (one-shot catch-up by elapsed time). No background growth.
- **Mid-tween visual:** city stays rendered live while camera zooms; region crossfades in underneath. Architecture must enable a nice animation (curve flexibility, layered render order).
- **Cancel after confirm:** not allowed; tween must finish.
- **Auto-save:** fires before the tween starts. Save failure → cancel transition + error toast in city view; sim resumes (or stays paused if already paused).
- **Tween duration:** adaptive, waits for scene load. Default cinematic target 1.5–2.0 s; hard cap to be picked in `/design-explore`.
- **First-time setup:** region is fully initialized at new-game time. First zoom-out is identical to all subsequent zoom-outs.
- **Camera ownership:** **single persistent camera** owned by a new always-loaded **CoreScene** that also holds HUD + stats + minimap. CityScene + RegionScene = pure additive world-content scenes. No camera swap, no per-scene HUD instance.
- **Landing-wait feedback:** pan/zoom disabled until streaming completes. Welcome stats panel + HUD + budget + minimap live from frame 1 (CoreScene-owned).
- **Landing position:** always centered on the player's own city.
- **Audio:** no music or SFX change in v1. Architecture leaves room for crossfade later.
- **Save layout:** two files paired by save id (`{saveId}.city`, `{saveId}.region`).
- **Failure mode:** any save failure or scene-load failure after confirm → cancel transition, show error toast, stay in city. No retry button v1.

### Inherited from prior work

- **DEC-A29** — Shared `IsoSceneCore` foundation (camera + chunk culling + tick + UI shell + tooling) is shared between CityScene + RegionScene. This seed REUSES the same core; the new CoreScene brings the IsoSceneCore-owned camera + HUD + UI into a persistent scene rather than re-instantiating per world scene.
- **Hub-preservation rule (DEC-A29 addendum).** Inspector-wired hub scripts NEVER renamed/moved/deleted. The CoreScene refactor must promote existing hubs to persistent ownership via reparenting / `DontDestroyOnLoad`, not by gutting them. New persistent coordination belongs to a NEW CoreScene-side hub (working title `SceneOrchestratorManager`).
- **Region cell scale:** 1 region cell = 32×32 city cells (locked in `region-scene-prototype`). Player city = 2×2 region-cell footprint anchored at its origin.
- **Region grid size:** 64×64 cells.
- **Global game tick.** One tick driver shared across both world scenes. Tween freezes the tick; on land, the catch-up step replays elapsed ticks against the destination scene's idle evolver.
- **UI Toolkit only** (DEC-A28). All new transition UI (confirmation panel, welcome stats panel, error toast) hand-authored UIDocument + UXML + USS + C# controller. No uGUI.
- **Prototype-first** (DEC-A22). First stage of the resulting plan must ship a Stage 1.0 tracer slice: button → confirm panel → minimal placeholder tween → land in region centered on city.

---

## Open Questions (to be grilled by `/design-explore`)

### Architecture

1. **CoreScene component manifest.** Exact list of components that move to CoreScene: camera, HUD root, stats panels, minimap, save coordinator, transition controller, audio mixer (placeholder), error toast surface. Which existing hubs migrate into CoreScene by reference (`DontDestroyOnLoad`) vs which stay scene-scoped? `GameManager`, `GridManager`, `GeographyInitService`, `UIManager`, `RegionManager` — classify each.
2. **CoreScene boot order.** On app start: CoreScene loads first; CityScene or RegionScene loads additively. What is the bootstrap sequence — splash → main menu → world scene additive? Or CoreScene + main-menu canvas only, world scene fires on save-load button?
3. **Single-camera lifecycle.** `Camera.main` lives in CoreScene. CityScene + RegionScene cannot ship their own camera. Who owns framing, post-fx, layer-mask switching during transition? `ZoomTransitionController` owns `orthographicSize` — does it also own layer-mask flips (city sprites visible vs region cells visible)?
4. **Scene unload ordering.** During zoom-out: city scene must unload AFTER the tween lands and region is interactive. What is the unload trigger — explicit controller call, fade-on-final-frame, coroutine post-land? Same question reversed for zoom-in.
5. **Hub migration order.** CoreScene refactor is the biggest architectural lift. Which hubs migrate in which stage? Can the migration be incremental (one hub per stage) or must it be atomic?

### Partitioned loading

6. **Welcome stats panel content.** Field list on landing in region view: population sum across known cities? Region treasury (pending Axis 1 of `region-depth-and-scale-switch`)? Time elapsed in city? Number of dormant cities? Lock the data source for each field.
7. **Cell streaming order.** When region scene activates, cells stream in. Order: center-out from player city? Reading-order? Distance-from-camera? Per-frame budget (N cells/frame, `Awaitable.NextFrameAsync` per the parent research)?
8. **Pan/zoom unlock condition.** Stream done = all 64×64 cells loaded? All visible cells loaded? First ring (3×3 around player) loaded? Trade fast-unlock vs visual completeness.
9. **Minimap behavior across scales.** Single persistent minimap component renders both city map (in CityScene) and region map (in RegionScene)? Or two distinct render modes inside the same component, swapped by scene context?

### Animation + visual continuity

10. **Tween curve + interpolation.** PrimeTween + `Ease.InOutCubic` is the research-doc default — `/design-explore` validates that or picks per-axis curves (separate ease for orthographic-size vs region-fade-in alpha)?
11. **City→region crossfade timing.** At what tween progress (0.5? 0.7?) does the region map start fading in? Hand-tuned per visual review or driven by city-fits-2×2-anchor geometric condition?
12. **City-stays-live cost.** Live city rendering during the tween costs per-frame sprite draws while the camera scales. Acceptable cost, or does the city swap to a frozen RenderTexture impostor at, say, tween progress 0.8? Affects "nice animation" feasibility budget.
13. **Region pre-fade state.** During the tween, before region is visible, what fills the area outside the city footprint? Black? Procedural fog? Region "shadow" pre-render?

### Algorithmic-growth handoff

14. **Catch-up algorithm choice.** On entry: apply N destination ticks where N = elapsed-time / dest-tick-period. Algorithm specifics — `Compact / Expand` + `ICityEvolver` from rescued multi-scale Stage 9.0 reused, or re-authored under per-scene FS save model?
15. **Growth source-of-truth + clock.** User-locked decision: growth happens on entry, not in background. Evolver lives where? In CoreScene (always loaded) — but its data is the dormant scene's. Lives in the destination scene on landing — but needs an elapsed-time delta from a clock that DID tick during the dormant interval. Pick the clock (real-time? game-time? abstract tick counter persisted with the save?).
16. **Determinism.** Same input snapshot + same elapsed time → same output? Required for save-replay tooling, future multiplayer? Or stochastic with seeded RNG?

### Save semantics

17. **Two-file pairing integrity.** `{saveId}.city` + `{saveId}.region` paired by save id. If region file is missing or corrupt on load: fail open (regenerate region from city's region-of-origin seed) or fail closed (error toast + abort load)?
18. **Save format compatibility.** Existing region-scene-prototype save format — extend in place or v-bump with migration? `RegionSaveService` already exists.
19. **Auto-save scope.** "Auto-save before tween" — both files? Only the active scene's file + a stamp-only metadata write to the other? Defines transactional shape.

### Failure handling

20. **Error toast surface.** Toast lives in CoreScene (always visible). Short banner top-right? Modal? Dismissable / auto-fade? Same component for both save and load failures?
21. **Save-fail vs load-fail differentiation.** Same toast copy, different copy, summary + log? What action does the player have besides OK?

### Sibling-exploration coupling

22. **`region-scale-city-blocks` contract.** This seed accepts placeholder brown-diamond tiles for the player's 2×2 footprint in region view. Sibling exploration replaces those with procedural composed blocks (high-rise / residential / industrial / greenery / farm). When sibling lands, does the player's 2×2 area re-render automatically from city evolution data? Is a new event fired by the transition?
23. **Neighbor-city rendering.** Neighbor cities in region view also need a visual. Sibling exploration owns procedural composition; for v1, neighbors render as brown diamonds same as player city, with a different tint to distinguish player vs neighbor?

---

## Approaches

*To be developed during `/design-explore` session.*

The parent research doc Approach C bundled proposals #6 (additive RegionScene load behind tween), #7 (orthographic zoom-tween segue), #8 (RenderTexture impostor — superseded by the "city stays live" decision in this seed), #15 (neighbor stubs — superseded by sibling exploration), #16 (`allowSceneActivation` gate). Likely fork shapes during `/design-explore` Phase 1:

- **A — Adopt research Approach C with adjustments.** Use the research design, swap RenderTexture impostor for live city rendering during tween (per locked decision), swap neighbor stubs for sibling-exploration-provided block compositions when ready. Default starting point.
- **B — Hybrid live-then-impostor.** City renders live for the first ~80% of the tween, then crossfades to a RenderTexture impostor for the final shrink. Compromise between performance + animation quality.
- **C — Pure live render the entire tween.** No impostor anywhere. Highest visual quality, highest per-frame cost. Falls back to A or B only if profiling shows the cost unacceptable.

`/design-explore` will rank these against constraint fit (DEC-A29 IsoSceneCore + hub-preservation + CoreScene refactor cost) + animation quality target + ship effort.

---

## Rescue Source — Multi-Scale Plan Stage 12.0 Tasks

The deprecated `multi-scale` master plan carried scale-switch tasks that map directly:

| Old task | Notes |
|---|---|
| T12.1 — Scale state machine | Direct rescue. Re-frame as `ZoomTransitionController` state machine owned by CoreScene. |
| T12.2 — Dissolve in/out VFX | Re-frame as the city-stays-live + region-crossfade-in animation. Optional VFX shader pickup later. |
| T12.3 — Dormant evolve dispatcher | Direct rescue. Becomes the on-entry catch-up step. Depends on Axis 4 of `region-depth-and-scale-switch` for fidelity contract. |
| T12.4 — Budget tests | Direct rescue. Frame budget for tween + stream-in target. |

Stage 14.0 region-save tasks (T14.1–T14.6) partially rescuable — the open product question "do existing v3 single-city saves need migration?" survives. Two-file pairing format is new.

---

## Notes

- This seed is INTENTIONALLY product-vision-locked + architecture-envelope-locked but implementation-open. `/design-explore` does the architecture detail + implementation roadmap.
- **CoreScene refactor is the BIG architectural lift.** `/design-explore` Phase 4 must walk the full migration order — which existing hubs move when, what `DontDestroyOnLoad` reparenting looks like, how save/load wires across the boundary. May warrant its own preparatory stage cluster before the transition stages themselves.
- Sibling exploration `region-scale-city-blocks` ships independently and slots into the player-city 2×2 footprint render path. Until that ships, brown-diamond placeholder tiles are acceptable.
- Companion exploration `docs/explorations/region-depth-and-scale-switch.md` Axis 2 (Scale Switch UX) **consumes this seed's outcome**. After `/design-explore` resolves this seed, that exploration's Axis 2 open questions auto-resolve and that exploration can scope to its remaining 3 axes (economy, build mode, dormant fidelity).
- Sister exploration `city-scene-loading-perf-quick-wins.md` (Approach A from same parent research) is independent and may ship first — its CoreScene-unaware perf improvements still apply.

---
