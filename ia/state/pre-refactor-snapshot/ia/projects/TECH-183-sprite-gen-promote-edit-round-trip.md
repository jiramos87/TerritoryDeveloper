---
purpose: "TECH-183 — promote --edit round-trip: .aseprite → flattened PNG + .meta."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-183 — `promote --edit` round-trip

> **Issue:** [TECH-183](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Extend `src/curate.py` `promote(src, dest_name, edit=False)` — when `src.suffix == '.aseprite'` and `edit=True`, shell-out `{aseprite_bin} --batch {src} --save-as {tmp}.png` (subprocess, check returncode), then run existing PNG promote pipeline on `{tmp}.png`; cleanup tmp. Add `--edit` flag to `cli.py promote`. Integration test: render --layered → modify one layer pixel via PIL → promote --edit → assert flattened PNG + `.meta` exist in `Assets/Sprites/Generated/`; modified pixel present in output.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `curate.promote(src, dest_name, edit=False)` — when `.aseprite + edit` → Aseprite flatten → promote PNG.
2. `cli.py promote ... --edit` flag.
3. Integration test: round-trip layered edit lands in Unity dest w/ modified pixel.

### 2.2 Non-Goals

1. Aseprite bin resolution (TECH-181 prereq).
2. Layered emit (TECH-182 prereq).

## 4. Current State

### 4.2 Systems map

- `tools/sprite-gen/src/curate.py` (edit — `edit=False` param).
- `tools/sprite-gen/src/cli.py` (edit — `--edit` flag on `promote`).
- Calls `find_aseprite_bin()` (TECH-181).
- Consumes `.aseprite` from TECH-182 emission.

## 7. Implementation Plan

### Phase 1 — Shell-out + test

- [ ] Extend `promote` signature w/ `edit=False`.
- [ ] Branch on `.aseprite + edit` → `subprocess.run([bin, '--batch', src, '--save-as', tmp_png], check=True)`.
- [ ] Run PNG promote pipeline on `tmp_png`; cleanup on success + failure (try/finally).
- [ ] `cli.py promote --edit` flag plumbed through.
- [ ] Integration test: render --layered fixture, mutate a pixel via `py_aseprite` or PIL per layer, promote --edit, assert dest PNG + `.meta` exist + mutated pixel present.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Round-trip integration | pytest | `pytest tools/sprite-gen/tests/test_promote_edit.py` | Skips when Aseprite bin absent |
| Lane validate | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `promote out/X.aseprite --as name --edit` produces flattened PNG + `.meta` in `Assets/Sprites/Generated/`.
- [ ] Modified layer pixels survive round-trip.
- [ ] Missing Aseprite bin → exit 4 w/ install hint (per TECH-181).
- [ ] `npm run validate:all` green.

## Open Questions

1. None — tooling only.
