---
title: Release-rollout skill — model-fit + componentization audit
status: draft
author: agent-led
date: 2026-04-17
scope: >
  Audit of `release-rollout` skill family (main + 3 helpers) and the
  lifecycle subagents it dispatches. Identifies phase-level splits where
  Sonnet can replace Opus, shared patterns eligible for new reusable
  subskills, and the one or two places where Opus reasoning is
  irreplaceable.
related:
  - ia/skills/release-rollout/SKILL.md
  - ia/skills/release-rollout-enumerate/SKILL.md
  - ia/skills/release-rollout-track/SKILL.md
  - ia/skills/release-rollout-skill-bug-log/SKILL.md
  - .claude/agents/release-rollout.md
  - .claude/agents/design-explore.md
  - .claude/agents/master-plan-new.md
  - .claude/agents/master-plan-extend.md
  - .claude/agents/stage-decompose.md
  - .claude/agents/stage-file.md
  - .claude/agents/project-new.md
---

# Release-rollout skill — model-fit + componentization audit

## 1. What was audited

| Artifact | Kind | Current model |
|----------|------|---------------|
| `ia/skills/release-rollout/SKILL.md` | umbrella skill | runs inside `release-rollout` subagent (Opus high) |
| `.claude/agents/release-rollout.md` | subagent | **opus / high** |
| `ia/skills/release-rollout-enumerate/SKILL.md` | helper skill | runs inline in caller (Opus today) |
| `ia/skills/release-rollout-track/SKILL.md` | helper skill | runs inline in caller (Opus today) |
| `ia/skills/release-rollout-skill-bug-log/SKILL.md` | helper skill | runs inline in caller (Opus today) |
| dispatched — `design-explore` | subagent | opus / xhigh |
| dispatched — `master-plan-new` | subagent | opus / high |
| dispatched — `master-plan-extend` | subagent | opus / high |
| dispatched — `stage-decompose` | subagent | opus (no effort pin) |
| dispatched — `stage-file` | subagent | opus (no effort pin) |
| dispatched — `project-new` (transitive via `stage-file`) | subagent | opus / high |

All of the rollout family today is Opus. The three helper skills run **inline inside the umbrella Opus host** — they are not promoted to subagents, so mechanical work burns Opus tokens every time.

---

## 2. Model fit criteria used

- **Opus** — orchestration, cross-doc reasoning, disagreement detection, product-language interview, tier arbitration, plan decomposition, design synthesis.
- **Sonnet** — templated authoring with deterministic inputs, Glob/Grep classification, table edits, MCP fetch-and-classify, cardinality counting, string validation.
- **No-model (pure script)** — `npm run progress`, `npm run validate:*`, `git` — wrap in Bash, no LLM thinking needed.

Heuristic: if a phase has a clear "IF X THEN Y glyph" table, it is Sonnet. If a phase reads two docs and must decide whether they disagree, it is Opus.

---

## 3. Phase-by-phase grading

### 3.1 `release-rollout` (umbrella, Opus high today)

| Phase | Work | Current | Recommended |
|-------|------|---------|-------------|
| 0 — Load + validate | Read umbrella + tracker; STOP on missing inputs | Opus | **Sonnet** — pure presence checks + routing table |
| 1 — Row state read | Rightmost non-`✓` scan + hard-gate marker detection (`⚠️`, `❓`) | Opus | **Sonnet** — deterministic glyph scan; extract to `rollout-row-state` subskill |
| 2 — MCP context (Tool recipe) | `list_specs → glossary_discover → glossary_lookup → router_for_task → spec_sections → backlog_search → backlog_issue` | Opus | **Sonnet** — recipe is fixed order; extract to shared `domain-context-load` (see §4) |
| 3 — Align gate (col g) | Per-entity: `glossary_lookup` + `router_for_task` + `spec_section` all return anchor | Opus | **Sonnet** — pass/fail classifier; extract to shared `term-anchor-verify` (see §4) |
| 4 — Handoff dispatch | Product-language interview, (b)✓ auto-chain decision, parallel-work guard, Agent-tool subagent calls | Opus | **Stays Opus** — core reasoning + UX |
| 5 — Tracker update | Invoke `release-rollout-track` helper | Opus (inline) | **Sonnet** — promote helper to own subagent (see §5) |
| 6 — Next-row recommendation | Tier ordering + parallel-safety check + emit `claude-personal "..."` handoff | Opus | **Mostly Opus, can be Sonnet-assisted** — Tier lookup is deterministic; parallel-safety is rule-based. Arbitration between two near-equal candidates is Opus. |

