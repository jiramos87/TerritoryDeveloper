---
purpose: "TECH-149 — Sprite-gen render {archetype} CLI command."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-149 — Sprite-gen `render {archetype}` CLI command

> **Issue:** [TECH-149](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Wire CLI entry `python -m sprite_gen render {archetype}` — resolves `specs/{archetype}.yaml`, loads + validates via `load_spec` (archived), calls `compose_sprite` (archived) N times (variants from spec), applies seed-based permutations (material swap within class, prism pitch ±20%), writes `out/{name}_v01.png` … `_v{N:02d}.png`. Part of Stage 1.2 Exit — first end-to-end render invocation.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `python -m sprite_gen render building_residential_small` writes N variant PNGs to `out/`.
2. Variant count from spec `variants:` field; default 1.
3. Seed-based permutation: `random.Random(spec['seed'] + variant_idx)` drives material swap + pitch ±20%.
4. Exit code 0 on success; exit 1 on spec validation / file-not-found failure.
5. `-m sprite_gen` module entry point wired via `src/__main__.py`.

### 2.2 Non-Goals (Out of Scope)

1. `--all` batch mode — lives in TECH-150.
2. `--terrain` flag — lives in TECH-150 (Stage 1.4 actually activates slope path).
3. `promote` / `reject` commands — Stage 1.4.
4. `palette extract` — Stage 1.3.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | As pipeline dev, I want single-command archetype render so that iterating on YAML specs stays fast | `python -m sprite_gen render building_residential_small` produces 4 PNGs in `out/` |

## 4. Current State

### 4.1 Domain behavior

No CLI exists. All renders must be hand-scripted Python. Blocks archetype iteration + Stage 1.2 integration smoke (TECH-152).

### 4.2 Systems map

- `tools/sprite-gen/src/cli.py` (new) — argparse / click dispatcher.
- `tools/sprite-gen/src/__main__.py` (new) — `python -m sprite_gen` entry.
- `tools/sprite-gen/src/spec.py` — loader (archived).
- `tools/sprite-gen/src/compose.py` (TECH-147) — compositor.
- `docs/isometric-sprite-generator-exploration.md` §10 CLI interface — ground truth command table + exit codes.

## 5. Proposed Design

### 5.1 Target behavior (product)

CLI parses `render` subcommand + positional `archetype` arg. Resolves `specs/{archetype}.yaml` relative to `tools/sprite-gen/specs/`. Loads via `load_spec`. Iterates `range(spec['variants'])`, deep-copies spec, applies per-variant permutation, calls `compose_sprite`, saves PNG to `out/{spec['id']}_v{idx+1:02d}.png`. Prints one line per variant written.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- `argparse` w/ subparsers (`render`, future `palette`, `promote`, `reject`).
- Seed permutation helper `apply_variant(spec, seed) → dict` — mutates copy only.
- Material swap scope = within-class (palette-class defined, not cross-class) — resolves to RGB stub in Stage 1.2, real palette in Stage 1.3.
- Prism pitch scale = `pitch * random.uniform(0.8, 1.2)` clamped `[0, 1]`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-14 | argparse vs click | stdlib; no new dep | click — cleaner API, adds dep |

## 7. Implementation Plan

### Phase 1 — CLI dispatcher

- [ ] Create `src/cli.py` w/ argparse subparsers.
- [ ] `src/__main__.py` delegates to `cli.main()`.
- [ ] `render` subcommand: load, permute, compose, save loop.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| CLI writes N variants | Python | `pytest tools/sprite-gen/tests/test_cli.py::test_render_writes_variants` | Temp `out/` via `tmp_path` |
| Exit code 1 on bad archetype | Python | `pytest tools/sprite-gen/tests/test_cli.py::test_missing_spec_exits_1` | `subprocess.run` + returncode |
| IA / validate chain | Node | `npm run validate:all` | Python outside chain until Stage 1.3 |

## 8. Acceptance Criteria

- [ ] `python -m sprite_gen render building_residential_small` exits 0 + writes 4 PNGs.
- [ ] Invalid archetype (missing file) exits 1.
- [ ] Invalid YAML exits 1 w/ `SpecValidationError` message.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.
