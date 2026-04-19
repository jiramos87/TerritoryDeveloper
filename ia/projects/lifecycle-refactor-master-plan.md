# Lifecycle Refactor — Opus Planner / Sonnet Executor + Hierarchy Collapse — Master Plan (Umbrella)

> **Status:** In Progress — Stage 7
>
> **Scope:** Big-bang collapse of Step/Stage/Phase/Task hierarchy to Stage/Task. Introduce Plan-Apply pair pattern (5 seams) with Opus pair-heads and Sonnet pair-tails. Sonnet-ify spec enrichment. Add Opus audit + code-review inline stages. Migrate all 16 open master plans + open project specs + backlog yaml in place. Tooling surface only — zero Unity runtime C# touch.
>
> **Exploration source:** `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` (§Design Expansion: Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Review Notes). Rev 2–4 extensions appended: plan-author + progress-emit (rev 2); stage-end bulk closeout (rev 2); stage-end bulk plan-author + audit + spec-enrich fold (rev 3); **rev 4 candidates + cache-mechanics amendments (2026-04-19 rev 4)** — prompt-caching optimization layer; Stage 10 post-merge fold gated by Q9 baseline.
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

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | Skeleton | Planned | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`; `Skeleton` + `Planned` authored by `master-plan-new` / `stage-decompose`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `stage-file` also flips Stage header `Draft/Planned → In Progress` (R2) and plan top Status `Draft → In Progress — Step {N} / Stage {N.M}` on first task ever filed (R1); `stage-decompose` → Step header `Skeleton → Draft (tasks _pending_)` (R7); `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level step rollup; `project-stage-close` / `project-spec-close` → plan top Status `→ Final` when all Steps read `Final` (R5); `master-plan-extend` → plan top Status `Final → In Progress — Step {N_new} / Stage {N_new}.1` when new Steps appended to a Final plan (R6).

---

### Stage 1 — Foundation: Freeze, Templates & Rules / Branch + Snapshot + Migration State

**Status:** Final

**Objectives:** Create migration branch. Snapshot current master plans + open specs + backlog yaml so M2/M3 can always re-read from clean state. Write migration JSON with resumability keys.

**Exit:**

