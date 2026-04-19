---
purpose: "TECH-180 — promote / reject CLI commands."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-180 — `promote` / `reject` CLI

> **Issue:** [TECH-180](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Extend `src/cli.py`: `promote out/X.png --as name` calls `curate.promote()`, asserts dest + `.meta` exist; `reject {archetype}` globs `out/{archetype}_*.png`, deletes all. Integration test: promote then reject same file — `Assets/Sprites/Generated/` has promoted file; `out/` clean post-reject.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `cli.py promote out/X.png --as {name}` → calls `curate.promote()`; exits 0 on success.
2. `cli.py reject {archetype}` → deletes `out/{archetype}_*.png`; exits 0.
3. Integration test covers promote → reject round-trip.

### 2.2 Non-Goals

1. Aseprite round-trip (`--edit` flag lands in TECH-177).

## 4. Current State

### 4.2 Systems map

- `tools/sprite-gen/src/cli.py` (edit — add subcommands).
- Uses `curate.promote` (TECH-179).

## 7. Implementation Plan

### Phase 1 — CLI + integration test

- [ ] Add `promote` + `reject` subcommands to `cli.py` arg parser.
- [ ] Wire to `curate.promote` + `glob+unlink`.
- [ ] Integration test: promote fixture, assert dest exists, reject, assert `out/` cleaned.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| CLI integration | pytest | `pytest tools/sprite-gen/tests/test_cli_curate.py` | |
| Lane validate | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `promote out/X.png --as name` lands PNG + `.meta` in `Assets/Sprites/Generated/`.
- [ ] `reject {archetype}` deletes matching `out/` files.
- [ ] `npm run validate:all` green.

## Open Questions

1. None — tooling only.
