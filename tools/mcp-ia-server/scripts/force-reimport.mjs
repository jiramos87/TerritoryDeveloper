import { randomUUID } from "crypto";
import pg from "pg";
const { Pool } = pg;
const pool = new Pool({ connectionString: "postgresql://postgres:postgres@localhost:5434/territory_ia_dev" });
const id = randomUUID();
await pool.query(
  `INSERT INTO agent_bridge_job (command_id, kind, status, request, agent_id) VALUES ($1, $2, 'pending', $3, $4)`,
  [id, 'execute_menu_item', JSON.stringify({
    schema_version: 1, artifact: "unity_agent_bridge_command", command_id: id,
    requested_at_utc: new Date().toISOString(), kind: 'execute_menu_item', agent_id: "claude-pilot",
    params: { menu_path: "Assets/Reimport All" }
  }), "claude-pilot"]);
const start = Date.now();
while (Date.now() - start < 30000) {
  const { rows } = await pool.query(`SELECT status FROM agent_bridge_job WHERE command_id=$1::uuid`, [id]);
  if (rows[0]?.status === "completed" || rows[0]?.status === "failed") {
    console.log("status:", rows[0].status); break;
  }
  await new Promise(r => setTimeout(r, 1000));
}
await pool.end();
