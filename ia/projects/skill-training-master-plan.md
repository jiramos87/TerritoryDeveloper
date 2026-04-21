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

### Stage 1 — skill-train Core + Glossary Foundation / Glossary + Docs Foundation

**Status:** Final (4 of 4 done: TECH-367, TECH-368, TECH-369, TECH-370)

**Objectives:** Land 4 canonical glossary terms and the docs surface-map update before any cross-ref is authored in Stage 1.2 or Step 2. Satisfies invariant #12.

**Exit:**

- `ia/specs/glossary.md`: 4 rows added — `skill self-report`, `skill training`, `patch proposal (skill)`, `skill-train`. MCP `glossary_discover "skill self-report"` returns a match.
- `docs/agent-lifecycle.md §Surface map`: `/skill-train` row present (Retrospective, Opus, outside main lifecycle flow).
- `CLAUDE.md §3` + `AGENTS.md`: one-paragraph pointer added to each.
- `npm run validate:all` exits 0.
- Phase 1 — Glossary rows + agent-lifecycle.md surface map row.
- Phase 2 — CLAUDE.md §3 + AGENTS.md one-paragraph pointers.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Glossary rows × 4 | **TECH-367** | Done (archived) | Add 4 rows to `ia/specs/glossary.md` (Documentation category): `skill self-report` — structured JSON emitted by lifecycle skill at handoff when friction detected; `skill training` — retrospective Changelog-driven proposal loop; `patch proposal (skill)` — unified-diff proposal against SKILL.md Phase sequence / Guardrails / Seed prompt, stored as `ia/skills/{name}/train-proposal-{YYYY-MM-DD}.md`; `skill-train` — Opus consumer subagent + slash command for on-demand skill retrospective. Cross-ref between rows where applicable. |
| T1.2 | agent-lifecycle.md surface row | **TECH-368** | Done (archived) | Add `/skill-train` row to `docs/agent-lifecycle.md §Surface map` table — Stage: Retrospective; Slash command: `/skill-train`; Subagent: `skill-train`; Skill: `skill-train`; Model: Opus. Add inline note "retrospective only — outside main lifecycle flow". |
| T1.3 | CLAUDE.md §3 pointer | **TECH-369** | Done (archived) | Add row to `CLAUDE.md §3` key files table: `ia/skills/skill-train/SKILL.md` — on-demand skill retrospective; reads Per-skill Changelog; proposes unified-diff patch against Phase sequence / Guardrails / Seed prompt sections. Caveman prose. |
| T1.4 | AGENTS.md pointer | **TECH-370** | Done (archived) | Add one-paragraph entry to `AGENTS.md` under the skill-lifecycle / retrospective section (create section if absent): explains `skill-train` role — reads accumulated Per-skill Changelog entries, aggregates recurring friction (≥2 occurrences threshold), writes `train-proposal-{DATE}.md` sibling file. Caveman prose. |

---

### Stage 2 — skill-train Core + Glossary Foundation / skill-train Skill Body + Agent + Command

**Status:** Final

**Backlog state (2026-04-18):** 4 tasks filed (TECH-392, TECH-393, TECH-394, TECH-395 archived).

**Objectives:** Author `ia/skills/skill-train/SKILL.md` with full Phase 0–5 sequence, canonical §Schema block, §Emitter stanza template (single source of truth for Step 2), and guardrails. Create matching `.claude/agents/skill-train.md` Opus subagent and `.claude/commands/skill-train.md` dispatcher.

**Exit:**

