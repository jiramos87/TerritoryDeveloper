/**
 * MCP tool: stage_closeout_digest — structured extract from `ia/projects/{ISSUE_ID}[-{description}].md`.
 *
 * Per-Task digest emitter invoked N times by `stage-closeout-apply` Sonnet pair-tail (one
 * call per Task of the closing Stage). Applier aggregates N per-Task digests into one
 * Stage-level closeout summary emitted at end of Stage.
 *
 * Renamed from `project_spec_closeout_digest` (lifecycle-refactor T7.14) when the per-Task
 * closeout was collapsed into Stage-scoped closeout per `ia/rules/plan-apply-pair-contract.md`
 * seam #4.
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
import { wrapTool } from "../envelope.js";

const inputShape = {
  issue_id: z
    .string()
    .optional()
    .describe(
      "Backlog id (e.g. BUG-12 / FEAT-7 / TECH-11). Exactly one of `issue_id` or `spec_path` required.",
    ),
  spec_path: z
    .string()
    .optional()
    .describe(
      "Repo-relative path under `ia/projects/`. Filename may be `{ISSUE_ID}.md` or `{ISSUE_ID}-{description}.md` (descriptive suffix). Exactly one of `issue_id` or `spec_path` required.",
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
 * Register the stage_closeout_digest tool.
 */
export function registerStageCloseoutDigest(server: McpServer): void {
  server.registerTool(
    "stage_closeout_digest",
    {
      description:
        "Parse one temporary project spec under `ia/projects/` for **stage-closeout-apply** (Sonnet pair-tail, seam #4). Returns H2 sections (Summary, Lessons Learned, Decision Log, §Audit, …), cited issue ids, optional glossary_discover keywords, and heuristic G1–I1 checklist hints. Called N times per closing Stage (one per Task); applier aggregates into one Stage-level digest. Filenames may be `{ISSUE_ID}.md` or `{ISSUE_ID}-{description}.md`. Does not edit files or author normative spec prose. Requires exactly one of `issue_id` or `spec_path`.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("stage_closeout_digest", async () => {
        const envelope = await wrapTool(
          async (input: { issue_id?: string; spec_path?: string }) => {
            const repoRoot = resolveRepoRoot();
            const resolved = resolveProjectSpecFile(repoRoot, {
              issue_id: input.issue_id,
              spec_path: input.spec_path,
            });
            if (!resolved.ok) {
              throw { code: "invalid_input" as const, message: resolved.message };
            }

            let markdown: string;
            try {
              markdown = fs.readFileSync(resolved.absPath, "utf8");
            } catch (e) {
              const msg = e instanceof Error ? e.message : String(e);
              throw {
                code: "internal_error" as const,
                message: `Could not read project spec: ${msg}`,
                details: { spec_path: resolved.relPosix },
              };
            }

            return buildProjectSpecCloseoutDigest(
              markdown,
              resolved.relPosix,
              resolved.issue_id,
            );
          },
        )(args as { issue_id?: string; spec_path?: string });
        return jsonResult(envelope);
      }),
  );
}
