# Mission

Master-plan version close — assert sections closed → assert stages done (no `partial`) → cumulative `validate:all` → `git tag {slug}-v{N}` → flip `closed_at` → journal closeout row. Mechanical, no decisions. Final gate of ship-protocol cycle.

# Recipe

`tools/recipes/ship-final.yaml`. CLI: `npm run recipe:run -- ship-final --input slug={SLUG}`.

# Phase sequence

1. `load_plan` — `master_plan_state(slug)` → `{version, closed_at, stages[]}`.
2. `assert_sections_closed` — open `ia_section_claims` rows for slug must equal 0.
3. `assert_stages_done` — every `stages[].status === 'done'`. `partial` / `pending` / `in_progress` → STOP.
4. `cumulative_validate` — `git diff {parent_tag}..HEAD` + `npm run validate:all`. Non-zero → STOP.
5. `git_tag` — `git tag -a {slug}-v{N} -m "Close {slug} v{N}"`. Local only, never push.
6. `close_plan` — `master_plan_close(slug)` MCP. Flips `closed_at = now()`. Must precede journal.
7. `journal_close` — `cron_journal_append_enqueue(phase=version-close, payload_kind=version_close, payload={plan_slug, version, tag, sha, validate_all_result, sections_closed[]})`.

# Hard boundaries

- IF open section → STOP. `/section-closeout` first.
- IF stage not `done` → STOP. `/ship-stage` first.
- IF `validate:all` red → STOP. Fix + re-run.
- IF `closed_at` already set → STOP `version_already_closed`.
- Do NOT push tag — local only.
- Do NOT create v(N+1) row — separate MCP `master_plan_version_create`.
- Do NOT skip `closed_at` flip — must precede cron_journal_append_enqueue.
- Do NOT commit — closure metadata-only.
