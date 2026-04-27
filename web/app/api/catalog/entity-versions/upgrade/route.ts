/**
 * Entity-version upgrade (TECH-2462 / Stage 11.1).
 *
 *  POST /api/catalog/entity-versions/upgrade
 *    body: { entity_version_id: string, target_archetype_version_id: string }
 *    -> 200 { new_version_id: string, warnings: MigrationWarning[] }
 *    -> 400 on downgrade, missing rows, type errors
 *
 * Re-pins a consumer entity_version onto a newer archetype version, applying
 * the chain of `migration_hint_json` rules through `applyMigration` to its
 * `params_json`. Inserts a fresh draft `entity_version` row keyed by next
 * `version_number`, parented at the source row, pinned at the target.
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2462 §Plan Digest
 */
import type { NextRequest } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { applyMigration, type MigrationWarning } from "@/lib/archetype/migration-runner";
import type { MigrationHint } from "@/lib/archetype/migration-hint-validator";

export const dynamic = "force-dynamic";
export const routeMeta = {
  POST: { requires: "catalog.entity.edit" },
} as const;

export type UpgradeBodyValidation =
  | { ok: true; entity_version_id: string; target_archetype_version_id: string }
  | { ok: false; reason: string };

/** Pure body validator (testable without the route runtime). */
export function validateUpgradeBody(body: unknown): UpgradeBodyValidation {
  if (typeof body !== "object" || body == null) {
    return { ok: false, reason: "body must be object" };
  }
  const evId = (body as { entity_version_id?: unknown }).entity_version_id;
  const targetAvId = (body as { target_archetype_version_id?: unknown })
    .target_archetype_version_id;
  if (typeof evId !== "string" || !/^\d+$/.test(evId)) {
    return { ok: false, reason: "entity_version_id must be a numeric string" };
  }
  if (typeof targetAvId !== "string" || !/^\d+$/.test(targetAvId)) {
    return { ok: false, reason: "target_archetype_version_id must be a numeric string" };
  }
  return { ok: true, entity_version_id: evId, target_archetype_version_id: targetAvId };
}

/**
 * Pure downgrade-block check: target version_number must exceed source. Equal
 * version numbers (same row) are blocked too.
 */
export function isDowngradeOrSame(
  sourceVersionNumber: number,
  targetVersionNumber: number,
): boolean {
  return targetVersionNumber <= sourceVersionNumber;
}

type EntityVersionRow = {
  id: string;
  entity_id: string;
  version_number: number;
  status: "draft" | "published";
  archetype_version_id: string | null;
  params_json: Record<string, unknown>;
  parent_version_id: string | null;
};

type ArchetypeVersionRow = {
  id: string;
  entity_id: string;
  version_number: number;
  parent_version_id: string | null;
  migration_hint_json: MigrationHint | null;
};

export async function POST(request: NextRequest) {
  try {
    const wrapped = withAudit(async (req, { emit, sql }) => {
      let body: unknown;
      try {
        body = await req.json();
      } catch {
        throw new Error("validation: Invalid JSON body");
      }
      const v = validateUpgradeBody(body);
      if (!v.ok) throw new Error(`validation: ${v.reason}`);
      const evIdNum = Number.parseInt(v.entity_version_id, 10);
      const targetAvIdNum = Number.parseInt(v.target_archetype_version_id, 10);
      const evId = v.entity_version_id;
      const targetAvId = v.target_archetype_version_id;

      // Lock source entity_version row.
      const srcRows = (await sql`
        select id::text as id,
               entity_id::text as entity_id,
               version_number,
               status,
               archetype_version_id::text as archetype_version_id,
               params_json,
               parent_version_id::text as parent_version_id
          from entity_version
         where id = ${evIdNum}
         for update
      `) as unknown as EntityVersionRow[];
      if (srcRows.length === 0) throw new Error("notfound: Source entity_version not found");
      const src = srcRows[0]!;
      if (src.archetype_version_id == null) {
        throw new Error("validation: Source entity_version has no archetype pin");
      }
      const srcAvIdNum = Number.parseInt(src.archetype_version_id, 10);

      // Resolve archetype version chain — both rows must share the same archetype entity_id.
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
        throw new Error("notfound: Archetype version not found");
      }
      if (srcAv.entity_id !== targetAv.entity_id) {
        throw new Error("validation: target archetype version belongs to a different archetype");
      }
      if (isDowngradeOrSame(Number(srcAv.version_number), Number(targetAv.version_number))) {
        throw new Error("validation: target version_number must exceed source version_number");
      }

      // Walk parent chain from target back to source, collecting hints in order.
      const hintChain: MigrationHint[] = [];
      const visited = new Set<string>();
      let cursorId: string | null = targetAv.id;
      while (cursorId != null && cursorId !== srcAv.id) {
        if (visited.has(cursorId)) {
          throw new Error("validation: archetype version chain has a cycle");
        }
        visited.add(cursorId);
        let cursorRow: ArchetypeVersionRow | null;
        if (cursorId === targetAv.id) {
          cursorRow = targetAv;
        } else {
          const fetched = (await sql`
                select id::text as id,
                       entity_id::text as entity_id,
                       version_number,
                       parent_version_id::text as parent_version_id,
                       migration_hint_json
                  from entity_version
                 where id = ${Number.parseInt(cursorId, 10)}
                 limit 1
              `) as unknown as ArchetypeVersionRow[];
          cursorRow = fetched[0] ?? null;
        }
        if (cursorRow == null) {
          throw new Error("notfound: Archetype version chain link missing");
        }
        if (cursorRow.migration_hint_json != null) {
          hintChain.unshift(cursorRow.migration_hint_json);
        }
        cursorId = cursorRow.parent_version_id;
      }
      if (cursorId == null) {
        throw new Error("validation: target archetype version is not a descendant of source");
      }

      // Apply hint chain in order — earliest hint first.
      let working = src.params_json;
      const warnings: MigrationWarning[] = [];
      for (const hint of hintChain) {
        const out = applyMigration(working, hint);
        working = out.params;
        for (const w of out.warnings) warnings.push(w);
      }

      // Insert new draft entity_version row pinned at target archetype version.
      const eIdNum = Number.parseInt(src.entity_id, 10);
      const maxRow = (await sql`
        select coalesce(max(version_number), 0)::int as m
          from entity_version where entity_id = ${eIdNum}
      `) as unknown as Array<{ m: number }>;
      const nextN = Number(maxRow[0]!.m) + 1;
      const inserted = (await sql`
        insert into entity_version (
          entity_id, version_number, status,
          archetype_version_id, params_json, parent_version_id
        )
        values (
          ${eIdNum}, ${nextN}, 'draft',
          ${targetAvIdNum},
          ${sql.json(working as Parameters<typeof sql.json>[0])},
          ${evIdNum}
        )
        returning id::text as id
      `) as unknown as Array<{ id: string }>;
      const newId = inserted[0]!.id;

      await emit("catalog.entity.version.upgrade", "entity_version", newId, {
        source_entity_version_id: evId,
        target_archetype_version_id: targetAvId,
        warnings_count: warnings.length,
      });
      return { status: 200, data: { new_version_id: newId, warnings } };
    });
    return await wrapped(request);
  } catch (e) {
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      return catalogJsonError(400, "bad_request", e.message.replace(/^validation:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", e.message.replace(/^notfound:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      return catalogJsonError(409, "conflict", e.message.replace(/^conflict:\s*/i, ""));
    }
    return responseFromPostgresError(e, "Entity version upgrade failed");
  }
}
