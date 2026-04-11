---
description: Run the canonical agent-led Verification block on current branch state. Dispatches the `verifier` subagent; output is formatted by the `verification-report` output style (JSON header + caveman markdown summary).
---

# /verify — dispatch `verifier` subagent

Use the **`verifier`** subagent (defined in `.claude/agents/verifier.md`) to run the canonical Verification block per `docs/agent-led-verification-policy.md`. The output is formatted by the `verification-report` output style at `.claude/output-styles/verification-report.md`.

## Subagent prompt (forward verbatim)

Forward the following prompt to the subagent via the Agent tool with `subagent_type: "verifier"`:

> Follow `caveman:caveman` skill rules for the human-readable summary that follows the JSON Verification block header (drop articles/filler/pleasantries/hedging; fragments OK; pattern `[thing] [action] [reason]. [next step].`). Standard exceptions apply: code, commits, security/auth content, verbatim error/tool output, and the **structured JSON Verification header itself** (which must parse as JSON and is exempt from caveman). Project anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run the canonical agent-led Verification block on current branch state. Output: a Verification block conforming to `.claude/output-styles/verification-report.md` (JSON header + caveman markdown summary). Do **not** restate the verification policy — point at `docs/agent-led-verification-policy.md`.
>
> ## Execution sequence
>
> 1. **Node / IA** — `npm run validate:all`. Capture exit code.
> 2. **Unity compile** — `npm run unity:compile-check` when `Assets/**/*.cs` touched on the branch. Capture exit code; N/A with reason otherwise.
> 3. **Bridge preflight** — `npm run db:bridge-preflight` before any `unity_bridge_command` call.
> 4. **Path A** — `npm run unity:testmode-batch -- --quit-editor-first --scenario-id {SCENARIO}` when §7b calls for test mode batch evidence. Always pass `--quit-editor-first` if a Unity Editor might hold `REPO_ROOT`.
> 5. **Path B** — `mcp__territory-ia__unity_bridge_command` with `timeout_ms: 40000` initial. On timeout, follow the timeout escalation protocol in the policy doc (do **not** restate it).
>
> If a step is N/A, state **why** in the JSON `reason` field. Do not omit rows.
>
> ## Hard boundaries
>
> - Do NOT restate verification policy timeout escalation, Path A lock release, or Path B preflight inside the response. `docs/agent-led-verification-policy.md` is the single canonical source.
> - Do NOT modify code. This subagent runs commands and reports.
> - Do NOT skip Path B "because it might be slow". Wait for Unity per the policy.
> - Do NOT bypass failures with `--no-verify`. Diagnose root cause and report it.
> - Do NOT compress the JSON header with caveman. JSON parses; fragments do not.
>
> ## Output
>
> Single Verification block per `.claude/output-styles/verification-report.md`: fenced JSON header first (`{validate_all, compile, batch, bridge}`), then caveman markdown summary. No prose preamble before the JSON.
