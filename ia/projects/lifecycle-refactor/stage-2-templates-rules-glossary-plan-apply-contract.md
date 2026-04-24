### Stage 2 — Foundation: Freeze, Templates & Rules / Templates + Rules + Glossary + Plan-Apply Contract

**Status:** Final

**Objectives:** Rewrite all foundational authoring surfaces so every downstream step authors against the new Stage/Task schema. Templates, rules, and glossary must be consistent before M2 begins touching master plans.

**Exit:**

- `ia/templates/master-plan-template.md`: Phase layer absent; Stage+Task retained; `§Stage File Plan` + `§Plan Fix` section stubs present; task-table `Phase` column removed.
- `ia/templates/project-spec-template.md`: 5 new sections present in order: `§Project-New Plan`, `§Audit`, `§Code Review`, `§Code Fix Plan`, `§Closeout Plan`.
- `ia/rules/project-hierarchy.md`: 2-row table (Stage · Task); cardinality gate = ≥2 tasks per Stage; Phase + Gate rows absent.
- `ia/rules/orchestrator-vs-spec.md`: R1–R7 matrix Phase-flip entries dropped; Stage flips retained.
- `ia/rules/plan-apply-pair-contract.md`: canonical `§Plan` section shape defined; 5 pair seam entries; apply/validation/escalation contract.
- `ia/specs/glossary.md`: 8 new terms added (**plan review**, **plan-fix apply**, **spec enrichment**, **Opus audit**, **Opus code review**, **code-fix apply**, **closeout apply**, **Plan-Apply pair**); **Stage** redefined as parent-of-Task; **Project hierarchy** redefined to 2-level; **Phase** + **Gate** tombstoned with redirect to **Stage**.
- `npm run validate:frontmatter` passes.
- Migration JSON M1 flipped to `done`.
- Phase 1 — Template rewrites.
- Phase 2 — Hierarchy rules rewrite.
- Phase 3 — Plan-Apply pair contract + glossary update.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | Rewrite master-plan template | **TECH-444** | Done (archived) | Rewrite `ia/templates/master-plan-template.md`: drop Phase bullet lists and Phase column from task-table (`\ | Task \ | Name \ | Phase \ | Issue \ | Status \ | Intent \ | ` → drop `Phase` column); keep Stage-level exit + Phase heading replaced by Stage-level heading; add `§Stage File Plan` stub (one-liner: "Opus planner writes materialization plan here") + `§Plan Fix` stub; preserve task-table `Issue` + `Status` + `Intent` columns. |
| T2.2 | Rewrite project-spec template | **TECH-445** | Done (archived) | Rewrite `ia/templates/project-spec-template.md`: append 5 new sections after `§Verification` in this order: `§Project-New Plan` (pair-head plan payload from `/project-new` Opus planner), `§Audit` (Opus audit paragraph post-implementation), `§Code Review` (Opus code-review verdict + notes), `§Code Fix Plan` (structured fix list from Opus reviewer; Sonnet applier reads verbatim), `§Closeout Plan` (structured migration anchors from Opus auditor; Sonnet closeout-applier reads verbatim). Each section = heading + one-sentence placeholder. |
| T2.3 | Rewrite project-hierarchy rule | **TECH-446** | Done (archived) | Rewrite `ia/rules/project-hierarchy.md` §table from 4-row (Step·Stage·Phase·Task) to 2-row (Stage·Task); restate cardinality gate: ≥2 tasks per Stage (hard), ≤6 tasks per Stage (soft); update lazy-materialization rule to Stage granularity (was Phase); update ephemeral-spec rule: Tasks still get individual `ia/projects/{ISSUE_ID}.md` specs. |
| T2.4 | Update orchestrator-vs-spec rule | **TECH-447** | Done (archived) | Edit `ia/rules/orchestrator-vs-spec.md` R1–R7 status flip matrix: drop any row referencing Phase-level flip (e.g. Phase completion → stage rollup); keep R2 (Stage In Progress flip via `stage-file`) + R5 (Final rollup via `project-stage-close`) + R6 (Final → In Progress via `master-plan-extend`) + R7 (Skeleton → Draft via `stage-decompose`); update all prose that says "Step/Stage/Phase" to "Stage/Task"; verify the orchestrator vs project-spec distinction prose still accurate. |
| T2.5 | Write plan-apply-pair-contract rule | **TECH-448** | Done (archived) | Write `ia/rules/plan-apply-pair-contract.md`: define canonical `§Plan` section shape — structured list of `{operation, target_path, target_anchor, payload}` tuples; Opus resolves anchors to exact line/heading/glossary-row-id; document 5 pair seams (plan-review→plan-fix-apply, stage-file-plan→stage-file-apply, project-new-plan→project-new-apply, code-review→code-fix-apply, audit→closeout-apply); define validation gate (Sonnet runs appropriate validator per pair; on failure returns control to Opus with error + failing tuple); define escalation rule (ambiguous anchor → immediate return to Opus; Sonnet never guesses); define idempotency requirement. |
| T2.6 | Update glossary + flip M1 done | **TECH-449** | Done (archived) | Edit `ia/specs/glossary.md`: add 8 new rows — **Plan-Apply pair** (pair pattern where Opus writes structured plan into `§Plan` section; Sonnet applies), **plan review** (Opus stage that reads all Tasks of a Stage together + master-plan header + invariants; outputs `§Plan Fix`), **plan-fix apply** (Sonnet pair-tail that reads `§Plan Fix` + applies edits), **spec enrichment** (Sonnet stage that pulls glossary anchors + tightens spec terminology; replaces kickoff), **Opus audit** (Opus stage post-verify that reads spec→impl→findings→verify output + writes `§Audit` + `§Closeout Plan`), **Opus code review** (Opus stage that reads diff vs spec + invariants + glossary; PASS / minor / `§Code Fix Plan`), **code-fix apply** (Sonnet pair-tail reads `§Code Fix Plan` + applies + re-enters `/verify-loop`), **closeout apply** (Sonnet pair-tail reads `§Closeout Plan` + archives BACKLOG row + deletes spec + validates); redefine **Stage** as parent-of-Task (was child of Step); redefine **Project hierarchy** to 2-level (Stage → Task); tombstone **Phase** (redirect: use Stage) + **Gate** (redirect: use Stage exit criteria); run `npm run validate:frontmatter`; flip M1 done in migration JSON. |

---
