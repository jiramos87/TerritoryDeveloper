/**
 * Cross-kind catalog entity similarity search (TECH-4180 / Stage 15.1).
 *
 * Uses pg_trgm `similarity()` against GIN-indexed columns on `catalog_entity`.
 * Indexes: catalog_entity_name_trgm_idx, catalog_entity_slug_trgm_idx,
 *          catalog_entity_tags_trgm_idx (0051_pg_trgm_search.sql).
 *
 * Score = similarity(lower(coalesce(display_name, slug)), lower(q)).
 * Ordered by score DESC, display_name ASC for deterministic tiebreak.
 *
 * @see web/app/api/catalog/search/route.ts — API surface
 */
import { getSql } from "@/lib/db/client";
import type { CatalogKind } from "@/lib/refs/types";

export const VALID_KINDS = new Set<string>([
  "sprite",
  "asset",
  "button",
  "panel",
  "pool",
  "token",
  "archetype",
  "audio",
]);

export const DEFAULT_LIMIT = 20;
export const MAX_LIMIT = 100;
export const MIN_SCORE = 0.1;

export type SearchResultRow = {
  entity_id: string;
  kind: string;
  slug: string;
  display_name: string;
  score: number;
};

export type SearchQueryResult = {
  results: SearchResultRow[];
  total: number;
};

export async function searchCatalogEntities(opts: {
  q: string;
  kind?: CatalogKind | null;
  limit?: number;
}): Promise<SearchQueryResult> {
  const sql = getSql();
  const { q, kind, limit = DEFAULT_LIMIT } = opts;
  const qLower = q.toLowerCase();

  const kindCond = kind != null ? sql` AND e.kind = ${kind}` : sql``;

  const rows = await sql<SearchResultRow[]>`
    SELECT
      e.id::text                                                           AS entity_id,
      e.kind,
      e.slug,
      e.display_name,
      similarity(lower(coalesce(e.display_name, e.slug)), ${qLower})::float AS score
    FROM catalog_entity e
    WHERE
      e.retired_at IS NULL
      AND (
        lower(coalesce(e.display_name, e.slug)) % ${qLower}
        OR lower(e.slug) % ${qLower}
        OR lower(e.tags::text) % ${qLower}
      )
      ${kindCond}
    ORDER BY score DESC, e.display_name ASC
    LIMIT ${limit}
  `;

  return { results: rows, total: rows.length };
}
