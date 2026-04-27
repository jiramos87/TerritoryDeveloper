/**
 * Button repo (TECH-1885 / Stage 8.1).
 *
 * Bridges `catalog_entity` (kind=button) + `button_detail`. Single tx
 * create/update of the 6 sprite slots + 4 token slots + size_variant +
 * action_id + enable_predicate_json per DEC-A7. Optimistic concurrency via
 * `catalog_entity.updated_at` fingerprint per DEC-A38.
 *
 * @see ia/projects/asset-pipeline (DB) — TECH-1885 §Plan Digest
 */
import type { Sql } from "postgres";
import { getSql } from "@/lib/db/client";
import type {
  CatalogButtonCreateBody,
  CatalogButtonDto,
  CatalogButtonPatchBody,
  EntityRefSearchRow,
} from "@/types/api/catalog-api";
import { SIZE_VARIANTS } from "./button-enums";

export type ButtonSpineListItem = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  size_variant: string | null;
  action_id: string | null;
  current_published_version_id: string | null;
  updated_at: string;
};

export type ButtonSpineListFilter = "active" | "retired" | "all";

export type ButtonRepoResult<T> =
  | { ok: "ok"; data: T }
  | { ok: "notfound" }
  | { ok: "conflict"; reason: string; current?: CatalogButtonDto }
  | { ok: "validation"; reason: string };

const SLUG_RE = /^[a-z][a-z0-9_]{2,63}$/;

const SPRITE_SLOT_COLUMNS = [
  "sprite_idle_entity_id",
  "sprite_hover_entity_id",
  "sprite_pressed_entity_id",
  "sprite_disabled_entity_id",
  "sprite_icon_entity_id",
  "sprite_badge_entity_id",
] as const;

const TOKEN_SLOT_COLUMNS = [
  "token_palette_entity_id",
  "token_frame_style_entity_id",
  "token_font_entity_id",
  "token_illumination_entity_id",
] as const;

const ALLOWED_PATCH_TOP = new Set([
  "updated_at",
  "display_name",
  "tags",
  "button_detail",
]);

const ALLOWED_DETAIL_KEYS = new Set([
  ...SPRITE_SLOT_COLUMNS,
  ...TOKEN_SLOT_COLUMNS,
  "size_variant",
  "action_id",
  "enable_predicate_json",
]);

