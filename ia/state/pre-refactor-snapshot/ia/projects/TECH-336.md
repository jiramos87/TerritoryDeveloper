---
purpose: "TECH-336 — Add LandmarkPopGate discriminator."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-336 — Add LandmarkPopGate discriminator

> **Issue:** [TECH-336](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Add `LandmarkPopGate` abstract base + two concrete subclasses — `ScaleTransitionGate { LandmarkTier fromTier }` (tier-defining track) + `IntraTierGate { int pop }` (intra-tier reward track). YAML-deserializable via tag field `kind` (`scale_transition` / `intra_tier`). Stage 1.1 Phase 1. Round-trip test deferred to Stage 1.3 T1.3.4 once catalog YAML + store ship.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Add `Assets/Scripts/Data/Landmarks/LandmarkPopGate.cs` — abstract base + 2 subclasses.
2. YAML tag discriminator wired — `kind: scale_transition` / `kind: intra_tier`.
3. `ScaleTransitionGate.fromTier` references `LandmarkTier` (TECH-335).
4. `IntraTierGate.pop` — `int` population threshold.
5. Compile clean; `npm run validate:all` green.

### 2.2 Non-Goals

1. Gate evaluation logic — lives in Stage 2.2 `LandmarkProgressionService.EvaluateGate`.
2. YAML round-trip test — Stage 1.3 (needs catalog + store).
3. Further gate kinds (time-gated, quest-gated) — post-MVP.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Stage 2.2 author | As `LandmarkProgressionService` author, I want to pattern-match on `ScaleTransitionGate` vs `IntraTierGate` so gate eval branches cleanly. | `switch (gate) { case ScaleTransitionGate g: … case IntraTierGate g: … }` compiles. |

## 4. Current State

### 4.1 Domain behavior

TECH-335 lands `LandmarkTier` enum. No pop-gate type yet. Catalog YAML authoring (TECH-? Stage 1.2) needs a tag-discriminated union to deserialize both tracks uniformly.

### 4.2 Systems map

- Target file: `Assets/Scripts/Data/Landmarks/LandmarkPopGate.cs` (new).
- Consumes: `LandmarkTier` (TECH-335).
- Consumed by: `LandmarkCatalogRow.popGate` (TECH-337), `LandmarkProgressionService.EvaluateGate` (Stage 2.2).
- YAML deserialization stack — verify at impl time whether repo uses YamlDotNet (sibling utilities catalog Stage 1.2 lands concurrent; pick compatible lib).

## 5. Proposed Design

### 5.2 Architecture

Abstract class `LandmarkPopGate` (no fields) + sealed subclasses:

- `ScaleTransitionGate : LandmarkPopGate { public LandmarkTier fromTier; }`
- `IntraTierGate : LandmarkPopGate { public int pop; }`

Tag field `kind` — string discriminator either on a base-level `[YamlTagMapping]` attribute OR a custom `INodeDeserializer` (impl picks at Stage 1.2 when YAML parser lib lands). XML doc base + both subclasses.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Abstract class + sealed subclasses (polymorphic) over discriminated-union record | C# 7.3 / Unity 2022 compatible; pattern-match in Stage 2.2 works | struct + `kind` enum — rejected (cannot carry per-branch fields cleanly) |

## 7. Implementation Plan

### Phase 1 — Create file + types

- [ ] Add `LandmarkPopGate.cs` — abstract base.
- [ ] Add `ScaleTransitionGate` sealed subclass w/ `fromTier`.
- [ ] Add `IntraTierGate` sealed subclass w/ `pop`.
- [ ] XML doc base + both subclasses + `kind` discriminator contract.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Types compile | Unity | `npm run unity:compile-check` | YAML round-trip deferred Stage 1.3 |
| IA clean | Node | `npm run validate:all` | — |

## 8. Acceptance Criteria

- [ ] `LandmarkPopGate.cs` present.
- [ ] Abstract base + 2 sealed subclasses.
- [ ] XML doc on base + each subclass.
- [ ] `npm run unity:compile-check` green.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only; YAML deserialization impl picked alongside Stage 1.2 catalog authoring.
