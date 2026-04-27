/**
 * Archetype version PATCH (TECH-2459 + TECH-2460 / Stage 11.1).
 *
 *  PATCH /api/catalog/archetypes/[slug]/versions/[versionId]
 *    body: CatalogArchetypeVersionPatchBody
 *
 * Edits a draft `entity_version.params_json` (and optional `migration_hint_json`).
 * Rejects when version status='published' (slug + schema both frozen on publish).
 * Optimistic concurrency via `updated_at` per DEC-A38.
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2459 §Plan Digest
 */
import { type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { getArchetypeBySlug, patchVersionParams } from "@/lib/catalog/archetype-repo";
import type { CatalogArchetypeVersionPatchBody } from "@/types/api/catalog-api";

export const dynamic = "force-dynamic";
export const routeMeta = {
  PATCH: { requires: "catalog.entity.edit" },
} as const;

type Ctx = { params: Promise<{ slug: string; versionId: string }> };

export async function PATCH(request: NextRequest, ctx: Ctx) {
  const { slug, versionId } = await ctx.params;
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
      const patchBody = body as CatalogArchetypeVersionPatchBody;
      const result = await patchVersionParams(
        arch.entity_id,
        versionId,
        {
          updated_at: patchBody.updated_at,
          params_json: patchBody.params_json,
          migration_hint_json:
            patchBody.migration_hint_json === undefined
              ? undefined
              : patchBody.migration_hint_json === null
                ? null
                : (patchBody.migration_hint_json as unknown as Record<string, unknown>),
        },
        sql,
      );
      if (result.ok === "notfound") throw new Error("notfound: Version not found");
      if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
      if (result.ok === "conflict") {
        const err = new Error(`conflict: ${result.reason}`) as Error & { current?: unknown };
        throw err;
      }
      await emit("catalog.archetype.version.update", "entity_version", result.data.version_id, {
        slug,
        version_id: result.data.version_id,
      });
      return { status: 200, data: result.data };
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
      const reason = e.message.replace(/^conflict:\s*/i, "");
      return catalogJsonError(409, "conflict", `Version edit blocked: ${reason}`);
    }
    return responseFromPostgresError(e, "Patch version failed");
  }
}
