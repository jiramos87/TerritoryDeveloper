---
purpose: "Opus Stage-scoped bulk non-pair: authors §Plan Author section across all N Task specs of one Stage in a single bulk pass; absorbs retired spec-enrich canonical-term fold."
audience: agent
loaded_by: skill:plan-author
slices_via: none
name: plan-author
description: >
  Opus Stage-scoped bulk spec-body authoring stage. Invoked once per Stage
  after `stage-file-apply` writes N spec stubs (multi-task path), or once at
  N=1 after `project-new-apply` (single-task path). Reads ALL N spec stubs +
  Stage header + shared MCP bundle + invariants + pre-loaded glossary in one
  bulk pass; writes ALL N §Plan Author sections (4 sub-sections each) in one
  Opus round. Canonical-term fold absorbs retired spec-enrich. Non-pair —
  no Sonnet tail. Triggers: "/author {MASTER_PLAN_PATH} {STAGE_ID}",
  "plan author", "stage bulk spec enrich", "author stage task specs".
phases:
  - "Load Stage context"
  - "Token-split guardrail"
  - "Bulk author §Plan Author"
  - "Canonical-term fold"
  - "Validate + hand-off"
---

# Plan-author skill (Opus Stage-scoped bulk, non-pair)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Opus **Stage-scoped bulk** spec-body author. Non-pair (no Sonnet tail). Runs **once per Stage** after `stage-file-apply` writes N stubs (multi-task path) or once at N=1 after `project-new-apply` (single-task path). Reads ALL N filed spec stubs + Stage header + shared Stage MCP bundle + invariants + pre-loaded glossary anchors in one bulk pass; writes ALL N `§Plan Author` sections in one Opus round. Same pass enforces canonical glossary terms across `§Objective` / `§Background` / `§Implementation Plan` — absorbs retired `spec-enrich` stage.

