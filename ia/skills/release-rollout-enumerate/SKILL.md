---
purpose: "Seed the rollout tracker doc (`ia/projects/{umbrella-slug}-rollout-tracker.md`) from the umbrella master-plan's bucket table. One row per child master-plan. Pre-fills cells (a)–(g) from repo reality."
audience: agent
loaded_by: skill:release-rollout-enumerate
slices_via: list_specs, spec_outline, spec_sections
name: release-rollout-enumerate
description: >
  Use when an umbrella master-plan needs its rollout tracker seeded from the bucket table. Creates
  `ia/projects/{umbrella-slug}-rollout-tracker.md` with one row per bucket + sibling. Pre-fills cells
  (a)–(g) based on repo reality at baseline SHA (exploration-doc presence, child master-plan presence,
  Stage count, BACKLOG row count). Surfaces disagreements between parent docs + repo state to a
  Disagreements appendix. Does NOT decide scope (umbrella owns). Does NOT create child master-plans (=
  `/master-plan-new`). Triggers: "enumerate rollout", "seed tracker", "release-rollout-enumerate
  {umbrella}", "bootstrap tracker from master plan".
---

# Release rollout — enumerate (seed tracker)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: tracker prose + disagreements appendix (human-consumed cold); commit messages; verbatim MCP payloads.

**Lifecycle:** One-shot. Runs ONCE per umbrella to seed the tracker. Re-runs only on explicit reseed request (appends new rows if umbrella bucket table grows). Runs BEFORE the umbrella skill (`release-rollout`) can advance any row.

