/**
 * Archetype repo (TECH-2459 + TECH-2461 + TECH-2462 / Stage 11.1).
 *
 * Bridges `catalog_entity` (kind='archetype') + `entity_version` rows that
 * carry the archetype schema (`params_json`) + per-version `migration_hint_json`.
 * Entities pin to a specific `entity_version.id` via the
 * `entity_version.archetype_version_id` column on consumer rows — pinning
 * prevents schema-bump breakage of pinned children (DEC-A46).
 *
 * Surface (Stage 11.1):
 *   - listArchetypes / getArchetypeBySlug / getArchetypeById
 *   - createArchetype  — soft-retire + restore via retireArchetype
 *   - patchArchetype   — DEC-A38 If-Match optimistic lock; slug frozen post-publish
 *   - listVersions     — version history + pinned-entity counts
 *   - getVersion       — single version row
 *   - patchVersionParams        — TECH-2460 schema-editor PATCH on draft rows
 *   - publishVersion            — TECH-2461 hint-validator gated
 *   - clonePublishedToDraft     — TECH-2461 bump-flow seed
 *   - countPinnedEntities       — TECH-2461 preview surface
 *   - findLatestPublishedArchetypeVersion — TECH-2462 banner trigger query
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2459 + TECH-2461 + TECH-2462 §Plan Digests
 */
import type { Sql } from "postgres";
import { getSql } from "@/lib/db/client";

const SLUG_RE = /^[a-z][a-z0-9_]{2,63}$/;
export const archetypeSlugRegex = SLUG_RE;

export type ValidationResult =
  | { ok: true }
  | { ok: false; reason: string };

export function validateArchetypeCreateBody(body: CreateArchetypeBody): ValidationResult {
  if (typeof body !== "object" || body == null) {
    return { ok: false, reason: "body must be object" };
  }
  if (!SLUG_RE.test(body.slug)) {
    return { ok: false, reason: "slug must match ^[a-z][a-z0-9_]{2,63}$" };
  }
  if (typeof body.display_name !== "string" || body.display_name.trim() === "") {
    return { ok: false, reason: "display_name required" };
  }
  return { ok: true };
}

export function validateArchetypePatchBody(body: PatchArchetypeBody): ValidationResult {
  if (typeof body !== "object" || body == null) {
    return { ok: false, reason: "body must be object" };
  }
  if (typeof body.updated_at !== "string") {
    return { ok: false, reason: "updated_at required" };
  }
  const unknown = unknownFieldsOf(body);
  if (unknown.length > 0) {
    return { ok: false, reason: `unknown fields: ${unknown.join(", ")}` };
  }
  if (body.slug !== undefined && !SLUG_RE.test(body.slug)) {
    return { ok: false, reason: "slug must match ^[a-z][a-z0-9_]{2,63}$" };
  }
  return { ok: true };
}

export function validateVersionPatchBody(body: PatchVersionParamsBody): ValidationResult {
  if (typeof body !== "object" || body == null) {
    return { ok: false, reason: "body must be object" };
  }
  if (typeof body.updated_at !== "string") {
    return { ok: false, reason: "updated_at required" };
  }
  if (typeof body.params_json !== "object" || body.params_json == null || Array.isArray(body.params_json)) {
    return { ok: false, reason: "params_json required (object)" };
  }
  return { ok: true };
}

export type ArchetypeListItem = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  current_published_version_id: string | null;
  updated_at: string;
  /** Convention: `entity_version.params_json.kind_tag` on the published version row. */
  kind_tag: string | null;
};

export type ArchetypeListFilter = "active" | "retired" | "all";

export type ArchetypeVersionRow = {
  version_id: string;
  entity_id: string;
  version_number: number;
  status: "draft" | "published";
  params_json: Record<string, unknown>;
  migration_hint_json: Record<string, unknown> | null;
  parent_version_id: string | null;
  created_at: string;
  updated_at: string;
};

export type ArchetypeVersionWithPinCount = ArchetypeVersionRow & {
  /** Count of `entity_version` rows where `archetype_version_id == version_id`. */
  pinned_entity_count: number;
};

