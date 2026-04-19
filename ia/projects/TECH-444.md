---
purpose: "TECH-444 — Rewrite master-plan template; drop Phase layer; add §Stage File Plan + §Plan Fix stubs."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T1.2.1"
---
# TECH-444 — Rewrite master-plan template

> **Issue:** [TECH-444](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Rewrite `ia/templates/master-plan-template.md` so all new orchestrators author against Stage/Task 2-level schema. Drop Phase column from task-table header + Phase bullet lists; merge Phase exit content up into Stage Exit. Add `§Stage File Plan` + `§Plan Fix` section stubs (consumed downstream by Plan-Apply pair skills). Satisfies one row of Stage 1.2 Exit (TECH-449 flips M1 done after all 6 tasks Final).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Phase column absent from task-table header.
2. Phase bullet lists removed.
3. `§Stage File Plan` + `§Plan Fix` section stubs present (one-line placeholder each).
4. Task-table `Issue` + `Status` + `Intent` columns preserved.
5. `npm run validate:frontmatter` exit 0.

### 2.2 Non-Goals

1. Migrate existing master plans — that is Step 2 (TECH not yet filed).
2. Touch project-spec template — TECH-445.
3. Touch hierarchy rule prose — TECH-446.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Orchestrator author | As an Opus planner authoring a new master plan, I want the template to enforce 2-level Stage/Task so I cannot accidentally re-introduce Phase layer. | Phase rows absent from template; ≥2 tasks per Stage gate referenced. |

## 4. Current State

### 4.1 Domain behavior

`ia/templates/master-plan-template.md` currently carries 4-level hierarchy (Step → Stage → Phase → Task) per pre-refactor schema in `ia/rules/project-hierarchy.md`. Task table includes `Phase` column.

### 4.2 Systems map

- `ia/templates/master-plan-template.md` — rewrite target.
- `ia/rules/project-hierarchy.md` — TECH-446 rewrites in parallel; this template must align with rewritten rule.
- `ia/state/pre-refactor-snapshot/` — original template available for reference / rollback.

## 5. Proposed Design

### 5.1 Target behavior

Template emits Stage-only headings; each Stage has Exit + Tasks subsections; task table = `| Task | Name | Issue | Status | Intent |` (no Phase). Two new section stubs at Stage scope: `§Stage File Plan` + `§Plan Fix`.

### 5.2 Architecture / implementation

Pure markdown edit. Implementer reads current template, removes Phase tokens, adds two new section headings w/ placeholder one-liners.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Phase exit content merged up into Stage Exit | Stage 1.2 Exit specifies cardinality collapse | Drop Phase exit content entirely (rejected — info loss) |

## 7. Implementation Plan

### Phase 1 — Template rewrite

- [ ] Read current `ia/templates/master-plan-template.md`.
- [ ] Drop Phase column from task-table header.
- [ ] Drop Phase bullet list section.
- [ ] Add `§Stage File Plan` stub (one-line placeholder).
- [ ] Add `§Plan Fix` stub (one-line placeholder).
- [ ] `npm run validate:frontmatter` green.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Template parses + Phase absent | Node | `npm run validate:all` | Doc validators only |

## 8. Acceptance Criteria

- [ ] Phase column absent from task-table header.
- [ ] Phase bullet lists absent.
- [ ] `§Stage File Plan` + `§Plan Fix` stubs present.
- [ ] `Issue` + `Status` + `Intent` columns retained.
- [ ] `npm run validate:all` exit 0.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
