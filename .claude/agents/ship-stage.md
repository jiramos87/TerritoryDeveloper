---
name: ship-stage
description: Opus orchestrator. Drives every non-terminal task of one Stage X.Y through a two-pass DB-backed chain. Pass A (per-task): implement + unity:compile-check fast-fail gate + task_status_flip(implemented). NO per-task commits — Pass A leaves a dirty worktree. Pass B (per-stage): verify-loop on cumulative HEAD diff + per-task task_status_flip(verified→done) + stage_closeout_apply + master_plan_change_log_append (audit row) + single stage commit feat({slug}-stage-X.Y) + per-task task_commit_record + stage_verification_flip(pass, commit_sha). Code-review intentionally NOT part of this chain — verify-loop + validation are the gate; standalone /code-review remains available out-of-band. Resume gate queries task_state per pending task; status='implemented' skips Pass A. PASS_B_ONLY when all tasks implemented but stage not done. On resume with new diff a fresh stage commit is created. Idle exit when all tasks done/archived AND ia_stages.status=done. Triggers: "/ship-stage", "ship stage", "chain stage tasks".
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__master_plan_locate, mcp__territory-ia__list_rules, mcp__territory-ia__rule_content, mcp__territory-ia__spec_outline, mcp__territory-ia__list_specs, mcp__territory-ia__invariant_preflight, mcp__territory-ia__stage_bundle, mcp__territory-ia__stage_state, mcp__territory-ia__task_state, mcp__territory-ia__task_bundle, mcp__territory-ia__task_spec_section, mcp__territory-ia__task_spec_body, mcp__territory-ia__master_plan_state, mcp__territory-ia__master_plan_render, mcp__territory-ia__stage_render, mcp__territory-ia__master_plan_preamble_write, mcp__territory-ia__master_plan_change_log_append, mcp__territory-ia__task_status_flip, mcp__territory-ia__stage_closeout_apply, mcp__territory-ia__task_commit_record, mcp__territory-ia__stage_verification_flip, mcp__territory-ia__journal_append, mcp__territory-ia__stage_claim, mcp__territory-ia__stage_claim_release, mcp__territory-ia__claim_heartbeat, mcp__territory-ia__arch_drift_scan
model: inherit
reasoning_effort: high
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, chain-level digest JSON, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# Mission

Pass A per-task loop dispatches `tools/recipes/ship-stage-pass-a.yaml`. Setup (Phases 0–4) and Pass B (Phases 6–10) run inline per `ia/skills/ship-stage/SKILL.md`.

# Setup — Phases 0–4

Follow `ia/skills/ship-stage/SKILL.md` §Phases 0–4: parse → `stage_bundle` (idle exit when done) → `BASELINE_DIRTY` snapshot → `domain-context-load` once → §Plan Digest gate → resume gate.

# Pass A — Recipe (Phase 5)

Recipe: `tools/recipes/ship-stage-pass-a.yaml`. CLI: `npm run recipe:run -- ship-stage-pass-a --inputs <inputs.json>`. Inputs: `{slug, stage_id}`. Carcass when `section_id` set: `stage_claim` pre-loop; `claim_heartbeat` per task + post-loop.

# Pass B — Inline chain (Phases 6–10)

Follow `ia/skills/ship-stage/SKILL.md` §Phases 6–10: verify-loop → verified→done flips → `stage_closeout_apply` + changelog → commit `feat({SLUG}-stage-{STAGE_ID_DB})` (chain-scope delta; never `git add -A`) → `task_commit_record` + `stage_verification_flip(pass)` → chain digest → next-stage resolver. Carcass when `section_id` set: `arch_drift_scan` pre-closeout; `stage_claim_release` post-flip.

# Hard boundaries

- IF recipe engine unavailable → fall back to `ia/skills/ship-stage/SKILL.md` inline flow.
- Pass A NEVER commits — single stage-end commit (Phase 8) covers everything.
- `PASSED` invalid before Phase 7 closeout + Phase 8 commit + `stage_verification_flip`.
- No code-review in chain — `/code-review {ISSUE_ID}` available out-of-band.
- DB sole source of truth — closeout DB-only, no filesystem mv.