export type ArchetypeDto = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  current_published_version_id: string | null;
  updated_at: string;
  current_version: ArchetypeVersionRow | null;
};

export type ArchetypeRepoResult<T> =
  | { ok: "ok"; data: T }
  | { ok: "notfound" }
  | { ok: "conflict"; reason: string; current?: ArchetypeDto }
  | { ok: "validation"; reason: string };

export type CreateArchetypeBody = {
  slug: string;
  display_name: string;
  tags?: string[];
  /** Optional initial draft `entity_version.params_json` body. Defaults to `{}`. */
  initial_params?: Record<string, unknown>;
  /** Sub-kind tag stored in `params_json.kind_tag`. */
  kind_tag?: string;
};

export type PatchArchetypeBody = {
  updated_at: string;
  display_name?: string;
  tags?: string[];
  /** Slug PATCH rejected when any published version exists (slug freeze post-publish). */
  slug?: string;
};

export type PatchVersionParamsBody = {
  updated_at: string;
  params_json: Record<string, unknown>;
  migration_hint_json?: Record<string, unknown> | null;
};

const ALLOWED_PATCH_TOP = new Set([
  "updated_at",
  "display_name",
  "tags",
  "slug",
]);

function unknownFieldsOf(body: PatchArchetypeBody): string[] {
  const out: string[] = [];
  for (const k of Object.keys(body)) if (!ALLOWED_PATCH_TOP.has(k)) out.push(k);
  return out;
}

