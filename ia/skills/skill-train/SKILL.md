---
purpose: "Retrospective consumer skill. Reads target skill's Per-skill Changelog since last `source: train-proposed` entry, aggregates recurring friction (≥ threshold), writes patch proposal (skill) as unified-diff file. User-gated; no auto-apply."
audience: agent
loaded_by: skill:skill-train
slices_via: none
name: skill-train
description: >
  Use on demand to retrospect a lifecycle skill's accumulated friction signal. Reads
  `ia/skills/{SKILL_NAME}/SKILL.md` §Changelog since last `source: train-proposed` entry
  (or `--since {YYYY-MM-DD}`), groups entries by `friction_types[]` value, filters to
  recurrence ≥ threshold (default 2), synthesizes unified-diff proposal targeting Phase
  sequence / Guardrails / Seed prompt sections, writes proposal file, appends pointer entry.
  Triggers: "skill-train", "train skill", "retrospect skill", "skill friction analysis",
  "skill improvement proposal".
---

# skill-train — retrospective consumer

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: code blocks, JSON schema block, seed prompt fenced block, verbatim diff hunks.

**Lifecycle:** On-demand. Run by **skill-train** subagent (Opus) via `/skill-train {SKILL_NAME}`. Reads accumulated **skill self-report** entries from target skill's Per-skill Changelog. Separate channel from `release-rollout-skill-bug-log` (user-logged bugs, not self-reported friction). Consumer side of the **skill training** loop.

**Dispatch mode:** Canonical path = dispatched as `.claude/agents/skill-train.md` subagent (Opus) via `/skill-train` command. Inline fallback (SKILL.md-only invocation) available when subagent dispatch unavailable — behavior identical, runs in caller's model context.

**Related:** [`release-rollout-skill-bug-log`](../release-rollout-skill-bug-log/SKILL.md) · skill-training-master-plan.

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `SKILL_NAME` | User / command | Slug of the target skill (e.g. `master-plan-new`). Required. Must map to `ia/skills/{SKILL_NAME}/SKILL.md`. |
| `--since {YYYY-MM-DD}` | User | Override scan window start. Reads all entries on or after this date regardless of last `source: train-proposed` marker. Optional. |
| `--threshold N` | User | Override recurrence minimum (default 2). Integer ≥ 1. Optional. |
| `--all` | User | Read entire Changelog regardless of any prior `source: train-proposed` marker. **Explicit token-cost warning**: full Changelog may be large. Optional flag. |

---

## Phase sequence

### Phase 0 — Validate target

1. Confirm `ia/skills/{SKILL_NAME}/SKILL.md` exists. Missing → STOP; report skill not found.
2. Read skill file. Locate `## Changelog` section at tail.
3. Absent → inject empty `## Changelog` section before terminal `---` or file EOF (mirror `release-rollout-skill-bug-log` Phase 0 pattern). Report injection.
4. Confirm at least one `source: self-report` entry exists in Changelog. None found → skip Phase 1–4; handoff reports "no self-report entries — nothing to aggregate".

### Phase 1 — Read Changelog entries

1. Determine scan window:
   - Default: entries since last `source: train-proposed` entry (exclusive). No prior marker → scan all entries.
   - `--since {YYYY-MM-DD}`: entries on or after date; ignore `train-proposed` marker.
   - `--all`: scan all entries; ignore `train-proposed` marker; emit token-cost note.
2. Collect all `source: self-report` entries within window. Parse each entry's `friction_types[]` array value.
3. Tally: `{friction_type → [entry_dates]}`.

### Phase 2 — Aggregate friction

1. Apply threshold (default 2; `--threshold N` overrides).
2. For each `friction_type` with recurrence count ≥ threshold, mark as **recurring**.
3. Log tallies (recurring + below-threshold) as caveman one-liners.
4. No recurring friction at threshold → skip Phase 3–4; handoff reports "no recurring friction at threshold N — no proposal written".

### Phase 3 — Synthesize diff

1. For each recurring **friction_type**, identify which section of the target SKILL.md it most directly affects:
   - Phase sequence → `## Phase sequence` section.
   - Guardrails → `## Guardrails` section.
   - Seed prompt → `## Seed prompt` section.
2. Draft unified-diff hunk(s) for each affected section. Use standard `--- a/...` / `+++ b/...` format.
3. Scope constraint: diff targets Phase sequence / Guardrails / Seed prompt **only**. Do NOT include §Schema, Inputs, frontmatter, or §Emitter stanza template hunks.
4. Annotate each hunk with `# friction_type: {type} (N occurrences)` comment above the `@@` line.

### Phase 4 — Write proposal + pointer entry

1. Determine output filename: `ia/skills/{SKILL_NAME}/train-proposal-{YYYY-MM-DD}.md`.
2. Collision (file exists): append `-N` suffix (e.g. `train-proposal-2026-04-18-2.md`). Increment until filename is free.
3. Write **patch proposal (skill)** file with:
   - YAML frontmatter: `skill`, `generated`, `friction_count`, `threshold`, `schema_version`.
   - Section per recurring friction_type: heading + diff fence.
   - Footer: "Review + apply manually. Do NOT auto-apply."
