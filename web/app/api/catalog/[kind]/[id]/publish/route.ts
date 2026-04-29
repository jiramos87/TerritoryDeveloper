/**
 * Generic catalog publish API (TECH-2571 / Stage 12.1).
 *
 *  POST /api/catalog/[kind]/[id]/publish
 *    body: { versionId: string; justification?: string }
 *    -> 200 { version_id, status: 'published' }
 *    -> 409 conflict — Layer 1 / Layer 2 lint block rows present
 *    -> 400 bad_request — missing/empty justification with warn rows present
 *    -> 400 bad_request — unknown kind / non-numeric id / malformed body
 *
 * Wraps `withAudit` (single tx) → runs `runLayer1` + `runLayer2` →
 * aggregates → enforces DEC-A30 hard-gate / soft-lint contract → freezes
 * `entity_version` row + bumps `catalog_entity.current_published_version_id`
 * (matches archetype `publishVersion` pattern) → emits `audit_log` row with
 * `lint_summary` + `warn_justification`.
 *
 * Idempotent re-publish (status already 'published') returns 200 without a
 * second mutation or audit row, mirroring `publishVersion` archetype repo.
 *
 * Archetype slug+migration-hint route at
 * `web/app/api/catalog/archetypes/[slug]/versions/[versionId]/publish/route.ts`
 * stays in place — slug+hint validation is archetype-specific.
 *
 * @see ia/projects/asset-pipeline/stage-12.1 — TECH-2571 §Plan Digest
 */

import { type NextRequest, NextResponse } from "next/server";

import { withAudit } from "@/lib/audit/with-audit";
import {
  catalogJsonError,
  responseFromPostgresError,
} from "@/lib/catalog/catalog-api-errors";
import { aggregateLintResults, runLayer2 } from "@/lib/lint/cross-entity";
import { runLayer1 } from "@/lib/lint/runner";
import { buildEdgesForVersion } from "@/lib/refs/edge-builder";
import type { CatalogKind } from "@/lib/refs/types";
import { enqueueSnapshotRebuild } from "@/lib/snapshot/enqueue";

export const dynamic = "force-dynamic";
export const routeMeta = {
  POST: { requires: "catalog.entity.edit" },
} as const;

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

class PublishLintBlockError extends Error {
  constructor(public details: unknown) {
    super("conflict: publish blocked by lint block rows");
  }
}

class PublishWarnJustificationMissing extends Error {
  constructor() {
    super("bad_request: justification required when warn rows are present");
  }
}

type PublishBody = {
  versionId: string;
  justification?: string;
};

/**
 * Pure body validator — exported for unit testing. Returns `{ ok: true, body }`
 * when shape passes, `{ ok: false, reason }` otherwise.
 */
export function validatePublishBody(
  raw: unknown,
):
  | { ok: true; body: PublishBody }
  | { ok: false; reason: string } {
  if (raw === null || typeof raw !== "object") {
    return { ok: false, reason: "body must be an object" };
  }
  const obj = raw as Record<string, unknown>;
  const versionId = obj.versionId;
  if (typeof versionId !== "string" || !/^\d+$/.test(versionId)) {
    return { ok: false, reason: "versionId must be a numeric string" };
  }
  const justification = obj.justification;
  if (justification !== undefined && typeof justification !== "string") {
    return { ok: false, reason: "justification must be a string when present" };
  }
  // Reject unknown keys for forward-compat hygiene.
  for (const key of Object.keys(obj)) {
    if (key !== "versionId" && key !== "justification") {
      return { ok: false, reason: `unknown field: ${key}` };
    }
  }
  return {
    ok: true,
    body: {
      versionId,
      justification: typeof justification === "string" ? justification : undefined,
    },
  };
}

