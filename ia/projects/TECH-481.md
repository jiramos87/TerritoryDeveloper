---
purpose: "TECH-481 — Stage-closeout-apply skill + applier agent + /closeout rewire + template drop + MCP rename + project-stage-close retire + M6 Phase 8 flip (Stage 7 T7.14)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.14"
---
# TECH-481 — Stage-closeout-apply skill + applier agent + /closeout rewire + template drop + MCP rename + project-stage-close retire + M6 Phase 8 flip (Stage 7 T7.14)

> **Issue:** [TECH-481](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Final Stage 7 surface piece. Sonnet `stage-closeout-apply` pair-tail executes Stage-scoped unified closeout (glossary/rule/doc shared edits once + loop N archive/delete/flip/purge + one `materialize-backlog.sh` + one `validate:dead-project-specs` + one Stage-level digest + Stage → Final rollup via R5 gate). `/closeout` command rewired Stage-scoped. Per-Task `§Closeout Plan` section dropped from spec template (replaced by Stage-level `§Stage Closeout Plan`). MCP tool `project_spec_closeout_digest` → `stage_closeout_digest` rename. Legacy `project-stage-close` skill retired. `ia/rules/agent-lifecycle.md` end-segment rewritten to rev 3 flow. Migration JSON M6 Phase 8 flip.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/stage-closeout-apply/SKILL.md` — Sonnet pair-tail; unified bulk contract; 1-retry bound escalation; Stage → Final rollup.
2. `.claude/agents/stage-closeout-applier.md` — Sonnet agent; caveman preamble; MCP allowlist.
3. `.claude/commands/closeout.md` rewired Stage-scoped (`{MASTER_PLAN_PATH} {STAGE_ID}`); no per-Task path.
4. `ia/templates/project-spec-template.md` — `§Closeout Plan` section dropped.
5. MCP tool renamed `project_spec_closeout_digest` → `stage_closeout_digest`; handler emits Stage-level digest; schema cache restarted; call sites updated.
6. `ia/skills/_retired/project-stage-close/` tombstone.
7. `ia/rules/agent-lifecycle.md` end-segment rewritten (drop per-Task `/closeout` + `/kickoff` + `/enrich` rows; add Stage-scoped `/closeout` + `/author` + `/audit` rows; multi-task + single-task flows in §Ordered flow).
8. `ia/state/lifecycle-refactor-migration.json` M6 Phase 8 = `done`.

### 2.2 Non-Goals

1. Running actual closeout on any existing spec (Stage 8 dry-run / Stage 9 merge).
2. Stage 10 optimization layer (post-merge).

## 4. Current State

### 4.2 Systems map

- TECH-480 `stage-closeout-plan` = pair-head upstream.
- TECH-471 `opus-audit` §Audit = upstream feed.
- TECH-458 MCP surface = rename base.
- `tools/mcp-ia-server/src/tools/project-spec-closeout-digest.*` = rename targets.
- `ia/skills/project-stage-close/` = retire target.
- `ia/rules/agent-lifecycle.md` = end-segment rewrite target.
- `.claude/commands/closeout.md` + `ia/templates/project-spec-template.md` = edit targets.

## 7. Implementation Plan

### Phase 1 — Author stage-closeout-apply SKILL.md + applier agent

### Phase 2 — Rewire `/closeout` Stage-scoped + drop §Closeout Plan template section

### Phase 3 — MCP tool rename + schema cache restart + call-site fix

### Phase 4 — Retire project-stage-close + rewrite agent-lifecycle end-segment

### Phase 5 — Validate + flip M6 P8 done

## 8. Acceptance Criteria

- [ ] Applier SKILL.md + agent + rewired command + template drop + MCP rename + retired skill + rule rewrite all in place.
- [ ] `phases:` frontmatter on SKILL.md.
- [ ] `npm run validate:all` exit 0.
- [ ] Migration JSON M6 Phase 8 = `done`.

## Open Questions

1. None — tooling only. Extension sources exploration doc §Design Expansion rev 2 (stage-end bulk closeout) + rev 3 (stage-end bulk plan-author + audit + spec-enrich fold).
