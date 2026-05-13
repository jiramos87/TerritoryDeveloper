/**
 * MCP tool: ui_panel_render_mock
 *
 * Renders a deterministic ASCII tree of a panel's child hierarchy.
 * Input: { slug, format: 'ascii', max_depth?, pin? }
 * Output: { slug, format, mock, generated_from: { pin, max_depth } }
 *
 * Two sequential calls with same input MUST return byte-identical strings.
 * No Date.now() / randomness in emitter.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";
import { getPanelBundle, getPanelChildren } from "../ia-db/ui-catalog.js";
import { renderAscii } from "../ia-db/ascii-mock-emitter.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const panelRenderMockSchema = z.object({
  slug: z.string().describe("Panel slug to render (e.g. 'stats-panel')."),
  format: z.literal("ascii").describe("Output format — only 'ascii' is supported."),
  max_depth: z.number().int().min(0).max(5).optional().default(2)
    .describe("Max recursion depth for children[] (default 2)."),
  pin: z.enum(["live", "frozen"]).optional().default("live")
    .describe("Version pin: 'live' = current published (default)."),
});

export function registerUiPanelRenderMock(server: McpServer): void {
  server.registerTool(
    "ui_panel_render_mock",
    {
      description:
        "Render a deterministic ASCII mock tree of a panel's child hierarchy. " +
        "Input: {slug, format:'ascii', max_depth?, pin?}. " +
        "Output: {slug, format, mock, generated_from:{pin, max_depth}}. " +
        "Two sequential calls with the same input return byte-identical strings (no randomness). " +
        "Throws not_found when slug not in DB.",
      inputSchema: panelRenderMockSchema,
    },
    async (args) =>
      runWithToolTiming("ui_panel_render_mock", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof panelRenderMockSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            const bundle = await getPanelBundle(client, input.slug);
            if (bundle == null) {
              throw { code: "not_found" as const, message: `Panel not found: ${input.slug}` };
            }

            const children = await getPanelChildren(
              client,
              bundle.entity.id,
              { maxDepth: input.max_depth, pin: input.pin },
            );

            const mock = renderAscii(children, {
              rootLabel: input.slug,
              layoutTemplate: bundle.detail.layout_template,
            });

            return {
              slug: input.slug,
              format: input.format,
              mock,
              generated_from: {
                pin: input.pin,
                max_depth: input.max_depth,
              },
            };
          } catch (e) {
            if (e && typeof e === "object" && "code" in e) throw e;
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error" as const, message: msg };
          } finally {
            client.release();
          }
        })(panelRenderMockSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
