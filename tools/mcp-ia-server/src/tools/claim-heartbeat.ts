/**
 * MCP tools: claim_heartbeat + claims_sweep.
 *
 * V2 row-only — no holder identity. Refreshed rows addressed by
 * (slug, section_id) or (slug, stage_id). Stage heartbeat cascades to the
 * parent section claim via `ia_stages.section_id` lookup so a single call
 * keeps both rows alive during `/ship-stage` Pass A iterations.
 *
 * `claims_sweep()` — background sweep using
 * `carcass_config.claim_heartbeat_timeout_minutes`. Releases stale rows
 * (last_heartbeat older than timeout) in both tables.
 *
 * parallel-carcass §6.2 / D4 (V2). Schema-cache restart required after add (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

const heartbeatShape = {
  slug: z.string().describe("Master-plan slug."),
  section_id: z
    .string()
    .optional()
    .describe(
      "Section id. When set without stage_id, refreshes section claim plus all open stage claims in the section.",
    ),
  stage_id: z
    .string()
    .optional()
    .describe(
      "Stage id. When set, refreshes stage claim plus parent section claim (looked up via ia_stages.section_id).",
    ),
};

const sweepShape = {};

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

type HeartbeatArgs = {
  slug: string;
  section_id?: string;
  stage_id?: string;
};

export interface HeartbeatResult {
  slug: string;
  section_id: string | null;
  stage_id: string | null;
  section_claims_refreshed: number;
  stage_claims_refreshed: number;
}

export interface SweepResult {
  timeout_minutes: number;
  section_claims_released: number;
  stage_claims_released: number;
}

export async function applyHeartbeat(
  args: HeartbeatArgs,
): Promise<HeartbeatResult> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }
  const { slug, section_id, stage_id } = args;
  if (!section_id && !stage_id) {
    throw {
      code: "missing_target",
      message: "claim_heartbeat requires section_id or stage_id",
    };
  }

  // Stage path: refresh stage claim + parent section claim.
  if (stage_id) {
    const stgRow = await pool.query<{ section_id: string | null }>(
      `SELECT section_id FROM ia_stages
        WHERE slug = $1 AND stage_id = $2`,
      [slug, stage_id],
    );
    if (stgRow.rows.length === 0) {
      throw {
        code: "stage_not_found",
        message: `stage ${stage_id} of plan ${slug} not found`,
      };
    }
    const parent_section_id = stgRow.rows[0]!.section_id;

    const stg = await pool.query(
      `UPDATE ia_stage_claims
          SET last_heartbeat = now()
        WHERE slug = $1 AND stage_id = $2 AND released_at IS NULL`,
      [slug, stage_id],
    );

    let secCount = 0;
    if (parent_section_id) {
      const sec = await pool.query(
        `UPDATE ia_section_claims
            SET last_heartbeat = now()
          WHERE slug = $1 AND section_id = $2 AND released_at IS NULL`,
        [slug, parent_section_id],
      );
      secCount = sec.rowCount ?? 0;
    }

    return {
      slug,
      section_id: parent_section_id,
      stage_id,
      section_claims_refreshed: secCount,
      stage_claims_refreshed: stg.rowCount ?? 0,
    };
  }

  // Section path: refresh section claim + cascade to all open stage claims
  // whose stages belong to this section.
  const sec = await pool.query(
    `UPDATE ia_section_claims
        SET last_heartbeat = now()
      WHERE slug = $1 AND section_id = $2 AND released_at IS NULL`,
    [slug, section_id!],
  );
  const stg = await pool.query(
    `UPDATE ia_stage_claims sc
        SET last_heartbeat = now()
       FROM ia_stages s
      WHERE sc.slug = s.slug
        AND sc.stage_id = s.stage_id
        AND sc.slug = $1
        AND s.section_id = $2
        AND sc.released_at IS NULL`,
    [slug, section_id!],
  );
  return {
    slug,
    section_id: section_id!,
    stage_id: null,
    section_claims_refreshed: sec.rowCount ?? 0,
    stage_claims_refreshed: stg.rowCount ?? 0,
  };
}

export async function applySweep(): Promise<SweepResult> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }
  const cfg = await pool.query<{ value: string }>(
    `SELECT value FROM carcass_config
      WHERE key = 'claim_heartbeat_timeout_minutes'`,
  );
  const timeoutMin =
    cfg.rows.length > 0 ? parseInt(cfg.rows[0]!.value, 10) || 10 : 10;

  // Time-based only — same threshold for both tables.
  const sec = await pool.query(
    `UPDATE ia_section_claims
        SET released_at = now()
      WHERE released_at IS NULL
        AND last_heartbeat < now() - ($1 || ' minutes')::interval`,
    [String(timeoutMin)],
  );
  const stg = await pool.query(
    `UPDATE ia_stage_claims
        SET released_at = now()
      WHERE released_at IS NULL
        AND last_heartbeat < now() - ($1 || ' minutes')::interval`,
    [String(timeoutMin)],
  );

  return {
    timeout_minutes: timeoutMin,
    section_claims_released: sec.rowCount ?? 0,
    stage_claims_released: stg.rowCount ?? 0,
  };
}

export function registerClaimHeartbeatTools(server: McpServer): void {
  server.registerTool(
    "claim_heartbeat",
    {
      description:
        "DB-backed mutate (V2 row-only): refresh `last_heartbeat = now()` " +
        "on claim rows addressed by row key. Pass `stage_id` to refresh the " +
        "stage claim plus parent section claim (most common — Pass A loop). " +
        "Pass `section_id` to refresh the section claim plus cascade to all " +
        "open stage claims in that section. parallel-carcass §6.2 (D4). " +
        "Schema-cache restart required after add (N4).",
      inputSchema: heartbeatShape,
    },
    async (args) =>
      runWithToolTiming("claim_heartbeat", async () => {
        const envelope = await wrapTool(
          async (input: HeartbeatArgs): Promise<HeartbeatResult> =>
            applyHeartbeat(input),
        )(args as HeartbeatArgs);
        return jsonResult(envelope);
      }),
  );

  server.registerTool(
    "claims_sweep",
    {
      description:
        "DB-backed mutate: release stale claim rows in both " +
        "`ia_section_claims` and `ia_stage_claims` whose `last_heartbeat` " +
        "is older than `carcass_config.claim_heartbeat_timeout_minutes` " +
        "(default 10). Time-based only — V2 row-only. Returns counts. " +
        "Idempotent. Schema-cache restart required after add (N4).",
      inputSchema: sweepShape,
    },
    async (_args) =>
      runWithToolTiming("claims_sweep", async () => {
        const envelope = await wrapTool(
          async (_input: unknown): Promise<SweepResult> => applySweep(),
        )(undefined);
        return jsonResult(envelope);
      }),
  );
}
