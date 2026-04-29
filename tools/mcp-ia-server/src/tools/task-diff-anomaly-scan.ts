/**
 * MCP tool — `task_diff_anomaly_scan(slug)` (db-lifecycle-extensions Stage 3
 * / TECH-3406).
 *
 * Diffs JSONB cols `expected_files_touched` ↔ `actual_files_touched` per
 * Task in a master plan, applying tolerance globs from
 * `ia_master_plans.tolerance_globs` to `unexpected_files` only (NOT
 * `missed_files` — author intent unmet is always real signal).
 *
 * NULL semantics (Q3 lock): pre-`0047` Tasks (either col NULL) are silently
 * skipped from output — never errored, never warned. Forward-only BF lock
 * tolerates pre-existing rows that lack the new cols.
 *
 * Output ordering: by `task_id` ASC for deterministic diff/snapshot tests.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { IaDbUnavailableError } from "../ia-db/queries.js";
import { matchesAny } from "../lib/glob-matcher.js";

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

interface AnomalyRow {
  task_id: string;
  expected_files: string[];
  actual_files: string[];
  unexpected_files: string[];
  missed_files: string[];
}

export function registerTaskDiffAnomalyScan(server: McpServer): void {
  server.registerTool(
    "task_diff_anomaly_scan",
    {
      description:
        "DB-backed JSONB diff: per Task in `slug`, compare `expected_files_touched` ↔ `actual_files_touched`, apply `ia_master_plans.tolerance_globs` to `unexpected_files` only (missed always anomalous). Returns `{ok, rows: [{task_id, expected_files, actual_files, unexpected_files, missed_files}]}` ordered by task_id ASC. Pre-`0047` Tasks (either col NULL) silently skipped.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
      },
    },
    async (args) =>
      runWithToolTiming("task_diff_anomaly_scan", async () => {
        const envelope = await wrapTool(async (input: { slug: string }) => {
          const slug = (input.slug ?? "").trim();
          if (!slug) {
            throw { code: "invalid_input", message: "slug is required" };
          }
          const pool = getIaDatabasePool();
          if (!pool) {
            throw new IaDbUnavailableError();
          }

          // Fetch tolerance globs (default `[]`).
          const mpRes = await pool.query<{ tolerance_globs: unknown }>(
            `SELECT tolerance_globs FROM ia_master_plans WHERE slug = $1`,
            [slug],
          );
          const tolerance_globs: string[] = Array.isArray(mpRes.rows[0]?.tolerance_globs)
            ? (mpRes.rows[0]!.tolerance_globs as unknown[]).map((s) => String(s))
            : [];

          // Fetch Tasks with both JSONB cols populated (NULL → skip).
          const trRes = await pool.query<{
            task_id: string;
            expected_files_touched: unknown;
            actual_files_touched: unknown;
          }>(
            `SELECT task_id, expected_files_touched, actual_files_touched
               FROM ia_tasks
              WHERE slug = $1
                AND expected_files_touched IS NOT NULL
                AND actual_files_touched IS NOT NULL
              ORDER BY task_id ASC`,
            [slug],
          );

          const rows: AnomalyRow[] = [];
          for (const r of trRes.rows) {
            const expected: string[] = Array.isArray(r.expected_files_touched)
              ? (r.expected_files_touched as unknown[]).map((s) => String(s))
              : [];
            const actual: string[] = Array.isArray(r.actual_files_touched)
              ? (r.actual_files_touched as unknown[]).map((s) => String(s))
              : [];
            const expectedSet = new Set(expected);
            const actualSet = new Set(actual);
            // Unexpected = actual \ expected, then filtered by tolerance globs.
            const unexpected_raw = actual.filter((p) => !expectedSet.has(p));
            const unexpected_files =
              tolerance_globs.length === 0
                ? unexpected_raw
                : unexpected_raw.filter((p) => !matchesAny(p, tolerance_globs));
            // Missed = expected \ actual (no tolerance).
            const missed_files = expected.filter((p) => !actualSet.has(p));
            rows.push({
              task_id: r.task_id,
              expected_files: expected,
              actual_files: actual,
              unexpected_files,
              missed_files,
            });
          }

          return { ok: true, slug, rows };
        })(args as { slug: string });
        return jsonResult(envelope);
      }),
  );
}
