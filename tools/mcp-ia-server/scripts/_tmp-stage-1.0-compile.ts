import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

async function refresh() {
  return runUnityBridgeCommand({ kind: "refresh_asset_database", timeout_ms: 60000 } as any);
}

async function status() {
  return runUnityBridgeCommand({ kind: "get_compilation_status", timeout_ms: 60000 } as any);
}

await refresh();

let last: any;
for (let i = 0; i < 30; i++) {
  last = await status();
  const r = last as any;
  // Try multiple plausible response shapes
  const cs1 = r?.compilation_status;
  const mr = r?.snapshot?.mutation_result;
  const compiling = cs1?.compiling ?? mr?.compiling ?? r?.compiling;
  const failed = cs1?.compilation_failed ?? mr?.compilation_failed ?? r?.compilation_failed;
  console.log(`poll ${i}: compiling=${compiling} failed=${failed}`);
  if (i === 0) console.log("FULL_FIRST:", JSON.stringify(r, null, 2).slice(0, 2000));
  if (compiling === false) break;
  await new Promise((r) => setTimeout(r, 1500));
}

const r = last as any;
const cs1 = r?.compilation_status;
const mr = r?.snapshot?.mutation_result;
const failed = cs1?.compilation_failed ?? mr?.compilation_failed ?? r?.compilation_failed;
console.log("\nFINAL_FAILED:", failed);
console.log("FINAL_LAST:", JSON.stringify(r, null, 2).slice(0, 3000));
process.exit(failed === true ? 1 : 0);
