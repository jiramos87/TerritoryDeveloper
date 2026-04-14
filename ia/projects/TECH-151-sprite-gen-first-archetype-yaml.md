---
purpose: "TECH-151 — First archetype YAML building_residential_small.yaml."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-151 — First archetype YAML `building_residential_small.yaml`

> **Issue:** [TECH-151](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Author first concrete archetype YAML — `building_residential_small.yaml`. Exercises `load_spec` (archived) + `compose_sprite` (archived) end-to-end. Composition: `iso_cube × 2` (walls) + `iso_prism` (roof). Footprint 1×1 flat terrain, 4 variants, seed 42, residential palette. Serves as reference for all 15 EA archetypes downstream.

## 2. Goals and Non-Goals

### 2.1 Goals

1. YAML validates via `load_spec` (archived) w/ no errors.
2. `render building_residential_small` produces 4 variant PNGs at canvas size (64, 64).
3. All required schema keys present (exploration §8).
4. Composition models small house — two stacked cubes (ground + upper level) + prism roof.

### 2.2 Non-Goals (Out of Scope)

1. Additional archetypes — other 14 EA targets land Step 3.
2. Palette JSON authoring — Stage 1.3; this YAML references `palette: residential` by name only.
3. Slope variants — Stage 1.4.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | As pipeline dev, I want one canonical archetype YAML so that loader + compose layer validate against real input | `render building_residential_small` succeeds + 4 PNGs emitted |

## 4. Current State

### 4.1 Domain behavior

No archetype YAML exists — `specs/` dir is empty (TECH-123 scaffold). Stage 1.2 integration smoke (TECH-152) blocks on this.

### 4.2 Systems map

- `tools/sprite-gen/specs/building_residential_small.yaml` (new).
- `tools/sprite-gen/src/spec.py` — validator (archived).
- `tools/sprite-gen/src/compose.py` (TECH-147) — consumer.
- `docs/isometric-sprite-generator-exploration.md` §8 YAML schema + §10 example.

## 5. Proposed Design

### 5.1 Target behavior (product)

YAML content:

```yaml
id: building_residential_small_v1
class: residential
footprint: [1, 1]
terrain: flat
levels: 2
seed: 42
variants: 4
palette: residential
composition:
  - {type: iso_cube, x0: 0, y0: 0, w: 1, d: 1, h: 16, material: wall_brick_red}
  - {type: iso_cube, x0: 0, y0: 0, w: 1, d: 1, h: 16, material: wall_brick_red, z: 16}
  - {type: iso_prism, x0: 0, y0: 0, w: 1, d: 1, h: 12, pitch: 0.5, axis: ns, material: roof_tile_brown, z: 32}
output:
  dir: out
diffusion:
  enabled: false
```

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- `z:` per composition entry — stacking offset (added to `y0`); implementer in TECH-147 may choose to derive from auto-stack instead — if so, drop `z:` from YAML.
- Material names (`wall_brick_red`, `roof_tile_brown`) — stub until palette JSON lands Stage 1.3.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-14 | 2 cubes stacked vs 1 tall cube | Exercises stacking logic in compose | single cube — skips stacking path |

## 7. Implementation Plan

### Phase 1 — Author + validate

- [ ] Write `specs/building_residential_small.yaml`.
- [ ] Validate via `load_spec` — no errors.
- [ ] Integration smoke via TECH-152 — 4 PNGs emitted.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| YAML parses + validates | Python | `load_spec('specs/building_residential_small.yaml')` returns dict | Ad-hoc in smoke test |
| Render produces 4 PNGs | Python | `pytest tools/sprite-gen/tests/test_render_integration.py` | TECH-152 owns |
| IA / validate chain | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] YAML file present at `tools/sprite-gen/specs/building_residential_small.yaml`.
- [ ] All required fields populated per exploration §8.
- [ ] `render building_residential_small` exits 0 + writes 4 PNGs.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. Exact composition `z:` encoding — auto-stack vs explicit per entry. Implementer of TECH-147 picks; sync YAML here.
