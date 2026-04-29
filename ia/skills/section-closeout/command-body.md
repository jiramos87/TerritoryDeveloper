`$ARGUMENTS` = `{SLUG} {SECTION_ID}`. Both required. Read `SESSION_ID` from `.parallel-section-claim.json` sentinel in the section worktree. Provide `BASE_BRANCH` = current main worktree branch.

## Mission

Drift gate + DB closeout + claim release + git merge + worktree remove for a closed parallel section.

## Recipe invocation

```bash
WORKTREE="${REPO_PARENT}/${REPO_NAME}.section-${SECTION_ID}"
SESSION_ID=$(cat "${WORKTREE}/.parallel-section-claim.json" | jq -r '.session_id')
npm run recipe:run -- section-closeout \
  --input slug={SLUG} \
  --input section_id={SECTION_ID} \
  --input session_id="$SESSION_ID" \
  --input base_branch=$(git rev-parse --abbrev-ref HEAD)
```

Recipe steps:

1. `drift_scan` — `arch_drift_scan(scope=intra-plan)` → `{affected_stages[]}`.
2. `drift_gate` — assert 0 affected stages. STOP on drift.
3. `closeout_apply` — DB closeout: assert stages done + change_log row + claim release.
4. `git_merge` — merge section branch → base_branch + `git worktree remove`.

## Hard boundaries

- IF drift found → STOP. Resolve drift first.
- IF stages not all done → STOP. Ship remaining stages first.
- IF main worktree not on base_branch → STOP. Checkout base_branch + retry.
- Do NOT re-ship stages. Do NOT reopen claim. Recipe handles the git commit via `--no-ff` merge.
