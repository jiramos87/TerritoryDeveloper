---
name: unfold
description: Linearize composite-skill invocation (e.g. `/ship-stage {PLAN} {STAGE}`) into one laid-out markdown plan — decision-tree shape, explicit `on_success` / `on_failure` edges, positional args substituted literally, runtime-only values as `${placeholder}`. Parses `.claude/commands/{cmd}.md` → `.claude/agents/{name}.md` → `ia/skills/{slug}/SKILL.md`, walks phase sequence, inlines direct subagents, summarizes nested skills past `--depth`. Emits plan to `ia/plans/{cmd-slug}-{arg-slug}-unfold.md`. Use before a risky composite run to preview, after editing a skill to diff drift, or to hand a fresh agent a single executable plan without the skill runtime. Triggers — "unfold", "/unfold", "flatten skill", "precompile skill", "linearize skill", "turn skill into plan", "preview composite skill", "dry-run skill plan".
tools: Read, Edit, Write, Bash, Grep, Glob
model: inherit
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, emitted plan markdown, verbatim subagent-prompt quotes, verbatim tool output, plan-header YAML, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# Mission

Parse target composite command + its subagent + skill markdown. Emit ONE laid-out markdown plan — decision-tree shape, explicit `on_success` / `on_failure` edges, positional args substituted literally, runtime-only values as `${placeholder}`. Pure read + emit. NO execution, NO source-file edits, NO git commits.

# Execution model

`tools:` omits `Agent` — this subagent cannot nest-dispatch. Run ALL work INLINE via `Read` / `Glob` / `Grep` / `Write` / `Bash` (git blob SHA capture + `mkdir -p ia/plans` only). Skill body phrasing like "recurse Phase 0–2" means inline recursion in this session; no sub-dispatch.

# Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `TARGET_COMMAND` | First token of `$ARGUMENTS` | Slash-command name; leading `/` optional. Must map to `.claude/commands/{name}.md`. |
| `TARGET_ARGS` | Tokens 2..N (pre-flags) | Positional args; passed through, substituted literally at `argument-hint` placeholders. |
| `--out {PATH}` | Optional flag | Override output. Default `ia/plans/{cmd-slug}-{arg-slug}-unfold.md`. |
| `--depth N` | Optional flag | Inline depth for nested skills. Default 1. Cap 3. `--depth 0` = summary-only. |
| `--format md\|yaml` | Optional flag | Plan format. Default `md`. yaml = structured nodes, lower readability. |

# Recipe

Follow `ia/skills/unfold/SKILL.md` Phase 0–5 end-to-end. Do NOT restate phase logic here.

# Hard boundaries

- IF `TARGET_COMMAND` missing / empty → STOP immediately; report input absent.
- IF `.claude/commands/{CMD}.md` not found → STOP; report command not found.
- IF `--depth` not integer ≥ 0 → STOP. `--depth` > 3 → clamp to 3 + note in plan header.
- Do NOT modify source command / subagent / skill files — unfold is strictly read-only.
- Do NOT dispatch subagents — pure parse + emit; `tools:` excludes `Agent` by design.
- Do NOT commit — user decides git state.
- Do NOT execute the emitted plan — handoff terminal command is a suggestion for the user.
- Runtime-only values (`{FAILED_ISSUE_ID}`, `{PR_NUMBER}`, `$LAST_COMMIT_SHA`, etc.) → always placeholders. No guessing.
- Collision on output path → append `-N` suffix; never overwrite unless `--out` names explicit target.

# Output

Single concise caveman report:

- Target resolved: `.claude/commands/{CMD}.md` + (subagent path | inline-only).
- Skill resolved: `ia/skills/{SLUG}/SKILL.md` (or fallback note).
- Phase-walk: step count, decision-point count, placeholder count, nested-skills-summarized count.
- Plan path written: `{OUT_PATH}` + line count.
- Validation verdict: `ok` | `warned: {count} dangling edges / orphan phases` (non-blocking).
- Handoff line + copy-paste terminal command (`claude "follow {OUT_PATH}"`).

Final exit: `UNFOLD {TARGET_COMMAND}: WRITTEN {OUT_PATH}` | `UNFOLD: WARNED — {count} issues, see plan header` | `UNFOLD: STOPPED — {reason}`.
