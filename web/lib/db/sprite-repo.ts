/**
 * Sprite repo — typed query helpers for catalog_entity (kind=sprite) +
 * entity_version + sprite_detail. Backs `web/app/api/catalog/sprites/*` (TECH-1675).
 *
 * Conventions:
 *   - All multi-row writes (create / promote) accept a tx via `Sql` parameter
 *     so callers wrap them in `withAudit` for atomic audit-log emission.
 *   - Slug regex enforced at DB layer (catalog_entity CHECK ck_catalog_entity_slug_format).
 *   - Frozen-version edits rejected here (returns 'frozen_version' marker);
 *     route layer translates to 409 + `frozen_version` error code.
 *
 * @see ia/projects/asset-pipeline/stage-6.1.md — TECH-1675 §Plan Digest
 */
import type { Sql } from "postgres";
import { getSql } from "@/lib/db/client";

export type SpriteListFilter = "active" | "retired" | "all";

export type SpriteListItem = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  current_published_version_id: string | null;
  active_version_id: string | null;
  active_version_status: "draft" | "published" | null;
  assets_path: string | null;
  pixels_per_unit: number | null;
  pivot_x: number | null;
  pivot_y: number | null;
  provenance: "hand" | "generator" | null;
  updated_at: string;
};

export type SpriteDetailDto = SpriteListItem & {
  source_uri: string | null;
  source_run_id: string | null;
  source_variant_idx: number | null;
  active_version_params: Record<string, unknown> | null;
  active_version_manual_pin: boolean;
};

export type CreateSpriteBody = {
  slug: string;
  display_name: string;
  tags?: string[];
  sprite_detail?: {
    source_uri?: string;
    pixels_per_unit?: number;
    pivot_x?: number;
    pivot_y?: number;
    provenance?: "hand" | "generator";
  };
  source_run_id?: string;
  source_variant_idx?: number;
  params_json?: Record<string, unknown>;
};

export type PatchSpriteBody = {
  display_name?: string;
  tags?: string[];
  params_json?: Record<string, unknown>;
  sprite_detail?: {
    pixels_per_unit?: number;
    pivot_x?: number;
    pivot_y?: number;
  };
};

export type RepoResult<T> =
  | { ok: "ok"; data: T }
  | { ok: "notfound" }
  | { ok: "conflict"; reason: "duplicate_slug" | "frozen_version" | "published_version" }
  | { ok: "validation"; reason: string };

const SLUG_RE = /^[a-z][a-z0-9_]{2,63}$/;

function validateCreate(body: CreateSpriteBody): string | null {
  if (typeof body.slug !== "string" || !SLUG_RE.test(body.slug)) {
    return "slug must match ^[a-z][a-z0-9_]{2,63}$";
  }
  if (typeof body.display_name !== "string" || body.display_name.length === 0) {
    return "display_name required";
  }
  if (body.tags && !Array.isArray(body.tags)) return "tags must be array";
  const detail = body.sprite_detail;
  if (detail) {
    if (detail.pixels_per_unit !== undefined && (!Number.isInteger(detail.pixels_per_unit) || detail.pixels_per_unit <= 0)) {
      return "sprite_detail.pixels_per_unit must be positive integer";
    }
    if (detail.pivot_x !== undefined && (typeof detail.pivot_x !== "number" || detail.pivot_x < 0 || detail.pivot_x > 1)) {
      return "sprite_detail.pivot_x must be in [0,1]";
    }
    if (detail.pivot_y !== undefined && (typeof detail.pivot_y !== "number" || detail.pivot_y < 0 || detail.pivot_y > 1)) {
      return "sprite_detail.pivot_y must be in [0,1]";
    }
    if (detail.provenance !== undefined && detail.provenance !== "hand" && detail.provenance !== "generator") {
      return "sprite_detail.provenance must be hand|generator";
    }
  }
  return null;
}

/**
 * List sprite entities. Joins catalog_entity + active entity_version + sprite_detail.
 * Active version: prefer current_published_version_id; else most recent draft.
 */
