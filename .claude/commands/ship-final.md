---
description: Close a master-plan version. Phases: assert all `ia_section_claims` open rows for the slug = 0 → assert all `ia_stages.status` ∈ {`done`} (no `pending` / `in_progress` / `partial`) → run plan-scoped `validate:fast --diff-paths` on union of paths touched by `ia_task_commits` rows for slug (fallback: `validate:fast` HEAD-diff when DB unreachable / no commits recorded) → `git tag {slug}-v{N}` (annotated, local only) → flip `ia_master_plans.closed_at = now()` via `master_plan_close` MCP (must precede cron_journal_append_enqueue) → `cron_journal_append_enqueue(phase=version-close, payload_kind=version_close, payload={plan_slug, version, tag, sha, validate_all_result, sections_closed[]})`. Triggers: "/ship-final {SLUG}", "ship final", "close master plan version".
argument-hint: "{SLUG}"
---

# /ship-final — Close a master-plan version: assert sections closed → assert all stages done (no `partial`) → run `validate:fast --diff-paths` scoped to plan's task commits (paths derived from `ia_task_commits` for slug; falls back to HEAD-diff if DB unreachable) → `git tag {slug}-v{N}` → flip `ia_master_plans.closed_at` → journal closeout row. Final gate of the ship-protocol cycle. Mechanical — no decisions.

Drive `$ARGUMENTS` via the [`ship-final`](../agents/ship-final.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /ship-final {SLUG}
- ship final
- close master plan version
<!-- skill-tools:body-override -->

`$ARGUMENTS` = `{SLUG}`. Required. Closes one master-plan version (mechanical).

## Mission

Final gate of ship-protocol cycle. Assert sections closed → assert all stages `done` (no `partial`) → cumulative `validate:all` → `git tag {slug}-v{N}` → flip `closed_at` → journal closeout row.

## Recipe invocation

```bash
npm run recipe:run -- ship-final \
  --input slug={SLUG}
```

Recipe steps:

1. `load_plan` — `master_plan_state(slug)` → version + stages.
2. `assert_sections_closed` — open `ia_section_claims` for slug = 0. STOP on > 0.
3. `assert_stages_done` — all `stages[].status === 'done'`. STOP on any `partial` / `pending` / `in_progress`.
4. `cumulative_validate` — `git diff {parent_tag}..HEAD` + `npm run validate:all`. STOP on non-zero exit.
5. `git_tag` — `git tag -a {slug}-v{N} -m "Close {slug} v{N}"` (local only).
6. `close_plan` — `master_plan_close(slug)` MCP — flips `closed_at`. Must precede journal.
7. `journal_close` — `cron_journal_append_enqueue(payload_kind=version_close, payload={plan_slug, version, tag, sha, validate_all_result, sections_closed[]})`.

## Hard boundaries

- IF open section claim → STOP. `/section-closeout` first.
- IF stage not done → STOP. `/ship-stage` first.
- IF `validate:all` red → STOP.
- IF `closed_at` already set → STOP `version_already_closed`.
- Do NOT push tag (local only).
- Do NOT create v(N+1) row (separate MCP).
- Do NOT commit — closure is metadata-only.
