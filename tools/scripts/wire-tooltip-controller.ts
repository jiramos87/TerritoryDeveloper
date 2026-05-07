/**
 * Stage 9 (TECH-8548) — one-shot scene-wire of TooltipController.
 *
 * Sequence:
 *   1. open_scene Assets/Scenes/CityScene.unity
 *   2. find UI Canvas (target_path "UI Canvas")
 *   3. create_gameobject "TooltipController" parent_path "UI Canvas"
 *   4. attach_component TooltipController to "UI Canvas/TooltipController"
 *   5. assign_serialized_field _tooltipPrefab → Assets/UI/Prefabs/Generated/tooltip.prefab
 *   6. save_scene
 *
 * Idempotent: skips create when find_gameobject of TooltipController succeeds.
 */
import { resolveRepoRoot } from "../mcp-ia-server/src/config.js";
import { loadRepoDotenvIfNotCi } from "../mcp-ia-server/src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../mcp-ia-server/src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

const TIMEOUT = 60_000;

async function step(label: string, body: Record<string, unknown>): Promise<unknown> {
  const r = await runUnityBridgeCommand({ ...body, timeout_ms: TIMEOUT } as never);
  const ok = typeof r === "object" && r !== null && "ok" in r ? (r as { ok: boolean }).ok : false;
  console.log(`[${label}] ok=${ok}`);
  if (!ok) {
    console.log(JSON.stringify(r, null, 2));
    throw new Error(`bridge step '${label}' failed`);
  }
  return r;
}

async function main() {
  await step("open_scene", { kind: "open_scene", scene_path: "Assets/Scenes/CityScene.unity", scene_mode: "single" });

  let exists = false;
  try {
    const r = await runUnityBridgeCommand({ kind: "find_gameobject", target_path: "UI Canvas/TooltipController", timeout_ms: TIMEOUT } as never) as { ok: boolean; response?: { mutation_result?: string } };
    if (r.ok && r.response?.mutation_result) {
      const parsed = JSON.parse(r.response.mutation_result) as { exists?: boolean };
      exists = !!parsed.exists;
    }
  } catch {
    exists = false;
  }
  console.log(`[find_existing] exists=${exists}`);

  if (!exists) {
    await step("create_gameobject", { kind: "create_gameobject", go_name: "TooltipController", parent_path: "UI Canvas" });
    await step("attach_component", { kind: "attach_component", target_path: "UI Canvas/TooltipController", component_type_name: "TooltipController" });
  }

  await step("assign_tooltip_prefab", {
    kind: "assign_serialized_field",
    target_path: "UI Canvas/TooltipController",
    component_type_name: "TooltipController",
    field_name: "_tooltipPrefab",
    value_kind: "asset_ref",
    value_object_path: "Assets/UI/Prefabs/Generated/tooltip.prefab",
  });

  // Bind UiTheme ref (Inspector slot per invariant #4 fallback).
  await step("assign_theme_ref", {
    kind: "assign_serialized_field",
    target_path: "UI Canvas/TooltipController",
    component_type_name: "TooltipController",
    field_name: "_themeRef",
    value_kind: "asset_ref",
    value_object_path: "Assets/UI/Theme/DefaultUiTheme.asset",
  });

  await step("save_scene", { kind: "save_scene", scene_path: "Assets/Scenes/CityScene.unity" });

  console.log("wire-tooltip-controller: DONE");
  process.exit(0);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
