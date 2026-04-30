/**
 * Catalog restore handler — soft-restore via clearing retired_at (DEC-A23).
 */

import type { Sql } from "postgres";
import type { CatalogKind } from "@/lib/refs/types";
import { restoreSprite } from "@/lib/db/sprite-repo";
import { restoreArchetype } from "@/lib/catalog/archetype-repo";
import { getSql } from "@/lib/db/client";

export type RestoreResult =
  | { ok: "ok"; data: { entity_id: string } }
  | { ok: "notfound" }
  | { ok: "conflict"; reason: string }
  | { ok: "validation"; reason: string };

/** Restore by slug — clears retired_at. */
export async function restoreCatalogEntity(
  kind: CatalogKind,
  slug: string,
  sql?: Sql,
): Promise<RestoreResult> {
  const db = sql ?? getSql();

  if (kind === "sprite") {
    return restoreSprite(slug, db) as Promise<RestoreResult>;
  }
  if (kind === "archetype") {
    return restoreArchetype(slug, db) as Promise<RestoreResult>;
  }

  // Generic restore via raw SQL for other kinds
  const rows = (await db`
    UPDATE catalog_entity SET retired_at = NULL, updated_at = now()
    WHERE kind = ${kind} AND slug = ${slug} AND retired_at IS NOT NULL
    RETURNING id::text AS id
  `) as unknown as Array<{ id: string }>;
  if (rows.length === 0) {
    const ex = (await db`
      SELECT 1 FROM catalog_entity WHERE kind = ${kind} AND slug = ${slug}
    `) as unknown as Array<unknown>;
    return ex.length === 0 ? { ok: "notfound" } : { ok: "conflict", reason: "not_retired" };
  }
  return { ok: "ok", data: { entity_id: rows[0]!.id } };
}
