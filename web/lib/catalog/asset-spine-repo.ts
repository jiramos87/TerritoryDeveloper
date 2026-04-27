/**
 * Spine-aware asset repo (TECH-1786 + TECH-1789).
 *
 * Bridges `catalog_entity` (kind=asset) + `asset_detail` + `economy_detail`
 * + `pool_member` + (TECH-1789) `asset_detail.primary_subtype_pool_id`.
 *
 * Distinct from the legacy `catalog_asset` table accessed by
 * `lib/catalog/fetch-asset-composite.ts` — Stage 7.1 surfaces the spine path
 * per the asset-pipeline architecture (DEC-A8 / DEC-A38).
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1786 §Plan Digest
 */
import type { Sql } from "postgres";
import { getSql } from "@/lib/db/client";
import type {
  CatalogAssetSpineDto,
  CatalogAssetSpinePatchBody,
  EntityRefSearchRow,
} from "@/types/api/catalog-api";

export type AssetSpineListItem = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  category: string | null;
  current_published_version_id: string | null;
  updated_at: string;
};

export type AssetSpineListFilter = "active" | "retired" | "all";

const SLUG_RE = /^[a-z][a-z0-9_]{2,63}$/;

const SLOT_COLUMNS = [
  "world_sprite_entity_id",
  "button_target_sprite_entity_id",
  "button_pressed_sprite_entity_id",
  "button_disabled_sprite_entity_id",
  "button_hover_sprite_entity_id",
] as const;

export type SpineRepoResult<T> =
  | { ok: "ok"; data: T }
  | { ok: "notfound" }
  | { ok: "conflict"; reason: string; current?: CatalogAssetSpineDto }
  | { ok: "validation"; reason: string };

export async function listAssetsSpine(opts: {
  filter: AssetSpineListFilter;
  limit: number;
  cursor: string | null;
}): Promise<{ items: AssetSpineListItem[]; next_cursor: string | null }> {
  const sql = getSql();
  const { filter, limit, cursor } = opts;
  const retiredCond =
    filter === "active"
      ? sql` and e.retired_at is null `
      : filter === "retired"
        ? sql` and e.retired_at is not null `
        : sql``;
  const cursorCond =
    cursor != null && cursor.length > 0
      ? sql` and e.id > ${Number.parseInt(cursor, 10)} `
      : sql``;
  const rows = await sql`
    select
      e.id::text as entity_id,
      e.slug,
      e.display_name,
      e.tags,
      e.retired_at,
      e.current_published_version_id::text as current_published_version_id,
      e.updated_at,
      d.category
    from catalog_entity e
    left join asset_detail d on d.entity_id = e.id
    where e.kind = 'asset' ${retiredCond} ${cursorCond}
    order by e.id asc
    limit ${limit}
  `;
  const items = rows as unknown as AssetSpineListItem[];
  const next_cursor = items.length === limit ? items[items.length - 1]!.entity_id : null;
  return { items, next_cursor };
}

async function loadResolutionRows(
  sql: Sql,
  ids: string[],
): Promise<Map<string, EntityRefSearchRow>> {
  const out = new Map<string, EntityRefSearchRow>();
  if (ids.length === 0) return out;
  const numericIds = ids
    .filter((s) => /^\d+$/.test(s))
    .map((s) => Number.parseInt(s, 10));
  if (numericIds.length === 0) return out;
  const rows = (await sql`
    select
      id::text as entity_id,
      slug,
      display_name,
      kind,
      current_published_version_id::text as current_published_version_id,
      retired_at
    from catalog_entity
    where id = any(${numericIds}::bigint[])
  `) as unknown as EntityRefSearchRow[];
  for (const r of rows) out.set(r.entity_id, r);
  return out;
}

