---
purpose: "TECH-334 — Utilities assembly + compile-check green."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/utilities-master-plan.md"
task_key: "T1.1.4"
---
# TECH-334 — Utilities assembly + compile-check green

> **Issue:** [TECH-334](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-21

## 1. Summary

Gatekeeper task for Stage 1.1 — ensure the five new types (three enums + struct + two interfaces under `Assets/Scripts/Data/Utilities/`) are visible to intended consumer assemblies and compile clean. Add `Utilities.asmdef` if repo convention is asmdef-per-folder; otherwise confirm types live in the main game assembly and document the decision. Closes Stage 1.1 exit criterion "Files compile clean".

## 2. Goals and Non-Goals

### 2.1 Goals

1. Inspect repo asmdef layout under `Assets/Scripts/Data/`.
2. Either create `Assets/Scripts/Data/Utilities/Utilities.asmdef` (with references to common data deps) OR leave types in parent assembly — record decision in Decision Log.
3. Confirm `Managers` assembly + `Tests/EditMode` assembly can resolve the new types (reference list update if asmdef is added).
4. `npm run unity:compile-check` green.
5. `npm run validate:all` green.

### 2.2 Non-Goals

1. Creating other asmdefs (infra / managers) — scoped to utilities folder only.
2. Changing existing assembly boundaries.
3. Runtime usage (Stage 1.2+).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Stage 1.2 service author | As `UtilityPoolService` author (under `Managers/GameManagers/`), I want the new Utilities types resolvable from my assembly so my code compiles without extra wiring. | `using Utilities.Data` (or whichever namespace) works; compile-check green. |

## 4. Current State

### 4.1 Domain behavior

TECH-331 / 332 / 333 land the raw files but do not guarantee assembly visibility.

### 4.2 Systems map

- Inspect `Assets/Scripts/Data/` for existing `*.asmdef` siblings.
- Candidate asmdef name: `Utilities` or `Territory.Data.Utilities` per repo convention.
- Downstream consumers: `Managers` asmdef (Stage 1.2+), `Tests.EditMode` asmdef (Stage 1.2 tests).

## 5. Proposed Design

### 5.2 Architecture

Two paths — pick during implementation:

**Path A — new asmdef.** Add `Utilities.asmdef` w/ `"autoReferenced": true` (or explicit referencer list: Managers, Tests.EditMode). Matches repo convention if other `Data/` subfolders each carry an asmdef.

**Path B — no asmdef.** Types inherit parent's compile unit. Matches repo convention if `Data/` is a single assembly. Record rationale.

Implementer picks Path A vs B after inspecting siblings; Decision Log records choice.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Path A vs B chosen at implementation | Repo convention inspection needed | Committing a path blind — rejected |

## 7. Implementation Plan

### Phase 1 — Convention inspection + asmdef decision

- [ ] `ls Assets/Scripts/Data/**/*.asmdef` — map existing asmdefs under Data.
- [ ] Pick Path A or Path B; record in Decision Log.
- [ ] If Path A: author `Utilities.asmdef` w/ correct references.
- [ ] If Path B: add `// intentionally no asmdef — parent assembly` comment in one of the new enum files.

### Phase 2 — Compile verification

- [ ] `npm run unity:compile-check` green.
- [ ] Smoke: write a throwaway `var k = UtilityKind.Water;` in an existing managers file, confirm compile, revert.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Types visible to consumer asm | Unity | `npm run unity:compile-check` | Smoke ref from Managers asm |
| IA clean | Node | `npm run validate:all` | — |

## 8. Acceptance Criteria

- [ ] Asmdef decision recorded in Decision Log (Path A or B).
- [ ] If Path A: `Utilities.asmdef` present with correct references.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.
- [ ] Types resolvable from `Managers` + `Tests.EditMode` assemblies.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: new `Utilities.asmdef` breaks circular refs with `Managers` or `Tests`. Mitigation: mirror sibling `Data/*` asmdef reference lists; run compile-check after each reference tweak.
- Risk: `autoReferenced` false hides types from Editor scripts. Mitigation: match repo convention from inspection; prefer explicit references listed in Exit criteria.
- Ambiguity: smoke `using` in Managers file — must revert before commit if policy forbids temp edits. Resolution: use dedicated test assembly file or revert hunk in same PR.
- Invariant touch: no change to global singleton patterns — assembly wiring only.

### §Examples

| Path chosen | Artifact | Notes |
|-------------|----------|-------|
| Path A | `Assets/Scripts/Data/Utilities/Utilities.asmdef` | References `UnityEngine` + parent data deps per siblings |
| Path B | Comment in one `.cs` | “No asmdef — inherits parent assembly” |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| compile_check | post-wiring | `npm run unity:compile-check` exit 0 | unity |
| managers_resolve | optional smoke reference | Types resolve from Managers asm | unity |
| validate_all | repo | `npm run validate:all` exit 0 | node |

### §Acceptance

- [ ] Decision Log records Path A or B with one-line rationale from repo inspection.
- [ ] If Path A: asmdef on disk with correct `references` / `includePlatforms`.
- [ ] `npm run unity:compile-check` and `npm run validate:all` green.

### §Findings

- Stage 1.2 `UtilityPoolService` assembly must reference Utilities if split — verify in follow-on task if compile fails.

## Open Questions

1. None — tooling only; Stage 1.1 gatekeeper for assembly wiring. Choice between Path A / B resolved at implementation via repo convention inspection.
