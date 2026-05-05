---
name: release-rollout-track
purpose: >-
  Update tracker cells after a downstream subagent (design-explore / master-plan-new /
  master-plan-extend / stage-decompose / stage-file) returns success. Mechanical cell flip + ticket
  append + change log entry. No decisions.
audience: agent
loaded_by: "skill:release-rollout-track"
slices_via: backlog_issue, backlog_search, glossary_lookup, router_for_task, spec_section
description: >-
  Use AFTER a downstream subagent returns success from `/design-explore`, `/master-plan-new`,
  `/master-plan-extend`, `/stage-decompose`, or `/stage-file` to flip the corresponding tracker cell
  `— → ◐` or `◐ → ✓`, append the completion ticket (SHA / doc path / issue id), and add a Change log
  row. Read-only verification pass via MCP (`glossary_lookup`, `router_for_task`, `spec_section`) for
  column (g) align gate. Does NOT decide cell targets (umbrella skill owns). Does NOT dispatch
  subagents (= umbrella skill). Triggers: "track cell flip", "update tracker after stage-file",
  "release-rollout-track {row-slug} {col} {ticket}".
phases: []
triggers:
  - track cell flip
  - update tracker after stage-file
  - release-rollout-track {row-slug} {col} {ticket}
model: inherit
input_token_budget: 120000
pre_split_threshold: 100000
tools_role: planner
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries:
  - IF row not in tracker → STOP.
  - IF `TARGET_COL` invalid → STOP.
  - IF `NEW_MARKER` invalid glyph → STOP.
  - IF (g) align verify fails AND `TARGET_COL = (e)` → STOP. Fall back to (g) = `—` + skill bug log entry. Do NOT tick (e).
  - Do NOT touch other rows.
  - Do NOT edit Disagreements appendix.
  - Do NOT commit.
caller_agent: release-rollout-track
---

# Release rollout — track (cell updater)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Mechanical tool — minimal prose.

**Recipe:** mechanical phases run as recipe [`tools/recipes/release-rollout-track.yaml`](../../../tools/recipes/release-rollout-track.yaml) (DEC-A19 Phase C recipify, 2026-04-29). Skill body documents inputs + caller responsibilities + boundaries; phase logic lives in yaml + bash helpers under `tools/scripts/recipe-engine/release-rollout-track/`.

**Lifecycle:** Runs FROM umbrella `release-rollout` Phase 5 AFTER a dispatched subagent returns success. Never runs standalone (outside umbrella skill) except for manual cell fixes.

**Dispatch mode:** Canonical path = dispatched as Agent `release-rollout-track` subagent (Sonnet pin) from `release-rollout` Phase 5. Inline fallback (SKILL.md-only invocation) available when subagent dispatch is unavailable — behavior identical, but runs in caller's model context.