**Gross estimate:** roughly 60 % of the umbrella skill body is mechanical bookkeeping that runs inside an Opus host today. Promoting Phases 0/1/5 and factoring out Phases 2/3 to Sonnet subskills removes the Opus tax on everything except the actual decision surface (Phase 4 + Phase 6 arbitration).

### 3.2 `release-rollout-enumerate` (one-shot seeder)

| Phase | Work | Current | Recommended |
|-------|------|---------|-------------|
| 0 — Load umbrella | Read bucket table + change log + parallel-work rule | inline Opus | **Sonnet** — parse-only |
| 1 — Repo reality sweep | Per-row: Glob exploration doc → expansion presence → child plan → Step count → Stage count → BACKLOG yaml count → map to (b)–(f) glyphs | inline Opus | **Sonnet** — deterministic decision table; ideal subskill (`release-rollout-repo-sweep`) |
| 2 — Disagreement detection | Compare repo reality vs umbrella bucket table: rename, split, sibling drift, exploration-doc intent mismatch | inline Opus | **Stays Opus** — cross-doc semantic compare + user-facing framing (Disagreements appendix prose is human-cold reading) |
| 3 — Tracker authoring | Template-filling: header / steps table / matrix / gate / disagreements / iteration log / handoff / change log | inline Opus | **Sonnet** for header + matrix + gate + iteration-log shells (template-driven); **Opus** for Disagreements appendix prose (narrative). Split along that boundary. |
| 4 — Handoff | Single caveman summary line | inline Opus | **Sonnet** |

### 3.3 `release-rollout-track` (cell updater)

| Phase | Work | Current | Recommended |
|-------|------|---------|-------------|
| 0 — Validate row/col/marker | String presence + enum check | inline Opus | **Sonnet** |
| 1 — Align verify (when col=e/g) | Same 3-MCP anchor check as rollout §3 | inline Opus | **Sonnet** — extract to shared `term-anchor-verify` |
| 1b — (f) filed-signal verify | Glob yaml + md, pair check | inline Opus | **Sonnet** — pure Glob classifier |
| 2 — Cell flip | Edit row's target column in place | inline Opus | **Sonnet** |
| 3 — Change log append | Append table row | inline Opus | **Sonnet** |
| 4 — Handoff | One-line summary | inline Opus | **Sonnet** |

**Entire skill is Sonnet-grade.** Zero reasoning once inputs are given. The parent Opus skill already decides which cell flips to which marker; this helper is the typing out of that decision. Promote to a Sonnet subagent.

### 3.4 `release-rollout-skill-bug-log`

| Phase | Work | Current | Recommended |
|-------|------|---------|-------------|
| 0 — Validate skill target | Path existence | inline Opus | **Sonnet** |
| 1 — Per-skill Changelog entry | Template fill + append newest-at-top | inline Opus | **Sonnet** — templated formatter |
| 2 — Tracker aggregator row | Table row append | inline Opus | **Sonnet** |
| 3 — Handoff | One-line summary | inline Opus | **Sonnet** |

**Entire skill is Sonnet-grade** — the Opus host already summarizes the bug into `BUG_SUMMARY` + `BUG_DETAIL` before calling; this skill just writes it to two places. Promote to Sonnet subagent.

### 3.5 Dispatched lifecycle subagents (out of rollout scope but called by it)

Rollout only dispatches these; no in-place change recommended to their model unless they carry similar internal splits. Flagged for **follow-up audits** (see §6):

- `design-explore` — Opus xhigh is correct for approach comparison + architecture. But Phase 5 MCP fetch + Phase 8 subagent review could factor the same `domain-context-load` pattern.
- `master-plan-new` / `master-plan-extend` — Opus high is correct for Step/Stage decomposition. But Phase 2 MCP recipe, Phase 6 cardinality gate, Phase 7b progress regen are mechanical and duplicated.
- `stage-decompose` — Opus. Same factoring opportunities.
- `stage-file` — Opus. Phase 4 (shared MCP context), Phase 6b (progress regen), Phase 5 loop (templated stub bootstrap) are mechanical.
- `project-new` — Opus high. The template-fill part (§11 copy + populate) is Sonnet-grade; the router / glossary disambiguation is Opus.

---

## 4. Componentization — new reusable subskills

Patterns that appear in ≥ 3 skills today. Extracting these avoids drift and concentrates maintenance.

