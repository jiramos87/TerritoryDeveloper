/**
 * MCP tool: master_plan_sections — read-only cluster + claim view per slug.
 *
 * Returns sections grouped by `section_id` for a master plan: member stages,
 * owned arch_surfaces (via stage_arch_surfaces), claim status (active row in
 * ia_section_claims), and surface-cluster validation flags.
 *
 * Parallel-carcass exploration §6.2 (Wave 0 Phase 2). Companion to migrations
 * 0049 + 0050.
 *
 * NOTE: After editing this descriptor, restart Claude Code to refresh schema
 * cache (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

const inputShape = {
  slug: z.string().describe("Master-plan slug."),
};

export interface SectionView {
  section_id: string;
  stages: { stage_id: string; status: string; carcass_role: string | null }[];
  owned_surfaces: string[];
  claim: {
    session_id: string;
    claimed_at: string;
    last_heartbeat: string;
  } | null;
  warnings: string[];
}

export interface MasterPlanSectionsPayload {
  slug: string;
  carcass_stages: { stage_id: string; status: string }[];
  sections: SectionView[];
  warnings: string[];
}

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

interface StageRow {
  stage_id: string;
  status: string;
  carcass_role: string | null;
  section_id: string | null;
}
interface SurfaceLinkRow {
  stage_id: string;
  surface_slug: string;
}
interface ClaimRow {
  section_id: string;
  session_id: string;
  claimed_at: Date | string;
  last_heartbeat: Date | string;
}

function toIso(v: Date | string): string {
  return typeof v === "string" ? new Date(v).toISOString() : v.toISOString();
}

export async function getMasterPlanSections(
  slug: string,
): Promise<MasterPlanSectionsPayload> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }

  const stagesRes = await pool.query<StageRow>(
    `SELECT stage_id, status, carcass_role, section_id
       FROM ia_stages
      WHERE slug = $1
      ORDER BY stage_id`,
    [slug],
  );

  const surfacesRes = await pool.query<SurfaceLinkRow>(
    `SELECT stage_id, surface_slug
       FROM stage_arch_surfaces
      WHERE slug = $1`,
    [slug],
  );

  const claimsRes = await pool.query<ClaimRow>(
    `SELECT section_id, session_id, claimed_at, last_heartbeat
       FROM ia_section_claims
      WHERE slug = $1 AND released_at IS NULL`,
    [slug],
  );

  const surfaceByStage = new Map<string, string[]>();
  for (const r of surfacesRes.rows) {
    if (!surfaceByStage.has(r.stage_id)) surfaceByStage.set(r.stage_id, []);
    surfaceByStage.get(r.stage_id)!.push(r.surface_slug);
  }

  const claimBySection = new Map<string, ClaimRow>();
  for (const r of claimsRes.rows) claimBySection.set(r.section_id, r);

  const carcass: { stage_id: string; status: string }[] = [];
  const sectionsMap = new Map<string, StageRow[]>();
  for (const s of stagesRes.rows) {
    if (s.carcass_role === "carcass") {
      carcass.push({ stage_id: s.stage_id, status: s.status });
    }
    if (s.section_id) {
      if (!sectionsMap.has(s.section_id)) sectionsMap.set(s.section_id, []);
      sectionsMap.get(s.section_id)!.push(s);
    }
  }

  // Surface cluster validation: a surface owned by stages from >1 section is
  // a cross-section ownership warning (D8).
  const surfaceToSections = new Map<string, Set<string>>();
  for (const s of stagesRes.rows) {
    if (!s.section_id) continue;
    const surfs = surfaceByStage.get(s.stage_id) ?? [];
    for (const surf of surfs) {
      if (!surfaceToSections.has(surf))
        surfaceToSections.set(surf, new Set());
      surfaceToSections.get(surf)!.add(s.section_id);
    }
  }
  const cross = new Set<string>();
  for (const [surf, secs] of surfaceToSections.entries()) {
    if (secs.size > 1) cross.add(surf);
  }

  const sections: SectionView[] = [];
  for (const [section_id, members] of sectionsMap.entries()) {
    const owned = new Set<string>();
    const warnings: string[] = [];
    for (const m of members) {
      for (const surf of surfaceByStage.get(m.stage_id) ?? []) {
        owned.add(surf);
        if (cross.has(surf)) {
          warnings.push(
            `surface ${surf} owned across multiple sections (cross-section)`,
          );
        }
      }
    }
    const c = claimBySection.get(section_id) ?? null;
    sections.push({
      section_id,
      stages: members.map((m) => ({
        stage_id: m.stage_id,
        status: m.status,
        carcass_role: m.carcass_role,
      })),
      owned_surfaces: Array.from(owned).sort(),
      claim: c
        ? {
            session_id: c.session_id,
            claimed_at: toIso(c.claimed_at),
            last_heartbeat: toIso(c.last_heartbeat),
          }
        : null,
      warnings: Array.from(new Set(warnings)),
    });
  }
  sections.sort((a, b) => a.section_id.localeCompare(b.section_id));

  const planWarnings: string[] = [];
  if (sections.length > 0 && carcass.length === 0) {
    planWarnings.push("plan has section stages but zero carcass stages");
  }

  return { slug, carcass_stages: carcass, sections, warnings: planWarnings };
}

type Args = { slug: string };

export function registerMasterPlanSections(server: McpServer): void {
  server.registerTool(
    "master_plan_sections",
    {
      description:
        "DB-backed read: groups `ia_stages` by `section_id` for one master " +
        "plan; returns member stages, owned arch_surfaces (via " +
        "`stage_arch_surfaces`), active claim row (`ia_section_claims`), " +
        "and surface-cluster validation warnings (cross-section ownership). " +
        "Companion to parallel-carcass primitives (mig 0049). " +
        "Schema-cache restart required after add (N4).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("master_plan_sections", async () => {
        const envelope = await wrapTool(
          async (input: Args): Promise<MasterPlanSectionsPayload> => {
            return getMasterPlanSections(input.slug);
          },
        )(args as Args);
        return jsonResult(envelope);
      }),
  );
}
