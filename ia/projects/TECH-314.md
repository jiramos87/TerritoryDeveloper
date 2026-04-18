---
purpose: "TECH-314 — MCP glossary_lookup bulk terms handler."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-314 — MCP `glossary_lookup` bulk `terms` handler

> **Issue:** [TECH-314](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Extend `glossary_lookup` MCP tool to accept bulk `terms: string[]` alongside existing single `term: string`. Returns per-term `{ results, errors }` partial-result shape plus `meta.partial` counts. Back-compat: single `term` path unchanged. Stage 1.1 of MCP lifecycle 4.7 audit — ships as quick win before Stage 2 envelope breaking cut, test-drives partial-result pattern used across 32-tool rewrite.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `glossary_lookup({ terms: ["HeightMap", "wet run", "nonexistent"] })` returns `{ results: { HeightMap: ..., "wet run": ... }, errors: { nonexistent: { code, message } }, meta: { partial: { succeeded: 2, failed: 1 } } }`.
2. `glossary_lookup({ term: "HeightMap" })` returns existing single-entry shape unchanged (back-compat).
3. Empty `terms: []` returns `{ results: {}, errors: {}, meta.partial: { succeeded: 0, failed: 0 } }` — not error.
4. Handler remains pure-read; no mutation side effects.

### 2.2 Non-Goals (Out of Scope)

1. Envelope wrap — Stage 2.2 concern.
2. Freshness metadata (`meta.graph_generated_at`) — Stage 3.3 concern.
3. Bulk path for `glossary_discover` — not in scope for this task.

## 4. Current State

### 4.2 Systems map

- `tools/mcp-ia-server/src/tools/glossary-lookup.ts` — single-term Zod schema + handler.
- `tools/mcp-ia-server/data/glossary-index.json` — read source.
- `tools/mcp-ia-server/data/glossary-graph-index.json` — related / cited_in / appears_in_code fields.

## 5. Proposed Design

### 5.2 Architecture / implementation

- Extend Zod input schema: make `term` + `terms` both optional; require exactly one present (`refine`).
- Fan-out path: map over `terms`, call existing per-term lookup internal, push to `results` on hit, `errors[term] = { code: "term_not_found", message }` on miss.
- Back-compat branch: `term` present → existing return shape unwrapped.

## 7. Implementation Plan

### Phase 1 — Bulk handler

- [ ] Widen Zod schema — `term?`/`terms?` with XOR refine.
- [ ] Refactor single-term lookup into reusable internal function.
- [ ] Add bulk dispatcher + aggregation into `{ results, errors, meta.partial }`.
- [ ] Preserve single-term return shape for back-compat.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Bulk + single-term shapes correct | Node | `npm run test:ia` | Covered by TECH-315 tests |
| MCP / glossary index alignment | Node | `npm run validate:all` | Chains validate:dead-project-specs, test:ia, validate:fixtures, generate:ia-indexes --check |

## 8. Acceptance Criteria

- [ ] Bulk `terms` path returns partial-result shape per §2.1 Goal 1.
- [ ] Single `term` path back-compat per §2.1 Goal 2.
- [ ] Empty `terms: []` per §2.1 Goal 3.
- [ ] `npm run validate:all` green.

## Open Questions

1. None — tooling only; see §8 Acceptance criteria.
