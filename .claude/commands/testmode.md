---
description: Run an agent-led test mode batch scenario loop. Dispatches the `test-mode-loop` subagent for the given scenario id (Path A batchmode or Path B bridge hybrid).
argument-hint: "{SCENARIO_ID} (e.g. reference-flat-32x32)"
---

# /testmode — dispatch `test-mode-loop` subagent

Use the **`test-mode-loop`** subagent (defined in `.claude/agents/test-mode-loop.md`) to run an agent-led test mode batch scenario loop for `$ARGUMENTS`.

## Subagent prompt (forward verbatim)

Forward the following prompt to the subagent via the Agent tool with `subagent_type: "test-mode-loop"`:

> Follow `caveman:caveman` skill rules for all responses (drop articles/filler/pleasantries/hedging; fragments OK; pattern `[thing] [action] [reason]. [next step].`). Standard exceptions apply: code, commits, security/auth content, verbatim error/tool output, structured JSON / batch report contents (`tools/reports/agent-testmode-batch-*.json`), MCP `unity_bridge_command` JSON inputs/outputs, destructive-op confirmations. Project anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run the `agent-test-mode-verify` skill (`ia/skills/agent-test-mode-verify/SKILL.md`) end-to-end for scenario id `$ARGUMENTS`. Pick Path A (Agent test mode batch) or Path B (IDE agent bridge hybrid) per the gate in the skill. Bounded iteration with compile gate. Structured handoff for the human normal-game QA gate.
>
> ## Path A — project lock release
>
> Always pass `--quit-editor-first` if a Unity Editor might hold `REPO_ROOT`:
>
> ```
> npm run unity:testmode-batch -- --quit-editor-first --scenario-id $ARGUMENTS
> ```
>
> Otherwise the second Unity process aborts with "another Unity instance is running" (often exit 134).
>
> ## Path B — bridge timeout
>
> Initial `mcp__territory-ia__unity_bridge_command` call uses `timeout_ms: 40000`. On timeout, follow the **timeout escalation protocol** in `docs/agent-led-verification-policy.md`. Do **not** restate the protocol here.
>
> ## Bounded iteration
>
> Two iterations max per scenario unless the user explicitly extends. Stop on compile failure that cannot be fixed in scope, repeated bridge timeout after one escalation retry (60 s ceiling), Postgres `db_unconfigured` (Path B only) → fall back to Path A or stop, or user-explicit stop signal.
>
> ## Hard boundaries
>
> - Do NOT replace human normal-game QA. This loop produces evidence; the human verifies the issue per `AGENTS.md`.
> - Do NOT bypass `--quit-editor-first` "to be quick". The second Unity process will fail.
> - Do NOT restate verification policy. Point at `docs/agent-led-verification-policy.md`.
> - Do NOT modify scenarios under `tools/fixtures/scenarios/` to make a failing run pass.
> - Do NOT escalate `timeout_ms` past 60 s.
>
> ## Output
>
> Single concise caveman report: path chosen + reason, scenario id, project lock state, iteration count, exit code + report path / `command_id`, compile gate result, handoff for human QA.
