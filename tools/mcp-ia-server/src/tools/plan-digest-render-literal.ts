/**
 * MCP tool: plan_digest_render_literal — verbatim line-range read.
 * Input: { file: string, line_start: number, line_end: number } (1-indexed, inclusive).
 * Output: { file, line_start, line_end, content: string }.
 * Refuses ranges >100 lines — plan-digest embeds small literals, not whole files.
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
  file: z.string().min(1).describe("Repo-relative file path."),
  line_start: z.number().int().min(1).describe("First line (1-based, inclusive)."),
  line_end: z.number().int().min(1).describe("Last line (1-based, inclusive)."),
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

export function registerPlanDigestRenderLiteral(server: McpServer): void {
  server.registerTool(
    "plan_digest_render_literal",
    {
      description: "Return verbatim content for a small line range (max 100 lines, 1-based inclusive).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("plan_digest_render_literal", async () => {
        const envelope = await wrapTool(
          async (input: { file: string; line_start: number; line_end: number }) => {
            if (input.line_end < input.line_start) {
              throw { code: "invalid_input" as const, message: "line_end < line_start" };
            }
            if (input.line_end - input.line_start + 1 > 100) {
              throw { code: "invalid_input" as const, message: "range_exceeds_100_lines" };
            }
            const root = resolveRepoRoot();
            const abs = path.resolve(root, input.file);
            if (!isWithinRepoRoot(abs, root) || !fs.existsSync(abs)) {
              throw { code: "invalid_input" as const, message: "file_not_found" };
            }
            const lines = fs.readFileSync(abs, "utf8").split("\n");
            const slice = lines
              .slice(input.line_start - 1, input.line_end)
              .join("\n");
            return {
              file: input.file,
              line_start: input.line_start,
              line_end: input.line_end,
              content: slice,
            };
          },
        )(args as { file: string; line_start: number; line_end: number });
        return jsonResult(envelope);
      }),
  );
}
