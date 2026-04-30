/**
 * Catalog retire handler — soft-retire via updated_at fingerprint (DEC-A23).
 */

import type { Sql } from "postgres";
import type { CatalogKind } from "@/lib/refs/types";
import { retireSprite } from "@/lib/db/sprite-repo";
import { retireArchetype } from "@/lib/catalog/archetype-repo";
import { getSql } from "@/lib/db/client";

export type RetireResult =
  | { ok: "ok"; data: { entity_id: string } }
  | { ok: "notfound" }
  | { ok: "conflict"; reason: string }
  | { ok: "validation"; reason: string };

/** Retire by slug. updated_at fingerprint checked for kinds that support it. */
export async function retireCatalogEntity(
  kind: CatalogKind,
  slug: string,
  updatedAt: string,
  sql?: Sql,
): Promise<RetireResult> {
  const db = sql ?? getSql();

  if (kind === "sprite") {
    return retireSprite(slug, db) as Promise<RetireResult>;
  }
  if (kind === "archetype") {
    return retireArchetype(slug, db) as Promise<RetireResult>;
  }

  // Generic retire via raw SQL for other kinds
  const locked = (await db`
    SELECT id, updated_at FROM catalog_entity
    WHERE kind = ${kind} AND slug = ${slug}
    FOR UPDATE
  `) as unknown as Array<{ id: string; updated_at: string }>;
  if (locked.length === 0) return { ok: "notfound" };
  const cur = locked[0]!;
  if (new Date(cur.updated_at).toISOString() !== new Date(updatedAt).toISOString()) {
    return { ok: "conflict", reason: "stale_updated_at" };
  }
  const idNum = Number.parseInt(cur.id, 10);
  await db`UPDATE catalog_entity SET retired_at = now(), updated_at = now() WHERE id = ${idNum}`;
  return { ok: "ok", data: { entity_id: cur.id } };
}
