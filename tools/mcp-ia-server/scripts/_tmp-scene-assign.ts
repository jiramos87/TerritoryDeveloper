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

// Producer refs (object_ref scene paths)
await step("assign _cityStats", {
  kind: "assign_serialized_field", target_path: HUD_BAR, component_type_name: ADAPTER,
  field_name: "_cityStats", value_kind: "object_ref", value_object_path: "Game Managers/CityStats",
});
await step("assign _economyManager", {
  kind: "assign_serialized_field", target_path: HUD_BAR, component_type_name: ADAPTER,
  field_name: "_economyManager", value_kind: "object_ref", value_object_path: "Game Managers/EconomyManager",
});
await step("assign _timeManager", {
  kind: "assign_serialized_field", target_path: HUD_BAR, component_type_name: ADAPTER,
  field_name: "_timeManager", value_kind: "object_ref", value_object_path: "Game Managers/TimeManager",
});

// Theme (asset_ref to UiTheme SO)
await step("assign _uiTheme", {
  kind: "assign_serialized_field", target_path: HUD_BAR, component_type_name: ADAPTER,
  field_name: "_uiTheme", value_kind: "asset_ref", value_object_path: "Assets/UI/Theme/DefaultUiTheme.asset",
});

// Consumer refs (StudioControl variants — scene-level instances under hud-bar)
await step("assign _moneyReadout", {
  kind: "assign_serialized_field", target_path: HUD_BAR, component_type_name: ADAPTER,
  field_name: "_moneyReadout", value_kind: "object_ref", value_object_path: "Game Managers/hud-bar/segmented-readout",
});

// VUMeter — populate happiness meter ref (still required even though needle preferred input)
await step("assign _happinessMeter", {
  kind: "assign_serialized_field", target_path: HUD_BAR, component_type_name: ADAPTER,
  field_name: "_happinessMeter", value_kind: "object_ref", value_object_path: "Game Managers/hud-bar/vu-meter",
});

// _happinessNeedle — NeedleBallistics is a sibling juice component on the same vu-meter GO per Stage 5 contract.
// Skip if absent (adapter is null-tolerant); but author writes target_path same as vu-meter for component lookup.
// Verify NeedleBallistics will be auto-attached by the bake handler later. For Stage 6 wiring, leave null;
// adapter falls back to writing nothing (VUMeter ignored per code comment) — matches §Plan Digest "preferred but not required".

// Speed buttons — array assignment requires setting array size + each element
// SerializedProperty path: _speedButtons.Array.size (int) + _speedButtons.Array.data[i] (object_ref)
await step("assign _speedButtons.Array.size = 5", {
  kind: "assign_serialized_field", target_path: HUD_BAR, component_type_name: ADAPTER,
  field_name: "_speedButtons.Array.size", value_kind: "int", value: "5",
});

const buttonPaths = [
  "Game Managers/hud-bar/illuminated-button",
  "Game Managers/hud-bar/illuminated-button (1)",
  "Game Managers/hud-bar/illuminated-button (2)",
  "Game Managers/hud-bar/illuminated-button (3)",
  "Game Managers/hud-bar/illuminated-button (4)",
];
for (let i = 0; i < buttonPaths.length; i++) {
  await step(`assign _speedButtons[${i}]`, {
    kind: "assign_serialized_field", target_path: HUD_BAR, component_type_name: ADAPTER,
    field_name: `_speedButtons.Array.data[${i}]`, value_kind: "object_ref", value_object_path: buttonPaths[i],
  });
}

// Save
await step("save_scene", { kind: "save_scene" });
console.log("\n=== ALL ASSIGNMENTS COMPLETE ===");
