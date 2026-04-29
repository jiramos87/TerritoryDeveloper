/**
 * Bulk entity mutation helpers (TECH-4182 / Stage 15.1).
 *
 * Three actions — retire / restore / publish — each executed inside a caller-
 * owned transaction. Returns affected count + per-entity audit payloads.
 * Audit is emitted by the caller (one row per entity, DEC-A33).
 *
 * @see web/app/api/catalog/bulk/route.ts — caller
 */
import type { TransactionSql } from "postgres";

type Sql = TransactionSql;

export type BulkAction = "retire" | "restore" | "publish";

export type BulkResult = {
  updated: number;
  audit_payloads: Array<{ entity_id: string; action: string; meta: Record<string, unknown> }>;
};

export async function runBulkRetire(tx: Sql, entityIds: string[]): Promise<BulkResult> {
  const rows = await tx<{ id: string; slug: string; kind: string; retired_at: string | null }[]>`
    UPDATE catalog_entity
    SET retired_at = now(), updated_at = now()
    WHERE id = ANY(${entityIds}::bigint[])
      AND retired_at IS NULL
    RETURNING id::text, slug, kind, retired_at::text
  `;
  return {
    updated: rows.length,
    audit_payloads: rows.map((r) => ({
      entity_id: r.id,
      action: "catalog.entity.retired_bulk",
      meta: { slug: r.slug, kind: r.kind, bulk_size: entityIds.length },
    })),
  };
}

export async function runBulkRestore(tx: Sql, entityIds: string[]): Promise<BulkResult> {
  const rows = await tx<{ id: string; slug: string; kind: string }[]>`
    UPDATE catalog_entity
    SET retired_at = NULL, retired_by_user_id = NULL, retired_reason = NULL, updated_at = now()
    WHERE id = ANY(${entityIds}::bigint[])
      AND retired_at IS NOT NULL
    RETURNING id::text, slug, kind
  `;
  return {
    updated: rows.length,
    audit_payloads: rows.map((r) => ({
      entity_id: r.id,
      action: "catalog.entity.restored_bulk",
      meta: { slug: r.slug, kind: r.kind, bulk_size: entityIds.length },
    })),
  };
}

export async function runBulkPublish(tx: Sql, entityIds: string[]): Promise<BulkResult> {
  const rows = await tx<{ id: string; slug: string; kind: string }[]>`
    SELECT e.id::text, e.slug, e.kind
    FROM catalog_entity e
    WHERE e.id = ANY(${entityIds}::bigint[])
      AND e.retired_at IS NULL
  `;
  if (rows.length === 0) return { updated: 0, audit_payloads: [] };

  await tx`
    INSERT INTO job_queue (kind, payload, created_at)
    SELECT
      'publish_entity',
      jsonb_build_object('entity_id', e.id::text, 'kind', e.kind),
      now()
    FROM catalog_entity e
    WHERE e.id = ANY(${entityIds}::bigint[])
      AND e.retired_at IS NULL
  `;

  return {
    updated: rows.length,
    audit_payloads: rows.map((r) => ({
      entity_id: r.id,
      action: "catalog.entity.published_bulk",
      meta: { slug: r.slug, kind: r.kind, bulk_size: entityIds.length },
    })),
  };
}
