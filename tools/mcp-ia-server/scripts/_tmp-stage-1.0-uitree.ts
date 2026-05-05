import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";
loadRepoDotenvIfNotCi(resolveRepoRoot());
const res: any = await runUnityBridgeCommand({ kind: "ui_tree_walk", root_path: "Canvas (Game UI)", timeout_ms: 60000 } as any);
console.log(JSON.stringify(res?.response?.ui_tree_walk_result, null, 2).slice(0, 4000));
