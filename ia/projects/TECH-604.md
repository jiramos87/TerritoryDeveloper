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

### §Audit Notes

- Risk: `design-explore/SKILL.md §Changelog` may be empty post-Stage-3 wiring (TECH-430) if no real invocation occurred — stanza fires only on friction; clean runs leave §Changelog untouched. Mitigation: spec explicitly allows triggering a short invocation on any existing stub doc under `docs/` to force signal accumulation; implementer must not skip this step.
- Ambiguity: "existing stub doc" is underspecified — `docs/skill-training-exploration.md` is the natural candidate (already used as ground truth for this plan). Resolution: use `docs/skill-training-exploration.md` unless already fully explored; fall back to any other `docs/*.md` exploration doc.
- Risk: appending readiness note to `skill-train/SKILL.md §Changelog` without a `schema_version` field would produce a malformed entry inconsistent with stanza schema. Mitigation: readiness notes use `source: dogfood-readiness` (plain prose entry, not a `skill_self_report` JSON block) — implementer must not inject a bare JSON block here.
- Invariant touch: TECH-430 cross-ref in §4.1 is filed in `ia/backlog-archive/` (archived). Verified resolvable — no rewrite needed.

### §Examples

| Scenario | `design-explore/SKILL.md §Changelog` state | Action taken | Readiness note content |
|----------|--------------------------------------------|--------------|------------------------|
| Signal present | ≥1 `source: self-report` entry dated after TECH-430 merge | No invocation triggered | `dogfood-readiness: signal found — N entries post-wiring; proceeding to T6.2` |
| Signal absent | No `source: self-report` entries post-wiring | Invoke `/design-explore docs/skill-training-exploration.md` | `dogfood-readiness: no prior signal; triggered design-explore on docs/skill-training-exploration.md; entry created {DATE}` |
| Invocation clean run | Stanza fires but friction_types[] empty | Entry still written (clean-run record) | `dogfood-readiness: invocation produced clean-run entry; proceeding to T6.2` |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| changelog_self_report_present | `design-explore/SKILL.md §Changelog` contents; TECH-430 merge date | ≥1 `source: self-report` entry present with date ≥ TECH-430 date, OR invocation triggered that produces one | manual |
| readiness_note_written | `skill-train/SKILL.md §Changelog` tail | Entry with `source: dogfood-readiness` (or equivalent) appended; no malformed JSON block | manual |
| validate_all_clean | repo state after readiness note write | `npm run validate:all` exits 0 | node |

### §Acceptance

- [ ] `design-explore/SKILL.md §Changelog` scanned for `source: self-report` entries dated after TECH-430 wiring.
- [ ] If absent: `/design-explore` invoked on `docs/skill-training-exploration.md` (or equivalent); §Changelog entry confirmed created.
- [ ] Readiness note appended to `skill-train/SKILL.md §Changelog` documenting outcome (signal-found or invocation-triggered).
- [ ] `npm run validate:all` exits 0 after readiness note write.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
