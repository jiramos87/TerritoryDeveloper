/**
 * Archetype version list + clone (TECH-2459 + TECH-2461 / Stage 11.1).
 *
 *  GET /api/catalog/archetypes/[slug]/versions
 *    -> { items: CatalogArchetypeVersionWithPinCount[] }
 *
 *  POST /api/catalog/archetypes/[slug]/versions
 *    body: { source_version_id: string }
 *    -> { new_version_id: string }
 *
 * Pinned-entity counts derived from `entity_version.archetype_version_id`
 * back-references (TECH-2459 §Acceptance row 3). POST seeds a new draft from
 * a published source version (TECH-2461 bump-flow entry).
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2459 + TECH-2461 §Plan Digests
 */
import { NextResponse, type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import {
  clonePublishedToDraft,
  getArchetypeBySlug,
  listVersionsForArchetype,
} from "@/lib/catalog/archetype-repo";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
  POST: { requires: "catalog.entity.edit" },
} as const;

type Ctx = { params: Promise<{ slug: string }> };

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const arch = await getArchetypeBySlug(slug);
    if (!arch) return catalogJsonError(404, "not_found", "Archetype not found");
    const items = await listVersionsForArchetype(arch.entity_id);
    return NextResponse.json({ ok: true, data: { items } }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "archetype-versions-list",
      });
    }
    return responseFromPostgresError(e, "Archetype versions list failed");
  }
}

export async function POST(request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const wrapped = withAudit(async (req, { emit, sql }) => {
      const arch = await getArchetypeBySlug(slug, sql);
      if (arch == null) throw new Error("notfound: Archetype not found");
      let body: unknown;
      try {
        body = await req.json();
      } catch {
        throw new Error("validation: Invalid JSON body");
      }
      const source = (body as { source_version_id?: unknown })?.source_version_id;
      if (typeof source !== "string" || !/^\d+$/.test(source)) {
        throw new Error("validation: source_version_id must be a numeric string");
      }
      const out = await clonePublishedToDraft(arch.entity_id, source, sql);
      if (out.ok === "notfound") throw new Error("notfound: Source version not found");
      if (out.ok === "validation") throw new Error(`validation: ${out.reason}`);
      if (out.ok === "conflict") throw new Error(`conflict: ${out.reason}`);
      await emit(
        "catalog.archetype.version.clone",
        "entity_version",
        out.data.new_version_id,
        { slug, source_version_id: source, new_version_id: out.data.new_version_id },
      );
      return { status: 201, data: { new_version_id: out.data.new_version_id } };
    });
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      return catalogJsonError(400, "bad_request", e.message.replace(/^validation:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", e.message.replace(/^notfound:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      return catalogJsonError(409, "conflict", e.message.replace(/^conflict:\s*/i, ""));
    }
    return responseFromPostgresError(e, "Archetype version clone failed");
  }
}
