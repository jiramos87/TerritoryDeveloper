### Stage 2 — Geometry MVP / Composition + YAML Schema + CLI Skeleton (Layer 2)


**Status:** Final (6 tasks archived as **TECH-147** through **TECH-152**; closed 2026-04-15)

**Objectives:** Wire primitives into a compose layer that reads YAML archetype specs and stacks primitives onto a canvas buffer. Implement CLI `render {archetype}` + `render --all` commands with seed-based variant permutation. Ship first archetype spec `building_residential_small.yaml` and validate round-trip to `out/`.

**Exit:**

- `compose.py` `compose_sprite(spec_dict) → PIL.Image` stacks all primitives from spec `composition:` list in order
- `spec.py` validates required YAML fields (id, class, footprint, terrain, composition, palette, output); exits with code 1 on invalid
- `cli.py render building_residential_small` writes `out/building_residential_small_v01.png` … `_v04.png` at correct canvas size
- `cli.py render --all` discovers all `specs/*.yaml` and renders all without crash
- Seed-based variant permutation applies material swap within class, window pattern shift, prism pitch ±20%
- `specs/building_residential_small.yaml` checked in with 4 variants, flat terrain, palette=residential
- Phase 1 — compose.py (Layer 2) + YAML spec loader/validator.
- Phase 2 — CLI render + render --all commands.
- Phase 3 — First archetype spec + integration smoke test.

**Tasks:**


| Task | Name                   | Issue        | Status          | Intent                                                                                                                                                                                                                                                                                                                                            |
| ---- | ---------------------- | ------------ | --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T2.1 | Compose layer          | **TECH-147** | Done (archived) | `src/compose.py` — `compose_sprite(spec: dict) → PIL.Image`: create canvas via `canvas_size(fx, fy, extra_h=0)`, iterate `composition:` list, dispatch each entry to matching primitive (iso_cube / iso_prism), return composited image; `extra_h` derived from tallest primitive stack                                                           |
| T2.2 | YAML spec loader       | **TECH-148** | Done (archived) | `src/spec.py` — `load_spec(path) → dict`: load YAML + validate required keys (id, class, footprint, terrain, composition, palette, output); `SpecValidationError` raised on missing/malformed fields; CLI catches and exits with code 1 (per §10 exit codes)                                                                                      |
| T2.3 | Render CLI command     | **TECH-149** | Done (archived) | `src/cli.py` — `render {archetype}` command: resolve `specs/{archetype}.yaml`, load + validate spec, call `compose_sprite` N times (variants count from spec), apply seed-based permutations (material swap within class, prism pitch ±20%), write `out/{name}_v01.png` … `_v{N:02d}.png`                                                         |
| T2.4 | Render --all command   | **TECH-150** | Done (archived) | `src/cli.py` — `render --all` command: glob `specs/*.yaml`, iterate, call `render {archetype}` logic per spec; collect errors per spec (exit 0 only if all succeeded, else print failed archetypes + exit 1); `--terrain {slope_id}` CLI flag overrides spec `terrain` field (matches §10 CLI interface)                                          |
| T2.5 | First archetype YAML   | **TECH-151** | Done            | `specs/building_residential_small.yaml` — first archetype: `id: building_residential_small_v1`, `class: residential`, `footprint: [1,1]`, `terrain: flat`, `levels: 2`, `seed: 42`, `variants: 4`; composition: iso_cube×2 (wall_brick_red) + iso_prism (roof_tile_brown, pitch=0.5, axis=ns); `palette: residential`; `diffusion.enabled: false` |
| T2.6 | Integration smoke test | **TECH-152** | Done            | Integration smoke: run `python -m sprite_gen render building_residential_small` in CI-friendly subprocess; assert `out/building_residential_small_v01.png` exists + PIL open succeeds + image size == (64, 64); assert 4 variant files written; no exception raised                                                                               |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
