import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

async function call(kind: string, params?: Record<string, unknown>, timeout = 30000) {
  const r = await runUnityBridgeCommand({ kind: kind as any, timeout_ms: timeout, params } as any);
  const summary = { ok: r.ok, error: (r as any).error, artifact_paths: (r.response as any)?.artifact_paths };
  console.log(`[${kind}]`, JSON.stringify(summary));
  return r;
}

async function sleep(ms: number) { return new Promise(r => setTimeout(r, ms)); }

(async () => {
  console.log("# stats.open");
  await call("dispatch_action", { action_id: "stats.open" });
  await sleep(1200);

  console.log("# capture stats");
  const r1 = await call("capture_screenshot", { include_ui: true, filename_stem: "stats-panel-iter7-bridge" });
  const stats_paths = (r1.response as any)?.artifact_paths ?? [];

  console.log("# stats.close");
  await call("dispatch_action", { action_id: "stats.close" });
  await sleep(800);

  console.log("# budget.open");
  await call("dispatch_action", { action_id: "budget.open" });
  await sleep(1200);

  console.log("# capture budget");
  const r2 = await call("capture_screenshot", { include_ui: true, filename_stem: "budget-panel-iter1-bridge" });
  const budget_paths = (r2.response as any)?.artifact_paths ?? [];

  console.log("# budget.close");
  await call("dispatch_action", { action_id: "budget.close" });
  await sleep(300);

  console.log("\nRESULT:");
  console.log(JSON.stringify({ stats: stats_paths, budget: budget_paths }, null, 2));
})();
