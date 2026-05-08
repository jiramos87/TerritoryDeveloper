---
purpose: "TECH-182 — layered .aseprite emission with named per-face layers."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-182 — Layered `.aseprite` emission

> **Issue:** [TECH-182](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Author `src/aseprite_io.py` — `write_layered_aseprite(dest_path, layers: dict[str, PIL.Image], canvas_size)` writes `.aseprite` via `py_aseprite` (add to `requirements.txt`) with named layers stacked as `foundation`, `east`, `south`, `top`; transparent alpha preserved per layer. Patch `src/compose.py` to split per-face buffers when `layered=True`. Add `--layered` flag to `cli.py render`. Composer always co-emits flat PNG so non-Aseprite users stay unblocked.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `write_layered_aseprite` emits valid `.aseprite` w/ 4 named layers (foundation only when non-flat).
2. `compose.py` supports `layered=True` → per-face buffers.
3. `cli.py render --layered {archetype}` co-emits `.aseprite` + flat PNG.
4. `py_aseprite` pinned in `requirements.txt`.

### 2.2 Non-Goals

1. `promote --edit` round-trip (TECH-177).
2. Aseprite bin resolution (TECH-181).

## 4. Current State

### 4.2 Systems map

- `tools/sprite-gen/src/aseprite_io.py` (new).
- `tools/sprite-gen/src/compose.py` (edit — per-face buffers).
- `tools/sprite-gen/src/cli.py` (edit — `--layered` flag).
- `tools/sprite-gen/requirements.txt` (edit — pin `py_aseprite`).

## 7. Implementation Plan

### Phase 1 — IO + composer split

- [ ] Pin `py_aseprite` in requirements.
- [ ] `write_layered_aseprite` wrapper over `py_aseprite`.
- [ ] Refactor `compose.py` primitive render to keep per-face buffers when `layered=True`.
- [ ] CLI `--layered` flag; co-emit flat PNG.
- [ ] Integration test: open `.aseprite` via `py_aseprite` reader, assert layer names + count.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Layered emit | pytest | `pytest tools/sprite-gen/tests/test_aseprite_io.py` | |
| Lane validate | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `render --layered` emits `.aseprite` + flat PNG.
- [ ] Layers: `foundation` (when non-flat), `east`, `south`, `top`.
- [ ] `npm run validate:all` green.

## Open Questions

1. None — tooling only.
