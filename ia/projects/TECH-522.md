---
purpose: "TECH-522 — SessionStart re-injection + deterministic preamble compat."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T5.1.3"
---
# TECH-522 — SessionStart re-injection + deterministic preamble compat

> **Issue:** [TECH-522](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Phase 2 re-injection half. Extends `session-start-prewarm.sh` to cat pack content in
volatile suffix zone, preserving Stage 3.1 D2 cacheable deterministic prefix.
Implements 24 h freshness gate + graceful absence.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `session-start-prewarm.sh` cats `.claude/context-pack.md` after deterministic block + `---` separator.
2. Existence gate (`-f`) + 24 h freshness gate on pack `ts` header.
3. Stale pack → stderr warning, no stdout.
4. Missing pack → silent (no stderr, no stdout).
5. BSD + GNU `date` parsing both supported.
6. Deterministic prefix byte-stable across runs (diff-verified).
7. `§Session continuity` doc sub-section extended with re-injection contract.

### 2.2 Non-Goals (Out of Scope)

1. Digest script authoring (handled in TECH-520 + TECH-521).
2. Integration test protocol (handled in TECH-523).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Resumed session's SessionStart preamble includes pack content when pack is fresh | cat output present in preamble; deterministic prefix unchanged |
| 2 | Developer | Stale or missing pack does not pollute preamble | Stale → stderr warning only; missing → no output at all |

## 4. Current State

### 4.1 Domain behavior

`session-start-prewarm.sh` (Stage 3.1 T3.1.1) emits deterministic preamble only. No re-injection of pack content. Resuming after `/compact` loses all active-task context.

### 4.2 Systems map

Touches: `tools/scripts/claude-hooks/session-start-prewarm.sh` (Stage 3.1 T3.1.1 output).
Touches: `docs/agent-led-verification-policy.md` §Session continuity.
Reads: `.claude/context-pack.md`.
No Unity / C# / runtime surface touched.

## 5. Proposed Design

### 5.1 Target behavior (product)

After deterministic preamble block + `---` separator: check `-f .claude/context-pack.md`; parse `ts` header; if age <24 h, `cat .claude/context-pack.md`. If age ≥24 h, emit stderr warning `stale context pack ({age_hours} h old); regenerate via /pack-context`, no stdout. Missing pack → silent.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phase 2 — Re-injection.
1. Append conditional block to `session-start-prewarm.sh`: `-f` + ts-age check.
2. Portable age-compute: try `date -jf` (BSD) first, fallback `date -d` (GNU).
3. 24 h gate → cat; stale → stderr warning line, no stdout.
4. Verify deterministic prefix byte-stable: run twice with different pack content; diff preamble up to `---` separator (must be identical).
5. Extend `docs/agent-led-verification-policy.md` §Session continuity with re-injection contract.
6. `npm run validate:all`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Volatile suffix placement (after `---` separator) | Preserves Stage 3.1 D2 cacheable prefix | Before separator — breaks cacheability guarantee |
| 2026-04-20 | 24 h staleness gate | Stale pack misleads more than helps | No gate — stale data injected silently |

## 7. Implementation Plan

### Phase 2 — Re-injection + preamble compat

- [ ] Append conditional block to `session-start-prewarm.sh` (`-f` + ts-age check).
- [ ] Implement portable age-compute (BSD `date -jf` + GNU `date -d` fallback).
- [ ] Fresh → cat; stale → stderr warning; missing → silent.
- [ ] Diff-verify deterministic prefix byte-stable across two runs with different pack content.
- [ ] Extend `docs/agent-led-verification-policy.md` §Session continuity with re-injection contract.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| IA tooling changes (docs update) | Node | `npm run validate:all` | Chains validate:dead-project-specs, test:ia, validate:fixtures |
| Deterministic prefix byte-stable | Shell diff | Two runs with different pack content; diff up to `---` separator | Manual; must return empty diff |

## 8. Acceptance Criteria

- [ ] `session-start-prewarm.sh` cats `.claude/context-pack.md` after deterministic block + `---` separator.
- [ ] Existence gate (`-f`) + 24 h freshness gate on pack `ts` header.
- [ ] Stale pack → stderr warning, no stdout.
- [ ] Missing pack → silent (no stderr, no stdout).
- [ ] BSD + GNU `date` parsing both supported.
- [ ] Deterministic prefix byte-stable across runs (diff-verified).
- [ ] `§Session continuity` doc sub-section extended with re-injection contract.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

_pending — populated by `/author ia/projects/session-token-latency-master-plan.md Stage 5.1`. 4 sub-sections: §Audit Notes / §Examples / §Test Blueprint / §Acceptance._

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
