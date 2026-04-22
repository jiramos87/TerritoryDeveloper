---
purpose: "TECH-686 — Update UIManager, modals, ZoneSService, BudgetAllocationService to use catalog-backed registry data."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T2.3.3
phases:
  - "Phase 1 — Call-site switch"
  - "Phase 2 — Retire or ifdef JSON path"
---
# TECH-686 — Update callers

> **Issue:** [TECH-686](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Route `SubTypePickerModal`, `UIManager` touch points, `BudgetAllocationService`, and `ZoneSService` through `ZoneSubTypeRegistry` APIs that resolve display strings, cent costs, and icon/prefab data via `GridAssetCatalog` + `asset_id` map (TECH-684/685). Keep Zone S **budget envelope** and treasury rules unchanged. Retire direct reads of JSON `baseCost` on `ZoneSubTypeEntry` where catalog now owns economy fields.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Picker and HUD show the same display names and cent costs as catalog DB for Zone S.
2. `BudgetAllocationService` + `ZoneSService` use registry accessors that pull cents from catalog rows, not legacy JSON field alone.
3. `npm run unity:compile-check` clean; optional `#if CATALOG_JSON_FALLBACK` only if master plan required single-stage rollback (default: no second path).

### 2.2 Non-Goals (Out of Scope)

1. No change to **envelope** math or `MaintenanceRegistry` in this task unless compile requires signature tweak.
2. No `web/` work.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|------------------------|
| 1 | Player | I see correct Zone S costs in picker | Values match published catalog cents |
| 2 | Developer | Services use one data path for Zone S | No duplicate cost constants |

## 4. Current State

### 4.1 Domain behavior

Callers use `ZoneSubTypeEntry.baseCost` / `displayName` from JSON. Catalog snapshot holds economy in DB-shaped fields after Stage 1–2.2.

### 4.2 Systems map

- Grep: `SubTypePickerModal`, `UIManager`, `BudgetAllocationService`, `ZoneSService` under `Assets/Scripts/`
- `ia/specs/economy-system.md` — Zone S, treasury, envelope

## 5. Proposed Design

### 5.1 Target behavior (product)

Zone S modals and services display catalog-backed names and **integer cent** costs per exploration money rules.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Add methods on `ZoneSubTypeRegistry` such as `TryGetDisplayAndCostForSubType(int subTypeId, out string name, out int costCents, …)` that combine map + `GridAssetCatalog` row read. Callers stop reading `entry.baseCost` for authoritative display when catalog path succeeds.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-22 | Single registry façade for UI | Avoids N duplicate catalog lookups in UI | — |

## 7. Implementation Plan

### Phase 1 — Call-site switch

- [ ] Add catalog-backed query APIs on `ZoneSubTypeRegistry` (or thin helper) using TECH-684/685.
- [ ] Update `SubTypePickerModal` + any `UIManager` Zone S paths to use new API.
- [ ] Re-wire `BudgetAllocationService` + `ZoneSService` to new costs for State Service math.

### Phase 2 — JSON path

- [ ] Remove or gate legacy JSON cost reads; keep JSON load only if still needed for non-catalog fields during transition.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| No compile regressions | Node | `npm run unity:compile-check` | Required |
| Zone S values | Unity EditMode | TECH-687 | |

## 8. Acceptance Criteria

- [ ] Picker and services use registry catalog-backed path for author-visible costs/names.
- [ ] No duplicate singletons; no hot-loop `FindObjectOfType`.
- [ ] `Debug.Log` / messages in English.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

`SubTypePickerModal`, `BudgetAllocationService`, and `ZoneSService` (plus any `UIManager` glue discovered by `rg`) use `ZoneSubTypeRegistry` + `GridAssetCatalog` for **display** and **cent** costs for the seven Zone S sub-types; `SubTypePickerModal` label line at `${entry.baseCost}` is replaced. Legacy JSON `baseCost` on `ZoneSubTypeEntry` is not the authoritative display path when catalog data exists. Envelope + `IsStateServiceZone` behavior stays unchanged.

### §Acceptance

- [ ] `Assets/Scripts/Managers/GameManagers/SubTypePickerModal.cs` line building `label.text` does not use raw `` `${entry.baseCost}` `` as sole cost source; uses registry/catalog-backed cents from TECH-684–685 surface.
- [ ] `BudgetAllocationService` + `ZoneSService` use the same cent source for State Service budget math; existing EditMode tests in `Assets/Tests/EditMode/Economy/` pass or updated with new expectations.
- [ ] `cd /Users/javier/bacayo-studio/territory-developer && npm run unity:compile-check` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| budget_allocation | existing suite | pass | `BudgetAllocationServiceTests` |
| zone_s_placement | existing suite | pass | `ZoneSServicePlacementTests` |
| compile_gate | C# | exit 0 | `npm run unity:compile-check` |

### §Examples

`SubTypePickerModal.BuildButtons` today:

```text
label.text = $"{entry.displayName} (${entry.baseCost})";
```

Target: same UX string shape, **integer cents** and display name from catalog row resolved via `registry` (method names from TECH-684/685 deliverables).

### §Mechanical Steps

#### Step 1 — Registry façade for picker label (depends TECH-684 + TECH-685 merged)

**Goal:** One call `registry.TryGetPickerLabelForSubType(int subTypeId, out string line, out int costCents)` (exact name in code) that combines `TryGetAssetIdForSubType` + `GridAssetCatalog` row read for `displayName` + economy cents. Implement in `ZoneSubTypeRegistry.cs` in the same PR chain before caller edits when possible.

**Edits:** New method body in `Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs` after map + `Catalog` property exist. Gate: `unity:compile-check`.

**STOP:** Re-open TECH-685 map task when `asset_id` lookup returns false.

**MCP hints:** `backlog_issue` (TECH-686), `glossary_lookup` (Zone S).

#### Step 2 — `SubTypePickerModal` label

**Goal:** Picker button uses façade from Step 1 instead of `entry.baseCost` for the dollar portion.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/SubTypePickerModal.cs` — **before**:
  ```
                var label = btnObj.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = $"{entry.displayName} (${entry.baseCost})";
  ```
  **after** (call registry façade; keep behavior when lookup fails — `LogError` + fallback to `entry.displayName` only with `[TECH-686]`-tagged message):
  ```
                var label = btnObj.GetComponentInChildren<Text>();
                if (label != null)
                {
                    if (registry != null && registry.TryGetPickerLabelForSubType(entry.id, out string line, out int cents))
                        label.text = line;
                    else
                    {
                        Debug.LogError("[SubTypePickerModal] [TECH-686] catalog-backed label failed for subType " + entry.id);
                        label.text = $"{entry.displayName} (—)";
                    }
                }
  ```
  (Adjust method name `TryGetPickerLabelForSubType` to match Step 1 implementation; implementer unifies string format with prior `"${...}"` UX.)

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run unity:compile-check
```

**STOP:** On compile error in façade signature, re-read Step 1 method signature in `ZoneSubTypeRegistry.cs`.

**MCP hints:** `plan_digest_resolve_anchor` on `label.text = $\"{entry.displayName}`.

#### Step 3 — `BudgetAllocationService` + `ZoneSService` cent path

**Goal:** `rg "baseCost|ZoneSubTypeEntry"` under `Assets/Scripts/Managers/GameManagers/BudgetAllocationService.cs` and `ZoneSService.cs`; replace authoritative cost reads for Zone S with registry/catalog cents matching Step 1. Preserve envelope array shape.

**Edits:** Per-site search-replace with unique 5-line `**before**` / `**after**` blocks per match (author iterates `rg` output at implement time). Gate: `npm run unity:compile-check` + `npx dotnet test` N/A (Unity) — run `Assets/Tests/EditMode/Economy/BudgetAllocationServiceTests.cs` + `ZoneSServicePlacementTests.cs` in Editor test runner or project test batch.

**STOP:** Failing `BudgetAllocationServiceTests` → diff expected cent arrays against catalog fixture; update test constants from seed, not from old JSON `baseCost`.

**MCP hints:** `invariants_summary` (economy + unity).

## Open Questions (resolve before / during implementation)

`prefabPath` on `ZoneSubTypeEntry` may stay JSON-sourced for prefab until catalog bind covers it; **cent costs and display names** use the catalog path from TECH-686 scope.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
