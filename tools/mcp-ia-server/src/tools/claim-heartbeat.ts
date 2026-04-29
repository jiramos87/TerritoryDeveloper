/**
 * MCP tools: claim_heartbeat + claims_sweep.
 *
 * `claim_heartbeat({session_id})` — single call refreshes `last_heartbeat`
 * across both `ia_section_claims` and `ia_stage_claims` for a session.
 *
 * `claims_sweep()` — background sweep using
 * `carcass_config.claim_heartbeat_timeout_minutes`. Releases stale rows
 * (last_heartbeat older than timeout) in both tables.
 *
 * parallel-carcass §6.2 / D4. Schema-cache restart required after add (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

const heartbeatShape = {
  session_id: z.string().describe("Caller session id."),
};

const sweepShape = {};

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

export interface HeartbeatResult {
  session_id: string;
  section_claims_refreshed: number;
  stage_claims_refreshed: number;
}

export interface SweepResult {
  timeout_minutes: number;
  section_claims_released: number;
  stage_claims_released: number;
}

export async function applyHeartbeat(
  session_id: string,
): Promise<HeartbeatResult> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }
  const sec = await pool.query(
    `UPDATE ia_section_claims
        SET last_heartbeat = now()
      WHERE session_id = $1 AND released_at IS NULL`,
    [session_id],
  );
  const stg = await pool.query(
    `UPDATE ia_stage_claims
        SET last_heartbeat = now()
      WHERE session_id = $1 AND released_at IS NULL`,
    [session_id],
  );
  return {
    session_id,
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

  // Section sweep first; cascade releases any stage claim still attached to
  // a stage in a swept section.
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
        "DB-backed mutate: refresh `last_heartbeat = now()` across BOTH " +
        "`ia_section_claims` and `ia_stage_claims` for a session_id in a " +
        "single call. Returns counts. parallel-carcass §6.2 (D4). " +
        "Schema-cache restart required after add (N4).",
      inputSchema: heartbeatShape,
    },
    async (args) =>
      runWithToolTiming("claim_heartbeat", async () => {
        const envelope = await wrapTool(
          async (input: { session_id: string }): Promise<HeartbeatResult> =>
            applyHeartbeat(input.session_id),
        )(args as { session_id: string });
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
        "(default 10). Returns counts. Idempotent. " +
        "Schema-cache restart required after add (N4).",
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
