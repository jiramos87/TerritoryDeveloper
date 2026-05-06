/**
 * MCP tool: cron_task_commit_record_enqueue
 *
 * Fire-and-forget enqueue of one task-commit-record row into cron_task_commit_record_jobs.
 * Mirrors task_commit_record payload shape.
 * P95 < 100 ms (single INSERT + commit).
 * Returns {job_id, status:'queued'}.
 *
 * TECH-18094 / async-cron-jobs Stage 2.0.2
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { IaDbUnavailableError } from "../ia-db/queries.js";

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

export function registerCronTaskCommitRecordEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_task_commit_record_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one task-commit-record row into cron_task_commit_record_jobs. Cron supervisor drains it to ia_task_commits. Same payload shape as task_commit_record. Returns {job_id, status:'queued'} in <100ms.",
      inputSchema: {
        task_id: z.string().describe("Task id e.g. TECH-776."),
        commit_sha: z.string().describe("Git commit sha (short or full)."),
        commit_kind: z
          .enum(["feat", "fix", "chore", "docs", "refactor", "test"])
          .describe("Conventional-commit prefix: feat|fix|chore|docs|refactor|test."),
        message: z.string().optional().describe("Optional commit subject line."),
        slug: z.string().optional().describe("Master-plan slug (optional, for audit)."),
        stage_id: z.string().optional().describe("Stage id (optional, for audit)."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional). Duplicate key → no-op enqueue."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_task_commit_record_enqueue", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  task_id?: string;
                  commit_sha?: string;
                  commit_kind?: "feat" | "fix" | "chore" | "docs" | "refactor" | "test";
                  message?: string;
                  slug?: string;
                  stage_id?: string;
                  idempotency_key?: string;
                }
              | undefined,
          ) => {
            const task_id = (input?.task_id ?? "").trim().toUpperCase();
            const commit_sha = (input?.commit_sha ?? "").trim();
            const commit_kind = input?.commit_kind;
            if (!task_id || !commit_sha || !commit_kind) {
              throw {
                code: "invalid_input",
                message: "task_id, commit_sha, and commit_kind are required.",
              };
            }
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            const idempotency_key = input?.idempotency_key ?? null;
            const res = await pool.query<{ job_id: string }>(
              `INSERT INTO cron_task_commit_record_jobs
                 (task_id, commit_sha, commit_kind, message, slug, stage_id, idempotency_key)
               VALUES ($1, $2, $3, $4, $5, $6, $7)
               ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
               RETURNING job_id::text`,
              [
                task_id,
                commit_sha,
                commit_kind,
                input?.message ?? null,
                input?.slug ?? null,
                input?.stage_id ?? null,
                idempotency_key,
              ],
            );
            if (res.rowCount === 0) {
              return { job_id: null, status: "queued", deduped: true };
            }
            return { job_id: res.rows[0]!.job_id, status: "queued" };
          },
        )(
          args as
            | {
                task_id?: string;
                commit_sha?: string;
                commit_kind?: "feat" | "fix" | "chore" | "docs" | "refactor" | "test";
                message?: string;
                slug?: string;
                stage_id?: string;
                idempotency_key?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}
