---
name: section-closeout
purpose: >-
  Close a parallel-carcass section worktree: intra-plan drift gate →
  DB closeout + claim release → git merge + worktree remove. Mechanical — no
  decisions. Owns the release side of the two-tier mutex (D4).
audience: agent
loaded_by: "skill:section-closeout"
slices_via: master_plan_locate, spec_section
description: >-
  Use to close a parallel section after all member stages are done. Runs
  intra-plan arch_drift_scan (blocks on any open drift), calls
  section_closeout_apply (asserts all stages done + writes change_log row
  section_done + releases claims), then merges the section branch into
  base_branch and removes the git worktree. Read `.parallel-section-claim.json`
  in the worktree for session_id. Does NOT re-ship stages. Does NOT reopen
  claim. Triggers - "/section-closeout {SLUG} {SECTION_ID}", "close section
  worktree", "merge section branch".
phases: []
triggers:
  - /section-closeout {SLUG} {SECTION_ID}
  - close section worktree
  - merge section branch
model: inherit
tools_role: planner
tools_extra:
  - mcp__territory-ia__arch_drift_scan
  - mcp__territory-ia__section_closeout_apply
  - mcp__territory-ia__master_plan_locate
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries:
  - IF drift_scan.affected_stages.length > 0 → STOP. Resolve drift first.
  - IF any section stage not done → STOP. section_closeout_apply returns error=stages_not_done.
  - IF main worktree not on base_branch → git-merge-section.sh exits 1.
  - Do NOT re-ship stages (= /ship-stage responsibility).
  - Do NOT reopen or re-claim (= /section-claim after explicit release).
  - Do NOT commit during recipe (recipe commits via git merge --no-ff).
caller_agent: section-closeout
---

# Section closeout — close parallel-carcass section

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Mechanical tool — minimal prose.

**Recipe:** phases run as recipe [`tools/recipes/section-closeout.yaml`](../../../tools/recipes/section-closeout.yaml) — DEC-A19 Phase E recipify (parallel-carcass Wave 0 Phase 3, PR 3.2). Phase logic lives in yaml + bash helpers under `tools/scripts/recipe-engine/section-closeout/`.

**Lifecycle:** Runs LAST per parallel-carcass §6.4 — after all `/ship-stage` runs on the section complete. Counterpart to `/section-claim` (PR 3.1).

**Read sentinel first:** `.parallel-section-claim.json` in the worktree carries `{slug, section_id, session_id}` needed for claim release.

**Related:** [`section-claim`](../section-claim/SKILL.md) · [`ship-stage`](../ship-stage/SKILL.md) · `section_closeout_apply` MCP.

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `SLUG` | Caller | Master-plan slug. Required. |
| `SECTION_ID` | Caller | Section id. Required. |
| `SESSION_ID` | Sentinel | Read from `{worktree}/.parallel-section-claim.json`. Required. |
| `BASE_BRANCH` | Caller | Branch to merge section branch into. Must be current branch in main worktree. Required. |
| `ACTOR` | Optional | For change_log row. |
| `COMMIT_SHA` | Optional | For change_log row. |
| `WORKTREE_ROOT` | Optional | Default = `{repo_parent}/{repo_name}.section-{SECTION_ID}`. |

---

## Invocation

```bash
SESSION_ID=$(cat {worktree}/.parallel-section-claim.json | jq -r '.session_id')
npm run recipe:run -- section-closeout \
  --input slug={SLUG} \
  --input section_id={SECTION_ID} \
  --input session_id="$SESSION_ID" \
  --input base_branch={BASE_BRANCH}
```

Optional overrides:

```bash
  --input actor={ACTOR}
  --input commit_sha={SHA}
  --input worktree_root={ABS_PATH}
```

Recipe steps (`tools/recipes/section-closeout.yaml`):

1. **`drift_scan`** — `arch_drift_scan(scope=intra-plan, plan_id={SLUG}, section_id={SECTION_ID})`. Returns `{affected_stages[]}`.
2. **`drift_gate`** — bash assert: `affected_stages.length === 0`. STOP on any drift.
3. **`closeout_apply`** — `section_closeout_apply` MCP. Asserts all section stages `status=done`; appends `ia_master_plan_change_log` row `kind=section_done`; releases section + stage claims.
4. **`git_merge`** — merge `feature/{SLUG}-section-{SECTION_ID}` into `{BASE_BRANCH}` with `--no-ff` commit; `git worktree remove --force`.

---

## Guards

- Drift found → recipe stops at `drift_gate` (exit 1). Fix drift events, re-run `/arch-drift-scan` out-of-band, retry.
- Stages not all done → `section_closeout_apply` returns `{applied:false, error:"stages_not_done"}`. Ship remaining stages first.
- Main worktree on wrong branch → `git-merge-section.sh` exits 1. Run `git checkout {BASE_BRANCH}` in main repo and retry.
- Same-session re-run after partial failure: idempotent at DB level; git merge step may fail on already-merged branch (noop manually).

---

## Guardrails

- IF drift_scan.affected_stages.length > 0 → recipe stops. Resolve drift first.
- IF any section stage not done → recipe stops at closeout_apply. Ship stages first.
- IF main worktree not on base_branch → recipe stops. `git checkout {BASE_BRANCH}`.
- Do NOT re-ship stages from this skill — `/ship-stage` owns that.
- Do NOT reopen claim — re-run `/section-claim` to start fresh parallel work.

---

## Seed prompt

```markdown
Run section-closeout for `{SLUG}` section `{SECTION_ID}`.

Read session_id from sentinel:
  SESSION_ID=$(cat {worktree}/.parallel-section-claim.json | jq -r '.session_id')

Invoke recipe:
  npm run recipe:run -- section-closeout \
    --input slug={SLUG} \
    --input section_id={SECTION_ID} \
    --input session_id="$SESSION_ID" \
    --input base_branch={BASE_BRANCH}

Recipe: drift_gate → closeout_apply → git_merge + worktree_remove.
STOP on drift found. STOP on stages not done. STOP on wrong base_branch.
Do NOT re-ship stages. Do NOT reopen claim. Recipe commit = git merge --no-ff.
```

---

## Changelog

| Date | Change | Trigger |
|------|--------|---------|
| 2026-04-29 | NEW skill — parallel-carcass Wave 0 Phase 3 PR 3.2 (DEC-A19 Phase E recipify). 4 steps: drift_gate + section_closeout_apply + git_merge + worktree_remove. Counterpart to section-claim (PR 3.1). | `docs/parallel-carcass-exploration.md` §7 PR 3.2 |
