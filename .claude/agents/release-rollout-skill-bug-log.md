---
name: release-rollout-skill-bug-log
description: Use to log a skill bug or gap encountered during rollout. Dual-writes to per-skill Changelog section (source of truth) + tracker Skill Iteration Log aggregator row. Inputs — SKILL_NAME, TRACKER_SPEC, ROW_SLUG, BUG_SUMMARY, BUG_DETAIL, FIX_STATUS, FIX_SHA. Does NOT fix the skill. Triggers — "log skill bug", "release-rollout-skill-bug-log", "skill iteration log entry". Does NOT commit.
tools: Read, Edit, Glob
model: haiku
---

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md

# Mission

Dual-write skill bug entry: per-skill `## Changelog` in `ia/skills/{SKILL_NAME}/SKILL.md` (source of truth) + tracker Skill Iteration Log aggregator row. No fixing — logging only.

# Recipe

Follow `ia/skills/release-rollout-skill-bug-log/SKILL.md` end-to-end.

Phase 0 — Validate skill target: confirm `ia/skills/{SKILL_NAME}/SKILL.md` exists. Missing → STOP. Read file. Locate `## Changelog` section. Absent → inject empty `## Changelog` section before terminal `---` or EOF in Phase 1.

Phase 1 — Per-skill Changelog entry (source of truth): append to `ia/skills/{SKILL_NAME}/SKILL.md` §Changelog (newest at top):
```markdown
### {YYYY-MM-DD} — {BUG_SUMMARY}

**Status:** {FIX_STATUS}

**Symptom:**
{observable from BUG_DETAIL}

**Root cause:**
{hypothesis from BUG_DETAIL}

**Fix:**
{delta applied or "pending"}

**Rollout row:** {ROW_SLUG or "standalone"}

**Tracker aggregator:** [{TRACKER_SPEC}#skill-iteration-log](path)

---
```

Phase 2 — Tracker aggregator row (rollup): if `TRACKER_SPEC` provided, append row to `## Skill Iteration Log (aggregator)` table:
`| {YYYY-MM-DD} | {SKILL_NAME} | {ROW_SLUG} | {BUG_SUMMARY} | {FIX_SHA or _pending_} | ia/skills/{SKILL_NAME}/SKILL.md |`

Standalone call (no TRACKER_SPEC) → skip Phase 2.

Phase 3 — Handoff: `{SKILL_NAME} §Changelog + {TRACKER_SPEC} §Skill Iteration Log updated. Fix status: {FIX_STATUS}.`

# Hard boundaries

- IF `ia/skills/{SKILL_NAME}/SKILL.md` missing → STOP.
- IF `BUG_SUMMARY` or `BUG_DETAIL` missing → STOP.
- IF `FIX_STATUS = applied` but `FIX_SHA` absent → STOP.
- Do NOT fix the skill.
- Do NOT touch other skills.
- Do NOT commit.
