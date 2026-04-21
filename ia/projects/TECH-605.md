---
purpose: "TECH-605 — Execute /skill-train design-explore; capture proposal path + friction-count + severity; judge signal quality; record outcome in design-explore/SKILL.md §Changelog as source: dogfood-result."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/skill-training-master-plan.md
task_key: T6.2
---
# TECH-605 — Run skill-train on design-explore (Stage 6)

> **Issue:** [TECH-605](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-21
> **Last updated:** 2026-04-21

## 1. Summary

First real skill-train retrospective run targeting design-explore skill.
Produces `ia/skills/design-explore/train-proposal-{DATE}.md`; evaluates proposal signal quality.
Records dogfood outcome in `design-explore/SKILL.md §Changelog`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Run `/skill-train design-explore` (Opus subagent dispatch).
2. Capture: proposal file path, friction-count aggregated, severity field value.
3. Review proposal diff hunks against known friction in §Changelog entries.
4. Judge signal: strong (clear actionable diff) / weak (vague or trivial) / partial (mixed).
5. Append `source: dogfood-result` §Changelog entry to `design-explore/SKILL.md` with outcome classification.

### 2.2 Non-Goals (Out of Scope)

1. Iterating skill-train Phase 2/3 — that is T6.3.
2. Applying proposal patches without user review.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | First real dogfood run validates end-to-end pipeline | Proposal file written; signal classified; §Changelog updated |

## 4. Current State

### 4.1 Domain behavior

TECH-604 confirms design-explore has §Changelog self-report entries (or generates them). skill-train skill (TECH-392) ready to consume them and produce train-proposal.

### 4.2 Systems map

Primary files: `ia/skills/design-explore/SKILL.md` (§Changelog append), `ia/skills/design-explore/train-proposal-{DATE}.md` (created by skill-train).
Agent: `.claude/agents/skill-train.md` (Opus). Command: `.claude/commands/skill-train.md`.
Skill body: `ia/skills/skill-train/SKILL.md` Phase 0–5.

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A — tooling only.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phase 1 — Retrospective run:
1. Invoke `claude-personal "/skill-train design-explore"`.
2. Wait for proposal file write; note path + friction-count.
3. Read proposal; evaluate quality (actionable hunks vs vague summary).
4. Classify outcome; append §Changelog entry to `design-explore/SKILL.md`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-21 | Classify signal as strong/weak/partial | Explicit classification drives conditional T6.3 logic | Binary pass/fail — loses granularity |

## 7. Implementation Plan

### Phase 1 — Retrospective run

- [ ] Invoke `/skill-train design-explore` (Opus subagent).
- [ ] Capture proposal file path + friction-count + severity.
- [ ] Evaluate proposal quality; classify outcome (strong/weak/partial).
- [ ] Append `source: dogfood-result` §Changelog entry to `design-explore/SKILL.md`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tooling-only | N/A | None — tooling only; see §8 Acceptance criteria | No C# / runtime claims |

## 8. Acceptance Criteria

- [ ] `/skill-train design-explore` executed; proposal file written.
- [ ] Proposal read; signal quality classified (strong/weak/partial).
- [ ] `source: dogfood-result` §Changelog entry appended to `design-explore/SKILL.md` with classification.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: `skill-train` Phase 3 diff synthesis targets SKILL.md Phase sequence / Guardrails / Seed prompt sections only — if `design-explore/SKILL.md` has no Guardrails or Seed prompt section, proposal may be sparse. Mitigation: check `design-explore/SKILL.md` structure before judging signal weak; sparse proposal from minimal source is expected, not a Phase 2 aggregation failure.
- Ambiguity: classification rubric (`strong`/`weak`/`partial`) is subjective. Resolution: strong = ≥1 actionable unified-diff hunk targeting a specific Phase bullet or Guardrail line; weak = proposal writes only general summary prose with no line-level diff; partial = mixed (some hunks actionable, others vague).
- Risk: proposal file path is date-stamped `train-proposal-{DATE}.md` — if invoked late in the day in a different timezone from CI, date may mismatch. Resolution: use local machine date; note path explicitly in §Changelog entry.
- Risk: user §2.2 non-goal says "applying proposal patches without user review" — implementer must not auto-apply even if hunks look clean. User gate is a locked decision from Stage 1.

### §Examples

| Scenario | proposal friction-count | severity | Classification | §Changelog entry shape |
|----------|------------------------|----------|----------------|------------------------|
| Strong signal | ≥2 recurring `phase_deviations` → concrete Phase N bullet rewrite | `medium` or `high` | `strong` | `source: dogfood-result, outcome: strong, proposal: ia/skills/design-explore/train-proposal-{DATE}.md` |
| Weak signal | 0 recurring (single occurrences only, threshold not met) | `low` | `weak` | `source: dogfood-result, outcome: weak — threshold not met; T6.3 iteration required` |
| Partial signal | 1 actionable hunk + 1 vague prose summary | `medium` | `partial` | `source: dogfood-result, outcome: partial — 1 actionable hunk; T6.3 may refine Phase 3` |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| proposal_file_written | `/skill-train design-explore` invocation | `ia/skills/design-explore/train-proposal-{DATE}.md` exists; non-empty | manual |
| friction_count_captured | proposal file contents | `friction_types[]` field present; count ≥ 0 (0 valid for clean run) | manual |
| dogfood_result_changelog_entry | `design-explore/SKILL.md §Changelog` tail | Entry with `source: dogfood-result` + outcome classification appended | manual |
| validate_all_clean | repo state after §Changelog append | `npm run validate:all` exits 0 | node |

### §Acceptance

- [ ] `/skill-train design-explore` executed; `ia/skills/design-explore/train-proposal-{DATE}.md` written.
- [ ] Proposal read; `friction_types[]` count + `severity` field captured.
- [ ] Signal quality classified as `strong`, `weak`, or `partial` per rubric (actionable diff hunk = strong criterion).
- [ ] `source: dogfood-result` §Changelog entry appended to `design-explore/SKILL.md` with classification and proposal path.
- [ ] `npm run validate:all` exits 0 after §Changelog append.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
