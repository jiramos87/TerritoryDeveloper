import { getSql } from "@/lib/db/client";
import type { CatalogPatchAssetBody } from "@/types/api/catalog-api";
import { mapRowToCatalogAsset, mapRowToEconomy } from "@/lib/catalog/row-mappers";
import { loadCatalogAssetById } from "@/lib/catalog/fetch-asset-composite";

type PatchOk = {
  ok: true;
  composite: Exclude<Awaited<ReturnType<typeof loadCatalogAssetById>>, "notfound" | "badid">;
};
type PatchOutcome =
  | PatchOk
  | { ok: "notfound" }
  | { ok: "badid" }
  | {
      ok: "conflict";
      current: NonNullable<Exclude<Awaited<ReturnType<typeof loadCatalogAssetById>>, "notfound" | "badid">>;
    };

/**
 * @see `ia/projects/TECH-644.md` — `UPDATE` with `where id` + `updated_at` match; 0 rows → 409.
 */
export async function patchCatalogAsset(
  idParam: string,
  body: CatalogPatchAssetBody,
): Promise<PatchOutcome> {
  if (!/^\d{1,32}$/.test(idParam)) return { ok: "badid" };
  const idNum = Number(idParam);
  if (!Number.isSafeInteger(idNum) || idNum < 1) return { ok: "badid" };
  if (!body.updated_at || typeof body.updated_at !== "string") {
    return { ok: "badid" };
  }
  const { updated_at: _v, ...rest } = body;
  if (Object.keys(rest).length === 0) {
    return { ok: "badid" };
  }
  const sql = getSql();
  return await sql.begin(async (tx) => {
    const [cur] = await tx`select * from catalog_asset where id = ${idNum} limit 1`;
    if (!cur) return { ok: "notfound" } as const;
    const base = mapRowToCatalogAsset(cur as never);
    const next = {
      category: base.category,
      slug: base.slug,
      display_name: body.display_name !== undefined ? body.display_name : base.display_name,
      status: body.status !== undefined ? body.status : base.status,
      replaced_by: body.replaced_by !== undefined ? body.replaced_by : base.replaced_by,
      footprint_w: body.footprint_w !== undefined ? body.footprint_w : base.footprint_w,
      footprint_h: body.footprint_h !== undefined ? body.footprint_h : base.footprint_h,
      placement_mode: body.placement_mode !== undefined ? body.placement_mode : base.placement_mode,
      unlocks_after: body.unlocks_after !== undefined ? body.unlocks_after : base.unlocks_after,
      has_button: body.has_button !== undefined ? body.has_button : base.has_button,
    };
    const rep =
      next.replaced_by == null || next.replaced_by === ""
        ? null
        : Number.parseInt(String(next.replaced_by), 10);
    if (next.replaced_by != null && next.replaced_by !== "" && (rep == null || Number.isNaN(rep))) {
      return { ok: "badid" as const };
    }
    const [urow] = await tx`
      update catalog_asset set
        category = ${next.category},
        slug = ${next.slug},
        display_name = ${next.display_name},
        status = ${next.status},
        replaced_by = ${rep},
        footprint_w = ${next.footprint_w},
        footprint_h = ${next.footprint_h},
        placement_mode = ${next.placement_mode},
        unlocks_after = ${next.unlocks_after},
        has_button = ${next.has_button},
        updated_at = now()
      where id = ${idNum} and updated_at = ${body.updated_at}
      returning *
    `;
    if (!urow) {
      const fr = await loadCatalogAssetById(idParam);
      if (fr === "notfound" || fr === "badid") {
        return { ok: "notfound" as const };
      }
      return { ok: "conflict", current: fr };
    }
    if (body.economy && Object.keys(body.economy).length > 0) {
      const e = body.economy;
      const [erow] = await tx`select * from catalog_economy where asset_id = ${idNum} limit 1`;
      if (!erow) {
        throw new Error("economy row missing for asset");
      }
      const ex = { ...mapRowToEconomy(erow as never) };
      if (e.base_cost_cents !== undefined) ex.base_cost_cents = e.base_cost_cents;
      if (e.monthly_upkeep_cents !== undefined) ex.monthly_upkeep_cents = e.monthly_upkeep_cents;
      if (e.demolition_refund_pct !== undefined) {
        ex.demolition_refund_pct = e.demolition_refund_pct;
      }
      if (e.construction_ticks !== undefined) ex.construction_ticks = e.construction_ticks;
      if (e.budget_envelope_id !== undefined) {
        ex.budget_envelope_id = e.budget_envelope_id;
      }
      if (e.cost_catalog_row_id !== undefined) {
        ex.cost_catalog_row_id = e.cost_catalog_row_id;
      }
      const costC =
        ex.cost_catalog_row_id == null || ex.cost_catalog_row_id === ""
          ? null
          : Number.parseInt(String(ex.cost_catalog_row_id), 10);
      await tx`
        update catalog_economy set
          base_cost_cents = ${ex.base_cost_cents},
          monthly_upkeep_cents = ${ex.monthly_upkeep_cents},
          demolition_refund_pct = ${ex.demolition_refund_pct},
          construction_ticks = ${ex.construction_ticks},
          budget_envelope_id = ${ex.budget_envelope_id},
          cost_catalog_row_id = ${costC}
        where asset_id = ${idNum}
      `;
    }
    const composite = await loadCatalogAssetById(idParam);
    if (composite === "notfound" || composite === "badid") {
      return { ok: "notfound" as const };
    }
    return { ok: true, composite } satisfies PatchOk;
  });
}
