/**
 * Catalog search handler — wraps search-query searchCatalogEntities.
 */

import type { CatalogKind } from "@/lib/refs/types";
import {
  searchCatalogEntities,
  type SearchQueryResult,
} from "@/lib/catalog/search-query";

export interface CatalogSearchInput {
  q: string;
  kind?: CatalogKind | null;
  limit?: number;
}

export async function searchCatalog(input: CatalogSearchInput): Promise<SearchQueryResult> {
  return searchCatalogEntities({ q: input.q, kind: input.kind, limit: input.limit });
}
