---
name: sprite-gen-visual-review
purpose: >-
  Regenerate sprite-gen PNG variants from a spec or preset, auto-review each PNG in-agent
  (tile-diamond containment + visible variation), then hand off to a human verification gate with
  absolute paths and a caveman verdict table.
audience: agent
loaded_by: "skill:sprite-gen-visual-review"
slices_via: none
description: >-
  Regenerate sprite-gen PNG variants from a spec under `tools/sprite-gen/specs/` (or a preset via a
  spec that references it), auto-review each PNG in-agent for tile-diamond containment + visible
  variation, and hand off to a human verification gate with absolute paths + per-variant verdict.
  Triggers: "sprite-gen visual review", "/sprite-gen-review {spec-id}", "review sprite variants",
  "verify building inside tile", "check vary block output".
phases: []
triggers:
  - sprite-gen visual review
  - /sprite-gen-review {spec-id}
  - review sprite variants
  - verify building inside tile
  - check vary block output
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

Start: read the spec file under `tools/sprite-gen/specs/{SPEC_ID}.yaml` + the referenced preset under `tools/sprite-gen/presets/{PRESET}.yaml` (if any). Target output: `tools/sprite-gen/out/{spec.output.name}_v{NN}.png`.

# Sprite-gen visual review loop

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: code, commit messages, human-facing product-term option labels inside the verification gate block.

Not a replacement for art director review. Covers mechanical containment + variation visibility only; final "looks right for the game" verdict = human.

## Invocation posture — calibration-phase only

User-invoked on demand during the generator-tuning phase. NOT a per-generation gate. Do not fire this skill automatically after `render`, inside `/ship-stage`, or as a CI-style hook — agent-eye review is token-expensive. Once the generator is tuned (compose.py + presets stable), this skill retires to archive.

Trigger only when the user explicitly says:
- `/sprite-gen-review {SPEC_ID}`
- "sprite-gen visual review"
- "review sprite variants for {SPEC_ID}"
- "run the calibration review on {SPEC_ID}"

Agent should not suggest or offer to run this skill unsolicited.

## Scope

Covers single-tile archetype specs under `tools/sprite-gen/specs/*.yaml`. Multi-tile slot-grammar specs (`tiled-row-N`, `tiled-column-N`) = follow-up — this skill reviews one tile at a time.

## Args

| Arg | Default | Meaning |
|-----|---------|---------|
| `SPEC_ID` | required | Stem of `tools/sprite-gen/specs/{SPEC_ID}.yaml` |
| `VARIANTS` | spec value (capped 8) | Override `variants.count` + `output.variants` for review runs |
| `CLEAN` | true | Delete prior `out/{name}_v*.png` before regenerating |

## Phase 0 — Preflight

1. Read the spec. Assert `preset` / `footprint_ratio` / `variants.vary` shape known to `src/spec.py`. Abort if spec missing.
2. Read preset if referenced. Note declared `footprint`, `ground`, building ratio defaults.
3. If `VARIANTS < spec.variants.count`, rewrite `variants.count` + `output.variants` in-place (Edit), note the override in the handoff.

## Phase 1 — Regenerate

```bash
cd tools/sprite-gen
rm -f out/{spec.output.name}_v*.png    # when CLEAN=true
python3 -m src render {SPEC_ID}
```

Capture stdout `wrote out/...` lines. Assert count == expected `variants`.

## Phase 2 — Auto-review (per PNG)

For each `out/{name}_v{NN}.png`:

1. Read the PNG via the `Read` tool (Claude Code renders the image inline).
2. Walk the image pixels programmatically via `python3 -m src inspect {path}` — returns JSON with `building_bbox`, `tile_diamond`, `bbox_norm_corners`, `containment`, `overflow_px`, `pixel_count`. Batch form `python3 -m src inspect out/{name}_v*.png` returns per-variant reports + aggregated variation metrics.
3. Footprint-only containment: only the bottom two bbox corners (sw, se) must lie inside the tile diamond. Roof / height pixels rising above the tile top apex are expected (3D buildings) and do NOT count as overflow. The helper already applies this rule — trust its verdict (`pass` / `overflow` / `empty`).
4. Variation check: compare building pixel-count + bbox between v01..vN. At least 2 variants must differ by > 5% pixel area OR > 4 px bbox shift → vary block exercised.
5. Record verdict per variant: `pass` / `overflow` / `static`.

When programmatic bbox extraction is blocked (no pillow helper yet), fall back to agent-eye visual confirmation: read each PNG, state containment + variation in one caveman line per variant.

## Phase 3 — Human verification gate

Emit a fenced block matching [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md) shape — product terms, not tooling jargon. Example:

```
SPRITE VISUAL REVIEW — {SPEC_ID}

Paths:
  v01: /abs/path/..._v01.png   auto: pass
  v02: /abs/path/..._v02.png   auto: pass
  v03: /abs/path/..._v03.png   auto: overflow (east corner clipped)
  v04: /abs/path/..._v04.png   auto: pass

Pick one:
  A) all good — ship
  B) v03 off — regenerate with new seed
  C) v03 off — fix spec (narrow padding range, stricter align)
  D) all static — vary block not firing, investigate
```

Wait for human pick. Never auto-commit.

## Phase 4 — Handoff

| Outcome | Next |
|---------|------|
| `A` (all good) | Stop. If the agent's initiating ask was a sprite-gen fix, point at the commit-ready diff; skill does not commit. |
| `B` (regenerate) | Bump `seed` in spec (Edit), re-run Phase 1..3. |
| `C` (fix spec) | Emit a tuple list (spec field + proposed value) under `## Spec Fix Plan`, wait for human pick before editing. |
| `D` (static) | Print `cli.py` + `compose.py` `sample_variant` wiring check. Abort review; open a BACKLOG BUG issue via `/project-new` — do NOT silently pass. |

## Guardrails

- Never delete `out/*.png` outside `{name}_v*` — preserve prior archetype renders.
- Never edit `tools/sprite-gen/src/*.py` from this skill — fix-path = BUG issue + `/project-new`.
- Never mutate the id counter or BACKLOG.md directly.
- Diamond containment math assumes 1×1 tile. Multi-tile specs → log "out of scope" + stop.
- If the PNG is empty / transparent-only, treat as `overflow` (likely off-canvas) — surface to human.

## Triggers

- `sprite-gen visual review {SPEC_ID}`
- `/sprite-gen-review {SPEC_ID}`
- `review sprite variants`
- `verify building stays inside tile for {SPEC_ID}`
- `check vary block firing for {SPEC_ID}`

## Related

- Tool: [`tools/sprite-gen/`](../../../tools/sprite-gen/) — cli + compose + spec loader.
- Rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md) — gate block shape.
- Rule: [`ia/rules/agent-output-caveman.md`](../../rules/agent-output-caveman.md) — prose style.

## Changelog

- 2026-04-23 — Initial draft. Authored after iso-space + diamond-clamp fix in `resolve_building_box` landed; first visual-verified run on `demo_position_padding` 4-variant batch passed containment + variation checks.
- 2026-04-23 — Marked as calibration-phase only. Agent must not auto-invoke (token-expensive). `inspect.py` helper wired as `python -m src inspect`; footprint-only containment rule (bottom bbox corners only) codified per user feedback.
