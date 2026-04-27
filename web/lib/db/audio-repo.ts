/**
 * Audio repo — typed query helpers for catalog_entity (kind=audio) +
 * entity_version + audio_detail. Backs `web/app/api/catalog/audio/*` (TECH-1958).
 *
 * Conventions mirror sprite-repo (TECH-1675): writes accept tx via `Sql`
 * parameter so callers wrap them in `withAudit`; slug regex enforced at
 * DB layer; frozen-version edits surface as `frozen_version` markers.
 *
 * @see ia/projects/asset-pipeline/stage-9.1/TECH-1958.md
 */
import type { Sql } from "postgres";
import { getSql } from "@/lib/db/client";

export type AudioRepoResult<T> =
  | { ok: "ok"; data: T }
  | { ok: "notfound" }
  | { ok: "validation"; reason: string }
  | { ok: "lint_block"; reason: string; details: unknown };

export type AudioListFilter = "active" | "retired" | "all";

export type AudioListItem = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  current_published_version_id: string | null;
  active_version_id: string | null;
  active_version_status: "draft" | "published" | null;
  assets_path: string | null;
  source_uri: string | null;
  duration_ms: number | null;
  sample_rate: number | null;
  channels: number | null;
  loudness_lufs: number | null;
  peak_db: number | null;
  fingerprint: string | null;
  updated_at: string;
};

export type AudioDetailDto = AudioListItem & {
  active_version_params: Record<string, unknown> | null;
};

export async function listAudioEntities(opts: {
  filter: AudioListFilter;
  limit: number;
  cursor: string | null;
}): Promise<{ items: AudioListItem[]; next_cursor: string | null }> {
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
      d.source_uri,
      d.duration_ms,
      d.sample_rate,
      d.channels,
      d.loudness_lufs,
      d.peak_db,
      d.fingerprint
    from catalog_entity e
    left join audio_detail d on d.entity_id = e.id
    where e.kind = 'audio' ${retiredCond} ${cursorCond}
    order by e.id asc
    limit ${limit}
  `;
  const items = rows as unknown as AudioListItem[];
  const next_cursor =
    items.length === limit ? items[items.length - 1]!.entity_id : null;
  return { items, next_cursor };
}

export async function getAudioBySlug(slug: string): Promise<AudioDetailDto | null> {
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
      d.assets_path,
      d.source_uri,
      d.duration_ms,
      d.sample_rate,
      d.channels,
      d.loudness_lufs,
      d.peak_db,
      d.fingerprint
    from catalog_entity e
    left join audio_detail d on d.entity_id = e.id
    where e.kind = 'audio' and e.slug = ${slug}
    limit 1
  `) as unknown as AudioDetailDto[];
  return rows.length > 0 ? rows[0]! : null;
}

/**
 * Read measurements + ensure entity exists. Used by promote handler before
 * blob copy so lint can fail fast without touching the filesystem.
 */
export async function getAudioMeasurements(
  slug: string,
  sql: Sql,
): Promise<
  AudioRepoResult<{
    entity_id: string;
    loudness_lufs: number | null;
    peak_db: number | null;
    source_uri: string;
  }>
> {
  const rows = (await sql`
    select
      e.id::text as entity_id,
      d.loudness_lufs,
      d.peak_db,
      d.source_uri
    from catalog_entity e
    join audio_detail d on d.entity_id = e.id
    where e.kind = 'audio' and e.slug = ${slug}
    limit 1
  `) as unknown as Array<{
    entity_id: string;
    loudness_lufs: number | null;
    peak_db: number | null;
    source_uri: string;
  }>;
  if (rows.length === 0) return { ok: "notfound" };
  return { ok: "ok", data: rows[0]! };
}

/**
 * Write promote target columns onto audio_detail. Caller is responsible for
 * the blob copy + lint gating; this helper is the DB half of the promote tx.
 */
export async function promoteAudio(
  slug: string,
  args: { assets_path: string },
  sql: Sql,
): Promise<AudioRepoResult<{ entity_id: string; assets_path: string }>> {
  const ent = (await sql`
    select id::text as id from catalog_entity
     where kind = 'audio' and slug = ${slug}
     limit 1
  `) as unknown as Array<{ id: string }>;
  if (ent.length === 0) return { ok: "notfound" };
  const entity_id = ent[0]!.id;

  await sql`
    update audio_detail
       set assets_path = ${args.assets_path}
     where entity_id = ${entity_id}::bigint
  `;
  return { ok: "ok", data: { entity_id, assets_path: args.assets_path } };
}
