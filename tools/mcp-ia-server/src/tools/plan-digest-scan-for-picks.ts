/**
 * MCP tool: plan_digest_scan_for_picks — lint-only pick detector.
 * Input: { content: string } (digested-plan body).
 * Output: { pick_count: number, findings: Array<{line: number, phrase: string, excerpt: string}> }.
 * Regex set (case-insensitive): "user decides", "user picks", "likely", "probably",
 * "we could", "might", "consider", "TBD", "up to you", "your call".
 * NEVER resolves picks — fails fast on leak; plan-digest's rubric gate uses this.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const inputShape = {
  content: z.string().min(1).describe("Body text to scan (e.g. §Plan Digest markdown)."),
};

const PICK_PATTERNS: RegExp[] = [
  /\buser decides\b/i,
  /\buser picks\b/i,
  /\blikely\b/i,
  /\bprobably\b/i,
  /\bwe could\b/i,
  /\bmight\b/i,
  /\bconsider\b/i,
  /\bTBD\b/,
  /\bup to you\b/i,
  /\byour call\b/i,
];

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

export function registerPlanDigestScanForPicks(server: McpServer): void {
  server.registerTool(
    "plan_digest_scan_for_picks",
    {
      description:
        "Flag hand-wavy phrases in a digested-plan body. Lint-only (does not resolve).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("plan_digest_scan_for_picks", async () => {
        const envelope = await wrapTool(
          async (input: { content: string }) => {
            const findings: Array<{ line: number; phrase: string; excerpt: string }> = [];
            const lines = input.content.split("\n");
            for (let i = 0; i < lines.length; i += 1) {
              for (const rx of PICK_PATTERNS) {
                const m = lines[i]!.match(rx);
                if (m) {
                  findings.push({
                    line: i + 1,
                    phrase: m[0]!,
                    excerpt: lines[i]!.slice(0, 120),
                  });
                  break;
                }
              }
            }
            return {
              pick_count: findings.length,
              findings: findings.slice(0, 20),
            };
          },
        )(args as { content: string });
        return jsonResult(envelope);
      }),
  );
}
