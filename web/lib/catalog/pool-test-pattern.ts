/**
 * Test-only insert pattern for `catalog_pool_member` (no production route).
 * Use with `postgres` tagged template from `web/lib/db/client` in Vitest or local scripts
 * after parent `catalog_spawn_pool` + `catalog_asset` rows exist.
 *
 * @example
 * ```ts
 * import { sql } from "@/lib/db/client";
 * import { examplePoolMemberInsertValues } from "@/lib/catalog/pool-test-pattern";
 *
 * const v = examplePoolMemberInsertValues("1", "2", "3");
 * await sql`
 *   INSERT INTO catalog_pool_member (pool_id, asset_id, weight)
 *   VALUES (${v.pool_id}::bigint, ${v.asset_id}::bigint, ${v.weight})
 * `;
 * ```
 */
export function examplePoolMemberInsertValues(
  poolId: string,
  assetId: string,
  weight: number
): { pool_id: string; asset_id: string; weight: number } {
  if (weight < 1) throw new Error("weight must be >= 1 per 0012 CHECK");
  return { pool_id: poolId, asset_id: assetId, weight };
}
