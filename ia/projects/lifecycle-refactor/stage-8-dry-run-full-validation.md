### Stage 8 — Validation + Merge / Dry-Run + Full Validation

**Status:** Final (closed 2026-04-19)

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
| T8.1 | Dry-run new chain end-to-end | **TECH-485** | Done | Select a small pending Task from any open master plan (prefer a Task in _pending_ state, not one currently In Progress); run the new chain: `/plan-review` on its Stage → `/author --task {ISSUE_ID}` → `/implement` (no actual code ship; stop after plan-review + author to validate dispatch wiring) → simulate audit + code-review outputs → verify closeout-apply reads `§Stage Closeout Plan` stub correctly; document each pair's handoff in migration JSON M7.dry-run section; no commit of dry-run artifacts. |
| T8.2 | Regen BACKLOG + progress.html | **TECH-486** | Done | Run `bash tools/scripts/materialize-backlog.sh` → verify `BACKLOG.md` + `BACKLOG-ARCHIVE.md` consistent with yaml state post-M3; run `npm run progress` → verify `docs/progress.html` renders Stage/Task 2-level tree correctly (no Phase rows). |
| T8.3 | Full verify:local | **TECH-487** | Done | Run `npm run verify:local` (validate:all + unity:compile-check + db:bridge-preflight); triage any failures by subsystem: web failures → Stage 3.2 patch; MCP failures → Stage 3.1 patch; skill/agent failures → Stage 3.3 patch; yaml failures → Stage 2.2 patch. |
| T8.4 | Fix remaining failures + M7 flip | **TECH-488** | Done | Apply minimal targeted fixes for any failures from T8.3; re-run `npm run verify:local` until green; flip migration JSON M7 `done`. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- operation: file_task
  target_anchor: "task_key:T8.1"
  reserved_id: ""
  issue_type: "TECH"
  title: "Dry-run new chain end-to-end"
  priority: "high"
  notes: |
    Pick small pending Task from any open master plan (not In Progress). Run new chain partial: /plan-review on its Stage then /enrich then /implement stub
    (no code ship — stop after wiring validation). Simulate audit + code-review outputs. Verify closeout-apply reads §Closeout Plan anchor correctly.
    Document each pair handoff in migration JSON M7.dry-run. No artifact commit. Exercises Plan-Apply pair seams end-to-end pre-merge.
  depends_on: []
  related:
    - "T8.2"
    - "T8.3"
    - "T8.4"
  stub_body:
    summary: |
      Dry-run exercise of new lifecycle chain on one small pending Task. Validates dispatch wiring across plan-review → enrich → implement → simulated
      audit → closeout-apply seam. Stops short of real code ship. Captures handoff artifacts to migration JSON for merge gate review.
    goals: |
      - Confirm each pair seam (plan-review → fix-apply, stage-closeout-plan → apply) hands off without anchor ambiguity.
      - Capture migration JSON M7.dry-run entries per seam.
      - Surface integration bugs before Stage 8 T8.3 full verify:local.
      - No state mutation persisted (no BACKLOG archive, no spec delete).
    systems_map: |
      - `.claude/commands/{plan-review,implement,closeout}.md` — dispatchers under test.
      - `.claude/agents/plan-reviewer.md`, `plan-fix-applier.md`, `stage-closeout-planner.md`, `stage-closeout-applier.md` — pair seams exercised.
      - `ia/skills/plan-review/`, `plan-fix-apply/`, `stage-closeout-plan/`, `stage-closeout-apply/` — skill bodies.
      - `ia/state/lifecycle-refactor-migration.json` — M7.dry-run section (new subtree).
      - `ia/rules/plan-apply-pair-contract.md` — contract under verification.
    impl_plan_sketch: |
      Phase 1 — Select candidate pending Task + run chain partial + record handoffs:
      scan open master plans for _pending_ Task in non-active Stage; invoke /plan-review on owning Stage; dispatch /enrich; dry-run /implement
      (stop pre-commit); simulate §Audit + §Code Review outputs inline; invoke stage-closeout-plan then stage-closeout-apply against simulated
      Stage-end state; write migration JSON M7.dry-run with per-seam {seam, handoff_anchor, tuple_count, verdict}; revert any accidental mutation.
