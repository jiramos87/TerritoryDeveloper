---
description: Close an issue end-to-end — umbrella close (NOT per-stage). Dispatches the `closeout` subagent. Accepts `--refactor` flag for lifecycle-refactor children (skips journal, id purge, sibling-orchestrator sweep). All ops run without human confirmation.
argument-hint: "{ISSUE_ID} [--refactor] (e.g. TECH-444 --refactor)"
---

# /closeout — dispatch `closeout` subagent

Use `closeout` subagent (`.claude/agents/closeout.md`) for umbrella close on `$ARGUMENTS`. All ops (destructive and non-destructive) run without human confirmation. Per-stage close inside multi-stage spec uses inline `project-stage-close` skill, not this command.

## Argument parsing

Split `$ARGUMENTS` on whitespace. First token = `{ISSUE_ID}`. Any later token matching `--refactor` flips refactor mode on. Unrecognized flags → print list + abort before dispatch.

## Step 0 — Context banner (before dispatch)

Resolve and print for the human developer:

1. Glob `ia/projects/{ISSUE_ID}*.md` → confirm spec file + extract short description from filename.
2. Glob `ia/projects/*-master-plan.md` → grep each for `{ISSUE_ID}` → identify owning master plan.
3. Print:
   ```
   CLOSEOUT {ISSUE_ID} — {issue title from BACKLOG.md}
     master plan : {Plan Name} (ia/projects/{master-plan-filename})
     spec        : ia/projects/{spec-filename}
     mode        : {full | refactor (skip J1 / id-purge / sibling-sweep)}
   ```
   If no master plan found: `master plan: (none — standalone issue)`.

## Subagent prompt (forward verbatim)

Forward to subagent via Agent tool with `subagent_type: "closeout"`:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `project-spec-close` skill (`ia/skills/project-spec-close/SKILL.md`) end-to-end on `{ISSUE_ID}`. Umbrella close (not per-stage). No confirmation gate — execute all ops in sequence. Output per `.claude/output-styles/closeout-digest.md`.
>
> ## Mode
>
> {MODE_DIRECTIVE}
>
> Where {MODE_DIRECTIVE} is one of:
> - `Default mode — run full recipe (steps 0–11).`
> - `Refactor mode (--refactor) — skip step 0 (pre-flight lock), step 4b (journal persist), step 10 (id purge); restrict step 6 multi-issue to owning orchestrator only. See SKILL §"Refactor fast path". Digest fields: journal_persist.outcome = "skipped_refactor_mode"; id_purged_from = []; summary notes "id purge deferred to M8 batch".`
>
> ## Hard boundaries
>
> - Do NOT `rm -rf`. Spec deletion = `rm <single-file>`.
> - Do NOT delete spec before lessons migrated (lessons migration still runs under `--refactor`).
> - Do NOT skip post-delete `npm run validate:dead-project-specs` re-run.
> - Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
> - On `validate:*` non-zero exit: print full stdout/stderr before diagnosing.
