---
purpose: "Retired — use plan-applier (TECH-506) Mode plan-fix; Sonnet pair-tail unified."
audience: agent
loaded_by: none
name: plan-fix-apply
---

# plan-fix-apply — RETIRED

Retired **2026-04-20** (lifecycle-refactor Stage 10 T10.5 / **TECH-506**). Unified pair-tail:

- **Replace with:** [`ia/skills/plan-applier/SKILL.md`](../../plan-applier/SKILL.md) — **Mode: plan-fix** (`§Plan Fix` under master-plan Stage block; gate `validate:master-plan-status` + `validate:backlog-yaml`).
- **Agent:** `.claude/agents/plan-applier.md` (replaces `plan-fix-applier.md`).

Do not reference this path in new commands or rules. Triggers `/plan-fix-apply`, `plan fix apply` → dispatch **`plan-applier`** Mode plan-fix.
