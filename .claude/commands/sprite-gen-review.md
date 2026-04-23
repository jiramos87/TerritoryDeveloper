---
description: Regenerate + visually review sprite-gen PNG variants for a spec. Calibration-phase only — user-invoked, not auto-fired. Dispatches to the sprite-gen-visual-review skill.
argument-hint: <SPEC_ID> [VARIANTS]
---

Run the `sprite-gen-visual-review` skill against `tools/sprite-gen/specs/$1.yaml`.

Posture: **calibration-phase only**. Agent-eye review is token-expensive; this command runs only when the user explicitly asks. It is not a per-generation gate and must not be chained into `/ship-stage`, CI, or automated loops. Once the generator is tuned, this command retires.

Args:
- `$1` = SPEC_ID (stem under `tools/sprite-gen/specs/`)
- `$2` = optional VARIANTS override (1..8; default = value in spec)

Phases:
1. Preflight — read spec + referenced preset.
2. Regenerate — `python3 -m src render $1` (after clean of prior `out/{name}_v*.png`).
3. Mechanical check — `python3 -m src inspect out/{name}_v*.png` → footprint-only tile-diamond containment (bottom bbox corners; roofs rising above tile top apex are expected) + inter-variant variation (pixel-count spread, bbox shift).
4. Human gate — emit product-term pick block (A/B/C/D) + absolute paths.

Does NOT commit. Does NOT edit `src/*.py` (bug-fix path = `/project-new` BUG row + fix).
