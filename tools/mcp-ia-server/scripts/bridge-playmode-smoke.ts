/**
 * IDE agent bridge smoke (Close Dev Loop): get_play_mode_status → enter_play_mode →
 * debug_context_bundle → exit_play_mode.
 *
 * Implementation: each step calls {@link runUnityBridgeCommand} — the **same** enqueue + poll
 * implementation as the territory-ia MCP tool **`unity_bridge_command`** (not a parallel protocol).
 * Agents with MCP enabled should prefer **`unity_bridge_command`**; this script is for CI shells
 * and humans running `npm run db:bridge-playmode-smoke` from the repo root.
 *
 * Prerequisites: Postgres with migration 0008 (`agent_bridge_job`), Unity Editor open on
 * {@link REPO_ROOT} (same folder as this repo) with `AgentBridgeCommandRunner` polling.
 *
 * Connection: set `DATABASE_URL` or put the full URI in `config/postgres-dev.json` (SCRAM needs a password in the URI).
 *
 * Usage from repository root:
 *   npm run db:bridge-playmode-smoke -- [seed_cell]
 */

import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import {
  runUnityBridgeCommand,
  type UnityBridgeCommandInput,
} from "../src/tools/unity-bridge-command.js";

const seedCell = process.argv[2]?.trim() || "62,0";

async function main(): Promise<number> {
  const repo = resolveRepoRoot();
  loadRepoDotenvIfNotCi(repo);
  const timeout_ms = Math.min(
    30_000,
    Math.max(1000, Number(process.env.BRIDGE_TIMEOUT_MS ?? 30_000) || 30_000),
  );
  console.error(`bridge-playmode-smoke: REPO_ROOT=${repo} seed_cell=${seedCell}`);

  const steps: Array<{ label: string; input: UnityBridgeCommandInput }> = [
    { label: "get_play_mode_status", input: { kind: "get_play_mode_status", timeout_ms } },
    { label: "enter_play_mode", input: { kind: "enter_play_mode", timeout_ms } },
    {
      label: "debug_context_bundle",
      input: {
        kind: "debug_context_bundle",
        timeout_ms,
        seed_cell: seedCell,
        include_screenshot: true,
        include_console: true,
        include_anomaly_scan: true,
      },
    },
    { label: "exit_play_mode", input: { kind: "exit_play_mode", timeout_ms } },
  ];

  for (const { label, input } of steps) {
    console.error(`--- ${label} ---`);
    const r = await runUnityBridgeCommand(input);
    console.log(JSON.stringify({ step: label, ...r }, null, 2));
    if (!r.ok) {
      console.error(`bridge-playmode-smoke: step "${label}" failed.`);
      if (r.error === "db_error" || r.error === "db_unconfigured") {
        console.error(
          "Database: set DATABASE_URL or config/postgres-dev.json database_url (full URI including password if SCRAM requires it).",
        );
      }
      if (r.error === "timeout") {
        console.error("Timeout: ensure Unity Editor is open on REPO_ROOT and dequeue is running.");
      }
      return 1;
    }

    if (label === "debug_context_bundle" && r.ok && "response" in r) {
      const resp = r.response as Record<string, unknown>;
      const bundle = resp.bundle as Record<string, unknown> | undefined;
      if (bundle) {
        console.error("--- bundle summary ---");
        console.error(
          JSON.stringify(
            {
              cell_export: bundle.cell_export,
              screenshot: bundle.screenshot,
              console: bundle.console && typeof bundle.console === "object"
                ? {
                    skipped: (bundle.console as { skipped?: boolean }).skipped,
                    line_count: (bundle.console as { line_count?: number }).line_count,
                  }
                : bundle.console,
              anomaly_count: bundle.anomaly_count,
              anomaly_scan_skipped: bundle.anomaly_scan_skipped,
              anomalies_preview: Array.isArray(bundle.anomalies)
                ? (bundle.anomalies as unknown[]).slice(0, 8)
                : bundle.anomalies,
            },
            null,
            2,
          ),
        );
      }
    }
  }

  console.error("bridge-playmode-smoke: all steps ok.");
  return 0;
}

main()
  .then((c) => process.exit(c))
  .catch((e) => {
    console.error(e);
    process.exit(1);
  });
