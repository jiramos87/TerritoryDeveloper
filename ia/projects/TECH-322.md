---
purpose: "TECH-322 — Ship-stage chain shipper: stateful subagent + skill + command (Approach B)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-322 — Ship-stage chain shipper — stateful subagent + skill + command (Approach B)

> **Issue:** [TECH-322](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

<!--
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Glossary: ../specs/glossary.md (spec wins on conflict).
  Design source: ../../docs/ship-stage-exploration.md (`## Design Expansion` block, Approach B selected).
  Authoring style: caveman prose (drop articles/filler/hedging; fragments OK). Tables, code, seed prompts stay normal.
-->

## 1. Summary

New `/ship-stage {MASTER_PLAN_PATH} {STAGE_ID}` lifecycle command chains `spec-kickoff → spec-implementer → verify-loop → closeout` across every filed task row of one **Stage** inside a master plan, end-to-end and autonomously. Stateful chain subagent (Opus orchestrator) caches MCP context once per stage via TECH-302 shared subskills + amortizes verification at stage boundary. Closes the gap between per-issue `/ship` and umbrella-scoped `/release-rollout`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Single-dispatch stage-scoped shipping — `claude-personal "/ship-stage {path} Stage X.Y"` drives every non-Done filed task row of that stage to closed, then stops at the stage boundary w/ one handoff line.
2. **Cached MCP context** per stage — one `domain-context-load` per chain vs N × 4–6 MCP round trips in naïve per-task approach (amortization win scales w/ stage size 2–6 tasks).
3. **Chain-level stage digest** — aggregates cross-task lessons + decisions + `verify-loop` iteration counts. Distinct from per-spec `project-stage-close` which still fires inside each inner `spec-implementer` unchanged (per Review Notes resolution).
4. **Hybrid verification** — per-task Path A compile gate fail-fast; batched Path B smoke run once at stage end on cumulative delta via new `--skip-path-b` flag on `verify-loop`.
5. **Full auto-resolve next-stage handoff** — scan master plan post-close, emit correct `Next:` command for all 4 cases: next filed stage → `/ship-stage`, next `_pending_` stage → `/stage-file`, next skeleton step → `/stage-decompose`, umbrella done → `/closeout`.
6. **Fail-loud parser** — narrow regex on `{task-id, status}` columns under `## Stage X.Y` / `### Stage X.Y` headers; schema drift → `STOPPED at parser` w/ expected-vs-found column diff. Follow-up issue files MCP `spec_stage_table` slice migration once MCP lifecycle audit lands.

### 2.2 Non-Goals (Out of Scope)

1. `/ship-step` + `/ship-plan` bulk-above-stage dispatchers — deferred per Q8, file once `/ship-stage` ships cleanly if demand appears.
2. Parallel task dispatch within a stage — tasks share files + invariants; sequential is load-bearing.
3. Stage-delta native mode in `verify-loop` — external batching via `--skip-path-b` flag suffices v1. Native mode = separate follow-up.
4. Continue-on-error flag — stop-on-first-failure is only defensible default per design doc §Risks #3.
5. MCP `spec_stage_table` parser migration — follow-up issue `depends_on` MCP lifecycle audit.
6. Postgres persistence of chain-level stage digest — follow-up mirror of `project_spec_journal_persist`, deferred.
7. `--no-batch-path-b` negation flag for debugging — deferred v2.
8. Runtime C# / gameplay changes — tooling / pipeline only.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a dev shipping a multi-task stage (e.g. Stage 1.1 = TECH-303 + TECH-304), I want one `/ship-stage` command to drive every task through kickoff → implement → verify → closeout so I stop pasting `/ship {next_id}` between tasks. | Single dispatch ships all non-Done tasks sequentially; stops w/ one handoff line for next stage. |
| 2 | Developer | As a dev debugging a mid-chain failure, I want a structured digest showing which task + which gate failed so I can resume via `/ship {failed_id}` after fix. | `STOPPED at {ISSUE_ID} — {gate}: {reason}` digest; tasks already closed stay closed; next run skips them. |
| 3 | Developer | As a dev running on battery / tight budget, I want MCP context loaded once per stage instead of N times so Opus token spend drops on stages ≥ 3 tasks. | `domain-context-load` fires once at chain start; per-task dispatches inherit cached payload. |
| 4 | Developer | As a dev relying on Path B smoke as a gate, I want verification batched at stage end on cumulative delta so I get one Path B run per stage instead of N. | `verify-loop --skip-path-b` runs per task; single batched Path B runs at chain end post-closeout; JSON verdict records `path_b: skipped_batched` per task + cumulative pass/fail at stage boundary. |

## 4. Current State

### 4.1 Domain behavior

Today's lifecycle has two shipping scales: single-issue `/ship {ISSUE_ID}` (gated pipeline, one task) + umbrella `/release-rollout` (advances rollout-tracker rows, does NOT close issues). No stage-scoped dispatcher exists. A **Stage** in a master plan routinely holds 2–6 filed task rows sharing Exit criteria, files, + invariants. User pastes `Next: /ship {id}` per task → one human-in-loop roundtrip per task; wasted orchestration cache; no stage-coherent autonomous run.

### 4.2 Systems map

| Surface | Role | Change shape |
|---|---|---|
| `.claude/commands/ship-stage.md` | NEW — slash-command dispatcher | Mirror `.claude/commands/ship.md` shape; args `{MASTER_PLAN_PATH} {STAGE_ID}`; forwards caveman-asserting prompt to `ship-stage` subagent. |
| `.claude/agents/ship-stage.md` | NEW — Opus chain orchestrator | Owns task loop, MCP cache, chain-level digest. Dispatches inner `spec-kickoff` (Opus) → `spec-implementer` (Sonnet) → `verify-loop` (Sonnet) → `closeout` (Opus) per task. |
| `ia/skills/ship-stage/SKILL.md` | NEW — phased procedure body | Phase 0 parse → Phase 1 context-load → Phase 2 task-loop → Phase 3 batched verify → Phase 4 chain digest → Phase 5 next-stage resolver. |
| `.claude/agents/verify-loop.md` + `ia/skills/verify-loop/SKILL.md` | CHANGE — `--skip-path-b` flag | Default off. On: Path A fail-fast runs; Path B skipped; JSON verdict field records `path_b: skipped_batched`. |
| `.claude/agents/spec-implementer.md` | NO CHANGE (post Phase 8 review) | Per-spec `project-stage-close` fires normally — chain-level digest is separate scope. |
| `ia/skills/project-stage-close/SKILL.md` | NO CHANGE | Per-spec skill unaffected. Chain digest is NEW concept, not a call to this skill. |
| `docs/agent-lifecycle.md` | UPDATE §2 Stage→surface matrix | Add `/ship-stage` row between `/testmode` and `project-stage-close`. |
| `ia/rules/agent-lifecycle.md` | UPDATE Surface map + flow | Add `/ship-stage` row + chain semantics paragraph. |
| `CLAUDE.md` §3 | UPDATE commands table | Add `/ship-stage` row. |
| `AGENTS.md` §2 | UPDATE lifecycle entry | Add `/ship-stage` to human-facing flow. |
| `ia/specs/glossary.md` | UPDATE | Add rows for `ship-stage dispatcher` + `chain-level stage digest`. |
| TECH-302 Stage 2 subskills | CONSUME | Hard dep — `domain-context-load` + `term-anchor-verify` must ship first. |

### 4.3 Implementation investigation notes (optional)

- Parser v1 — regex against `## Stage X.Y` + `### Stage X.Y` header variance across existing master plans (`citystats-overhaul-master-plan.md` uses `###`, `multi-scale-master-plan.md` uses `##` — verify at authoring time; add fixtures for both depths).
- STAGE_VERIFY_FAIL terminal state — distinct from mid-chain `STOPPED`. All tasks closed + archived, batched Path B fails → no rollback, emit digest w/ failure note + human-review directive. Example 5 in design doc.
- Task-row status race — closeout archives BACKLOG row + writes master-plan task-table status. Chain must re-read master plan per iteration to catch flipped state. Sequential gating already enforces ordering.
- MCP cache shape — `domain-context-load` returns `{glossary_anchors, router_domains, spec_sections, invariants}` (per TECH-302 Stage 2 contract). Passed as pre-resolved input to each per-task inner dispatch so kickoff / implementer don't re-query.

## 5. Proposed Design

### 5.1 Target behavior (developer-visible)

**Entry:** `claude-personal "/ship-stage {MASTER_PLAN_PATH} {STAGE_ID}"` (e.g. `/ship-stage ia/projects/citystats-overhaul-master-plan.md Stage 1.1`).

**Happy path:** subagent parses stage task table → loads MCP context once → for each non-Done task: kickoff → implement → verify-loop `--skip-path-b` → closeout; closeout archives backlog row + flips master-plan task status. After last task: one batched Path B verify on cumulative delta → chain-level stage digest → next-stage resolver emits `Next:` line for one of 4 cases.

**Exit lines:**
- Success: `SHIP_STAGE {STAGE_ID}: PASSED` + handoff.
- Mid-chain failure: `SHIP_STAGE {STAGE_ID}: STOPPED at {ISSUE_ID} — {gate}: {reason}`.
- Stage-verify failure: `SHIP_STAGE {STAGE_ID}: STAGE_VERIFY_FAIL` (all tasks closed, batched Path B failed, human review required, no rollback).
- Parser schema drift: `SHIP_STAGE {STAGE_ID}: STOPPED at parser — schema mismatch` + expected vs found column diff.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

See design doc `docs/ship-stage-exploration.md` `## Design Expansion` §Architecture mermaid. Locked structural decisions:

1. **Opus orchestrator + Sonnet inner work** — `ship-stage` subagent = Opus (orchestrator); inner pipeline preserves existing model pins per `spec-kickoff` (Opus), `spec-implementer` (Sonnet), `verify-loop` (Sonnet), `closeout` (Opus).
2. **Stateful chain, NOT thin orchestrator** (Q1=B) — single subagent owns task loop + MCP cache + in-process journal accumulator. Not `Agent(spawn)` × N.
3. **Per-spec `project-stage-close` fires normally** inside each inner `spec-implementer` (Phase 8 resolution — no inhibit flag). Chain-level stage digest is NEW surface owned by `ship-stage`, not a call to `project-stage-close`.
4. **Hybrid verify** (Q4=C) — per-task `verify-loop --skip-path-b` (Path A fail-fast mandatory) + one batched Path B at stage boundary on cumulative delta.
5. **Narrow regex parser** (Q5=C) — column-scope limited to task-id + status. Fails loud on mismatch. MCP `spec_stage_table` slice migration filed as follow-up issue `depends_on` MCP lifecycle audit.
6. **Next-stage resolver** (Q3=A) — re-reads master plan post-close; covers 4 cases: next filed stage, next `_pending_` stage, next skeleton step, umbrella done.

### 5.3 Method / algorithm notes

Agent-owned during Phase 3 + 4 of Implementation Plan. Key algorithms to author:
- Master-plan stage-table regex extractor (header-depth agnostic, fails loud on schema drift).
- Next-stage resolver (4-case scan).
- Chain journal accumulator (in-process list of `{task_id, lessons[], decisions[], verify_iterations}`).
- Chain-level digest formatter.

## 6. Decision Log

Interview-locked decisions from `/design-explore docs/ship-stage-exploration.md` Phase 0.5 poll.

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | **Q1 = B** — Stateful chain subagent w/ cached MCP context | Stage sizes routinely 2–6 tasks; amortizes MCP load; composes w/ TECH-302 Stage 2 shared subskills; unlocks chain-level digest + batched verify — both prerequisites for deferred `/ship-step` / `/ship-plan`. | A thin orchestrator command (~1 day, no cache); C inline skill (blows caller context, no Opus/Sonnet split). |
| 2026-04-17 | **Q2 = A resolved-as-none-needed** — per-spec `project-stage-close` unchanged; chain-level stage digest is NEW separate scope owned by `/ship-stage` | Phase 8 review found no double-invocation — per-spec skill fires per inner spec (normal), chain digest is new surface. No inhibit flag needed. No contract change to `spec-implementer`. | A — inhibit inner `project-stage-close` (rejected: breaks per-spec contract + would require wiring change in `spec-implementer`); B — outer no-op (rejected: confusing duplication). |
| 2026-04-17 | **Q3 = A** — Full auto-resolve next-stage handoff across all 4 cases | Removes last manual paste cycle; scan logic already exists in `ship.md` Step 0, extend to stage granularity. | B — emit generic "look at master plan" hint (rejected: regressive vs single-issue `/ship`). |
| 2026-04-17 | **Q4 = C** — Hybrid verify: per-task Path A compile gate fail-fast + batched Path B smoke at stage end via `--skip-path-b` flag on `verify-loop` | Path A catches compile regressions immediately (mid-chain halt cheap); Path B Unity smoke is expensive per run + catches cross-task regressions better on cumulative delta. | A — per-task Path A + Path B (expensive on 4-task stages, no cumulative signal); B — Path A only (loses Path B gate entirely); D — stage-delta native mode in `verify-loop` (too big for v1 scope, separate follow-up). |
| 2026-04-17 | **Q5 = C** — Hybrid parser: narrow regex v1 + follow-up issue for MCP `spec_stage_table` slice (`depends_on` MCP lifecycle audit); parser fails loud on schema mismatch | Regex is contained (task-id + status columns only); MCP slice tool doesn't exist yet + depends on unshipped MCP lifecycle audit — blocking v1 on it defeats the point; fail-loud prevents silent drift. | A — pure regex (fragile long-term); B — pure MCP slice (blocks on audit). |
| 2026-04-17 | **Scope v1 = `/ship-stage` only** — `/ship-step` + `/ship-plan` deferred (Q8) | Single-issue path + rollout tracker already cover step + plan scales (loosely). Stage gap is sharpest + most frequent pain point. Ship smallest useful unit; re-evaluate demand once v1 lands. | File all 3 dispatchers now (rejected: scope creep, triples effort for marginal return). |
| 2026-04-17 | **Hard dep on TECH-302 Stage 2** — do not start Phase 4 authoring until `domain-context-load` + `term-anchor-verify` ship | Approach B's cache-reuse win comes from reusing TECH-302 shared subskills. Authoring before they exist forces stub work that must be re-done. | Start now w/ stub subskills inline (rejected: throws away Phase 4 work when TECH-302 ships). |

## 7. Implementation Plan

### Phase 1 — Prerequisites (hard gate on TECH-302 Phase 2)

- [ ] Verify TECH-302 Stage 2 closed: `domain-context-load` + `term-anchor-verify` subskills callable from caller SKILL.md bodies.
- [ ] Confirm MCP `spec_stage_table` slice tool status — not required v1; file follow-up issue `depends_on` MCP lifecycle audit.

### Phase 2 — Flag plumbing (minimal, isolated)

- [ ] Add `--skip-path-b` flag to `verify-loop` agent + skill. Default off. On: Path A fail-fast runs; Path B skipped; JSON verdict field records `path_b: skipped_batched`.
- [ ] No `--inhibit-stage-close` flag needed (Phase 8 resolution) — per-spec `project-stage-close` + chain-level stage digest are distinct scopes.
- [ ] Smoke test: `/ship TECH-xxx --skip-path-b` single task → Path A runs, Path B reports skipped in JSON verdict.

### Phase 3 — Parser + resolver

- [ ] Author narrow regex parser: extracts `{task-id, status}` rows under `## Stage X.Y` or `### Stage X.Y` headers (both header depths). Fails loud on schema drift w/ expected-vs-found column diff.
- [ ] Author next-stage resolver: scans master plan post-close; returns one of `{next_filed_stage, next_pending_stage, next_skeleton_step, umbrella_done}`; emits correct command per case.
- [ ] Add parser test fixtures against 2–3 existing master plans (`citystats-overhaul-master-plan.md`, `multi-scale-master-plan.md`, plus one more).

### Phase 4 — Chain subagent + skill

- [ ] Create `.claude/agents/ship-stage.md` (Opus orchestrator, caveman directive, mission prompt).
- [ ] Create `.claude/commands/ship-stage.md` dispatcher (mirror `ship.md` shape).
- [ ] Create `ia/skills/ship-stage/SKILL.md` body: phased procedure (parse → context-load → task-loop → batched verify → chain digest → resolver).
- [ ] Wire `domain-context-load` subskill call at Phase 1 start; pass cached payload to per-task inner dispatches.
- [ ] Wire stage journal accumulator (in-process) — collect per-task lessons + decisions for chain digest.

### Phase 5 — Chain-level stage digest + handoff

- [ ] Implement chain-level stage digest: aggregates cross-task lessons, decisions, `verify-loop` iteration counts. Distinct from per-spec `project-stage-close` which still fires inside each inner `spec-implementer`.
- [ ] Wire handoff line emission: `Next: claude-personal "/{resolved-command}"` with one of 4 forms per Phase 3 resolver.
- [ ] STAGE_VERIFY_FAIL handling: on batched Path B failure (all tasks closed), emit digest w/ failure note + human-review directive; no rollback.

### Phase 6 — Docs + glossary

- [ ] Update `docs/agent-lifecycle.md` §2 Stage→surface matrix (add `/ship-stage` row between `/testmode` and `project-stage-close`).
- [ ] Update `ia/rules/agent-lifecycle.md` Surface map + flow (add `/ship-stage` row + chain semantics paragraph).
- [ ] Update `CLAUDE.md` §3 commands table (add `/ship-stage`).
- [ ] Update `AGENTS.md` §2 lifecycle entry.
- [ ] Add glossary rows `ia/specs/glossary.md` — `ship-stage dispatcher` + `chain-level stage digest`.

### Phase 7 — Smoke verification

- [ ] Dry run against a real stage (identify non-shipped stage w/ ≥2 open tasks at implementation time — candidates: `citystats-overhaul-master-plan.md` Stage 1.1 TECH-303 + TECH-304 if still open).
- [ ] Verify single chain-level stage digest fire at chain end (not duplicated w/ per-spec stage-close).
- [ ] Verify batched Path B runs once at stage end on cumulative delta.
- [ ] Verify handoff resolver across all 4 cases (filed / pending / skeleton / umbrella-done).
- [ ] File follow-up issue: `spec_stage_table` MCP slice tool migration (`depends_on` MCP lifecycle audit).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| IA wiring — subagent + skill + command files present, caveman preambles | Node | `npm run validate:all` | Chains `validate:dead-project-specs`, `test:ia`, `validate:fixtures`, `generate:ia-indexes --check`. |
| Parser regex — header-depth + column schema | Test fixtures | Parser unit test file (TBD under `tools/` or embedded in skill) against 2–3 master-plan fixtures | Fails loud on schema drift; column-diff output required. |
| `--skip-path-b` flag plumbing on `verify-loop` | Manual + JSON verdict | `/verify-loop {id} --skip-path-b` on a real task | JSON verdict records `path_b: skipped_batched`; Path A still runs + gates. |
| End-to-end chain smoke | Dev machine | Dry run on live stage w/ ≥2 open tasks (e.g. Stage 1.1 TECH-303 + TECH-304 if still open at impl time) | Single chain-level digest fire; batched Path B passes; `Next:` handoff correct. |
| Next-stage resolver — 4 cases | Manual fixtures | Contrived master-plan fixtures covering filed / pending / skeleton / umbrella-done | Resolver emits correct command per case. |
| Doc / glossary sync | Node | `npm run validate:all` + `npm run validate:dead-project-specs` | `/ship-stage` row + glossary anchors present. |

## 8. Acceptance Criteria

- [ ] `/ship-stage {MASTER_PLAN_PATH} {STAGE_ID}` chains every non-Done filed task row sequentially through `spec-kickoff → spec-implementer → verify-loop --skip-path-b → closeout`.
- [ ] Mid-chain failure → `SHIP_STAGE {STAGE_ID}: STOPPED at {ISSUE_ID} — {gate}: {reason}` digest; tasks already closed remain closed.
- [ ] Chain-level stage digest emitted at chain end, aggregating cross-task lessons + decisions + `verify-loop` iteration counts; distinct from per-spec `project-stage-close`.
- [ ] Batched Path B smoke runs once at stage end on cumulative delta; STAGE_VERIFY_FAIL terminal state handled (no rollback, human review directive).
- [ ] `Next:` handoff auto-resolves for all 4 cases: next filed stage → `/ship-stage`, next `_pending_` stage → `/stage-file`, next skeleton step → `/stage-decompose`, umbrella done → `/closeout`.
- [ ] `verify-loop` grows `--skip-path-b` flag w/ JSON verdict `path_b: skipped_batched`; Path A stays mandatory.
- [ ] Regex parser fails loud on task-table schema mismatch w/ expected-vs-found column diff; test fixtures cover 2–3 master plans (header-depth `##` + `###` variance).
- [ ] Smoke run on live stage w/ ≥2 open tasks passes end-to-end.
- [ ] Follow-up issue filed for `spec_stage_table` MCP slice migration (`depends_on` MCP lifecycle audit).
- [ ] `CLAUDE.md` §3 + `AGENTS.md` §2 + `docs/agent-lifecycle.md` §2 + `ia/rules/agent-lifecycle.md` all list `/ship-stage`.
- [ ] Glossary rows added for `ship-stage dispatcher` + `chain-level stage digest`.
- [ ] `npm run validate:all` clean.

## 9. Issues Found During Development

<!-- Populated during implementation. -->

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

<!-- Populated at closeout. Migrate to AGENTS.md, glossary, canonical docs on close. -->

- …

## Open Questions (resolve before / during implementation)

Interview-locked decisions moved to §6 Decision Log. Remaining runtime questions:

1. Parser header-depth variance — confirm at Phase 3 authoring which master plans use `##` vs `###` for stage headers; pick fixtures accordingly.
2. Smoke target freshness — at Phase 7 time, confirm chosen stage still open (citystats-overhaul Stage 1.1 = TECH-303 + TECH-304 candidate; fall back to any live multi-task stage if shipped before then).
3. Chain-level digest format — should it mirror `project-stage-close` digest shape (JSON header + caveman summary) or diverge? Agent-owned unless UX conflict surfaces.
4. Should the `--skip-path-b` flag also be surfaced on `/verify` (single-pass) in addition to `/verify-loop`, for parity? Default: no — chain consumer is `verify-loop` only. Revisit if `/verify` callers request it.
