/**
 * Refs repo — paginated `catalog_ref_edge` list (incoming + outgoing) per
 * (kind, entity_id).
 *
 * Stage 14.4 / TECH-3408 — drives `RefsTab` (TECH-3409) + 8 detail-client
 * wires (TECH-3410) + token ripple-count parity (TECH-3411). Reads
 * `catalog_ref_edge` (Stage 14.1, `db/migrations/0043_catalog_ref_edge.sql`)
 * joined to `catalog_entity` for the current-published filter
 * (`current_published_version_id`). Bigint columns serialized as strings to
 * match history-repo + detail-payload conventions.
 *
 * Cursor: opaque base64 of JSON `{created_at_us, src_id, dst_id}` where
 * `created_at_us = extract(epoch from created_at) * 1000000` (bigint
 * microseconds). Keyset comparison uses the same integer expression so the
 * roundtrip is lossless — postgres-js text→timestamptz binding flakes
 * intermittently when sub-millisecond precision matters (parity test
 * regression on `(created_at, src_id, dst_id)` tuple keyset).
 *
 * @see ia/projects/asset-pipeline/stage-14.4 — TECH-3408 §Plan Digest
 * @see db/migrations/0043_catalog_ref_edge.sql — table + indexes
 * @see web/lib/repos/history-repo.ts — sibling pattern
 */
import { getSql } from "@/lib/db/client";
import type { CatalogKind, EdgeRole } from "@/lib/refs/types";

export interface CatalogRefEdgeRow {
  src_kind: CatalogKind;
  src_id: string;
  src_version_id: string;
  dst_kind: CatalogKind;
  dst_id: string;
  dst_version_id: string;
  edge_role: EdgeRole;
  created_at: string;
}

export interface ListRefsResult {
  rows: CatalogRefEdgeRow[];
  nextCursor: string | null;
}

const DEFAULT_LIMIT = 20;
const MAX_LIMIT = 100;
const MIN_LIMIT = 1;

interface DecodedCursor {
  created_at_us: string;
  src_id: string;
  dst_id: string;
}

export class InvalidCursorError extends Error {
  constructor(message = "invalid_cursor") {
    super(message);
    this.name = "InvalidCursorError";
  }
}

export function clampLimit(input: number | null | undefined): number {
  if (input == null || Number.isNaN(input)) return DEFAULT_LIMIT;
  const n = Math.trunc(input);
  if (n < MIN_LIMIT) return MIN_LIMIT;
  if (n > MAX_LIMIT) return MAX_LIMIT;
  return n;
}

export function encodeCursor(row: {
  created_at_us: string;
  src_id: string;
  dst_id: string;
}): string {
  const json = JSON.stringify({
    created_at_us: row.created_at_us,
    src_id: row.src_id,
    dst_id: row.dst_id,
  });
  return Buffer.from(json, "utf8").toString("base64");
}

export function decodeCursor(raw: string): DecodedCursor {
  let json: string;
  try {
    json = Buffer.from(raw, "base64").toString("utf8");
  } catch {
    throw new InvalidCursorError();
  }
  let parsed: unknown;
  try {
    parsed = JSON.parse(json);
  } catch {
    throw new InvalidCursorError();
  }
  if (
    parsed == null ||
    typeof parsed !== "object" ||
    Array.isArray(parsed) ||
    typeof (parsed as { created_at_us?: unknown }).created_at_us !== "string" ||
    typeof (parsed as { src_id?: unknown }).src_id !== "string" ||
    typeof (parsed as { dst_id?: unknown }).dst_id !== "string" ||
    !/^-?\d+$/.test((parsed as { created_at_us: string }).created_at_us) ||
    !/^\d+$/.test((parsed as { src_id: string }).src_id) ||
    !/^\d+$/.test((parsed as { dst_id: string }).dst_id)
  ) {
    throw new InvalidCursorError();
  }
  return parsed as DecodedCursor;
}

