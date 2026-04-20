---
purpose: "TECH-524 — Extract IA-core + bridge servers."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T1.2.1"
---
# TECH-524 — Extract IA-core + bridge servers

> **Issue:** [TECH-524](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Extract single territory-ia MCP server into IA-core + bridge dual-server shape behind MCP_SPLIT_SERVERS feature flag. IA-authoring sessions load lean core; verify/implement stages opt-in to bridge. Flag default off in this Stage; flip in Stage 1.3 post-sweep.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New file `tools/mcp-ia-server/src/index-ia.ts` registers ≥22 IA-authoring tools.
2. New file `tools/mcp-ia-server/src/index-bridge.ts` registers 14 Unity-bridge + compute tools.
3. `index.ts` retains backward-compat default; imports both server modules.
4. `MCP_SPLIT_SERVERS=1` selects `index-ia.ts` standalone path.

### 2.2 Non-Goals (Out of Scope)

1. Flipping `MCP_SPLIT_SERVERS` default to `1` — deferred to Stage 1.3 post-sweep.
2. Per-agent allowlist narrowing (B3) — Stage 1.3 T1.3.1.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer running IA-authoring sessions, I want lean MCP server load so that bridge tools don't inflate context. | `MCP_SPLIT_SERVERS=1` path loads only IA-core tools. |

## 4. Current State

### 4.1 Domain behavior

Single `index.ts` registers all ≥36 tools (IA-authoring + Unity-bridge + compute). IA-authoring sessions load bridge tools unnecessarily — token overhead B1 target.

### 4.2 Systems map

New: `tools/mcp-ia-server/src/index-ia.ts`.
New: `tools/mcp-ia-server/src/index-bridge.ts`.
Touches: `tools/mcp-ia-server/src/index.ts` (env-check + dual-import).
No Unity / C# / runtime surface touched.

### 4.3 Implementation investigation notes (optional)

Phase 1 — Server extraction.
1. Inventory current `index.ts` tool registrations (≥36 total).
2. Bucket tools: IA-authoring (backlog/router/glossary/spec/rules/invariants/journal/reserve/materialize) vs Unity-bridge + compute (14).
3. Author `index-ia.ts`: import shared registration helpers + register IA-authoring tools.
4. Author `index-bridge.ts`: register the 14 bridge tools.
5. Edit `index.ts`: add `MCP_SPLIT_SERVERS` env check; default path imports both; `=1` path loads `index-ia.ts` only.
6. Run `npm run validate:all`.

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
| 2026-04-20 | Flag default off | Post-sweep flip in Stage 1.3 after correctness confirmed (NB-6). | Immediate flip — risky pre-validation. |

## 7. Implementation Plan

### Phase 1 — Server extraction

- [ ] Inventory `index.ts` registrations; bucket into IA-authoring vs bridge.
- [ ] Author `index-ia.ts` registering IA-authoring tools.
- [ ] Author `index-bridge.ts` registering 14 bridge tools.
- [ ] Edit `index.ts`: `MCP_SPLIT_SERVERS` env check + dual-import default.
- [ ] Run `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| MCP server split wiring | Node | `npm run validate:all` | Chains validate:dead-project-specs + validate:backlog-yaml |

## 8. Acceptance Criteria

- [ ] `tools/mcp-ia-server/src/index-ia.ts` exists, registers ≥22 IA-authoring tools.
- [ ] `tools/mcp-ia-server/src/index-bridge.ts` exists, registers 14 Unity-bridge + compute tools.
- [ ] `index.ts` retains backward-compat default; imports both server modules.
- [ ] `MCP_SPLIT_SERVERS=1` selects `index-ia.ts` standalone path.
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
