/**
 * Audio promote — copy source blob to Assets/Audio/Generated/{slug}.ogg + write
 * `audio_detail.assets_path` (TECH-1959 / Stage 9.1).
 *
 * POST /api/catalog/audio/[slug]/promote
 *      body: {} (slug in path; source URI lives in audio_detail.source_uri)
 *
 * Hard-gate flow per DEC-A30 + DEC-A31:
 *   1. Load `audio_detail.{loudness_lufs, peak_db, source_uri}` for the slug.
 *   2. Run `auditAudioLoudness` against the persisted measurements (TECH-1957
 *      populated these at render time; TECH-1959 §Implementer Latitude row 2
 *      pragmatic deviation: lint runs on already-stored values, not a fresh
 *      Python re-measure — measurements live with the source bytes hash).
 *   3. Block with DEC-A48 envelope when `hasBlockingAudioLint` returns true.
 *   4. Resolve `source_uri` → local path; copy to
 *      `Assets/Audio/Generated/{slug}.ogg`; update `audio_detail.assets_path`.
 *
 * Atomic: blob copy first; on DB tx failure best-effort rollback unlinks the
 * freshly-copied file. Pattern mirrors sprite promote (TECH-1675).
 *
 * Capability: catalog.entity.publish (gated by route-meta-map).
 *
 * @see ia/projects/asset-pipeline/stage-9.1 — TECH-1959 §Plan Digest
 * @see web/app/api/catalog/sprites/[slug]/promote/route.ts — sibling pattern
 */
import { NextResponse, type NextRequest } from "next/server";
import * as fs from "node:fs/promises";
import * as path from "node:path";
import { fileURLToPath } from "node:url";
import { withAudit } from "@/lib/audit/with-audit";
import {
  catalogJsonError,
  responseFromPostgresError,
} from "@/lib/catalog/catalog-api-errors";
import { BlobResolver } from "@/lib/blob-resolver";
import { getSql } from "@/lib/db/client";
import { getAudioMeasurements, promoteAudio } from "@/lib/db/audio-repo";
import {
  auditAudioLoudness,
  hasBlockingAudioLint,
} from "@/lib/lint/audio-loudness";
import {
  loadEnabledLintRules,
  resolveAudioLoudnessConfig,
} from "@/lib/lint/registry";

export const dynamic = "force-dynamic";
export const routeMeta = {
  POST: { requires: "catalog.entity.publish" },
} as const;

type Ctx = { params: Promise<{ slug: string }> };

function repoRoot(): string {
  // route.ts dir = web/app/api/catalog/audio/[slug]/promote (7 components below repo).
  const here = path.dirname(fileURLToPath(import.meta.url));
  return path.resolve(here, "..", "..", "..", "..", "..", "..", "..");
}

export async function POST(request: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;

  // Load DB-side rule config + measurements before touching disk.
  let lintConfig;
  try {
    const rules = await loadEnabledLintRules("audio");
    lintConfig = resolveAudioLoudnessConfig(rules);
  } catch (e) {
    if (
      e instanceof Error &&
      e.message === "DATABASE_URL not set — required for DB access."
    ) {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "audio-promote-rules",
      });
    }
    return responseFromPostgresError(e, "Load audio lint rules failed");
  }

  // Phase 1 — read measurements + source_uri from audio_detail (no audit; probe).
  let measurements: {
    entity_id: string;
    loudness_lufs: number | null;
    peak_db: number | null;
    source_uri: string;
  };
  try {
    const sql = getSql();
    const row = await getAudioMeasurements(slug, sql);
    if (row.ok === "notfound") {
      return catalogJsonError(404, "not_found", "Audio entity not found");
    }
    if (row.ok !== "ok") {
      return catalogJsonError(500, "internal", "Unexpected repo state");
    }
    measurements = row.data;
  } catch (e) {
    if (
      e instanceof Error &&
      e.message === "DATABASE_URL not set — required for DB access."
    ) {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "audio-promote-measurements",
      });
    }
    return responseFromPostgresError(e, "Load audio measurements failed");
  }

  // Phase 2 — hard-gate lints (DEC-A30 + DEC-A31). Block before copy.
  const lintResults = auditAudioLoudness(
    {
      loudness_lufs: measurements.loudness_lufs,
      peak_db: measurements.peak_db,
    },
    lintConfig,
  );
  if (hasBlockingAudioLint(lintResults)) {
    return NextResponse.json(
      {
        error: "Audio publish blocked by hard-gate lint",
        code: "lint_block",
        details: { rules: lintResults },
      },
      { status: 422 },
    );
  }

  // Phase 3 — resolve source blob + copy.
  const resolver = BlobResolver.fromEnv();
  let sourcePath: string;
  try {
    sourcePath = resolver.resolve(measurements.source_uri);
  } catch (e) {
    return catalogJsonError(
      400,
      "bad_request",
      `Blob resolve failed: ${(e as Error).message}`,
    );
  }

  try {
    await fs.access(sourcePath);
  } catch {
    return catalogJsonError(
      404,
      "not_found",
      `Source blob missing at ${measurements.source_uri}`,
    );
  }

  const destDir = path.join(repoRoot(), "Assets", "Audio", "Generated");
  const destPath = path.join(destDir, `${slug}.ogg`);
  await fs.mkdir(destDir, { recursive: true });
  await fs.copyFile(sourcePath, destPath);
  const assetsPath = `Assets/Audio/Generated/${slug}.ogg`;

  // Phase 4 — DB tx: write assets_path + emit audit event.
  let didCommit = false;
  try {
    const wrapped = withAudit<{ slug: string; assets_path: string }>(
      async (_req, { emit, sql }) => {
        const result = await promoteAudio(slug, { assets_path: assetsPath }, sql);
        if (result.ok === "notfound") throw new Error("notfound: Audio not found");
        if (result.ok === "validation")
          throw new Error(`validation: ${result.reason}`);
        if (result.ok === "lint_block")
          throw new Error(`lint_block: ${result.reason}`);
        await emit(
          "catalog.audio.promote",
          "catalog_entity",
          result.data.entity_id,
          {
            slug,
            assets_path: result.data.assets_path,
            loudness_lufs: measurements.loudness_lufs,
            peak_db: measurements.peak_db,
          },
        );
        didCommit = true;
        return {
          status: 200,
          data: { slug, assets_path: result.data.assets_path },
        };
      },
    );
    return await wrapped(request);
  } catch (e) {
    if (!didCommit) {
      await fs.unlink(destPath).catch((unlinkErr) => {
        console.error("[audio-promote] rollback unlink failed", destPath, unlinkErr);
      });
    }
    if (e instanceof Error && e.message?.startsWith("notfound:")) {
      return catalogJsonError(404, "not_found", "Audio not found");
    }
    if (e instanceof Error && e.message?.startsWith("validation:")) {
      return catalogJsonError(
        400,
        "bad_request",
        e.message.replace(/^validation:\s*/i, ""),
      );
    }
    if (
      e instanceof Error &&
      e.message === "DATABASE_URL not set — required for DB access."
    ) {
      return catalogJsonError(500, "internal", "Database not configured", {
        logContext: "audio-promote",
      });
    }
    return responseFromPostgresError(e, "Promote audio failed");
  }
}
