/**
 * MCP tool: project_spec_closeout_digest — structured extract from `ia/projects/{ISSUE_ID}[-{description}].md` (TECH-58, paths updated by TECH-85 / Stage 2).
 */

import fs from "node:fs";
import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import {
  buildProjectSpecCloseoutDigest,
  resolveProjectSpecFile,
} from "../parser/project-spec-closeout-parse.js";

const inputShape = {
  issue_id: z
    .string()
    .optional()
    .describe(
      "Backlog id (e.g. TECH-58). Exactly one of `issue_id` or `spec_path` required.",
    ),
  spec_path: z
    .string()
    .optional()
    .describe(
      "Repo-relative path under `ia/projects/` or `.cursor/projects/` (legacy). Filename may be `{ISSUE_ID}.md` or `{ISSUE_ID}-{description}.md` (per TECH-85 Q8). Exactly one of `issue_id` or `spec_path` required.",
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
 * Register the project_spec_closeout_digest tool.
 */
export function registerProjectSpecCloseoutDigest(server: McpServer): void {
  server.registerTool(
    "project_spec_closeout_digest",
    {
      description:
        "Parse a temporary project spec under `ia/projects/` (or legacy `.cursor/projects/`) for **project-spec-close**: H2 sections (Summary, Lessons Learned, Decision Log, …), cited issue ids, optional glossary_discover keywords, and heuristic G1–I1 checklist hints. Filenames may be `{ISSUE_ID}.md` or `{ISSUE_ID}-{description}.md` (per TECH-85 Q8). Does not edit files or author normative spec prose. Requires exactly one of `issue_id` or `spec_path`.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("project_spec_closeout_digest", async () => {
        const a = args as { issue_id?: string; spec_path?: string };
        const repoRoot = resolveRepoRoot();
        const resolved = resolveProjectSpecFile(repoRoot, {
          issue_id: a.issue_id,
          spec_path: a.spec_path,
        });
        if (!resolved.ok) {
          return jsonResult({
            error: resolved.error,
            message: resolved.message,
          });
        }

        let markdown: string;
        try {
          markdown = fs.readFileSync(resolved.absPath, "utf8");
        } catch (e) {
          const msg = e instanceof Error ? e.message : String(e);
          return jsonResult({
            error: "read_failed",
            message: `Could not read project spec: ${msg}`,
            spec_path: resolved.relPosix,
          });
        }

        const digest = buildProjectSpecCloseoutDigest(
          markdown,
          resolved.relPosix,
          resolved.issue_id,
        );

        return jsonResult(digest);
      }),
  );
}
