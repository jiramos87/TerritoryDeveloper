`$ARGUMENTS` = `{SLUG} {SECTION_ID}`. Both required. V2 row-only — no session_id, no base_branch, no worktree.

## Mission

Drift gate + DB closeout + claim release for a closed parallel section. Same branch, same worktree — no merge step.

## Recipe invocation

```bash
npm run recipe:run -- section-closeout \
  --input slug={SLUG} \
  --input section_id={SECTION_ID}
```

Recipe steps:

1. `drift_scan` — `arch_drift_scan(scope=intra-plan)` → `{affected_stages[]}`.
2. `drift_gate` — assert 0 affected stages. STOP on drift.
3. `closeout_apply` — DB closeout: assert stages done + change_log row + claim release by row key alone (V2 row-only).

## Hard boundaries

- IF drift found → STOP. Resolve drift first.
- IF stages not all done → STOP. Ship remaining stages first.
- Do NOT re-ship stages. Do NOT reopen claim.
- Do NOT open worktrees, branches, or merge — V2 same-branch same-worktree.
- Do NOT commit — V2 dropped the merge commit step. Stage commits already land via `/ship-stage` Pass B.
