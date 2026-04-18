---
purpose: "TECH-302 — Release-rollout skill family model-fit + componentization refactor."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-302 — Release-rollout skill family — model-fit + componentization refactor

> **Issue:** [TECH-302](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Refactor **release-rollout** skill family (umbrella skill + 3 helpers + Opus subagent) to move mechanical Edit / Glob / MCP-fetch work from Opus host to Sonnet surfaces + extract 5 shared subskills that kill ~8 copies of the same MCP recipe across lifecycle skills. Source: model-fit + componentization audit at [`docs/release-rollout-model-audit.md`](../../docs/release-rollout-model-audit.md). Today rollout family = 100 % Opus (umbrella + 3 helpers inline inside Opus host); audit graded ~60 % of umbrella body mechanical bookkeeping paid at Opus rates. Refactor leaves Opus owning reasoning surface only (dispatch interview, disagreement detection, tier arbitration) while Sonnet handles typing.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Stage 1 no-risk Sonnet extractions shipped: `progress-regen` (Bash wrapper, 6 callers), `cardinality-gate-check` (4 callers), `surface-path-precheck` (3 callers).
2. Stage 2 shared MCP recipe shipped: `domain-context-load` (Sonnet, 8+ callers — biggest maintenance win), `term-anchor-verify` (Sonnet, 3+ callers).
3. Stage 3 rollout-specific splits shipped: `release-rollout-repo-sweep` (Sonnet, wired into enumerate Phase 1), `rollout-row-state` (Sonnet, wired into release-rollout Phase 1).
4. Stage 4 helper-to-subagent promotions: new `.claude/agents/release-rollout-track.md` (Sonnet) + `.claude/agents/release-rollout-skill-bug-log.md` (Sonnet); release-rollout Phase 5 + skill-bug branch dispatch via Agent tool.
5. All caller SKILL.md + agent bodies updated to invoke new subskills / subagents — no duplicate recipe text left.
6. `npm run validate:all` green post-refactor; rollout tracker advance-one-row smoke test unregressed.
7. Audit doc (`docs/release-rollout-model-audit.md`) §7 stages 1–4 ticked.

### 2.2 Non-Goals (Out of Scope)

1. Stage 5 follow-up audits — `stage-decompose` missing `reasoning_effort` pin, `project-new` template-fill split, `design-explore` Phase 8 review Sonnet downgrade. Flagged here, filed separately.
2. No changes to lifecycle subagent bodies beyond wiring to new shared subskills. `design-explore` / `master-plan-new` / `master-plan-extend` / `stage-decompose` / `stage-file` / `project-new` model pins unchanged.
3. No runtime C# changes. Skill / IA infrastructure only.
4. No change to the 7-column rollout lifecycle glyph vocabulary or the **Alignment gate** semantics.
5. No new MCP tools — `domain-context-load` + `term-anchor-verify` wrap existing MCP tools, do not add any.
6. No integration test harness for new subskills — flagged in Open Questions, deferred to separate BACKLOG row if product wants it.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Agent author | Want single shared subskill for the MCP recipe (`glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary`) so tuning lands in one place | `ia/skills/domain-context-load/SKILL.md` exists + 8+ callers invoke it |
| 2 | Agent author | Want mechanical rollout cell flips + change-log writes off Opus host | `release-rollout-track` + `release-rollout-skill-bug-log` run as Sonnet subagents |
| 3 | Skill maintainer | Want cardinality gate + surface-path precheck + progress regen contracts centralized | 3 Stage 1 Sonnet subskills land; 4 + 3 + 6 callers updated |
| 4 | Product owner | Want audit-driven refactor without regressing the `/release-rollout` advance-one-row smoke test | `validate:all` green + manual smoke unregressed |

## 4. Current State

### 4.1 Domain behavior

Rollout family today:

- `release-rollout` umbrella = Opus high subagent; Phases 0/1/2/3/5 are mechanical but run in Opus host.
- 3 helper skills (`release-rollout-enumerate`, `release-rollout-track`, `release-rollout-skill-bug-log`) run **inline** inside umbrella Opus host — no own model pin, forced to Opus.
- Cross-skill duplication of the MCP recipe (`glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary`) in 8+ skills.
- **Cardinality gate** inline in 4 skills (`master-plan-new` Phase 6, `master-plan-extend` Phase 6, `stage-decompose` Phase 5, `stage-file` Phase 3).
- Surface-path pre-check inline in 3 skills (`master-plan-new` / `master-plan-extend` / `stage-decompose`).
- `npm run progress` wrapper inline in 6 callers (master-plan-new, master-plan-extend, stage-decompose, stage-file, project-spec-close, project-stage-close).
- **Term-anchor-verify** pattern (glossary_lookup + router_for_task + spec_section triple) inline in 3+ skills (rollout Phase 3 **Alignment gate**, track Phase 1 align verify, implicitly in master-plan authoring).

### 4.2 Systems map

Audit source: [`docs/release-rollout-model-audit.md`](../../docs/release-rollout-model-audit.md) — full phase-by-phase grading + componentization plan.

**Skill bodies touched:**
- [`ia/skills/release-rollout/SKILL.md`](../skills/release-rollout/SKILL.md) — umbrella
- [`ia/skills/release-rollout-enumerate/SKILL.md`](../skills/release-rollout-enumerate/SKILL.md) — helper (one-shot seeder)
- [`ia/skills/release-rollout-track/SKILL.md`](../skills/release-rollout-track/SKILL.md) — helper (cell updater)
- [`ia/skills/release-rollout-skill-bug-log/SKILL.md`](../skills/release-rollout-skill-bug-log/SKILL.md) — helper (skill bug logger)

**Subagent bodies touched:**
- [`.claude/agents/release-rollout.md`](../../.claude/agents/release-rollout.md) — Opus high (model unchanged; dispatch wiring updated)
- New `.claude/agents/release-rollout-track.md` — Sonnet
- New `.claude/agents/release-rollout-skill-bug-log.md` — Sonnet

**New shared subskills (5):**
- `ia/skills/progress-regen/SKILL.md` — Bash wrapper (no-model)
- `ia/skills/cardinality-gate-check/SKILL.md` — Sonnet
- `ia/skills/surface-path-precheck/SKILL.md` — Sonnet
- `ia/skills/domain-context-load/SKILL.md` — Sonnet
- `ia/skills/term-anchor-verify/SKILL.md` — Sonnet

**New rollout-specific subskills (2):**
- `ia/skills/release-rollout-repo-sweep/SKILL.md` — Sonnet
- `ia/skills/rollout-row-state/SKILL.md` — Sonnet

**Callers needing wiring updates** (count from audit §4): master-plan-new, master-plan-extend, stage-decompose, stage-file, project-new, design-explore, project-spec-close, project-stage-close, plus 4 rollout skills above.

Glossary anchors (all pre-existing, no new terms): **Rollout lifecycle**, **Rollout tracker**, **Alignment gate**, **Per-skill Changelog**, **Skill Iteration Log**, **Orchestrator document**.

Router domain: `no_matching_domain` — this is skill / IA infrastructure, not a gameplay/domain lane. Audit explicitly calls this out as expected.

### 4.3 Implementation investigation notes (optional)

- Audit §5 rationale for subagent (not just Sonnet skill) for the 2 Stage 4 promotions: a skill runs in caller's model context. Unless umbrella downgrades to Sonnet (it cannot — Phase 4 is real reasoning), an inline skill is forced to Opus. A subagent has its own model pin + fresh context → Sonnet carries edit work, single-line handoff back to Opus parent.
- `progress-regen` is the only no-model subskill — pure Bash wrapper. Others are Sonnet because templated authoring + classification still benefits from model-driven edit precision.
- `domain-context-load` input contract: `keywords[]`, `brownfield_flag` (skip router + specs + invariants on greenfield), `tooling_only_flag` (skip invariants). Output: `{glossary_anchors, router_domains, spec_sections, invariants}`.
- `term-anchor-verify` input contract: `terms[]` (English). Output: per-term `{anchored: bool, missing: [glossary|router|spec]}`. Codifies the **Alignment gate** column (g) check.

## 5. Proposed Design

### 5.1 Target behavior (product)

Product behavior unchanged. `/release-rollout` advance-one-row flow identical from user perspective. Internal dispatch lands on Sonnet for mechanical work; Opus owns reasoning surface only.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Follow audit §7 staged order. Each stage commits independently; Stage N+1 reads from Stage N's shipped subskills.

Open for implementer to arbitrate: exact SKILL.md preamble phrasing; whether `domain-context-load` returns a structured JSON block or a caveman-prose summary; whether `term-anchor-verify` emits a table or one-line-per-term verdict.

Locked by audit: the 7 subskill names; the 2 subagent names; the staged order (1 → 4); the caller count per subskill (6/4/3/8/3/1/1 for stages 1–3; 2 subagents for stage 4).

### 5.3 Method / algorithm notes (optional)

Not applicable — refactor is templated SKILL.md authoring + body wiring edits.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Audit-driven staged refactor, not big-bang rewrite | Each stage independently ships value; Stage 2 MCP recipe consolidation is biggest maintenance win | Big-bang umbrella rewrite — rejected (regression risk + loses cross-skill leverage) |
| 2026-04-17 | Subagents (not inline skills) for Stage 4 helper promotions | Skill = caller model context; only subagent has own Sonnet pin | Keep inline, downgrade umbrella to Sonnet — rejected (umbrella Phase 4 = real reasoning) |
| 2026-04-17 | Stage 5 follow-up audits scoped OUT | stage-decompose pin fix + project-new split + design-explore Phase 8 review warrant own rows | Fold all into this issue — rejected (scope creep) |
| 2026-04-17 | Router `no_matching_domain` accepted | Skill / IA infrastructure is not a router table domain | Force-fit to "Backlog / issues" — rejected (wrong surface) |

## 7. Implementation Plan

### Phase 1 — Stage 1 no-risk Sonnet extractions

- [ ] Author `ia/skills/progress-regen/SKILL.md` (Bash wrapper, no-model). Caveman preamble; invokes `npm run progress` from repo root; non-blocking contract.
- [ ] Update 6 callers: `master-plan-new`, `master-plan-extend`, `stage-decompose`, `stage-file`, `project-spec-close`, `project-stage-close`. Swap inline `npm run progress` blocks for `progress-regen` subskill reference.
- [ ] Author `ia/skills/cardinality-gate-check/SKILL.md` (Sonnet). Input: phase → tasks map. Output: `{phases_lt_2, phases_gt_6, phases_with_one_file, pause_or_proceed}`.
- [ ] Update 4 callers: `master-plan-new` Phase 6, `master-plan-extend` Phase 6, `stage-decompose` Phase 5, `stage-file` Phase 3.
- [ ] Author `ia/skills/surface-path-precheck/SKILL.md` (Sonnet). Globs every Architecture-block path, returns `[{path, exists, line_hint}]` with `(new)` markers.
- [ ] Update 3 callers: `master-plan-new` Phase 2 sub-step, `master-plan-extend` Phase 2 sub-step, `stage-decompose` Phase 3 sub-step.

### Phase 2 — Stage 2 shared MCP recipe

- [ ] Author `ia/skills/domain-context-load/SKILL.md` (Sonnet). Runs `glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary`. Flags: `brownfield_flag`, `tooling_only_flag`.
- [ ] Update 8 callers: `release-rollout` Phase 2, `release-rollout-enumerate`, `master-plan-new` Phase 2, `master-plan-extend` Phase 2, `stage-decompose` Phase 3, `stage-file` Phase 4, `project-new` steps 2–6, `design-explore` Phase 5.
- [ ] Author `ia/skills/term-anchor-verify/SKILL.md` (Sonnet). Per English term calls `glossary_lookup` + `router_for_task` + `spec_section`. Output per term: `{anchored, missing}`.
- [ ] Update 3+ callers: `release-rollout` Phase 3 (**Alignment gate**), `release-rollout-track` Phase 1 align verify, implicitly master-plan-authoring anchor checks + project-new router match verify.

### Phase 3 — Stage 3 rollout-specific splits

- [ ] Author `ia/skills/release-rollout-repo-sweep/SKILL.md` (Sonnet). Per-row Glob/Grep sweep returning pre-fill glyph map `{(a), (b), (c), (d), (e), (f)}`.
- [ ] Wire into `release-rollout-enumerate` Phase 1. Disagreement detection (Opus, Phase 2) consumes structured sweep output instead of re-deriving.
- [ ] Author `ia/skills/rollout-row-state/SKILL.md` (Sonnet). Input: `{TRACKER_SPEC, ROW_SLUG}`. Output: `{target_col, hard_gate: ⚠️|❓|ok, chain_ready, next_action}`.
- [ ] Wire into `release-rollout` Phase 1.

### Phase 4 — Stage 4 helper-to-subagent promotions

- [ ] Create `.claude/agents/release-rollout-track.md` (Sonnet pin). Copy helper SKILL.md contract; caveman directive preserved; invoked via Agent tool from `release-rollout` Phase 5.
- [ ] Create `.claude/agents/release-rollout-skill-bug-log.md` (Sonnet pin). Same treatment; invoked from `release-rollout` Phase 5 skill-bug branch.
- [ ] Update `release-rollout` SKILL.md + agent body: Phase 5 + skill-bug branch dispatch via Agent tool instead of inline helper call.
- [ ] Update 2 helper SKILL.md bodies: note dual-mode (inline fallback + subagent-dispatched canonical path).

### Phase 5 — Validate + audit doc tick

- [ ] `npm run validate:all` clean.
- [ ] Manual smoke: `/release-rollout` advance-one-row on `full-game-mvp-rollout-tracker.md` unregressed end-to-end.
- [ ] Tick `docs/release-rollout-model-audit.md` §7 stages 1–4 checkboxes (add status note `shipped 2026-MM-DD` per stage).
- [ ] Fix `tools/scripts/reserve-id.sh` PATH on macOS — `flock` lives at `/opt/homebrew/opt/util-linux/bin/flock` (not default `$PATH`). Surfaced by `project-new` subagent while filing TECH-302. Repro: run `reserve-id.sh` from a minimal shell without Homebrew's `util-linux/bin` prepended → `flock: command not found`. Fix: prepend `util-linux/bin` inside the script OR document the `PATH` requirement in script header. See Open Question #5 for single-issue-vs-separate-row decision.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| All 7 new SKILL.md files exist + 2 new agent files exist | File presence | Glob under `ia/skills/` + `.claude/agents/` | Manual review |
| IA doc links resolve | Node | `npm run validate:all` | Chains validate:dead-project-specs, validate:fixtures, generate:ia-indexes --check |
| Caller skills + agents reference new subskills (no duplicate recipe text) | Grep audit | Manual Grep for `glossary_discover.*router_for_task.*spec_sections` multi-line across callers | Any hit outside new `domain-context-load/SKILL.md` = leftover duplication |
| Rollout advance-one-row smoke unregressed | Agent report | `/release-rollout` dispatch on `full-game-mvp-rollout-tracker.md` | One row advance, tracker change-log appended; verify cell flips + handoff message intact |
| Audit doc §7 stages 1–4 ticked | File content | `docs/release-rollout-model-audit.md` §7 | Add `shipped 2026-MM-DD` status note per stage |

## 8. Acceptance Criteria

- [ ] 3 Stage 1 subskills shipped: `progress-regen`, `cardinality-gate-check`, `surface-path-precheck`; 6 + 4 + 3 callers updated.
- [ ] 2 Stage 2 shared MCP subskills shipped: `domain-context-load`, `term-anchor-verify`; 8 + 3 callers updated.
- [ ] 2 Stage 3 rollout-specific subskills shipped: `release-rollout-repo-sweep`, `rollout-row-state`; wired into enumerate + release-rollout.
- [ ] 2 Stage 4 Sonnet subagents live: `.claude/agents/release-rollout-track.md`, `.claude/agents/release-rollout-skill-bug-log.md`; `release-rollout` Phase 5 dispatches via Agent tool.
- [ ] All caller skill + agent bodies reference new shared subskills — no duplicate MCP recipe text left (Grep audit clean).
- [ ] `npm run validate:all` green.
- [ ] Manual `/release-rollout` advance-one-row smoke on `full-game-mvp-rollout-tracker.md` unregressed.
- [ ] Audit doc `docs/release-rollout-model-audit.md` §7 stages 1–4 checkboxes ticked w/ `shipped YYYY-MM-DD` status notes.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. **Single-issue vs `/master-plan-new` promotion** — This refactor has 4 shippable stages + 5 Phase 1 deliverables + ~11 callers touched. Audit §7 explicitly staged. Product owner decide: keep as single TECH-302 w/ 5 Phases (current shape), OR promote to umbrella master plan `ia/projects/release-rollout-refactor-master-plan.md` w/ 4 child master-plans (one per stage)? Recommendation: run `/design-explore docs/release-rollout-model-audit.md` next to decide; if Opus exploration surfaces more than 2 decision surfaces per stage, promote. Scope creep risk: single issue.
2. **Integration test harness per shared subskill** — Audit §9 flags this as open. Do we want lightweight fixture per `domain-context-load` / `term-anchor-verify` / `cardinality-gate-check` to catch drift when MCP tool surface changes? Cost vs benefit not yet measured. If product wants, file as separate BACKLOG row.
3. **Stage 5 follow-ups scope** — Any of the §6 cross-family follow-ups urgent enough to fold into Phase 5 of this issue vs held as separate audit? Specifically: `stage-decompose` missing `reasoning_effort` pin looks like a one-line typo fix. Filing separately might be unnecessary friction.
4. **Dual-mode helper SKILL.md** — After Stage 4, `release-rollout-track` + `release-rollout-skill-bug-log` exist both as SKILL.md (inline fallback) and `.claude/agents/*.md` (canonical Sonnet subagent). Is dual-mode worth the maintenance overhead, or should the helper skills be deleted in favor of subagent-only? Recommendation: keep dual-mode until subagent dispatch proven stable for one full rollout cycle, then delete SKILL.md.
5. **`reserve-id.sh` flock PATH fix — single-issue vs separate row** — Phase 5 task added for macOS `flock` PATH fix (surfaced by `project-new` subagent while filing TECH-302). Fold into this refactor because Phase 5 already touches validator infra + audit tick, OR file as standalone BUG row because root cause (shell PATH on macOS, Homebrew `util-linux/bin` not default) is orthogonal to rollout-family model-fit? Recommendation: keep folded in Phase 5 — one-line script edit, zero skill-graph impact, cheaper than a separate issue roundtrip. Re-file if scope grows beyond the one-liner.
