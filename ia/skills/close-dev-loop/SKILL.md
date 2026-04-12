---
purpose: "Orchestrates agent-driven fix → verify cycle: Play Mode baseline and post-fix debug_context_bundle at seed cells, compile gate (get_compilation_status / unity_compile, npm run unity:compile-check, or get_console_logs),…"
audience: agent
loaded_by: skill:close-dev-loop
slices_via: none
name: close-dev-loop
description: >
  Orchestrates agent-driven fix → verify cycle: Play Mode baseline and post-fix debug_context_bundle at
  seed cells, compile gate (get_compilation_status / unity_compile, npm run unity:compile-check, or
  get_console_logs), diff anomaly counts, structured verdict. Requires Postgres agent_bridge_job (0008),
  DATABASE_URL, Unity Editor on REPO_ROOT, shipped IDE agent bridge kinds. Triggers: "close dev loop",
  "verify fix in play mode", "agent-driven QA", "closed-loop verification".
---

# Close Dev Loop — fix → verify → report (IDE agent bridge)

Visual/terrain bug recipe. Before/after via `debug_context_bundle` (Moore export + screenshot + console + anomalies). **Canonical:** glossary IDE agent bridge, unity-development-context §10, [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md).

**Timeouts:** `timeout_ms: 40000` initial; on timeout → `npm run unity:ensure-editor` → retry 60 s. Ceiling 120 s. Policy: [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md).

**Related:** [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) · [`project-spec-implement`](../project-spec-implement/SKILL.md) · [`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md) (Step 0).

## Prerequisites

| Requirement | Notes |
|---|---|
| `DATABASE_URL` or `config/postgres-dev.json` | Editor export registry |
| Migration `0008_agent_bridge_job.sql` | `npm run db:migrate` |
| Unity Editor on repo root | Missing → `npm run unity:ensure-editor` |
| territory-ia MCP | Agent mode |

## Parameterize

| Placeholder | Meaning |
|---|---|
| `{ISSUE_ID}` | `BUG-`/`FEAT-`/`TECH-` from `backlog_issue` |
| `{SEED_CELLS}` | 1–3 `"x,y"` from repro steps |
| `{MAX_ITERATIONS}` | Fix cycles before escalating (default 2) |

## Tool recipe (territory-ia) — execution order

1. **CONTEXT** — `backlog_issue` `issue_id: {ISSUE_ID}` → `router_for_task` / `spec_section` as needed.
2. **REPRO CELLS** — From Notes / spec set `{SEED_CELLS}` (1–3 Moore centers).
3. **BASELINE**
   - `unity_bridge_command` `kind: enter_play_mode` → poll `get_play_mode_status` until `play_mode_ready` + `ready: true`.
   - Per cell: `unity_bridge_command` `kind: debug_context_bundle`, `seed_cell: "x,y"` — store `response.bundle` (`anomaly_count`, `anomalies`, `cell_export`, screenshot, console).
   - `unity_bridge_command` `kind: exit_play_mode`.
4. **FIX** — Edit C# / assets. English comments + logs.
5. **COMPILE GATE** — After C# edits, do NOT `enter_play_mode` until compile clean. Preference order:
   - **a.** `unity_bridge_command` `kind: get_compilation_status` or `unity_compile` (alias) when Editor holds the bridge — read `response.compilation_status` (`compiling`, `compilation_failed`, `last_error_excerpt`, `recent_error_messages`). If `compiling`, poll (5–8 attempts, ~2–3 s) up to `timeout_ms`.
   - **b.** No Editor lock on `projectPath` → `npm run unity:compile-check` from repo root (`-batchmode -nographics -quit`). Script sources `.env` / `.env.local`; macOS resolves Hub binary from `ProjectSettings/ProjectVersion.txt`. Never run while Editor has same project open.
   - **c.** `unity_bridge_command` `kind: get_console_logs` — scan `error CS` / compiler errors. Success cues heuristic.
   - **d.** Short wait (10–20 s), repeat **c** if ambiguous.
   - **e.** Confirmed errors → step 4.
6. **POST-FIX** — Repeat step 3.
7. **DIFF** — Per cell: `anomaly_count` delta; added/removed `anomalies`; height/child-name hints from export JSON; screenshot paths.
8. **VERDICT** — Structured summary (before/after counts, remaining anomalies, screenshot paths).
9. **ITERATE** — Anomalies remain + cause clear → step 4. Stop after `{MAX_ITERATIONS}` (default 2), escalate.
10. **HANDOFF** — Human approves or requests changes.

## Compile gate notes

- `get_compilation_status` reflects `EditorApplication.isCompiling`, `EditorUtility.scriptCompilationFailed`, recent error lines from `AgentBridgeConsoleBuffer` (cleared on domain reload).
- `npm run unity:compile-check` writes `tools/reports/unity-compile-check-*.log`; non-zero exit on failure. `UNITY_EDITOR_PATH` in repo-root `.env` or macOS Hub inference. Example: `…/Unity.app/Contents/MacOS/Unity`. Do NOT pre-check `$UNITY_EDITOR_PATH` — script loads dotenv.

## Seed prompt

```markdown
Run the close-dev-loop workflow for issue {ISSUE_ID} with seed cells {SEED_CELLS}.
Follow ia/skills/close-dev-loop/SKILL.md: territory-ia bridge commands, compile gate order, max {MAX_ITERATIONS} fix iterations.
```

## Step 0 — environment preflight

Before step 3, run [`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md) or:

```
npm run db:bridge-preflight
```

- Exit 0 → step 1 (CONTEXT).
- Exit 1 (no URL) → report, no retry.
- Exit 2 (server down) → `npm run db:setup-local` once → re-run.
- Exit 3 (table missing) → `npm run db:migrate` once → re-run.
- Exit 4 (SQL error) → report code + stderr, no retry.
- Still failing after one repair → report, escalate, no loop.

See [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) (Bridge environment preflight) for URL resolution + Unity/MCP alignment.
