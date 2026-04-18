---
purpose: "TECH-431 — Wire filing-trio Phase-N-tail stanza (Stage 2.1 T2.1.2)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/skill-training-master-plan.md"
task_key: "T2.1.2"
---
# TECH-431 — Wire filing-trio Phase-N-tail stanza

> **Issue:** [TECH-431](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Append Phase-N-tail emitter stanza verbatim from `ia/skills/skill-train/SKILL.md §Emitter stanza template` to 3 filing lifecycle skills: `stage-decompose`, `stage-file`, `project-new`. Same procedure as TECH-430 (authoring trio). Completes Stage 2.1 Exit criterion (a) for all 6 core authoring+filing surfaces.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/stage-decompose/SKILL.md` carries stanza + `## Changelog` section.
2. `ia/skills/stage-file/SKILL.md` — same.
3. `ia/skills/project-new/SKILL.md` — same.
4. Stanza at existing final handoff phase per skill; only `{SKILL_NAME}` + `{YYYY-MM-DD}` placeholders substituted.

### 2.2 Non-Goals

1. No edits to 4 spec-lifecycle skills or 3 rollout-family skills (Stage 2.2 scope).
2. No edits to `skill-train/SKILL.md`.
3. No skill behavior refactor — this is pure append-and-inject-if-absent.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a filing-skill author, I want `stage-decompose` / `stage-file` / `project-new` runs to emit `skill_self_report` on friction so `/skill-train` aggregates signal across the filing surface. | Each of the 3 skills appends `source: self-report` §Changelog entry when friction fires. |

## 4. Current State

### 4.1 Domain behavior

Filing skills finish via their existing handoff phase without structured friction reporting. Phase drift / missing-input patterns currently invisible to `skill-train` consumer.

### 4.2 Systems map

- Source of truth: `ia/skills/skill-train/SKILL.md §Emitter stanza template`.
- Target surfaces: `ia/skills/stage-decompose/SKILL.md`, `ia/skills/stage-file/SKILL.md`, `ia/skills/project-new/SKILL.md`.
- Precedent: TECH-430 authoring-trio wiring (same template, same procedure).
- Glossary anchors: `skill self-report`, `Per-skill Changelog`.

### 4.3 Implementation investigation notes

Filing skills have distinct handoff phase shapes (e.g. `stage-file` ends with atomic table update + progress regen; `project-new` ends with SHIP block). Stanza lands at phase-tail regardless of phase body shape; friction-condition check is phase-body-agnostic.

## 5. Proposed Design

### 5.1 Target behavior

Same as TECH-430: friction-condition check at phase tail; JSON construction; §Changelog append on non-empty friction. Clean run = no-op.

### 5.2 Architecture / implementation

Procedure identical to TECH-430 §5.2, applied to the filing trio: copy canonical block, locate final handoff phase per skill, paste at tail, substitute placeholders, inject `## Changelog` if absent.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Filing trio treated as second wave after authoring trio | Matches orchestrator Phase 1 grouping; same procedure, separate task to keep blast radius small | Single combined 6-skill task rejected — larger diff surface + harder review |

## 7. Implementation Plan

### Phase 1 — Paste stanza into 3 filing skills

- [ ] `ia/skills/stage-decompose/SKILL.md`: final handoff phase tail; stanza paste; placeholders; inject §Changelog if absent.
- [ ] `ia/skills/stage-file/SKILL.md`: same.
- [ ] `ia/skills/project-new/SKILL.md`: same.

### Phase 2 — Self-audit + validate

- [ ] Byte-for-byte match vs template.
- [ ] `schema_version` stamps present on all 3.
- [ ] `## Changelog` section present on all 3.
- [ ] `npm run validate:all` — exit 0.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Stanza wired in 3 filing SKILL.md files | Node | `npm run validate:all` | Chains validation suite |
| Verbatim match | Manual diff | diff vs template | Placeholders exempted |

## 8. Acceptance Criteria

- [ ] 3 SKILL.md files carry stanza verbatim.
- [ ] `schema_version` date-stamp on each.
- [ ] `## Changelog` section on each.
- [ ] Stanza at final handoff phase.
- [ ] `npm run validate:all` exits 0.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

None — tooling only; see §8 Acceptance criteria.
