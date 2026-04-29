# Mission

Take V2 row-only section claim for `{SLUG}` section `{SECTION_ID}`. Pure DB mutex on `(slug, section_id)`. Same branch, same worktree — no git worktree, no per-section branch. Heartbeats happen externally via `/ship-stage`.

# Recipe

Mechanical phase (claim) runs as recipe `section-claim` (`tools/recipes/section-claim.yaml`) — DEC-A19 Phase E recipify, parallel-carcass Wave 0 Phase 3 PR 3.1, V2 rewrite. Invoke:

```
npm run recipe:run -- section-claim \
  --input slug={SLUG} \
  --input section_id={SECTION_ID}
```

Recipe stops on first failure:

1. `claim` — STOPs `section_claim_held` only on concurrent INSERT race. Subsequent caller refreshes heartbeat (V2 row-only — section IS the holder).

# Inputs

| Var | Notes |
|-----|-------|
| `SLUG` | Master-plan slug. Required. |
| `SECTION_ID` | Section id (matches `ia_stages.section_id`). Required. |

V2 dropped: `SESSION_ID`, `WORKTREE_ROOT`, `BASE_BRANCH`. Section is the holder.

# Hard boundaries

- IF concurrent INSERT race → `section_claim_held`. Retry — second call refreshes heartbeat.
- Do NOT open git worktrees or branches — V2 same-branch same-worktree model.
- Do NOT write `.parallel-section-claim.json` — V2 dropped sentinel.
- Do NOT run `/ship-stage` from this skill (caller invokes after recipe returns).
- Do NOT close the section (`/section-closeout` owns drift gate + DB closeout).
- Do NOT commit.

# Next step

```
/ship-stage {SLUG} {SECTION_ID}.1
```

`/ship-stage` Pass A refreshes claim heartbeat per stage via `claim_heartbeat({slug, stage_id})` MCP.
