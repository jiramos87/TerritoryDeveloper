/**
 * MCP tool: plan_digest_resolve_anchor — narrow-scope grep replacement.
 * Input: { file: string, substring: string, max_hits?: number (default 5) }.
 * Output: { file, hits: number, matches: Array<{line: number, context: string}> }.
 * Contract: plan-digest requires unique anchors — consumer must verify hits === 1.
 * Context = ≤3 lines surrounding. Token budget: ≤20 lines output even on miss.
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
  file: z.string().min(1).describe("Repo-relative path to a file under REPO_ROOT."),
  substring: z.string().min(1).describe("Non-empty substring to find per line."),
  max_hits: z
    .number()
    .int()
    .min(1)
    .max(20)
    .optional()
    .default(5)
    .describe("Max match records to return (default 5)."),
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

export function registerPlanDigestResolveAnchor(server: McpServer): void {
  server.registerTool(
    "plan_digest_resolve_anchor",
    {
      description:
        "Given (file, substring), return hit count + up to 3 lines context per hit. Uniqueness (hits === 1) is a caller contract for plan-digest.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("plan_digest_resolve_anchor", async () => {
        const envelope = await wrapTool(
          async (input: {
            file: string;
            substring: string;
            max_hits: number;
          }) => {
            const root = resolveRepoRoot();
            const abs = path.resolve(root, input.file);
            if (!isWithinRepoRoot(abs, root) || !fs.existsSync(abs)) {
              return {
                file: input.file,
                hits: 0,
                matches: [] as Array<{ line: number; context: string }>,
                error: "file_not_found" as const,
              };
            }
            const lines = fs.readFileSync(abs, "utf8").split("\n");
            const matches: Array<{ line: number; context: string }> = [];
            const maxH = input.max_hits;
            for (let i = 0; i < lines.length && matches.length < maxH; i += 1) {
              if (lines[i]!.includes(input.substring)) {
                const start = Math.max(0, i - 1);
                const end = Math.min(lines.length, i + 2);
                matches.push({
                  line: i + 1,
                  context: lines.slice(start, end).join("\n"),
                });
              }
            }
            return {
              file: input.file,
              hits: matches.length,
              matches,
            };
          },
        )({
          file: (args as { file?: string }).file ?? "",
          substring: (args as { substring?: string }).substring ?? "",
          max_hits: (args as { max_hits?: number }).max_hits ?? 5,
        });
        return jsonResult(envelope);
      }),
  );
}
