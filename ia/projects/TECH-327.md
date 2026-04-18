---
purpose: "TECH-327 — Concurrency test for reserve_backlog_ids."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-327 — Concurrency test for `reserve_backlog_ids`

> **Issue:** [TECH-327](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Concurrency harness at the MCP layer for `reserve_backlog_ids` (TECH-326). Mirrors `tools/scripts/test/reserve-id-concurrent.sh` but through the Node MCP surface — ensures Node spawn + flock-backed script interaction yields zero duplicate ids under parallel load. Closes Phase 2 of Stage 1.2.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New file `tools/mcp-ia-server/tests/tools/reserve-backlog-ids.test.ts`.
2. Spawn 8 parallel `reserve_backlog_ids({ prefix: "TECH", count: 2 })` invocations via `Promise.all`.
3. Assert: 16 ids collected, all unique, monotonic range `[baseline+1 .. baseline+16]`.
4. Assert counter file advanced by exactly 16.
5. Use a throwaway counter fixture dir (env override `IA_COUNTER_FILE`) — do NOT consume real ids.

### 2.2 Non-Goals

1. Test the bash script directly — already covered by `reserve-id-concurrent.sh`.
2. Fault injection (script fails mid-run) — follow-up.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | CI agent | As CI, want parallel-safety proof at MCP layer so `/release-rollout` parallel dispatches stay race-free. | 16 unique ids, counter correct. |

## 4. Current State

### 4.1 Domain behavior

`tools/scripts/test/reserve-id-concurrent.sh` covers script-layer concurrency. MCP layer uncovered — Node spawn overhead could theoretically interact badly w/ flock.

### 4.2 Systems map

- TECH-326 tool — subject under test.
- `tools/scripts/reserve-id.sh` — env-var overrides `IA_COUNTER_FILE` + `IA_COUNTER_LOCK`.
- Test harness pattern: neighbor `tests/tools/*.test.ts`.

## 5. Proposed Design

### 5.2 Architecture

- Test `beforeEach`: mkdtemp dir, seed counter file `{"TECH":100}`, set env vars.
- Invoke handler directly (not via MCP transport) — 8x parallel Promise.all.
- Collect all ids; set + sort; assert length 16 + min 101 + max 116 + no duplicates.
- Read counter file post-run; assert `TECH === 116`.
- `afterEach`: rm tmpdir.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Handler-direct invocation | Avoids MCP transport noise; isolates Node spawn + flock path | Full MCP client round-trip — rejected, slower + not load-bearing |

## 7. Implementation Plan

### Phase 1 — Harness + asserts

- [ ] Write test w/ tmpdir + env override setup.
- [ ] 8x parallel handler calls; collect ids.
- [ ] Assert uniqueness + monotonic range + counter advance.
- [ ] `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Parallel-safe at MCP layer | Node | `npm run validate:all` | Chain covers MCP tests |

## 8. Acceptance Criteria

- [ ] Test spawns 8 parallel calls; 16 distinct ids.
- [ ] Counter advances by exactly 16 (no gap, no dup).
- [ ] Tmpdir + env override isolates from real counter file.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only. Flock unavailability on macOS CI agents handled at script layer (install hint). Test skips gracefully if `flock` not on PATH? Decision at implementation: skip-with-warn vs hard-fail. Log in §6 when resolved.
