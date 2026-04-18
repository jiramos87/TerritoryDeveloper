---
purpose: "TECH-315 — MCP glossary_lookup bulk-terms unit tests."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-315 — MCP `glossary_lookup` bulk-terms unit tests

> **Issue:** [TECH-315](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Unit tests covering TECH-314 bulk-terms extension of `glossary_lookup`. Four cases: bulk happy path (all terms found), partial failure (mix hit/miss), single-`term` back-compat, empty `terms: []` edge. Lands Stage 1.1 of MCP lifecycle 4.7 audit alongside TECH-314 handler.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Bulk happy path: all provided terms present in glossary → all entries in `results`, `errors` empty, `meta.partial: { succeeded: N, failed: 0 }`.
2. Partial failure: mixed-existence input → found terms under `results`, missing terms under `errors`, counts reflect split.
3. Single-`term` back-compat: existing shape returned unchanged; no `results` / `errors` wrapping.
4. Empty `terms: []` edge: returns `{ results: {}, errors: {}, meta.partial: { succeeded: 0, failed: 0 } }` — not thrown error.

### 2.2 Non-Goals (Out of Scope)

1. Envelope-shape snapshot tests — Stage 2.4 concern.
2. Performance / benchmark tests.

## 4. Current State

### 4.2 Systems map

- `tools/mcp-ia-server/tests/tools/glossary-lookup.test.ts` — existing single-term tests (extend, do not replace).
- `tools/mcp-ia-server/src/tools/glossary-lookup.ts` — unit under test (updated in TECH-314).

## 5. Proposed Design

### 5.2 Architecture / implementation

- Add 4 `test()` blocks to existing test file; reuse the mocked glossary fixture already present.
- Fixture: at minimum two known terms (e.g. `HeightMap`, `wet run`) + one deliberately missing (`nonexistent`).

## 7. Implementation Plan

### Phase 1 — Tests

- [ ] Bulk happy path test.
- [ ] Partial failure test.
- [ ] Single-`term` back-compat test.
- [ ] Empty `terms: []` test.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| 4 test cases green | Node | `npm run test:ia` | vitest under tools/mcp-ia-server |
| IA index alignment | Node | `npm run validate:all` | full chain |

## 8. Acceptance Criteria

- [ ] Four test cases green per §2.1 Goals 1–4.
- [ ] `npm run validate:all` green.

## Open Questions

1. None — tooling only; see §8 Acceptance criteria.
