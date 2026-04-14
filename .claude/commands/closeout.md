---
description: Close an issue end-to-end — umbrella close (NOT per-stage). Dispatches the `closeout` subagent for the given issue. All ops run without human confirmation.
argument-hint: "{ISSUE_ID} (e.g. BUG-14)"
---

# /closeout — dispatch `closeout` subagent

Use `closeout` subagent (`.claude/agents/closeout.md`) for umbrella close on `$ARGUMENTS`. All ops (destructive and non-destructive) run without human confirmation. Per-stage close inside multi-stage spec uses inline `project-stage-close` skill, not this command.

## Step 0 — Context banner (before dispatch)

Before dispatching the subagent, resolve and print for the human developer:

1. Glob `ia/projects/$ARGUMENTS*.md` → confirm spec file + extract short description from filename.
2. Glob `ia/projects/*-master-plan.md` → grep each for `$ARGUMENTS` → identify owning master plan.
3. Print:
   ```
   CLOSEOUT $ARGUMENTS — {issue title from BACKLOG.md}
     master plan : {Plan Name} (ia/projects/{master-plan-filename})
     spec        : ia/projects/{spec-filename}
   ```
   If no master plan found: `master plan: (none — standalone issue)`.

## Subagent prompt (forward verbatim)

Forward to subagent via Agent tool with `subagent_type: "closeout"`:

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `project-spec-close` skill (`ia/skills/project-spec-close/SKILL.md`) — umbrella close (not per-stage) — on verified issue `$ARGUMENTS`. Migrate lessons → canonical IA, persist journal, validate dead spec paths, then delete spec, remove BACKLOG row, append to `BACKLOG-ARCHIVE.md`, purge id from durable docs/code. No confirmation gate — execute all ops in sequence.
>
> ## Sequence
>
> 1. `mcp__territory-ia__backlog_issue` for `$ARGUMENTS`.
> 2. `mcp__territory-ia__project_spec_closeout_digest` — extract H2s from `ia/projects/$ARGUMENTS*.md` (resolve via Glob; may be `$ARGUMENTS.md` or `$ARGUMENTS-{description}.md`).
> 3. **Migrate lessons** (non-destructive) — each Lessons Learned bullet → `docs/information-architecture-overview.md`, `AGENTS.md`, `ia/specs/glossary.md`, `ARCHITECTURE.md`, `ia/rules/*.md`, or `.claude/memory/{slug}.md` per Q12 (>~10 lines → per-decision file).
> 4. **Persist journal** (non-destructive) — `mcp__territory-ia__project_spec_journal_persist` with `issue_id`. Outcomes: `ok`, `db_unconfigured` (skip), `db_error` (log + continue unless user overrides).
> 5. **Validate** — `npm run validate:dead-project-specs` + `npm run validate:all`. Stop on failure.
> 6. **Destructive ops** — delete spec (`rm <single-file>`), remove BACKLOG row, append `[x] **$ARGUMENTS**` to `BACKLOG-ARCHIVE.md`, purge id from durable docs/code via targeted Edit (Grep first to enumerate).
> 6b. **Regenerate progress dashboard** — `npm run progress` (repo root). Reflects `Done (archived)` state in `docs/progress.html`. Deterministic; failure does NOT block step 7 — log exit code and continue.
> 7. **Re-validate** — `npm run validate:dead-project-specs` after deletion.
>
> ## Hard boundaries
>
> - Do NOT `rm -rf`. Spec deletion = `rm <single-file>`.
> - Do NOT run `project-stage-close` from here — inline path for `spec-implementer`.
> - Do NOT delete spec before lessons migrated.
> - Do NOT skip post-delete `validate:dead-project-specs`. Close incomplete until validator confirms path gone.
> - Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.
>
> ## Output
>
> Single closeout digest per `.claude/output-styles/closeout-digest.md`: fenced JSON header (`{issue_id, spec_path, lessons_migrated, journal_persist, validate_*, spec_deleted, backlog_row_removed, archive_appended, id_purged_from, validate_dead_specs_post}`), then caveman markdown summary.
