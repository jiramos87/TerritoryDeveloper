/**
 * MCP tool: stage_closeout_digest — structured extract from a Task spec body in
 * `ia_task_specs.body_md` (DB-backed). Per-Task digest emitter invoked N times by
 * stage closeout (one call per Task of the closing Stage). Applier aggregates
 * N per-Task digests into one Stage-level closeout summary emitted at end of Stage.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { buildProjectSpecCloseoutDigest } from "../parser/project-spec-closeout-parse.js";
import { queryTaskBody } from "../ia-db/queries.js";
import { normalizeIssueId } from "../parser/project-spec-closeout-parse.js";
import { wrapTool } from "../envelope.js";

const inputShape = {
  issue_id: z
    .string()
    .describe("Backlog id (e.g. BUG-12 / FEAT-7 / TECH-11). Task spec body fetched from DB."),
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
 * Register the stage_closeout_digest tool.
 */
export function registerStageCloseoutDigest(server: McpServer): void {
  server.registerTool(
    "stage_closeout_digest",
    {
      description:
        "Parse one Task spec body from DB-backed `ia_task_specs.body_md` for stage-closeout. Returns H2 sections (Summary, Lessons Learned, Decision Log, §Audit, …), cited issue ids, optional glossary_discover keywords, and heuristic G1–I1 checklist hints. Called N times per closing Stage (one per Task); applier aggregates into one Stage-level digest. Does not edit files or author normative spec prose.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("stage_closeout_digest", async () => {
        const envelope = await wrapTool(
          async (input: { issue_id?: string }) => {
            const rawId = (input.issue_id ?? "").trim();
            if (!rawId) {
              throw {
                code: "invalid_input" as const,
                message: "`issue_id` is required.",
              };
            }
            const issueId = normalizeIssueId(rawId);
            const markdown = await queryTaskBody(issueId);
            if (markdown == null) {
              throw {
                code: "task_not_found" as const,
                message: `No task spec body for '${issueId}' in ia_task_specs.`,
              };
            }
            return buildProjectSpecCloseoutDigest(markdown, issueId);
          },
        )(args as { issue_id?: string });
        return jsonResult(envelope);
      }),
  );
}
