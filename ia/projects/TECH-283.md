---
purpose: "TECH-283 — EditMode tests for ZoneType enum extension + ZoneSubTypeRegistry."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-283 — EditMode tests for `ZoneType` + `ZoneSubTypeRegistry`

> **Issue:** [TECH-283](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

New test class `ZoneSubTypeRegistryTests` under `Assets/Tests/EditMode/Economy/` (new asmdef if needed). Covers: `GetById` lookup round-trip for 7 seeded entries; `GetById(-1)` miss semantics; `IsStateServiceZone` predicate bool table for all enum values; `Zone.subTypeId` default + serialization round-trip. Locks Stage 1.1 scaffolding behavior.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Test class `ZoneSubTypeRegistryTests` lands under `Assets/Tests/EditMode/Economy/`.
2. New asmdef `Tests.EditMode.Economy` (or reuse existing EditMode asmdef if package structure allows).
3. Coverage: (a) `GetById(0..6)` returns matching entry; (b) `GetById(-1)` returns null; (c) `IsStateServiceZone` true for 6 new enum values; (d) `IsStateServiceZone` false for R/C/I + legacy; (e) `Zone.subTypeId` default `-1`; (f) subTypeId persists via serialization round-trip.
4. `npm run unity:testmode-batch` exercises new tests green.

### 2.2 Non-Goals

1. No `BudgetAllocationService` coverage — lands in Stage 1.3 test task.
2. No `TreasuryFloorClampService` coverage — Stage 1.2.
3. No integration / PlayMode scenarios.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Guard regression on enum extension | Predicate + registry lookup tests fail if scaffolding breaks |
| 2 | Developer | Catch serialization drift early | subTypeId round-trip fails on save-format change |

## 4. Current State

### 4.1 Domain behavior

`Assets/Tests/EditMode/` holds existing EditMode asmdef. No Economy subdir yet. No tests cover TECH-278/279/280/281 outputs.

### 4.2 Systems map

- `Assets/Tests/EditMode/Economy/` *(new dir)*.
- `Assets/Tests/EditMode/Economy/Tests.EditMode.Economy.asmdef` *(new)* — refs `Tests.EditMode` baseline + `Assembly-CSharp` + `UnityEngine.TestRunner`.
- `Assets/Tests/EditMode/Economy/ZoneSubTypeRegistryTests.cs` *(new)*.
- Depends on: TECH-278 (enum), TECH-279 (subTypeId), TECH-280 (registry class), TECH-281 (asset).
- Router domain: Zones, buildings, RCI.

## 5. Proposed Design

### 5.1 Target behavior

NUnit test class w/ 4–6 `[Test]` methods. Uses `AssetDatabase.LoadAssetAtPath` to resolve real `ZoneSubTypeRegistry.asset` for registry lookup tests.

### 5.2 Architecture / implementation

```csharp
public class ZoneSubTypeRegistryTests {
    private ZoneSubTypeRegistry registry;

    [SetUp] public void SetUp() {
        registry = AssetDatabase.LoadAssetAtPath<ZoneSubTypeRegistry>(
            "Assets/ScriptableObjects/Economy/ZoneSubTypeRegistry.asset");
    }

    [Test] public void GetById_ValidIds_ReturnsEntry() { /* 0..6 */ }
    [Test] public void GetById_MinusOne_ReturnsNull() { /* -1 → null */ }
    [Test] public void IsStateServiceZone_NewEnumValues_TrueForAllSix() { /* predicate table */ }
    [Test] public void IsStateServiceZone_RCI_False() { /* R, C, I */ }
    [Test] public void Zone_SubTypeId_DefaultsToMinusOne() { /* fresh GO */ }
    [Test] public void Zone_SubTypeId_SerializationRoundTrip() { /* JsonUtility or prefab round-trip */ }
}
```

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | EditMode vs PlayMode | Enum + registry lookup are data-only; no scene needed | PlayMode (rejected — overkill for pure-data tests) |
| 2026-04-17 | Load real asset vs fixture clone | Tests registry asset shape stays in sync w/ TECH-281 seeds | Fixture clone (rejected — drift risk) |

## 7. Implementation Plan

### Phase 1 — Asmdef + test class

- [ ] Create `Assets/Tests/EditMode/Economy/` dir.
- [ ] Author `Tests.EditMode.Economy.asmdef` w/ correct refs.
- [ ] Author `ZoneSubTypeRegistryTests.cs` w/ 6 `[Test]` methods per §5.2 sketch.
- [ ] Run `npm run unity:compile-check`.
- [ ] Run `npm run unity:testmode-batch` — confirm all tests green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tests compile + pass | EditMode batch | `npm run unity:testmode-batch` | Runs full EditMode suite |
| Asmdef refs resolve | Unity compile | `npm run unity:compile-check` | |

## 8. Acceptance Criteria

- [ ] `Assets/Tests/EditMode/Economy/` dir w/ asmdef.
- [ ] `ZoneSubTypeRegistryTests.cs` covers 6 test cases per §5.2.
- [ ] `npm run unity:testmode-batch` green — all new tests pass.
- [ ] `npm run validate:all` green.

## Open Questions

1. None — test scaffolding only, verifies sibling tasks.
