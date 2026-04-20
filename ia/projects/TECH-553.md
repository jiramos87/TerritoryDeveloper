---
purpose: "TECH-553 — S zoning button in `UIManager.ToolbarChrome`."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T7.1"
---
# TECH-553 — S zoning button in `UIManager.ToolbarChrome`

> **Issue:** [TECH-553](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Add fourth zoning control to **toolbar chrome** so player selects **Zone S** (`StateServiceLightZoning`) and enters placement flow that pairs with sub-type picker (TECH-555). Placeholder art until final icon.

## 2. Goals and Non-Goals

### 2.1 Goals

1. S button in zoning cluster beside R/C/I; placeholder “S” glyph; `UiTheme` spacing parity.
2. Click sets `ZoneManager.activeZoneType = ZoneType.StateServiceLightZoning` and invokes sub-type picker entry (hook coordinates with TECH-555).
3. No regression to existing R/C/I toolbar or hotkeys.

### 2.2 Non-Goals (Out of Scope)

1. Final icon asset — placeholder only.
2. Bond or budget panel — Stage 7 siblings.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Choose S zoning from toolbar like R/C/I | S visible; click selects S channel |
| 2 | Developer | Extend chrome without new singletons | Inspector / `FindObjectOfType` patterns only |

## 4. Current State

### 4.1 Domain behavior

**Zone S** uses same toolbar affordance as RCI; MVP locks light zoning enum entry for toolbar (exploration).

### 4.2 Systems map

- `UIManager.ToolbarChrome` — zoning button row
- `ZoneManager` — `activeZoneType`
- `ZoneType.StateServiceLightZoning`
- `UiTheme` / layout constants for toolbar

### 4.3 Implementation investigation notes (optional)

Confirm exact file path for `ToolbarChrome` partial under `UIManager`.

## 5. Proposed Design

### 5.1 Target behavior (product)

Player clicks S → enters S placement mode → picker opens (TECH-555) before first place.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Mirror R/C/I button wiring: serialized refs or lazy find for `ZoneManager`; onClick sets enum then calls into `UIManager` or placement coordinator to show picker.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-20 | Placeholder S glyph | Art deferred | Empty button (rejected — unreadable) |

## 7. Implementation Plan

### Phase 1 — Toolbar control

- [ ] Locate R/C/I button creation / wiring in `ToolbarChrome`.
- [ ] Add S button with same layout hooks + placeholder label/icon.

### Phase 2 — Click behavior

- [ ] OnClick: `activeZoneType = StateServiceLightZoning` + trigger picker (stub callback until TECH-555 lands — feature-flag or no-op with log acceptable for ordering within Stage).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile + no regression | Node | `npm run unity:compile-check` | After C# edit |
| Manual toolbar | Human | Play Mode — click S | Picker when TECH-555 merged |

## 8. Acceptance Criteria

- [ ] S button visible; theme spacing matches siblings.
- [ ] Click sets `StateServiceLightZoning` and opens picker path per Stage Exit.
- [ ] R/C/I unchanged.
- [ ] `npm run unity:compile-check` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: **ToolbarChrome** already dense — mitigation: reuse exact cell size from R button.
- Ambiguity: picker trigger timing if TECH-555 not merged same branch — resolution: expose `Action` or `UIManager` method stub callable from S button; document dependency in PR.
- Invariant: no new singletons — use existing `UIManager` / `ZoneManager` discovery patterns.

### §Examples

| Input | Expected | Notes |
|-------|----------|-------|
| Click S | `activeZoneType == StateServiceLightZoning` | Before grid click |
| Click R after S | R type restored | No sticky S state |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| toolbar_s_sets_zone_type | Click S in Play | enum = StateServiceLightZoning | Manual / future UI test |
| rci_unchanged | Click R,C,I | original types | Manual |

### §Acceptance

- [ ] Placeholder S visible; layout parity.
- [ ] Click sets zone type + picker hook live when TECH-555 present.
- [ ] Compile green.

## Open Questions (resolve before / during implementation)

1. Exact API name on `UIManager` for “open sub-type picker” — implementing agent defines; align with TECH-555.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
