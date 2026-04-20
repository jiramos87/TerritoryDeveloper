---
purpose: "Update tracker cells after a downstream subagent (design-explore / master-plan-new / master-plan-extend / stage-decompose / stage-file) returns success. Mechanical cell flip + ticket append + change log entry. No decisions."
audience: agent
loaded_by: skill:release-rollout-track
slices_via: backlog_issue, backlog_search, glossary_lookup, router_for_task, spec_section
name: release-rollout-track
description: >
  Use AFTER a downstream subagent returns success from `/design-explore`, `/master-plan-new`,
  `/master-plan-extend`, `/stage-decompose`, or `/stage-file` to flip the corresponding tracker cell
  `— → ◐` or `◐ → ✓`, append the completion ticket (SHA / doc path / issue id), and add a Change log
  row. Read-only verification pass via MCP (`glossary_lookup`, `router_for_task`, `spec_section`) for
  column (g) align gate. Does NOT decide cell targets (umbrella skill owns). Does NOT dispatch
  subagents (= umbrella skill). Triggers: "track cell flip", "update tracker after stage-file",
  "release-rollout-track {row-slug} {col} {ticket}".
model: inherit
---

# Release rollout — track (cell updater)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Mechanical tool — minimal prose.

**Lifecycle:** Runs FROM umbrella `release-rollout` Phase 5 AFTER a dispatched subagent returns success. Never runs standalone (outside umbrella skill) except for manual cell fixes.

**Dispatch mode:** Canonical path = dispatched as Agent `release-rollout-track` subagent (Sonnet pin) from `release-rollout` Phase 5. Inline fallback (SKILL.md-only invocation) available when subagent dispatch is unavailable — behavior identical, but runs in caller's model context.

**Related:** [`release-rollout`](../release-rollout/SKILL.md) · [`release-rollout-enumerate`](../release-rollout-enumerate/SKILL.md) · [`release-rollout-skill-bug-log`](../release-rollout-skill-bug-log/SKILL.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `TRACKER_SPEC` | Umbrella skill | Path to `ia/projects/{umbrella-slug}-rollout-tracker.md`. Required. |
| `ROW_SLUG` | Umbrella skill | Row to update. Required. Must exist in tracker. |
| `TARGET_COL` | Umbrella skill | `(a)` / `(b)` / `(c)` / `(d)` / `(e)` / `(f)` / `(g)`. Required. |
| `NEW_MARKER` | Umbrella skill | `✓` / `◐` / `—` / `❓` / `⚠️`. Required. |
| `TICKET` | Umbrella skill | Completion evidence — commit SHA, doc path, issue id. Optional but recommended. |
| `CHANGELOG_NOTE` | Umbrella skill | One-line caveman delta for `## Change log` table. Required. |

---

## Phase sequence

### Phase 0 — Load + validate

1. Read `{TRACKER_SPEC}`. Grep for `| {ROW_SLUG}` in rollout matrix. Missing → STOP, report mismatch.
2. Confirm `TARGET_COL` is one of (a)–(g). Invalid → STOP.
3. Confirm `NEW_MARKER` is valid glyph. Invalid → STOP.

### Phase 1 — Column (g) align verify (only when `TARGET_COL = (g)` OR `TARGET_COL = (e)` with `(g)` gate)

Run `term-anchor-verify` subskill ([`ia/skills/term-anchor-verify/SKILL.md`](../term-anchor-verify/SKILL.md)) for every NEW domain entity introduced by this row (read from child master-plan Objectives / Exit criteria). Inputs: `terms` = English entity names.

`all_anchored = true` → (g) `✓`. `all_anchored = false` → (g) `—` with Skill Iteration Log note naming `unresolved_terms` (route to `release-rollout-skill-bug-log` helper).

### Phase 1b — Column (f) filed-signal verify (only when `TARGET_COL = (f)` AND `NEW_MARKER = ✓` or `◐`)

**Filed signal:** `ia/backlog/{ISSUE_ID}.yaml` exists AND `ia/projects/{ISSUE_ID}*.md` exists — both sides required to count (f). Check via `Glob ia/backlog/*.yaml` + `Glob ia/projects/{slug}*.md`. Any `ia/backlog/{id}.yaml` present for slug with no matching `ia/projects/{id}*.md` → (f) = `◐` (partially filed); zero yaml records found → `—`.

### Phase 2 — Cell flip

Edit `{TRACKER_SPEC}`. Find row line `| {ROW_SLUG} |`. Replace target column cell with `{NEW_MARKER} ({TICKET})`. Preserve other columns verbatim.

**Idempotence:** if cell already at target marker + ticket matches → no-op + skip Phase 3. Different marker OR different ticket → overwrite (append audit to Change log).

### Phase 3 — Change log append

Append row to `## Change log` table at tracker tail:

```
| {YYYY-MM-DD} | {ROW_SLUG} cell ({TARGET_COL}) → {NEW_MARKER}; ticket: {TICKET} ({CHANGELOG_NOTE}) | release-rollout-track |
```

### Phase 4 — Handoff

Single caveman line: `{TRACKER_SPEC} {ROW_SLUG}({TARGET_COL}) → {NEW_MARKER} ({TICKET}).`

---

## Guardrails

- IF row `{ROW_SLUG}` not in tracker → STOP.
- IF `TARGET_COL` invalid → STOP.
- IF `NEW_MARKER` invalid glyph → STOP.
- IF (g) align verify fails AND `TARGET_COL = (e)` → STOP. Fall back to (g) = `—` + skill bug log entry instead. Do NOT tick (e).
- Do NOT touch other rows.
- Do NOT edit Disagreements appendix — user-owned.
- Do NOT commit.

---

## Seed prompt

```markdown
Run release-rollout-track to flip one tracker cell.

Inputs:
  TRACKER_SPEC: {path}
  ROW_SLUG: {slug}
  TARGET_COL: {(a)|(b)|(c)|(d)|(e)|(f)|(g)}
  NEW_MARKER: {✓|◐|—|❓|⚠️}
  TICKET: {commit SHA | doc path | issue id}
  CHANGELOG_NOTE: {one-line caveman delta}

Phase 0 validates row + column + marker. Phase 1 runs (g) align verify when relevant. Phase 2 flips cell in place (idempotent). Phase 3 appends Change log row. Phase 4 handoff line.

Do NOT touch other rows / disagreements. Do NOT commit.
```

---

## Next step

After cell flip → umbrella skill Phase 6 next-row recommendation.
