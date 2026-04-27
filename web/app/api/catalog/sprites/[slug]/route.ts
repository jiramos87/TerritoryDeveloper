/**
 * Sprite by-slug detail + edit + retire/restore/delete-draft (TECH-1675).
 *
 *  GET    /api/catalog/sprites/[slug]                                -> SpriteDetailDto
 *  PATCH  /api/catalog/sprites/[slug]   body: PatchSpriteBody         -> 200 / 409 frozen
 *  DELETE /api/catalog/sprites/[slug]?mode=retire|restore|delete-draft
 *
 * Capabilities: GET/PATCH = catalog.entity.{create|edit}; DELETE = catalog.entity.retire
 * (gated upstream by proxy.ts via route-meta-map).
 *
 * @see ia/projects/asset-pipeline/stage-6.1.md — TECH-1675 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import {
  deleteDraftSprite,
  getSpriteBySlug,
  patchSprite,
  restoreSprite,
  retireSprite,
  type PatchSpriteBody,
} from "@/lib/db/sprite-repo";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
  PATCH: { requires: "catalog.entity.edit" },
  DELETE: { requires: "catalog.entity.retire" },
} as const;

type Ctx = { params: Promise<{ slug: string }> };

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const dto = await getSpriteBySlug(slug);
    if (!dto) return catalogJsonError(404, "not_found", "Sprite not found");
    return NextResponse.json({ ok: true, data: dto }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "sprite-get" });
    }
    return responseFromPostgresError(e, "Get sprite query failed");
  }
}

export async function PATCH(request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const wrapped = withAudit<{ slug: string }>(async (req, { emit, sql }) => {
      let body: unknown;
      try {
        body = await req.json();
      } catch {
        throw new Error("validation: Invalid JSON body");
      }
      const result = await patchSprite(slug, body as PatchSpriteBody, sql);
      if (result.ok === "notfound") throw new Error("notfound: Sprite not found");
      if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
      if (result.ok === "conflict") throw new Error(`conflict: ${result.reason}`);
      await emit("catalog.sprite.edit", "catalog_entity", result.data.entity_id, { slug });
      return { status: 200, data: { slug } };
    });
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      return catalogJsonError(400, "bad_request", e.message.replace(/^validation:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", "Sprite not found");
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      const reason = e.message.replace(/^conflict:\s*/i, "");
      return NextResponse.json(
        { error: `Sprite edit blocked: ${reason}`, code: reason === "frozen_version" ? "frozen_version" : "conflict" },
        { status: 409 },
      );
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "sprite-patch" });
    }
    return responseFromPostgresError(e, "Patch sprite failed");
  }
}

export async function DELETE(request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  const modeRaw = request.nextUrl.searchParams.get("mode") ?? "retire";
  if (modeRaw !== "retire" && modeRaw !== "restore" && modeRaw !== "delete-draft") {
    return catalogJsonError(400, "bad_request", "mode must be retire|restore|delete-draft");
  }
  try {
    const wrapped = withAudit<{ slug: string; mode: string }>(async (_req, { emit, sql }) => {
      const result =
        modeRaw === "retire"
          ? await retireSprite(slug, sql)
          : modeRaw === "restore"
            ? await restoreSprite(slug, sql)
            : await deleteDraftSprite(slug, sql);
      if (result.ok === "notfound") throw new Error("notfound: Sprite not found");
      if (result.ok === "conflict") throw new Error(`conflict: ${result.reason}`);
      if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
      const action =
        modeRaw === "retire"
          ? "catalog.sprite.retire"
          : modeRaw === "restore"
            ? "catalog.sprite.restore"
            : "catalog.sprite.delete_draft";
      await emit(action, "catalog_entity", result.data.entity_id || slug, { slug, mode: modeRaw });
      return { status: 200, data: { slug, mode: modeRaw } };
    });
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", "Sprite not found");
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      const reason = e.message.replace(/^conflict:\s*/i, "");
      return NextResponse.json(
        { error: `Sprite delete blocked: ${reason}`, code: reason },
        { status: 409 },
      );
    }
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      return catalogJsonError(400, "bad_request", e.message.replace(/^validation:\s*/i, ""));
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "sprite-delete" });
    }
    return responseFromPostgresError(e, "Delete sprite failed");
  }
}
