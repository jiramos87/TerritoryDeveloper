# Isometric Sprite Generator — Exploration

**Status**: Exploratory / pre-spec  
**Date**: 2026-04-13  
**Context**: Territory Developer needs isometric pixel art sprites with correct shadowing, perspective, and palette consistency. No current off-the-shelf AI tool handles this reliably. This doc surveys approaches and proposes a build direction.

---

## Problem

Isometric pixel art has hard constraints that generic AI image tools fail on:

1. **Fixed projection** — true 2:1 pixel ratio, 26.565° angle (arctan 0.5). Generated art rarely lands on this exactly.
2. **Directional light** — shadow must fall consistently (typically NW light source → SE shadow) across all sprites.
3. **Palette coherence** — all sprites must share a restricted, consistent palette for lo-fi look.
4. **Tile-friendliness** — edges must align to isometric grid without bleeding or misalignment.
5. **Animation frames** — buildings, units, effects need frame strips on the same grid.

Current tools (Midjourney, DALL·E, Stable Diffusion, Imagen) produce aesthetically interesting results but require heavy manual correction for all five constraints — making them net negative for a systematic pipeline.

---

## Approaches surveyed

### A. Fine-tuned diffusion model (LoRA / DreamBooth)
- Train on a curated isometric pixel art dataset (OpenGameArt, itch.io CC0 assets)
- Fine-tune Stable Diffusion with LoRA targeting the projection + shadow style
- **Pro**: flexible prompting, can generate novel assets
- **Con**: training data curation heavy; output still needs pixel-snapping post-process; palette enforcement external
- **Effort**: high (weeks)

### B. Programmatic sprite renderer (3D → pixel bake)
- Build low-poly 3D models (Blender / MagicaVoxel) at correct isometric camera angle
- Render to pixel-perfect resolution with palette-restricted shader
- Bake to sprite sheets
- **Pro**: mathematically correct projection + shadows; full control; re-renderable at any scale
- **Con**: requires 3D asset authoring; not generative (manual per-asset)
- **Effort**: medium per asset, but systematic

### C. Voxel pipeline (MagicaVoxel → isometric bake)
- MagicaVoxel natively outputs voxel models with palette
- Isometric render scripts exist (vox2sprite, goxel exporters)
- AI voxel generators emerging (text → voxel)
- **Pro**: lo-fi aesthetic matches Territory Developer; palette-native; good tooling
- **Con**: current text→voxel AI quality low; still manual authoring per asset class
- **Effort**: low-medium for tooling setup; manual per asset

### D. Hybrid: LLM-guided pixel art engine (custom)
- Build a generator that: takes a text description → LLM outputs sprite spec (color indices, shadow map, outline rules) → renders to pixel grid programmatically
- Palette = hardcoded per biome/asset class
- Shadow = rule-based (NW light, offset by height)
- Shape = parameterized templates (building footprint, roof type, vegetation silhouette)
- **Pro**: fully deterministic, palette-safe, projection-correct; no training data needed
- **Con**: expressiveness limited by templates; not truly generative
- **Effort**: medium (build templates + renderer)

### E. Post-process pipeline (AI draft → auto-correct)
- Generate with Stable Diffusion using isometric LoRA
- Apply pixel-snap script (round to nearest grid pixel)
- Apply palette reduction (pngquant / custom quantizer with fixed palette)
- Apply shadow layer via depth map
- **Pro**: leverages generative power; pipeline automatable
- **Con**: quality of "snap + quantize" step degrades detail; needs human QA per asset
- **Effort**: medium pipeline build + ongoing QA

---

## Recommendation

**Short-term**: Approach C (voxel pipeline). MagicaVoxel + isometric bake scripts. Palette locked per biome. Fast to set up, consistent output, lo-fi aesthetic match. Use for buildings, terrain features, props.

**Medium-term**: Approach D (custom programmatic generator) for parameterized asset families (road segments, zone overlays, infrastructure). Template-driven = guaranteed grid alignment and palette coherence.

**Long-term / moonshot**: Approach E (AI draft → post-process pipeline) once isometric LoRA quality improves and snap/quantize tooling matures. Revisit in ~12 months.

---

## Isometric sprite generator — build proposal (MVP)

A CLI tool (`tools/sprite-gen/`) that:

1. Reads a YAML spec: `{ asset_type, biome, height, palette_id, shadow_angle }`
2. Renders to a canvas using a JS/Python pixel renderer (no 3D engine dependency)
3. Outputs PNG sprite sheet + JSON metadata (frame count, anchor point, tile offset)
4. Palette enforced from `tools/sprite-gen/palettes/{palette_id}.json`
5. Shadow computed by height offset in NW→SE direction at 2:1 pixel ratio

Input example:
```yaml
asset_type: building_residential_small
biome: temperate
height: 2
palette_id: lo-fi-earth
shadow_angle: nw
```

Output: `assets/sprites/generated/building_residential_small_temperate.png`

---

## Open questions

- Which voxel authoring tool fits the workflow best (MagicaVoxel vs Goxel vs hand-coded)?
- Should the sprite-gen CLI live in this repo or as a standalone tool?
- What is the minimum tile set needed for a Steam Early Access build?
- Is there budget/time to commission a pixel artist for hero assets and use the generator for bulk/repeat tiles?

---

## Next steps

1. Audit current sprite inventory — what exists, what quality
2. Define minimum tile set for EA build
3. Prototype voxel pipeline: one building class end-to-end (MagicaVoxel → bake → Unity import)
4. If prototype holds, create `TECH-` backlog issue for sprite-gen MVP build
