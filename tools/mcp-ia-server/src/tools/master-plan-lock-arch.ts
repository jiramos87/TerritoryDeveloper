/**
 * MCP tool: master_plan_lock_arch — Phase A architecture-lock seal.
 *
 * Sets `architecture_locked_at = now()` and `locked_commit_sha = $sha` on
 * `ia_master_plans`. Appends `ia_master_plan_change_log` row
 * (kind='arch_locked'). After this call the plan-scoped `arch_decisions`
 * UPDATE-lock trigger from migration 0049 is armed (D17).
 *
 * Idempotent on identical commit_sha; conflicting re-lock with a different
 * sha returns `error='already_locked'` (no overwrite without explicit
 * superseded-flow).
 *
 * parallel-carcass §6.2. Schema-cache restart required after add (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

const inputShape = {
  slug: z.string().describe("Master-plan slug."),
  commit_sha: z.string().describe("Commit SHA representing the locked state."),
  actor: z.string().optional().describe("Optional actor for change_log row."),
};

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

type Args = { slug: string; commit_sha: string; actor?: string };

export interface MasterPlanLockArchResult {
  slug: string;
  applied: boolean;
  architecture_locked_at: string | null;
  locked_commit_sha: string | null;
  change_log_entry_id: number | null;
  error?: string;
}

function toIso(v: Date | string | null): string | null {
  if (v === null) return null;
  return typeof v === "string" ? new Date(v).toISOString() : v.toISOString();
}

export async function applyMasterPlanLockArch(
  args: Args,
): Promise<MasterPlanLockArchResult> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }
  const { slug, commit_sha, actor } = args;

  const planRes = await pool.query<{
    architecture_locked_at: Date | string | null;
    locked_commit_sha: string | null;
  }>(
    `SELECT architecture_locked_at, locked_commit_sha
       FROM ia_master_plans WHERE slug = $1`,
    [slug],
  );
  if (planRes.rows.length === 0) {
    return {
      slug,
      applied: false,
      architecture_locked_at: null,
      locked_commit_sha: null,
      change_log_entry_id: null,
      error: "plan_not_found",
    };
  }
  const cur = planRes.rows[0]!;
  if (cur.architecture_locked_at !== null) {
    if (cur.locked_commit_sha === commit_sha) {
      return {
        slug,
        applied: false,
        architecture_locked_at: toIso(cur.architecture_locked_at),
        locked_commit_sha: cur.locked_commit_sha,
        change_log_entry_id: null,
        error: undefined,
      };
    }
    return {
      slug,
      applied: false,
      architecture_locked_at: toIso(cur.architecture_locked_at),
      locked_commit_sha: cur.locked_commit_sha,
      change_log_entry_id: null,
      error: "already_locked",
    };
  }

  const client = await pool.connect();
  try {
    await client.query("BEGIN");
    const upd = await client.query<{
      architecture_locked_at: Date | string;
      locked_commit_sha: string;
    }>(
      `UPDATE ia_master_plans
          SET architecture_locked_at = now(),
              locked_commit_sha = $2
        WHERE slug = $1
          AND architecture_locked_at IS NULL
        RETURNING architecture_locked_at, locked_commit_sha`,
      [slug, commit_sha],
    );
    if (upd.rows.length === 0) {
      await client.query("ROLLBACK");
      return {
        slug,
        applied: false,
        architecture_locked_at: null,
        locked_commit_sha: null,
        change_log_entry_id: null,
        error: "race_already_locked",
      };
    }
    const body = JSON.stringify({ commit_sha });
    const ins = await client.query<{ entry_id: number }>(
      `INSERT INTO ia_master_plan_change_log
         (slug, kind, body, actor, commit_sha)
       VALUES ($1, 'arch_locked', $2, $3, $4)
       RETURNING entry_id`,
      [slug, body, actor ?? null, commit_sha],
    );
    await client.query("COMMIT");
    return {
      slug,
      applied: true,
      architecture_locked_at: toIso(upd.rows[0]!.architecture_locked_at),
      locked_commit_sha: upd.rows[0]!.locked_commit_sha,
      change_log_entry_id: ins.rows[0]!.entry_id,
    };
  } catch (err) {
    await client.query("ROLLBACK").catch(() => {});
    throw err;
  } finally {
    client.release();
  }
}

export function registerMasterPlanLockArch(server: McpServer): void {
  server.registerTool(
    "master_plan_lock_arch",
    {
      description:
        "DB-backed mutate: set `architecture_locked_at = now()` + " +
        "`locked_commit_sha = $sha` on `ia_master_plans` and append " +
        "`ia_master_plan_change_log` row (kind='arch_locked'). After this " +
        "the plan-scoped `arch_decisions` UPDATE-lock trigger (mig 0049) " +
        "is armed (D17). Idempotent when re-called with same commit_sha; " +
        "different sha returns `error='already_locked'`. " +
        "parallel-carcass §6.2. " +
        "Schema-cache restart required after add (N4).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("master_plan_lock_arch", async () => {
        const envelope = await wrapTool(
          async (input: Args): Promise<MasterPlanLockArchResult> =>
            applyMasterPlanLockArch(input),
        )(args as Args);
        return jsonResult(envelope);
      }),
  );
}
