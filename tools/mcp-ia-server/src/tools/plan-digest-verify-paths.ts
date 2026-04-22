/**
 * MCP tool: plan_digest_verify_paths — token-economy variant of Glob.
 * Input: { paths: string[] } (repo-relative). Output: { results: Record<string, boolean> }
 * where true = path exists on disk. No listings, no globbing, no stat metadata.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

function isWithinRepoRoot(abs: string, root: string): boolean {
  const a = path.resolve(abs);
  const r = path.resolve(root);
  return a === r || a.startsWith(r + path.sep);
}

const inputShape = {
  paths: z
    .array(z.string().min(1))
    .min(1)
    .max(200)
    .describe("Repo-relative paths to test for existence under REPO_ROOT."),
};

function jsonResult(payload: unknown) {
  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(payload, null, 2),
      },
    ],
  };
}

export function registerPlanDigestVerifyPaths(server: McpServer): void {
  server.registerTool(
    "plan_digest_verify_paths",
    {
      description:
        "Given list of repo-relative paths, return exists/not-exists per path. Token-economy alternative to Glob.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("plan_digest_verify_paths", async () => {
        const envelope = await wrapTool(
          async (input: { paths: string[] }) => {
            const root = resolveRepoRoot();
            const results: Record<string, boolean> = {};
            for (const p of input.paths) {
              const abs = path.resolve(root, p);
              if (!isWithinRepoRoot(abs, root)) {
                results[p] = false;
                continue;
              }
              results[p] = fs.existsSync(abs);
            }
            return { results };
          },
        )(args as { paths: string[] });
        return jsonResult(envelope);
      }),
  );
}
