---
purpose: "Read one tracker row and return {target_col, hard_gate, chain_ready, next_action}. Sonnet subskill: pure row-state derivation. Called from release-rollout Phase 1."
audience: agent
loaded_by: skill:rollout-row-state
slices_via: none
name: rollout-row-state
description: >
  Sonnet subskill. Reads one row from a rollout tracker doc and derives the next cell to
  tick plus hard-gate flags. Output: {target_col, hard_gate, chain_ready, next_action}.
  Offloads row-state classification from Opus release-rollout Phase 1. No reasoning —
  deterministic predicate over column glyphs. Triggers: called internally by release-rollout
  Phase 1.
---

# Rollout row state (row-state classifier)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Structured output section: normal English for field names + values.

**Model:** Sonnet (deterministic classification — no reasoning required).

**Lifecycle:** Called by `release-rollout` Phase 1 (once per row-advance operation). Never called directly by user.

**Related:** [`release-rollout`](../release-rollout/SKILL.md) · [`release-rollout-track`](../release-rollout-track/SKILL.md) · [`release-rollout-repo-sweep`](../release-rollout-repo-sweep/SKILL.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `TRACKER_SPEC` | Parent skill | Path to `ia/projects/{umbrella-slug}-rollout-tracker.md`. Required. |
| `ROW_SLUG` | Parent skill | Row identifier to classify. Required. Must exist in tracker. |

---

## Phase sequence

### Phase 0 — Load row

Read `{TRACKER_SPEC}`. Find row line matching `| {ROW_SLUG} |`. Extract column cells (a)–(g).

Missing row → STOP, return `{error: "ROW_SLUG not found in tracker"}`.

Hold column values in working memory as `cells[(a)...(g)]`.

### Phase 1 — Hard gate scan

Scan all cells for blocking markers:

- Any cell = `⚠️` (active disagreement) → set `hard_gate = "DISAGREEMENT"`. Stop classification; return immediately.
- Column (b) = `❓` (equivalence gate) → set `hard_gate = "EQUIVALENCE_GATE"`. Return immediately.

If no hard gate markers → `hard_gate = "ok"`.

### Phase 2 — Target column derivation

Walk columns left to right: (a) → (b) → (c) → (d) → (e) → (f). Target = leftmost column not yet `✓`.

Special rules:
- Column (e) candidate: check column (g). If (g) = `—` or `❓` → target = `(g)` instead (align gate must clear before (e)).
- Column (a) already `✓` always (row exists) — skip.
- All columns `✓` → `target_col = "terminal"` (row done).

### Phase 3 — Next action derivation

Map `target_col` to action string:

| target_col | next_action |
|-----------|-------------|
| `(b)` | `design-explore docs/{ROW_SLUG}-exploration.md` |
| `(c)` | `master-plan-new docs/{ROW_SLUG}-exploration.md` |
| `(d)` | `stage-decompose ia/projects/{ROW_SLUG}-master-plan.md Step 1` |
| `(e)` | `stage-file ia/projects/{ROW_SLUG}-master-plan.md Stage 1.1` |
| `(f)` | `stage-file ia/projects/{ROW_SLUG}-master-plan.md Stage {NEXT_UNFILED_STAGE}` |
| `(g)` | `term-anchor-verify (align gate for {ROW_SLUG})` |
| `terminal` | `row complete — no further action` |

`{ROW_SLUG}`, `{NEXT_UNFILED_STAGE}` — substitute from tracker row data. If master plan path or stage cannot be resolved from tracker row, set `next_action` to the best-effort string and set `chain_ready = false`.

### Phase 4 — Chain ready flag

`chain_ready = true` when all of:
- `hard_gate = "ok"`.
- `target_col` is one of `(c)`, `(d)`, `(e)`, `(f)` (autonomous chain path in release-rollout Phase 4).
- Column (b) = `✓`.

`chain_ready = false` when:
- `hard_gate != "ok"`, OR
- `target_col = "(b)"` (user interview required), OR
- `target_col = "(g)"` (align gate authoring required), OR
- `target_col = "terminal"` (nothing to do).

### Phase 5 — Output

Return structured result:

```
ROW_SLUG: {slug}
cells:
  (a): {glyph}
  (b): {glyph}
  (c): {glyph}
  (d): {glyph}
  (e): {glyph}
  (f): {glyph}
  (g): {glyph}
target_col: {(b)|(c)|(d)|(e)|(f)|(g)|terminal}
hard_gate: {ok|DISAGREEMENT|EQUIVALENCE_GATE}
chain_ready: {true|false}
next_action: {action string}
```

---

## Guardrails

- IF `TRACKER_SPEC` unreadable → STOP, return `{error: "TRACKER_SPEC not found"}`.
- IF `ROW_SLUG` not in tracker → STOP, return `{error: "ROW_SLUG not found"}`.
- Do NOT modify any file — read-only classification.
- Do NOT make judgment calls about column readiness beyond the predicate rules above — deterministic only.
- Do NOT call MCP tools — reads tracker file only.
- Do NOT continue classification after `hard_gate != "ok"` — return immediately with gate flag.

---

## Next step

Return structured block to `release-rollout` Phase 1. Parent uses `target_col`, `hard_gate`, `chain_ready`, `next_action` to decide Phase 3–4 dispatch.
