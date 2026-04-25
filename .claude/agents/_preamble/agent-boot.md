<!--
Universal subagent boot directive — shared by all `.claude/agents/*.md` via `@.claude/agents/_preamble/agent-boot.md` import.

Carries ONLY the progress-emission contract. Caveman directive stays per-agent (per-surface exception lists differ). Tier 1 cache `cache_control` declaration stays inline per validator regex (`tools/scripts/validate-cache-block-sizing.ts` requires literal text in agent body).
-->

Progress emission: `@ia/skills/subagent-progress-emit/SKILL.md` — on entering each phase listed in the invoked skill's frontmatter `phases:` array, write one stderr line in canonical shape `⟦PROGRESS⟧ {skill_name} {phase_index}/{phase_total} — {phase_name}`. No stdout. No MCP. No log file.
