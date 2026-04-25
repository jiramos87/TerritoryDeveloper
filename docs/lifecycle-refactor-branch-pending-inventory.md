# Lifecycle Refactor — Branch Pending Inventory

> **Date:** 2026-04-19
> **Branch:** `feature/lifecycle-collapse-cognitive-split`
> **Scope:** Every change physically on this branch that awaits M8 freeze-lift before pickup.
> **Status:** Tracker doc. Read-only snapshot; refresh via re-run before each pickup.
> **Companion:** `docs/lifecycle-refactor-stage-8-dry-run-findings.md` (skill-improvement fix table).

---

## Dispatch prompt — for fresh post-M8 agent session

> **Gate:** Do NOT pick up any item below until:
> 1. Stage 9 T9.1 (TECH-489) user sign-off gate row written to migration JSON.
> 2. Stage 9 T9.3 (TECH-491) branch merged to main.
> 3. Stage 9 T9.4 (TECH-492) freeze note removed from `CLAUDE.md` §Key commands.
> After freeze lifts, work through sections B → C → D → E → F in order. Section A (dirty state) commits pre-freeze-lift as part of Stage 9 itself.

### Paste into fresh session after freeze lifts

```
Read docs/lifecycle-refactor-branch-pending-inventory.md end-to-end. Also read docs/lifecycle-refactor-stage-8-dry-run-findings.md (companion skill-fix table). Execute in order:

  Section B — Stage 9 tasks already filed as TECH-489..493. Ship via /ship-stage ia/projects/lifecycle-refactor-master-plan.md 9. Stop + wait for user LGTM at T9.1 before T9.3 merge.

  Section D — Findings-doc dispatch. Apply findings-doc Rows 1, 2, 3, 7 (high-priority skill fixes). See findings doc dispatch prompt for exact steps.

  Section C — Stage 10 cache layer. File TECH issues for T10.1..T10.8 via /stage-file ia/projects/lifecycle-refactor-master-plan.md 10. Requires T9.4 Q9 telemetry tracker operational + ≥3 post-merge Stages sampled. Gate via T10.1 before T10.2+ fire.

  Section E — Deferred exploration docs. For each doc in Section E table, decide pickup route (master-plan-extend vs master-plan-new vs defer-again) + dispatch per column "Intended pickup".

  Section F — Cross-plan escalations. Hand off to owning orchestrator owners — do NOT patch in lifecycle-refactor lane.

Respect ia/rules/invariants.md + glossary terminology + caveman output default. After each section run npm run validate:all + commit.
```

---

## A. Uncommitted dirty state (18 paths)

State captured at 2026-04-19 via `git status --short`. Commits as part of Stage 9 closeout — NOT deferred to post-freeze.

| Group | Files | Purpose | Next step |
|---|---|---|---|
| Stage 8 closeout residue | `ia/backlog-archive/TECH-485.yaml`, `TECH-486.yaml`, `TECH-487.yaml`, `TECH-488.yaml` | Archived dry-run task records | Commit during Stage 9 ship |
| Stage 8 artifacts | `docs/progress.html`, `ia/state/lifecycle-refactor-migration.json`, `ia/state/id-counter.json`, `ia/projects/lifecycle-refactor-master-plan.md` | Post-T8.4 M7 flip + progress regen + Stage 9/10 plan append | Commit during Stage 9 ship |
| Stage 9 filed stubs | `ia/backlog/TECH-489..493.yaml` (5) + `ia/projects/TECH-489..492.md` (4) | Stage 9 tasks already filed via stage-file | Commit during Stage 9 ship |
| Stage 9 stub gap | `ia/projects/TECH-493.md` (missing — yaml exists, spec stub not created) | Follow-up stub for ship-stage chain-journal | Create stub during Stage 9 T9.1 or treat as out-of-plan |
| New doc | `docs/lifecycle-refactor-stage-8-dry-run-findings.md` | 12 findings + fix table + dispatch prompt | Commit during Stage 9 ship |
| New doc | `docs/lifecycle-refactor-branch-pending-inventory.md` (this file) | Branch-pending tracker | Commit during Stage 9 ship |

---

## B. Stage 9 — Validation + Merge / Sign-Off + Merge (5 TECH issues filed)

All rows `Draft` status in master plan. Owner = refactor dispatcher.

| Task | Id | Title | Depends on | Priority |
|---|---|---|---|---|
| T9.1 | TECH-489 | User sign-off gate (M8.gate row) | — | high |
| T9.2 | TECH-490 | MCP restart + schema verify on post-merge main | TECH-491 | high |
| T9.3 | TECH-491 | Merge branch into main (no squash, merge commit) | TECH-489 | high |
| T9.4 | TECH-492 | Freeze close + token-cost telemetry tracker + Q9 baseline instrumentation | TECH-491 | medium |
| — | TECH-493 | Ship-stage chain-journal persistence follow-up | — | medium |

**Gate ordering:** TECH-489 (sign-off) blocks TECH-491 (merge); TECH-491 blocks TECH-490 + TECH-492 (post-merge main). TECH-493 = sidecar follow-up, not sign-off-critical.

---

## C. Stage 10 — Prompt-Caching Optimization Layer (Q9-gated, post-merge)

All tasks `_pending_` + unfiled (no TECH ids reserved yet). Precondition: Stage 9 T9.4 Q9 baseline must record ≥3 pair-head reads per Stage on ≥3 sampled post-merge Stages.

