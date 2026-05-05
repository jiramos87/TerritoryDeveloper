/**
 * Cross-kind catalog entity search (TECH-4180 / Stage 15.1).
 *
 *  GET /api/catalog/search?q=<text>&kind=<kind>&limit=<1..100>
 *    -> 200 { ok: true, data: { results: [{entity_id, kind, slug, display_name, score}], total } }
 *    -> 400 missing_q | invalid_kind | invalid_limit
 *
 * Read-only — gates on `catalog.entity.read` (DEC-A33; seeded in mig 0072).
 *
 * Audit emitted when q is non-empty: action='catalog_search', target_kind=NULL,
 * payload={q, kind, result_count}.
 *
 * @see web/lib/catalog/search-query.ts — trgm query engine
 * @see db/migrations/0051_pg_trgm_search.sql — GIN indexes
 */
import { type NextRequest, NextResponse } from "next/server";

import {
  catalogJsonError,
  responseFromPostgresError,
} from "@/lib/catalog/catalog-api-errors";
import {
  DEFAULT_LIMIT,
  MAX_LIMIT,
  searchCatalogEntities,
  VALID_KINDS,
} from "@/lib/catalog/search-query";
import { audit } from "@/lib/audit/emitter";
import { getSql } from "@/lib/db/client";
import type { CatalogKind } from "@/lib/refs/types";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.read" },
} as const;

export async function GET(request: NextRequest) {
  const url = new URL(request.url);
  const q = url.searchParams.get("q");
  if (!q || q.trim().length === 0) {
    return catalogJsonError(400, "bad_request", "Missing q parameter");
  }
  const qTrimmed = q.trim();

  const kindRaw = url.searchParams.get("kind");
  if (kindRaw != null && !VALID_KINDS.has(kindRaw)) {
    return catalogJsonError(400, "bad_request", "Invalid kind", {
      details: { kind: kindRaw },
    });
  }
  const kind = kindRaw as CatalogKind | null;

  const limitRaw = url.searchParams.get("limit");
  let limit = DEFAULT_LIMIT;
  if (limitRaw != null) {
    const n = Number.parseInt(limitRaw, 10);
    if (Number.isNaN(n) || n < 1 || n > MAX_LIMIT) {
      return catalogJsonError(400, "bad_request", `limit must be 1..${MAX_LIMIT}`);
    }
    limit = n;
  }

  try {
    const result = await searchCatalogEntities({ q: qTrimmed, kind, limit });

    const sql = getSql();
    await audit(sql, {
      action: "catalog_search",
      actor_user_id: null,
      target_kind: "catalog_entity",
      target_id: "",
      payload: { q: qTrimmed, kind: kind ?? null, result_count: result.total },
    });

    return NextResponse.json({ ok: true, data: result }, { status: 200 });
  } catch (e) {
    if (
      e instanceof Error &&
      e.message === "DATABASE_URL not set — required for DB access."
    ) {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "catalog-search",
      });
    }
    return responseFromPostgresError(e, "Catalog search failed");
  }
}
