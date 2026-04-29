`$ARGUMENTS` = `{SLUG} {SECTION_ID}`. Both required. Caller derives `SESSION_ID` (convention: `section-claim-{SLUG}-{SECTION_ID}-{ISO8601_compact}`). Optional `--worktree-root` + `--base-branch` overrides.

## Mission

Open parallel section worktree + take the section claim (D4) so the caller can drive `/ship-stage` runs from a dedicated branch without contention.

## Recipe invocation

```bash
SESSION_ID="section-claim-{SLUG}-{SECTION_ID}-$(date -u +%Y%m%dT%H%M%SZ)"
npm run recipe:run -- section-claim \
  --input slug={SLUG} \
  --input section_id={SECTION_ID} \
  --input session_id="$SESSION_ID"
```

Recipe steps:

1. `open_worktree` — `git worktree add` at `{repo_parent}/{repo_name}.section-{SECTION_ID}` on branch `feature/{SLUG}-section-{SECTION_ID}`. Idempotent same-branch re-entry.
2. `claim` — `mcp.section_claim`. Throws `section_claim_held` when held by another session.
3. `write_sentinel` — write `.parallel-section-claim.json` `{slug, section_id, session_id}` inside worktree. Downstream `/ship-stage` + `/section-closeout` read it.

## Hard boundaries

- IF section claimed by another session → STOP. MCP raises `section_claim_held`.
- IF worktree path exists on different branch → STOP. Resolve clash manually.
- Do NOT run `/ship-stage` from this command — caller invokes after recipe returns.
- Do NOT close the section (`/section-closeout` owns release).
- Do NOT commit.

## Next step

```
cd {repo_parent}/{repo_name}.section-{SECTION_ID}
/ship-stage {SLUG} {SECTION_ID}.1
```
