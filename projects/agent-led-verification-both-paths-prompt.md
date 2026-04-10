# Agent prompt: Agent-led verification (both paths, autonomous)

Copy everything below the line into a new **Agent** chat (tools enabled, **territory-ia** MCP + terminal). Goal: run **Path A** and **Path B** without asking the human to click Unity or close windows—use documented flags and scripts only.

---

## Task

Verify the current branch/worktree using **both** agent-led verification paths and report a full **Verification** block per [`docs/agent-led-verification-policy.md`](../docs/agent-led-verification-policy.md).

### Non-negotiables (read first)

1. **Project lock:** Batch Unity and the Unity Editor **cannot** open the same `REPO_ROOT` at once. **Path A must release the lock before batch starts.** Do **not** ask the human to quit Unity manually unless scripts fail.
   - **Always** run Path A as:  
     `npm run unity:testmode-batch -- --quit-editor-first --scenario-id reference-flat-32x32`  
     (see policy **Path A — project lock** and [`.cursor/skills/agent-test-mode-verify/SKILL.md`](../.cursor/skills/agent-test-mode-verify/SKILL.md).)
2. **Order when running both paths:** **Path A first** (with `--quit-editor-first`), then **`npm run unity:ensure-editor`** (macOS; exit 0 = Editor ready) **before Path B**, because Path B needs an Editor on `REPO_ROOT`.
3. **Bridge timeouts:** First bridge calls use **`timeout_ms` 40000**. On **timeout**, follow the **timeout escalation protocol** in the policy (`npm run unity:ensure-editor` → retry same command with **60000**; on second timeout, `npm run db:bridge-preflight` and `get_console_logs` if useful, then escalate).

### Sequence

1. **`npm run validate:all`** (repo root) — report exit code.

2. **`npm run unity:compile-check`** — run and report exit code **only if** any `Assets/**/*.cs` (or Unity scripts under `Assets/`) changed in the working tree vs `HEAD`; otherwise **N/A** + reason.

3. **Path A — Agent test mode batch**  
   ```bash
   npm run unity:testmode-batch -- --quit-editor-first --scenario-id reference-flat-32x32
   ```  
   Report: exit code; path to newest `tools/reports/agent-testmode-batch-*.json` **if created**; summary fields **`ok`** / **`exit_code`**.  
   If no JSON (batch aborted), report the Unity log path printed by the script and the fatal error from the log tail.  
   If exit **3** from `--quit-editor-first`, report `unity-quit-project.sh` outcome and do not claim Path A passed.

4. **Before Path B (macOS):** Run **`npm run unity:ensure-editor`** and report exit code. If **≠ 0**, stop Path B and explain (e.g. not macOS, binary not found). If **0**, proceed.

5. **Path B — IDE agent bridge**  
   a. **`npm run db:bridge-preflight`** — if exit ≠ 0, apply **bridge-environment-preflight** repair policy once (`db:setup-local` for 2, `db:migrate` for 3, etc.) and retry preflight once.  
   b. **`unity_bridge_command`** **`get_play_mode_status`**, **`timeout_ms` 40000**.  
   c. On **timeout:** `npm run unity:ensure-editor`; report exit code. If ≠ 0, stop Path B. If 0, retry **`get_play_mode_status`** with **`timeout_ms` 60000**.  
   d. On **second timeout:** `npm run db:bridge-preflight`, then **`unity_bridge_command`** **`get_console_logs`** if the Editor might still be responsive; summarize and escalate to human.  
   e. If status calls succeed, run (each with **`timeout_ms` 40000** unless you are in the 60s retry step from escalation):  
      - **`enter_play_mode`**  
      - **`get_play_mode_status`**  
      - **`debug_context_bundle`** with **`seed_cell`** `"3,0"`  
      - **`exit_play_mode`**  
   f. **Interpretation:** `enter_play_mode` may return **error** while a later **`get_play_mode_status`** still shows **`play_mode_ready`**. Report both; do not assume the Editor is broken without checking status after.  
   g. For **each** bridge step: record **`command_id`**, **ok / error / timeout**, and for **`debug_context_bundle`** note **`anomaly_count`**, screenshot/export outcomes, and notable console lines in the bundle.

6. **Optional follow-up:** If **`debug_context_bundle`** fails on screenshot, retry once with **`include_screenshot: false`** to isolate screenshot vs export/anomaly scan; mention Game view visibility if the error mentions screenshot timeout.

### Final Verification block (required shape)

Use a markdown table or bullet list including:

- `validate:all` exit code  
- `unity:compile-check` exit code or **N/A** + reason  
- Path A: exit code + JSON path/summary **or** log path + failure reason (note use of **`--quit-editor-first`**)  
- Path B: preflight outcome, whether **`unity:ensure-editor`** ran (before Path B and/or during escalation), every **`timeout_ms`** used, every **`command_id`** + outcome  

If Path B was not executed, state **exactly why** — do not omit that row.

---

## Success criteria for “without human intervention”

- You did **not** ask the user to quit Unity for Path A; you used **`--quit-editor-first`** (or documented equivalent).  
- You brought the Editor back for Path B via **`npm run unity:ensure-editor`** when on macOS.  
- You completed the **Verification** block with concrete exit codes and artifact paths.
