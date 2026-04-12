---
description: Run an agent-led test mode batch scenario loop. Dispatches the `test-mode-loop` subagent for the given scenario id (Path A batchmode or Path B bridge hybrid).
argument-hint: "{SCENARIO_ID} (e.g. reference-flat-32x32)"
---

# /testmode — dispatch `test-mode-loop` subagent

Use `test-mode-loop` subagent (`.claude/agents/test-mode-loop.md`) for scenario `$ARGUMENTS`.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "test-mode-loop"`:

> Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured JSON / batch report contents (`tools/reports/agent-testmode-batch-*.json`), MCP `unity_bridge_command` JSON, destructive-op confirmations. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `agent-test-mode-verify` skill (`ia/skills/agent-test-mode-verify/SKILL.md`) end-to-end for scenario `$ARGUMENTS`. Pick Path A or Path B per gate in skill. Bounded iteration with compile gate. Structured handoff for human normal-game QA.
>
> ## Path A — project lock release
>
> Always pass `--quit-editor-first` if Editor might hold `REPO_ROOT`:
>
> ```
> npm run unity:testmode-batch -- --quit-editor-first --scenario-id $ARGUMENTS
> ```
>
> Else second Unity process aborts with "another Unity instance is running" (often exit 134).
>
> ## Path B — bridge timeout
>
> Initial `mcp__territory-ia__unity_bridge_command` uses `timeout_ms: 40000`. On timeout, follow escalation protocol in `docs/agent-led-verification-policy.md`. No restate.
>
> ## Bounded iteration
>
> Max 2 iterations per scenario unless user extends. Stop on compile failure unfixable in scope, repeated bridge timeout after one escalation retry (60 s ceiling), Postgres `db_unconfigured` (Path B) → fall back to Path A or stop, user-explicit stop.
>
> ## Hard boundaries
>
> - Do NOT replace human normal-game QA. Loop produces evidence; human verifies per `AGENTS.md`.
> - Do NOT bypass `--quit-editor-first` "to be quick". Second Unity process fails.
> - Do NOT restate verification policy. Point at `docs/agent-led-verification-policy.md`.
> - Do NOT modify scenarios under `tools/fixtures/scenarios/` to make failing run pass.
> - Do NOT escalate `timeout_ms` past 60 s.
>
> ## Output
>
> Single concise caveman report: path chosen + reason, scenario id, project lock state, iteration count, exit code + report path / `command_id`, compile gate result, handoff for human QA.
