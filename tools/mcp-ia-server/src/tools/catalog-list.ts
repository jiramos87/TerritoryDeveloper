/**
 * MCP tool: catalog_list — paginated catalog_asset rows (default published only).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { Pool } from "pg";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";
import { mapRowToCatalogAsset } from "../catalog/pg-catalog-mappers.js";

const STATUSES = ["draft", "published", "retired"] as const;

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

function statusWhereClause(
  includeDraft: boolean,
  statusFilter: (typeof STATUSES)[number] | null,
): { sql: string; params: unknown[] } {
  if (statusFilter) {
    return { sql: `status = $1`, params: [statusFilter] };
  }
  if (includeDraft) {
    return { sql: `(status in ('draft', 'published'))`, params: [] };
  }
  return { sql: `status = 'published'`, params: [] };
}

export async function runCatalogList(
  pool: Pool,
  input: {
    include_draft?: boolean;
    status?: string;
    category?: string;
    limit?: number;
    cursor?: string;
  },
): Promise<{ assets: ReturnType<typeof mapRowToCatalogAsset>[]; next_cursor: string | null; limit: number }> {
  const includeDraft =
    input.include_draft === true ||
    String(input.include_draft).toLowerCase() === "true" ||
    input.include_draft === 1;
  let statusFilter: (typeof STATUSES)[number] | null = null;
  if (input.status != null && String(input.status).length > 0) {
    const s = String(input.status);
    if (!(STATUSES as readonly string[]).includes(s)) {
      throw { code: "invalid_input" as const, message: "Invalid status (use draft, published, or retired)." };
    }
    statusFilter = s as (typeof STATUSES)[number];
  }
  const category = input.category?.trim() || null;
  let limit = 200;
  if (input.limit != null) {
    const n = Number(input.limit);
    if (!Number.isFinite(n) || n < 1) {
      throw { code: "invalid_input" as const, message: "Invalid limit." };
    }
    limit = Math.min(n, 500);
  }
  const cursor = input.cursor?.trim() || null;
  if (cursor != null && !/^\d+$/.test(cursor)) {
    throw { code: "invalid_input" as const, message: "Invalid cursor (numeric id string)." };
  }

  const { sql: statusSql, params: statusParams } = statusWhereClause(includeDraft, statusFilter);
  const args: unknown[] = [...statusParams];
  let paramIdx = args.length;

  let categoryClause = "";
  if (category) {
    paramIdx += 1;
    categoryClause = ` and category = $${paramIdx}`;
    args.push(category);
  }
  let cursorClause = "";
  if (cursor != null && cursor.length > 0) {
    paramIdx += 1;
    cursorClause = ` and id > $${paramIdx}`;
    args.push(Number.parseInt(cursor, 10));
  }
  paramIdx += 1;
  args.push(limit);

  const query = `
    select id, category, slug, display_name, status, replaced_by, footprint_w, footprint_h,
           placement_mode, unlocks_after, has_button, updated_at
    from catalog_asset
    where ${statusSql} ${categoryClause} ${cursorClause}
    order by id asc
    limit $${paramIdx}
  `;

  const { rows } = await pool.query(query, args);
  const list = (rows as Record<string, unknown>[]).map((r) => mapRowToCatalogAsset(r as never));
  const nextCursor = list.length === limit ? (list[list.length - 1]?.id ?? null) : null;
  return { assets: list, next_cursor: nextCursor, limit };
}

const catalogListInputSchema = z.object({
  include_draft: z
    .union([z.boolean(), z.string(), z.number()])
    .optional()
    .describe("When true, list draft+published (same as HTTP include_draft). Default false = published only."),
  status: z.string().optional().describe("Optional single status filter: draft | published | retired."),
  category: z.string().optional().describe("Optional category = filter."),
  limit: z.coerce.number().int().min(1).max(500).optional().describe("Page size (default 200, max 500)."),
  cursor: z.string().optional().describe("Keyset cursor: numeric id string; return rows with id > cursor."),
});

type CatalogListInput = z.infer<typeof catalogListInputSchema>;

export function registerCatalogList(server: McpServer): void {
  server.registerTool(
    "catalog_list",
    {
      description:
        "List **catalog_asset** rows from Postgres with pagination (keyset cursor). Default visibility matches HTTP **GET /api/catalog/assets**: **published** only unless `include_draft` or `status` overrides. Requires **DATABASE_URL** or **config/postgres-dev.json**.",
      inputSchema: catalogListInputSchema,
    },
    async (args) =>
      runWithToolTiming("catalog_list", async () => {
        const envelope = await wrapTool(async (input: CatalogListInput) => {
            const pool = getIaDatabasePool();
            if (!pool) throw dbUnconfiguredError();
            try {
              const payload = await runCatalogList(pool, input);
              return payload;
            } catch (e) {
              if (e && typeof e === "object" && "code" in e) throw e;
              const msg = e instanceof Error ? e.message : String(e);
              const code =
                e && typeof e === "object" && "code" in e ? String((e as { code?: string }).code) : "";
              if (code === "42P01") {
                throw {
                  code: "db_error" as const,
                  message: "catalog tables missing. Run `npm run db:migrate` from the repository root.",
                  hint: "Run `npm run db:migrate`",
                };
              }
              throw { code: "db_error" as const, message: msg };
            }
        })(catalogListInputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
