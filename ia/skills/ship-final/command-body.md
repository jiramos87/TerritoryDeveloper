`$ARGUMENTS` = `{SLUG}`. Required. Closes one master-plan version (mechanical).

## Mission

Final gate of ship-protocol cycle. Assert sections closed → assert all stages `done` (no `partial`) → plan-scoped `validate:fast --diff-paths` → assert post-close validate drainer caught up → `git tag {slug}-v{N}` → flip `closed_at` → journal closeout row.

## Recipe invocation

```bash
npm run recipe:run -- ship-final \
  --input slug={SLUG}
```

Recipe steps:

1. `load_plan` — `master_plan_state(slug)` → version + stages.
2. `assert_sections_closed` — open `ia_section_claims` for slug = 0. STOP on > 0.
3. `assert_stages_done` — all `stages[].status === 'done'`. STOP on any `partial` / `pending` / `in_progress`.
4. `cumulative_validate` — `validate:fast --diff-paths` over plan's `ia_task_commits` paths (HEAD-diff fallback when DB unreachable). STOP on non-zero exit.
5. `assert_post_close_validate_drained` — `cron_validate_post_close_jobs` queued+running rows for slug = 0. STOP on > 0 (drainer behind; re-run after drained).
6. `git_tag` — `git tag -a {slug}-v{N} -m "Close {slug} v{N}"` (local only).
7. `close_plan` — `master_plan_close(slug)` MCP — flips `closed_at`. Must precede journal.
8. `journal_close` — `cron_journal_append_enqueue(payload_kind=version_close, payload={plan_slug, version, tag, sha, validate_all_result, sections_closed[]})`.

## Hard boundaries

- IF open section claim → STOP. `/section-closeout` first.
- IF stage not done → STOP. `/ship-cycle` first.
- IF plan-scoped `validate:fast` red → STOP.
- IF post-close validate drainer behind → STOP. Re-run after drained.
- IF `closed_at` already set → STOP `version_already_closed`.
- Do NOT push tag (local only).
- Do NOT create v(N+1) row (separate MCP).
- Do NOT commit — closure is metadata-only.