export async function listArchetypes(opts: {
  filter: ArchetypeListFilter;
  limit: number;
  cursor: string | null;
}): Promise<{ items: ArchetypeListItem[]; next_cursor: string | null }> {
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
      v.params_json
    from catalog_entity e
    left join entity_version v on v.id = e.current_published_version_id
    where e.kind = 'archetype' ${retiredCond} ${cursorCond}
    order by e.id asc
    limit ${limit}
  `) as unknown as Array<Record<string, unknown>>;
  const items: ArchetypeListItem[] = rows.map((r) => {
    const params = (r.params_json as Record<string, unknown> | null) ?? null;
    const kind_tag = params != null && typeof params.kind_tag === "string"
      ? (params.kind_tag as string)
      : null;
    return {
      entity_id: r.entity_id as string,
      slug: r.slug as string,
      display_name: r.display_name as string,
      tags: ((r.tags as string[] | null) ?? []),
      retired_at: ((r.retired_at as string | null) ?? null),
      current_published_version_id:
        ((r.current_published_version_id as string | null) ?? null),
      updated_at: r.updated_at as string,
      kind_tag,
    };
  });
  const next_cursor = items.length === limit ? items[items.length - 1]!.entity_id : null;
  return { items, next_cursor };
}

async function loadVersionRow(
  sql: Sql,
  versionId: number,
): Promise<ArchetypeVersionRow | null> {
  const rows = (await sql`
    select
      id::text as version_id,
      entity_id::text as entity_id,
      version_number,
      status,
      params_json,
      migration_hint_json,
      parent_version_id::text as parent_version_id,
      created_at,
      updated_at
    from entity_version
    where id = ${versionId}
    limit 1
  `) as unknown as Array<Record<string, unknown>>;
  if (rows.length === 0) return null;
  const r = rows[0]!;
  return {
    version_id: r.version_id as string,
    entity_id: r.entity_id as string,
    version_number: Number(r.version_number),
    status: r.status as "draft" | "published",
    params_json: (r.params_json as Record<string, unknown>) ?? {},
    migration_hint_json: (r.migration_hint_json as Record<string, unknown> | null) ?? null,
    parent_version_id: (r.parent_version_id as string | null) ?? null,
    created_at: r.created_at as string,
    updated_at: r.updated_at as string,
  };
}

export async function getArchetypeBySlug(
  slug: string,
  externalTx?: Sql,
): Promise<ArchetypeDto | null> {
  const sql = externalTx ?? getSql();
  const rows = (await sql`
    select
      e.id::text as entity_id,
      e.slug,
      e.display_name,
      e.tags,
      e.retired_at,
      e.current_published_version_id::text as current_published_version_id,
      e.updated_at
    from catalog_entity e
    where e.kind = 'archetype' and e.slug = ${slug}
    limit 1
  `) as unknown as Array<Record<string, unknown>>;
  if (rows.length === 0) return null;
  const r = rows[0]!;
  let current_version: ArchetypeVersionRow | null = null;
  const cpvId = r.current_published_version_id as string | null;
  if (cpvId != null) {
    current_version = await loadVersionRow(sql as Sql, Number.parseInt(cpvId, 10));
  }
  return {
    entity_id: r.entity_id as string,
    slug: r.slug as string,
    display_name: r.display_name as string,
    tags: ((r.tags as string[] | null) ?? []),
    retired_at: ((r.retired_at as string | null) ?? null),
    current_published_version_id: cpvId,
    updated_at: r.updated_at as string,
    current_version,
  };
}

export async function getArchetypeById(
  entityId: string,
  externalTx?: Sql,
): Promise<ArchetypeDto | null> {
  if (!/^\d+$/.test(entityId)) return null;
  const sql = externalTx ?? getSql();
  const idNum = Number.parseInt(entityId, 10);
  const rows = (await sql`
    select
      e.id::text as entity_id,
      e.slug,
      e.display_name,
      e.tags,
      e.retired_at,
      e.current_published_version_id::text as current_published_version_id,
      e.updated_at
    from catalog_entity e
    where e.kind = 'archetype' and e.id = ${idNum}
    limit 1
  `) as unknown as Array<Record<string, unknown>>;
  if (rows.length === 0) return null;
  const r = rows[0]!;
  let current_version: ArchetypeVersionRow | null = null;
  const cpvId = r.current_published_version_id as string | null;
  if (cpvId != null) {
    current_version = await loadVersionRow(sql as Sql, Number.parseInt(cpvId, 10));
  }
  return {
    entity_id: r.entity_id as string,
    slug: r.slug as string,
    display_name: r.display_name as string,
    tags: ((r.tags as string[] | null) ?? []),
    retired_at: ((r.retired_at as string | null) ?? null),
    current_published_version_id: cpvId,
    updated_at: r.updated_at as string,
    current_version,
  };
}

export async function createArchetype(
  body: CreateArchetypeBody,
  sql: Sql,
): Promise<ArchetypeRepoResult<{ entity_id: string; slug: string; draft_version_id: string }>> {
  const v = validateArchetypeCreateBody(body);
  if (!v.ok) return { ok: "validation", reason: v.reason };

  const dup = (await sql`
    select 1 from catalog_entity where kind='archetype' and slug=${body.slug} limit 1
  `) as unknown as Array<unknown>;
  if (dup.length > 0) return { ok: "conflict", reason: "duplicate_slug" };

  const tags = body.tags ?? [];
  const inserted = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('archetype', ${body.slug}, ${body.display_name}, ${tags})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const entity_id = inserted[0]!.id;
  const idNum = Number.parseInt(entity_id, 10);

  const params: Record<string, unknown> = { ...(body.initial_params ?? {}) };
  if (body.kind_tag !== undefined) params.kind_tag = body.kind_tag;

  const versionRows = (await sql`
    insert into entity_version (entity_id, version_number, status, params_json)
    values (${idNum}, 1, 'draft', ${sql.json(params as Parameters<typeof sql.json>[0])})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const draft_version_id = versionRows[0]!.id;

  return { ok: "ok", data: { entity_id, slug: body.slug, draft_version_id } };
}

