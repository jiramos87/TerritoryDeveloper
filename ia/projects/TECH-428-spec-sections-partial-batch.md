---
purpose: "TECH-428 — partial-result batch shape for spec_sections."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md"
task_key: "T2.3.3"
---
# TECH-428 — Partial-result batch shape for spec_sections

> **Issue:** [TECH-428](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Refactor `spec-sections.ts` to return partial-result batch: `{ results: {[spec_key]: {sections: [...]}}, errors: {[spec_key]: {code, message}}, meta: {partial: {succeeded, failed}} }`. One bad input key no longer fails whole batch. Envelope `ok: true` when ≥1 request succeeds; all-fail → `ok: false, error.code: "invalid_input"`. Mirrors Stage 1.1 `glossary_lookup` bulk-terms shape.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Mixed good + bad keys → `ok: true`, `results` populated for good, `errors` for bad, `meta.partial` counters correct.
2. All-good → `errors: {}`, `meta.partial: {succeeded: N, failed: 0}`.
3. All-bad → `ok: false, error.code: "invalid_input"`, hint lists failure keys.
4. `meta.partial` propagates via `EnvelopeMeta` (Stage 2.1 shape).

### 2.2 Non-Goals

1. No behavior change to individual `spec_section` call.
2. No parallelism — per-request loop is sequential (deferred).
3. No alias resurrection — canonical params only (TECH-426).

## 4. Current State

### 4.2 Systems map

- `tools/mcp-ia-server/src/tools/spec-sections.ts` — current returns first-fail error envelope.
- Stage 2.1 `envelope.ts` — `EnvelopeMeta.partial` field ready.
- Stage 1.1 glossary bulk-terms — pattern template.

## 5. Proposed Design

### 5.2 Architecture / implementation

Iterate input requests; per-request try/catch around inner `spec_section` dispatch; accumulate into `results` map (success key) or `errors` map (failure key). Emit `meta.partial: {succeeded, failed}`. Envelope success path: `ok: true` when `succeeded ≥ 1`. All-fail → `ok: false, error.code: "invalid_input"`.

## 7. Implementation Plan

### Phase 1 — Batch refactor

- [ ] Replace current fail-fast loop w/ partial-accumulation loop.
- [ ] Wire `meta.partial` into envelope response.
- [ ] Unit tests: mixed / all-good / all-bad; verify envelope `ok` flip on all-fail.
- [ ] Snapshot test updates.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Mixed batch partial | Node unit | `cd tools/mcp-ia-server && npm test` | Results + errors both populated |
| All-fail ok=false | Node unit | same | Envelope flips to error |
| `validate:all` green | Node | `npm run validate:all` | |

## 8. Acceptance Criteria

- [ ] Mixed / all-good / all-bad paths return correct envelope.
- [ ] `meta.partial` counters accurate.
- [ ] Snapshot tests updated.
- [ ] `validate:all` green.

## Open Questions

1. None — tooling only; see §8 Acceptance criteria.
