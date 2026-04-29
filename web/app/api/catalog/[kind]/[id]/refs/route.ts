/**
 * Generic catalog refs list API (TECH-3408 / Stage 14.4).
 *
 *  GET /api/catalog/[kind]/[id]/refs?cursor=<opaque>&limit=<1..100>&side=<incoming|outgoing>
 *    -> 200 { ok: true, data: { incoming: {rows, nextCursor}, outgoing: {rows, nextCursor} } }
 *    -> 400 invalid_kind | invalid_id | invalid_cursor
 *
 * Read-only paginated `catalog_ref_edge` list per `(kind, entity_id)`.
 * Empty entity returns an empty page (200) — matches list-endpoint convention.
 *
 * `side` query param scopes a follow-up cursor request to one column
 * (`incoming` or `outgoing`); default unspecified returns both sides.
 *
 * @see ia/projects/asset-pipeline/stage-14.4 — TECH-3408 §Plan Digest
 * @see web/lib/repos/refs-repo.ts — pagination engine
 */
import { type NextRequest, NextResponse } from "next/server";

import {
  catalogJsonError,
  responseFromPostgresError,
} from "@/lib/catalog/catalog-api-errors";
import {
  InvalidCursorError,
  listIncomingRefs,
  listOutgoingRefs,
  type ListRefsResult,
} from "@/lib/repos/refs-repo";
import type { CatalogKind } from "@/lib/refs/types";

export const dynamic = "force-dynamic";

type Ctx = { params: Promise<{ kind: string; id: string }> };

const VALID_KINDS: ReadonlySet<string> = new Set([
  "sprite",
  "asset",
  "button",
  "panel",
  "pool",
  "token",
  "archetype",
  "audio",
]);

const EMPTY_PAGE: ListRefsResult = { rows: [], nextCursor: null };

function parseLimit(raw: string | null): number | null {
  if (raw == null) return null;
  const n = Number.parseInt(raw, 10);
  if (Number.isNaN(n)) return null;
  return n;
}

function parseSide(raw: string | null): "incoming" | "outgoing" | null {
  if (raw == null) return null;
  if (raw === "incoming" || raw === "outgoing") return raw;
  return null;
}

export async function GET(request: NextRequest, ctx: Ctx) {
  const { kind, id } = await ctx.params;
  if (!VALID_KINDS.has(kind)) {
    return catalogJsonError(400, "bad_request", "Invalid kind", {
      details: { kind },
    });
  }
  if (!/^\d+$/.test(id)) {
    return catalogJsonError(400, "bad_request", "Invalid id", {
      details: { id },
    });
  }
  const url = new URL(request.url);
  const cursor = url.searchParams.get("cursor");
  const limit = parseLimit(url.searchParams.get("limit"));
  const sideRaw = url.searchParams.get("side");
  const side = parseSide(sideRaw);
  if (sideRaw != null && side == null) {
    return catalogJsonError(400, "bad_request", "Invalid side", {
      details: { side: sideRaw },
    });
  }
  try {
    const k = kind as CatalogKind;
    let incoming: ListRefsResult = EMPTY_PAGE;
    let outgoing: ListRefsResult = EMPTY_PAGE;
    if (side == null) {
      [incoming, outgoing] = await Promise.all([
        listIncomingRefs(k, id, cursor, limit),
        listOutgoingRefs(k, id, cursor, limit),
      ]);
    } else if (side === "incoming") {
      incoming = await listIncomingRefs(k, id, cursor, limit);
    } else {
      outgoing = await listOutgoingRefs(k, id, cursor, limit);
    }
    return NextResponse.json(
      { ok: true, data: { incoming, outgoing } },
      { status: 200 },
    );
  } catch (e) {
    if (e instanceof InvalidCursorError) {
      return catalogJsonError(400, "bad_request", "Invalid cursor", {
        details: { cursor },
      });
    }
    if (
      e instanceof Error &&
      e.message === "DATABASE_URL not set — required for DB access."
    ) {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "refs-list",
      });
    }
    return responseFromPostgresError(e, "Refs list failed");
  }
}
