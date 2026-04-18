---
purpose: "TECH-326 — Implement reserve_backlog_ids MCP tool."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-326 — Implement `reserve_backlog_ids` MCP tool

> **Issue:** [TECH-326](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Ship `reserve_backlog_ids` MCP tool (IP3) — wrapper around `tools/scripts/reserve-id.sh` so filing skills reserve ids via MCP instead of shelling out. Preserves invariant #13 (monotonic id source = `id-counter.json` via `reserve-id.sh` under flock). Opens Phase 2 of Stage 1.2.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New file `tools/mcp-ia-server/src/tools/reserve-backlog-ids.ts`.
2. Input schema: `{ prefix: "TECH" | "FEAT" | "BUG" | "ART" | "AUDIO", count: number }` w/ `count` bounds 1..50.
3. Spawn `tools/scripts/reserve-id.sh {prefix} {count}` via `node:child_process` (`spawn` w/ `cwd: REPO_ROOT`).
4. Parse stdout: one id per line; return `{ ids: string[] }`.
5. Non-zero exit or stderr → throw MCP error w/ captured stderr.
6. Register in `tools/mcp-ia-server/src/index.ts`.

### 2.2 Non-Goals

1. Replicate flock logic in Node — script owns flock.
2. Batch writes (`backlog_record_create`) — IP6 / Stage 2.3.
3. Parallel-call correctness test — TECH-327.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Filing agent | As stage-file / project-new, want one MCP call returning N ids so I skip bash plumbing. | Single call, monotonic ids, counter advanced. |

## 4. Current State

### 4.1 Domain behavior

Agents shell out today via `bash tools/scripts/reserve-id.sh TECH N`. No MCP surface. Skills document the bash path in bodies.

### 4.2 Systems map

- `tools/scripts/reserve-id.sh` — wrapped; must not be modified.
- `ia/state/id-counter.json` — indirectly mutated via script under flock.
- `tools/mcp-ia-server/src/index.ts` — registry.
- Invariant #13 — enforces script-backed mutation.

## 5. Proposed Design

### 5.2 Architecture

- Handler spawns child proc, captures stdout + stderr + exit code.
- Parse: split stdout by newline, trim, filter empties; validate each matches `^{prefix}-\d+$`.
- Error: non-zero exit OR zero ids parsed → structured MCP error `{ code: "RESERVE_FAILED", stderr }`.
- `count` input validated client-side (1..50 inclusive) before spawn.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Spawn `reserve-id.sh` (not reimplement flock in Node) | Single source of truth; macOS flock quirks handled once | Node-side lockfile — rejected, drift risk |

## 7. Implementation Plan

### Phase 1 — Handler + registry

- [ ] Author handler w/ `spawn` + output parse.
- [ ] Input validation (prefix enum, count bounds).
- [ ] Register in tool registry.
- [ ] Happy-path unit test (single + batch).
- [ ] `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Handler + registry | Node | `npm run validate:all` | MCP boot + happy path |
| Concurrency | Follow-up | TECH-327 | Dedicated test |

## 8. Acceptance Criteria

- [ ] Tool file authored + registered.
- [ ] Single + batch happy-path works; counter advances.
- [ ] Input validation rejects bad prefix / count out of range.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only. macOS flock PATH issue (util-linux in Cellar) handled at script layer, not here.
