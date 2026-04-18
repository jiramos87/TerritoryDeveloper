# Lifecycle Refactor — Opus Planner / Sonnet Executor + Hierarchy Collapse — Master Plan (Umbrella)

> **Status:** Draft — Step 1 / Stage 1.1 pending (no BACKLOG rows filed yet)
>
> **Scope:** Big-bang collapse of Step/Stage/Phase/Task hierarchy to Stage/Task. Introduce Plan-Apply pair pattern (5 seams) with Opus pair-heads and Sonnet pair-tails. Sonnet-ify spec enrichment. Add Opus audit + code-review inline stages. Migrate all 16 open master plans + open project specs + backlog yaml in place. Tooling surface only — zero Unity runtime C# touch.
>
> **Exploration source:** `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` (§Design Expansion: Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Review Notes).
>
> **Locked decisions (do not reopen in this plan):**
> - Q1 = Approach B — full hierarchy collapse, big-bang sequential.
> - Q2 = migrate all in place; no dual-schema window.
> - Q3 = parent layer renamed to **Stage** (minimizes rename surface: `project-stage-close` / `/ship-stage` / web `/dashboard` already say "Stage"). Old Step+Stage pair collapses → new Stage; old Phase rows merge up into parent Stage.
> - Q4 = per-task project specs (`ia/projects/{ISSUE_ID}.md`) preserved across migration; only frontmatter phase fields dropped.
> - Q5 = one sequential big-bang pass; multi-session resumable via `ia/state/lifecycle-refactor-migration.json` (keyed per phase + per file).
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task — current schema; rewritten in Stage 1.2 to Stage > Task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Note — self-migration:** This orchestrator itself will be subject to Stage 2.1 transform. The `migrate-master-plans.ts` script must run this file last in the batch, after all other plans are validated. Self-migration is idempotent: the script reads snapshot, emits to current path.
>
> **Read first if landing cold:**
> - `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` — full design + architecture + review notes. Design Expansion block is ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase — applies until Stage 1.2 rewrites the rule to ≥2 tasks per stage).
> - `ia/rules/terminology-consistency.md` — canonical vocabulary: **Plan-Apply pair**, **Orchestrator document**, **Project spec**, **Backlog record**.
> - Related orchestrator: `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` — composite MCP bundle proposal; Stage 3.1 builds on its `lifecycle_stage_context` pattern.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | Skeleton | Planned | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`; `Skeleton` + `Planned` authored by `master-plan-new` / `stage-decompose`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `stage-file` also flips Stage header `Draft/Planned → In Progress` (R2) and plan top Status `Draft → In Progress — Step {N} / Stage {N.M}` on first task ever filed (R1); `stage-decompose` → Step header `Skeleton → Draft (tasks _pending_)` (R7); `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level step rollup; `project-stage-close` / `project-spec-close` → plan top Status `→ Final` when all Steps read `Final` (R5); `master-plan-extend` → plan top Status `Final → In Progress — Step {N_new} / Stage {N_new}.1` when new Steps appended to a Final plan (R6).

---

### Step 1 — Foundation: Freeze, Templates & Rules

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 1):** 0 filed

**Objectives:** Establish migration branch and snapshot pre-refactor state so rollback is safe. Rewrite all foundational authoring surfaces — templates (master-plan + project-spec), hierarchy rules, orchestrator rules, Plan-Apply pair contract, glossary new terms + tombstones. Downstream steps depend on these rewritten surfaces; Step 2 must not start until Stage 1.2 is Final.

**Exit criteria:**

