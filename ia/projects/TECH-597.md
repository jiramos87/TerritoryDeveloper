---
purpose: "TECH-597 — Umbrella rollout-tracker alignment check."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/zone-s-economy-master-plan.md"
task_key: "T9.5"
---
# TECH-597 — Umbrella rollout-tracker alignment check

> **Issue:** [TECH-597](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Close-the-loop check vs umbrella rollout tracker: Bucket 3 alignment before program calls column (f) done elsewhere.

## 2. Goals and Non-Goals

### 2.1 Goals

- Tracker row state documented; mismatches filed or noted for umbrella owner.
- Explicit note that column (f) tick is out of scope for this task.

### 2.2 Non-Goals (Out of Scope)

1. Editing umbrella tracker column (f) in this task.

## 4. Current State

### 4.2 Systems map

- `ia/projects/full-game-mvp-rollout-tracker.md`, `ia/projects/zone-s-economy-master-plan.md`

## 6. Decision Log

- **Row slug:** `zone-s-economy` (Bucket 3, Order 1) — read `ia/projects/full-game-mvp-rollout-tracker.md` rollout matrix row.
- **(a)–(d):** Tracker shows ✓ — enumerate, exploration, child plan, stages present; matches repo.
- **(e) Decomp:** Tracker still ◐ with parenthetical citing glossary + `economy-system.md` pending — prose is pre–Stage 9. Repo now has fully decomposed `ia/projects/zone-s-economy-master-plan.md` through Stage 9; umbrella may refresh cell text when convenient.
- **(f) Filed:** Tracker ✓ — Stage 1.1 tasks cited; **this task did not edit column (f)** per scope.
- **(g) Align (repo reality after TECH-593–596):** `ia/specs/economy-system.md` exists; glossary rows (**Zone S**, **ZoneSubTypeRegistry**, **ZoneSService**, **envelope (budget sense)**, **TreasuryFloorClampService**, **BudgetAllocationService**, **IBudgetAllocator**, **BondLedgerService**, **IBondLedger**, **IMaintenanceContributor**) link to `economy-system.md` anchors; `ia/rules/agent-router.md` includes economy / Zone S row → `economy-system.md`; `tools/mcp-ia-server/data/spec-index.json` regenerated. Umbrella owner may flip (g) to ✓ and shorten (e) parenthetical after review — **not done in this issue**.

## 7. Implementation Plan

### Phase 1 — Read tracker + compare to repo

- [ ] Bucket 3 row: (a)–(e), (g) vs actual docs/specs.

### Phase 2 — Write findings into Task spec §Verification / Decision Log as needed

- [ ] Document gaps for umbrella; no column (f) tick.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Doc alignment | Human / agent | Tracker markdown + repo paths | |

## 8. Acceptance Criteria

- [ ] Tracker row state documented; mismatches filed or noted for umbrella owner.
- [ ] Explicit note that column (f) tick is out of scope for this task.

## 10. Lessons Learned

<!-- Populate at task completion for closeout migration. -->

## §Plan Author

### §Audit Notes

- Read-only vs umbrella process: do **not** edit column (f); document only. Align gate (g) requires spec + router + glossary — satisfied only after TECH-593–595/596 per program rules.
- Bucket 3 row label must match `full-game-mvp-rollout-tracker.md` naming — read file header for exact slug.

### §Examples

| Column | Check |
|--------|--------|
| (a)–(e) | Prior lifecycle cells complete for Zone S economy row |
| (g) | Glossary + router + spec point at `economy-system.md` |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| tracker_read | Bucket 3 row markdown | State summarized in §10 or Decision Log | manual |
| gap_list | mismatch vs repo | Bullet list in spec body | manual |

### §Acceptance

- [ ] Short written summary of tracker vs repo alignment attached in §6 Decision Log or §10.
- [ ] Statement that column (f) not modified by this task.

### §Findings

## Open Questions (resolve before / during implementation)

1. None — read-only verification vs tracker.

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
