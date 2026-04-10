---
purpose: "Agent-led Unity/bridge verification — attempt batch + IDE bridge; report full Verification block"
audience: agent
loaded_by: always
slices_via: none
description: Agent-led Unity/bridge verification — attempt batch + IDE bridge; report full Verification block
alwaysApply: true
---

# Agent-led verification (Territory Developer)

**Canonical policy:** [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md). **Read it; do not restate it here.** This stub exists only to surface the rule via Cursor `alwaysApply` and to point at the canonical doc + the two execution skills: [`agent-test-mode-verify`](../skills/agent-test-mode-verify/SKILL.md) and [`ide-bridge-evidence`](../skills/ide-bridge-evidence/SKILL.md). Verification block format, bridge timeout (40 s initial, escalation on timeout, 120 s ceiling), Path A project-lock release, and Path B preflight all live in the canonical doc.
