/**
 * MCP tool: ui_visual_diff_run — server-side pixel-byte compare, writes ia_visual_diff row.
 *
 * Stand-in pixel compare until Editor ImageAssert lands at Task 1.0.3.
 * Computes sha256 of candidate PNG, looks up active baseline, compares sha256
 * (byte-identical == 0.0 diff_pct, any mismatch == tolerance_pct violation check),
 * inserts ia_visual_diff row, returns verdict.
 *
 * Strategy γ one-file-per-slice. No C# touched.
 */
import { z } from "zod";
import * as fs from "node:fs";
import * as path from "node:path";
import * as crypto from "node:crypto";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { resolveRepoRoot } from "../config.js";
import { visualBaselineRepo, visualDiffRepo } from "../ia-db/ui-catalog.js";
import type { VisualDiffRow, MaskRegion } from "../ia-db/ui-catalog.js";

// ── Input ──────────────────────────────────────────────────────────────────

// MaskRegion schema — matches MaskRegion interface in ui-catalog.ts.
const maskRegionSchema = z.object({
  x: z.number().int(),
  y: z.number().int(),
  w: z.number().int(),
  h: z.number().int(),
  name: z.string().optional(),
  reason: z.string().optional(),
});

const inputShape = {
  panel_slug: z.string().min(1).describe("Panel slug (e.g. 'pause-menu')."),
  candidate_path: z
    .string()
    .min(1)
    .describe("Absolute or repo-relative path to candidate PNG."),
  resolution: z
    .string()
    .optional()
    .describe("Resolution string (default '1920x1080')."),
  theme: z.string().optional().describe("Theme string (default 'dark')."),
  region_map: z
    .array(maskRegionSchema)
    .optional()
    .describe(
      "Caller-provided region rects to zero before pixel diff. " +
      "Overrides sidecar when both present. " +
      "Sidecar at Assets/UI/VisualBaselines/{slug}.masks.json auto-loaded when absent.",
    ),
};

// ── Helpers ────────────────────────────────────────────────────────────────

function sha256File(filePath: string): string {
  const bytes = fs.readFileSync(filePath);
  return crypto.createHash("sha256").update(bytes).digest("hex");
}

function sha256Buffer(buf: Buffer): string {
  return crypto.createHash("sha256").update(buf).digest("hex");
}

/**
 * Load region mask sidecar for slug.
 * Caller-provided rects override sidecar when both present.
 */
function resolveRegions(
  slug: string,
  callerRegions: MaskRegion[] | undefined,
  repoRoot: string,
): MaskRegion[] {
  if (callerRegions && callerRegions.length > 0) return callerRegions;
  const sidecarPath = path.join(
    repoRoot,
    "Assets/UI/VisualBaselines",
    `${slug}.masks.json`,
  );
  if (!fs.existsSync(sidecarPath)) return [];
  try {
    const raw = JSON.parse(fs.readFileSync(sidecarPath, "utf8")) as {
      regions?: MaskRegion[];
    };
    return Array.isArray(raw.regions) ? raw.regions : [];
  } catch {
    return [];
  }
}

/**
 * Apply mask regions to PNG bytes via sharp (zero pixels inside each rect).
 * Falls back to original bytes when sharp unavailable or regions empty.
 */
async function applyMaskRegions(
  pngBytes: Buffer,
  regions: MaskRegion[],
): Promise<Buffer> {
  if (regions.length === 0) return pngBytes;
  try {
    const sharp = (await import("sharp")).default;
    const { data, info } = await sharp(pngBytes)
      .ensureAlpha()
      .raw()
      .toBuffer({ resolveWithObject: true });

    // Zero pixels inside each rect.
    for (const rect of regions) {
      const x0 = Math.max(0, Math.min(rect.x, info.width - 1));
      const y0 = Math.max(0, Math.min(rect.y, info.height - 1));
      const x1 = Math.min(rect.x + rect.w, info.width);
      const y1 = Math.min(rect.y + rect.h, info.height);
      for (let y = y0; y < y1; y++) {
        for (let x = x0; x < x1; x++) {
          const idx = (y * info.width + x) * 4;
          data[idx] = 0;
          data[idx + 1] = 0;
          data[idx + 2] = 0;
          data[idx + 3] = 0;
        }
      }
    }

    return await sharp(data, {
      raw: { width: info.width, height: info.height, channels: 4 },
    })
      .png()
      .toBuffer();
  } catch {
    return pngBytes;
  }
}

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

// ── Registration ───────────────────────────────────────────────────────────

type Input = {
  panel_slug: string;
  candidate_path: string;
  resolution?: string;
  theme?: string;
  region_map?: MaskRegion[];
};

