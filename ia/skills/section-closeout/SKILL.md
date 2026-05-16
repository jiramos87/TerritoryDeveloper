---
name: section-closeout
purpose: >-
  Close a parallel-carcass section: intra-plan drift gate → DB closeout + claim
  release. V2 row-only — same branch, same worktree, no merge step. Mechanical —
  no decisions. Owns the release side of the V2 mutex (D4).
audience: agent
loaded_by: "skill:section-closeout"
slices_via: master_plan_locate, spec_section
description: >-
  Use to close a parallel section after all member stages are done. Runs
  intra-plan arch_drift_scan (blocks on any open drift), calls
  section_closeout_apply (asserts all stages done + writes change_log row
  section_done + releases section + cascade-releases stage claims by row key
  alone). V2 row-only — no session_id, no git merge, no worktree teardown.
  Same branch + same worktree model. Does NOT re-ship stages. Does NOT reopen
  claim. Triggers - "/section-closeout {SLUG} {SECTION_ID}", "close section",
  "release section claim".
phases: []
triggers:
  - /section-closeout {SLUG} {SECTION_ID}
  - close section
  - release section claim
model: inherit
input_token_budget: 120000
pre_split_threshold: 100000
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
  - Do NOT re-ship stages (= /ship-stage responsibility).
  - Do NOT reopen or re-claim (= /section-claim after explicit release).
  - Do NOT open worktrees, branches, or merge — V2 same-branch same-worktree.
  - Do NOT commit during recipe — V2 dropped the merge step.
caller_agent: section-closeout
---

# Section closeout — V2 row-only release

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Mechanical tool — minimal prose.

**Recipe:** phases run as recipe [`tools/recipes/section-closeout.yaml`](../../../tools/recipes/section-closeout.yaml) — DEC-A19 Phase E recipify (parallel-carcass Wave 0 Phase 3, PR 3.2; V2 simplification dropped session_id arg + git merge + worktree teardown).

**Lifecycle:** Runs LAST per parallel-carcass §6.4 — after all `/ship-stage` runs on the section complete. Counterpart to `/section-claim` (PR 3.1).

**V2 row-only model:**

- No session_id. Section claim addressed by `(slug, section_id)`. Any caller may release.
- No git merge — same branch, same worktree. `/ship-stage` Pass B already lands stage commits on the active branch.
- No worktree teardown — V2 dropped per-section worktree.
- Drift gate + DB closeout + claim release stay; that's the entire skill.

**Related:** [`section-claim`](../section-claim/SKILL.md) · [`ship-cycle`](../ship-cycle/SKILL.md) · `section_closeout_apply` MCP.

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `SLUG` | Caller | Master-plan slug. Required. |
| `SECTION_ID` | Caller | Section id. Required. |
| `ACTOR` | Optional | For change_log row. |
| `COMMIT_SHA` | Optional | For change_log row (last `/ship-stage` Pass B commit on this section). |

V2 dropped: `SESSION_ID`, `BASE_BRANCH`, `WORKTREE_ROOT`.

---

## Invocation

```bash
npm run recipe:run -- section-closeout \
  --input slug={SLUG} \
  --input section_id={SECTION_ID}
```

Optional overrides:

```bash
  --input actor={ACTOR}
  --input commit_sha={SHA}
```

Recipe steps (`tools/recipes/section-closeout.yaml`):

1. **`drift_scan`** — `arch_drift_scan(scope=intra-plan, plan_id={SLUG}, section_id={SECTION_ID})`. Returns `{affected_stages[]}`.
2. **`drift_gate`** — bash assert: `affected_stages.length === 0`. STOP on any drift.
3. **`closeout_apply`** — `section_closeout_apply` MCP. Asserts all section stages `status=done`; appends `ia_master_plan_change_log` row `kind=section_done`; releases section claim + cascade-releases stage claims by row key alone (V2 row-only).

---

## Guards

- Drift found → recipe stops at `drift_gate` (exit 1). Fix drift events, re-run `/arch-drift-scan` out-of-band, retry.
- Stages not all done → `section_closeout_apply` returns `{applied:false, error:"stages_not_done"}`. Ship remaining stages first.
- Same-section re-run after partial failure: idempotent at DB level — drift scan + closeout assertions re-run cleanly.

---

## Guardrails

- IF drift_scan.affected_stages.length > 0 → recipe stops. Resolve drift first.
- IF any section stage not done → recipe stops at closeout_apply. Ship stages first.
- Do NOT re-ship stages from this skill — `/ship-stage` owns that.
- Do NOT reopen claim — re-run `/section-claim` to start fresh parallel work.
- Do NOT open worktrees, branches, or merge — V2 dropped all three.
- Do NOT commit — V2 dropped the merge commit step. Stage commits already land via `/ship-stage` Pass B on the active branch.

---

## Seed prompt

```markdown
Run section-closeout for `{SLUG}` section `{SECTION_ID}` (V2 row-only).

Invoke recipe:
  npm run recipe:run -- section-closeout \
    --input slug={SLUG} \
    --input section_id={SECTION_ID}

Recipe: drift_scan → drift_gate → closeout_apply.
STOP on drift found. STOP on stages not done.
Do NOT re-ship stages. Do NOT reopen claim. Do NOT merge or remove worktree —
V2 same-branch same-worktree model.
```

---

## Changelog

| Date | Change | Trigger |
|------|--------|---------|
| 2026-04-29 | NEW skill — parallel-carcass Wave 0 Phase 3 PR 3.2 (DEC-A19 Phase E recipify). 4 steps: drift_gate + section_closeout_apply + git_merge + worktree_remove. Counterpart to section-claim (PR 3.1). | `docs/parallel-carcass-exploration.md` §7 PR 3.2 |
| 2026-04-29 | V2 rewrite — dropped session_id arg, dropped git_merge step, dropped worktree teardown. Same branch + same worktree model. Recipe is now drift_scan + drift_gate + closeout_apply (3 steps). Section claim released by row key alone. | parallel-carcass V2 rewrite (no worktree / no branch / no holder-token) |

### 2026-04-29 — skill-train run

**source:** train-proposed

**proposal:** `ia/skills/section-closeout/proposed/2026-04-29-train.md`

**friction_count:** 0

**threshold:** 2

---
