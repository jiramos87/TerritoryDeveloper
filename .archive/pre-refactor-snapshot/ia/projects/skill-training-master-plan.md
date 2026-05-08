# Skill Training — Master Plan (IA / Skill Lifecycle, tooling-only)

> **Status:** In Progress — Step 2 / Stage 2.1
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

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Step 1 — skill-train Core + Glossary Foundation

**Status:** Final

**Backlog state (Step 1):** 8 filed, 8 closed — TECH-367..TECH-370 (Stage 1.1 Final), TECH-392..TECH-395 (Stage 1.2 Final)

**Objectives:** Author the `skill-train` SKILL.md body, Opus subagent, and slash-command dispatcher. Simultaneously land the 4 glossary rows and docs surface-map update so cross-refs in Step 2 wiring are backed by canonical terms (invariant #12). On step close, `/skill-train {SKILL_NAME}` is an executable command and all skill-training terminology is MCP-queryable.

**Exit criteria:**

- `ia/skills/skill-train/SKILL.md` exists: Phase 0–5 sequence, §Schema block (`skill_self_report` JSON + `schema_version` date-stamp), §Emitter stanza template (copy-paste ready for 13 skills), Guardrails ("do NOT apply", "do NOT commit"), Seed prompt.
- `.claude/agents/skill-train.md` (Opus) + `.claude/commands/skill-train.md` present; dispatcher forwards all args to subagent.
- `ia/specs/glossary.md` carries 4 new rows: `skill self-report`, `skill training`, `patch proposal (skill)`, `skill-train`.
- `docs/agent-lifecycle.md` §Surface map carries `/skill-train` row tagged "retrospective only".
- `CLAUDE.md §3` + `AGENTS.md` carry one-paragraph pointer each.
- `npm run validate:all` exits 0.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `docs/skill-training-exploration.md §Design Expansion — Chosen Approach + Architecture + Implementation Points Phase A + Phase C` — ground truth for this step.
- `ia/specs/glossary.md` (exists: true) — 4 additive rows; must precede any cross-ref.
- `docs/agent-lifecycle.md` (exists: true) — §Surface map, 1 additive row.
- `CLAUDE.md` (exists: true), `AGENTS.md` (exists: true) — §3 / skill-section additive paragraphs.
- `.claude/agents/release-rollout-skill-bug-log.md` (exists: true) — shape reference for new agent.
- `ia/skills/skill-train/` (new — all files net-new).
- `.claude/agents/skill-train.md` (new), `.claude/commands/skill-train.md` (new).
- Invariant #12: glossary rows must land in Stage 1.1 before Stage 1.2 authors cross-refs.

---

#### Stage 1.1 — Glossary + Docs Foundation

**Status:** Final (4 of 4 done: TECH-367, TECH-368, TECH-369, TECH-370)

**Objectives:** Land 4 canonical glossary terms and the docs surface-map update before any cross-ref is authored in Stage 1.2 or Step 2. Satisfies invariant #12.

**Exit:**

- `ia/specs/glossary.md`: 4 rows added — `skill self-report`, `skill training`, `patch proposal (skill)`, `skill-train`. MCP `glossary_discover "skill self-report"` returns a match.
- `docs/agent-lifecycle.md §Surface map`: `/skill-train` row present (Retrospective, Opus, outside main lifecycle flow).
- `CLAUDE.md §3` + `AGENTS.md`: one-paragraph pointer added to each.
- `npm run validate:all` exits 0.

**Phases:**

- [x] Phase 1 — Glossary rows + agent-lifecycle.md surface map row.
- [x] Phase 2 — CLAUDE.md §3 + AGENTS.md one-paragraph pointers.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.1.1 | Glossary rows × 4 | 1 | **TECH-367** | Done (archived) | Add 4 rows to `ia/specs/glossary.md` (Documentation category): `skill self-report` — structured JSON emitted by lifecycle skill at handoff when friction detected; `skill training` — retrospective Changelog-driven proposal loop; `patch proposal (skill)` — unified-diff proposal against SKILL.md Phase sequence / Guardrails / Seed prompt, stored as `ia/skills/{name}/train-proposal-{YYYY-MM-DD}.md`; `skill-train` — Opus consumer subagent + slash command for on-demand skill retrospective. Cross-ref between rows where applicable. |
| T1.1.2 | agent-lifecycle.md surface row | 1 | **TECH-368** | Done (archived) | Add `/skill-train` row to `docs/agent-lifecycle.md §Surface map` table — Stage: Retrospective; Slash command: `/skill-train`; Subagent: `skill-train`; Skill: `skill-train`; Model: Opus. Add inline note "retrospective only — outside main lifecycle flow". |
| T1.1.3 | CLAUDE.md §3 pointer | 2 | **TECH-369** | Done (archived) | Add row to `CLAUDE.md §3` key files table: `ia/skills/skill-train/SKILL.md` — on-demand skill retrospective; reads Per-skill Changelog; proposes unified-diff patch against Phase sequence / Guardrails / Seed prompt sections. Caveman prose. |
| T1.1.4 | AGENTS.md pointer | 2 | **TECH-370** | Done (archived) | Add one-paragraph entry to `AGENTS.md` under the skill-lifecycle / retrospective section (create section if absent): explains `skill-train` role — reads accumulated Per-skill Changelog entries, aggregates recurring friction (≥2 occurrences threshold), writes `train-proposal-{DATE}.md` sibling file. Caveman prose. |

---

#### Stage 1.2 — skill-train Skill Body + Agent + Command

**Status:** Final

**Backlog state (2026-04-18):** 4 tasks filed (TECH-392, TECH-393, TECH-394, TECH-395 archived).

**Objectives:** Author `ia/skills/skill-train/SKILL.md` with full Phase 0–5 sequence, canonical §Schema block, §Emitter stanza template (single source of truth for Step 2), and guardrails. Create matching `.claude/agents/skill-train.md` Opus subagent and `.claude/commands/skill-train.md` dispatcher.

**Exit:**

- `ia/skills/skill-train/SKILL.md`: Phase 0–5 sequence; `skill_self_report` JSON schema block with `schema_version`; `§Emitter stanza template` section (verbatim copy-paste block for 13 skills); Guardrails include "do NOT apply", "do NOT touch other skills", "do NOT commit"; Seed prompt block.
- `.claude/agents/skill-train.md` (Opus): accepts SKILL_NAME (required), `--since {YYYY-MM-DD}`, `--threshold N`, `--all` (with explicit Opus-cost warning); caveman preamble; mirrors `release-rollout-skill-bug-log.md` header shape.
- `.claude/commands/skill-train.md`: thin dispatcher; forwards SKILL_NAME + all optional flags; caveman preamble.
- `npm run validate:all` exits 0.

**Phases:**

- [x] Phase 1 — SKILL.md body (Phase 0–5 + §Schema block + §Emitter stanza template).
- [x] Phase 2 — Agent + command dispatcher.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.2.1 | skill-train SKILL.md body | 1 | **TECH-392** | Done (archived) | Create `ia/skills/skill-train/SKILL.md`. Phase 0: validate target SKILL.md exists + §Changelog present (inject if absent). Phase 1: read Changelog entries since last `source: train-proposed` entry (or `--since` date). Phase 2: aggregate `friction_types` — recurring = ≥2 occurrences (`--threshold N` overrides). Phase 3: synthesize unified diff targeting Phase sequence / Guardrails / Seed prompt sections of target skill. Phase 4: write `ia/skills/{SKILL_NAME}/train-proposal-{YYYY-MM-DD}.md`; append Changelog pointer entry `source: train-proposed`. Phase 5: handoff — path + friction-count + "review + apply manually". §Schema block defines `skill_self_report` JSON: `{skill, run_date, schema_version, friction_types[], guardrail_hits[], phase_deviations[], missing_inputs[], severity}`. Guardrails: do NOT apply patch; do NOT touch other skills' SKILL.md; do NOT commit. |
| T1.2.2 | Emitter stanza template section | 1 | **TECH-393** | Done (archived) | Add `## Emitter stanza template` section to `skill-train/SKILL.md` — canonical Phase-N-tail block for lifecycle skills to copy verbatim: (1) friction-condition check (`guardrail_hits > 0 OR phase_deviations > 0 OR missing_inputs > 0`); (2) construct `skill_self_report` JSON block; (3) append §Changelog entry `source: self-report` with schema_version date-stamp. Clean run (all conditions false) → no-op, §Changelog untouched. This section is the single source of truth consumed in T2.1.1, T2.1.2, T2.2.1, T2.2.2. |
| T1.2.3 | skill-train agent | 2 | **TECH-394** | Done (archived) | Create `.claude/agents/skill-train.md` (Opus subagent). Mirror `.claude/agents/release-rollout-skill-bug-log.md` header shape: title, model, caveman preamble directive. Inputs: SKILL_NAME (required); `--since {YYYY-MM-DD}` optional; `--threshold N` optional (default 2); `--all` flag carries explicit token-cost warning. Body delegates to `ia/skills/skill-train/SKILL.md` Phase 0–5. No auto-apply; no self-commit. |
| T1.2.4 | skill-train command | 2 | **TECH-395** | Done (archived) | Create `.claude/commands/skill-train.md` — thin dispatcher. Caveman preamble. Forwards `{SKILL_NAME}` (required), `--since`, `--all`, `--threshold` args to `skill-train` subagent via Agent tool call. One-paragraph body. |

---

### Step 2 — Phase-N-tail Wiring (13 Lifecycle Skills)

**Status:** In Progress — Stage 2.1

**Backlog state (Step 2):** 4 filed (TECH-430, TECH-431, TECH-432, TECH-433 — Stage 2.1 Draft)

**Objectives:** Wire all 13 lifecycle and rollout-family skills with an identical Phase-N-tail stanza copied verbatim from `skill-train/SKILL.md §Emitter stanza template`. Each skill gains structured self-report emission triggered on friction detection (guardrail hits, phase deviations, missing inputs), plus a §Changelog section if not present. On step close, every in-scope skill can feed the `skill-train` consumer with structured signal.

**Exit criteria:**

- All 13 `ia/skills/*/SKILL.md` files carry: (a) Phase-N-tail stanza — verbatim template copy, `schema_version` stamped; (b) `## Changelog` section.
- Friction check logic covers `guardrail_hits`, `phase_deviations`, `missing_inputs`; clean runs stay silent.
- `release-rollout-skill-bug-log/SKILL.md` NOT modified (sibling producer — separate channel).
- `npm run validate:all` exits 0.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `ia/skills/skill-train/SKILL.md §Emitter stanza template` — canonical block; copy verbatim, do NOT paraphrase.
- `docs/skill-training-exploration.md §Design Expansion — Implementation Points Phase B` — 13-skill list + stanza spec.
- `ia/skills/release-rollout-skill-bug-log/SKILL.md` — sibling producer; §Changelog shape reference; do NOT modify.
- Step 1 outputs (prior step): glossary terms MCP-queryable; `skill-train/SKILL.md §Emitter stanza template` present.
- 13 target SKILL.md paths (all exist: true): `ia/skills/design-explore/SKILL.md`, `master-plan-new/SKILL.md`, `master-plan-extend/SKILL.md`, `stage-decompose/SKILL.md`, `stage-file/SKILL.md`, `project-new/SKILL.md`, `project-spec-kickoff/SKILL.md`, `project-spec-implement/SKILL.md`, `project-stage-close/SKILL.md`, `project-spec-close/SKILL.md`, `release-rollout/SKILL.md`, `release-rollout-enumerate/SKILL.md`, `release-rollout-track/SKILL.md`.

---

#### Stage 2.1 — Core Authoring + Filing Skills (6 skills)

**Status:** In Progress — TECH-433 (4 of 4 filed: TECH-430, TECH-431, TECH-432, TECH-433)

**Objectives:** Wire the 6 authoring-and-filing lifecycle skills (`design-explore`, `master-plan-new`, `master-plan-extend`, `stage-decompose`, `stage-file`, `project-new`) with Phase-N-tail stanzas.

**Exit:**

- All 6 SKILL.md files carry Phase-N-tail stanza (verbatim template, `schema_version` stamped) + `## Changelog` section.
- Stanza placed at final handoff phase in each skill's existing Phase sequence.
- `npm run validate:all` exits 0.

**Phases:**

- [ ] Phase 1 — design-explore, master-plan-new, master-plan-extend + stage-decompose, stage-file, project-new wiring.
- [ ] Phase 2 — Cross-read consistency check + validate:all.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | Wire authoring-trio Phase-N-tail | 1 | **TECH-430** | Done (archived) | Edit `ia/skills/design-explore/SKILL.md`, `master-plan-new/SKILL.md`, `master-plan-extend/SKILL.md`: append Phase-N-tail stanza verbatim from `skill-train/SKILL.md §Emitter stanza template`; inject `## Changelog` section if absent; place stanza at existing handoff Phase N position. Verify `schema_version` date-stamp on all 3. |
| T2.1.2 | Wire filing-trio Phase-N-tail | 1 | **TECH-431** | Done (archived) | Edit `ia/skills/stage-decompose/SKILL.md`, `stage-file/SKILL.md`, `project-new/SKILL.md`: same procedure as T2.1.1. Stanza at final handoff phase; §Changelog injected if absent; schema_version present on all 3. |
| T2.1.3 | Cross-read stanza consistency | 2 | **TECH-432** | Done (archived) | Cross-read all 6 wired SKILL.md files; verify stanza text matches canonical template character-for-character (no paraphrase); `schema_version` stamps identical across all 6; `## Changelog` sections present. Document any deviation found in the relevant skill's §Changelog as `source: wiring-review`. |
| T2.1.4 | validate:all post Stage 2.1 | 2 | **TECH-433** | In Progress | Run `npm run validate:all` from repo root; confirm exit 0. Surface any frontmatter/index failures introduced by skill edits; fix inline before closing stage. |

---

#### Stage 2.2 — Spec Lifecycle + Rollout-Family Skills (7 skills)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Wire the 4 spec-lifecycle skills (`project-spec-kickoff`, `project-spec-implement`, `project-stage-close`, `project-spec-close`) and 3 rollout-family skills (`release-rollout`, `release-rollout-enumerate`, `release-rollout-track`) with identical Phase-N-tail stanzas.

**Exit:**

- All 7 SKILL.md files carry Phase-N-tail stanza + `## Changelog` section.
- All 13 skills total (Stages 2.1 + 2.2) confirmed consistent via final validation pass.
- `release-rollout-skill-bug-log/SKILL.md` untouched.
- `npm run validate:all` exits 0.

**Phases:**

- [ ] Phase 1 — Spec lifecycle + rollout-family wiring.
- [ ] Phase 2 — Full 13-skill validation + AGENTS.md wiring-complete entry.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | Wire spec-lifecycle Phase-N-tail | 1 | _pending_ | _pending_ | Edit `ia/skills/project-spec-kickoff/SKILL.md`, `project-spec-implement/SKILL.md`, `project-stage-close/SKILL.md`, `project-spec-close/SKILL.md`: append Phase-N-tail stanza verbatim; inject §Changelog if absent. `project-spec-implement` + `project-spec-close` carry caveman preambles — preserve unchanged. |
| T2.2.2 | Wire rollout-family Phase-N-tail | 1 | _pending_ | _pending_ | Edit `ia/skills/release-rollout/SKILL.md`, `release-rollout-enumerate/SKILL.md`, `release-rollout-track/SKILL.md`: append Phase-N-tail stanza verbatim; inject §Changelog if absent. Do NOT touch `release-rollout-skill-bug-log/SKILL.md` — sibling producer with separate `source: user-logged` channel; modifying it would break dual-producer alignment. |
| T2.2.3 | Full 13-skill consistency + validate | 2 | _pending_ | _pending_ | Cross-read all 13 SKILL.md files; verify stanza text matches template on every file; `schema_version` stamps all match; `## Changelog` present on all 13. Run `npm run validate:all`; exit 0 required before closing stage. |
| T2.2.4 | AGENTS.md wiring-complete entry | 2 | _pending_ | _pending_ | Append wiring-complete entry to `AGENTS.md` skill-train section: list the 13 wired skills with their SKILL.md paths; date-stamp; note `release-rollout-skill-bug-log` is sibling producer (not wired, unchanged). Signals to future readers that `skill-train` consumer is ready to aggregate. |

---

### Step 3 — Caveman Soft-Lint (Phase D)

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 3):** 0 filed

