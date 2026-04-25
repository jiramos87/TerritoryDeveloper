---
description: Use on demand to retrospect a lifecycle skill's accumulated friction signal. Reads `ia/skills/{SKILL_NAME}/SKILL.md` §Changelog since last `source: train-proposed` entry (or `--since {YYYY-MM-DD}`), groups entries by `friction_types[]` value, filters to recurrence ≥ threshold (default 2), synthesizes unified-diff proposal targeting Phase sequence / Guardrails / Seed prompt sections, writes proposal file, appends pointer entry. Triggers: "skill-train", "train skill", "retrospect skill", "skill friction analysis", "skill improvement proposal".
argument-hint: "{SKILL_NAME} [--since YYYY-MM-DD] [--all] [--threshold N]"
---

# /skill-train — Retrospective consumer skill. Reads target skill's Per-skill Changelog since last `source: train-proposed` entry, aggregates recurring friction (≥ threshold), writes patch proposal (skill) as unified-diff file. User-gated; no auto-apply.

Drive `$ARGUMENTS` via the [`skill-train`](../agents/skill-train.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- skill-train
- train skill
- retrospect skill
- skill friction analysis
- skill improvement proposal
<!-- skill-tools:body-override -->

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "skill-train"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `skill-train` skill (`ia/skills/skill-train/SKILL.md`) Phase 0–5 end-to-end for `$ARGUMENTS`. First token of `$ARGUMENTS` is `SKILL_NAME`; remaining tokens are flags (`--since`, `--all`, `--threshold`) passed through unchanged.
>
> ## Hard boundaries
>
> - IF `SKILL_NAME` missing → STOP immediately; report input absent.
> - IF `ia/skills/{SKILL_NAME}/SKILL.md` not found → STOP; report skill not found.
> - Do NOT auto-apply patch proposal — review only; user applies manually.
> - Do NOT commit — user decides git state.
> - Do NOT touch other skills' SKILL.md — scope is `{SKILL_NAME}` only.
>
> ## Output
>
> Single concise caveman report: skill targeted, Changelog window scanned, friction count by type, threshold used, proposal file path written, pointer entry appended, next step for user review.
