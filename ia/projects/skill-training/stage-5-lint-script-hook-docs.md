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
