---
name: sprite-gen-calibrate-axis
purpose: >-
  Drive one sprite-gen calibration axis cycle end-to-end — probe specs → render → inspect → agent
  visual → user verdict → data/code/spec fix loop → docs append → axis-scoped commit.
audience: agent
loaded_by: "skill:sprite-gen-calibrate-axis"
slices_via: none
description: >-
  Drive a single sprite-gen calibration axis through the full cycle — author probe specs under
  `tools/sprite-gen/specs/`, render + mechanically inspect, agent visual pass, human verdict gate,
  dead-plumb / data / code fix loop, docs append to `docs/sprite-gen-calibration.md`, axis-scoped
  commit. Triggers: "/sprite-gen-calibrate-axis {AXIS}", "calibrate axis <name>", "run calibration
  axis", "next axis".
phases: []
triggers:
  - /sprite-gen-calibrate-axis {AXIS}
  - calibrate axis <name>
  - run calibration axis
  - next axis
argument_hint: <AXIS_NAME> [VALUES] [VARIANT_COUNT]
model: inherit
tools_role: custom
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# Sprite-gen calibrate axis

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: code, commit messages, human-facing product-term option labels inside the verdict gate block.

Wraps per-spec review ([`sprite-gen-visual-review`](../sprite-gen-visual-review/SKILL.md)) into full axis cycle. Visual-review = 1 spec; this skill = N probes across 1 vary dimension + verdict + fix + doc + commit.

## Posture

User-invoked only. Calibration-phase tool — token-expensive (agent-eye reads N variants × M probes). Never auto-fire from `/ship-stage` / CI / post-render hooks.

Invoke only when user says:
- `/sprite-gen-calibrate-axis {AXIS}`
- "calibrate axis <name>"
- "run calibration on <axis>"
- "next axis" (resume from last doc `Next candidates` list)

## Args

| Arg | Default | Meaning |
|-----|---------|---------|
| `AXIS_NAME` | required | `footprint_ratio`, `roof_pitch`, `palette`, `terrain`, `size`, or free label — lands in spec id + doc section |
| `VALUES` | spec from doc `next candidates` | Sweep values (comma-separated, e.g. `0.20,0.35,0.50,0.65,0.80,0.95`) — geometry axis = one spec per value; palette/value axis = single spec with vary block |
| `VARIANT_COUNT` | 1 geometry / 6 palette | Per-probe variant count |
| `BASE_SPEC` | `demo_palette_variation` (palette) / `demo_footprint_050` (geometry) | Starting spec to clone — overwrite only axis-under-test fields |

## Phase 0 — Preflight

1. Read `docs/sprite-gen-calibration.md` tail — find last axis section + its `Next calibration candidates` list. Confirms sweep shape + identifies untouched axes.
2. MCP slice check if relevant (skip if axis is purely artistic): `glossary_lookup sprite-gen`, `router_for_task`.
3. Read `tools/sprite-gen/src/compose.py` `sample_variant` + `_axis_scope` + `_set_deep` + `_apply_vary_ground` — verify the axis field is plumbed through `variants.vary.*`. Record path shape.
4. Confirm axis + values with user via product-term poll block when inputs missing ([`agent-human-polling.md`](../../rules/agent-human-polling.md)). Do NOT proceed on guesses.

## Phase 1 — Author probe specs

Two modes:

**Geometry axis** (footprint_ratio, pitch, size, h_px, etc.) — one spec per sweep value. Naming: `tools/sprite-gen/specs/demo_<axis>_<NNN>.yaml` where `NNN` encodes the value (e.g. `footprint_095` for 0.95). Each spec sets `variants.count: 1`, `seed_scope: geometry`, seed constant across probes, all non-axis fields held fixed + inline-commented as such.

**Palette/value axis** (material values, hue, ground) — single spec with `variants.count: N`, `seed_scope: palette`, `variants.vary.<path>.values` or `{min,max}`. Naming: `tools/sprite-gen/specs/demo_<axis>_variation.yaml`.

Each spec starts with a comment block:
- axis name + hypothesis
- held-constant fields
- expected plumbing path (e.g. `variants.vary.wall.material.values → composition[*].material` via `_COMPOSITION_ROLE_KEYS`)
- expected visual failure mode (what "fail" looks like)

