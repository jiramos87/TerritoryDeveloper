---
purpose: "Opus per-Task pair-head: reads diff vs spec + invariants + glossary; outcomes PASS / minor / critical; critical branch writes §Code Fix Plan tuples."
audience: agent
loaded_by: skill:opus-code-review
slices_via: invariants_summary, glossary_lookup, glossary_discover
name: opus-code-review
description: >
  Opus pair-head skill. Runs per-Task after implement + verify-loop completes.
  Reads implementation diff vs ia/projects/{id}.md §Implementation Plan + invariants
  subset + glossary bundle (Stage-level if called inside chain).
  Three verdict branches: (a) PASS → mini-report, no tail;
  (b) minor → suggest fix-in-place or deferred issue, no tail;
  (c) critical → writes §Code Fix Plan tuples → triggers `plan-applier` Mode code-fix.
  Triggers: "/code-review {ISSUE_ID}", "opus code review", "code review task",
  "post-verify code review".
model: inherit
phases:
  - "Load diff + context"
  - "Verdict branch"
  - "Write §Code Fix Plan (critical only)"
  - "Hand-off"
---

# Opus-code-review skill (per-Task pair-head)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Opus pair-head per-Task. Runs after `implement` + `verify-loop` complete for the Task. Reads the implementation diff against the spec + invariants + glossary; emits one of three verdicts. Critical verdict triggers sibling pair-tail **`plan-applier`** Mode code-fix.

