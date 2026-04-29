/**
 * MCP tool: master_plan_health — read `ia_master_plan_health` MV (TECH-3226)
 * + merge `ia_runtime_state.updated_at` (renamed `last_verify_at` for the
 * health surface) into payload.
 *
 * Slug-omitted variant returns `{plans: [...]}` cross-section. Slug-given
 * variant returns single per-slug payload, or `{slug, error: 'not_found'}`
 * when the slug is absent from `ia_master_plans`.
 *
 * db-lifecycle-extensions Stage 2 / TECH-3227.
 *
 * NOTE: After editing this descriptor, restart Claude Code (or run
 * `tsx tools/mcp-ia-server/src/index.ts` script) to refresh the in-memory
 * schema cache — the MCP server caches tool descriptors at session start (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

// ---------------------------------------------------------------------------
// Input schema
// ---------------------------------------------------------------------------

const inputShape = {
  slug: z
    .string()
    .optional()
    .describe(
      "Master-plan slug. When omitted, returns cross-section `{plans: [...]}` " +
        "with row-per-slug for all `ia_master_plans`. When given and slug not " +
        "in `ia_master_plans`, returns `{slug, error: 'not_found'}` " +
        "(partial-result shape; not thrown).",
    ),
};

// ---------------------------------------------------------------------------
// Output shapes
// ---------------------------------------------------------------------------

export interface MasterPlanHealthRow {
  slug: string;
  n_stages: number;
  n_done: number;
  n_in_progress: number;
  n_pending: number;
  oldest_in_progress_age_days: number | null;
  missing_arch_surfaces: string[];
  drift_events_open: number;
  sibling_collisions: string[];
  refreshed_at: string;
  last_verify_at: string | null;
}

export interface MasterPlanHealthPayload {
  slug: string;
  error?: "not_found";
  n_stages?: number;
  n_done?: number;
  n_in_progress?: number;
  n_pending?: number;
  oldest_in_progress_age_days?: number | null;
  missing_arch_surfaces?: string[];
  drift_events_open?: number;
  sibling_collisions?: string[];
  refreshed_at?: string;
  last_verify_at?: string | null;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

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

interface RuntimeStateRow {
  updated_at: Date | string;
}

/**
 * Read `last_verify_at` from `ia_runtime_state` singleton row. Returns null
 * when the row is absent (fresh DB / pre-Step-9.6.5 schema). Never throws.
 */
async function readLastVerifyAt(): Promise<string | null> {
  try {
    const pool = getIaDatabasePool();
    if (!pool) return null;
    const res = await pool.query<RuntimeStateRow>(
      `SELECT updated_at FROM ia_runtime_state WHERE id = 1`,
    );
    if (res.rows.length === 0) return null;
    const u = res.rows[0]!.updated_at;
    return typeof u === "string" ? new Date(u).toISOString() : u.toISOString();
  } catch {
    // Permissive merge: runtime-state read failure should not poison MV read.
    return null;
  }
}

interface HealthMvRow {
  slug: string;
  n_stages: number;
  n_done: number;
  n_in_progress: number;
  n_pending: number;
  oldest_in_progress_age_days: number | null;
  missing_arch_surfaces: string[];
  drift_events_open: number;
  sibling_collisions: string[];
  refreshed_at: Date | string;
}

function toIso(v: Date | string): string {
  return typeof v === "string" ? new Date(v).toISOString() : v.toISOString();
}

function rowToPayload(
  r: HealthMvRow,
  lastVerifyAt: string | null,
): MasterPlanHealthRow {
  return {
    slug: r.slug,
    n_stages: r.n_stages,
    n_done: r.n_done,
    n_in_progress: r.n_in_progress,
    n_pending: r.n_pending,
    oldest_in_progress_age_days: r.oldest_in_progress_age_days,
    missing_arch_surfaces: r.missing_arch_surfaces ?? [],
    drift_events_open: r.drift_events_open,
    sibling_collisions: r.sibling_collisions ?? [],
    refreshed_at: toIso(r.refreshed_at),
    last_verify_at: lastVerifyAt,
  };
}

/**
 * Read MV row(s). Slug given → single row or null. Slug omitted → all rows.
 * Stable shape: missing slug returns null (caller maps to error envelope).
 */
export async function getMasterPlanHealth(
  slug?: string,
): Promise<HealthMvRow[]> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw {
      code: "db_unavailable",
      message: "ia_db pool not initialized",
    };
  }
  if (slug !== undefined) {
    const res = await pool.query<HealthMvRow>(
      `SELECT slug, n_stages, n_done, n_in_progress, n_pending,
              oldest_in_progress_age_days,
              missing_arch_surfaces, drift_events_open, sibling_collisions,
              refreshed_at
         FROM ia_master_plan_health
        WHERE slug = $1`,
      [slug],
    );
    return res.rows;
  }
  const res = await pool.query<HealthMvRow>(
    `SELECT slug, n_stages, n_done, n_in_progress, n_pending,
            oldest_in_progress_age_days,
            missing_arch_surfaces, drift_events_open, sibling_collisions,
            refreshed_at
       FROM ia_master_plan_health
      ORDER BY slug ASC`,
  );
  return res.rows;
}

// ---------------------------------------------------------------------------
// Registration
// ---------------------------------------------------------------------------

type HealthArgs = { slug?: string };

export function registerMasterPlanHealth(server: McpServer): void {
  server.registerTool(
    "master_plan_health",
    {
      description:
        "DB-backed: read `ia_master_plan_health` materialized view (TECH-3226) + " +
        "merge `ia_runtime_state.updated_at` as `last_verify_at`. " +
        "Slug-omitted returns `{plans: [...]}` with row-per-slug for all " +
        "`ia_master_plans`. Slug-given returns single per-slug payload, " +
        "or `{slug, error: 'not_found'}` when slug absent. " +
        "Per-slug payload shape: " +
        "`{slug, n_stages, n_done, n_in_progress, n_pending, " +
        "oldest_in_progress_age_days, missing_arch_surfaces, " +
        "drift_events_open, sibling_collisions, refreshed_at, last_verify_at}`. " +
        "Replaces hand-written cross-plan audit doc workflow. " +
        "Schema-cache restart required after adding this tool (N4): " +
        "restart Claude Code or run `tsx tools/mcp-ia-server/src/index.ts`.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("master_plan_health", async () => {
        const envelope = await wrapTool(
          async (
            input: HealthArgs | undefined,
          ): Promise<
            MasterPlanHealthPayload | { plans: MasterPlanHealthRow[] }
          > => {
            const slug = input?.slug?.trim();
            const lastVerifyAt = await readLastVerifyAt();

            if (slug !== undefined && slug !== "") {
              const rows = await getMasterPlanHealth(slug);
              if (rows.length === 0) {
                return { slug, error: "not_found" } as MasterPlanHealthPayload;
              }
              return rowToPayload(rows[0]!, lastVerifyAt);
            }

            const rows = await getMasterPlanHealth();
            return {
              plans: rows.map((r) => rowToPayload(r, lastVerifyAt)),
            };
          },
        )(args as HealthArgs | undefined);

        return jsonResult(envelope);
      }),
  );
}
