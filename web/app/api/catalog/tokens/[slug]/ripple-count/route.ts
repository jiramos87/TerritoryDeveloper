/**
 * Token ripple-count GET (TECH-3411 / Stage 14.4).
 *
 *   GET /api/catalog/tokens/[slug]/ripple-count -> { ok: true, data: { count } }
 *
 * Per DEC-A44: surfaces "Editing this changes N entities" upstream of the
 * token detail save button. Sources `catalog_ref_edge` (Stage 14.1
 * materializer) joined to `catalog_entity` on `current_published_version_id`
 * — only published incoming edges are counted (DEC-A44 ripple semantics).
 *
 * Envelope unchanged from Stage 10.1 stub (`RippleBanner.tsx` consumer
 * untouched). Banner row remains present regardless of count value.
 *
 * @see ia/projects/asset-pipeline/stage-14.4 — TECH-3411 §Plan Digest
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2093 origin (stub)
 * @see web/lib/repos/refs-repo.ts — sibling `listIncomingRefs` (parity)
 * @see db/migrations/0043_catalog_ref_edge.sql — backing table
 */
import { NextResponse, type NextRequest } from "next/server";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { getSql } from "@/lib/db/client";
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
    const sql = getSql();
    const idNum = Number.parseInt(token.entity_id, 10);
    const rows = (await sql`
      select count(*)::int as count
      from catalog_ref_edge ce
      join catalog_entity src
        on src.id = ce.src_id
       and src.current_published_version_id = ce.src_version_id
      where ce.dst_kind = 'token'
        and ce.dst_id = ${idNum}
    `) as unknown as Array<{ count: number }>;
    const count = rows[0]?.count ?? 0;
    return NextResponse.json({ ok: true, data: { count } }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "token-ripple-count" });
    }
    return responseFromPostgresError(e, "Ripple count query failed");
  }
}
