---
purpose: "TECH-480 — Stage-closeout-plan skill + planner agent + master-plan template §Stage Closeout Plan (Stage 7 T7.13)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.13"
---
# TECH-480 — Stage-closeout-plan skill + planner agent + master-plan template §Stage Closeout Plan (Stage 7 T7.13)

> **Issue:** [TECH-480](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Land Opus pair-head of Stage-end bulk closeout. `stage-closeout-plan` runs once per Stage when all Tasks reach `Done` post-verify — reads master-plan Stage header + all Task `§Audit` paragraphs + all Task §Implementation/§Findings/§Verification + invariants + glossary; writes `§Stage Closeout Plan` section in master plan (unified tuple list: shared glossary rows, rule section edits, doc paragraph edits, plus N BACKLOG archive ops + N id purges + N spec deletes + N master-plan task-row status flips + N per-task digest emissions). Anchor resolution complete before handing off to applier (T7.14). Idempotent on re-run.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/stage-closeout-plan/SKILL.md` — Opus pair-head; full tuple-shape contract; idempotent.
2. `.claude/agents/stage-closeout-planner.md` — Opus agent; caveman preamble; MCP allowlist.
3. `ia/templates/master-plan-template.md` — `§Stage Closeout Plan` section stub per Stage.
4. `ia/rules/plan-apply-pair-contract.md` — new `stage-closeout-plan → stage-closeout-apply` seam row.
5. `phases:` frontmatter on SKILL.md.

### 2.2 Non-Goals

1. Sonnet applier (T7.14 / TECH-481).
2. `/closeout` command rewire (T7.14).
3. MCP tool rename (T7.14).

## 4. Current State

### 4.2 Systems map

- TECH-471 `opus-audit` §Audit output = upstream source.
- TECH-481 `stage-closeout-apply` = pair-tail consumer.
- `ia/templates/master-plan-template.md` (TECH-444) — template section insertion target.

## 7. Implementation Plan

### Phase 1 — Author stage-closeout-plan SKILL.md

### Phase 2 — Planner agent + template section + pair-contract seam

### Phase 3 — Validate

## 8. Acceptance Criteria

- [ ] SKILL.md + planner agent + template stub + seam present.
- [ ] `phases:` frontmatter.
- [ ] `npm run validate:all` exit 0.

## Open Questions

1. None — tooling only. Extension source exploration doc §Design Expansion rev 2 (stage-end bulk closeout).