```

```yaml
- operation: file_task
  target_anchor: "task_key:T8.2"
  reserved_id: ""
  issue_type: "TECH"
  title: "Regen BACKLOG + progress.html"
  priority: "medium"
  notes: |
    Run `bash tools/scripts/materialize-backlog.sh` — verify BACKLOG.md + BACKLOG-ARCHIVE.md consistent with yaml state post-M3 Phase-drop. Run
    `npm run progress` — verify docs/progress.html renders Stage/Task 2-level tree (no Phase rows). Flags any regen drift from Stage 4 yaml edits.
  depends_on: []
  related:
    - "T8.1"
    - "T8.3"
  stub_body:
    summary: |
      Regenerate Backlog view + progress dashboard from post-M3 yaml state. Confirms materialize-backlog.sh + progress script both honor the
      dropped Phase column / parent_phase frontmatter fold without manual patching.
    goals: |
      - Backlog view (BACKLOG.md + BACKLOG-ARCHIVE.md) regenerates cleanly from current yaml records.
      - docs/progress.html emits Stage → Task 2-level tree; zero Phase rows.
      - No stale Phase artifact leaks through either generator.
    systems_map: |
      - `tools/scripts/materialize-backlog.sh` — Backlog view generator (flock-guarded).
      - `ia/backlog/*.yaml`, `ia/backlog-archive/*.yaml` — source records (post-Stage 4 schema).
      - `BACKLOG.md`, `BACKLOG-ARCHIVE.md` — regenerated views (read-only).
      - `npm run progress` → `docs/progress.html` — dashboard renderer; parses master plans.
      - `tools/mcp-ia-server/src/parser/` — backlog-schema expectation (Stage 4 no-phase allowlist).
    impl_plan_sketch: |
      Phase 1 — Regen + diff:
      run materialize-backlog.sh; diff BACKLOG.md head vs last committed version (expect only legit Stage 4 row changes); run npm run progress;
      open docs/progress.html in browser or static diff; confirm tree depth = 2 (Stage → Task) across all 16 master plans; flag any Phase-row
      regression to migration JSON T8.2.findings; no commit unless green.
```

```yaml
- operation: file_task
  target_anchor: "task_key:T8.3"
  reserved_id: ""
  issue_type: "TECH"
  title: "Full verify:local"
  priority: "high"
  notes: |
    Run `npm run verify:local` — full chain (validate:all + unity:compile-check + db:bridge-preflight + Editor save/quit + db:bridge-playmode-smoke).
    Triage failures by subsystem: web → Stage 6 patch; MCP → Stage 5 patch; skill/agent → Stage 7 patch; yaml → Stage 4 patch. Escalate
    rather than guess cause. This is the merge-gate acceptance run.
  depends_on: []
  related:
    - "T8.1"
    - "T8.2"
    - "T8.4"
  stub_body:
    summary: |
      Full local verification chain as merge-gate acceptance. Single invocation of npm run verify:local covers validate:all, Unity compile,
      Postgres bridge preflight, Editor save/quit, bridge playmode smoke. Failures triaged by owning Stage.
    goals: |
      - verify:local green end-to-end on current branch.
      - Every failure routed to correct Stage patch lane (no cross-lane fixes).
      - Acceptance artifact recorded for Stage 9 sign-off gate.
    systems_map: |
      - `package.json` — `verify:local` / `verify:post-implementation` composition (see `ARCHITECTURE.md` §Local verification).
      - `tools/scripts/unity-*.sh` + `$UNITY_EDITOR_PATH` from `.env.local`.
      - `tools/scripts/db-bridge-*.ts` + Postgres :5434 (brew native).
      - `docs/agent-led-verification-policy.md` — canonical verification policy.
      - Triage targets: Stage 4 (yaml), Stage 5 (MCP), Stage 6 (web), Stage 7 (skills/agents).
    impl_plan_sketch: |
      Phase 1 — Run full chain + triage:
      confirm `.env.local` sets UNITY_EDITOR_PATH + Postgres creds; run npm run verify:local; on first non-zero exit, capture full stdout + stderr
      into migration JSON T8.3.failures[]; classify each failure by subsystem; hand off to T8.4 for targeted fix; do NOT self-patch inside T8.3 —
      T8.3 is a read-only observation task.
