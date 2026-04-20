---
purpose: "TECH-507 — R1 SSE cache-commit event gate + C4 progress-emit marker extension."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T10.6"
phases:
  - "Phase 1 — Skill edits"
  - "Phase 2 — Audit + validate"
---
# TECH-507 — R1 SSE cache-commit event gate + C4 progress-emit marker extension

> **Issue:** [TECH-507](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-20

## 1. Summary

Edit `ia/skills/subagent-progress-emit/SKILL.md` to add §SSE cache-commit gate (R1) and extend `⟦PROGRESS⟧` marker with optional `cache:` + `tokens:` suffix (C4). Forbid ms-latency heuristics. Document Q17 upstream-pending note in §Caveats. Audit 15 lifecycle skills' `phases:` frontmatter for backwards compatibility.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `subagent-progress-emit` SKILL carries §SSE cache-commit gate + §Caveats Q17 note + ms-latency-heuristic forbidden clause.
2. `⟦PROGRESS⟧` marker spec extended with optional `cache:` + `tokens:` suffix; default emit unchanged.
3. 15 lifecycle skills' `phases:` frontmatter audited; backwards-compat confirmed (no forced schema break).
4. `npm run validate:all` green.

### 2.2 Non-Goals (Out of Scope)

1. Implementing the SSE event consumer in Claude Code (upstream dependency Q17).
2. D2/D3 notes (T10.7 scope).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Progress markers carry cache telemetry when usage available | PROGRESS marker spec shows optional cache+tokens suffix |

## 4. Current State

### 4.1 Domain behavior

`⟦PROGRESS⟧` stderr marker has fixed shape. No cache telemetry. ms-latency heuristics undocumented but not forbidden.

### 4.2 Systems map

Edits: ia/skills/subagent-progress-emit/SKILL.md (§SSE gate, §Caveats, marker spec).
Reads: all 15 lifecycle skills' `phases:` frontmatter (audit only — backwards compat).

### 4.3 Implementation investigation notes (optional)

R1: `message_start.usage.cache_creation_input_tokens > 0` = cache written; `cache_read_input_tokens > 0` = cache hit; else = cache miss or n/a. Fallback: `content_block_delta` event arrival = conservative commit signal. Zero regression: default emit `⟦PROGRESS⟧ {skill} {N}/{T} — {phase}` unchanged.

## 5. Proposed Design

### 5.1 Target behavior (product)

SSE cache-commit gate documented in skill. `⟦PROGRESS⟧` marker optionally carries `cache:{written|hit|miss|n/a} tokens:{N}` when usage data present. ms-latency heuristics explicitly forbidden.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Add §SSE cache-commit gate section to `subagent-progress-emit` SKILL.
2. Extend marker spec: `⟦PROGRESS⟧ {skill} {N}/{T} — {phase} [cache:{state} tokens:{N}]?`.
3. Add §Caveats with Q17 note + ms-latency-heuristic forbidden clause.
4. Audit 15 lifecycle skills for `phases:` frontmatter — confirm no forced schema break.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Optional suffix only | Zero regression on existing skills | Mandatory cache suffix (breaking) |

## 7. Implementation Plan

### Phase 1 — Skill edits

- [ ] Add §SSE cache-commit gate to `subagent-progress-emit` SKILL.
- [ ] Extend `⟦PROGRESS⟧` marker spec.
- [ ] Add §Caveats Q17 note + ms-latency forbidden clause.

### Phase 2 — Audit + validate

- [ ] Audit 15 lifecycle skills' `phases:` frontmatter — confirm backwards compat.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| validate:all | Node | `npm run validate:all` | Tooling only |

## 8. Acceptance Criteria

- [ ] `subagent-progress-emit` SKILL carries §SSE cache-commit gate + §Caveats Q17 note + ms-latency-heuristic forbidden clause.
- [ ] `⟦PROGRESS⟧` marker spec extended with optional `cache:` + `tokens:` suffix; default emit unchanged.
- [ ] 15 lifecycle skills' `phases:` frontmatter audited; backwards-compat confirmed (no forced schema break).
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: Optional SSE suffix changes log parsers that key on exact `⟦PROGRESS⟧` prefix — Mitigation: document suffix as optional; default line unchanged (zero regression).
- Risk: Q17 upstream (Anthropic) — SSE `usage` fields may differ by model; Mitigation: conservative fallbacks (`content_block_delta`) documented in R1 section.
- Ambiguity: Which lifecycle skills count as "15" — Resolution: use same list as `ia/skills/README.md` lifecycle set (or `AGENTS.md` table); audit is checklist, not code.
- Invariant touch: none (tooling / docs only).

### §Examples

| Scenario | `⟦PROGRESS⟧` line | Interpretation |
|----------|-------------------|----------------|
| Default (no usage) | `⟦PROGRESS⟧ subagent-progress-emit 2/5 — Load Stage context` | Unchanged from today |
| Usage available | `⟦PROGRESS⟧ plan-author 1/1 — Bulk author §Plan Author cache:written tokens:4096` | Optional `cache:` + `tokens:` suffix |
| Miss / n/a | `… cache:miss tokens:0` | Documented enum in SKILL |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| default_marker_unchanged | `subagent-progress-emit` SKILL default emit | No required `cache:` token when usage absent | manual |
| sse_gate_doc_present | SKILL §SSE cache-commit gate | R1 signals + `content_block_delta` fallback documented | manual |
| ms_latency_forbidden | SKILL §Caveats + body | No ms-based cache-commit heuristic | grep + manual |
| q17_caveat | §Caveats | Q17 upstream-pending note present | manual |
| lifecycle_phases_audit | 15 lifecycle SKILL.md files | `phases:` frontmatter still valid YAML list; no forced break | manual |
| validate_all_green | repo root | `npm run validate:all` exit 0 | node |

### §Acceptance

- [ ] `subagent-progress-emit` SKILL documents §SSE cache-commit gate (R1) + forbids ms-latency heuristics.
- [ ] §Caveats includes Q17 upstream note.
- [ ] `⟦PROGRESS⟧` spec documents optional `cache:{written|hit|miss|n/a} tokens:{N}` suffix; default emit unchanged.
- [ ] 15 lifecycle skills audited; no forced schema break.
- [ ] `npm run validate:all` green.

### §Findings

- Optional: cross-link `docs/prompt-caching-mechanics.md` from new §SSE section if not already linked from skill Overview (implementer choice).

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `plan-applier` reads tuples + applies + re-enters `/verify-loop`._
