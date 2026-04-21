---
purpose: "TECH-333 — IUtilityContributor + IUtilityConsumer interfaces."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/utilities-master-plan.md"
task_key: "T1.1.3"
---
# TECH-333 — IUtilityContributor + IUtilityConsumer interfaces

> **Issue:** [TECH-333](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-21

## 1. Summary

Read-only interface contracts separating producers from consumers in the utilities system. `IUtilityContributor` exposes `Kind`, `ProductionRate`, `Scale`; `IUtilityConsumer` exposes `Kind`, `ConsumptionRate`, `Scale`. Consumed by `UtilityPoolService.TickPools` (Stage 1.2) and `UtilityContributorRegistry` (Stage 1.3). No concrete implementations in this task — those land Step 2 (infrastructure buildings) + Step 4 (landmarks hook).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Create `Assets/Scripts/Data/Utilities/IUtilityContributor.cs` — read-only: `UtilityKind Kind`, `float ProductionRate`, `ScaleTag Scale`.
2. Create `Assets/Scripts/Data/Utilities/IUtilityConsumer.cs` — read-only: `UtilityKind Kind`, `float ConsumptionRate`, `ScaleTag Scale`.
3. XML doc each property citing role (producer summation vs consumer summation in pool tick).
4. No default implementations, no abstract base classes — plain interfaces.

### 2.2 Non-Goals

1. Concrete `InfrastructureContributor` (Step 2).
2. `RegisterWithMultiplier` wrapper (Stage 1.3).
3. Serialization support.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Stage 1.3 registry author | As author of `UtilityContributorRegistry`, I want contributor / consumer interfaces ready so `Register` / `RegisterConsumer` signatures compile. | Interfaces exist w/ stated properties. |

## 4. Current State

### 4.1 Domain behavior

No contributor / consumer contracts exist yet.

### 4.2 Systems map

- New files under `Assets/Scripts/Data/Utilities/`.
- Depends on TECH-331 (UtilityKind + ScaleTag).
- Consumers: `UtilityPoolService` (Stage 1.2), `UtilityContributorRegistry` (Stage 1.3), Step 2 infrastructure building components, Step 4 landmarks hook.

## 5. Proposed Design

### 5.2 Architecture

```csharp
public interface IUtilityContributor {
    UtilityKind Kind { get; }
    float ProductionRate { get; }
    ScaleTag Scale { get; }
}

public interface IUtilityConsumer {
    UtilityKind Kind { get; }
    float ConsumptionRate { get; }
    ScaleTag Scale { get; }
}
```

Read-only (getters only). No setters — rate changes happen on the implementation (e.g. `InfrastructureContributor` recomputes `ProductionRate` from `def × tierMultiplier`).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Separate Contributor / Consumer interfaces | Different summation sides in `ComputeNet()` — clean dispatch | Single `IUtilityParticipant` w/ signed rate — rejected, loses semantic clarity |

## 7. Implementation Plan

### Phase 1 — Interfaces

- [ ] `IUtilityContributor.cs` — three getters, XML doc.
- [ ] `IUtilityConsumer.cs` — three getters, XML doc.
- [ ] `npm run unity:compile-check` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Interfaces compile | Unity | `npm run unity:compile-check` | No implementations yet |
| IA clean | Node | `npm run validate:all` | — |

## 8. Acceptance Criteria

- [ ] Two interface files exist w/ three properties each.
- [ ] XML doc on every property.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: implementers add setters “for convenience” — breaks read-only contract for registry snapshots. Mitigation: code review + XML states getters only.
- Risk: `ProductionRate` / `ConsumptionRate` units undefined (per second vs per tick). Mitigation: XML cites “summed per tick in `TickPools`” per orchestrator; numeric contract finalized in Stage 1.2 doc.
- Ambiguity: same type implements both interfaces. Resolution: allowed; registry lists stay separate.
- Invariant touch: interfaces live in Data assembly; no MonoBehaviour.

### §Examples

| Implementer (future) | `Kind` | `Rate` | Notes |
|----------------------|--------|--------|-------|
| Stub test double | `UtilityKind.Power` | `100f` | EditMode fakes in Stage 1.2 |
| `InfrastructureContributor` (Step 2) | Building def driven | Tier multiplier | Out of scope here |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| compile_interfaces | TECH-331 present | `npm run unity:compile-check` exit 0 | unity |
| validate_ia | repo | `npm run validate:all` exit 0 | node |

### §Acceptance

- [ ] `IUtilityContributor.cs` and `IUtilityConsumer.cs` with three read-only members each.
- [ ] XML on every property.
- [ ] `npm run validate:all` green.

### §Findings

- **Landmarks** `RegisterWithMultiplier` wrapper is Stage 1.3 — do not add to interfaces in this task.

## Open Questions

1. None — tooling / data-model scaffolding only.
