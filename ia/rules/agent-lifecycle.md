---
purpose: "Thin always-loaded anchor for the agent lifecycle (exploration → close). Names every slash command + subagent + skill in one place and points at the canonical doc."
audience: agent
loaded_by: always
slices_via: none
description: "Canonical ordered flow for agents + slash commands + skills. Defers to docs/agent-lifecycle.md for the full matrix."
alwaysApply: true
---

# Agent lifecycle — canonical flow

Full canonical doc: [`docs/agent-lifecycle.md`](../../docs/agent-lifecycle.md) (flow diagram, stage → surface matrix, handoff contract, decision tree).

## Ordered flow (rev 3 — multi-task stage end-to-end)

```
/design-explore
  → /master-plan-new
  → [/stage-decompose (re-decompose only)]
  → /stage-file (→ stage-file-plan → stage-file-apply)
  → /author (plan-author Stage 1×N)
  → /plan-review (→ plan-fix-apply when critical)
  → [per-Task loop: /implement → /verify-loop → /code-review (→ code-fix-apply when critical)]
  → /audit (opus-audit Stage 1×N, post all per-Task loops)
  → /closeout (Stage-scoped: stage-closeout-plan → stage-closeout-apply)
```

Single-task path (standalone issue, N=1):

```
/project-new (→ project-new-apply)
  → /author (plan-author N=1)
  → /implement
  → /verify-loop
  → /code-review (→ code-fix-apply when critical)
  → /audit (opus-audit N=1)
  → /closeout (Stage-scoped, N=1: stage-closeout-plan → stage-closeout-apply)
```

Stage-end batching: `/author`, `/audit`, `/closeout` all fire ONCE per Stage (bulk Stage 1×N). Per-Task seams = `/implement`, `/verify-loop`, `/code-review`. No `spec-enrich`, no `spec-kickoff`, no `project-stage-close`, no per-Task `project-spec-close` — all absorbed into the Stage-scoped bulk pair shape (T7.12 / T7.13 / T7.14 lifecycle-refactor M6 collapse).

`/stage-decompose` is optional: run it when an already-decomposed step needs re-decomposition (scope change, design pivot). `master-plan-new` fully decomposes ALL steps — no skeletons. Does NOT create BACKLOG rows.

`/master-plan-extend` is the append-only companion to `/master-plan-new`: run it when an existing orchestrator needs new Steps sourced from a fresh exploration doc or an extensions doc (`{slug}-post-mvp-extensions.md`). Never rewrites existing Steps. Full decomposition of every new Step at author time (same cardinality gate as `/master-plan-new`). **Pre-condition:** source doc must have a `## Design Expansion` block (or semantic equivalent). Missing block → `master-plan-extend` Phase 0 stops and routes to `/design-explore {SOURCE_DOC}` — if the source doc is a locked design, add `--against {UMBRELLA_DOC}` to run gap-analysis mode first.

`/ship-stage {MASTER_PLAN_PATH} {STAGE_ID} [--per-task-verify]` is a stage-scoped chain dispatcher sitting BETWEEN `/verify-loop` and `/closeout`: run it when a master plan Stage X.Y has ≥1 non-Done filed task row. Default two-pass shape: **Pass 1** (per-Task) = implement → `unity:compile-check` fast-fail gate → atomic Task-level commit; **Pass 2** (Stage-end bulk, once) = verify-loop (full Path A+B on cumulative Stage delta) → code-review (Stage-level diff; shared context amortized) → audit → closeout. Emits a chain-level stage digest. Next-stage handoff auto-resolved for all 4 cases. Chain stops on first Pass 1 gate failure; `STAGE_CODE_REVIEW_CRITICAL` re-entry cap = 1 (second critical → `STAGE_CODE_REVIEW_CRITICAL_TWICE`). `--per-task-verify` flag: skip Pass 2 verify-loop + code-review; promote Pass 1 to full `verify-loop --skip-path-b` + `code-review` per Task (pre-TECH-519 shape; safety valve for oversized Stages).

