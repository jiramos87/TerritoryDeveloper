/**
 * Audio catalog list (TECH-1958).
 *
 * GET /api/catalog/audio?status=active|retired|all&limit=50&cursor=...
 *     -> { items, next_cursor }
 *
 * Capability: catalog.entity.create (gated upstream by proxy via
 * route-meta-map). Mirrors the sprite list shape so the UI can reuse
 * filter/cursor/error patterns verbatim.
 *
 * @see ia/projects/asset-pipeline/stage-9.1/TECH-1958.md
 */
import { NextResponse, type NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";
import {
  catalogJsonError,
  responseFromPostgresError,
} from "@/lib/catalog/catalog-api-errors";
import {
  listAudioEntities,
  type AudioListFilter,
} from "@/lib/db/audio-repo";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
} as const;

const DEFAULT_LIMIT = 50;
const MAX_LIMIT = 200;

export async function GET(request: NextRequest) {
  const params = request.nextUrl.searchParams;
  const statusRaw = params.get("status") ?? "active";
  if (statusRaw !== "active" && statusRaw !== "retired" && statusRaw !== "all") {
    return catalogJsonError(
      400,
      "bad_request",
      "status must be one of active|retired|all",
    );
  }
  const filter = statusRaw as AudioListFilter;
  const limitRaw = params.get("limit");
  let limit = DEFAULT_LIMIT;
  if (limitRaw !== null) {
    const n = Number.parseInt(limitRaw, 10);
    if (!Number.isInteger(n) || n <= 0 || n > MAX_LIMIT) {
      return catalogJsonError(
        400,
        "bad_request",
        `limit must be a positive integer ≤ ${MAX_LIMIT}`,
      );
    }
    limit = n;
  }
  const cursorRaw = params.get("cursor");
  if (cursorRaw !== null && !/^\d+$/.test(cursorRaw)) {
    return catalogJsonError(
      400,
      "bad_request",
      "cursor must be a non-negative integer string",
    );
  }
  try {
    const out = await listAudioEntities({ filter, limit, cursor: cursorRaw });
    return NextResponse.json({ ok: true, data: out }, { status: 200 });
  } catch (e) {
    if (
      e instanceof Error &&
      e.message === "DATABASE_URL not set — required for DB access."
    ) {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "audio-list",
      });
    }
    return responseFromPostgresError(e, "Audio list query failed");
  }
}

// Tree-shake guard.
void getSql;
