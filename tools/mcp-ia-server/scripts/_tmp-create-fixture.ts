import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

async function step(name: string, body: any) {
  console.log(`\n=== ${name} ===`);
  const r: any = await runUnityBridgeCommand({ ...body, timeout_ms: 30000 });
  const ok = r && typeof r === "object" && r.ok === true;
  console.log(JSON.stringify({ ok, mr: r?.snapshot?.mutation_result, error: r?.error, message: r?.message }, null, 2));
  if (!ok) { console.error("STEP FAILED:", name); process.exit(1); }
  return r;
}

const ASSET_PATH = "Assets/Tests/Fixtures/UI/HUD/HudParityFixture.asset";

// Ensure fixtures dir exists (Unity AssetDatabase will create the .meta)
import { mkdirSync } from "node:fs";
import { resolve } from "node:path";
mkdirSync(resolve(resolveRepoRoot(), "Assets/Tests/Fixtures/UI/HUD"), { recursive: true });

await step("refresh_asset_database (pre)", { kind: "refresh_asset_database" });

await step("create_scriptable_object HudParityFixture", {
  kind: "create_scriptable_object",
  type_name: "HudParityFixture",
  asset_path: ASSET_PATH,
});

// Seed baseline values (defaults captured from MainScene.unity sim init: money=0, pop=0, happiness=0, speed=0)
await step("modify_scriptable_object set baseline", {
  kind: "modify_scriptable_object",
  asset_path: ASSET_PATH,
  field_writes: [
    { field_name: "expectedMoney", value_kind: "int", value: "0" },
    { field_name: "expectedPopulation", value_kind: "int", value: "0" },
    { field_name: "expectedHappiness", value_kind: "float", value: "0" },
    { field_name: "expectedSpeedIndex", value_kind: "int", value: "0" },
    { field_name: "happinessTolerance", value_kind: "float", value: "0.05" },
    { field_name: "simTickBudget", value_kind: "int", value: "30" },
  ],
});

console.log("\n=== FIXTURE CREATED + SEEDED ===");
