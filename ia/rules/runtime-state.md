---
purpose: "Harness-agnostic runtime state — verify / bridge / queued scenario"
audience: agent
loaded_by: on-demand
slices_via: none
---

# Runtime state (`ia/state/runtime-state.json`)

Cross-harness shared JSON at **`ia/state/runtime-state.json`**. Per clone / worktree; **not** shared via git (file is gitignored). Committed contract: **`tools/schemas/runtime-state.schema.json`**, example shape **`ia/state/runtime-state.example.json`**.

## Fields

- `last_verify_exit_code` — last `npm run verify:local` exit; `-1` = unknown.
- `last_bridge_preflight_exit_code` — last `db:bridge-preflight` exit; `-1` = unknown.
- `queued_test_scenario_id` — Path B test-mode scenario id or `null`.
- `updated_at` — ISO-8601 UTC; set on every write.

**Not in this file:** `active_task_id` / `active_stage` — use **`.claude/active-session.json`** or **`.cursor/active-session.json`** (gitignored; per harness).

## Lock + writes

Mutations use **`ia/state/.runtime-state.lock`** with `flock` (Guardrail 3 — distinct lockfile per domain). Bash path: **`tools/scripts/runtime-state-write.sh`**. MCP path: **`runtime_state`** with `action: write` + `patch` (merge).

**Writer policy:** Skills/agents with MCP → prefer **`runtime_state`**. Hooks/scripts without MCP → file + lock only. Cursor has no SessionStart writer; stale state in Cursor-only sessions is acceptable — read via MCP or file.

## Read

Prefer **`mcp__territory-ia__runtime_state`** (`action: read`). Fallback: read JSON file if present; missing file → treat as unknown / not run per field semantics.
