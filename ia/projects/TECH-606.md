---
purpose: "TECH-606 — Iterate skill-train Phase 2/3 if first retrospective signal weak; re-run /skill-train design-explore; append source: iteration §Changelog entries."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/skill-training-master-plan.md
task_key: T6.3
---
# TECH-606 — Iterate skill-train if weak signal (Stage 6)

> **Issue:** [TECH-606](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-21
> **Last updated:** 2026-04-21

## 1. Summary

Iterative refinement pass on skill-train Phase 2/3 if first retrospective signal weak.
Edits `skill-train/SKILL.md` aggregation or diff logic; re-runs `/skill-train design-explore`.
Appends `source: iteration` §Changelog entries for each edit cycle.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Evaluate T6.2 classification (received from T6.2 §Changelog entry or review notes).
2. If strong: append `source: dogfood-result` to `skill-train/SKILL.md §Changelog`; proceed to T6.4.
3. If weak/partial: diagnose gap category (threshold, Phase 2 grouping, Phase 3 diff granularity).
4. Edit `skill-train/SKILL.md` (Phase 2 or Phase 3 body) to address gap.
5. Re-run `/skill-train design-explore`; re-evaluate; repeat until strong or max 2 iterations.
6. Append `source: iteration` §Changelog entry per cycle noting change + outcome.

### 2.2 Non-Goals (Out of Scope)

1. Auto-applying proposal patches — user gate mandatory.
2. More than 2 iteration cycles without user review.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Signal quality improved if first run weak | Strong classification achieved or final state documented |

## 4. Current State

### 4.1 Domain behavior

T6.2 produces signal classification. If weak or partial, Phase 2 (aggregation) or Phase 3 (diff synthesis) of skill-train/SKILL.md may need tuning.

### 4.2 Systems map

Primary files: `ia/skills/skill-train/SKILL.md` (Phase 2/3 edits, §Changelog append),
`ia/skills/design-explore/SKILL.md` (§Changelog read for signal context),
`ia/skills/design-explore/train-proposal-{DATE}.md` (re-generated per iteration).
Agent: `.claude/agents/skill-train.md` (Opus). Command: `.claude/commands/skill-train.md`.

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A — tooling only.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phase 1 — Signal evaluation + conditional iteration:
1. Read T6.2 outcome from `design-explore/SKILL.md §Changelog` (last `source: dogfood-result` entry).
2. If strong: append confirmation to `skill-train/SKILL.md §Changelog`; skip to Phase 2.
3. If weak/partial: read `skill-train/SKILL.md` Phase 2 (aggregation) + Phase 3 (diff synthesis).
4. Edit identified gap; re-run `/skill-train design-explore`; re-evaluate.
5. Append `source: iteration` entry after each cycle.

Phase 2 — Close iteration loop:
6. Confirm signal upgraded to strong or document final state if max iterations reached.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-21 | Max 2 iteration cycles before human review | Bound unbounded iteration; preserve token economy | No max cap — could loop indefinitely |

## 7. Implementation Plan

### Phase 1 — Signal evaluation + conditional iteration

- [ ] Read T6.2 outcome from `design-explore/SKILL.md §Changelog`.
- [ ] If strong: append `source: dogfood-result` confirmation to `skill-train/SKILL.md §Changelog`.
- [ ] If weak/partial: diagnose gap; edit `skill-train/SKILL.md` Phase 2 or 3; re-run `/skill-train design-explore`.
- [ ] Append `source: iteration` §Changelog entry per cycle.

### Phase 2 — Close iteration loop

- [ ] Confirm strong signal or document final state after max 2 iterations.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tooling-only | N/A | None — tooling only; see §8 Acceptance criteria | No C# / runtime claims |

## 8. Acceptance Criteria

- [ ] T6.2 outcome read from `design-explore/SKILL.md §Changelog`.
- [ ] If strong: `source: dogfood-result` confirmation appended to `skill-train/SKILL.md §Changelog`.
- [ ] If weak/partial: `skill-train/SKILL.md` Phase 2 or 3 edited; `/skill-train design-explore` re-run; signal upgraded; `source: iteration` entries appended per cycle.

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
