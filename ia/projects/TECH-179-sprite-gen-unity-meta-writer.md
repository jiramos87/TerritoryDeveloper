---
purpose: "TECH-179 — Unity .meta writer + curate.promote() helper."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-179 — Unity `.meta` writer + `curate.promote`

> **Issue:** [TECH-179](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Author `src/unity_meta.py` — `write_meta(png_path, canvas_h) → str` emits Unity `.meta` YAML w/ guid (uuid4), textureImporter settings: PPU=64, `spritePivot=(0.5, 16/canvas_h)`, filterMode=Point, textureCompression=None, spriteMode=Single. Author `src/curate.py` `promote(src_png, dest_name)` — copies PNG to `Assets/Sprites/Generated/{dest_name}.png`, calls `write_meta`, writes `.meta` sibling. Guards against Unity auto-import resetting PPU/pivot defaults (breaks grid alignment — sprite-gen master plan §Do not).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `src/unity_meta.py` `write_meta(png_path, canvas_h) → str`: Unity-compatible YAML `.meta`.
2. `src/curate.py` `promote(src_png, dest_name)`: copies PNG + writes sibling `.meta`.
3. `.meta` opens in Unity without console warnings; PPU=64, pivot `(0.5, 16/h)`, filter=Point, compression=None.

### 2.2 Non-Goals

1. CLI dispatch (TECH-174).
2. Aseprite round-trip (TECH-176..177).

## 4. Current State

### 4.2 Systems map

- `tools/sprite-gen/src/unity_meta.py` (new).
- `tools/sprite-gen/src/curate.py` (new).
- Dest: `Assets/Sprites/Generated/{name}.png` + `.meta`.

## 7. Implementation Plan

### Phase 1 — Meta + promote

- [ ] `write_meta(png_path, canvas_h)` — YAML string w/ uuid4 guid.
- [ ] `curate.promote(src, dest_name)` — copy + write sibling `.meta`.
- [ ] Unit test: promote a fixture PNG; assert both files exist, `.meta` parses as YAML, PPU=64.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Meta YAML valid | pytest | `pytest tools/sprite-gen/tests/test_unity_meta.py` | |
| Lane validate | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] `promote(src, dest_name)` produces PNG + `.meta` in `Assets/Sprites/Generated/`.
- [ ] `.meta` carries PPU=64, pivot `(0.5, 16/h)`, filter=Point, compression=None.
- [ ] `npm run validate:all` green.

## Open Questions

1. None — tooling only.
