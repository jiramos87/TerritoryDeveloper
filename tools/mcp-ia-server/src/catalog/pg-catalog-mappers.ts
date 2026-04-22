/**
 * Row mappers for grid catalog Postgres tables — mirrors `web/lib/catalog/row-mappers.ts`
 * without importing the Next.js app.
 */

import type { PoolClient } from "pg";

/** `bigserial` / numeric id from postgres → JSON string id. */
export function idString(v: string | number | bigint): string {
  if (typeof v === "string") return v;
  if (typeof v === "bigint") return v.toString();
  if (v === null || v === undefined) return "";
  return String(v);
}

function tsString(v: Date | string | null | undefined): string {
  if (v == null) return new Date(0).toISOString();
  if (v instanceof Date) return v.toISOString();
  return v;
}

export interface CatalogAssetRowJson {
  id: string;
  category: string;
  slug: string;
  display_name: string;
  status: string;
  replaced_by: string | null;
  footprint_w: number;
  footprint_h: number;
  placement_mode: string | null;
  unlocks_after: string | null;
  has_button: boolean;
  updated_at: string;
}

export function mapRowToCatalogAsset(row: {
  id: string | number | bigint;
  category: string;
  slug: string;
  display_name: string;
  status: string;
  replaced_by: string | number | bigint | null;
  footprint_w: number;
  footprint_h: number;
  placement_mode: string | null;
  unlocks_after: string | null;
  has_button: boolean;
  updated_at: Date | string;
}): CatalogAssetRowJson {
  return {
    id: idString(row.id),
    category: row.category,
    slug: row.slug,
    display_name: row.display_name,
    status: row.status,
    replaced_by: row.replaced_by == null ? null : idString(row.replaced_by),
    footprint_w: row.footprint_w,
    footprint_h: row.footprint_h,
    placement_mode: row.placement_mode,
    unlocks_after: row.unlocks_after,
    has_button: row.has_button,
    updated_at: tsString(row.updated_at),
  };
}

export interface CatalogEconomyRowJson {
  asset_id: string;
  base_cost_cents: number;
  monthly_upkeep_cents: number;
  demolition_refund_pct: number;
  construction_ticks: number;
  budget_envelope_id: number | null;
  cost_catalog_row_id: string | null;
}

export function mapRowToEconomy(row: {
  asset_id: string | number | bigint;
  base_cost_cents: string | number | bigint;
  monthly_upkeep_cents: string | number | bigint;
  demolition_refund_pct: number;
  construction_ticks: number;
  budget_envelope_id: number | null;
  cost_catalog_row_id: string | number | bigint | null;
}): CatalogEconomyRowJson {
  return {
    asset_id: idString(row.asset_id),
    base_cost_cents: Number(row.base_cost_cents),
    monthly_upkeep_cents: Number(row.monthly_upkeep_cents),
    demolition_refund_pct: row.demolition_refund_pct,
    construction_ticks: row.construction_ticks,
    budget_envelope_id: row.budget_envelope_id,
    cost_catalog_row_id: row.cost_catalog_row_id == null ? null : idString(row.cost_catalog_row_id),
  };
}

export interface CatalogAssetSpriteRowJson {
  asset_id: string;
  sprite_id: string;
  slot: string;
}

export function mapRowToAssetSprite(row: {
  asset_id: string | number | bigint;
  sprite_id: string | number | bigint;
  slot: string;
}): CatalogAssetSpriteRowJson {
  return {
    asset_id: idString(row.asset_id),
    sprite_id: idString(row.sprite_id),
    slot: row.slot,
  };
}

export interface CatalogSpriteRowJson {
  id: string;
  path: string;
  ppu: number;
  pivot_x: number;
  pivot_y: number;
  provenance: string;
  generator_archetype_id: string | null;
  generator_build_fingerprint: string | null;
  art_revision: number;
}

export function mapRowToSprite(row: {
  id: string | number | bigint;
  path: string;
  ppu: number;
  pivot_x: number;
  pivot_y: number;
  provenance: string;
  generator_archetype_id: string | null;
  generator_build_fingerprint: string | null;
  art_revision: number;
}): CatalogSpriteRowJson {
  return {
    id: idString(row.id),
    path: row.path,
    ppu: row.ppu,
    pivot_x: row.pivot_x,
    pivot_y: row.pivot_y,
    provenance: row.provenance,
    generator_archetype_id: row.generator_archetype_id,
    generator_build_fingerprint: row.generator_build_fingerprint,
    art_revision: row.art_revision,
  };
}

export interface CatalogAssetByIdResultJson {
  asset: CatalogAssetRowJson;
  economy: CatalogEconomyRowJson | null;
  sprite_slots: Array<{
    binding: CatalogAssetSpriteRowJson;
    sprite: CatalogSpriteRowJson;
  }>;
}

/**
 * Load asset + economy + sprite slots (same queries as `web/lib/catalog/fetch-asset-composite.ts`).
 */
export async function loadCatalogAssetComposite(
  client: PoolClient,
  idParam: string,
): Promise<CatalogAssetByIdResultJson | "notfound" | "badid"> {
  if (!/^\d{1,32}$/.test(idParam)) return "badid";
  const idNum = Number(idParam);
  if (!Number.isSafeInteger(idNum) || idNum < 1) return "badid";

  const ar = await client.query(`select * from catalog_asset where id = $1 limit 1`, [idNum]);
  if (ar.rows.length === 0) return "notfound";
  const asset = mapRowToCatalogAsset(ar.rows[0] as never);
  const ecoRows = await client.query(`select * from catalog_economy where asset_id = $1 limit 1`, [
    idNum,
  ]);
  const economy = ecoRows.rows[0] ? mapRowToEconomy(ecoRows.rows[0] as never) : null;
  const slotSql = await client.query(
    `select
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
    where cas.asset_id = $1`,
    [idNum],
  );
  const sprite_slots = (
    slotSql.rows as Array<{
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
  ).map((r) => ({
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
  }));
  return { asset, economy, sprite_slots };
}