interface RawEdgeRow {
  src_kind: string;
  src_id: string;
  src_version_id: string;
  dst_kind: string;
  dst_id: string;
  dst_version_id: string;
  edge_role: string;
  created_at: string | Date;
  created_at_us: string;
}

function mapRow(r: RawEdgeRow): CatalogRefEdgeRow & { created_at_us: string } {
  return {
    src_kind: r.src_kind as CatalogKind,
    src_id: r.src_id,
    src_version_id: r.src_version_id,
    dst_kind: r.dst_kind as CatalogKind,
    dst_id: r.dst_id,
    dst_version_id: r.dst_version_id,
    edge_role: r.edge_role as EdgeRole,
    created_at:
      r.created_at instanceof Date ? r.created_at.toISOString() : r.created_at,
    created_at_us: r.created_at_us,
  };
}

/**
 * Page through inbound `catalog_ref_edge` rows for `(kind, entityId)` —
 * edges where the target entity matches. Joined to `catalog_entity` on the
 * source side filtered by `current_published_version_id` so retired source
 * versions are excluded (DEC-A44 ripple semantics).
 *
 * Empty / non-numeric `entityId` → empty page. `InvalidCursorError` thrown
 * on bad cursor — route handler maps to 400.
 */
export async function listIncomingRefs(
  kind: CatalogKind,
  entityId: string,
  cursor: string | null,
  limit: number | null | undefined,
): Promise<ListRefsResult> {
  if (!/^\d+$/.test(entityId)) {
    return { rows: [], nextCursor: null };
  }
  const limitClamped = clampLimit(limit ?? DEFAULT_LIMIT);
  const decodedCursor =
    cursor != null && cursor.length > 0 ? decodeCursor(cursor) : null;
  const sql = getSql();
  const idNum = Number.parseInt(entityId, 10);
  const fetchN = limitClamped + 1;

  // NOTE: cursor uses bigint microseconds (`extract(epoch from created_at) *
  // 1000000`) for lossless integer comparison. postgres-js text→timestamptz
  // binding flakes at sub-ms precision; integer keyset bypasses the driver
  // codec entirely.
  const rows = decodedCursor == null
    ? ((await sql`
        select
          ce.src_kind,
          ce.src_id::text         as src_id,
          ce.src_version_id::text as src_version_id,
          ce.dst_kind,
          ce.dst_id::text         as dst_id,
          ce.dst_version_id::text as dst_version_id,
          ce.edge_role,
          ce.created_at::text     as created_at,
          (extract(epoch from ce.created_at) * 1000000)::bigint::text as created_at_us
        from catalog_ref_edge ce
        join catalog_entity src
          on src.id = ce.src_id
         and src.current_published_version_id = ce.src_version_id
        where ce.dst_kind = ${kind}
          and ce.dst_id = ${idNum}
        order by ce.created_at desc, ce.src_id desc, ce.dst_id desc
        limit ${fetchN}
      `) as unknown as RawEdgeRow[])
    : ((await sql`
        select
          ce.src_kind,
          ce.src_id::text         as src_id,
          ce.src_version_id::text as src_version_id,
          ce.dst_kind,
          ce.dst_id::text         as dst_id,
          ce.dst_version_id::text as dst_version_id,
          ce.edge_role,
          ce.created_at::text     as created_at,
          (extract(epoch from ce.created_at) * 1000000)::bigint::text as created_at_us
        from catalog_ref_edge ce
        join catalog_entity src
          on src.id = ce.src_id
         and src.current_published_version_id = ce.src_version_id
        where ce.dst_kind = ${kind}
          and ce.dst_id = ${idNum}
          and ((extract(epoch from ce.created_at) * 1000000)::bigint, ce.src_id, ce.dst_id) <
              (${decodedCursor.created_at_us}::bigint,
               ${decodedCursor.src_id}::bigint,
               ${decodedCursor.dst_id}::bigint)
        order by ce.created_at desc, ce.src_id desc, ce.dst_id desc
        limit ${fetchN}
      `) as unknown as RawEdgeRow[]);

  const mapped = rows.map(mapRow);

  if (mapped.length > limitClamped) {
    const truncated = mapped.slice(0, limitClamped);
    const last = truncated[truncated.length - 1]!;
    return {
      rows: truncated.map(stripUs),
      nextCursor: encodeCursor({
        created_at_us: last.created_at_us,
        src_id: last.src_id,
        dst_id: last.dst_id,
      }),
    };
  }
  return { rows: mapped.map(stripUs), nextCursor: null };
}

