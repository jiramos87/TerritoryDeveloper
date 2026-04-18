/**
 * MCP tool: backlog_issue — one BACKLOG.md issue by id (structured + raw block).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import {
  parseBacklogIssue,
  resolveDependsOnStatus,
} from "../parser/backlog-parser.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

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

type BacklogIssueArgs = { issue_id?: string };

/**
 * Register the backlog_issue tool.
 */
export function registerBacklogIssue(server: McpServer): void {
  server.registerTool(
    "backlog_issue",
    {
      description:
        "Load a single issue by id from BACKLOG.md (open rows) or BACKLOG-ARCHIVE.md (historical completions): title, status, Files/Spec/Notes, depends_on_status for cited ids, raw block. Use first when starting work on BUG-XX/FEAT-XX/TECH-XX.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("backlog_issue", async () => {
        const envelope = await wrapTool(
          async (input: BacklogIssueArgs | undefined) => {
            const issueId = (input?.issue_id ?? "").trim();
            if (!issueId) {
              throw { code: "invalid_input", message: "issue_id is required." };
            }

            const repoRoot = resolveRepoRoot();
            const parsed = parseBacklogIssue(repoRoot, issueId);
            if (!parsed) {
              throw {
                code: "issue_not_found",
                message: `No issue '${issueId}' in BACKLOG.md or BACKLOG-ARCHIVE.md.`,
                hint: "Check ia/backlog/ and ia/backlog-archive/",
              };
            }

            const depends_on_status = resolveDependsOnStatus(
              repoRoot,
              parsed.depends_on,
            );

            return { ...parsed, depends_on_status };
          },
        )(args as BacklogIssueArgs | undefined);

        return jsonResult(envelope);
      }),
  );
}
