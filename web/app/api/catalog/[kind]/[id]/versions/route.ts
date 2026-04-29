/**
 * Generic catalog versions list API (TECH-3222 / Stage 14.2).
 *
 *  GET /api/catalog/[kind]/[id]/versions?cursor=<opaque>&limit=<1..100>
 *    -> 200 { ok: true, data: { rows: EntityVersionRow[], nextCursor: string|null } }
 *    -> 400 invalid_kind | invalid_id | invalid_cursor
 *
 * Read-only paginated `entity_version` history per `(kind, entity_id)`.
 * Empty entity returns an empty page (200) — matches list-endpoint convention.
 * Mirrors the existing archetype-specific route at
 * `web/app/api/catalog/archetypes/[slug]/versions/route.ts` (slug-keyed)
 * with a generic id-keyed surface that all 8 catalog kinds share.
 *
 * @see ia/projects/asset-pipeline/stage-14.2 — TECH-3222 §Plan Digest
 * @see web/lib/repos/history-repo.ts — pagination engine
 */
import { type NextRequest, NextResponse } from "next/server";

import {
  catalogJsonError,
  responseFromPostgresError,
} from "@/lib/catalog/catalog-api-errors";
import {
  InvalidCursorError,
  listVersions,
} from "@/lib/repos/history-repo";
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

function parseLimit(raw: string | null): number | null {
  if (raw == null) return null;
  const n = Number.parseInt(raw, 10);
  if (Number.isNaN(n)) return null;
  return n;
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
  try {
    const out = await listVersions(kind as CatalogKind, id, cursor, limit);
    return NextResponse.json({ ok: true, data: out }, { status: 200 });
  } catch (e) {
    if (e instanceof InvalidCursorError) {
      return catalogJsonError(400, "bad_request", "Invalid cursor", {
        details: { cursor },
      });
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "versions-list",
      });
    }
    return responseFromPostgresError(e, "Versions list failed");
  }
}
