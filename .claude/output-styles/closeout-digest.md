---
name: Closeout digest
description: Structured umbrella-close report (lessons migrated, journal persisted, validate exit codes, confirmation gate result, spec deleted, BACKLOG row removed, archive entry appended, IDs purged). Used by the `closeout` subagent and the `/closeout` slash command.
---

You are emitting a **closeout digest** for an umbrella close run. The digest is the final report after all destructive operations have completed (or after the user has aborted at the confirmation gate).

## Output structure (mandatory)

The digest has **two parts** in this exact order:

### Part 1 — Fenced JSON header (required)

A single fenced code block tagged `json` with one object summarizing the close:

```json
{
  "issue_id": "TECH-XX",
  "spec_path": "ia/projects/TECH-XX-{description}.md",
  "lessons_migrated": {
    "count": 0,
    "targets": [
      { "surface": "docs/information-architecture-overview.md", "bullets": 0 },
      { "surface": "AGENTS.md", "bullets": 0 },
      { "surface": "ia/specs/glossary.md", "bullets": 0 }
    ]
  },
  "journal_persist": {
    "outcome": "ok",
    "row_ids": [],
    "reason": null
  },
  "validate_dead_specs_pre": { "exit_code": 0 },
  "validate_all_pre":        { "exit_code": 0 },
  "confirmation_gate":       { "result": "yes", "aborted_items": [] },
  "spec_deleted":            { "path": "ia/projects/TECH-XX-{description}.md", "ok": true },
  "backlog_row_removed":     { "id": "TECH-XX", "ok": true },
  "archive_appended":        { "line": "[x] **TECH-XX** — {title}", "ok": true },
  "id_purged_from": [
    { "file": "docs/example.md", "occurrences": 0 }
  ],
  "validate_dead_specs_post": { "exit_code": 0 }
}
```

Field rules:

- **`journal_persist.outcome`** — one of `"ok"`, `"db_unconfigured"`, `"db_error"`. On `"db_unconfigured"`, set `reason` to the skip reason (e.g. `"no DATABASE_URL and no config/postgres-dev.json"`). On `"db_error"`, paste the error string in `reason`.
- **`confirmation_gate.result`** — one of `"yes"`, `"no"`, `"partial"`. On `"partial"` or `"no"`, list the items the user declined under `aborted_items`. On `"no"`, every destructive section below (`spec_deleted`, `backlog_row_removed`, `archive_appended`, `id_purged_from`) must show `ok: false` and an empty / null body.
- **`spec_deleted.ok`** — `false` if the deletion failed or the user aborted. The path field still records the intended target.
- **`id_purged_from`** — array of `{file, occurrences}` per file the closed id was removed from. Empty array means no purges were needed.
- **`validate_dead_specs_post`** — re-run after deletion. **Exit code 0 is the close criterion.** If non-zero, the close is incomplete and the digest must say so in the summary.
- **JSON exempt from caveman.** Field names and values stay in standard JSON.
- **JSON must parse.** No trailing commas, no comments.

### Part 2 — Caveman markdown summary (required)

After the JSON code block, emit a short markdown summary in **caveman** voice (drop articles/filler/pleasantries/hedging; fragments OK; pattern `[thing] [action] [reason]. [next step].`). Cover, in order:

1. Lessons migrated (count + target surfaces).
2. Journal persistence outcome.
3. Pre-delete validate exit codes.
4. Confirmation gate result.
5. Destructive ops (spec deleted, BACKLOG row removed, archive line appended, id purges).
6. Post-delete `validate:dead-project-specs` exit code.
7. Next step ("close complete" or "close incomplete — {reason}").

Standard caveman exceptions still apply: code identifiers, commit messages, security/auth content, verbatim error / tool output. The digest **does not include** the original confirmation prompt — that prompt was full English and lives in the chat history before this digest. The digest's summary is post-confirmation.

## Examples

### Example — clean close

```json
{
  "issue_id": "TECH-11",
  "spec_path": "ia/projects/TECH-11-example-spec.md",
  "lessons_migrated": {
    "count": 33,
    "targets": [
      { "surface": "docs/information-architecture-overview.md", "bullets": 12 },
      { "surface": "AGENTS.md", "bullets": 8 },
      { "surface": "ia/specs/glossary.md", "bullets": 5 },
      { "surface": ".claude/memory/tech-11-permissions.md", "bullets": 8 }
    ]
  },
  "journal_persist": { "outcome": "ok", "row_ids": ["123", "124"], "reason": null },
  "validate_dead_specs_pre": { "exit_code": 0 },
  "validate_all_pre":        { "exit_code": 0 },
  "confirmation_gate":       { "result": "yes", "aborted_items": [] },
  "spec_deleted":            { "path": "ia/projects/TECH-11-example-spec.md", "ok": true },
  "backlog_row_removed":     { "id": "TECH-11", "ok": true },
  "archive_appended":        { "line": "[x] **TECH-11** — example issue title", "ok": true },
  "id_purged_from": [
    { "file": "docs/information-architecture-overview.md", "occurrences": 2 },
    { "file": "AGENTS.md", "occurrences": 1 }
  ],
  "validate_dead_specs_post": { "exit_code": 0 }
}
```

- lessons migrated 33. surfaces: ia overview 12, AGENTS 8, glossary 5, memory file 8.
- journal ok. row_ids 123/124.
- validate pre 0/0. green.
- confirmation gate yes. all destructive ops authorized.
- spec deleted `ia/projects/TECH-11-example-spec.md`.
- BACKLOG row TECH-11 removed.
- archive appended.
- id purged from 2 files (3 occurrences total).
- validate post 0. green.
- close complete.

### Example — user aborted

```json
{
  "issue_id": "TECH-99",
  "spec_path": "ia/projects/TECH-99-example.md",
  "lessons_migrated": { "count": 4, "targets": [{ "surface": "AGENTS.md", "bullets": 4 }] },
  "journal_persist": { "outcome": "ok", "row_ids": ["200"], "reason": null },
  "validate_dead_specs_pre": { "exit_code": 0 },
  "validate_all_pre":        { "exit_code": 0 },
  "confirmation_gate":       { "result": "no", "aborted_items": ["spec_delete", "backlog_row_remove", "archive_append", "id_purge"] },
  "spec_deleted":            { "path": "ia/projects/TECH-99-example.md", "ok": false },
  "backlog_row_removed":     { "id": "TECH-99", "ok": false },
  "archive_appended":        { "line": null, "ok": false },
  "id_purged_from":          [],
  "validate_dead_specs_post": { "exit_code": null }
}
```

- lessons migrated 4. AGENTS only.
- journal ok. row 200.
- validate pre 0. green.
- confirmation gate NO. user declined destructive ops.
- spec NOT deleted. backlog row NOT removed. archive NOT appended. no id purges.
- close incomplete — user aborted at confirmation gate. lessons + journal stay. spec preserved.

## Hard rules

- JSON header **first**, summary **second**. Never reverse.
- The confirmation gate prompt (full English) lives in chat history **before** this digest. Do not re-emit it inside the digest.
- Never compress the JSON with caveman.
- Never fabricate exit codes or row ids.
- On `confirmation_gate.result = "no"`, every destructive section shows `ok: false`. Do not pretend the close completed.
- On `validate_dead_specs_post.exit_code != 0`, the summary must end with "close incomplete — dead-spec validator non-zero" and the user must be told what to fix.
- Never wrap the digest in extra prose. The digest is the entire response.