**Objectives:** Implement a diff-scoped warn-only bash script that scans new/modified caveman surfaces (skill prose, agent bodies, command dispatchers, project spec §1–§10) for long-form English patterns. Wire as opt-in pre-commit hook behind `SKILL_TRAIN_LINT=1` env var. Document in `ia/rules/agent-output-caveman-authoring.md`. Exit 0 always — intent is visibility, not blocking.

**Exit criteria:**

- `tools/scripts/caveman-lint.sh` present and executable; skips fenced code blocks; counts long-form-English indicators; outputs `file:line:indicator` summary; exits 0 always.
- Hook wired warn-only in `.claude/settings.json` behind `SKILL_TRAIN_LINT=1` env var.
- `ia/rules/agent-output-caveman-authoring.md` carries `## Soft-lint` section: what script checks, how to enable, known false-positive surfaces.
- Smoke-test against current repo diff confirms exit 0 + summary output.
- `npm run validate:all` exits 0.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `docs/skill-training-exploration.md §Design Expansion — Implementation Points Phase D` — script spec + warn-only + env-var opt-in design.
- `docs/skill-training-exploration.md §Review Notes` — "Phase D parallelizable; if total effort > 5 dev days, split into separate TECH- issue."
- `ia/rules/agent-output-caveman-authoring.md` (exists: true) — append `## Soft-lint` section.
- `.claude/settings.json` (exists: true) — hook wiring patterns reference.
- `tools/scripts/` (exists: true) — new script lands here.
- Step 1 + Step 2 outputs: all 13 skills wired (state when Step 3 opens).