export async function getAssetSpineBySlug(
  slug: string,
  externalTx?: Sql,
): Promise<CatalogAssetSpineDto | null> {
  const sql = externalTx ?? getSql();
  const rows = (await sql`
    select
      e.id::text as entity_id,
      e.slug,
      e.display_name,
      e.tags,
      e.retired_at,
      e.current_published_version_id::text as current_published_version_id,
      e.updated_at,
      d.category,
      d.footprint_w,
      d.footprint_h,
      d.placement_mode,
      d.unlocks_after,
      d.has_button,
      d.world_sprite_entity_id::text as world_sprite_entity_id,
      d.button_target_sprite_entity_id::text as button_target_sprite_entity_id,
      d.button_pressed_sprite_entity_id::text as button_pressed_sprite_entity_id,
      d.button_disabled_sprite_entity_id::text as button_disabled_sprite_entity_id,
      d.button_hover_sprite_entity_id::text as button_hover_sprite_entity_id,
      d.primary_subtype_pool_id::text as primary_subtype_pool_id,
      ec.base_cost_cents,
      ec.monthly_upkeep_cents,
      ec.demolition_refund_pct,
      ec.construction_ticks
    from catalog_entity e
    left join asset_detail d on d.entity_id = e.id
    left join economy_detail ec on ec.entity_id = e.id
    where e.kind = 'asset' and e.slug = ${slug}
    limit 1
  `) as unknown as Array<Record<string, unknown>>;
  if (rows.length === 0) return null;
  const r = rows[0] as Record<string, unknown>;

  const slotIds: string[] = [];
  for (const col of SLOT_COLUMNS) {
    const v = r[col];
    if (typeof v === "string") slotIds.push(v);
  }
  const resMap = await loadResolutionRows(sql as Sql, slotIds);
  const sprite_slot_resolutions: Record<string, EntityRefSearchRow | null> = {};
  for (const col of SLOT_COLUMNS) {
    const v = r[col];
    sprite_slot_resolutions[col] = typeof v === "string" ? resMap.get(v) ?? null : null;
  }

  const memberRows = (await sql`
    select e.id::text as entity_id, e.slug, e.display_name, e.kind,
           e.current_published_version_id::text as current_published_version_id,
           e.retired_at
      from pool_member pm
      join catalog_entity e on e.id = pm.pool_entity_id
     where pm.asset_entity_id = ${Number.parseInt(r.entity_id as string, 10)}
     order by e.display_name asc
  `) as unknown as EntityRefSearchRow[];

  const dto: CatalogAssetSpineDto = {
    entity_id: r.entity_id as string,
    slug: r.slug as string,
    display_name: r.display_name as string,
    tags: (r.tags as string[]) ?? [],
    retired_at: (r.retired_at as string | null) ?? null,
    current_published_version_id: (r.current_published_version_id as string | null) ?? null,
    updated_at: r.updated_at as string,
    asset_detail:
      r.category != null
        ? {
            category: r.category as string,
            footprint_w: r.footprint_w as number,
            footprint_h: r.footprint_h as number,
            placement_mode: (r.placement_mode as string | null) ?? null,
            unlocks_after: (r.unlocks_after as string | null) ?? null,
            has_button: (r.has_button as boolean) ?? true,
            world_sprite_entity_id: (r.world_sprite_entity_id as string | null) ?? null,
            button_target_sprite_entity_id: (r.button_target_sprite_entity_id as string | null) ?? null,
            button_pressed_sprite_entity_id: (r.button_pressed_sprite_entity_id as string | null) ?? null,
            button_disabled_sprite_entity_id: (r.button_disabled_sprite_entity_id as string | null) ?? null,
            button_hover_sprite_entity_id: (r.button_hover_sprite_entity_id as string | null) ?? null,
            primary_subtype_pool_id: (r.primary_subtype_pool_id as string | null) ?? null,
          }
        : null,
    economy_detail:
      r.base_cost_cents != null
        ? {
            base_cost_cents: Number(r.base_cost_cents),
            monthly_upkeep_cents: Number(r.monthly_upkeep_cents),
            demolition_refund_pct: r.demolition_refund_pct as number,
            construction_ticks: r.construction_ticks as number,
          }
        : null,
    sprite_slot_resolutions,
    subtype_memberships: memberRows,
  };
  return dto;
}

