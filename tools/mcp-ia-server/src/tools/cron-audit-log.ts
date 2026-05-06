/**
 * MCP tool: cron_audit_log_enqueue
 *
 * Fire-and-forget enqueue of one audit-log row into cron_audit_log_jobs.
 * Mirrors master_plan_change_log_append payload shape.
 * P95 < 100 ms (single INSERT + commit).
 * Returns {job_id, status:'queued'}.
 *
 * TECH-18091 / async-cron-jobs Stage 1.0.3
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

export function registerCronAuditLogEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_audit_log_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one audit-log row into cron_audit_log_jobs. Cron supervisor drains it to ia_master_plan_change_log. Same payload shape as master_plan_change_log_append. Returns {job_id, status:'queued'} in <100ms.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        audit_kind: z
          .string()
          .describe("Short tag e.g. `stage_closed`, `closeout-digest`."),
        body: z.string().describe("Markdown body of the entry."),
        version: z
          .number()
          .int()
          .optional()
          .describe("Plan version (default 1)."),
        actor: z.string().optional().describe("Who recorded the entry."),
        commit_sha: z.string().optional().describe("Commit sha (optional)."),
        stage_id: z.string().optional().describe("Stage id (optional)."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional). Duplicate key → no-op enqueue."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_audit_log_enqueue", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug?: string;
                  audit_kind?: string;
                  body?: string;
                  version?: number;
                  actor?: string;
                  commit_sha?: string;
                  stage_id?: string;
                  idempotency_key?: string;
                }
              | undefined,
          ) => {
            const slug = (input?.slug ?? "").trim();
            const audit_kind = (input?.audit_kind ?? "").trim();
            const body = input?.body ?? "";
            if (!slug || !audit_kind || !body) {
              throw {
                code: "invalid_input",
                message: "slug, audit_kind, and body are required.",
              };
            }
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            const idempotency_key = input?.idempotency_key ?? null;
            const res = await pool.query<{ job_id: string }>(
              `INSERT INTO cron_audit_log_jobs
                 (slug, version, audit_kind, body, actor, commit_sha, stage_id, idempotency_key)
               VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
               ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
               RETURNING job_id::text`,
              [
                slug,
                input?.version ?? 1,
                audit_kind,
                body,
                input?.actor ?? null,
                input?.commit_sha ?? null,
                input?.stage_id ?? null,
                idempotency_key,
              ],
            );
            if (res.rowCount === 0) {
              // Idempotency dedup — row already queued
              return { job_id: null, status: "queued", deduped: true };
            }
            return { job_id: res.rows[0]!.job_id, status: "queued" };
          },
        )(
          args as
            | {
                slug?: string;
                audit_kind?: string;
                body?: string;
                version?: number;
                actor?: string;
                commit_sha?: string;
                stage_id?: string;
                idempotency_key?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}
