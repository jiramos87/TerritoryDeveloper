/**
 * Sprite promote — copy variant blob to Assets/Sprites/Generated/{slug}.png
 * + write detail columns (TECH-1675).
 *
 * POST /api/catalog/sprites/[slug]/promote
 *      body: { run_id, variant_idx }
 *
 * Atomic: the blob copy runs first (fail-fast on missing source); the DB
 * detail update runs inside the withAudit tx. If the DB tx fails after the
 * file copy, we attempt a best-effort rollback unlink so the dir does not
 * accumulate orphaned PNGs.
 *
 * Capability: catalog.entity.publish (gated upstream by proxy.ts).
 *
 * @see ia/projects/asset-pipeline/stage-6.1.md — TECH-1675 §Plan Digest
 */
import { NextResponse, type NextRequest } from "next/server";
import * as fs from "node:fs/promises";
import * as path from "node:path";
import { fileURLToPath } from "node:url";
import { withAudit } from "@/lib/audit/with-audit";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { BlobResolver } from "@/lib/blob-resolver";
import { promoteSprite } from "@/lib/db/sprite-repo";

export const dynamic = "force-dynamic";
export const routeMeta = {
  POST: { requires: "catalog.entity.publish" },
} as const;

type Ctx = { params: Promise<{ slug: string }> };

const UUID_RE = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

function repoRoot(): string {
  // route.ts dir = web/app/api/catalog/sprites/[slug]/promote (7 components below repo).
  // Up 7 levels lands on the repo root. Anchor on the module URL to stay cwd-independent.
  const here = path.dirname(fileURLToPath(import.meta.url));
  return path.resolve(here, "..", "..", "..", "..", "..", "..", "..");
}

export async function POST(request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  let body: { run_id?: unknown; variant_idx?: unknown };
  try {
    body = (await request.json()) as { run_id?: unknown; variant_idx?: unknown };
  } catch {
    return catalogJsonError(400, "bad_request", "Invalid JSON body");
  }
  const run_id = body.run_id;
  const variant_idx = body.variant_idx;
  if (typeof run_id !== "string" || !UUID_RE.test(run_id)) {
    return catalogJsonError(400, "bad_request", "run_id must be a UUID");
  }
  if (!Number.isInteger(variant_idx) || (variant_idx as number) < 0) {
    return catalogJsonError(400, "bad_request", "variant_idx must be a non-negative integer");
  }

  // Resolve source blob + dest path.
  const resolver = BlobResolver.fromEnv();
  const sourceUri = `gen://${run_id}/${variant_idx as number}`;
  let sourcePath: string;
  try {
    sourcePath = resolver.resolve(sourceUri);
  } catch (e) {
    return catalogJsonError(400, "bad_request", `Blob resolve failed: ${(e as Error).message}`);
  }

  // Probe source.
  try {
    await fs.access(sourcePath);
  } catch {
    return catalogJsonError(404, "not_found", `Source blob missing at ${sourceUri}`);
  }

  const destDir = path.join(repoRoot(), "Assets", "Sprites", "Generated");
  const destPath = path.join(destDir, `${slug}.png`);
  await fs.mkdir(destDir, { recursive: true });
  await fs.copyFile(sourcePath, destPath);
  const assetsPath = `Assets/Sprites/Generated/${slug}.png`;

  let didCommit = false;
  try {
    const wrapped = withAudit<{ slug: string; assets_path: string }>(async (_req, { emit, sql }) => {
      const result = await promoteSprite(
        slug,
        { assets_path: assetsPath, run_id, variant_idx: variant_idx as number },
        sql,
      );
      if (result.ok === "notfound") throw new Error("notfound: Sprite not found");
      if (result.ok === "validation") throw new Error(`validation: ${result.reason}`);
      if (result.ok === "conflict") throw new Error(`conflict: ${result.reason}`);
      await emit("catalog.sprite.promote", "catalog_entity", result.data.entity_id, {
        slug,
        run_id,
        variant_idx,
        assets_path: result.data.assets_path,
      });
      didCommit = true;
      return { status: 200, data: { slug, assets_path: result.data.assets_path } };
    });
    return await wrapped(request);
  } catch (e) {
    // Best-effort rollback: unlink the freshly-copied PNG so the dir does not
    // collect orphans. Failures are logged + swallowed (DB tx already failed).
    if (!didCommit) {
      await fs.unlink(destPath).catch((unlinkErr) => {
        console.error("[promote] rollback unlink failed", destPath, unlinkErr);
      });
    }
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", "Sprite not found");
    }
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      return catalogJsonError(400, "bad_request", e.message.replace(/^validation:\s*/i, ""));
    }
    if (e instanceof Error && e.message?.startsWith("conflict:")) {
      return NextResponse.json(
        { error: e.message.replace(/^conflict:\s*/i, ""), code: "conflict" },
        { status: 409 },
      );
    }
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "sprite-promote" });
    }
    return responseFromPostgresError(e, "Promote sprite failed");
  }
}