export async function patchArchetype(
  slug: string,
  body: PatchArchetypeBody,
  sql: Sql,
): Promise<ArchetypeRepoResult<ArchetypeDto>> {
  const v = validateArchetypePatchBody(body);
  if (!v.ok) return { ok: "validation", reason: v.reason };

  const locked = (await sql`
    select id, updated_at
      from catalog_entity
     where kind = 'archetype' and slug = ${slug}
     for update
  `) as unknown as Array<{ id: string; updated_at: string }>;
  if (locked.length === 0) return { ok: "notfound" };
  const cur = locked[0]!;
  if (new Date(cur.updated_at).toISOString() !== new Date(body.updated_at).toISOString()) {
    const current = await getArchetypeBySlug(slug, sql);
    return { ok: "conflict", reason: "stale_updated_at", current: current ?? undefined };
  }
  const idNum = Number.parseInt(cur.id, 10);

  if (body.slug !== undefined && body.slug !== slug) {
    if (!SLUG_RE.test(body.slug)) {
      return { ok: "validation", reason: "slug must match ^[a-z][a-z0-9_]{2,63}$" };
    }
    const published = (await sql`
      select 1 from entity_version
       where entity_id = ${idNum} and status = 'published' limit 1
    `) as unknown as Array<unknown>;
    if (published.length > 0) {
      return { ok: "conflict", reason: "slug_frozen_post_publish" };
    }
    await sql`update catalog_entity set slug=${body.slug} where id=${idNum}`;
  }

  if (body.display_name !== undefined) {
    await sql`update catalog_entity set display_name=${body.display_name} where id=${idNum}`;
  }
  if (body.tags !== undefined) {
    await sql`update catalog_entity set tags=${body.tags} where id=${idNum}`;
  }

  const after = await getArchetypeBySlug(body.slug ?? slug, sql);
  if (after == null) return { ok: "notfound" };
  return { ok: "ok", data: after };
}

/** Soft-retire (idempotent re-retire returns current). */
export async function retireArchetype(
  slug: string,
  sql: Sql,
): Promise<ArchetypeRepoResult<ArchetypeDto>> {
  const locked = (await sql`
    select id, retired_at from catalog_entity
     where kind='archetype' and slug=${slug} for update
  `) as unknown as Array<{ id: string; retired_at: string | null }>;
  if (locked.length === 0) return { ok: "notfound" };
  const idNum = Number.parseInt(locked[0]!.id, 10);
  if (locked[0]!.retired_at == null) {
    await sql`update catalog_entity set retired_at=now() where id=${idNum}`;
  }
  const after = await getArchetypeBySlug(slug, sql);
  return after == null ? { ok: "notfound" } : { ok: "ok", data: after };
}

/** Restore = clear retired_at (idempotent). */
export async function restoreArchetype(
  slug: string,
  sql: Sql,
): Promise<ArchetypeRepoResult<ArchetypeDto>> {
  const locked = (await sql`
    select id from catalog_entity
     where kind='archetype' and slug=${slug} for update
  `) as unknown as Array<{ id: string }>;
  if (locked.length === 0) return { ok: "notfound" };
  const idNum = Number.parseInt(locked[0]!.id, 10);
  await sql`update catalog_entity set retired_at=null where id=${idNum}`;
  const after = await getArchetypeBySlug(slug, sql);
  return after == null ? { ok: "notfound" } : { ok: "ok", data: after };
}

/** List versions for an archetype with pinned-entity counts (for detail page). */
export async function listVersionsForArchetype(
  entityId: string,
  externalTx?: Sql,
): Promise<ArchetypeVersionWithPinCount[]> {
  if (!/^\d+$/.test(entityId)) return [];
  const sql = externalTx ?? getSql();
  const idNum = Number.parseInt(entityId, 10);
  const rows = (await sql`
    select
      v.id::text as version_id,
      v.entity_id::text as entity_id,
      v.version_number,
      v.status,
      v.params_json,
      v.migration_hint_json,
      v.parent_version_id::text as parent_version_id,
      v.created_at,
      v.updated_at,
      coalesce(pc.cnt, 0)::int as pinned_entity_count
    from entity_version v
    left join (
      select archetype_version_id, count(*) as cnt
        from entity_version
       where archetype_version_id is not null
       group by archetype_version_id
    ) pc on pc.archetype_version_id = v.id
    where v.entity_id = ${idNum}
    order by v.version_number asc
  `) as unknown as Array<Record<string, unknown>>;
  return rows.map((r) => ({
    version_id: r.version_id as string,
    entity_id: r.entity_id as string,
    version_number: Number(r.version_number),
    status: r.status as "draft" | "published",
    params_json: (r.params_json as Record<string, unknown>) ?? {},
    migration_hint_json: (r.migration_hint_json as Record<string, unknown> | null) ?? null,
    parent_version_id: (r.parent_version_id as string | null) ?? null,
    created_at: r.created_at as string,
    updated_at: r.updated_at as string,
    pinned_entity_count: Number(r.pinned_entity_count),
  }));
}