export type CreateAssetSpineBody = {
  slug: string;
  display_name: string;
  category: string;
  tags?: string[];
  asset_detail?: Partial<{
    footprint_w: number;
    footprint_h: number;
    placement_mode: string | null;
    unlocks_after: string | null;
    has_button: boolean;
    world_sprite_entity_id: string | null;
    button_target_sprite_entity_id: string | null;
    button_pressed_sprite_entity_id: string | null;
    button_disabled_sprite_entity_id: string | null;
    button_hover_sprite_entity_id: string | null;
  }>;
  economy_detail?: Partial<{
    base_cost_cents: number;
    monthly_upkeep_cents: number;
    demolition_refund_pct: number;
    construction_ticks: number;
  }>;
};

export async function createAssetSpine(
  body: CreateAssetSpineBody,
  sql: Sql,
): Promise<SpineRepoResult<{ entity_id: string; slug: string }>> {
  if (!SLUG_RE.test(body.slug)) {
    return { ok: "validation", reason: "slug must match ^[a-z][a-z0-9_]{2,63}$" };
  }
  if (typeof body.display_name !== "string" || body.display_name.trim() === "") {
    return { ok: "validation", reason: "display_name required" };
  }
  if (typeof body.category !== "string" || body.category.trim() === "") {
    return { ok: "validation", reason: "category required" };
  }
  const dup = (await sql`
    select 1 from catalog_entity where kind='asset' and slug=${body.slug} limit 1
  `) as unknown as Array<{ "?column?": number }>;
  if (dup.length > 0) return { ok: "conflict", reason: "duplicate_slug" };

  const tags = body.tags ?? [];
  const inserted = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('asset', ${body.slug}, ${body.display_name}, ${tags})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const entity_id = inserted[0]!.id;
  const idNum = Number.parseInt(entity_id, 10);

  const d = body.asset_detail ?? {};
  await sql`
    insert into asset_detail (
      entity_id, category, footprint_w, footprint_h, placement_mode, unlocks_after, has_button,
      world_sprite_entity_id, button_target_sprite_entity_id, button_pressed_sprite_entity_id,
      button_disabled_sprite_entity_id, button_hover_sprite_entity_id
    ) values (
      ${idNum}, ${body.category},
      ${d.footprint_w ?? 1}, ${d.footprint_h ?? 1},
      ${d.placement_mode ?? null}, ${d.unlocks_after ?? null}, ${d.has_button ?? true},
      ${d.world_sprite_entity_id ? Number.parseInt(d.world_sprite_entity_id, 10) : null},
      ${d.button_target_sprite_entity_id ? Number.parseInt(d.button_target_sprite_entity_id, 10) : null},
      ${d.button_pressed_sprite_entity_id ? Number.parseInt(d.button_pressed_sprite_entity_id, 10) : null},
      ${d.button_disabled_sprite_entity_id ? Number.parseInt(d.button_disabled_sprite_entity_id, 10) : null},
      ${d.button_hover_sprite_entity_id ? Number.parseInt(d.button_hover_sprite_entity_id, 10) : null}
    )
  `;
  const eco = body.economy_detail ?? {};
  await sql`
    insert into economy_detail (entity_id, base_cost_cents, monthly_upkeep_cents,
                                demolition_refund_pct, construction_ticks)
    values (
      ${idNum},
      ${Math.trunc(eco.base_cost_cents ?? 0)},
      ${Math.trunc(eco.monthly_upkeep_cents ?? 0)},
      ${eco.demolition_refund_pct ?? 0},
      ${eco.construction_ticks ?? 0}
    )
  `;
  return { ok: "ok", data: { entity_id, slug: body.slug } };
}

const ALLOWED_PATCH_TOP = new Set([
  "updated_at",
  "display_name",
  "tags",
  "asset_detail",
  "economy_detail",
  "subtype_membership",
]);

const ALLOWED_DETAIL_KEYS = new Set([
  "category",
  "footprint_w",
  "footprint_h",
  "placement_mode",
  "unlocks_after",
  "has_button",
  "world_sprite_entity_id",
  "button_target_sprite_entity_id",
  "button_pressed_sprite_entity_id",
  "button_disabled_sprite_entity_id",
  "button_hover_sprite_entity_id",
  "primary_subtype_pool_id",
]);