4. Append `source: train-proposed` pointer entry to target skill's `## Changelog`:

```markdown
### {YYYY-MM-DD} — skill-train run

**source:** train-proposed

**proposal:** `ia/skills/{SKILL_NAME}/train-proposal-{YYYY-MM-DD}[-N].md`

**friction_count:** {N}

**threshold:** {T}

---
```

### Phase 5 — Handoff

Single caveman line: `{SKILL_NAME} proposal written → {proposal_path}. {friction_count} recurring friction type(s) at threshold {T}. Review + apply manually.`

---

## Schema

`skill_self_report` — JSON block emitted by lifecycle skills at Phase-N-tail when friction conditions fire. Consumer (this skill) reads and aggregates these entries.

```json
{
  "skill": "string — skill slug, e.g. master-plan-new",
  "run_date": "YYYY-MM-DD",
  "schema_version": "YYYY-MM-DD — date stamp of emitter stanza template; consumer warns on mismatch but still aggregates",
  "friction_types": ["array of string tokens — each entry is one friction category, e.g. missing_input, guardrail_bypass, phase_deviation"],
  "guardrail_hits": ["array of string — which guardrails fired, verbatim"],
  "phase_deviations": ["array of string — which phases were skipped or reordered, with reason"],
  "missing_inputs": ["array of string — which required inputs were absent or malformed"],
  "severity": "low | medium | high"
}
```

**`schema_version` rule:** consumer warns when emitter `schema_version` differs from expected (latest template date) but continues aggregation. Do NOT block on mismatch.

---

## Emitter stanza template

Copy-paste block for lifecycle-skill Phase-N-tail. Single source of truth — wiring tasks (Step 2) copy verbatim; only `{SKILL_NAME}` and `{YYYY-MM-DD}` placeholders are substituted.

**Step 1 — Friction-condition check**

Evaluate:

```
friction_fires = (guardrail_hits.length > 0) OR (phase_deviations.length > 0) OR (missing_inputs.length > 0)
```

Clean-run rule: if all conditions are false → skip Steps 2–3; no-op. §Changelog untouched.

**Step 2 — Construct `skill_self_report` JSON**

Build JSON per §Schema. Set `skill: {SKILL_NAME}`, `run_date: {YYYY-MM-DD}` (today), `schema_version: {YYYY-MM-DD}` (date of this emitter stanza template). Populate `friction_types[]`, `guardrail_hits[]`, `phase_deviations[]`, `missing_inputs[]`, `severity` from phase execution data.

**Step 3 — Append §Changelog entry**

Append to `## Changelog` section of `ia/skills/{SKILL_NAME}/SKILL.md`:

```markdown
### {YYYY-MM-DD} — self-report

**source:** self-report

**schema_version:** {YYYY-MM-DD}

```json
{
  "skill": "{SKILL_NAME}",
  "run_date": "{YYYY-MM-DD}",
  "schema_version": "{YYYY-MM-DD}",
  "friction_types": [],
  "guardrail_hits": [],
  "phase_deviations": [],
  "missing_inputs": [],
  "severity": "low"
}
```

---
```

---

## Guardrails

- IF `ia/skills/{SKILL_NAME}/SKILL.md` missing → STOP. Report skill not found.
- IF no `source: self-report` entries in scan window → skip Phase 3–4; report "no entries".
- IF no recurring friction at threshold → skip Phase 3–4; report "no recurring friction".
- Do NOT apply patch — proposal is review-only; user or fresh subagent applies.
- Do NOT touch other skills' SKILL.md — scope is `{SKILL_NAME}` only.
- Do NOT commit — user decides git state.

---

## Seed prompt

```markdown
Run skill-train.

Inputs:
  SKILL_NAME: {slug under ia/skills/}
  --since: {YYYY-MM-DD, optional — override scan window}
  --threshold: {N, optional, default 2}
  --all: {flag, optional — scan full Changelog; token-cost warning applies}

Phase 0 validates target SKILL.md + §Changelog (injects if absent).
Phase 1 reads Changelog entries since last source: train-proposed (or --since / --all override).
Phase 2 aggregates friction_types[]; recurring = recurrence >= threshold.
Phase 3 synthesizes unified diff targeting Phase sequence / Guardrails / Seed prompt only.
Phase 4 writes ia/skills/{SKILL_NAME}/train-proposal-{YYYY-MM-DD}.md; appends train-proposed pointer entry.
Phase 5 handoff: path + friction-count + "review + apply manually".

Do NOT apply patch. Do NOT touch other skills' SKILL.md. Do NOT commit.
```

---

## Next step

After proposal written → user opens `ia/skills/{SKILL_NAME}/train-proposal-{YYYY-MM-DD}.md`, reviews diff hunks, applies manually (or delegates to a fresh subagent). On apply → run `npm run validate:all`; commit.