export async function listButtonsSpine(opts: {
  filter: ButtonSpineListFilter;
  limit: number;
  cursor: string | null;
}): Promise<{ items: ButtonSpineListItem[]; next_cursor: string | null }> {
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
      d.size_variant,
      d.action_id
    from catalog_entity e
    left join button_detail d on d.entity_id = e.id
    where e.kind = 'button' ${retiredCond} ${cursorCond}
    order by e.id asc
    limit ${limit}
  `) as unknown as ButtonSpineListItem[];
  const next_cursor = rows.length === limit ? rows[rows.length - 1]!.entity_id : null;
  return { items: rows, next_cursor };
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

export async function getButtonSpineBySlug(
  slug: string,
  externalTx?: Sql,
): Promise<CatalogButtonDto | null> {
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
      d.sprite_idle_entity_id::text     as sprite_idle_entity_id,
      d.sprite_hover_entity_id::text    as sprite_hover_entity_id,
      d.sprite_pressed_entity_id::text  as sprite_pressed_entity_id,
      d.sprite_disabled_entity_id::text as sprite_disabled_entity_id,
      d.sprite_icon_entity_id::text     as sprite_icon_entity_id,
      d.sprite_badge_entity_id::text    as sprite_badge_entity_id,
      d.token_palette_entity_id::text     as token_palette_entity_id,
      d.token_frame_style_entity_id::text as token_frame_style_entity_id,
      d.token_font_entity_id::text        as token_font_entity_id,
      d.token_illumination_entity_id::text as token_illumination_entity_id,
      d.size_variant,
      d.action_id,
      d.enable_predicate_json
    from catalog_entity e
    left join button_detail d on d.entity_id = e.id
    where e.kind = 'button' and e.slug = ${slug}
    limit 1
  `) as unknown as Array<Record<string, unknown>>;
  if (rows.length === 0) return null;
  const r = rows[0]!;

  const refIds: string[] = [];
  for (const col of [...SPRITE_SLOT_COLUMNS, ...TOKEN_SLOT_COLUMNS]) {
    const v = r[col];
    if (typeof v === "string") refIds.push(v);
  }
  const resMap = await loadResolutionRows(sql as Sql, refIds);
  const slot_resolutions: Record<string, EntityRefSearchRow | null> = {};
  for (const col of [...SPRITE_SLOT_COLUMNS, ...TOKEN_SLOT_COLUMNS]) {
    const v = r[col];
    slot_resolutions[col] = typeof v === "string" ? resMap.get(v) ?? null : null;
  }

  const hasDetail = r.size_variant != null;
  return {
    entity_id: r.entity_id as string,
    slug: r.slug as string,
    display_name: r.display_name as string,
    tags: (r.tags as string[]) ?? [],
    retired_at: (r.retired_at as string | null) ?? null,
    current_published_version_id: (r.current_published_version_id as string | null) ?? null,
    updated_at: r.updated_at as string,
    button_detail: hasDetail
      ? {
          sprite_idle_entity_id: (r.sprite_idle_entity_id as string | null) ?? null,
          sprite_hover_entity_id: (r.sprite_hover_entity_id as string | null) ?? null,
          sprite_pressed_entity_id: (r.sprite_pressed_entity_id as string | null) ?? null,
          sprite_disabled_entity_id: (r.sprite_disabled_entity_id as string | null) ?? null,
          sprite_icon_entity_id: (r.sprite_icon_entity_id as string | null) ?? null,
          sprite_badge_entity_id: (r.sprite_badge_entity_id as string | null) ?? null,
          token_palette_entity_id: (r.token_palette_entity_id as string | null) ?? null,
          token_frame_style_entity_id: (r.token_frame_style_entity_id as string | null) ?? null,
          token_font_entity_id: (r.token_font_entity_id as string | null) ?? null,
          token_illumination_entity_id: (r.token_illumination_entity_id as string | null) ?? null,
          size_variant: r.size_variant as string,
          action_id: (r.action_id as string) ?? "",
          enable_predicate_json: (r.enable_predicate_json as Record<string, unknown>) ?? {},
        }
      : null,
    slot_resolutions,
  };
}

function toIdNumOrNull(v: string | null | undefined): number | null {
  if (v == null) return null;
  const trimmed = String(v).trim();
  if (trimmed === "") return null;
  if (!/^\d+$/.test(trimmed)) return null;
  return Number.parseInt(trimmed, 10);
}

export async function createButtonSpine(
  body: CatalogButtonCreateBody,
  sql: Sql,
): Promise<ButtonRepoResult<{ entity_id: string; slug: string }>> {
  if (!SLUG_RE.test(body.slug)) {
    return { ok: "validation", reason: "slug must match ^[a-z][a-z0-9_]{2,63}$" };
  }
  if (typeof body.display_name !== "string" || body.display_name.trim() === "") {
    return { ok: "validation", reason: "display_name required" };
  }
  const sizeVariant = body.button_detail?.size_variant ?? "md";
  if (!SIZE_VARIANTS.includes(sizeVariant as (typeof SIZE_VARIANTS)[number])) {
    return { ok: "validation", reason: `size_variant must be one of ${SIZE_VARIANTS.join("|")}` };
  }

  const dup = (await sql`
    select 1 from catalog_entity where kind='button' and slug=${body.slug} limit 1
  `) as unknown as Array<{ "?column?": number }>;
  if (dup.length > 0) return { ok: "conflict", reason: "duplicate_slug" };

  const tags = body.tags ?? [];
  const inserted = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('button', ${body.slug}, ${body.display_name}, ${tags})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const entity_id = inserted[0]!.id;
  const idNum = Number.parseInt(entity_id, 10);

  const d = body.button_detail ?? {};
  const enablePredicate = d.enable_predicate_json ?? {};
  await sql`
    insert into button_detail (
      entity_id,
      sprite_idle_entity_id, sprite_hover_entity_id, sprite_pressed_entity_id,
      sprite_disabled_entity_id, sprite_icon_entity_id, sprite_badge_entity_id,
      token_palette_entity_id, token_frame_style_entity_id,
      token_font_entity_id, token_illumination_entity_id,
      size_variant, action_id, enable_predicate_json
    ) values (
      ${idNum},
      ${toIdNumOrNull(d.sprite_idle_entity_id ?? null)},
      ${toIdNumOrNull(d.sprite_hover_entity_id ?? null)},
      ${toIdNumOrNull(d.sprite_pressed_entity_id ?? null)},
      ${toIdNumOrNull(d.sprite_disabled_entity_id ?? null)},
      ${toIdNumOrNull(d.sprite_icon_entity_id ?? null)},
      ${toIdNumOrNull(d.sprite_badge_entity_id ?? null)},
      ${toIdNumOrNull(d.token_palette_entity_id ?? null)},
      ${toIdNumOrNull(d.token_frame_style_entity_id ?? null)},
      ${toIdNumOrNull(d.token_font_entity_id ?? null)},
      ${toIdNumOrNull(d.token_illumination_entity_id ?? null)},
      ${sizeVariant},
      ${d.action_id ?? ""},
      ${sql.json(enablePredicate as Parameters<typeof sql.json>[0])}
    )
  `;
  return { ok: "ok", data: { entity_id, slug: body.slug } };
}