## Phase 2 — Render + inspect (batch)

For each probe spec:
```bash
cd tools/sprite-gen
python3 -m src render <spec_id>
```

Batch inspect across all rendered PNGs for the axis:
```bash
python3 -m src inspect out/<name1>_v*.png out/<name2>_v*.png ...
```

Capture JSON: per-variant `containment`, `overflow_px`, `pixel_count`, `palette.{sig,building_sig,ground_sig}`; aggregate `variation.{pixel_spread_pct, bbox_shift_px, palette.distinct_*_sigs, min_*_jaccard, verdict}`.

Assert expected PNG count written. Halt on renderer exceptions — do NOT silently retry.

## Phase 3 — Agent visual inline

Read each PNG via the `Read` tool (inline image). Note per-variant: dominant wall color, roof color, ground color, readable silhouette. Caveman one-liner per variant.

## Phase 4 — Dead-plumb detection

Before user handoff — if aggregate `variation.verdict == static` OR `distinct_*_sigs` lower than expected for the sweep size, run dead-plumb trace:

```python
# Inline driver — do NOT add to src/.
import yaml
from src.compose import sample_variant
with open('specs/<spec>.yaml') as f: spec = yaml.safe_load(f)
for i in range(N):
    s = sample_variant(spec, i)
    # Print the axis-under-test field per variant.
```

Two signatures:

| Sampled values distinct? | Rendered palettes distinct? | Root cause |
|---|---|---|
| No | No | Plumbing bug — `_axis_scope` mis-classifies or `_set_deep` routes to unread key |
| Yes | No | Data bug — palette JSON material triplet mislabeled (e.g. `concrete` encoded as green RGB), OR renderer ignores the field |
| Yes | Yes | Plumbing + data OK; user just wants narrower range |

Report diagnosis as caveman line before verdict block. Do NOT silently fix — user picks branch.

## Phase 5 — User verdict gate

Emit block per [`agent-human-polling.md`](../../rules/agent-human-polling.md). Product-term option labels; tooling details in metric summary only.

