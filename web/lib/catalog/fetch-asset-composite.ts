import { getSql } from "@/lib/db/client";
import type { CatalogAssetByIdResult } from "@/types/api/catalog-asset-by-id";
import {
  mapRowToAssetSprite,
  mapRowToCatalogAsset,
  mapRowToEconomy,
  mapRowToSprite,
} from "@/lib/catalog/row-mappers";

/**
 * @see `ia/projects/TECH-641.md` — one round-trip for bindings + sprite rows, plus economy row.
 * Asset row + economy + slots use separate queries; slots query is one join (no N+1).
 */
export async function loadCatalogAssetById(
  idParam: string,
): Promise<CatalogAssetByIdResult | "notfound" | "badid"> {
  if (!/^\d{1,32}$/.test(idParam)) return "badid";
  const idNum = Number(idParam);
  if (!Number.isSafeInteger(idNum) || idNum < 1) return "badid";
  const sql = getSql();
  const ar = await sql`select * from catalog_asset where id = ${idNum} limit 1`;
  if (ar.length === 0) return "notfound";
  const asset = mapRowToCatalogAsset(ar[0] as never);
  const ecoRows = await sql`select * from catalog_economy where asset_id = ${idNum} limit 1`;
  const economy = ecoRows[0] ? mapRowToEconomy(ecoRows[0] as never) : null;
  const slotSql = await sql`
    select
      cas.asset_id,
      cas.sprite_id,
      cas.slot,
      spr.id,
      spr.path,
      spr.ppu,
      spr.pivot_x,
      spr.pivot_y,
      spr.provenance,
      spr.generator_archetype_id,
      spr.generator_build_fingerprint,
      spr.art_revision
    from catalog_asset_sprite cas
    inner join catalog_sprite spr on spr.id = cas.sprite_id
    where cas.asset_id = ${idNum}
  `;
  const sprite_slots: CatalogAssetByIdResult["sprite_slots"] = (
    slotSql as unknown as Array<{
      asset_id: string | number | bigint;
      sprite_id: string | number | bigint;
      slot: string;
      id: string | number | bigint;
      path: string;
      ppu: number;
      pivot_x: number;
      pivot_y: number;
      provenance: string;
      generator_archetype_id: string | null;
      generator_build_fingerprint: string | null;
      art_revision: number;
    }>
  ).map((r) => {
    return {
      binding: mapRowToAssetSprite({
        asset_id: r.asset_id,
        sprite_id: r.sprite_id,
        slot: r.slot,
      }),
      sprite: mapRowToSprite({
        id: r.id,
        path: r.path,
        ppu: r.ppu,
        pivot_x: r.pivot_x,
        pivot_y: r.pivot_y,
        provenance: r.provenance,
        generator_archetype_id: r.generator_archetype_id,
        generator_build_fingerprint: r.generator_build_fingerprint,
        art_revision: r.art_revision,
      }),
    };
  });
  return { asset, economy, sprite_slots };
}
