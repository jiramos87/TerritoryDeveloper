// Re-bake hud_bar + re-screenshot post HLG fix.

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
  if (mr) console.log(`  mr: ${String(mr).slice(0, 250)}`);
  return res;
}

(async () => {
  await call("refresh_asset_database");
  for (let i = 0; i < 30; i++) {
    const cs: any = await call("get_compilation_status");
    if (cs?.response?.compilation_status?.compiling === false) break;
    await new Promise((r) => setTimeout(r, 1500));
  }
  await call("execute_menu_item", { menu_path: "Tools/Bake/Catalog Bake From Snapshot" });
  await call("refresh_asset_database");

  // Re-open scene + drop + re-instantiate
  await call("open_scene", { scene_path: "Assets/Scenes/MainScene.unity", mode: "single" });

  const findHud: any = await call("find_gameobject", { target_path: "Canvas (Game UI)/hud_bar" });
  let hudExists = false;
  try {
    hudExists = JSON.parse(findHud?.response?.mutation_result || "{}").exists === true;
  } catch {}
  if (hudExists) await call("delete_gameobject", { target_path: "Canvas (Game UI)/hud_bar" });

  await call("instantiate_prefab", {
    prefab_path: "Assets/UI/Prefabs/Generated/hud_bar.prefab",
    parent_path: "Canvas (Game UI)",
  });
  await call("save_scene", { scene_path: "Assets/Scenes/MainScene.unity" });

  // Enter play mode (may timeout on GridManager — capture_screenshot still works)
  await call("enter_play_mode", {}, 120000);

  const ss: any = await call("capture_screenshot", {
    file_stem: "game-ui-catalog-bake-stage-1.0-hud-bar-v2",
    include_ui: true,
  });

  await call("exit_play_mode", {}, 60000);

  const path =
    ss?.response?.bundle?.screenshot?.artifact_path ||
    ss?.response?.snapshot?.screenshot?.artifact_path ||
    "";
  console.log("\nSCREENSHOT_PATH:", path);
})();