- `ia/skills/skill-train/SKILL.md`: Phase 0–5 sequence; `skill_self_report` JSON schema block with `schema_version`; `§Emitter stanza template` section (verbatim copy-paste block for 13 skills); Guardrails include "do NOT apply", "do NOT touch other skills", "do NOT commit"; Seed prompt block.
- `.claude/agents/skill-train.md` (Opus): accepts SKILL_NAME (required), `--since {YYYY-MM-DD}`, `--threshold N`, `--all` (with explicit Opus-cost warning); caveman preamble; mirrors `release-rollout-skill-bug-log.md` header shape.
- `.claude/commands/skill-train.md`: thin dispatcher; forwards SKILL_NAME + all optional flags; caveman preamble.
- `npm run validate:all` exits 0.
- Phase 1 — SKILL.md body (Phase 0–5 + §Schema block + §Emitter stanza template).
- Phase 2 — Agent + command dispatcher.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | skill-train SKILL.md body | **TECH-392** | Done (archived) | Create `ia/skills/skill-train/SKILL.md`. Phase 0: validate target SKILL.md exists + §Changelog present (inject if absent). Phase 1: read Changelog entries since last `source: train-proposed` entry (or `--since` date). Phase 2: aggregate `friction_types` — recurring = ≥2 occurrences (`--threshold N` overrides). Phase 3: synthesize unified diff targeting Phase sequence / Guardrails / Seed prompt sections of target skill. Phase 4: write `ia/skills/{SKILL_NAME}/train-proposal-{YYYY-MM-DD}.md`; append Changelog pointer entry `source: train-proposed`. Phase 5: handoff — path + friction-count + "review + apply manually". §Schema block defines `skill_self_report` JSON: `{skill, run_date, schema_version, friction_types[], guardrail_hits[], phase_deviations[], missing_inputs[], severity}`. Guardrails: do NOT apply patch; do NOT touch other skills' SKILL.md; do NOT commit. |
| T2.2 | Emitter stanza template section | **TECH-393** | Done (archived) | Add `## Emitter stanza template` section to `skill-train/SKILL.md` — canonical Phase-N-tail block for lifecycle skills to copy verbatim: (1) friction-condition check (`guardrail_hits > 0 OR phase_deviations > 0 OR missing_inputs > 0`); (2) construct `skill_self_report` JSON block; (3) append §Changelog entry `source: self-report` with schema_version date-stamp. Clean run (all conditions false) → no-op, §Changelog untouched. This section is the single source of truth consumed in T2.1.1, T2.1.2, T2.2.1, T2.2.2. |
| T2.3 | skill-train agent | **TECH-394** | Done (archived) | Create `.claude/agents/skill-train.md` (Opus subagent). Mirror `.claude/agents/release-rollout-skill-bug-log.md` header shape: title, model, caveman preamble directive. Inputs: SKILL_NAME (required); `--since {YYYY-MM-DD}` optional; `--threshold N` optional (default 2); `--all` flag carries explicit token-cost warning. Body delegates to `ia/skills/skill-train/SKILL.md` Phase 0–5. No auto-apply; no self-commit. |
| T2.4 | skill-train command | **TECH-395** | Done (archived) | Create `.claude/commands/skill-train.md` — thin dispatcher. Caveman preamble. Forwards `{SKILL_NAME}` (required), `--since`, `--all`, `--threshold` args to `skill-train` subagent via Agent tool call. One-paragraph body. |

---

### Stage 3 — Phase-N-tail Wiring (13 Lifecycle Skills) / Core Authoring + Filing Skills (6 skills)

**Status:** In Progress — TECH-433 (4 of 4 filed: TECH-430, TECH-431, TECH-432, TECH-433)

**Objectives:** Wire the 6 authoring-and-filing lifecycle skills (`design-explore`, `master-plan-new`, `master-plan-extend`, `stage-decompose`, `stage-file`, `project-new`) with Phase-N-tail stanzas.

**Exit:**

