/**
 * Read-only sections-cluster query — companion to MCP `master_plan_sections`.
 *
 * Returns sections grouped by `section_id` for one master plan: member stages,
 * owned arch_surfaces (via stage_arch_surfaces), active claim row, and
 * surface-cluster validation warnings (cross-section ownership). Bundle also
 * carries `claim_heartbeat_timeout_minutes` from `carcass_config` so the UI
 * surface (StaleClaimBadge) computes stale state from a single payload.
 *
 * Mirrors the MCP server-side helper at
 * `tools/mcp-ia-server/src/tools/master-plan-sections.ts` but reads `ia_*`
 * tables directly via the shared web `sql` client (skips subprocess
 * transport — same pattern as `web/lib/ia/queries.ts`).
 *
 * Stage 2.2 / TECH-5243 of `parallel-carcass-rollout`.
 */

import { sql } from "@/lib/db/client";

export interface SectionStageView {
  stage_id: string;
  status: string;
  carcass_role: string | null;
}

export interface SectionView {
  section_id: string;
  stages: SectionStageView[];
  owned_surfaces: string[];
  claim: {
    claimed_at: string;
    last_heartbeat: string;
  } | null;
  warnings: string[];
}

export interface CarcassStageView {
  stage_id: string;
  status: string;
}

export interface PlanSectionsBundle {
  slug: string;
  carcass_stages: CarcassStageView[];
  sections: SectionView[];
  warnings: string[];
  /** Threshold (minutes) above which a held claim is considered stale. */
  claim_heartbeat_timeout_minutes: number;
  /** True iff every carcass-role stage is `done`. */
  carcass_done: boolean;
}

const DEFAULT_TIMEOUT_MIN = 10;

export async function getPlanSectionsBundle(
  slug: string,
): Promise<PlanSectionsBundle | null> {
  // Plan must exist — otherwise BFF returns 404.
  const planRows = await sql<{ slug: string }[]>`
    SELECT slug FROM ia_master_plans WHERE slug = ${slug}
  `;
  if (planRows.length === 0) return null;

  const stageRows = await sql<
    {
      stage_id: string;
      status: string;
      carcass_role: string | null;
      section_id: string | null;
    }[]
  >`
    SELECT stage_id, status::text AS status, carcass_role, section_id
    FROM ia_stages WHERE slug = ${slug}
    ORDER BY stage_id
  `;

  const surfaceRows = await sql<
    { stage_id: string; surface_slug: string }[]
  >`
    SELECT stage_id, surface_slug
    FROM stage_arch_surfaces WHERE slug = ${slug}
  `;

  const claimRows = await sql<
    {
      section_id: string;
      claimed_at: Date;
      last_heartbeat: Date;
    }[]
  >`
    SELECT section_id, claimed_at, last_heartbeat
    FROM ia_section_claims
    WHERE slug = ${slug} AND released_at IS NULL
  `;

  const cfgRows = await sql<{ value: string }[]>`
    SELECT value FROM carcass_config
    WHERE key = 'claim_heartbeat_timeout_minutes'
  `;
  const claim_heartbeat_timeout_minutes =
    cfgRows.length > 0
      ? parseInt(cfgRows[0].value, 10) || DEFAULT_TIMEOUT_MIN
      : DEFAULT_TIMEOUT_MIN;

  const surfaceByStage = new Map<string, string[]>();
  for (const r of surfaceRows) {
    if (!surfaceByStage.has(r.stage_id)) surfaceByStage.set(r.stage_id, []);
    surfaceByStage.get(r.stage_id)!.push(r.surface_slug);
  }

  const claimBySection = new Map<
    string,
    { claimed_at: Date; last_heartbeat: Date }
  >();
  for (const r of claimRows) {
    claimBySection.set(r.section_id, {
      claimed_at: r.claimed_at,
      last_heartbeat: r.last_heartbeat,
    });
  }

  const carcass: CarcassStageView[] = [];
  const sectionsMap = new Map<
    string,
    {
      stage_id: string;
      status: string;
      carcass_role: string | null;
      section_id: string;
    }[]
  >();
  for (const s of stageRows) {
    if (s.carcass_role === "carcass") {
      carcass.push({ stage_id: s.stage_id, status: s.status });
    }
    if (s.section_id) {
      const k = s.section_id;
      if (!sectionsMap.has(k)) sectionsMap.set(k, []);
      sectionsMap.get(k)!.push({
        stage_id: s.stage_id,
        status: s.status,
        carcass_role: s.carcass_role,
        section_id: s.section_id,
      });
    }
  }

  // Cross-section ownership warning: surface owned by stages in >1 section.
  const surfaceToSections = new Map<string, Set<string>>();
  for (const s of stageRows) {
    if (!s.section_id) continue;
    for (const surf of surfaceByStage.get(s.stage_id) ?? []) {
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
    const warnings = new Set<string>();
    for (const m of members) {
      for (const surf of surfaceByStage.get(m.stage_id) ?? []) {
        owned.add(surf);
        if (cross.has(surf)) {
          warnings.add(
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
            claimed_at: c.claimed_at.toISOString(),
            last_heartbeat: c.last_heartbeat.toISOString(),
          }
        : null,
      warnings: Array.from(warnings),
    });
  }
  sections.sort((a, b) => a.section_id.localeCompare(b.section_id));

  const planWarnings: string[] = [];
  if (sections.length > 0 && carcass.length === 0) {
    planWarnings.push("plan has section stages but zero carcass stages");
  }

  const carcass_done =
    carcass.length > 0 && carcass.every((c) => c.status === "done");

  return {
    slug,
    carcass_stages: carcass,
    sections,
    warnings: planWarnings,
    claim_heartbeat_timeout_minutes,
    carcass_done,
  };
}
