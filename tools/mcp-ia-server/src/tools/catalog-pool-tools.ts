/**
 * MCP tools: catalog_pool_list, catalog_pool_get, catalog_pool_upsert
 * — spawn pool tables per migration 0012.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { checkCaller } from "../auth/caller-allowlist.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";
import { idString } from "../catalog/pg-catalog-mappers.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const poolListSchema = z.object({
  owner_category: z.string().optional().describe("When set, filter `catalog_spawn_pool.owner_category =` this value."),
});

const poolGetSchema = z.object({
  pool_id: z.string().regex(/^\d{1,32}$/).describe("**catalog_spawn_pool.id**"),
});

const poolUpsertSchema = z.discriminatedUnion("kind", [
  z.object({
    kind: z.literal("spawn_pool"),
    caller_agent: z.string().min(1),
    slug: z.string().min(1),
    owner_category: z.string().min(1),
    owner_subtype: z.string().nullable().optional(),
  }),
  z.object({
    kind: z.literal("pool_member"),
    caller_agent: z.string().min(1),
    pool_id: z.string().regex(/^\d{1,32}$/),
    asset_id: z.string().regex(/^\d{1,32}$/),
    weight: z.coerce.number().int().min(1),
  }),
]);

export async function runCatalogPoolList(input: z.infer<typeof poolListSchema>): Promise<unknown> {
  const pool = getIaDatabasePool();
  if (!pool) throw dbUnconfiguredError();
  const cat = input.owner_category?.trim();
  if (cat) {
    const { rows } = await pool.query(
      `select id, slug, owner_category, owner_subtype from catalog_spawn_pool where owner_category = $1 order by id asc`,
      [cat],
    );
    return { pools: rows };
  }
  const { rows } = await pool.query(
    `select id, slug, owner_category, owner_subtype from catalog_spawn_pool order by id asc`,
  );
  return { pools: rows };
}

export async function runCatalogPoolGet(input: z.infer<typeof poolGetSchema>): Promise<unknown> {
  const pool = getIaDatabasePool();
  if (!pool) throw dbUnconfiguredError();
  const idNum = Number.parseInt(input.pool_id, 10);
  if (!Number.isSafeInteger(idNum) || idNum < 1) {
    throw { code: "invalid_input" as const, message: "Invalid pool_id." };
  }
  const pr = await pool.query(
    `select id, slug, owner_category, owner_subtype from catalog_spawn_pool where id = $1 limit 1`,
    [idNum],
  );
  if (pr.rows.length === 0) {
    throw {
      code: "invalid_input" as const,
      message: "Spawn pool not found.",
      details: { pool_id: input.pool_id },
    };
  }
  const mr = await pool.query(
    `select pool_id, asset_id, weight from catalog_pool_member where pool_id = $1 order by asset_id asc`,
    [idNum],
  );
  const spawn_pool = pr.rows[0] as Record<string, unknown>;
  return {
    spawn_pool: {
      id: idString(spawn_pool.id as never),
      slug: spawn_pool.slug,
      owner_category: spawn_pool.owner_category,
      owner_subtype: spawn_pool.owner_subtype ?? null,
    },
    members: (mr.rows as Record<string, unknown>[]).map((r) => ({
      pool_id: idString(r.pool_id as never),
      asset_id: idString(r.asset_id as never),
      weight: Number(r.weight),
    })),
  };
}

export async function runCatalogPoolUpsert(input: z.infer<typeof poolUpsertSchema>): Promise<unknown> {
  checkCaller("catalog_pool_upsert", input.caller_agent);
  const pool = getIaDatabasePool();
  if (!pool) throw dbUnconfiguredError();

  if (input.kind === "spawn_pool") {
    const st = input.owner_subtype ?? null;
    const r = await pool.query(
      `insert into catalog_spawn_pool (slug, owner_category, owner_subtype)
       values ($1, $2, $3)
       on conflict (slug) do update set
         owner_category = excluded.owner_category,
         owner_subtype = excluded.owner_subtype
       returning id, slug, owner_category, owner_subtype`,
      [input.slug, input.owner_category, st],
    );
    return { upserted: "spawn_pool", row: r.rows[0] };
  }

  const poolId = Number.parseInt(input.pool_id, 10);
  const assetId = Number.parseInt(input.asset_id, 10);
  if (!Number.isSafeInteger(poolId) || poolId < 1 || !Number.isSafeInteger(assetId) || assetId < 1) {
    throw { code: "invalid_input" as const, message: "Invalid pool_id or asset_id." };
  }
  const r = await pool.query(
    `insert into catalog_pool_member (pool_id, asset_id, weight)
     values ($1, $2, $3)
     on conflict (pool_id, asset_id) do update set weight = excluded.weight
     returning pool_id, asset_id, weight`,
    [poolId, assetId, input.weight],
  );
  return { upserted: "pool_member", row: r.rows[0] };
}

export function registerCatalogPoolList(server: McpServer): void {
  server.registerTool(
    "catalog_pool_list",
    {
      description:
        "List **catalog_spawn_pool** rows (optional **owner_category** filter). Read-only; requires Postgres + migration **0012_catalog_spawn_pools.sql**.",
      inputSchema: poolListSchema,
    },
    async (args) =>
      runWithToolTiming("catalog_pool_list", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof poolListSchema>) => {
          try {
            return await runCatalogPoolList(input);
          } catch (e) {
            const code =
              e && typeof e === "object" && "code" in e ? String((e as { code?: string }).code) : "";
            if (code === "42P01") {
              throw {
                code: "db_error" as const,
                message: "catalog_spawn_pool not found. Run `npm run db:migrate`.",
                hint: "Run `npm run db:migrate`",
              };
            }
            throw e;
          }
        })(poolListSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

export function registerCatalogPoolGet(server: McpServer): void {
  server.registerTool(
    "catalog_pool_get",
    {
      description:
        "Load one **catalog_spawn_pool** by id plus **catalog_pool_member** rows for that pool. Read-only.",
      inputSchema: poolGetSchema,
    },
    async (args) =>
      runWithToolTiming("catalog_pool_get", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof poolGetSchema>) => {
          try {
            return await runCatalogPoolGet(input);
          } catch (e) {
            if (e && typeof e === "object" && "code" in e) throw e;
            const code =
              e && typeof e === "object" && "code" in e ? String((e as { code?: string }).code) : "";
            if (code === "42P01") {
              throw {
                code: "db_error" as const,
                message: "catalog pool tables missing. Run `npm run db:migrate`.",
                hint: "Run `npm run db:migrate`",
              };
            }
            throw e;
          }
        })(poolGetSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

export function registerCatalogPoolUpsert(server: McpServer): void {
  server.registerTool(
    "catalog_pool_upsert",
    {
      description:
        "**kind:spawn_pool** — upsert by **slug** (insert or update owner fields). **kind:pool_member** — upsert **(pool_id, asset_id)** weight. **caller_agent** required; gated like **catalog_upsert**.",
      inputSchema: poolUpsertSchema,
    },
    async (args) =>
      runWithToolTiming("catalog_pool_upsert", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof poolUpsertSchema>) => {
          try {
            return await runCatalogPoolUpsert(input);
          } catch (e) {
            const code =
              e && typeof e === "object" && "code" in e ? String((e as { code?: string }).code) : "";
            if (code === "42P01") {
              throw {
                code: "db_error" as const,
                message: "catalog pool tables missing. Run `npm run db:migrate`.",
                hint: "Run `npm run db:migrate`",
              };
            }
            throw e;
          }
        })(poolUpsertSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
