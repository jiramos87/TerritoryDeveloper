---
description: Drive a single sprite-gen calibration axis through the full cycle — author probe specs under `tools/sprite-gen/specs/`, render + mechanically inspect, agent visual pass, human verdict gate, dead-plumb / data / code fix loop, docs append to `docs/sprite-gen-calibration.md`, axis-scoped commit. Triggers: "/sprite-gen-calibrate-axis {AXIS}", "calibrate axis <name>", "run calibration axis", "next axis".
argument-hint: "<AXIS_NAME> [VALUES] [VARIANT_COUNT]"
---

# /sprite-gen-calibrate-axis — Drive one sprite-gen calibration axis cycle end-to-end — probe specs → render → inspect → agent visual → user verdict → data/code/spec fix loop → docs append → axis-scoped commit.

Drive `$ARGUMENTS` via the [`sprite-gen-calibrate-axis`](../agents/sprite-gen-calibrate-axis.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /sprite-gen-calibrate-axis {AXIS}
- calibrate axis <name>
- run calibration axis
- next axis
<!-- skill-tools:body-override -->

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
