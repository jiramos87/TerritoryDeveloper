/**
 * MCP tools: section_claim + section_claim_release.
 *
 * Two-tier claim mutex (parallel-carcass §6.2 / D4, V2 row-only). One open row
 * per (slug, section_id) in `ia_section_claims`. The section IS the holder —
 * any agent can renew/release the open row. Concurrent contention enforced by
 * INSERT-or-fail on the PRIMARY KEY. Stale rows cleared by time-based
 * claims_sweep. Multi-sequential agents on the same task: trivially supported
 * (both address the same row by (slug, section_id)).
 *
 * Section release cascades stale stage claims for the same section (same slug,
 * same section_id).
 *
 * Schema-cache restart required after add (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

const claimShape = {
  slug: z.string().describe("Master-plan slug."),
  section_id: z.string().describe("Section identifier."),
};

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

interface ClaimRow {
  slug: string;
  section_id: string;
  claimed_at: Date | string;
  last_heartbeat: Date | string;
}

function toIso(v: Date | string): string {
  return typeof v === "string" ? new Date(v).toISOString() : v.toISOString();
}

type ClaimArgs = { slug: string; section_id: string };

export interface SectionClaimResult {
  slug: string;
  section_id: string;
  claimed_at: string;
  last_heartbeat: string;
  status: "claimed" | "renewed";
}

export interface SectionReleaseResult {
  slug: string;
  section_id: string;
  released: boolean;
  cascaded_stage_releases: number;
}

export async function applySectionClaim(
  args: ClaimArgs,
): Promise<SectionClaimResult> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }
  const { slug, section_id } = args;

  const existing = await pool.query<ClaimRow>(
    `SELECT slug, section_id, claimed_at, last_heartbeat
       FROM ia_section_claims
      WHERE slug = $1 AND section_id = $2 AND released_at IS NULL`,
    [slug, section_id],
  );

  if (existing.rows.length > 0) {
    // V2 row-only: any caller may refresh the open claim.
    const upd = await pool.query<ClaimRow>(
      `UPDATE ia_section_claims
          SET last_heartbeat = now()
        WHERE slug = $1 AND section_id = $2 AND released_at IS NULL
        RETURNING slug, section_id, claimed_at, last_heartbeat`,
      [slug, section_id],
    );
    const u = upd.rows[0]!;
    return {
      slug,
      section_id,
      claimed_at: toIso(u.claimed_at),
      last_heartbeat: toIso(u.last_heartbeat),
      status: "renewed",
    };
  }

  const ins = await pool.query<ClaimRow>(
    `INSERT INTO ia_section_claims (slug, section_id)
     VALUES ($1, $2)
     ON CONFLICT (slug, section_id) DO UPDATE
       SET claimed_at = now(),
           last_heartbeat = now(),
           released_at = NULL
       WHERE ia_section_claims.released_at IS NOT NULL
     RETURNING slug, section_id, claimed_at, last_heartbeat`,
    [slug, section_id],
  );
  if (ins.rows.length === 0) {
    throw {
      code: "section_claim_held",
      message: `section ${section_id} of plan ${slug} concurrently claimed`,
    };
  }
  const r = ins.rows[0]!;
  return {
    slug,
    section_id,
    claimed_at: toIso(r.claimed_at),
    last_heartbeat: toIso(r.last_heartbeat),
    status: "claimed",
  };
}

export async function applySectionRelease(
  args: ClaimArgs,
): Promise<SectionReleaseResult> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }
  const { slug, section_id } = args;

  const upd = await pool.query(
    `UPDATE ia_section_claims
        SET released_at = now()
      WHERE slug = $1 AND section_id = $2
        AND released_at IS NULL`,
    [slug, section_id],
  );

  if ((upd.rowCount ?? 0) === 0) {
    return {
      slug,
      section_id,
      released: false,
      cascaded_stage_releases: 0,
    };
  }

  // Cascade: release stage claims for stages in this section.
  const cascade = await pool.query(
    `UPDATE ia_stage_claims sc
        SET released_at = now()
       FROM ia_stages s
      WHERE sc.slug = s.slug
        AND sc.stage_id = s.stage_id
        AND sc.slug = $1
        AND s.section_id = $2
        AND sc.released_at IS NULL`,
    [slug, section_id],
  );

  return {
    slug,
    section_id,
    released: true,
    cascaded_stage_releases: cascade.rowCount ?? 0,
  };
}

export function registerSectionClaimTools(server: McpServer): void {
  server.registerTool(
    "section_claim",
    {
      description:
        "DB-backed mutate: insert active row in `ia_section_claims` for " +
        "(slug, section_id). Fails (`section_claim_held`) only on concurrent " +
        "INSERT race; otherwise refreshes heartbeat (V2 row-only — any agent " +
        "may renew the open claim). Two-tier claim mutex per parallel-carcass " +
        "§6.2 (D4). Schema-cache restart required after add (N4).",
      inputSchema: claimShape,
    },
    async (args) =>
      runWithToolTiming("section_claim", async () => {
        const envelope = await wrapTool(
          async (input: ClaimArgs): Promise<SectionClaimResult> =>
            applySectionClaim(input),
        )(args as ClaimArgs);
        return jsonResult(envelope);
      }),
  );

  server.registerTool(
    "section_claim_release",
    {
      description:
        "DB-backed mutate: set `released_at = now()` on the active section " +
        "claim for (slug, section_id). V2 row-only — callable by any agent. " +
        "Cascade-releases active stage claims for stages in the same " +
        "section. Returns `{released, cascaded_stage_releases}`. " +
        "Schema-cache restart required after add (N4).",
      inputSchema: claimShape,
    },
    async (args) =>
      runWithToolTiming("section_claim_release", async () => {
        const envelope = await wrapTool(
          async (input: ClaimArgs): Promise<SectionReleaseResult> =>
            applySectionRelease(input),
        )(args as ClaimArgs);
        return jsonResult(envelope);
      }),
  );
}
