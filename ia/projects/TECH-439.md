---
purpose: "TECH-439 — Test `backlog_list` locator filters."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/backlog-yaml-mcp-alignment-master-plan.md"
task_key: "T4.2.2"
---
# TECH-439 — Test `backlog_list` locator filters

> **Issue:** [TECH-439](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Extend `tools/mcp-ia-server/tests/tools/backlog-list.test.ts` to cover the three new locator filters shipped in TECH-438 (`parent_plan` / `stage` / `task_key`). Fixture set spans ≥2 parent plans + ≥2 stages. Each filter alone + combined w/ existing `priority` / `type` + empty result + scope switch. Satisfies Stage 4.2 Phase 1 exit of `backlog-yaml-mcp-alignment-master-plan.md`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Fixture set covers schema-v2 records across ≥2 plans + ≥2 stages.
2. Each new filter asserted alone (single-filter path).
3. Multi-filter intersection w/ existing `priority` / `type` filters asserted.
4. Empty-result case asserted (filter excludes all).
5. Scope switch (open vs archive) exercised w/ new filters.
6. id-desc ordering asserted in each case.
7. Lowercase substring compare — mixed-case input matches.

### 2.2 Non-Goals

1. Filter impl — lives in TECH-438.
2. Catalog docs — lives in TECH-440.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Dev | As the maintainer, I want test coverage proving new filters behave per spec so future refactors don't regress silently. | All new assertions pass green; refactor-safety maintained. |

## 4. Current State

### 4.1 Domain behavior

`backlog-list.test.ts` covers existing filter dimensions. Schema-v2 records exist in fixture dir but aren't filter-asserted.

### 4.2 Systems map

- `tools/mcp-ia-server/tests/tools/backlog-list.test.ts` — test surface.
- `tools/scripts/test-fixtures/` — fixture yaml store.
- `tools/mcp-ia-server/src/tools/backlog-list.ts` — target under test.
- Depends on TECH-438 filter impl.

## 5. Proposed Design

### 5.1 Target behavior

Test file imports `backlog_list` tool handler, loads fixture yaml via existing test harness, asserts each new filter path. Mixed-case input cases assert lowercase compare.

### 5.2 Architecture / implementation

Implementer decides exact fixture file layout. Prefer re-using existing fixture records where possible; add new ones only where ≥2-plan / ≥2-stage coverage demands it.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Tests separate from filter impl (TECH-438) | Keeps per-issue scope small; impl + tests often co-reviewed but split for parallel author-ability | Fold tests into TECH-438 (rejected — same author risk) |

## 7. Implementation Plan

### Phase 1 — Fixture + test extension

- [ ] Inventory existing `backlog-list.test.ts` fixture set.
- [ ] Add / extend fixtures to cover ≥2 plans + ≥2 stages + ≥2 task_keys.
- [ ] Author assertion blocks for each new filter alone + intersections + empty + scope switch.
- [ ] Mixed-case input assertion block.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| New coverage green | Node | `npm run validate:all` | Chains mcp-server tests |

## 8. Acceptance Criteria

- [ ] Fixture set covers ≥2 plans + ≥2 stages + ≥2 task_keys.
- [ ] Each new filter asserted alone.
- [ ] Multi-filter intersection w/ existing `priority` / `type` asserted.
- [ ] Empty-result + scope switch + mixed-case input asserted.
- [ ] id-desc ordering asserted.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
