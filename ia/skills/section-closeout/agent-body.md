# Mission

Close parallel section `{SLUG}` section `{SECTION_ID}` (V2 row-only). Mechanical: drift gate → DB closeout + claim release. Same branch, same worktree — no merge step.

# Recipe

Mechanical phases run as recipe `section-closeout` (`tools/recipes/section-closeout.yaml`) — DEC-A19 Phase E recipify, parallel-carcass Wave 0 Phase 3 PR 3.2, V2 rewrite.

```bash
npm run recipe:run -- section-closeout \
  --input slug={SLUG} \
  --input section_id={SECTION_ID}
```

Optional `--input actor={ACTOR}` + `--input commit_sha={SHA}` for change_log row.

Recipe stops on first failure:

1. `drift_scan` — `arch_drift_scan(scope=intra-plan)`. Returns `{affected_stages[]}`.
2. `drift_gate` — bash assert: 0 affected stages. STOP when any drift found.
3. `closeout_apply` — `section_closeout_apply` MCP. STOP when stages not all done. Releases section + stage claims by row key alone (V2 row-only).

# Inputs

| Var | Notes |
|-----|-------|
| `SLUG` | Master-plan slug. Required. |
| `SECTION_ID` | Section id. Required. |
| `ACTOR` | Optional. For change_log row. |
| `COMMIT_SHA` | Optional. For change_log row. |

V2 dropped: `SESSION_ID`, `BASE_BRANCH`, `WORKTREE_ROOT`.

# Hard boundaries

- IF drift found → STOP. Resolve drift, re-run `/arch-drift-scan`, retry.
- IF any section stage not done → STOP. Ship remaining stages first.
- Do NOT re-ship stages (= `/ship-stage`).
- Do NOT reopen claim (= `/section-claim` from scratch).
- Do NOT open worktrees, branches, or merge — V2 same-branch same-worktree.
- Do NOT commit — V2 dropped the merge commit step.
