---
purpose: "Sonnet pair-head: reviews all Task specs of a Stage before first Task kickoff; emits §Plan Fix tuples or PASS sentinel under Stage block in master plan."
audience: agent
loaded_by: skill:plan-review
slices_via: none
name: plan-review
description: >
  Sonnet pair-head skill (model downgrade 2026-04-20 — drift scan is
  mechanical against plan-author output; reserve Opus budget for authoring +
  code review). Runs once per Stage before first Task kickoff.
  Reads all filed Task specs under the Stage + master-plan Stage header +
  invariants subset + glossary snippets; emits structured §Plan Fix tuples
  per ia/rules/plan-apply-pair-contract.md or writes PASS sentinel.
  Triggers: "plan review", "/plan-review {MASTER_PLAN_PATH} {STAGE_ID}",
  "stage plan review", "pre-stage drift scan".
model: inherit
phases:
  - "Load Stage context"
  - "Drift scan"
  - "Write §Plan Fix tuples"
  - "Hand-off"
---

# Plan-review skill (Sonnet pair-head)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Sonnet pair-head (model downgrade 2026-04-20 — drift scan is mechanical against plan-author output). Runs **once per Stage** before any Task kickoff begins. Reads the Stage header + all filed Task specs + invariants; checks alignment against master-plan intent; outputs either a **PASS sentinel** or a structured **§Plan Fix tuple list** under the target Stage block.

