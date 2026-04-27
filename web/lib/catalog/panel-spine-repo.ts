/**
 * Panel repo (TECH-1887 + TECH-1888 / Stage 8.1).
 *
 * Bridges `catalog_entity` (kind=panel) + `panel_detail` + `panel_child`.
 * Supplies list/get/create/patch DAOs; `panel_child` tree replace lives in
 * `panel-child-set.ts`. Optimistic concurrency via `catalog_entity.updated_at`
 * fingerprint per DEC-A38.
 *
 * @see ia/projects/asset-pipeline (DB) — TECH-1887 + TECH-1888 §Plan Digest
 */
import type { Sql } from "postgres";
import { getSql } from "@/lib/db/client";
import type {
  CatalogPanelChildDto,
  CatalogPanelChildKind,
  CatalogPanelCreateBody,
  CatalogPanelDto,
  CatalogPanelPatchBody,
  CatalogPanelSlotSchemaEntry,
  EntityRefSearchRow,
} from "@/types/api/catalog-api";
import { getArchetypeSlotsSchema } from "./panel-archetype-slots";

export type PanelSpineListItem = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  archetype_entity_id: string | null;
  child_count: number;
  current_published_version_id: string | null;
  updated_at: string;
};

export type PanelSpineListFilter = "active" | "retired" | "all";

export type PanelRepoResult<T> =
  | { ok: "ok"; data: T }
  | { ok: "notfound" }
  | { ok: "conflict"; reason: string; current?: CatalogPanelDto }
  | { ok: "validation"; reason: string };

const SLUG_RE = /^[a-z][a-z0-9_]{2,63}$/;
const LAYOUTS = ["vstack", "hstack", "grid", "free"] as const;

const DETAIL_REF_COLS = [
  "archetype_entity_id",
  "background_sprite_entity_id",
  "palette_entity_id",
  "frame_style_entity_id",
] as const;

const ALLOWED_PATCH_TOP = new Set(["updated_at", "display_name", "tags", "panel_detail"]);
const ALLOWED_DETAIL_KEYS = new Set([
  ...DETAIL_REF_COLS,
  "layout_template",
  "modal",
]);

function toIdNumOrNull(v: string | null | undefined): number | null {
  if (v == null) return null;
  const trimmed = String(v).trim();
  if (trimmed === "") return null;
  if (!/^\d+$/.test(trimmed)) return null;
  return Number.parseInt(trimmed, 10);
}

