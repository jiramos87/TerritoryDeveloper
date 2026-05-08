---
purpose: "TECH-338 — Landmarks assembly + compile-check green."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-338 — Landmarks assembly + compile-check green

> **Issue:** [TECH-338](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Gatekeeper task for Stage 1.1 — ensure the three new types (enum + gate discriminator + row class under `Assets/Scripts/Data/Landmarks/`) are visible to intended consumer assemblies and compile clean. Add `Landmarks.asmdef` if repo convention is asmdef-per-folder; otherwise confirm types live in the main game assembly and document in Decision Log. Closes Stage 1.1 exit criterion "Files compile clean". Pattern mirrors utilities TECH-334.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Inspect repo asmdef layout under `Assets/Scripts/Data/` — notably the sibling `Utilities/` folder.
2. Either create `Assets/Scripts/Data/Landmarks/Landmarks.asmdef` w/ matching references OR leave types in parent assembly — record decision in Decision Log.
3. Confirm `Managers` assembly + `Tests/EditMode` assembly resolve the new types.
4. `npm run unity:compile-check` green.
5. `npm run validate:all` green.

### 2.2 Non-Goals

1. Other asmdefs (infra / managers) — scoped to landmarks folder only.
2. Changing existing assembly boundaries.
3. Runtime usage (Stage 1.2+).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Stage 1.3 service author | As `LandmarkCatalogStore` author (under `Managers/GameManagers/`), I want Landmarks data types resolvable from my assembly so code compiles without extra wiring. | `using Landmarks.Data` (or chosen namespace) works; compile-check green. |

## 4. Current State

### 4.1 Domain behavior

TECH-335 / 336 / 337 land the raw files but do not guarantee assembly visibility. Mirrors sibling utilities TECH-334.

### 4.2 Systems map

- Inspect `Assets/Scripts/Data/` for existing `*.asmdef` siblings (notably `Utilities.asmdef` from sibling Bucket 4-a Stage 1.1 TECH-334).
- Candidate asmdef name: `Landmarks` or `Territory.Data.Landmarks` per repo convention.
- Downstream consumers: `Managers` asmdef (Stage 1.3+), `Tests.EditMode` asmdef (Stage 1.3 tests).

## 5. Proposed Design

### 5.2 Architecture

Two paths — pick during implementation:

**Path A — new asmdef.** Add `Landmarks.asmdef` w/ `"autoReferenced": true` (or explicit referencer list: Managers, Tests.EditMode). Matches repo convention if other `Data/` subfolders each carry an asmdef.

**Path B — no asmdef.** Types inherit parent's compile unit. Matches repo convention if `Data/` is a single assembly. Record rationale.

Implementer picks Path A vs B after inspecting siblings (especially `Utilities.asmdef` landed under TECH-334 — follow the same precedent); Decision Log records choice.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Path A vs B chosen at implementation | Repo convention inspection needed; mirror sibling utilities decision | Committing a path blind — rejected |

## 7. Implementation Plan

### Phase 1 — Convention inspection + asmdef decision

- [ ] `ls Assets/Scripts/Data/**/*.asmdef` — map existing asmdefs under Data (check sibling `Utilities.asmdef` from TECH-334).
- [ ] Pick Path A or Path B; record in Decision Log + mirror utilities choice.
- [ ] If Path A: author `Landmarks.asmdef` w/ correct references.
- [ ] If Path B: add `// intentionally no asmdef — parent assembly` comment in one of the new files.

### Phase 2 — Compile verification

- [ ] `npm run unity:compile-check` green.
- [ ] Smoke: write a throwaway `var t = LandmarkTier.Region;` in an existing managers file, confirm compile, revert.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Types visible to consumer asm | Unity | `npm run unity:compile-check` | Smoke ref from Managers asm |
| IA clean | Node | `npm run validate:all` | — |

## 8. Acceptance Criteria

- [ ] Asmdef decision recorded in Decision Log (Path A or B).
- [ ] If Path A: `Landmarks.asmdef` present with correct references.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.
- [ ] Types resolvable from `Managers` + `Tests.EditMode` assemblies.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only; Stage 1.1 gatekeeper for assembly wiring. Choice between Path A / B resolved at implementation via repo convention inspection, mirroring sibling utilities TECH-334 outcome.