export async function listSprites(opts: {
  filter: SpriteListFilter;
  limit: number;
  cursor: string | null;
}): Promise<{ items: SpriteListItem[]; next_cursor: string | null }> {
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
      coalesce(
        e.current_published_version_id,
        (select v.id from entity_version v where v.entity_id = e.id order by v.version_number desc limit 1)
      )::text as active_version_id,
      (
        select v.status from entity_version v
         where v.id = coalesce(
           e.current_published_version_id,
           (select v2.id from entity_version v2 where v2.entity_id = e.id order by v2.version_number desc limit 1)
         )
      ) as active_version_status,
      d.assets_path,
      d.pixels_per_unit,
      d.pivot_x,
      d.pivot_y,
      d.provenance
    from catalog_entity e
    left join sprite_detail d on d.entity_id = e.id
    where e.kind = 'sprite' ${retiredCond} ${cursorCond}
    order by e.id asc
    limit ${limit}
  `;
  const items = rows as unknown as SpriteListItem[];
  const next_cursor = items.length === limit ? items[items.length - 1]!.entity_id : null;
  return { items, next_cursor };
}

/**
 * Create entity + draft version + sprite_detail in one tx. Caller passes the
 * tx via `sql` parameter (typically from withAudit). Returns the new
 * entity_id as string.
 */
export async function createSprite(
  body: CreateSpriteBody,
  sql: Sql,
): Promise<RepoResult<{ entity_id: string; slug: string }>> {
  const v = validateCreate(body);
  if (v) return { ok: "validation", reason: v };

  // Duplicate slug check (NB: also enforced by uq_catalog_entity_kind_slug — we
  // pre-check to surface a clean 409 before the FK to entity_version chains).
  const dup = (await sql`
    select 1 as ok from catalog_entity where kind = 'sprite' and slug = ${body.slug} limit 1
  `) as unknown as Array<{ ok: number }>;
  if (dup.length > 0) return { ok: "conflict", reason: "duplicate_slug" };

  const tags = body.tags ?? [];
  const insertedEntity = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('sprite', ${body.slug}, ${body.display_name}, ${tags})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const entity_id = insertedEntity[0]!.id;

  await sql`
    insert into entity_version (entity_id, version_number, status, params_json,
                                source_run_id, source_variant_idx)
    values (
      ${entity_id}::bigint,
      1,
      'draft',
      ${sql.json((body.params_json ?? {}) as never)}::jsonb,
      ${body.source_run_id ?? null},
      ${body.source_variant_idx ?? null}
    )
  `;

  const detail = body.sprite_detail ?? {};
  await sql`
    insert into sprite_detail (entity_id, source_uri, pixels_per_unit, pivot_x, pivot_y,
                               provenance, source_run_id, source_variant_idx)
    values (
      ${entity_id}::bigint,
      ${detail.source_uri ?? null},
      ${detail.pixels_per_unit ?? 100},
      ${detail.pivot_x ?? 0.5},
      ${detail.pivot_y ?? 0.5},
      ${detail.provenance ?? "hand"},
      ${body.source_run_id ?? null},
      ${body.source_variant_idx ?? null}
    )
  `;

  return { ok: "ok", data: { entity_id, slug: body.slug } };
}

async function loadActiveVersion(
  sql: Sql,
  entity_id: string,
): Promise<{ id: string; status: "draft" | "published"; params_json: Record<string, unknown>; manual_pin: boolean } | null> {
  const rows = (await sql`
    select v.id::text as id, v.status, v.params_json, v.manual_pin
      from entity_version v
     where v.entity_id = ${entity_id}::bigint
     order by v.version_number desc
     limit 1
  `) as unknown as Array<{ id: string; status: "draft" | "published"; params_json: Record<string, unknown>; manual_pin: boolean }>;
  return rows[0] ?? null;
}

export async function getSpriteBySlug(slug: string): Promise<SpriteDetailDto | null> {
  const sql = getSql();
  const rows = (await sql`
    select
      e.id::text as entity_id,
      e.slug,
      e.display_name,
      e.tags,
      e.retired_at,
      e.current_published_version_id::text as current_published_version_id,
      e.updated_at,
      coalesce(
        e.current_published_version_id,
        (select v.id from entity_version v where v.entity_id = e.id order by v.version_number desc limit 1)
      )::text as active_version_id,
      (
        select v.status from entity_version v
         where v.id = coalesce(
           e.current_published_version_id,
           (select v2.id from entity_version v2 where v2.entity_id = e.id order by v2.version_number desc limit 1)
         )
      ) as active_version_status,
      (
        select v.params_json from entity_version v
         where v.id = coalesce(
           e.current_published_version_id,
           (select v2.id from entity_version v2 where v2.entity_id = e.id order by v2.version_number desc limit 1)
         )
      ) as active_version_params,
      coalesce((
        select v.manual_pin from entity_version v
         where v.id = coalesce(
           e.current_published_version_id,
           (select v2.id from entity_version v2 where v2.entity_id = e.id order by v2.version_number desc limit 1)
         )
      ), false) as active_version_manual_pin,
      d.source_uri,
      d.assets_path,
      d.pixels_per_unit,
      d.pivot_x,
      d.pivot_y,
      d.provenance,
      d.source_run_id::text as source_run_id,
      d.source_variant_idx
    from catalog_entity e
    left join sprite_detail d on d.entity_id = e.id
    where e.kind = 'sprite' and e.slug = ${slug}
    limit 1
  `) as unknown as SpriteDetailDto[];
  return rows[0] ?? null;
}

/**
 * PATCH a draft version. Rejects edits against frozen (published) versions
 * with `frozen_version`. Caller passes tx via sql param.
 */
export async function patchSprite(
  slug: string,
  body: PatchSpriteBody,
  sql: Sql,
): Promise<RepoResult<{ entity_id: string }>> {
  const ent = (await sql`
    select id::text as id from catalog_entity
     where kind = 'sprite' and slug = ${slug}
     limit 1
  `) as unknown as Array<{ id: string }>;
  if (ent.length === 0) return { ok: "notfound" };
  const entity_id = ent[0]!.id;

  const active = await loadActiveVersion(sql, entity_id);
  if (active && active.status === "published") {
    return { ok: "conflict", reason: "frozen_version" };
  }

  // Entity-level edits.
  if (body.display_name !== undefined) {
    await sql`
      update catalog_entity set display_name = ${body.display_name}
       where id = ${entity_id}::bigint
    `;
  }
  if (body.tags !== undefined) {
    await sql`
      update catalog_entity set tags = ${body.tags}
       where id = ${entity_id}::bigint
    `;
  }

  // Draft version params edit.
  if (body.params_json !== undefined && active) {
    await sql`
      update entity_version set params_json = ${sql.json(body.params_json as never)}::jsonb
       where id = ${active.id}::bigint
    `;
  }

  // Sprite detail edits.
  const sd = body.sprite_detail;
  if (sd) {
    if (sd.pixels_per_unit !== undefined) {
      if (!Number.isInteger(sd.pixels_per_unit) || sd.pixels_per_unit <= 0) {
        return { ok: "validation", reason: "sprite_detail.pixels_per_unit must be positive integer" };
      }
      await sql`
        update sprite_detail set pixels_per_unit = ${sd.pixels_per_unit}
         where entity_id = ${entity_id}::bigint
      `;
    }
    if (sd.pivot_x !== undefined) {
      if (sd.pivot_x < 0 || sd.pivot_x > 1) return { ok: "validation", reason: "pivot_x out of range" };
      await sql`
        update sprite_detail set pivot_x = ${sd.pivot_x}
         where entity_id = ${entity_id}::bigint
      `;
    }
    if (sd.pivot_y !== undefined) {
      if (sd.pivot_y < 0 || sd.pivot_y > 1) return { ok: "validation", reason: "pivot_y out of range" };
      await sql`
        update sprite_detail set pivot_y = ${sd.pivot_y}
         where entity_id = ${entity_id}::bigint
      `;
    }
  }

  return { ok: "ok", data: { entity_id } };
}

export async function retireSprite(slug: string, sql: Sql): Promise<RepoResult<{ entity_id: string }>> {
  const rows = (await sql`
    update catalog_entity
       set retired_at = now()
     where kind = 'sprite' and slug = ${slug} and retired_at is null
     returning id::text as id
  `) as unknown as Array<{ id: string }>;
  if (rows.length === 0) {
    // Either notfound or already retired — disambiguate.
    const ex = (await sql`
      select 1 as ok from catalog_entity where kind='sprite' and slug=${slug}
    `) as unknown as Array<{ ok: number }>;
    return ex.length === 0 ? { ok: "notfound" } : { ok: "ok", data: { entity_id: "" } };
  }
  return { ok: "ok", data: { entity_id: rows[0]!.id } };
}

export async function restoreSprite(slug: string, sql: Sql): Promise<RepoResult<{ entity_id: string }>> {
  const rows = (await sql`
    update catalog_entity
       set retired_at = null
     where kind = 'sprite' and slug = ${slug} and retired_at is not null
     returning id::text as id
  `) as unknown as Array<{ id: string }>;
  if (rows.length === 0) {
    const ex = (await sql`
      select 1 as ok from catalog_entity where kind='sprite' and slug=${slug}
    `) as unknown as Array<{ ok: number }>;
    return ex.length === 0 ? { ok: "notfound" } : { ok: "ok", data: { entity_id: "" } };
  }
  return { ok: "ok", data: { entity_id: rows[0]!.id } };
}

export async function deleteDraftSprite(
  slug: string,
  sql: Sql,
): Promise<RepoResult<{ entity_id: string }>> {
  const ent = (await sql`
    select id::text as id from catalog_entity
     where kind = 'sprite' and slug = ${slug}
     limit 1
  `) as unknown as Array<{ id: string }>;
  if (ent.length === 0) return { ok: "notfound" };
  const entity_id = ent[0]!.id;

  // Block delete-draft if any version is published.
  const pub = (await sql`
    select 1 as ok from entity_version
     where entity_id = ${entity_id}::bigint and status = 'published'
     limit 1
  `) as unknown as Array<{ ok: number }>;
  if (pub.length > 0) return { ok: "conflict", reason: "published_version" };

  // CASCADE removes entity_version + sprite_detail rows.
  await sql`
    delete from catalog_entity where id = ${entity_id}::bigint
  `;
  return { ok: "ok", data: { entity_id } };
}

export type PromoteSpriteBody = {
  run_id: string;
  variant_idx: number;
};

/**
 * Write promote target columns onto sprite_detail. Caller is responsible for
 * the blob copy; this helper is the DB half of the atomic promote tx.
 */
export async function promoteSprite(
  slug: string,
  args: { assets_path: string; pixels_per_unit?: number; pivot_x?: number; pivot_y?: number; run_id: string; variant_idx: number },
  sql: Sql,
): Promise<RepoResult<{ entity_id: string; assets_path: string }>> {
  const ent = (await sql`
    select id::text as id from catalog_entity
     where kind = 'sprite' and slug = ${slug}
     limit 1
  `) as unknown as Array<{ id: string }>;
  if (ent.length === 0) return { ok: "notfound" };
  const entity_id = ent[0]!.id;

  await sql`
    update sprite_detail
       set assets_path = ${args.assets_path},
           pixels_per_unit = coalesce(${args.pixels_per_unit ?? null}, pixels_per_unit),
           pivot_x = coalesce(${args.pivot_x ?? null}, pivot_x),
           pivot_y = coalesce(${args.pivot_y ?? null}, pivot_y),
           source_run_id = ${args.run_id}::uuid,
           source_variant_idx = ${args.variant_idx},
           provenance = 'generator'
     where entity_id = ${entity_id}::bigint
  `;
  return { ok: "ok", data: { entity_id, assets_path: args.assets_path } };
}
