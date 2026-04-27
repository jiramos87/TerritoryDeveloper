/**
 * Archetype version pin-count GET (TECH-2461 / Stage 11.1).
 *
 *  GET /api/catalog/archetypes/[slug]/versions/[versionId]/pin-count
 *    -> { count: number }
 *
 * Used by the bump flow's preview panel to surface blast radius before publish.
 * Counts entities pinned to the parent (old) `entity_version`.
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2461 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { countPinnedEntities, getArchetypeBySlug } from "@/lib/catalog/archetype-repo";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
} as const;

type Ctx = { params: Promise<{ slug: string; versionId: string }> };

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { slug, versionId } = await ctx.params;
  if (!/^\d+$/.test(versionId)) {
    return catalogJsonError(400, "bad_request", "versionId must be a non-negative integer string");
  }
  try {
    const arch = await getArchetypeBySlug(slug);
    if (arch == null) return catalogJsonError(404, "not_found", "Archetype not found");
    const count = await countPinnedEntities(versionId);
    return NextResponse.json({ ok: true, data: { count } }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "archetype-pin-count",
      });
    }
    return responseFromPostgresError(e, "Pin-count query failed");
  }
}