Does **NOT** write code, run verify, or flip Task status. Downstream: `plan-review` (seam #1 gate) then per-Task `/implement`.

Contract: [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — plan-author listed as non-pair Stage-scoped Opus stage; no Sonnet tail.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `MASTER_PLAN_PATH` | 1st arg | Repo-relative path (e.g. `ia/projects/lifecycle-refactor-master-plan.md`). |
| `STAGE_ID` | 2nd arg | Stage identifier (e.g. `7.2`). |
| `--task {ISSUE_ID}` | optional flag | Single-spec re-author escape hatch (on a previously filed spec). Skips Stage-scoped loop — bulk pass of N=1. |

---

## Phase 1 — Load Stage context

1. Read `MASTER_PLAN_PATH` Stage `STAGE_ID` block: Objectives, Exit criteria, Tasks table. Collect every Task row whose Status ∈ {Draft, In Review} with a filed `{ISSUE_ID}`.
2. For each Task: read `ia/projects/{ISSUE_ID}.md` — §1 Summary, §2 Goals, §4 Current State, §5 Proposed Design, §7 Implementation Plan stub, §8 Acceptance.
3. Call [`domain-context-load`](../domain-context-load/SKILL.md) once (inputs: keywords = union of Task titles + Intent tokens). Returned payload `{glossary_anchors, router_domains, spec_sections, invariants}` kept in Stage scope — shared across all N Task authorings.
4. Load `ia/rules/plan-apply-pair-contract.md` via `rule_content` (for seam references in Acceptance).
5. Load `ia/specs/glossary.md` canonical-term table via `glossary_discover` — used for canonical-term fold in Phase 4.

**Output:** bulk input payload `{stage_header, task_specs[], mcp_bundle, glossary_table}`.

---

## Phase 2 — Token-split guardrail

Count total input tokens: sum of Stage header + N spec stubs + MCP bundle + invariants snippet. Opus threshold ≈ 180k input tokens (leave headroom for output).

- Under threshold → proceed to Phase 3 with single bulk pass (N Tasks).
- Over threshold → split into ⌈N/2⌉ bulk sub-passes. Each sub-pass covers ⌈N/2⌉ Tasks; shared context (Stage header + MCP bundle + glossary_table) replayed per sub-pass.
- **Never** regress to per-Task mode — per-Task authoring defeats the bulk intent (R10 regression bar).

Emit split decision in hand-off summary.

---

## Phase 3 — Bulk author §Plan Author

For each Task spec in the bulk input, write one `§Plan Author` section containing 4 sub-sections in strict order:

### §Plan Author structure (per Task)

```markdown
## §Plan Author

### §Audit Notes

<!-- Upfront conceptual audit — risks, ambiguity, invariant touches. 2–5 bullets. -->

- Risk: {risk or invariant touch}. Mitigation: {approach}.
- Ambiguity: {open question}. Resolution: {decision or defer to §Open Questions}.
- …

### §Examples

<!-- Concrete inputs/outputs + edge cases + legacy shapes. Tables or code blocks. -->

| Input | Expected output | Notes |
|-------|-----------------|-------|
| {case 1} | {result} | {edge case / legacy shape} |
| … | … | … |

### §Test Blueprint

<!-- Structured tuples consumed by `/implement` + `/verify-loop`. One row per test. -->

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| {name} | {inputs} | {expected} | {node \| unity-batch \| bridge \| manual} |
| … | … | … | … |

### §Acceptance

<!-- Refined per-Task acceptance criteria — narrower than Stage Exit. Checkbox list. -->

- [ ] …
- [ ] …
```

**Placement:** `§Plan Author` section goes **between §10 Lessons Learned** and **§Open Questions** in the target spec. Anchor: insert after last line of `## 10. Lessons Learned` block, before `## Open Questions`.

**Write strategy:** single Opus bulk call returns a map `{ISSUE_ID → {audit_notes, examples, test_blueprint, acceptance}}`. For each entry, edit the target spec in-place — replace any existing `## §Plan Author` section (idempotent on re-run) or insert fresh.

---

## Phase 4 — Canonical-term fold

Second pass of the same bulk Opus call (or immediately after Phase 3 in same context). For each Task spec, enforce canonical glossary terms across:

- §1 Summary
- §4 Current State (§4.1 Domain behavior paragraph)
- §5 Proposed Design (§5.1 Target behavior paragraph)
- §7 Implementation Plan (Phase names + deliverable bullets)

Rules:
- Every domain term must match `ia/specs/glossary.md` spelling exactly.
- Ad-hoc synonyms → replace with canonical term inline.
- If a term is not in glossary → add it to `§Open Questions` as candidate glossary row (do NOT edit glossary from this skill).
- Opus authors canonical at write time. No post-hoc Sonnet mechanical transform (retired spec-enrich behavior).

Emit per-Task count `{ISSUE_ID → n_term_replacements}` in hand-off summary.

---

## Phase 5 — Validate + hand-off

1. Run `npm run validate:dead-project-specs` (cheap, fast) — confirms all edited `ia/projects/*.md` still have valid cross-refs.
2. Emit structured hand-off summary:

```
plan-author: Stage {STAGE_ID} — {N} Tasks authored in {split_count} bulk pass(es).
  Per-Task:
    {ISSUE_ID}: §Plan Author written ({n_audit_notes} audit notes, {n_examples} examples, {n_tests} test rows, {n_accept} acceptance criteria); canonical-term fold: {n_term_replacements} replacements.
    …
  Next: /plan-review {MASTER_PLAN_PATH} {STAGE_ID}  (multi-task path)
        /implement {ISSUE_ID}                      (single-task path, N=1)
```

Does NOT flip Task Status — `plan-review` (multi-task) or `/implement` (single-task N=1) is next.

---

## Cross-references

- [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — plan-author non-pair entry; 4 surviving pair seams.
- [`ia/skills/plan-review/SKILL.md`](../plan-review/SKILL.md) — downstream seam #1 gate (multi-task path).
- [`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md) — shared Stage MCP bundle recipe.
- [`ia/skills/stage-file-apply/SKILL.md`](../stage-file-apply/SKILL.md) — upstream (writes N stubs before plan-author fires).
- [`ia/skills/project-new-apply/SKILL.md`](../project-new-apply/SKILL.md) — upstream N=1 path.
- [`ia/templates/project-spec-template.md`](../../templates/project-spec-template.md) — §Plan Author section stub location.
- Glossary: `ia/specs/glossary.md` — canonical-term fold source of truth.

## Changelog
