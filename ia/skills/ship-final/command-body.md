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