---

#### Stage 3.1 — Lint Script + Hook + Docs

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Author `caveman-lint.sh`, wire as warn-only opt-in hook, document usage in caveman-authoring rule.

**Exit:**

- `tools/scripts/caveman-lint.sh` executable; outputs `N indicators found in M files (warn-only)` summary; exits 0 on zero or more findings; skips fenced blocks confirmed via smoke-test.
- Hook wired; `SKILL_TRAIN_LINT=1` gate confirmed warn-only (does not block tool execution).
- `ia/rules/agent-output-caveman-authoring.md §Soft-lint` present.
- `npm run validate:all` exits 0.

**Phases:**

- [ ] Phase 1 — Script authoring + hook wiring.
- [ ] Phase 2 — Documentation + smoke-test.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.1.1 | caveman-lint.sh | 1 | _pending_ | _pending_ | Create `tools/scripts/caveman-lint.sh`. Input: `git diff --cached` (pre-commit) or `git diff HEAD` (manual). Scope: `ia/skills/*/SKILL.md`, `.claude/agents/*.md`, `.claude/commands/*.md`, `ia/projects/*.md` (§1–§10 prose only). Skip fenced blocks (``` delimiters). Long-form indicators: sentences > 12 words, articles ("the"/"a"/"an") in prose context, hedging verbs ("should"/"might"/"could"/"would"). Output per hit: `file:line:indicator`. Exit 0 always. Footer: `N indicators found in M files (warn-only).` |
| T3.1.2 | Hook wiring in settings.json | 1 | _pending_ | _pending_ | Add warn-only hook to `.claude/settings.json` hooks array: guard on `SKILL_TRAIN_LINT=1` env var; call `tools/scripts/caveman-lint.sh`; hook exit code forced 0 (warn-only posture). Wire as PreToolUse on Write/Edit tool calls, matching patterns of existing hooks in settings.json. |
| T3.1.3 | Caveman-authoring docs | 2 | _pending_ | _pending_ | Add `## Soft-lint` section to `ia/rules/agent-output-caveman-authoring.md`: what script checks (indicator list), how to enable (`export SKILL_TRAIN_LINT=1`), how to read output (file:line refs), warn-only rationale, known false-positive-free surfaces (fenced blocks already skipped). |
| T3.1.4 | Smoke-test + validate | 2 | _pending_ | _pending_ | Run `bash tools/scripts/caveman-lint.sh` against current repo diff (at least 1 skill SKILL.md modified from Step 2). Verify exit 0; verify summary line present; spot-check that no fenced-block lines appear in output. Run `npm run validate:all`; exit 0. Note smoke-test result in script header comment. |

---

### Step 4 — Dogfood Cycle (Phase E)

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 4):** 0 filed