export function registerUiVisualDiffRun(server: McpServer): void {
  server.registerTool(
    "ui_visual_diff_run",
    {
      description:
        "Compare candidate PNG against active ia_visual_baseline for panel_slug. " +
        "Loads region mask sidecar from Assets/UI/VisualBaselines/{slug}.masks.json when present " +
        "(caller-provided region_map overrides sidecar when both present); zeroes masked pixels " +
        "on BOTH candidate and baseline images before SHA comparison, so live-state panels " +
        "(hud-bar, budget-panel, time-strip) return verdict=match despite ticker updates. " +
        "Byte-identical after masking → diff_pct=0.0 verdict=match; " +
        "hash mismatch → diff_pct=tolerance_pct verdict=regression; " +
        "no active baseline → verdict=new_baseline_needed. " +
        "Inserts ia_visual_diff row and returns it. " +
        "Inputs: panel_slug, candidate_path (abs or repo-rel), resolution, theme, " +
        "region_map (optional array of {x,y,w,h} — overrides sidecar). " +
        "Output: full VisualDiffRow + { panel_slug, baseline_status }.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("ui_visual_diff_run", async () => {
        const envelope = await wrapTool(
          async (
            input: Input | undefined,
          ): Promise<VisualDiffRow & { panel_slug: string; baseline_status: string }> => {
            const slug = (input?.panel_slug ?? "").trim();
            if (!slug) throw { code: "invalid_input", message: "panel_slug required." };
            const rawPath = (input?.candidate_path ?? "").trim();
            if (!rawPath) throw { code: "invalid_input", message: "candidate_path required." };

            const repoRoot = resolveRepoRoot();
            const candidatePath = path.isAbsolute(rawPath)
              ? rawPath
              : path.join(repoRoot, rawPath);
            if (!fs.existsSync(candidatePath)) {
              throw { code: "not_found", message: `candidate_path not found: ${candidatePath}` };
            }

            // Resolve mask regions: caller rects override sidecar.
            const regions = resolveRegions(slug, input?.region_map, repoRoot);

            // Compute hash after applying mask (zero live regions before SHA).
            const rawCandidateBytes = fs.readFileSync(candidatePath);
            const maskedCandidateBytes = await applyMaskRegions(
              rawCandidateBytes as Buffer,
              regions,
            );
            const candidateHash = sha256Buffer(maskedCandidateBytes as Buffer);

            const pool = getIaDatabasePool();
            if (!pool) throw { code: "db_unavailable", message: "Postgres pool not configured." };
            const client = await pool.connect();
            try {
              const baseRepo = visualBaselineRepo(client);
              const diffRepo = visualDiffRepo(client);

              const baseline = await baseRepo.get(slug, {
                resolution: input?.resolution,
                theme: input?.theme,
              });

              if (baseline === null) {
                // No active baseline — record new_baseline_needed diff with dummy baseline.
                // We insert without a real baseline_id; find or create a candidate placeholder.
                // Spec: inserts ia_visual_diff row + returns verdict.
                // For new_baseline_needed, we still need a row — use a null-safe approach:
                // insert the diff with candidateHash as both hashes, verdict=new_baseline_needed.
                // Since baseline_id FK is NOT NULL, we must surface this as a structured result
                // without a DB row. Return synthetic verdict object.
                return {
                  id: "00000000-0000-0000-0000-000000000000",
                  baseline_id: "00000000-0000-0000-0000-000000000000",
                  candidate_hash: candidateHash,
                  diff_pct: 0,
                  verdict: "new_baseline_needed",
                  diff_image_ref: null,
                  region_map: regions.length > 0 ? regions : (input?.region_map ?? null),
                  ran_at: new Date(),
                  panel_slug: slug,
                  baseline_status: "missing",
                };
              }

              // Compute masked baseline hash: load baseline image + apply same mask.
              // When baseline image file is present locally, zero the same regions before
              // comparing hashes so live-state panels always produce verdict=match
              // (masked region zeroed on both images → identical bytes → match).
              let effectiveBaselineHash = baseline.image_sha256;
              const baselineImagePath = path.isAbsolute(baseline.image_ref)
                ? baseline.image_ref
                : path.join(repoRoot, baseline.image_ref);
              if (regions.length > 0 && fs.existsSync(baselineImagePath)) {
                try {
                  const baselineBytes = fs.readFileSync(baselineImagePath);
                  const maskedBaseline = await applyMaskRegions(baselineBytes as Buffer, regions);
                  effectiveBaselineHash = sha256Buffer(maskedBaseline as Buffer);
                } catch {
                  // Fallback: use stored hash if masking fails.
                }
              }

              // Byte-identical after masking → match, 0.0 diff_pct.
              const isIdentical = candidateHash === effectiveBaselineHash;
              const diff_pct = isIdentical ? 0.0 : baseline.tolerance_pct;
              const verdict: "match" | "regression" = isIdentical ? "match" : "regression";

              const diffRow = await diffRepo.run({
                baseline_id: baseline.id,
                candidate_hash: candidateHash,
                diff_pct,
                verdict,
                region_map: regions.length > 0 ? regions : (input?.region_map ?? undefined),
              });

              return { ...diffRow, panel_slug: slug, baseline_status: "active" };
            } finally {
              client.release();
            }
          },
        )(args as Input | undefined);
        return jsonResult(envelope);
      }),
  );
}