- All 6 SKILL.md files carry Phase-N-tail stanza (verbatim template, `schema_version` stamped) + `## Changelog` section.
- Stanza placed at final handoff phase in each skill's existing Phase sequence.
- `npm run validate:all` exits 0.
- Phase 1 — design-explore, master-plan-new, master-plan-extend + stage-decompose, stage-file, project-new wiring.
- Phase 2 — Cross-read consistency check + validate:all.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | Wire authoring-trio Phase-N-tail | **TECH-430** | Done (archived) | Edit `ia/skills/design-explore/SKILL.md`, `master-plan-new/SKILL.md`, `master-plan-extend/SKILL.md`: append Phase-N-tail stanza verbatim from `skill-train/SKILL.md §Emitter stanza template`; inject `## Changelog` section if absent; place stanza at existing handoff Phase N position. Verify `schema_version` date-stamp on all 3. |
| T3.2 | Wire filing-trio Phase-N-tail | **TECH-431** | Done (archived) | Edit `ia/skills/stage-decompose/SKILL.md`, `stage-file/SKILL.md`, `project-new/SKILL.md`: same procedure as T2.1.1. Stanza at final handoff phase; §Changelog injected if absent; schema_version present on all 3. |
| T3.3 | Cross-read stanza consistency | **TECH-432** | Done (archived) | Cross-read all 6 wired SKILL.md files; verify stanza text matches canonical template character-for-character (no paraphrase); `schema_version` stamps identical across all 6; `## Changelog` sections present. Document any deviation found in the relevant skill's §Changelog as `source: wiring-review`. |
| T3.4 | validate:all post Stage 2.1 | **TECH-433** | In Progress | Run `npm run validate:all` from repo root; confirm exit 0. Surface any frontmatter/index failures introduced by skill edits; fix inline before closing stage. |

---

### Stage 4 — Phase-N-tail Wiring (spec-lifecycle + rollout-family)

**Status:** Obsoleted by M6 collapse (2026-04-21)

> **Scope-obsolete note.** Of the 7 skills originally in this Stage, the 4 spec-lifecycle targets (`project-spec-kickoff`, `project-stage-close`, `project-spec-close`) are **retired** (tombstones under `ia/skills/_retired/`; replacement Stage 1×N + Stage-scoped closeout pair already carry Phase-N-tail via Stages 2.1 + 2.2 wiring). The remaining live work is Phase-N-tail wiring for the 3 rollout-family skills (`release-rollout`, `release-rollout-enumerate`, `release-rollout-track`) — re-scope into a follow-up Stage if still desired. No tasks filed here. Preserved for audit trail.

**Original objectives (superseded):** Wire the 4 spec-lifecycle skills (`project-spec-kickoff`, `project-spec-implement`, `project-stage-close`, `project-spec-close`) and 3 rollout-family skills with identical Phase-N-tail stanzas.

**Tasks (cancelled):**

| Task | Name | Status | Reason |
| --- | --- | --- | --- |
| T4.1 | Wire spec-lifecycle Phase-N-tail | Cancelled (obsolete) | 3 of 4 targets retired; surviving `project-spec-implement` wiring folded into Stage 2.1 / 2.2. |
| T4.2 | Wire rollout-family Phase-N-tail | Cancelled (re-scope) | Re-file as standalone Stage if still needed; no spec-lifecycle dependency remains. |
| T4.3 | Full 13-skill consistency + validate | Cancelled (N/A) | 13-skill count presupposed the 4 retired spec-lifecycle skills. |
| T4.4 | AGENTS.md wiring-complete entry | Cancelled (N/A) | Predicated on T4.1–T4.3. |

---

### Stage 5 — Caveman Soft-Lint (Phase D) / Lint Script + Hook + Docs

**Status:** Final

**Objectives:** Author `caveman-lint.sh`, wire as warn-only opt-in hook, document usage in caveman-authoring rule.

**Exit:**

