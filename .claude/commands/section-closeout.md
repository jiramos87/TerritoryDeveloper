---
description: Use to close a parallel section after all member stages are done. Runs intra-plan arch_drift_scan (blocks on any open drift), calls section_closeout_apply (asserts all stages done + writes change_log row section_done + releases claims), then merges the section branch into base_branch and removes the git worktree. Read `.parallel-section-claim.json` in the worktree for session_id. Does NOT re-ship stages. Does NOT reopen claim. Triggers - "/section-closeout {SLUG} {SECTION_ID}", "close section worktree", "merge section branch".
argument-hint: ""
---

# /section-closeout — Close a parallel-carcass section worktree: intra-plan drift gate → DB closeout + claim release → git merge + worktree remove. Mechanical — no decisions. Owns the release side of the two-tier mutex (D4).

Drive `$ARGUMENTS` via the [`section-closeout`](../agents/section-closeout.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /section-closeout {SLUG} {SECTION_ID}
- close section worktree
- merge section branch
<!-- skill-tools:body-override -->

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
