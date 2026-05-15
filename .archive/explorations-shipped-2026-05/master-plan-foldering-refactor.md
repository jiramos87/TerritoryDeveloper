---
slug: master-plan-foldering-refactor
target_version: 2
parent_plan_id: 1
notes: |
  v2 reshape (2026-05-10). Original v1 plan (5 stages, 25 tasks) had stages 1.0+2.0+4.0 silently shipped via sibling plans (db-lifecycle-extensions, async-cron-jobs, ship-protocol, ship-cycle-db-read-efficiency, parallel-carcass-rollout). v1 task rows + stage rows hard-deleted (15 tasks archived; 3 stages cascade-deleted via stage_delete MCP) — full audit trail preserved in ia_master_plan_change_log entries 6212–6218.
  v2 active scope = 3 sequential stages: (1) reconcile spec to landed state, (2) retire stale review/applier skills, (3) cutover ia/projects+ia/backlog file residue.
  All decisions D1–D12 from v1-reshape preamble already locked. No pending decisions reach ship-plan.
stages:
  - id: "1.0"
    title: "Reconcile spec to landed state"
    exit: "v2 spec reconciled. Tool-name remap table refreshed (planned→live). D1–D12 marked shipped. master_plan_health green. arch_changelog row written."
    red_stage_proof_block:
      red_test_anchor: design-only:docs/master-plan-foldering-refactor-design.md::reconcile
      target_kind: design_only
      proof_artifact_id: docs/master-plan-foldering-refactor-design.md
      proof_status: not_applicable
    red_stage_proof: |
      Stage is doc-only reconciliation. No runtime test. Proof = master_plan_health(slug) returns green AND ia_master_plan_change_log carries 'reconcile-note' kind row referencing entries 6212-6218.
    tasks:
      - id: 1.0.1
        title: "Audit landed surface vs original D1–D12; emit reconcile note"
        prefix: TECH
        depends_on: []
        kind: doc-only
        digest_outline: |
          Walk D1–D12 from v1-reshape preamble. For each decision, emit one-line status: shipped-via-{sibling-slug} | dropped | superseded. Compose reconcile-note body. Append via cron_arch_changelog_append_enqueue (kind=reconcile-note).
        touched_paths:
          - docs/master-plan-foldering-refactor-design.md
      - id: 1.0.2
        title: "Refresh tool-name remap table (planned→live)"
        prefix: TECH
        depends_on: [1.0.1]
        kind: doc-only
        digest_outline: |
          Update v1-reshape preamble tool-name remap section. Verify each live tool (master_plan_render, master_plan_state, master_plan_health, stage_state, stage_bundle, stage_render, cron_journal_append_enqueue, project_spec_journal_get, task_spec_search, cron_arch_changelog_append_enqueue, task_spec_section_write, cron_stage_verification_flip_enqueue) registered via list_specs / list_rules MCP. Append remap-refresh row via cron_arch_changelog_append_enqueue.
        touched_paths:
          - docs/master-plan-foldering-refactor-design.md
      - id: 1.0.3
        title: "Freeze v2 baseline preamble + master_plan_description_write"
        prefix: TECH
        depends_on: [1.0.2]
        kind: mcp-only
        digest_outline: |
          Compose v2-baseline preamble referencing reconciled D1–D12 + refreshed remap table. Write via master_plan_description_write + master_plan_preamble_write MCP. master_plan_health(slug) must return green; capture rollup for closeout digest.
        touched_paths: []
  - id: "2.0"
    title: "Skill retirement — drop plan-review* + plan-applier + opus-code-reviewer"
    exit: "4 skill folders moved to ia/skills/_retired/. Generated .claude/agents+commands counterparts in _retired/. Tier 1 cache block re-validated. validate:skill-drift + validate:cache-block-sizing green."
    red_stage_proof_block:
      red_test_anchor: design-only:tools/scripts/validate-skill-drift.mjs::run
      target_kind: design_only
      proof_artifact_id: tools/scripts/validate-skill-drift.mjs
      proof_status: not_applicable
    red_stage_proof: |
      Stage is mechanical retirement — file moves + validator runs. Proof = validate:skill-drift exits 0 AND grep for 'plan-review-mechanical|plan-review-semantic|plan-applier|opus-code-reviewer' across ia/skills/ (excluding _retired/) returns zero hits.
    tasks:
      - id: 2.0.1
        title: "Move 4 skill folders to ia/skills/_retired/"
        prefix: TECH
        depends_on: []
        kind: code
        digest_outline: |
          git mv ia/skills/{plan-review-mechanical,plan-review-semantic,plan-applier,opus-code-review} ia/skills/_retired/. Each folder's SKILL.md + agent-body.md + command-body.md preserved verbatim under _retired/.
        touched_paths:
          - ia/skills/plan-review-mechanical/
          - ia/skills/plan-review-semantic/
          - ia/skills/plan-applier/
          - ia/skills/opus-code-review/
      - id: 2.0.2
        title: "Move generated .claude counterparts to _retired/"
        prefix: TECH
        depends_on: [2.0.1]
        kind: code
        digest_outline: |
          git mv .claude/agents/{plan-reviewer-mechanical,plan-reviewer-semantic,plan-applier,opus-code-reviewer}.md → .claude/agents/_retired/. Same for .claude/commands/{plan-review,plan-applier,code-review}.md → _retired/. Preserve filenames.
        touched_paths:
          - .claude/agents/
          - .claude/commands/
      - id: 2.0.3
        title: "Update Tier 1 cache block + re-validate sizing"
        prefix: TECH
        depends_on: [2.0.2]
        kind: code
        digest_outline: |
          Edit ia/skills/_preamble/stable-block.md — drop references to retired skills. Run npm run validate:cache-block-sizing — must exit 0. Rationale: cache floor invariants change when skill list shrinks.
        touched_paths:
          - ia/skills/_preamble/stable-block.md
      - id: 2.0.4
        title: "Run skill:sync:all + validate:skill-drift green gate"
        prefix: TECH
        depends_on: [2.0.3]
        kind: code
        digest_outline: |
          npm run skill:sync:all — regenerates .claude/agents+commands from remaining SKILL.md frontmatter. npm run validate:skill-drift — must exit 0 (no orphan generated files; no stale references). Capture stdout/stderr in journal entry.
        touched_paths: []
      - id: 2.0.5
        title: "Update agent-lifecycle.md retired-seam tombstones"
        prefix: TECH
        depends_on: [2.0.4]
        kind: doc-only
        digest_outline: |
          Edit docs/agent-lifecycle.md §1 + §2: confirm tombstone lines for plan-review-mechanical / plan-review-semantic / plan-applier / opus-code-review match retired skill list. Verify §2a R-rules unaffected (all flips owned by ship-plan / ship-cycle inline).
        touched_paths:
          - docs/agent-lifecycle.md
  - id: "3.0"
    title: "Cutover — drop ia/projects + ia/backlog file residue + freeze yaml writers"
    exit: "ia/projects/*.md + ia/backlog/*.yaml moved to _retired/. Zero yaml-write paths in ia/skills/ + tools/scripts/ outside _retired/. validate:all green. Plan closeout digest in change_log."
    red_stage_proof_block:
      red_test_anchor: design-only:tools/scripts/validate-all.mjs::run
      target_kind: design_only
      proof_artifact_id: tools/scripts/validate-all.mjs
      proof_status: not_applicable
    red_stage_proof: |
      Stage is mechanical cutover — file moves + validator gate. Proof = grep -rn 'ia/projects/\|ia/backlog/' ia/skills/ tools/scripts/ (excluding _retired/) returns zero non-comment hits AND validate:all exits 0.
    tasks:
      - id: 3.0.1
        title: "Audit grep — enumerate residual read/write paths"
        prefix: TECH
        depends_on: []
        kind: doc-only
        digest_outline: |
          grep -rn 'ia/projects/\|ia/backlog/' ia/skills/ tools/scripts/ — emit table of {file, line, kind: read|write} hits. Filter out _retired/. Output saved to journal entry as audit_report payload.
        touched_paths: []
      - id: 3.0.2
        title: "Patch residual yaml-write callsites — replace with MCP mutate"
        prefix: TECH
        depends_on: [3.0.1]
        kind: code
        digest_outline: |
          For each write-kind hit from 3.0.1: replace yaml/markdown direct write with task_spec_section_write / task_status_flip / cron_*_enqueue MCP call. No fs.writeFileSync to ia/projects/ or ia/backlog/.
        touched_paths:
          - ia/skills/
          - tools/scripts/
      - id: 3.0.3
        title: "Move ia/projects/*.md + ia/backlog/*.yaml to _retired/"
        prefix: TECH
        depends_on: [3.0.2]
        kind: code
        digest_outline: |
          git mv ia/projects/*.md ia/projects/_retired/. git mv ia/backlog/*.yaml ia/backlog/_retired/. Preserve filenames. Single commit. Update .gitignore if needed.
        touched_paths:
          - ia/projects/
          - ia/backlog/
      - id: 3.0.4
        title: "Run validate:all + verify:local green gate"
        prefix: TECH
        depends_on: [3.0.3]
        kind: code
        digest_outline: |
          npm run validate:all — must exit 0. npm run verify:local — must exit 0 (full chain: validate:all + compile-check + db:migrate + bridge-preflight + Editor save/quit + playmode-smoke). Capture verdict + exit codes in journal.
        touched_paths: []
      - id: 3.0.5
        title: "Plan closeout digest + status flip"
        prefix: TECH
        depends_on: [3.0.4]
        kind: mcp-only
        digest_outline: |
          Compose closeout digest summarising v2 reshape outcome (3 stages done, 13 tasks done, original 25-task v1 retired). Append via cron_arch_changelog_append_enqueue (kind=plan-closeout). /ship-final master-plan-foldering-refactor flips ia_master_plans.closed_at + git tag master-plan-foldering-refactor-v2.
        touched_paths: []
---

# Master-plan foldering refactor — v2 reshape exploration

Caveman-tech default per `ia/rules/agent-output-caveman.md`.

---

## 1. Context

Original v1 plan (`master-plan-foldering-refactor`, 2026-05-04) carried 5 stages × 5 tasks = 25 tasks for the IA-state DB cutover. Six months of sibling-plan work silently shipped:

- Stages 1.0 + 2.0 + 4.0 of v1 → covered by `db-lifecycle-extensions`, `async-cron-jobs`, `ship-protocol`, `ship-cycle-db-read-efficiency`, `parallel-carcass-rollout`.
- DB schema (`ia_master_plans`, `ia_stages`, `ia_tasks`, `ia_task_specs`, `ia_master_plan_change_log`, `cron_*_jobs`) all live (mig 0015 → 0131).
- MCP tool surface live: `master_plan_*`, `stage_*`, `task_*`, `cron_*_enqueue`, `fix_plan_*`, `arch_*`.
- Web dashboard live at `web/app/plans/[slug]/`.

Net-new work remaining → 3 stages: spec reconciliation, skill retirement, file residue cutover.

---

## 2. v1 → v2 reshape audit trail

| change_log entry | kind | scope |
|---|---|---|
| 6212 | stage-delete | v1 stage 2.0 (MCP tool surface) — superseded |
| 6213 | stage-delete | v1 stage 4.0 (web dashboard) — superseded |
| 6216 | stage-delete | v1 stage 1.0 (DB schema + import + first read tool) — sibling-shipped |
| 6217 | stage-delete | v1 stage 3.0 (skill flips merge+drop) — partially shipped, residue → v2 stage 2.0 |
| 6218 | stage-delete | v1 stage 5.0 (cutover) — residue → v2 stage 3.0 |

15 archived tasks cascade-deleted in same audit window. v2 reshape preamble in `ia_master_plans.description` references all decision rows (D1–D12).

---

## 3. v2 stage shape

3 sequential stages, no parallelism:

```
Stage 1.0 (reconcile)  →  Stage 2.0 (skill retire)  →  Stage 3.0 (cutover)
   doc-only + MCP        file moves + validator     file moves + verify:local + close
```

Each stage = 3–5 mechanical tasks. No tracer-slice / visibility-delta — every stage `target_kind: design_only` because runtime surface already shipped via siblings; v2 work is purely organizational + validation.

---

## 4. Locked decisions (D1–D12 from v1-reshape preamble)

All resolved at reshape time (2026-05-10). Quoted verbatim from preamble — no re-grilling at ship-plan time:

- D1: drop mcp-client wrapper — `web/lib/plan-loader.ts` covers read surface.
- D2: per-stage drill at `/plans/[slug]/sections/[id]` already landed.
- D3: route `/plans/` (not `/projects/`).
- D4: async journal canonical (`cron_journal_append_enqueue`).
- D5: hand-authored carcass folder requirement DROPPED — DB-only authoring.
- D6: migration-list dropped — point at live `db/migrations/`.
- D7: skill retirement set = `plan-review-mechanical`, `plan-review-semantic`, `plan-applier`, `opus-code-review`.
- D8: `mechanicalization_score` task DROPPED.
- D9: bootstrap caveat DROPPED — schema landed.
- D10: locked decisions A1–A6, B1–B8, C1–C15, E1–E13, E15, E17, E18, F1–F15 marked shipped via siblings.
- D11: slug `master-plan-foldering-refactor` retained (DB lineage preserved).
- D12: 3 active stages (renumbered 1.0/2.0/3.0 in v2; v1 used 1.0/3.0/5.0).

Zero pending decisions. ship-plan reads handoff frontmatter + dispatches.

---

## 5. Pending decisions

None. All upstream-resolved at v1-reshape time.

---

## 6. Handoff to /ship-plan

Frontmatter shape ready (top of this doc). Run:

```
/ship-plan master-plan-foldering-refactor
```

Bulk authors §Plan Digest for all 13 tasks across 3 stages via single `master_plan_bundle_apply` Postgres tx. Reserves 13 fresh TECH-* ids via `reserve-id.sh`.

After ship-plan completes:

```
/ship-cycle master-plan-foldering-refactor 1.0   # reconcile
/ship-cycle master-plan-foldering-refactor 2.0   # skill retire
/ship-cycle master-plan-foldering-refactor 3.0   # cutover
/ship-final master-plan-foldering-refactor       # close v2
```

---

## Design Expansion

### Stage 1.0 — Reconcile spec to landed state

**Surface:** `docs/master-plan-foldering-refactor-design.md` + `ia_master_plans.description` (preamble) + `ia_master_plan_change_log` (audit rows).

**Risk:** v1 design doc carries stale Round 1–5 lock list — easy to drift on which decisions still apply. Mitigation: walk D1–D12 mechanically; reference change_log entries 6212–6218 for stage retirements.

**Touched paths:** `docs/master-plan-foldering-refactor-design.md`. No code touched.

**Validators:** `master_plan_health` green. `validate:frontmatter` (preamble shape).

### Stage 2.0 — Skill retirement

**Surface:** `ia/skills/{plan-review-mechanical,plan-review-semantic,plan-applier,opus-code-review}/`, `.claude/agents/*`, `.claude/commands/*`, `ia/skills/_preamble/stable-block.md`.

**Risk:** Tier 1 cache block sizing invariant — preamble cache has line budget; dropping skills shrinks block. Mitigation: `validate:cache-block-sizing` in task 2.0.3.

**Lifecycle impact:** `agent-lifecycle.md` row 6 (`/plan-review`) and row 9 (`/code-review`) tombstoned earlier; this stage drops the underlying skill folders. Operator-facing slash commands already dropped.

**Touched paths:** `ia/skills/`, `.claude/agents/`, `.claude/commands/`, `ia/skills/_preamble/stable-block.md`, `docs/agent-lifecycle.md`.

**Validators:** `skill:sync:all`, `validate:skill-drift`, `validate:cache-block-sizing`.

### Stage 3.0 — Cutover residue

**Surface:** `ia/projects/*.md` (residual single-issue specs), `ia/backlog/*.yaml` (residual yaml records), any skill/script still writing to those paths.

**Risk:** Hidden yaml-write callsites in tooling. Mitigation: 3.0.1 grep audit before any move; 3.0.2 patches before file moves.

**Verification:** `verify:local` full chain (validate:all + compile-check + db:migrate + bridge-preflight + Editor save/quit + playmode-smoke). Closes when entire chain green.

**Touched paths:** `ia/projects/`, `ia/backlog/`, residual skill/script files.

**Validators:** `validate:all`, `verify:local`, `validate:dead-project-specs`, `validate:backlog-yaml`.

---

## 7. Closeout

Stage 3.0 task 3.0.5 calls `cron_arch_changelog_append_enqueue` (kind=plan-closeout) summarising v2 outcome. `/ship-final` flips `ia_master_plans.closed_at` + emits annotated git tag `master-plan-foldering-refactor-v2`.