Contract: [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, seam #4, §Escalation rule.
Sibling pair-tail: [`plan-applier/SKILL.md`](../plan-applier/SKILL.md) — Mode **code-fix**.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `ISSUE_ID` | 1st arg | Task issue id (e.g. `TECH-471`). |
| `STAGE_MCP_BUNDLE` | optional | Pre-loaded `domain-context-load` payload from caller chain. Avoids re-query when called inside `/ship-stage` chain. |

---

## Stage-diff input mode

When invoked as **Pass 2 of `/ship-stage`** (Stage-end bulk code-review, not per-Task), the following contract governs the review:

**Review surface — Stage-level diff:**
- Same cumulative delta anchor as Pass 2 verify-loop: `git diff {FIRST_TASK_COMMIT_PARENT}..HEAD`, EXCLUDING closeout commits (which don't exist yet at Pass 2 time).
- Caller (`ship-stage` Step 3.2) provides this diff or the anchor SHA; do NOT recompute per-Task diffs.

**Acceptance reference:**
- All N `§Plan Author` sections from `{MASTER_PLAN_PATH}` for the Stage's Tasks serve as the combined acceptance criteria reference. Read all N spec files (or the master plan Stage block) to assemble the full acceptance surface.

**Shared context amortization:**
- `STAGE_MCP_BUNDLE` is REQUIRED in Stage-diff mode — the `domain-context-load` payload cached by `ship-stage` Phase 1. Do NOT re-run `domain-context-load`; do NOT re-query `glossary_discover`, `router_for_task`, or `invariants_summary`.
- Single `domain-context-load` payload covers all N Tasks. Context overhead = O(1) per Stage, not O(N).

**Re-entry cap:**
- On `critical` verdict: `ship-stage` Step 3.2 runs `plan-applier` Mode code-fix, then re-enters verify-loop (Step 3.1) + code-review (Step 3.2) ONCE.
- On second `critical` verdict → caller emits `STAGE_CODE_REVIEW_CRITICAL_TWICE`; halt; do NOT re-enter a third time.

**Input fields when called from `/ship-stage` Pass 2:**
- `ISSUE_ID`: not applicable per-Task (set to Stage id or last Task id for context). Review surface = Stage diff, not per-Task.
- `STAGE_MCP_BUNDLE`: required (pre-loaded).
- `REVIEW_MODE`: `"stage_diff"` (set by caller to activate this section's contract).

---

## Phase 1 — Load diff + context

1. Run `git diff main...HEAD -- $(find ia/skills ia/rules ia/templates ia/projects -name '*.md') $(find Assets/Scripts -name '*.cs' 2>/dev/null)` to capture implementation delta for `ISSUE_ID` work. If diff is empty, use staged + recent commit diff.
2. Read `ia/projects/{ISSUE_ID}.md` — §7 Implementation Plan, §8 Acceptance Criteria, §Findings, §Verification.
3. If `STAGE_MCP_BUNDLE` not provided: run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)) with `keywords` derived from `ISSUE_ID` spec title + domain terms, `tooling_only_flag: true` (skip invariants if Task is IA/tooling-only, set `false` for C# Tasks).
4. Load invariants subset relevant to changed files via `mcp__territory-ia__invariants_summary` (domain = changed file domains).
5. Assemble review context: `{diff, spec_impl_plan, acceptance_criteria, glossary_anchors, invariants}`.

---

## Phase 2 — Verdict branch

Run review against all checks below. Collect findings by severity.

| Check | Severity |
|-------|----------|
| All §8 Acceptance Criteria met by diff | critical if not met |
| §7 Implementation Plan phases executed (no phase silently skipped) | critical if phase missing |
| Invariants respected in changed C# (or N/A if tooling-only) | critical if violated |
| Glossary terms spelled canonically in changed docs | minor |
| No adjacent refactors beyond phase scope | minor |
| Cross-ref links (sibling skills, contract, agents) resolve | minor |
| Frontmatter `phases:` present on new SKILL.md files | minor |
| No new singletons; no `FindObjectOfType` in per-frame (C# only) | critical if violated |

**Determine verdict:**
- Zero findings → **PASS**.
- Only minor findings → **minor**.
- Any critical finding → **critical**.

---

## Phase 2a — PASS branch

Write mini-report into spec `§Code Review` section:

```yaml
- operation: replace_section
  target_path: ia/projects/{ISSUE_ID}.md
  target_anchor: "## §Code Review"
  payload: |
    ## §Code Review

    Verdict: **PASS**

    Reviewed: {diff_summary — files changed, lines}.
    Acceptance criteria: all {N} met.
    Invariants: no violations found.
    Glossary: canonical terms confirmed.
    No tail skill triggered.
```

If `## §Code Review` absent → use `insert_after` anchored at `## §Verification` (or last present section).

Exit. Return control to caller: `{verdict: "PASS", issue_id}`.

---

## Phase 2b — Minor branch

Write mini-report into spec `§Code Review` with suggestions:

```yaml
- operation: replace_section
  target_path: ia/projects/{ISSUE_ID}.md
  target_anchor: "## §Code Review"
  payload: |
    ## §Code Review

    Verdict: **minor**

    Minor findings (no tail triggered):
    - {finding 1}: {suggestion — fix-in-place or open deferred issue {id}}
    - {finding 2}: ...

    No §Code Fix Plan emitted.
```

Exit. Return control to caller: `{verdict: "minor", issue_id, findings: [...]}`.

---

## Phase 3 — Write §Code Fix Plan (critical branch only)

Author `§Code Fix Plan` tuples conforming to contract 4-key shape. Resolve every `target_anchor` to a single match before emitting (contract §Escalation rule). Tuples must be atomic, ordered, idempotent.

Write into spec:

```yaml
- operation: replace_section
  target_path: ia/projects/{ISSUE_ID}.md
  target_anchor: "## §Code Fix Plan"
  payload: |
    ## §Code Fix Plan

    > opus-code-review — {N} critical findings. Spawn `plan-applier` Mode code-fix `{ISSUE_ID}`.

    ```yaml
    - operation: {op}
      target_path: {path}
      target_anchor: "{anchor}"
      payload: |
        {literal final-state content}

    - operation: {op}
      ...
    ```
```

If `## §Code Fix Plan` absent → use `insert_after` anchored at `## §Code Review`.

Include `§Code Review` mini-report as separate write (same pass):

```yaml
- operation: replace_section
  target_path: ia/projects/{ISSUE_ID}.md
  target_anchor: "## §Code Review"
  payload: |
    ## §Code Review

    Verdict: **critical**

    Critical findings:
    - {finding 1}: {description}
    - {finding 2}: ...

    §Code Fix Plan written. Spawning plan-applier Mode code-fix.
```

---

## Phase 4 — Hand-off

**PASS / minor branch:** return `{verdict, issue_id}` to caller. No tail triggered.

**Critical branch:** emit:

```
opus-code-review: CRITICAL — {N} findings. §Code Fix Plan written to ia/projects/{ISSUE_ID}.md.
Spawn: plan-applier Mode code-fix {ISSUE_ID}.
```

Caller routes to `plan-applier` Mode code-fix `{ISSUE_ID}`.

---

## Cross-references

- [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, seam #4, §Escalation rule.
- [`ia/skills/plan-applier/SKILL.md`](../plan-applier/SKILL.md) — Sonnet pair-tail Mode code-fix.
- [`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md) — shared Stage MCP bundle recipe.
- [`ia/skills/opus-audit/SKILL.md`](../opus-audit/SKILL.md) — Stage-scoped bulk audit that runs after all per-Task code-reviews pass.
- Glossary term **Opus code review** (`ia/specs/glossary.md`).
