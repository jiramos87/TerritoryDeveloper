---
purpose: "TECH-308 — simulation-signals.md reference spec (signal contract permanent domain)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-308 — `ia/specs/simulation-signals.md` reference spec

> **Issue:** [TECH-308](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Author permanent reference spec `ia/specs/simulation-signals.md` — closes spec gap flagged in city-sim-depth exploration review. Covers: signal inventory (12 entries), diffusion physics contract, producer/consumer interface contract, rollup rule table (P90 vs Mean), spec-gap closure note. Linked from `ia/specs/simulation-system.md` §Tick execution order. Invariant #12 (permanent signal contract → `ia/specs/`, not `ia/projects/`).

## 2. Goals and Non-Goals

### 2.1 Goals

1. New `ia/specs/simulation-signals.md` present w/ 5 required sections (inventory / diffusion / interface / rollup / closure).
2. Signal inventory table — 12 rows; per row: signal name, source types, sink types, rollup rule (Mean / P90), update cadence (daily / monthly).
3. Diffusion physics section — separable horizontal + vertical Gaussian; per-axis sigma from `SignalMetadataRegistry.anisotropy`; decay per step; clamp-floor-0 rule.
4. Interface contract — `ISignalProducer.EmitSignals(SignalFieldRegistry)` + `ISignalConsumer.ConsumeSignals(SignalFieldRegistry, DistrictSignalCache)` signatures + ordering guarantees.
5. Rollup rule table — P90 for `Crime` + `TrafficLevel`; Mean for remaining 10.
6. `ia/specs/simulation-system.md` §Tick execution order references new spec.
7. Glossary rows added per `ia/rules/terminology-consistency.md`.

### 2.2 Non-Goals (Out of Scope)

1. No code (TECH-305 / -306 / -307 deliver types).
2. No diffusion kernel impl (Stage 1.2 TECH).
3. No producer/consumer impls (Step 2+).
4. No district aggregation details (Stage 1.3 spec carve-out; cross-link only).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Agent | `spec_section` the signal inventory before filing Step 2 tasks | Section returns 12 rows; MCP index reflects new spec |
| 2 | Developer | Read diffusion physics to implement `DiffusionKernel` (Stage 1.2) | Separable Gaussian + anisotropy + decay + clamp rules fully specified |

## 4. Current State

### 4.1 Domain behavior

Signal contract lives only in `docs/city-sim-depth-exploration.md` Design Expansion + city-sim-depth master plan locked-decisions header. No canonical reference spec.

### 4.2 Systems map

- `ia/specs/simulation-system.md` — add §Tick execution order addendum w/ signal phase.
- `ia/specs/glossary.md` — add rows for new terms (`SignalField`, `SignalFieldRegistry`, `DiffusionKernel`, `SignalTickScheduler`, `SimulationSignal`, rollup rule).
- `docs/city-sim-depth-exploration.md` §Design Expansion — source truth.
- Invariant #12 (permanent signal contract → `ia/specs/`).

## 5. Proposed Design

### 5.1 Target behavior

Reference spec replaces exploration doc as canonical source for downstream impl tasks. Authored once; revised via normal reference-spec workflow.

### 5.2 Architecture / implementation

Pure Markdown. Structure:

1. **Purpose + scope** — close spec gap; permanent signal contract per invariant #12.
2. **Signal inventory** table — 12 rows.
3. **Diffusion physics contract** — separable Gaussian, anisotropy, decay, clamp-floor-0.
4. **Interface contract** — `ISignalProducer` + `ISignalConsumer` method signatures + ordering rules (producers → diffusion → consumers → rollup).
5. **Rollup rule table** — Crime P90, TrafficLevel P90, rest Mean.
6. **Tick phase insertion** — cross-ref `simulation-system.md` §Tick execution order (between `UrbanCentroidService.RecalculateFromGrid` and `AutoRoadBuilder.ProcessTick`).
7. **Spec-gap closure note** — links to city-sim-depth master plan header.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | File under `ia/specs/` not `ia/projects/` | Invariant #12 — permanent domain | `ia/projects/simulation-signals.md`; rejected — not issue-scoped |

## 7. Implementation Plan

### Phase 1 — Draft + cross-links

- [ ] Write `ia/specs/simulation-signals.md` w/ 5 required sections.
- [ ] Populate signal inventory table (12 rows) — source/sink types from master plan header + exploration doc.
- [ ] Populate rollup rule table.

### Phase 2 — Glossary + sim-system link + validate

- [ ] Append §Tick execution order addendum to `ia/specs/simulation-system.md` linking new spec.
- [ ] Add glossary rows for new terms.
- [ ] Regenerate IA indexes (`npm run generate:ia-indexes`).
- [ ] `npm run validate:all` clean.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Spec present + MCP index reflects new doc | Node | `npm run validate:all` | Chains `generate:ia-indexes --check` |
| Cross-link from `simulation-system.md` resolves | Node | `npm run validate:all` | Dead-link detector |

## 8. Acceptance Criteria

- [ ] `ia/specs/simulation-signals.md` authored w/ 5 required sections.
- [ ] Signal inventory has exactly 12 rows matching locked list.
- [ ] Rollup table: P90 for `Crime` + `TrafficLevel`; Mean for rest.
- [ ] `ia/specs/simulation-system.md` §Tick execution order references new spec.
- [ ] Glossary rows added for new terms.
- [ ] `npm run validate:all` clean.

## Open Questions

None — doc-only; contents locked by master plan header + exploration Design Expansion.
