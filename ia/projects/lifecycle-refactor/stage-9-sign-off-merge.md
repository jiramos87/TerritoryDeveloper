### Stage 9 — Validation + Merge / Sign-Off + Merge

**Status:** Done

**Objectives:** Present dry-run artifacts to user. Collect sign-off. Merge branch, restart MCP, and close freeze window. File token-cost telemetry follow-up. File ship-stage chain-journal persistence follow-up.

**Exit:**

- Migration JSON M8 gate entry written with user sign-off timestamp.
- `feature/lifecycle-collapse-cognitive-split` merged to main.
- `territory-ia` MCP server restarted post-merge; new schema verified.
- Freeze note removed from `CLAUDE.md`.
- Token-cost telemetry follow-up TECH issue filed in `ia/backlog/`.
- Ship-stage chain-journal persistence follow-up TECH issue filed in `ia/backlog/`.
- Migration JSON M8 flipped to `done`.
- Phase 1 — User gate + MCP post-merge restart.
- Phase 2 — Merge + freeze-close + follow-up issues.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | User sign-off gate | **TECH-489** | Done | Present dry-run artifacts (migration JSON M7.dry-run, `BACKLOG.md` diff, `docs/progress.html` screenshot) to user; wait for explicit sign-off ("LGTM" / "merge"); record sign-off + timestamp in migration JSON M8.gate; do not proceed to T4.2.3 without gate. |
| T9.2 | MCP restart + schema verify | **TECH-490** | Done | Kill and respawn `territory-ia` MCP process on the post-merge main branch; send a test `router_for_task` call with `plan_review` stage name; confirm enum accepted; confirm `plan_apply_validate` tool responds; record restart success in migration JSON. |
| T9.3 | Merge branch | **TECH-491** | Done | Merge `feature/lifecycle-collapse-cognitive-split` into main (standard merge commit, no squash — preserve migration history); resolve any conflicts in `BACKLOG.md` / `BACKLOG-ARCHIVE.md` from concurrent activity during freeze window by re-running `materialize-backlog.sh` post-merge; flip migration JSON M8 `done`. |
| T9.4 | Freeze close + token-cost issue + Q9 baseline instrumentation | **TECH-492** | Done | Remove freeze note from `CLAUDE.md` §Key commands; file a token-cost telemetry tracker TECH issue in `ia/backlog/` (title: "Token-cost telemetry baseline — pre/post lifecycle refactor + Q9 pair-head read-count"; priority: Low). Issue MUST require per-Stage instrumentation that captures (a) total prompt tokens per Stage, (b) **pair-head read count per Stage** (distinct from total tokens — each cache-hit read counted separately; precondition for Stage 10 P1 savings validation per `docs/prompt-caching-mechanics.md` §4 R5), (c) cache-write / cache-read / cache-miss token counts from `usage.cache_creation_input_tokens` + `usage.cache_read_input_tokens`, (d) per-Stage bundle byte + token size (validates F2 sizing gate per rev 4 C1/R2). Data feeds Stage 10 T10.1 precondition gate. Run final `npm run validate:all` on main post-merge to confirm clean state. |
| T9.5 | Ship-stage chain-journal persistence follow-up | **TECH-493** | Done | File a TECH issue in `ia/backlog/` via `/project-new` (title: "Ship-stage chain-journal persistence — crash-survivable stage digest + resume UX"; priority: Medium). Issue MUST scope: (a) `ia/skills/ship-stage/SKILL.md` Step 2.5 writes `ia/state/ship-stage-{master-plan-slug}-{stage-id}.json` after each closeout (append `{task_id, lessons[], decisions[], verify_iterations}` accumulator entry), (b) Phase 0 reads existing journal on re-invocation + emits `Resuming at task K/N (skipped: T1..Tk-1 already Done)` line before continuing, (c) Phase 4 final digest reads journal as authoritative source for `chain.tasks[]` aggregation, (d) Phase 4 deletes journal file on `SHIP_STAGE PASSED` exit (preserve on STOPPED / STAGE_VERIFY_FAIL for next-run resume), (e) lockfile `ia/state/.ship-stage-{master-plan-slug}-{stage-id}.lock` per concurrency-domain rule (invariants Guardrails §IF flock guard); read-only Phase 0 inspection skips flock. Out of scope: spec-implementer mid-phase transactional markers (separate concern; covered today by `subagent-progress-emit` stderr markers + Edit-tool `old_string` idempotency floor + Test Blueprint atomic phases — no runtime spec-frontmatter mutation pathway needed). Acceptance: kill `/ship-stage` mid-Stage at task 2/3, re-invoke same args, observe resume line + only T3 dispatched + final digest contains all 3 tasks' lessons/decisions. |

