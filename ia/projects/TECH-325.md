---
purpose: "TECH-325 — Test backlog_record_validate against fixtures."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-325 — Test `backlog_record_validate` against fixtures

> **Issue:** [TECH-325](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Fixture coverage for the `backlog_record_validate` MCP tool (TECH-324). Locks the lint contract before downstream consumers (IP6 `backlog_record_create`) start depending on specific error text. Closes Phase 1 of Stage 1.2.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New file `tools/mcp-ia-server/tests/tools/backlog-record-validate.test.ts`.
2. ≥1 good-record fixture → `ok: true`, `errors: []`.
3. ≥4 bad-record fixtures covering: missing required field, bad id format, invalid status enum, `depends_on: [TECH-1]` w/ empty `depends_on_raw`.
4. Snapshot-stable error text per case.

### 2.2 Non-Goals

1. Cross-record checks — IP8.
2. Soft-dep marker round-trip — already covered by TECH-301.
3. Warning cases (IP8 drift warning) — Stage 2.2.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Future IP6 author | As `backlog_record_create` implementer, want stable error text contract so my integration tests don't flake on upstream churn. | Snapshots stable; error strings frozen. |

## 4. Current State

### 4.1 Domain behavior

No fixture coverage exists for the tool (TECH-324 ships handler-only).

### 4.2 Systems map

- `tools/mcp-ia-server/tests/tools/` — existing harness (see `backlog-issue.test.ts` etc.).
- TECH-324 handler — direct dependency.

## 5. Proposed Design

### 5.2 Architecture

- Use existing test runner (`vitest` / `node:test` — match neighbors).
- Inline yaml bodies as template literals; no separate fixture directory needed for this scale.
- Assert `{ ok, errors, warnings }` shape + key substrings in errors.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Inline template-literal fixtures | Self-contained per case; no cross-file indirection | `tests/fixtures/` dir — rejected at this scale |

## 7. Implementation Plan

### Phase 1 — Fixtures + assertions

- [ ] Author test file w/ good + 4 bad fixtures.
- [ ] Assert exact error substring per case.
- [ ] `npm run test:ia` (or project test command) green.
- [ ] `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tool lint coverage | Node | `npm run validate:all` | Chain runs MCP tests |

## 8. Acceptance Criteria

- [ ] ≥1 good + ≥4 bad fixtures.
- [ ] Each bad fixture asserts key error substring.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only.
