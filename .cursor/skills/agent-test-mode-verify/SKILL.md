---
name: agent-test-mode-verify
description: >
  Run after project-spec-implement (or standalone) when agent-led test mode verification is required:
  scenario selection or ad-hoc artifact, Unity batchmode and/or IDE bridge hybrid, exports under tools/reports/,
  bounded iterate, then hand off to human for normal-game QA. Triggers: "agent test mode loop", "verify in test mode
  without opening Unity", "batchmode scenario check", "post-implement Play Mode suite". **Status:** implementation
  plan — **TECH-31** program stage **31a3** — [`projects/TECH-31a3-agent-test-mode-verify-skill.md`](../../../projects/TECH-31a3-agent-test-mode-verify-skill.md).
---

# Agent test-mode verification loop

**Normative implementation plan:** [`projects/TECH-31a3-agent-test-mode-verify-skill.md`](../../../projects/TECH-31a3-agent-test-mode-verify-skill.md) (**TECH-31** program stage **31a3** — **BACKLOG** anchor **[`TECH-31`](../../../BACKLOG.md)**; complete the **Implementation Plan** there and confirm verification before treating the skill as **done**).

**Related:** **[`project-spec-implement`](../project-spec-implement/SKILL.md)** (optional **phase exit**); **[`close-dev-loop`](../close-dev-loop/SKILL.md)** (**Moore** **before/after** — **compose**, do not duplicate); **[`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md)**; **[`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md)**; **[`project-implementation-validation`](../project-implementation-validation/SKILL.md)** (**`validate:all`**). **Batch tooling prerequisite:** [`projects/TECH-31a2-batch-testmode-tooling.md`](../../../projects/TECH-31a2-batch-testmode-tooling.md). **Program tracker:** [`projects/TECH-31-agent-scenario-generator-program.md`](../../../projects/TECH-31-agent-scenario-generator-program.md).

## Status (for agents)

Until **31a3** is implemented per [`projects/TECH-31a3-agent-test-mode-verify-skill.md`](../../../projects/TECH-31a3-agent-test-mode-verify-skill.md), treat this file as a **pointer** only: follow **`ide-bridge-evidence`** + **`.queued-test-scenario-id`** ([`tools/fixtures/scenarios/README.md`](../../../tools/fixtures/scenarios/README.md)) for partial automation.

## Intended tool recipe (to be finalized in TECH-31a3)

1. **Gate** — Run only if **§7b** / **§8** or touched domains warrant **Load pipeline** / **test mode** / in-game checks; else **skip** and document.
2. **Node** — `npm run validate:all` when the diff warrants it; **`npm run unity:compile-check`** when **C#** changed (or bridge **compile gate** per **`close-dev-loop`**).
3. **Scenario** — **v1:** committed **fixture** or **`tools/fixtures/scenarios/agent-generated/`** JSON; **v2:** **scenario builder** (**program** stage **31b**).
4. **Path A (batch)** — Shell: best-effort **quit** Unity → **Unity** **`-batchmode`** **`-executeMethod`** … → read **`tools/reports/`** artifact.
5. **Path B (hybrid)** — Write **`.queued-test-scenario-id`** → **`npm run db:bridge-preflight`** → **`unity_bridge_command`** **`enter_play_mode`** → **`debug_context_bundle`** / logs → **`exit_play_mode`**.
6. **Iterate** — Fix → repeat steps 2–5 up to **`{MAX_ITERATIONS}`** (default **2**, align **`close-dev-loop`**).
7. **Handoff** — English verdict + artifact paths; request **human** **normal** **QA** (**no** **test mode** flags).

## Seed prompt (parameterize)

```markdown
Run the agent-test-mode-verify workflow for the completed spec work.
Follow .cursor/skills/agent-test-mode-verify/SKILL.md and implement any missing pieces per projects/TECH-31a3-agent-test-mode-verify-skill.md.
Use territory-ia bridge tools when Path B applies; use batch shell when Path A is available.
Max iterations: {MAX_ITERATIONS}.
```
