import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

async function find(target_path: string) {
  const r: any = await runUnityBridgeCommand({ kind: "find_gameobject", target_path, timeout_ms: 15000 });
  console.log(`find ${target_path}:`, JSON.stringify({ ok: r?.ok, mutation_result: r?.snapshot?.mutation_result, msg: r?.message }, null, 2));
  return r;
}

await find("CityStats");
await find("Game Managers/CityStats");
await find("EconomyManager");
await find("Game Managers/EconomyManager");
await find("TimeManager");
await find("Game Managers/TimeManager");
await find("Game Managers/hud-bar");
await find("Game Managers/hud-bar/segmented-readout");
await find("Game Managers/hud-bar/vu-meter");
await find("Game Managers/hud-bar/illuminated-button");