```

```yaml
- operation: file_task
  target_anchor: "task_key:T8.4"
  reserved_id: ""
  issue_type: "TECH"
  title: "Fix remaining failures + M7 flip"
  priority: "high"
  notes: |
    Apply minimal targeted fixes for failures surfaced by T8.3 (issue id refs currently say "T4.1.3" — stale pre-migration id; canonical source =
    T8.3 findings). Re-run `npm run verify:local` until green. Flip migration JSON M7 `done`. Bounded — if fix scope exceeds one touch per
    failure, escalate + split rather than pile on.
  depends_on: []
  related:
    - "T8.1"
    - "T8.2"
    - "T8.3"
  stub_body:
    summary: |
      Close-out task for Stage 8. Applies minimal fixes against T8.3-surfaced failures, re-runs verify:local to green, and flips migration JSON
      M7 done. Gate before Stage 9 user sign-off.
    goals: |
      - Every T8.3 failure gets a minimal targeted fix in its owning Stage's lane.
      - verify:local green on final re-run.
      - migration JSON M7 flipped done; Stage 8 exit criteria satisfied.
      - No scope creep — escalate if a fix needs cross-stage work.
    systems_map: |
      - `ia/state/lifecycle-refactor-migration.json` — M7 entry flip target.
      - Fix targets per T8.3 triage: `ia/backlog/*.yaml` (Stage 4), `tools/mcp-ia-server/` (Stage 5), `web/lib/` (Stage 6), `ia/skills/` + `.claude/agents/` (Stage 7).
      - `npm run verify:local` — acceptance gate.
    impl_plan_sketch: |
      Phase 1 — Iterate fix + re-verify:
      read T8.3.failures[] from migration JSON; for each failure apply minimal patch in owning Stage lane; after each patch run verify:local
      again; on persistent failure after 1 retry, escalate to user with diagnosis (no deeper auto-patching); on green, flip M7 done in migration
      JSON + close T8.4.
```

#### §Plan Fix

> plan-review — 5 tuples. Spawn `plan-fix-apply ia/projects/lifecycle-refactor-master-plan.md 8`.

```yaml
- operation: replace_section
  target_path: ia/projects/lifecycle-refactor-master-plan.md
  target_anchor: "task_key:T8.1"
  payload: |
    | T8.1 | Dry-run new chain end-to-end | **TECH-485** | Draft | Select a small pending Task from any open master plan (prefer a Task in _pending_ state, not one currently In Progress); run the new chain: `/plan-review` on its Stage → `/author --task {ISSUE_ID}` → `/implement` (no actual code ship; stop after plan-review + author to validate dispatch wiring) → simulate audit + code-review outputs → verify closeout-apply reads `§Stage Closeout Plan` stub correctly; document each pair's handoff in migration JSON M7.dry-run section; no commit of dry-run artifacts. |

- operation: replace_section
  target_path: ia/projects/lifecycle-refactor-master-plan.md
  target_anchor: "task_key:T8.4"
  payload: |
    | T8.4 | Fix remaining failures + M7 flip | **TECH-488** | Draft | Apply minimal targeted fixes for any failures from T8.3; re-run `npm run verify:local` until green; flip migration JSON M7 `done`. |

- operation: replace_section
  target_path: ia/backlog/TECH-485.yaml
  target_anchor: "notes"
  payload: |
    notes: |
      Pick small pending Task from any open master plan (not In Progress). Run new chain partial: /plan-review on its Stage then /author --task {ISSUE_ID} then /implement stub
      (no code ship — stop after wiring validation). Simulate audit + code-review outputs. Verify closeout-apply reads §Stage Closeout Plan anchor correctly.
      Document each pair handoff in migration JSON M7.dry-run. No artifact commit. Exercises Plan-Apply pair seams end-to-end pre-merge.

- operation: replace_section
  target_path: ia/backlog/TECH-488.yaml
  target_anchor: "notes"
  payload: |
    notes: |
      Apply minimal targeted fixes for failures surfaced by T8.3. Re-run `npm run verify:local` until green. Flip migration JSON M7 `done`.
      Bounded — if fix scope exceeds one touch per failure, escalate + split rather than pile on.

- operation: replace_section
  target_path: ia/backlog/TECH-485.yaml
  target_anchor: "raw_markdown"
  payload: |
    raw_markdown: |
      Dry-run new chain end-to-end — Pick small pending Task from any open master plan (not In Progress). Run new chain partial using /author (no /enrich — retired).
```
---
