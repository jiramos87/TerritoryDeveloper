---
purpose: "TECH-476 — Rewrite lifecycle rule docs — agent-lifecycle.md + docs/agent-lifecycle.md (Stage 7 T7.9)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.9"
---
# TECH-476 — Rewrite lifecycle rule docs — agent-lifecycle.md + docs/agent-lifecycle.md (Stage 7 T7.9)

> **Issue:** [TECH-476](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Rewrite both canonical lifecycle docs. `ia/rules/agent-lifecycle.md` gets new ordered flow (plan-author Stage 1×N + plan-review between stage-file-apply + first implement; opus-audit Stage 1×N + opus-code-review between verify-loop + stage-closeout); Plan-Apply pair first-class hard rule section; surface map 12 new rows. `docs/agent-lifecycle.md` gets updated flow diagram + stage→surface matrix + handoff contract table.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Ordered flow reflects rev 3 end-segment (Stage-end batch stages).
2. Plan-Apply pair hard rule section present.
3. Surface map table rows consistent with filed agents + commands.
4. docs/agent-lifecycle.md Mermaid or ASCII flow diagram updated.
5. Handoff contract table lists new pair seams.

### 2.2 Non-Goals

1. Rule + doc content about other subsystems (verification policy, invariants).
2. CLAUDE.md / AGENTS.md edits (T7.10 / TECH-477).

## 4. Current State

### 4.2 Systems map

- `ia/rules/agent-lifecycle.md` — current surface map 13 rows.
- `docs/agent-lifecycle.md` — canonical full doc (flow + matrix + handoff).
- Filed Stage 7 agents + commands = new row sources.

## 7. Implementation Plan

### Phase 1 — Rewrite `ia/rules/agent-lifecycle.md` flow + hard-rule section + surface map

### Phase 2 — Rewrite `docs/agent-lifecycle.md` flow diagram + matrix + contract

### Phase 3 — Validate

## 8. Acceptance Criteria

- [ ] Both docs reflect new 2-level Stage/Task flow + pair pattern + Stage-scoped bulk.
- [ ] Surface map consistent with filed agents/commands.
- [ ] Plan-Apply pair hard rule section present.
- [ ] `npm run validate:all` exit 0.

## Open Questions

1. None — tooling only.
