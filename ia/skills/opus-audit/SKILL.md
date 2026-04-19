---
purpose: "Opus Stage-scoped bulk audit: one pass reads ALL N Task specs + Stage header; writes ALL N §Audit paragraphs in one synthesis round; feeds stage-closeout-plan."
audience: agent
loaded_by: skill:opus-audit
slices_via: none
name: opus-audit
description: >
  Opus bulk skill. Invoked once per Stage after all Tasks reach post-verify Green.
  Single pass reads ALL N Task specs (§Implementation + §Findings + §Verification) +
  Stage header + invariants + glossary snippets (pre-loaded shared Stage MCP bundle);
  writes ALL N §Audit paragraphs in one synthesis round.
  Does NOT write §Closeout Plan — that is Stage-level work (T7.13 stage-closeout-plan).
  Phase 0 guardrail: assert every Task in Stage has non-empty §Findings (R11); escalate
  to user if any Task missing.
  Triggers: "/audit {MASTER_PLAN_PATH} {STAGE_ID}", "stage audit", "opus audit bulk",
  "run opus audit Stage".
phases:
  - "§Findings gate"
  - "Load Stage MCP bundle"
  - "Synthesize N §Audit paragraphs"
  - "Write tuples"
  - "Hand-off"
---

# Opus-audit skill (Stage-scoped bulk)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Opus bulk. Invoked **once per Stage** after all Tasks in the Stage reach post-verify Green (implement + verify-loop + opus-code-review + any code-fix loops complete). Single Opus pass over the shared Stage MCP bundle produces one `§Audit` paragraph per Task. Does NOT write `§Closeout Plan` — that belongs to `stage-closeout-plan` (T7.13 / TECH-480).

Contract: [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, §Validation gate, §Escalation rule.
This skill is **not a pair-head** — emits `§Audit` section content directly via tuples (no downstream Sonnet applier). Tuples use `replace_section` / `insert_after` operations directly against each `ia/projects/{id}.md`.
Downstream consumer: [`stage-closeout-plan/SKILL.md`](../stage-closeout-plan/SKILL.md) reads the N `§Audit` paragraphs as raw material.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `MASTER_PLAN_PATH` | 1st arg | Repo-relative path to master plan (e.g. `ia/projects/lifecycle-refactor-master-plan.md`). |
| `STAGE_ID` | 2nd arg | Stage identifier (e.g. `7.4`). |

---

## Phase 0 — §Findings gate (invariant R11)

> **Guardrail:** Every Task in the target Stage MUST have a non-empty `§Findings` section in its spec before this skill proceeds. Missing or empty `§Findings` = escalate immediately.

1. Read `MASTER_PLAN_PATH` Stage `STAGE_ID` Tasks table.
2. For each Task row with Status ≠ `Done (archived)`: open `ia/projects/{ISSUE_ID}.md`; search for `## §Findings` heading with non-empty content below.
3. Collect any Task ids where `§Findings` is absent or empty into `missing_findings[]`.
4. If `missing_findings` is non-empty → STOP. Emit to user:

```
opus-audit: BLOCKED — §Findings gate failed.
Tasks missing §Findings: {missing_findings}.
Verify-loop must complete and write §Findings before /audit runs.
```

5. All Tasks pass gate → proceed to Phase 1.

---

## Phase 1 — Load Stage MCP bundle

Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)).
Inputs: `keywords: ["audit", "stage", "lifecycle", "closeout", "findings"]`, `brownfield_flag: false`, `tooling_only_flag: false`, `context_label: "opus-audit Stage {STAGE_ID}"`.

Single call — do NOT re-query glossary / router / invariants per-Task. Use returned `{glossary_anchors, router_domains, spec_sections, invariants}` across all N Task reads.

---

## Phase 2 — Read ALL N Task specs

For each Task in Stage `STAGE_ID` (Status ≠ `Done (archived)`):

1. Read `ia/projects/{ISSUE_ID}.md` sections:
   - `## 7. Implementation Plan` (or `## §Implementation`) — what was planned.
   - `## §Findings` — what verify-loop found.
   - `## §Verification` — what was verified and how.
2. Hold all N payloads in memory as `task_reads[{id, impl, findings, verification}]`.

---

## Phase 3 — Synthesize N §Audit paragraphs

Single synthesis round over all N `task_reads`. For each Task, produce one paragraph:

> **§Audit** prose = "What was built" (from impl plan) + "What worked / what the verify loop confirmed" (from §Findings + §Verification) + "What to watch" (any caveats, deferred issues, glossary terms introduced). Consistent voice across all N paragraphs. No per-Task MCP re-queries.

Collect into `audit_paragraphs[{id, paragraph}]`.

---

## Phase 4 — Write tuples

Emit one `replace_section` / `insert_after` tuple per Task. Target_anchor = `## §Audit` heading in `ia/projects/{id}.md`.

```yaml
- operation: replace_section
  target_path: ia/projects/{ISSUE_ID}.md
  target_anchor: "## §Audit"
  payload: |
    ## §Audit

    {paragraph text}

- operation: insert_after
  target_path: ia/projects/{ISSUE_ID}.md
  target_anchor: "## §Verification"
  payload: |
    ## §Audit

    {paragraph text}
```

Rule: if `## §Audit` heading already present → use `replace_section`. If absent → use `insert_after` anchored at `## §Verification`. Resolve anchor to single match per contract §Escalation rule before emitting tuple.

Apply tuples directly (this skill is not a pair-head; no downstream Sonnet applier for §Audit writes).

---

## Phase 5 — Hand-off

Emit summary:

```
opus-audit: Stage {STAGE_ID} — {N} §Audit paragraphs written.
Tasks audited: {task_ids[]}.
Downstream: stage-closeout-plan consumes §Audit paragraphs as raw material.
```

Return: `{stage_id, tasks_audited[], audit_paragraphs_written: N}` → caller routes to `stage-closeout-plan`.

---

## Cross-references

- [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, §Escalation rule, §Idempotency requirement.
- [`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md) — shared Stage MCP bundle recipe.
- [`ia/skills/stage-closeout-plan/SKILL.md`](../stage-closeout-plan/SKILL.md) — downstream consumer of §Audit paragraphs.
- [`ia/skills/opus-code-review/SKILL.md`](../opus-code-review/SKILL.md) — per-Task pair-head that runs before /audit.
- Glossary term **Opus audit** (`ia/specs/glossary.md`).
