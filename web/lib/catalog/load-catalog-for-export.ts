import { getSql } from "@/lib/db/client";
import {
  mapRowToAssetSprite,
  mapRowToCatalogAsset,
  mapRowToEconomy,
  mapRowToSprite,
} from "@/lib/catalog/row-mappers";
import type { CatalogAssetRow } from "@/types/api/catalog-asset";
import type { CatalogAssetSpriteRow } from "@/types/api/catalog-asset-sprite";
import type { CatalogEconomyRow } from "@/types/api/catalog-economy";
import type { CatalogSpriteRow } from "@/types/api/catalog-sprite";

/**
 * Load joined catalog tables for file export.
 * Default: `status = published` only. {@link LoadCatalogForExportOptions.includeDrafts} adds `draft` rows; `retired` always excluded.
 *
 * **Stable ordering** (baker / snapshot determinism): assets by `(category, slug, id)`; bindings `(asset_id, slot)`; economy `asset_id`; sprites `id`.
 */
export type LoadCatalogForExportOptions = {
  includeDrafts: boolean;
};

export type LoadedCatalogForExport = {
  assets: CatalogAssetRow[];
  sprites: CatalogSpriteRow[];
  bindings: CatalogAssetSpriteRow[];
  economy: CatalogEconomyRow[];
};

function sortSlots(a: CatalogAssetSpriteRow, b: CatalogAssetSpriteRow): number {
  const ca = a.asset_id.localeCompare(b.asset_id, "en", { numeric: true });
  if (ca !== 0) return ca;
  return a.slot.localeCompare(b.slot);
}

export async function loadCatalogForExport(
  opts: LoadCatalogForExportOptions,
): Promise<LoadedCatalogForExport> {
  const sqlConn = getSql();
  const assetsRaw = opts.includeDrafts
    ? await sqlConn`
        select * from catalog_asset
        where status in ('draft', 'published')
        order by category asc, slug asc, id asc
      `
    : await sqlConn`
        select * from catalog_asset
        where status = 'published'
        order by category asc, slug asc, id asc
      `;

  const assets = (assetsRaw as unknown as never[]).map((r) =>
    mapRowToCatalogAsset(r as never),
  );
  if (assets.length === 0) {
    return { assets, sprites: [], bindings: [], economy: [] };
  }

  const assetIds = assets.map((a) => Number(a.id));

  const bindRaw = await sqlConn`
    select asset_id, sprite_id, slot
    from catalog_asset_sprite
    where asset_id in ${sqlConn(assetIds)}
    order by asset_id asc, slot asc
  `;
  const bindings = (bindRaw as unknown as never[]).map((r) =>
    mapRowToAssetSprite(r as never),
  );

  const ecoRaw = await sqlConn`
    select * from catalog_economy
    where asset_id in ${sqlConn(assetIds)}
    order by asset_id asc
  `;
  const economy = (ecoRaw as unknown as never[]).map((r) =>
    mapRowToEconomy(r as never),
  );

  const spriteIdSet = new Set<string>();
  for (const b of bindings) {
    spriteIdSet.add(b.sprite_id);
  }
  const spriteIdNums = [...spriteIdSet].map((s) => Number(s));
  if (spriteIdNums.length === 0) {
    return { assets, sprites: [], bindings, economy };
  }

  const sprRaw = await sqlConn`
    select * from catalog_sprite
    where id in ${sqlConn(spriteIdNums)}
    order by id asc
  `;
  const sprites = (sprRaw as unknown as never[]).map((r) =>
    mapRowToSprite(r as never),
  );

  return {
    assets,
    sprites,
    bindings: [...bindings].sort(sortSlots),
    economy,
  };
}
