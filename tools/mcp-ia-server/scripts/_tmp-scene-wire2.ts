import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

async function step(name: string, body: any) {
  console.log(`\n=== ${name} ===`);
  const r: any = await runUnityBridgeCommand({ ...body, timeout_ms: 60000 });
  const ok = r && typeof r === "object" && r.ok === true;
  console.log(JSON.stringify({ ok, mutation_result: r?.snapshot?.mutation_result, error: r?.error, message: r?.message }, null, 2));
  if (!ok) {
    console.error("STEP FAILED:", name);
    console.error(JSON.stringify(r, null, 2));
    process.exit(1);
  }
  return r;
}

// Refresh AssetDatabase (force compile + import) — bridge to pick up HudBarDataAdapter.cs
await step("refresh_asset_database", { kind: "refresh_asset_database" });

// Wait for compilation to finish
for (let i = 0; i < 10; i++) {
  const r: any = await runUnityBridgeCommand({ kind: "get_compilation_status", timeout_ms: 30000 });
  const cs = r?.snapshot?.compilation_status;
  console.log(`  compile_status[${i}] compiling=${cs?.compiling} failed=${cs?.compilation_failed}`);
  if (cs && cs.compiling === false && cs.compilation_failed === false) break;
  if (cs?.compilation_failed) {
    console.error("compilation FAILED:", cs.last_error_excerpt);
    process.exit(1);
  }
  await new Promise(r => setTimeout(r, 2000));
}

await step("attach HudBarDataAdapter", {
  kind: "attach_component",
  target_path: "Game Managers/hud-bar",
  component_type_name: "HudBarDataAdapter",
});
