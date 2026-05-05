// Bake canonical hud_bar.prefab via menu item, then wire under Canvas (Game UI).

import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

async function call(kind: string, payload: Record<string, unknown> = {}, timeout_ms = 60000) {
  const res: any = await runUnityBridgeCommand({ kind, timeout_ms, ...(payload as any) } as any);
  const ok = res?.ok ?? res?.response?.ok;
  const err = res?.response?.error || res?.error || "";
  const mr = res?.response?.mutation_result || "";
  console.log(`[${kind}] ok=${ok} err=${err}`);
  if (mr) console.log(`  mutation_result: ${String(mr).slice(0, 300)}`);
  return res;
}

(async () => {
  // 0. Refresh asset DB so newly-added MenuItem is registered after compile
  await call("refresh_asset_database");

  // Wait for compile to settle
  for (let i = 0; i < 30; i++) {
    const cs: any = await call("get_compilation_status");
    const compiling = cs?.response?.compilation_status?.compiling;
    if (compiling === false) break;
    await new Promise((r) => setTimeout(r, 1500));
  }

  // 1. Execute the bake menu item
  await call("execute_menu_item", { menu_path: "Tools/Bake/Catalog Bake From Snapshot" });

  // Refresh after bake
  await call("refresh_asset_database");

  // 2. Open MainScene
  await call("open_scene", { scene_path: "Assets/Scenes/MainScene.unity", mode: "single" });

  // 3. Find/create Canvas (Game UI)
  const findRes: any = await call("find_gameobject", { target_path: "Canvas (Game UI)" });
  let canvasExists = false;
  try {
    const parsed = JSON.parse(findRes?.response?.mutation_result || "{}");
    canvasExists = parsed.exists === true;
  } catch {}
  console.log(`canvas_exists: ${canvasExists}`);

  if (!canvasExists) {
    await call("create_gameobject", { go_name: "Canvas (Game UI)" });
    await call("attach_component", { target_path: "Canvas (Game UI)", component_type_name: "Canvas" });
    await call("attach_component", { target_path: "Canvas (Game UI)", component_type_name: "CanvasScaler" });
    await call("attach_component", { target_path: "Canvas (Game UI)", component_type_name: "GraphicRaycaster" });
    await call("assign_serialized_field", {
      target_path: "Canvas (Game UI)",
      component_type_name: "Canvas",
      field_name: "m_RenderMode",
      value_kind: "int",
      value: "0",
    });
  }

  // 4. Drop prior hud_bar (idempotent — error here is fine)
  const findHud: any = await call("find_gameobject", { target_path: "Canvas (Game UI)/hud_bar" });
  let hudExists = false;
  try {
    hudExists = JSON.parse(findHud?.response?.mutation_result || "{}").exists === true;
  } catch {}
  if (hudExists) await call("delete_gameobject", { target_path: "Canvas (Game UI)/hud_bar" });

  // 5. Instantiate prefab
  await call("instantiate_prefab", {
    prefab_path: "Assets/UI/Prefabs/Generated/hud_bar.prefab",
    parent_path: "Canvas (Game UI)",
  });

  // 6. Save
  await call("save_scene", { scene_path: "Assets/Scenes/MainScene.unity" });

  // 7. Capture screenshot game view (requires Play Mode per bridge contract)
  await call("enter_play_mode", {}, 90000);

  const ss: any = await call("capture_screenshot", {
    file_stem: "game-ui-catalog-bake-stage-1.0-hud-bar",
    include_ui: true,
  });

  await call("exit_play_mode", {}, 60000);

  const path =
    ss?.response?.bundle?.screenshot?.artifact_path ||
    ss?.response?.snapshot?.screenshot?.artifact_path ||
    ss?.response?.screenshot?.artifact_path ||
    "";
  console.log("\nSCREENSHOT_PATH:", path);
})();
