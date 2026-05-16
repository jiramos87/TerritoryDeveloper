---
purpose: "Agent-led Unity/bridge verification — attempt batch + IDE bridge; report full Verification block"
audience: agent
loaded_by: always
slices_via: none
description: Agent-led Unity/bridge verification — attempt batch + IDE bridge; report full Verification block
alwaysApply: true
---

# Agent-led verification (Territory Developer)

**Canonical policy:** [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md). **Read; do not restate here.** Stub surfaces rule via Cursor `alwaysApply` + points at canonical doc + two execution skills: [`agent-test-mode-verify`](../skills/agent-test-mode-verify/SKILL.md) + [`ide-bridge-evidence`](../skills/ide-bridge-evidence/SKILL.md). Verification block format, bridge timeout (40 s initial, escalation on timeout, 120 s ceiling), Path A project-lock release, Path B preflight → canonical doc.
