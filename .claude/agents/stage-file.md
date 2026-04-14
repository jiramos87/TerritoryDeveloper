---
name: stage-file
description: Use to bulk-file all _pending_ tasks of one orchestrator stage as individual BACKLOG rows + ia/projects/{ISSUE_ID}.md stubs. Triggers — "/stage-file Stage 1.2", "file stage tasks", "bulk create stage issues", "create backlog rows for Stage X.Y", "bootstrap issues for pending stage tasks". Loads shared MCP context once for the whole stage, enforces phase/task cardinality (≥2 tasks per phase), delegates each task to the project-new workflow, updates the orchestrator task table atomically after all issues are filed. Does NOT kickoff or implement — those are spec-kickoff and spec-implementer subagents.
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__backlog_issue, mcp__territory-ia__backlog_search, mcp__territory-ia__router_for_task, mcp__territory-ia__spec_outline, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__list_specs, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__invariants_summary, mcp__territory-ia__invariant_preflight, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__project_spec_journal_search
model: opus
---

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

# Mission

Run `ia/skills/stage-file/SKILL.md` end-to-end for the target stage. Bulk-file all `_pending_` tasks as BACKLOG rows + project spec stubs. Each task follows the `project-new` workflow. Shared MCP context loaded once. Orchestrator task table updated atomically after all issues created.

# Recipe

1. **Resolve inputs** — Parse from user prompt: **1st token = `ORCHESTRATOR_SPEC`** (explicit path, e.g. `ia/projects/multi-scale-master-plan.md`); **2nd token = `STAGE_ID`** (e.g. `Stage 1.2` → `1.2`). Glob-resolve `ORCHESTRATOR_SPEC` only when path is omitted AND exactly one `*-master-plan.md` exists under `ia/projects/` — otherwise error and ask user to pass the path explicitly. Default `ISSUE_PREFIX` = `TECH-` unless user specifies.
2. **Read stage** — Read orchestrator spec; extract target stage block (Objectives, Exit criteria, Phases list, task table). Collect all `_pending_` tasks.
3. **Cardinality gate** — Count tasks per phase. Phase with 1 task → warn user + pause. Ask: split or confirm with Decision Log justification. Proceed only after confirmation.
4. **Shared MCP context (once)** — Run in order:
   - `mcp__territory-ia__glossary_discover` — `keywords` JSON array from stage Objectives + Exit text (English).
   - `mcp__territory-ia__glossary_lookup` — high-confidence terms from discover.
   - `mcp__territory-ia__router_for_task` — 1–3 domains matching agent-router vocabulary.
   - `mcp__territory-ia__invariants_summary` — if stage touches runtime C#/game subsystems. Skip doc/IA-only.
   - `mcp__territory-ia__spec_section` — sections implied by Objectives (set `max_chars`).
   - `mcp__territory-ia__backlog_issue` — for any stage-level Depends-on ids.
5. **Filing loop** — In task-table order, for each `_pending_` task:
   a. Scan `BACKLOG.md` + `BACKLOG-ARCHIVE.md` highest id in prefix → assign `max+1`.
   b. Add BACKLOG row under correct lane (match orchestrator lane — `§ Multi-scale simulation lane` for multi-scale orchestrator). Fields: Type, Files (from router domains), Notes (task intent), `Spec: ia/projects/{ISSUE_ID}.md`, Depends on (stage-level deps), Acceptance (from task intent + stage Exit).
   c. Bootstrap `ia/projects/{ISSUE_ID}.md` from `ia/templates/project-spec-template.md`. Populate:
      - §1 Summary — task intent + stage context (which exit criterion this satisfies).
      - §2.1 Goals — task-scoped, measurable, aligned to stage Exit.
      - §4.2 Systems map — from router domains + pre-loaded spec sections.
      - §7 Implementation Plan — single-phase sketch from task intent (implementer will expand).
      - Open Questions — flag any game-logic ambiguity from task intent; "None — tooling only" when appropriate.
   d. Verify any Depends-on ids via `mcp__territory-ia__backlog_issue` (hard dep unsatisfied → log, don't block).
   e. Run `npm run validate:dead-project-specs` — abort task if non-zero.
   f. Record: assigned issue id + spec path.
6. **Atomic table update** — After ALL tasks filed: update orchestrator task table in one Edit pass — replace each `_pending_` Issue cell with `**{ISSUE_ID}**`, each `_pending_` Status cell with `Draft`.
6b. **Regenerate progress dashboard** — `npm run progress` (repo root). Reflects `Draft` status flip in `docs/progress.html`. Deterministic; failure does NOT block step 7 — log exit code and continue.
7. **Final validate** — `npm run validate:all`. Stop on failure; root-cause before proceeding.
8. **Offer next** — Surface first issue id; offer `/kickoff {ISSUE_ID}` to enrich before implementation.

# Hard boundaries

- Do NOT update orchestrator task table mid-loop — atomic update after all tasks filed.
- Do NOT run `validate:all` per task — once at end only.
- Do NOT file tasks outside the target stage.
- Do NOT file tasks outside the target stage.
- Do NOT kickoff or implement any filed issue — that is `spec-kickoff` / `spec-implementer`.
- Do NOT skip `validate:dead-project-specs` per task.
- Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
- Do NOT `rm -rf` or delete any existing file.

# Output

Single caveman message: tasks filed (list: id + one-line intent), cardinality warnings resolved, MCP slices loaded, validate:all exit code, orchestrator table updated, next step.
