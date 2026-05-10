---
name: ship-final
description: Close a master-plan version. Phases: assert all `ia_section_claims` open rows for the slug = 0 → assert all `ia_stages.status` ∈ {`done`} (no `pending` / `in_progress` / `partial`) → run plan-scoped `validate:fast --diff-paths` on union of paths touched by `ia_task_commits` rows for slug (fallback: `validate:fast` HEAD-diff when DB unreachable / no commits recorded) → `git tag {slug}-v{N}` (annotated, local only) → flip `ia_master_plans.closed_at = now()` via `master_plan_close` MCP (must precede cron_journal_append_enqueue) → `cron_journal_append_enqueue(phase=version-close, payload_kind=version_close, payload={plan_slug, version, tag, sha, validate_all_result, sections_closed[]})`. Triggers: "/ship-final {SLUG}", "ship final", "close master plan version".
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__master_plan_locate, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__master_plan_state, mcp__territory-ia__master_plan_close, mcp__territory-ia__cron_journal_append_enqueue
model: opus
reasoning_effort: high
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# Mission

Master-plan version close — assert sections closed → assert stages done (no `partial`) → cumulative `validate:all` → `git tag {slug}-v{N}` → flip `closed_at` → journal closeout row. Mechanical, no decisions. Final gate of ship-protocol cycle.

# Recipe

`tools/recipes/ship-final.yaml`. CLI: `npm run recipe:run -- ship-final --input slug={SLUG}`.

# Phase sequence

1. `load_plan` — `master_plan_state(slug)` → `{version, closed_at, stages[]}`.
2. `assert_sections_closed` — open `ia_section_claims` rows for slug must equal 0.
3. `assert_stages_done` — every `stages[].status === 'done'`. `partial` / `pending` / `in_progress` → STOP.
4. `cumulative_validate` — plan-scoped `validate:fast --diff-paths` over union of `git show --name-only` paths from `ia_task_commits` shas for slug (HEAD-diff fallback when DB unreachable). Non-zero → STOP.
5. `assert_post_close_validate_drained` — `cron_validate_post_close_jobs` queued+running rows for slug = 0. Non-zero → STOP (drainer behind; re-run after drained).
6. `git_tag` — `git tag -a {slug}-v{N} -m "Close {slug} v{N}"`. Local only, never push.
7. `close_plan` — `master_plan_close(slug)` MCP. Flips `closed_at = now()`. Must precede journal.
8. `journal_close` — `cron_journal_append_enqueue(phase=version-close, payload_kind=version_close, payload={plan_slug, version, tag, sha, validate_all_result, sections_closed[]})`.

# Hard boundaries

- IF open section → STOP. `/section-closeout` first.
- IF stage not `done` → STOP. `/ship-cycle` first.
- IF plan-scoped `validate:fast` red → STOP. Fix + re-run. Scope = paths from `ia_task_commits` for slug; unrelated-plan drift cannot block.
- IF `cron_validate_post_close_jobs` open rows for slug → STOP. Drainer behind; re-run after drained.
- IF `closed_at` already set → STOP `version_already_closed`.
- Do NOT push tag — local only.
- Do NOT create v(N+1) row — separate MCP `master_plan_version_create`.
- Do NOT skip `closed_at` flip — must precede cron_journal_append_enqueue.
- Do NOT commit — closure metadata-only.
