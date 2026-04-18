/**
 * MCP tool: backlog_record_validate — validate raw backlog yaml record body against shared schema core.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { validateBacklogRecord } from "../parser/backlog-record-schema.js";
import { runWithToolTiming } from "../instrumentation.js";

const inputShape = {
  yaml_body: z
    .string()
    .describe(
      "Raw yaml body of a single ia/backlog/{id}.yaml record to validate against the shared schema core.",
    ),
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

/**
 * Register the backlog_record_validate tool.
 */
export function registerBacklogRecordValidate(server: McpServer): void {
  server.registerTool(
    "backlog_record_validate",
    {
      description:
        "Validate raw backlog yaml record body against shared schema core. Returns ok + errors + warnings. Call before disk-writing ia/backlog/{id}.yaml to catch schema defects pre-commit.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("backlog_record_validate", async () => {
        const body = (args?.yaml_body ?? "").trim();
        if (!body) {
          return jsonResult({
            ok: false,
            errors: ["invalid_input: yaml_body is empty"],
            warnings: [],
          });
        }
        return jsonResult(validateBacklogRecord(body));
      }),
  );
}
