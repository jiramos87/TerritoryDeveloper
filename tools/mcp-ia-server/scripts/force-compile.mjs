import { randomUUID } from "crypto";
import pg from "pg";
const { Pool } = pg;
const pool = new Pool({ connectionString: "postgresql://postgres:postgres@localhost:5434/territory_ia_dev" });
async function call(kind, params = {}, timeoutMs = 120000) {
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
console.log("compile:", JSON.stringify(await call("unity_compile", {}, 240000), null, 2).slice(0, 400));
console.log("status:", JSON.stringify(await call("get_compilation_status"), null, 2).slice(0, 400));
await pool.end();
