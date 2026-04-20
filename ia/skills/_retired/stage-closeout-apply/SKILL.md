---
purpose: "Retired — use plan-applier (TECH-506) Mode stage-closeout; Sonnet pair-tail unified."
audience: agent
loaded_by: none
name: stage-closeout-apply
---

# stage-closeout-apply — RETIRED

Retired **2026-04-20** (lifecycle-refactor Stage 10 T10.5 / **TECH-506**). Unified pair-tail:

- **Replace with:** [`ia/skills/plan-applier/SKILL.md`](../../plan-applier/SKILL.md) — **Mode: stage-closeout** (`§Stage Closeout Plan`; `materialize-backlog.sh` + `validate:all` + R5 rollup).
- **Agent:** `.claude/agents/plan-applier.md` (replaces `stage-closeout-applier.md`).

Do not reference this path in new commands or rules. Triggers `/closeout` tail, `apply stage closeout plan` → dispatch **`plan-applier`** Mode stage-closeout.
