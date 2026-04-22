/**
 * Row shape for `catalog_economy` — `db/migrations/0011_catalog_core.sql`.
 * Money fields are **integer cents** (glossary) — `number` in DTOs for in-range values.
 */
export interface CatalogEconomyRow {
  asset_id: string;
  base_cost_cents: number;
  monthly_upkeep_cents: number;
  /** 0–100 inclusive */
  demolition_refund_pct: number;
  construction_ticks: number;
  budget_envelope_id: number | null;
  cost_catalog_row_id: string | null;
}
