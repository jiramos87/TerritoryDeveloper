/**
 * red_stage_proof_mine MCP tool — TECH-15910
 *
 * Wraps tools/scripts/red-stage-proof-mine.mjs.
 * Scans Assets/Scripts/Tests/** for BDD-name patterns matching the given issue id.
 * Emits candidate red-stage proof Markdown (stdout) or empty string when no match.
 */

import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { execFile } from "child_process";
import { promisify } from "util";
import { resolve } from "path";
import { runWithToolTiming } from "../instrumentation.js";
import { resolveRepoRoot } from "../config.js";

const execFileAsync = promisify(execFile);

const InputSchema = z.object({
  issue_id: z
    .string()
    .min(1)
    .describe(
      "Issue id to mine test candidates for (e.g. FEAT-123, TECH-15909). " +
        "Case-insensitive. Used to score BDD-name matches in Assets/Scripts/Tests/**.",
    ),
});

async function handle(args: unknown) {
  const { issue_id } = InputSchema.parse(args);
  const repoRoot = resolveRepoRoot();
  const script = resolve(repoRoot, "tools/scripts/red-stage-proof-mine.mjs");

  try {
    const { stdout, stderr } = await execFileAsync(
      process.execPath, // node
      [script, issue_id],
      { cwd: repoRoot, timeout: 15000 },
    );

    if (stderr && stderr.trim()) {
      return {
        content: [
          {
            type: "text" as const,
            text: JSON.stringify({ ok: false, error: { code: "script_error", message: stderr.trim() } }, null, 2),
          },
        ],
      };
    }

    const candidate = stdout.trim();
    return {
      content: [
        {
          type: "text" as const,
          text: JSON.stringify(
            {
              ok: true,
              payload: {
                issue_id,
                candidate_length: candidate.length,
                candidate: candidate || null,
              },
            },
            null,
            2,
          ),
        },
      ],
    };
  } catch (e: unknown) {
    const err = e as { message?: string; code?: number };
    return {
      content: [
        {
          type: "text" as const,
          text: JSON.stringify(
            { ok: false, error: { code: "exec_failed", message: err.message ?? String(e) } },
            null,
            2,
          ),
        },
      ],
    };
  }
}

export function registerRedStageProofMine(server: McpServer): void {
  server.registerTool(
    "red_stage_proof_mine",
    {
      title: "red_stage_proof_mine",
      description:
        "Mine Assets/Scripts/Tests/** for C# test files matching BDD-name patterns for a given issue id. " +
        "Emits a candidate red-stage proof Markdown block. " +
        "Returns {ok: true, payload: {issue_id, candidate_length, candidate}} — candidate is null when no match. " +
        "TECH-15910.",
      inputSchema: InputSchema.shape,
    },
    async (args) => runWithToolTiming("red_stage_proof_mine", () => handle(args)),
  );
}