**Always absolute paths.** `tools/sprite-gen/out/` is gitignored ([feedback memory](../../../.claude-personal/) — user's IDE hides it). Include `open /abs/path/*.png` Preview gallery cmd.

```
SPRITE CALIBRATION AXIS — {AXIS_NAME}

Metrics:
  probes: N     variants total: M
  containment pass: X/M     overflow: Y/M
  pixel_spread: P%     bbox_shift: Q px
  palette distinct_building_sigs: A/M     min_building_jaccard: B

Per-variant visual (caveman):
  <spec_id>_v01: /abs/.../v01.png   <one-line obs>
  ...

Diagnosis (if static): <plumbing|data|not-static>

Gallery:
  open /abs/path/out/<prefix>*.png

Pick one:
  A) range accepted — document + commit
  B) narrow range — re-probe with tighter values
  C) data fix — palette / preset mislabel found (e.g. concrete → green)
  D) code fix — plumbing dead (wall/roof/foundation vary not routed)
  E) extend range — probe wider / more values
```

Wait. Never auto-commit.

## Phase 6 — Fix loop

| Verdict | Action |
|---|---|
| `A` accept | Proceed Phase 7. |
| `B` narrow | Edit probe specs in place (Edit tool). Re-run Phase 2..5. |
| `C` data fix | Propose triplet to user (hex / RGB). On confirm, edit `tools/sprite-gen/palettes/<class>.json` (or preset). Re-run Phase 2..5. |
| `D` code fix | Propose minimal diff (e.g. add role keys to `_COMPOSITION_ROLE_KEYS`, extend `_axis_scope` classifier). On confirm, edit `src/compose.py`. Re-run Phase 2..5. Never refactor beyond the fix. |
| `E` extend | Add probes for new values (Phase 1..5). |

Fix loop always returns to Phase 2. Doc + commit only after `A`.

## Phase 7 — Docs append

Append new section to `docs/sprite-gen-calibration.md` following established shape:

```
## Axis N — <AXIS_NAME> (<short descriptor>)

**Date:** YYYY-MM-DD
**Spec(s):** tools/sprite-gen/specs/demo_<axis>_*.yaml
**Surface:** <src/compose.py:function or palettes/<class>.json>

### Hypothesis
<caveman — what does varying this axis reveal>

### Probes
| Value | Variants | Containment | Palette distinct | Notes |
|---|---|---|---|---|
...

### Findings
- <plumbing fix if any — commit sha or pending>
- <data fix if any>
- <semantic banding observations — e.g. ratio 0.20..0.30 reads as shed, 0.55..0.75 reads as large residential>

### Final accepted range / values
<caveman>

### Next calibration candidates
- <untouched axis>
- <refinement on current axis>
```

Increment axis number monotonically from last section. Update root bullet list if one exists.

## Phase 8 — Commit (axis-scoped)

```bash
git status --short
```

Stage ONLY axis-scoped files:
- `tools/sprite-gen/specs/demo_<axis>_*.yaml`
- `tools/sprite-gen/src/compose.py` (if code fix)
- `tools/sprite-gen/src/inspect.py` (if metric extension)
- `tools/sprite-gen/palettes/<class>.json` (if data fix)
- `docs/sprite-gen-calibration.md`

Unrelated dirty files (docs/progress.html, ia/backlog-archive, project specs from prior sessions) stay unstaged. Check `git status` before commit.

Commit message:
```
feat(sprite-gen-cal-<axis>): <one-line summary>

- probes: <list>
- <plumbing|data> fix: <one-line>
- verdict: <accepted range / values>
```

NEVER `git push`. User pushes manually.

## Guardrails

- User-invoked only. Never offer unsolicited.
- Always absolute paths for rendered PNGs ([feedback memory](../../../.claude-personal/projects/-Users-javier-bacayo-studio-territory-developer/memory/feedback_sprite_gen_paths.md)) + `open` cmd.
- Axis-scoped commits only — never stage unrelated dirty tree.
- Dead-plumb: always classify (plumbing vs data vs OK) before user gate — never silently fix.
- Code fixes = minimal targeted edit. No refactoring, no drive-by cleanup.
- Never mutate `ia/state/id-counter.json` / BACKLOG / spec ids from this skill.
- Never auto-commit. Never push.
- Multi-tile slot specs (`tiled-row-N`, etc.) = out of scope — log + stop.
- One axis per invocation. Do not chain axes unless user says "next axis".

## Triggers

- `/sprite-gen-calibrate-axis {AXIS_NAME}`
- `calibrate axis <name>`
- `run calibration on <axis>`
- `next axis` (resume from doc `Next candidates` — prompt user to pick if ambiguous)
- `sprite-gen calibrate <axis>`

## Related

- Sub-skill: [`sprite-gen-visual-review`](../sprite-gen-visual-review/SKILL.md) — per-spec render+inspect loop (optional subcall for individual probes).
- Doc: [`docs/sprite-gen-calibration.md`](../../../docs/sprite-gen-calibration.md) — running calibration log (this skill's write target).
- Tool: [`tools/sprite-gen/src/compose.py`](../../../tools/sprite-gen/src/compose.py) — `sample_variant`, `_axis_scope`, `_set_deep`, `_apply_vary_ground`.
- Tool: [`tools/sprite-gen/src/inspect.py`](../../../tools/sprite-gen/src/inspect.py) — geometric + palette-variation metrics.
- Rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md) — verdict gate shape.
- Rule: [`ia/rules/agent-output-caveman.md`](../../rules/agent-output-caveman.md) — prose style.
- Memory: `feedback_sprite_gen_paths.md` — absolute-path handoff requirement.

## Changelog

- 2026-04-24 — Initial draft. Authored after Axis 1..4 manual runs + Axis 3 palette plumbing discovery (third dead-plumb `_axis_scope` mis-classified composition-role `material` as geometry, masked by mislabeled `concrete` → green RGB triplet in `palettes/residential.json`). Process codified: probe specs → batch render + inspect → dead-plumb triage (sampled-values × rendered-sigs 2×2 matrix) → product-term verdict → fix-loop → axis-scoped commit.
