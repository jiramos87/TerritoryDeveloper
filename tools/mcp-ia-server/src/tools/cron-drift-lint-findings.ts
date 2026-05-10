/**
 * MCP tools: cron_drift_lint_findings_enqueue, cron_drift_lint_findings_promote
 *
 * Two-phase commit pattern for drift-lint findings:
 *   1. Agent enqueues findings with status='staged' BEFORE master_plan_bundle_apply.
 *   2. master_plan_bundle_apply success calls promote_drift_lint_staged()
 *      which flips matching staged rows → 'queued'.
 *   3. Cron drainer skips 'staged' rows; only drains 'queued'.
 *
 * Crash-safe replacement for the in-memory drift buffer in ship-plan Phase 6.
 *
 * Distinct from cron_drift_lint_jobs (TECH-18105 sweep): different queue, different
 * concern (post-bundle findings stash vs. committed-code sweep).
 *
 * Lifecycle skills refactor — Phase 2 / weak-spot #2.
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

interface FindingsEnqueueInput {
  slug?: string;
  version?: number;
  findings?: unknown;
  n_resolved?: number;
  n_unresolved?: number;
  status?: "staged" | "queued";
  idempotency_key?: string;
}

interface PromoteInput {
  slug?: string;
  version?: number;
}

export function registerCronDriftLintFindingsTools(server: McpServer): void {
  server.registerTool(
    "cron_drift_lint_findings_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one drift-lint findings stash row into cron_drift_lint_findings_jobs. Default status='staged' (drainer skips). Flipped to 'queued' atomically by master_plan_bundle_apply via promote_drift_lint_staged(). Returns {job_id, status} in <100ms. Crash-safe two-phase commit replacing ship-plan Phase 6 in-memory drift buffer.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        version: z.number().int().describe("Plan version this stash belongs to."),
        findings: z
          .array(z.any())
          .describe("Findings array (jsonb). Same shape as ship-stage-journal-schema drift_lint_summary payload."),
        n_resolved: z.number().int().optional().describe("Resolved finding count (default 0)."),
        n_unresolved: z
          .number()
          .int()
          .optional()
          .describe("Unresolved finding count (default 0)."),
        status: z
          .enum(["staged", "queued"])
          .optional()
          .describe("Initial status. Default 'staged' for two-phase commit; pass 'queued' to drain immediately."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key. Recommended pattern `${slug}:${version}:drift-lint`."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_drift_lint_findings_enqueue", async () => {
        const envelope = await wrapTool(async (input: FindingsEnqueueInput | undefined) => {
          const slug = (input?.slug ?? "").trim();
          if (!slug) {
            throw { code: "invalid_input", message: "slug is required." };
          }
          if (input?.version === undefined || input?.version === null) {
            throw { code: "invalid_input", message: "version is required." };
          }
          const findings = Array.isArray(input?.findings) ? input.findings : [];
          const pool = getIaDatabasePool();
          if (!pool) {
            throw new IaDbUnavailableError();
          }
          const idempotency_key = input?.idempotency_key ?? null;
          const status = input?.status ?? "staged";
          const res = await pool.query<{ job_id: string; status: string }>(
            `INSERT INTO cron_drift_lint_findings_jobs
               (slug, version, findings, n_resolved, n_unresolved, status, idempotency_key)
             VALUES ($1, $2, $3::jsonb, $4, $5, $6::cron_job_status, $7)
             ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
             RETURNING job_id::text, status::text`,
            [
              slug,
              input.version,
              JSON.stringify(findings),
              input?.n_resolved ?? 0,
              input?.n_unresolved ?? 0,
              status,
              idempotency_key,
            ],
          );
          if (res.rowCount === 0) {
            return { job_id: null, status, deduped: true };
          }
          return { job_id: res.rows[0]!.job_id, status: res.rows[0]!.status };
        })(args as FindingsEnqueueInput | undefined);
        return jsonResult(envelope);
      }),
  );

  server.registerTool(
    "cron_drift_lint_findings_promote",
    {
      description:
        "Manual escape hatch: flip cron_drift_lint_findings_jobs rows for (slug, version) from status='staged' → 'queued'. Normally invoked atomically inside master_plan_bundle_apply via promote_drift_lint_staged(); this tool exposes it for crash-recovery / manual replay. Returns {flipped:int}.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        version: z.number().int().describe("Plan version."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_drift_lint_findings_promote", async () => {
        const envelope = await wrapTool(async (input: PromoteInput | undefined) => {
          const slug = (input?.slug ?? "").trim();
          if (!slug || input?.version === undefined || input?.version === null) {
            throw { code: "invalid_input", message: "slug and version are required." };
          }
          const pool = getIaDatabasePool();
          if (!pool) {
            throw new IaDbUnavailableError();
          }
          const res = await pool.query<{ flipped: number }>(
            "SELECT promote_drift_lint_staged($1, $2)::int AS flipped",
            [slug, input.version],
          );
          return { flipped: res.rows[0]?.flipped ?? 0 };
        })(args as PromoteInput | undefined);
        return jsonResult(envelope);
      }),
  );
}
