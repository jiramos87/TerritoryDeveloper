---
purpose: "TECH-349 — SemverCompare helper + EditMode truth-table tests."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-349 — SemverCompare helper + EditMode truth-table tests

> **Issue:** [TECH-349](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Author `Assets/Scripts/Runtime/Distribution/SemverCompare.cs` — pure static `Compare(string a, string b) → int` helper handling `MAJOR.MINOR.PATCH` + optional `-PRERELEASE` suffix per Design Expansion IP-8 subset. EditMode truth-table test locks behavior for Stage 2.3 UpdateNotifier local-vs-remote version gate.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `SemverCompare.Compare(string a, string b) → int` returns `<0` / `0` / `>0` by semver ordering.
2. Supports `MAJOR.MINOR.PATCH` + optional `-PRERELEASE` tail (alphanumeric dot-separated).
3. Malformed input → return `0` (silent fallback; notifier treats as "cannot decide, skip").
4. EditMode test `Assets/Tests/EditMode/Distribution/SemverCompareTests.cs` covers ≥6 cases — equal, major >, minor >, patch >, prerelease ordering (`1.0.0` > `1.0.0-beta`), malformed → 0.
5. No external semver library; pure C# string parsing + integer compare.
6. `npm run unity:compile-check` + EditMode run green.

### 2.2 Non-Goals

1. Full semver 2.0.0 precedence spec — subset only (prerelease simple lex compare).
2. Build metadata tail (`+BUILD`) — not in scope.
3. Caller integration — Stage 2.3 UpdateNotifier wires the gate.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As notifier author, want a locked-behavior semver compare so local vs `latest.json` decisions are deterministic across testers. | ≥6 EditMode cases green; reused directly in UpdateNotifier Stage 2.3. |

## 4. Current State

### 4.1 Domain behavior

No semver compare helper exists in repo. No distribution version-gate surface today.

### 4.2 Systems map

| Path | Role | Status |
|---|---|---|
| `Assets/Scripts/Runtime/Distribution/SemverCompare.cs` | Pure static helper | new |
| `Assets/Tests/EditMode/Distribution/SemverCompareTests.cs` | Truth-table fixture | new |
| `Assets/Scripts/UI/Distribution/UpdateNotifier.cs` | Consumer (Stage 2.3) | future |

## 5. Proposed Design

### 5.1 Target behavior (product)

None — internal helper.

### 5.2 Architecture / implementation

```csharp
using System;

namespace Territory.Distribution
{
    public static class SemverCompare
    {
        // Returns -1 / 0 / +1 per semver ordering (subset — MAJOR.MINOR.PATCH + optional -PRERELEASE).
        // Malformed input → 0 (silent fallback; callers treat as "cannot decide").
        public static int Compare(string a, string b)
        {
            if (!TryParse(a, out var aCore, out var aPre)) return 0;
            if (!TryParse(b, out var bCore, out var bPre)) return 0;

            for (int i = 0; i < 3; i++)
            {
                int cmp = aCore[i].CompareTo(bCore[i]);
                if (cmp != 0) return Math.Sign(cmp);
            }
            // Core equal — prerelease lower than release.
            bool aHasPre = !string.IsNullOrEmpty(aPre);
            bool bHasPre = !string.IsNullOrEmpty(bPre);
            if (aHasPre && !bHasPre) return -1;
            if (!aHasPre && bHasPre) return +1;
            if (!aHasPre && !bHasPre) return 0;
            return Math.Sign(string.Compare(aPre, bPre, StringComparison.Ordinal));
        }

        private static bool TryParse(string s, out int[] core, out string pre)
        {
            core = new int[3];
            pre = null;
            if (string.IsNullOrWhiteSpace(s)) return false;
            int dash = s.IndexOf('-');
            string head = dash >= 0 ? s.Substring(0, dash) : s;
            pre = dash >= 0 ? s.Substring(dash + 1) : null;
            var parts = head.Split('.');
            if (parts.Length != 3) return false;
            for (int i = 0; i < 3; i++)
                if (!int.TryParse(parts[i], out core[i])) return false;
            return true;
        }
    }
}
```

Truth table (≥6 cases):

| a | b | Expected sign | Reason |
|---|---|---|---|
| `1.2.3` | `1.2.3` | 0 | equal |
| `2.0.0` | `1.9.9` | +1 | major > |
| `1.3.0` | `1.2.9` | +1 | minor > |
| `1.2.4` | `1.2.3` | +1 | patch > |
| `1.0.0` | `1.0.0-beta` | +1 | release > prerelease |
| `1.0.0-rc` | `1.0.0-beta` | +1 | prerelease lex |
| `not.a.ver` | `1.0.0` | 0 | malformed fallback |

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Helper + tests = single task (Phase 2 single-task) | Atomic algorithm-layer deliverable; splitting tests from impl adds artificial dependency w/o risk reduction (per stage-file sizing heuristic) | Split T1.1.3a impl + T1.1.3b tests (4x overhead, zero parallelism gain) |
| 2026-04-18 | Malformed → 0 not throw | Silent-fail notifier contract per Design Expansion IP-7 | Throw + caller try/catch (noisier) |
| 2026-04-18 | Prerelease = ordinal string compare | Subset of semver 2.0.0 — good enough for `0.1.0-beta.1` tier scheme | Full numeric-ident precedence (out of scope) |

## 7. Implementation Plan

### Phase 1 — Author helper + tests

- [ ] Write `SemverCompare.cs` per §5.2.
- [ ] Write `SemverCompareTests.cs` w/ `[Test]` per truth-table row (NUnit via Unity Test Framework).
- [ ] Run EditMode test suite — all green.
- [ ] Run `npm run unity:compile-check`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| ≥6 truth-table cases green | EditMode | `npm run unity:testmode-batch` (EditMode filter) | NUnit `[Test]` per row |
| Compiles | Unity | `npm run unity:compile-check` | — |

## 8. Acceptance Criteria

- [ ] `SemverCompare.cs` authored w/ `Compare(string, string) → int`.
- [ ] Truth table ≥6 cases — equal, major >, minor >, patch >, prerelease ordering, malformed → 0.
- [ ] No external semver lib dep.
- [ ] EditMode tests green; `npm run unity:compile-check` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. None — tooling / pure function.