export async function POST(request: NextRequest, ctx: Ctx) {
  const { kind, id } = await ctx.params;

  if (!VALID_KINDS.has(kind)) {
    return catalogJsonError(400, "bad_request", `unknown kind: ${kind}`);
  }
  if (!/^\d+$/.test(id)) {
    return catalogJsonError(400, "bad_request", "id must be numeric");
  }

  let rawBody: unknown;
  try {
    rawBody = await request.json();
  } catch {
    return catalogJsonError(400, "bad_request", "body must be valid JSON");
  }
  const validated = validatePublishBody(rawBody);
  if (!validated.ok) {
    return catalogJsonError(400, "bad_request", validated.reason);
  }
  const { versionId, justification } = validated.body;

  try {
    const wrapped = withAudit(async (_req, { emit, sql }) => {
      // Run both lint layers + aggregate by severity.
      const layer1 = await runLayer1(kind, id, versionId, sql);
      const layer2 = await runLayer2(kind, id, versionId, sql);
      const results = aggregateLintResults(layer1, layer2);

      if (results.block.length > 0) {
        throw new PublishLintBlockError(results.block);
      }

      const trimmedJustification = (justification ?? "").trim();
      if (results.warn.length > 0 && trimmedJustification.length === 0) {
        throw new PublishWarnJustificationMissing();
      }

      // Lock + verify version belongs to entity, flip status if draft.
      const idNum = Number.parseInt(id, 10);
      const versionIdNum = Number.parseInt(versionId, 10);

      const locked = (await sql`
        select id, entity_id::text as entity_id, status
        from entity_version
        where id = ${versionIdNum}
        for update
      `) as Array<{
        id: number;
        entity_id: string;
        status: "draft" | "published";
      }>;

      if (locked.length === 0) {
        throw new Error("notfound: version not found");
      }
      if (locked[0].entity_id !== String(idNum)) {
        throw new Error("notfound: version does not belong to entity");
      }

      const alreadyPublished = locked[0].status === "published";

      if (!alreadyPublished) {
        await sql`
          update entity_version
          set status = 'published', updated_at = now()
          where id = ${versionIdNum}
        `;
        await sql`
          update catalog_entity
          set current_published_version_id = ${versionIdNum},
              slug_frozen_at = coalesce(slug_frozen_at, now())
          where id = ${idNum}
        `;
        // TECH-3003 — Stage 14.1 publish hook: materialize cross-entity
        // ref edges (DEC-A37 + DEC-A42) before the snapshot job is enqueued.
        // Builder runs inside the same `withAudit` tx so DELETE+INSERT share
        // rollback scope; snapshot rebuild reads materialized edges so
        // ordering matters (builder commits first).
        await buildEdgesForVersion(
          kind as CatalogKind,
          idNum,
          versionIdNum,
          sql,
        );
        // TECH-2674 — auto-enqueue snapshot rebuild after every successful
        // publish freeze. Runs inside the same tx as `entity_version` freeze
        // + audit emit so any rollback drops the queued job too.
        await enqueueSnapshotRebuild(sql, {
          trigger: "publish",
          source_kind: kind,
          source_entity_id: id,
          source_version_id: versionId,
        });
        await emit(
          `catalog.${kind}.publish`,
          "entity_version",
          versionId,
          {
            lint_summary: {
              block: 0,
              warn: results.warn.length,
              info: results.info.length,
            },
            warn_justification:
              results.warn.length > 0 ? trimmedJustification : null,
          },
        );
      }

      return {
        status: 200,
        data: {
          version_id: versionId,
          status: "published" as const,
        },
      };
    });
    return await wrapped(request);
  } catch (e) {
    if (e instanceof PublishLintBlockError) {
      return catalogJsonError(
        409,
        "conflict",
        "Publish blocked: lint block rows present",
        { details: e.details },
      );
    }
    if (e instanceof PublishWarnJustificationMissing) {
      return catalogJsonError(
        400,
        "bad_request",
        "justification required when warn rows are present",
      );
    }
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(
        404,
        "not_found",
        e.message.replace(/^notfound:\s*/i, ""),
      );
    }
    return responseFromPostgresError(e, "Publish failed");
  }
}

// Re-export for any caller that wants to inspect dynamic config.
export { NextResponse };