- `tools/scripts/caveman-lint.sh` executable; outputs `N indicators found in M files (warn-only)` summary; exits 0 on zero or more findings; skips fenced blocks confirmed via smoke-test.
- Hook wired; `SKILL_TRAIN_LINT=1` gate confirmed warn-only (does not block tool execution).
- `ia/rules/agent-output-caveman-authoring.md §Soft-lint` present.
- `npm run validate:all` exits 0.
- Phase 1 — Script authoring + hook wiring.
- Phase 2 — Documentation + smoke-test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | caveman-lint.sh | **TECH-598** | Done (archived) | Create `tools/scripts/caveman-lint.sh`. Input: `git diff --cached` (pre-commit) or `git diff HEAD` (manual). Scope: `ia/skills/*/SKILL.md`, `.claude/agents/*.md`, `.claude/commands/*.md`, `ia/projects/*.md` (§1–§10 prose only). Skip fenced blocks (``` delimiters). Long-form indicators: sentences > 12 words, articles ("the"/"a"/"an") in prose context, hedging verbs ("should"/"might"/"could"/"would"). Output per hit: `file:line:indicator`. Exit 0 always. Footer: `N indicators found in M files (warn-only).` |
| T5.2 | Hook wiring in settings.json | **TECH-599** | Done (archived) | Add warn-only hook to `.claude/settings.json` hooks array: guard on `SKILL_TRAIN_LINT=1` env var; call `tools/scripts/caveman-lint.sh`; hook exit code forced 0 (warn-only posture). Wire as PreToolUse on Write/Edit tool calls, matching patterns of existing hooks in settings.json. |
| T5.3 | Caveman-authoring docs | **TECH-600** | Done (archived) | Add `## Soft-lint` section to `ia/rules/agent-output-caveman-authoring.md`: what script checks (indicator list), how to enable (`export SKILL_TRAIN_LINT=1`), how to read output (file:line refs), warn-only rationale, known false-positive-free surfaces (fenced blocks already skipped). |
| T5.4 | Smoke-test + validate | **TECH-601** | Done (archived) | Run `bash tools/scripts/caveman-lint.sh` against current repo diff (at least 1 skill SKILL.md modified from Step 2). Verify exit 0; verify summary line present; spot-check that no fenced-block lines appear in output. Run `npm run validate:all`; exit 0. Note smoke-test result in script header comment. |

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "Create tools/scripts/caveman-lint.sh. Input: git diff --cached (pre-commit) or git diff HEAD (manual). Scope: ia/skills/*/SKILL.md, .claude/agents/*.md, .claude/commands/*.md, ia/projects/*.md (§1–§10 prose only). Skip fenced blocks (``` delimiters). Long-form indicators: sentences > 12 words, articles (\"the\"/\"a\"/\"an\") in prose context, hedging verbs (\"should\"/\"might\"/\"could\"/\"would\"). Output per hit: file:line:indicator. Exit 0 always. Footer: N indicators found in M files (warn-only)."
  priority: "medium"
  issue_type: "TECH"
  notes: |
    New bash script under tools/scripts/. Reads stdin (git diff piped) or operates on repo
    working tree. Parses file-path scope list; skips fenced code blocks via delimiter tracking.
    Emits per-hit lines + footer summary. Exit 0 always (warn-only posture).
  depends_on: []
  related: []
  stub_body:
    summary: |
      Create caveman-lint.sh — warn-only soft-lint script for caveman authoring surfaces.
      Detects long sentences, articles, and hedging verbs in prose sections of skill/agent/command/project-spec files.
      Skips fenced blocks; exits 0 on any finding count.
    goals: |
      - Parse git diff or HEAD diff to identify in-scope files (ia/skills, .claude/agents, .claude/commands, ia/projects).
      - Track fenced-block boundaries (```) per file; skip lines inside fenced blocks.
      - Emit file:line:indicator hits for sentences > 12 words, articles in prose, hedging verbs.
      - Output footer "N indicators found in M files (warn-only)"; exit 0 unconditionally.
    systems_map: |
      Primary file: tools/scripts/caveman-lint.sh (new).
      Related: tools/scripts/claude-hooks/ (pattern reference for bash script shape).
      Consumed by: T5.2 hook wiring; T5.4 smoke-test.
    impl_plan_sketch: |
      Phase 1 — Script authoring:
        1. Create tools/scripts/caveman-lint.sh with shebang + header comment noting smoke-test result.
        2. Implement scope filter (grep/awk for file paths in diff output).
        3. Implement fenced-block skip logic (state machine on ``` open/close).
        4. Implement 4-indicator pattern matches (sentence length, "the"/"a"/"an", hedging verbs).
        5. Emit per-hit lines + footer; chmod +x; test locally with a minimal diff.

- reserved_id: ""
  title: "Add warn-only hook to .claude/settings.json hooks array: guard on SKILL_TRAIN_LINT=1 env var; call tools/scripts/caveman-lint.sh; hook exit code forced 0 (warn-only posture). Wire as PreToolUse on Write/Edit tool calls, matching patterns of existing hooks in settings.json."
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Edit .claude/settings.json PreToolUse array. Add new hook entry with matcher "Edit|Write|MultiEdit"
    (mirrors cs-edit-reminder.sh pattern). Guard: check SKILL_TRAIN_LINT env var before invoking
    script; if unset skip silently. Hook must exit 0 regardless of lint findings.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Wire caveman-lint.sh as opt-in PreToolUse hook in .claude/settings.json.
      Hook fires on Write/Edit/MultiEdit tool calls when SKILL_TRAIN_LINT=1 env var set.
      Forced exit 0 — never blocks tool execution.
    goals: |
      - Add PreToolUse hook entry to .claude/settings.json with matcher "Edit|Write|MultiEdit".
      - Guard invocation on SKILL_TRAIN_LINT=1 env var check inside hook command or wrapper.
      - Confirm hook exit code is 0 (warn-only); existing PreToolUse entries unaffected.
      - Verify settings.json remains valid JSON after edit (npm run validate:all).
    systems_map: |
      Primary file: .claude/settings.json (edit, PreToolUse array).
      Script called: tools/scripts/caveman-lint.sh (T5.1 output).
      Reference pattern: existing "Edit|Write|MultiEdit" PostToolUse hook (cs-edit-reminder.sh).
    impl_plan_sketch: |
      Phase 1 — Hook wiring:
        1. Read .claude/settings.json current PreToolUse array.
        2. Add new entry: matcher "Edit|Write|MultiEdit", command wraps caveman-lint.sh with env guard.
        3. Ensure command string: `if [ "${SKILL_TRAIN_LINT}" = "1" ]; then bash tools/scripts/caveman-lint.sh; fi; exit 0`.
        4. Write updated settings.json; confirm JSON valid.

- reserved_id: ""
  title: "Add ## Soft-lint section to ia/rules/agent-output-caveman-authoring.md: what script checks (indicator list), how to enable (export SKILL_TRAIN_LINT=1), how to read output (file:line refs), warn-only rationale, known false-positive-free surfaces (fenced blocks already skipped)."
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Edit ia/rules/agent-output-caveman-authoring.md. Append new ## Soft-lint section at end of file.
    Cover: indicator list (long sentences, articles, hedging verbs), enable command, output format,
    warn-only rationale, fenced-block skip note. Caveman prose throughout.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Add ## Soft-lint section to caveman-authoring companion rule documenting the caveman-lint.sh script.
      Covers what it checks, how to enable, how to read output, warn-only design rationale.
    goals: |
      - Document 4 indicator categories checked by caveman-lint.sh (sentence length, articles, hedging verbs).
      - Provide enable instruction: `export SKILL_TRAIN_LINT=1` before running Claude Code session.
      - Explain output format (file:line:indicator) and footer summary.
      - State warn-only rationale — blocks nothing; signals drift for human review.
      - Note fenced-block skip as false-positive mitigation.
    systems_map: |
      Primary file: ia/rules/agent-output-caveman-authoring.md (edit, append section).
      Script documented: tools/scripts/caveman-lint.sh (T5.1 output).
      Parent rule: ia/rules/agent-output-caveman.md.
    impl_plan_sketch: |
      Phase 1 — Documentation:
        1. Read ia/rules/agent-output-caveman-authoring.md current tail.
        2. Author ## Soft-lint section with subsections: What it checks, Enabling, Reading output, Rationale.
        3. Append section; run npm run validate:all to confirm frontmatter/index clean.

- reserved_id: ""
  title: "Run bash tools/scripts/caveman-lint.sh against current repo diff (at least 1 skill SKILL.md modified from Step 2). Verify exit 0; verify summary line present; spot-check that no fenced-block lines appear in output. Run npm run validate:all; exit 0. Note smoke-test result in script header comment."
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Verification task. Runs caveman-lint.sh against a real diff (Stage 3 skill edits qualify).
    Confirms exit 0, summary footer present, fenced-block lines absent from output.
    Runs npm run validate:all; confirms exit 0. Records smoke-test outcome in script header comment.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Smoke-test caveman-lint.sh against real repo diff + run validate:all gate.
      Confirms exit-0 posture, correct footer format, fenced-block exclusion, and clean validator run.
    goals: |
      - Execute bash tools/scripts/caveman-lint.sh with a git diff containing ≥1 wired SKILL.md.
      - Assert exit code 0.
      - Assert footer line "N indicators found in M files (warn-only)" present in output.
      - Spot-check output — confirm no lines sourced from inside fenced code blocks.
      - Run npm run validate:all; assert exit 0.
      - Append smoke-test result note to script header comment in caveman-lint.sh.
    systems_map: |
      Script under test: tools/scripts/caveman-lint.sh.
      Validator: npm run validate:all (package.json script).
      Diff source: git diff HEAD covering Stage 3 wiring edits (ia/skills/*/SKILL.md changes).
    impl_plan_sketch: |
      Phase 1 — Smoke-test:
        1. Run bash tools/scripts/caveman-lint.sh (pipe or pass git diff HEAD).
        2. Inspect output — verify exit 0, footer line, no fenced-block leakage.
        3. Run npm run validate:all; capture exit code.
        4. If both pass: append "# smoke-test: PASS {DATE}" comment to script header.
        5. If either fails: diagnose + fix before marking T5.4 done.
```

#### §Stage Closeout Plan

> **Applied** 2026-04-21 — Stage 5 closeout completed (`plan-applier` Mode stage-closeout). Four tasks archived (**TECH-598**–**TECH-601**): backlog rows moved under `ia/backlog-archive/`, temporary project specs removed per closeout, Stage 5 task table rows **Done (archived)**. Raw YAML tuple list removed from this orchestrator so `validate:dead-project-specs` does not scan stale `delete_file` paths to already-deleted spec files; see git history for the pre-apply tuple batch.

---

### Stage 6 — Dogfood Cycle (Phase E) / First Retrospective + Meta-Dogfood

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Execute `design-explore` retrospective; iterate prompt/schema if signal weak; run meta-dogfood; record all outcomes in §Changelog entries; mark orchestrator Final.

**Exit:**

- `ia/skills/design-explore/train-proposal-{DATE}.md` present; non-empty; ≥1 friction point.
- `skill-train/SKILL.md §Changelog` carries `source: dogfood-result` entry for both target runs.
- Any schema/prompt iterations recorded in `skill-train/SKILL.md §Changelog` with `source: iteration`.
- `npm run validate:all` exits 0. Orchestrator Status: Final.
- Phase 1 — Dogfood readiness check + first retrospective run.
- Phase 2 — Iterate if weak + meta-dogfood + orchestrator final.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | Dogfood readiness check | **TECH-604** | Draft | Verify ≥1 real run of `design-explore` (or any wired skill) has occurred since Phase-N-tail wiring (Step 2) landed, generating a `source: self-report` §Changelog entry. If none: trigger a short `design-explore` invocation on an existing stub doc to accumulate signal. Document readiness note in `skill-train/SKILL.md §Changelog`. |
| T6.2 | Run /skill-train design-explore | **TECH-605** | Draft | Execute `/skill-train design-explore`. Capture: proposal file path, friction-count, severity. Review proposal — judge signal quality against known §Changelog entries or observed run behavior. Record outcome (`strong`/`weak`/`partial`) in `design-explore/SKILL.md §Changelog` as `source: dogfood-result`. |
| T6.3 | Iterate schema + prompt if weak | **TECH-606** | Draft | If T6.2 outcome = `weak` or `partial`: identify gap (aggregation threshold off? Phase 2 summarization too vague? diff too coarse?). Edit `skill-train/SKILL.md` Phase 2 or 3; re-run `/skill-train design-explore`; verify stronger signal. Append `source: iteration` §Changelog entry noting change. If outcome = `strong`: skip edits; append `source: dogfood-result` entry confirming first-run success. |
| T6.4 | Meta-dogfood + orchestrator final | **TECH-607** | Draft | Run `/skill-train skill-train`. Capture proposal if generated; record in `skill-train/SKILL.md §Changelog source: dogfood-result`. Apply any self-proposed patches that survive user review. Run `npm run validate:all`; exit 0. Flip this orchestrator to `Status: Final`. |

### §Plan Fix — PASS (no drift)

> plan-review re-entry exit 0 — Stage 6 Task specs aligned. Prior 2 tuples already reflected in file state (no-ops). No new drift found. Downstream pipeline continue.

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "Verify ≥1 real design-explore run since Phase-N-tail wiring landed; trigger short invocation if none; document readiness in skill-train/SKILL.md §Changelog"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Read design-explore/SKILL.md §Changelog for any source: self-report entry added after
    Stage 3 wiring (TECH-430). If absent, trigger short design-explore invocation on any
    existing stub doc to accumulate signal. Append dogfood-readiness note to
    skill-train/SKILL.md §Changelog (source: dogfood-result or source: readiness-note).
  depends_on: []
  related: []
  stub_body:
    summary: |
      Readiness gate before first skill-train retrospective run. Confirms design-explore
      has a real §Changelog self-report entry since Phase-N-tail wiring landed (Stage 3).
      Triggers signal-accumulation run if §Changelog is empty.
    goals: |
      - Read ia/skills/design-explore/SKILL.md §Changelog; check for source: self-report entry.
      - If none present: invoke design-explore on an existing stub doc to generate signal.
      - Confirm §Changelog entry created by that run (friction_types[] populated or empty clean run).
      - Append readiness note to skill-train/SKILL.md §Changelog documenting outcome.
    systems_map: |
      Primary files: ia/skills/design-explore/SKILL.md (§Changelog read), ia/skills/skill-train/SKILL.md (§Changelog write).
      Related: .claude/agents/skill-train.md, .claude/commands/skill-train.md.
    impl_plan_sketch: |
      Phase 1 — Readiness check:
        1. Read design-explore/SKILL.md §Changelog tail; scan for source: self-report entries post-wiring.
        2. If found: proceed to T6.2 directly; append readiness note (found signal).
        3. If not found: select an existing exploration doc under docs/; invoke /design-explore {path}
           to produce at least one §Changelog entry (friction or clean).
        4. Append readiness note to skill-train/SKILL.md §Changelog.

- reserved_id: ""
  title: "Execute /skill-train design-explore; capture proposal path + friction-count + severity; judge signal quality; record outcome in design-explore/SKILL.md §Changelog as source: dogfood-result"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Invoke /skill-train design-explore subagent (Opus). Read generated train-proposal-{DATE}.md;
    evaluate signal quality against known §Changelog entries or observed run behavior.
    Classify outcome as strong / weak / partial. Append source: dogfood-result entry to
    design-explore/SKILL.md §Changelog noting classification.
  depends_on: []
  related: []
  stub_body:
    summary: |
      First real skill-train retrospective run targeting design-explore skill.
      Produces ia/skills/design-explore/train-proposal-{DATE}.md; evaluates proposal signal quality.
      Records dogfood outcome in design-explore/SKILL.md §Changelog.
    goals: |
      - Run /skill-train design-explore (Opus subagent dispatch).
      - Capture: proposal file path, friction-count aggregated, severity field value.
      - Review proposal diff hunks against known friction in §Changelog entries.
      - Judge signal: strong (clear actionable diff) / weak (vague or trivial) / partial (mixed).
      - Append source: dogfood-result §Changelog entry to design-explore/SKILL.md with outcome classification.
    systems_map: |
      Primary files: ia/skills/design-explore/SKILL.md (§Changelog append), ia/skills/design-explore/train-proposal-{DATE}.md (created by skill-train).
      Agent: .claude/agents/skill-train.md (Opus). Command: .claude/commands/skill-train.md.
      Skill body: ia/skills/skill-train/SKILL.md Phase 0–5.
    impl_plan_sketch: |
      Phase 1 — Retrospective run:
        1. Invoke claude-personal "/skill-train design-explore".
        2. Wait for proposal file write; note path + friction-count.
        3. Read proposal; evaluate quality (actionable hunks vs vague summary).
        4. Classify outcome; append §Changelog entry to design-explore/SKILL.md.

- reserved_id: ""
  title: "If T6.2 outcome weak or partial: identify gap, edit skill-train/SKILL.md Phase 2 or 3, re-run /skill-train design-explore, verify stronger signal, append source: iteration §Changelog entry; if strong: append source: dogfood-result confirming first-run success"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Conditional on T6.2 classification. Weak/partial → diagnose gap (threshold? aggregation?
    diff granularity?); edit skill-train/SKILL.md Phase 2 or 3 body; re-run; judge again.
    Strong → skip edits; append confirmation entry only. All changes appended to
    skill-train/SKILL.md §Changelog as source: iteration or source: dogfood-result.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Iterative refinement pass on skill-train Phase 2/3 if first retrospective signal is weak.
      Edits skill-train/SKILL.md aggregation or diff logic; re-runs /skill-train design-explore.
      Appends source: iteration §Changelog entries for each edit cycle.
    goals: |
      - Evaluate T6.2 classification (received from T6.2 §Changelog entry or review notes).
      - If strong: append source: dogfood-result to skill-train/SKILL.md §Changelog; proceed to T6.4.
      - If weak/partial: diagnose gap category (threshold, Phase 2 grouping, Phase 3 diff granularity).
      - Edit skill-train/SKILL.md (Phase 2 or Phase 3 body) to address gap.
      - Re-run /skill-train design-explore; re-evaluate; repeat until strong or max 2 iterations.
      - Append source: iteration §Changelog entry per cycle noting change + outcome.
    systems_map: |
      Primary files: ia/skills/skill-train/SKILL.md (Phase 2/3 edits, §Changelog append),
        ia/skills/design-explore/SKILL.md (§Changelog read for signal context),
        ia/skills/design-explore/train-proposal-{DATE}.md (re-generated per iteration).
      Agent: .claude/agents/skill-train.md (Opus). Command: .claude/commands/skill-train.md.
    impl_plan_sketch: |
      Phase 1 — Signal evaluation + conditional iteration:
        1. Read T6.2 outcome from design-explore/SKILL.md §Changelog (last source: dogfood-result entry).
        2. If strong: append confirmation to skill-train/SKILL.md §Changelog; skip to Phase 2.
        3. If weak/partial: read skill-train/SKILL.md Phase 2 (aggregation) + Phase 3 (diff synthesis).
        4. Edit identified gap; re-run /skill-train design-explore; re-evaluate.
        5. Append source: iteration entry after each cycle.
      Phase 2 — Close iteration loop:
        6. Confirm signal upgraded to strong or document final state if max iterations reached.

- reserved_id: ""
  title: "Run /skill-train skill-train (meta-dogfood); record proposal in skill-train/SKILL.md §Changelog source: dogfood-result; apply user-approved patches; run npm run validate:all; flip orchestrator to Status: Final"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Meta-dogfood: skill-train retrospects itself. Invoke /skill-train skill-train; capture
    proposal if generated; user reviews + approves any patches; apply approved patches to
    skill-train/SKILL.md. Run npm run validate:all; confirm exit 0. Update orchestrator
    skill-training-master-plan.md Status line from Draft → Final.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Meta-dogfood run: skill-train retrospects its own SKILL.md via /skill-train skill-train.
      User reviews generated proposal; approved patches applied. Orchestrator flipped Final after validate:all passes.
    goals: |
      - Invoke /skill-train skill-train; capture proposal file path + friction-count.
      - User reviews proposal; approve or reject each hunk.
      - Apply approved patches to skill-train/SKILL.md.
      - Append source: dogfood-result entry to skill-train/SKILL.md §Changelog.
      - Run npm run validate:all; confirm exit 0.
      - Edit skill-training-master-plan.md Status line → Status: Final; flip all Stage 6 task rows Done (archived).
    systems_map: |
      Primary files: ia/skills/skill-train/SKILL.md (retrospect target + patch destination + §Changelog),
        ia/projects/skill-training-master-plan.md (Status flip).
      Agent: .claude/agents/skill-train.md (Opus). Command: .claude/commands/skill-train.md.
      Validator: npm run validate:all (package.json).
    impl_plan_sketch: |
      Phase 1 — Meta-dogfood:
        1. Invoke /skill-train skill-train.
        2. Capture proposal; present to user for hunk-by-hunk review.
        3. Apply approved hunks to skill-train/SKILL.md; discard rejected hunks.
        4. Append source: dogfood-result §Changelog entry.
      Phase 2 — Close + validate:
        5. Run npm run validate:all; confirm exit 0; fix any failures inline.
        6. Edit orchestrator Status: Final; flip T6.1–T6.4 rows to Done (archived).
```

---

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
