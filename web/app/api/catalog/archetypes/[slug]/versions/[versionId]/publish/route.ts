/**
 * Archetype version publish (TECH-2461 / Stage 11.1).
 *
 *  POST /api/catalog/archetypes/[slug]/versions/[versionId]/publish
 *    -> 200 { version: CatalogArchetypeVersion }
 *    -> 409 { code: "conflict", details: HintError[] } when hint invalid
 *
 * Validates `migration_hint_json` against the schema diff vs parent published
 * version, flips status draft->published, locks slug on first publish, all in
 * one TX (DEC-A38 + DEC-A46).
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2461 §Plan Digest
 */
import { type NextRequest, NextResponse } from "next/server";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import {
  getArchetypeBySlug,
  getVersion,
  publishVersion,
} from "@/lib/catalog/archetype-repo";
import { diffSchemas } from "@/lib/archetype/diff-schemas";
import {
  validateMigrationHint,
  type MigrationHint,
} from "@/lib/archetype/migration-hint-validator";
import type { JsonSchemaNode } from "@/lib/json-schema-form/types";

export const dynamic = "force-dynamic";
export const routeMeta = {
  POST: { requires: "catalog.entity.edit" },
} as const;

type Ctx = { params: Promise<{ slug: string; versionId: string }> };

class HintConflictError extends Error {
  constructor(public details: Array<{ path: string; message: string }>) {
    super("conflict: migration hint invalid");
  }
}

export async function POST(request: NextRequest, ctx: Ctx) {
  const { slug, versionId } = await ctx.params;
  try {
    const wrapped = withAudit(async (_req, { emit, sql }) => {
      const arch = await getArchetypeBySlug(slug, sql);
      if (arch == null) throw new Error("notfound: Archetype not found");

      const draft = await getVersion(arch.entity_id, versionId, sql);
      if (draft == null) throw new Error("notfound: Version not found");

      // Hint validation only fires when a parent version exists. First publish
      // (no parent) is always pure-additive — no hint required.
      if (draft.parent_version_id != null) {
        const parent = await getVersion(arch.entity_id, draft.parent_version_id, sql);
        if (parent != null) {
          const diff = diffSchemas(
            parent.params_json as JsonSchemaNode,
            draft.params_json as JsonSchemaNode,
          );
          const result = validateMigrationHint(
            diff,
            (draft.migration_hint_json ?? {}) as MigrationHint,
            draft.params_json as JsonSchemaNode,
          );
          if (!result.ok) {
            throw new HintConflictError(result.errors);
          }
        }
      }

      const out = await publishVersion(arch.entity_id, versionId, sql);
      if (out.ok === "notfound") throw new Error("notfound: Version not found");
      if (out.ok === "validation") throw new Error(`validation: ${out.reason}`);
      if (out.ok === "conflict") throw new Error(`conflict: ${out.reason}`);

      await emit("catalog.archetype.version.publish", "entity_version", out.data.version_id, {
        slug,
        version_id: out.data.version_id,
      });
      return { status: 200, data: { version: out.data } };
    });
    return await wrapped(request);
  } catch (e) {
    if (e instanceof HintConflictError) {
      return NextResponse.json(
        {
          ok: "error",
          error: {
            code: "conflict",
            message: "Migration hint validation failed",
            details: e.details,
          },
        },
        { status: 409 },
      );
    }
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      return catalogJsonError(400, "bad_request", e.message.replace(/^validation:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", e.message.replace(/^notfound:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      const reason = e.message.replace(/^conflict:\s*/i, "");
      return catalogJsonError(409, "conflict", `Publish blocked: ${reason}`);
    }
    return responseFromPostgresError(e, "Publish failed");
  }
}
