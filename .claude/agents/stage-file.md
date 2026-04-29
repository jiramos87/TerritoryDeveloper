---
name: stage-file
description: DB-backed single-skill filing. Loads shared Stage MCP bundle once; gates cardinality (≥2 Tasks per Stage) + sizing (H1–H6); batch-verifies Depends-on ids via single `backlog_list`; resolves target BACKLOG.md section from master-plan H1 title; per-Task writes via `task_insert` MCP tool (DB-backed monotonic id from per-prefix sequence — no reserve-id.sh); appends manifest entry to `ia/state/backlog-sections.json`; bootstraps task spec body in DB via `task_spec_section_write`; runs `materialize-backlog.sh` (DB source default) — exit code is the filing gate; atomic task-table flip + R1/R2 Status flips. No yaml file written under `ia/backlog/`. Triggers: "/stage-file {orchestrator-path} Stage 1.2", "file stage tasks", "bulk create stage issues", "create backlog rows for Stage X.Y", "bootstrap issues for pending stage tasks", "compress stage tasks", "merge draft tasks". Argument order (explicit): SLUG first, STAGE_ID second.
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__master_plan_locate, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__backlog_list, mcp__territory-ia__backlog_record_validate, mcp__territory-ia__backlog_search, mcp__territory-ia__spec_outline, mcp__territory-ia__list_specs, mcp__territory-ia__invariant_preflight, mcp__territory-ia__master_plan_render, mcp__territory-ia__stage_render, mcp__territory-ia__master_plan_preamble_write, mcp__territory-ia__master_plan_change_log_append, mcp__territory-ia__task_insert, mcp__territory-ia__task_spec_section_write, mcp__territory-ia__lifecycle_stage_context
model: opus
reasoning_effort: high
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose (Notes + acceptance caveman; row structure verbatim per agent-output-caveman-authoring). Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

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
