/**
 * Retire one `catalog_snapshot` row (TECH-2674 §Acceptance #5).
 *
 *   POST /api/catalog/snapshot/[id]/retire
 *     -> 200 { id, status: 'retired', retired_at, unchanged: false }
 *     -> 200 { id, status: 'retired', retired_at, unchanged: true } when row
 *        was already retired (idempotent retry-safe per spec).
 *     -> 404 not_found when row missing.
 *
 * Capability: `catalog.entity.edit`. Audit row emitted on the first flip
 * (not on idempotent re-calls).
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2674 §Plan Digest
 */

import { type NextRequest } from "next/server";

import { withAudit } from "@/lib/audit/with-audit";
import {
  catalogJsonError,
  responseFromPostgresError,
} from "@/lib/catalog/catalog-api-errors";

export const dynamic = "force-dynamic";
export const routeMeta = {
  POST: { requires: "catalog.entity.edit" },
} as const;

type Ctx = { params: Promise<{ id: string }> };

export type SnapshotRetireResponse = {
  id: string;
  status: "retired";
  retired_at: string;
  unchanged: boolean;
};

export async function POST(request: NextRequest, ctx: Ctx) {
  const { id } = await ctx.params;

  try {
    const wrapped = withAudit<SnapshotRetireResponse>(
      async (_req, { emit, sql }) => {
        const locked = (await sql`
          select id::text as id, status::text as status, retired_at
          from catalog_snapshot
          where id = ${id}::uuid
          for update
        `) as unknown as Array<{
          id: string;
          status: "active" | "retired";
          retired_at: Date | null;
        }>;

        if (locked.length === 0) {
          throw new Error("notfound: snapshot not found");
        }

        if (locked[0]!.status === "retired") {
          // Idempotent path — no mutation, no audit row.
          return {
            status: 200,
            data: {
              id: locked[0]!.id,
              status: "retired" as const,
              retired_at:
                locked[0]!.retired_at?.toISOString() ?? new Date().toISOString(),
              unchanged: true,
            },
          };
        }

        const flipped = (await sql`
          update catalog_snapshot
          set status = 'retired', retired_at = now()
          where id = ${id}::uuid
          returning id::text as id, retired_at
        `) as unknown as Array<{ id: string; retired_at: Date }>;

        await emit(
          "catalog.snapshot.retire",
          "catalog_snapshot",
          flipped[0]!.id,
          {},
        );

        return {
          status: 200,
          data: {
            id: flipped[0]!.id,
            status: "retired" as const,
            retired_at: flipped[0]!.retired_at.toISOString(),
            unchanged: false,
          },
        };
      },
    );
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(
        404,
        "not_found",
        e.message.replace(/^notfound:\s*/i, ""),
      );
    }
    if (
      e instanceof Error &&
      e.message === "DATABASE_URL not set — required for DB access."
    ) {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "snapshot-retire",
      });
    }
    return responseFromPostgresError(e, "Snapshot retire failed");
  }
}
