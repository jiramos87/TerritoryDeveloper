---
purpose: "Retired — use plan-author (TECH-478) for Stage-scoped bulk spec-body authoring; /kickoff retired, use /author."
audience: agent
loaded_by: none
---

# project-spec-kickoff — RETIRED

This skill has been retired as part of the lifecycle-refactor (Stage 7, T7.5).

The legacy `/kickoff` flow combined spec-body authoring with canonical-term enforcement in a single per-Task Opus pass. That responsibility has been split and absorbed into the Stage-scoped bulk `plan-author` flow:

- **Spec-body authoring** → `ia/skills/plan-author/SKILL.md` (Opus, Stage-scoped bulk N×1 invocation). Writes §1 Summary, §2 Goals, §4 Current State, §5 Proposed Design, §7 Implementation Plan for ALL N Task specs in one Opus round per Stage.
- **Canonical-term enforcement** → folded into the same `plan-author` bulk pass (no separate `spec-enrich` stage). Opus enforces glossary canonical terms at author time while the shared Stage MCP bundle holds pre-loaded glossary snippets (R12).

There is no `ia/skills/spec-enrich/` — it was never authored. Canonical-term enforcement requires no mechanical Sonnet post-pass; Opus handles it inline during `plan-author`.

Do not reference `project-spec-kickoff` or `/kickoff` in new code, skills, commands, or agent bodies. Use `/author {MASTER_PLAN_PATH} {STAGE_ID}` (Stage-scoped bulk) or `/author --task {ISSUE_ID}` (N=1 escape hatch) instead.
