/**
 * Catalog publish handler — wraps publish route logic (DEC-A22, DEC-A30).
 */

import type { Sql } from "postgres";
import type { CatalogKind } from "@/lib/refs/types";

export type PublishResult =
  | { ok: "ok"; data: { version_id: string; status: "published" } }
  | { ok: "notfound"; reason?: string }
  | { ok: "conflict"; reason: string; details?: unknown }
  | { ok: "validation"; reason: string };

export interface PublishInput {
  entityId: string;
  versionId: string;
  justification?: string;
}

/** Publish entity version. Lint enforcement delegated to route layer (DEC-A30). */
export async function publishCatalogEntity(
  kind: CatalogKind,
  input: PublishInput,
  sql: Sql,
): Promise<PublishResult> {
  const entityIdNum = Number.parseInt(input.entityId, 10);
  const versionIdNum = Number.parseInt(input.versionId, 10);
  if (Number.isNaN(entityIdNum) || Number.isNaN(versionIdNum)) {
    return { ok: "validation", reason: "entityId and versionId must be numeric strings" };
  }

  // Lock version + verify ownership
  const locked = (await sql`
    SELECT id, entity_id::text AS entity_id, status
    FROM entity_version
    WHERE id = ${versionIdNum}
    FOR UPDATE
  `) as unknown as Array<{ id: number; entity_id: string; status: "draft" | "published" }>;

  if (locked.length === 0) {
    return { ok: "notfound", reason: "version not found" };
  }
  if (locked[0]!.entity_id !== String(entityIdNum)) {
    return { ok: "notfound", reason: "version does not belong to entity" };
  }

  if (locked[0]!.status === "published") {
    return { ok: "ok", data: { version_id: input.versionId, status: "published" } };
  }

  await sql`
    UPDATE entity_version SET status = 'published', updated_at = now() WHERE id = ${versionIdNum}
  `;
  await sql`
    UPDATE catalog_entity
    SET current_published_version_id = ${versionIdNum},
        slug_frozen_at = COALESCE(slug_frozen_at, now())
    WHERE id = ${entityIdNum}
  `;

  return { ok: "ok", data: { version_id: input.versionId, status: "published" } };
}
