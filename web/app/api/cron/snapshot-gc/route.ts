/**
 * Snapshot GC cron route — TECH-2676 §Acceptance #5.
 *
 *   POST /api/cron/snapshot-gc
 *     -> 200 { removedCount, removedIds }
 *     Runs `sweepRetiredSnapshots(new Date())` over the default 7-day window;
 *     idempotent over an already-clean DB. MVP: cron registration is
 *     deferred — manual POST is acceptable.
 *
 * Capability: `gc.trigger` (seeded under DEC-A33). Gated upstream by
 * `proxy.ts` via the route-meta map; this handler also performs an explicit
 * session check so the vitest direct-invoke harness fails closed.
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2676 §Plan Digest
 */

import { type NextRequest, NextResponse } from "next/server";

import { getSessionUser } from "@/lib/auth/get-session";
import { loadCapabilitiesForRole } from "@/lib/auth/capabilities";
import {
  catalogJsonError,
  responseFromPostgresError,
} from "@/lib/catalog/catalog-api-errors";
import { sweepRetiredSnapshots } from "@/lib/snapshot/gc-sweep";

export const dynamic = "force-dynamic";
export const routeMeta = {
  POST: { requires: "gc.trigger" },
} as const;

export type SnapshotGcResponse = {
  removedCount: number;
  removedIds: string[];
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

    const caps = await loadCapabilitiesForRole(session.role);
    if (!caps.has("gc.trigger")) {
      return NextResponse.json(
        { error: "gc.trigger capability required", code: "forbidden" },
        { status: 403 },
      );
    }

    const result = await sweepRetiredSnapshots(new Date());
    const body: SnapshotGcResponse = {
      removedCount: result.removedCount,
      removedIds: result.removedIds,
    };
    return NextResponse.json({ ok: true, data: body }, { status: 200 });
  } catch (e) {
    if (
      e instanceof Error &&
      e.message === "DATABASE_URL not set — required for DB access."
    ) {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "snapshot-gc",
      });
    }
    return responseFromPostgresError(e, "Snapshot GC sweep failed");
  }
}
