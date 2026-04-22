/**
 * MCP tool: plan_digest_gate_author_helper — suggests a canonical gate command for an edit tuple.
 * Input: { operation: "edit"|"create"|"delete", file: string, before?: string, after?: string }.
 * Output: { gate_cmd: string, expectation: string } — one shell line, one pass criterion.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const inputShape = {
  operation: z.enum(["edit", "create", "delete"]),
  file: z.string().min(1),
  before: z.string().optional(),
  after: z.string().optional(),
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

export function registerPlanDigestGateAuthorHelper(server: McpServer): void {
  server.registerTool(
    "plan_digest_gate_author_helper",
    {
      description: "Suggest canonical gate command for an edit tuple (one shell line + expectation).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("plan_digest_gate_author_helper", async () => {
        const envelope = await wrapTool(
          async (input: {
            operation: "edit" | "create" | "delete";
            file: string;
            before?: string;
            after?: string;
          }) => {
            let gate_cmd = "npm run validate:all";
            let expectation = "exit 0";
            if (input.operation === "create") {
              gate_cmd = `test -f ${input.file} && echo OK`;
              expectation = "prints OK";
            } else if (input.operation === "delete") {
              gate_cmd = `test ! -e ${input.file} && echo OK`;
              expectation = "prints OK";
            } else if (input.operation === "edit" && input.after) {
              const firstLine = input.after.split("\n")[0]!.replace(/'/g, "'\\''").slice(0, 80);
              gate_cmd = `grep -cF '${firstLine}' ${input.file}`;
              expectation = "≥1 match";
            }
            return { gate_cmd, expectation };
          },
        )(args as {
          operation: "edit" | "create" | "delete";
          file: string;
          before?: string;
          after?: string;
        });
        return jsonResult(envelope);
      }),
  );
}
