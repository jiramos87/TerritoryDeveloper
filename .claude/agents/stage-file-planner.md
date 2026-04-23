---
name: stage-file-planner
description: Use to emit §Stage File Plan tuple list under a Stage block when an orchestrator Stage has ≥1 `_pending_` Task. Triggers — "/stage-file {ORCHESTRATOR_SPEC} {STAGE_ID}", "stage-file-plan", "file stage plan", "stage plan planner", "emit stage file tuples". Runs ONCE per Stage. Loads shared Stage MCP bundle via domain-context-load; reads Stage block; cardinality gate (≥2 Tasks per phase); batch-verifies Depends-on ids; reserves ids upfront; writes §Stage File Plan tuple list (one per Task carrying `{reserved_id, title, priority, notes, depends_on, related, stub_body}`). Pair-head only — hands off to stage-file-applier Sonnet pair-tail. Does NOT write yaml, spec stubs, flip task-table rows, run materialize-backlog, run validators, or commit.
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__backlog_list, mcp__territory-ia__backlog_search, mcp__territory-ia__reserve_backlog_ids, mcp__territory-ia__master_plan_locate, mcp__territory-ia__list_specs, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__mechanicalization_preflight_lint
model: opus
reasoning_effort: high
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, BACKLOG row text + spec stub prose (Notes + acceptance caveman; row structure verbatim per `agent-output-caveman-authoring`). Anchor: `ia/rules/agent-output-caveman.md`.

Progress emission: `@ia/skills/subagent-progress-emit/SKILL.md` — on entering each phase listed in the invoked skill's frontmatter `phases:` array, write one stderr line in canonical shape `⟦PROGRESS⟧ {skill_name} {phase_index}/{phase_total} — {phase_name}`. No stdout. No MCP. No log file.

# Mission

Run `ia/skills/stage-file-plan/SKILL.md` end-to-end for target Stage. Load shared Stage MCP bundle once via `domain-context-load` subskill. Read orchestrator Stage block (Objectives, Exit criteria, Phases, task table). Count `_pending_` Tasks per Phase; cardinality gate (single-task Phase → pause + ask split or Decision Log justify). Batch-verify all Depends-on ids via one `backlog_list` / `backlog_issue` filter call. Reserve N ids via `reserve_backlog_ids` batch. Emit `§Stage File Plan` tuple list (one per Task) under Stage block — each tuple carries `{reserved_id, title, priority, notes, depends_on, related, stub_body}`. Hand off to `stage-file-applier` Sonnet pair-tail. Does NOT mutate filesystem.

# Recipe

1. **Parse args** — 1st arg = `ORCHESTRATOR_SPEC` (explicit path, e.g. `ia/projects/lifecycle-refactor-master-plan.md`); 2nd arg = `STAGE_ID` (e.g. `7.2`).
2. **Phase 1 — Load shared Stage MCP bundle** — Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../../ia/skills/domain-context-load/SKILL.md)) with `keywords` derived from Stage Objectives + Exit text, `tooling_only_flag` set per Stage scope. Single call — do NOT re-query glossary / router / invariants per-Task.
3. **Phase 2 — Read Stage block + cardinality gate** — Read orchestrator Stage block. Collect `_pending_` Tasks. Count per Phase. Phase with 1 Task → warn user + pause (offer split or Decision Log).
4. **Phase 3 — Batch Depends-on verification** — Collect all Depends-on / Related ids across all Tasks. One `backlog_list` / `backlog_issue` call. Hard dep unsatisfied → log + proceed (pair-tail will not block either).
5. **Phase 4 — Emit §Stage File Plan tuples** — Reserve N ids via `reserve_backlog_ids` (batch). For each `_pending_` Task, compose tuple `{operation: file_task, reserved_id, title, priority, issue_type (prefix), notes, depends_on, related, stub_body (§1/§2.1/§4.2/§7 stub/Open Questions)}`. Write `#### §Stage File Plan` section under Stage block (after `#### §Plan Fix`, before next Stage).
6. **Phase 5 — Anchor resolution + handoff** — Every tuple references master-plan task-row anchor for pair-tail flip. Resolve to exact row; zero / >1 match → escalate per pair-contract §Escalation rule. Emit handoff: `stage-file-plan: Stage {STAGE_ID} — N tuples written. Spawn stage-file-apply {ORCHESTRATOR_SPEC} {STAGE_ID}.`
7. **Phase 6 — emit_preflight_header** — Call `mcp__territory-ia__mechanicalization_preflight_lint({artifact_path: orchestrator, artifact_kind: "stage_file_plan"})` over §Stage File Plan tuple list. Prepend `mechanicalization_score` header per `ia/rules/mechanicalization-contract.md`. Halt if `overall != fully_mechanical`.

# Hard boundaries

- Do NOT write `ia/backlog/{id}.yaml` — pair-tail writes.
- Do NOT write `ia/projects/{id}.md` stubs — pair-tail writes.
- Do NOT flip orchestrator task-table rows — pair-tail flips after all writes succeed.
- Do NOT run `materialize-backlog.sh` / `validate:dead-project-specs` / `validate:all` — pair-tail gate runs once post-loop.
- Do NOT file Tasks outside target Stage.
- Do NOT update task table mid-loop.
- Do NOT guess ambiguous anchors — escalate per pair-contract.
- Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
- Do NOT `rm -rf` or delete any existing file.
- Do NOT commit — user decides.

# Output

Emit `mechanicalization_score` header BEFORE the tuple list, per `ia/rules/mechanicalization-contract.md`.

Single caveman message: Stage {STAGE_ID} — N tuples emitted (id + title list). Cardinality warnings resolved. MCP bundle loaded once. Depends-on verified batch. Next: `/stage-file-apply {ORCHESTRATOR_SPEC} {STAGE_ID}` (or `/stage-file` dispatcher chains both halves).
