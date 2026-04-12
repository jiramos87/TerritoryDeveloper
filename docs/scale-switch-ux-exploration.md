# Scale Switch UX — Exploration: Google Earth-style Zoom Transition

> **Status:** Committed to MVP (Step 3). Semantic zoom + `ScaleToolProvider` replace top-bar button mechanism.
> **Created:** 2026-04-12

## Problem

The master plan currently proposes a top-bar button + plain loading screen for scale transitions (city <-> region <-> country). This works but feels mechanical. A Google Earth-style continuous zoom could make scale transitions feel spatial and intuitive.

## Current camera system

`CameraController.cs` uses discrete orthographic zoom levels: `[2f, 5f, 10f, 15f, 20f, 30f]`. Scroll wheel triggers level-based jumps with smooth lerp (speed 18f). Movement speed scales with zoom. Max zoom ratio: 15x (ortho 2 -> 30). This covers street-level to city-overview but NOT region or country distances.

## Proposed approach: semantic zoom

Instead of a button click, the player zooms out past the current scale's maximum zoom level. The transition happens in stages:

### Phase A — Zoom out beyond city max

1. Player scrolls past ortho size 30 (city max).
2. Camera continues zooming smoothly. City detail fades — individual cells become abstract dots.
3. At a threshold (e.g. ortho 60), the city footprint becomes a single visual node.
4. Background thread begins loading region data + running reconstruction for neighboring cities.

### Phase B — Scale transition mask

5. Cloud/fog layer fades in as a masking transition (cosmetic, not gameplay).
6. While masked: swap the active scene content from city to region grid.
7. Camera is now at region-level zoom. Region cells visible — city nodes shown as compact sprites.
8. Fog fades out. Player is now in region scale.

### Phase C — Zoom into a city

9. Player zooms in on a city node in the region view.
10. At threshold, begin loading that city's reconstruction in background.
11. Cloud/fog mask, swap scene, resume at city zoom level.

## Technical considerations

### Camera extension

Current zoom levels are discrete. Need:
- Continuous zoom range across scales (ortho 2 -> 300+)
- Dynamic cell LOD: at extreme zoom-out, cells switch to abstract representation
- Per-scale zoom bands: city 2-30, transition 30-60, region 60-200, transition 200-400, country 400+

### Reconstruction latency

Critical bottleneck. If reconstruction takes >500ms, the transition feels like a loading screen with extra steps. Options:
- **Precompute region shell** — keep a low-res region grid always in memory (~minimal overhead)
- **Progressive reconstruction** — show region grid immediately, fill city nodes lazily
- **Snapshot cache** — cache last-seen reconstruction per city node, only re-evolve on switch

### Visual masking

Cloud/fog layer makes the scene swap invisible. Requirements:
- Shader or overlay that grows opacity as zoom crosses the transition band
- Works with the existing isometric rendering pipeline
- Minimal GPU cost (fullscreen quad with noise texture)

### Input design

- Mouse wheel: continuous zoom, same scroll sensitivity scaled to zoom level
- Keyboard Z/X: jump one zoom level (within scale) or trigger scale switch (at boundary)
- Pinch-to-zoom: same as mouse wheel (future touch support)
- Visual cues: scale name label appears when approaching a transition threshold
- Optional: hold Shift + scroll for fast cross-scale zoom

### Performance budget

| Metric | Target | Fallback |
|--------|--------|----------|
| Zoom responsiveness | <16ms per frame | Same as current |
| Transition mask duration | 500ms-1000ms | Plain loading screen if >2s |
| Region shell render | <200ms | Pre-cached |
| City reconstruction | <500ms for precomputed, <2s for full | Loading indicator over fog |

## Comparison with alternatives

| Approach | Pros | Cons |
|----------|------|------|
| **Button + loading screen** | Simple, reliable, no camera work | Breaks spatial immersion, feels like a menu |
| **Google Earth zoom** (this exploration) | Spatial, intuitive, immersive | Complex camera/LOD work, reconstruction latency matters more |
| **Minimap click** | Quick navigation, spatial reference | Still needs a transition animation |
| **Animated fly-to** | Cinematic, dramatic | Player loses control during animation |

## Feasibility assessment

**Likely feasible** with these constraints:
- Region shell must be pre-cached (low-res representation of all cities in the region)
- Cloud/fog mask hides the swap — NO requirement for truly continuous rendering across scales
- Camera zoom extension is straightforward (remove discrete levels, use continuous range)
- The hardest part is reconstruction latency — but progressive loading + snapshot caching makes this manageable

**Risk:** if reconstruction consistently takes >2s, the fog mask becomes a dressed-up loading screen. Mitigation: progressive rendering (show grid immediately, fill detail lazily).

## Resolved questions

