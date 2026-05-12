import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

async function call(kind: string, params?: Record<string, unknown>) {
  const r = await runUnityBridgeCommand({ kind: kind as any, timeout_ms: 60000, params } as any);
  if (!r.ok) {
    console.error(`BRIDGE FAIL ${kind}:`, JSON.stringify(r, null, 2));
    process.exit(1);
  }
  return r;
}

async function sleep(ms: number) {
  return new Promise(res => setTimeout(res, ms));
}

(async () => {
  console.log("# probe play_mode_status");
  let st = await call("get_play_mode_status");
  console.log("play_mode_state =", (st.response as any)?.play_mode_state);

  if ((st.response as any)?.play_mode_state !== "play_mode") {
    console.log("# enter_play_mode");
    await call("enter_play_mode");
    // poll until ready
    for (let i = 0; i < 30; i++) {
      await sleep(2000);
      st = await call("get_play_mode_status");
      const state = (st.response as any)?.play_mode_state;
      console.log(`  attempt ${i+1}: state=${state}`);
      if (state === "play_mode") break;
    }
  }

  console.log("# dispatch_action stats.open");
  await call("dispatch_action", { action_id: "stats.open" });
  await sleep(800);

  console.log("# capture_screenshot stats-panel iter7");
  let r1 = await call("capture_screenshot", { include_ui: true, filename_stem: "stats-panel-iter7" });
  const stats_paths = (r1.response as any)?.artifact_paths ?? [];
  console.log("stats artifact:", JSON.stringify(stats_paths));

  console.log("# dispatch_action stats.close");
  await call("dispatch_action", { action_id: "stats.close" });
  await sleep(500);

  console.log("# dispatch_action budget.open");
  await call("dispatch_action", { action_id: "budget.open" });
  await sleep(800);

  console.log("# capture_screenshot budget-panel iter1");
  let r2 = await call("capture_screenshot", { include_ui: true, filename_stem: "budget-panel-iter1" });
  const budget_paths = (r2.response as any)?.artifact_paths ?? [];
  console.log("budget artifact:", JSON.stringify(budget_paths));

  console.log("# dispatch_action budget.close");
  await call("dispatch_action", { action_id: "budget.close" });
  await sleep(300);

  console.log("# RESULT:");
  console.log(JSON.stringify({ stats: stats_paths, budget: budget_paths }, null, 2));
})();