**Objectives:** Validate the full loop end-to-end: a real lifecycle skill run emits a self-report, `skill-train` consumer reads it, produces a coherent patch proposal, and the meta-dogfood pass (`/skill-train skill-train`) surfaces any second-order gaps in the skill-train body itself. Prompt + schema iterate if signal is weak before this step closes.

**Exit criteria:**

- `/skill-train design-explore` executed; `ia/skills/design-explore/train-proposal-{DATE}.md` produced with ≥1 friction point + diff-format patch suggestion.
- Proposal quality confirmed sufficient by user (signal not weak: friction type aligns with known Changelog entries or observed run behavior).
- If prompt/schema iterated: changes committed to `skill-train/SKILL.md`; §Changelog entry (`source: iteration`) notes what changed and why.
- `/skill-train skill-train` executed (meta-dogfood); outcome recorded in `skill-train/SKILL.md §Changelog` (`source: dogfood-result`).
- `npm run validate:all` exits 0. Orchestrator `Status: Final`.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `ia/skills/skill-train/SKILL.md` — consumer Phase 0–5 + §Schema (from Step 1).
- `ia/skills/design-explore/SKILL.md §Changelog` — target for first retrospective; expected ≥1 `source: self-report` entry from Step 2 wiring.
- `docs/skill-training-exploration.md §Design Expansion — Implementation Points Phase E` — dogfood criteria: "`design-explore` (highest run count)" + "meta-dogfood: `/skill-train skill-train`".
- Step 1 + 2 + 3 outputs (all infrastructure in place when Step 4 opens).

