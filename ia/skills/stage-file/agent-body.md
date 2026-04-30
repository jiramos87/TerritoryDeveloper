# Mission

Run [`ia/skills/stage-file/SKILL.md`](../../ia/skills/stage-file/SKILL.md) end-to-end for target Stage. Recipe owns Phases 0–6; subagent owns arg parse, recipe dispatch, halt-handling, post-recipe passes, return shape.

# Recipe

1. **Parse args** — 1st = `SLUG`; 2nd = `STAGE_ID`; opt 3rd = `ISSUE_PREFIX` (default `TECH`).
2. **Dispatch recipe** — inputs JSON → `npm run recipe:run -- stage-file --inputs <path>`. Exit 0 → `{mode, filed_count, target_section, materialize_status}`.
3. **Handle halts** — `mode_detect` no-op → exit clean. `cardinality` PAUSE → prompt user. `sizing` FAIL → `/stage-decompose`. `manifest_resolve` ambiguous → prompt + re-dispatch with `target_section`. Other → escalate.
4. **Batch deps verify (pre-recipe)** — `stage_render` + one `backlog_list`; unresolvable → HALT.
5. **Post-recipe: deps register** — `task_dep_register` per Task with deps (Tarjan cycle check).
6. **Post-recipe: raw_markdown** — `task_raw_markdown_write` per Task.
7. **Post-recipe: R1/R2 flips** — `master_plan_preamble_write` if preamble Status = Draft.
8. **Return** — caveman block to dispatcher.

# Hard boundaries

- Do NOT bypass recipe — `tools/recipes/stage-file.yaml` owns Phases 0–6.
- Do NOT write yaml under `ia/backlog/` — DB is source of truth.
- Do NOT call `reserve-id.sh` — `task_insert` MCP owns id assignment.
- Do NOT read or edit master-plan markdown on disk — DB only.
- Do NOT reorder Tasks — recipe `pending_q` ORDER BY task_id ASC is canonical.
- Do NOT commit — user decides.

# Escalation shape

`{escalation: true, phase, reason, failed_step?, ...}` — Triggers: cardinality PAUSE, sizing FAIL, manifest ambiguous, dep unresolvable, dep cycle, `task_insert` error, materialize non-zero.

# Output

Caveman block to dispatcher: `stage-file done. STAGE_ID={STAGE_ID} FILED={N} SKIPPED={K} Section: ... Materialize: ... next=stage-file-chain-continue`. On escalation: JSON `{escalation: true, phase, reason, ...}`.
