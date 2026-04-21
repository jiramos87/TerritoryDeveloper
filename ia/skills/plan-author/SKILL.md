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
model: inherit
phases:
  - "Sequential-dispatch guardrail"
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

## Phase 0 — Sequential-dispatch guardrail (F3)

> **Guardrail:** Stage-scoped bulk N→1 dispatches Tasks sequentially. Never spawn concurrent Opus invocations (rev 4 A2 + amendment 2). One Task authored → next Task authored — no parallel fan-out.

Applies even when sub-passes are active (Phase 2 token-split path). Each sub-pass is a sequential slice; sub-passes themselves are sequential.

---

## Guardrail — spec path accuracy

Before citing `Assets/Scripts/...` paths in §Plan Author output: confirm the symbol still lives there — audit specs often cite stale filenames. Preflight example: `grep -rn "class {T}" Assets/Scripts/`.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `MASTER_PLAN_PATH` | 1st arg | Repo-relative path (e.g. `ia/projects/lifecycle-refactor-master-plan.md`). |
| `STAGE_ID` | 2nd arg | Stage identifier (e.g. `7.2`). |
| `--task {ISSUE_ID}` | optional flag | Single-spec re-author escape hatch (on a previously filed spec). Skips Stage-scoped loop — bulk pass of N=1. |

---

## Phase 1 — Load Stage context

**Composite-first call (MCP available):**

1. Call `mcp__territory-ia__lifecycle_stage_context({ master_plan_path: "{MASTER_PLAN_PATH}", stage_id: "{STAGE_ID}" })` — first MCP call; returns stage header + Task spec bodies + glossary anchors + invariants + pair-contract slice in one bundle. Use this as the primary Stage context payload.
2. Proceed to authoring from bundle contents. Supplement with `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)) only when bundle is missing domain-specific routing for a touched subsystem (inputs: keywords = union of Task titles + Intent tokens). Returned payload `{glossary_anchors, router_domains, spec_sections, invariants}` kept in Stage scope — shared across all N Task authorings.

### Bash fallback (MCP unavailable)

1. Read `MASTER_PLAN_PATH` Stage `STAGE_ID` block: Objectives, Exit criteria, Tasks table. Collect every Task row whose Status ∈ {Draft, In Review} with a filed `{ISSUE_ID}`.
2. For each Task: read `ia/projects/{ISSUE_ID}.md` — §1 Summary, §2 Goals, §4 Current State, §5 Proposed Design, §7 Implementation Plan stub, §8 Acceptance.
3. Call [`domain-context-load`](../domain-context-load/SKILL.md) once (inputs: keywords = union of Task titles + Intent tokens). Returned payload `{glossary_anchors, router_domains, spec_sections, invariants, cache_block}` kept in Stage scope — shared across all N Task authorings. `cache_block` is the Tier 2 per-Stage ephemeral bundle; reuse without re-fetch per `ia/rules/plan-apply-pair-contract.md` §Tier 2 bundle reuse.
4. Load `ia/rules/plan-apply-pair-contract.md` via `rule_content` (for seam references in Acceptance).
5. Load `ia/specs/glossary.md` canonical-term table via `glossary_discover` — used for canonical-term fold in Phase 4.

---

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

### §Findings

<!-- Per-Task audit notes surfaced during author pass — risks flagged, glossary gaps, unresolved cross-refs. Populated inline at author time; consumed by opus-audit without asserting non-empty gate. Leave blank if no findings. -->
```

**Placement:** `§Plan Author` section goes **between §10 Lessons Learned** and **§Open Questions** in the target spec. Anchor: insert after last line of `## 10. Lessons Learned` block, before `## Open Questions`.

**Write strategy:** single Opus bulk call returns a map `{ISSUE_ID → {audit_notes, examples, test_blueprint, acceptance}}`. For each entry, edit the target spec in-place — replace any existing `## §Plan Author` section (idempotent on re-run) or insert fresh.

---

## Phase 4 — Canonical-term fold + drift scan

Second pass of the same bulk Opus call (or immediately after Phase 3 in same context). Four sub-checks (all four MUST run per Task; emit per-Task counts in hand-off summary).

### 4a. Canonical-term fold (glossary)

For each Task spec, enforce canonical glossary terms across:

