---
purpose: "TECH-569 — MiniMapController S palette."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T8.5"
---
# TECH-569 — `MiniMapController` S palette

> **Issue:** [TECH-569](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Mini-map distinguishes **Zone S** cells from R/C/I using one shared tint for all **StateService** **ZoneType** values (exploration N5).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Extend `MiniMapController` color lookup for `StateService*` **ZoneType** values.
2. Single purple (or chosen **UiTheme** token) for all S; no per-sub-type split.
3. R/C/I colors unchanged.

### 2.2 Non-Goals (Out of Scope)

1. Per-sub-type palette (post-MVP).
2. Mini-map iconography overhaul.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I see S zones as distinct from R/C/I on mini-map | Color differs |
| 2 | Developer | Enum switch covers all six building + zoning StateService types | Grep ZoneType |

## 4. Current State

### 4.1 Domain behavior

Mini-map maps zone type → color; new **ZoneType** enums added in Stage 1.

### 4.2 Systems map

- `MiniMapController.cs`, `Zone.ZoneType`, `UiTheme` if applicable

### 4.3 Implementation investigation notes (optional)

Follow existing R/C/I color table pattern; add branch or dictionary entries.

## 5. Proposed Design

### 5.1 Target behavior (product)

All S cells render same hue; zoning + building variants share color.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Centralize `IsStateServiceZone`-style check or explicit enum cases returning shared `Color`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Single S color | N5 MVP lock | Seven colors (rejected) |

## 7. Implementation Plan

### Phase 1 — Enum → color

- [ ] Locate color resolution for zone types in `MiniMapController`.
- [ ] Map `StateServiceLightBuilding` … `StateServiceHeavyZoning` to one color constant.

### Phase 2 — Sanity

- [ ] Visual check: S distinct from R/C/I; legend if any.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile | Unity | `npm run unity:compile-check` | |

## 8. Acceptance Criteria

- [ ] `MiniMapController` maps all **StateService** **ZoneType** values to one S color
- [ ] R/C/I palette unchanged; no per-sub-type S tint

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: Missing one of six **StateService** enum values → cell falls through to default/wrong color. Mitigation: grep **ZoneType** enum + **IsStateServiceZone** helper if available; table-driven map.
- **N5** — one color for all S; no **subTypeId** branch in minimap.
- No **GridManager** expansion — color lookup only.

### §Examples

| ZoneType | Color |
|----------|-------|
| **StateServiceLightBuilding** … **StateServiceHeavyZoning** | Single S tint |
| R / C / I | Unchanged |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| enum_coverage | Each StateService type | Returns S color | EditMode |
| rci_unchanged | R zone | Prior R color | visual / assert |

### §Acceptance

- [ ] All **StateService** types map to one S color; RCI unchanged

### §Findings

- None at author time.

## Open Questions (resolve before / during implementation)

1. Exact color token — follow **UiTheme** if mini-map already uses theme; else single constant with TODO for art pass.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
