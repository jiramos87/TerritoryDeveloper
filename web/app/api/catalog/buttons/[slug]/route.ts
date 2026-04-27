/**
 * Button by-slug GET + PATCH (TECH-1885 / Stage 8.1).
 *
 *  GET   /api/catalog/buttons/[slug]                  -> CatalogButtonDto
 *  PATCH /api/catalog/buttons/[slug] body: CatalogButtonPatchBody
 *
 * Optimistic concurrency via `updated_at` per DEC-A38; envelope per DEC-A48.
 *
 * @see ia/projects/asset-pipeline (DB) — TECH-1885 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { getButtonSpineBySlug, patchButtonSpine } from "@/lib/catalog/button-spine-repo";
import type { CatalogButtonPatchBody } from "@/types/api/catalog-api";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
  PATCH: { requires: "catalog.entity.edit" },
} as const;

type Ctx = { params: Promise<{ slug: string }> };

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const dto = await getButtonSpineBySlug(slug);
    if (!dto) return catalogJsonError(404, "not_found", "Button not found");
    return NextResponse.json({ ok: true, data: dto }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "button-get" });
    }
    return responseFromPostgresError(e, "Get button failed");
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
      const result = await patchButtonSpine(slug, body as CatalogButtonPatchBody, sql);
      if (result.ok === "notfound") throw new Error("notfound: Button not found");
      if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
      if (result.ok === "conflict") {
        const err = new Error(`conflict: ${result.reason}`) as Error & { current?: unknown };
        err.current = result.current;
        throw err;
      }
      await emit("catalog.button.update", "catalog_entity", result.data.entity_id, {
        slug,
      });
      return { status: 200, data: result.data };
    });
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      return catalogJsonError(400, "bad_request", e.message.replace(/^validation:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", "Button not found");
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      const reason = e.message.replace(/^conflict:\s*/i, "");
      const current = (e as Error & { current?: unknown }).current;
      return catalogJsonError(409, "conflict", `Button edit blocked: ${reason}`, { current });
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "button-patch" });
    }
    return responseFromPostgresError(e, "Patch button failed");
  }
}
