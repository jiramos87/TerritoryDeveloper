---
description: Linearize composite-skill invocation (e.g. `/ship-stage {PLAN} {STAGE}`) into one laid-out markdown plan — decision-tree shape, explicit `on_success` / `on_failure` edges, positional args substituted literally, runtime-only values as `${placeholder}`. Parses `.claude/commands/{cmd}.md` → `.claude/agents/{name}.md` → `ia/skills/{slug}/SKILL.md`, walks phase sequence, inlines direct subagents, summarizes nested skills past `--depth`. Emits plan to `ia/plans/{cmd-slug}-{arg-slug}-unfold.md`. Use before a risky composite run to preview, after editing a skill to diff drift, or to hand a fresh agent a single executable plan without the skill runtime. Triggers — "unfold", "/unfold", "flatten skill", "precompile skill", "linearize skill", "turn skill into plan", "preview composite skill", "dry-run skill plan".
argument-hint: "{TARGET_COMMAND} {TARGET_ARGS...} [--out PATH] [--depth N] [--format md|yaml]"
---

# /unfold — Meta-tool. Read composite slash-command invocation, trace subagent + skill chain, emit one self-contained decision-tree plan with explicit on_success / on_failure edges. Runtime-only values emitted as placeholders. Read-only; NO execution, NO source edits, NO git commits.

Drive `$ARGUMENTS` via the [`unfold`](../agents/unfold.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, emitted plan markdown, verbatim subagent-prompt quotes, verbatim tool output, plan-header YAML, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- unfold
- /unfold
- flatten skill
- precompile skill
- linearize skill
- turn skill into plan
- preview composite skill
- dry-run skill plan
<!-- skill-tools:body-override -->

Translate `{TARGET_COMMAND} {TARGET_ARGS...}` into ONE laid-out markdown plan — decision-tree shape, explicit `on_success` / `on_failure` edges, positional args substituted literally, runtime-only values as `${placeholder}`. Output: `ia/plans/{cmd-slug}-{arg-slug}-unfold.md` (override via `--out`).

Use before a risky composite run (preview behavior), after editing a skill (diff vs prior unfold), or to hand a plan to a fresh agent session without the skill runtime.

Follow `caveman:caveman` for all your own output and all dispatched subagents. Standard exceptions: code, emitted plan markdown, verbatim subagent-prompt quotes, verbatim tool output, plan-header YAML, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

## Step 0 — Context resolution (before dispatch)

Parse `$ARGUMENTS`:

- `TARGET_COMMAND` = first token (strip leading `/` if present).
- `TARGET_ARGS` = tokens 2..N up to first flag (`--out` / `--depth` / `--format`).
- Flags (`--out PATH`, `--depth N`, `--format md|yaml`) → forward unchanged.

Verify `.claude/commands/{TARGET_COMMAND}.md` exists (Glob). Missing → emit `command not found: {TARGET_COMMAND}` + STOP; do NOT dispatch.

Print context banner:

```
UNFOLD /{TARGET_COMMAND}
  args   : {TARGET_ARGS joined}
  out    : {resolved OUT_PATH or default}
  depth  : {N or 1}
  format : {md or yaml}
```

---

## Stage 1 — Dispatch (`unfold` subagent)

Dispatch Agent with `subagent_type: "unfold"`. Forward prompt verbatim:

> Follow `caveman:caveman`. Standard exceptions: code, emitted plan markdown, verbatim subagent-prompt quotes, tool output, plan-header YAML.
>
> ## Mission
>
> Run `ia/skills/unfold/SKILL.md` Phase 0–5 end-to-end on `$ARGUMENTS`. First token = `TARGET_COMMAND` (leading `/` optional). Tokens 2..N (pre-flags) = `TARGET_ARGS`. Flags: `--out PATH`, `--depth N` (default 1, cap 3), `--format md|yaml` (default `md`). Emit plan to `ia/plans/{cmd-slug}-{arg-slug}-unfold.md` (or `--out` override). Read-only; NO execution.
>
> ## Phase sequence
>
> 1. Phase 0 — Resolve target command (`.claude/commands/{CMD}.md` + dispatch target extraction + `argument-hint` capture).
> 2. Phase 1 — Load subagent (`.claude/agents/{NAME}.md`) + skill (`ia/skills/{SLUG}/SKILL.md`); extract inputs, phase sequence, guardrails, exits.
> 3. Phase 2 — Walk skill phases; substitute `TARGET_ARGS` at positional placeholders; recurse nested skills up to `--depth`; runtime-only values → `${placeholder}` + registry.
> 4. Phase 3 — Emit plan to `OUT_PATH` (create `ia/plans/` if missing; collision → `-N` suffix). Plan header carries generator / date / target / SHAs / depth / format / fallback.
> 5. Phase 4 — Validate: every `on_success` / `on_failure` edge targets a known step id or terminal label; every SKILL phase maps to ≥1 step; warn (non-blocking) on dangling edges / orphan phases.
> 6. Phase 5 — Handoff: caveman one-liner + copy-paste `claude "follow {OUT_PATH}"`.
>
> ## Hard boundaries
>
> - `TARGET_COMMAND` missing → STOP immediately.
> - `.claude/commands/{CMD}.md` not found → STOP.
> - `--depth` not integer ≥ 0 → STOP. `--depth` > 3 → clamp to 3 + note in header.
> - Do NOT modify source command / subagent / skill files.
> - Do NOT dispatch subagents — pure parse + emit.
> - Do NOT commit — user decides git state.
> - Do NOT execute the emitted plan.
> - Runtime-only values → placeholders. No guessing.
> - Collision on output path → append `-N` suffix; never overwrite unless `--out` names explicit target.
>
> ## Output
>
> Single concise caveman report: target resolved, subagent + skill resolved, phase-walk counts (steps / branches / placeholders / nested-summarized), plan path + line count, validation verdict (`ok` | `warned`), handoff line + copy-paste terminal command.
>
> ## Exit
>
> End with one of:
> - `UNFOLD {TARGET_COMMAND}: WRITTEN {OUT_PATH}`
> - `UNFOLD: WARNED — {count} issues, see plan header`
> - `UNFOLD: STOPPED — {reason}`

---

## Pipeline summary output

After dispatch completes (or on stop), emit:

```
UNFOLD /{TARGET_COMMAND}: {WRITTEN|STOPPED|WARNED}
  plan  : {OUT_PATH}
  stats : {step_count} steps · {branch_count} branches · {placeholder_count} placeholders
```

On `WRITTEN`: append copy-paste `claude "follow {OUT_PATH}"` terminal command.
On `WARNED`: append `see plan header warnings: array` + path to plan.
On `STOPPED`: append `reason: {reason}`; no plan file written.
