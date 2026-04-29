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
