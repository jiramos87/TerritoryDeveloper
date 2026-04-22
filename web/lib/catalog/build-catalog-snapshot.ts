import type { LoadedCatalogForExport } from "@/lib/catalog/load-catalog-for-export";
import type { CatalogAssetRow } from "@/types/api/catalog-asset";
import type { CatalogAssetSpriteRow } from "@/types/api/catalog-asset-sprite";
import type { CatalogEconomyRow } from "@/types/api/catalog-economy";
import type { CatalogSpriteRow } from "@/types/api/catalog-sprite";

/** Bump in lockstep with Unity `GridAssetCatalog` when the JSON shape breaks. */
export const CATALOG_SNAPSHOT_SCHEMA_VERSION = 1;

export type CatalogImportHygieneEntry = {
  /** Path from `catalog_sprite.path` */
  texturePath: string;
  pixelsPerUnit: number;
  pivot: { x: number; y: number };
};

export type CatalogSnapshotFile = {
  schemaVersion: number;
  generatedAt: string;
  includeDrafts: boolean;
  assets: CatalogAssetRow[];
  sprites: CatalogSpriteRow[];
  bindings: CatalogAssetSpriteRow[];
  economy: CatalogEconomyRow[];
  importHygiene: CatalogImportHygieneEntry[];
};

export function buildImportHygiene(sprites: CatalogSpriteRow[]): CatalogImportHygieneEntry[] {
  const rows: CatalogImportHygieneEntry[] = sprites.map((s) => ({
    texturePath: s.path,
    pixelsPerUnit: s.ppu,
    pivot: { x: s.pivot_x, y: s.pivot_y },
  }));
  return [...rows].sort((a, b) =>
    a.texturePath.localeCompare(b.texturePath, "en", { numeric: true }),
  );
}

/**
 * Assemble the versioned JSON envelope. Key order is normalized by `stableJsonStringify`.
 */
export function buildCatalogSnapshot(
  loaded: LoadedCatalogForExport,
  meta: { includeDrafts: boolean },
): CatalogSnapshotFile {
  return {
    schemaVersion: CATALOG_SNAPSHOT_SCHEMA_VERSION,
    generatedAt: new Date().toISOString(),
    includeDrafts: meta.includeDrafts,
    assets: loaded.assets,
    sprites: loaded.sprites,
    bindings: loaded.bindings,
    economy: loaded.economy,
    importHygiene: buildImportHygiene(loaded.sprites),
  };
}
