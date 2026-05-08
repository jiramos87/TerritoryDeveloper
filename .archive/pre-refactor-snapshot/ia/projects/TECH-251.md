---
purpose: "TECH-251 — Adopt Claude Opus 4.7 across agent lifecycle."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-251 — Adopt Claude Opus 4.7 across agent lifecycle

> **Issue:** [TECH-251](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-16
> **Last updated:** 2026-04-16

<!--
  Filename: `ia/projects/TECH-251.md` (bare id back-compat).
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Glossary: ../specs/glossary.md (spec wins on conflict).
  Authoring style: caveman prose (drop articles/filler/hedging; fragments OK). Tables, code, seed prompts stay normal.
-->

## 1. Summary

Opus 4.7 shipped 2026-04-16 (pricing flat, coding +13%, 3× Rakuten-SWE-Bench prod resolution, +10% review recall, vision 2576px, loop resistance + output self-verification). Tech-debt sweep: doc drift fixes, `/ultrareview` wiring into `/verify-loop`, `xhigh` effort for critical-path orchestrators, prompt retune pass on skill bodies relying on soft instructions, `spec-implementer` 4.7 opt-in gated behind effort flag. Global subagent frontmatter bump out of scope — `model: opus` alias auto-resolves at dispatch.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Smoke-test gate Opus 4.7 on one low-blast-radius flow before broader rollout.
2. Fix `CLAUDE.md` §3 doc drift (verify-loop model family + subagent count 10 vs 11).
3. Wire `/ultrareview` terminal deep-review step into `/verify-loop` when bounded fix iterations converge.
4. Adopt `xhigh` effort level for `closeout` + `master-plan-new` (critical-path, review-loop dominated).
5. Prompt retune pass on skill bodies with implicit/soft instructions — start `master-plan-new`, `stage-decompose`, `project-spec-implement`.
6. `spec-implementer` Opus 4.7 opt-in behind effort flag; default stays Sonnet.
7. Extend `ide-bridge-evidence` SKILL to capture 2576px Unity evidence pairing w/ Opus 4.7 vision.
8. 2-week cost monitoring window — per-agent token counts pre/post tokenizer flip.

### 2.2 Non-Goals (Out of Scope)

1. Global bulk frontmatter bump to versioned `claude-opus-4-7` — alias `model: opus` handles auto-upgrade at router dispatch.
2. 4.7 file-system memory API wiring — separate work; MCP `project_spec_journal_*` already covers cross-session continuity.
3. Task budgets GA rollout — currently public beta; revisit when stable.
4. Rewriting every skill body — retune targets only soft-instruction surfaces.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Run `/verify-loop` on a stuck fix iteration and get `/ultrareview` deep-review pass before bailout | Terminal step fires when `MAX_ITERATIONS` hit; review output surfaced in loop report |
| 2 | Developer | Trigger `/implement` on a complex phase and opt in to Opus 4.7 via effort flag | Flag documented in skill body; default Sonnet behavior preserved when flag absent |
| 3 | Developer | View per-agent token cost for 2-week window to validate tokenizer flip impact | Cost log artifact produced; per-agent deltas visible |

## 4. Current State

### 4.1 Domain behavior

Agent lifecycle uses `model: opus` + `model: sonnet` aliases in `.claude/agents/*.md` frontmatter. Claude Code router resolves alias → family default at dispatch. `CLAUDE.md` §3 table lists verify-loop as Sonnet but `.claude/agents/verify-loop.md` frontmatter pins Opus — drift. Count says "10 native subagents" but 11 exist (stage-decompose added post-doc).

Verified agent map (2026-04-16):
- Opus (8): design-explore, master-plan-new, stage-decompose, stage-file, project-new, spec-kickoff, verify-loop, closeout
- Sonnet (3): spec-implementer, verifier, test-mode-loop

### 4.2 Systems map

Surfaces touched:
- `.claude/agents/*.md` — 11 subagent frontmatter (read-only check; no bump)
- `.claude/commands/*.md` — slash dispatchers (`/ultrareview` wiring target)
- `.claude/output-styles/{verification-report,closeout-digest}.md` — review output formatting
- `ia/skills/master-plan-new/SKILL.md` — prompt retune
- `ia/skills/stage-decompose/SKILL.md` — prompt retune
- `ia/skills/project-spec-implement/SKILL.md` — prompt retune + 4.7 opt-in flag
- `ia/skills/project-spec-kickoff/SKILL.md` — prompt retune candidate
- `ia/skills/verify-loop/SKILL.md` — `/ultrareview` terminal step
- `ia/skills/close-dev-loop/SKILL.md` — prompt retune candidate
- `ia/skills/ide-bridge-evidence/SKILL.md` — 2576px vision evidence
- `CLAUDE.md` §1 (model IDs), §3 (subagent table)
- `docs/agent-led-verification-policy.md` — `/ultrareview` policy note
- `ia/rules/agent-lifecycle.md` — lifecycle table refresh

### 4.3 Implementation investigation notes (optional)

Anthropic release summary ref: https://www.anthropic.com/news/claude-opus-4-7. Key specs:
- Model id: `claude-opus-4-7`
- Pricing: $5/$25 per 1M in/out (unchanged vs 4.6)
- Tokenizer: 1.0–1.35× more tokens per same content; net favorable on internal coding evals
- New effort level: `xhigh` ("extra high")
- New slash cmd: `/ultrareview` — dedicated bug/design review session
- Vision: up to 2576px long-edge (~3.75MP, >3× prev)
- File-system memory improvements for multi-session work
- Agent improvements: loop resistance, tool-failure recovery, output self-verification before reporting
- Behavioral shift: stricter/more literal instruction following — existing prompts may need retune

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A — tooling/IA-only issue. No player-visible game rules affected.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phased rollout — smoke-test gate blocks broader adoption until pilot passes. Alias-based auto-upgrade means zero-touch for most subagents. Versioned pins reserved for surfaces requiring early lock-in (smoke-test pilot, any agent needing stability during retune pass).

### 5.3 Method / algorithm notes (optional)

`/ultrareview` integration into `/verify-loop`:
- Trigger: bounded fix iteration hits `MAX_ITERATIONS` (default 2) w/ unresolved failures
- Mode: terminal deep-review pass (read-only) before loop bailout
- Output: review findings appended to verify-loop report; does NOT spawn additional fix iterations

Opus 4.7 opt-in for `spec-implementer`:
- Default: Sonnet (cost optimization preserved)
- Opt-in trigger: effort flag in spec Implementation Plan phase metadata OR explicit `/implement --effort=xhigh`
- Justification: 3× Rakuten-SWE-Bench prod task resolution + 13% coding bench lift on complex refactor/algorithm phases
- Trade: ~5× cost/token + tokenizer 1.0–1.35× bloat

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-16 | No global frontmatter bump to `claude-opus-4-7` | Alias `model: opus` auto-resolves at Claude Code router dispatch — zero-touch upgrade | Versioned pin across all 11 subagents (rejected — churn w/o benefit; alias handles it) |
| 2026-04-16 | Smoke-test gate on one flow before broader rollout | Stricter literal instruction following may regress caveman preamble + cardinality hints | Adopt everywhere day 1 (rejected — prompt behavior shift risk) |
| 2026-04-16 | `spec-implementer` default stays Sonnet | ~5× cost delta; most implementations don't need 3× prod gains | Default Opus 4.7 (rejected — cost inflation for routine phases) |

## 7. Implementation Plan

### Phase 1 — Smoke-test gate

- [ ] Pilot Opus 4.7 on `/design-explore` (low-blast-radius; exploratory output easy to review)
- [ ] Use versioned `claude-opus-4-7` in pilot frontmatter to force 4.7 before alias flip
- [ ] Validate caveman preamble compliance + cardinality hints intact
- [ ] Document findings + go/no-go for broader rollout

### Phase 2 — Doc drift fixes

- [ ] `CLAUDE.md` §3 — correct verify-loop model family (Opus, not Sonnet per frontmatter)
- [ ] `CLAUDE.md` §3 — subagent count "11 native subagents" (was "10"); ensure stage-decompose listed
- [ ] `CLAUDE.md` §1 — refresh model family line to include 4.7
- [ ] `ia/rules/agent-lifecycle.md` — verify lifecycle table matches reality

### Phase 3 — `/ultrareview` wiring into `/verify-loop`

- [ ] Design `/ultrareview` terminal step trigger (bounded iteration exhausted)
- [ ] Update `ia/skills/verify-loop/SKILL.md` — add terminal step definition
- [ ] Update `.claude/agents/verify-loop.md` — invoke `/ultrareview` on bailout
- [ ] Cross-reference in `docs/agent-led-verification-policy.md`

### Phase 4 — `xhigh` effort for critical-path orchestrators

- [ ] Identify effort-flag wiring in `.claude/agents/closeout.md` + `.claude/agents/master-plan-new.md`
- [ ] Apply `xhigh` effort level to both
- [ ] Document rationale in each agent body (review-loop dominated, not token-dominated)

### Phase 5 — Prompt retune pass

- [ ] Audit `ia/skills/master-plan-new/SKILL.md` for implicit/soft instructions; convert to explicit directives
- [ ] Audit `ia/skills/stage-decompose/SKILL.md` — same
- [ ] Audit `ia/skills/project-spec-implement/SKILL.md` — same
- [ ] Spot-check caveman preambles + cardinality hints survive stricter adherence

### Phase 6 — `spec-implementer` Opus 4.7 opt-in

- [ ] Add effort flag to `.claude/agents/spec-implementer.md`
- [ ] Document opt-in trigger in `ia/skills/project-spec-implement/SKILL.md`
- [ ] Default path unchanged (Sonnet); opt-in gates complex phases only

### Phase 7 — Vision evidence extension

- [ ] Extend `ia/skills/ide-bridge-evidence/SKILL.md` — capture 2576px Unity Game view + Console evidence
- [ ] Pair w/ Opus 4.7 vision for HeightMap visual debug, road-cache visual invariants

### Phase 8 — Cost monitoring

- [ ] Log token counts per agent run for 2-week window
- [ ] Surface via `tools/scripts/claude-hooks/` or telemetry hook
- [ ] Validate tokenizer 1.0–1.35× bloat vs coding-bench gains on internal runs

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Doc drift + IA consistency (CLAUDE.md §3, agent-lifecycle.md, SKILL bodies) | Node | `npm run validate:all` (repo root) | Chains `validate:dead-project-specs`, `test:ia`, `validate:fixtures`, `generate:ia-indexes --check` |
| Smoke-test pilot outcome (caveman preamble + cardinality hints intact) | Agent report | `/design-explore` pilot run transcript + reviewer gate | Manual review; no CI equivalent |
| `/ultrareview` wiring into `/verify-loop` | Agent report | Trigger `/verify-loop` on synthetic stuck iteration; confirm terminal step fires | Capture in verification block |
| Cost monitoring window | Telemetry | Token-count log artifact under `tools/scripts/claude-hooks/` or equivalent | 2-week retention minimum |

## 8. Acceptance Criteria

- [ ] Phase 1 smoke-test gate: pilot on `/design-explore` w/ versioned `claude-opus-4-7` passes caveman preamble + cardinality hints compliance check
- [ ] `CLAUDE.md` §3 model column matches `.claude/agents/*.md` frontmatter; subagent count reads "11 native subagents" (stage-decompose listed)
- [ ] `/ultrareview` terminal step wired into `/verify-loop` on bounded-iteration bailout
- [ ] `xhigh` effort level applied to `closeout` + `master-plan-new` w/ rationale documented
- [ ] Prompt retune pass landed for `master-plan-new` + `stage-decompose` + `project-spec-implement` SKILL bodies — soft instructions converted to explicit directives
- [ ] `spec-implementer` Opus 4.7 opt-in flag documented in agent body + SKILL; default Sonnet preserved when flag absent
- [ ] `ia/skills/ide-bridge-evidence/SKILL.md` captures 2576px Unity evidence steps
- [ ] 2-week cost monitoring artifact produced; per-agent token deltas (pre/post tokenizer flip) visible
- [ ] `npm run validate:all` exits 0

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

<!--
  Tooling-only spec: no gameplay change. Policy-style questions allowed.
-->

1. Does `/ultrareview` terminal step count against `MAX_ITERATIONS` budget or run outside it? (Proposed: outside — purely diagnostic, not a fix attempt.)
2. Should `spec-implementer` opt-in flag live in spec Implementation Plan phase metadata (per-phase granular) or CLI arg (per-run)? Or both?
3. Cost monitoring retention — 2 weeks enough to validate tokenizer flip net-favorable claim, or extend to 4 weeks to cover typical stage cycle?
4. Smoke-test pilot flow — is `/design-explore` the right choice vs e.g. `/stage-decompose`? Exploratory output may not stress stricter adherence as hard as structured decomposition.
