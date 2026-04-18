---
purpose: "TECH-329 — Test backlog_list filter combinations."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-329 — Test `backlog_list` filter combinations

> **Issue:** [TECH-329](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Fixture coverage for `backlog_list` MCP tool (TECH-328). Closes Phase 3 of Stage 1.2. Locks filter semantics before `backlog_search` filter extensions in Stage 2.3 (IP9) — downstream tool will re-use the same filter field names.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New file `tools/mcp-ia-server/tests/tools/backlog-list.test.ts`.
2. Fixture set covering ≥2 sections, ≥2 priorities, ≥2 types, open + archive records.
3. Assert scope switch (open / archive / all) returns correct sets.
4. Single-filter cases: `section` / `priority` / `type` / `status` each work standalone.
5. Multi-filter intersection case (≥2 filters AND).
6. Empty-result case — no error, `{ issues: [], total: 0 }`.
7. Id-desc ordering stable across runs.

### 2.2 Non-Goals

1. Date-range filters — IP9.
2. Large-N performance — defer.
3. Fuzz fixtures — follow-up if drift found.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Stage 2.3 author | As future `backlog_search` extender, want stable filter-field semantics so my additions compose cleanly. | Filter names + behavior frozen by this test. |

## 4. Current State

### 4.1 Domain behavior

TECH-328 ships handler-only. No tests.

### 4.2 Systems map

- TECH-328 handler — subject.
- Test harness pattern: `tests/tools/backlog-issue.test.ts`, `backlog-search.test.ts`.

## 5. Proposed Design

### 5.2 Architecture

- Tmpdir fixture: seed a small set of yaml records (5–7) covering section/priority/type variance + 1–2 archive records.
- Handler-direct invocation per case.
- Assert count + id list per case; snapshot ordering.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Tmpdir yaml fixtures | Isolates from real backlog churn | Mock `parseAllBacklogIssues` — rejected, weaker coverage |

## 7. Implementation Plan

### Phase 1 — Fixtures + filter matrix

- [ ] Seed tmpdir w/ fixture records.
- [ ] Case: no filter → all open.
- [ ] Case: each single filter.
- [ ] Case: multi-filter AND.
- [ ] Case: empty result.
- [ ] Case: scope = archive + scope = all.
- [ ] Ordering snapshot.
- [ ] `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Filter coverage | Node | `npm run validate:all` | MCP tests chain |

## 8. Acceptance Criteria

- [ ] ≥6 test cases covering filter matrix.
- [ ] Ordering snapshot stable.
- [ ] Empty-result case returns `{ issues: [], total: 0 }`.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only.
