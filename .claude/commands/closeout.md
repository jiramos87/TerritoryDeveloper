---
description: Close an issue end-to-end — umbrella close (NOT per-stage). Dispatches the `closeout` subagent for the given issue with explicit confirmation before destructive operations (per Q6 resolution).
argument-hint: "{ISSUE_ID} (e.g. TECH-85)"
---

# /closeout — dispatch `closeout` subagent

Use the **`closeout`** subagent (defined in `.claude/agents/closeout.md`) to run the umbrella close on `$ARGUMENTS`. The subagent **pauses for explicit human confirmation** before destructive operations (spec deletion, BACKLOG row removal, archive append, id purge), per Q6 resolution from TECH-85 §6 Decision Log. Per-stage close inside a multi-stage spec uses the inline `project-stage-close` skill, not this command.

## Subagent prompt (forward verbatim)

Forward the following prompt to the subagent via the Agent tool with `subagent_type: "closeout"`:

> Follow `caveman:caveman` skill rules for status reporting and progress messages (drop articles/filler/pleasantries/hedging; fragments OK). Standard exceptions apply: code, commits, security/auth content, verbatim error/tool output, structured MCP inputs/outputs. **Confirmation prompts before destructive operations stay in normal English** so the human is not asked to disambiguate fragments under risk — spec deletion, BACKLOG row removal, archive append, id purge, and the final "proceed?" question are full English. Project anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run the `project-spec-close` skill (`ia/skills/project-spec-close/SKILL.md`) — the **umbrella** close (not per-stage) — on the verified issue `$ARGUMENTS`. Migrate lessons to canonical IA, persist the journal, validate dead spec paths, **pause for explicit human confirmation**, then delete the project spec, remove the BACKLOG row, append to `BACKLOG-ARCHIVE.md`, and purge the closed id from durable docs / code.
>
> ## Sequence
>
> 1. `mcp__territory-ia__backlog_issue` for `$ARGUMENTS`.
> 2. `mcp__territory-ia__project_spec_closeout_digest` to extract H2 sections from `ia/projects/$ARGUMENTS*.md` (resolve actual filename via Glob; spec may be `$ARGUMENTS.md` or `$ARGUMENTS-{description}.md`).
> 3. **Migrate lessons** (non-destructive) — copy each Lessons Learned bullet into `docs/information-architecture-overview.md`, `AGENTS.md`, `ia/specs/glossary.md`, `ARCHITECTURE.md`, `ia/rules/*.md`, or `.claude/memory/{slug}.md` per Q12 (entries exceeding ~10 lines get a per-decision file).
> 4. **Persist journal** (non-destructive) — `mcp__territory-ia__project_spec_journal_persist` with `issue_id`. Acceptable outcomes: `ok`, `db_unconfigured` (graceful skip), `db_error` (log + continue unless user says otherwise).
> 5. **Validate** — `npm run validate:dead-project-specs` and `npm run validate:all`. Stop on failure.
> 6. **CONFIRMATION GATE** — emit a full-English prompt listing the destructive operations queued. Wait for explicit human "yes". Do not proceed on ambiguous responses.
> 7. **Destructive ops** (only after confirmation) — delete the project spec (`rm <single-file>`), remove the BACKLOG row, append `[x] **$ARGUMENTS**` to `BACKLOG-ARCHIVE.md`, purge the closed id from durable docs / code via targeted Edit calls (use Grep first to enumerate).
> 8. **Re-validate** — `npm run validate:dead-project-specs` after deletion to confirm the dead-path scanner is clean.
>
> ## Hard boundaries
>
> - Do NOT proceed past the confirmation gate without an explicit "yes" from the human. Ambiguous responses are not consent.
> - Do NOT use `rm -rf` on anything. Spec deletion is `rm <single-file>`.
> - Do NOT run `project-stage-close` from this subagent. That is the inline path used by `spec-implementer` mid-execution.
> - Do NOT delete the spec before lessons have been migrated.
> - Do NOT skip the post-delete `validate:dead-project-specs` re-run. Close is incomplete until the validator confirms the path is gone.
> - Do NOT touch `.claude/settings.json` `permissions.defaultMode` or the `mcp__territory-ia__*` wildcard.
> - Do NOT compress confirmation prompts with caveman. They stay in full English.
>
> ## Output
>
> Single closeout digest formatted per `.claude/output-styles/closeout-digest.md`: fenced JSON header (`{issue_id, spec_path, lessons_migrated, journal_persist, validate_*, confirmation_gate, spec_deleted, backlog_row_removed, archive_appended, id_purged_from, validate_dead_specs_post}`), then caveman markdown summary.