export async function getVersion(
  entityId: string,
  versionId: string,
  externalTx?: Sql,
): Promise<ArchetypeVersionRow | null> {
  if (!/^\d+$/.test(entityId) || !/^\d+$/.test(versionId)) return null;
  const sql = externalTx ?? getSql();
  const idNum = Number.parseInt(versionId, 10);
  const eIdNum = Number.parseInt(entityId, 10);
  const row = await loadVersionRow(sql as Sql, idNum);
  if (row == null || row.entity_id !== String(eIdNum)) return null;
  return row;
}

/** Count entities pinned to a specific archetype version. */
export async function countPinnedEntities(
  versionId: string,
  externalTx?: Sql,
): Promise<number> {
  if (!/^\d+$/.test(versionId)) return 0;
  const sql = externalTx ?? getSql();
  const idNum = Number.parseInt(versionId, 10);
  const rows = (await sql`
    select count(*)::int as cnt from entity_version
     where archetype_version_id = ${idNum}
  `) as unknown as Array<{ cnt: number }>;
  return Number(rows[0]?.cnt ?? 0);
}

/** TECH-2462 banner trigger: highest-version_number published row for archetype. */
export async function findLatestPublishedArchetypeVersion(
  archetypeEntityId: string,
  externalTx?: Sql,
): Promise<ArchetypeVersionRow | null> {
  if (!/^\d+$/.test(archetypeEntityId)) return null;
  const sql = externalTx ?? getSql();
  const idNum = Number.parseInt(archetypeEntityId, 10);
  const rows = (await sql`
    select
      id::text as version_id,
      entity_id::text as entity_id,
      version_number,
      status,
      params_json,
      migration_hint_json,
      parent_version_id::text as parent_version_id,
      created_at,
      updated_at
    from entity_version
    where entity_id = ${idNum} and status = 'published'
    order by version_number desc
    limit 1
  `) as unknown as Array<Record<string, unknown>>;
  if (rows.length === 0) return null;
  const r = rows[0]!;
  return {
    version_id: r.version_id as string,
    entity_id: r.entity_id as string,
    version_number: Number(r.version_number),
    status: r.status as "draft" | "published",
    params_json: (r.params_json as Record<string, unknown>) ?? {},
    migration_hint_json: (r.migration_hint_json as Record<string, unknown> | null) ?? null,
    parent_version_id: (r.parent_version_id as string | null) ?? null,
    created_at: r.created_at as string,
    updated_at: r.updated_at as string,
  };
}

/** PATCH a draft version's params_json + optional migration_hint_json. Rejects published rows. */
export async function patchVersionParams(
  entityId: string,
  versionId: string,
  body: PatchVersionParamsBody,
  sql: Sql,
): Promise<ArchetypeRepoResult<ArchetypeVersionRow>> {
  const v = validateVersionPatchBody(body);
  if (!v.ok) return { ok: "validation", reason: v.reason };
  if (!/^\d+$/.test(versionId) || !/^\d+$/.test(entityId)) {
    return { ok: "validation", reason: "ids must be numeric" };
  }
  const idNum = Number.parseInt(versionId, 10);
  const eIdNum = Number.parseInt(entityId, 10);

  const locked = (await sql`
    select id, entity_id::text as entity_id, status, updated_at
      from entity_version
     where id = ${idNum}
     for update
  `) as unknown as Array<{
    id: string;
    entity_id: string;
    status: "draft" | "published";
    updated_at: string;
  }>;
  if (locked.length === 0) return { ok: "notfound" };
  const cur = locked[0]!;
  if (cur.entity_id !== String(eIdNum)) return { ok: "notfound" };
  if (cur.status === "published") {
    return { ok: "conflict", reason: "version_already_published" };
  }
  if (new Date(cur.updated_at).toISOString() !== new Date(body.updated_at).toISOString()) {
    return { ok: "conflict", reason: "stale_updated_at" };
  }

  await sql`
    update entity_version
       set params_json = ${sql.json(body.params_json as Parameters<typeof sql.json>[0])},
           migration_hint_json = ${
             body.migration_hint_json === undefined
               ? sql`migration_hint_json`
               : body.migration_hint_json === null
                 ? null
                 : sql.json(body.migration_hint_json as Parameters<typeof sql.json>[0])
           },
           updated_at = now()
     where id = ${idNum}
  `;
  const after = await loadVersionRow(sql, idNum);
  return after == null ? { ok: "notfound" } : { ok: "ok", data: after };
}

