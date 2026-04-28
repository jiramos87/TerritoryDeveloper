import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";
loadRepoDotenvIfNotCi(resolveRepoRoot());
const r: any = await runUnityBridgeCommand({ kind: "save_scene", timeout_ms: 30000 });
console.log(JSON.stringify({ ok: r?.ok, mr: r?.snapshot?.mutation_result, msg: r?.message }));
