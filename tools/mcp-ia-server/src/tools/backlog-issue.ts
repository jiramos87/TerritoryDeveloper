/**
 * MCP tool: backlog_issue — one BACKLOG.md issue by id (structured + raw block).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { parseBacklogIssue } from "../parser/backlog-parser.js";
import { runWithToolTiming } from "../instrumentation.js";

const inputShape = {
  issue_id: z
    .string()
    .describe(
      "Issue id (e.g. BUG-37, FEAT-44, TECH-17, FEAT-37b). Case-insensitive type prefix.",
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
 * Register the backlog_issue tool.
 */
export function registerBacklogIssue(server: McpServer): void {
  server.registerTool(
    "backlog_issue",
    {
      description:
        "Load a single issue from BACKLOG.md by id (title, status, Files/Spec/Notes, raw block). Use first when starting work on BUG-XX/FEAT-XX/TECH-XX.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("backlog_issue", async () => {
        const issueId = (args?.issue_id ?? "").trim();
        if (!issueId) {
          return jsonResult({
            error: "invalid_input",
            message: "issue_id is required.",
          });
        }

        const repoRoot = resolveRepoRoot();
        const parsed = parseBacklogIssue(repoRoot, issueId);
        if (!parsed) {
          return jsonResult({
            error: "unknown_issue",
            message: `No issue '${issueId}' found in BACKLOG.md. Check spelling, id format (e.g. BUG-37), or BACKLOG-ARCHIVE.md for older items.`,
            hint: "Use list_specs for IA docs; backlog lives only in BACKLOG.md for this tool.",
          });
        }

        return jsonResult(parsed);
      }),
  );
}
