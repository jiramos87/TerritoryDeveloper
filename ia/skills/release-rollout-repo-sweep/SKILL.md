---
purpose: "Per-row Glob/Grep repo reality sweep returning a pre-fill glyph map {(a)–(f)} for one tracker row. Sonnet subskill: templated classification, no reasoning. Called from release-rollout-enumerate Phase 1."
audience: agent
loaded_by: skill:release-rollout-repo-sweep
slices_via: none
name: release-rollout-repo-sweep
description: >
  Sonnet subskill. Per-row Glob/Grep sweep for one tracker row slug. Returns a structured
  glyph map for columns (a)–(f) plus optional disagreement flags. Offloads filesystem
  classification from Opus release-rollout-enumerate Phase 1. No reasoning — pure
  predicate-per-column classification. Triggers: called internally by release-rollout-enumerate
  Phase 1 (one invocation per tracker row).
---

# Release rollout — repo sweep (per-row Glob/Grep classifier)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Structured output section: normal English for field names + values.

**Model:** Sonnet (templated Glob/Grep classification — no reasoning required).

**Lifecycle:** Called by `release-rollout-enumerate` Phase 1 (one invocation per tracker row). Never called directly by user.

**Related:** [`release-rollout-enumerate`](../release-rollout-enumerate/SKILL.md) · [`release-rollout`](../release-rollout/SKILL.md) · [`release-rollout-track`](../release-rollout-track/SKILL.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `ROW_SLUG` | Parent skill | e.g. `city-sim-depth`, `zone-s-economy`. Required. |
| `REPO_ROOT` | Parent skill | Absolute path to repo root. Required. |

---

## Phase sequence

### Phase 0 — Resolve paths

Derive candidate paths from `ROW_SLUG`:

- `EXPLORATION_DOC` = `{REPO_ROOT}/docs/{ROW_SLUG}-exploration.md`
- `MASTER_PLAN` = `{REPO_ROOT}/ia/projects/{ROW_SLUG}-master-plan.md`

Also derive backlog search pattern: `ia/backlog/*.yaml` records whose `notes` or `raw_markdown` field mentions `ROW_SLUG`.

### Phase 1 — Column classification

Run each predicate in order. Emit one glyph per column.

**Column (a) — row exists:**
Always `✓` once this sweep runs (row will exist in tracker).

**Column (b) — exploration doc:**
- Glob `{EXPLORATION_DOC}`. Absent → `—`.
- Present: Grep for `## Design Expansion` (or semantic equivalent heading). Found → `✓`. Not found → `—` with flag `EXPLORATION_STUB`.

**Column (c) — master plan:**
- Glob `{MASTER_PLAN}`. Absent → `—`. Present → `✓`.

**Column (d) — steps decomposed:**
- If column (c) = `—` → `—` (no master plan to check).
- Read `{MASTER_PLAN}`. Grep for `### Step` count. ≥1 step with ≥1 stage (`#### Stage`) → `✓`. Steps present but no stages → `—`.

**Column (e) — stage tasks filed:**
- If column (d) = `—` → `—`.
- Grep `{MASTER_PLAN}` for `#### Stage` blocks + `**Tasks:**` tables (lines containing `| T` row pattern). ≥1 stage with task rows present → `◐`. All stages with task rows present → `✓`. None → `—`.

**Column (f) — BACKLOG filed:**
- Glob `{REPO_ROOT}/ia/backlog/*.yaml`. Filter to records whose `notes` or `raw_markdown` contains `ROW_SLUG`. For each matching `{id}.yaml`: check `{REPO_ROOT}/ia/projects/{id}*.md` exists.
  - Both yaml + spec present for ≥1 record → `◐` (partial filed).
  - Zero records found → `—`.
  - All found records have both yaml + spec → `✓`.

### Phase 2 — Disagreement flags

Flag conditions that parent skill should surface in Disagreements appendix:

- `EXPLORATION_STUB`: exploration doc exists but has no Design Expansion block.
- `MASTER_PLAN_MISSING_STAGES`: master plan has steps but no stage decompositions.
- `PARTIAL_FILED`: (f) = `◐` (some yaml records present without matching spec).
- `SLUG_MISMATCH`: if Glob found `docs/{variant}-exploration.md` but not `docs/{ROW_SLUG}-exploration.md` exactly — suggest rename.

Return empty list when none detected.

### Phase 3 — Output

Return structured result (one block):

```
ROW_SLUG: {slug}
glyph_map:
  (a): ✓
  (b): {✓|—} [{EXPLORATION_STUB?}]
  (c): {✓|—}
  (d): {✓|—}
  (e): {✓|◐|—}
  (f): {✓|◐|—}
  (g): {❓ if (e)=◐|✓ else —}
disagreement_flags: [{flag_name}, ...]
```

Column (g) pre-fill rule (matches `release-rollout-enumerate` Phase 1 step 6): `❓` when (e) = `◐` or `✓` (verify-required); `—` otherwise.

---

## Guardrails

- IF `ROW_SLUG` empty → STOP, report missing input.
- IF `REPO_ROOT` unresolvable → STOP, report path error.
- Do NOT modify any file — read-only sweep.
- Do NOT make inferences about design content — pure predicate classification only.
- Do NOT call MCP tools — parent skill owns the MCP context; this subskill is Bash/Glob/Grep only.
- Do NOT emit multi-row output — one structured block per invocation.

---

## Next step

Return structured block to `release-rollout-enumerate` Phase 1. Parent assembles all row blocks into tracker matrix before Phase 2 disagreement detection.
