---
description: Per-Task post-verify code review — dispatches `opus-code-reviewer` (seam #4 pair-head) to scan implementation diff against spec + invariants + glossary. 3 verdict branches — PASS (mini §Code Review report, no tail) / minor (suggestions, no tail) / critical (writes §Code Fix Plan tuples + auto-dispatches `code-fix-applier` Sonnet pair-tail). Fires per-Task between `/verify-loop` tail and Stage-scoped `/audit`.
argument-hint: "{ISSUE_ID}"
---

# /code-review — dispatch seam #4 per-Task pair-head (opus-code-reviewer → code-fix-applier on critical)

Use `opus-code-reviewer` subagent (`.claude/agents/opus-code-reviewer.md`) to review implementation diff for `{ISSUE_ID}` against spec + invariants + glossary. Runs per-Task after `/implement` + `/verify-loop` reach Green. Three verdict branches — PASS / minor → write `## §Code Review` mini-report, no tail; critical → write `## §Code Fix Plan` tuple list + auto-dispatch `code-fix-applier` Sonnet pair-tail (applies fix tuples + re-enters verify-loop; 1-retry bound).

## Argument parsing

Trim `$ARGUMENTS`. First token = `{ISSUE_ID}` (e.g. `TECH-475`). Missing → print usage + abort.

## Step 1 — Dispatch `opus-code-reviewer` (Opus pair-head)

Forward via Agent tool with `subagent_type: "opus-code-reviewer"`:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/opus-code-review/SKILL.md` end-to-end for `{ISSUE_ID}`. Phase 1 Load diff (`git diff main...HEAD` across `ia/**/*.md` + `Assets/Scripts/**/*.cs`; fallback staged + recent-commit diff) + `ia/projects/{ISSUE_ID}.md` §7 Implementation Plan / §8 Acceptance / §Findings / §Verification. Run `domain-context-load` subskill for shared MCP bundle (keywords from spec title + domain terms). Load `invariants_summary` domain subset for changed files. Phase 2 Run 8-check review matrix → verdict (PASS / minor / critical). Phase 2a PASS → write `## §Code Review` mini-report (verdict + diff summary + acceptance + invariants + glossary). Phase 2b minor → mini-report + suggestions (fix-in-place or defer). Phase 3 critical → write `## §Code Fix Plan` tuples (contract 4-key shape — `operation`, `target_path`, `target_anchor`, `payload`) + `## §Code Review` mini-report. Phase 4 Hand-off.
>
> ## Hard boundaries
>
> - Do NOT mutate source code (C# / TS / skill bodies / commands / agents) — only spec `§Code Review` + `§Code Fix Plan` writes. Source fixes happen in pair-tail `code-fix-applier`.
> - Do NOT re-run `/verify-loop` — pair-tail re-enters on critical verdict.
> - Do NOT run validators — pair-tail runs gate.
> - Do NOT guess ambiguous anchors — escalate per `ia/rules/plan-apply-pair-contract.md`.
> - Do NOT emit `§Code Fix Plan` on PASS or minor verdict.
> - Do NOT commit — user decides.

Reviewer returns `{verdict: "PASS"|"minor"|"critical", issue_id}`. PASS / minor → skip Step 2 + emit summary. Critical → proceed to Step 2.

## Step 2 — Dispatch `code-fix-applier` (Sonnet pair-tail) — conditional

On critical verdict: forward via Agent tool with `subagent_type: "code-fix-applier"`:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/code-fix-apply/SKILL.md` end-to-end for `{ISSUE_ID}`. Read `## §Code Fix Plan` tuples verbatim from `ia/projects/{ISSUE_ID}.md`. Resolve every `target_anchor` to single match before applying. Apply tuples in declared order (one atomic edit per tuple). Re-enter `/verify-loop` (seam #4 gate = `npm run verify:local` for C# edits OR `npm run validate:all` for tooling-only). 1-retry bound on verify fail (2 total attempts). Second fail → escalate to Opus pair-head. Idempotent.
>
> ## Hard boundaries
>
> - Do NOT re-review drift — read tuples verbatim.
> - Do NOT reorder tuples — declared order only.
> - Do NOT interpret ambiguous anchors — escalate per `ia/rules/plan-apply-pair-contract.md`.
> - Do NOT skip verify re-entry on tuple application.
> - Do NOT commit — user decides.

## Output

Chain summary: `{ISSUE_ID}` verdict + tuple count (if critical) + pair-tail verify exit (if dispatched). PASS / minor → next step: loop to next Stage Task `/code-review {NEXT_ISSUE_ID}` OR Stage-scoped `/audit {MASTER_PLAN_PATH} {STAGE_ID}` (when all Stage Tasks PASS / minor). Critical after pair-tail success → re-run `/code-review {ISSUE_ID}` to confirm PASS.
