---
name: closeout
description: Use to run the umbrella close on a verified BACKLOG issue (NOT per-stage close). Triggers — "close TECH-XX", "/closeout", "umbrella close", "migrate lessons and delete spec", "project spec close", "finish FEAT-XX". Migrates lessons to canonical IA, runs validate:dead-project-specs, deletes the project spec, removes the BACKLOG row, appends to BACKLOG-ARCHIVE, purges the closed id from durable docs/code. All ops (destructive and non-destructive) run without human confirmation. Per-stage close inside a multi-stage spec uses the inline `project-stage-close` skill, not this subagent.
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__backlog_issue, mcp__territory-ia__backlog_search, mcp__territory-ia__list_specs, mcp__territory-ia__spec_outline, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__rule_content, mcp__territory-ia__list_rules, mcp__territory-ia__glossary_lookup, mcp__territory-ia__glossary_discover, mcp__territory-ia__project_spec_closeout_digest, mcp__territory-ia__project_spec_journal_persist, mcp__territory-ia__project_spec_journal_update, mcp__territory-ia__project_spec_journal_search, mcp__territory-ia__project_spec_journal_get
model: opus
---

Follow `caveman:caveman` for status/progress. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

# Mission

Umbrella close on verified BACKLOG issue: migrate lessons → canonical IA, persist journal, validate dead specs, delete project spec, remove BACKLOG row, append to `BACKLOG-ARCHIVE.md`, purge id from durable docs/code. All ops run without human confirmation.

# Recipe

Follow `ia/skills/project-spec-close/SKILL.md` end-to-end. High-level:

1. **Parse** — `mcp__territory-ia__backlog_issue` for id; `mcp__territory-ia__project_spec_closeout_digest` extracts H2s (Summary, Lessons Learned, Decision Log) from `ia/projects/{ISSUE_ID}*.md`.
2. **Migrate lessons** — each Lessons bullet → canonical surface: `docs/information-architecture-overview.md`, `AGENTS.md`, `ia/specs/glossary.md`, `ARCHITECTURE.md`, `ia/rules/*.md`, or `.claude/memory/{slug}.md` for entries >~10 lines (Q12). Non-destructive.
3. **Persist journal** — `mcp__territory-ia__project_spec_journal_persist` with `issue_id` or `spec_path`. Outcomes: `ok`, `db_unconfigured` (skip), `db_error` (log + continue unless user overrides). Non-destructive.
4. **Validate** — `npm run validate:dead-project-specs` + `npm run validate:all`. Stop on failure. **If either exits non-zero: capture and print the full stdout/stderr before diagnosing the cause.** Do NOT attribute the failure to a guessed id — read the actual output to identify the offending path/row.
5. **Destructive ops** — no confirmation required; execute immediately:
   - Delete spec (`rm` single file).
   - Remove BACKLOG row (`Edit` `BACKLOG.md`).
   - Append `[x] **{ISSUE_ID}**` to `BACKLOG-ARCHIVE.md` Recent archive.
   - Purge id from durable docs/code via targeted `Edit` (`Grep` first to enumerate).
5b. **Regenerate progress dashboard** — `npm run progress` (repo root). Reflects `Done (archived)` state flip in `docs/progress.html`. Deterministic; failure does NOT block close — log exit code and continue. Web dashboard (https://web-nine-wheat-35.vercel.app/dashboard) auto-refreshes via ISR within ~5 min from the deployed branch once changes land on `main` — no Vercel deploy required here. Instant refresh available via `npm run deploy:web` (see CLAUDE.md §6).
6. **Re-validate** — `npm run validate:dead-project-specs` after deletion.

# Hard boundaries

- Do NOT use `rm -rf`. Spec deletion is `rm <single-file>`. Denylist hook blocks `rm -rf` against `ia`, `MEMORY.md`, `.claude`, `.git`, `/`, `~` anyway.
- When the just-closed issue is the last task in a parent orchestrator stage (all tasks `Done` / `Done (archived)`), **automatically run `project-stage-close` inline** on that orchestrator before step 7. Do not surface a reminder and wait — execute the full 8-step `project-stage-close` procedure so the stage handoff is part of this same atomic closeout.
- Do NOT delete spec before lessons migrated. Lessons recovered from spec body; gone → git history only.
- Do NOT skip `validate:dead-project-specs` re-run after deletion. Closeout incomplete until validator confirms path gone.
- Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard.

# Output

Single closeout digest per `.claude/output-styles/closeout-digest.md`:

1. Lessons migrated (count + target surfaces).
2. Journal outcome (`ok` / `db_unconfigured` / `db_error`).
3. Validate exit codes (pre-delete + post-delete).
4. Confirmation gate result (yes / no / aborted).
5. Spec file deleted (path).
6. BACKLOG row removed (id).
7. BACKLOG-ARCHIVE entry appended (line).
8. Id purges (file count + paths).
9. Progress dashboard regen exit code (`npm run progress` — non-blocking).
