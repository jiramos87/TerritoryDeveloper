---
description: Retrospective friction analysis for a lifecycle skill. Dispatches the `skill-train` subagent to read §Changelog, aggregate recurring friction at threshold, and write a unified-diff patch proposal. User-gated — no auto-apply, no commit.
argument-hint: "{SKILL_NAME} [--since YYYY-MM-DD] [--all] [--threshold N]"
---

# /skill-train — dispatch `skill-train` subagent

Use `skill-train` subagent (`.claude/agents/skill-train.md`) to run a retrospective friction analysis on `$ARGUMENTS`. Reads `ia/skills/{SKILL_NAME}/SKILL.md` §Changelog, aggregates recurring friction types at threshold (default 2), produces a unified-diff patch proposal targeting Phase sequence / Guardrails / Seed prompt sections. No auto-apply; no commit.

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