Contract: [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, seam #1, §Escalation rule.
Sibling pair-tail: [`plan-fix-apply/SKILL.md`](../plan-fix-apply/SKILL.md).

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `MASTER_PLAN_PATH` | 1st arg | Repo-relative path to master plan (e.g. `ia/projects/lifecycle-refactor-master-plan.md`). |
| `STAGE_ID` | 2nd arg | Stage identifier (e.g. `7.1`). |

---

## Phase 1 — Load Stage context

1. Read `MASTER_PLAN_PATH` Stage `STAGE_ID` block: Objectives, Exit criteria, Tasks table.
2. For each Task row in the Stage whose Status ≠ `Done`: read `ia/projects/{ISSUE_ID}.md` — §1 Summary, §2 Goals, §7 Implementation Plan, §8 Acceptance Criteria.
3. Load invariants subset via `mcp__territory-ia__invariants_summary` (domain = skill, tooling, ia).
4. Load glossary anchors for terms appearing in Task specs via `mcp__territory-ia__glossary_discover` + `mcp__territory-ia__glossary_lookup`.
5. Load relevant spec sections via `mcp__territory-ia__spec_sections` (pair-contract, project-hierarchy, orchestrator-vs-spec).

**Output:** in-memory context payload `{stage_header, task_specs[], invariants, glossary_anchors, spec_sections}`.

---

## Phase 2 — Drift scan

Run these checks against each Task spec. Record every finding as a candidate tuple.

| Check | What to look for |
|-------|-----------------|
| **Goal–intent alignment** | Task §1 Summary / §2 Goals must match master-plan Task Intent cell. Drift = candidate `replace_section` on spec §1 or §2. |
| **Implementation plan completeness** | §7 Implementation Plan must have ≥1 numbered Phase. Missing or empty → candidate `replace_section`. |
| **Acceptance criteria presence** | §8 Acceptance Criteria must be non-empty. Missing → candidate `replace_section`. |
| **Glossary term consistency** | Domain terms in spec must match canonical glossary spelling. Misspelling → candidate `replace_section` on affected line. |
| **Invariant compliance** | §7 phases must not schedule operations that violate loaded invariants. Violation → candidate `replace_section` with corrected phase text. |
| **Cross-ref accuracy** | Links to sibling skills, pair-contract, agents must resolve. Broken link → candidate `replace_section`. |
| **Frontmatter completeness** | `phases:` ordered array must be present. Missing → candidate `set_frontmatter`. |
| **Status coherence** | Task row Status in master plan must match spec `> **Status:**` block. Mismatch → candidate `append_row` or `replace_section` on master-plan task table. |

PASS condition: zero candidate tuples after full scan.

---

## Phase 3 — Write §Plan Fix tuples

### PASS branch

Zero drift found. Write sentinel under Stage block in master plan:

```markdown
### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.
```

Exit. Do NOT spawn `plan-fix-apply`.

### Fix branch

One or more candidates found. Resolve each to a single anchor before writing (invariant: pair-head MUST resolve `target_anchor` to a single match — see pair-contract §Escalation rule). Then write §Plan Fix under Stage block:

```markdown
### §Plan Fix

> plan-review — {N} tuples. Spawn `plan-fix-apply {MASTER_PLAN_PATH} {STAGE_ID}`.

```yaml
- operation: replace_section
  target_path: ia/projects/TECH-NNN.md
  target_anchor: "## 1. Summary"
  payload: |
    <corrected content>

- operation: set_frontmatter
  target_path: ia/projects/TECH-NNN.md
  target_anchor: "phases"
  payload:
    phases:
      - "Phase 1 name"
      - "Phase 2 name"
```
```

Rules:
- One tuple = one atomic edit. Tuples execute in declared order.
- `target_anchor` exact heading text, exact line number, glossary row id, or `task_key:T{N}`.
- `payload` = literal final-state content; never a diff.
- Multiple edits to same file: one tuple per anchor, ordered top-to-bottom in file.

---

## Phase 4 — Hand-off

**PASS branch:** emit summary `plan-review: PASS — Stage {STAGE_ID} aligned. Downstream continue.` Return control to caller.

**Fix branch:** emit summary `plan-review: {N} tuples written to §Plan Fix. Spawn plan-fix-apply {MASTER_PLAN_PATH} {STAGE_ID}.`

Caller (agent or `/plan-review` dispatcher) reads §Plan Fix result and routes:
- PASS → proceed to stage Task kickoff.
- Fix tuples present → invoke `plan-fix-apply {MASTER_PLAN_PATH} {STAGE_ID}`.

---

## Cross-references

- [`ia/rules/plan-apply-pair-contract.md`](../../rules/plan-apply-pair-contract.md) — §Plan tuple shape, seam #1, §Escalation rule, §Idempotency requirement.
- [`ia/skills/plan-fix-apply/SKILL.md`](../plan-fix-apply/SKILL.md) — Sonnet pair-tail.
- [`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md) — shared Stage MCP bundle recipe.
- Glossary term **plan review** (`ia/specs/glossary.md`).
- [`ia/rules/project-hierarchy.md`](../../rules/project-hierarchy.md) — Stage/Task lifecycle.

---

## Changelog

### 2026-04-20 — Model downgrade Opus → Sonnet + F6 re-fold into /stage-file dispatcher

**Status:** applied

**Symptom:**
Two orthogonal changes landed same day. (1) Drift scan is mechanical against plan-author output — rule-book lookups (retired-term scan, section-name match, cross-ref id resolve), not synthesis. Opus budget overkill (F5 dry-run finding — 30% of Stage-entry token total). (2) Stage-entry friction: 3 commands across 2 CLI sessions (`/stage-file` → `/author` → `/plan-review`) per F6 dry-run finding.

**Fix:**
(1) Frontmatter `model: opus` → `model: sonnet`. Role heading + body text flipped to Sonnet pair-head. Supersedes pending F5 Sonnet-downgrade candidate entry below. Pair-contract escalation rule now reads "pair-head MUST resolve target_anchor" (model-agnostic). (2) `plan-review` now dispatched from `/stage-file` chain tail (Step 4 in `.claude/commands/stage-file.md`) AFTER `stage-file-applier` + `plan-author` (bulk Stage 1×N) complete. Re-entry cap = 1 on critical — second critical → abort with `STAGE_PLAN_REVIEW_CRITICAL_TWICE`. Standalone `/plan-review` + subagent remain valid for ad-hoc drift scan / recovery.

**Rollout row:** f6-re-fold

---

### 2026-04-19 — Refactor-in-flight sampling bias logged (F4 dry-run finding)

**Status:** pending (open question — re-measure required)

**Symptom:**
M8 dry-run on lifecycle-refactor Stage 8 — 3 of 5 fix tuples were lifecycle-refactor churn artifacts (retired commands, renumbered Stages, renamed sections). Steady-state yield on non-refactor work unknown.

**Root cause:**
Self-referential dry-run scope (filing lifecycle-refactor's own Tasks) inflates `plan-review` apparent yield. Cannot generalize "caught real bugs" → "load-bearing at steady state".

**Fix:**
pending — re-run T8.1 against external open master plan with _pending_ Task (T8.1b row 9 deferred). Locks Sonnet-downgrade decision (Fix #4) + conditional-gate decision (Fix #5).

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---

### 2026-04-19 — Token cost ≈30% of Stage-entry pipeline (F5 dry-run finding)

**Status:** pending (Sonnet-downgrade candidate deferred — Fix #4)

**Symptom:**
M8 dry-run plan-review = 66.7k Opus + 36.2k Sonnet ≈ 103k tokens (30% of 345k Stage-entry total). Most checks = rule-book lookups (retired-term scan, section-name match, cross-ref id resolve), not synthesis.

**Root cause:**
Opus overkill for mechanical checks. Cross-sub-pass coherence role (when plan-author splits) load-bearing — but only that subset.

**Fix:**
pending — Sonnet-downgrade for mechanical checks; Opus retained only for cross-sub-pass coherence path. Conditional-gate (skip when plan-author single-pass; auto-PASS sentinel) deferred to Fix #5. Re-measure first per F4.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---