**Related:** [`release-rollout`](../release-rollout/SKILL.md) · [`master-plan-new`](../master-plan-new/SKILL.md) · [`master-plan-extend`](../master-plan-extend/SKILL.md) · [`ia/rules/orchestrator-vs-spec.md`](../../rules/orchestrator-vs-spec.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `UMBRELLA_SPEC` | User prompt | Path to `ia/projects/{slug}-master-plan.md`. Required. Must carry a bucket-table section. |
| `BASELINE_SHA` | User prompt | Optional git SHA anchor. Default: `HEAD`. Tracker cells reference this SHA for "Done" evidence. |
| `SIBLING_ROWS` | User prompt | Optional — non-bucket siblings to add as rows (e.g. `music-player`). Each = `{slug}` path pair. |

---

## Phase sequence (gated)

### Phase 0 — Load umbrella

Read `{UMBRELLA_SPEC}`. Extract:

1. Bucket table section (row per child plan: bucket number, slug, status, exploration path, master-plan path).
2. Change log section (for rollout baseline reference).
3. Parallel-work rule + tier lanes (for tracker hard rules section).

Missing bucket table → STOP, route to `/master-plan-new` or ask user to add bucket table section manually.

### Phase 1 — Repo reality sweep

Per bucket + sibling row:

1. **Glob** `docs/{slug}-exploration.md` → column (b) pre-fill. Present + has `## Design Expansion` (or semantic equivalent) → `✓`. Present + stub (no expansion) → `—` with note. Absent → `—`.
2. **Glob** `ia/projects/{slug}-master-plan.md` → column (c) pre-fill. Present → `✓`. Absent → `—`.
3. **Grep** child master-plan for `### Step` count → column (d) pre-fill. ≥1 step with ≥1 stage → `✓`. Steps present but no stages → `—`.
4. **Grep** child master-plan for `#### Stage` + `**Tasks:**` tables → column (e) pre-fill. ≥1 stage has `Tasks:` table → `◐`; all stages decomposed → `✓`; none → `—`.
5. **Grep** `BACKLOG.md` + `BACKLOG-ARCHIVE.md` for slug → column (f) pre-fill. ≥1 BACKLOG id → `◐`; many filed / all Stage 1 filed → `✓`; zero → `—`.
6. Column (g) pre-fill → `❓` for every row with (e) `◐` or `✓` (verify-required by rollout skill). `—` for rows at (d) or earlier.
7. Column (a) → always `✓` once row exists in tracker.

### Phase 2 — Disagreement detection

Compare repo reality vs umbrella bucket table. Flag:

- Bucket declares slug `X-master-plan.md` but repo has `Y-master-plan.md` (rename or split).
- Bucket declares "NEW or fold" — two paths; ask user pick.
- Bucket exploration doc referenced but missing on disk (stub or absent).
- Sibling row not in umbrella table but exists on disk.
- Parent exploration doc treats buckets differently than bucket table implies.

Each flag → row marker `⚠️` on affected column + entry in Disagreements appendix (numbered). Appendix entry shape: observation, options A (recommended) / B, gate (row cell blocked until user picks).

### Phase 3 — Tracker authoring

Author `ia/projects/{umbrella-slug}-rollout-tracker.md` matching the canonical shape (see [`full-game-mvp-rollout-tracker.md`](../../projects/full-game-mvp-rollout-tracker.md)):

1. Header block — Status, Scope, Baseline SHA, Read-first, Hard rules.
2. Rollout steps definition table — columns (a)–(g) with semantics + gate owner.
3. Rollout matrix — rows pre-filled from Phase 1 sweep.
4. Column (g) align gate detail — explicit hard-block policy + skill-iteration-log coupling.
5. Disagreements appendix — Phase 2 flags.
6. Skill Iteration Log aggregator — empty table ready for rollout agents.
7. Handoff contract — sequential fresh-context agent entry points.
8. Change log — initial authoring entry dated today.

**Do NOT:**

- Author child master-plans. Tracker only reports their presence.
- Decide disagreements. Tracker surfaces them for user pick.
- Skip Phase 2 disagreement detection — incomplete tracker = dangerous rollout.

### Phase 4 — Handoff

Single caveman message:

- `{TRACKER_SPEC}` created — `{row_count}` rows; `{disagreement_count}` disagreements flagged.
- Baseline SHA anchor: `{BASELINE_SHA}`.
- Rows at each column: `(a){n} (b){n} (c){n} (d){n} (e){n} (f){n}`.
- Next step: user resolves disagreements OR runs `claude-personal "/release-rollout {UMBRELLA_SPEC} {row-slug}"` to advance (resolve `{UMBRELLA_SPEC}` + `{row-slug}` to actual paths / first actionable row before emit — paste-ready, no braces).

---

## Guardrails

- IF `{UMBRELLA_SPEC}` does not exist → STOP. Route to `/master-plan-new`.
- IF umbrella has no bucket table section → STOP. Ask user to add bucket table manually or edit umbrella spec.
- IF `{TRACKER_SPEC}` already exists → STOP, ask user reseed-or-merge pick. Default: refuse overwrite.
- IF repo has child master-plan NOT in umbrella bucket table → add row to tracker as `SIBLING_ROW` + log Disagreement #N (umbrella table drift).
- Do NOT create child master-plans — tracker reports presence only.
- Do NOT decide disagreements — surface to user.
- Do NOT commit — user decides.

---

## Seed prompt

```markdown
Run release-rollout-enumerate against {UMBRELLA_SPEC}.

Follow ia/skills/release-rollout-enumerate/SKILL.md end-to-end. Inputs:
  UMBRELLA_SPEC: {path to umbrella master plan}
  BASELINE_SHA: {optional git SHA, default HEAD}
  SIBLING_ROWS: {optional list of non-bucket siblings}

Phase 0 loads umbrella bucket table. Phase 1 sweeps repo reality per row. Phase 2 detects disagreements. Phase 3 authors tracker doc. Phase 4 handoff summary.

Hard STOPs:
- Umbrella missing bucket table → user must add manually.
- Tracker already exists → refuse overwrite; ask merge or reseed.

Do NOT author child master-plans. Do NOT decide disagreements. Do NOT commit.
```

---

## Next step

After tracker seeded → user resolves disagreements (if any) → `claude-personal "/release-rollout {UMBRELLA_SPEC} {row-slug}"` advances first row (emit shell-wrapped + fully resolved: read tracker's implementation-order matrix, pick Order 1 row, substitute both args before handoff).

---

## Changelog

### 2026-04-17 — Shell-wrap Phase 4 + Next step handoffs

**Status:** applied (pending commit)

**Symptom:**
Phase 4 handoff + §Next step emitted bare `/release-rollout {UMBRELLA_SPEC} {row-slug}` template. User pasted → shell failure + placeholder still present.

**Root cause:**
Authored before the `feedback_exact_command_handoff.md` convention (placeholder resolution + `claude-personal "..."` wrap) was saved.

**Fix:**
Phase 4 handoff + §Next step wrapped as `claude-personal "/..."` with explicit "resolve before emit" reminder pointing at tracker's implementation-order matrix.

**Rollout row:** standalone (seeder skill)

**Tracker aggregator:** [`ia/projects/full-game-mvp-rollout-tracker.md#skill-iteration-log`](../../projects/full-game-mvp-rollout-tracker.md#skill-iteration-log)

---