const ALLOWED_ECON_KEYS = new Set([
  "base_cost_cents",
  "monthly_upkeep_cents",
  "demolition_refund_pct",
  "construction_ticks",
]);

function unknownFieldsOf(body: CatalogAssetSpinePatchBody): string[] {
  const unknown: string[] = [];
  for (const k of Object.keys(body)) if (!ALLOWED_PATCH_TOP.has(k)) unknown.push(k);
  if (body.asset_detail) {
    for (const k of Object.keys(body.asset_detail)) {
      if (!ALLOWED_DETAIL_KEYS.has(k)) unknown.push(`asset_detail.${k}`);
    }
  }
  if (body.economy_detail) {
    for (const k of Object.keys(body.economy_detail)) {
      if (!ALLOWED_ECON_KEYS.has(k)) unknown.push(`economy_detail.${k}`);
    }
  }
  return unknown;
}

/**
 * Apply a spine-aware asset PATCH. Enforces:
 *   - optimistic-lock via `updated_at`
 *   - unknown-fields rejection
 *   - primary_subtype_pool_id ∈ membership (DEC-A11) — auto-inserts pool_member
 *     row when primary set without pre-existing membership.
 */
export async function patchAssetSpine(
  slug: string,
  body: CatalogAssetSpinePatchBody,
  sql: Sql,
): Promise<SpineRepoResult<{ entity_id: string; composite: CatalogAssetSpineDto }>> {
  if (!body.updated_at || typeof body.updated_at !== "string") {
    return { ok: "validation", reason: "updated_at required" };
  }
  const unknown = unknownFieldsOf(body);
  if (unknown.length > 0) {
    return { ok: "validation", reason: `unknown fields: ${unknown.join(", ")}` };
  }
  // Locate entity + lock for optimistic check.
  const ent = (await sql`
    select id::text as id, updated_at from catalog_entity
     where kind='asset' and slug=${slug} for update
  `) as unknown as Array<{ id: string; updated_at: string }>;
  if (ent.length === 0) return { ok: "notfound" };
  const entity_id = ent[0]!.id;
  const idNum = Number.parseInt(entity_id, 10);
  if (ent[0]!.updated_at !== body.updated_at) {
    const cur = await getAssetSpineBySlug(slug, sql);
    return { ok: "conflict", reason: "stale_updated_at", current: cur ?? undefined };
  }

  if (body.display_name !== undefined) {
    await sql`update catalog_entity set display_name=${body.display_name} where id=${idNum}`;
  }
  if (body.tags !== undefined) {
    await sql`update catalog_entity set tags=${body.tags} where id=${idNum}`;
  }

  if (body.asset_detail) {
    const d = body.asset_detail;
    if (d.category !== undefined) {
      await sql`update asset_detail set category=${d.category} where entity_id=${idNum}`;
    }
    if (d.footprint_w !== undefined) {
      await sql`update asset_detail set footprint_w=${d.footprint_w} where entity_id=${idNum}`;
    }
    if (d.footprint_h !== undefined) {
      await sql`update asset_detail set footprint_h=${d.footprint_h} where entity_id=${idNum}`;
    }
    if (d.placement_mode !== undefined) {
      await sql`update asset_detail set placement_mode=${d.placement_mode} where entity_id=${idNum}`;
    }
    if (d.unlocks_after !== undefined) {
      await sql`update asset_detail set unlocks_after=${d.unlocks_after} where entity_id=${idNum}`;
    }
    if (d.has_button !== undefined) {
      await sql`update asset_detail set has_button=${d.has_button} where entity_id=${idNum}`;
    }
    for (const col of SLOT_COLUMNS) {
      const v = (d as Record<string, unknown>)[col];
      if (v !== undefined) {
        const num = v == null ? null : Number.parseInt(String(v), 10);
        await sql`update asset_detail set ${sql(col)}=${num} where entity_id=${idNum}`;
      }
    }
    // primary_subtype_pool_id (TECH-1789) — enforce primary∈membership; auto-insert when missing.
    if (d.primary_subtype_pool_id !== undefined) {
      const primaryRaw = d.primary_subtype_pool_id;
      const primaryNum = primaryRaw == null ? null : Number.parseInt(String(primaryRaw), 10);
      if (primaryNum != null) {
        const ex = (await sql`
          select 1 from pool_member where pool_entity_id=${primaryNum} and asset_entity_id=${idNum} limit 1
        `) as unknown as Array<{ "?column?": number }>;
        if (ex.length === 0) {
          await sql`
            insert into pool_member (pool_entity_id, asset_entity_id, weight, conditions_json)
            values (${primaryNum}, ${idNum}, 1, '{}'::jsonb)
            on conflict do nothing
          `;
        }
      }
      await sql`update asset_detail set primary_subtype_pool_id=${primaryNum} where entity_id=${idNum}`;
    }
  }

  if (body.economy_detail) {
    const e = body.economy_detail;
    if (e.base_cost_cents !== undefined) {
      await sql`update economy_detail set base_cost_cents=${Math.trunc(e.base_cost_cents)} where entity_id=${idNum}`;
    }
    if (e.monthly_upkeep_cents !== undefined) {
      await sql`update economy_detail set monthly_upkeep_cents=${Math.trunc(e.monthly_upkeep_cents)} where entity_id=${idNum}`;
    }
    if (e.demolition_refund_pct !== undefined) {
      await sql`update economy_detail set demolition_refund_pct=${e.demolition_refund_pct} where entity_id=${idNum}`;
    }
    if (e.construction_ticks !== undefined) {
      await sql`update economy_detail set construction_ticks=${e.construction_ticks} where entity_id=${idNum}`;
    }
  }

  // Subtype membership diff (TECH-1789).
  if (body.subtype_membership) {
    const { added, removed } = body.subtype_membership;
    for (const poolIdStr of added) {
      const poolNum = Number.parseInt(poolIdStr, 10);
      if (!Number.isFinite(poolNum)) continue;
      await sql`
        insert into pool_member (pool_entity_id, asset_entity_id, weight, conditions_json)
        values (${poolNum}, ${idNum}, 1, '{}'::jsonb)
        on conflict do nothing
      `;
    }
    for (const poolIdStr of removed) {
      const poolNum = Number.parseInt(poolIdStr, 10);
      if (!Number.isFinite(poolNum)) continue;
      // Block remove when pool is current primary.
      const primary = (await sql`
        select primary_subtype_pool_id::text as id from asset_detail where entity_id=${idNum}
      `) as unknown as Array<{ id: string | null }>;
      if (primary[0] && primary[0].id === String(poolNum)) {
        return { ok: "conflict", reason: "primary_not_in_membership" };
      }
      await sql`
        delete from pool_member where pool_entity_id=${poolNum} and asset_entity_id=${idNum}
      `;
    }
  }

  // Final post-update enforcement: primary∈membership (covers any path).
  const finalCheck = (await sql`
    select asd.primary_subtype_pool_id::text as primary_id,
           exists(
             select 1 from pool_member
              where pool_entity_id = asd.primary_subtype_pool_id
                and asset_entity_id = asd.entity_id
           ) as has_member
      from asset_detail asd where asd.entity_id=${idNum}
  `) as unknown as Array<{ primary_id: string | null; has_member: boolean }>;
  if (finalCheck[0] && finalCheck[0].primary_id != null && finalCheck[0].has_member !== true) {
    return { ok: "conflict", reason: "primary_not_in_membership" };
  }

  // Touch updated_at to bump the optimistic-lock token (trigger handles it on UPDATE of entity).
  await sql`update catalog_entity set updated_at = now() where id=${idNum}`;

  const composite = await getAssetSpineBySlug(slug, sql);
  if (!composite) return { ok: "notfound" };
  return { ok: "ok", data: { entity_id, composite } };
}
