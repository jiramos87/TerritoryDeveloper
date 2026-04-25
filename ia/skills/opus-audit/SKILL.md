---
name: opus-audit
purpose: >-
  Opus Stage-scoped bulk audit (DB-backed). Single pass reads ALL N Task §Plan Digest
  + §Verification + §Code Review from DB; writes ALL N §Audit paragraphs in one synthesis round.
audience: agent
loaded_by: "skill:opus-audit"
slices_via: stage_bundle, task_spec_section, domain-context-load
description: >-
  Opus bulk skill. Invoked once per Stage after all Tasks reach post-verify Green. Single Opus pass
  reads ALL N Task spec sections (§Plan Digest + §Verification + §Code Review) + Stage header +
  shared Stage MCP bundle (glossary / router / invariants); writes ALL N §Audit paragraphs in one
  synthesis round via `task_spec_section_write`. Does NOT write §Closeout Plan — Stage closeout runs
  inline via `stage_closeout_apply` MCP. Phase 0 guardrail: F3 sequential-dispatch (no concurrent
  Opus fan-out). Triggers: "/audit {SLUG} {STAGE_ID}", "stage audit", "opus audit bulk".
phases:
  - Sequential-dispatch guardrail + Stage preflight
  - Load Stage MCP bundle
  - Read ALL N Task specs
  - Synthesize N §Audit paragraphs
  - Persist §Audit per Task
  - Hand-off
triggers:
  - /audit
  - stage audit
  - opus audit bulk
  - run opus audit Stage
model: inherit
tools_role: custom
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# Opus-audit skill (Stage-scoped bulk, DB-backed)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Opus bulk. Invoked **once per Stage** after all Tasks in the Stage reach post-verify Green (implement + verify-loop + opus-code-review complete). Single Opus pass over the shared Stage MCP bundle produces one `§Audit` paragraph per Task. Does NOT write `§Closeout Plan` — Stage closeout runs inline via `stage_closeout_apply` MCP.

DB is sole source of truth for task spec sections. Reads via `task_spec_section`; writes via `task_spec_section_write`. No filesystem spec read/write. No tuple emission.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `SLUG` | 1st arg | Master plan slug (bare token, e.g. `blip`). |
| `STAGE_ID` | 2nd arg | Stage identifier (e.g. `7.4`). |
| `STAGE_MCP_BUNDLE` | optional | Pre-loaded `domain-context-load` payload from caller. Avoids re-query when called inside chain. |

---

## Phase 0 — Sequential-dispatch guardrail (F3) + Stage preflight

> **Guardrail (F3):** Stage-scoped bulk N→1 synthesizes Tasks sequentially within one Opus pass. Never spawn concurrent Opus invocations. One Task §Audit paragraph at a time — no parallel fan-out.

1. `mcp__territory-ia__stage_bundle({slug: "{SLUG}", stage_id: "{STAGE_ID}"})` → returns `{stage, tasks}`.
2. Filter Tasks where `status ∈ {verified, done}` (post-verify Green). Skip `archived`. Halt if any Task `status ∈ {pending, implemented}` → `STOPPED — Task {id} not yet verified`.
3. Hold `task_ids[]` for Phase 2.

---

## Phase 1 — Load Stage MCP bundle

When `STAGE_MCP_BUNDLE` provided by caller: use directly. Otherwise run [`domain-context-load`](../domain-context-load/SKILL.md):
Inputs: `keywords: ["audit", "stage", "closeout", "findings"]`, `brownfield_flag: false`, `tooling_only_flag: false`, `context_label: "opus-audit Stage {STAGE_ID}"`.

Single call — do NOT re-query glossary / router / invariants per-Task. Use returned `{glossary_anchors, router_domains, spec_sections, invariants, cache_block}` across all N Task reads.

---

## Phase 2 — Read ALL N Task specs

For each `{TASK_ID}` in `task_ids[]`:

1. `mcp__territory-ia__task_spec_section({task_id: "{TASK_ID}", section: "Plan Digest"})` → §Goal / §Acceptance / §Mechanical Steps (what was planned).
2. `mcp__territory-ia__task_spec_section({task_id: "{TASK_ID}", section: "Verification"})` → what verify-loop confirmed.
3. `mcp__territory-ia__task_spec_section({task_id: "{TASK_ID}", section: "Code Review"})` → review verdict + any caveats.
4. Hold all N payloads in memory as `task_reads[{id, plan_digest, verification, code_review}]`.

---

## Phase 3 — Synthesize N §Audit paragraphs

Single synthesis round over all N `task_reads`. For each Task, produce one paragraph:

> **§Audit** prose = "What was built" (from §Plan Digest §Goal + §Mechanical Steps) + "What the verify loop confirmed" (from §Verification) + "What review caught" (from §Code Review verdict + findings) + "What to watch" (caveats, deferred issues, glossary terms introduced). Consistent voice across all N paragraphs. No per-Task MCP re-queries.

Collect into `audit_paragraphs[{task_id, paragraph}]`.

---

## Phase 4 — Persist §Audit per Task

For each `{TASK_ID, paragraph}` in `audit_paragraphs`:

```
mcp__territory-ia__task_spec_section_write({
  task_id: "{TASK_ID}",
  section: "Audit",
  body: "{paragraph}"
})
```

DB sole persistence — no filesystem write. No tuple emission. No pair-applier dispatch.

---

## Phase 5 — Hand-off

Emit caveman summary:

```
opus-audit: Stage {STAGE_ID} — {N} §Audit paragraphs written.
Tasks audited: {task_ids[]}.
DB writes: {N} task_spec_section_write OK.
Downstream: stage_closeout_apply MCP (inline in /ship-stage Pass B) consumes §Audit.
```

Return: `{stage_id, tasks_audited[], audit_paragraphs_written: N}`.

---

## Hard boundaries

- Do NOT read or write task spec body from filesystem — DB only via `task_spec_section` / `task_spec_section_write`.
- Do NOT emit `§Plan` tuples — direct DB writes only.
- Do NOT spawn pair-tail applier — opus-audit is single-skill.
- Do NOT re-query `domain-context-load` — use `STAGE_MCP_BUNDLE` payload from caller when provided.
- Do NOT write §Closeout Plan — `stage_closeout_apply` MCP handles closeout inline in `/ship-stage` Pass B.
- Do NOT flip `task_status` — caller owns flips.
- Do NOT commit — caller emits the single stage commit.

---

## Cross-references

- [`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md) — shared Stage MCP bundle recipe.
- [`ia/skills/opus-code-review/SKILL.md`](../opus-code-review/SKILL.md) — per-Stage code review (runs before audit).
- [`ia/skills/ship-stage/SKILL.md`](../ship-stage/SKILL.md) — caller; owns closeout via `stage_closeout_apply`.
- Glossary term **Opus audit** (`ia/specs/glossary.md`).
