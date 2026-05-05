---
description: Single-skill bulk plan author. Input = lean handoff YAML frontmatter at top of `docs/explorations/{slug}.md` (emitted by `design-explore` Phase 4). One Opus xhigh pass pre-fetches glossary + router + invariants once per plan, builds a 3-section §Plan Digest body per task (§Goal + §Red-Stage Proof + §Work Items, ~30 lines), inlines anchor expansion at digest write, runs synchronous drift lint, and dispatches one `master_plan_bundle_apply(jsonb)` MCP call. No filesystem mirror — DB sole source of truth. Replaces the `stage-file` + `stage-authoring` two-step roundtrip. Triggers: "/ship-plan {SLUG}", "ship plan", "bulk-author plan from handoff yaml".
argument-hint: "{slug} [--force-model {model}]"
---

# /ship-plan — DB-backed bulk plan-authoring skill that replaces `stage-file` + `stage-authoring`. Reads lean handoff YAML frontmatter from `docs/explorations/{slug}.md`, pre-fetches glossary + router + invariants once, inlines anchor expansion at digest write, runs synchronous drift lint (anchor + glossary + retired-surface), and dispatches one atomic `master_plan_bundle_apply` Postgres tx — `ia_master_plans` + `ia_stages` + `ia_tasks` + `ia_task_specs` rows materialised in a single call.

Drive `$ARGUMENTS` via the [`ship-plan`](../agents/ship-plan.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads, handoff YAML frontmatter (verbatim). Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /ship-plan {SLUG}
- ship plan
- bulk-author plan from handoff yaml
## Dispatch

Single Agent invocation with `subagent_type: "ship-plan"` carrying `$ARGUMENTS` verbatim.

## Hard boundaries

See [`ia/skills/ship-plan/SKILL.md`](../../ia/skills/ship-plan/SKILL.md) §Hard boundaries.
