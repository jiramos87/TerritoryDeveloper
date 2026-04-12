---
description: Run the canonical agent-led Verification block on current branch state. Dispatches the `verifier` subagent; output is formatted by the `verification-report` output style (JSON header + caveman markdown summary).
---

# /verify — dispatch `verifier` subagent

Use `verifier` subagent (`.claude/agents/verifier.md`) for canonical Verification block per `docs/agent-led-verification-policy.md`. Output format: `verification-report` style at `.claude/output-styles/verification-report.md`.

## Subagent prompt (forward verbatim)

Forward via Agent tool with `subagent_type: "verifier"`:

> Follow `caveman:caveman` for summary after JSON Verification block header. Standard exceptions: code, commits, security/auth, verbatim error/tool output, **structured JSON Verification header** (must parse as JSON, exempt from caveman). Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run canonical agent-led Verification block on current branch. Output per `.claude/output-styles/verification-report.md` (JSON header + caveman summary). Do NOT restate verification policy — point at `docs/agent-led-verification-policy.md`.
>
> ## Execution sequence
>
> 1. **Node/IA** — `npm run validate:all`. Capture exit code.
> 2. **Unity compile** — `npm run unity:compile-check` when `Assets/**/*.cs` touched. Capture exit code; N/A with reason otherwise.
> 3. **Bridge preflight** — `npm run db:bridge-preflight` before any `unity_bridge_command`.
> 4. **Path A** — `npm run unity:testmode-batch -- --quit-editor-first --scenario-id {SCENARIO}` when §7b needs test mode batch evidence. Always `--quit-editor-first` if Editor might hold `REPO_ROOT`.
> 5. **Path B** — `mcp__territory-ia__unity_bridge_command` with `timeout_ms: 40000` initial. On timeout, follow escalation protocol in policy doc (no restate).
>
> N/A step → state why in JSON `reason`. Do not omit rows.
>
> ## Hard boundaries
>
> - Do NOT restate verification policy (timeout escalation, Path A lock release, Path B preflight). `docs/agent-led-verification-policy.md` is single canonical source.
> - Do NOT modify code. Runs commands + reports.
> - Do NOT skip Path B "because slow". Wait for Unity per policy.
> - Do NOT bypass failures with `--no-verify`. Diagnose root cause, report.
> - Do NOT compress JSON header with caveman. JSON parses; fragments don't.
>
> ## Output
>
> Single Verification block per `.claude/output-styles/verification-report.md`: fenced JSON header first (`{validate_all, compile, batch, bridge}`), then caveman summary. No prose preamble before JSON.
