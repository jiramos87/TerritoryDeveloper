/**
 * Single-version diff API (TECH-3301 / Stage 14.3).
 *
 *   GET /api/catalog/[kind]/[id]/diff/[versionId]
 *     -> 200 { ok: true, data: { from: EntityVersionRow | null, to: EntityVersionRow, diff: KindDiff } }
 *     -> 400 bad_request (invalid kind / non-numeric versionId)
 *     -> 404 not_found (target row missing for kind)
 *     -> 500 internal (Postgres errors mapped via responseFromPostgresError)
 *
 * Loads the target `entity_version` row by `versionId`, then loads the row
 * referenced by `parent_version_id` (or treats `from = {}` when null — root
 * version), and returns `diffVersions(kind, from.params_json, to.params_json)`.
 *
 * Route segment `[id]` is opaque here — entity-id correlation is not enforced
 * because `versionId` already keys the target row uniquely; `kind` filter on
 * the join scopes the row to the matching catalog kind.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3301 §Plan Digest
 * @see web/lib/diff/diff-versions.ts — diff engine
 * @see db/migrations/0021_catalog_spine.sql lines 53-68 — entity_version
 */
import { type NextRequest, NextResponse } from "next/server";

import {
  catalogJsonError,
  responseFromPostgresError,
} from "@/lib/catalog/catalog-api-errors";
import { getSql } from "@/lib/db/client";
import { diffVersions } from "@/lib/diff/diff-versions";
import type { CatalogKind } from "@/lib/refs/types";
import type { EntityVersionRow } from "@/lib/repos/history-repo";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.read" },
} as const;

type Ctx = { params: Promise<{ kind: string; id: string; versionId: string }> };

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

interface VersionRowWithParams extends EntityVersionRow {
  params_json: Record<string, unknown>;
}

async function loadVersionRow(
  versionId: string,
  kind: string,
): Promise<VersionRowWithParams | null> {
  const sql = getSql();
  const idNum = Number.parseInt(versionId, 10);
  const rows = (await sql`
    select
      ev.id::text                       as id,
      ev.entity_id::text                as entity_id,
      ev.version_number,
      ev.status,
      ev.created_at,
      ev.parent_version_id::text        as parent_version_id,
      ev.archetype_version_id::text     as archetype_version_id,
      ev.params_json                    as params_json
    from entity_version ev
    join catalog_entity ce on ce.id = ev.entity_id
    where ev.id = ${idNum} and ce.kind = ${kind}
    limit 1
  `) as unknown as Array<Record<string, unknown>>;
  const r = rows[0];
  if (r == null) return null;
  return {
    id: r.id as string,
    entity_id: r.entity_id as string,
    version_number: Number(r.version_number),
    status: r.status as EntityVersionRow["status"],
    created_at:
      r.created_at instanceof Date
        ? r.created_at.toISOString()
        : (r.created_at as string),
    parent_version_id: (r.parent_version_id as string | null) ?? null,
    archetype_version_id: (r.archetype_version_id as string | null) ?? null,
    params_json:
      (r.params_json as Record<string, unknown> | null) ?? ({} as Record<string, unknown>),
  };
}

async function loadParentRow(
  parentVersionId: string | null,
): Promise<VersionRowWithParams | null> {
  if (parentVersionId == null) return null;
  const sql = getSql();
  const idNum = Number.parseInt(parentVersionId, 10);
  if (Number.isNaN(idNum)) return null;
  const rows = (await sql`
    select
      ev.id::text                       as id,
      ev.entity_id::text                as entity_id,
      ev.version_number,
      ev.status,
      ev.created_at,
      ev.parent_version_id::text        as parent_version_id,
      ev.archetype_version_id::text     as archetype_version_id,
      ev.params_json                    as params_json
    from entity_version ev
    where ev.id = ${idNum}
    limit 1
  `) as unknown as Array<Record<string, unknown>>;
  const r = rows[0];
  if (r == null) return null;
  return {
    id: r.id as string,
    entity_id: r.entity_id as string,
    version_number: Number(r.version_number),
    status: r.status as EntityVersionRow["status"],
    created_at:
      r.created_at instanceof Date
        ? r.created_at.toISOString()
        : (r.created_at as string),
    parent_version_id: (r.parent_version_id as string | null) ?? null,
    archetype_version_id: (r.archetype_version_id as string | null) ?? null,
    params_json:
      (r.params_json as Record<string, unknown> | null) ?? ({} as Record<string, unknown>),
  };
}

export async function GET(_request: NextRequest, ctx: Ctx) {
  const { kind, versionId } = await ctx.params;
  if (!VALID_KINDS.has(kind)) {
    return catalogJsonError(400, "bad_request", "Invalid kind", {
      details: { kind },
    });
  }
  if (!/^\d+$/.test(versionId)) {
    return catalogJsonError(400, "bad_request", "Invalid versionId", {
      details: { versionId },
    });
  }
  try {
    const target = await loadVersionRow(versionId, kind);
    if (target == null) {
      return catalogJsonError(404, "not_found", "Version not found", {
        details: { versionId, kind },
      });
    }
    const parent = await loadParentRow(target.parent_version_id);
    const fromParams = parent?.params_json ?? {};
    const diff = diffVersions(kind as CatalogKind, fromParams, target.params_json);
    // Strip params_json from response rows — clients only need metadata + diff.
    const stripParams = (
      r: VersionRowWithParams | null,
    ): EntityVersionRow | null => {
      if (r == null) return null;
      const { params_json: _ignored, ...rest } = r;
      return rest;
    };
    return NextResponse.json(
      {
        ok: true,
        data: {
          from: stripParams(parent),
          to: stripParams(target)!,
          diff,
        },
      },
      { status: 200 },
    );
  } catch (e) {
    if (
      e instanceof Error &&
      e.message === "DATABASE_URL not set — required for DB access."
    ) {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "diff-route",
      });
    }
    return responseFromPostgresError(e, "Diff route failed");
  }
}