- `feature/lifecycle-collapse-cognitive-split` branch exists with initial commit.
- `ia/state/lifecycle-refactor-migration.json` written: phases M0–M8 keyed; M0 `done`, M1 `done`, M2–M8 `pending`; per-file arrays seeded.
- `ia/state/pre-refactor-snapshot/` tarball or directory present; file count matches `ia/projects/*master-plan*.md` + `ia/backlog/*.yaml` + open `ia/projects/{ISSUE_ID}.md` counts at snapshot time.
- `ia/templates/master-plan-template.md` — Phase layer removed; Stage + Task retained; `§Stage File Plan` + `§Plan Fix` section stubs present; `Phase` column absent from task-table header.
- `ia/templates/project-spec-template.md` — 5 new sections present: `§Project-New Plan`, `§Audit`, `§Code Review`, `§Code Fix Plan`, `§Closeout Plan`.
- `ia/rules/project-hierarchy.md` — 2-level table only (Stage · Task); cardinality gate states ≥2 tasks per Stage; Phase + Gate rows removed.
- `ia/rules/orchestrator-vs-spec.md` — R1–R7 matrix updated: Phase flip entries dropped; Stage + Step-as-Stage flips retained.
- `ia/rules/plan-apply-pair-contract.md` — new file present; canonical `§Plan` section shape + apply/validation/escalation contract for all 5 pair seams.
- `ia/specs/glossary.md` — 8 new terms added; **Stage** + **Project hierarchy** redefined; **Phase** + **Gate** tombstoned.
- `npm run validate:frontmatter` passes on all modified templates/rules.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` §Design Expansion → Chosen Approach, Subsystem Impact, Implementation Points M0–M1.
- `ia/templates/master-plan-template.md` (exists — rewrite target).
- `ia/templates/project-spec-template.md` (exists — rewrite target).
- `ia/rules/project-hierarchy.md` (exists — rewrite target).
- `ia/rules/orchestrator-vs-spec.md` (exists — rewrite target).
- `ia/rules/plan-apply-pair-contract.md` (new).
- `ia/specs/glossary.md` (exists — update target; check current Phase/Gate/Stage definitions before editing).
- `ia/state/lifecycle-refactor-migration.json` (new).
- `ia/state/pre-refactor-snapshot/` (new).

---

#### Stage 1.1 — Branch + Snapshot + Migration State

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Create migration branch. Snapshot current master plans + open specs + backlog yaml so M2/M3 can always re-read from clean state. Write migration JSON with resumability keys.

**Exit:**

- Branch `feature/lifecycle-collapse-cognitive-split` checked out.
- `ia/state/pre-refactor-snapshot/` contains tarball (or flat copy) of all `ia/projects/*master-plan*.md`, `ia/backlog/*.yaml`, `ia/backlog-archive/*.yaml`, and open `ia/projects/{ISSUE_ID}.md` files at snapshot time.
- `ia/state/lifecycle-refactor-migration.json` written with M0–M8 phase entries (`pending` / `done`) + per-file progress arrays for M2 and M3.
- Migration JSON M0 flipped to `done`.

**Phases:**

- [ ] Phase 1 — Branch creation + freeze note + initial migration JSON.
- [ ] Phase 2 — Snapshot pre-refactor state + validate integrity.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.1.1 | Branch + freeze setup | 1 | _pending_ | _pending_ | Create `feature/lifecycle-collapse-cognitive-split` via `git checkout -b`; add freeze note to `CLAUDE.md` §Key commands warning against running `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, `/stage-file` until M8 sign-off; write initial `ia/state/lifecycle-refactor-migration.json` (M0 done, M1–M8 pending; per-file arrays for M2: list of all `*master-plan*.md` paths, each `pending`; per-file array for M3: list of all `ia/backlog/*.yaml` + open `ia/projects/{ISSUE_ID}.md` paths). |
| T1.1.2 | Pre-refactor snapshot | 2 | _pending_ | _pending_ | Copy all `ia/projects/*master-plan*.md`, `ia/backlog/*.yaml`, `ia/backlog-archive/*.yaml`, and open `ia/projects/{ISSUE_ID}.md` into `ia/state/pre-refactor-snapshot/` (preserve relative paths); write `ia/state/pre-refactor-snapshot/manifest.json` with file list + counts + git SHA; update migration JSON referencing snapshot path; flip M0 `done` in JSON. |

---

#### Stage 1.2 — Templates + Rules + Glossary + Plan-Apply Contract

**Status:** Draft (tasks _pending_ — not yet filed)

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

**Phases:**

- [ ] Phase 1 — Template rewrites.
- [ ] Phase 2 — Hierarchy rules rewrite.
- [ ] Phase 3 — Plan-Apply pair contract + glossary update.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.2.1 | Rewrite master-plan template | 1 | _pending_ | _pending_ | Rewrite `ia/templates/master-plan-template.md`: drop Phase bullet lists and Phase column from task-table (`\| Task \| Name \| Phase \| Issue \| Status \| Intent \|` → drop `Phase` column); keep Stage-level exit + Phase heading replaced by Stage-level heading; add `§Stage File Plan` stub (one-liner: "Opus planner writes materialization plan here") + `§Plan Fix` stub; preserve task-table `Issue` + `Status` + `Intent` columns. |
| T1.2.2 | Rewrite project-spec template | 1 | _pending_ | _pending_ | Rewrite `ia/templates/project-spec-template.md`: append 5 new sections after `§Verification` in this order: `§Project-New Plan` (pair-head plan payload from `/project-new` Opus planner), `§Audit` (Opus audit paragraph post-implementation), `§Code Review` (Opus code-review verdict + notes), `§Code Fix Plan` (structured fix list from Opus reviewer; Sonnet applier reads verbatim), `§Closeout Plan` (structured migration anchors from Opus auditor; Sonnet closeout-applier reads verbatim). Each section = heading + one-sentence placeholder. |
| T1.2.3 | Rewrite project-hierarchy rule | 2 | _pending_ | _pending_ | Rewrite `ia/rules/project-hierarchy.md` §table from 4-row (Step·Stage·Phase·Task) to 2-row (Stage·Task); restate cardinality gate: ≥2 tasks per Stage (hard), ≤6 tasks per Stage (soft); update lazy-materialization rule to Stage granularity (was Phase); update ephemeral-spec rule: Tasks still get individual `ia/projects/{ISSUE_ID}.md` specs. |
| T1.2.4 | Update orchestrator-vs-spec rule | 2 | _pending_ | _pending_ | Edit `ia/rules/orchestrator-vs-spec.md` R1–R7 status flip matrix: drop any row referencing Phase-level flip (e.g. Phase completion → stage rollup); keep R2 (Stage In Progress flip via `stage-file`) + R5 (Final rollup via `project-stage-close`) + R6 (Final → In Progress via `master-plan-extend`) + R7 (Skeleton → Draft via `stage-decompose`); update all prose that says "Step/Stage/Phase" to "Stage/Task"; verify the orchestrator vs project-spec distinction prose still accurate. |
| T1.2.5 | Write plan-apply-pair-contract rule | 3 | _pending_ | _pending_ | Write `ia/rules/plan-apply-pair-contract.md`: define canonical `§Plan` section shape — structured list of `{operation, target_path, target_anchor, payload}` tuples; Opus resolves anchors to exact line/heading/glossary-row-id; document 5 pair seams (plan-review→plan-fix-apply, stage-file-plan→stage-file-apply, project-new-plan→project-new-apply, code-review→code-fix-apply, audit→closeout-apply); define validation gate (Sonnet runs appropriate validator per pair; on failure returns control to Opus with error + failing tuple); define escalation rule (ambiguous anchor → immediate return to Opus; Sonnet never guesses); define idempotency requirement. |
| T1.2.6 | Update glossary + flip M1 done | 3 | _pending_ | _pending_ | Edit `ia/specs/glossary.md`: add 8 new rows — **Plan-Apply pair** (pair pattern where Opus writes structured plan into `§Plan` section; Sonnet applies), **plan review** (Opus stage that reads all Tasks of a Stage together + master-plan header + invariants; outputs `§Plan Fix`), **plan-fix apply** (Sonnet pair-tail that reads `§Plan Fix` + applies edits), **spec enrichment** (Sonnet stage that pulls glossary anchors + tightens spec terminology; replaces kickoff), **Opus audit** (Opus stage post-verify that reads spec→impl→findings→verify output + writes `§Audit` + `§Closeout Plan`), **Opus code review** (Opus stage that reads diff vs spec + invariants + glossary; PASS / minor / `§Code Fix Plan`), **code-fix apply** (Sonnet pair-tail reads `§Code Fix Plan` + applies + re-enters `/verify-loop`), **closeout apply** (Sonnet pair-tail reads `§Closeout Plan` + archives BACKLOG row + deletes spec + validates); redefine **Stage** as parent-of-Task (was child of Step); redefine **Project hierarchy** to 2-level (Stage → Task); tombstone **Phase** (redirect: use Stage) + **Gate** (redirect: use Stage exit criteria); run `npm run validate:frontmatter`; flip M1 done in migration JSON. |

---

### Step 2 — Data Migration: Master Plans + Backlog Schema

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 2):** 0 filed

**Objectives:** Transform all 16 open orchestrator master plans in place from Step/Stage/Phase/Task schema to Stage/Task schema using a crash-safe script. Fold Phase frontmatter out of all open project specs and backlog yaml records. Regenerate BACKLOG.md and BACKLOG-ARCHIVE.md views without Phase column. After this step the entire codebase authoring surface is consistently on Stage/Task schema.

**Exit criteria:**

- `tools/scripts/migrate-master-plans.ts` authored and committed (canary + batch tested).
- All 16 `ia/projects/*master-plan*.md` migrated: old Step/Stage/Phase/Task → new Stage/Task; task rows verbatim (Issue ids + Status columns untouched); per-file progress in migration JSON.
- All open `ia/projects/{ISSUE_ID}.md` specs: `parent_phase` frontmatter field absent; `parent_stage` set to old (step, stage) combo.
- All `ia/backlog/*.yaml`: `phase` field absent; `parent_stage` set; `id` + monotonic counter untouched.
- `BACKLOG.md` + `BACKLOG-ARCHIVE.md` regenerated without Phase column.
- `validate:dead-project-specs` + yaml schema check pass.
- `npm run validate:all` passes.
- Migration JSON M2 + M3 flipped to `done`.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` §Implementation Points M2–M3 + §Examples (master plan transform example + edge cases).
- Step 1 outputs: rewritten `ia/templates/master-plan-template.md` + `ia/rules/project-hierarchy.md` (ground truth for post-migration schema).
- `ia/state/pre-refactor-snapshot/` — source for transform (read snapshot, emit to current path).
- `ia/state/lifecycle-refactor-migration.json` — per-file progress tracking.
- `tools/scripts/materialize-backlog.sh` (exists — runs after yaml updates).
- `web/lib/plan-parser.ts` (exists — reader: Stage 3.2 updates it; M3 migration must not break it catastrophically before Stage 3.2 runs; note ordering dep).

---

#### Stage 2.1 — Transform Script + Master-Plan In-Place Migration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Author and run `migrate-master-plans.ts`. Canary on 1 low-risk closed plan, then batch on remaining 15. Each run reads from snapshot (idempotent), emits to current path. Per-file progress tracked in migration JSON for crash resume.

**Exit:**

- `tools/scripts/migrate-master-plans.ts` exists and parses old Step/Stage/Phase/Task AST; emits Stage/Task; preserves task-row `Issue` + `Status` columns verbatim; appends old Phase exit criteria to parent Stage exit; renames task ids `T{step}.{stage}.{task}` → `T{stage}.{task}` (stage renumbered sequentially).
- Canary: `blip-master-plan.md` (fully closed, safe) migrated + `npm run validate:all` passes.
- All 15 remaining open master plans migrated; 2 randomly selected manually diffed.
- Migration JSON M2 per-file entries all `done`.
- `npm run validate:all` passes on full set.

**Phases:**

- [ ] Phase 1 — Transform script authoring + canary run.
- [ ] Phase 2 — Batch migration + validate.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | Author migrate-master-plans.ts | 1 | _pending_ | _pending_ | Write `tools/scripts/migrate-master-plans.ts` (TypeScript, run via `npx tsx`): (a) reads `ia/state/lifecycle-refactor-migration.json` M2.per-file; skips files with status `done`; (b) for each pending file, reads from `ia/state/pre-refactor-snapshot/{relative-path}` (not current); (c) parses markdown AST: detect Step→Stage→Phase→Task structure; map old Step+Stage pair → new Stage (name = "{Step name} / {Stage name}"); merge Phase exit bullets into parent Stage Exit section; strip Phase heading rows from Phases section; drop `Phase` column from task-table headers; renumber task ids `T{N}.{M}.{k}` → `T{stage_seq}.{k}` preserving Issue + Status; (d) emit to current file path (not snapshot); (e) flip file to `done` in migration JSON immediately after emit. |
| T2.1.2 | Canary run + parser fix | 1 | _pending_ | _pending_ | Run `npx tsx tools/scripts/migrate-master-plans.ts --only blip-master-plan.md`; diff output against snapshot; verify task rows verbatim (Issue ids unchanged), exit criteria merged, Phase column absent; run `npm run validate:all`; fix any parser edge cases (e.g. nested code blocks, missing Phase sections); commit fix to migration script; mark canary `done` in migration JSON. |
| T2.1.3 | Batch migration — 15 remaining plans | 2 | _pending_ | _pending_ | Run `npx tsx tools/scripts/migrate-master-plans.ts` (all pending); monitor migration JSON per-file progress; on crash: re-run (idempotent — reads snapshot + skips done files); update migration JSON M2 per-file to all `done`; manually diff 2 randomly selected plans (e.g. `zone-s-economy-master-plan.md` + `multi-scale-master-plan.md`) against expected output. |
| T2.1.4 | Batch validate + M2 flip | 2 | _pending_ | _pending_ | Run `npm run validate:all` on full repo after batch migration; fix any validation failures (likely: task-id format checks in validators); run `npm run validate:frontmatter`; flip migration JSON M2 `done`. |

---

#### Stage 2.2 — Phase Layer Fold: Specs + YAML Schema

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Remove Phase frontmatter from all open project specs. Remove `phase` field from all backlog yaml records. Regenerate BACKLOG views without Phase column. Validate no orphan specs or broken yaml after schema update.

**Exit:**

- All open `ia/projects/{ISSUE_ID}.md`: `parent_phase:` frontmatter line absent; `parent_stage:` present with correct value.
- All `ia/backlog/*.yaml`: `phase:` field absent; `parent_stage:` present; `id:` + counter untouched.
- `BACKLOG.md` regenerated: Phase column absent from all rows.
- `validate:dead-project-specs` passes (no orphan specs).
- `npm run validate:all` passes.
- Migration JSON M3 flipped to `done`.

**Phases:**

- [ ] Phase 1 — Spec frontmatter fold + dead-spec validate.
- [ ] Phase 2 — YAML schema update + BACKLOG regen.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | Fold parent_phase from open specs | 1 | _pending_ | _pending_ | For each open `ia/projects/{ISSUE_ID}.md` (read list from migration JSON M3.specs array): remove `parent_phase:` line; ensure `parent_stage:` is set to old `(parent_step, parent_stage)` concatenated as `"{step}.{stage}"`; leave all body content (§Implementation, §Verification, §Audit etc.) untouched; update migration JSON M3.specs per-file to `done` immediately after edit. |
| T2.2.2 | Validate dead-spec + spec frontmatter | 1 | _pending_ | _pending_ | Run `npm run validate:dead-project-specs`; fix any orphan specs flagged (spec file with no matching yaml entry in `ia/backlog/`); run `npm run validate:frontmatter` on all modified spec files; confirm no `parent_phase` field remains in any open spec. |
| T2.2.3 | Drop phase field from backlog yaml | 2 | _pending_ | _pending_ | For each `ia/backlog/*.yaml` (read list from migration JSON M3.yaml array): remove `phase:` field; set `parent_stage:` to correct stage id; update `tools/mcp-ia-server/src/parser/` backlog-schema expectation to not require `phase` field (check `ia/templates/frontmatter-schema.md` + any schema validation in parser for field allowlist); update migration JSON M3.yaml per-file to `done`. |
| T2.2.4 | BACKLOG regen + M3 flip | 2 | _pending_ | _pending_ | Run `bash tools/scripts/materialize-backlog.sh`; verify `BACKLOG.md` + `BACKLOG-ARCHIVE.md` emit without Phase column; run `npm run validate:all`; flip migration JSON M3 `done`. |

---

### Step 3 — Infrastructure + Execution Surface

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 3):** 0 filed

**Objectives:** Update the three execution-facing subsystems so the new chain is runnable end-to-end. MCP server drops Phase-aware params and gains `plan_apply_validate`. Web dashboard parser + PlanTree collapse to Stage/Task 2-level. Full skill/agent/command inventory rewritten: 11 new skills (5 Opus pair-heads + 5 Sonnet pair-tails + 1 demoted spec-enricher), 11 new agents, 7 command changes, lifecycle rule docs rewritten. After this step a Task can be filed and shipped through the new chain.

**Exit criteria:**

- MCP `router_for_task` lifecycle-stage enum includes pair-head + pair-tail stage names; Phase-aware params absent from all MCP tools; `plan_apply_validate` tool registered + schema-cache restarted.
- `web/lib/plan-parser.ts` + `web/lib/plan-loader-types.ts` expect Stage→Task 2-level; `web/lib/plan-tree.ts` renders 2-level; `npm run validate:web` passes.
- 11 new `ia/skills/{name}/SKILL.md` files present.
- 11 new `.claude/agents/*.md` files present; `spec-kickoff.md` + Opus `closeout.md` moved to `.claude/agents/_retired/`.
- 4 new + 3 repointed `.claude/commands/*.md` updated.
- `ia/rules/agent-lifecycle.md` + `docs/agent-lifecycle.md` rewritten with pair-head/pair-tail surface map.
- `npm run validate:all` passes.
- Migration JSON M4 + M5 + M6 flipped to `done`.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` §Design Expansion → Architecture (flowchart), Subsystem Impact table, Implementation Points M4–M6.
- Step 1 outputs: `ia/rules/plan-apply-pair-contract.md` (contract all new skills reference).
- `tools/mcp-ia-server/src/tools/` (exists — MCP tool handlers).
- `tools/mcp-ia-server/src/ia-index/` (exists — likely contains router_for_task enum).
- `web/lib/plan-parser.ts` + `web/lib/plan-loader-types.ts` + `web/lib/plan-tree.ts` (exist — web parser targets).
- `.claude/agents/` (exists — 17 current agents; add 11, retire 2, create `_retired/` subdir).
- `ia/skills/project-spec-kickoff/SKILL.md` + `ia/skills/project-spec-close/SKILL.md` (exist — retire targets).
- `ia/rules/agent-lifecycle.md` + `docs/agent-lifecycle.md` (exist — rewrite targets).
- Prior step outputs: Stage 2.1 migrated master plans (all in Stage/Task schema; confirms parser can expect 2-level).

---

#### Stage 3.1 — MCP Server: Drop Phase + plan_apply_validate

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Remove Phase-aware parameters from MCP tool handlers. Add `plan_apply_validate` tool (validates §Plan anchor presence before applier runs). Update `router_for_task` lifecycle-stage enum to include pair-head + pair-tail stage names. Restart schema cache.

**Exit:**

- `router_for_task` `lifecycle_stage` enum: Phase-related values removed; new values added: `plan_review`, `plan_fix_apply`, `stage_file_plan`, `stage_file_apply`, `project_new_plan`, `project_new_apply`, `spec_enrich`, `opus_audit`, `opus_code_review`, `code_fix_apply`, `closeout_apply`.
- Phase params absent from `router_for_task`, `spec_section`, `backlog_issue`, `project_spec_closeout_digest`.
- `project_spec_closeout_digest` reads 4 new spec sections: `§Audit`, `§Code Review`, `§Code Fix Plan`, `§Closeout Plan`.
- `plan_apply_validate(section_header, target_path)` tool registered; handler validates that `§{section_header}` heading exists in `{target_path}` + tuple list is non-empty.
- MCP schema cache restarted (kill + respawn `territory-ia` process).
- MCP smoke tests pass (`npm run validate:mcp` if exists; else handler unit tests).
- Migration JSON M4 flipped to `done`.

**Phases:**

- [ ] Phase 1 — Drop Phase params + enum update + closeout-digest new sections.
- [ ] Phase 2 — New plan_apply_validate tool + cache restart + validate.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.1.1 | Drop Phase params + update enum | 1 | _pending_ | _pending_ | In `tools/mcp-ia-server/src/`: find all tool handler files referencing `phase` or `parent_phase` as input params (grep `phase` across `tools/` + `schemas/` dirs); remove Phase-aware params from `router_for_task`, `spec_section`, `backlog_issue`; update `router_for_task` `lifecycle_stage` enum (add 11 pair/enrich values; remove Phase-era values); update `project_spec_closeout_digest` handler to read 4 new spec sections (`§Audit`, `§Code Review`, `§Code Fix Plan`, `§Closeout Plan`) in addition to existing reads. |
| T3.1.2 | Schema cache restart + smoke | 1 | _pending_ | _pending_ | Restart `territory-ia` MCP process (kill PID or use project npm script); verify Claude Code reconnects to updated schema; run `npm run validate:mcp` (or targeted handler unit tests in `tools/mcp-ia-server/`); confirm `router_for_task` responds with new enum values when queried with `plan_review` stage name. |
| T3.1.3 | Author plan_apply_validate tool | 2 | _pending_ | _pending_ | Add `plan_apply_validate` tool to MCP server: handler signature `(section_header: string, target_path: string) → {ok: boolean, found: boolean, tuple_count: number, error?: string}`; implementation reads `target_path`, searches for `## {section_header}` heading, counts `{operation, target_path, target_anchor, payload}` tuple lines below it; returns `found: false` if heading absent; registers tool in MCP index alongside `plan_apply_pair-contract.md` reference. |
| T3.1.4 | Register + validate + M4 flip | 2 | _pending_ | _pending_ | Register `plan_apply_validate` in MCP tool index (`tools/mcp-ia-server/src/index.ts` or equivalent entry point); restart schema cache again; run smoke test calling `plan_apply_validate` with a valid spec path + known section header; flip migration JSON M4 `done`. |

---

#### Stage 3.2 — Web Dashboard: Parser + PlanTree Collapse

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Update web dashboard parser and PlanTree rendering to expect Stage→Task 2-level hierarchy. Remove Phase level from tree model and UI. Validate web build passes.

**Exit:**

- `web/lib/plan-loader-types.ts` — `PlanNode` / `StageNode` / `TaskNode` types reflect 2-level (no `PhaseNode`).
- `web/lib/plan-parser.ts` — parser reads Stage→Task; Phase grouping logic removed.
- `web/lib/plan-tree.ts` — `PlanTree` renders Stage/Task rows; Phase headers absent.
- `npm run validate:web` passes (lint + typecheck + build).
- Migration JSON M5 flipped to `done`.

**Phases:**

- [ ] Phase 1 — Types + parser update.
- [ ] Phase 2 — PlanTree rendering update + validate.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.2.1 | Update plan-loader-types + parser | 1 | _pending_ | _pending_ | Edit `web/lib/plan-loader-types.ts`: remove `PhaseNode` type (or `phase` field in `StageNode`); update `StageNode` to hold `tasks: TaskNode[]` directly; edit `web/lib/plan-parser.ts`: remove Phase-level parsing block (the parser currently detects `#### Phase N` or `- [ ] Phase N` rows and groups tasks under phases); re-group tasks directly under their parent Stage; verify `web/lib/releases.ts` + `web/lib/plan-loader.ts` are updated if they reference Phase fields. |
| T3.2.2 | Update plan-tree + plan-loader | 1 | _pending_ | _pending_ | Edit `web/lib/plan-tree.ts`: remove Phase header rendering row; render tasks directly under Stage; verify task Status column still renders `_pending_ / Draft / In Review / In Progress / Done (archived)` correctly after restructure; check `web/app/dashboard/**/page.tsx` usage of `PlanTree` for any Phase-specific props that need removal. |
| T3.2.3 | Web validate + type-check | 2 | _pending_ | _pending_ | Run `cd web && npm run validate:web` (= lint + typecheck + build); fix any TypeScript errors from Phase-type removal; re-run until green. |
| T3.2.4 | Preview deploy + M5 flip | 2 | _pending_ | _pending_ | Run `npm run deploy:web:preview`; open `/dashboard` on preview URL; visually confirm Stage→Task tree renders without Phase rows; confirm all migrated master plans (Step 2 output) display correctly; flip migration JSON M5 `done`. |

---

#### Stage 3.3 — Skills + Agents + Commands Rewrite

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Author all 11 new skill SKILL.md files (5 Opus pair-heads + 5 Sonnet pair-tails + 1 Sonnet spec-enricher). Author 11 new `.claude/agents/*.md` files. Update 7 `.claude/commands/*.md` (4 new + 3 repointed). Retire `spec-kickoff` + Opus `closeout` agents. Rewrite `ia/rules/agent-lifecycle.md` + `docs/agent-lifecycle.md` with Plan-Apply pair as a first-class hard rule.

**Exit:**

- `ia/skills/plan-review/`, `ia/skills/plan-fix-apply/`, `ia/skills/stage-file-plan/`, `ia/skills/stage-file-apply/`, `ia/skills/project-new-plan/`, `ia/skills/project-new-apply/`, `ia/skills/opus-audit/`, `ia/skills/opus-code-review/`, `ia/skills/code-fix-apply/`, `ia/skills/closeout-apply/`, `ia/skills/spec-enrich/` — all SKILL.md files present.
- `.claude/agents/` — 11 new agent markdown files present; `spec-kickoff.md` + Opus `closeout.md` moved to `.claude/agents/_retired/`.
- `.claude/commands/` — `/plan-review.md`, `/enrich.md`, `/audit.md`, `/code-review.md` new; `/kickoff.md` repointed → spec-enricher; `/stage-file.md` repointed → planner→applier pair; `/project-new.md` repointed → planner→applier pair.
- `ia/rules/agent-lifecycle.md`: ordered flow updated; Plan-Apply pair hard rule added; surface map rows updated.
- `docs/agent-lifecycle.md`: flow diagram + stage→surface matrix updated.
- Remaining lifecycle skills (project-spec-implement, verify-loop, ship-stage, stage-file, project-new): Phase references removed; Stage-level MCP bundle contract added.
- `npm run validate:all` passes.
- Migration JSON M6 flipped to `done`.

**Phases:**

- [ ] Phase 1 — Plan-review + stage-file pair skills (head + tail each).
- [ ] Phase 2 — Remaining pair skills + audit-chain skills.
- [ ] Phase 3 — Spec-enrich skill + retire old skills + update remaining lifecycle skills.
- [ ] Phase 4 — All new agents + retire old agents.
- [ ] Phase 5 — Commands repoint + lifecycle rule docs + validate.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.3.1 | Plan-review + plan-fix-apply skills | 1 | _pending_ | _pending_ | Author `ia/skills/plan-review/SKILL.md`: Opus pair-head; runs once per Stage before first Task kickoff; reads all Stage Task specs + master-plan Stage header + invariants; outputs `§Plan Fix` with `{operation, target_path, target_anchor, payload}` tuples; contract per `ia/rules/plan-apply-pair-contract.md`; PASS → downstream continue; fix-authored → spawn plan-fix-apply. Author `ia/skills/plan-fix-apply/SKILL.md`: Sonnet pair-tail; reads `§Plan Fix` tuples; applies edits to master-plan task-table rows + spec §Impl / §Tests sections literally; runs `validate:frontmatter`; on anchor ambiguity → escalate to Opus; on clean → report applied + return to plan-reviewer for re-check. |
| T3.3.2 | Stage-file pair skills | 1 | _pending_ | _pending_ | Author `ia/skills/stage-file-plan/SKILL.md`: Opus pair-head; split from current `stage-file` skill; authors `§Stage File Plan` section in master plan per Stage (structured list: one entry per Task with `{reserved_id, title, priority, notes, depends_on, related, stub_body}`); resolves all anchors before handing to applier; loads shared Stage-level MCP bundle once (glossary + router + invariants via `domain-context-load`). Author `ia/skills/stage-file-apply/SKILL.md`: Sonnet pair-tail; reads `§Stage File Plan` entries; for each entry: runs `reserve-id.sh`, writes `ia/backlog/{id}.yaml`, writes `ia/projects/{id}.md` stub; updates master-plan task table; runs `materialize-backlog.sh`; validates dead specs; escalates on id-counter lock failure. |
| T3.3.3 | Project-new pair skills | 2 | _pending_ | _pending_ | Author `ia/skills/project-new-plan/SKILL.md`: Opus pair-head; single-issue variant of stage-file-plan (N=1); authors `§Project-New Plan` section in spec draft; when invoked under `/stage-file`, upstream plan reused — skip direct plan step; otherwise runs full plan authoring. Author `ia/skills/project-new-apply/SKILL.md`: Sonnet pair-tail; same apply contract as stage-file-apply at N=1; reads `§Project-New Plan`; runs `reserve-id.sh` + writes yaml + stub + materialize-backlog + validate. |
| T3.3.4 | Opus-audit + code-review + code-fix + closeout-apply skills | 2 | _pending_ | _pending_ | Author `ia/skills/opus-audit/SKILL.md`: Opus stage post-verify; reads spec §Implementation + §Findings + §Verification output; writes `§Audit` paragraph (one-paragraph synthesis — what was built, what worked, what to watch); also writes `§Closeout Plan` (structured migration anchors: glossary rows, rule sections, doc paragraphs, BACKLOG archive op, id purge list). Author `ia/skills/opus-code-review/SKILL.md`: Opus pair-head; reads diff vs spec + invariants + glossary; outcomes: (a) PASS → mini-report; (b) minor → suggest fix-in-place or deferred issue, no pair tail; (c) critical → writes `§Code Fix Plan`. Author `ia/skills/code-fix-apply/SKILL.md`: Sonnet pair-tail; reads `§Code Fix Plan`; applies edits; re-enters `/verify-loop`; bounded 1 retry — escalates to Opus on second fail. Author `ia/skills/closeout-apply/SKILL.md`: Sonnet pair-tail; reads `§Closeout Plan`; applies each migration anchor (glossary/rule/doc edits); archives `ia/backlog/{id}.yaml` → `ia/backlog-archive/`; deletes spec; runs `validate:dead-project-specs` + `materialize-backlog.sh`; emits closeout-digest; escalates to Opus on anchor ambiguity or validator failure. |
| T3.3.5 | Spec-enrich skill + retire old skills + update lifecycle skills | 3 | _pending_ | _pending_ | Author `ia/skills/spec-enrich/SKILL.md`: Sonnet executor; demoted rename from `project-spec-kickoff`; pulls glossary anchors via MCP `glossary_discover`; tightens spec terminology to canonical terms in `§Objective`, `§Background`, `§Implementation Plan` sections; validates no ad-hoc synonyms remain; no judgment or spec-body rewrite — mechanical text transform only. Add tombstone redirect header to `ia/skills/project-spec-kickoff/SKILL.md` ("Retired — use spec-enrich") and `ia/skills/project-spec-close/SKILL.md` ("Retired — use closeout-apply"). |
| T3.3.6 | Update remaining lifecycle skills | 3 | _pending_ | _pending_ | Edit `ia/skills/project-spec-implement/SKILL.md`, `ia/skills/verify-loop/SKILL.md`, `ia/skills/ship-stage/SKILL.md`, `ia/skills/stage-file/SKILL.md`, `ia/skills/project-new/SKILL.md`: in each, remove all Phase-layer references (Phase bullets, Phase cardinality gate, Phase context reload); replace with Stage-level MCP bundle contract (shared `domain-context-load` result from Stage opener; Sonnet never re-queries glossary/router within a Stage); update lifecycle-stage enum references to match new pair-head + pair-tail names from Stage 3.1. |
| T3.3.7 | Author all new agents | 4 | _pending_ | _pending_ | Write `.claude/agents/plan-reviewer.md` (Opus), `plan-fix-applier.md` (Sonnet), `stage-file-planner.md` (Opus — rename of current stage-file agent), `stage-file-applier.md` (Sonnet), `project-new-planner.md` (Opus — rename of current project-new agent), `project-new-applier.md` (Sonnet), `opus-auditor.md` (Opus), `opus-code-reviewer.md` (Opus), `code-fix-applier.md` (Sonnet), `closeout-applier.md` (Sonnet — replaces current Opus closeout agent), `spec-enricher.md` (Sonnet — demoted rename of spec-kickoff). Each agent body: caveman preamble + matching SKILL.md reference + model tier header; Opus agents list MCP tools needed; Sonnet agents list `plan-apply-pair-contract.md` as primary constraint. Move `spec-kickoff.md` + Opus `closeout.md` to `.claude/agents/_retired/` (create dir). |
| T3.3.8 | Update commands | 4 | _pending_ | _pending_ | Write 4 new `.claude/commands/`: `/plan-review.md` (dispatches plan-reviewer agent), `/enrich.md` (dispatches spec-enricher), `/audit.md` (dispatches opus-auditor), `/code-review.md` (dispatches opus-code-reviewer). Repoint 3 existing: `/kickoff.md` → dispatches spec-enricher (drop Opus spec-kickoff dispatch); `/stage-file.md` → dispatches stage-file-planner then stage-file-applier pair; `/project-new.md` → dispatches project-new-planner then project-new-applier pair. Each command: caveman-asserting prompt forwarding args; cites SKILL.md. |
| T3.3.9 | Rewrite lifecycle rule docs | 5 | _pending_ | _pending_ | Rewrite `ia/rules/agent-lifecycle.md`: update ordered flow (add plan-review between stage-file and first enrich; add audit + code-review between verify-loop and closeout-apply; rename kickoff → enrich); add Plan-Apply pair as first-class hard rule section; update surface map table rows (add 11 new pair-head/tail rows; update model column for enricher → Sonnet; mark retired stages). Rewrite `docs/agent-lifecycle.md`: update flow diagram (Mermaid or ASCII) to match new chain; update stage→surface matrix with pair entries; update handoff contract table. |
| T3.3.10 | Validate + memory update + M6 flip | 5 | _pending_ | _pending_ | Run `npm run validate:all`; fix any failures from command/agent dispatch references to retired agents; update `AGENTS.md` lifecycle section to match new surface map; update project MEMORY entries in `MEMORY.md` that reference Phase or old kickoff/closeout dispatch pattern; flip migration JSON M6 `done`. |

---

### Step 4 — Validation + Merge

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 4):** 0 filed

**Objectives:** Dry-run one Task through the new chain end-to-end (no commit). Regenerate all views. Run full `verify:local`. Present to user for sign-off. Merge branch, restart MCP, close freeze window. File token-cost telemetry tracker as follow-up.

**Exit criteria:**

- Dry-run on 1 pending Task using new chain completes without failure (plan-review → stage-file-plan → stage-file-apply → spec-enrich → implement → verify-loop → opus-audit → opus-code-review → closeout-apply); dry-run artifacts committed to migration JSON.
- `BACKLOG.md` + `BACKLOG-ARCHIVE.md` regenerated; `docs/progress.html` regenerated.
- `npm run verify:local` passes (validate:all + unity:compile-check + db:bridge-preflight chain).
- User gate: sign-off recorded in migration JSON M8.
- `feature/lifecycle-collapse-cognitive-split` merged to main.
- MCP server restarted post-merge.
- Freeze note removed from `CLAUDE.md`.
- Token-cost telemetry follow-up TECH issue filed.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `ia/state/lifecycle-refactor-migration.json` — current migration state (M7 entry).
- Step 3 outputs: all 11 new skills + agents + commands.
- `docs/agent-led-verification-policy.md` — verification policy (unchanged; only calling surface changed).

---

#### Stage 4.1 — Dry-Run + Full Validation

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Execute one Task end-to-end through the new chain to catch integration issues before merge. Regenerate all view files. Run full local verification chain.

**Exit:**

- Dry-run Task identified (small, non-critical pending task from any open master plan); chain executed without error.
- `BACKLOG.md` + `BACKLOG-ARCHIVE.md` + `docs/progress.html` regenerated and consistent.
- `npm run verify:local` green.
- Migration JSON M7 flipped to `done`.

**Phases:**

- [ ] Phase 1 — Dry-run new chain + regen views.
- [ ] Phase 2 — Full verify:local + fix + M7 flip.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.1.1 | Dry-run new chain end-to-end | 1 | _pending_ | _pending_ | Select a small pending Task from any open master plan (prefer a Task in _pending_ state, not one currently In Progress); run the new chain: `/plan-review` on its Stage → `/enrich` → `/implement` (no actual code ship; stop after plan-review + enrich to validate dispatch wiring) → simulate audit + code-review outputs → verify closeout-apply reads `§Closeout Plan` stub correctly; document each pair's handoff in migration JSON M7.dry-run section; no commit of dry-run artifacts. |
| T4.1.2 | Regen BACKLOG + progress.html | 1 | _pending_ | _pending_ | Run `bash tools/scripts/materialize-backlog.sh` → verify `BACKLOG.md` + `BACKLOG-ARCHIVE.md` consistent with yaml state post-M3; run `npm run progress` → verify `docs/progress.html` renders Stage/Task 2-level tree correctly (no Phase rows). |
| T4.1.3 | Full verify:local | 2 | _pending_ | _pending_ | Run `npm run verify:local` (validate:all + unity:compile-check + db:bridge-preflight); triage any failures by subsystem: web failures → Stage 3.2 patch; MCP failures → Stage 3.1 patch; skill/agent failures → Stage 3.3 patch; yaml failures → Stage 2.2 patch. |
| T4.1.4 | Fix remaining failures + M7 flip | 2 | _pending_ | _pending_ | Apply minimal targeted fixes for any failures from T4.1.3; re-run `npm run verify:local` until green; flip migration JSON M7 `done`. |

---

#### Stage 4.2 — Sign-Off + Merge

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Present dry-run artifacts to user. Collect sign-off. Merge branch, restart MCP, and close freeze window. File token-cost telemetry follow-up.

**Exit:**

- Migration JSON M8 gate entry written with user sign-off timestamp.
- `feature/lifecycle-collapse-cognitive-split` merged to main.
- `territory-ia` MCP server restarted post-merge; new schema verified.
- Freeze note removed from `CLAUDE.md`.
- Token-cost telemetry follow-up TECH issue filed in `ia/backlog/`.
- Migration JSON M8 flipped to `done`.

**Phases:**

- [ ] Phase 1 — User gate + MCP post-merge restart.
- [ ] Phase 2 — Merge + freeze-close + follow-up issue.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.2.1 | User sign-off gate | 1 | _pending_ | _pending_ | Present dry-run artifacts (migration JSON M7.dry-run, `BACKLOG.md` diff, `docs/progress.html` screenshot) to user; wait for explicit sign-off ("LGTM" / "merge"); record sign-off + timestamp in migration JSON M8.gate; do not proceed to T4.2.3 without gate. |
| T4.2.2 | MCP restart + schema verify | 1 | _pending_ | _pending_ | Kill and respawn `territory-ia` MCP process on the post-merge main branch; send a test `router_for_task` call with `plan_review` stage name; confirm enum accepted; confirm `plan_apply_validate` tool responds; record restart success in migration JSON. |
| T4.2.3 | Merge branch | 2 | _pending_ | _pending_ | Merge `feature/lifecycle-collapse-cognitive-split` into main (standard merge commit, no squash — preserve migration history); resolve any conflicts in `BACKLOG.md` / `BACKLOG-ARCHIVE.md` from concurrent activity during freeze window by re-running `materialize-backlog.sh` post-merge; flip migration JSON M8 `done`. |
| T4.2.4 | Freeze close + token-cost issue | 2 | _pending_ | _pending_ | Remove freeze note from `CLAUDE.md` §Key commands; file a token-cost telemetry tracker TECH issue in `ia/backlog/` (title: "Token-cost telemetry baseline — pre/post lifecycle refactor"; stub only; priority: Low); run final `npm run validate:all` on main post-merge to confirm clean state. |

---

## Orchestration guardrails

**Do:**

- Open one Stage at a time. Next Stage opens only after current Stage's `project-stage-close` runs.
- Run `claude-personal "/stage-file ia/projects/lifecycle-refactor-master-plan.md Stage 1.1"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update Stage / Step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Q1–Q5 are closed; changes require explicit re-decision + sync edit to exploration doc.
- Respect migration JSON: always read current state before resuming; write per-file progress immediately after each file is processed (crash safety).
- Consult `ia/state/pre-refactor-snapshot/` for the canonical source of pre-refactor state. Never use current `ia/projects/*master-plan*.md` as M2 input — always read from snapshot.
- Self-migration note: when running `migrate-master-plans.ts` in Stage 2.1, run `lifecycle-refactor-master-plan.md` itself last in the M2 batch after all other plans are validated.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only Stage 4.2 Final triggers `Status: Final`; the file stays.
- Skip the user gate at Stage 4.2 T4.2.1 — merge requires explicit human sign-off.
- Run new `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, or `/stage-file` calls outside this orchestrator during the freeze window (M0–M8). The freeze is declared in `CLAUDE.md` by T1.1.1.
- Merge partial stage state — every Stage must reach green-bar before `project-stage-close` runs.
- Insert BACKLOG rows directly into this doc — only `stage-file-apply` materializes them.
- Use `migrate-master-plans.ts` on any spec file other than orchestrator master plans — project specs (`ia/projects/{ISSUE_ID}.md`) are handled by T2.2.1 (separate targeted edit, not the batch transform script).
