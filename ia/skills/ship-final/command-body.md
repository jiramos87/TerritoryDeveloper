`$ARGUMENTS` = `{SLUG}`. Required. Closes one master-plan version (mechanical).

## Mission

Final gate of ship-protocol cycle. Assert sections closed → assert all stages `done` (no `partial`) → Pass B critic review (3 subagents in parallel) → plan-scoped `validate:fast --diff-paths` → assert post-close validate drainer caught up → `git tag {slug}-v{N}` → flip `closed_at` → journal closeout row.

## Recipe invocation

```bash
npm run recipe:run -- ship-final \
  --input slug={SLUG}
```

Recipe steps:

1. `load_plan` — `master_plan_state(slug)` → version + stages.
2. `assert_sections_closed` — open `ia_section_claims` for slug = 0. STOP on > 0.
3. `assert_stages_done` — all `stages[].status === 'done'`. STOP on any `partial` / `pending` / `in_progress`.
4. `pass_b_critics` — dispatch 3 critics in parallel (one message, 3 Agent tool uses):
   `/critic-style`, `/critic-logic`, `/critic-security`. Each emits findings via `review_findings_write`.
   After all 3 return: if `ia_review_findings.severity='high'` count > 0 → `AskUserQuestion` override prompt.
   Override → log `arch_changelog kind=critic_override`. No override → STOP `critic_high_severity_block`.
5. `cumulative_validate` — `validate:fast --diff-paths` over plan's `ia_task_commits` paths (HEAD-diff fallback when DB unreachable). STOP on non-zero exit.
6. `assert_post_close_validate_drained` — `cron_validate_post_close_jobs` queued+running rows for slug = 0. STOP on > 0 (drainer behind; re-run after drained).
7. `git_tag` — `git tag -a {slug}-v{N} -m "Close {slug} v{N}"` (local only).
8. `close_plan` — `master_plan_close(slug)` MCP — flips `closed_at`. Must precede journal.
9. `journal_close` — `cron_journal_append_enqueue(payload_kind=version_close, payload={plan_slug, version, tag, sha, validate_all_result, sections_closed[], critic_findings_count, critic_high_count})`.

## Hard boundaries

- IF open section claim → STOP. `/section-closeout` first.
- IF stage not done → STOP. `/ship-cycle` first.
- IF `severity=high` finding AND no operator override → STOP `critic_high_severity_block`.
- Critics MUST be dispatched in parallel — one message, 3 Agent tool uses.
- Override MUST log to `arch_changelog kind=critic_override` before continuing.
- IF plan-scoped `validate:fast` red → STOP.
- IF post-close validate drainer behind → STOP. Re-run after drained.
- IF `closed_at` already set → STOP `version_already_closed`.
- Do NOT push tag (local only).
- Do NOT create v(N+1) row (separate MCP).
- Do NOT commit — closure is metadata-only.
