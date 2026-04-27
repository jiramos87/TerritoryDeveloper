/**
 * Pool repo (TECH-1788).
 *
 * Bridges `catalog_entity` (kind=pool) + `pool_detail` + `pool_member`. Single
 * tx member upsert/delete diff. Conditions JSON edited via predicate-vocab UI;
 * stored as `pool_member.conditions_json` (added by 0028 migration).
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1788 §Plan Digest
 */
import type { Sql } from "postgres";
import { getSql } from "@/lib/db/client";
import type {
  CatalogPoolDto,
  CatalogPoolPatchBody,
  CatalogPoolCreateBody,
  CatalogPoolMemberSpineRow,
} from "@/types/api/catalog-api";

export type PoolSpineListItem = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  owner_category: string | null;
  member_count: number;
  current_published_version_id: string | null;
  updated_at: string;
};

export type PoolSpineListFilter = "active" | "retired" | "all";

export type PoolRepoResult<T> =
  | { ok: "ok"; data: T }
  | { ok: "notfound" }
  | { ok: "conflict"; reason: string; current?: CatalogPoolDto }
  | { ok: "validation"; reason: string };

const SLUG_RE = /^[a-z][a-z0-9_]{2,63}$/;

const ALLOWED_PATCH_TOP = new Set(["updated_at", "display_name", "tags", "pool_detail", "members", "removed_member_entity_ids"]);
const ALLOWED_DETAIL_KEYS = new Set(["primary_subtype", "owner_category"]);

export async function listPoolsSpine(opts: {
  filter: PoolSpineListFilter;
  limit: number;
  cursor: string | null;
}): Promise<{ items: PoolSpineListItem[]; next_cursor: string | null }> {
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
  const rows = (await sql`
    select
      e.id::text as entity_id,
      e.slug,
      e.display_name,
      e.tags,
      e.retired_at,
      e.current_published_version_id::text as current_published_version_id,
      e.updated_at,
      d.owner_category,
      coalesce((select count(*) from pool_member pm where pm.pool_entity_id = e.id), 0)::int as member_count
    from catalog_entity e
    left join pool_detail d on d.entity_id = e.id
    where e.kind = 'pool' ${retiredCond} ${cursorCond}
    order by e.id asc
    limit ${limit}
  `) as unknown as PoolSpineListItem[];
  const next_cursor = rows.length === limit ? rows[rows.length - 1]!.entity_id : null;
  return { items: rows, next_cursor };
}

