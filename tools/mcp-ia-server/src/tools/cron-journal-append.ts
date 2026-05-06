/**
 * MCP tool: cron_journal_append_enqueue
 *
 * Fire-and-forget enqueue of one journal row into cron_journal_append_jobs.
 * Mirrors journal_append payload shape.
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

export function registerCronJournalAppendEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_journal_append_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one journal row into cron_journal_append_jobs. Cron supervisor drains it to ia_ship_stage_journal. Same payload shape as journal_append. Returns {job_id, status:'queued'} in <100ms.",
      inputSchema: {
        session_id: z.string().describe("Agent session id (uuid or slug)."),
        phase: z
          .string()
          .describe("Phase name e.g. `pass_a.implement`, `pass_b.verify`."),
        payload_kind: z
          .string()
          .describe("Discriminator e.g. `tool_call`, `phase_checkpoint`."),
        payload: z
          .record(z.string(), z.unknown())
          .describe("Event-body object (jsonb)."),
        task_id: z.string().optional().describe("Task context (optional)."),
        slug: z.string().optional().describe("Master-plan slug (optional)."),
        stage_id: z.string().optional().describe("Stage id (optional)."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional)."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_journal_append_enqueue", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  session_id?: string;
                  phase?: string;
                  payload_kind?: string;
                  payload?: Record<string, unknown>;
                  task_id?: string;
                  slug?: string;
                  stage_id?: string;
                  idempotency_key?: string;
                }
              | undefined,
          ) => {
            const session_id = (input?.session_id ?? "").trim();
            const phase = (input?.phase ?? "").trim();
            const payload_kind = (input?.payload_kind ?? "").trim();
            if (!session_id || !phase || !payload_kind) {
              throw {
                code: "invalid_input",
                message:
                  "session_id, phase, and payload_kind are required.",
              };
            }
            if (
              !input?.payload ||
              typeof input.payload !== "object" ||
              Array.isArray(input.payload)
            ) {
              throw {
                code: "invalid_input",
                message: "payload must be an object (non-null, non-array).",
              };
            }
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            const idempotency_key = input?.idempotency_key ?? null;
            const res = await pool.query<{ job_id: string }>(
              `INSERT INTO cron_journal_append_jobs
                 (session_id, phase, payload_kind, payload, task_id, slug, stage_id, idempotency_key)
               VALUES ($1::uuid, $2, $3, $4::jsonb, $5, $6, $7, $8)
               ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
               RETURNING job_id::text`,
              [
                session_id,
                phase,
                payload_kind,
                JSON.stringify(input.payload),
                input?.task_id ?? null,
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
                session_id?: string;
                phase?: string;
                payload_kind?: string;
                payload?: Record<string, unknown>;
                task_id?: string;
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