---

#### Stage 4.1 — First Retrospective + Meta-Dogfood

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Execute `design-explore` retrospective; iterate prompt/schema if signal weak; run meta-dogfood; record all outcomes in §Changelog entries; mark orchestrator Final.

**Exit:**

- `ia/skills/design-explore/train-proposal-{DATE}.md` present; non-empty; ≥1 friction point.
- `skill-train/SKILL.md §Changelog` carries `source: dogfood-result` entry for both target runs.
- Any schema/prompt iterations recorded in `skill-train/SKILL.md §Changelog` with `source: iteration`.
- `npm run validate:all` exits 0. Orchestrator Status: Final.

**Phases:**

- [ ] Phase 1 — Dogfood readiness check + first retrospective run.
- [ ] Phase 2 — Iterate if weak + meta-dogfood + orchestrator final.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.1.1 | Dogfood readiness check | 1 | _pending_ | _pending_ | Verify ≥1 real run of `design-explore` (or any wired skill) has occurred since Phase-N-tail wiring (Step 2) landed, generating a `source: self-report` §Changelog entry. If none: trigger a short `design-explore` invocation on an existing stub doc to accumulate signal. Document readiness note in `skill-train/SKILL.md §Changelog`. |
| T4.1.2 | Run /skill-train design-explore | 1 | _pending_ | _pending_ | Execute `/skill-train design-explore`. Capture: proposal file path, friction-count, severity. Review proposal — judge signal quality against known §Changelog entries or observed run behavior. Record outcome (`strong`/`weak`/`partial`) in `design-explore/SKILL.md §Changelog` as `source: dogfood-result`. |
| T4.1.3 | Iterate schema + prompt if weak | 2 | _pending_ | _pending_ | If T4.1.2 outcome = `weak` or `partial`: identify gap (aggregation threshold off? Phase 2 summarization too vague? diff too coarse?). Edit `skill-train/SKILL.md` Phase 2 or 3; re-run `/skill-train design-explore`; verify stronger signal. Append `source: iteration` §Changelog entry noting change. If outcome = `strong`: skip edits; append `source: dogfood-result` entry confirming first-run success. |
| T4.1.4 | Meta-dogfood + orchestrator final | 2 | _pending_ | _pending_ | Run `/skill-train skill-train`. Capture proposal if generated; record in `skill-train/SKILL.md §Changelog source: dogfood-result`. Apply any self-proposed patches that survive user review. Run `npm run validate:all`; exit 0. Flip this orchestrator to `Status: Final`. |

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `project-stage-close` runs.
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
