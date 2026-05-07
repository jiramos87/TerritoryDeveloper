/**
 * MCP tool: cron_anchor_reindex_enqueue
 *
 * Fire-and-forget enqueue of one anchor reindex run into
 * cron_anchor_reindex_jobs. Cron supervisor drains it by running
 * npm run generate:ia-indexes -- --write-anchors (upserts ia_spec_anchors).
 * P95 < 100 ms (single INSERT + commit).
 * Returns {job_id, status:'queued'}.
 *
 * TECH-18102 / async-cron-jobs Stage 4.0.2
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

export function registerCronAnchorReindexEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_anchor_reindex_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one anchor reindex run into cron_anchor_reindex_jobs. Cron supervisor drains it by running npm run generate:ia-indexes -- --write-anchors (upserts ia_spec_anchors rows; cadence: */5 * * * *). paths: changed spec file paths (informational, for future per-path sub-scan support). Returns {job_id, status:'queued'} in <100ms.",
      inputSchema: {
        paths: z
          .array(z.string())
          .optional()
          .describe("Changed spec file paths to reindex (e.g. ['ia/specs/glossary.md']). Optional — empty or omitted = full reindex."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional). Duplicate key → no-op enqueue."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_anchor_reindex_enqueue", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  paths?: string[];
                  idempotency_key?: string;
                }
              | undefined,
          ) => {
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            const paths = input?.paths ?? [];
            const idempotency_key = input?.idempotency_key ?? null;
            const res = await pool.query<{ job_id: string }>(
              `INSERT INTO cron_anchor_reindex_jobs
                 (paths, idempotency_key)
               VALUES ($1, $2)
               ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
               RETURNING job_id::text`,
              [paths, idempotency_key],
            );
            if (res.rowCount === 0) {
              return { job_id: null, status: "queued", deduped: true };
            }
            return { job_id: res.rows[0]!.job_id, status: "queued" };
          },
        )(
          args as
            | {
                paths?: string[];
                idempotency_key?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}
