/**
 * Panel children replace-tree (TECH-1888 / Stage 8.1).
 *
 *   POST /api/catalog/panels/[slug]/children body: CatalogPanelChildSetBody
 *
 * Atomic replace-tree per DEC-A43 (delete-all-then-insert in single tx).
 * Validators run before delete; on validation failure caller returns
 * DEC-A48 error envelope; cycle detection per DEC-A27.
 *
 * @see ia/projects/asset-pipeline (DB) — TECH-1888 §Plan Digest
 */
import { type NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { setPanelChildTree } from "@/lib/catalog/panel-child-set";
import { getPanelSpineBySlug } from "@/lib/catalog/panel-spine-repo";
import type { CatalogPanelChildSetBody } from "@/types/api/catalog-api";

export const dynamic = "force-dynamic";
export const routeMeta = {
  POST: { requires: "catalog.entity.edit" },
} as const;

type Ctx = { params: Promise<{ slug: string }> };

export async function POST(request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  try {
    const wrapped = withAudit(async (req, { emit, sql }) => {
      let body: unknown;
      try {
        body = await req.json();
      } catch {
        throw new Error("validation: Invalid JSON body");
      }
      // Resolve slug -> entity id (no lock; setPanelChildTree locks the panel row).
      const lookup = (await sql`
        select id::text as id from catalog_entity where kind='panel' and slug=${slug} limit 1
      `) as unknown as Array<{ id: string }>;
      if (lookup.length === 0) throw new Error("notfound: Panel not found");
      const panelEntityId = Number.parseInt(lookup[0]!.id, 10);

      const result = await setPanelChildTree(
        panelEntityId,
        body as CatalogPanelChildSetBody,
        sql,
      );
      if (result.ok === "notfound") throw new Error("notfound: Panel not found");
      if (result.ok === "validation") {
        const err = new Error(`validation: ${result.error.code}`) as Error & {
          validationError?: unknown;
        };
        err.validationError = result.error;
        throw err;
      }
      if (result.ok === "stale") {
        const current = await getPanelSpineBySlug(slug, sql);
        const err = new Error("conflict: stale_updated_at") as Error & {
          current?: unknown;
          current_updated_at?: string;
        };
        err.current = current;
        err.current_updated_at = result.current_updated_at;
        throw err;
      }
      await emit("catalog.panel.children_set", "catalog_entity", String(panelEntityId), {
        slug,
        rows_written: result.rows_written,
      });
      return {
        status: 200,
        data: {
          entity_id: String(panelEntityId),
          slug,
          rows_written: result.rows_written,
          updated_at: result.updated_at,
        },
      };
    });
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", "Panel not found");
    }
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      const validationError = (e as Error & { validationError?: unknown }).validationError;
      if (validationError != null) {
        return catalogJsonError(400, "bad_request", "Panel children validation failed", {
          details: validationError,
        });
      }
      return catalogJsonError(400, "bad_request", e.message.replace(/^validation:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      const reason = e.message.replace(/^conflict:\s*/i, "");
      const current = (e as Error & { current?: unknown }).current;
      const current_updated_at = (e as Error & { current_updated_at?: string }).current_updated_at;
      return catalogJsonError(409, "conflict", `Panel children blocked: ${reason}`, {
        current,
        details: { current_updated_at },
      });
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "panel-children-set",
      });
    }
    return responseFromPostgresError(e, "Panel children replace failed");
  }
}
