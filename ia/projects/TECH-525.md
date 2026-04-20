---
purpose: "TECH-525 — .mcp.json split config."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T1.2.2"
---
# TECH-525 — .mcp.json split config

> **Issue:** [TECH-525](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Wire dual-server config in `.mcp.json`: register `territory-ia-bridge` alongside existing `territory-ia` entry; default flag off. Document flag semantics + flip timeline in `docs/mcp-ia-server.md`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `.mcp.json` carries `territory-ia-bridge` server entry pointing to `index-bridge.ts`.
2. `territory-ia` env block carries `MCP_SPLIT_SERVERS=0` default.
3. `docs/mcp-ia-server.md` gains `§Server split architecture` section documenting flag.
4. `npm run validate:all` green.

### 2.2 Non-Goals (Out of Scope)

1. Flipping `MCP_SPLIT_SERVERS` default to `1` — Stage 1.3 post-sweep.
2. Integration tests — T1.2.3.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want dual-server config wired so that opting into bridge server requires only flag flip. | `.mcp.json` bridge entry present; flag default `0`. |

## 4. Current State

### 4.1 Domain behavior

`.mcp.json` has single `territory-ia` entry. No bridge-server entry. No `MCP_SPLIT_SERVERS` key.

### 4.2 Systems map

Touches: `.mcp.json` (root).
Touches: `docs/mcp-ia-server.md` (new §Server split architecture section).
No Unity / C# / runtime surface touched.

### 4.3 Implementation investigation notes (optional)

Phase 1 — Config + docs.
1. Edit `.mcp.json`: add `territory-ia-bridge` server block (mirrors `territory-ia` shape, command targets `index-bridge.ts`).
2. Edit `territory-ia` env block: add `"MCP_SPLIT_SERVERS": "0"` alongside `DEBUG_MCP_COMPUTE`.
3. Author `docs/mcp-ia-server.md §Server split architecture`: rationale + flag semantics + flip timeline pointer (Stage 1.3 sweep).
4. Run `npm run validate:all`.

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
| 2026-04-20 | Flag default `0` | Flip only after Stage 1.3 sweep confirms correctness. | Default `1` — risky pre-validation. |

## 7. Implementation Plan

### Phase 1 — Config + docs

- [ ] Edit `.mcp.json`: add `territory-ia-bridge` server block.
- [ ] Edit `territory-ia` env block: add `MCP_SPLIT_SERVERS=0`.
- [ ] Author `docs/mcp-ia-server.md §Server split architecture` section.
- [ ] Run `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Config + docs wiring | Node | `npm run validate:all` | Chains validate:dead-project-specs + validate:backlog-yaml |

## 8. Acceptance Criteria

- [ ] `.mcp.json` carries `territory-ia-bridge` server entry pointing to `index-bridge.ts`.
- [ ] `territory-ia` env block carries `MCP_SPLIT_SERVERS=0` default.
- [ ] `docs/mcp-ia-server.md` gains `§Server split architecture` section documenting flag.
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
