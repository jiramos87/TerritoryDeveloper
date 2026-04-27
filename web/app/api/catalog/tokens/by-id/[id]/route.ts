/**
 * Slim token by-id GET (TECH-2093 / Stage 10.1).
 *
 *   GET /api/catalog/tokens/by-id/[id] -> { token_detail: { semantic_target_entity_id } | null }
 *
 * Used by `<SemanticTokenEditor>` cycle-check fetcher to walk the alias chain
 * one hop at a time without re-fetching the heavy slug DTO. Returns just the
 * `semantic_target_entity_id` field needed by `semanticCycleCheck`.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2093 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { getSql } from "@/lib/db/client";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
} as const;

type Ctx = { params: Promise<{ id: string }> };

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { id } = await ctx.params;
  if (!/^\d+$/.test(id)) {
    return catalogJsonError(400, "bad_request", "id must be a non-negative integer string");
  }
  const idNum = Number.parseInt(id, 10);
  try {
    const sql = getSql();
    const rows = (await sql`
      select d.semantic_target_entity_id::text as semantic_target_entity_id
      from catalog_entity e
      left join token_detail d on d.entity_id = e.id
      where e.id = ${idNum} and e.kind = 'token'
      limit 1
    `) as unknown as Array<{ semantic_target_entity_id: string | null }>;
    if (rows.length === 0) return catalogJsonError(404, "not_found", "Token not found");
    return NextResponse.json(
      {
        ok: true,
        data: {
          token_detail: { semantic_target_entity_id: rows[0]!.semantic_target_entity_id },
        },
      },
      { status: 200 },
    );
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "token-by-id" });
    }
    return responseFromPostgresError(e, "Token by-id query failed");
  }
}
