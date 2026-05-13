/**
 * MCP tool: ui_visual_baseline_get — query active baseline for a panel slug.
 *
 * Returns the active ia_visual_baseline row for (panel_slug, resolution, theme)
 * or { status: 'missing' } when no active row exists.
 *
 * Strategy γ one-file-per-slice. No C# touched.
 */
import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { visualBaselineRepo } from "../ia-db/ui-catalog.js";

// ── Input ──────────────────────────────────────────────────────────────────

const inputShape = {
  panel_slug: z.string().min(1).describe("Panel slug (e.g. 'pause-menu')."),
  resolution: z
    .string()
    .optional()
    .describe("Resolution string (default '1920x1080')."),
  theme: z
    .string()
    .optional()
    .describe("Theme string (default 'dark')."),
};

// ── Result ─────────────────────────────────────────────────────────────────

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

// ── Registration ───────────────────────────────────────────────────────────

type Input = { panel_slug: string; resolution?: string; theme?: string };

export function registerUiVisualBaselineGet(server: McpServer): void {
  server.registerTool(
    "ui_visual_baseline_get",
    {
      description:
        "Return the active ia_visual_baseline row for (panel_slug, resolution, theme) " +
        "or { status: 'missing' } when no active baseline exists. " +
        "Inputs: panel_slug (required), resolution (default '1920x1080'), theme (default 'dark'). " +
        "Output: full VisualBaselineRow or { status: 'missing' }.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("ui_visual_baseline_get", async () => {
        const envelope = await wrapTool(async (input: Input | undefined) => {
          const slug = (input?.panel_slug ?? "").trim();
          if (!slug) {
            throw { code: "invalid_input", message: "panel_slug required." };
          }
          const pool = getIaDatabasePool();
          if (!pool) throw { code: "db_unavailable", message: "Postgres pool not configured." };
          const client = await pool.connect();
          try {
            const repo = visualBaselineRepo(client);
            const row = await repo.get(slug, {
              resolution: input?.resolution,
              theme: input?.theme,
            });
            if (row === null) return { status: "missing" as const };
            return row;
          } finally {
            client.release();
          }
        })(args as Input | undefined);
        return jsonResult(envelope);
      }),
  );
}
