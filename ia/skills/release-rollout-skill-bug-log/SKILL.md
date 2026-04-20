---
purpose: "Log a skill bug / gap encountered during rollout. Dual-write: per-skill `## Changelog` section (source of truth) + tracker Skill Iteration Log aggregator row (rollup). Used when a lifecycle skill misbehaves mid-rollout."
audience: agent
loaded_by: skill:release-rollout-skill-bug-log
slices_via: list_rules, rule_content
name: release-rollout-skill-bug-log
description: >
  Use when a lifecycle skill (`design-explore`, `master-plan-new`, `master-plan-extend`,
  `stage-decompose`, `stage-file`) misbehaves during rollout — misses a guardrail, misroutes a Phase,
  fails a pre-condition check, produces invalid output shape. Dual-writes the bug + fix to the per-skill
  `## Changelog` section (authoritative source) + appends an aggregator row to the tracker's Skill
  Iteration Log table (rollup, cross-referenced). Does NOT fix the skill (= hand-edit by user or
  targeted patch via a fresh subagent). Triggers: "log skill bug", "release-rollout-skill-bug-log",
  "skill iteration log entry".
model: inherit
---

# Release rollout — skill bug log (dual-write)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: code blocks in Changelog (reproduction steps), verbatim error messages.

**Lifecycle:** Runs FROM umbrella `release-rollout` Phase 5 when a dispatched subagent reported a skill bug / gap in its handoff message. Also runs standalone when user spots a skill gap outside rollout flow.

**Dispatch mode:** Canonical path = dispatched as Agent `release-rollout-skill-bug-log` subagent (Sonnet pin) from `release-rollout` Phase 5 skill-bug branch. Inline fallback (SKILL.md-only invocation) available when subagent dispatch is unavailable — behavior identical, but runs in caller's model context.

**Related:** [`release-rollout`](../release-rollout/SKILL.md) · [`release-rollout-track`](../release-rollout-track/SKILL.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `SKILL_NAME` | User / umbrella | Slug of the skill with the bug (e.g. `master-plan-extend`). Required. Must map to `ia/skills/{SKILL_NAME}/SKILL.md`. |
| `TRACKER_SPEC` | Umbrella skill | Path to tracker. Required when called from rollout context. Optional standalone. |
| `ROW_SLUG` | Umbrella skill | Tracker row where bug surfaced. Optional. |
| `BUG_SUMMARY` | User / umbrella | One-line caveman description of the gap. Required. |
| `BUG_DETAIL` | User / umbrella | 2–5 lines: reproduction steps, observed vs expected, root cause hypothesis. Required. |
| `FIX_STATUS` | User / umbrella | `pending` / `applied {SHA}` / `rejected`. Required. |
| `FIX_SHA` | User / umbrella | Git SHA if `FIX_STATUS = applied`. Optional otherwise. |

---

## Phase sequence

### Phase 0 — Validate skill target

1. Confirm `ia/skills/{SKILL_NAME}/SKILL.md` exists. Missing → STOP.
2. Read skill file. Locate `## Changelog` section at tail. Absent → inject empty `## Changelog` section before terminal `---` or file EOF in Phase 1.

### Phase 1 — Per-skill Changelog entry (source of truth)

Append to `ia/skills/{SKILL_NAME}/SKILL.md` §Changelog (newest at top, under the section header):

```markdown
## Changelog

### {YYYY-MM-DD} — {BUG_SUMMARY}

**Status:** {FIX_STATUS} {— {FIX_SHA} if applied}

**Symptom:**
{one-line observable from BUG_DETAIL}

**Root cause:**
{one-line hypothesis from BUG_DETAIL}

**Fix:**
{one-line delta applied or "pending"}

**Rollout row:** {ROW_SLUG or "standalone"}

**Tracker aggregator:** [{TRACKER_SPEC}#skill-iteration-log](../../projects/{tracker-filename}#skill-iteration-log)

---
```

Entry serves as the skill's own durable record. Stays with the skill forever.

### Phase 2 — Tracker aggregator row (rollup)

If `TRACKER_SPEC` provided, append row to `## Skill Iteration Log (aggregator)` table:

```
| {YYYY-MM-DD} | {SKILL_NAME} | {ROW_SLUG} | {BUG_SUMMARY} | {FIX_SHA or _pending_} | `ia/skills/{SKILL_NAME}/SKILL.md#changelog-{YYYY-MM-DD}` |
```

Aggregator keeps rollout-scope entries only. Full detail stays in per-skill Changelog. Standalone call (no TRACKER_SPEC) skips Phase 2.

### Phase 3 — Handoff

Single caveman line: `{SKILL_NAME} §Changelog + {TRACKER_SPEC} §Skill Iteration Log updated. Fix status: {FIX_STATUS}.`

---

## Guardrails

- IF `ia/skills/{SKILL_NAME}/SKILL.md` missing → STOP. Report skill not found.
- IF `BUG_SUMMARY` or `BUG_DETAIL` missing → STOP. Ask user for detail — log entries without reproduction steps are useless.
- IF `FIX_STATUS = applied` but `FIX_SHA` absent → STOP. Applied fixes must cite the commit.
- Do NOT fix the skill — authoring fix is out of scope. User or fresh subagent applies patch.
- Do NOT touch other skills.
- Do NOT commit — user decides.

---

## Seed prompt

```markdown
Run release-rollout-skill-bug-log.

Inputs:
  SKILL_NAME: {slug under ia/skills/}
  TRACKER_SPEC: {path, optional for standalone}
  ROW_SLUG: {optional — rollout row where bug surfaced}
  BUG_SUMMARY: {one-line caveman}
  BUG_DETAIL: {2–5 lines reproduction + root cause}
  FIX_STATUS: {pending | applied {SHA} | rejected}
  FIX_SHA: {if applied}

Phase 0 validates skill target. Phase 1 dual-writes per-skill Changelog entry. Phase 2 appends tracker aggregator row (skipped standalone). Phase 3 handoff.

Do NOT fix the skill. Do NOT touch other skills. Do NOT commit.
```

---

## Next step

After bug logged → user or fresh subagent applies the fix (if `FIX_STATUS = pending`) + re-runs the skill. On fix applied → re-run this skill with `FIX_STATUS = applied {SHA}` to update both records.
