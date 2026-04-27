---
name: stage-file
description: DB-backed single-skill filing. Loads shared Stage MCP bundle once; gates cardinality (≥2 Tasks per Stage) + sizing (H1–H6); batch-verifies Depends-on ids via single `backlog_list`; resolves target BACKLOG.md section from master-plan H1 title; per-Task writes via `task_insert` MCP tool (DB-backed monotonic id from per-prefix sequence — no reserve-id.sh); appends manifest entry to `ia/state/backlog-sections.json`; bootstraps `ia/projects/{ISSUE_ID}.md` spec stub from template; runs `materialize-backlog.sh` (DB source default) + `validate:dead-project-specs`; atomic task-table flip + R1/R2 Status flips. No yaml file written under `ia/backlog/`. Triggers: "/stage-file {orchestrator-path} Stage 1.2", "file stage tasks", "bulk create stage issues", "create backlog rows for Stage X.Y", "bootstrap issues for pending stage tasks", "compress stage tasks", "merge draft tasks". Argument order (explicit): SLUG first, STAGE_ID second.
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

Run [`ia/skills/stage-file/SKILL.md`](../../ia/skills/stage-file/SKILL.md) end-to-end for target Stage. Single-skill DB-backed filing — no pair-head / pair-tail split. 8 phases (Mode detection → Load Stage MCP bundle → Stage block + gates → Batch deps verify → Manifest section resolve → Per-task iterator via `task_insert` MCP → Post-loop materialize + validate + flips → Return).

# Recipe

1. **Parse args** — 1st = `SLUG` (bare master-plan slug, e.g. `blip`); 2nd = `STAGE_ID` (e.g. `5` or `Stage 5` or `7.2`); optional 3rd = `ISSUE_PREFIX` (`TECH` / `FEAT` / `BUG` / `ART` / `AUDIO`, default `TECH`).
2. **Phase 0 — Mode detection** — Scan Stage task table before any action. File / Compress / Mixed / No-op routes per SKILL.md §Phase 0.
3. **Phase 1 — Load Stage MCP bundle** — Single `mcp__territory-ia__lifecycle_stage_context` call (fallback `domain-context-load`). Do NOT re-run per Task.
4. **Phase 2 — Stage block + gates** — Read `### Stage {STAGE_ID}` (H3 canonical; H4 legacy warn). Collect `_pending_` rows in table order. Run cardinality gate + sizing gate H1–H6. FAIL → HALT + `/stage-decompose` handoff.
5. **Phase 3 — Batch deps verify** — One `backlog_list({ids: [union]})` call. Unresolvable → HALT.
6. **Phase 4 — Resolve target manifest section** — Slug heuristic vs `ia/state/backlog-sections.json`; ambiguous → user prompt.
7. **Phase 5 — Per-task iterator** — For each `_pending_` Task: compose `task_insert` args + `raw_markdown` (Pass A null + Pass B backfill per SKILL.md §5.1a); call MCP; append manifest entry; persist spec stub body to DB via `task_spec_section_write`; record for post-loop.
8. **Phase 6 — Post-loop: materialize + validate + flips** — Short-circuit when `filed_tasks.length === 0` (every Task hit idempotent skip → zero new DB rows AND zero manifest appends): SKIP `materialize-backlog.sh` + `validate:dead-project-specs`; emit `materialize=skipped (no-op)`. Otherwise: `bash tools/scripts/materialize-backlog.sh` (DB source default) + `npm run validate:dead-project-specs`. Then atomic task-table Edit + R2 Stage Status flip + R1 plan-top Status flip + non-blocking `npm run progress`.
9. **Phase 7 — Return to dispatcher** — Single caveman block with STAGE_ID / FILED / SKIPPED / ids / section / validators / `next=stage-file-chain-continue`.

# Hard boundaries

- Do NOT write yaml under `ia/backlog/` — DB is source of truth.
- Do NOT call `reserve-id.sh` — per-prefix DB sequences own id assignment via `task_insert` MCP.
- Do NOT read or edit master-plan markdown on disk — DB is source of truth.
- Do NOT re-query `backlog_issue` per Task — Phase 3 batch-verified.
- Do NOT reorder Tasks — apply in task-table order.
- Do NOT update task-table mid-loop — atomic Edit after Phase 6.1+6.2 exit 0.
- Do NOT edit `BACKLOG.md` directly — `materialize-backlog.sh` regenerates from DB + manifest.
- Do NOT run `validate:backlog-yaml` — no yaml written on DB path.
- Do NOT run `validate:all` — gate is `validate:dead-project-specs` only.
- Do NOT emit user-facing `/ship-stage` or `/ship` handoff — dispatcher owns post-chain handoff.
- Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
- Do NOT `rm -rf` or delete any existing file.
- Do NOT commit — user decides.

# Escalation shape

`{escalation: true, phase: N, reason: "...", candidate_matches?: [...], stderr?: "..."}` — returned to dispatcher. See SKILL.md §Escalation rules for full trigger list (cardinality pause, sizing FAIL, dep not found, task_insert unique/sequence, manifest ambiguous, materialize non-zero, validator non-zero, R2 self-check miss).

# Output

Single caveman block returned to `/stage-file` dispatcher (not user). Shape:

```
stage-file done. STAGE_ID={STAGE_ID} FILED={N} SKIPPED={K}
Filed: {ISSUE_ID_1} — {title_1}
       {ISSUE_ID_2} — {title_2}
       ...
Section: {TARGET_SECTION_HEADER}
Materialize: {ran|skipped (no-op)}
Validators: exit 0.
next=stage-file-chain-continue
```

On escalation: JSON `{escalation: true, phase, reason, ...}` payload.
