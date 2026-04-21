---
purpose: "TECH-604 — Verify >=1 real design-explore run since Phase-N-tail wiring landed; trigger short invocation if none; document readiness in skill-train/SKILL.md §Changelog."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/skill-training-master-plan.md
task_key: T6.1
---
# TECH-604 — Dogfood readiness check (skill-train Stage 6)

> **Issue:** [TECH-604](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-21
> **Last updated:** 2026-04-21

## 1. Summary

Readiness gate before first skill-train retrospective run. Confirms design-explore
has real §Changelog self-report entry since Phase-N-tail wiring landed (Stage 3).
Triggers signal-accumulation run if §Changelog empty.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Read `ia/skills/design-explore/SKILL.md §Changelog`; check for `source: self-report` entry post-Stage-3 wiring (TECH-430).
2. If none present: invoke design-explore on existing stub doc to generate signal.
3. Confirm §Changelog entry created by that run (friction_types[] populated or empty clean run).
4. Append readiness note to `skill-train/SKILL.md §Changelog` documenting outcome.

### 2.2 Non-Goals (Out of Scope)

1. Running skill-train retrospective — that is T6.2.
2. Editing design-explore SKILL.md Phase sequence.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Confirm wiring produced real signal before dogfood run | §Changelog entry present or created after invocation |

## 4. Current State

### 4.1 Domain behavior

Phase-N-tail stanzas wired to design-explore in Stage 3 (TECH-430). Unknown whether real design-explore invocation has occurred post-wiring to generate `source: self-report` §Changelog entry.

### 4.2 Systems map

Primary files: `ia/skills/design-explore/SKILL.md` (§Changelog read), `ia/skills/skill-train/SKILL.md` (§Changelog write).
Related: `.claude/agents/skill-train.md`, `.claude/commands/skill-train.md`.

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A — tooling only.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phase 1 — Readiness check:
1. Read `design-explore/SKILL.md §Changelog` tail; scan for `source: self-report` entries post-wiring.
2. If found: proceed to T6.2 directly; append readiness note (found signal).
3. If not found: select existing exploration doc under `docs/`; invoke `/design-explore {path}` to produce at least one §Changelog entry (friction or clean).
4. Append readiness note to `skill-train/SKILL.md §Changelog`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-21 | Trigger short invocation if §Changelog empty | Need real signal to validate stanza works end-to-end | Skip and assume wired correctly — risky |

## 7. Implementation Plan

### Phase 1 — Readiness check

- [ ] Read `design-explore/SKILL.md §Changelog`; check for `source: self-report` post-TECH-430.
- [ ] If absent: invoke `/design-explore` on existing stub doc; verify §Changelog entry created.
- [ ] Append readiness note to `skill-train/SKILL.md §Changelog`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tooling-only | N/A | None — tooling only; see §8 Acceptance criteria | No C# / runtime claims |

## 8. Acceptance Criteria

- [ ] `design-explore/SKILL.md §Changelog` checked for `source: self-report` entry post-Stage-3.
- [ ] If none: short design-explore invocation triggered; §Changelog entry created.
- [ ] `skill-train/SKILL.md §Changelog` carries readiness note documenting outcome.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

_pending — populated by `/author ia/projects/skill-training-master-plan.md Stage 6`. 4 sub-sections: §Audit Notes / §Examples / §Test Blueprint / §Acceptance._

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
