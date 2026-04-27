/**
 * Archetype retire / restore (TECH-2459 / Stage 11.1).
 *
 *  POST   /api/catalog/archetypes/[slug]/retire   -> soft-retire (idempotent)
 *  DELETE /api/catalog/archetypes/[slug]/retire   -> restore (idempotent)
 *
 * Per DEC-A23 retire-not-delete; idempotent re-retire returns 200 with current.
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2459 §Plan Digest
 */
import { type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { restoreArchetype, retireArchetype } from "@/lib/catalog/archetype-repo";

export const dynamic = "force-dynamic";
export const routeMeta = {
  POST: { requires: "catalog.entity.edit" },
  DELETE: { requires: "catalog.entity.edit" },
} as const;

type Ctx = { params: Promise<{ slug: string }> };

export async function POST(request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const wrapped = withAudit(async (_req, { emit, sql }) => {
      const result = await retireArchetype(slug, sql);
      if (result.ok === "notfound") throw new Error("notfound: Archetype not found");
      if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
      if (result.ok === "conflict") throw new Error(`conflict: ${result.reason}`);
      await emit("catalog.archetype.retire", "catalog_entity", result.data.entity_id, {
        slug,
      });
      return { status: 200, data: result.data };
    });
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", "Archetype not found");
    }
    return responseFromPostgresError(e, "Retire archetype failed");
  }
}

export async function DELETE(request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const wrapped = withAudit(async (_req, { emit, sql }) => {
      const result = await restoreArchetype(slug, sql);
      if (result.ok === "notfound") throw new Error("notfound: Archetype not found");
      if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
      if (result.ok === "conflict") throw new Error(`conflict: ${result.reason}`);
      await emit("catalog.archetype.restore", "catalog_entity", result.data.entity_id, {
        slug,
      });
      return { status: 200, data: result.data };
    });
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", "Archetype not found");
    }
    return responseFromPostgresError(e, "Restore archetype failed");
  }
}
