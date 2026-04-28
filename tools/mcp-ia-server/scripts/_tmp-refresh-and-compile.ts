import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

async function step(name: string, body: any) {
  console.log(`\n=== ${name} ===`);
  const r: any = await runUnityBridgeCommand({ ...body, timeout_ms: 60000 });
  console.log(JSON.stringify({ ok: r?.ok, mr: r?.snapshot?.mutation_result, error: r?.error, message: r?.message }, null, 2));
  return r;
}

await step("refresh_asset_database", { kind: "refresh_asset_database" });
// Wait for compilation to settle
let cs: any;
for (let i = 0; i < 20; i++) {
  cs = await step(`get_compilation_status (poll ${i})`, { kind: "get_compilation_status" });
  const compiling = cs?.snapshot?.mutation_result?.compiling ?? cs?.compiling;
  if (compiling === false) break;
  await new Promise(r => setTimeout(r, 1000));
}
const failed = cs?.snapshot?.mutation_result?.compilation_failed ?? cs?.compilation_failed;
console.log("\nFINAL compilation_failed:", failed);
process.exit(failed === true ? 1 : 0);
