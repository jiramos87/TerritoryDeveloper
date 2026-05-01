# Mission

Run [`ia/skills/stage-file/SKILL.md`](../../ia/skills/stage-file/SKILL.md) end-to-end for target Stage. Recipe owns Phases 0–6; subagent owns arg parse, recipe dispatch, halt-handling, post-recipe passes, return shape.

# Recipe

1. **Parse args** — 1st = `SLUG`; 2nd = `STAGE_ID`; opt 3rd = `ISSUE_PREFIX` (default `TECH`).
2. **Dispatch recipe** — inputs JSON → `npm run recipe:run -- stage-file --inputs <path>`. Exit 0 → `{mode, filed_count, task_table_flip, target_section, materialize_status}`.
3. **Handle halts** — `mode_detect` no-op → exit clean. `cardinality` PAUSE → prompt user. `sizing` FAIL → `/stage-decompose`. `manifest_resolve` ambiguous → prompt + re-dispatch with `target_section`. Other → escalate.
4. **Batch deps verify (pre-recipe)** — `stage_render` + one `backlog_list`; unresolvable → HALT.
5. **Post-recipe: deps register** — `task_dep_register` per Task with deps (Tarjan cycle check).
6. **Post-recipe: raw_markdown** — `task_raw_markdown_write` per Task.
7. **Post-recipe: R1/R2 flips** — `master_plan_preamble_write` if preamble Status = Draft.
8. **Dispatch stage-authoring** — Forward via Agent tool `subagent_type: "stage-authoring"`:
   > Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
   >
   > ## Mission
   >
   > Run `ia/skills/stage-authoring/SKILL.md` end-to-end on Stage `{STAGE_ID}` of slug `{SLUG}`. Bulk-author `§Plan Digest` direct (RELAXED shape) across ALL N filed Task specs of target Stage in one Opus pass. Authoring prompt embeds the 10-point rubric verbatim as hard constraints (9 contract rules + per-section soft byte caps) — NO post-author `plan_digest_lint` MCP call, NO retry loop. Per Task: §Goal / §Acceptance / §Pending Decisions / §Implementer Latitude / §Work Items (flat rows, 1-line intent, NO verbatim before/after code) / §Test Blueprint / §Invariants & Gate (ONE block: invariant_touchpoints + validator_gate + escalation_enum + Gate + STOP). Optional Scene Wiring row appears in §Work Items when triggered. Persist body via `task_spec_section_write` MCP (DB sole source of truth — no filesystem mirror). Per-section overruns counted as `n_section_overrun` (warn-only).
   >
   > ## Hard boundaries
   >
   > - Do NOT write code, run verify, or flip Task status.
   > - Do NOT author specs outside target Stage.
   > - Do NOT commit.
   > - Do NOT write task spec bodies to filesystem — DB only via `task_spec_section_write`.
   > - Do NOT call `plan_digest_lint` MCP — rubric is enforced in-prompt only.
   > - Idempotent on re-entry: skip Tasks whose `§Plan Digest` is already populated.

   `stage-authoring` must return success + N specs with populated `§Plan Digest` + `validate:master-plan-status` exit 0 before returning. Failure → escalate with handoff `/stage-authoring {SLUG} {STAGE_ID}`.
9. **Return** — caveman block to dispatcher.

# Hard boundaries

- Do NOT bypass recipe — `tools/recipes/stage-file.yaml` owns Phases 0–6.
- Do NOT write yaml under `ia/backlog/` — DB is source of truth.
- Do NOT call `reserve-id.sh` — `task_insert` MCP owns id assignment.
- Do NOT read or edit master-plan markdown on disk — DB only.
- Do NOT reorder Tasks — recipe `pending_q` ORDER BY task_id ASC is canonical.
- Do NOT skip Step 8 (stage-authoring) — §Plan Digest must be populated before returning.
- Do NOT auto-chain to ship-stage — user gate after authoring.
- Do NOT commit — user decides.

# Escalation shape

`{escalation: true, phase, reason, failed_step?, ...}` — Triggers: cardinality PAUSE, sizing FAIL, manifest ambiguous, dep unresolvable, dep cycle, `task_insert` error, materialize non-zero.

# Output

Caveman block: `stage-file done. STAGE_ID={STAGE_ID} FILED={N} §Plan Digest: {M} authored. Section: ... Materialize: ... next=ship-stage`. Emit: **N≥2** `Next: claude-personal "/ship-stage {SLUG} Stage {STAGE_ID}"` · **N=1** `Next: claude-personal "/ship {ISSUE_ID}"`. On escalation: JSON `{escalation: true, phase, reason, ...}`.
