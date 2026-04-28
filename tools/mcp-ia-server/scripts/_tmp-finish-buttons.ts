import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

async function step(name: string, body: any) {
  console.log(`\n=== ${name} ===`);
  const r: any = await runUnityBridgeCommand({ ...body, timeout_ms: 30000 });
  const ok = r && typeof r === "object" && r.ok === true;
  console.log(JSON.stringify({ ok, mr: r?.snapshot?.mutation_result, error: r?.error, message: r?.message }, null, 2));
  if (!ok) {
    console.error("STEP FAILED:", name);
    process.exit(1);
  }
  return r;
}

const HUD_BAR = "Game Managers/hud-bar";
const ADAPTER = "HudBarDataAdapter";

// Reload scene from disk to pick up renamed m_Name overrides
await step("open_scene reload", { kind: "open_scene", scene_path: "Assets/Scenes/MainScene.unity", scene_mode: "single" });

// Verify renamed paths resolve
async function find(target_path: string) {
  const r: any = await runUnityBridgeCommand({ kind: "find_gameobject", target_path, timeout_ms: 15000 });
  console.log(`find ${target_path}:`, JSON.stringify({ ok: r?.ok, msg: r?.message }));
  return r?.ok === true;
}

const renamed = [
  "Game Managers/hud-bar/illuminated-button",
  "Game Managers/hud-bar/illuminated-button-speed-1",
  "Game Managers/hud-bar/illuminated-button-speed-2",
  "Game Managers/hud-bar/illuminated-button-speed-3",
  "Game Managers/hud-bar/illuminated-button-speed-4",
];
for (const p of renamed) {
  const ok = await find(p);
  if (!ok) { console.error("button missing:", p); process.exit(1); }
}

// Re-assign _speedButtons array (size already 5; rewrite [0] for safety + [1..4] new)
for (let i = 0; i < renamed.length; i++) {
  await step(`assign _speedButtons[${i}]`, {
    kind: "assign_serialized_field",
    target_path: HUD_BAR,
    component_type_name: ADAPTER,
    field_name: `_speedButtons.Array.data[${i}]`,
    value_kind: "object_ref",
    value_object_path: renamed[i],
  });
}

await step("save_scene", { kind: "save_scene" });
console.log("\n=== ALL ASSIGNMENTS COMPLETE ===");
