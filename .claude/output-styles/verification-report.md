---
name: Verification report
description: Structured Verification block (JSON header + caveman markdown summary) per docs/agent-led-verification-policy.md. Used by the `verifier` subagent and the `/verify` slash command.
---

You are emitting a **Verification block** per `docs/agent-led-verification-policy.md`. The block is the only thing in your response — no preamble, no closing remarks.

## Output structure (mandatory)

The block has **two parts** in this exact order:

### Part 1 — Fenced JSON header (required)

A single fenced code block tagged `json` containing one object with these keys:

```json
{
  "validate_all": { "exit_code": 0, "skipped": false, "reason": null },
  "compile":      { "exit_code": 0, "skipped": false, "reason": null, "applies": true },
  "batch":        { "exit_code": 0, "ok": true, "report_path": "tools/reports/agent-testmode-batch-{ts}.json", "scenario_id": "{id}", "applies": true, "skipped": false, "reason": null },
  "bridge":       { "outcome": "ok", "command_id": "{id}", "timeout_ms": 40000, "applies": true, "skipped": false, "reason": null }
}
```

Field rules:

- **`exit_code`** — integer; 0 = success.
- **`skipped`** / **`applies`** — boolean. When a check is N/A (no `Assets/**/*.cs` touched, no Postgres, no Editor), set `skipped: true` and put the reason in `reason` (string, one line). `applies: false` when the check is structurally inapplicable (e.g. branch has no C# changes).
- **`compile.applies`** — `false` when no `Assets/**/*.cs` touched on the branch.
- **`batch.applies`** — `false` when `## 7b. Test Contracts` does not call for a test mode batch row.
- **`bridge.outcome`** — one of `"ok"`, `"error"`, `"timeout"`, `"skipped"`. On `"timeout"`, follow the timeout escalation protocol in `docs/agent-led-verification-policy.md` (do **not** restate it).
- **JSON must parse.** No trailing commas, no comments. The JSON is the machine-readable contract — downstream tooling reads it.
- **JSON exempt from caveman.** Field names and values stay in standard JSON; do not compress them.

### Part 2 — Caveman markdown summary (required)

After the JSON code block, emit a short markdown summary in **caveman** voice (drop articles/filler/pleasantries/hedging; fragments OK; pattern `[thing] [action] [reason]. [next step].`). One paragraph or bullet per check row, in the same order as the JSON: `validate_all`, `compile`, `batch`, `bridge`.

Each line: `[check] [exit code or outcome]. [one-line root cause if non-zero or timeout]. [next step if non-trivial].`

Standard caveman exceptions still apply to the summary: code identifiers, commit messages, security/auth content, verbatim error / tool output (paste error lines as-is).

## Examples

### Example — all green

```json
{
  "validate_all": { "exit_code": 0, "skipped": false, "reason": null },
  "compile":      { "exit_code": 0, "skipped": false, "reason": null, "applies": true },
  "batch":        { "exit_code": 0, "ok": true, "report_path": "tools/reports/agent-testmode-batch-2026-04-10T18-00-00Z.json", "scenario_id": "reference-flat-32x32", "applies": true, "skipped": false, "reason": null },
  "bridge":       { "outcome": "ok", "command_id": "abc-123", "timeout_ms": 40000, "applies": true, "skipped": false, "reason": null }
}
```

- validate_all 0. green.
- compile 0. green. C# touched.
- batch 0. ok. report `tools/reports/agent-testmode-batch-2026-04-10T18-00-00Z.json`. scenario `reference-flat-32x32`.
- bridge ok. `command_id` abc-123. 40s.

### Example — compile N/A, bridge timeout

```json
{
  "validate_all": { "exit_code": 0, "skipped": false, "reason": null },
  "compile":      { "exit_code": null, "skipped": true, "reason": "no Assets/**/*.cs touched on branch", "applies": false },
  "batch":        { "exit_code": null, "ok": null, "report_path": null, "scenario_id": null, "applies": false, "skipped": true, "reason": "no §7b test mode batch row" },
  "bridge":       { "outcome": "timeout", "command_id": "xyz-789", "timeout_ms": 60000, "applies": true, "skipped": false, "reason": "second timeout after escalation; preflight ok; Editor responsive" }
}
```

- validate_all 0. green.
- compile N/A. no C# diff.
- batch N/A. no §7b row.
- bridge timeout x2. escalated to 60s per protocol. preflight ok. Editor up. escalate to human.

## Hard rules

- JSON header **first**, summary **second**. Never reverse.
- Never omit a row. N/A rows show `skipped: true` + `reason`.
- Never restate the verification policy timeout escalation, Path A lock release, or Path B preflight in the summary. Point at `docs/agent-led-verification-policy.md` instead.
- Never fabricate exit codes. Run the commands; report what they returned.
- Never compress the JSON header with caveman. JSON parses; caveman fragments do not.
- Never wrap the block in extra prose. The Verification block is the entire response.
