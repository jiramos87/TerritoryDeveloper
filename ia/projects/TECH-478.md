---
purpose: "TECH-478 — Plan-author skill + agent + /author command — Stage-scoped bulk; canonical-term fold (Stage 7 T7.11)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.11"
---
# TECH-478 — Plan-author skill + agent + /author command — Stage-scoped bulk; canonical-term fold (Stage 7 T7.11)

> **Issue:** [TECH-478](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Introduce Opus **Stage-scoped bulk non-pair** `plan-author` stage — sits between `stage-file-apply` (multi-task) / `project-new-apply` (N=1) and `plan-review`. One Opus pass per Stage reads all N spec stubs + Stage header + shared MCP bundle + invariants + glossary; writes all N `§Plan Author` sections (4 sub-sections each: Audit Notes, Examples, Test Blueprint, Acceptance). Same pass enforces canonical glossary terms across `§Objective` / `§Background` / `§Implementation Plan` — absorbs retired `spec-enrich` responsibility. Phase 0 guardrail: split into ⌈N/2⌉ bulk sub-passes if input exceeds Opus context threshold; never per-Task regression.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/plan-author/SKILL.md` — Opus Stage-scoped bulk non-pair; 4-sub-section output contract; canonical-term fold; Phase 0 token-split guardrail.
2. `.claude/agents/plan-author.md` — Opus agent; caveman preamble; MCP allowlist (router, glossary_discover/lookup, invariants_summary, spec_section/sections, backlog_issue, master_plan_locate).
3. `.claude/commands/author.md` — `/author {MASTER_PLAN_PATH} {STAGE_ID}` Stage-scoped dispatcher + `--task {ISSUE_ID}` escape hatch; auto-invoked by `/stage-file` + `/project-new`.
4. `ia/templates/project-spec-template.md` — new `§Plan Author` section + 4 sub-section stubs between `§Verification` + `§Audit`.
5. `ia/rules/plan-apply-pair-contract.md` updated: plan-author = Stage-scoped bulk non-pair; 4 pair seams remain; drop project-new-plan→project-new-apply + audit→closeout-apply entries.
6. `ia/rules/agent-lifecycle.md` ordered flow updated (plan-author inserted multi-task + N=1 paths).

### 2.2 Non-Goals

1. Spec-enrich skill authoring (never exists).
2. Per-Task closeout surfaces (Stage-level in T7.13 / T7.14).

## 4. Current State

### 4.2 Systems map

- `ia/skills/stage-file-apply/` (TECH-469) + `ia/skills/project-new-apply/` (TECH-470) = upstream handoff.
- `ia/skills/plan-review/` (TECH-468) = downstream consumer.
- Template §Plan Author anchor target = `ia/templates/project-spec-template.md`.
- Shared Stage MCP bundle = `ia/skills/domain-context-load/SKILL.md`.

## 7. Implementation Plan

### Phase 1 — Author plan-author SKILL.md (non-pair contract + 4 sub-sections + token-split guardrail)

### Phase 2 — Agent markdown + `/author` command

### Phase 3 — Template edit + pair-contract + agent-lifecycle rule updates

### Phase 4 — Validate

## 8. Acceptance Criteria

- [ ] plan-author SKILL.md + agent + command present; `phases:` frontmatter.
- [ ] Template `§Plan Author` section + 4 sub-sections in order.
- [ ] Pair-contract rule reflects non-pair status + 4 remaining seams + dropped seams.
- [ ] agent-lifecycle rule ordered flow updated.
- [ ] `npm run validate:all` exit 0.

## Open Questions

1. None — tooling only. Resolves Open Q7 + S7 (exploration §Design Expansion — plan-author + progress-emit + rev 3 stage-end bulk fold).