- Branch `feature/lifecycle-collapse-cognitive-split` checked out.
- `ia/state/pre-refactor-snapshot/` contains tarball (or flat copy) of all `ia/projects/*master-plan*.md`, `ia/backlog/*.yaml`, `ia/backlog-archive/*.yaml`, and open `ia/projects/{ISSUE_ID}.md` files at snapshot time.
- `ia/state/lifecycle-refactor-migration.json` written with M0–M8 phase entries (`pending` / `done`) + per-file progress arrays for M2 and M3.
- Migration JSON M0 flipped to `done`.
- Phase 1 — Branch creation + freeze note + initial migration JSON.
- Phase 2 — Snapshot pre-refactor state + validate integrity.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Branch + freeze setup | **TECH-442** | Done (archived) | Create `feature/lifecycle-collapse-cognitive-split` via `git checkout -b`; add freeze note to `CLAUDE.md` §Key commands warning against running `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, `/stage-file` until M8 sign-off; write initial `ia/state/lifecycle-refactor-migration.json` (M0 done, M1–M8 pending; per-file arrays for M2: list of all `*master-plan*.md` paths, each `pending`; per-file array for M3: list of all `ia/backlog/*.yaml` + open `ia/projects/{ISSUE_ID}.md` paths). |
| T1.2 | Pre-refactor snapshot | **TECH-443** | Done (archived) | Copy all `ia/projects/*master-plan*.md`, `ia/backlog/*.yaml`, `ia/backlog-archive/*.yaml`, and open `ia/projects/{ISSUE_ID}.md` into `ia/state/pre-refactor-snapshot/` (preserve relative paths); write `ia/state/pre-refactor-snapshot/manifest.json` with file list + counts + git SHA; update migration JSON referencing snapshot path; flip M0 `done` in JSON. |

---

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

### Stage 3 — Data Migration: Master Plans + Backlog Schema / Transform Script + Master-Plan In-Place Migration

**Status:** Final

**Objectives:** Author and run `migrate-master-plans.ts`. Canary on 1 low-risk closed plan, then batch on remaining 15. Each run reads from snapshot (idempotent), emits to current path. Per-file progress tracked in migration JSON for crash resume.

**Exit:**

- `tools/scripts/migrate-master-plans.ts` exists and parses old Step/Stage/Phase/Task AST; emits Stage/Task; preserves task-row `Issue` + `Status` columns verbatim; appends old Phase exit criteria to parent Stage exit; renames task ids `T{step}.{stage}.{task}` → `T{stage}.{task}` (stage renumbered sequentially).
- Canary: `blip-master-plan.md` (fully closed, safe) migrated + `npm run validate:all` passes.
- All 15 remaining open master plans migrated; 2 randomly selected manually diffed.
- Migration JSON M2 per-file entries all `done`.
- `npm run validate:all` passes on full set.
- Phase 1 — Transform script authoring + canary run.
- Phase 2 — Batch migration + validate.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | Author migrate-master-plans.ts | **TECH-450** | Done (archived) | Write `tools/scripts/migrate-master-plans.ts` (TypeScript, run via `npx tsx`): (a) reads `ia/state/lifecycle-refactor-migration.json` M2.per-file; skips files with status `done`; (b) for each pending file, reads from `ia/state/pre-refactor-snapshot/{relative-path}` (not current); (c) parses markdown AST: detect Step→Stage→Phase→Task structure; map old Step+Stage pair → new Stage (name = "{Step name} / {Stage name}"); merge Phase exit bullets into parent Stage Exit section; strip Phase heading rows from Phases section; drop `Phase` column from task-table headers; renumber task ids `T{N}.{M}.{k}` → `T{stage_seq}.{k}` preserving Issue + Status; (d) emit to current file path (not snapshot); (e) flip file to `done` in migration JSON immediately after emit. |
| T3.2 | Canary run + parser fix | **TECH-451** | Done (archived) | Run `npx tsx tools/scripts/migrate-master-plans.ts --only blip-master-plan.md`; diff output against snapshot; verify task rows verbatim (Issue ids unchanged), exit criteria merged, Phase column absent; run `npm run validate:all`; fix any parser edge cases (e.g. nested code blocks, missing Phase sections); commit fix to migration script; mark canary `done` in migration JSON. |
| T3.3 | Batch migration — 15 remaining plans | **TECH-452** | Done (archived) | Run `npx tsx tools/scripts/migrate-master-plans.ts` (all pending); monitor migration JSON per-file progress; on crash: re-run (idempotent — reads snapshot + skips done files); update migration JSON M2 per-file to all `done`; manually diff 2 randomly selected plans (e.g. `zone-s-economy-master-plan.md` + `multi-scale-master-plan.md`) against expected output. |
| T3.4 | Batch validate + M2 flip | **TECH-453** | Done (archived) | Run `npm run validate:all` on full repo after batch migration; fix any validation failures (likely: task-id format checks in validators); run `npm run validate:frontmatter`; flip migration JSON M2 `done`. |

---

### Stage 4 — Data Migration: Master Plans + Backlog Schema / Phase Layer Fold: Specs + YAML Schema

**Status:** Done

**Objectives:** Remove Phase frontmatter from all open project specs. Remove `phase` field from all backlog yaml records. Regenerate BACKLOG views without Phase column. Validate no orphan specs or broken yaml after schema update.

**Exit:**

- All open `ia/projects/{ISSUE_ID}.md`: `parent_phase:` frontmatter line absent; `parent_stage:` present with correct value.
- All `ia/backlog/*.yaml`: `phase:` field absent; `parent_stage:` present; `id:` + counter untouched.
- `BACKLOG.md` regenerated: Phase column absent from all rows.
- `validate:dead-project-specs` passes (no orphan specs).
- `npm run validate:all` passes.
- Migration JSON M3 flipped to `done`.
- Phase 1 — Spec frontmatter fold + dead-spec validate.
- Phase 2 — YAML schema update + BACKLOG regen.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | Fold parent_phase from open specs | **TECH-454** | Done (archived) | For each open `ia/projects/{ISSUE_ID}.md` (read list from migration JSON M3.specs array): remove `parent_phase:` line; ensure `parent_stage:` is set to old `(parent_step, parent_stage)` concatenated as `"{step}.{stage}"`; leave all body content (§Implementation, §Verification, §Audit etc.) untouched; update migration JSON M3.specs per-file to `done` immediately after edit. |
| T4.2 | Validate dead-spec + spec frontmatter | **TECH-455** | Done (archived) | Run `npm run validate:dead-project-specs`; fix any orphan specs flagged (spec file with no matching yaml entry in `ia/backlog/`); run `npm run validate:frontmatter` on all modified spec files; confirm no `parent_phase` field remains in any open spec. |
| T4.3 | Drop phase field from backlog yaml | **TECH-456** | Done (archived) | For each `ia/backlog/*.yaml` (read list from migration JSON M3.yaml array): remove `phase:` field; set `parent_stage:` to correct stage id; update `tools/mcp-ia-server/src/parser/` backlog-schema expectation to not require `phase` field (check `ia/templates/frontmatter-schema.md` + any schema validation in parser for field allowlist); update migration JSON M3.yaml per-file to `done`. |
| T4.4 | BACKLOG regen + M3 flip | **TECH-457** | Done (archived) | Run `bash tools/scripts/materialize-backlog.sh`; verify `BACKLOG.md` + `BACKLOG-ARCHIVE.md` emit without Phase column; run `npm run validate:all`; flip migration JSON M3 `done`. |

---

### Stage 5 — Infrastructure + Execution Surface / MCP Server: Drop Phase + plan_apply_validate

**Status:** Final

**Objectives:** Remove Phase-aware parameters from MCP tool handlers. Add `plan_apply_validate` tool (validates §Plan anchor presence before applier runs). Update `router_for_task` lifecycle-stage enum to include pair-head + pair-tail stage names. Restart schema cache.

**Exit:**

- `router_for_task` `lifecycle_stage` enum: Phase-related values removed; new values added: `plan_review`, `plan_fix_apply`, `stage_file_plan`, `stage_file_apply`, `project_new_plan`, `project_new_apply`, `spec_enrich`, `opus_audit`, `opus_code_review`, `code_fix_apply`, `closeout_apply`.
- Phase params absent from `router_for_task`, `spec_section`, `backlog_issue`, `project_spec_closeout_digest`.
- `project_spec_closeout_digest` reads 4 new spec sections: `§Audit`, `§Code Review`, `§Code Fix Plan`, `§Closeout Plan`.
- `plan_apply_validate(section_header, target_path)` tool registered; handler validates that `§{section_header}` heading exists in `{target_path}` + tuple list is non-empty.
- MCP schema cache restarted (kill + respawn `territory-ia` process).
- MCP smoke tests pass (`npm run validate:mcp` if exists; else handler unit tests).
- Migration JSON M4 flipped to `done`.
- Phase 1 — Drop Phase params + enum update + closeout-digest new sections.
- Phase 2 — New plan_apply_validate tool + cache restart + validate.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | Drop Phase params + update enum | **TECH-458** | Done (archived) | In `tools/mcp-ia-server/src/`: find all tool handler files referencing `phase` or `parent_phase` as input params (grep `phase` across `tools/` + `schemas/` dirs); remove Phase-aware params from `router_for_task`, `spec_section`, `backlog_issue`; update `router_for_task` `lifecycle_stage` enum (add 11 pair/enrich values; remove Phase-era values); update `project_spec_closeout_digest` handler to read 4 new spec sections (`§Audit`, `§Code Review`, `§Code Fix Plan`, `§Closeout Plan`) in addition to existing reads. |
| T5.2 | Schema cache restart + smoke | **TECH-459** | Done (archived) | Restart `territory-ia` MCP process (kill PID or use project npm script); verify Claude Code reconnects to updated schema; run `npm run validate:mcp` (or targeted handler unit tests in `tools/mcp-ia-server/`); confirm `router_for_task` responds with new enum values when queried with `plan_review` stage name. |
| T5.3 | Author plan_apply_validate tool | **TECH-460** | Done (archived) | Add `plan_apply_validate` tool to MCP server: handler signature `(section_header: string, target_path: string) → {ok: boolean, found: boolean, tuple_count: number, error?: string}`; implementation reads `target_path`, searches for `## {section_header}` heading, counts `{operation, target_path, target_anchor, payload}` tuple lines below it; returns `found: false` if heading absent; registers tool in MCP index alongside `plan_apply_pair-contract.md` reference. |
| T5.4 | Register + validate + M4 flip | **TECH-461** | Done (archived) | Register `plan_apply_validate` in MCP tool index (`tools/mcp-ia-server/src/index.ts` or equivalent entry point); restart schema cache again; run smoke test calling `plan_apply_validate` with a valid spec path + known section header; flip migration JSON M4 `done`. |

---

### Stage 6 — Infrastructure + Execution Surface / Web Dashboard: Parser + PlanTree Collapse

**Status:** Final

**Objectives:** Update web dashboard parser and PlanTree rendering to expect Stage→Task 2-level hierarchy. Remove Phase level from tree model and UI. Validate web build passes.

**Exit:**

- `web/lib/plan-loader-types.ts` — `PlanNode` / `StageNode` / `TaskNode` types reflect 2-level (no `PhaseNode`).
- `web/lib/plan-parser.ts` — parser reads Stage→Task; Phase grouping logic removed.
- `web/lib/plan-tree.ts` — `PlanTree` renders Stage/Task rows; Phase headers absent.
- `npm run validate:web` passes (lint + typecheck + build).
- Migration JSON M5 flipped to `done`.
- Phase 1 — Types + parser update.
- Phase 2 — PlanTree rendering update + validate.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | Update plan-loader-types + parser | **TECH-462** | Done (archived) | Edit `web/lib/plan-loader-types.ts`: remove `PhaseNode` type (or `phase` field in `StageNode`); update `StageNode` to hold `tasks: TaskNode[]` directly; edit `web/lib/plan-parser.ts`: remove Phase-level parsing block (the parser currently detects `#### Phase N` or `- [ ] Phase N` rows and groups tasks under phases); re-group tasks directly under their parent Stage; verify `web/lib/releases.ts` + `web/lib/plan-loader.ts` are updated if they reference Phase fields. |
| T6.2 | Update plan-tree + plan-loader | **TECH-463** | Done (archived) | Edit `web/lib/plan-tree.ts`: remove Phase header rendering row; render tasks directly under Stage; verify task Status column still renders `_pending_ / Draft / In Review / In Progress / Done (archived)` correctly after restructure; check `web/app/dashboard/**/page.tsx` usage of `PlanTree` for any Phase-specific props that need removal. |
| T6.3 | Web validate + type-check | **TECH-464** | Done (archived) | Run `cd web && npm run validate:web` (= lint + typecheck + build); fix any TypeScript errors from Phase-type removal; re-run until green. |
| T6.4 | Preview deploy + M5 flip | **TECH-465** | Done (archived) | Run `npm run deploy:web:preview`; open `/dashboard` on preview URL; visually confirm Stage→Task tree renders without Phase rows; confirm all migrated master plans (Step 2 output) display correctly; flip migration JSON M5 `done`. |

---

### Stage 7 — Infrastructure + Execution Surface / Skills + Agents + Commands Rewrite

**Status:** In Progress

**Objectives:** Author 12 new skill SKILL.md files — 4 Opus pair-heads (plan-review, stage-file-plan, opus-code-review, stage-closeout-plan) + 4 Sonnet pair-tails (plan-fix-apply, stage-file-apply, code-fix-apply, stage-closeout-apply) + 1 Sonnet project-new-apply (slim, no pair — `§Project-New Plan` dropped) + 1 Opus **Stage-scoped bulk `plan-author`** (non-pair; one pass per Stage writes all N `§Plan Author` sections + enforces canonical glossary terms across spec bodies — absorbs retired `spec-enrich` responsibility; resolves Open Q7 + S7) + 1 Opus **Stage-scoped bulk `opus-audit`** (non-pair; one pass per Stage writes all N `§Audit` paragraphs; feeds `stage-closeout-plan`) + 1 cross-cutting `subagent-progress-emit` (stderr phase-marker contract). Author 11 new `.claude/agents/*.md` files including `plan-author.md`, `stage-closeout-planner.md`, `stage-closeout-applier.md`. Update 7 `.claude/commands/*.md` (4 new + 3 repointed) — adds `/author` (Stage-scoped `{MASTER_PLAN_PATH} {STAGE_ID}` + `--task {ISSUE_ID}` escape hatch); `/audit` Stage-scoped `{MASTER_PLAN_PATH} {STAGE_ID}`; `/closeout` rewired to dispatch stage-closeout-plan → stage-closeout-apply (per-Stage bulk, not per-Task). Audit every lifecycle skill to add top-level `phases:` frontmatter. Retire `spec-kickoff` + Opus `closeout` agents + per-task `closeout-apply` skill (replaced by stage-closeout pair) + `project-stage-close` skill (folded into `stage-closeout-apply`) + `spec-enrich` skill (never authored; responsibility absorbed into bulk plan-author) + `spec-enricher` agent (never authored) + `/enrich` command (never authored) + `/kickoff` command (retired to `.claude/commands/_retired/`). Rewrite `ia/rules/agent-lifecycle.md` + `docs/agent-lifecycle.md` with Plan-Apply pair as a first-class hard rule plus Stage-scoped bulk `plan-author` + bulk `opus-audit` non-pair stages + `subagent-progress-emit` cross-cutting contract + stage-end bulk closeout replacing per-task closeout.

**Extension source:** `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` §Design Expansion — plan-author + progress-emit extension (2026-04-19); §Design Expansion — stage-end bulk closeout (2026-04-19 rev 2); §Design Expansion — stage-end bulk plan-author + audit + spec-enrich fold (2026-04-19 rev 3).

**Exit:**

- `ia/skills/plan-review/`, `ia/skills/plan-fix-apply/`, `ia/skills/stage-file-plan/`, `ia/skills/stage-file-apply/`, `ia/skills/plan-author/`, `ia/skills/project-new-apply/`, `ia/skills/opus-audit/`, `ia/skills/opus-code-review/`, `ia/skills/code-fix-apply/`, `ia/skills/stage-closeout-plan/`, `ia/skills/stage-closeout-apply/`, `ia/skills/subagent-progress-emit/` — all SKILL.md files present.
- `ia/skills/_retired/project-new-plan/` — retired skill archived with tombstone redirect to `plan-author`.
- `ia/skills/_retired/closeout-apply/` — retired per-task closeout-apply skill archived with tombstone redirect to `stage-closeout-apply` (bulk per-Stage replacement).
- `ia/skills/_retired/project-stage-close/` — retired stage-rollup skill archived with tombstone redirect to `stage-closeout-apply` (folded into bulk Stage closeout).
- `ia/skills/_retired/project-spec-kickoff/` — retired kickoff skill archived with tombstone redirect to `plan-author` (canonical-term enforcement absorbed into bulk plan-author; Sonnet mechanical enrichment pass eliminated).
- `ia/skills/spec-enrich/` — NOT authored (never exists); `spec-enrich` responsibility folded into bulk `plan-author` at author time.
- `.claude/agents/` — 11 new agent markdown files present (including `plan-author.md`, `stage-closeout-planner.md`, `stage-closeout-applier.md`; NOT including `spec-enricher.md` — never authored); `spec-kickoff.md` + Opus `closeout.md` moved to `.claude/agents/_retired/`.
- `.claude/commands/` — `/plan-review.md`, `/audit.md`, `/code-review.md`, `/author.md` new (4); `/stage-file.md` repointed → planner→applier pair + bulk plan-author chain (1); `/project-new.md` repointed → project-new-applier → plan-author (N=1) chain (1); `/closeout.md` repointed → stage-closeout-plan → stage-closeout-apply per-Stage bulk dispatcher (1); `/enrich.md` NOT authored; `/kickoff.md` retired → `.claude/commands/_retired/kickoff.md` (use `/author` instead); `/author` Stage-scoped `{MASTER_PLAN_PATH} {STAGE_ID}` + `--task {ISSUE_ID}` escape hatch; `/audit` Stage-scoped `{MASTER_PLAN_PATH} {STAGE_ID}`.
- `ia/templates/project-spec-template.md` revised: `§Plan Author` section added with 4 sub-sections (`§Audit Notes`, `§Examples`, `§Test Blueprint`, `§Acceptance`); `§Project-New Plan` section dropped (retired with skill); `§Closeout Plan` section dropped from per-task template (closeout plan now lives at Stage level in master-plan `§Stage Closeout Plan`).
- `ia/templates/master-plan-template.md` revised: `§Stage Closeout Plan` section stub added (one per Stage; holds unified closeout tuples — glossary rows, rule edits, doc edits, N BACKLOG archive ops, N id purges, N spec deletes, N status flips, N digests); Opus `stage-closeout-plan` writes; Sonnet `stage-closeout-apply` reads verbatim.
- `ia/rules/plan-apply-pair-contract.md` updated: 4 pair seams (plan-review→fix-apply, stage-file-plan→apply, code-review→code-fix-apply, stage-closeout-plan→stage-closeout-apply); `plan-author` explicitly noted as Opus **Stage-scoped bulk** non-pair stage (one pass per Stage, all N `§Plan Author` sections + canonical-term enforcement); `opus-audit` explicitly noted as Opus **Stage-scoped bulk** non-pair stage (one pass per Stage, all N `§Audit` paragraphs; feeds stage-closeout-plan).
- `ia/rules/agent-lifecycle.md`: ordered flow updated with `plan-author` per-task sequential stage; Plan-Apply pair hard rule added; surface map rows updated (12 new rows including `plan-author` + `subagent-progress-emit`).
- `docs/agent-lifecycle.md`: flow diagram + stage→surface matrix updated.
- Every lifecycle skill (new pair skills + `plan-author` + `spec-enrich` + existing `project-spec-implement`, `verify-loop`, `ship-stage`, `stage-file`, `project-new`): top-level `phases:` frontmatter array present + ordered; stderr progress-marker emission via `@`-loaded `subagent-progress-emit` preamble.
- `.claude/agents/*.md` common preamble `@`-loads `ia/skills/subagent-progress-emit/SKILL.md` — one line, zero per-agent boilerplate.
- Remaining lifecycle skills (project-spec-implement, verify-loop, ship-stage, stage-file, project-new): Phase references removed; Stage-level MCP bundle contract added.
- `npm run validate:all` passes.
- Migration JSON M6 flipped to `done`.
- Phase 1 — Plan-review + stage-file pair skills (head + tail each).
- Phase 2 — Remaining pair skills + audit-chain skills.
- Phase 3 — Plan-author skill + agent + `/author` command (resolves Open Q7; per-task Opus spec-body authoring).
- Phase 4 — Spec-enrich skill + retire old skills + slim `project-new-apply` + update remaining lifecycle skills.
- Phase 5 — Subagent-progress-emit skill + `phases:` frontmatter audit across every lifecycle skill + common-preamble `@`-load.
- Phase 6 — All new agents + retire old agents.
- Phase 7 — Commands repoint + `/author` new + lifecycle rule docs + validate.
- Phase 8 — Stage-closeout pair skills + agents + `/closeout` rewire + template edits + MCP rename + project-stage-close retire.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | Plan-review + plan-fix-apply skills | **TECH-468** | Draft | Author `ia/skills/plan-review/SKILL.md`: Opus pair-head; runs once per Stage before first Task kickoff; reads all Stage Task specs + master-plan Stage header + invariants; outputs `§Plan Fix` with `{operation, target_path, target_anchor, payload}` tuples; contract per `ia/rules/plan-apply-pair-contract.md`; PASS → downstream continue; fix-authored → spawn plan-fix-apply. Author `ia/skills/plan-fix-apply/SKILL.md`: Sonnet pair-tail; reads `§Plan Fix` tuples; applies edits to master-plan task-table rows + spec §Impl / §Tests sections literally; runs `validate:frontmatter`; on anchor ambiguity → escalate to Opus; on clean → report applied + return to plan-reviewer for re-check. |
| T7.2 | Stage-file pair skills | **TECH-469** | Draft | Author `ia/skills/stage-file-plan/SKILL.md`: Opus pair-head; split from current `stage-file` skill; authors `§Stage File Plan` section in master plan per Stage (structured list: one entry per Task with `{reserved_id, title, priority, notes, depends_on, related, stub_body}`); resolves all anchors before handing to applier; loads shared Stage-level MCP bundle once (glossary + router + invariants via `domain-context-load`). Author `ia/skills/stage-file-apply/SKILL.md`: Sonnet pair-tail; reads `§Stage File Plan` entries; for each entry: runs `reserve-id.sh`, writes `ia/backlog/{id}.yaml`, writes `ia/projects/{id}.md` stub; updates master-plan task table; runs `materialize-backlog.sh`; validates dead specs; escalates on id-counter lock failure. |
| T7.3 | Project-new-apply slim skill | **TECH-470** | Draft | Author `ia/skills/project-new-apply/SKILL.md`: Sonnet slim materialization; reads args directly from `/project-new` command (no §Project-New Plan read — retired); runs `reserve-id.sh` + writes `ia/backlog/{id}.yaml` + writes `ia/projects/{id}.md` stub + runs `materialize-backlog.sh` + `validate:dead-project-specs`; hands off to `plan-author` at N=1. Retire `ia/skills/project-new-plan/` → move to `ia/skills/_retired/project-new-plan/` with tombstone redirect header ("Retired — use plan-author"). Drop `§Project-New Plan` section from `ia/templates/project-spec-template.md` (replaced by `§Plan Author`). |
| T7.4 | Opus-audit (Stage-scoped bulk) + code-review + code-fix skills | **TECH-471** | Draft | Author `ia/skills/opus-audit/SKILL.md`: Opus **Stage-scoped bulk** stage invoked once per Stage after all Tasks reach post-verify Green (implement + verify-loop + opus-code-review + any code-fix loops complete); single pass reads ALL N Task specs (§Implementation + §Findings + §Verification) + Stage header + invariants + glossary snippets (pre-loaded in shared Stage MCP bundle); writes ALL N `§Audit` paragraphs in one synthesis round (one-paragraph per Task — what was built, what worked, what to watch); does NOT write `§Closeout Plan` (that is Stage-level work — see T7.13 `stage-closeout-plan`); N §Audit paragraphs feed directly into `stage-closeout-plan` as raw material. Phase 0 guardrail: assert every Task in Stage has non-empty §Findings (R11); escalate to user if any Task missing. `/audit` command = Stage-scoped dispatcher `{MASTER_PLAN_PATH} {STAGE_ID}` (not per-Task). Author `ia/skills/opus-code-review/SKILL.md`: Opus pair-head; reads diff vs spec + invariants + glossary; outcomes: (a) PASS → mini-report; (b) minor → suggest fix-in-place or deferred issue, no pair tail; (c) critical → writes `§Code Fix Plan`. (Kept per-Task — defect-detection latency coupling to /verify-loop; bulk code-review flagged as rev 4 candidate S8.) Author `ia/skills/code-fix-apply/SKILL.md`: Sonnet pair-tail; reads `§Code Fix Plan`; applies edits; re-enters `/verify-loop`; bounded 1 retry — escalates to Opus on second fail. (Per-task `closeout-apply` skill NOT authored — replaced by stage-level bulk pair in T7.13 / T7.14; `§Closeout Plan` template section dropped.) Extension source: exploration doc §Design Expansion — stage-end bulk plan-author + audit + spec-enrich fold (2026-04-19 rev 3). |
| T7.5 | Retire legacy kickoff + close skills (spec-enrich NOT authored — folded into bulk plan-author) | **TECH-472** | Draft | `ia/skills/spec-enrich/` NOT authored — canonical-term enforcement (§Objective / §Background / §Implementation Plan) absorbed into bulk `plan-author` (T7.11); Opus enforces at author time while shared Stage MCP bundle holds glossary snippets (R12); no mechanical Sonnet pass needed. Move `ia/skills/project-spec-kickoff/` → `ia/skills/_retired/project-spec-kickoff/` with tombstone redirect header ("Retired — use `plan-author` (Stage-scoped bulk); `/kickoff` command retired, use `/author`"). Move `ia/skills/project-spec-close/` → `ia/skills/_retired/project-spec-close/` with tombstone redirect header ("Retired — use `stage-closeout-apply` (Stage-scoped bulk pair-tail); `/closeout` command rewired Stage-scoped"). No new skill authoring in this task; retirement + tombstone only. Extension source: exploration doc §Design Expansion — stage-end bulk plan-author + audit + spec-enrich fold (2026-04-19 rev 3). |
| T7.6 | Update remaining lifecycle skills | **TECH-473** | Draft | Edit `ia/skills/project-spec-implement/SKILL.md`, `ia/skills/verify-loop/SKILL.md`, `ia/skills/ship-stage/SKILL.md`, `ia/skills/stage-file/SKILL.md`, `ia/skills/project-new/SKILL.md`: in each, remove all Phase-layer references (Phase bullets, Phase cardinality gate, Phase context reload); replace with Stage-level MCP bundle contract (shared `domain-context-load` result from Stage opener; Sonnet never re-queries glossary/router within a Stage); update lifecycle-stage enum references to match new pair-head + pair-tail names from Stage 3.1. |
| T7.7 | Author all new agents | **TECH-474** | Draft | Write `.claude/agents/plan-reviewer.md` (Opus), `plan-fix-applier.md` (Sonnet), `stage-file-planner.md` (Opus — rename of current stage-file agent), `stage-file-applier.md` (Sonnet), `project-new-planner.md` (Opus — rename of current project-new agent), `project-new-applier.md` (Sonnet), `opus-auditor.md` (Opus — Stage-scoped bulk invocation per T7.4), `opus-code-reviewer.md` (Opus), `code-fix-applier.md` (Sonnet), `closeout-applier.md` (Sonnet — replaces current Opus closeout agent). `spec-enricher.md` NOT authored (spec-enrich folded into bulk plan-author — see T7.5). Each agent body: caveman preamble + matching SKILL.md reference + model tier header; Opus agents list MCP tools needed; Sonnet agents list `plan-apply-pair-contract.md` as primary constraint. Move `spec-kickoff.md` + Opus `closeout.md` to `.claude/agents/_retired/` (create dir). |
| T7.8 | Update commands | **TECH-475** | Draft | Write 4 new `.claude/commands/`: `/plan-review.md` (dispatches plan-reviewer agent), `/audit.md` (Stage-scoped `{MASTER_PLAN_PATH} {STAGE_ID}` — dispatches opus-auditor bulk per T7.4), `/code-review.md` (dispatches opus-code-reviewer per-Task), `/author.md` (Stage-scoped `{MASTER_PLAN_PATH} {STAGE_ID}` — dispatches bulk plan-author per T7.11; `--task {ISSUE_ID}` escape hatch for single-spec re-author). `/enrich.md` NOT authored (spec-enrich folded into bulk plan-author — see T7.5). Repoint 3 existing: `/stage-file.md` → dispatches stage-file-planner then stage-file-applier pair then chains bulk `/author` Stage-scoped; `/project-new.md` → dispatches project-new-planner then project-new-applier then chains `/author --task {ISSUE_ID}` at N=1; `/closeout.md` → rewired Stage-scoped dispatcher (see T7.14). Retire 1: move `.claude/commands/kickoff.md` → `.claude/commands/_retired/kickoff.md` (legacy alias for pre-refactor Opus spec-kickoff + rev 2 spec-enricher repoint; both retired in rev 3; user invokes `/author` instead). Each command: caveman-asserting prompt forwarding args; cites SKILL.md. Extension source: exploration doc §Design Expansion — stage-end bulk plan-author + audit + spec-enrich fold (2026-04-19 rev 3). |
| T7.9 | Rewrite lifecycle rule docs | **TECH-476** | Draft | Rewrite `ia/rules/agent-lifecycle.md`: update ordered flow (add plan-review between stage-file and first enrich; add audit + code-review between verify-loop and closeout-apply; rename kickoff → enrich); add Plan-Apply pair as first-class hard rule section; update surface map table rows (add 11 new pair-head/tail rows; update model column for enricher → Sonnet; mark retired stages). Rewrite `docs/agent-lifecycle.md`: update flow diagram (Mermaid or ASCII) to match new chain; update stage→surface matrix with pair entries; update handoff contract table. |
| T7.10 | Validate + memory update + M6 flip | **TECH-477** | Draft | Run `npm run validate:all`; fix any failures from command/agent dispatch references to retired agents; update `AGENTS.md` lifecycle section to match new surface map; update project MEMORY entries in `MEMORY.md` that reference Phase or old kickoff/closeout dispatch pattern; flip migration JSON M6 `done`. |
| T7.11 | Plan-author skill + agent + /author command (Stage-scoped bulk; canonical-term fold) | **TECH-478** | Draft | Author `ia/skills/plan-author/SKILL.md`: Opus **Stage-scoped bulk** spec-body authoring stage (non-pair — no Sonnet tail); invoked once per Stage after `stage-file-apply` writes all N Task stubs (multi-task path) or once at N=1 after `project-new-apply` (single-task path); reads ALL N spec stubs + Stage header + shared Stage MCP bundle (from `domain-context-load`, loaded once by stage-file-plan) + invariants + pre-loaded glossary snippets in one bulk pass; writes ALL N `§Plan Author` sections in one Opus round, each with 4 sub-sections: `§Audit Notes` (upfront conceptual audit — risks, ambiguity, invariant touches), `§Examples` (concrete inputs/outputs + edge cases + legacy shapes), `§Test Blueprint` (structured `{test_name, inputs, expected, harness}` tuples consumed by `/implement`), `§Acceptance` (refined per-task acceptance criteria, narrower than Stage Exit). **Canonical-term fold (absorbs retired `spec-enrich`)**: same bulk pass enforces glossary canonical terms across `§Objective`, `§Background`, `§Implementation Plan` sections of every Task spec — no ad-hoc synonyms; Opus authors canonical terms at write time (vs. Sonnet post-hoc mechanical transform). Does NOT write code, run verify, or flip task status. Phase 0 guardrail: if total input tokens (N specs + Stage context) exceed Opus context threshold, split into ⌈N/2⌉ bulk sub-passes — never regress to per-Task mode (R10). Author `.claude/agents/plan-author.md`: Opus agent; caveman preamble; MCP tool allowlist (`router_for_task`, `glossary_discover`, `glossary_lookup`, `invariants_summary`, `spec_section`, `spec_sections`, `backlog_issue`, `master_plan_locate`); references `plan-author/SKILL.md`; top-level `phases:` frontmatter. Author `.claude/commands/author.md`: `/author {MASTER_PLAN_PATH} {STAGE_ID}` Stage-scoped dispatcher — default bulk invocation across all N Tasks of target Stage; `--task {ISSUE_ID}` escape hatch for single-spec re-author (on a previously filed spec); auto-invoked inside `/stage-file` post stage-file-apply + `/project-new` (N=1 path); forwards caveman-asserting prompt citing `plan-author/SKILL.md`. Revise `ia/templates/project-spec-template.md`: add `§Plan Author` section heading + 4 sub-section stubs in order (`§Audit Notes` → `§Examples` → `§Test Blueprint` → `§Acceptance`); position section after `§Verification` and before `§Audit`. Update `ia/rules/plan-apply-pair-contract.md`: note `plan-author` is an Opus **Stage-scoped bulk non-pair** stage (no Sonnet tail; N→1 invocations per Stage); 4 pair seams remain (plan-review→fix-apply, stage-file-plan→apply, code-review→code-fix-apply, stage-closeout-plan→stage-closeout-apply) — drop project-new-plan→project-new-apply seam entry; drop audit→closeout-apply seam entry (replaced by stage-closeout pair at Stage level in T7.13 / T7.14). Update `ia/rules/agent-lifecycle.md` ordered flow: insert `plan-author (Stage 1×N)` between `stage-file-apply` and `plan-review` (multi-task path); insert `plan-author (N=1)` between `project-new-apply` and `implement` (single-task path, skip plan-review at N=1); no `spec-enrich` stage in new flow. Resolves Open Q7 + S7 (exploration doc §Design Expansion — plan-author + progress-emit; rev 3 stage-end bulk fold). |
| T7.12 | Subagent-progress-emit skill + phases frontmatter audit | **TECH-479** | Draft | Author `ia/skills/subagent-progress-emit/SKILL.md`: cross-cutting progress-marker skill; defines stderr emission shape `⟦PROGRESS⟧ {skill_name} {phase_index}/{phase_total} — {phase_name}` (Unicode brackets `⟦ ⟧` reserved as regex-stable canonical delimiter; forbid in skill body prose); defines frontmatter convention — every lifecycle `SKILL.md` gains top-level `phases:` YAML array listing phase names in order (e.g. `phases: ["Phase 0 — Parse", "Phase 1 — Context load", "Phase 2 — Loop", "Phase 3 — Validate"]`); subagent body reads own frontmatter `phases:` + emits marker to stderr on phase entry; parent agent stderr surfaces marker line verbatim to user terminal (no log-file polling, no MCP round-trip); zero per-skill emission boilerplate — common preamble handles it; non-lifecycle one-shots (e.g. glossary patchers) exempt. Update every lifecycle skill to add top-level `phases:` frontmatter array: new pair skills (plan-review, plan-fix-apply, stage-file-plan, stage-file-apply, project-new-apply, opus-audit, opus-code-review, code-fix-apply, stage-closeout-plan, stage-closeout-apply), new non-pair (plan-author), plus existing rewritten skills (project-spec-implement, verify-loop, ship-stage, stage-file, project-new) — 15 skills total (spec-enrich NOT authored — folded into bulk plan-author per T7.5); each `phases:` array must list one entry per `### Phase N —` heading in skill body in order. Update `.claude/agents/*.md` common preamble to `@`-load `ia/skills/subagent-progress-emit/SKILL.md` — one line include; one edit per agent (11 new + common preamble template). Add `validate:frontmatter` rule to parse `phases:` array + assert one matching `### Phase N —` heading per entry in skill body (prevents drift between metadata + body); run validator on all 15 lifecycle skills; fix any mismatches. Update `ia/rules/agent-lifecycle.md` surface map: add `subagent-progress-emit` row (marked as cross-cutting). |
| T7.13 | Stage-closeout-plan skill + agent | **TECH-480** | Draft | Author `ia/skills/stage-closeout-plan/SKILL.md`: Opus pair-head invoked once per Stage when all Tasks reach `Done` post-verify (replaces per-task `closeout-apply`); reads master-plan Stage header + all Task `§Audit` paragraphs (written per-task by narrowed `opus-audit` in T7.4) + all Task §Implementation + §Findings + §Verification sections + invariants + glossary; writes `§Stage Closeout Plan` section in master plan (structured tuple list covering unified operations: shared glossary rows to add/edit, shared rule section edits, shared doc paragraph edits, plus N BACKLOG archive ops + N id purges + N spec deletes + N master-plan task-row status flips + N per-task digest emissions); resolves all anchors to exact line/heading/glossary-row-id before handing to applier; idempotent on re-run (reads current §Stage Closeout Plan, regenerates if stale). Author `.claude/agents/stage-closeout-planner.md`: Opus agent; caveman preamble + `@`-loads `stage-closeout-plan/SKILL.md`; MCP tool allowlist (`backlog_issue`, `spec_section`, `spec_sections`, `glossary_discover`, `glossary_lookup`, `invariants_summary`, `list_rules`, `rule_content`, `master_plan_locate`); `phases:` frontmatter. Update `ia/templates/master-plan-template.md` to include `§Stage Closeout Plan` section stub (one per Stage heading). Update `ia/rules/plan-apply-pair-contract.md` pair seams list to include stage-closeout-plan→stage-closeout-apply (Stage-scoped; fires once per Stage, not per Task). Extension source: exploration doc §Design Expansion — stage-end bulk closeout (2026-04-19 rev 2). |
| T7.14 | Stage-closeout-apply skill + agent + /closeout rewire + template drop + MCP rename + project-stage-close retire + M6 Phase 8 flip | **TECH-481** | Draft | Author `ia/skills/stage-closeout-apply/SKILL.md`: Sonnet pair-tail invoked once per Stage after `stage-closeout-plan`; reads `§Stage Closeout Plan` tuples from master plan; executes unified bulk: (a) applies shared glossary/rule/doc migration edits once; (b) loops N Tasks: `ia/backlog/{id}.yaml` → `ia/backlog-archive/{id}.yaml`, deletes `ia/projects/{id}.md`, flips master-plan task row Status → `Done (archived)`, purges id references across durable docs; (c) runs `materialize-backlog.sh` + `validate:dead-project-specs` once at end; (d) emits one Stage closeout digest (chain-level, not per-task); (e) flips Stage header `Status → Final` + rolls up to Step/Plan-level Final via R5 gate; escalates to Opus on anchor ambiguity or validator failure (bounded 1 retry on transient). Author `.claude/agents/stage-closeout-applier.md`: Sonnet agent; caveman preamble + `@`-loads `stage-closeout-apply/SKILL.md`; MCP tool allowlist (`backlog_issue`, `stage_closeout_digest` [renamed — see below], `materialize_backlog` equivalent); `phases:` frontmatter. Rewire `.claude/commands/closeout.md`: dispatches stage-closeout-planner then stage-closeout-applier (Stage-scoped, single pass per Stage — no per-Task closeout invocation path); accepts `{MASTER_PLAN_PATH} {STAGE_ID}` args; caveman-asserting prompt. Drop `§Closeout Plan` section from `ia/templates/project-spec-template.md` (template T7.3 update already drops `§Project-New Plan`; this drops `§Closeout Plan` — both section anchors live at Stage level now via `§Stage Closeout Plan` in master-plan template). Rename MCP tool `project_spec_closeout_digest` → `stage_closeout_digest` in `tools/mcp-ia-server/`: update handler to read per-task `§Audit` + Stage `§Stage Closeout Plan` + emit one Stage-level digest payload (not one per Task); restart schema cache; update all call sites in `stage-closeout-apply/SKILL.md` and any remaining skill that references the old tool name. Retire `ia/skills/project-stage-close/SKILL.md` → move to `ia/skills/_retired/project-stage-close/` with tombstone redirect header ("Retired — use stage-closeout-apply"; stage-rollup behavior folded into the Sonnet applier). Retire `ia/skills/closeout-apply/` (never authored; referenced in retired list for symmetry). Rewrite `ia/rules/agent-lifecycle.md`: drop per-Task `/closeout` row from surface map; drop `/kickoff` row (retired per T7.8); drop `/enrich` row (never authored per T7.5); add `/closeout` row as Stage-scoped dispatcher (stage-closeout-planner + stage-closeout-applier); add `/author` row (Stage-scoped bulk plan-author dispatcher per T7.11); add `/audit` row (Stage-scoped bulk opus-auditor dispatcher per T7.4); update ordered flow end-segment from legacy `verify-loop → opus-audit → opus-code-review → closeout (per-Task)` to rev 3 `stage-file-apply → plan-author (Stage 1×N) → plan-review → [per-Task loop: implement → verify-loop → opus-code-review (per-Task) → code-fix-apply?] → opus-audit (Stage, 1×N) → /closeout (Stage-scoped: stage-closeout-plan → stage-closeout-apply)`; single-task path: `project-new-apply → plan-author (N=1) → implement → verify-loop → opus-code-review → opus-audit (N=1) → /closeout (N=1)`; note stage-end batching semantics for plan-author + opus-audit + closeout (N→1 Opus bulk calls per Stage; no `spec-enrich` stage anywhere). Run `npm run validate:all`; flip migration JSON M6 Phase 8 `done`. Extension source: exploration doc §Design Expansion — stage-end bulk closeout (2026-04-19 rev 2) + stage-end bulk plan-author + audit + spec-enrich fold (2026-04-19 rev 3). |

---

### Stage 8 — Validation + Merge / Dry-Run + Full Validation

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Execute one Task end-to-end through the new chain to catch integration issues before merge. Regenerate all view files. Run full local verification chain.

**Exit:**

- Dry-run Task identified (small, non-critical pending task from any open master plan); chain executed without error.
- `BACKLOG.md` + `BACKLOG-ARCHIVE.md` + `docs/progress.html` regenerated and consistent.
- `npm run verify:local` green.
- Migration JSON M7 flipped to `done`.
- Phase 1 — Dry-run new chain + regen views.
- Phase 2 — Full verify:local + fix + M7 flip.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | Dry-run new chain end-to-end | _pending_ | _pending_ | Select a small pending Task from any open master plan (prefer a Task in _pending_ state, not one currently In Progress); run the new chain: `/plan-review` on its Stage → `/enrich` → `/implement` (no actual code ship; stop after plan-review + enrich to validate dispatch wiring) → simulate audit + code-review outputs → verify closeout-apply reads `§Closeout Plan` stub correctly; document each pair's handoff in migration JSON M7.dry-run section; no commit of dry-run artifacts. |
| T8.2 | Regen BACKLOG + progress.html | _pending_ | _pending_ | Run `bash tools/scripts/materialize-backlog.sh` → verify `BACKLOG.md` + `BACKLOG-ARCHIVE.md` consistent with yaml state post-M3; run `npm run progress` → verify `docs/progress.html` renders Stage/Task 2-level tree correctly (no Phase rows). |
| T8.3 | Full verify:local | _pending_ | _pending_ | Run `npm run verify:local` (validate:all + unity:compile-check + db:bridge-preflight); triage any failures by subsystem: web failures → Stage 3.2 patch; MCP failures → Stage 3.1 patch; skill/agent failures → Stage 3.3 patch; yaml failures → Stage 2.2 patch. |
| T8.4 | Fix remaining failures + M7 flip | _pending_ | _pending_ | Apply minimal targeted fixes for any failures from T4.1.3; re-run `npm run verify:local` until green; flip migration JSON M7 `done`. |

---

### Stage 9 — Validation + Merge / Sign-Off + Merge

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Present dry-run artifacts to user. Collect sign-off. Merge branch, restart MCP, and close freeze window. File token-cost telemetry follow-up.

**Exit:**

- Migration JSON M8 gate entry written with user sign-off timestamp.
- `feature/lifecycle-collapse-cognitive-split` merged to main.
- `territory-ia` MCP server restarted post-merge; new schema verified.
- Freeze note removed from `CLAUDE.md`.
- Token-cost telemetry follow-up TECH issue filed in `ia/backlog/`.
- Migration JSON M8 flipped to `done`.
- Phase 1 — User gate + MCP post-merge restart.
- Phase 2 — Merge + freeze-close + follow-up issue.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | User sign-off gate | _pending_ | _pending_ | Present dry-run artifacts (migration JSON M7.dry-run, `BACKLOG.md` diff, `docs/progress.html` screenshot) to user; wait for explicit sign-off ("LGTM" / "merge"); record sign-off + timestamp in migration JSON M8.gate; do not proceed to T4.2.3 without gate. |
| T9.2 | MCP restart + schema verify | _pending_ | _pending_ | Kill and respawn `territory-ia` MCP process on the post-merge main branch; send a test `router_for_task` call with `plan_review` stage name; confirm enum accepted; confirm `plan_apply_validate` tool responds; record restart success in migration JSON. |
| T9.3 | Merge branch | _pending_ | _pending_ | Merge `feature/lifecycle-collapse-cognitive-split` into main (standard merge commit, no squash — preserve migration history); resolve any conflicts in `BACKLOG.md` / `BACKLOG-ARCHIVE.md` from concurrent activity during freeze window by re-running `materialize-backlog.sh` post-merge; flip migration JSON M8 `done`. |
| T9.4 | Freeze close + token-cost issue + Q9 baseline instrumentation | _pending_ | _pending_ | Remove freeze note from `CLAUDE.md` §Key commands; file a token-cost telemetry tracker TECH issue in `ia/backlog/` (title: "Token-cost telemetry baseline — pre/post lifecycle refactor + Q9 pair-head read-count"; priority: Low). Issue MUST require per-Stage instrumentation that captures (a) total prompt tokens per Stage, (b) **pair-head read count per Stage** (distinct from total tokens — each cache-hit read counted separately; precondition for Stage 10 P1 savings validation per `docs/prompt-caching-mechanics.md` §4 R5), (c) cache-write / cache-read / cache-miss token counts from `usage.cache_creation_input_tokens` + `usage.cache_read_input_tokens`, (d) per-Stage bundle byte + token size (validates F2 sizing gate per rev 4 C1/R2). Data feeds Stage 10 T10.1 precondition gate. Run final `npm run validate:all` on main post-merge to confirm clean state. |

---

### Stage 10 — Prompt-Caching Optimization Layer (Post-Merge, Q9-Gated)

**Status:** Draft (tasks _pending_ — not yet filed; activation gated by Q9 baseline from Stage 9 T9.4)

**Objectives:** Land the rev 4 prompt-caching optimization layer on top of the merged refactor. Add Tier 1 stable cross-Stage cache block + Tier 2 per-Stage ephemeral bundle. Enforce F2 sizing gate at CI time (R2). Stagger bulk Opus fan-out per F3. Retire R11 §Findings gate (B2) + unify pair-tail Sonnet appliers (B4). Wire SSE cache-commit event gate (R1). Land invalidation-cascade + 20-block guardrail notes (D2/D3). Validate P1 savings band (−30% to −57% per Stage at ≥3 pair-head reads) against instrumented Q9 baseline before sign-off.

**Extension source:** `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` §Design Expansion — rev 4 candidates + cache-mechanics amendments (2026-04-19 rev 4). Reference doc: `docs/prompt-caching-mechanics.md`.

**Precondition gate:** Stage 9 T9.4 Q9 baseline data must record pair-head read count per Stage for ≥ 3 distinct post-merge Stages (any open master plan) before Stage 10 T10.1 may open. If measured read count < 3/Stage on all sampled Stages → Stage 10 REJECTED (P1 economics not viable); record rejection in migration JSON M9 stub + close Stage 10 as `Status: Rejected (Q9 baseline < 3 reads/Stage)`.

**Exit:**

- `docs/prompt-caching-mechanics.md` — reference doc landed (authored ahead of Stage 10 activation per D1 tier).
- Tier 1 stable cross-Stage cache block implemented: `@`-concatenated rules preamble emitted as single `messages` content block with `cache_control: {"type":"ephemeral","ttl":"1h"}`; inherited by all 4 pair seams + `plan-author` + `opus-audit` within one Stage (per rev 4 A1 + A4).
- Tier 2 per-Stage ephemeral bundle implemented: `ia/skills/domain-context-load/SKILL.md` Phase N concatenates MCP aggregator output (glossary subset + spec_sections + invariants_summary) into a single content block with `cache_control: {"type":"ephemeral","ttl":"1h"}` (per rev 4 A3 + R3 + R4).
- F2 sizing gate CI check landed: `tools/scripts/validate-cache-block-sizing.ts` asserts each emitted cache block ≥ F2 floor (4,096 tok Opus 4.7 / 1,024 tok Sonnet 4.6); CI fails on silent no-cache (per rev 4 C1 + R2).
- F3 bulk-dispatcher stagger fix: `plan-author` + `opus-audit` Stage-scoped bulk invocations staggered sequentially (no concurrent identical-prompt fan-out); documented in skill Phase 0 guardrail (per rev 4 A2 + amendment 2).
- R11 §Findings gate retired (rev 4 B2): `plan-author` Phase N writes §Findings inline per Task; `opus-audit` Phase 0 drops the "assert every Task has non-empty §Findings" gate and reads §Findings from plan-author output directly. Commit ordering: B2 lands BEFORE any opus-audit refactor (amendment 3).
- Unified pair-applier (rev 4 B4): `ia/skills/plan-fix-apply/`, `ia/skills/code-fix-apply/`, `ia/skills/stage-closeout-apply/` consolidated into single `ia/skills/plan-applier/SKILL.md` Sonnet skill reading any `§*Fix Plan` / `§Stage Closeout Plan` tuple shape; per-pair applier skills retired with tombstones. Resolves legacy Open Q11.
- SSE cache-commit event gate (rev 4 C3/R1): subagent progress-emit reads `message_start.usage.cache_creation_input_tokens` as commit signal; `content_block_delta` safe fallback; no ms-latency heuristic. Filed Q17 upstream with Anthropic.
- F5 tool-allowlist uniformity (amendment 4): all pair-seam agents (`.claude/agents/plan-reviewer.md`, `plan-applier.md`, `stage-file-planner.md`, `stage-file-applier.md`, `opus-code-reviewer.md`, `stage-closeout-planner.md`) share identical `tools:` frontmatter; validator asserts uniformity.
- F6 invalidation cascade + 20-block guardrail notes landed: `docs/mcp-ia-server.md` §Cache impact note added (D2); skill-author guide `ia/rules/subagent-progress-emit.md` adds D3 single-block rule ("NEVER emit multi-block stable prefix").
- P1 savings band validated: Q9 baseline replay under Tier 1 + Tier 2 cache enabled shows actual savings within ±5% of R5 predicted band at measured read count; if delta > ±5% → investigate + patch before sign-off.
- `npm run validate:all` + `npm run verify:local` green post-Stage 10.
- Migration JSON M9 entry written (new phase; not in M0–M8 core refactor) with Stage 10 sign-off timestamp.
- Phase 1 — Reference doc landed + Q9 baseline gate check.
- Phase 2 — Tier 1 stable block + F2 sizing gate CI.
- Phase 3 — Tier 2 per-Stage bundle + domain-context-load Phase N concat.
- Phase 4 — F3 stagger + F5 uniformity + B2 retire R11.
- Phase 5 — B4 unified plan-applier consolidation.
- Phase 6 — R1 SSE commit gate + C4 progress-emit extension.
- Phase 7 — D2/D3 docs + 20-block guardrail.
- Phase 8 — P1 validation replay + sign-off + M9 flip.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | D1 reference doc + Q9 gate check | _pending_ | _pending_ | Verify `docs/prompt-caching-mechanics.md` present (authored 2026-04-19 ahead of Stage 10 activation); read Stage 9 T9.4 Q9 telemetry output; compute per-Stage pair-head read count mean + median across ≥ 3 sampled post-merge Stages; if median ≥ 3 reads/Stage → open Stage 10 Phase 2 gate; if < 3 reads/Stage → close Stage 10 as Rejected + record rationale in migration JSON M9.reject + file follow-up TECH issue noting cache economics not viable at observed read volume. |
| T10.2 | Tier 1 stable cross-Stage block + F2 sizing gate CI | _pending_ | _pending_ | Implement Tier 1 stable block: author `ia/skills/_preamble/stable-block.md` (rules `@`-concat target — invariants + terminology-consistency + mcp-ia-default + agent-output-caveman + agent-lifecycle + project-hierarchy + orchestrator-vs-spec + glossary preamble); emit as single `messages` content block with `cache_control: {"type":"ephemeral","ttl":"1h"}` in each pair-seam agent body + `plan-author` + `opus-audit` agent bodies (A1+A4). Author `tools/scripts/validate-cache-block-sizing.ts` CI validator: parses agent bodies for `cache_control` declarations; estimates block token count (bytes × 0.25); fails if count < F2 floor (4,096 Opus 4.7 / 1,024 Sonnet 4.6); wired into `npm run validate:all` chain via `package.json`. Document in `docs/prompt-caching-mechanics.md` §5. |
| T10.3 | Tier 2 per-Stage bundle + domain-context-load Phase N concat | _pending_ | _pending_ | Edit `ia/skills/domain-context-load/SKILL.md`: add Phase N (final concatenation phase) that assembles glossary subset + spec_sections + invariants_summary from MCP aggregator output into single content block; emit with `cache_control: {"type":"ephemeral","ttl":"1h"}`; Phase N asserts token estimate ≥ F2 floor before emit (runtime safety net complementing T10.2 CI gate); document in skill §Overview. Update `stage-file-plan` skill to invoke `domain-context-load` exactly once per Stage (shared Tier 2 bundle reused across all Tasks). Update `ia/rules/plan-apply-pair-contract.md` to cite Tier 2 bundle reuse contract. Run `validate:all`; verify validator accepts new Phase N. |
| T10.4 | F3 stagger + F5 tool-allowlist uniformity + B2 retire R11 | _pending_ | _pending_ | Edit `ia/skills/plan-author/SKILL.md` Phase 0 guardrail: sequential dispatch (no concurrent Opus calls for Stage-scoped bulk N→1 invocation; F3 guardrail per rev 4 A2 + amendment 2). Same edit in `ia/skills/opus-audit/SKILL.md` Phase 0. Audit all pair-seam agent bodies (`.claude/agents/plan-reviewer.md`, `plan-applier.md` [or legacy per-pair appliers pending T10.5], `stage-file-planner.md`, `stage-file-applier.md`, `opus-code-reviewer.md`, `stage-closeout-planner.md`): enforce identical `tools:` frontmatter (F5 uniformity per amendment 4); author `tools/scripts/validate-agent-tools-uniformity.ts` validator wired to `validate:all`. **B2 retire R11:** edit `ia/skills/plan-author/SKILL.md` Phase N output contract to include per-Task `§Findings` sub-section alongside `§Plan Author` 4-part output; edit `ia/skills/opus-audit/SKILL.md` Phase 0 to drop the "assert every Task has non-empty §Findings" gate (rev 3 R11) and read §Findings from plan-author output directly. **Commit ordering:** B2 plan-author edit lands in the same commit as opus-audit Phase 0 drop (never partial — prevents mid-flight ordering breakage per amendment 3). |
| T10.5 | B4 unified plan-applier consolidation | _pending_ | _pending_ | Author `ia/skills/plan-applier/SKILL.md`: Sonnet literal-applier reading any `§*Fix Plan` or `§Stage Closeout Plan` tuple shape (`{operation, target_path, target_anchor, payload}`); dispatches per operation type (fs edit, glossary row, BACKLOG archive, id purge, spec delete, status flip, digest emit); escalates to Opus on anchor ambiguity; bounded 1 retry on transient; resolves legacy Open Q11. Retire `ia/skills/plan-fix-apply/` + `ia/skills/code-fix-apply/` + `ia/skills/stage-closeout-apply/` → move each to `ia/skills/_retired/{name}/` with tombstone redirect header ("Retired — use `plan-applier` (unified)"). Retire corresponding agents `.claude/agents/plan-fix-applier.md` + `code-fix-applier.md` + `stage-closeout-applier.md` → move to `.claude/agents/_retired/`. Author `.claude/agents/plan-applier.md` (Sonnet; caveman preamble; tools uniformity per T10.4). Update all pair-head skills + commands (`/plan-review`, `/code-review`, `/closeout`) to dispatch `plan-applier` instead of legacy per-pair applier. Update `ia/rules/plan-apply-pair-contract.md` to reflect unified applier. |
| T10.6 | R1 SSE cache-commit event gate + C4 progress-emit extension | _pending_ | _pending_ | Edit `ia/skills/subagent-progress-emit/SKILL.md`: add §SSE cache-commit gate section documenting `message_start.usage.cache_creation_input_tokens` as conservative commit signal + `content_block_delta` safe fallback (R1); forbid ms-latency heuristics in skill bodies. Extend `⟦PROGRESS⟧` marker shape with optional `cache:{written|hit|miss|n/a} tokens:{N}` suffix when `usage` data available (rev 4 C4 fold — zero regression at default). Document Q17 upstream-pending note in skill §Caveats. Update all 15 lifecycle skills' `phases:` frontmatter to optionally consume SSE usage data (backwards compatible — no change required for skills that don't surface cache telemetry). |
| T10.7 | D2 cascade note + D3 20-block guardrail note | _pending_ | _pending_ | Edit `docs/mcp-ia-server.md`: add §Cache invalidation impact section — any `tools/mcp-ia-server/` edit cascades down to cached Stage bundles per F5; PR author must flag tool-registration edits in PR description + expect Stage-boundary re-warm. Edit `ia/skills/subagent-progress-emit/SKILL.md` (or new `ia/rules/subagent-caching-guardrails.md` — author's call): add D3 single-block rule — NEVER emit multi-block stable prefix; `@`-concatenation at skill-preamble author time is the ONLY supported assembly mode; multi-`@`-load with separate `cache_control` per block is forbidden (risks falling outside F6 20-block lookback as conversation grows). Cross-link both notes from `docs/prompt-caching-mechanics.md` §6 + §7. |
| T10.8 | P1 validation replay + sign-off + M9 flip | _pending_ | _pending_ | Select ≥ 3 post-merge Stages measured in Stage 9 T9.4 Q9 baseline; replay each under Tier 1 + Tier 2 cache enabled; capture actual cache-hit-rate + write-count + read-count + token-delta per Stage; compute actual savings % vs R5 predicted band (−10% at 2 reads / +23% at 3 reads / +50% at 5 reads / +57% at 6 reads); assert actual within ±5% of predicted band at measured read count; if delta > ±5% → investigate + patch (likely F2 sizing gate regression or F5 cascade not honored) + re-replay. Present validation report to user; wait for explicit sign-off ("LGTM" / "ship cache layer"); flip migration JSON M9 `done` + stamp sign-off timestamp. Run final `npm run validate:all` + `npm run verify:local` on main post-Stage-10 to confirm clean state. |

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
- **Tooling-only verify fast-path (M0–M10):** For refactor task closeouts in Stages 5, 6, 7, 9, 10 (MCP TypeScript / web Next.js / skills + agents + commands markdown / docs / scripts — zero Unity runtime C# touch), use `npm run validate:all` directly OR dispatch `/verify-loop --tooling-only` (skips Steps 0, 1, 3, 4a, 4b, 5, 6 of the decision matrix; runs Step 2 + Step 7 only). Full `/verify-loop` (compile gate + Path A / Path B + bridge) reserved for Stage 8 T8.3 where `npm run verify:local` is the explicit acceptance gate. Plan header (line 5) guarantees `Tooling surface only — zero Unity runtime C# touch`; enforce at task-close time. Skill-level flag definition: `ia/skills/verify-loop/SKILL.md` §Inputs + §Pre-matrix mode gate.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only Stage 4.2 Final triggers `Status: Final`; the file stays.
- Skip the user gate at Stage 4.2 T4.2.1 — merge requires explicit human sign-off.
- Run new `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, or `/stage-file` calls outside this orchestrator during the freeze window (M0–M8). The freeze is declared in `CLAUDE.md` by T1.1.1.
- Merge partial stage state — every Stage must reach green-bar before `project-stage-close` runs.
- Insert BACKLOG rows directly into this doc — only `stage-file-apply` materializes them.
- Use `migrate-master-plans.ts` on any spec file other than orchestrator master plans — project specs (`ia/projects/{ISSUE_ID}.md`) are handled by T2.2.1 (separate targeted edit, not the batch transform script).
- Open Stage 10 before M8 sign-off + Q9 baseline landed. Stage 10 is a post-merge optimization layer; activation is gated by T10.1 precondition check (≥ 3 pair-head reads/Stage median across ≥ 3 sampled post-merge Stages). Pre-Stage-10 work limited to reference doc + candidate-pool persistence (already landed 2026-04-19) — never runtime cache wiring during freeze window.
