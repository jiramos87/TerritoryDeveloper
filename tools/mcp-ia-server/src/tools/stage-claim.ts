/**
 * MCP tools: stage_claim + stage_claim_release.
 *
 * Stage-tier of the two-tier claim mutex (V2 row-only). `stage_claim` asserts
 * that an active section claim row exists for the parent section (any holder)
 * before inserting `ia_stage_claims`. Stages without `section_id` (legacy
 * linear plans, carcass stages) bypass the section-claim check.
 *
 * Section IS the holder — any caller may claim/renew/release. Concurrent
 * contention enforced by INSERT-or-fail on PRIMARY KEY. Stale rows cleared
 * by time-based claims_sweep.
 *
 * parallel-carcass §6.2 / D4. Schema-cache restart required after add (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

const claimShape = {
  slug: z.string().describe("Master-plan slug."),
  stage_id: z.string().describe("Stage identifier."),
};

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

interface StageClaimRow {
  slug: string;
  stage_id: string;
  claimed_at: Date | string;
  last_heartbeat: Date | string;
}

function toIso(v: Date | string): string {
  return typeof v === "string" ? new Date(v).toISOString() : v.toISOString();
}

type Args = { slug: string; stage_id: string };

export interface StageClaimResult {
  slug: string;
  stage_id: string;
  claimed_at: string;
  last_heartbeat: string;
  status: "claimed" | "renewed";
}

export interface StageReleaseResult {
  slug: string;
  stage_id: string;
  released: boolean;
}

export async function applyStageClaim(args: Args): Promise<StageClaimResult> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }
  const { slug, stage_id } = args;

  const stageRes = await pool.query<{ section_id: string | null }>(
    `SELECT section_id FROM ia_stages WHERE slug = $1 AND stage_id = $2`,
    [slug, stage_id],
  );
  if (stageRes.rows.length === 0) {
    throw {
      code: "stage_not_found",
      message: `stage ${stage_id} of plan ${slug} not found`,
    };
  }
  const section_id = stageRes.rows[0]!.section_id;

  if (section_id) {
    const sec = await pool.query<{ slug: string }>(
      `SELECT slug FROM ia_section_claims
        WHERE slug = $1 AND section_id = $2 AND released_at IS NULL`,
      [slug, section_id],
    );
    if (sec.rows.length === 0) {
      throw {
        code: "section_claim_required",
        message: `section ${section_id} of plan ${slug} not claimed`,
      };
    }
  }

  const existing = await pool.query<StageClaimRow>(
    `SELECT slug, stage_id, claimed_at, last_heartbeat
       FROM ia_stage_claims
      WHERE slug = $1 AND stage_id = $2 AND released_at IS NULL`,
    [slug, stage_id],
  );

  if (existing.rows.length > 0) {
    // V2 row-only: any caller may refresh the open claim.
    const upd = await pool.query<StageClaimRow>(
      `UPDATE ia_stage_claims
          SET last_heartbeat = now()
        WHERE slug = $1 AND stage_id = $2 AND released_at IS NULL
        RETURNING slug, stage_id, claimed_at, last_heartbeat`,
      [slug, stage_id],
    );
    const u = upd.rows[0]!;
    return {
      slug,
      stage_id,
      claimed_at: toIso(u.claimed_at),
      last_heartbeat: toIso(u.last_heartbeat),
      status: "renewed",
    };
  }

  const ins = await pool.query<StageClaimRow>(
    `INSERT INTO ia_stage_claims (slug, stage_id)
     VALUES ($1, $2)
     ON CONFLICT (slug, stage_id) DO UPDATE
       SET claimed_at = now(),
           last_heartbeat = now(),
           released_at = NULL
       WHERE ia_stage_claims.released_at IS NOT NULL
     RETURNING slug, stage_id, claimed_at, last_heartbeat`,
    [slug, stage_id],
  );
  if (ins.rows.length === 0) {
    throw {
      code: "stage_claim_held",
      message: `stage ${stage_id} concurrently claimed`,
    };
  }
  const r = ins.rows[0]!;
  return {
    slug,
    stage_id,
    claimed_at: toIso(r.claimed_at),
    last_heartbeat: toIso(r.last_heartbeat),
    status: "claimed",
  };
}

export async function applyStageRelease(
  args: Args,
): Promise<StageReleaseResult> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }
  const { slug, stage_id } = args;
  const upd = await pool.query(
    `UPDATE ia_stage_claims
        SET released_at = now()
      WHERE slug = $1 AND stage_id = $2
        AND released_at IS NULL`,
    [slug, stage_id],
  );
  return { slug, stage_id, released: (upd.rowCount ?? 0) > 0 };
}

export function registerStageClaimTools(server: McpServer): void {
  server.registerTool(
    "stage_claim",
    {
      description:
        "DB-backed mutate: assert section claim row open for parent " +
        "section (when stage has section_id), then insert active row " +
        "in `ia_stage_claims`. V2 row-only — any caller may renew the open " +
        "claim. Stages without section_id bypass section-claim check (legacy/" +
        "carcass). parallel-carcass §6.2 (D4). " +
        "Schema-cache restart required after add (N4).",
      inputSchema: claimShape,
    },
    async (args) =>
      runWithToolTiming("stage_claim", async () => {
        const envelope = await wrapTool(
          async (input: Args): Promise<StageClaimResult> =>
            applyStageClaim(input),
        )(args as Args);
        return jsonResult(envelope);
      }),
  );

  server.registerTool(
    "stage_claim_release",
    {
      description:
        "DB-backed mutate: set `released_at = now()` on active stage " +
        "claim for (slug, stage_id). V2 row-only — any caller. " +
        "Returns `{released}`. " +
        "Schema-cache restart required after add (N4).",
      inputSchema: claimShape,
    },
    async (args) =>
      runWithToolTiming("stage_claim_release", async () => {
        const envelope = await wrapTool(
          async (input: Args): Promise<StageReleaseResult> =>
            applyStageRelease(input),
        )(args as Args);
        return jsonResult(envelope);
      }),
  );
}
