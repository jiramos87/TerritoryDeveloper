---
purpose: "TECH-474 — Author all new agents — 10 new .claude/agents/*.md + retire 2 (Stage 7 T7.7)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.7"
---
# TECH-474 — Author all new agents — 10 new .claude/agents/*.md + retire 2 (Stage 7 T7.7)

> **Issue:** [TECH-474](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Author 10 new `.claude/agents/*.md` subagent bodies matching the pair + bulk skills authored earlier in Stage 7. Retire `spec-kickoff.md` + Opus `closeout.md` under `_retired/`. Common preamble `@`-loads `subagent-progress-emit/SKILL.md` (T7.12 wiring). `spec-enricher.md` NOT authored.

## 2. Goals and Non-Goals

### 2.1 Goals

1. 10 new agent markdown files: `plan-reviewer.md`, `plan-fix-applier.md`, `stage-file-planner.md`, `stage-file-applier.md`, `project-new-planner.md`, `project-new-applier.md`, `opus-auditor.md`, `opus-code-reviewer.md`, `code-fix-applier.md`, `closeout-applier.md`.
2. 2 retired agents moved under `.claude/agents/_retired/`: `spec-kickoff.md`, `closeout.md` (Opus).
3. Each new agent: caveman preamble + SKILL.md reference + model tier header; Opus agents list MCP allowlist; Sonnet agents cite pair-contract rule.

### 2.2 Non-Goals

1. `plan-author.md` agent (authored in T7.11 / TECH-478).
2. `stage-closeout-planner.md` + `stage-closeout-applier.md` (authored in T7.13 / T7.14).
3. Slash command files (T7.8 / TECH-475).

## 4. Current State

### 4.2 Systems map

- `.claude/agents/*.md` — current 13 subagent surface; refactor reshapes to ~15+ with pair split.
- `ia/skills/subagent-progress-emit/SKILL.md` (TECH-479) = common-preamble `@`-load target.
- Existing `spec-kickoff.md` + Opus `closeout.md` — retire targets.

## 7. Implementation Plan

### Phase 1 — Author 10 new agent markdowns

### Phase 2 — Move 2 retired agents + common-preamble edit

### Phase 3 — Validate

## 8. Acceptance Criteria

- [ ] 10 new agent markdown files present.
- [ ] 2 retired agents moved under `_retired/`.
- [ ] Common preamble `@`-loads progress-emit skill.
- [ ] `npm run validate:all` exit 0.

## Open Questions

1. None — tooling only. Common-preamble include shape resolved by T7.12 dependency.