### 4.1 `domain-context-load` (Sonnet)
- **What it does:** runs `glossary_discover → glossary_lookup → router_for_task → spec_sections → invariants_summary`, returns a single structured payload `{glossary_anchors, router_domains, spec_sections, invariants}`.
- **Inputs:** `keywords[]`, `brownfield_flag` (skip router+specs+invariants on greenfield), `tooling_only_flag` (skip invariants).
- **Reused by:** `release-rollout` Phase 2, `release-rollout-enumerate`, `master-plan-new` Phase 2, `master-plan-extend` Phase 2, `stage-decompose` Phase 3, `stage-file` Phase 4, `project-new` steps 2–6, `design-explore` Phase 5.
- **Impact:** kills ~8 copies of the same recipe text; single place to tune keyword strategy.

### 4.2 `term-anchor-verify` (Sonnet)
- **What it does:** for every English term passed in, calls `glossary_lookup` + `router_for_task` + `spec_section`; returns per-term `{anchored: bool, missing: [glossary|router|spec]}`.
- **Reused by:** `release-rollout` Phase 3 (align gate), `release-rollout-track` Phase 1 (align verify on (g)/(e)), implicitly by `master-plan-new` / `master-plan-extend` when validating new entities, `project-new` when verifying router match.
- **Impact:** codifies the glossary-spec anchor gate so drift between callers cannot happen.

### 4.3 `surface-path-precheck` (Sonnet)
- **What it does:** Globs every entry/exit path in an Architecture block; returns `[{path, exists, line_hint}]` with `(new)` markers for greenfield paths.
- **Reused by:** `master-plan-new` Phase 2 sub-step, `master-plan-extend` Phase 2 sub-step, `stage-decompose` Phase 3 sub-step.
- **Impact:** removes the "ghost line numbers downstream" footgun by centralizing the Glob-then-classify loop.

### 4.4 `cardinality-gate-check` (Sonnet)
- **What it does:** takes phase → tasks map; returns `{phases_lt_2: [...], phases_gt_6: [...], phases_with_one_file: [...]}` and a structured pause-or-proceed verdict.
- **Reused by:** `master-plan-new` Phase 6, `master-plan-extend` Phase 6, `stage-decompose` Phase 5, `stage-file` Phase 3.
- **Impact:** single place to adjust gate thresholds; every master-plan-authoring skill agrees on the rule.

### 4.5 `progress-regen` (no-model Bash wrapper, invocable as skill)
- **What it does:** runs `npm run progress` from repo root, logs exit code, does NOT block caller on non-zero.
- **Reused by:** `master-plan-new`, `master-plan-extend`, `stage-decompose`, `stage-file`, `project-spec-close`, `project-stage-close`, `release-rollout-track` (when cell flip closes a task).
- **Impact:** one shell wrapper, every caller follows the same non-blocking contract.

### 4.6 `release-rollout-repo-sweep` (Sonnet)
- **What it does:** carves Phase 1 of `release-rollout-enumerate` into its own skill — per-row Glob/Grep sweep that returns the pre-fill glyph map `{(a): ✓, (b): — | ✓ | stub, ...}`.
- **Reused by:** `release-rollout-enumerate` (first use), `release-rollout` Phase 0 on re-seed, ad-hoc tracker audit.
- **Impact:** makes the glyph decision table inspectable and testable in isolation; disagreement detection (which stays Opus) then consumes a structured input instead of re-deriving.

### 4.7 `rollout-row-state` (Sonnet)
- **What it does:** given `{TRACKER_SPEC, ROW_SLUG}`, returns `{target_col, hard_gate: ⚠️|❓|ok, chain_ready: bool, next_action: string}`.
- **Reused by:** `release-rollout` Phase 1, and (future) a dashboard endpoint that renders rollout health without dispatching anything.
- **Impact:** separates "what should happen next on this row" (deterministic glyph math) from "who dispatches it" (Opus umbrella).

---

## 5. Promote helpers from inline skills → Sonnet subagents

Both `release-rollout-track` and `release-rollout-skill-bug-log` are 100 % mechanical. Today they run inside the Opus umbrella host, which means the umbrella pays Opus tokens to do Edit/Write/append work.

**Proposal:**

| Helper | Surface |
|--------|---------|
| `release-rollout-track` | new `.claude/agents/release-rollout-track.md` (Sonnet), invoked from Phase 5 via Agent tool |
| `release-rollout-skill-bug-log` | new `.claude/agents/release-rollout-skill-bug-log.md` (Sonnet), invoked from Phase 5 sub-branch when subagent handoff reports a skill gap |

