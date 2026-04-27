/**
 * Pool by-slug GET + PATCH (TECH-1788).
 *
 *  GET   /api/catalog/pools/[slug]                     -> CatalogPoolDto
 *  PATCH /api/catalog/pools/[slug] body: CatalogPoolPatchBody
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1788 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import {
  getPoolSpineBySlug,
  patchPoolSpine,
} from "@/lib/catalog/pool-spine-repo";
import type { CatalogPoolPatchBody } from "@/types/api/catalog-api";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
  PATCH: { requires: "catalog.entity.edit" },
} as const;

type Ctx = { params: Promise<{ slug: string }> };

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const dto = await getPoolSpineBySlug(slug);
    if (!dto) return catalogJsonError(404, "not_found", "Pool not found");
    return NextResponse.json({ ok: true, data: dto }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "pool-get" });
    }
    return responseFromPostgresError(e, "Get pool failed");
  }
}

export async function PATCH(request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const wrapped = withAudit(async (req, { emit, sql }) => {
      let body: unknown;
      try {
        body = await req.json();
      } catch {
        throw new Error("validation: Invalid JSON body");
      }
      const result = await patchPoolSpine(slug, body as CatalogPoolPatchBody, sql);
      if (result.ok === "notfound") throw new Error("notfound: Pool not found");
      if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
      if (result.ok === "conflict") throw new Error(`conflict: ${result.reason}`);
      await emit("catalog.pool.member.update", "catalog_entity", result.data.entity_id, {
        slug,
        members_after: result.data.members.length,
      });
      return { status: 200, data: result.data };
    });
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      return catalogJsonError(400, "bad_request", e.message.replace(/^validation:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", "Pool not found");
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      const reason = e.message.replace(/^conflict:\s*/i, "");
      return NextResponse.json(
        { error: `Pool edit blocked: ${reason}`, code: reason === "stale_updated_at" ? "conflict" : "conflict" },
        { status: 409 },
      );
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "pool-patch" });
    }
    return responseFromPostgresError(e, "Patch pool failed");
  }
}
