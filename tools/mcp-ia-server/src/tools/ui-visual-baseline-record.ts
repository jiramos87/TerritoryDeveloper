/**
 * MCP tool: ui_visual_baseline_record — insert ia_visual_baseline row + copy PNG into Assets.
 *
 * Computes sha256 of PNG bytes at image_path, copies into
 * Assets/UI/VisualBaselines/{slug}@v{N}.png, retires prior active row for
 * same (slug, resolution, theme), inserts new active row.
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
import { visualBaselineRepo } from "../ia-db/ui-catalog.js";
import type { VisualBaselineRow } from "../ia-db/ui-catalog.js";

// ── Input ──────────────────────────────────────────────────────────────────

const inputShape = {
  panel_slug: z.string().min(1).describe("Panel slug (e.g. 'pause-menu')."),
  image_path: z
    .string()
    .min(1)
    .describe(
      "Absolute or repo-relative path to the candidate PNG to promote as baseline.",
    ),
  resolution: z
    .string()
    .optional()
    .describe("Resolution string (default '1920x1080')."),
  theme: z.string().optional().describe("Theme string (default 'dark')."),
  captured_by: z.string().optional().describe("Agent or user id who captured."),
  tolerance_pct: z
    .number()
    .min(0)
    .max(1)
    .optional()
    .describe("Per-pixel correctness threshold (default 0.005)."),
};

// ── Helpers ────────────────────────────────────────────────────────────────

function sha256File(filePath: string): string {
  const bytes = fs.readFileSync(filePath);
  return crypto.createHash("sha256").update(bytes).digest("hex");
}

function nextVersionNumber(repoRoot: string, slug: string): number {
  const dir = path.join(repoRoot, "Assets/UI/VisualBaselines");
  if (!fs.existsSync(dir)) return 1;
  const files = fs.readdirSync(dir);
  const re = new RegExp(`^${slug.replace(/-/g, "\\-")}@v(\\d+)\\.png$`);
  let max = 0;
  for (const f of files) {
    const m = re.exec(f);
    if (m) max = Math.max(max, Number(m[1]));
  }
  return max + 1;
}

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

// ── Registration ───────────────────────────────────────────────────────────

type Input = {
  panel_slug: string;
  image_path: string;
  resolution?: string;
  theme?: string;
  captured_by?: string;
  tolerance_pct?: number;
};

export function registerUiVisualBaselineRecord(server: McpServer): void {
  server.registerTool(
    "ui_visual_baseline_record",
    {
      description:
        "Insert a new ia_visual_baseline row and copy the source PNG into " +
        "Assets/UI/VisualBaselines/{slug}@v{N}.png. Retires any prior active row " +
        "for the same (panel_slug, resolution, theme). " +
        "Inputs: panel_slug, image_path (abs or repo-rel), resolution (default '1920x1080'), " +
        "theme (default 'dark'), captured_by, tolerance_pct (default 0.005). " +
        "Output: full VisualBaselineRow of the new active row + dest_path.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("ui_visual_baseline_record", async () => {
        const envelope = await wrapTool(
          async (input: Input | undefined): Promise<VisualBaselineRow & { dest_path: string }> => {
            const slug = (input?.panel_slug ?? "").trim();
            if (!slug) throw { code: "invalid_input", message: "panel_slug required." };
            const rawPath = (input?.image_path ?? "").trim();
            if (!rawPath) throw { code: "invalid_input", message: "image_path required." };

            const repoRoot = resolveRepoRoot();
            const srcPath = path.isAbsolute(rawPath)
              ? rawPath
              : path.join(repoRoot, rawPath);
            if (!fs.existsSync(srcPath)) {
              throw { code: "not_found", message: `image_path not found: ${srcPath}` };
            }

            const sha256 = sha256File(srcPath);
            const versionN = nextVersionNumber(repoRoot, slug);
            const destDir = path.join(repoRoot, "Assets/UI/VisualBaselines");
            fs.mkdirSync(destDir, { recursive: true });
            const destName = `${slug}@v${versionN}.png`;
            const destPath = path.join(destDir, destName);
            fs.copyFileSync(srcPath, destPath);

            const imageRef = `Assets/UI/VisualBaselines/${destName}`;

            const pool = getIaDatabasePool();
            if (!pool) throw { code: "db_unavailable", message: "Postgres pool not configured." };
            const client = await pool.connect();
            try {
              const repo = visualBaselineRepo(client);
              const row = await repo.record({
                panel_slug: slug,
                image_ref: imageRef,
                image_sha256: sha256,
                resolution: input?.resolution,
                theme: input?.theme,
                captured_by: input?.captured_by,
                tolerance_pct: input?.tolerance_pct,
              });
              return { ...row, dest_path: destPath };
            } finally {
              client.release();
            }
          },
        )(args as Input | undefined);
        return jsonResult(envelope);
      }),
  );
}
