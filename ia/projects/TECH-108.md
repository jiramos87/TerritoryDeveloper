---
purpose: "TECH-108 — Save/load round-trip test for neighbor stubs + bindings."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-108 — Save/load round-trip: stubs + bindings preserved

> **Issue:** [TECH-108](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Stage 1.3 Phase 4 opener. Verification-only — exercise save + load path post new-game + interstate build; confirm `neighborStubs` list + binding data byte-identical across round-trip. Uses testmode batch scenario similar to TECH-89 (parent-id round-trip) pattern.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Testmode batch scenario: new-game → build interstate to border → save → load → assert stub + binding match pre-save.
2. Exit 0 + zero violations.
3. Report artifact under `tools/reports/`.

### 2.2 Non-Goals

1. New production code — test-only.
2. Legacy save migration (covered by TECH-103).

## 4. Current State

### 4.2 Systems map

- `tools/` testmode batch runner; `AgentTestModeBatchRunner` (see TECH-89 / TECH-97 precedents).
- Depends: TECH-103–106 (save wiring + binding).
- Orchestrator: Stage 1.3.

## 5. Proposed Design

### 5.2 Architecture / implementation

- Extend testmode scenario or add new `neighbor-stub-roundtrip` scenario.
- Pre-save snapshot list + bindings → save → load → re-read → assert equal.

## 7. Implementation Plan

### Phase 1 — Scenario

- [ ] Author testmode scenario.
- [ ] Run `unity:testmode-batch`; exit 0; report saved.
- [ ] `validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Round-trip equal | Testmode | `npm run unity:testmode-batch --scenario neighbor-stub-roundtrip` | exit 0 |

## 8. Acceptance Criteria

- [ ] Testmode batch exit 0; stub + binding fields equal pre- vs post-save.
- [ ] Report `tools/reports/agent-testmode-batch-*.json` attached.
- [ ] `validate:all` green.

## Open Questions

1. None — verification scenario.
