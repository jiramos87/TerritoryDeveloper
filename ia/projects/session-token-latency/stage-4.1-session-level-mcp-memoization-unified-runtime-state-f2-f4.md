### Stage 4.1 — Session-level MCP memoization + unified runtime state (F2 + F4)


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Wire PostToolUse hook writing `{tool_name, args_hash, result_hash, ts}` to `.claude/tool-usage.jsonl` (F2). Enable subagent dispatch to read that file and skip re-calls within Stage window. Fully migrate scattered flat-file state markers to the unified `ia/state/runtime-state.json` schema (F4, building on Stage 3.1 skeleton).

**Exit:**

- `.claude/tool-usage.jsonl` written per tool call via PostToolUse hook; fields: `tool_name`, `args_hash` (sha256 of serialized args), `result_hash` (sha256 of result), `ts`, `session_id`.
- `.claude/tool-usage.jsonl` gitignored (session-ephemeral).
- `spec-implementer.md` + `design-explore.md` preambles: read `.claude/tool-usage.jsonl` for session; skip `glossary_discover` / `router_for_task` re-call when args_hash matches within Stage window.
- `ia/state/runtime-state.json`: `last_verify_exit_code`, `last_bridge_preflight_exit_code`, `queued_test_scenario_id` fields populated by hooks; old flat-file markers (`.claude/last-verify-exit-code`, etc.) deleted.
- `verify-loop` + `bridge-environment-preflight` skills write exit codes to `runtime-state.json` via `jq` (reuses D3 `jq` dep from Theme-0-r1 D3 issue).
- `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T4.1.1 | Tool-usage PostToolUse hook | _pending_ | _pending_ | **scope: claude-code-only** — Extend `.claude/settings.json` PostToolUse hook (or add second hook entry): run `tools/scripts/agent-telemetry/tool-usage-hook.sh`. Author that script: reads tool name + args + result from hook env; computes `args_hash = sha256(tool_name + sorted_args_json)`, `result_hash = sha256(result_json)`; appends JSON line to `.claude/tool-usage.jsonl`. Add `.claude/tool-usage.jsonl` to `.gitignore`. |
| T4.1.2 | Subagent memoization read path | _pending_ | _pending_ | In `spec-implementer.md` + `design-explore.md` preamble: add "Session-window memoization check" block: before `glossary_discover` / `router_for_task` calls, compute args_hash; check `.claude/tool-usage.jsonl` for matching `{tool_name, args_hash}` within same `session_id`; if found, use cached `result_hash` lookup from a companion `.claude/tool-usage-cache.json` (key: args_hash → result). Skip live MCP call. Author `tools/scripts/agent-telemetry/cache-lookup.sh {tool_name} {args_hash}` returning result or exit 1 on miss. |
| T4.1.3 | Unified runtime-state migration | _pending_ | _pending_ | All write paths → `ia/state/runtime-state.json`. MCP `runtime_state` write path (patch + lockfile). Skills (`verify-loop`, `bridge-environment-preflight`, `agent-test-mode-verify`) write via MCP where available; `tools/scripts/runtime-state-write.sh` + `jq` fallback documented. |
| T4.1.4 | Flat-file marker cleanup | _pending_ | _pending_ | After migration verified: delete old flat-file markers (`.claude/last-verify-exit-code`, `.claude/last-bridge-preflight-exit-code`, root `/.queued-test-scenario-id` if present). Exit criteria: no references to legacy marker paths remain (grep check). SessionStart preamble reads `last_verify_exit_code` from `ia/state/runtime-state.json`. `npm run validate:all` green. |
| T4.1.5 | Harness-agnostic surfacing (F4b) | _pending_ | _pending_ | `ia/rules/runtime-state.md`, `AGENTS.md` / `CLAUDE.md` / `ia/rules/invariants.md` pointers, `.cursor/rules/runtime-state.mdc`, subagent self-read lines + glossary `runtime-state`; `validate:runtime-state` in `validate:all`; MCP catalog parity with `runtime_state` registration. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
