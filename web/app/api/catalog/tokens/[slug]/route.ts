/**
 * Token by-slug GET + PATCH (TECH-2092 / Stage 10.1).
 *
 *  GET   /api/catalog/tokens/[slug]                 -> CatalogTokenDto
 *  PATCH /api/catalog/tokens/[slug] body: CatalogTokenPatchBody
 *
 * Optimistic concurrency via `updated_at` per DEC-A38; envelope per DEC-A48.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2092 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { getTokenSpineBySlug, patchTokenSpine } from "@/lib/catalog/token-spine-repo";
import type { CatalogTokenPatchBody } from "@/types/api/catalog-api";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
  PATCH: { requires: "catalog.entity.edit" },
} as const;

type Ctx = { params: Promise<{ slug: string }> };

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const dto = await getTokenSpineBySlug(slug);
    if (!dto) return catalogJsonError(404, "not_found", "Token not found");
    return NextResponse.json({ ok: true, data: dto }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "token-get" });
    }
    return responseFromPostgresError(e, "Get token failed");
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
      const result = await patchTokenSpine(slug, body as CatalogTokenPatchBody, sql);
      if (result.ok === "notfound") throw new Error("notfound: Token not found");
      if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
      if (result.ok === "conflict") {
        const err = new Error(`conflict: ${result.reason}`) as Error & { current?: unknown };
        err.current = result.current;
        throw err;
      }
      await emit("catalog.token.update", "catalog_entity", result.data.entity_id, {
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
      return catalogJsonError(404, "not_found", "Token not found");
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      const reason = e.message.replace(/^conflict:\s*/i, "");
      const current = (e as Error & { current?: unknown }).current;
      return catalogJsonError(409, "conflict", `Token edit blocked: ${reason}`, { current });
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "token-patch" });
    }
    return responseFromPostgresError(e, "Patch token failed");
  }
}
