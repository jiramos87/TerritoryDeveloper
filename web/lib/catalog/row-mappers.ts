import type { CatalogAssetRow } from "@/types/api/catalog-asset";
import type { CatalogAssetStatus } from "@/types/api/catalog-enums";
import type { CatalogEconomyRow } from "@/types/api/catalog-economy";
import type { CatalogAssetSpriteRow } from "@/types/api/catalog-asset-sprite";
import type { CatalogSpriteRow } from "@/types/api/catalog-sprite";

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
}): CatalogAssetRow {
  return {
    id: idString(row.id),
    category: row.category,
    slug: row.slug,
    display_name: row.display_name,
    status: row.status as CatalogAssetStatus,
    replaced_by: row.replaced_by == null ? null : idString(row.replaced_by),
    footprint_w: row.footprint_w,
    footprint_h: row.footprint_h,
    placement_mode: row.placement_mode,
    unlocks_after: row.unlocks_after,
    has_button: row.has_button,
    updated_at: tsString(row.updated_at),
  };
}

export function mapRowToEconomy(row: {
  asset_id: string | number | bigint;
  base_cost_cents: string | number | bigint;
  monthly_upkeep_cents: string | number | bigint;
  demolition_refund_pct: number;
  construction_ticks: number;
  budget_envelope_id: number | null;
  cost_catalog_row_id: string | number | bigint | null;
}): CatalogEconomyRow {
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

export function mapRowToAssetSprite(
  row: {
    asset_id: string | number | bigint;
    sprite_id: string | number | bigint;
    slot: string;
  },
): CatalogAssetSpriteRow {
  return {
    asset_id: idString(row.asset_id),
    sprite_id: idString(row.sprite_id),
    slot: row.slot as CatalogAssetSpriteRow["slot"],
  };
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
}): CatalogSpriteRow {
  return {
    id: idString(row.id),
    path: row.path,
    ppu: row.ppu,
    pivot_x: row.pivot_x,
    pivot_y: row.pivot_y,
    provenance: row.provenance as CatalogSpriteRow["provenance"],
    generator_archetype_id: row.generator_archetype_id,
    generator_build_fingerprint: row.generator_build_fingerprint,
    art_revision: row.art_revision,
  };
}
