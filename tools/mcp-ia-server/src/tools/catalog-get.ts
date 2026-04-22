/**
 * MCP tool: catalog_get — composite catalog asset by id (asset + economy + sprite_slots).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";
import { loadCatalogAssetComposite } from "../catalog/pg-catalog-mappers.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const catalogGetInputSchema = z.object({
  asset_id: z
    .string()
    .describe("Numeric primary key of **catalog_asset** (same as HTTP **GET /api/catalog/assets/:id**)."),
});

export function registerCatalogGet(server: McpServer): void {
  server.registerTool(
    "catalog_get",
    {
      description:
        "Load one **catalog_asset** by id with **catalog_economy** + resolved **sprite_slots** (join to **catalog_sprite**). Matches HTTP composite shape. Returns structured error when id invalid or row missing. Requires **DATABASE_URL** or **config/postgres-dev.json**.",
      inputSchema: catalogGetInputSchema,
    },
    async (args) =>
      runWithToolTiming("catalog_get", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof catalogGetInputSchema>) => {
            const pool = getIaDatabasePool();
            if (!pool) throw dbUnconfiguredError();
            const assetId = input.asset_id.trim();
            if (!assetId) {
              throw { code: "invalid_input" as const, message: "asset_id is required." };
            }
            const client = await pool.connect();
            try {
              try {
                const out = await loadCatalogAssetComposite(client, assetId);
                if (out === "badid") {
                  throw { code: "invalid_input" as const, message: "Invalid asset_id." };
                }
                if (out === "notfound") {
                  throw {
                    code: "invalid_input" as const,
                    message: "Asset not found.",
                    details: { asset_id: assetId },
                  };
                }
                return out;
              } catch (e) {
                if (e && typeof e === "object" && "code" in e) throw e;
                const msg = e instanceof Error ? e.message : String(e);
                const code =
                  e && typeof e === "object" && "code" in e
                    ? String((e as { code?: string }).code)
                    : "";
                if (code === "42P01") {
                  throw {
                    code: "db_error" as const,
                    message: "catalog tables missing. Run `npm run db:migrate` from the repository root.",
                    hint: "Run `npm run db:migrate`",
                  };
                }
                throw { code: "db_error" as const, message: msg };
              }
            } finally {
              client.release();
            }
        })(catalogGetInputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