function stripUs(r: CatalogRefEdgeRow & { created_at_us?: string }): CatalogRefEdgeRow {
  const { created_at_us: _us, ...rest } = r;
  void _us;
  return rest;
}

/**
 * Page through outbound `catalog_ref_edge` rows for `(kind, entityId)` —
 * edges where the source entity matches. Joined to `catalog_entity` on the
 * target side filtered by `current_published_version_id`.
 */
export async function listOutgoingRefs(
  kind: CatalogKind,
  entityId: string,
  cursor: string | null,
  limit: number | null | undefined,
): Promise<ListRefsResult> {
  if (!/^\d+$/.test(entityId)) {
    return { rows: [], nextCursor: null };
  }
  const limitClamped = clampLimit(limit ?? DEFAULT_LIMIT);
  const decodedCursor =
    cursor != null && cursor.length > 0 ? decodeCursor(cursor) : null;
  const sql = getSql();
  const idNum = Number.parseInt(entityId, 10);
  const fetchN = limitClamped + 1;

  // See `listIncomingRefs` note: bigint microseconds keyset bypasses
  // postgres-js timestamptz binding flakiness.
  const rows = decodedCursor == null
    ? ((await sql`
        select
          ce.src_kind,
          ce.src_id::text         as src_id,
          ce.src_version_id::text as src_version_id,
          ce.dst_kind,
          ce.dst_id::text         as dst_id,
          ce.dst_version_id::text as dst_version_id,
          ce.edge_role,
          ce.created_at::text     as created_at,
          (extract(epoch from ce.created_at) * 1000000)::bigint::text as created_at_us
        from catalog_ref_edge ce
        join catalog_entity dst
          on dst.id = ce.dst_id
         and dst.current_published_version_id = ce.dst_version_id
        where ce.src_kind = ${kind}
          and ce.src_id = ${idNum}
        order by ce.created_at desc, ce.src_id desc, ce.dst_id desc
        limit ${fetchN}
      `) as unknown as RawEdgeRow[])
    : ((await sql`
        select
          ce.src_kind,
          ce.src_id::text         as src_id,
          ce.src_version_id::text as src_version_id,
          ce.dst_kind,
          ce.dst_id::text         as dst_id,
          ce.dst_version_id::text as dst_version_id,
          ce.edge_role,
          ce.created_at::text     as created_at,
          (extract(epoch from ce.created_at) * 1000000)::bigint::text as created_at_us
        from catalog_ref_edge ce
        join catalog_entity dst
          on dst.id = ce.dst_id
         and dst.current_published_version_id = ce.dst_version_id
        where ce.src_kind = ${kind}
          and ce.src_id = ${idNum}
          and ((extract(epoch from ce.created_at) * 1000000)::bigint, ce.src_id, ce.dst_id) <
              (${decodedCursor.created_at_us}::bigint,
               ${decodedCursor.src_id}::bigint,
               ${decodedCursor.dst_id}::bigint)
        order by ce.created_at desc, ce.src_id desc, ce.dst_id desc
        limit ${fetchN}
      `) as unknown as RawEdgeRow[]);

  const mapped = rows.map(mapRow);

  if (mapped.length > limitClamped) {
    const truncated = mapped.slice(0, limitClamped);
    const last = truncated[truncated.length - 1]!;
    return {
      rows: truncated.map(stripUs),
      nextCursor: encodeCursor({
        created_at_us: last.created_at_us,
        src_id: last.src_id,
        dst_id: last.dst_id,
      }),
    };
  }
  return { rows: mapped.map(stripUs), nextCursor: null };
}
