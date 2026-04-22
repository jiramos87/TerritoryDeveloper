/**
 * MCP tool: catalog_upsert — create or patch catalog_asset (+ economy + sprite_binds on create).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { PoolClient } from "pg";
import { checkCaller } from "../auth/caller-allowlist.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";
import {
  loadCatalogAssetComposite,
  mapRowToCatalogAsset,
  mapRowToEconomy,
} from "../catalog/pg-catalog-mappers.js";

const SPRITE_SLOTS = new Set([
  "world",
  "button_target",
  "button_pressed",
  "button_disabled",
  "button_hover",
]);

const economyCreateSchema = z.object({
  base_cost_cents: z.number(),
  monthly_upkeep_cents: z.number(),
  demolition_refund_pct: z.number().optional(),
  construction_ticks: z.number().optional(),
  budget_envelope_id: z.number().nullable().optional(),
  cost_catalog_row_id: z.string().nullable().optional(),
});

const createBodySchema = z.object({
  category: z.string().min(1),
  slug: z.string().min(1),
  display_name: z.string().min(1),
  status: z.enum(["draft", "published", "retired"]),
  replaced_by: z.string().nullable().optional(),
  footprint_w: z.number().int().optional(),
  footprint_h: z.number().int().optional(),
  placement_mode: z.string().nullable().optional(),
  unlocks_after: z.string().nullable().optional(),
  has_button: z.boolean().optional(),
  economy: economyCreateSchema,
  sprite_binds: z.array(
    z.object({
      slot: z.string(),
      sprite_id: z.string().regex(/^\d+$/),
    }),
  ),
});

const economyPatchSchema = z
  .object({
    base_cost_cents: z.number().optional(),
    monthly_upkeep_cents: z.number().optional(),
    demolition_refund_pct: z.number().optional(),
    construction_ticks: z.number().optional(),
    budget_envelope_id: z.number().nullable().optional(),
    cost_catalog_row_id: z.string().nullable().optional(),
  })
  .optional();

const catalogPatchBranchSchema = z
  .object({
    mode: z.literal("patch"),
    caller_agent: z.string().min(1),
    asset_id: z.string().regex(/^\d{1,32}$/),
    updated_at: z.string().min(1),
    display_name: z.string().optional(),
    status: z.enum(["draft", "published", "retired"]).optional(),
    replaced_by: z.string().nullable().optional(),
    footprint_w: z.number().int().optional(),
    footprint_h: z.number().int().optional(),
    placement_mode: z.string().nullable().optional(),
    unlocks_after: z.string().nullable().optional(),
    has_button: z.boolean().optional(),
    economy: economyPatchSchema,
  })
  .superRefine((val, ctx) => {
    const eco = val.economy;
    const hasEco =
      eco != null && Object.keys(eco).some((k) => eco[k as keyof typeof eco] !== undefined);
    const hasField =
      val.display_name !== undefined ||
      val.status !== undefined ||
      val.replaced_by !== undefined ||
      val.footprint_w !== undefined ||
      val.footprint_h !== undefined ||
      val.placement_mode !== undefined ||
      val.unlocks_after !== undefined ||
      val.has_button !== undefined;
    if (!hasField && !hasEco) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "patch requires at least one mutable field besides asset_id and updated_at.",
      });
    }
  });

const catalogUpsertInputSchema = z.discriminatedUnion("mode", [
  z.object({
    mode: z.literal("create"),
    caller_agent: z.string().min(1),
    body: createBodySchema,
  }),
  catalogPatchBranchSchema,
]);

export type CatalogUpsertInput = z.infer<typeof catalogUpsertInputSchema>;

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

function validateCreateBody(body: z.infer<typeof createBodySchema>): string | null {
  for (const bind of body.sprite_binds) {
    if (!SPRITE_SLOTS.has(bind.slot)) return `sprite_bind.slot invalid: ${bind.slot}`;
  }
  return null;
}

async function runCreate(client: PoolClient, body: z.infer<typeof createBodySchema>): Promise<string> {
  const err = validateCreateBody(body);
  if (err) throw { code: "invalid_input" as const, message: err };

  const fpw = body.footprint_w ?? 1;
  const fph = body.footprint_h ?? 1;
  const hasButton = body.has_button ?? true;
  const rep = body.replaced_by == null || body.replaced_by === "" ? null : body.replaced_by;
  const replNum: number | null = rep == null ? null : Number.parseInt(rep, 10);
  if (rep != null && (replNum == null || Number.isNaN(replNum))) {
    throw { code: "invalid_input" as const, message: "replaced_by must be a numeric id" };
  }
  const eco = body.economy;
  const dfp = eco.demolition_refund_pct ?? 0;
  const ctk = eco.construction_ticks ?? 0;
  const budgetE = eco.budget_envelope_id ?? null;
  const costC = eco.cost_catalog_row_id;
  const costCNum: number | null =
    costC == null || costC === "" ? null : Number.parseInt(String(costC), 10);
  if (costC != null && costC !== "" && (costCNum == null || Number.isNaN(costCNum))) {
    throw { code: "invalid_input" as const, message: "cost_catalog_row_id must be a numeric id" };
  }

  const ins = await client.query(
    `insert into catalog_asset (
        category, slug, display_name, status, replaced_by,
        footprint_w, footprint_h, placement_mode, unlocks_after, has_button, updated_at
      )
      values ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10, now())
      returning id`,
    [
      body.category,
      body.slug,
      body.display_name,
      body.status,
      replNum,
      fpw,
      fph,
      body.placement_mode ?? null,
      body.unlocks_after ?? null,
      hasButton,
    ],
  );
  const rawId = ins.rows[0]?.id as number | bigint;
  const assetIdStr = String(rawId);
  const aNum = typeof rawId === "bigint" ? Number(rawId) : (rawId as number);

  await client.query(
    `insert into catalog_economy (
        asset_id, base_cost_cents, monthly_upkeep_cents, demolition_refund_pct,
        construction_ticks, budget_envelope_id, cost_catalog_row_id
      )
      values ($1,$2,$3,$4,$5,$6,$7)`,
    [
      aNum,
      Math.trunc(eco.base_cost_cents),
      Math.trunc(eco.monthly_upkeep_cents),
      dfp,
      ctk,
      budgetE,
      costCNum,
    ],
  );

  for (const bind of body.sprite_binds) {
    const sid = Number.parseInt(bind.sprite_id, 10);
    await client.query(
      `insert into catalog_asset_sprite (asset_id, sprite_id, slot) values ($1,$2,$3)`,
      [aNum, sid, bind.slot],
    );
  }
  return assetIdStr;
}

async function runPatch(
  client: PoolClient,
  input: Extract<CatalogUpsertInput, { mode: "patch" }>,
): Promise<{ ok: true; composite: NonNullable<Awaited<ReturnType<typeof loadCatalogAssetComposite>>> } | { ok: "conflict"; current: NonNullable<Awaited<ReturnType<typeof loadCatalogAssetComposite>>> } | { ok: "notfound" }> {
  const idNum = Number.parseInt(input.asset_id, 10);
  if (!Number.isSafeInteger(idNum) || idNum < 1) {
    throw { code: "invalid_input" as const, message: "Invalid asset_id." };
  }

  const curR = await client.query(`select * from catalog_asset where id = $1 limit 1`, [idNum]);
  if (curR.rows.length === 0) return { ok: "notfound" } as const;
  const base = mapRowToCatalogAsset(curR.rows[0] as never);
  const next = {
    category: base.category,
    slug: base.slug,
    display_name: input.display_name !== undefined ? input.display_name : base.display_name,
    status: input.status !== undefined ? input.status : base.status,
    replaced_by: input.replaced_by !== undefined ? input.replaced_by : base.replaced_by,
    footprint_w: input.footprint_w !== undefined ? input.footprint_w : base.footprint_w,
    footprint_h: input.footprint_h !== undefined ? input.footprint_h : base.footprint_h,
    placement_mode: input.placement_mode !== undefined ? input.placement_mode : base.placement_mode,
    unlocks_after: input.unlocks_after !== undefined ? input.unlocks_after : base.unlocks_after,
    has_button: input.has_button !== undefined ? input.has_button : base.has_button,
  };
  const rep =
    next.replaced_by == null || next.replaced_by === ""
      ? null
      : Number.parseInt(String(next.replaced_by), 10);
  if (next.replaced_by != null && next.replaced_by !== "" && (rep == null || Number.isNaN(rep))) {
    throw { code: "invalid_input" as const, message: "replaced_by must be a numeric id" };
  }

  const urow = await client.query(
    `update catalog_asset set
        category = $1,
        slug = $2,
        display_name = $3,
        status = $4,
        replaced_by = $5,
        footprint_w = $6,
        footprint_h = $7,
        placement_mode = $8,
        unlocks_after = $9,
        has_button = $10,
        updated_at = now()
      where id = $11 and updated_at = $12
      returning *`,
    [
      next.category,
      next.slug,
      next.display_name,
      next.status,
      rep,
      next.footprint_w,
      next.footprint_h,
      next.placement_mode,
      next.unlocks_after,
      next.has_button,
      idNum,
      input.updated_at,
    ],
  );

  if (urow.rows.length === 0) {
    const fr = await loadCatalogAssetComposite(client, input.asset_id);
    if (fr === "notfound" || fr === "badid") return { ok: "notfound" as const };
    return { ok: "conflict", current: fr };
  }

  if (input.economy && Object.keys(input.economy).length > 0) {
    const e = input.economy;
    const erow = await client.query(`select * from catalog_economy where asset_id = $1 limit 1`, [
      idNum,
    ]);
    if (erow.rows.length === 0) {
      throw { code: "db_error" as const, message: "economy row missing for asset" };
    }
    const ex = { ...mapRowToEconomy(erow.rows[0] as never) };
    if (e.base_cost_cents !== undefined) ex.base_cost_cents = e.base_cost_cents;
    if (e.monthly_upkeep_cents !== undefined) ex.monthly_upkeep_cents = e.monthly_upkeep_cents;
    if (e.demolition_refund_pct !== undefined) ex.demolition_refund_pct = e.demolition_refund_pct;
    if (e.construction_ticks !== undefined) ex.construction_ticks = e.construction_ticks;
    if (e.budget_envelope_id !== undefined) ex.budget_envelope_id = e.budget_envelope_id;
    if (e.cost_catalog_row_id !== undefined) ex.cost_catalog_row_id = e.cost_catalog_row_id;
    const costC =
      ex.cost_catalog_row_id == null || ex.cost_catalog_row_id === ""
        ? null
        : Number.parseInt(String(ex.cost_catalog_row_id), 10);
    await client.query(
      `update catalog_economy set
          base_cost_cents = $1,
          monthly_upkeep_cents = $2,
          demolition_refund_pct = $3,
          construction_ticks = $4,
          budget_envelope_id = $5,
          cost_catalog_row_id = $6
        where asset_id = $7`,
      [
        ex.base_cost_cents,
        ex.monthly_upkeep_cents,
        ex.demolition_refund_pct,
        ex.construction_ticks,
        ex.budget_envelope_id,
        costC,
        idNum,
      ],
    );
  }

  const composite = await loadCatalogAssetComposite(client, input.asset_id);
  if (composite === "notfound" || composite === "badid") return { ok: "notfound" as const };
  return { ok: true, composite };
}

export async function runCatalogUpsert(input: CatalogUpsertInput): Promise<unknown> {
  checkCaller("catalog_upsert", input.caller_agent);
  const pool = getIaDatabasePool();
  if (!pool) throw dbUnconfiguredError();

  const client = await pool.connect();
  try {
    await client.query("BEGIN");
    try {
      if (input.mode === "create") {
        const id = await runCreate(client, input.body);
        await client.query("COMMIT");
        const composite = await loadCatalogAssetComposite(client, id);
        if (composite === "notfound" || composite === "badid") {
          throw { code: "db_error" as const, message: "Read-after-create failed" };
        }
        return { created: true, asset_id: id, composite };
      }

      const patchResult = await runPatch(client, input);
      if (patchResult.ok === "notfound") {
        await client.query("ROLLBACK");
        throw {
          code: "invalid_input" as const,
          message: "Asset not found.",
          details: { asset_id: input.asset_id },
        };
      }
      if (patchResult.ok === "conflict") {
        await client.query("ROLLBACK");
        return {
          created: false,
          patch_result: "conflict",
          message: "Stale updated_at — row changed since updated_at token.",
          current: patchResult.current,
        };
      }
      await client.query("COMMIT");
      return { created: false, composite: patchResult.composite };
    } catch (e) {
      await client.query("ROLLBACK");
      throw e;
    }
  } finally {
    client.release();
  }
}

export function registerCatalogUpsert(server: McpServer): void {
  server.registerTool(
    "catalog_upsert",
    {
      description:
        "Create (**mode:create**) or optimistic-lock patch (**mode:patch**) a **catalog_asset** row (+ economy + sprite_binds on create). Matches HTTP **POST /api/catalog/assets** and **PATCH /api/catalog/assets/:id** semantics. **caller_agent** required; gated per **caller-allowlist** (ship-stage, stage-file, project-new, closeout). Requires Postgres catalog migrations applied.",
      inputSchema: catalogUpsertInputSchema,
    },
    async (args) =>
      runWithToolTiming("catalog_upsert", async () => {
        const envelope = await wrapTool(async (input: CatalogUpsertInput) => {
          try {
            return await runCatalogUpsert(input);
          } catch (e) {
            const code =
              e && typeof e === "object" && "code" in e ? String((e as { code?: string }).code) : "";
            if (code === "42P01") {
              throw {
                code: "db_error" as const,
                message: "catalog tables missing. Run `npm run db:migrate` from the repository root.",
                hint: "Run `npm run db:migrate`",
              };
            }
            throw e;
          }
        })(catalogUpsertInputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