`/release-rollout` is an umbrella-level driver ABOVE the single-issue flow: run it when a multi-bucket umbrella master-plan (e.g. `full-game-mvp-master-plan.md`) has a sibling rollout tracker (`ia/projects/{umbrella-slug}-rollout-tracker.md`) and needs to advance one row through the 7-column lifecycle (a)–(g) toward step (f) ≥1-task-filed. Dispatches to the same single-issue commands (`/design-explore`, `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, `/stage-file`) per target cell. Does NOT close issues (= `/closeout`). Tracker is seeded by `release-rollout-enumerate` helper (one-shot per umbrella).

## Surface map (one row per lifecycle seam)

| Seam | Slash command | Subagent | Skill | Model |
|-------|---------------|----------|-------|-------|
| Explore | `/design-explore` | `design-explore` | `design-explore` | Opus |
| Orchestrate | `/master-plan-new` | `master-plan-new` | `master-plan-new` | Opus |
| Extend orchestrator | `/master-plan-extend` | `master-plan-extend` | `master-plan-extend` | Opus |
| Decompose step | `/stage-decompose` | `stage-decompose` | `stage-decompose` | Opus |
| Bulk-file stage (pair) | `/stage-file` | `stage-file-planner` → `stage-file-applier` | `stage-file-plan` → `stage-file-apply` | Opus → Sonnet |
| Single issue (pair) | `/project-new` | `project-new-planner` → `project-new-applier` | `project-new` → `project-new-apply` | Opus → Sonnet |
| Bulk author (Stage 1×N) | `/author` | `plan-author` | `plan-author` | Opus |
| Plan review (pair) | `/plan-review` | `plan-reviewer` → `plan-fix-applier` | `plan-review` → `plan-fix-apply` | Opus → Sonnet |
| Implement | `/implement` | `spec-implementer` | `project-spec-implement` | Sonnet |
| Verify (single-pass) | `/verify` | `verifier` | composed | Sonnet |
| Verify (closed-loop) | `/verify-loop` | `verify-loop` | `verify-loop` | Sonnet |
| Code review (pair) | `/code-review` | `opus-code-reviewer` → `code-fix-applier` | `opus-code-review` → `code-fix-apply` | Opus → Sonnet |
| Audit (Stage 1×N) | `/audit` | `opus-auditor` | `opus-audit` | Opus |
| Close Stage (pair) | `/closeout` | `stage-closeout-planner` → `stage-closeout-applier` | `stage-closeout-plan` → `stage-closeout-apply` | Opus → Sonnet |
| Test-mode ad-hoc | `/testmode` | `test-mode-loop` | `agent-test-mode-verify` | Sonnet |
| Stage-scoped chain ship | `/ship-stage` | `ship-stage` | `ship-stage` | Opus |
| Rollout umbrella | `/release-rollout` | `release-rollout` | `release-rollout` (+ `-enumerate`, `-track`, `-skill-bug-log` helpers) | Opus |
| Progress emit (preamble) | *(none)* | *(all agents, `@`-load)* | `subagent-progress-emit` | — |

Retired surfaces (post-M6 — do not reference in new skills / agents / commands): `/kickoff` + `spec-kickoff` + `project-spec-kickoff` (folded into `plan-author`); `project-stage-close` + `project-spec-close` (folded into Stage-scoped `/closeout` pair). Tombstones live under `ia/skills/_retired/`.

## Hard rules

- **`design-explore --against` for locked docs** — when `/design-explore` is called on a doc that has locked decisions but no Approaches list, pass `--against {REFERENCE_DOC}` (path to umbrella orchestrator or master plan) to activate gap-analysis mode. Without it the skill stops and asks. Useful when a child orchestrator's exploration doc needs alignment-checking against the full-game MVP or any umbrella before `/master-plan-extend`.
- **`/ship-stage` vs single-task flow** — after `/stage-file` files ≥2 tasks in a Stage X.Y, default next step is `/ship-stage {MASTER_PLAN_PATH} {STAGE_ID}` (Pass 1: per-Task implement + `unity:compile-check`; Pass 2: Stage-end bulk verify-loop full Path A+B + code-review Stage diff + audit + closeout). Fall back to the single-task linear flow for standalone issues (no master plan). Use `--per-task-verify` for Stages that are too large for bulk Pass 2 review (safety valve; preserves pre-TECH-519 N× per-Task verify-loop + code-review shape).
- **`/stage-file` chain boundary (T8 dry-run F1 / Row 3, Option B)** — `/stage-file` STOPS at applier tail. Does NOT auto-chain to `/author`. Applier handoff suggests `/ship-stage {MASTER_PLAN_PATH} Stage {STAGE_ID}` for N≥2 (or `/ship {ISSUE_ID}` for N=1) — the chain dispatcher owns author → implement → verify-loop → code-review → audit → closeout. Rationale: avoid two competing auto-chains (here vs `/ship-stage`); single canonical entry boundary; user can intervene between filing and shipping. Hard rule: NEVER suggest `/author` standalone after `/stage-file` — folded into ship chain.
- **`/verify` vs `/verify-loop`** — `/verify` = single pass, read-only, no fix iteration. `/verify-loop` = 7-step closed loop with bounded fix iteration (`MAX_ITERATIONS` default 2).
- **Orchestrator docs are permanent.** `master-plan-new` output (`ia/projects/{slug}-master-plan.md`) is NEVER closeable via `/closeout`. See [`ia/rules/orchestrator-vs-spec.md`](orchestrator-vs-spec.md).
- **`/closeout` is Stage-scoped.** One invocation closes ALL Task rows of one Stage X.Y in bulk (planner → applier pair). Per-Task closeout surface retired — closeout tuples live under Stage block `§Stage Closeout Plan` in the master plan, not in each per-Task spec.
- **Pair contract.** All Plan-Apply pair seams (`stage-file`, `project-new`, `plan-review`, `code-review`, `closeout`) obey [`ia/rules/plan-apply-pair-contract.md`](plan-apply-pair-contract.md): Opus pair-head writes `{operation, target_path, target_anchor, payload}` tuples; Sonnet pair-tail reads verbatim and applies — never re-orders, re-interprets, or re-queries anchors.
- **Verification policy is single canonical.** All verify agents defer to [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md). Do not restate timeout escalation, Path A lock release, or Path B preflight in skill / agent / command bodies.
- **Handoff artifact required per stage.** Missing artifact → next stage refuses to start. Full contract: `docs/agent-lifecycle.md` §3.
- **Monotonic ids per prefix.** `BUG-` / `FEAT-` / `TECH-` / `ART-` / `AUDIO-` ids never reused across BACKLOG + BACKLOG-ARCHIVE ([`AGENTS.md`](../../AGENTS.md) §7).
- **Stage sizing gate.** Before filing, `stage-decompose` (Phase 3.5) and `stage-file-plan` (Phase 1) run the 6-heuristic cluster check in [`ia/rules/stage-sizing-gate.md`](stage-sizing-gate.md); FAIL → split X.Y → X.Y.A / X.Y.B + reauthor before proceeding.

## Authoritative neighbors

- [`docs/agent-lifecycle.md`](../../docs/agent-lifecycle.md) — full canonical doc.
- [`ia/rules/project-hierarchy.md`](project-hierarchy.md) — step > stage > phase > task.
- [`ia/rules/orchestrator-vs-spec.md`](orchestrator-vs-spec.md) — permanent vs temporary split.
- [`ia/rules/plan-apply-pair-contract.md`](plan-apply-pair-contract.md) — Plan-Apply pair tuple + escalation contract.
- [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md) — Verification block policy.
- [`AGENTS.md`](../../AGENTS.md) §2 — lifecycle entry for human-facing agents.
- [`CLAUDE.md`](../../CLAUDE.md) §3 — Claude Code host surface (hooks, subagents, commands).
