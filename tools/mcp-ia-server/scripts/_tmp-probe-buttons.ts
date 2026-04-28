import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

async function find(target_path: string) {
  const r: any = await runUnityBridgeCommand({ kind: "find_gameobject", target_path, timeout_ms: 15000 });
  console.log(`find ${JSON.stringify(target_path)}:`, JSON.stringify({ ok: r?.ok, msg: r?.message, mr: r?.snapshot?.mutation_result }));
}

await find("Game Managers/hud-bar/illuminated-button");
await find("Game Managers/hud-bar/illuminated-button (1)");
await find("Game Managers/hud-bar/illuminated-button (2)");
await find("Game Managers/hud-bar/illuminated-button (3)");
await find("Game Managers/hud-bar/illuminated-button (4)");
