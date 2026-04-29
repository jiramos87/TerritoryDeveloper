# Mission

Run [`ia/skills/stage-file/SKILL.md`](../../ia/skills/stage-file/SKILL.md) end-to-end for target Stage. Recipe-runner shape: `tools/recipes/stage-file.yaml` owns Phases 0–6 mechanics (mode detection, gates, manifest resolve, per-task `task_insert`, manifest append, materialize, change-log, progress). Subagent owns arg parsing, recipe dispatch, halt-handling, Phase 5.B deps registration, return shape.

# Recipe

1. **Parse args** — 1st = `SLUG` (bare master-plan slug); 2nd = `STAGE_ID` (`X.Y` or `Stage X.Y`); optional 3rd = `ISSUE_PREFIX` (`TECH` / `FEAT` / `BUG` / `ART` / `AUDIO`, default `TECH`).
2. **Dispatch recipe** — Write inputs JSON `{slug, stage_id, issue_prefix?, target_section?}` to a temp file; run `npm run recipe:run -- stage-file --inputs <path>`. Recipe returns structured outputs `{mode, filed_count, target_section, materialize_status}` on exit 0.
3. **Handle halts** — Recipe non-zero exit → inspect `failed_step` + stderr:
   - `mode_detect` no-op → report stage state, exit clean.
   - `cardinality` PAUSE (pending<2) → prompt user to confirm singleton stage; on confirm, re-dispatch with override flag (recipe path TBD; inline subagent file for now).
   - `sizing` FAIL (>8 tasks) → halt + handoff `/stage-decompose`.
   - `manifest_resolve` ambiguous → list candidates, prompt user, re-dispatch with `target_section` override.
   - Any other → escalate to dispatcher with `{escalation: true, phase, reason, stderr}`.
4. **Phase 3 — Batch deps verify (subagent-side)** — Read pending tasks via `mcp__territory-ia__stage_render`; collect union of Depends-on ids; one `backlog_list({ids})` call. Unresolvable → HALT before recipe dispatch.
5. **Phase 5.B — Cross-iter deps registration (post-recipe)** — After recipe exit 0, for each newly-filed Task with declared deps: `task_dep_register({task_id, depends_on, related})` MCP (atomic Tarjan SCC cycle check). Same-batch deps resolve here since all `task_insert`s are committed.
6. **Phase 5.C — raw_markdown persist (post-recipe)** — Per Task: `task_raw_markdown_write` MCP. Recipe writes empty body; stage-authoring populates §Plan Digest later.
7. **R1/R2 Status flips (post-recipe)** — Recipe emits `stage_status_flip` change-log row; subagent confirms `ia_stages.status` flipped Draft → In Progress + master plan preamble Status updated via `master_plan_preamble_write` if currently Draft.
8. **Return to dispatcher** — Single caveman block. Shape under §Output.

# Hard boundaries

- Do NOT bypass the recipe — Phases 0–6 mechanics live in `tools/recipes/stage-file.yaml`. Inline reimplementation is drift.
- Do NOT write yaml under `ia/backlog/` — DB is source of truth.
- Do NOT call `reserve-id.sh` — per-prefix DB sequences own id assignment via `task_insert` MCP.
- Do NOT read or edit master-plan markdown on disk — DB is source of truth.
- Do NOT reorder Tasks — recipe `pending_q` ORDER BY task_id ASC is canonical.
- Do NOT edit `BACKLOG.md` directly — recipe `materialize` step regenerates from DB + manifest.
- Do NOT run `validate:backlog-yaml` — no yaml written on DB path.
- Do NOT run `validate:all` — gate is recipe `materialize` exit code.
- Do NOT emit user-facing `/ship-stage` or `/ship` handoff — dispatcher owns post-chain handoff.
- Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
- Do NOT `rm -rf` or delete any existing file.
- Do NOT commit — user decides.

# Escalation shape

`{escalation: true, phase: N, reason: "...", failed_step?: "...", candidate_matches?: [...], stderr?: "..."}` — returned to dispatcher. Triggers: cardinality PAUSE, sizing FAIL, manifest ambiguous, dep unresolvable, dep cycle (Tarjan), `task_insert` unique/sequence, materialize non-zero, R2 self-check miss.

# Output

Single caveman block returned to `/stage-file` dispatcher (not user). Shape:

```
stage-file done. STAGE_ID={STAGE_ID} FILED={N} SKIPPED={K}
Filed: {ISSUE_ID_1} — {title_1}
       {ISSUE_ID_2} — {title_2}
       ...
Section: {TARGET_SECTION_HEADER}
Materialize: {ran|skipped (no-op)}
Recipe: exit 0.
next=stage-file-chain-continue
```

On escalation: JSON `{escalation: true, phase, reason, ...}` payload.
