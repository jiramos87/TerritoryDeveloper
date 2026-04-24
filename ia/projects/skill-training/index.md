# Skill Training — Master Plan (IA / Skill Lifecycle, tooling-only)

> **Status:** In Progress — Stage 3 (wiring) + Stage 6 (pending)
>
> **Scope:** Approach A two-skill split — structured JSON self-report emitter at Phase-N-tail of 13 lifecycle skills + `skill-train` consumer subagent (Opus, on-demand) that synthesizes recurring friction into patch proposals for SKILL.md bodies, gated by user review. Excludes auto-apply, rule-level promotion, shared subskills, scheduled loop, dashboard visualization, evaluator-model judge, and `.claude/agents/*.md` body edits — see `docs/skill-training-exploration.md §Implementation Points — Deferred`.
>
> **Exploration source:** `docs/skill-training-exploration.md` (§Design Expansion — Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Examples, Review Notes are ground truth).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach A (two-skill split) over B (inference-only) or C (continuous auto-propose) — Phase 1 matrix: highest signal/cost ratio.
> - `user_correction` removed from self-report schema — unreliable via self-inspection; handled by `release-rollout-skill-bug-log` (`source: user-logged` channel).
> - Schema version = date-stamped `schema_version` field; consumer warns on mismatch but aggregates.
> - Auto-apply never in v1 — user-gate mandatory.
> - Scope: 13 skills (10 core lifecycle + 3 rollout-family); shared subskills deferred to v2.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `docs/skill-training-exploration.md` — full design + architecture + examples + review notes. Design Expansion block is ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — #12 (glossary rows land before cross-refs in skill/agent/command bodies).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

### Stage index

- [Stage 1 — skill-train Core + Glossary Foundation / Glossary + Docs Foundation](stage-1-glossary-docs-foundation.md) — _Final (4 of 4 done: TECH-367, TECH-368, TECH-369, TECH-370)_
- [Stage 2 — skill-train Core + Glossary Foundation / skill-train Skill Body + Agent + Command](stage-2-skill-train-skill-body-agent-command.md) — _Final_
- [Stage 3 — Phase-N-tail Wiring (13 Lifecycle Skills) / Core Authoring + Filing Skills (6 skills)](stage-3-core-authoring-filing-skills-6-skills.md) — _In Progress — TECH-433 (4 of 4 filed: TECH-430, TECH-431, TECH-432, TECH-433)_
- [Stage 4 — Phase-N-tail Wiring (spec-lifecycle + rollout-family)](stage-4-phase-n-tail-wiring-spec-lifecycle-rollout-family.md) — _Obsoleted by M6 collapse (2026-04-21)_
- [Stage 5 — Caveman Soft-Lint (Phase D) / Lint Script + Hook + Docs](stage-5-lint-script-hook-docs.md) — _Final_
- [Stage 6 — Dogfood Cycle (Phase E) / First Retrospective + Meta-Dogfood](stage-6-first-retrospective-meta-dogfood.md) — _Draft (tasks _pending_ — not yet filed)_

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) runs.
- Run `claude-personal "/stage-file ia/projects/skill-training-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (header block). Changes require explicit re-decision + sync edit to `docs/skill-training-exploration.md §Design Expansion`.
- Land Stage 1.1 (glossary rows) before Stage 1.2 or any Step 2 body authors cross-refs — invariant #12.
- If total effort crosses 5 dev days: split Step 3 (soft-lint) into a standalone TECH- issue per Review Notes; proceed with Steps 1–2–4 unblocked.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (`ia/rules/orchestrator-vs-spec.md`). Step 4 close sets `Status: Final`; the file stays.
- Promote post-MVP items into MVP stages — deferred list in `docs/skill-training-exploration.md §Implementation Points — Deferred` (auto-apply, rule-level promotion, shared subskills, scheduled loop, dashboard, evaluator-judge, GC strategy for old train-proposal files).
- Merge partial stage state — every stage must land on a green bar.
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Modify `ia/skills/release-rollout-skill-bug-log/SKILL.md` in Step 2 wiring — it is a sibling producer (`source: user-logged` channel) and must remain unchanged.
