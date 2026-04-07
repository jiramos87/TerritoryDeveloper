---
name: close-dev-loop
description: >
  Orchestrates agent-driven fix → verify cycle: Play Mode baseline and post-fix debug_context_bundle at
  seed cells, compile gate (get_compilation_status / unity_compile, npm run unity:compile-check, or
  get_console_logs), diff anomaly counts, structured verdict. Requires Postgres agent_bridge_job (0008),
  DATABASE_URL, Unity Editor on REPO_ROOT, shipped IDE agent bridge kinds. Triggers: "close dev loop",
  "verify fix in play mode", "agent-driven QA", "closed-loop verification".
---

# Close Dev Loop — fix → verify → report (IDE agent bridge)

This skill is the **end-to-end** recipe for **visual / terrain** bugs where the agent can compare **before** and **after** using **`debug_context_bundle`** (**Moore** export + screenshot + console + **`bundle.anomalies`**). **Canonical IA:** glossary **IDE agent bridge**, **unity-development-context** §10, [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md).

**Related:** **[`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md)** (one-off logs/screenshots). **[`project-spec-implement`](../project-spec-implement/SKILL.md)** (optional: run this recipe after a phase that changes **Play Mode** visuals). **Optional Step 0:** when **`.cursor/skills/bridge-environment-preflight/SKILL.md`** exists, use it for **Postgres** / **`agent_bridge_job`** checks before the bridge loop; otherwise confirm **`DATABASE_URL`** and **`npm run db:migrate`** per [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md). **Normative tool names:** **territory-ia** **`unity_bridge_command`**, **`unity_compile`**, **`unity_bridge_get`**, **`backlog_issue`**, **`router_for_task`**, **`spec_section`**.

## Prerequisites (all required for the bridge path)

| Requirement | Notes |
|-------------|--------|
| **`DATABASE_URL`** or **`config/postgres-dev.json`** | Same as **Editor export registry** |
| Migration **`0008_agent_bridge_job.sql`** | `npm run db:migrate` — [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) |
| **Unity Editor** open on **repository root** | **`AgentBridgeCommandRunner`** polls dequeue |
| **territory-ia** MCP with **`unity_bridge_command`** | Cursor **Agent** mode |

## Parameterize (replace before running)

| Placeholder | Meaning |
|-------------|---------|
| **`{ISSUE_ID}`** | **`BUG-` / `FEAT-` / `TECH-`** id from **`backlog_issue`** |
| **`{SEED_CELLS}`** | 1–3 **`"x,y"`** strings from repro steps (e.g. **`"62,0"`**, **`"93,0"`**) |
| **`{MAX_ITERATIONS}`** | Auto fix cycles before escalating (default **2**, per project spec) |

## Tool recipe (territory-ia) — execution order

1. **CONTEXT** — **`backlog_issue`** with **`issue_id`:** **`{ISSUE_ID}`** → **`router_for_task`** / **`spec_section`** as needed for the bug domain.
2. **IDENTIFY REPRO CELLS** — From backlog **Notes** / project spec, set **`{SEED_CELLS}`** (1–3 **Moore** centers).
3. **BASELINE CAPTURE**
   - **`unity_bridge_command`** **`kind`:** **`enter_play_mode`** → poll **`get_play_mode_status`** until **`play_mode_ready`** and **`ready: true`** if needed.
   - For each cell in **`{SEED_CELLS}`:** **`unity_bridge_command`** **`kind`:** **`debug_context_bundle`**, **`seed_cell`:** **`"x,y"`** — store **`response.bundle`** ( **`anomaly_count`**, **`anomalies`**, **`cell_export`**, screenshot path, console summary).
   - **`unity_bridge_command`** **`kind`:** **`exit_play_mode`**.
4. **IMPLEMENT FIX** — Edit **C#** / assets per analysis (**English** comments and logs).
5. **COMPILE GATE** — After **C#** edits, **do not** call **`enter_play_mode`** until compilation is acceptable. Preference order (first that applies):
   - **a.** **`unity_bridge_command`** **`kind`:** **`get_compilation_status`** or **`unity_compile`** (same payload; **`unity_compile`** is a thin MCP alias) when the **Editor** is open for the bridge — read **`response.compilation_status`** (**`compiling`**, **`compilation_failed`**, **`last_error_excerpt`**, **`recent_error_messages`**). If **`compiling`** is true, wait and poll again (bounded retries, e.g. 5–8 attempts, ~2–3 s apart) up to **`timeout_ms`**.
   - **b.** If **`UNITY_EDITOR_PATH`** is set **and** no **Editor** holds a **lock** on this **projectPath**, run from repo root: **`npm run unity:compile-check`** (Unity **`-batchmode -nographics -quit`**). **Never** run this while the **Editor** has the same project open.
   - **c.** **`unity_bridge_command`** **`kind`:** **`get_console_logs`** — look for **`error CS`** / compiler errors; optional success cues (Unity-version-specific, e.g. **`Compilation`** / **`Reload`** phrases in **log** lines) are **heuristic**.
   - **d.** Short bounded wait (10–20 s), then repeat **c** if still ambiguous.
   - **e.** On confirmed compile errors → return to step 4, then repeat step 5.
6. **POST-FIX CAPTURE** — Same as step 3 (**`enter_play_mode`** → per-cell **`debug_context_bundle`** → **`exit_play_mode`**).
7. **DIFF** — Per seed cell: **`anomaly_count`** delta; **`anomalies`** added/removed; **height** / child-name hints from export JSON if present; screenshot **paths**.
8. **VERDICT** — Structured summary for the developer (before/after counts, remaining **`anomalies`**, screenshot **paths**).
9. **ITERATE** — If **`anomalies`** remain and the cause is clear, go to step 4. Stop after **`{MAX_ITERATIONS}`** (default **2**) and escalate.
10. **HANDOFF** — Human approves or requests changes.

## Compile gate notes

- **`get_compilation_status`** reflects **`EditorApplication.isCompiling`**, **`EditorUtility.scriptCompilationFailed`**, and recent **error**-severity lines from **`AgentBridgeConsoleBuffer`** (cleared on script domain reload).
- **`npm run unity:compile-check`** writes **`tools/reports/unity-compile-check-*.log`**; exit non-zero on failure. Requires **`UNITY_EDITOR_PATH`** to the **Unity** binary (see **`ProjectSettings/ProjectVersion.txt`** for version). Example macOS: **`…/Unity.app/Contents/MacOS/Unity`**.

## Seed prompt (parameterize)

```markdown
Run the close-dev-loop workflow for issue {ISSUE_ID} with seed cells {SEED_CELLS}.
Follow .cursor/skills/close-dev-loop/SKILL.md: territory-ia bridge commands, compile gate order, max {MAX_ITERATIONS} fix iterations.
```

## Step 0 — environment preflight

**Before step 3**, run [**`bridge-environment-preflight`**](../bridge-environment-preflight/SKILL.md) or its equivalent:

```
npm run db:bridge-preflight
```

- **Exit 0** → proceed to step 1 (CONTEXT).
- **Exit 1** (no URL) → report to developer; do not retry.
- **Exit 2** (server down) → `npm run db:setup-local` once → re-run preflight.
- **Exit 3** (table missing) → `npm run db:migrate` once → re-run preflight.
- **Exit 4** (SQL error) → report code + stderr; do not retry.
- Still failing after one repair → report and escalate; do not loop.

See [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) (**Bridge environment preflight**) for URL resolution and Unity vs MCP alignment notes.
