---
purpose: "TECH-348 — BuildInfo asset instance under Assets/Resources."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-348 — BuildInfo asset instance under Assets/Resources

> **Issue:** [TECH-348](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Create `Assets/Resources/BuildInfo.asset` — the committed SO instance of the TECH-347 BuildInfo type, loadable at runtime via `Resources.Load<BuildInfo>("BuildInfo")`. Stage 1.2 ReleaseBuilder stamps it from env vars on every build; Stage 2.3 UpdateNotifier reads it at launch. Defaults `0.0.0-dev` / `unknown` / `unknown` until first build run.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `Assets/Resources/BuildInfo.asset` instance created via Territory/BuildInfo menu command from TECH-347.
2. `.asset` + `.asset.meta` committed to git.
3. `Resources.Load<BuildInfo>("BuildInfo")` returns non-null at runtime (EditMode fixture confirms).
4. Default field values `0.0.0-dev` / `unknown` / `unknown` per BuildInfo type defaults.

### 2.2 Non-Goals

1. Stamping env-var values into the asset — Stage 1.2 ReleaseBuilder.
2. Consuming the asset at runtime — Stage 1.3 Credits screen + Stage 2.3 UpdateNotifier.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As dev, want a committed BuildInfo asset so Resources.Load resolves at runtime w/o build-time generation. | `.asset` tracked in git; EditMode load returns non-null. |

## 4. Current State

### 4.1 Domain behavior

`Assets/Resources/` exists; no BuildInfo instance present.

### 4.2 Systems map

| Path | Role | Status |
|---|---|---|
| `Assets/Resources/BuildInfo.asset` | SO instance committed here | new |
| `Assets/Resources/BuildInfo.asset.meta` | Unity meta for asset | new |
| `Assets/Scripts/Runtime/Distribution/BuildInfo.cs` | Type (TECH-347) | precondition |

## 5. Proposed Design

### 5.1 Target behavior (product)

None — asset file only.

### 5.2 Architecture / implementation

- Open Unity Editor w/ TECH-347 BuildInfo type compiled.
- Assets → Create → Territory → BuildInfo in Project window under `Assets/Resources/`.
- Rename created file to `BuildInfo` (so `Resources.Load<BuildInfo>("BuildInfo")` resolves by name — Unity drops `.asset` suffix in Resources lookup).
- Commit both `.asset` + `.asset.meta`.
- Optional EditMode fixture `Assets/Tests/EditMode/Distribution/BuildInfoAssetTests.cs` asserting `Resources.Load<BuildInfo>("BuildInfo") != null` (can fold into TECH-349 SemverCompareTests file or keep separate — implementer choice).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Commit `.asset` instead of regenerate on build | Preserves default-value guarantee across fresh clones; ReleaseBuilder overwrites on release builds | Regenerate on each build (race w/ git state); Addressables (out of scope) |

## 7. Implementation Plan

### Phase 1 — Create + commit asset

- [ ] Confirm TECH-347 type compiles.
- [ ] Create asset via menu under `Assets/Resources/`.
- [ ] Verify defaults via Inspector.
- [ ] Stage + commit `.asset` + `.meta`.
- [ ] Run `npm run unity:compile-check` + optional EditMode load check.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Asset committed | git | `git ls-files Assets/Resources/BuildInfo.asset*` lists 2 files | — |
| Runtime load resolves | EditMode | `Resources.Load<BuildInfo>("BuildInfo")` non-null | Optional fixture |

## 8. Acceptance Criteria

- [ ] `Assets/Resources/BuildInfo.asset` + `.meta` committed.
- [ ] Defaults match `0.0.0-dev` / `unknown` / `unknown`.
- [ ] `Resources.Load<BuildInfo>("BuildInfo")` returns non-null.
- [ ] `npm run unity:compile-check` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. None — tooling only; asset instance + commit.
