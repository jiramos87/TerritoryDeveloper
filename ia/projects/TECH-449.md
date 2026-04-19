---
purpose: "TECH-449 — Add 8 new glossary terms; redefine Stage + Project hierarchy; tombstone Phase + Gate; flip migration JSON M1 done."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T1.2.6"
---
# TECH-449 — Glossary update + M1 flip

> **Issue:** [TECH-449](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Edit `ia/specs/glossary.md`: add 8 new rows for Plan-Apply pair vocabulary; redefine **Stage** as parent-of-Task and **Project hierarchy** as 2-level; tombstone **Phase** + **Gate** w/ redirect to **Stage**. After all edits validate frontmatter + flip `ia/state/lifecycle-refactor-migration.json` M1 → `done`. This is the final task of Stage 1.2; closing it satisfies Stage 1.2 Exit + advances Step 1 toward Final.

## 2. Goals and Non-Goals

### 2.1 Goals

1. 8 new glossary rows added: **Plan-Apply pair**, **plan review**, **plan-fix apply**, **spec enrichment**, **Opus audit**, **Opus code review**, **code-fix apply**, **closeout apply**.
2. **Stage** row redefined as parent-of-Task (was child of Step).
3. **Project hierarchy** row redefined to 2-level (Stage → Task).
4. **Phase** row tombstoned w/ redirect: "Retired — use **Stage**".
5. **Gate** row tombstoned w/ redirect: "Retired — use Stage exit criteria".
6. `ia/state/lifecycle-refactor-migration.json` M1 flipped to `done`.
7. `npm run validate:frontmatter` exit 0.

### 2.2 Non-Goals

1. Cascade glossary rewrites into reference specs — out of Stage 1.2.
2. Migrate existing master plans / specs — Step 2.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Lifecycle skill author | As a Step 3 skill author, I want canonical glossary terms for all pair stages already in glossary so I anchor citations correctly. | 8 new rows present + linkable. |
| 2 | Migration runner | As Step 2 / 3 / 4 runner, I want M1 flipped done so resumability state mirrors completion. | M1 = `done`. |

## 4. Current State

### 4.1 Domain behavior

`ia/specs/glossary.md` lacks rows for Plan-Apply pair vocabulary. **Stage** + **Project hierarchy** entries describe pre-refactor 4-level schema. **Phase** + **Gate** still active terms.

`ia/state/lifecycle-refactor-migration.json` M1 = `pending` (set by TECH-442).

### 4.2 Systems map

- `ia/specs/glossary.md` — edit target (rows + tombstones).
- `ia/state/lifecycle-refactor-migration.json` — flip target (M1 → done).
- `ia/rules/plan-apply-pair-contract.md` — TECH-448; 8 new glossary terms anchor in this rule.
- `ia/rules/project-hierarchy.md` — TECH-446; **Stage** + **Project hierarchy** redefinitions match this rule rewrite.
- `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` §Design Expansion — source of new term definitions.

## 5. Proposed Design

### 5.1 Target behavior

Glossary reader sees 8 new rows (alphabetized into existing table per glossary convention) w/ definitions sourced from exploration doc. **Phase** + **Gate** rows show "Retired — use **Stage**" definition, preserving discoverability for legacy doc readers. **Stage** + **Project hierarchy** rows show 2-level definitions matching TECH-446.

Migration JSON M1 entry: `"M1": "done"`.

### 5.2 Architecture / implementation

Markdown table edit + JSON edit. Implementer reads exploration doc §Design Expansion for the 8 term definitions, transcribes per glossary table convention (caveman; spec ref; cited_in optional).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Tombstone Phase + Gate vs delete | Discoverability for legacy readers + downstream migration scripts can detect retired terms | Hard delete (rejected — breaks legacy doc grep) |
| 2026-04-19 | Flip M1 done at tail of this task | Last task of Stage 1.2; matches Stage Exit "Migration JSON M1 flipped to done" | Flip on Stage close (rejected — Stage close is automation-driven; explicit flip is safer) |

## 7. Implementation Plan

### Phase 1 — Glossary edits

- [ ] Read current `ia/specs/glossary.md`.
- [ ] Add 8 new rows w/ definitions sourced from exploration doc.
- [ ] Redefine **Stage** + **Project hierarchy** rows.
- [ ] Tombstone **Phase** + **Gate** rows.
- [ ] `npm run validate:frontmatter` green.

### Phase 2 — Migration JSON flip + validate

- [ ] Edit `ia/state/lifecycle-refactor-migration.json`: M1 → `done`.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Glossary parses + 8 new rows | Node | `npm run validate:all` | Doc validators only |
| Migration JSON parses + M1 done | Node | `node -e "console.log(require('./ia/state/lifecycle-refactor-migration.json').M1)"` | Smoke check |

## 8. Acceptance Criteria

- [ ] 8 new glossary rows added.
- [ ] **Stage** + **Project hierarchy** rows redefined.
- [ ] **Phase** + **Gate** rows tombstoned.
- [ ] Migration JSON M1 = `done`.
- [ ] `npm run validate:all` exit 0.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
