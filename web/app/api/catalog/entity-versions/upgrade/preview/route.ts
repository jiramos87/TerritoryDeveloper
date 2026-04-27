/**
 * Entity-version upgrade preview (TECH-2462 / Stage 11.1).
 *
 *  GET /api/catalog/entity-versions/upgrade/preview
 *      ?source_version_id={id}&target_archetype_version_id={id}
 *    -> 200 { before: object, after: object, warnings: MigrationWarning[] }
 *
 * Read-only twin of the upgrade route — applies the migration runner against
 * the hint chain so the UI can render a side-by-side diff before commit.
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2462 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { getSql } from "@/lib/db/client";
import { applyMigration, type MigrationWarning } from "@/lib/archetype/migration-runner";
import type { MigrationHint } from "@/lib/archetype/migration-hint-validator";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.create" },
} as const;

type EntityVersionRow = {
  id: string;
  entity_id: string;
  archetype_version_id: string | null;
  params_json: Record<string, unknown>;
};

type ArchetypeVersionRow = {
  id: string;
  entity_id: string;
  version_number: number;
  parent_version_id: string | null;
  migration_hint_json: MigrationHint | null;
};

export async function GET(request: NextRequest) {
  const { searchParams } = new URL(request.url);
  const evId = searchParams.get("source_version_id");
  const targetAvId = searchParams.get("target_archetype_version_id");
  if (!evId || !/^\d+$/.test(evId)) {
    return catalogJsonError(400, "bad_request", "source_version_id must be a numeric string");
  }
  if (!targetAvId || !/^\d+$/.test(targetAvId)) {
    return catalogJsonError(
      400,
      "bad_request",
      "target_archetype_version_id must be a numeric string",
    );
  }
  try {
    const sql = getSql();
    const evIdNum = Number.parseInt(evId, 10);
    const targetAvIdNum = Number.parseInt(targetAvId, 10);

    const srcRows = (await sql`
      select id::text as id,
             entity_id::text as entity_id,
             archetype_version_id::text as archetype_version_id,
             params_json
        from entity_version
       where id = ${evIdNum}
       limit 1
    `) as unknown as EntityVersionRow[];
    if (srcRows.length === 0) {
      return catalogJsonError(404, "not_found", "Source entity_version not found");
    }
    const src = srcRows[0]!;
    if (src.archetype_version_id == null) {
      return catalogJsonError(400, "bad_request", "Source entity_version has no archetype pin");
    }

    const srcAvIdNum = Number.parseInt(src.archetype_version_id, 10);
    const avRows = (await sql`
      select id::text as id,
             entity_id::text as entity_id,
             version_number,
             parent_version_id::text as parent_version_id,
             migration_hint_json
        from entity_version
       where id in (${srcAvIdNum}, ${targetAvIdNum})
    `) as unknown as ArchetypeVersionRow[];
    const srcAv = avRows.find((r) => r.id === src.archetype_version_id) ?? null;
    const targetAv = avRows.find((r) => r.id === String(targetAvIdNum)) ?? null;
    if (srcAv == null || targetAv == null) {
      return catalogJsonError(404, "not_found", "Archetype version not found");
    }
    if (srcAv.entity_id !== targetAv.entity_id) {
      return catalogJsonError(
        400,
        "bad_request",
        "target archetype version belongs to a different archetype",
      );
    }
    if (Number(targetAv.version_number) <= Number(srcAv.version_number)) {
      return catalogJsonError(
        400,
        "bad_request",
        "target version_number must exceed source version_number",
      );
    }

    // Walk parent chain target -> source, collecting hints in chronological order.
    const hintChain: MigrationHint[] = [];
    const visited = new Set<string>();
    let cursor: ArchetypeVersionRow | null = targetAv;
    while (cursor != null && cursor.id !== srcAv.id) {
      if (visited.has(cursor.id)) {
        return catalogJsonError(400, "bad_request", "archetype version chain has a cycle");
      }
      visited.add(cursor.id);
      if (cursor.migration_hint_json != null) {
        hintChain.unshift(cursor.migration_hint_json);
      }
      const parentId = cursor.parent_version_id;
      if (parentId == null) break;
      if (parentId === srcAv.id) {
        cursor = srcAv;
        break;
      }
      const next = (await sql`
        select id::text as id,
               entity_id::text as entity_id,
               version_number,
               parent_version_id::text as parent_version_id,
               migration_hint_json
          from entity_version
         where id = ${Number.parseInt(parentId, 10)}
         limit 1
      `) as unknown as ArchetypeVersionRow[];
      cursor = next[0] ?? null;
    }
    if (cursor == null || cursor.id !== srcAv.id) {
      return catalogJsonError(
        400,
        "bad_request",
        "target archetype version is not a descendant of source",
      );
    }

    let working = src.params_json;
    const warnings: MigrationWarning[] = [];
    for (const hint of hintChain) {
      const out = applyMigration(working, hint);
      working = out.params;
      for (const w of out.warnings) warnings.push(w);
    }
    return NextResponse.json(
      { ok: true, data: { before: src.params_json, after: working, warnings } },
      { status: 200 },
    );
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "entity-version-upgrade-preview",
      });
    }
    return responseFromPostgresError(e, "Upgrade preview failed");
  }
}
