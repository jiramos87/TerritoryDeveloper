/**
 * Entity search helper for `<EntityRefPicker>` (TECH-1787).
 *
 * Returns kind-filtered entity rows for the picker dropdown, with optional
 * substring search against slug + display_name. Resolution-state metadata
 * (current_published_version_id, retired_at) is included so the picker can
 * render the green/red badge per DEC-A22.
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1787 §Plan Digest
 */
import { getSql } from "@/lib/db/client";

export type EntityRefSearchRow = {
  entity_id: string;
  slug: string;
  display_name: string;
  kind: string;
  current_published_version_id: string | null;
  retired_at: string | null;
};

const ALLOWED_KINDS = new Set([
  "sprite",
  "asset",
  "button",
  "panel",
  "pool",
  "token",
  "archetype",
  "audio",
]);

export function validateKindList(kindsRaw: string): string[] | null {
  const list = kindsRaw
    .split(",")
    .map((k) => k.trim())
    .filter((k) => k.length > 0);
  if (list.length === 0) return null;
  for (const k of list) {
    if (!ALLOWED_KINDS.has(k)) return null;
  }
  return list;
}

export async function searchEntitiesForPicker(opts: {
  kinds: string[];
  q: string | null;
  limit: number;
}): Promise<EntityRefSearchRow[]> {
  const sql = getSql();
  const { kinds, q, limit } = opts;
  const qLike = q && q.length > 0 ? `%${q.toLowerCase()}%` : null;
  const qCond = qLike
    ? sql` and (lower(e.slug) like ${qLike} or lower(e.display_name) like ${qLike}) `
    : sql``;
  const rows = await sql`
    select
      e.id::text as entity_id,
      e.slug,
      e.display_name,
      e.kind,
      e.current_published_version_id::text as current_published_version_id,
      e.retired_at
    from catalog_entity e
    where e.kind = any(${kinds}::text[])
      ${qCond}
    order by e.display_name asc
    limit ${limit}
  `;
  return rows as unknown as EntityRefSearchRow[];
}
