---
purpose: "TECH-689 — PlacementFailReason enum + PlacementResult; XML docs; EditMode table tests."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T3.1.2
---
# TECH-689 — Reason codes + result struct

> **Issue:** [TECH-689](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Define **`PlacementFailReason`** (footprint, zoning, locked, unaffordable, occupied, plus success/none as needed) and a **`PlacementResult`** (or tuple) carrying **`bool`**, reason, optional detail string. Wire **`PlacementValidator.CanPlace`** to return structured results. Add table-driven **EditMode** tests per **Stage 3.1** exit criteria.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Enum covers footprint, zoning, locked, unaffordable, occupied.
2. XML docs on public **`CanPlace`** and result types.
3. **EditMode** tests: at least one pass and representative fail per category (deps may be stubbed/mocked).

### 2.2 Non-Goals (Out of Scope)

1. Full zoning/economy implementation (validators return stubbed failures until **TECH-690**–**692**).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | UX + ghosts can read structured fail reasons | Enum + result type stable |

## 4. Current State

### 4.1 Domain behavior

**TECH-688** provides **`PlacementValidator`** scaffold; this task makes failure reasons explicit.

### 4.2 Systems map

- **`PlacementValidator.cs`**
- Tests: existing **EditMode** assembly under **`Assets/Tests/`** (or project convention)

### 4.3 Implementation investigation notes (optional)

Prefer **`[TestCase]`** matrix over one test per reason.

## 5. Proposed Design

### 5.1 Target behavior (product)

Player-facing copy maps from **`PlacementFailReason`** in **Stage 3.2**; here API only.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

**`struct PlacementResult`** or readonly class; avoid GC churn on hot path if profiler demands (document if so).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
|  |  |  |  |

## 7. Implementation Plan

### Phase 1 — Result + tests

- [ ] Add **`PlacementFailReason`** + **`PlacementResult`**.
- [ ] Change **`CanPlace`** to return **`PlacementResult`** (or out-param pattern if repo standard differs — document).
- [ ] **EditMode** test fixture with table-driven cases.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| EditMode matrix | Unity Test | Unity Test Runner | Table-driven |
| Compile | Unity | `npm run unity:compile-check` |  |

## 8. Acceptance Criteria

- [ ] Enum covers footprint, zoning, locked, unaffordable, occupied (+ none/ok as appropriate).
- [ ] XML documentation on public API.
- [ ] EditMode tests with **[TestCase]** (or equivalent) coverage.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

`PlacementFailReason` enum + `PlacementResult` (or struct) with XML docs; `CanPlace` returns structured outcome; EditMode tests table-driven in existing Economy EditMode assembly.

### §Acceptance

- [ ] Enum values: footprint, zoning, locked, unaffordable, occupied (+ ok/none per design)
- [ ] Public API XML complete on `CanPlace` and result types
- [ ] EditMode test file extends coverage; `npm run unity:compile-check` exits 0

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| reason_matrix | stubbed validator deps | each reason reachable | Unity EditMode |
| compile_gate | n/a | exit 0 | `npm run unity:compile-check` |

### §Examples

| Reason | When set |
|--------|----------|
| Zoning | Channel mismatch (wired in TECH-690) |
| Unaffordable | Treasury check fails (TECH-691) |

### §Mechanical Steps

#### Step 1 — Structured return type

**Goal:** Replace bool stub with `PlacementResult` carrying reason + optional detail string.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/PlacementValidator.cs` — **before:**
```
        public bool CanPlace(int assetId, Vector2 gridPosition, int rotation)
        {
            return true;
        }
```
**after:**
```
        public PlacementResult CanPlace(int assetId, Vector2 gridPosition, int rotation)
        {
            return PlacementResult.Allowed();
        }
```
(Define `PlacementFailReason`, `PlacementResult`, and factory/helpers immediately above `PlacementValidator` class in the same file per §7.)

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** On compile error, align namespaces and usings with `Territory.Core` / `Territory.Economy` then re-run gate.

#### Step 2 — EditMode tests

**Goal:** Table-driven tests lock reason enum behavior independent of Play Mode.

**Edits:**

- `Assets/Tests/EditMode/Economy/ZoneSServicePlacementTests.cs` — **before:**
```
        private static T GetPrivateField<T>(System.Type type, object target, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Reflection: {fieldName} not found on {type.Name}");
            return (T)field.GetValue(target);
        }
    }
}
```
**after:**
```
        private static T GetPrivateField<T>(System.Type type, object target, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Reflection: {fieldName} not found on {type.Name}");
            return (T)field.GetValue(target);
        }

        // TECH-689: add PlacementValidator reason tests in dedicated fixture when ready; placeholder anchor for plan-digest.
    }
}
```

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** If test assembly fails to compile, add a new EditMode fixture under `Assets/Tests/EditMode/Economy/` per §7 after the file exists on disk, then replace this anchor.

## Open Questions (resolve before / during implementation)

1. Naming: standardize on **`PlacementResult.Allowed`**; avoid **`Success`** alias unless glossary demands both.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
