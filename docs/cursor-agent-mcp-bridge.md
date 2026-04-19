# Cursor agent — MCP bridge quick guide

**Audience:** Cursor Composer agent working in `territory-developer` repo during Claude Code gap.
**Purpose:** Closed-loop Unity bridge testing via `territory-ia` MCP tools. Read-only / query / compile / bridge command surface only.
**Not purpose:** Editing the MCP server (`tools/mcp-ia-server/**`). Off-limits during gap — collision risk with pending MCP audit Stages 6–16.

---

## 1. Connect to MCP server

Config already committed at `.cursor/mcp.json` (mirrors Claude Code's `.mcp.json`):

```json
{
  "mcpServers": {
    "territory-ia": {
      "command": "tools/mcp-ia-server/node_modules/.bin/tsx",
      "args": ["tools/mcp-ia-server/src/index.ts"],
      "env": { "REPO_ROOT": ".", "DEBUG_MCP_COMPUTE": "1" }
    }
  }
}
```

Cursor auto-loads per-project MCP config on workspace open. Approve the `territory-ia` server on first prompt (one-time). If `command` resolves fail, run `cd tools/mcp-ia-server && npm install` to restore the `tsx` binary at `node_modules/.bin/tsx`.

Tools surface inside Cursor as `territory-ia:{tool_name}` (Cursor namespacing). Example: `territory-ia:unity_bridge_command`, `territory-ia:backlog_issue`. This guide references them as `mcp__territory-ia__{name}` (Claude Code namespacing) — same tool, different display prefix.

Smoke-test after connect: ask Composer to call `territory-ia:list_rules` — returns the rules index. Non-empty list = MCP live.

---

## 2. Preflight (before any bridge call)

1. Postgres running on `localhost:5434` (brew PG14, role `postgres`/`postgres`). No Docker.
2. Unity Editor open on the repo with Play Mode bridge worker active (humans start this — not the agent).
3. Run preflight: `npm run db:bridge-preflight`. Non-zero exit = don't enqueue; tell human to start Postgres / Unity.

---

## 3. Closed-loop pattern

```
unity_bridge_command  →  poll unity_bridge_get  →  read payload.result
```

### 3.1 Enqueue command

`mcp__territory-ia__unity_bridge_command` — inputs:
- `command_name` — e.g. `"findobjectoftype_scan"`, `"invariants_verify"`, `"playmode_load_scenario"`.
- `payload` — JSON object matching the command contract.
- `timeout_ms` — default 60000. Bump to 180000 for Play Mode scenarios.

Returns envelope `{ok: true, payload: {job_id, status: "queued" | "running"}}`.

### 3.2 Poll result

`mcp__territory-ia__unity_bridge_get` with `{job_id}`. Poll every 2–5 s until `status === "succeeded"` or `"failed"`. Envelope:

```
{ ok: true, payload: { job_id, status, result: { ... }, error: null | { code, message } } }
```

Stop polling on `"succeeded"` / `"failed"` / timeout (return error envelope). `result` shape is command-specific — check caller docstring or command registry in `Assets/Scripts/Tools/AgentBridge/Commands/`.

### 3.3 Don't call

- `unity_bridge_lease` — worker side (Unity pulls jobs). Calling from agent breaks FIFO.
- Any tool with `mutate` / `authorship` semantics (`glossary_row_create`, `spec_section_append`, `orchestrator_task_update`, `rollout_tracker_flip`, `backlog_record_upsert`, `rule_create`, `glossary_row_update`). All gated by caller-allowlist in `tools/mcp-ia-server/src/auth/caller-allowlist.ts`; Cursor agent id NOT on allowlist → returns `{ok: false, error: {code: "unauthorized_caller"}}`. Authorship lands via Claude Code lifecycle subagents on return.

---

## 4. Non-bridge tools agent CAN call (read-only)

Bulk-safe. No caller gate. Useful before / after bridge round-trip.

| Tool | Purpose |
|------|---------|
| `backlog_issue` | Structured view of one issue (id + yaml fields). Call when you have `TECH-XXX`. |
| `backlog_list` / `backlog_search` | Filter open / closed issues by status, priority, tag. |
| `router_for_task` | Resolve router hints — returns related specs + rules for a given task intent. |
| `spec_outline` / `spec_section` / `spec_sections` | Slice `ia/specs/*.md` by header. Prefer over full-file read. |
| `list_specs` / `list_rules` | Discover available specs / rules. |
| `rule_content` / `rule_section` | Fetch rule bodies on demand. |
| `invariants_summary` / `invariant_preflight` | Read invariants + pre-implementation checklist. |
| `glossary_discover` / `glossary_lookup` | Translate user wording → glossary canonical terms (English only). |
| `unity_compile` | C# compile gate. Call after edits to `Assets/**/*.cs`. Expect `{ok: true, payload: {errors: [], warnings: [...]}}`. Non-empty `errors` = block commit. |
| `unity_callers_of` / `unity_subscribers_of` / `csharp_class_summary` / `findobjectoftype_scan` | Read-only code graph queries. |
| `project_spec_journal_search` / `project_spec_journal_get` | Lessons learned from closed specs. |
| `master_plan_locate` / `master_plan_next_pending` | Resolve master plan paths + next `_pending_` task. |

---

## 5. Typical blip closed-loop turn

1. `backlog_issue {"id": "TECH-XXX"}` — read spec stub + `router_for_task` hints.
2. `spec_section {"id": "TECH-XXX", "section": "Implementation Plan"}` — load just the plan.
3. Make code edits in `Assets/Scripts/Audio/Blip/` (Cursor Edit tool, not via MCP).
4. `unity_compile` — block on errors.
5. `unity_bridge_command {"command_name": "findobjectoftype_scan", "payload": {"type": "BlipCatalog"}}` — sanity check wiring.
6. Poll `unity_bridge_get` until succeeded.
7. If behavioral test needed: `unity_bridge_command {"command_name": "playmode_load_scenario", "payload": {"scenario_id": "blip-smoke"}}` + `unity_bridge_get`.
8. Commit per task (see `cursor-agent-master-plan-tasks.md` §6).

---

## 6. Error envelope shape (post Stage 3 of MCP audit)

All tools return:

```
{ ok: true, payload: { ... } }
{ ok: false, error: { code: "<machine-code>", message: "...", hint: "..." } }
```

Common codes:
- `unauthorized_caller` — mutation/authorship tool hit without allowlist. Skip; leave for Claude Code.
- `preflight_failed` — Postgres / Unity not up. Stop and notify human.
- `timeout_exceeded` — bridge job ran past `timeout_ms`. Bump timeout or split command.
- `invariant_violation` — `invariant_preflight` flagged an invariant before your change. Fix before commit.

Never catch-swallow — surface error to the loop and stop.

---

## 7. Hard rules

- **Do NOT edit `tools/mcp-ia-server/**`.** Off-limits during gap.
- **Do NOT flip master-plan task Status cells** via any tool. Status moves belong to `/closeout` / `project-stage-close` / `ship-stage` (Claude Code).
- **Do NOT run `validate:dead-project-specs`** as a gate — it expects Claude Code's yaml archive moves. Run `validate:frontmatter` + `validate:all` instead.
- **Postgres = native brew :5434** — never suggest Docker.
- **Unity Editor lives outside agent control.** If bridge timeout hits, ask human to check Editor state.
