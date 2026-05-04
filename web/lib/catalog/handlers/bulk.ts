/**
 * Catalog bulk handler — wraps bulk-actions for MCP tool surface (DEC-A35).
 */

import type { TransactionSql } from "postgres";
import { runBulkRetire, runBulkRestore, runBulkPublish, type BulkAction } from "@/lib/catalog/bulk-actions";

export type { BulkAction };

export type BulkResult =
  | { ok: "ok"; data: { updated: number; action: BulkAction } }
  | { ok: "validation"; reason: string };

/** Run bulk retire/restore/publish for given entity_ids. */
export async function bulkCatalogAction(
  action: BulkAction,
  entityIds: string[],
  sql: TransactionSql,
): Promise<BulkResult> {
  if (entityIds.length === 0 || entityIds.length > 1000) {
    return { ok: "validation", reason: "entity_ids must be a non-empty array of ≤1000 ids" };
  }

  let result;
  if (action === "retire") {
    result = await runBulkRetire(sql, entityIds);
  } else if (action === "restore") {
    result = await runBulkRestore(sql, entityIds);
  } else {
    result = await runBulkPublish(sql, entityIds);
  }

  return { ok: "ok", data: { updated: result.updated, action } };
}
