/**
 * MCP tool: cron_glossary_backlinks_enqueue
 *
 * Fire-and-forget enqueue of one glossary back-link enrichment run into
 * cron_glossary_backlinks_jobs. Cron supervisor drains it by shelling to
 * node tools/scripts/glossary-backlink-enrich.mjs --plan-id {slug}.
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

export function registerCronGlossaryBacklinksEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_glossary_backlinks_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one glossary back-link enrichment run into cron_glossary_backlinks_jobs. Cron supervisor drains it by running node tools/scripts/glossary-backlink-enrich.mjs --plan-id {slug} (cadence: */5 * * * *). slug: master-plan slug. plan_id: optional uuid. Returns {job_id, status:'queued'} in <100ms.",
      inputSchema: {
        slug: z
          .string()
          .describe("Master-plan slug (e.g. 'async-cron-jobs')."),
        plan_id: z
          .string()
          .uuid()
          .optional()
          .describe("ia_master_plans.plan_id UUID (optional)."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional). Duplicate key → no-op enqueue."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_glossary_backlinks_enqueue", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug: string;
                  plan_id?: string;
                  idempotency_key?: string;
                }
              | undefined,
          ) => {
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            if (!input?.slug) {
              throw new Error("slug is required");
            }
            const slug = input.slug;
            const plan_id = input?.plan_id ?? null;
            const idempotency_key = input?.idempotency_key ?? null;
            const res = await pool.query<{ job_id: string }>(
              `INSERT INTO cron_glossary_backlinks_jobs
                 (slug, plan_id, idempotency_key)
               VALUES ($1, $2, $3)
               ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
               RETURNING job_id::text`,
              [slug, plan_id, idempotency_key],
            );
            if (res.rowCount === 0) {
              return { job_id: null, status: "queued", deduped: true };
            }
            return { job_id: res.rows[0]!.job_id, status: "queued" };
          },
        )(
          args as
            | {
                slug: string;
                plan_id?: string;
                idempotency_key?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}
