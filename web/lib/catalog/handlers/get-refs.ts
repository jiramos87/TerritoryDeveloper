/**
 * Catalog refs handler — wraps refs-repo listIncomingRefs / listOutgoingRefs.
 */

import type { CatalogKind } from "@/lib/refs/types";
import {
  listIncomingRefs,
  listOutgoingRefs,
  type ListRefsResult,
} from "@/lib/repos/refs-repo";

export interface GetRefsInput {
  entityId: string;
  cursor?: string | null;
  limit?: number | null;
}

export interface GetRefsOutput {
  incoming: ListRefsResult;
  outgoing: ListRefsResult;
}

export async function getCatalogRefs(
  kind: CatalogKind,
  input: GetRefsInput,
): Promise<GetRefsOutput> {
  const [incoming, outgoing] = await Promise.all([
    listIncomingRefs(kind, input.entityId, input.cursor ?? null, input.limit),
    listOutgoingRefs(kind, input.entityId, input.cursor ?? null, input.limit),
  ]);
  return { incoming, outgoing };
}
