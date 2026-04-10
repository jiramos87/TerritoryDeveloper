---
description: Run a test-mode batch scenario loop (Stage 4 stub — wires to test-mode-loop subagent)
argument-hint: "{SCENARIO_ID} (e.g. reference-flat-32x32)"
---

# /testmode — Stage 1 stub

This slash command is a **Stage 1 stub** for [TECH-85](../../ia/projects/TECH-85-ia-migration.md). The real implementation lands in **Stage 4 / Phase 4.3**, when it will dispatch the `test-mode-loop` subagent.

**Not yet wired — coming in Stage 4.**

For now, run the test-mode loop inline using:

- [`.cursor/skills/agent-test-mode-verify/SKILL.md`](../../.cursor/skills/agent-test-mode-verify/SKILL.md)
- `npm run unity:testmode-batch -- --quit-editor-first --scenario-id {SCENARIO_ID}`

Argument received: `$ARGUMENTS`
