---
purpose: "Retired — use plan-author (TECH-478) for N=1 spec-body authoring."
audience: agent
loaded_by: none
---

# project-new-plan — RETIRED

This skill was never instantiated as a standalone directory. The `/project-new` flow originally used the monolithic `ia/skills/project-new/SKILL.md` (Opus) which combined id reservation, yaml writing, spec stub, and spec-body authoring in one pass.

The lifecycle-refactor (Stage 7) split that flow into two responsibilities:

- **Mechanical materialization** → `ia/skills/project-new-apply/SKILL.md` (Sonnet pair-tail, seam #3). Reserves id, writes yaml, writes spec stub, materializes BACKLOG, validates.
- **Spec-body authoring** → `ia/skills/plan-author/SKILL.md` (Opus, N=1 handoff). Writes §1 Summary, §2 Goals, §4 Current State, §5 Proposed Design, §7 Implementation Plan.

Do not reference `project-new-plan` in new code, skills, or commands. The seam #3 row in `ia/rules/plan-apply-pair-contract.md` will be removed by TECH-478 / T7.11.
