/**
 * Spine-aware asset by-slug GET + PATCH (TECH-1786 + TECH-1789).
 *
 * Distinct from the legacy numeric-id route at `[id]/route.ts` (which targets
 * the deprecated `catalog_asset` table). This slug path reads/writes the
 * spine model: catalog_entity (kind=asset) + asset_detail + economy_detail
 * + pool_member.
 *
 *  GET   /api/catalog/assets/[slug]                         -> CatalogAssetSpineDto
 *  PATCH /api/catalog/assets/[slug] body: CatalogAssetSpinePatchBody
 *
 * Capability: GET = catalog.entity.create; PATCH = catalog.entity.edit.
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1786 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import {
  getAssetSpineBySlug,
  patchAssetSpine,
} from "@/lib/catalog/asset-spine-repo";
import type { CatalogAssetSpinePatchBody } from "@/types/api/catalog-api";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
  PATCH: { requires: "catalog.entity.edit" },
} as const;

type Ctx = { params: Promise<{ slug: string }> };

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const dto = await getAssetSpineBySlug(slug);
    if (!dto) return catalogJsonError(404, "not_found", "Asset not found");
    return NextResponse.json({ ok: true, data: dto }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "asset-spine-get" });
    }
    return responseFromPostgresError(e, "Get asset (spine) failed");
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
      const result = await patchAssetSpine(slug, body as CatalogAssetSpinePatchBody, sql);
      if (result.ok === "notfound") throw new Error("notfound: Asset not found");
      if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
      if (result.ok === "conflict") throw new Error(`conflict: ${result.reason}`);
      await emit("catalog.asset.edit", "catalog_entity", result.data.entity_id, { slug });
      return { status: 200, data: result.data };
    });
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      return catalogJsonError(400, "bad_request", e.message.replace(/^validation:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", "Asset not found");
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      const reason = e.message.replace(/^conflict:\s*/i, "");
      return NextResponse.json(
        {
          error: `Asset edit blocked: ${reason}`,
          code:
            reason === "stale_updated_at"
              ? "conflict"
              : reason === "primary_not_in_membership"
                ? "primary_not_in_membership"
                : "conflict",
        },
        { status: 409 },
      );
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "asset-spine-patch" });
    }
    return responseFromPostgresError(e, "Patch asset (spine) failed");
  }
}
