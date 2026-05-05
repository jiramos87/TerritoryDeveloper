---
name: section-claim
purpose: >-
  Take the V2 row-only section claim row in `ia_section_claims`. Same branch,
  same worktree — no git worktree, no per-section branch. Pure DB mutex on
  `(slug, section_id)`. Mechanical — no decisions.
audience: agent
loaded_by: "skill:section-claim"
slices_via: master_plan_locate, spec_section
description: >-
  Use to start parallel work on one master-plan section. Inserts (or refreshes)
  the row in `ia_section_claims` keyed by `(slug, section_id)`. V2 row-only —
  no holder identity, no worktree, no new branch. Concurrent INSERT race throws
  `section_claim_held`; any subsequent caller refreshes the open row.
  Heartbeats happen externally — `/ship-stage` Pass A iterations call
  `claim_heartbeat` MCP. Background sweep (`claims_sweep` MCP) releases stale
  rows past `carcass_config.claim_heartbeat_timeout_minutes`. Does NOT close
  the section (= `/section-closeout`). Does NOT run any ship-stage work.
  Triggers - "/section-claim {SLUG} {SECTION_ID}", "claim section row".
phases: []
triggers:
  - /section-claim {SLUG} {SECTION_ID}
  - claim section row
  - take section claim
model: inherit
input_token_budget: 120000
pre_split_threshold: 100000
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
  - IF concurrent INSERT race → MCP throws `section_claim_held`. Retry idempotent.
  - Do NOT open git worktrees or branches — same branch, same worktree model.
  - Do NOT run /ship-stage from this skill (= caller responsibility).
  - Do NOT close the section (= /section-closeout).
  - Do NOT commit.
caller_agent: section-claim
---

# Section claim — V2 row-only mutex

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Mechanical tool — minimal prose.

**Recipe:** mechanical phase runs as recipe [`tools/recipes/section-claim.yaml`](../../../tools/recipes/section-claim.yaml) — DEC-A19 Phase E recipify (parallel-carcass Wave 0 Phase 3, PR 3.1; V2 simplification dropped worktree + sentinel steps).

**Lifecycle:** Runs FIRST per parallel-carcass §6.4 — before any `/ship-stage` call on a section. Each section gets its own row in `ia_section_claims`. `/section-closeout` (PR 3.2) releases at the end. **No git worktree, no per-section branch** — N parallel agents OR 1 sequential agent across turns operate on the same checkout. Coordination is pure DB mutex.

**V2 row-only design** (parallel-carcass §6.2 V2 rewrite):

- Section IS the holder. Row key `(slug, section_id)` is identity.
- Concurrent INSERT race → `section_claim_held` (PRIMARY KEY conflict). Any subsequent caller refreshes the heartbeat on the open row.
- Multi-sequential agents on the same section trivially supported — both address the same row.
- Stale rows cleared by time-based `claims_sweep` (no holder-token check).
- Threat model: same-machine same-user. No adversarial parties to authenticate.

**Dispatch mode:** Inline (SKILL.md-only) or via Agent subagent (Sonnet pin). No multi-turn LLM reasoning — pure mechanical DB mutex.

**Related:** [`section-closeout`](../section-closeout/SKILL.md) (release) · [`ship-stage`](../ship-stage/SKILL.md) (heartbeats) · `section_claim` MCP · `claim_heartbeat` MCP.

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `SLUG` | Caller | Master-plan slug. Required. Must exist in `ia_master_plans`. |
| `SECTION_ID` | Caller | Section id. Required. Must match `ia_stages.section_id` for ≥1 member stage. |

No `SESSION_ID`, no `WORKTREE_ROOT`, no `BASE_BRANCH` — V2 dropped all three.

---

## Invocation

```bash
npm run recipe:run -- section-claim \
  --input slug={SLUG} \
  --input section_id={SECTION_ID}
```

Recipe steps (`tools/recipes/section-claim.yaml`):

1. **`claim`** — `section_claim` MCP. Inserts row in `ia_section_claims`. Returns `status: claimed | renewed`. Throws `section_claim_held` only on concurrent INSERT race.

Outputs (`outputs.claim_status`, `outputs.claimed_at`, `outputs.last_heartbeat`) consumed by caller for next-step messaging.

---

## Heartbeats

The recipe is **one-shot**. Heartbeats happen externally:

- `/ship-stage` Pass A iterations call `claim_heartbeat({slug, stage_id})` MCP per stage — refreshes stage claim + parent section claim.
- Background sweep (`claims_sweep` MCP) releases stale rows past `carcass_config.claim_heartbeat_timeout_minutes`.

A long-idle session that never re-enters `/ship-stage` will have its claim swept; re-running `section-claim` re-acquires (status=`claimed`).

---

## Guardrails

- IF concurrent INSERT race → recipe step `claim` raises `section_claim_held`. Retry — second call refreshes heartbeat.
- Do NOT open git worktrees or branches — V2 dropped this. Same branch, same worktree.
- Do NOT write `.parallel-section-claim.json` sentinel — V2 dropped this.
- Do NOT run `/ship-stage` from this skill — caller invokes it after recipe returns.
- Do NOT close the section — `/section-closeout` (PR 3.2) handles drift gate + DB closeout.
- Do NOT commit.

---

## Seed prompt

```markdown
Run section-claim to take the V2 row-only section claim.

Inputs:
  SLUG: {plan slug}
  SECTION_ID: {section id}

Mechanical phase wrapped by recipe `section-claim`:

  npm run recipe:run -- section-claim \
    --input slug={SLUG} \
    --input section_id={SECTION_ID}

Recipe stops on concurrent INSERT race (`section_claim_held`). Retry — second
call refreshes heartbeat. Same branch, same worktree — no worktree open,
no branch creation.

Heartbeats are external — /ship-stage Pass A drives them via
claim_heartbeat({slug, stage_id}) MCP.

Do NOT run /ship-stage. Do NOT close the section. Do NOT commit.
```

---

## Next step

After claim returns `outputs.claim_status ∈ {claimed, renewed}`:

```bash
/ship-stage {SLUG} {SECTION_ID}.1
```

Heartbeats refreshed automatically by `/ship-stage` Pass A loop via `claim_heartbeat({slug, stage_id})`.

---

## Changelog

| Date | Change | Trigger |
|------|--------|---------|
| 2026-04-29 | NEW skill — parallel-carcass Wave 0 Phase 3 PR 3.1 (DEC-A19 Phase E recipify). 0 seams: `git worktree add` + `mcp.section_claim` + sentinel write. Heartbeats external. | `docs/parallel-carcass-exploration.md` §7 PR 3.1 |
| 2026-04-29 | V2 rewrite — dropped session_id arg, dropped git worktree step, dropped sentinel write step. Same branch + same worktree model. Section IS the holder, row key is identity. Multi-sequential agents on same section trivially supported. | parallel-carcass V2 rewrite (no worktree / no branch / no holder-token) |

### 2026-04-29 — skill-train run

**source:** train-proposed

**proposal:** `ia/skills/section-claim/proposed/2026-04-29-train.md`

**friction_count:** 0

**threshold:** 2

---