| Task | Title | Blocks |
|---|---|---|
| T10.1 | D1 reference doc + Q9 gate check | all T10.2+ |
| T10.2 | Tier 1 stable cross-Stage block + F2 sizing gate CI | T10.3 |
| T10.3 | Tier 2 per-Stage bundle + domain-context-load Phase N concat | T10.4 |
| T10.4 | F3 stagger + F5 tool-allowlist uniformity + B2 R11 retire | T10.5 |
| T10.5 | B4 unified `plan-applier` consolidation (replaces 3 legacy appliers) | T10.6 |
| T10.6 | R1 SSE cache-commit event gate + C4 progress-emit extension | T10.7 |
| T10.7 | D2 cascade note + D3 20-block guardrail note | T10.8 |
| T10.8 | P1 validation replay + sign-off + M9 flip | — |

**Reject path:** if Q9 median < 3 reads/Stage → close Stage 10 as `Rejected (P1 economics not viable)` + record in migration JSON M9.reject + file follow-up TECH noting cache layer not viable at observed read volume.

**Reference doc already landed:** `docs/prompt-caching-mechanics.md` (authored ahead of Stage 10 activation per D1 tier).

---

## D. Findings-doc dispatch queue

Source: `docs/lifecycle-refactor-stage-8-dry-run-findings.md` fix table. 12 findings (F1–F12, F8 retracted). 11 fix rows.

### High priority (post-M8 immediate pickup) — ✅ DONE (commit `1c448e4`, 2026-04-19)

| Row | Target skill | Fix summary | Status |
|---|---|---|---|
| Row 1 | `ia/skills/plan-author/SKILL.md` Phase 4 | Load retired-surface tombstones + template-section allowlist + cross-ref task-id resolver | ✅ done — `1c448e4` |
| Row 2 | `ia/skills/stage-file-apply/` + `project-new-apply/` tails + subagent bodies | Hard rule: N≥2 filed → suggest `/ship-stage`, never `/ship` | ✅ done — `1c448e4` |
| Row 3 | `ia/rules/agent-lifecycle.md` + `CLAUDE.md` §3 | Auto-chain boundary decision (F1): chain all the way OR stop at stage-file-apply (Option B — stop at applier tail) | ✅ done — `1c448e4` |
| Row 7 | `release-rollout-skill-bug-log` helper | Dual-write F1..F12 findings to per-skill Changelog + tracker aggregator (`docs/lifecycle-refactor-rollout-tracker.md`) | ✅ done — `1c448e4` |

**Verification:** `npm run validate:all` exit 0 + `/verify-loop --tooling-only` verdict pass.

### Deferred (read-only this pass)

| Row | Intent |
|---|---|
| Row 4 | per-skill tracker structure parity |
| Row 5 | token-split guardrail telemetry |
| Row 6 | `/plan-review` conditional-gate or Sonnet-downgrade (stacked options 2+3 per upstream analysis) |
| Row 9 | re-run T8.1b external-plan sample for steady-state plan-review yield measurement |
| Row 10 | lock STAGE_ID arg format (`Stage 8` vs `8` disambiguation) |
| Row 11 | typed migration-JSON status surface |

---

## E. Deferred exploration docs on disk

Each waits for a pickup decision post-M8. Authorship already complete; no code written yet.

| Doc | Size | Intended pickup |
|---|---|---|
| `docs/lifecycle-refactor-post-mvp-extensions.md` | 5.6k | `/master-plan-extend ia/projects/lifecycle-refactor-master-plan.md docs/lifecycle-refactor-post-mvp-extensions.md` (verify_mode frontmatter; scope variants C-min / C-med / C-full) |
| `docs/web-platform-post-mvp-extensions.md` | 87k | `/master-plan-extend ia/projects/web-platform-master-plan.md docs/web-platform-post-mvp-extensions.md` |
| `docs/session-token-latency-audit-exploration.md` + `docs/session-token-latency-design-review-2026-04-19.md` | 50k + 29k | `/design-explore` continuation OR `/master-plan-new`; depends on scope decision |
| `docs/ai-mechanics-audit-2026-04-19.md` | 35k | Unscoped — needs `/design-explore` next pass to decide route |
| `docs/cursor-composer-4day-plan.md` + `docs/cursor-agent-master-plan-tasks.md` + `docs/cursor-agent-mcp-bridge.md` | 27k + 7.6k + 6.8k | Cursor integration — unscoped; `/design-explore --against` against existing orchestrator may apply |
| `docs/prompt-caching-mechanics.md` | 12k | Reference doc, consumed by Stage 10 (section C above). No separate pickup needed. |
| `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` | 86k | Source of truth for refactor. Rev 4 already folded into master plan; skim for residual candidates not yet mapped to Stage 10 tasks. |

---

## F. Cross-plan escalations (from Stage 8 T8.4)

Recorded in `ia/state/lifecycle-refactor-migration.json` M7.T8.4.escalations. NOT in lifecycle-refactor scope — hand off to owning orchestrator owners.

| Failure lane | Tests | Owner orchestrator | Triage |
|---|---|---|---|
| Blip audio golden fixtures | 10 | `ia/projects/blip-master-plan.md` | Fixture refresh or audio renderer drift diagnosis |
| Economy Treasury floor clamp | 3 | `ia/projects/zone-s-economy-master-plan.md` | Service regression triage |

Attribution per T8.4: zero refactor-scope — both lanes untouched since pre-refactor snapshot base `48656d2`.

---

## Refresh protocol

Re-run this inventory at each freeze-lift checkpoint:

1. `git status --short` → refresh Section A.
2. Grep master plan for `Draft` + `_pending_` rows → refresh Sections B + C.
3. Scan `docs/` new docs mtime > last commit on main → refresh Section E.
4. Scan `ia/state/lifecycle-refactor-migration.json` M7.T8.4 + M8 state → refresh Section F + gate ordering.

Freeze-lift lifts when TECH-491 (merge) + TECH-492 (freeze-note removal) both Done.