/** TECH-2461: clone a published version into a new draft (next version_number). */
export async function clonePublishedToDraft(
  entityId: string,
  sourceVersionId: string,
  sql: Sql,
): Promise<ArchetypeRepoResult<{ new_version_id: string }>> {
  if (!/^\d+$/.test(entityId) || !/^\d+$/.test(sourceVersionId)) {
    return { ok: "validation", reason: "ids must be numeric" };
  }
  const eIdNum = Number.parseInt(entityId, 10);
  const srcIdNum = Number.parseInt(sourceVersionId, 10);
  const src = (await sql`
    select id, entity_id, version_number, status, params_json
      from entity_version
     where id = ${srcIdNum} and entity_id = ${eIdNum}
     limit 1
  `) as unknown as Array<{
    id: string;
    entity_id: string;
    version_number: number;
    status: "draft" | "published";
    params_json: Record<string, unknown>;
  }>;
  if (src.length === 0) return { ok: "notfound" };
  if (src[0]!.status !== "published") {
    return { ok: "validation", reason: "source must be published" };
  }
  const max = (await sql`
    select coalesce(max(version_number), 0)::int as m
      from entity_version where entity_id = ${eIdNum}
  `) as unknown as Array<{ m: number }>;
  const nextN = Number(max[0]!.m) + 1;
  const inserted = (await sql`
    insert into entity_version (entity_id, version_number, status, params_json, parent_version_id)
    values (
      ${eIdNum}, ${nextN}, 'draft',
      ${sql.json(src[0]!.params_json as Parameters<typeof sql.json>[0])},
      ${srcIdNum}
    )
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return { ok: "ok", data: { new_version_id: inserted[0]!.id } };
}

/**
 * TECH-2461 publish: validate hint -> flip status draft->published -> bump
 * `catalog_entity.current_published_version_id` -> first publish locks slug
 * (sets `slug_frozen_at`). All in one TX (caller passes `sql` from withAudit).
 */
export async function publishVersion(
  entityId: string,
  versionId: string,
  sql: Sql,
): Promise<ArchetypeRepoResult<ArchetypeVersionRow>> {
  if (!/^\d+$/.test(entityId) || !/^\d+$/.test(versionId)) {
    return { ok: "validation", reason: "ids must be numeric" };
  }
  const idNum = Number.parseInt(versionId, 10);
  const eIdNum = Number.parseInt(entityId, 10);

  const locked = (await sql`
    select id, entity_id::text as entity_id, status
      from entity_version where id = ${idNum} for update
  `) as unknown as Array<{ id: string; entity_id: string; status: "draft" | "published" }>;
  if (locked.length === 0) return { ok: "notfound" };
  if (locked[0]!.entity_id !== String(eIdNum)) return { ok: "notfound" };
  if (locked[0]!.status === "published") {
    const after = await loadVersionRow(sql, idNum);
    return after == null ? { ok: "notfound" } : { ok: "ok", data: after };
  }

  await sql`
    update entity_version set status='published', updated_at=now() where id=${idNum}
  `;
  await sql`
    update catalog_entity
       set current_published_version_id = ${idNum},
           slug_frozen_at = coalesce(slug_frozen_at, now())
     where id = ${eIdNum}
  `;
  const after = await loadVersionRow(sql, idNum);
  return after == null ? { ok: "notfound" } : { ok: "ok", data: after };
}
