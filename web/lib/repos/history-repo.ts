/**
 * History repo — paginated `entity_version` list for a given (kind, entity_id).
 *
 * Stage 14.2 / TECH-3222 — drives `VersionsTab` (TECH-3223) + 8 kind detail
 * page wires (TECH-3224). Reads spine `entity_version` (Stage 1.1) joined to
 * `catalog_entity` (kind filter). Live schema carries no `author` column —
 * row shape is `{id, entity_id, version_number, status, created_at,
 * parent_version_id, archetype_version_id}`. Bigint columns serialized as
 * strings to match existing detail-payload conventions.
 *
 * Cursor: opaque base64 of JSON `{created_at, id}`. Keyset pagination on
 * `(created_at, id) < (cursor.created_at, cursor.id)` keeps page boundaries
 * deterministic when two rows share `created_at`.
 *
 * @see ia/projects/asset-pipeline/stage-14.2 — TECH-3222 §Plan Digest
 * @see db/migrations/0021_catalog_spine.sql lines 53-68 — entity_version
 */
import { getSql } from "@/lib/db/client";
import type { CatalogKind } from "@/lib/refs/types";

export type EntityVersionStatus = "draft" | "published";

export interface EntityVersionRow {
  id: string;
  entity_id: string;
  version_number: number;
  status: EntityVersionStatus;
  created_at: string;
  parent_version_id: string | null;
  archetype_version_id: string | null;
}

export interface ListVersionsResult {
  rows: EntityVersionRow[];
  nextCursor: string | null;
}

const DEFAULT_LIMIT = 20;
const MAX_LIMIT = 100;
const MIN_LIMIT = 1;

interface DecodedCursor {
  created_at_us: string;
  id: string;
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

export function encodeCursor(row: { created_at_us: string; id: string }): string {
  const json = JSON.stringify({ created_at_us: row.created_at_us, id: row.id });
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
    typeof (parsed as { id?: unknown }).id !== "string" ||
    !/^-?\d+$/.test((parsed as { created_at_us: string }).created_at_us) ||
    !/^\d+$/.test((parsed as { id: string }).id)
  ) {
    throw new InvalidCursorError();
  }
  return parsed as DecodedCursor;
}

/**
 * Page through `entity_version` rows for a given `(kind, entityId)` pair.
 * Returns up to `limit` rows in `(created_at DESC, id DESC)` order plus an
 * opaque `nextCursor` when more rows are available. Empty entity → empty page
 * (no error).
 *
 * Cursor decode failures surface via `InvalidCursorError`; route handler maps
 * to HTTP 400 `invalid_cursor`.
 */
export async function listVersions(
  kind: CatalogKind,
  entityId: string,
  cursor: string | null,
  limit: number | null | undefined,
): Promise<ListVersionsResult> {
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
  // binding flakes at sub-ms precision under parallel load; integer keyset
  // bypasses the driver codec entirely. Mirrors `web/lib/repos/refs-repo.ts`.
  const rows = decodedCursor == null
    ? ((await sql`
        select
          ev.id::text                       as id,
          ev.entity_id::text                as entity_id,
          ev.version_number,
          ev.status,
          to_char(ev.created_at at time zone 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"') as created_at,
          (extract(epoch from ev.created_at) * 1000000)::bigint::text as created_at_us,
          ev.parent_version_id::text        as parent_version_id,
          ev.archetype_version_id::text     as archetype_version_id
        from entity_version ev
        join catalog_entity ce on ce.id = ev.entity_id
        where ce.kind = ${kind} and ev.entity_id = ${idNum}
        order by ev.created_at desc, ev.id desc
        limit ${fetchN}
      `) as unknown as Array<Record<string, unknown>>)
    : ((await sql`
        select
          ev.id::text                       as id,
          ev.entity_id::text                as entity_id,
          ev.version_number,
          ev.status,
          to_char(ev.created_at at time zone 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"') as created_at,
          (extract(epoch from ev.created_at) * 1000000)::bigint::text as created_at_us,
          ev.parent_version_id::text        as parent_version_id,
          ev.archetype_version_id::text     as archetype_version_id
        from entity_version ev
        join catalog_entity ce on ce.id = ev.entity_id
        where ce.kind = ${kind}
          and ev.entity_id = ${idNum}
          and ((extract(epoch from ev.created_at) * 1000000)::bigint, ev.id) <
              (${decodedCursor.created_at_us}::bigint, ${Number.parseInt(decodedCursor.id, 10)}::bigint)
        order by ev.created_at desc, ev.id desc
        limit ${fetchN}
      `) as unknown as Array<Record<string, unknown>>);

  interface InternalRow extends EntityVersionRow {
    created_at_us: string;
  }
  const mapped: InternalRow[] = rows.map((r) => ({
    id: r.id as string,
    entity_id: r.entity_id as string,
    version_number: Number(r.version_number),
    status: r.status as EntityVersionStatus,
    created_at:
      r.created_at instanceof Date
        ? r.created_at.toISOString()
        : (r.created_at as string),
    created_at_us: r.created_at_us as string,
    parent_version_id: (r.parent_version_id as string | null) ?? null,
    archetype_version_id: (r.archetype_version_id as string | null) ?? null,
  }));

  const stripUs = (r: InternalRow): EntityVersionRow => {
    const { created_at_us: _us, ...rest } = r;
    void _us;
    return rest;
  };

  if (mapped.length > limitClamped) {
    const truncated = mapped.slice(0, limitClamped);
    const last = truncated[truncated.length - 1]!;
    return {
      rows: truncated.map(stripUs),
      nextCursor: encodeCursor({ created_at_us: last.created_at_us, id: last.id }),
    };
  }
  return { rows: mapped.map(stripUs), nextCursor: null };
}
