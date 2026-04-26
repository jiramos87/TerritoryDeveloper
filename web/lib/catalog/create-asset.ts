import { getSql } from "@/lib/db/client";
import type { Sql } from "postgres";
import type { CatalogCreateAssetBody } from "@/types/api/catalog-api";
import { loadCatalogAssetById } from "@/lib/catalog/fetch-asset-composite";
import { type CatalogSpriteSlot, type CatalogAssetStatus } from "@/types/api/catalog-enums";

const SLOTS: ReadonlySet<string> = new Set<string>([
  "world",
  "button_target",
  "button_pressed",
  "button_disabled",
  "button_hover",
]);

function isNonEmptyString(x: unknown): x is string {
  return typeof x === "string" && x.trim().length > 0;
}

/**
 * Validates create body; returns error message or null.
 */
export function validateCreateBody(body: unknown): string | null {
  if (body == null || typeof body !== "object") return "Body must be a JSON object";
  const b = body as Record<string, unknown>;
  if (!isNonEmptyString(b.category)) return "category required";
  if (!isNonEmptyString(b.slug)) return "slug required";
  if (!isNonEmptyString(b.display_name)) return "display_name required";
  const st = b.status;
  if (st !== "draft" && st !== "published" && st !== "retired") {
    return "status must be draft, published, or retired";
  }
  const eco = b.economy;
  if (eco == null || typeof eco !== "object") return "economy required";
  const e = eco as Record<string, unknown>;
  if (typeof e.base_cost_cents !== "number" || !Number.isFinite(e.base_cost_cents)) {
    return "economy.base_cost_cents required";
  }
  if (typeof e.monthly_upkeep_cents !== "number" || !Number.isFinite(e.monthly_upkeep_cents)) {
    return "economy.monthly_upkeep_cents required";
  }
  const binds = b.sprite_binds;
  if (!Array.isArray(binds)) return "sprite_binds must be an array";
  for (const x of binds) {
    if (x == null || typeof x !== "object") return "each sprite_bind must be an object";
    const o = x as Record<string, unknown>;
    const slot = o.slot;
    const sid = o.sprite_id;
    if (typeof slot !== "string" || !SLOTS.has(slot)) return "sprite_bind.slot invalid";
    if (typeof sid !== "string" || !/^\d+$/.test(sid)) {
      return "sprite_bind.sprite_id must be a numeric id string";
    }
  }
  return null;
}

export async function createCatalogAssetTransaction(
  body: CatalogCreateAssetBody,
  externalTx?: Sql,
): Promise<string> {
  const valid = validateCreateBody(body);
  if (valid) throw new Error(`validation: ${valid}`);
  const sql = externalTx ?? getSql();
  const fpw = body.footprint_w ?? 1;
  const fph = body.footprint_h ?? 1;
  const hasButton = body.has_button ?? true;
  const rep = body.replaced_by == null || body.replaced_by === "" ? null : body.replaced_by;
  const replNum: number | null = rep == null ? null : Number.parseInt(rep, 10);
  if (rep != null && (replNum == null || Number.isNaN(replNum))) {
    throw new Error("validation: replaced_by must be a numeric id");
  }
  const eco = body.economy;
  const dfp = eco.demolition_refund_pct ?? 0;
  const ctk = eco.construction_ticks ?? 0;
  const budgetE = eco.budget_envelope_id == null ? null : eco.budget_envelope_id;
  const costC = eco.cost_catalog_row_id;
  const costCNum: number | null =
    costC == null || costC === "" ? null : Number.parseInt(String(costC), 10);
  if (costC != null && costC !== "" && (costCNum == null || Number.isNaN(costCNum))) {
    throw new Error("validation: cost_catalog_row_id must be a numeric id");
  }

  const runInserts = async (tx: Sql): Promise<string> => {
    const [row] = await tx`
      insert into catalog_asset (
        category, slug, display_name, status, replaced_by,
        footprint_w, footprint_h, placement_mode, unlocks_after, has_button, updated_at
      )
      values (
        ${body.category},
        ${body.slug},
        ${body.display_name},
        ${body.status as CatalogAssetStatus},
        ${replNum},
        ${fpw},
        ${fph},
        ${body.placement_mode ?? null},
        ${body.unlocks_after ?? null},
        ${hasButton},
        now()
      )
      returning id
    `;
    const rawId = (row as { id: bigint | number }).id;
    const assetIdStr = String(rawId);
    const aNum = typeof rawId === "bigint" ? Number(rawId) : (rawId as number);
    await tx`
      insert into catalog_economy (
        asset_id, base_cost_cents, monthly_upkeep_cents, demolition_refund_pct,
        construction_ticks, budget_envelope_id, cost_catalog_row_id
      )
      values (
        ${aNum},
        ${Math.trunc(eco.base_cost_cents)},
        ${Math.trunc(eco.monthly_upkeep_cents)},
        ${dfp},
        ${ctk},
        ${budgetE},
        ${costCNum}
      )
    `;
    for (const bind of body.sprite_binds) {
      const sid = Number.parseInt(bind.sprite_id, 10);
      await tx`
        insert into catalog_asset_sprite (asset_id, sprite_id, slot)
        values (${aNum}, ${sid}, ${bind.slot as CatalogSpriteSlot})
      `;
    }
    return assetIdStr;
  };

  // If caller already owns a tx, run inline (no nested begin). Otherwise wrap.
  if (externalTx) {
    return await runInserts(externalTx);
  }
  return await (sql as Sql).begin(async (tx) => runInserts(tx as unknown as Sql)) as unknown as string;
}

export async function getCreatedResponse(id: string, externalTx?: Sql) {
  // TECH-1351: when wrapped by `withAudit`, read-after-create must run inside
  // the same tx so the just-inserted row is visible before commit.
  return loadCatalogAssetById(id, externalTx ? { tx: externalTx } : {});
}
