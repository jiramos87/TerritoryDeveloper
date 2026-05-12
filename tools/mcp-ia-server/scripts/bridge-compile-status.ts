import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());
const r = await runUnityBridgeCommand({ kind: "get_compilation_status" as any, timeout_ms: 30000 } as any);
console.log(JSON.stringify(r, null, 2));
