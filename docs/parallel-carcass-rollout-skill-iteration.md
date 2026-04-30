# Parallel Carcass Rollout — Skill Iteration Summary

Wave 1 dogfood skill-train pass. 5 lifecycle skills retrospected: `master-plan-new`, `stage-decompose`, `ship-stage`, `section-claim`, `section-closeout`. Scan window: 2026-04-22 → 2026-04-29.

---

## Top-3 Frictions

All 5 proposals returned `friction_count: 0` at threshold 2 — no recurring friction above threshold. Top observations (below-threshold signals worth tracking):

| # | Observation | Skill | Recurrence | Source proposal |
|---|-------------|-------|------------|-----------------|
| 1 | `missing_task_preseed_step` — Phase 7 task pre-seed via `task_insert` was missing; `/stage-file` halted with `pending=0`. Fixed on 2026-04-29 via Guardrail addition. | `master-plan-new` | 1 | `ia/skills/master-plan-new/proposed/2026-04-29-train.md` |
| 2 | No `source: self-report` emitter stanza wired in any of the 5 skills — future skill-train passes return 0 signals until stanzas are added to Phase tail. Cross-skill structural gap. | all 5 | 5 (structural) | all 5 proposals |
| 3 | V2 session-id / worktree / sentinel drop was a rapid 3-way convergence (`section-claim`, `section-closeout`, `ship-stage` all updated same date). Indicates the original V1 design assumption (per-session identity) was wrong. Signal: architecture pivots should wire emitter stanzas before shipping. | `ship-stage`, `section-claim`, `section-closeout` | 1 each | `ia/skills/ship-stage/proposed/2026-04-29-train.md`, `section-claim`, `section-closeout` |

---

## Proposed Fixes

| Fix | Target | Priority |
|-----|--------|----------|
| Wire `source: self-report` emitter stanza to each skill's Phase-N tail (per `skill-train` §Emitter stanza template). Focus: `master-plan-new` Phase 10, `ship-stage` Phase 9, `stage-decompose` Phase 5, `section-claim` recipe tail, `section-closeout` recipe tail. | all 5 skills | Medium — deferred to Wave 2 |
| Add emitter stanza firing condition for architecture-pivot changesets (when V2/V3 protocol rewrites land, log `architecture_pivot` friction_type). | `ship-stage`, `section-claim`, `section-closeout` | Low |

---

## Accepted Edits

No friction reached recurrence ≥ 2 threshold — no SKILL.md Guardrail rows written, no `ia/rules/*.md` edits made.

| Skill | Edit | Kind |
|-------|------|------|
| `master-plan-new` | `train-proposed` pointer entry appended to `## Changelog` | changelog-pointer |
| `stage-decompose` | `train-proposed` pointer entry appended to `## Changelog` | changelog-pointer |
| `ship-stage` | `train-proposed` pointer entry appended to `## Changelog` | changelog-pointer |
| `section-claim` | `train-proposed` pointer entry appended to `## Changelog` | changelog-pointer |
| `section-closeout` | `train-proposed` pointer entry appended to `## Changelog` | changelog-pointer |

---

## Carryovers

| Carryover | Reason |
|-----------|--------|
| `emitter_stanza_missing`: not yet captured because no Phase-tail emitter stanzas exist in the 5 skills — structured self-report requires explicit wiring first. | Wire stanzas in Wave 2 skill-sync pass before next skill-train run. |
| `architecture_pivot` friction type: not yet captured because V2 rewrites happened before emitter stanzas were in place. | Retrospectively documented in proposals; no recurrence count available. |
