---
purpose: "TECH-543 — Power-plant contributor audit + adapter."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T5.4"
---
# TECH-543 — Power-plant contributor audit + adapter

> **Issue:** [TECH-543](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Audit **`ProcessMonthlyMaintenance`** / **`ComputeMonthlyUtilityMaintenanceCost`** for **utility building** (**power plant**) upkeep and either add **`PowerPlantMaintenanceContributor`** or log deferral in **§6 Decision Log** if scope moves to Bucket 4.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Grep/read current monthly tick for power plant charges.
2. If present: adapter analogous to roads with stable contributor id.
3. If absent: Decision Log entry + no code churn.

### 2.2 Non-Goals (Out of Scope)

1. New utility systems beyond maintenance contributor scope.

## 4. Current State

### 4.1 Domain behavior

**Monthly maintenance** includes **`ComputeMonthlyUtilityMaintenanceCost`** = **`CityStats.GetRegisteredPowerPlantCount()`** × **`maintenanceCostPerPowerPlant`** when rates positive — adapter expected unless explicitly deferred.

### 4.2 Systems map

- `EconomyManager.ProcessMonthlyMaintenance`
- Power / utility managers as referenced in codebase audit

## 5. Proposed Design

### 5.1 Target behavior (product)

If utility line exists: contributor id e.g. **`power-aggregate`**, **`GetSubTypeId() == -1`**, cost matches **`ComputeMonthlyUtilityMaintenanceCost`**. If program defers utilities: Decision Log only — no adapter in this task.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Mirror **TECH-542** pattern: **`IMaintenanceContributor`** + **`EconomyManager.Awake`** registration.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Utility line present — adapter implemented | `ComputeMonthlyUtilityMaintenanceCost` confirmed: `plantCount × rate`. Adapter `PowerPlantMaintenanceContributor` added, id `power-aggregate` | Bucket 4 deferral rejected |

## 7. Implementation Plan

### Phase 1 — Audit + decision

- [ ] Record findings in **§6 Decision Log**.

### Phase 2 — Optional adapter

- [ ] Implement only if audit finds existing power-plant maintenance line.

## 10. Lessons Learned

- _TBD at stage closeout._

## §Plan Author

### §Audit Notes

- **Audit result (2026-04 codebase):** **`ComputeMonthlyUtilityMaintenanceCost`** + **`ProcessMonthlyMaintenance`** include power plants — **Phase 2 adapter expected** unless product overrides.
- Risk: plant count source must stay **`CityStats.GetRegisteredPowerPlantCount()`** for parity with **`monthly maintenance`** glossary.
- If skipping: explicit Decision Log row + master-plan Notes so **TECH-541** removal does not drop utility cost.

### §Examples

| `GetRegisteredPowerPlantCount()` | `maintenanceCostPerPowerPlant` | Expected cost |
|----------------------------------|-------------------------------|---------------|
| 0 | 10 | 0 |
| 2 | 15 | 30 |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| utility_adapter_matches_legacy | stub plant count | equals legacy helper | **TECH-544** |
| skip_path_documented | N/A | Decision Log + no `PowerPlantMaintenanceContributor.cs` | manual review |

### §Acceptance

- [ ] **§6 Decision Log** records audit verdict (implement adapter vs defer).
- [ ] If implement: **`PowerPlantMaintenanceContributor`** matches **`ComputeMonthlyUtilityMaintenanceCost`**; stable contributor id; registered in **`EconomyManager.Awake`**.
- [ ] If defer: no orphan references in **TECH-541** / **TECH-544** — **TECH-541** still removes duplicate hardcoded path only when adapters cover all lines.

### §Findings

- Current **`EconomyManager`** implements utility maintenance — default path = add adapter; defer only with explicit sign-off.

## Open Questions (resolve before / during implementation)

1. Canonical contributor id string: **`power-aggregate`** vs **`power-plant-aggregate`**? (Pick one; use in **TECH-544** assertions.)
