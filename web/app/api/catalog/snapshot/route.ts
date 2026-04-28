/**
 * Snapshot API — manual export (POST) + history list (GET).
 *
 *   POST /api/catalog/snapshot
 *     -> 200 { snapshot_id, hash, manifest_path }
 *     Synchronous `exportSnapshot(authorUserId, false)` — MVP per spec §2.1 #3;
 *     worker-driven async dequeue is deferred. Author = current session user.
 *
 *   GET /api/catalog/snapshot?limit=20&cursor=<iso>
 *     -> 200 { items: SnapshotRow[], nextCursor: string | null }
 *     Cursor-on-`created_at` DESC (newest first); cursor is the URL-encoded
 *     ISO timestamp of the last row from the previous page (DEC-A48).
 *
 * Capability: `catalog.entity.edit` (gated upstream by `proxy.ts`).
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2674 §Plan Digest
 */

import { type NextRequest, NextResponse } from "next/server";

import { getSessionUser } from "@/lib/auth/get-session";
import {
  catalogJsonError,
  responseFromPostgresError,
} from "@/lib/catalog/catalog-api-errors";
import { getSql } from "@/lib/db/client";
import { exportSnapshot } from "@/lib/snapshot/export";

export const dynamic = "force-dynamic";
export const routeMeta = {
  POST: { requires: "catalog.entity.edit" },
  GET: { requires: "catalog.entity.edit" },
} as const;

const DEFAULT_LIMIT = 20;
const MAX_LIMIT = 100;

export type SnapshotRow = {
  id: string;
  hash: string;
  manifest_path: string;
  schema_version: number;
  status: "active" | "retired";
  entity_counts_json: Record<string, number>;
  created_at: string;
  retired_at: string | null;
  created_by: string | null;
};

export type SnapshotListResponse = {
  items: SnapshotRow[];
  nextCursor: string | null;
};

export type SnapshotPostResponse = {
  snapshot_id: string;
  hash: string;
  manifest_path: string;
};

export async function POST(_request: NextRequest) {
  try {
    const session = await getSessionUser();
    if (!session) {
      return NextResponse.json(
        { error: "no session user", code: "forbidden" },
        { status: 403 },
      );
    }

    const out = await exportSnapshot(session.id, { includeDrafts: false });
    const body: SnapshotPostResponse = {
      snapshot_id: out.snapshotId,
      hash: out.hash,
      manifest_path: out.manifestPath,
    };
    return NextResponse.json({ ok: true, data: body }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "snapshot-post",
      });
    }
    return responseFromPostgresError(e, "Snapshot export failed");
  }
}

/**
 * Pure cursor + limit parser — exported for unit tests. `cursor` must be a
 * URL-encoded ISO timestamp. Invalid values yield `{ ok: false, reason }`.
 */
export function parseListQuery(
  rawLimit: string | null,
  rawCursor: string | null,
):
  | { ok: true; limit: number; cursor: Date | null }
  | { ok: false; reason: string } {
  let limit = DEFAULT_LIMIT;
  if (rawLimit !== null) {
    const n = Number.parseInt(rawLimit, 10);
    if (!Number.isInteger(n) || n <= 0 || n > MAX_LIMIT) {
      return {
        ok: false,
        reason: `limit must be a positive integer ≤ ${MAX_LIMIT}`,
      };
    }
    limit = n;
  }
  let cursor: Date | null = null;
  if (rawCursor !== null && rawCursor !== "") {
    const parsed = new Date(rawCursor);
    if (Number.isNaN(parsed.getTime())) {
      return { ok: false, reason: "cursor must be a valid ISO timestamp" };
    }
    cursor = parsed;
  }
  return { ok: true, limit, cursor };
}

export async function GET(request: NextRequest) {
  // Use `new URL(request.url)` instead of `request.nextUrl` so the handler
  // also works under the vitest direct-invoke harness (plain `Request`).
  const params = new URL(request.url).searchParams;
  const parsed = parseListQuery(params.get("limit"), params.get("cursor"));
  if (!parsed.ok) {
    return catalogJsonError(400, "bad_request", parsed.reason);
  }
  const { limit, cursor } = parsed;

  try {
    const sql = getSql();
    const rows = (
      cursor === null
        ? ((await sql`
            select
              id::text as id,
              hash,
              manifest_path,
              schema_version,
              status::text as status,
              entity_counts_json,
              created_at,
              retired_at,
              created_by::text as created_by
            from catalog_snapshot
            order by created_at desc, id desc
            limit ${limit + 1}
          `) as unknown as Array<{
            id: string;
            hash: string;
            manifest_path: string;
            schema_version: number;
            status: "active" | "retired";
            entity_counts_json: Record<string, number>;
            created_at: Date;
            retired_at: Date | null;
            created_by: string | null;
          }>)
        : ((await sql`
            select
              id::text as id,
              hash,
              manifest_path,
              schema_version,
              status::text as status,
              entity_counts_json,
              created_at,
              retired_at,
              created_by::text as created_by
            from catalog_snapshot
            where created_at < ${cursor.toISOString()}
            order by created_at desc, id desc
            limit ${limit + 1}
          `) as unknown as Array<{
            id: string;
            hash: string;
            manifest_path: string;
            schema_version: number;
            status: "active" | "retired";
            entity_counts_json: Record<string, number>;
            created_at: Date;
            retired_at: Date | null;
            created_by: string | null;
          }>)
    );

    let nextCursor: string | null = null;
    let pageRows = rows;
    if (rows.length > limit) {
      pageRows = rows.slice(0, limit);
      const tail = pageRows[pageRows.length - 1]!;
      nextCursor = tail.created_at.toISOString();
    }

    const items: SnapshotRow[] = pageRows.map((r) => ({
      id: r.id,
      hash: r.hash,
      manifest_path: r.manifest_path,
      schema_version: r.schema_version,
      status: r.status,
      entity_counts_json: r.entity_counts_json,
      created_at: r.created_at.toISOString(),
      retired_at: r.retired_at === null ? null : r.retired_at.toISOString(),
      created_by: r.created_by,
    }));

    const body: SnapshotListResponse = { items, nextCursor };
    return NextResponse.json({ ok: true, data: body }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "snapshot-get",
      });
    }
    return responseFromPostgresError(e, "Snapshot list failed");
  }
}