export async function listPanelsSpine(opts: {
  filter: PanelSpineListFilter;
  limit: number;
  cursor: string | null;
}): Promise<{ items: PanelSpineListItem[]; next_cursor: string | null }> {
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
      d.archetype_entity_id::text as archetype_entity_id,
      coalesce((select count(*) from panel_child pc where pc.panel_entity_id = e.id), 0)::int as child_count
    from catalog_entity e
    left join panel_detail d on d.entity_id = e.id
    where e.kind = 'panel' ${retiredCond} ${cursorCond}
    order by e.id asc
    limit ${limit}
  `) as unknown as PanelSpineListItem[];
  const next_cursor = rows.length === limit ? rows[rows.length - 1]!.entity_id : null;
  return { items: rows, next_cursor };
}

async function loadResolutionRows(
  sql: Sql,
  ids: number[],
): Promise<Map<number, EntityRefSearchRow>> {
  const out = new Map<number, EntityRefSearchRow>();
  if (ids.length === 0) return out;
  const rows = (await sql`
    select
      id::text as entity_id,
      slug,
      display_name,
      kind,
      current_published_version_id::text as current_published_version_id,
      retired_at
    from catalog_entity
    where id = any(${ids}::bigint[])
  `) as unknown as EntityRefSearchRow[];
  for (const r of rows) out.set(Number.parseInt(r.entity_id, 10), r);
  return out;
}

export async function getPanelSpineBySlug(
  slug: string,
  externalTx?: Sql,
): Promise<CatalogPanelDto | null> {
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
      d.archetype_entity_id::text as archetype_entity_id,
      d.background_sprite_entity_id::text as background_sprite_entity_id,
      d.palette_entity_id::text as palette_entity_id,
      d.frame_style_entity_id::text as frame_style_entity_id,
      d.layout_template,
      d.modal
    from catalog_entity e
    left join panel_detail d on d.entity_id = e.id
    where e.kind = 'panel' and e.slug = ${slug}
    limit 1
  `) as unknown as Array<Record<string, unknown>>;
  if (rows.length === 0) return null;
  const r = rows[0]!;
  const idNum = Number.parseInt(r.entity_id as string, 10);

  const refIdsNum: number[] = [];
  for (const col of DETAIL_REF_COLS) {
    const v = r[col];
    const n = toIdNumOrNull((v as string | null) ?? null);
    if (n != null) refIdsNum.push(n);
  }

  const childRows = (await sql`
    select
      pc.slot_name,
      pc.order_idx,
      pc.child_kind,
      pc.child_entity_id::text as child_entity_id,
      pc.params_json
    from panel_child pc
    where pc.panel_entity_id = ${idNum}
    order by pc.slot_name asc, pc.order_idx asc
  `) as unknown as Array<{
    slot_name: string;
    order_idx: number;
    child_kind: CatalogPanelChildKind;
    child_entity_id: string | null;
    params_json: Record<string, unknown>;
  }>;
  for (const cr of childRows) {
    const n = toIdNumOrNull(cr.child_entity_id);
    if (n != null) refIdsNum.push(n);
  }

  const resMap = await loadResolutionRows(sql as Sql, refIdsNum);

  // Slots schema lookup (null when no archetype or no published version).
  const archetypeIdNum = toIdNumOrNull((r.archetype_entity_id as string | null) ?? null);
  const slotsSchema = archetypeIdNum != null ? await getArchetypeSlotsSchema(archetypeIdNum, sql as Sql) : null;

  // Group children by slot. Order slots by archetype-declared order, then any
  // un-declared slots present in DB rows alphabetically.
  const slotOrder: string[] = [];
  if (slotsSchema != null) slotOrder.push(...Object.keys(slotsSchema));
  const seenSlots = new Set(slotOrder);
  const childrenBySlot = new Map<string, CatalogPanelChildDto[]>();
  for (const cr of childRows) {
    if (!seenSlots.has(cr.slot_name)) {
      slotOrder.push(cr.slot_name);
      seenSlots.add(cr.slot_name);
    }
    const list = childrenBySlot.get(cr.slot_name) ?? [];
    const childNum = toIdNumOrNull(cr.child_entity_id);
    list.push({
      child_entity_id: cr.child_entity_id,
      child_kind: cr.child_kind,
      slot_name: cr.slot_name,
      order_idx: cr.order_idx,
      params_json: cr.params_json ?? {},
      resolved: childNum != null ? resMap.get(childNum) ?? null : null,
    });
    childrenBySlot.set(cr.slot_name, list);
  }
  const slots = slotOrder.map((name) => {
    const schema: CatalogPanelSlotSchemaEntry | null =
      slotsSchema != null && slotsSchema[name] != null ? slotsSchema[name] : null;
    return { name, schema, children: childrenBySlot.get(name) ?? [] };
  });

  const hasDetail =
    archetypeIdNum != null ||
    r.background_sprite_entity_id != null ||
    r.palette_entity_id != null ||
    r.frame_style_entity_id != null ||
    r.layout_template != null;
  const layout = (r.layout_template as string | null) ?? "vstack";
  const archetype_resolution = archetypeIdNum != null ? resMap.get(archetypeIdNum) ?? null : null;

  return {
    entity_id: r.entity_id as string,
    slug: r.slug as string,
    display_name: r.display_name as string,
    tags: (r.tags as string[]) ?? [],
    retired_at: (r.retired_at as string | null) ?? null,
    current_published_version_id: (r.current_published_version_id as string | null) ?? null,
    updated_at: r.updated_at as string,
    panel_detail: hasDetail
      ? {
          archetype_entity_id: (r.archetype_entity_id as string | null) ?? null,
          background_sprite_entity_id: (r.background_sprite_entity_id as string | null) ?? null,
          palette_entity_id: (r.palette_entity_id as string | null) ?? null,
          frame_style_entity_id: (r.frame_style_entity_id as string | null) ?? null,
          layout_template: layout as "vstack" | "hstack" | "grid" | "free",
          modal: (r.modal as boolean) ?? false,
          slots_schema: slotsSchema,
        }
      : null,
    slots,
    archetype_resolution,
  };
}

export async function createPanelSpine(
  body: CatalogPanelCreateBody,
  sql: Sql,
): Promise<PanelRepoResult<{ entity_id: string; slug: string }>> {
  if (!SLUG_RE.test(body.slug)) {
    return { ok: "validation", reason: "slug must match ^[a-z][a-z0-9_]{2,63}$" };
  }
  if (typeof body.display_name !== "string" || body.display_name.trim() === "") {
    return { ok: "validation", reason: "display_name required" };
  }
  const d = body.panel_detail ?? {};
  const layout = d.layout_template ?? "vstack";
  if (!LAYOUTS.includes(layout as (typeof LAYOUTS)[number])) {
    return { ok: "validation", reason: `layout_template must be one of ${LAYOUTS.join("|")}` };
  }

  const dup = (await sql`
    select 1 from catalog_entity where kind='panel' and slug=${body.slug} limit 1
  `) as unknown as Array<{ "?column?": number }>;
  if (dup.length > 0) return { ok: "conflict", reason: "duplicate_slug" };

  const tags = body.tags ?? [];
  const inserted = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('panel', ${body.slug}, ${body.display_name}, ${tags})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const entity_id = inserted[0]!.id;
  const idNum = Number.parseInt(entity_id, 10);

  await sql`
    insert into panel_detail (
      entity_id, archetype_entity_id, background_sprite_entity_id,
      palette_entity_id, frame_style_entity_id, layout_template, modal
    ) values (
      ${idNum},
      ${toIdNumOrNull(d.archetype_entity_id ?? null)},
      ${toIdNumOrNull(d.background_sprite_entity_id ?? null)},
      ${toIdNumOrNull(d.palette_entity_id ?? null)},
      ${toIdNumOrNull(d.frame_style_entity_id ?? null)},
      ${layout},
      ${d.modal ?? false}
    )
  `;
  return { ok: "ok", data: { entity_id, slug: body.slug } };
}

