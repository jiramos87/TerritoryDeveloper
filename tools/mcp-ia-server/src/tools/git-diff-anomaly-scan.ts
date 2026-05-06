/**
 * MCP tool — `git_diff_anomaly_scan` (TECH-15906).
 *
 * Regex-pack classifier over git diff text. Replaces LLM diff review (~25k
 * tokens) with deterministic anomaly detection in ship-cycle Pass B
 * pre-verify.
 *
 * Wraps tools/scripts/diff-anomaly-classify.mjs via child_process.
 *
 * Anomaly kinds: debug_log, meta_delete, large_hunk, retired_symbol.
 */

import { z } from "zod";
import { execSync, spawnSync } from "child_process";
import * as path from "path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { resolveRepoRoot } from "../config.js";

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

interface AnomalyResult {
  ok: boolean;
  anomaly_count: number;
  anomalies: Array<{ kind: string; description: string; match: string }>;
}

export function registerGitDiffAnomalyScan(server: McpServer): void {
  server.registerTool(
    "git_diff_anomaly_scan",
    {
      description:
        "Regex-pack classifier over git diff text (TECH-15906). Replaces LLM diff review (~25k tokens) in ship-cycle Pass B pre-verify. " +
        "Pass `diff` (git diff HEAD output) or `diff_ref` (e.g. 'HEAD' — runs git diff HEAD internally). " +
        "Returns { ok, anomaly_count, anomalies: [{kind, description, match}] }. " +
        "Anomaly kinds: debug_log (Debug.Log/console.log inserts), meta_delete (.meta file deletions), " +
        "large_hunk (>200 added lines), retired_symbol (known retired symbols added). " +
        "Exit ok=false → anomalies found; caller decides whether to block or warn.",
      inputSchema: {
        diff: z
          .string()
          .optional()
          .describe("Git diff text to classify. Mutually exclusive with diff_ref."),
        diff_ref: z
          .string()
          .optional()
          .describe(
            "Git ref to diff against HEAD (e.g. 'HEAD', 'HEAD~1'). " +
              "Runs `git diff <diff_ref>` in repo root. Mutually exclusive with diff.",
          ),
      },
    },
    async (args) =>
      runWithToolTiming("git_diff_anomaly_scan", async () => {
        const envelope = await wrapTool(
          async (input: { diff?: string; diff_ref?: string } | undefined) => {
            const repoRoot = resolveRepoRoot();
            const scriptPath = path.join(
              repoRoot,
              "tools/scripts/diff-anomaly-classify.mjs",
            );

            let diffText: string;
            if (input?.diff) {
              diffText = input.diff;
            } else {
              const ref = (input?.diff_ref ?? "HEAD").trim();
              try {
                diffText = execSync(`git diff ${ref}`, {
                  cwd: repoRoot,
                  maxBuffer: 10 * 1024 * 1024,
                  encoding: "utf8",
                });
              } catch (e: unknown) {
                throw {
                  code: "git_diff_failed",
                  message: `git diff ${ref} failed: ${(e as Error).message}`,
                };
              }
            }

            try {
              const stdout = execSync(
                `node ${scriptPath} --text ${JSON.stringify(diffText)}`,
                {
                  cwd: repoRoot,
                  maxBuffer: 5 * 1024 * 1024,
                  encoding: "utf8",
                },
              );
              return JSON.parse(stdout) as AnomalyResult;
            } catch (e: unknown) {
              // Script exited non-zero — anomalies found. Parse stdout.
              const err = e as { stdout?: string; message?: string };
              if (err.stdout) {
                try {
                  return JSON.parse(err.stdout) as AnomalyResult;
                } catch {
                  // fall through
                }
              }
              throw {
                code: "classifier_error",
                message: `diff-anomaly-classify.mjs failed: ${err.message}`,
              };
            }
          },
        )(args as { diff?: string; diff_ref?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}
