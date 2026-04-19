---
purpose: "TECH-445 — Append 5 new sections to project-spec template (Project-New Plan, Audit, Code Review, Code Fix Plan, Closeout Plan)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T1.2.2"
---
# TECH-445 — Rewrite project-spec template

> **Issue:** [TECH-445](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Append 5 new sections to `ia/templates/project-spec-template.md` after `§Verification` so every new project spec carries Plan-Apply pair payload anchors out of the box: `§Project-New Plan`, `§Audit`, `§Code Review`, `§Code Fix Plan`, `§Closeout Plan`. Each section = heading + one-sentence placeholder (filled by pair-head Opus stages downstream).

## 2. Goals and Non-Goals

### 2.1 Goals

1. 5 new sections appended in fixed order after `§Verification`.
2. Each section = heading + one-sentence placeholder.
3. Existing sections (§1–§Open Questions) untouched.
4. `npm run validate:frontmatter` exit 0.

### 2.2 Non-Goals

1. Backfill new sections into existing open project specs — separate Step (T2.2.1).
2. Author actual Plan-Apply pair tuple shapes — TECH-448 (`plan-apply-pair-contract.md`).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Pair-head Opus | As a `/project-new` planner writing a new spec, I want `§Project-New Plan` already present so I drop tuples in w/o adding heading scaffolding. | 5 sections present in fixed order. |
| 2 | Pair-tail Sonnet | As a `closeout-apply` agent, I want `§Closeout Plan` heading guaranteed to exist so anchor lookup never misses. | `§Closeout Plan` heading present in template. |

## 4. Current State

### 4.1 Domain behavior

`ia/templates/project-spec-template.md` ends at `§Open Questions` (line 142). No Plan-Apply pair sections. Pair-head Opus stages would today need to author their own headings on first write.

### 4.2 Systems map

- `ia/templates/project-spec-template.md` — append target.
- `ia/rules/plan-apply-pair-contract.md` — TECH-448 defines tuple shape these sections will carry.
- `ia/templates/frontmatter-schema.md` — verify no schema constraint on section list.

## 5. Proposed Design

### 5.1 Target behavior

After `§Verification` (or `§Open Questions` per current ordering — implementer confirms placement vs §Open Questions block) the 5 new headings appear in this fixed order:

1. `## §Project-New Plan`
2. `## §Audit`
3. `## §Code Review`
4. `## §Code Fix Plan`
5. `## §Closeout Plan`

Each heading immediately followed by an HTML comment + one-line placeholder explaining what the pair-head Opus stage will fill in.

### 5.2 Architecture / implementation

Pure markdown append. Implementer chooses exact insertion point (end-of-file vs after §10 Lessons Learned vs after §Open Questions); preserve existing section ordering.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Sections = heading + placeholder only | Pair-head Opus owns tuple authoring; template stays minimal | Pre-populate empty tuple list (rejected — risks looking like authored content) |

## 7. Implementation Plan

### Phase 1 — Append 5 sections

- [ ] Read current template.
- [ ] Confirm placement (end-of-file recommended; §Open Questions stays last among existing sections).
- [ ] Append 5 headings + placeholders in fixed order.
- [ ] `npm run validate:frontmatter` green.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Template parses + 5 sections present | Node | `npm run validate:all` | Doc validators only |

## 8. Acceptance Criteria

- [ ] 5 new sections appended in fixed order.
- [ ] Each = heading + one-sentence placeholder.
- [ ] Existing sections untouched.
- [ ] `npm run validate:all` exit 0.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
