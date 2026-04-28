import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

async function step(name: string, body: any) {
  console.log(`\n=== ${name} ===`);
  const r: any = await runUnityBridgeCommand({ ...body, timeout_ms: 30000 });
  const ok = r && typeof r === "object" && r.ok === true;
  console.log(JSON.stringify({ ok, mutation_result: r?.snapshot?.mutation_result, error: r?.error, message: r?.message }, null, 2));
  if (!ok) {
    console.error("STEP FAILED:", name);
    console.error(JSON.stringify(r, null, 2));
    process.exit(1);
  }
  return r;
}

// 1) open scene
await step("open_scene", { kind: "open_scene", scene_path: "Assets/Scenes/MainScene.unity", scene_mode: "single" });

// 2) instantiate hud-bar prefab DIRECTLY under Game Managers
await step("instantiate hud-bar", {
  kind: "instantiate_prefab",
  prefab_path: "Assets/UI/Prefabs/Generated/hud-bar.prefab",
  parent_path: "Game Managers",
});

// 3) instantiate child StudioControl variants under Game Managers/hud-bar
//    so ThemedPanel composer can later match accepts
await step("instantiate segmented-readout (money)", {
  kind: "instantiate_prefab",
  prefab_path: "Assets/UI/Prefabs/Generated/segmented-readout.prefab",
  parent_path: "Game Managers/hud-bar",
});
await step("instantiate vu-meter (happiness)", {
  kind: "instantiate_prefab",
  prefab_path: "Assets/UI/Prefabs/Generated/vu-meter.prefab",
  parent_path: "Game Managers/hud-bar",
});
for (let i = 0; i < 5; i++) {
  await step(`instantiate illuminated-button (speed ${i})`, {
    kind: "instantiate_prefab",
    prefab_path: "Assets/UI/Prefabs/Generated/illuminated-button.prefab",
    parent_path: "Game Managers/hud-bar",
  });
}

// 4) attach HudBarDataAdapter onto hud-bar root
await step("attach HudBarDataAdapter", {
  kind: "attach_component",
  target_path: "Game Managers/hud-bar",
  component_type_name: "HudBarDataAdapter",
});

// 5) save scene (intermediate save before assign — picks up sibling instances first)
await step("save_scene (interim)", { kind: "save_scene" });
