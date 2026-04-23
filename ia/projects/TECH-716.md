---
purpose: "TECH-716 — Add optional accent_dark / accent_light palette keys + seed two materials."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.4.2
---
# TECH-716 — Palette JSON accent_dark / accent_light keys

> **Issue:** [TECH-716](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend the palette JSON schema with optional `accent_dark` / `accent_light` keys per material. Loader surfaces them (`None` if absent). Seed concrete values for `grass_flat` + `pavement` in the active palette so `iso_ground_noise` (TECH-717) has at least two consumers at Stage 6.4 close.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Palette loader surfaces `accent_dark` / `accent_light` or `None`.
2. `grass_flat` + `pavement` seeded in active palette JSON with both keys.
3. Existing palette entries untouched.
4. Absence → None unit-tested.

### 2.2 Non-Goals

1. Noise primitive itself — TECH-717.
2. Composer jitter / auto-insert — TECH-718.
3. Full palette re-audit for all materials.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Reference palette accent colours from primitives | `palette.accent_dark(material)` returns tuple or None |
| 2 | Spec author | Texture `grass_flat` tiles without editing palette | Default palette ships with usable accents |

## 4. Current State

### 4.1 Domain behavior

Palette JSON has per-material `ramp` (dark→light) only. No slot for accent colours used by scatter primitives.

### 4.2 Systems map

- `tools/sprite-gen/palettes/*.json` (schema + active palette file).
- `tools/sprite-gen/src/palette.py` (loader).

### 4.3 Implementation investigation notes

Active palette file lives at `tools/sprite-gen/palettes/default.json` (verify during Phase 1). Loader currently parses `ramp` as tuple; extend parser to optionally read `accent_dark` / `accent_light` as tuples if present.

## 5. Proposed Design

### 5.1 Target behavior

```json
{
  "grass_flat": {
    "ramp": [[34,110,58],[55,140,78],[82,170,98]],
    "accent_dark": [22,84,42],
    "accent_light": [132,200,140]
  }
}
```

### 5.2 Architecture / implementation

- Loader reads keys with `.get("accent_dark")` / `.get("accent_light")`.
- Expose on palette entry struct as `accent_dark: tuple | None`.
- Helper `palette.accent(material, "dark" | "light")` for primitive use.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Two accent slots (dark + light) | Matches typical scatter-noise vocabulary — specks are either darker or lighter than base | Single accent — rejected, too flat |
| 2026-04-23 | Seed only 2 materials | Smallest surface that proves the end-to-end path | Seed all — rejected, scope creep |

## 7. Implementation Plan

### Phase 1 — Loader surfaces optional keys

### Phase 2 — Seed `grass_flat` + `pavement` with concrete accent values

### Phase 3 — Unit test for absence → None

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Loader surface | Python | `pytest tests/test_palette.py::test_accent_present -q` | grass_flat both keys present |
| Absence → None | Python | `pytest tests/test_palette.py::test_accent_absent -q` | Material without accents returns None |
| Existing palette unchanged | Python | `pytest tests/ -q` | Legacy tests green |

## 8. Acceptance Criteria

- [ ] Loader surfaces `accent_dark` / `accent_light` or None.
- [ ] `grass_flat` + `pavement` seeded with both keys.
- [ ] Existing palette entries unchanged (diff-reviewed).
- [ ] Unit test for absence path.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Carry optional keys as `None` at the loader — avoids scattering `.get(..., None)` at every call site.

## §Plan Digest

### §Goal

Palette schema gains two optional accent slots per material so scatter primitives have a colour vocabulary; seed two materials so Stage 6.4 ships with live consumers.

### §Acceptance

- [ ] `palette.py` parses `accent_dark` / `accent_light` as optional RGB tuples
- [ ] Material without accents → loader returns `None`
- [ ] `grass_flat` + `pavement` in active palette JSON have both keys populated with sensible values
- [ ] `pytest tools/sprite-gen/tests/test_palette.py -q` green
- [ ] No diff on existing materials' keys

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_accent_present | `grass_flat` entry | `accent_dark`, `accent_light` tuples non-None | pytest |
| test_accent_absent | material without keys | both None | pytest |
| test_existing_materials_unchanged | palette JSON load | ramps match pre-change baseline | pytest |

### §Examples

```python
# tools/sprite-gen/src/palette.py (excerpt)
@dataclass
class MaterialEntry:
    ramp: list[tuple[int, int, int]]
    accent_dark: tuple[int, int, int] | None = None
    accent_light: tuple[int, int, int] | None = None

def _load_material(raw: dict) -> MaterialEntry:
    return MaterialEntry(
        ramp=[tuple(c) for c in raw["ramp"]],
        accent_dark=tuple(raw["accent_dark"]) if raw.get("accent_dark") else None,
        accent_light=tuple(raw["accent_light"]) if raw.get("accent_light") else None,
    )
```

### §Mechanical Steps

#### Step 1 — Loader surfaces optional keys

**Edits:**

- `tools/sprite-gen/src/palette.py` — extend dataclass + loader.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_palette.py::test_accent_absent -q
```

#### Step 2 — Seed `grass_flat` + `pavement`

**Edits:**

- `tools/sprite-gen/palettes/default.json` (or active palette) — add `accent_dark` + `accent_light` to `grass_flat` and `pavement`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_palette.py::test_accent_present -q
```

#### Step 3 — Full regression

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Active palette file path — confirm under `tools/sprite-gen/palettes/`. **Resolution:** grep `from_json` call sites in Phase 1.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
