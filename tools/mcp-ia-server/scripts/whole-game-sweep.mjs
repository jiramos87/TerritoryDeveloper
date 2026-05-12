import { randomUUID } from "crypto";
import pg from "pg";
const { Pool } = pg;
const pool = new Pool({ connectionString: "postgresql://postgres:postgres@localhost:5434/territory_ia_dev" });

async function call(kind, params = {}, timeoutMs = 90000) {
  const id = randomUUID();
  await pool.query(
    `INSERT INTO agent_bridge_job (command_id, kind, status, request, agent_id) VALUES ($1, $2, 'pending', $3, $4)`,
    [id, kind, JSON.stringify({ schema_version: 1, artifact: "unity_agent_bridge_command", command_id: id, requested_at_utc: new Date().toISOString(), kind, agent_id: "claude-pilot", params }), "claude-pilot"]
  );
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

async function ensurePlayMode() {
  const st = await call("get_play_mode_status");
  if (st.response?.play_mode_state === "play_mode_ready") return;
  console.log("# enter_play_mode");
  await call("enter_play_mode", {}, 180000);
  for (let i = 0; i < 30; i++) {
    await sleep(2000);
    const s = await call("get_play_mode_status");
    if (s.response?.play_mode_state === "play_mode_ready") return;
  }
}

async function shoot(stem) {
  const r = await call("capture_screenshot", { include_ui: true, filename_stem: stem });
  return r.response?.artifact_paths?.[0] ?? null;
}

async function open(actionId, openWait = 1500) {
  await call("dispatch_action", { action_id: actionId });
  await sleep(openWait);
}

async function close(actionId, closeWait = 600) {
  await call("dispatch_action", { action_id: actionId });
  await sleep(closeWait);
}

await ensurePlayMode();
const results = {};

results.hud_baseline = await shoot("rebake-hud-baseline");

await open("stats.open");
results.stats = await shoot("rebake-stats-final");
await close("stats.close");

await open("budget.open");
results.budget = await shoot("rebake-budget-final");
await close("budget.close");

await open("pause.open");
results.pause = await shoot("rebake-pause-final");
await close("pause.resume");

await open("mainmenu.open");
results.main_menu = await shoot("rebake-main-menu");
await open("mainmenu.openSettings", 1200);
results.settings = await shoot("rebake-settings");
await close("mainmenu.back", 800);
await open("mainmenu.openSaveLoad", 1200);
results.save_load = await shoot("rebake-save-load");
await close("mainmenu.back", 800);
await open("mainmenu.openNewGame", 1200);
results.new_game = await shoot("rebake-new-game");
await close("mainmenu.back", 800);
await close("mainmenu.close", 800);

await open("map-panel.open");
results.map_panel = await shoot("rebake-map-panel");
await close("map-panel.close");

await open("info.open");
results.info_panel = await shoot("rebake-info-panel");
await close("info.close");

results.notifications_toast_passive = await shoot("rebake-notifications-toast");

await open("toolbar.tool-select", 800);
results.tool_subtype_picker = await shoot("rebake-tool-subtype-picker");
await close("toolbar.tool-deselect", 400);

results.toolbar = await shoot("rebake-toolbar");

console.log(JSON.stringify(results, null, 2));
await pool.end();
