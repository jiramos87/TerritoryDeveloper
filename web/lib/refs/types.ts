/**
 * Canonical row shape + edge-role union for `catalog_ref_edge`
 * (DEC-A37 + DEC-A42, asset-pipeline Stage 14.1 / TECH-3001).
 *
 * Materialized cross-entity ref graph populated by the publish hook
 * (`web/lib/refs/edge-builder.ts` — TECH-3002) on every entity publish.
 *
 * @see db/migrations/0043_catalog_ref_edge.sql — table + indexes
 * @see ia/projects/asset-pipeline/stage-14.1 — Stage 14.1 master plan
 */

/**
 * 8 catalog kinds (matches `catalog_entity.kind` CHECK constraint in
 * `db/migrations/0021_catalog_spine.sql`). Inline declaration — no
 * pre-existing canonical alias module exists; future consolidation
 * may move this to `web/lib/catalog/kinds.ts`.
 */
export type CatalogKind =
  | "sprite"
  | "asset"
  | "button"
  | "panel"
  | "pool"
  | "token"
  | "archetype"
  | "audio";

/**
 * 8 enumerated edge roles per spec §2.1 (Stage 14.1 objective).
 *
 * Discriminator on `catalog_ref_edge.edge_role`. New roles may be added
 * in later Stages (e.g. `button.token`); current MVP locks the 8 listed.
 */
export type EdgeRole =
  | "panel.token"
  | "button.sprite"
  | "asset.sprite"
  | "pool.asset"
  | "archetype.asset"
  | "archetype.sprite"
  | "archetype.token"
  | "archetype.audio";

/**
 * Row shape for `catalog_ref_edge` (mirrors SQL columns 1:1).
 *
 * `created_at` typed as ISO-8601 string — matches `postgres` driver
 * default text decoding for `timestamptz` reads in this codebase.
 */
export interface CatalogRefEdge {
  src_kind: CatalogKind;
  src_id: number;
  src_version_id: number;
  dst_kind: CatalogKind;
  dst_id: number;
  dst_version_id: number;
  edge_role: EdgeRole;
  created_at: string;
}
