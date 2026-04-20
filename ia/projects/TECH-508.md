---
purpose: "TECH-508 — D2 cache-invalidation cascade note + D3 20-block single-block guardrail."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T10.7"
---
# TECH-508 — D2 cache-invalidation cascade note + D3 20-block single-block guardrail

> **Issue:** [TECH-508](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Edit `docs/mcp-ia-server.md` to add §Cache invalidation impact section (D2). Document D3 single-block rule (NEVER emit multi-block stable prefix) in `subagent-progress-emit` SKILL or new `ia/rules/subagent-caching-guardrails.md`. Cross-link both notes from `docs/prompt-caching-mechanics.md` §6 + §7.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `docs/mcp-ia-server.md` carries §Cache invalidation impact section with PR-flag requirement.
2. D3 single-block rule documented in `subagent-progress-emit` SKILL OR dedicated `subagent-caching-guardrails.md` rule.
3. `docs/prompt-caching-mechanics.md` §6 + §7 cross-link both D2 + D3 notes.
4. `npm run validate:all` green.

### 2.2 Non-Goals (Out of Scope)

1. Implementing cache invalidation detection (documentation only).
2. R1 SSE gate (T10.6 scope).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | MCP tool edits flagged as cache-invalidating in PR | §Cache invalidation impact section in mcp-ia-server.md |
| 2 | Developer | Single-block assembly rule prevents 20-block lookback regression | D3 rule documented |

## 4. Current State

### 4.1 Domain behavior

No documentation of cache invalidation cascade. Multi-block `@`-load not forbidden.

### 4.2 Systems map

Edits: docs/mcp-ia-server.md (§Cache invalidation impact).
Creates or edits: ia/skills/subagent-progress-emit/SKILL.md OR ia/rules/subagent-caching-guardrails.md.
Edits: docs/prompt-caching-mechanics.md §6 + §7 (cross-links).

### 4.3 Implementation investigation notes (optional)

Author decides: D3 in subagent-progress-emit SKILL (cohesive with R1 from T10.6) vs new rule file (cleaner separation). Either satisfies acceptance. Cross-link from prompt-caching-mechanics.md either way.

## 5. Proposed Design

### 5.1 Target behavior (product)

D2: Any `tools/mcp-ia-server/` edit = cache-invalidating event; PR author must flag in description. D3: `@`-concatenation at skill-preamble author time is the ONLY supported stable-prefix assembly mode; multi-`@`-load with separate `cache_control` per block is forbidden.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Edit `docs/mcp-ia-server.md` — add §Cache invalidation impact section.
2. Document D3 rule (author's call: subagent-progress-emit SKILL or new rule file).
3. Edit `docs/prompt-caching-mechanics.md` §6 + §7 — cross-link D2 + D3.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Author decides D3 placement | Both locations valid; avoids over-specifying | Fixed to subagent-progress-emit |

## 7. Implementation Plan

### Phase 1 — D2 cascade note

- [ ] Edit `docs/mcp-ia-server.md` — add §Cache invalidation impact section.

### Phase 2 — D3 single-block guardrail

- [ ] Document D3 in subagent-progress-emit SKILL or new `ia/rules/subagent-caching-guardrails.md`.

### Phase 3 — Cross-links + validate

- [ ] Edit `docs/prompt-caching-mechanics.md` §6 + §7.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| validate:all | Node | `npm run validate:all` | Tooling only |

## 8. Acceptance Criteria

- [ ] `docs/mcp-ia-server.md` carries §Cache invalidation impact section with PR-flag requirement.
- [ ] D3 single-block rule documented in `subagent-progress-emit` SKILL OR dedicated `subagent-caching-guardrails.md` rule.
- [ ] `docs/prompt-caching-mechanics.md` §6 + §7 cross-link both D2 + D3 notes.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

_pending — populated by `/author ia/projects/lifecycle-refactor-master-plan.md Stage 10`. 4 sub-sections: §Audit Notes / §Examples / §Test Blueprint / §Acceptance._

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
