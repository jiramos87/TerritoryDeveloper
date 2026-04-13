---
purpose: "TECH-89 — Parent-id round-trip + legacy-migration tests (testmode)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-89 — Parent-id round-trip + legacy-migration tests (testmode)

> **Issue:** [TECH-89](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-12
> **Last updated:** 2026-04-12
> **Orchestrator:** [`multi-scale-master-plan.md`](multi-scale-master-plan.md) — Step 1 / Stage 1.1 / Phase 3.

## 1. Summary

Testmode batch scenario that exercises parent-id persistence: (a) new-game → save → reload → assert ids preserved; (b) legacy save fixture (pre-TECH-87 version) → load → assert placeholder migration. Closes Stage 1.1 verification.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Testmode scenario asserts new-game parent ids round-trip through save / load.
2. Legacy fixture save (pre-version-bump) loads w/ placeholder GUIDs non-null.
3. Scenario runs via `npm run unity:testmode-batch`.
4. Fixture committed under existing testmode fixture path.

### 2.2 Non-Goals

1. Any region / country sim logic.
2. Cross-scale tests (Stage 1.2 / 1.3).
3. Fuzzing or property-based tests.
4. Performance benchmarks.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want automated verification that parent ids persist + migrate, so future save-schema changes surface regressions in CI-equivalent runs. | Testmode scenario green; fixture committed; failure asserts legible. |

## 4. Current State

### 4.1 Domain behavior

Post TECH-87 + TECH-88: `GameSaveData` + `GridManager` carry parent ids. No automated verification yet.

### 4.2 Systems map

- `Assets/Editor/TestMode/` — existing testmode scenarios (check for nearest pattern).
- `tools/scripts/testmode/` — runner scripts.
- Fixture location: match existing legacy-save fixture convention (investigate during Phase 1).
- `docs/agent-led-verification-policy.md` — testmode policy.
- `ia/skills/agent-test-mode-verify/SKILL.md` — harness recipe.

### 4.3 Implementation investigation notes

- Existing testmode scenarios provide scenario-id registration pattern.
- Legacy fixture = raw JSON save from pre-TECH-87 build; may need hand-authored minimal example rather than captured file (TBD Phase 1).
- Assertions via existing testmode assertion API (whatever current scenarios use).

## 5. Proposed Design

### 5.1 Target behavior (product)

Agent runs `npm run unity:testmode-batch -- --scenario-id parent-id-roundtrip` → Unity launches batchmode → scenario executes round-trip + migration cases → green / red report.

### 5.2 Architecture / implementation

- New testmode scenario class under `Assets/Editor/TestMode/`.
- Two cases:
  - **Round-trip:** new-game → capture `GridManager.ParentRegionId` / `.ParentCountryId` → save → clear session → load → assert ids equal captured.
  - **Legacy migration:** load hand-authored legacy-JSON fixture → assert ids non-null + GUID-shaped.
- Fixture: minimal hand-authored legacy `GameSaveData` JSON missing parent-id fields, committed under existing testmode fixture dir.

### 5.3 Method / algorithm notes

Scenario registers w/ existing scenario id registry. Failure mode: assertion → scenario returns non-zero → batch run fails.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-12 | Hand-authored minimal legacy fixture | Lower maintenance than captured real save; deterministic | Captured full save from earlier build (rejected — bloat + churn) |

## 7. Implementation Plan

### Phase 1 — Scenario scaffold

- [ ] Locate existing testmode scenario pattern.
- [ ] Register new scenario id (e.g. `parent-id-roundtrip`).
- [ ] Author scenario skeleton.

### Phase 2 — Round-trip case

- [ ] Implement new-game → save → load → assert flow.
- [ ] Verify via `npm run unity:testmode-batch -- --scenario-id parent-id-roundtrip`.

### Phase 3 — Legacy migration case

- [ ] Author minimal legacy fixture JSON.
- [ ] Commit under existing fixture dir.
- [ ] Implement load → assert placeholder migration.

### Phase 4 — Verification block

- [ ] `npm run validate:all`.
- [ ] `npm run unity:compile-check`.
- [ ] `npm run unity:testmode-batch -- --scenario-id parent-id-roundtrip`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| IA / scenario registration consistent | Node | `npm run validate:all` | |
| C# compiles | Node | `npm run unity:compile-check` | Editor scenario code |
| Round-trip + migration green | Agent report | `npm run unity:testmode-batch -- --scenario-id parent-id-roundtrip` | Batch exit code + scenario log |

## 8. Acceptance Criteria

- [ ] Scenario registered + discoverable by batch runner.
- [ ] Round-trip case passes.
- [ ] Legacy-migration case passes against committed fixture.
- [ ] Full Verification block (validate:all + compile-check + testmode-batch) green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling-only; see §8 Acceptance criteria.
