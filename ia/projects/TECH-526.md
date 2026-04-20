---
purpose: "TECH-526 — Integration test fixture."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T1.2.3"
---
# TECH-526 — Integration test fixture

> **Issue:** [TECH-526](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Ship integration test asserting B1 server-split semantics. Two dispatches: lean IA-core path excludes bridge tools; bridge-prefix path exposes them. Wired via `npm run test:mcp-split`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New file `tools/mcp-ia-server/tests/server-split.test.ts`.
2. Test asserts `MCP_SPLIT_SERVERS=1` + IA-core dispatch hides 14 bridge tools.
3. Test asserts bridge-prefix dispatch exposes 14 bridge tools.
4. `package.json` scripts carry `test:mcp-split` entry.

### 2.2 Non-Goals (Out of Scope)

1. End-to-end agent dispatch testing — out of scope for this fixture.
2. Performance measurement — telemetry harness B7 (Stage 1.3).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want a CI gate so that server-split semantics can't regress silently. | `test:mcp-split` green; bridge tools absent from IA-core path. |

## 4. Current State

### 4.1 Domain behavior

No integration test for split-server semantics. B1 correctness unguarded by CI.

### 4.2 Systems map

New: `tools/mcp-ia-server/tests/server-split.test.ts`.
Touches: `package.json` (scripts.test:mcp-split).
Reads: `tools/mcp-ia-server/src/index-ia.ts` + `index-bridge.ts` (T1.2.1 output).
No Unity / C# / runtime surface touched.

### 4.3 Implementation investigation notes (optional)

Phase 2 — Integration test.
1. Author `server-split.test.ts`: spawn server with `MCP_SPLIT_SERVERS=1`; query `tools/list`; assert bridge tools absent.
2. Add bridge-prefix branch: query `tools/list` with bridge config; assert 14 bridge tools present.
3. Add `test:mcp-split` script to `package.json`.
4. Run `npm run test:mcp-split` locally; confirm green.
5. Run `npm run validate:all`.

## 5. Proposed Design

### 5.1 Target behavior (product)

Tooling-only. No gameplay surface touched.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

_Pending — plan-author populates._

### 5.3 Method / algorithm notes (optional)

_None._

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Integration test over unit test | Split semantics are server-level; unit mock insufficient. | Unit test — doesn't catch env-var path wiring. |

## 7. Implementation Plan

### Phase 2 — Integration test

- [ ] Author `tools/mcp-ia-server/tests/server-split.test.ts`.
- [ ] Assert `MCP_SPLIT_SERVERS=1` IA-core path hides 14 bridge tools.
- [ ] Assert bridge-prefix path exposes 14 bridge tools.
- [ ] Add `test:mcp-split` to `package.json` scripts.
- [ ] Run `npm run test:mcp-split`; confirm green.
- [ ] Run `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| B1 split semantics locked | Node | `npm run test:mcp-split` | New script; relies on T1.2.1 output |
| Overall IA wiring | Node | `npm run validate:all` | Chains validate:dead-project-specs + validate:backlog-yaml |

## 8. Acceptance Criteria

- [ ] `tools/mcp-ia-server/tests/server-split.test.ts` exists.
- [ ] Test asserts `MCP_SPLIT_SERVERS=1` + IA-core dispatch hides 14 bridge tools.
- [ ] Test asserts bridge-prefix dispatch exposes 14 bridge tools.
- [ ] `package.json` scripts carry `test:mcp-split` entry.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

_pending — populated by `/author ia/projects/session-token-latency-master-plan.md Stage 1.2`. 4 sub-sections: §Audit Notes / §Examples / §Test Blueprint / §Acceptance._

### §Audit Notes

### §Examples

### §Test Blueprint

### §Acceptance

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