- §1 Summary
- §4 Current State (§4.1 Domain behavior paragraph)
- §5 Proposed Design (§5.1 Target behavior paragraph)
- §7 Implementation Plan (Phase names + deliverable bullets)

Rules:
- Every domain term must match `ia/specs/glossary.md` spelling exactly.
- Ad-hoc synonyms → replace with canonical term inline.
- If a term is not in glossary → add it to `§Open Questions` as candidate glossary row (do NOT edit glossary from this skill).
- Opus authors canonical at write time. No post-hoc Sonnet mechanical transform (retired spec-enrich behavior).

Per-Task counter: `n_term_replacements`.

### 4b. Retired-surface tombstone scan

Load tombstone list from disk (one-shot per Stage):

```bash
ls -1 ia/skills/_retired/
ls -1 .claude/agents/_retired/
ls -1 .claude/commands/_retired/
```

Build retired-name set: skill basenames (e.g. `project-spec-kickoff`, `project-spec-close`, `project-stage-close`, `project-new-plan`, `stage-file-monolith`), agent basenames (e.g. `spec-kickoff`, `closeout`, `project-new`, `stage-file`), command basenames (e.g. `kickoff`).

Plus hard-coded retired slash refs: `/enrich`, `/kickoff` (any case).

For each Task spec, scan §1 / §4 / §5 / §7 / §8 / §10 prose AND §Plan Author sub-sections for any retired surface name. Match must be replaced with the live successor:

| Retired | Live successor | Notes |
|---------|---------------|-------|
| `/enrich {id}` / `spec-enrich` | `/author --task {ISSUE_ID}` | T7.11 fold |
| `/kickoff` / `spec-kickoff` / `project-spec-kickoff` | `/author` (Stage 1×N) | M6 collapse |
| `project-spec-close` / `project-stage-close` | `/closeout` (Stage-scoped pair) | T7.14 fold |
| `stage-file-monolith` | `stage-file-plan` + `stage-file-apply` | T7.7 split |
| `project-new-plan` | `/project-new` args-only pair | T7.10 fold |

Per-Task counter: `n_retired_refs_replaced`.

### 4c. Template-section allowlist

Read `ia/templates/project-spec-template.md` once per Stage. Extract every `## ` and `### ` heading line — call this the **canonical-section-set**.

For each Task spec, scan `## ` / `### ` headings. Any heading NOT in the canonical-section-set = drift. Common drifts:

| Drifted heading | Canonical replacement |
|----------------|----------------------|
| `§Closeout Plan` (per-Task) | `§Stage Closeout Plan` (master-plan Stage block — NOT spec) |
| `§Audit Plan` | `§Audit` |
| `§Review` / `§Code Review Plan` | `§Code Review` |

Do NOT delete unknown headings — emit warning in per-Task hand-off entry. If a known retired-pair-section appears in a Task spec (e.g. `## §Closeout Plan`), replace with link to Stage-scoped location: rewrite to a single comment line `<!-- Closeout tuples live under Stage block §Stage Closeout Plan in {MASTER_PLAN_PATH} per T7.14 fold. -->` and remove subordinate content.

Per-Task counter: `n_section_drift_fixed`.

### 4d. Cross-ref task-id resolver

For each Task spec, scan all prose for two id classes:

1. **BACKLOG ids**: pattern `\b(BUG|FEAT|TECH|ART|AUDIO)-\d+\b`. Resolve via:
   - `ia/backlog/{id}.yaml` (open) OR `ia/backlog-archive/{id}.yaml` (closed) — file must exist.
   - Bash: `[ -f ia/backlog/{id}.yaml ] || [ -f ia/backlog-archive/{id}.yaml ]`.
   - Unresolved → add to per-Task warning list `unresolved_backlog_refs[]`. Do NOT auto-rewrite (could be a valid forward-ref or typo — Opus must judge).
