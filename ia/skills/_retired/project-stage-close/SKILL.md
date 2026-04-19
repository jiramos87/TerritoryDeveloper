---
purpose: "Retired — use stage-closeout-apply (T7.14 / TECH-481) Stage-scoped bulk pair-tail. Per-Stage close absorbed into Stage-scoped seam #4."
audience: agent
loaded_by: none
slices_via: none
---

# project-stage-close — RETIRED

Retired in lifecycle-refactor Stage 7 (T7.14 / TECH-481) alongside `project-spec-close`.

The legacy per-Stage close flow (phase checklist ticks + handoff message emit + optional journal persist) ran inline per-Stage, paired with `project-spec-close` only on the final Stage. That dual path is absorbed into the Stage-scoped seam #4 pair:

- **Stage closeout plan** → `ia/skills/stage-closeout-plan/SKILL.md` (Opus pair-head, T7.13 / TECH-480). Writes `§Stage Closeout Plan` unified tuple list under master-plan Stage block (shared migration ops deduped + N per-Task archive / delete / status-flip / id-purge / digest_emit ops). Fires once per Stage after every Task reaches Done post-verify.
- **Stage closeout apply** → `ia/skills/stage-closeout-apply/SKILL.md` (Sonnet pair-tail, T7.14 / TECH-481). Reads tuples verbatim, applies bulk: shared ops once + per-Task ops in loop; runs `materialize-backlog.sh` + `validate:all` once at end; aggregates N per-Task digests into one Stage-level digest; flips Stage header Status → Final + rolls up to Step / Plan-level Final via R5 rollup.

`/closeout {MASTER_PLAN_PATH} {STAGE_ID}` now dispatches the seam #4 pair (planner → applier) for the full Stage in one invocation. Per-Task closeout is no longer a distinct lifecycle step.

Historical per-skill friction log preserved in `CHANGELOG.md` alongside this tombstone.

Do not reference `project-stage-close` in new code, skills, commands, or agent bodies.
