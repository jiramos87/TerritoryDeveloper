---
purpose: "TECH-339 — Release registry + resolver (web/lib/releases.ts)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-339 — Release registry + resolver

> **Issue:** [TECH-339](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Hand-maintained release registry for web dashboard release-scoped progress view. Exports `Release` interface + `resolveRelease(id)` + seeded `full-game-mvp` entry listing 9 child master plans. Consumed by Stage 7.2 RSC pages. No routes, no UI, no auth — pure data layer. Satisfies Stage 7.1 Exit bullet 1 (`web/lib/releases.ts` ships seeded row w/ header pointer to rollout tracker doc).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `web/lib/releases.ts` exports `Release` interface — `id`, `label`, `umbrellaMasterPlan`, `children: string[]`.
2. `resolveRelease(id: string): Release | null` lookup by id.
3. Seed `releases` const array w/ single `full-game-mvp` row; 9 children from `docs/web-platform-post-mvp-extensions.md` §1 Examples block.
4. Header comment cites `ia/projects/full-game-mvp-rollout-tracker.md` as source of truth for `children[]`; flag drift risk.
5. `npm run validate:web` green.

### 2.2 Non-Goals

1. Multi-release support (YAGNI — hardcode full-game-mvp per locked decision).
2. Automated sync w/ rollout tracker (manual drift acceptable at MVP).
3. Filter shaper (lands TECH-340).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | TECH-340 implementer | As author of `getReleasePlans`, I want typed `Release` w/ `children: string[]` so my filter compiles. | Interface exported; seeded row resolves. |

## 4. Current State

### 4.1 Domain behavior

No release registry exists. `web/lib/plan-loader.ts` already loads all plans indiscriminately; release-scoped view needs a filter seed.

### 4.2 Systems map

- New file `web/lib/releases.ts`.
- Consumers (future tasks): `web/lib/releases/resolve.ts` (TECH-340), Stage 7.2 RSC pages.
- Canonical source: `docs/web-platform-post-mvp-extensions.md` §Design Expansion §1 — Chosen Approach + Examples.
- Drift source: `ia/projects/full-game-mvp-rollout-tracker.md`.

## 5. Proposed Design

### 5.1 Target behavior

Pure TypeScript module. Single exported const array + lookup function. No side effects, no I/O.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Hardcode single `full-game-mvp` row | YAGNI — second release tracker does not exist yet | Dynamic discovery from filesystem — rejected, premature |

## 7. Implementation Plan

### Phase 1 — Registry scaffolding

- [ ] Create `web/lib/releases.ts`.
- [ ] Export `Release` interface.
- [ ] Seed `releases` array w/ `full-game-mvp` row (9 children from extensions doc Examples).
- [ ] Export `resolveRelease(id)` lookup.
- [ ] Header comment citing rollout tracker.
- [ ] `npm run validate:web` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Registry compiles | Node | `npm run validate:web` | Lint + typecheck + build |
| Resolver behavior | Unit test (deferred) | Covered by TECH-340 tests | Co-located test file lands w/ TECH-340 |

## 8. Acceptance Criteria

- [ ] `web/lib/releases.ts` exists + compiles.
- [ ] `Release` interface shape matches spec.
- [ ] Seeded `full-game-mvp` row has 9 children matching extensions doc.
- [ ] Header cites rollout tracker.
- [ ] `npm run validate:web` green.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling / data-model scaffolding only; no gameplay rules.
