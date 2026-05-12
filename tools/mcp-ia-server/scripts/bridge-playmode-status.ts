import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

const r = await runUnityBridgeCommand({ kind: "get_play_mode_status", timeout_ms: 30000 });
console.log(JSON.stringify(r, null, 2));
