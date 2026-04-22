/**
 * Row shapes for `catalog_spawn_pool` and `catalog_pool_member` —
 * `db/migrations/0012_catalog_spawn_pools.sql`.
 */
export interface CatalogSpawnPoolRow {
  id: string;
  slug: string;
  owner_category: string;
  owner_subtype: string | null;
}

/**
 * Composite PK `(pool_id, asset_id)`; `weight` &gt; 0 per CHECK.
 */
export interface CatalogPoolMemberRow {
  pool_id: string;
  asset_id: string;
  weight: number;
}
