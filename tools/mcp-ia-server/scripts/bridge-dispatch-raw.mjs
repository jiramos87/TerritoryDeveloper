import { randomUUID } from "crypto";
import pg from "pg";

const { Pool } = pg;
const pool = new Pool({ connectionString: "postgresql://postgres:postgres@localhost:5434/territory_ia_dev" });

async function call(kind, params = {}, timeoutMs = 30000) {
  const commandId = randomUUID();
  const request = {
    schema_version: 1,
    artifact: "unity_agent_bridge_command",
    command_id: commandId,
    requested_at_utc: new Date().toISOString(),
    kind,
    agent_id: "claude-pilot",
    params,
  };
  await pool.query(
    `INSERT INTO agent_bridge_job (command_id, kind, status, request, agent_id) VALUES ($1, $2, 'pending', $3, $4)`,
    [commandId, kind, JSON.stringify(request), "claude-pilot"]
  );
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    const { rows } = await pool.query(`SELECT status, response, error FROM agent_bridge_job WHERE command_id=$1::uuid`, [commandId]);
    if (rows[0]?.status === "completed") return { ok: true, response: rows[0].response };
    if (rows[0]?.status === "failed") return { ok: false, error: rows[0].error };
    await new Promise(r => setTimeout(r, 500));
  }
  return { ok: false, error: "timeout" };
}

const sleep = ms => new Promise(r => setTimeout(r, ms));

async function main() {
  // probe
  let st = await call("get_play_mode_status", {});
  console.log("[status]", JSON.stringify({ state: st.response?.play_mode_state, ready: st.response?.ready }));

  console.log("# stats.open");
  let r = await call("dispatch_action", { action_id: "stats.open" });
  console.log("[dispatch stats.open]", JSON.stringify({ ok: r.ok, mutation: r.response?.mutation_result, err: r.error }));
  await sleep(1500);

  let s1 = await call("capture_screenshot", { include_ui: true, filename_stem: "stats-iter7-bridge" });
  const stats_path = s1.response?.artifact_paths?.[0];
  console.log("[capture stats]", stats_path);

  r = await call("dispatch_action", { action_id: "stats.close" });
  console.log("[dispatch stats.close]", JSON.stringify({ ok: r.ok, mutation: r.response?.mutation_result }));
  await sleep(800);

  console.log("# budget.open");
  r = await call("dispatch_action", { action_id: "budget.open" });
  console.log("[dispatch budget.open]", JSON.stringify({ ok: r.ok, mutation: r.response?.mutation_result }));
  await sleep(1500);

  let s2 = await call("capture_screenshot", { include_ui: true, filename_stem: "budget-iter1-bridge" });
  const budget_path = s2.response?.artifact_paths?.[0];
  console.log("[capture budget]", budget_path);

  r = await call("dispatch_action", { action_id: "budget.close" });
  console.log("[dispatch budget.close]", JSON.stringify({ ok: r.ok, mutation: r.response?.mutation_result }));

  console.log("\nRESULT", JSON.stringify({ stats_path, budget_path }, null, 2));
  await pool.end();
}
main().catch(e => { console.error(e); process.exit(1); });
