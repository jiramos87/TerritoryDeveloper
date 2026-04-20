---
purpose: "Run `npm run progress` from repo root — regenerate docs/progress.html. Non-blocking: failure logs exit code but does NOT halt caller."
audience: agent
loaded_by: skill:progress-regen
slices_via: none
name: progress-regen
description: >
  Bash wrapper subskill. Runs `npm run progress` from repo root and logs the exit code.
  Non-blocking contract: caller continues regardless of exit code. No LLM model needed — pure
  shell. Invoked as an inline subskill by master-plan-new, master-plan-extend, stage-decompose,
  stage-file, and stage-closeout-apply (retired project-spec-close + project-stage-close folded
  into Stage-scoped closeout pair per M6 collapse) wherever `npm run progress` previously
  appeared inline. Triggers: "regen progress", "regenerate progress dashboard",
  "npm run progress wrapper", "progress-regen subskill".
---

# Progress regen — Bash wrapper subskill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

No model needed. Pure Bash.

**Purpose:** single canonical place for the `npm run progress` call used across lifecycle skills.
Centralizes the non-blocking contract so callers do not copy-paste the same boilerplate.

---

## Contract

1. Run from repo root: `npm run progress`
2. Capture exit code.
3. Exit 0 → report `progress-regen: exit 0 (docs/progress.html updated)`.
4. Non-zero → report `progress-regen: exit {N} (tooling only — does NOT block caller)`.
5. Return. Caller continues unconditionally.

**Non-blocking:** progress dashboard is tooling-only. A non-zero exit does not indicate IA
breakage; caller must NOT gate on this exit code.

---

## Invocation (from caller skill body)

Replace any inline `npm run progress` block with:

> Run `progress-regen` subskill: `npm run progress` from repo root. Log exit code.
> Failure does NOT block next step.

---

## Guardrails

- Do NOT block caller on non-zero exit.
- Do NOT run from a subdirectory — always repo root.
- Do NOT replace `npm run validate:all` — different tool, different contract.

---

## Callers

`master-plan-new` Phase 8b · `master-plan-extend` Phase 7b · `stage-decompose` Phase 5 ·
`stage-file` Post-loop step 1b · `stage-closeout-apply` (Stage-scoped closeout pair — absorbs retired `project-spec-close` step 9b + `project-stage-close` step 7b per M6 collapse).
