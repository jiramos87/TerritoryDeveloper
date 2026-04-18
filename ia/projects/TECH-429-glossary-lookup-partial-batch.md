---
purpose: "TECH-429 — wire glossary_lookup bulk-terms partial-result through envelope."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md"
task_key: "T2.3.4"
---
# TECH-429 — Wire glossary_lookup bulk-terms partial-result through envelope

> **Issue:** [TECH-429](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Wire bulk-`terms` partial-result shape (from Stage 1.1 TECH-314 / TECH-315) through Stage 2.2 TECH-400 envelope wrapper. Ensure `meta.partial: {succeeded, failed}` propagates into `EnvelopeMeta`. Single-`term` path still returns unwrapped `GlossaryEntry` in `payload` (back-compat preserved per Stage 1.1 contract).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `glossary_lookup({ terms: [...] })` → envelope `payload: { results, errors }` + `meta.partial`.
2. Single-`term` unchanged — returns unwrapped `GlossaryEntry` in `payload`.
3. Empty `terms: []` → `ok: true`, `results: {}`, `errors: {}`, `meta.partial: {0, 0}`.
4. All-fail bulk → `ok: false, error.code: "invalid_input"` w/ hint listing failure terms.

### 2.2 Non-Goals

1. No graph-freshness metadata wiring (Stage 3.3 TECH scope).
2. No new fuzzy-match logic.
3. No single-term shape changes.

## 4. Current State

### 4.2 Systems map

- `tools/mcp-ia-server/src/tools/glossary-lookup.ts` — Stage 1.1 added bulk-`terms` handler + partial map; Stage 2.2 TECH-400 wrapped in `wrapTool`.
- Stage 2.1 `envelope.ts` — `EnvelopeMeta.partial` field.
- Graph-freshness fields (`graph_generated_at`, `graph_stale`) handled separately in Stage 3.3.

## 5. Proposed Design

### 5.2 Architecture / implementation

In bulk path, populate `EnvelopeMeta.partial` from internal succeeded/failed tallies. Single-term path: unchanged. Empty array: early-return envelope w/ empty maps + zero tallies. All-fail: envelope `ok: false` w/ `invalid_input` + failure terms in `error.hint`.

## 7. Implementation Plan

### Phase 1 — Meta wiring + tests

- [ ] Extend bulk path to emit `meta.partial` into envelope response.
- [ ] Preserve single-term back-compat.
- [ ] Unit tests: bulk mixed, single, empty, all-fail.
- [ ] Snapshot test updates.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Bulk partial + meta | Node unit | `cd tools/mcp-ia-server && npm test` | Assert `meta.partial.succeeded` / `failed` |
| Single-term unchanged | Node unit | same | Snapshot matches pre-Stage-2.3 |
| `validate:all` green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] Bulk / single / empty / all-fail paths return correct envelope.
- [ ] `meta.partial` counters accurate.
- [ ] Single-term snapshot unchanged from pre-Stage-2.3 baseline.
- [ ] `validate:all` green.

## Open Questions

1. None — tooling only; see §8 Acceptance criteria.
