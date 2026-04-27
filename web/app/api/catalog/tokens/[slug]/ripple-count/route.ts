/**
 * Token ripple-count GET (TECH-2093 / Stage 10.1).
 *
 *   GET /api/catalog/tokens/[slug]/ripple-count -> { count: number }
 *
 * Per DEC-A44: surfaces "Editing this changes N entities" upstream of the
 * token detail save button. Source-of-truth would be `catalog_ref_edge`
 * (Stage 14.1 materializer) — the table does not exist on Stage 10.1
 * baseline, so this handler returns `count: 0` while still resolving the
 * token slug to confirm the row exists (404 otherwise). Banner row is
 * present regardless of count value per Acceptance.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2093 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { getTokenSpineBySlug } from "@/lib/catalog/token-spine-repo";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
} as const;

type Ctx = { params: Promise<{ slug: string }> };

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const token = await getTokenSpineBySlug(slug);
    if (!token) return catalogJsonError(404, "not_found", "Token not found");
    // Stage 14.1 will replace this stub with a real catalog_ref_edge query.
    return NextResponse.json({ ok: true, data: { count: 0 } }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "token-ripple-count" });
    }
    return responseFromPostgresError(e, "Ripple count query failed");
  }
}
