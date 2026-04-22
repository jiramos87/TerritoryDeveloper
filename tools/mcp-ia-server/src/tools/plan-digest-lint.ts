/**
 * MCP tool: plan_digest_lint — runs the 9-point rubric (ia/rules/plan-digest-contract.md).
 * Input: { content: string, file?: string } — body of §Plan Digest or aggregate stage doc.
 * Output: { pass: boolean, failures: Array<{rule: 1..9, where: string, detail: string}> }.
 * Cap: returns at most 20 failures.
 */

import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const inputShape = {
  content: z.string().min(1).describe("Markdown body to validate."),
  file: z.string().optional().describe("Optional path label for output context."),
};

const PICK_RX =
  /\b(user decides|user picks|likely|probably|we could|might|consider|TBD|up to you|your call)\b/i;
const PARALLEL_RX = /\bin parallel\b|\b\|\| true\b|\bif-then-else\b/i;
const PATH_RX = /(?:^|\s)([A-Za-z0-9_\-/.]+\/[A-Za-z0-9_\-/.]+\.[A-Za-z0-9]+)/gm;

function isWithinRepoRoot(abs: string, root: string): boolean {
  const a = path.resolve(abs);
  const r = path.resolve(root);
  return a === r || a.startsWith(r + path.sep);
}

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

export function registerPlanDigestLint(server: McpServer): void {
  server.registerTool(
    "plan_digest_lint",
    {
      description:
        "Validate §Plan Digest or stage doc against the 9-point rubric. Returns {pass, failures} (max 20).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("plan_digest_lint", async () => {
        const envelope = await wrapTool(
          async (input: { content: string; file?: string }) => {
            const failures: Array<{ rule: number; where: string; detail: string }> = [];
            const lines = input.content.split("\n");
            const root = resolveRepoRoot();
            for (let i = 0; i < lines.length; i += 1) {
              if (failures.length >= 20) break;
              if (PICK_RX.test(lines[i]!)) {
                failures.push({
                  rule: 1,
                  where: `L${i + 1}`,
                  detail: lines[i]!.slice(0, 120),
                });
              }
              if (PARALLEL_RX.test(lines[i]!)) {
                failures.push({
                  rule: 6,
                  where: `L${i + 1}`,
                  detail: lines[i]!.slice(0, 120),
                });
              }
            }
            const hasGate = /\n\*\*Gate:\*\*/.test(input.content);
            if (!hasGate) {
              failures.push({ rule: 5, where: "doc", detail: "no **Gate:** section" });
            }
            const hasStop =
              /\n\*\*STOP\*\*/.test(input.content) || /\n\*\*STOP:\*\*/.test(input.content);
            if (!hasStop) {
              failures.push({ rule: 9, where: "doc", detail: "no **STOP** section" });
            }
            const hasEdit = /\n\*\*Edits?:\*\*/.test(input.content);
            if (!hasEdit) {
              failures.push({ rule: 3, where: "doc", detail: "no **Edits:** section" });
            }
            let m: RegExpExecArray | null;
            PATH_RX.lastIndex = 0;
            while ((m = PATH_RX.exec(input.content)) !== null && failures.length < 20) {
              const p = m[1]!;
              const abs = path.resolve(root, p);
              if (!isWithinRepoRoot(abs, root)) continue;
              if (!fs.existsSync(abs)) {
                failures.push({ rule: 2, where: `path:${p}`, detail: "path not found on HEAD" });
              }
            }
            const pass = failures.length === 0;
            return { pass, failures: failures.slice(0, 20) };
          },
        )(args as { content: string; file?: string });
        return jsonResult(envelope);
      }),
  );
}
