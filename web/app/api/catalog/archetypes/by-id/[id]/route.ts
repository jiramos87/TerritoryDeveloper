/**
 * Archetype by-id GET (TECH-2459 / Stage 11.1).
 *
 *  GET /api/catalog/archetypes/by-id/[id]
 *    -> { archetype: CatalogArchetype, versions: CatalogArchetypeVersionWithPinCount[] }
 *
 * Detail-page fetcher: returns archetype DTO + full version history with pinned
 * counts in a single round-trip.
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2459 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { getArchetypeById, listVersionsForArchetype } from "@/lib/catalog/archetype-repo";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
} as const;

type Ctx = { params: Promise<{ id: string }> };

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { id } = await ctx.params;
  if (!/^\d+$/.test(id)) {
    return catalogJsonError(400, "bad_request", "id must be a non-negative integer string");
  }
  try {
    const arch = await getArchetypeById(id);
    if (!arch) return catalogJsonError(404, "not_found", "Archetype not found");
    const versions = await listVersionsForArchetype(arch.entity_id);
    return NextResponse.json(
      { ok: true, data: { archetype: arch, versions } },
      { status: 200 },
    );
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "archetype-by-id",
      });
    }
    return responseFromPostgresError(e, "Archetype by-id query failed");
  }
}
