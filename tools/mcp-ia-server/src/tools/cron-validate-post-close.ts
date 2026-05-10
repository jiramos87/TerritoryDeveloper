/**
 * MCP tool: cron_validate_post_close_enqueue
 *
 * Fire-and-forget enqueue of one validate-post-close row into
 * cron_validate_post_close_jobs. Cron handler shells
 * `npm run validate:fast --diff-paths {csv}` scoped to a stage commit.
 *
 * Replaces the synchronous validate:fast run inside ship-cycle Pass B.
 *
 * Lifecycle skills refactor — Phase 2 / weak-spot #10.
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

interface ValidatePostCloseInput {
  slug?: string;
  stage_id?: string;
  commit_sha?: string;
  diff_paths?: string[];
  validate_kind?: string;
  idempotency_key?: string;
}

export function registerCronValidatePostCloseEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_validate_post_close_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one validate-post-close row into cron_validate_post_close_jobs. Cron drainer shells `npm run validate:fast --diff-paths {csv}` scoped to the stage commit. Returns {job_id, status:'queued'} in <100ms. Replaces sync validate run in ship-cycle Pass B.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        stage_id: z.string().describe("Stage id (canonical N.M form preferred)."),
        commit_sha: z.string().optional().describe("Stage commit sha (optional)."),
        diff_paths: z
          .array(z.string())
          .optional()
          .describe("Repo-relative paths touched by the stage commit. Empty → HEAD-diff fallback."),
        validate_kind: z
          .string()
          .optional()
          .describe("Which validate target. Default 'fast'."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional). Duplicate key → no-op enqueue."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_validate_post_close_enqueue", async () => {
        const envelope = await wrapTool(async (input: ValidatePostCloseInput | undefined) => {
          const slug = (input?.slug ?? "").trim();
          const stage_id = (input?.stage_id ?? "").trim();
          if (!slug || !stage_id) {
            throw {
              code: "invalid_input",
              message: "slug and stage_id are required.",
            };
          }
          const pool = getIaDatabasePool();
          if (!pool) {
            throw new IaDbUnavailableError();
          }
          const idempotency_key = input?.idempotency_key ?? null;
          const res = await pool.query<{ job_id: string }>(
            `INSERT INTO cron_validate_post_close_jobs
               (slug, stage_id, commit_sha, diff_paths, validate_kind, idempotency_key)
             VALUES ($1, $2, $3, $4::jsonb, $5, $6)
             ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
             RETURNING job_id::text`,
            [
              slug,
              stage_id,
              input?.commit_sha ?? null,
              JSON.stringify(input?.diff_paths ?? []),
              input?.validate_kind ?? "fast",
              idempotency_key,
            ],
          );
          if (res.rowCount === 0) {
            return { job_id: null, status: "queued", deduped: true };
          }
          return { job_id: res.rows[0]!.job_id, status: "queued" };
        })(args as ValidatePostCloseInput | undefined);
        return jsonResult(envelope);
      }),
  );
}
