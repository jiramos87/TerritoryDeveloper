---
description: Use when a multi-bucket umbrella master-plan (DB-backed, e.g. slug `full-game-mvp`) needs a repeatable rollout process that drives each child orchestrator through the lifecycle (a)–(g) up to step (f) ≥1-task-filed. Orchestrates per-row handoffs to `/design-explore`, `/master-plan-new`, `/master-plan-extend`, `/stage-decompose`, `/stage-file`. Owns the tracker doc (`docs/{umbrella-slug}-rollout-tracker.md`) + invokes helper skills (`release-rollout-enumerate`, `release-rollout-track`, `release-rollout-skill-bug-log`). Does NOT close issues (handled inline by `/ship-stage` Pass B). Does NOT execute Tier A→E rollout body directly — dispatches to per-row subagents in fresh context. Triggers: "/release-rollout {row-slug}", "rollout next row", "drive child plan to task-filed", "release rollout track".
argument-hint: "{UMBRELLA_SLUG} {ROW_SLUG} [OPERATION] (e.g. full-game-mvp zone-s-economy advance | status | next)"
---

# /release-rollout — Umbrella rollout orchestration — track + drive every child master-plan under an umbrella (e.g. full-game-mvp) through the 7-column lifecycle (a)–(g) to step (f) ≥1-task-filed.

Drive `$ARGUMENTS` via the [`release-rollout`](../agents/release-rollout.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, tracker prose + disagreements appendix entries (human-consumed cold — may run 2–4 sentences). Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /release-rollout {row-slug}
- rollout next row
- drive child plan to task-filed
- release rollout track
<!-- skill-tools:body-override -->

`$ARGUMENTS` = `{UMBRELLA_SPEC} {ROW_SLUG} [OPERATION]`. First token = path to umbrella master plan. Second token = row slug from tracker (e.g. `city-sim-depth`, `zone-s-economy`, `music-player`). Optional third token = `advance` (default), `status`, or `next`. Sibling tracker path is derived: `ia/projects/{umbrella-slug}-rollout-tracker.md`.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "release-rollout"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, tracker prose + disagreements appendix entries (human-consumed cold — 2–4 sentences where required). Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `release-rollout` skill (`ia/skills/release-rollout/SKILL.md`) end-to-end on the umbrella + row given in `$ARGUMENTS`. Parse args: first token = `UMBRELLA_SPEC`, second token = `ROW_SLUG`, optional third token = `OPERATION` (`advance` default, `status`, `next`). Derive `TRACKER_SPEC` = sibling `{umbrella-slug}-rollout-tracker.md`. Resolve all paths via Read — any unreadable → STOP and report path error (route to `release-rollout-enumerate` for missing tracker).
>
> ## Phase sequence (gated)
>
> 0. Load + validate — Read `UMBRELLA_SPEC` + `TRACKER_SPEC`. Missing tracker → STOP, route to `release-rollout-enumerate {UMBRELLA_SPEC}`. Missing row → STOP, ask user pick or enumerate.
> 1. Row state read — Identify rightmost non-`✓` column for `ROW_SLUG`. Hard gates: `⚠️` → STOP + surface Disagreements appendix. (b) `❓` → STOP + equivalence pick. (g) `—`/`❓` with (e) target → Phase 3.
> 2. MCP context — Tool recipe (below). Skip on `OPERATION = status`.
> 3. Align gate check (target = (e) only) — Per new domain entity: `glossary_lookup` + `router_for_task` + `spec_section` must all return anchor. Fail → (g) `—` + skill-bug-log. Does NOT block (a)–(d) / (f).
> 4. Handoff dispatch (autonomous chain) — When (b) ✓: call Agent tool for master-plan-new → read authored plan → call Agent tool for stage-file. Chain (c)→(f) without pausing. Human pause ONLY for: (b) incomplete (product/game-design language interview, no C# internals, ≤5 q one-at-a-time) / ⚠️ / ❓ / subagent failure. Parallel-work rule enforced.
> 5. Tracker update — AFTER subagent returns success, invoke `release-rollout-track` (cell flip + ticket + Change log) + `release-rollout-skill-bug-log` (if skill bug).
> 6. Next-row recommendation — Tier-ordered pick (A → B/B' → C → D → E). Parallel-safety enforced.
>
> ## Tool recipe — Phase 2 only
>
> Skip on `OPERATION = status`.
>
> 1. `mcp__territory-ia__list_specs` — enumerate specs for align-gate reference.
> 2. `mcp__territory-ia__glossary_discover` — English tokens from ROW_SLUG scope.
> 3. `mcp__territory-ia__glossary_lookup` — high-confidence terms. Flag missing rows.
> 4. `mcp__territory-ia__router_for_task` — 1 domain matching ROW_SLUG's primary subsystem.
> 5. `mcp__territory-ia__spec_sections` — sections implied by routed domain; `max_chars` small.
> 6. `mcp__territory-ia__backlog_search` — `ROW_SLUG` search term. Capture open ids.
> 7. `mcp__territory-ia__backlog_issue` — only if specific id needs full context.
>
> ## Dispatch matrix (Phase 4)
>
> | Target cell | Subagent / command |
> |-------------|--------------------|
> | (a) reseed | `release-rollout-enumerate` helper |
> | (b) | `/design-explore docs/{slug}-exploration.md` (`--against {UMBRELLA_SPEC}` when locked) |
> | (c) NEW | `/master-plan-new docs/{slug}-exploration.md` |
> | (c) EXTEND | `/master-plan-extend ia/projects/{slug}-master-plan.md docs/{slug}-exploration.md` |
> | (d)/(e) | `/stage-decompose ia/projects/{slug}-master-plan.md Step {N}` |
> | (f) | `/stage-file ia/projects/{slug}-master-plan.md Stage {N}.{M}` |
> | (g) | Hand-author glossary row + spec section anchor. No subagent. |
>
> ## Hard boundaries
>
> - Do NOT close issues (= `/closeout`).
> - Do NOT author child master-plans directly — delegate to lifecycle subagents.
> - Do NOT touch other rows' cells.
> - Do NOT proceed on `⚠️` or `❓` markers — route to user pick.
> - Do NOT skip align gate on (e) target — failure = (g) `—` + skill-bug-log, not tick.
> - Do NOT violate parallel-work rule — emit Tier-ordered alt row instead.
> - Do NOT commit — user decides.
>
> ## Output
>
> Single concise caveman message: chain summary (`{ROW_SLUG} → (f) ✓, chain: master-plan-new → stage-file ({issue-ids})`), align gate outcome (if (e)), Tier + parallel-safety, pending disagreements count, next-row recommendation (`claude-personal "/release-rollout {UMBRELLA_SPEC} {next-row}"` OR "umbrella-complete"). Do NOT pause between (c)→(f) when (b) ✓.