function unknownFieldsOf(body: CatalogPanelPatchBody): string[] {
  const unknown: string[] = [];
  for (const k of Object.keys(body)) if (!ALLOWED_PATCH_TOP.has(k)) unknown.push(k);
  if (body.panel_detail) {
    for (const k of Object.keys(body.panel_detail)) {
      if (!ALLOWED_DETAIL_KEYS.has(k)) unknown.push(`panel_detail.${k}`);
    }
  }
  return unknown;
}

export async function patchPanelSpine(
  slug: string,
  body: CatalogPanelPatchBody,
  sql: Sql,
): Promise<PanelRepoResult<CatalogPanelDto>> {
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
  if (body.panel_detail?.layout_template !== undefined) {
    const lt = body.panel_detail.layout_template;
    if (!LAYOUTS.includes(lt as (typeof LAYOUTS)[number])) {
      return { ok: "validation", reason: `layout_template must be one of ${LAYOUTS.join("|")}` };
    }
  }

  const locked = (await sql`
    select id, updated_at
      from catalog_entity
     where kind = 'panel' and slug = ${slug}
     for update
  `) as unknown as Array<{ id: string; updated_at: string }>;
  if (locked.length === 0) return { ok: "notfound" };
  const cur = locked[0]!;
  if (new Date(cur.updated_at).toISOString() !== new Date(body.updated_at).toISOString()) {
    const current = await getPanelSpineBySlug(slug, sql);
    return { ok: "conflict", reason: "stale_updated_at", current: current ?? undefined };
  }
  const idNum = Number.parseInt(cur.id, 10);

  if (body.display_name !== undefined) {
    await sql`update catalog_entity set display_name=${body.display_name} where id=${idNum}`;
  }
  if (body.tags !== undefined) {
    await sql`update catalog_entity set tags=${body.tags} where id=${idNum}`;
  }

  if (body.panel_detail) {
    const d = body.panel_detail;
    for (const col of DETAIL_REF_COLS) {
      if (col in d) {
        const v = (d as Record<string, unknown>)[col] as string | null;
        const num = toIdNumOrNull(v);
        await sql`update panel_detail set ${sql(col)}=${num} where entity_id=${idNum}`;
      }
    }
    if (d.layout_template !== undefined) {
      await sql`update panel_detail set layout_template=${d.layout_template} where entity_id=${idNum}`;
    }
    if (d.modal !== undefined) {
      await sql`update panel_detail set modal=${d.modal} where entity_id=${idNum}`;
    }
  }

  await sql`update catalog_entity set updated_at = now() where id = ${idNum}`;
  const after = await getPanelSpineBySlug(slug, sql);
  if (after == null) return { ok: "notfound" };
  return { ok: "ok", data: after };
}

export async function retirePanelSpine(
  slug: string,
  body: { updated_at: string },
  sql: Sql,
): Promise<PanelRepoResult<CatalogPanelDto>> {
  if (typeof body?.updated_at !== "string") {
    return { ok: "validation", reason: "updated_at required" };
  }
  const locked = (await sql`
    select id, updated_at
      from catalog_entity
     where kind = 'panel' and slug = ${slug}
     for update
  `) as unknown as Array<{ id: string; updated_at: string }>;
  if (locked.length === 0) return { ok: "notfound" };
  const cur = locked[0]!;
  if (new Date(cur.updated_at).toISOString() !== new Date(body.updated_at).toISOString()) {
    const current = await getPanelSpineBySlug(slug, sql);
    return { ok: "conflict", reason: "stale_updated_at", current: current ?? undefined };
  }
  const idNum = Number.parseInt(cur.id, 10);
  await sql`update catalog_entity set retired_at = now(), updated_at = now() where id = ${idNum}`;
  const after = await getPanelSpineBySlug(slug, sql);
  if (after == null) return { ok: "notfound" };
  return { ok: "ok", data: after };
}
