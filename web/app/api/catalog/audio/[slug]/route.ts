/**
 * Audio entity detail by slug (TECH-1958).
 *
 *  GET /api/catalog/audio/[slug] -> AudioDetailDto
 *
 * Capability: catalog.entity.create (gated upstream by proxy via
 * route-meta-map). PATCH/DELETE are not in TECH-1958 §Acceptance scope
 * (audio entity create + retire flow lives behind /api/catalog/entities
 * with kind=audio per §Acceptance row 6).
 *
 * @see ia/projects/asset-pipeline/stage-9.1/TECH-1958.md
 */
import { NextResponse, type NextRequest } from "next/server";
import {
  catalogJsonError,
  responseFromPostgresError,
} from "@/lib/catalog/catalog-api-errors";
import { getAudioBySlug } from "@/lib/db/audio-repo";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
} as const;

type Ctx = { params: Promise<{ slug: string }> };

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const dto = await getAudioBySlug(slug);
    if (!dto) return catalogJsonError(404, "not_found", "Audio entity not found");
    return NextResponse.json({ ok: true, data: dto }, { status: 200 });
  } catch (e) {
    if (
      e instanceof Error &&
      e.message === "DATABASE_URL not set — required for DB access."
    ) {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "audio-get",
      });
    }
    return responseFromPostgresError(e, "Get audio entity query failed");
  }
}