**Why subagents, not just "the same inline skill on Sonnet":** a skill runs in the caller's model context. Unless the umbrella is downgraded (it shouldn't be — Phase 4 is real reasoning), the inline skill is forced to Opus. A subagent has its own model pin and fresh context, so Sonnet carries the edit + its handoff message back to the Opus parent survives in one line.

**Cost expectation:** every advance-one-row invocation today includes at least one Opus-host cell flip + change log write + (sometimes) a skill bug log write. Moving those to Sonnet removes all three from the Opus caller's working set.

---

## 6. Cross-family follow-ups (beyond rollout)

Not part of this audit's primary scope, but surfaced while reading the dispatched subagents:

1. **`spec-implementer` is already Sonnet.** Confirm no Opus-grade reasoning hides in its Phase work — it looked solid.
2. **`stage-decompose` has no `reasoning_effort` pin** in the agent frontmatter (line 5). Every other Opus orchestrator pins `high` (or `xhigh`). This is a likely typo — audit and align.
3. **`project-new` Opus high feels heavy** for the template-fill half. Could either (a) split into `project-new-router` (Opus, decides prefix + depends-on ids) + `project-new-stub` (Sonnet, writes the spec + BACKLOG row) or (b) leave alone and accept the cost because single-issue calls are rare vs. `stage-file` batch.
4. **`design-explore` Phase 8 review subagent** could itself be Sonnet if the review prompt is tight (it is, per SKILL.md). Worth measuring.

---

## 7. Proposed action plan

### Stage 1 — No-risk extractions (pure Sonnet mechanical)
1. Author `ia/skills/progress-regen/SKILL.md` (Sonnet-grade, Bash wrapper). Update 6 callers to invoke it.
2. Author `ia/skills/cardinality-gate-check/SKILL.md`. Update 4 callers.
3. Author `ia/skills/surface-path-precheck/SKILL.md`. Update 3 callers.

### Stage 2 — Shared MCP recipe
4. Author `ia/skills/domain-context-load/SKILL.md` (Sonnet). Update 8 callers — biggest maintenance win.
5. Author `ia/skills/term-anchor-verify/SKILL.md` (Sonnet). Update 3+ callers (rollout Phase 3, track Phase 1, etc.).

### Stage 3 — Rollout-specific splits
6. Author `ia/skills/release-rollout-repo-sweep/SKILL.md` (Sonnet). Wire into `release-rollout-enumerate` Phase 1.
7. Author `ia/skills/rollout-row-state/SKILL.md` (Sonnet). Wire into `release-rollout` Phase 1.

### Stage 4 — Helper → subagent promotions
8. Create `.claude/agents/release-rollout-track.md` (Sonnet). Update `release-rollout` Phase 5 to dispatch via Agent tool.
9. Create `.claude/agents/release-rollout-skill-bug-log.md` (Sonnet). Same.

### Stage 5 — Follow-up audits (scoped out today)
10. Add `reasoning_effort: high` to `.claude/agents/stage-decompose.md` (likely a missed typo).
11. Revisit `project-new` split after Stage 2 reveals how much of its token cost was the shared MCP recipe.

---

## 8. Net picture

- **Stays Opus (reasoning surface):** rollout Phase 4 dispatch + interview, Phase 6 arbitration, enumerate Phase 2 disagreement detection, enumerate Phase 3 disagreements prose, `design-explore`, `master-plan-new`, `master-plan-extend` step/stage decomposition.
- **Moves to Sonnet (mechanical surface):** everything in `release-rollout-track` + `release-rollout-skill-bug-log`, rollout Phases 0/1/2/3/5, enumerate Phases 0/1/3 (template shells) /4, every cross-skill pattern in §4.
- **Moves to pure Bash:** `npm run progress` wrapper.

Net effect: the umbrella `release-rollout` subagent becomes a *thinner* Opus orchestrator whose context carries decisions and not edits. Sonnet subagents + skills handle the typing. Shared subskills kill ~8 copies of the same MCP recipe and anchor-check logic, so future tuning (e.g. when a new MCP tool gets added to the recipe) happens once.

---

## 9. Open questions

- Do we want each new shared subskill (§4) to be a **skill** (inline, inherits caller's model) or a **subagent** (own model pin)? Recommendation: skill for the 5 in §4, subagent for the 2 in §5. Rationale: skills are cheaper when the caller already has the right context; subagents are worth the cold-start when the work is big enough to amortize.
- Worth adding a lightweight integration test fixture per shared subskill to catch drift when the MCP tool surface changes?
- Any of the §6 follow-ups urgent enough to fold into Stage 1 of the plan, vs. held as a separate audit?
