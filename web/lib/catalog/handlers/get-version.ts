/**
 * Catalog version-history handler — wraps history-repo listVersions.
 */

import type { CatalogKind } from "@/lib/refs/types";
import { listVersions, type ListVersionsResult } from "@/lib/repos/history-repo";

export interface GetVersionInput {
  entityId: string;
  cursor?: string | null;
  limit?: number | null;
}

export async function getCatalogVersions(
  kind: CatalogKind,
  input: GetVersionInput,
): Promise<ListVersionsResult> {
  return listVersions(kind, input.entityId, input.cursor ?? null, input.limit);
}