**Related:** [`release-rollout`](../release-rollout/SKILL.md) · [`release-rollout-enumerate`](../release-rollout-enumerate/SKILL.md) · [`release-rollout-skill-bug-log`](../release-rollout-skill-bug-log/SKILL.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `TRACKER_SPEC` | Umbrella skill | Path to `ia/projects/{umbrella-slug}-rollout-tracker.md` (or `docs/{slug}-rollout-tracker.md`). Required. |
| `ROW_SLUG` | Umbrella skill | Row to update. Required. Must exist in tracker (recipe `validate_row` enforces). |
| `TARGET_COL` | Umbrella skill | `a` / `b` / `c` / `d` / `e` / `f` / `g` (parens stripped). Required. |
| `NEW_MARKER` | Umbrella skill | `✓` / `◐` / `—` / `❓` / `⚠️`. Required. |
| `TICKET` | Umbrella skill | Completion evidence — commit SHA, doc path, issue id. Optional but recommended. |
| `CHANGELOG_NOTE` | Umbrella skill | One-line caveman delta for `## Change log` table. Required. |
| `DATE` | Umbrella skill | Override Change log date (default = today). Optional. |

---

## Invocation

```bash
npm run recipe:run -- release-rollout-track \
  --input tracker_spec={TRACKER_SPEC} \
  --input row_slug={ROW_SLUG} \
  --input target_col={a..g} \
  --input new_marker={✓|◐|—|❓|⚠️} \
  --input ticket={TICKET} \
  --input changelog_note={CHANGELOG_NOTE}
```

Recipe steps (`tools/recipes/release-rollout-track.yaml`):

1. **`validate_row`** — confirm tracker exists, `| {ROW_SLUG} |` row present, column letter ∈ `a..g`, marker glyph valid. Bash exit 1 stops chain.
2. **`cell_flip`** — locate header `(a)..(g)` column index, replace target cell of `| {ROW_SLUG} |` row with `{NEW_MARKER} ({TICKET})`. Idempotent — `noop` when cell already at desired text.
3. **`changelog_append`** — append `| YYYY-MM-DD | {row} cell ({col}) → {marker}; ticket: {ticket} ({note}) | release-rollout-track |` to tracker tail. Skips if identical line present.
4. **`handoff`** — emit single caveman handoff line for umbrella Phase 6.

Outputs (`outputs.handoff_line`, `outputs.flip_status`, `outputs.changelog_status`) bind into umbrella next-row recommendation.

---

## Caller responsibilities (NOT in recipe — defer to seam Phase D)

The recipe handles purely mechanical phases. Two semantic gates remain caller-side until DEC-A19 Phase D wires them as seams:

- **Column (g) align verify** when `TARGET_COL = g` OR `TARGET_COL = e` with (g) gate. Run `term-anchor-verify` subskill ([`ia/skills/term-anchor-verify/SKILL.md`](../term-anchor-verify/SKILL.md)) over child orchestrator domain entities (Objectives / Exit criteria). `all_anchored = true` → pass marker `✓`. Otherwise pass `—` + write Skill Iteration Log entry via [`release-rollout-skill-bug-log`](../release-rollout-skill-bug-log/SKILL.md) naming `unresolved_terms`.
- **Column (f) filed-signal verify** when `TARGET_COL = f`. Either run `tools/scripts/recipe-engine/release-rollout-track/filed-signal.sh --slug {ROW_SLUG}` (coarse heuristic — counts paired `ia/backlog/{id}.yaml` + `ia/projects/{id}*.md`), or hand-inspect Glob output. Caller chooses final glyph based on real evidence.

---

## Guardrails

- IF row `{ROW_SLUG}` not in tracker → recipe `validate_row` STOPs (exit 1). Do not retry.
- IF `TARGET_COL` invalid → recipe STOPs.
- IF `NEW_MARKER` invalid glyph → recipe STOPs.
- IF (g) align verify fails AND `TARGET_COL = (e)` → caller passes `target_col=g` + `new_marker=—` + skill bug log entry. Do NOT tick (e).
- Do NOT touch other rows (cell-flip awk anchors on `| {row} |` literal).
- Do NOT edit Disagreements appendix — user-owned.
- Do NOT commit (recipe does not run git).

---

## Seed prompt

```markdown
Run release-rollout-track to flip one tracker cell.

Inputs:
  TRACKER_SPEC: {path}
  ROW_SLUG: {slug}
  TARGET_COL: {a|b|c|d|e|f|g}
  NEW_MARKER: {✓|◐|—|❓|⚠️}
  TICKET: {commit SHA | doc path | issue id}
  CHANGELOG_NOTE: {one-line caveman delta}

Mechanical phases (validate, cell flip, Change log append, handoff) wrapped by
recipe `release-rollout-track`:

  npm run recipe:run -- release-rollout-track \
    --input tracker_spec={TRACKER_SPEC} \
    --input row_slug={ROW_SLUG} \
    --input target_col={col} \
    --input new_marker={glyph} \
    --input ticket={TICKET} \
    --input changelog_note={CHANGELOG_NOTE}

Recipe stops on first failure (validate / cell-flip / changelog). Both flip and
changelog are idempotent.

When TARGET_COL=g (or =e with (g) gate), run term-anchor-verify FIRST and pass
the resulting marker. When TARGET_COL=f, decide marker via filed-signal.sh or
manual Glob review.

Do NOT touch other rows / disagreements. Do NOT commit.
```

---

## Next step

After cell flip → umbrella skill Phase 6 next-row recommendation.

---

## Changelog

| Date | Change | Trigger |
|------|--------|---------|
| 2026-04-29 | DEC-A19 Phase C recipify — mechanical phases extracted to `tools/recipes/release-rollout-track.yaml` + bash helpers under `tools/scripts/recipe-engine/release-rollout-track/`. Skill body now documents inputs + caller responsibilities + boundaries; numbered phase prose retired. Phase 1 (g) align + Phase 1b filed-signal remain caller-side until Phase D seam wiring. | `docs/agent-as-recipe-runner.md` §G Phase C step 1 |