function unknownFieldsOf(body: CatalogButtonPatchBody): string[] {
  const unknown: string[] = [];
  for (const k of Object.keys(body)) if (!ALLOWED_PATCH_TOP.has(k)) unknown.push(k);
  if (body.button_detail) {
    for (const k of Object.keys(body.button_detail)) {
      if (!ALLOWED_DETAIL_KEYS.has(k)) unknown.push(`button_detail.${k}`);
    }
  }
  return unknown;
}

export async function patchButtonSpine(
  slug: string,
  body: CatalogButtonPatchBody,
  sql: Sql,
): Promise<ButtonRepoResult<CatalogButtonDto>> {
  if (typeof body !== "object" || body == null) {
    return { ok: "validation", reason: "body must be object" };
  }
  if (typeof body.updated_at !== "string") {
    return { ok: "validation", reason: "updated_at required" };
  }
  const unknown = unknownFieldsOf(body);
  if (unknown.length > 0) {
    return { ok: "validation", reason: `unknown fields: ${unknown.join(", ")}` };
  }
  if (body.button_detail?.size_variant !== undefined) {
    const sv = body.button_detail.size_variant;
    if (!SIZE_VARIANTS.includes(sv as (typeof SIZE_VARIANTS)[number])) {
      return { ok: "validation", reason: `size_variant must be one of ${SIZE_VARIANTS.join("|")}` };
    }
  }

  const locked = (await sql`
    select id, updated_at
      from catalog_entity
     where kind = 'button' and slug = ${slug}
     for update
  `) as unknown as Array<{ id: string; updated_at: string }>;
  if (locked.length === 0) return { ok: "notfound" };
  const cur = locked[0]!;
  if (new Date(cur.updated_at).toISOString() !== new Date(body.updated_at).toISOString()) {
    const current = await getButtonSpineBySlug(slug, sql);
    return { ok: "conflict", reason: "stale_updated_at", current: current ?? undefined };
  }
  const idNum = Number.parseInt(cur.id, 10);

  if (body.display_name !== undefined) {
    await sql`update catalog_entity set display_name=${body.display_name} where id=${idNum}`;
  }
  if (body.tags !== undefined) {
    await sql`update catalog_entity set tags=${body.tags} where id=${idNum}`;
  }

  if (body.button_detail) {
    const d = body.button_detail;
    for (const col of [...SPRITE_SLOT_COLUMNS, ...TOKEN_SLOT_COLUMNS]) {
      const v = (d as Record<string, unknown>)[col];
      if (v !== undefined) {
        const num = toIdNumOrNull(v as string | null);
        await sql`update button_detail set ${sql(col)}=${num} where entity_id=${idNum}`;
      }
    }
    if (d.size_variant !== undefined) {
      await sql`update button_detail set size_variant=${d.size_variant} where entity_id=${idNum}`;
    }
    if (d.action_id !== undefined) {
      await sql`update button_detail set action_id=${d.action_id} where entity_id=${idNum}`;
    }
    if (d.enable_predicate_json !== undefined) {
      const json = (d.enable_predicate_json ?? {}) as Parameters<typeof sql.json>[0];
      await sql`update button_detail set enable_predicate_json=${sql.json(json)} where entity_id=${idNum}`;
    }
  }

  await sql`update catalog_entity set updated_at = now() where id = ${idNum}`;
  const after = await getButtonSpineBySlug(slug, sql);
  if (after == null) return { ok: "notfound" };
  return { ok: "ok", data: after };
}
