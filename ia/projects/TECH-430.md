---
purpose: "TECH-430 — Wire authoring-trio Phase-N-tail stanza (Stage 2.1 T2.1.1)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/skill-training-master-plan.md"
task_key: "T2.1.1"
---
# TECH-430 — Wire authoring-trio Phase-N-tail stanza

> **Issue:** [TECH-430](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Append Phase-N-tail emitter stanza verbatim from `ia/skills/skill-train/SKILL.md §Emitter stanza template` to 3 authoring lifecycle skills: `design-explore`, `master-plan-new`, `master-plan-extend`. Satisfies Stage 2.1 Exit criterion (a) for the authoring-trio subset — each skill gains `skill_self_report` emission path feeding the `skill-train` consumer.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/design-explore/SKILL.md` carries Phase-N-tail stanza (verbatim template copy, `schema_version` stamped) + `## Changelog` section.
2. `ia/skills/master-plan-new/SKILL.md` — same.
3. `ia/skills/master-plan-extend/SKILL.md` — same.
4. Stanza lands at existing final handoff Phase position in each skill — not a new phase, not reorder.
5. Only `{SKILL_NAME}` + `{YYYY-MM-DD}` placeholders substituted; all other template text byte-for-byte identical.

### 2.2 Non-Goals

1. No edits to other 10 lifecycle skills (T2.1.2 / Stage 2.2 scope).
2. No edits to `ia/skills/skill-train/SKILL.md` — source of truth, read-only here.
3. No edits to `ia/skills/release-rollout-skill-bug-log/SKILL.md` — sibling producer.
4. No schema changes — template carries canonical schema; this task is wiring only.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a lifecycle-skill author, I want `design-explore` / `master-plan-new` / `master-plan-extend` runs to emit structured friction self-reports so `/skill-train` can aggregate recurring patterns. | Each of the 3 skills, when run with friction conditions firing, appends a `source: self-report` §Changelog entry matching the canonical schema. |

## 4. Current State

### 4.1 Domain behavior

The 3 authoring skills currently finish via their existing handoff phase without emitting any `skill_self_report`. §Changelog sections may or may not exist per skill. `skill-train` consumer (from Step 1) has no producer signal to aggregate on these surfaces.

### 4.2 Systems map

- Source of truth: `ia/skills/skill-train/SKILL.md §Emitter stanza template` (lines ~126–169 of current file).
- Target surfaces: `ia/skills/design-explore/SKILL.md`, `ia/skills/master-plan-new/SKILL.md`, `ia/skills/master-plan-extend/SKILL.md`.
- Schema reference: `ia/skills/skill-train/SKILL.md §Schema`.
- Sibling reference (do NOT modify): `ia/skills/release-rollout-skill-bug-log/SKILL.md`.
- Glossary anchors: `skill self-report`, `skill training`, `skill-train`, `Per-skill Changelog` (all MCP-queryable post Stage 1.1).

### 4.3 Implementation investigation notes

Template uses 3-step structure: (1) friction-condition check, (2) construct JSON, (3) append §Changelog entry. Clean-run rule = all arrays empty → no-op; §Changelog untouched. Final handoff phase in each skill is distinct — implementer locates correct phase per skill before pasting.

## 5. Proposed Design

### 5.1 Target behavior

Each of the 3 authoring skills, at the tail of its final handoff phase, runs the friction-condition check. If any of `guardrail_hits` / `phase_deviations` / `missing_inputs` is non-empty, emits a `source: self-report` §Changelog entry carrying the full `skill_self_report` JSON with today's `run_date` and the emitter template's `schema_version` date-stamp. Clean run → no §Changelog mutation.

### 5.2 Architecture / implementation

1. Read `ia/skills/skill-train/SKILL.md §Emitter stanza template` fully — copy block text exactly.
2. For each target SKILL.md: locate final handoff phase (usually `## Phase sequence` final `### Phase N — Handoff` or equivalent closing phase); append stanza at phase tail.
3. If `## Changelog` section absent: inject empty `## Changelog` section before terminal `---` or EOF (mirror `release-rollout-skill-bug-log` Phase 0 pattern).
4. Substitute `{SKILL_NAME}` and `{YYYY-MM-DD}` placeholders per target (today's date for `schema_version`).
5. No other substitutions — preserve fenced blocks, JSON field names, step headings verbatim.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Stanza placed at existing final handoff phase (not new phase) | Matches orchestrator T2.1.1 intent; avoids phase-count drift; keeps lifecycle flow unchanged | New `## Phase N+1 — Self-report` rejected — inflates phase count + breaks existing skill narrative |

## 7. Implementation Plan

### Phase 1 — Paste stanza into 3 authoring skills

- [ ] Copy canonical stanza block from `ia/skills/skill-train/SKILL.md §Emitter stanza template`.
- [ ] `ia/skills/design-explore/SKILL.md`: locate final handoff phase; paste stanza at tail; substitute placeholders; inject `## Changelog` if absent.
- [ ] `ia/skills/master-plan-new/SKILL.md`: same.
- [ ] `ia/skills/master-plan-extend/SKILL.md`: same.

### Phase 2 — Self-audit + validate

- [ ] Diff-check: stanza text matches template byte-for-byte (excluding placeholder substitutions).
- [ ] Confirm `schema_version` date present on all 3.
- [ ] Confirm `## Changelog` section present on all 3.
- [ ] `npm run validate:all` — exit 0.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Stanza wired in 3 authoring SKILL.md files | Node | `npm run validate:all` | Chains frontmatter + dead-project-spec + ia-indexes check |
| Verbatim match | Manual diff | `diff` against template block | Byte-for-byte; placeholders exempted |

## 8. Acceptance Criteria

- [ ] 3 SKILL.md files carry Phase-N-tail stanza matching canonical template verbatim.
- [ ] `schema_version` date-stamp present on each.
- [ ] `## Changelog` section present on each.
- [ ] Stanza at final handoff phase, not new phase.
- [ ] `npm run validate:all` exits 0.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

None — tooling only; see §8 Acceptance criteria.