1. **Zoom bands configurable per map size?** No — same transition points regardless of map size. Simplifies the camera system and keeps the UX predictable across saves.
2. **Speed-control panel interaction?** Speed control stays identical across all scales. No per-scale speed variants.
3. **Cancel mid-transition?** Yes — player can scroll back during the transition band to abort and return to the current scale. The fog/mask reverses smoothly.
4. **Separate zoom-out vs zoom-in transition styles?** Undecided — start with identical transitions for both directions. Revisit if playtesting reveals a need for asymmetry.
5. **Fog/cloud texture source?** Procedural. Fullscreen noise shader, no authored art assets. Keeps the transition lightweight and resolution-independent.

---

## Per-scale tool panels

### Problem

Each scale has **different player tools**. The UI shell (layout, panel structure, visual design) stays consistent, but the toolbar contents swap entirely per scale:

| Scale | Example tools |
|-------|---------------|
| **City** | Draw streets, zone RCI, place buildings, bulldoze, set budget |
| **Region** | Draw highways/railways, found new cities, set inter-city policy, allocate regional budget |
| **Country** | Set national priorities, declare relations, route national infrastructure, allocate per-region budget |

This is not just a visibility toggle — the button sets, contextual menus, and input modes are fundamentally different per scale.

### Design constraints

- **Visual consistency:** Same panel chrome, same layout grid, same icon style, same interaction patterns (click, drag-draw, radial menu). Player learns one UI language, tools change underneath.
- **No stacking:** Only the active scale's tools are visible. No "grayed out city tools while in region view."
- **Smooth swap:** Tool panel swaps during the fog/cloud transition mask — when the fog clears, the new scale's tools are already in place. No pop-in.
- **Persistent per-scale state:** If the player had the zoning tool selected in city, switching to region and back should restore zoning tool selection (not reset to default).

### Proposed architecture: `ScaleToolProvider`

Each scale registers a `ScaleToolProvider` that defines:
- Available tool categories (e.g., Infrastructure, Zoning, Policy, Budget)
- Tool entries per category (icon, label, input mode, tooltip)
- Default selected tool on first entry
- Context panel content (right-side inspector when tool is active)

The main toolbar reads from the active `ScaleToolProvider`. On scale switch:
1. Serialize current tool state (selected tool, sub-options)
2. Fog mask begins
3. Swap active `ScaleToolProvider`
4. Toolbar rebuilds from new provider
5. Restore saved tool state for incoming scale (or default if first visit)
6. Fog clears — new tools visible

### Resolved sub-questions

- **Tool taxonomy depth:** MVP ships minimal toolsets per scale. Full tool depth is post-MVP content (see §post-MVP scope below).
- **Shared tools:** Demolish, inspect, speed control live in a **fixed always-visible strip** outside the `ScaleToolProvider`. They do not swap on scale change.
- **Tool keybindings:** Consistent semantic mapping — same key = same intent across scales (e.g., "B" = build/place in all scales, "Z" = zone/designate in all scales). Few keybindings exist today, so this is forward-compatible.

---

## MVP vs post-MVP split

### MVP scope (master plan Step 3)

These items are **load-bearing for the scale-switch proof:**

1. **Continuous zoom camera** — remove discrete zoom levels, implement continuous ortho range across scales with per-scale zoom bands (fixed transition points).
2. **Fog/cloud procedural mask** — fullscreen noise shader that hides the scene swap. Opacity ramps up in the transition band, scene swaps while opaque, ramps down.
3. **Cancel-by-scrolling-back** — player scrolls back during transition band to abort. Fog reverses.
4. **Scale label indicator** — text label appears when approaching a transition threshold (e.g., "Entering Region View").
5. **Basic `ScaleToolProvider` swap** — toolbar rebuilds per scale during fog mask. Minimal tool sets per scale (city: existing tools; region: found city + draw highway + budget; country: priorities + budget). Same panel chrome.
6. **Speed control unchanged** — same panel, same behavior, all scales.

### Post-MVP scope (expansion doc)

These items improve the experience but are **not required** for the three-scale proof:

1. **Google Earth-style truly continuous rendering** — rendering across scales without the fog mask (LOD streaming, progressive detail). The fog mask is the MVP escape hatch.
2. **Animated fly-to** on city node click from region view (cinematic transition alternative).
3. **Minimap integration** with scale transitions.
4. **Progressive scale-switch loader** — dashboard renders first from snapshot while map chunks load center-out (already in post-MVP §6.2).
5. **Asymmetric transition styles** — different zoom-out vs zoom-in animations if playtesting warrants.
6. **Full per-scale tool depth** — rich region and country toolsets beyond the minimum.
7. **Tool keybinding policy** — per-scale vs semantic-consistent key mapping.
8. **Shift+scroll fast cross-scale zoom** — power-user shortcut.
9. **Pinch-to-zoom** touch support.
10. **Per-scale tool state persistence across saves** — MVP persists in-session only; save/load persistence is post-MVP.
