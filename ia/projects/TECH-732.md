---
purpose: "TECH-732 — seed preset suburban_house_with_yard.yaml: residential_small + grass ground + 3-axis vary."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.6.3
---
# TECH-732 — Seed preset — `suburban_house_with_yard`

> **Issue:** [TECH-732](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

First seed preset. Fully-valid sprite-gen spec (minus `id` / `output.name`) that renders a residential-small house with a grass yard (ground texture on) and `vary:` over roof / facade / ground. Gives Stage 6.6 a live consumer on day one.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Renders cleanly with `preset: suburban_house_with_yard` and no author overrides.
2. `vary:` covers ≥3 axes (roof, facade, ground).
3. Ground uses the Stage 6.4 object form (material + texture on).

### 2.2 Non-Goals

1. Preset loader — TECH-730/731.
2. Other seed presets — TECH-733 (strip mall), TECH-734 (row houses).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Bootstrap a suburban house sprite in three lines | `preset: suburban_house_with_yard` + `id` + `output.name` renders |
| 2 | Curator | See meaningful variation in one preset family | 3 axes producing distinguishable variants |
| 3 | Test harness | Assert preset renders byte-stable under fixed seed | Determinism test passes (TECH-735) |

## 4. Current State

### 4.1 Domain behavior

`tools/sprite-gen/presets/` does not yet exist.

### 4.2 Systems map

- `tools/sprite-gen/presets/suburban_house_with_yard.yaml` — new file.
- Base template: `tools/sprite-gen/archetypes/building_residential_small.yaml` (existing).

### 4.3 Implementation investigation notes

Strip `id` / `output.name` from the template so the author must supply them. Add grass ground per Stage 6.4's object form. Include `vary:` over roof color, facade color, ground hue jitter.

## 5. Proposed Design

### 5.1 Target behavior

```yaml
# tools/sprite-gen/presets/suburban_house_with_yard.yaml
archetype: residential_small
footprint: [1, 1]
building:
  facade: { material: facade_warm_beige }
  roof:   { material: roof_terracotta }
ground:
  material: grass
  texture: on
vary:
  roof:
    values: [roof_terracotta, roof_slate, roof_cedar]
  facade:
    values: [facade_warm_beige, facade_cool_grey, facade_ivory]
  ground:
    hue_jitter: 0.03
```

Author invocation:

```yaml
preset: suburban_house_with_yard
id: my_house_07
output:
  name: my_house_07.png
```

### 5.2 Architecture / implementation

- Static YAML only; no code.
- Validated indirectly by TECH-735 tests + dev run.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Ground texture on by default | Matches "suburban" visual intent | Texture off — rejected, flat yard reads as generic |
| 2026-04-23 | 3 `vary:` axes | Balances diversity vs. determinism | 1 axis — rejected, too samey; 5 axes — rejected, combinatorial explosion |

## 7. Implementation Plan

### Phase 1 — Copy base from `building_residential_small.yaml`

### Phase 2 — Strip `id` / `output.name`

### Phase 3 — Add grass ground + `vary:` block

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Preset renders | Python | `pytest tests/test_preset_system.py::test_preset_suburban_renders -q` | TECH-735 |
| `vary:` covers 3 axes | Static | manual inspection | — |
| Ground object form | Static | manual inspection | Matches Stage 6.4 schema |

## 8. Acceptance Criteria

- [ ] Renders cleanly with `preset: suburban_house_with_yard` and no author overrides.
- [ ] `vary:` covers ≥3 axes (roof, facade, ground).
- [ ] Ground uses Stage 6.4 object form (material + texture).
- [ ] Unit test covers preset-only render path (in TECH-735).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Preset seeds double as teaching examples; terseness + density of good choices beats exhaustive commenting.

## §Plan Digest

### §Goal

Ship a fully-valid residential-small house preset with grass yard + 3-axis variation so authors can bootstrap a sprite with a single `preset:` line.

### §Acceptance

- [ ] `tools/sprite-gen/presets/suburban_house_with_yard.yaml` exists and parses
- [ ] Author spec `{preset: suburban_house_with_yard, id: X, output.name: Y}` renders non-empty PNG
- [ ] `vary:` block declares `roof`, `facade`, `ground` axes at minimum
- [ ] `ground` is an object (not scalar) with `material` + `texture` keys
- [ ] Same preset + same seed → byte-identical PNG twice in a row

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_preset_suburban_renders | preset + seed | non-empty PNG | pytest |
| test_preset_suburban_vary_axes | loaded preset | 3 axes declared | pytest |
| test_preset_suburban_deterministic | 2 renders, same seed | byte-identical | pytest |

### §Examples

See §5.1 above for the complete preset YAML.

### §Mechanical Steps

#### Step 1 — Copy template base

**Edits:**

- `tools/sprite-gen/presets/suburban_house_with_yard.yaml` — start from `archetypes/building_residential_small.yaml`.

**Gate:**

```bash
ls tools/sprite-gen/presets/suburban_house_with_yard.yaml
```

#### Step 2 — Strip identity fields

**Edits:**

- Same file — remove `id:` and `output.name:` keys.

**Gate:**

```bash
grep -E "^(id|output):" tools/sprite-gen/presets/suburban_house_with_yard.yaml || echo "stripped OK"
```

#### Step 3 — Add grass ground + vary block

**Edits:**

- Same file — add `ground: {material: grass, texture: on}` + `vary:` block.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "
from src.spec import load_spec, _load_preset
spec = _load_preset('suburban_house_with_yard')
assert set(spec['vary'].keys()) >= {'roof','facade','ground'}
assert isinstance(spec['ground'], dict)
print('preset shape OK')
"
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Does `residential_small` archetype already support `vary.ground.hue_jitter`? **Resolution:** TECH-718 (Stage 6.4) seeded the hue_jitter axis into the composer — preset uses it directly.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
