---
purpose: "TECH-181 — Aseprite binary resolver with env/config/platform probes."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-181 — Aseprite binary resolver

> **Issue:** [TECH-181](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Author `src/aseprite_bin.py` — `find_aseprite_bin() → Path` resolves in order: `$ASEPRITE_BIN` env var → `tools/sprite-gen/config.toml` `[aseprite] bin` → platform default probes (macOS: `/Applications/Aseprite.app/Contents/MacOS/aseprite`, then `~/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite`). Raises `AsepriteBinNotFoundError` on miss — caught by CLI, exit 4 w/ install hint. Tier 2 editor integration prereq.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `find_aseprite_bin()` resolves via env → config.toml → platform probes.
2. `AsepriteBinNotFoundError` on miss; CLI catches → exit 4 w/ install hint text.
3. Unit test mocks env var + filesystem — asserts probe order.

### 2.2 Non-Goals

1. Aseprite layered emit (TECH-176).
2. `promote --edit` round-trip (TECH-177).

## 4. Current State

### 4.2 Systems map

- `tools/sprite-gen/src/aseprite_bin.py` (new).
- `tools/sprite-gen/config.toml` (new or existing — `[aseprite] bin = "..."`).
- CLI error handling in `cli.py` (edit — catch `AsepriteBinNotFoundError`).

## 7. Implementation Plan

### Phase 1 — Resolver + error

- [ ] `find_aseprite_bin()` w/ probe order.
- [ ] `AsepriteBinNotFoundError` exception class.
- [ ] CLI exit 4 w/ install hint (docs link or brew formula suggestion).
- [ ] Unit test: mock env + fs; assert correct binary chosen per scenario.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Resolver unit | pytest | `pytest tools/sprite-gen/tests/test_aseprite_bin.py` | |
| Lane validate | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] Resolver probes in correct order.
- [ ] Missing binary → CLI exit 4 w/ hint.
- [ ] `npm run validate:all` green.

## Open Questions

1. None — tooling only.
