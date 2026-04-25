Run the `sprite-gen-calibrate-axis` skill for axis `$1`.

Posture: **calibration-phase only**. Token-expensive (agent-eye reads N probes × M variants). Never auto-fire.

Args:
- `$1` = AXIS_NAME (e.g. `footprint_ratio`, `roof_pitch`, `palette`, `terrain`, `size`)
- `$2` = optional VALUES (comma-separated; default from doc `Next candidates`)
- `$3` = optional VARIANT_COUNT (1 geometry / 6 palette default)

Phases:
0. Preflight — read `docs/sprite-gen-calibration.md` tail + `compose.py` plumbing path.
1. Author probe specs under `tools/sprite-gen/specs/demo_<axis>_*.yaml`.
2. Render + batch inspect.
3. Agent visual inline read.
4. Dead-plumb triage (sampled-values × rendered-sigs 2×2 matrix).
5. Human verdict gate (absolute paths + `open` cmd).
6. Fix loop (narrow / data / code / extend) — always re-renders.
7. Doc append to `docs/sprite-gen-calibration.md`.
8. Axis-scoped commit — never stages unrelated dirty tree.

Does NOT push. Does NOT chain to other axes unless user says "next axis".
