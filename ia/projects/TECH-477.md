---
purpose: "TECH-477 — Stage 7 validate + memory update + M6 flip (Stage 7 T7.10)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.10"
---
# TECH-477 — Stage 7 validate + memory update + M6 flip (Stage 7 T7.10)

> **Issue:** [TECH-477](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Final Stage 7 closeout task. Run `npm run validate:all`; fix any failures from command/agent dispatch references to retired surfaces. Update `AGENTS.md` lifecycle section to match new surface map (T7.9). Update MEMORY entries in `MEMORY.md` referencing legacy Phase / kickoff / closeout patterns. Flip migration JSON M6 `done`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `validate:all` green.
2. `AGENTS.md` lifecycle section aligned with new surface map.
3. MEMORY entries updated (no legacy Phase / kickoff / closeout dispatch references).
4. `ia/state/lifecycle-refactor-migration.json` M6 = `done`.

### 2.2 Non-Goals

1. Stage 8 dry-run execution (separate stage).
2. Freeze removal (Stage 9).

## 4. Current State

### 4.2 Systems map

- All Stage 7 filed tasks (T7.1–T7.9, T7.11–T7.14) must reach Done before this runs.
- `ia/state/lifecycle-refactor-migration.json` M6 gate.
- `AGENTS.md` §Lifecycle = edit target.
- `MEMORY.md` root-scoped = legacy reference scan target.

## 7. Implementation Plan

### Phase 1 — Run validate:all + triage

### Phase 2 — AGENTS + MEMORY sync

### Phase 3 — Flip M6 done

## 8. Acceptance Criteria

- [ ] `npm run validate:all` exit 0.
- [ ] `AGENTS.md` lifecycle section aligned.
- [ ] No MEMORY entries reference legacy Phase / kickoff / closeout patterns.
- [ ] Migration JSON M6 = `done`.

## Open Questions

1. None — tooling only.
