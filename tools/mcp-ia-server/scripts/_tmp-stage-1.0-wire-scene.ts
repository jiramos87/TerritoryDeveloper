// One-off: wire baked hud_bar prefab under "Canvas (Game UI)" in MainScene.unity
// per TECH-11929 §Plan Digest.

import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

async function call(kind: string, payload: Record<string, unknown> = {}) {
  const res: any = await runUnityBridgeCommand({ kind, timeout_ms: 60000, ...(payload as any) } as any);
  const ok = res?.ok ?? res?.response?.ok;
  const err = res?.response?.error || res?.error || "";
  const mr = res?.response?.mutation_result || "";
  console.log(`[${kind}] ok=${ok} err=${err}`);
  if (mr) console.log(`  mutation_result: ${String(mr).slice(0, 300)}`);
  return res;
}

(async () => {
  // 1. Open MainScene
  await call("open_scene", { scene_path: "Assets/Scenes/MainScene.unity", mode: "single" });

  // 2. Probe Canvas (Game UI)
  const findRes: any = await call("find_gameobject", { target_path: "Canvas (Game UI)" });
  const mrJson = findRes?.response?.mutation_result || "";
  let canvasExists = false;
  try {
    const parsed = JSON.parse(mrJson || "{}");
    canvasExists = parsed.found === true || parsed.exists === true;
  } catch {}
  console.log(`canvas_exists: ${canvasExists}`);

  if (!canvasExists) {
    await call("create_gameobject", { go_name: "Canvas (Game UI)" });
    await call("attach_component", {
      target_path: "Canvas (Game UI)",
      component_type_name: "Canvas",
    });
    await call("attach_component", {
      target_path: "Canvas (Game UI)",
      component_type_name: "CanvasScaler",
    });
    await call("attach_component", {
      target_path: "Canvas (Game UI)",
      component_type_name: "GraphicRaycaster",
    });
    await call("assign_serialized_field", {
      target_path: "Canvas (Game UI)",
      component_type_name: "Canvas",
      field_name: "m_RenderMode",
      value_kind: "int",
      value: "0",
    });
  }

  // 3. Drop any prior hud_bar instance for idempotency
  await call("delete_gameobject", { target_path: "Canvas (Game UI)/hud_bar" });

  // 4. Instantiate baked prefab as child of Canvas
  await call("instantiate_prefab", {
    prefab_path: "Assets/UI/Prefabs/Generated/hud_bar.prefab",
    parent_path: "Canvas (Game UI)",
  });

  // 5. Save scene
  await call("save_scene", { scene_path: "Assets/Scenes/MainScene.unity" });

  // 6. Screenshot game view (include_ui = full overlay capture)
  const ss: any = await call("capture_screenshot", {
    file_stem: "game-ui-catalog-bake-stage-1.0-hud-bar",
    include_ui: true,
  });
  const path =
    ss?.response?.bundle?.screenshot?.artifact_path ||
    ss?.response?.snapshot?.screenshot?.artifact_path ||
    ss?.response?.screenshot?.artifact_path ||
    "";
  console.log("\nSCREENSHOT_PATH:", path);
  console.log("FULL_SCREENSHOT_RESPONSE:", JSON.stringify(ss?.response?.bundle || ss?.response, null, 2).slice(0, 1500));
})();
