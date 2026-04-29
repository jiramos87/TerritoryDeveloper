---
name: section-claim
purpose: >-
  Open a parallel-carcass section worktree + take the two-tier section claim (D4).
  Mechanical: git worktree add + mcp.section_claim + sentinel write. No decisions.
audience: agent
loaded_by: "skill:section-claim"
slices_via: master_plan_locate, spec_section
description: >-
  Use to start parallel work on one master-plan section. Opens a git worktree at
  `../territory-developer.section-{section_id}` on branch `feature/{slug}-section-{section_id}`,
  takes the section claim row in `ia_section_claims`, and writes a `.parallel-section-claim.json`
  sentinel inside the worktree so downstream `/ship-stage` + `/section-closeout` calls can read
  the same `session_id`. Heartbeats happen externally — `/ship-stage` Pass A iterations call
  `claim_heartbeat` MCP. Does NOT close the section (= `/section-closeout`). Does NOT run any
  ship-stage work. Triggers - "/section-claim {SLUG} {SECTION_ID}", "claim section worktree".
phases: []
triggers:
  - /section-claim {SLUG} {SECTION_ID}
  - claim section worktree
  - open parallel section
model: inherit
tools_role: planner
tools_extra:
  - mcp__territory-ia__section_claim
  - mcp__territory-ia__master_plan_locate
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries:
  - IF section already claimed by another session → STOP. MCP returns `section_claim_held`.
  - IF worktree path exists on a different branch → STOP. Resolve manually before retry.
  - Do NOT run /ship-stage from this skill (= caller responsibility).
  - Do NOT close the section (= /section-closeout).
  - Do NOT commit.
caller_agent: section-claim
---

# Section claim — open parallel-carcass worktree

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Mechanical tool — minimal prose.

**Recipe:** mechanical phases run as recipe [`tools/recipes/section-claim.yaml`](../../../tools/recipes/section-claim.yaml) — DEC-A19 Phase E recipify (parallel-carcass Wave 0 Phase 3, PR 3.1). Skill body documents inputs + caller responsibilities + boundaries; phase logic lives in yaml + bash helpers under `tools/scripts/recipe-engine/section-claim/`.

**Lifecycle:** Runs FIRST per parallel-carcass §6.4 — before any `/ship-stage` call on a section. Each section gets its own worktree + claim. `/section-closeout` (PR 3.2) releases at the end.

**Dispatch mode:** Inline (SKILL.md-only) or via Agent subagent (Sonnet pin). No multi-turn LLM reasoning — pure mechanical mutex + worktree open.

**Related:** [`section-closeout`](../section-closeout/SKILL.md) (release + merge) · [`ship-stage`](../ship-stage/SKILL.md) (consumes `session_id` sentinel).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `SLUG` | Caller | Master-plan slug. Required. Must exist in `ia_master_plans`. |
| `SECTION_ID` | Caller | Section id. Required. Must match `ia_stages.section_id` for ≥1 member stage. |
| `SESSION_ID` | Caller | Stable id reused across `/ship-stage` + `/section-closeout` in the worktree. Convention - `section-claim-{SLUG}-{SECTION_ID}-{ISO8601_compact}`. Required. |
| `WORKTREE_ROOT` | Caller | Optional override. Default = `{repo_parent}/{repo_name}.section-{SECTION_ID}`. |
| `BASE_BRANCH` | Caller | Optional fork point. Default = current HEAD of repo. |

---

## Invocation

```bash
npm run recipe:run -- section-claim \
  --input slug={SLUG} \
  --input section_id={SECTION_ID} \
  --input session_id={SESSION_ID}
```

Optional overrides:

```bash
  --input worktree_root={ABS_PATH}
  --input base_branch={REF}
```

Recipe steps (`tools/recipes/section-claim.yaml`):

1. **`open_worktree`** — `git worktree add` at computed path on branch `feature/{SLUG}-section-{SECTION_ID}`. Idempotent: noop when path on expected branch; STOP when path on different branch.
2. **`claim`** — `section_claim` MCP. Inserts row in `ia_section_claims`. Returns `status: claimed | renewed`. Throws `section_claim_held` when held by another session.
3. **`write_sentinel`** — write `.parallel-section-claim.json` `{slug, section_id, session_id}` inside the worktree. Idempotent on identical content.

Outputs (`outputs.worktree`, `outputs.claim_status`, `outputs.claimed_at`, `outputs.last_heartbeat`, `outputs.sentinel`) consumed by caller for next-step messaging.

---

## Heartbeats

The recipe is **one-shot**. Heartbeats happen externally:

- `/ship-stage` Pass A iterations call `claim_heartbeat` MCP per stage.
- Background sweep (`claims_sweep` MCP) releases stale rows past `carcass_config.claim_heartbeat_timeout_minutes`.

A long-idle session that never re-enters `/ship-stage` will have its claim swept; re-running `section-claim` re-acquires (status=`claimed`).

---

## Guardrails

- IF section already claimed by another session → recipe step `claim` raises `section_claim_held`. Do not force.
- IF worktree path exists on a different branch → step `open_worktree` STOPs (exit 1). Resolve naming clash manually.
- Same-session re-invocation = idempotent: worktree noop + claim heartbeat refresh + sentinel noop.
- Do NOT run `/ship-stage` from this skill — caller invokes it after recipe returns.
- Do NOT close the section — `/section-closeout` (PR 3.2) handles drift gate + DB closeout + git merge + release.
- Do NOT commit.

---

## Seed prompt

```markdown
Run section-claim to open a parallel section worktree.

Inputs:
  SLUG: {plan slug}
  SECTION_ID: {section id}
  SESSION_ID: section-claim-{SLUG}-{SECTION_ID}-{ISO8601_compact}

Mechanical phases (worktree open, claim, sentinel) wrapped by recipe `section-claim`:

  npm run recipe:run -- section-claim \
    --input slug={SLUG} \
    --input section_id={SECTION_ID} \
    --input session_id={SESSION_ID}

Recipe stops on first failure (worktree clash / claim held / sentinel write).
All three steps are idempotent on same-session re-run.

Heartbeats are external — /ship-stage Pass A drives them.

Do NOT run /ship-stage. Do NOT close the section. Do NOT commit.
```

---

## Next step

After claim returns `outputs.claim_status ∈ {claimed, renewed}`:

```bash
cd {worktree_root}
/ship-stage {SLUG} {SECTION_ID}.1
```

Sentinel `.parallel-section-claim.json` carries `session_id` for `/ship-stage` Pass A `stage_claim` + `claim_heartbeat`.

---

## Changelog

| Date | Change | Trigger |
|------|--------|---------|
| 2026-04-29 | NEW skill — parallel-carcass Wave 0 Phase 3 PR 3.1 (DEC-A19 Phase E recipify). 0 seams: `git worktree add` + `mcp.section_claim` + sentinel write. Heartbeats external. | `docs/parallel-carcass-exploration.md` §7 PR 3.1 |
