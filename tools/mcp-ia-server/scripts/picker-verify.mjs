import { randomUUID } from "crypto";
import pg from "pg";
const { Pool } = pg;
const pool = new Pool({ connectionString: "postgresql://postgres:postgres@localhost:5434/territory_ia_dev" });
async function call(kind, params = {}, timeoutMs = 60000) {
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
  await call("enter_play_mode", {}, 180000);
  for (let i = 0; i < 30; i++) {
    await sleep(2000);
    const s = await call("get_play_mode_status");
    if (s.response?.play_mode_state === "play_mode_ready") break;
  }
}
await call("dispatch_action", { action_id: "action.tool-select", payload: "Residential" });
await sleep(1500);
const r = await call("capture_screenshot", { include_ui: true, filename_stem: "picker-verify-residential" });
console.log("residential:", r.response?.artifact_paths?.[0]);
await pool.end();
