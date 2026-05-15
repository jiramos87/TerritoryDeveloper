/**
 * MCP tool: feature_flags_snapshot_write
 *
 * Reads ia_feature_flags rows from Postgres and writes the interchange
 * snapshot artifact to tools/interchange/feature-flags-snapshot.json.
 *
 * Called by agents after toggling flags; Unity FeatureFlagsBootstrap and the
 * bridge flag_flip kind consume the resulting file.
 *
 * TECH-36136 / vibe-coding-safety stage-5-0
 */

import { z } from "zod";
import path from "node:path";
import fs from "node:fs";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { IaDbUnavailableError } from "../ia-db/queries.js";
import { resolveRepoRoot } from "../config.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

interface FlagRow {
  slug: string;
  enabled: boolean;
  default_value: boolean;
}

export function registerFeatureFlagsSnapshotWrite(server: McpServer): void {
  server.registerTool(
    "feature_flags_snapshot_write",
    {
      description:
        "Read ia_feature_flags rows from Postgres and write tools/interchange/feature-flags-snapshot.json. Call after toggling flags so Unity runtime and bridge flag_flip pick up the new state. Returns {ok, flags_count, path}.",
      inputSchema: {
        dry_run: z
          .boolean()
          .optional()
          .describe("When true, return the payload without writing to disk."),
      },
    },
    async (args) =>
      runWithToolTiming("feature_flags_snapshot_write", async () => {
        const envelope = await wrapTool(async () => {
          const pool = getIaDatabasePool();
          if (!pool) throw new IaDbUnavailableError();

          const result = await pool.query<FlagRow>(
            `SELECT slug, enabled, default_value FROM ia_feature_flags ORDER BY slug`
          );
          const flags = result.rows.map((r) => ({
            slug: r.slug,
            enabled: r.enabled,
            default_value: r.default_value,
          }));

          const snapshot = {
            artifact: "feature-flags-snapshot",
            schema_version: 1,
            generated_at: new Date().toISOString(),
            flags,
          };

          if (!args.dry_run) {
            const repoRoot = resolveRepoRoot();
            const outDir = path.join(repoRoot, "tools", "interchange");
            fs.mkdirSync(outDir, { recursive: true });
            const outPath = path.join(outDir, "feature-flags-snapshot.json");
            fs.writeFileSync(outPath, JSON.stringify(snapshot, null, 2) + "\n", "utf-8");
            return { ok: true, flags_count: flags.length, path: outPath };
          }

          return { ok: true, flags_count: flags.length, path: null, dry_run: true, snapshot };
        });
        return jsonResult(envelope);
      })
  );
}
