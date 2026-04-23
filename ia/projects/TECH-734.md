---
purpose: "TECH-734 — seed preset row_houses_3x.yaml: tiled-row-3 slot + shared grass ground + per-row vary."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.6.5
---
# TECH-734 — Seed preset — `row_houses_3x`

> **Issue:** [TECH-734](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Third seed preset. Demonstrates the Stage 9 addendum's parametric `tiled-row-N` slot (TECH-744) with three small houses sharing a grass ground and a per-row `vary:` block that palette-differentiates each house. Renders cleanly only once the Stage 9 addendum (TECH-744) lands — documented in depends_on.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Renders cleanly with `preset: row_houses_3x` once Stage 9 addendum lands.
2. Uses `tiled-row-3` slot (parametric slot grammar from TECH-744).
3. Shared grass ground across the row; `vary:` applies per-house.

### 2.2 Non-Goals

1. Implementing the `tiled-row-N` slot — TECH-744.
2. Loader / merge rule — TECH-730/731.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Bootstrap a 3-house row sprite | `preset: row_houses_3x` + id + output renders |
| 2 | Curator | Diverse houses on one sprite | Each of 3 houses picks a different facade color |
| 3 | Stage 9 reviewer | Live consumer of `tiled-row-3` | Preset exercises slot grammar on merge |

## 4. Current State

### 4.1 Domain behavior

Stage 9 addendum (TECH-744) is un-reserved at Stage 6.6 filing time; `row_houses_3x` blocks on it by design.

### 4.2 Systems map

- `tools/sprite-gen/presets/row_houses_3x.yaml` — new file.
- Depends on: TECH-744 (parametric slot), TECH-715 (ground object form), TECH-730/731 (preset loader + vary merge).

### 4.3 Implementation investigation notes

Shared ground means one ground block above the `buildings:` list (Stage 9 schema). Per-house variation via `vary:` with axis values picked from a 3-color palette. File can be written now; test activation waits on TECH-744.

## 5. Proposed Design

### 5.1 Target behavior

```yaml
# tools/sprite-gen/presets/row_houses_3x.yaml
archetype: residential_row
footprint: [2, 2]
ground:
  material: grass
  texture: on
buildings:
  - slot: tiled-row-3
    facade: { material: facade_warm_beige }
    roof:   { material: roof_terracotta }
vary:
  facade:
    values: [facade_warm_beige, facade_cool_grey, facade_ivory]
    strategy: per_tile
  roof:
    values: [roof_terracotta, roof_slate, roof_cedar]
    strategy: per_tile
```

### 5.2 Architecture / implementation

- Static YAML only.
- `vary.*.strategy: per_tile` is the tie-in to `tiled-row-3` — each tile samples independently (TECH-744 responsibility).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Parametric `tiled-row-3` (not hard-coded) | Future row_houses_4x / row_houses_5x presets reuse grammar | Bespoke `tiled-row-3` slot — rejected, Stage 9 addendum went parametric |
| 2026-04-23 | `vary.*.strategy: per_tile` | Explicit that axis samples per-building, not per-sprite | Implicit per-tile — rejected, collides with global `vary:` semantics |

## 7. Implementation Plan

### Phase 1 — Scaffold spec with `tiled-row-3` slot

### Phase 2 — Shared grass ground

### Phase 3 — Per-house `vary:` with `strategy: per_tile`

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Preset renders (post-TECH-744) | Python | `pytest tests/test_preset_system.py::test_preset_row_houses_renders -q` | Blocked on TECH-744 |
| 3 resolved footprints | Python | `pytest tests/test_slots.py::test_tiled_row_3 -q` | TECH-744 |
| `vary.strategy: per_tile` present | Static | manual inspection | — |

## 8. Acceptance Criteria

- [ ] Renders cleanly with `preset: row_houses_3x` once Stage 9 addendum (TECH-744) lands.
- [ ] Uses `tiled-row-3` slot (parametric slot grammar).
- [ ] Shared grass ground across row; `vary:` applies per-house.
- [ ] Test asserts 3 resolved footprints via Stage 9 slot grammar (in TECH-744).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Forward-dependent presets are valid scaffolding — writing the file now surfaces Stage 9 schema ambiguities earlier than waiting.

## §Plan Digest

### §Goal

Ship a 3-house row preset riding Stage 9's parametric `tiled-row-3` slot, so the preset library covers all three footprint classes (single / wide / row) at Stage 6.6 exit.

### §Acceptance

- [ ] `tools/sprite-gen/presets/row_houses_3x.yaml` exists and parses
- [ ] Preset references `slot: tiled-row-3` under `buildings:`
- [ ] `ground:` is a single shared block at preset level (not per-building)
- [ ] `vary:` declares at least two axes with `strategy: per_tile`
- [ ] Once TECH-744 lands: preset renders non-empty PNG with 3 distinguishable house footprints

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_preset_row_houses_parses | preset file | `_load_preset` returns dict with `tiled-row-3` | pytest |
| test_preset_row_houses_renders | preset + seed (post-TECH-744) | non-empty PNG | pytest |
| test_preset_row_houses_per_tile_variation | loaded preset | each axis has `strategy: per_tile` | pytest |

### §Examples

See §5.1 above for the complete preset YAML.

### §Mechanical Steps

#### Step 1 — Scaffold with `tiled-row-3`

**Edits:**

- `tools/sprite-gen/presets/row_houses_3x.yaml` — archetype + `footprint: [2, 2]` + `buildings: [{slot: tiled-row-3, ...}]`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "
import yaml
p = yaml.safe_load(open('presets/row_houses_3x.yaml'))
assert p['buildings'][0]['slot'] == 'tiled-row-3'
print('slot OK')
"
```

#### Step 2 — Shared grass ground

**Edits:**

- Same file — single top-level `ground: {material: grass, texture: on}`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "
import yaml
p = yaml.safe_load(open('presets/row_houses_3x.yaml'))
assert p['ground']['material'] == 'grass'
print('ground OK')
"
```

#### Step 3 — Per-house `vary:`

**Edits:**

- Same file — `vary: {facade: {values, strategy: per_tile}, roof: {values, strategy: per_tile}}`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "
import yaml
p = yaml.safe_load(open('presets/row_houses_3x.yaml'))
for axis in ('facade','roof'):
    assert p['vary'][axis].get('strategy') == 'per_tile'
print('per_tile OK')
"
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Exact name of parametric slot — `tiled-row-N` or `row-N`? **Resolution:** use `tiled-row-3` per TECH-744 spec; if the addendum renames, update preset at merge time.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
