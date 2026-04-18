/**
 * MCP tool: unity_bridge_lease — acquire, release, or check Play Mode ownership lease.
 *
 * Ensures only one agent drives Unity Play Mode at a time when multiple agents run in parallel.
 * Backed by agent_bridge_lease table (migration 0010). TTL of 8 min provides crash-safety:
 * if an agent crashes after enter_play_mode, the lease auto-expires and the next agent proceeds.
 *
 * Workflow: acquire → enter_play_mode → [debug_context_bundle] → exit_play_mode → release.
 * On lease_unavailable: retry with 60 s backoff up to 10 min, then emit play_mode_lease: skipped_busy.
 */

import { z } from "zod";
import type { Pool } from "pg";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";

/** Play Mode lease TTL in seconds. Must exceed longest expected Play Mode session (~4 min). */
const LEASE_TTL_SECONDS = 8 * 60;

export const unityBridgeLeaseInputSchema = z.object({
  action: z
    .enum(["acquire", "release", "status"])
    .describe(
      "acquire: claim play_mode lease (idempotent when same agent_id already holds it; returns lease_id). release: relinquish by lease_id after exit_play_mode completes. status: check current holder without acquiring.",
    ),
  agent_id: z
    .string()
    .min(1)
    .optional()
    .describe(
      "Caller identity — use issue id (e.g. 'TECH-121') or a session tag. Required for acquire.",
    ),
  lease_id: z
    .string()
    .uuid()
    .optional()
    .describe("UUID returned by a prior acquire. Required for release."),
  kind: z
    .enum(["play_mode"])
    .default("play_mode")
    .describe("Lease kind. Only play_mode is supported."),
});

export type UnityBridgeLeaseInput = z.infer<typeof unityBridgeLeaseInputSchema>;

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

async function sweepExpiredLeases(pool: Pool, kind: string): Promise<void> {
  await pool.query(
    `UPDATE agent_bridge_lease SET status = 'expired'
     WHERE status = 'active' AND kind = $1 AND expires_at < now()`,
    [kind],
  );
}