#### §Stage File Plan

<!-- Emitted by stage-file-plan (Opus). Pair-tail stage-file-apply (Sonnet) reads tuples verbatim, writes yaml + spec stub, flips row Issue cell, then materializes BACKLOG. -->

```yaml
tuples:
  - operation: file_task
    reserved_id: TECH-489
    task_key: T9.1
    title: "User sign-off gate"
    priority: high
    issue_type: TECH
    section: "Validation + Merge / Sign-Off + Merge"
    target_path: ia/projects/lifecycle-refactor-master-plan.md
    target_anchor: "| T9.1 | User sign-off gate | _pending_ |"
    depends_on: []
    related: ["TECH-490", "TECH-491", "TECH-492", "TECH-493"]
    notes: |
      Present dry-run artifacts (migration JSON M7.dry-run, `BACKLOG.md` diff, `docs/progress.html` screenshot) to user. Wait for explicit sign-off ("LGTM" / "merge"). Record sign-off + timestamp in migration JSON M8.gate. Do not proceed to merge without gate.
    acceptance: |
      - [ ] Dry-run artifacts surfaced to user (migration JSON M7.dry-run row + BACKLOG diff + progress.html screenshot).
      - [ ] Explicit user sign-off captured verbatim ("LGTM" / "merge" / equivalent).
      - [ ] Migration JSON M8.gate entry written with sign-off text + ISO8601 timestamp.
      - [ ] No merge (T9.3) dispatch until gate row present.
    stub_body: |
      ## 1. Summary
      Human sign-off gate before merging `feature/lifecycle-collapse-cognitive-split`. Collect artifacts, poll user, record gate row.

      ## 2. Goals and Non-Goals
      ### 2.1 Goals
      1. Surface dry-run artifacts (M7.dry-run + BACKLOG diff + progress.html).
      2. Capture explicit sign-off string + timestamp in migration JSON M8.gate.
      ### 2.2 Non-Goals
      1. Running the merge itself (T9.3 scope).
      2. Restarting MCP (T9.2 scope).

      ## 4. Current State
      ### 4.2 Systems map
      - `ia/state/lifecycle-refactor-migration.json` — M8.gate row target.
      - `docs/progress.html` — dry-run screenshot source.
      - `BACKLOG.md` — diff source.

      ## Open Questions
      - None.

  - operation: file_task
    reserved_id: TECH-490
    task_key: T9.2
    title: "MCP restart + schema verify"
    priority: high
    issue_type: TECH
    section: "Validation + Merge / Sign-Off + Merge"
    target_path: ia/projects/lifecycle-refactor-master-plan.md
    target_anchor: "| T9.2 | MCP restart + schema verify | _pending_ |"
    depends_on: ["TECH-491"]
    related: ["TECH-489", "TECH-492", "TECH-493"]
    notes: |
      Kill + respawn `territory-ia` MCP process on post-merge main. Send test `router_for_task` call with `plan_review` stage name; confirm enum accepted. Confirm `plan_apply_validate` tool responds. Record restart success in migration JSON.
    acceptance: |
      - [ ] MCP process respawned on post-merge main (PID logged).
      - [ ] `router_for_task` with `lifecycle_stage: plan_review` returns ok.
      - [ ] `plan_apply_validate` tool discoverable + responsive.
      - [ ] Migration JSON entry records restart success + timestamp.
    stub_body: |
      ## 1. Summary
      Restart `territory-ia` MCP server post-merge so new schema (plan-apply-pair-contract enums, retired tools) is live.

      ## 2. Goals and Non-Goals
      ### 2.1 Goals
      1. Fresh MCP process on merged main branch.
      2. Confirm schema includes plan_review enum + plan_apply_validate tool.
      ### 2.2 Non-Goals
      1. Schema edits (frozen post-merge).
      2. Tool catalog rewrite.

      ## 4. Current State
      ### 4.2 Systems map
      - `.mcp.json` — MCP registration.
      - `tools/mcp-ia-server/src/index.ts` — entrypoint.
      - `ia/state/lifecycle-refactor-migration.json` — restart log target.

      ## Open Questions
      - None.

  - operation: file_task
    reserved_id: TECH-491
    task_key: T9.3
    title: "Merge branch"
    priority: high
    issue_type: TECH
    section: "Validation + Merge / Sign-Off + Merge"
    target_path: ia/projects/lifecycle-refactor-master-plan.md
    target_anchor: "| T9.3 | Merge branch | _pending_ |"
    depends_on: ["TECH-489"]
    related: ["TECH-490", "TECH-492", "TECH-493"]
    notes: |
      Merge `feature/lifecycle-collapse-cognitive-split` into main (standard merge commit, no squash — preserve migration history). Resolve any `BACKLOG.md` / `BACKLOG-ARCHIVE.md` conflicts from concurrent activity by re-running `materialize-backlog.sh` post-merge. Flip migration JSON M8 `done`.
    acceptance: |
      - [ ] Branch merged to main with merge commit (no squash).
      - [ ] BACKLOG.md + BACKLOG-ARCHIVE.md re-materialized post-merge if conflicts surfaced.
      - [ ] Migration JSON M8 flipped to `done` with timestamp.
      - [ ] `npm run validate:all` green on main post-merge.
    stub_body: |
      ## 1. Summary
      Land lifecycle-refactor branch on main. Preserve migration history. Re-materialize BACKLOG views if needed.

      ## 2. Goals and Non-Goals
      ### 2.1 Goals
      1. Merge commit on main (no squash).
      2. M8 flip to done.
      ### 2.2 Non-Goals
      1. MCP restart (T9.2 scope).
      2. Freeze-note removal (T9.4 scope).

      ## 4. Current State
      ### 4.2 Systems map
      - `BACKLOG.md` / `BACKLOG-ARCHIVE.md` — generated views, may conflict.
      - `tools/scripts/materialize-backlog.sh` — regen tool.
      - `ia/state/lifecycle-refactor-migration.json` — M8 row.

      ## Open Questions
      - None.

  - operation: file_task
    reserved_id: TECH-492
    task_key: T9.4
    title: "Freeze close + token-cost telemetry tracker + Q9 baseline instrumentation"
    priority: medium
    issue_type: TECH
    section: "Validation + Merge / Sign-Off + Merge"
    target_path: ia/projects/lifecycle-refactor-master-plan.md
    target_anchor: "| T9.4 | Freeze close + token-cost issue + Q9 baseline instrumentation | _pending_ |"
    depends_on: ["TECH-491"]
    related: ["TECH-489", "TECH-490", "TECH-493"]
    notes: |
      Remove freeze note from `CLAUDE.md` §Key commands. File token-cost telemetry tracker TECH issue (title: "Token-cost telemetry baseline — pre/post lifecycle refactor + Q9 pair-head read-count"; priority: Low). Issue MUST scope per-Stage instrumentation: (a) total prompt tokens per Stage; (b) **pair-head read count per Stage** (distinct from total tokens; each cache-hit read counted separately; precondition for Stage 10 P1 savings validation per `docs/prompt-caching-mechanics.md` §4 R5); (c) cache-write / cache-read / cache-miss token counts from `usage.cache_creation_input_tokens` + `usage.cache_read_input_tokens`; (d) per-Stage bundle byte + token size (validates F2 sizing gate per rev 4 C1/R2). Data feeds Stage 10 T10.1 precondition gate. Run final `npm run validate:all` on main to confirm clean state.
    acceptance: |
      - [ ] Freeze note removed from `CLAUDE.md` §Key commands.
      - [ ] Q9 baseline tracker TECH issue filed in `ia/backlog/` with scope (a)–(d) verbatim.
      - [ ] `npm run validate:all` green on main post-merge.
      - [ ] Filed issue id cross-referenced from Stage 10 T10.1 precondition note (read-only reference, no edit required here).
    stub_body: |
      ## 1. Summary
      Retire freeze window + file Q9 baseline telemetry tracker. Gate feed for Stage 10 cache-layer activation.

      ## 2. Goals and Non-Goals
      ### 2.1 Goals
      1. Remove freeze prose from `CLAUDE.md` §Key commands.
      2. File telemetry tracker with read-count + cache-usage scope.
      3. Baseline ready for Stage 10 T10.1 precondition.
      ### 2.2 Non-Goals
      1. Implementing the telemetry collector (tracker scope, separate issue).
      2. Landing any Stage 10 cache wiring.

      ## 4. Current State
      ### 4.2 Systems map
      - `CLAUDE.md` §Key commands — freeze note target.
      - `ia/backlog/` — tracker yaml destination.
      - `docs/prompt-caching-mechanics.md` §4 R5 — read-count semantics source.

      ## Open Questions
      - None.

  - operation: file_task
    reserved_id: TECH-493
    task_key: T9.5
    title: "Ship-stage chain-journal persistence follow-up"
    priority: medium
    issue_type: TECH
    section: "Validation + Merge / Sign-Off + Merge"
    target_path: ia/projects/lifecycle-refactor-master-plan.md
    target_anchor: "| T9.5 | Ship-stage chain-journal persistence follow-up | _pending_ |"
    depends_on: ["TECH-491"]
    related: ["TECH-489", "TECH-490", "TECH-492"]
    notes: |
      File a TECH issue in `ia/backlog/` via `/project-new` (title: "Ship-stage chain-journal persistence — crash-survivable stage digest + resume UX"; priority: Medium). Issue MUST scope: (a) `ia/skills/ship-stage/SKILL.md` Step 2.5 writes `ia/state/ship-stage-{master-plan-slug}-{stage-id}.json` after each closeout (append `{task_id, lessons[], decisions[], verify_iterations}` accumulator entry); (b) Phase 0 reads existing journal on re-invocation + emits `Resuming at task K/N (skipped: T1..Tk-1 already Done)` line before continuing; (c) Phase 4 final digest reads journal as authoritative source for `chain.tasks[]` aggregation; (d) Phase 4 deletes journal on `SHIP_STAGE PASSED` exit (preserve on STOPPED / STAGE_VERIFY_FAIL for next-run resume); (e) lockfile `ia/state/.ship-stage-{master-plan-slug}-{stage-id}.lock` per concurrency-domain rule (invariants Guardrails §IF flock guard); read-only Phase 0 inspection skips flock. Out of scope: spec-implementer mid-phase transactional markers.
    acceptance: |
      - [ ] TECH issue filed with scope (a)–(e) verbatim.
      - [ ] Acceptance in filed issue includes: kill `/ship-stage` mid-Stage at task 2/3, re-invoke same args, observe resume line + only T3 dispatched + final digest contains all 3 tasks' lessons/decisions.
      - [ ] Issue priority = Medium.
      - [ ] Issue id cross-referenced from `ia/skills/ship-stage/SKILL.md` §Open Questions (read-only reference; edit deferred to filed issue's implementer).
    stub_body: |
      ## 1. Summary
      File crash-survivable ship-stage journal tracker. Resume semantics + lockfile + Phase 4 digest-source swap.

      ## 2. Goals and Non-Goals
      ### 2.1 Goals
      1. Journal file accumulator post-closeout.
      2. Phase 0 resume line + skip-completed behavior.
      3. Phase 4 journal-as-source + conditional delete.
      ### 2.2 Non-Goals
      1. Spec-implementer mid-phase transactional markers (separate concern).
      2. Refactoring `subagent-progress-emit` stderr channel.

      ## 4. Current State
      ### 4.2 Systems map
      - `ia/skills/ship-stage/SKILL.md` — Step 2.5 + Phase 0 + Phase 4 edit targets.
      - `ia/state/` — journal + lockfile destination.
      - `ia/rules/invariants.md` §Guardrails — flock rule anchor.

      ## Open Questions
      - None.
```

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs (TECH-489, TECH-490, TECH-491, TECH-492, TECH-493) aligned. No tuples emitted. Downstream pipeline continue.

---