export async function getPoolSpineBySlug(
  slug: string,
  externalTx?: Sql,
): Promise<CatalogPoolDto | null> {
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
      d.primary_subtype,
      d.owner_category
    from catalog_entity e
    left join pool_detail d on d.entity_id = e.id
    where e.kind = 'pool' and e.slug = ${slug}
    limit 1
  `) as unknown as Array<Record<string, unknown>>;
  if (rows.length === 0) return null;
  const r = rows[0]!;
  const idNum = Number.parseInt(r.entity_id as string, 10);

  const memberRows = (await sql`
    select
      pm.asset_entity_id::text as asset_entity_id,
      e.slug,
      e.display_name,
      pm.weight,
      coalesce(pm.conditions_json, '{}'::jsonb) as conditions_json
    from pool_member pm
    join catalog_entity e on e.id = pm.asset_entity_id
    where pm.pool_entity_id = ${idNum}
    order by e.display_name asc
  `) as unknown as CatalogPoolMemberSpineRow[];

  const tagged = (await sql`
    select count(*)::int as n
    from asset_detail
    where primary_subtype_pool_id = ${idNum}
  `) as unknown as Array<{ n: number }>;

  return {
    entity_id: r.entity_id as string,
    slug: r.slug as string,
    display_name: r.display_name as string,
    tags: (r.tags as string[]) ?? [],
    retired_at: (r.retired_at as string | null) ?? null,
    current_published_version_id: (r.current_published_version_id as string | null) ?? null,
    updated_at: r.updated_at as string,
    pool_detail:
      r.primary_subtype != null || r.owner_category != null
        ? {
            primary_subtype: (r.primary_subtype as string | null) ?? null,
            owner_category: (r.owner_category as string | null) ?? null,
          }
        : null,
    members: memberRows,
    primary_tagged_by_count: tagged[0]?.n ?? 0,
  };
}

export async function createPoolSpine(
  body: CatalogPoolCreateBody,
  sql: Sql,
): Promise<PoolRepoResult<{ entity_id: string; slug: string }>> {
  if (!SLUG_RE.test(body.slug)) {
    return { ok: "validation", reason: "slug must match ^[a-z][a-z0-9_]{2,63}$" };
  }
  if (typeof body.display_name !== "string" || body.display_name.trim() === "") {
    return { ok: "validation", reason: "display_name required" };
  }
  const dup = (await sql`
    select 1 from catalog_entity where kind='pool' and slug=${body.slug} limit 1
  `) as unknown as Array<{ "?column?": number }>;
  if (dup.length > 0) return { ok: "conflict", reason: "duplicate_slug" };

  const tags = body.tags ?? [];
  const inserted = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('pool', ${body.slug}, ${body.display_name}, ${tags})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const entity_id = inserted[0]!.id;
  const idNum = Number.parseInt(entity_id, 10);

  const d = body.pool_detail ?? {};
  await sql`
    insert into pool_detail (entity_id, primary_subtype, owner_category)
    values (${idNum}, ${d.primary_subtype ?? null}, ${d.owner_category ?? null})
  `;
  return { ok: "ok", data: { entity_id, slug: body.slug } };
}

export async function patchPoolSpine(
  slug: string,
  body: CatalogPoolPatchBody,
  sql: Sql,
): Promise<PoolRepoResult<CatalogPoolDto>> {
  if (typeof body !== "object" || body == null) {
    return { ok: "validation", reason: "body must be object" };
  }
  for (const k of Object.keys(body)) {
    if (!ALLOWED_PATCH_TOP.has(k)) return { ok: "validation", reason: `unknown_fields: ${k}` };
  }
  if (typeof body.updated_at !== "string") {
    return { ok: "validation", reason: "updated_at required" };
  }

  // Lock + verify updated_at.
  const locked = (await sql`
    select id, updated_at
      from catalog_entity
     where kind = 'pool' and slug = ${slug}
     for update
  `) as unknown as Array<{ id: string; updated_at: string }>;
  if (locked.length === 0) return { ok: "notfound" };
  const cur = locked[0]!;
  if (new Date(cur.updated_at).toISOString() !== new Date(body.updated_at).toISOString()) {
    const current = await getPoolSpineBySlug(slug, sql);
    return { ok: "conflict", reason: "stale_updated_at", current: current ?? undefined };
  }
  const idNum = Number.parseInt(cur.id, 10);

  // catalog_entity top-level fields.
  if (body.display_name != null) {
    await sql`update catalog_entity set display_name = ${body.display_name} where id = ${idNum}`;
  }
  if (body.tags != null) {
    await sql`update catalog_entity set tags = ${body.tags} where id = ${idNum}`;
  }

  // pool_detail.
  if (body.pool_detail != null) {
    for (const k of Object.keys(body.pool_detail)) {
      if (!ALLOWED_DETAIL_KEYS.has(k)) {
        return { ok: "validation", reason: `unknown_fields: pool_detail.${k}` };
      }
    }
    const d = body.pool_detail;
    if ("primary_subtype" in d) {
      await sql`update pool_detail set primary_subtype = ${d.primary_subtype ?? null} where entity_id = ${idNum}`;
    }
    if ("owner_category" in d) {
      await sql`update pool_detail set owner_category = ${d.owner_category ?? null} where entity_id = ${idNum}`;
    }
  }

  // Member upserts.
  if (body.members != null) {
    for (const m of body.members) {
      if (typeof m.asset_entity_id !== "string" || !/^\d+$/.test(m.asset_entity_id)) {
        return { ok: "validation", reason: "members[].asset_entity_id must be numeric-string" };
      }
      if (!Number.isInteger(m.weight) || m.weight <= 0) {
        return { ok: "validation", reason: "members[].weight must be positive integer" };
      }
      const aid = Number.parseInt(m.asset_entity_id, 10);
      const cond = (m.conditions_json ?? {}) as unknown as Parameters<typeof sql.json>[0];
      await sql`
        insert into pool_member (pool_entity_id, asset_entity_id, weight, conditions_json)
        values (${idNum}, ${aid}, ${m.weight}, ${sql.json(cond)})
        on conflict (pool_entity_id, asset_entity_id)
        do update set weight = excluded.weight, conditions_json = excluded.conditions_json
      `;
    }
  }

  // Member deletes.
  if (body.removed_member_entity_ids != null && body.removed_member_entity_ids.length > 0) {
    const numericIds = body.removed_member_entity_ids
      .filter((s) => /^\d+$/.test(s))
      .map((s) => Number.parseInt(s, 10));
    if (numericIds.length > 0) {
      await sql`
        delete from pool_member
         where pool_entity_id = ${idNum}
           and asset_entity_id = any(${numericIds}::bigint[])
      `;
    }
  }

  // Touch updated_at + reload.
  await sql`update catalog_entity set updated_at = now() where id = ${idNum}`;
  const after = await getPoolSpineBySlug(slug, sql);
  if (after == null) return { ok: "notfound" };
  return { ok: "ok", data: after };
}
