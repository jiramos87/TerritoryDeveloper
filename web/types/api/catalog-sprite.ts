import type { CatalogSpriteProvenance } from "./catalog-enums";

/**
 * Row shape for `catalog_sprite` — `db/migrations/0011_catalog_core.sql`.
 */
export interface CatalogSpriteRow {
  id: string;
  path: string;
  ppu: number;
  pivot_x: number;
  pivot_y: number;
  provenance: CatalogSpriteProvenance;
  generator_archetype_id: string | null;
  generator_build_fingerprint: string | null;
  art_revision: number;
}
