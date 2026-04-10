---
description: Run validate:all + compile-check + bridge preflight + smoke (Stage 4 stub — wires to verifier subagent)
---

# /verify — Stage 1 stub

This slash command is a **Stage 1 stub** for [TECH-85](../../ia/projects/TECH-85-ia-migration.md). The real implementation lands in **Stage 4 / Phase 4.3**, when it will dispatch the `verifier` subagent and emit a Verification block formatted by the `verification-report` output style.

**Not yet wired — coming in Stage 4.**

For now, run verification inline per [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md):

- `npm run validate:all`
- `npm run unity:compile-check` (when `Assets/**/*.cs` changed)
- `npm run db:bridge-preflight` + Path A or Path B
