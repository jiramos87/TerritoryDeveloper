---
description: Use to start parallel work on one master-plan section. Inserts (or refreshes) the row in `ia_section_claims` keyed by `(slug, section_id)`. V2 row-only — no holder identity, no worktree, no new branch. Concurrent INSERT race throws `section_claim_held`; any subsequent caller refreshes the open row. Heartbeats happen externally — `/ship-stage` Pass A iterations call `claim_heartbeat` MCP. Background sweep (`claims_sweep` MCP) releases stale rows past `carcass_config.claim_heartbeat_timeout_minutes`. Does NOT close the section (= `/section-closeout`). Does NOT run any ship-stage work. Triggers - "/section-claim {SLUG} {SECTION_ID}", "claim section row".
argument-hint: ""
---

# /section-claim — Take the V2 row-only section claim row in `ia_section_claims`. Same branch, same worktree — no git worktree, no per-section branch. Pure DB mutex on `(slug, section_id)`. Mechanical — no decisions.

Drive `$ARGUMENTS` via the [`section-claim`](../agents/section-claim.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /section-claim {SLUG} {SECTION_ID}
- claim section row
- take section claim
<!-- skill-tools:body-override -->

`$ARGUMENTS` = `{SLUG} {SECTION_ID}`. Both required. V2 row-only — no session_id, no worktree, no branch.

## Mission

Take the V2 row-only section claim (D4 V2) so the caller can drive `/ship-stage` runs without cross-section contention. Same branch + same worktree — N parallel agents OR 1 sequential agent across turns.

## Recipe invocation

```bash
npm run recipe:run -- section-claim \
  --input slug={SLUG} \
  --input section_id={SECTION_ID}
```

Recipe steps:

1. `claim` — `mcp.section_claim`. Throws `section_claim_held` only on concurrent INSERT race. Any subsequent caller refreshes heartbeat (section IS the holder).

## Hard boundaries

- IF concurrent INSERT race → `section_claim_held`. Retry — second call refreshes heartbeat.
- Do NOT open git worktrees or branches — V2 same-branch same-worktree model.
- Do NOT write `.parallel-section-claim.json` sentinel — V2 dropped.
- Do NOT run `/ship-stage` from this command — caller invokes after recipe returns.
- Do NOT close the section (`/section-closeout` owns release).
- Do NOT commit.

## Next step

```
/ship-stage {SLUG} {SECTION_ID}.1
```

`/ship-stage` Pass A refreshes claim heartbeat per stage via `claim_heartbeat({slug, stage_id})` MCP.
