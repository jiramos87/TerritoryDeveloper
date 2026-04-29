/**
 * MCP tool: master_plan_cross_impact_scan — join `ia_master_plans` ×
 * `stage_arch_surfaces` × open `arch_changelog` rows (commit_sha IS NULL,
 * kind IN ('edit','spec_edit_commit')) → emit cross-plan drift hotspots +
 * per-plan impact table.
 *
 * db-lifecycle-extensions Stage 2 / TECH-3229. Replaces the hand-written
 * `docs/asset-pipeline-stage-13-1-cross-plan-impact-audit.md` workflow.
 *
 * Schema adaptation note (per §Implementer Latitude): TECH-3229 §Plan Digest
 * cited `arch_surface_links` + `arch_drift_events` tables that do not exist
 * in the live schema. Real surfaces:
 *   - `stage_arch_surfaces (slug, stage_id, surface_slug)` link table
 *   - `arch_changelog (kind, surface_slug, commit_sha, ...)` — open rows
 *      (commit_sha IS NULL, kind IN ('edit','spec_edit_commit')) act as
 *      drift-event proxy.
 *
 * Output payload mirrors the audit doc shape:
 *   - `drift_hotspots: [{surface_slug, drift_events_open, affected_plans[]}]`
 *     — surfaces with ≥1 open drift; affected_plans = slugs that own ≥1 stage
 *     row referencing surface_slug. Sorted DESC by drift count, ties alpha.
 *   - `impact_table: [{slug, surfaces[], drift_count}]` — per-plan rollup.
 *     Sorted DESC by drift_count, ties alpha by slug.
 *
 * Empty input — Zod `{}`. No filters; cross-section by design.
 *
 * NOTE: After editing this descriptor, restart Claude Code (or run
 * `tsx tools/mcp-ia-server/src/index.ts` script) to refresh the in-memory
 * schema cache (N4).
 */

import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

// ---------------------------------------------------------------------------
// Input schema
// ---------------------------------------------------------------------------

const inputShape = {} as const;

// ---------------------------------------------------------------------------
// Output shapes
// ---------------------------------------------------------------------------

export interface DriftHotspot {
  surface_slug: string;
  drift_events_open: number;
  affected_plans: string[];
}

export interface ImpactTableEntry {
  slug: string;
  surfaces: string[];
  drift_count: number;
}

export interface CrossImpactScanPayload {
  drift_hotspots: DriftHotspot[];
  impact_table: ImpactTableEntry[];
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

interface HotspotRow {
  surface_slug: string;
  drift_events_open: number;
  affected_plans: string[];
}

interface ImpactRow {
  slug: string;
  surfaces: string[];
  drift_count: number;
}

/**
 * Pure DB-side roll-up. Single CTE chain so the read is atomic + cheap.
 *
 *   open_drift  : aggregate open `arch_changelog` rows per surface_slug.
 *   plan_surface: distinct (slug, surface_slug) from `stage_arch_surfaces`.
 *   hotspots    : surfaces with ≥1 open drift × plans referencing them.
 *   impact      : per-plan surfaces[] + sum of open drift on those surfaces.
 */
export async function queryCrossImpactScan(): Promise<CrossImpactScanPayload> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw {
      code: "db_unavailable",
      message: "ia_db pool not initialized",
    };
  }

  // Hotspots: surfaces with open drift, plus the plans that touch them.
  const hotspotsRes = await pool.query<HotspotRow>(
    `WITH open_drift AS (
       SELECT surface_slug, COUNT(*)::int AS drift_events_open
         FROM arch_changelog
        WHERE commit_sha IS NULL
          AND kind IN ('edit','spec_edit_commit')
          AND surface_slug IS NOT NULL
        GROUP BY surface_slug
     ),
     plan_surface AS (
       SELECT DISTINCT slug, surface_slug
         FROM stage_arch_surfaces
     )
     SELECT od.surface_slug,
            od.drift_events_open,
            COALESCE(
              ARRAY_AGG(DISTINCT ps.slug ORDER BY ps.slug)
                FILTER (WHERE ps.slug IS NOT NULL),
              ARRAY[]::text[]
            ) AS affected_plans
       FROM open_drift od
  LEFT JOIN plan_surface ps ON ps.surface_slug = od.surface_slug
   GROUP BY od.surface_slug, od.drift_events_open
   ORDER BY od.drift_events_open DESC, od.surface_slug ASC`,
  );

  // Impact: per-plan surface list + summed drift count over those surfaces.
  const impactRes = await pool.query<ImpactRow>(
    `WITH open_drift AS (
       SELECT surface_slug, COUNT(*)::int AS drift_events_open
         FROM arch_changelog
        WHERE commit_sha IS NULL
          AND kind IN ('edit','spec_edit_commit')
          AND surface_slug IS NOT NULL
        GROUP BY surface_slug
     ),
     plan_surface AS (
       SELECT DISTINCT slug, surface_slug
         FROM stage_arch_surfaces
     )
     SELECT mp.slug,
            COALESCE(
              ARRAY_AGG(DISTINCT ps.surface_slug ORDER BY ps.surface_slug)
                FILTER (WHERE ps.surface_slug IS NOT NULL),
              ARRAY[]::text[]
            ) AS surfaces,
            COALESCE(
              SUM(od.drift_events_open) FILTER (WHERE od.surface_slug IS NOT NULL),
              0
            )::int AS drift_count
       FROM ia_master_plans mp
  LEFT JOIN plan_surface ps ON ps.slug = mp.slug
  LEFT JOIN open_drift od ON od.surface_slug = ps.surface_slug
   GROUP BY mp.slug
   ORDER BY drift_count DESC, mp.slug ASC`,
  );

  return {
    drift_hotspots: hotspotsRes.rows.map((r) => ({
      surface_slug: r.surface_slug,
      drift_events_open: r.drift_events_open,
      affected_plans: r.affected_plans ?? [],
    })),
    impact_table: impactRes.rows.map((r) => ({
      slug: r.slug,
      surfaces: r.surfaces ?? [],
      drift_count: r.drift_count,
    })),
  };
}

// ---------------------------------------------------------------------------
// Registration
// ---------------------------------------------------------------------------

export function registerMasterPlanCrossImpactScan(server: McpServer): void {
  server.registerTool(
    "master_plan_cross_impact_scan",
    {
      description:
        "DB-backed: cross-plan drift impact scan. Joins `ia_master_plans` × " +
        "`stage_arch_surfaces` × open `arch_changelog` rows (commit_sha IS NULL, " +
        "kind IN ('edit','spec_edit_commit')). " +
        "Replaces hand-written `docs/asset-pipeline-stage-13-1-cross-plan-impact-audit.md` " +
        "workflow (db-lifecycle-extensions Stage 2 / TECH-3229). " +
        "No input. Output: `{drift_hotspots: [{surface_slug, drift_events_open, " +
        "affected_plans[]}], impact_table: [{slug, surfaces[], drift_count}]}`. " +
        "`drift_hotspots` sorted DESC by drift_events_open then surface_slug ASC. " +
        "`impact_table` sorted DESC by drift_count then slug ASC. " +
        "Schema-cache restart required after adding this tool (N4): " +
        "restart Claude Code or run `tsx tools/mcp-ia-server/src/index.ts`.",
      inputSchema: inputShape,
    },
    async () =>
      runWithToolTiming("master_plan_cross_impact_scan", async () => {
        const envelope = await wrapTool(
          async (): Promise<CrossImpactScanPayload> => {
            return await queryCrossImpactScan();
          },
        )(undefined);

        return jsonResult(envelope);
      }),
  );
}
