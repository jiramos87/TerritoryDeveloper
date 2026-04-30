import { createRequire } from 'node:module';
const _require = createRequire(import.meta.url);
const { Pool } = _require('../mcp-ia-server/node_modules/pg');
import { randomUUID } from 'node:crypto';
import { readFileSync } from 'node:fs';

const cfg = JSON.parse(readFileSync('config/postgres-dev.json', 'utf8'));
const url = process.env.DATABASE_URL || cfg.database_url;
const pool = new Pool({ connectionString: url });

async function enqueue(kind, params) {
  const cid = randomUUID();
  const env = {
    schema_version: 1,
    artifact: 'unity_agent_bridge_command',
    command_id: cid,
    requested_at_utc: new Date().toISOString(),
    kind,
    agent_id: 'smoke-uifid',
    params,
  };
  await pool.query(
    `INSERT INTO agent_bridge_job (command_id, kind, status, request, agent_id)
     VALUES ($1::uuid, $2, $3, $4::jsonb, $5)`,
    [cid, kind, 'pending', JSON.stringify(env), 'smoke-uifid']
  );
  return cid;
}

async function poll(cid, timeoutMs = 60000) {
  const t0 = Date.now();
  for (;;) {
    const r = await pool.query(
      `SELECT status, response, error FROM agent_bridge_job WHERE command_id = $1::uuid`,
      [cid]
    );
    if (r.rows.length === 0) throw new Error(`row vanished: ${cid}`);
    const row = r.rows[0];
    if (row.status === 'completed' || row.status === 'failed') return row;
    if (Date.now() - t0 > timeoutMs) return { status: 'timeout', response: null, error: null };
    await new Promise((res) => setTimeout(res, 500));
  }
}

const surfaces = [
  { slug: 'pause-menu', prefab: 'Assets/UI/Prefabs/Generated/pause-menu.prefab' },
  { slug: 'info-panel', prefab: 'Assets/UI/Prefabs/Generated/info-panel.prefab' },
];

const irPath = 'web/design-refs/step-1-game-ui/ir.json';
const themeSO = 'Assets/UI/Theme/DefaultUiTheme.asset';

for (const s of surfaces) {
  console.log(`\n=== ${s.slug} ===`);

  // Phase 2: prefab_inspect
  let cid = await enqueue('prefab_inspect', { prefab_path: s.prefab });
  let r = await poll(cid);
  const inspectRoot = r.response?.prefab_inspect_result?.root;
  console.log(`prefab_inspect: status=${r.status} root.name=${inspectRoot?.name ?? '(none)'} components=${r.response?.prefab_inspect_result?.component_count ?? 0} nodes=${r.response?.prefab_inspect_result?.node_count ?? 0}`);
  if (r.status === 'failed') console.log(`  ERR: ${r.error}`);

  // Phase 4: claude_design_conformance
  cid = await enqueue('claude_design_conformance', { ir_path: irPath, theme_so: themeSO, prefab_path: s.prefab, scene_root_path: '' });
  r = await poll(cid);
  const conf = r.response?.claude_design_conformance_result;
  console.log(`conformance: status=${r.status} target=${conf?.target_kind}/${conf?.target_path} rows=${conf?.row_count ?? 0} fails=${conf?.fail_count ?? 0}`);
  if (r.status === 'failed') console.log(`  ERR: ${r.error}`);
  if (conf?.rows?.length > 0) {
    const sample = conf.rows[0];
    console.log(`  sample row keys: ${Object.keys(sample).join(',')}`);
    console.log(`  sample row: ${JSON.stringify(sample)}`);
    const fails = conf.rows.filter((row) => row.pass === false).slice(0, 5);
    console.log(`  top fails (pass=false, ${fails.length}/${conf.fail_count}):`);
    for (const f of fails) console.log(`    - ${f.node_path} ${f.check_kind} sev=${f.severity} pass=${f.pass} msg=${f.message}`);
    const byKind = {};
    const byPass = { true: 0, false: 0 };
    const bySev = {};
    for (const row of conf.rows) {
      byKind[row.check_kind] = (byKind[row.check_kind] || 0) + 1;
      byPass[row.pass] = (byPass[row.pass] || 0) + 1;
      bySev[row.severity] = (bySev[row.severity] || 0) + 1;
    }
    console.log(`  by check_kind: ${JSON.stringify(byKind)}`);
    console.log(`  by pass: ${JSON.stringify(byPass)}`);
    console.log(`  by severity: ${JSON.stringify(bySev)}`);
  }
}

await pool.end();
