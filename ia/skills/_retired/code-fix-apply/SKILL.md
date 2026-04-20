---
purpose: "Retired — use plan-applier (TECH-506) Mode code-fix; Sonnet pair-tail unified."
audience: agent
loaded_by: none
name: code-fix-apply
---

# code-fix-apply — RETIRED

Retired **2026-04-20** (lifecycle-refactor Stage 10 T10.5 / **TECH-506**). Unified pair-tail:

- **Replace with:** [`ia/skills/plan-applier/SKILL.md`](../../plan-applier/SKILL.md) — **Mode: code-fix** (`§Code Fix Plan` in `ia/projects/{ISSUE_ID}.md`; verify gate + 1-retry bound).
- **Agent:** `.claude/agents/plan-applier.md` (replaces `code-fix-applier.md`).

Do not reference this path in new commands or rules. Triggers `/code-fix-apply`, `apply §Code Fix Plan` → dispatch **`plan-applier`** Mode code-fix.
