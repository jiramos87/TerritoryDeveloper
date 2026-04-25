---
description: Mechanize §Plan Author into §Plan Digest (per-Task) + compile aggregate doc at docs/implementation/. Dispatches the `plan-digest` subagent. Stage-scoped bulk non-pair; runs after `/author` and before `/plan-review` in the `/stage-file` chain.
argument-hint: "{master-plan-path} Stage {X.Y} [--force-model {model}]   |   --task {ISSUE_ID} [--force-model {model}]"
---

# /plan-digest — dispatch `plan-digest` subagent

Use `plan-digest` subagent (`.claude/agents/plan-digest.md`) to mechanize and compile `$ARGUMENTS`. Canonical committed section is `§Plan Digest`; `§Plan Author` is ephemeral and dropped when digest succeeds. Contract: `ia/rules/plan-digest-contract.md`.

## Argument parsing

Split `$ARGUMENTS`. If `--force-model {model}` present: extract `{model}` (valid: `sonnet`, `opus`, `haiku`); store as `FORCE_MODEL`. Absent or invalid → `FORCE_MODEL` unset.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "plan-digest"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-digest/SKILL.md` end-to-end for `$ARGUMENTS`. Default: Stage-scoped on `{MASTER_PLAN_PATH}` + `{STAGE_ID}`. Escape hatch: `--task {ISSUE_ID}` = single-spec digest (N=1).
>
> Auto-invoked from `/stage-file` Step 4 (after `plan-author`, before `plan-reviewer`). Manual: recovery after a failed chain step, or N=1 path after `/author --task` before `/ship`.
>
> ## Output
>
> Emit caveman summary: N digested, aggregate `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` path, lint pass. Next: `claude-personal "/plan-review {MASTER_PLAN_PATH} Stage {STAGE_ID}"` (multi-task) OR `claude-personal "/ship {ISSUE_ID}"` (N=1).

## Related

- [`/stage-file`](stage-file.md) (full seam #2 chain including this step) · [`/author`](author.md) (writes ephemeral `§Plan Author`) · [`/plan-review`](plan-review.md) · [`/ship`](ship.md) (N=1) · [`/ship-stage`](ship-stage.md) (N≥2)
