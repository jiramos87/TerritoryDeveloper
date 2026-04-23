---
purpose: "TECH-733 — seed preset strip_mall_with_parking.yaml: wide commercial + pavement ground + vary over facade/ground."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.6.4
---
# TECH-733 — Seed preset — `strip_mall_with_parking`

> **Issue:** [TECH-733](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Second seed preset. Fully-valid sprite-gen spec (minus `id` / `output.name`) for a wide commercial strip: wide footprint, pavement ground (Stage 6.4 accent keys from TECH-716), and `vary:` over facade + ground.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Renders cleanly with `preset: strip_mall_with_parking` and no author overrides.
2. Pavement ground uses the Stage 6.4 accent keys seeded in TECH-716.
3. `vary:` covers ≥2 axes (facade, ground).

### 2.2 Non-Goals

1. Loader / merge rule — TECH-730/731.
2. Other seed presets — TECH-732/734.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Bootstrap a commercial strip sprite | `preset:` + id + output → renders |
| 2 | Curator | Distinguish strip-mall visuals from residential | Pavement ground + wider building immediately legible |
| 3 | Repo guardian | Preset exercises Stage 6.4 accent keys | TECH-716 accent keys consumed |

## 4. Current State

### 4.1 Domain behavior

No commercial preset exists; commercial archetype (`building_commercial.yaml`) renders a single narrow block.

### 4.2 Systems map

- `tools/sprite-gen/presets/strip_mall_with_parking.yaml` — new file.
- Consumes: TECH-716 pavement accent keys; TECH-715 ground object form.

### 4.3 Implementation investigation notes

Scale `footprint` and `building.bbox` so the sprite reads as a wide strip even at 1×1 canvas (long and short facade proportions). Pavement accent keys enable striped parking look without needing Stage 10's `iso_paved_parking` primitive.

## 5. Proposed Design

### 5.1 Target behavior

```yaml
# tools/sprite-gen/presets/strip_mall_with_parking.yaml
archetype: commercial
footprint: [1, 1]
building:
  facade: { material: facade_commercial_cream }
  roof:   { material: roof_flat_grey }
  width_scale: 1.4   # visually wider than default
ground:
  material: pavement
  accent_keys: [pavement_stripe_yellow]
  texture: on
vary:
  facade:
    values: [facade_commercial_cream, facade_commercial_teal, facade_commercial_sand]
  ground:
    hue_jitter: 0.02
    value_jitter: 0.04
```

### 5.2 Architecture / implementation

- Static YAML only.
- Validated by TECH-735 tests + dev run.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | 1×1 footprint with `width_scale` rather than 2×2 | Stage 6.6 doesn't yet need the 2×2 unlock (Stage 9) | 2×2 footprint — rejected, scope creep |
| 2026-04-23 | Pavement accent keys via TECH-716 | Stripe look without new primitives | Paint stripes at composer — rejected, Stage 10 work |

## 7. Implementation Plan

### Phase 1 — Scaffold wide-footprint commercial spec

### Phase 2 — Pavement ground with accent keys + texture on

### Phase 3 — `vary:` over facade + ground

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Preset renders | Python | `pytest tests/test_preset_system.py::test_preset_strip_mall_renders -q` | TECH-735 |
| Accent keys present | Static | manual inspection | TECH-716 keys used |
| `vary:` covers 2 axes | Static | manual inspection | — |

## 8. Acceptance Criteria

- [ ] Renders cleanly with `preset: strip_mall_with_parking` and no author overrides.
- [ ] Pavement ground uses Stage 6.4 accent keys (TECH-716 seeded material).
- [ ] `vary:` covers ≥2 axes (facade, ground).
- [ ] Unit test covers preset-only render path (in TECH-735).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Commercial presets read noticeably "commercial" only when ground + building signal together — a residential-shaped building on pavement looks ambiguous.

## §Plan Digest

### §Goal

Ship a wide commercial-strip preset with pavement ground and 2+ axis variation so authors can bootstrap a strip-mall sprite in three lines.

### §Acceptance

- [ ] `tools/sprite-gen/presets/strip_mall_with_parking.yaml` exists and parses
- [ ] Author spec `{preset: strip_mall_with_parking, id: X, output.name: Y}` renders non-empty PNG
- [ ] `ground.material: pavement` with at least one TECH-716 accent key
- [ ] `vary:` declares `facade` and `ground` axes
- [ ] Same preset + same seed → byte-identical PNG twice in a row

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_preset_strip_mall_renders | preset + seed | non-empty PNG | pytest |
| test_preset_strip_mall_vary_axes | loaded preset | facade + ground axes | pytest |
| test_preset_strip_mall_accent_keys | loaded preset | TECH-716 accent key present | pytest |

### §Examples

See §5.1 above for the complete preset YAML.

### §Mechanical Steps

#### Step 1 — Scaffold commercial base

**Edits:**

- `tools/sprite-gen/presets/strip_mall_with_parking.yaml` — archetype + wide footprint + `width_scale`.

**Gate:**

```bash
ls tools/sprite-gen/presets/strip_mall_with_parking.yaml
```

#### Step 2 — Pavement ground

**Edits:**

- Same file — `ground: {material: pavement, accent_keys: [...], texture: on}`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "
from src.spec import _load_preset
p = _load_preset('strip_mall_with_parking')
assert p['ground']['material'] == 'pavement'
assert p['ground']['accent_keys']
print('pavement OK')
"
```

#### Step 3 — `vary:` block

**Edits:**

- Same file — `vary: {facade: {...}, ground: {...}}`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "
from src.spec import _load_preset
p = _load_preset('strip_mall_with_parking')
assert {'facade','ground'} <= set(p['vary'].keys())
print('vary OK')
"
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Which TECH-716 accent keys actually exist in repo after Stage 6.4? **Resolution:** use `pavement_stripe_yellow` as the canonical one; if TECH-716 seeds a different name, update preset at merge time.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
