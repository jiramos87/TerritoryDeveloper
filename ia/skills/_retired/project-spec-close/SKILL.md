---
purpose: "Retired — use stage-closeout-apply (TECH-481) Stage-scoped bulk pair-tail; /closeout command rewired Stage-scoped."
audience: agent
loaded_by: none
---

# project-spec-close — RETIRED

This skill has been retired as part of the lifecycle-refactor (Stage 7, T7.5).

The legacy per-Task closeout flow ran a full IA persistence + id purge + spec-delete pass for each individual Task spec. That per-Task pattern is absorbed into the Stage-level bulk closeout pair:

- **Stage closeout plan** → `ia/skills/stage-closeout-plan/SKILL.md` (Opus pair-head, T7.13 / TECH-480). Invoked once per Stage when all Tasks reach Done post-verify; reads all N Task `§Audit` paragraphs + §Implementation + §Findings + §Verification sections; writes `§Stage Closeout Plan` with unified IA migration tuples + N BACKLOG archive ops + N id purges + N spec deletes in one structured block.
- **Stage closeout apply** → `ia/skills/stage-closeout-apply/SKILL.md` (Sonnet pair-tail, T7.14 / TECH-481). Reads `§Stage Closeout Plan` tuples; executes bulk: shared glossary/rule/doc edits once; loops N Tasks archiving yaml + deleting spec + flipping task-row status; runs `materialize-backlog.sh` + `validate:dead-project-specs` once at end; emits one Stage closeout digest.

The `/closeout` command is rewired Stage-scoped: dispatches stage-closeout-planner then stage-closeout-applier for a full Stage in one invocation, not per-Task.

Historical per-skill friction log preserved in `CHANGELOG.md` alongside this tombstone.

Do not reference `project-spec-close` in new code, skills, commands, or agent bodies. `/closeout` now dispatches to the Stage-scoped skill pair.
