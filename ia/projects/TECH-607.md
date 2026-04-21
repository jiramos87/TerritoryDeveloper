---
purpose: "TECH-607 — Run /skill-train skill-train (meta-dogfood); record proposal; apply user-approved patches; run validate:all; flip orchestrator to Status: Final."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/skill-training-master-plan.md
task_key: T6.4
---
# TECH-607 — Meta-dogfood + orchestrator final (Stage 6)

> **Issue:** [TECH-607](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-21
> **Last updated:** 2026-04-21

## 1. Summary

Meta-dogfood run: skill-train retrospects its own SKILL.md via `/skill-train skill-train`.
User reviews generated proposal; approved patches applied. Orchestrator flipped Final after validate:all passes.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Invoke `/skill-train skill-train`; capture proposal file path + friction-count.
2. User reviews proposal; approve or reject each hunk.
3. Apply approved patches to `skill-train/SKILL.md`.
4. Append `source: dogfood-result` entry to `skill-train/SKILL.md §Changelog`.
5. Run `npm run validate:all`; confirm exit 0.
6. Edit `skill-training-master-plan.md` Status line → `Status: Final`; flip all Stage 6 task rows `Done (archived)`.

### 2.2 Non-Goals (Out of Scope)

1. Auto-applying patches without user review.
2. Running validate:all before user review complete.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | skill-train self-improves via its own retrospective | Proposal generated; user-approved patches applied; orchestrator Final |

## 4. Current State

### 4.1 Domain behavior

T6.3 confirms strong signal from design-explore retrospective. skill-train/SKILL.md has accumulated §Changelog entries from Stages 2–3 wiring and dogfood runs. Ready for self-retrospective.

### 4.2 Systems map

Primary files: `ia/skills/skill-train/SKILL.md` (retrospect target + patch destination + §Changelog),
`ia/projects/skill-training-master-plan.md` (Status flip).
Agent: `.claude/agents/skill-train.md` (Opus). Command: `.claude/commands/skill-train.md`.
Validator: `npm run validate:all` (package.json).

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A — tooling only.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phase 1 — Meta-dogfood:
1. Invoke `/skill-train skill-train`.
2. Capture proposal; present to user for hunk-by-hunk review.
3. Apply approved hunks to `skill-train/SKILL.md`; discard rejected hunks.
4. Append `source: dogfood-result` §Changelog entry.

Phase 2 — Close + validate:
5. Run `npm run validate:all`; confirm exit 0; fix any failures inline.
6. Edit orchestrator `Status: Final`; flip T6.1–T6.4 rows to `Done (archived)`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-21 | User-gate on all patches before apply | Auto-apply never in v1 — user-gate mandatory per locked decision | Auto-apply approved patches — deferred to v2 |

## 7. Implementation Plan

### Phase 1 — Meta-dogfood

- [ ] Invoke `/skill-train skill-train`.
- [ ] Present proposal hunks to user; collect approve/reject per hunk.
- [ ] Apply approved hunks to `skill-train/SKILL.md`.
- [ ] Append `source: dogfood-result` §Changelog entry.

### Phase 2 — Close + validate

- [ ] Run `npm run validate:all`; confirm exit 0; fix any failures inline.
- [ ] Run `stage-closeout-plan` → `plan-applier` Mode `stage-closeout` for Stage 6; this flips T6.1–T6.4 task rows to `Done (archived)` and Stage 6 Status to `Final` per orchestration guardrails. Do NOT hand-edit task rows or orchestrator Status.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| validate:all exits 0 | Node | `npm run validate:all` | Chains validate:dead-project-specs + test:ia + validate:fixtures + generate:ia-indexes --check |

## 8. Acceptance Criteria

- [ ] `/skill-train skill-train` executed; proposal captured (or empty-run noted).
- [ ] User-approved patches applied to `skill-train/SKILL.md`.
- [ ] `source: dogfood-result` §Changelog entry appended to `skill-train/SKILL.md`.
- [ ] `npm run validate:all` exits 0.
- [ ] `skill-training-master-plan.md` Status: Final; Stage 6 task rows Done (archived).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: skill-train retrospecting itself may produce a low-friction proposal if `skill-train/SKILL.md §Changelog` has few entries (only Stage 2 authoring + Stage 3 wiring — no actual dogfood-loop entries yet). Mitigation: T6.4 runs after T6.1–T6.3 which append multiple §Changelog entries; meta-dogfood signal improves with each prior Stage 6 task completing.
- Risk: "apply user-approved patches" is the only task in Stage 6 that mutates a SKILL.md body outside of the normal `/implement` path — no `verify-loop` or `code-fix-apply` applies here. Mitigation: `validate:all` gate (§2.1 Goal 5) + locked non-auto-apply decision; implementer must gate each hunk on explicit user approval, not batch-approve.
- Ambiguity: "flip all Stage 6 task rows Done (archived)" — does this mean running `/closeout` pair or manual flip? Resolution: per master-plan orchestration guardrails, Stage closeout uses `stage-closeout-plan` → `plan-applier` Mode `stage-closeout` (not hand-edit). T6.4 spec says "edit orchestrator Status: Final; flip T6.1–T6.4 rows" — implementer must use the closeout pair, not direct edit.
- Risk: orchestrator `skill-training-master-plan.md` Status line flip from "In Progress" → "Final" touches a doc scanned by `validate:all`. Mitigation: run validate:all after edit; confirm exit 0.

### §Examples

| Scenario | `skill-train/SKILL.md §Changelog` state | Proposal generated? | User action | Outcome |
|----------|-----------------------------------------|--------------------|-----------|----|
| Entries from Stages 2–3 + T6.1–T6.3 | 5–8 entries; ≥2 recurring `phase_deviations` across stanza-wiring runs | Yes — ≥1 actionable hunk | Approve 2/3 hunks; reject 1 | Approved hunks applied; `source: dogfood-result` appended; validate:all green |
| Very few entries | Only 2 entries (Stage 2 authoring), no recurrence | No proposal (below threshold) | N/A | Empty-run noted in §Changelog; no edits; proceed to validate:all |
| Proposal with schema-breaking hunk | Hunk removes `schema_version` field from stanza | Hunk proposed | User rejects | Hunk discarded; `source: dogfood-result, rejected: schema-version-removal` noted |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| skill_train_self_invoked | `/skill-train skill-train` command | Skill runs Phase 0–5; proposal file written or empty-run noted | manual |
| user_gate_enforced | proposal hunks | No hunk applied without explicit per-hunk user approval | manual |
| dogfood_result_changelog_appended | `skill-train/SKILL.md §Changelog` tail after run | `source: dogfood-result` entry present | manual |
| validate_all_exits_0 | repo state after patches + §Changelog append | `npm run validate:all` exits 0 | node |
| orchestrator_status_final | `skill-training-master-plan.md` Status line | Status reads `Final`; Stage 6 task rows `Done (archived)` | manual |

### §Acceptance

- [ ] `/skill-train skill-train` executed; proposal captured or empty-run noted explicitly.
- [ ] Each proposal hunk presented to user; approved/rejected per hunk (no batch auto-apply).
- [ ] Approved patches applied to `skill-train/SKILL.md`; rejected hunks discarded.
- [ ] `source: dogfood-result` §Changelog entry appended to `skill-train/SKILL.md`.
- [ ] `npm run validate:all` exits 0 after all edits.
- [ ] Stage 6 closeout pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) run; task rows T6.1–T6.4 `Done (archived)`; orchestrator `Status: Final`.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