2. **Task-key refs**: pattern `\bT\d+\.\d+(\.\d+)?\b` (e.g. `T8.3`, `T4.1.3`). Resolve via the owning master plan task-table only (read once per Stage from `MASTER_PLAN_PATH`):
   - Match must appear as `task_key` value in the Stage Tasks tables of the current master plan.
   - Pre-Step/Stage-collapse legacy format `T{step}.{stage}.{task}` may have been renumbered post-M6 — explicitly flag if length ≠ current scheme.
   - Unresolved → emit drift entry in per-Task hand-off + add comment `<!-- WARN: stale task-ref {T_REF} — verify against {MASTER_PLAN_PATH} -->` next to the offending line. Auto-rewrite ONLY when the ref clearly maps to a single live task (Opus judgment).

Per-Task counters: `n_unresolved_backlog_refs`, `n_stale_task_refs`.

### 4e. Stage-level summary

Aggregate counters into per-Task hand-off entries (Phase 5):

```
{ISSUE_ID}:
  glossary_replacements: {n_term_replacements}
  retired_refs_replaced: {n_retired_refs_replaced}
  section_drift_fixed:   {n_section_drift_fixed}
  unresolved_backlog_refs: [{id}, ...]   # warnings — not auto-fixed
  stale_task_refs:         [{T_REF}, ...] # warnings + inline <!-- WARN --> comments
```

Sub-pass exit gate: if `unresolved_backlog_refs` OR `stale_task_refs` non-empty for ANY Task → tag Stage hand-off summary with `drift_warnings: true` so downstream `/plan-review` knows which Tasks need cross-ref re-check.

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

### 2026-04-20 — F6 re-fold into /stage-file dispatcher

**Status:** applied

**Symptom:**
Stage-entry friction logged in lifecycle-refactor T8 dry-run (F6 finding): 3 commands across 2 CLI sessions (`/stage-file` → `/author` → `/plan-review`). User directive: collapse into ONE `/stage-file` invocation.

**Fix:**
`plan-author` now dispatched from `/stage-file` chain tail (Step 3 in `.claude/commands/stage-file.md`) AFTER `stage-file-applier` writes N stubs + BEFORE `plan-reviewer` drift scan. Idempotent: re-entry on populated `§Plan Author` = no-op skip. Standalone `/author` + `--task {ISSUE_ID}` re-author surface remain valid for single-issue path (post `/project-new`) and Stage recovery. Ordering constraint satisfied — `plan-author` requires stubs on disk (stage-file-applier Step 2 output), cannot fold higher into `stage-file-planner`. Chain terminates at plan-review PASS (STOP); handoff to `/ship-stage` (N≥2) or `/ship` (N=1). `/ship-stage` Phase 1.5 now a READINESS GATE only (no active plan-author dispatch) — STOPPED + `/author` handoff on partial-failure recovery.

**Rollout row:** f6-re-fold

---

### 2026-04-19 — Phase 4 canonical-term fold expanded (retired-surface tombstone scan + template-section allowlist + cross-ref task-id resolver)

**Status:** applied (uncommitted on `feature/master-plans-1`)

**Symptom:**
M8 dry-run (Stage 8 lifecycle-refactor self-referential filing) — `/plan-review` flagged 5 drift tuples that Phase 4 should have caught: retired `/enrich` surface name in spec body; `§Closeout Plan` section header (template now uses `§Stage Closeout Plan`); stale `T4.1.3` cross-ref (pre-Step/Stage-collapse numbering); 2 cross-ref yaml errors in `ia/backlog/TECH-485.yaml` + `ia/backlog/TECH-488.yaml`.

**Root cause:**
Phase 4 fold loaded glossary canonical terms only — did not scan retired-surface tombstones (`ia/skills/_retired/**`, `.claude/commands/_retired/**`, `.claude/agents/_retired/**`), did not validate §-headers against current `ia/templates/project-spec-template.md`, did not resolve `TECH-XXX` / `T-X.Y.Z` cross-refs against owning master plan + BACKLOG.

**Fix:**
Phase 4 expanded into 4a (glossary fold — pre-existing) + 4b (retired-surface tombstone scan with replacement table) + 4c (template-section allowlist) + 4d (cross-ref task-id resolver) + 4e (Stage-level summary). Counters: `n_term_replacements`, `n_retired_refs_replaced`, `n_section_drift_fixed`, `unresolved_backlog_refs[]`, `stale_task_refs[]`.

**Rollout row:** m8-retrospective

**Tracker aggregator:** [`ia/projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/lifecycle-refactor-rollout-tracker.md#skill-iteration-log-aggregator)

---
