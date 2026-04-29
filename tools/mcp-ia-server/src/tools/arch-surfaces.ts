/**
 * arch_surfaces_backfill — TECH-2978 (db-lifecycle-extensions Stage 1).
 *
 * MCP-side port of `tools/scripts/backfill-arch-surfaces.mjs`. Walks every
 * `ia_master_plans` row + each Stage, infers `arch_surfaces[]` candidates from
 * the Stage body via substring scan of `arch_surfaces.spec_path` /
 * `arch_surfaces.slug` / `arch_surfaces.spec_section`, then writes
 * confident-single-match candidates into `stage_arch_surfaces` (PK on
 * `(slug, stage_id, surface_slug)`).
 *
 * Idempotent: PK collision = silent skip on re-run; second run yields zero
 * inserted rows. NEVER inserts into `arch_surfaces` (Invariant #12 — link
 * existing rows only).
 *
 * Inputs:
 *   - `dry_run` (default false) — preview only; no DB writes (still walks +
 *     surfaces summary + polling list).
 *   - `plan_slug` (optional) — scope to one master plan; default = all open
 *     plans.
 *
 * Returns `{stages_walked, confident_links, ambiguous_count,
 * none_eligible_count, polling[]}`.
 *
 * Run handler `runArchSurfacesBackfill` exported separately from
 * registration so unit tests + the thin CLI wrapper can drive it without
 * the MCP transport.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { Pool } from "pg";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const archSurfacesBackfillInputSchema = z.object({
  dry_run: z
    .boolean()
    .optional()
    .describe("Preview-only when true; skip INSERT. Default false."),
  plan_slug: z
    .string()
    .optional()
    .describe("Scope to one master plan slug. Default: all open plans."),
});

export interface ArchSurfacesBackfillPollingRow {
  slug: string;
  stage_id: string;
  kind: "ambiguous" | "none-eligible";
  candidates: string[];
}

export interface ArchSurfacesBackfillResult {
  dry_run: boolean;
  plan_scope: string | null;
  stages_walked: number;
  confident_links: number;
  ambiguous_count: number;
  none_eligible_count: number;
  polling: ArchSurfacesBackfillPollingRow[];
}

interface ArchSurfaceRow {
  slug: string;
  kind: string;
  spec_path: string | null;
  spec_section: string | null;
}

export async function runArchSurfacesBackfill(
  pool: Pool,
  input: { dry_run?: boolean; plan_slug?: string },
): Promise<ArchSurfacesBackfillResult> {
  const dryRun = input.dry_run === true;
  const planScope = (input.plan_slug ?? "").trim() || null;

  const surfacesRes = await pool.query<ArchSurfaceRow>(
    `SELECT slug, kind, spec_path, spec_section FROM arch_surfaces ORDER BY slug`,
  );
  const surfaces = surfacesRes.rows;
  if (surfaces.length === 0) {
    throw {
      code: "invalid_input" as const,
      message: "no rows in arch_surfaces — Stage 1.1 seed missing",
    };
  }

  const plansRes = await pool.query<{ slug: string }>(
    planScope
      ? `SELECT slug FROM ia_master_plans WHERE slug = $1 ORDER BY slug`
      : `SELECT slug FROM ia_master_plans ORDER BY slug`,
    planScope ? [planScope] : [],
  );
  if (plansRes.rowCount === 0) {
    throw {
      code: "invalid_input" as const,
      message: planScope
        ? `no master plan found for slug=${planScope}`
        : "no master plans in ia_master_plans",
    };
  }

  let stagesWalked = 0;
  let confidentLinks = 0;
  let ambiguousCount = 0;
  let noneEligibleCount = 0;
  const polling: ArchSurfacesBackfillPollingRow[] = [];

  for (const { slug } of plansRes.rows) {
    const stagesRes = await pool.query<{
      stage_id: string;
      body: string | null;
      objective: string | null;
      exit_criteria: string | null;
    }>(
      `SELECT stage_id, body, objective, exit_criteria
         FROM ia_stages
        WHERE slug = $1
        ORDER BY stage_id`,
      [slug],
    );

    for (const stage of stagesRes.rows) {
      stagesWalked += 1;
      const haystack = [
        stage.body ?? "",
        stage.objective ?? "",
        stage.exit_criteria ?? "",
      ]
        .join("\n")
        .toLowerCase();

      const candidates = new Set<string>();
      for (const s of surfaces) {
        const specPath = (s.spec_path ?? "").toLowerCase();
        const specSection = (s.spec_section ?? "").toLowerCase();
        const surfaceSlug = s.slug.toLowerCase();
        let hit = false;
        if (specPath && haystack.includes(specPath)) hit = true;
        if (!hit && surfaceSlug && haystack.includes(surfaceSlug)) hit = true;
        if (
          !hit &&
          specSection &&
          specSection.length >= 6 &&
          haystack.includes(specSection)
        ) {
          hit = true;
        }
        if (hit) candidates.add(s.slug);
      }

      const existingRes = await pool.query<{ surface_slug: string }>(
        `SELECT surface_slug FROM stage_arch_surfaces WHERE slug = $1 AND stage_id = $2`,
        [slug, stage.stage_id],
      );
      const alreadyLinked = new Set(
        existingRes.rows.map((r) => r.surface_slug),
      );
      const newCandidates = [...candidates].filter(
        (c) => !alreadyLinked.has(c),
      );

      if (alreadyLinked.size > 0 && newCandidates.length === 0) {
        continue;
      }

      if (newCandidates.length === 1 && alreadyLinked.size === 0) {
        const surfaceSlug = newCandidates[0]!;
        if (!dryRun) {
          await pool.query(
            `INSERT INTO stage_arch_surfaces (slug, stage_id, surface_slug)
               VALUES ($1, $2, $3)
               ON CONFLICT (slug, stage_id, surface_slug) DO NOTHING`,
            [slug, stage.stage_id, surfaceSlug],
          );
        }
        confidentLinks += 1;
        continue;
      }

      if (newCandidates.length >= 2) {
        ambiguousCount += 1;
        polling.push({
          slug,
          stage_id: stage.stage_id,
          kind: "ambiguous",
          candidates: newCandidates,
        });
        continue;
      }

      if (newCandidates.length === 0 && alreadyLinked.size === 0) {
        noneEligibleCount += 1;
        polling.push({
          slug,
          stage_id: stage.stage_id,
          kind: "none-eligible",
          candidates: [],
        });
      }
    }
  }

  // Invariant guard — confirm arch_surfaces row count unchanged.
  const postCountRes = await pool.query<{ n: number }>(
    `SELECT count(*)::int AS n FROM arch_surfaces`,
  );
  const postCount = postCountRes.rows[0]!.n;
  if (postCount !== surfaces.length) {
    throw {
      code: "invariant_violation" as const,
      message: `arch_surfaces row count changed (${surfaces.length} → ${postCount}); tool must NEVER mutate arch_surfaces`,
    };
  }

  return {
    dry_run: dryRun,
    plan_scope: planScope,
    stages_walked: stagesWalked,
    confident_links: confidentLinks,
    ambiguous_count: ambiguousCount,
    none_eligible_count: noneEligibleCount,
    polling,
  };
}

export function registerArchSurfacesBackfill(server: McpServer): void {
  server.registerTool(
    "arch_surfaces_backfill",
    {
      title: "arch_surfaces_backfill",
      description:
        "DB-backed: walk plans + stages, infer arch_surfaces candidates from stage body, write confident single-matches into stage_arch_surfaces. Idempotent (PK skip). Stage 1 / TECH-2978.",
      inputSchema: archSurfacesBackfillInputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("arch_surfaces_backfill", async () => {
        const envelope = await wrapTool(
          async (
            input: z.infer<typeof archSurfacesBackfillInputSchema>,
          ): Promise<ArchSurfacesBackfillResult> => {
            const pool = getIaDatabasePool();
            if (!pool) throw dbUnconfiguredError();
            return await runArchSurfacesBackfill(pool, input);
          },
        )(archSurfacesBackfillInputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