export async function runUnityBridgeLease(
  input: UnityBridgeLeaseInput,
  pool: Pool,
): Promise<unknown> {
  // Always sweep stale leases before any operation.
  await sweepExpiredLeases(pool, input.kind);

  if (input.action === "status") {
    const { rows } = await pool.query<{
      lease_id: string;
      agent_id: string;
      acquired_at: Date;
      expires_at: Date;
    }>(
      `SELECT lease_id, agent_id, acquired_at, expires_at
       FROM agent_bridge_lease
       WHERE kind = $1 AND status = 'active'
       LIMIT 1`,
      [input.kind],
    );
    if (rows.length === 0) {
      return { ok: true, lease_available: true, kind: input.kind };
    }
    const row = rows[0];
    return {
      ok: true,
      lease_available: false,
      kind: input.kind,
      held_by: row.agent_id,
      lease_id: String(row.lease_id),
      acquired_at: row.acquired_at.toISOString(),
      expires_at: row.expires_at.toISOString(),
    };
  }

  if (input.action === "acquire") {
    if (!input.agent_id) {
      return {
        ok: false,
        error: "agent_id_required",
        message: "agent_id is required for acquire.",
      };
    }

    // Idempotent: if this agent already holds the lease, return it.
    const { rows: existing } = await pool.query<{
      lease_id: string;
      acquired_at: Date;
      expires_at: Date;
    }>(
      `SELECT lease_id, acquired_at, expires_at
       FROM agent_bridge_lease
       WHERE kind = $1 AND status = 'active' AND agent_id = $2
       LIMIT 1`,
      [input.kind, input.agent_id],
    );
    if (existing.length > 0) {
      const row = existing[0];
      return {
        ok: true,
        acquired: true,
        idempotent: true,
        lease_id: String(row.lease_id),
        agent_id: input.agent_id,
        kind: input.kind,
        acquired_at: row.acquired_at.toISOString(),
        expires_at: row.expires_at.toISOString(),
      };
    }

    // Try to insert (partial unique index blocks two active leases of the same kind).
    try {
      const { rows } = await pool.query<{
        lease_id: string;
        acquired_at: Date;
        expires_at: Date;
      }>(
        `INSERT INTO agent_bridge_lease (agent_id, kind, expires_at)
         VALUES ($1, $2, now() + ($3 || ' seconds')::interval)
         RETURNING lease_id, acquired_at, expires_at`,
        [input.agent_id, input.kind, String(LEASE_TTL_SECONDS)],
      );
      const row = rows[0];
      return {
        ok: true,
        acquired: true,
        idempotent: false,
        lease_id: String(row.lease_id),
        agent_id: input.agent_id,
        kind: input.kind,
        acquired_at: row.acquired_at.toISOString(),
        expires_at: row.expires_at.toISOString(),
      };
    } catch (e: unknown) {
      // Postgres unique violation (23505) → another agent holds the active lease.
      if ((e as { code?: string }).code === "23505") {
        const { rows: holder } = await pool.query<{
          lease_id: string;
          agent_id: string;
          expires_at: Date;
        }>(
          `SELECT lease_id, agent_id, expires_at
           FROM agent_bridge_lease
           WHERE kind = $1 AND status = 'active'
           LIMIT 1`,
          [input.kind],
        );
        const h = holder[0];
        return {
          ok: false,
          error: "lease_unavailable",
          message: `play_mode lease held by '${h?.agent_id ?? "unknown"}'. Retry after ${h?.expires_at?.toISOString() ?? "unknown"} or when released. If idle >8 min the lease auto-expires.`,
          held_by: h?.agent_id ?? null,
          held_lease_id: h ? String(h.lease_id) : null,
          expires_at: h?.expires_at?.toISOString() ?? null,
        };
      }
      throw e;
    }
  }

  if (input.action === "release") {
    if (!input.lease_id) {
      return {
        ok: false,
        error: "lease_id_required",
        message: "lease_id (uuid from acquire) is required for release.",
      };
    }
    const { rowCount } = await pool.query(
      `UPDATE agent_bridge_lease
       SET status = 'released', released_at = now()
       WHERE lease_id = $1::uuid AND status = 'active'`,
      [input.lease_id],
    );
    if ((rowCount ?? 0) === 0) {
      return {
        ok: false,
        error: "lease_not_found",
        message:
          "No active lease for that lease_id (already released, expired, or wrong id). Safe to ignore if play_mode is already exited.",
      };
    }
    return { ok: true, released: true, lease_id: input.lease_id };
  }

  return { ok: false, error: "unknown_action", message: `Unknown action: ${String(input.action)}` };
}

export function registerUnityBridgeLease(server: McpServer): void {
  server.registerTool(
    "unity_bridge_lease",
    {
      description:
        "Multi-agent Play Mode ownership lease (migration 0010). Ensures only one agent drives Unity Play Mode at a time. TTL 8 min provides crash-safety. Actions: acquire (returns lease_id; idempotent for same agent_id), release (by lease_id after exit_play_mode), status (check holder without acquiring). Workflow: unity_bridge_lease(acquire) → unity_bridge_command(enter_play_mode) → unity_bridge_command(debug_context_bundle) → unity_bridge_command(exit_play_mode) → unity_bridge_lease(release). On lease_unavailable error: wait 60 s, retry up to 10 min total; if still busy skip Play Mode and emit play_mode_lease: skipped_busy in Verification block. Non-Play-Mode bridge commands (export_agent_context, get_compilation_status, get_console_logs) do not require a lease.",
      inputSchema: unityBridgeLeaseInputSchema,
    },
    async (args) =>
      runWithToolTiming("unity_bridge_lease", async () => {
        const envelope = await wrapTool(async (input: UnityBridgeLeaseInput) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const result = await runUnityBridgeLease(input, pool);
          // runUnityBridgeLease returns ok/error shapes; pass through as-is (wrapTool detects ok field).
          return result;
        })(unityBridgeLeaseInputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
