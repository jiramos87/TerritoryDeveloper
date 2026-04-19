---
purpose: "TECH-433 — validate:all post Stage 2.1 (Stage 2.1 T2.1.4)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/skill-training-master-plan.md"
task_key: "T2.1.4"
---
# TECH-433 — validate:all post Stage 2.1

> **Issue:** [TECH-433](../../BACKLOG.md)
> **Status:** In Progress
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Final green-bar check for Stage 2.1. Run `npm run validate:all` from repo root after TECH-430 / TECH-431 / TECH-432 land. Fix any frontmatter / index / dead-project-spec regression inline before closing stage.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `npm run validate:all` exits 0 after Stage 2.1 wiring + audit complete.
2. Any regression introduced by 6 skill edits (frontmatter drift, stale ia-index, broken cross-ref) fixed inline.
3. Stage 2.1 ready for `project-stage-close` skill.

### 2.2 Non-Goals

1. No `unity:compile-check` — docs-only changes; no C# touched.
2. No Play Mode or bridge preflight — tooling stage.
3. No new skill edits beyond regression fixes surfaced by validator.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As Stage 2.1 closer, I want a green `validate:all` bar before running `project-stage-close` so the stage closes on verified ground. | Validator exits 0; any surfaced regression fixed. |

## 4. Current State

### 4.1 Domain behavior

After TECH-430 / TECH-431 / TECH-432, 6 SKILL.md files carry stanza + §Changelog. Validator chain may flag: frontmatter shape issues (new §Changelog section), ia-index regeneration needed, cross-ref link resolution.

### 4.2 Systems map

- Validator: `npm run validate:all` — chains `validate:dead-project-specs`, `test:ia`, `validate:fixtures`, `generate:ia-indexes --check`, `validate:frontmatter`.
- Audit targets: 6 skills wired + this Stage's 4 project specs (TECH-430, TECH-431, TECH-432, TECH-433 — see `BACKLOG.md`).
- Fix surfaces: `ia/skills/*/SKILL.md` frontmatter, `ia/indexes/*.json`, cross-ref paths.
- Sibling specs: [`TECH-430.md`](TECH-430.md), [`TECH-431.md`](TECH-431.md), [`TECH-432.md`](TECH-432.md). Parent plan: [`skill-training-master-plan.md`](skill-training-master-plan.md).

### 4.3 Implementation investigation notes

Typical post-wiring regressions: ia-index out of sync (run `npm run generate:ia-indexes` if `--check` fails; commit regenerated index). Frontmatter drift unlikely since wiring only appends content body. Dead-project-spec failures possible if any of the 4 new Stage 2.1 project spec stubs (TECH-430 through TECH-433) carry broken cross-refs.

## 5. Proposed Design

### 5.1 Target behavior

Single validator run → exit 0 → stage green-bar → ready for `project-stage-close`.

### 5.2 Architecture / implementation

1. Run `npm run validate:all` from repo root.
2. Exit 0 → acceptance met; close task.
3. Non-zero → read stdout; attribute failure (which chain step, which file); fix inline; re-run until exit 0. Do NOT `--skip` any chain step.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Final validate is its own task not folded into TECH-432 | Clear stage-close gate; easier to attribute regression ownership | Fold into TECH-432 rejected — mixes audit + validation |

## 7. Implementation Plan

### Phase 1 — Run validator

- [ ] `cd` to repo root; `npm run validate:all`.
- [ ] Capture exit code + failing step on non-zero.

### Phase 2 — Fix regression if any

- [ ] Identify failing chain step from stdout (never guess).
- [ ] Patch inline per failure type:
  - `generate:ia-indexes --check` red → `npm run generate:ia-indexes` then commit regenerated `ia/indexes/*.json`.
  - `validate:frontmatter` red → fix SKILL.md front-block shape (key order, required fields).
  - `validate:dead-project-specs` red → repair broken cross-ref or stale id reference.
  - `test:ia` / `validate:fixtures` red → patch fixture + rerun.
- [ ] Re-run `npm run validate:all` until exit 0. Do NOT `--skip` any chain step.

### Phase 3 — Stage-close readiness

- [ ] Confirm TECH-430 / TECH-431 / TECH-432 rows marked Done in `ia/projects/skill-training-master-plan.md` Stage 2.1 table.
- [ ] Hand off to `project-stage-close` skill (ticks Stage 2.1 row; does NOT close umbrella).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Stage 2.1 green bar | Node | `npm run validate:all` | Exit 0 required from repo root |
| No chain step skipped | Process | stdout inspection | All 5 chain steps must report pass |
| Stage-close ready | Doc | `skill-training-master-plan.md` Stage 2.1 | T2.1.1 / T2.1.2 / T2.1.3 rows = Done before running `project-stage-close` |

## 8. Acceptance Criteria

- [ ] `npm run validate:all` exits 0 from repo root.
- [ ] Any regression surfaced by Stage 2.1 wiring fixed inline (no `--skip`).
- [ ] Failing step identified from stdout (never guessed from context).
- [ ] Stage 2.1 sibling tasks (TECH-430 / TECH-431 / TECH-432) Done in master plan.
- [ ] Stage 2.1 ready for `project-stage-close`.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

None — tooling only; see §8 Acceptance criteria.
