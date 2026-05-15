# Mission

Master-plan version close — assert sections closed → assert stages done (no `partial`) → Pass B critic review → cumulative `validate:all` → `git tag {slug}-v{N}` → flip `closed_at` → journal closeout row. Mechanical, no decisions. Final gate of ship-protocol cycle.

# Recipe

`tools/recipes/ship-final.yaml`. CLI: `npm run recipe:run -- ship-final --input slug={SLUG}`.

# Phase sequence

1. `load_plan` — `master_plan_state(slug)` → `{version, closed_at, stages[]}`.
2. `assert_sections_closed` — open `ia_section_claims` rows for slug must equal 0.
3. `assert_stages_done` — every `stages[].status === 'done'`. `partial` / `pending` / `in_progress` → STOP.
4. **`pass_b_critics`** — dispatch all 3 critic subagents in parallel via Agent tool (one message, three tool uses):
   - `/critic-style {slug} {cumulative_diff}` → findings via `review_findings_write` MCP.
   - `/critic-logic {slug} {cumulative_diff}` → findings via `review_findings_write` MCP.
   - `/critic-security {slug} {cumulative_diff}` → findings via `review_findings_write` MCP.
   After all 3 return: query `ia_review_findings WHERE plan_slug = '{slug}' AND severity = 'high'`.
   - IF `count > 0` AND no operator override → `AskUserQuestion("Critic found {count} high-severity finding(s). Override to proceed? (yes/no)")`.
     - `yes` → log override: `cron_arch_changelog_append_enqueue(kind='critic_override', slug, body='Operator overrode {count} high-severity findings at ship-final.')`. Continue.
     - `no` → STOP `critic_high_severity_block`.
   - IF `count == 0` → continue immediately.
5. `cumulative_validate` — plan-scoped `validate:fast --diff-paths` over union of `git show --name-only` paths from `ia_task_commits` shas for slug (HEAD-diff fallback when DB unreachable). Non-zero → STOP.
6. `assert_post_close_validate_drained` — `cron_validate_post_close_jobs` queued+running rows for slug = 0. Non-zero → STOP (drainer behind; re-run after drained).
7. `git_tag` — `git tag -a {slug}-v{N} -m "Close {slug} v{N}"`. Local only, never push.
8. `close_plan` — `master_plan_close(slug)` MCP. Flips `closed_at = now()`. Must precede journal.
9. `journal_close` — `cron_journal_append_enqueue(phase=version-close, payload_kind=version_close, payload={plan_slug, version, tag, sha, validate_all_result, sections_closed[], critic_findings_count, critic_high_count})`.

# Hard boundaries

- IF open section → STOP. `/section-closeout` first.
- IF stage not `done` → STOP. `/ship-cycle` first.
- IF Pass B critics return `severity=high` AND no override → STOP `critic_high_severity_block`. AskUserQuestion override path mandatory.
- Override path MUST log to `arch_changelog` kind=`critic_override` before continuing.
- Critics dispatched in parallel — one message, three Agent tool uses. Never sequential.
- IF plan-scoped `validate:fast` red → STOP. Fix + re-run. Scope = paths from `ia_task_commits` for slug; unrelated-plan drift cannot block.
- IF `cron_validate_post_close_jobs` open rows for slug → STOP. Drainer behind; re-run after drained.
- IF `closed_at` already set → STOP `version_already_closed`.
- Do NOT push tag — local only.
- Do NOT create v(N+1) row — separate MCP `master_plan_version_create`.
- Do NOT skip `closed_at` flip — must precede cron_journal_append_enqueue.
- Do NOT commit — closure metadata-only.
