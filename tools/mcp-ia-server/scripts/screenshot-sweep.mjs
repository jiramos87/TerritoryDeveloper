import { randomUUID } from "crypto";
import pg from "pg";
const { Pool } = pg;
const pool = new Pool({ connectionString: "postgresql://postgres:postgres@localhost:5434/territory_ia_dev" });
async function call(kind, params = {}, timeoutMs = 90000) {
  const id = randomUUID();
  await pool.query(`INSERT INTO agent_bridge_job (command_id, kind, status, request, agent_id) VALUES ($1, $2, 'pending', $3, $4)`,
    [id, kind, JSON.stringify({ schema_version: 1, artifact: "unity_agent_bridge_command", command_id: id, requested_at_utc: new Date().toISOString(), kind, agent_id: "claude-pilot", params }), "claude-pilot"]);
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    const { rows } = await pool.query(`SELECT status, response, error FROM agent_bridge_job WHERE command_id=$1::uuid`, [id]);
    if (rows[0]?.status === "completed") return { ok: true, response: rows[0].response };
    if (rows[0]?.status === "failed") return { ok: false, error: rows[0].error };
    await new Promise(r => setTimeout(r, 500));
  }
  return { ok: false, error: "timeout" };
}
const sleep = ms => new Promise(r => setTimeout(r, ms));

let st = await call("get_play_mode_status");
if (st.response?.play_mode_state !== "play_mode_ready") {
  console.log("# enter_play_mode");
  await call("enter_play_mode", {}, 180000);
  for (let i = 0; i < 30; i++) {
    await sleep(2000);
    const s = await call("get_play_mode_status");
    if (s.response?.play_mode_state === "play_mode_ready") break;
  }
}

const results = {};
// Baseline: HUD only (no modal)
const baseline = await call("capture_screenshot", { include_ui: true, filename_stem: "rebake-hud-baseline" });
results.hud_baseline = baseline.response?.artifact_paths?.[0];

// Stats
await call("dispatch_action", { action_id: "stats.open" });
await sleep(1500);
let r = await call("capture_screenshot", { include_ui: true, filename_stem: "rebake-stats-final" });
results.stats = r.response?.artifact_paths?.[0];
await call("dispatch_action", { action_id: "stats.close" });
await sleep(600);

// Budget
await call("dispatch_action", { action_id: "budget.open" });
await sleep(1500);
r = await call("capture_screenshot", { include_ui: true, filename_stem: "rebake-budget-final" });
results.budget = r.response?.artifact_paths?.[0];
await call("dispatch_action", { action_id: "budget.close" });
await sleep(600);

// Pause menu
await call("dispatch_action", { action_id: "pause.open" });
await sleep(1500);
r = await call("capture_screenshot", { include_ui: true, filename_stem: "rebake-pause-final" });
results.pause = r.response?.artifact_paths?.[0];
await call("dispatch_action", { action_id: "pause.close" });
await sleep(500);

console.log(JSON.stringify(results, null, 2));
await pool.end();
