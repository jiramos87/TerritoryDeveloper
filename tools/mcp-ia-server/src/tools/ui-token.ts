/**
 * MCP tools: ui_token_get + ui_token_list + ui_token_publish.
 *
 * Three-sub-tool MCP slice for token CRUD.
 * get(slug):    token_detail row + spine (catalog_entity) + linked panel consumers.
 * list():       kind=token rows with status filter.
 * publish(slug): increments entity_version + flags tokens.json regen.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

// ── ui_token_get ─────────────────────────────────────────────────────────

const tokenGetSchema = z.object({
  slug: z.string().describe("Token slug (e.g. 'color-bg-cream')."),
});

export function registerUiTokenGet(server: McpServer): void {
  server.registerTool(
    "ui_token_get",
    {
      description:
        "Get one token by slug: catalog_entity spine (id, slug, kind, display_name, current_published_version_id) joined to token_detail (token_kind, value_json, semantic_target_entity_id) plus linked panel consumers (panel slugs whose params_json references this token slug). Returns null payload when slug not found.",
      inputSchema: tokenGetSchema,
    },
    async (args) =>
      runWithToolTiming("ui_token_get", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof tokenGetSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            // Fetch token entity + detail
            const result = await client.query(
              `SELECT ce.id, ce.slug, ce.kind, ce.display_name,
                      ce.current_published_version_id, ce.tags,
                      ce.created_at, ce.updated_at,
                      td.token_kind, td.value_json, td.semantic_target_entity_id
               FROM token_detail td
               JOIN catalog_entity ce ON ce.id = td.entity_id
               WHERE ce.kind = 'token' AND ce.slug = $1`,
              [input.slug],
            );

            if (result.rows.length === 0) {
              return null;
            }

            const row = result.rows[0];

            // Reverse-lookup linked panel consumers: panel_detail JSONB grep
            const consumerResult = await client.query(
              `SELECT ce.slug AS panel_slug
               FROM panel_detail pd
               JOIN catalog_entity ce ON ce.id = pd.entity_id
               WHERE ce.kind = 'panel'
                 AND pd.params_json::text ILIKE $1`,
              [`%${input.slug}%`],
            );

            const consumers = consumerResult.rows.map(
              (r: { panel_slug: string }) => r.panel_slug,
            );

            return {
              token: row,
              panel_consumers: consumers,
              consumer_count: consumers.length,
            };
          } catch (e) {
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error" as const, message: msg };
          } finally {
            client.release();
          }
        })(tokenGetSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

// ── ui_token_list ────────────────────────────────────────────────────────

const tokenListSchema = z.object({
  token_kind: z
    .enum(["color", "type-scale", "motion", "spacing", "semantic"])
    .optional()
    .describe("Filter by token_kind (color|type-scale|motion|spacing|semantic). Omit for all."),
  include_retired: z
    .boolean()
    .optional()
    .default(false)
    .describe("Include retired/archived tokens (default false)."),
});

export function registerUiTokenList(server: McpServer): void {
  server.registerTool(
    "ui_token_list",
    {
      description:
        "List tokens: slug, display_name, token_kind, value_json summary, current_published_version_id. Filter by token_kind (color|type-scale|motion|spacing|semantic). Excludes retired tokens by default.",
      inputSchema: tokenListSchema,
    },
    async (args) =>
      runWithToolTiming("ui_token_list", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof tokenListSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            const retiredFilter = input.include_retired ? "" : "AND ce.retired_at IS NULL";
            const kindFilter = input.token_kind ? "AND td.token_kind = $2" : "";
            const params: unknown[] = input.token_kind
              ? [input.token_kind]
              : [];

            const result = await client.query(
              `SELECT ce.id, ce.slug, ce.display_name,
                      ce.current_published_version_id, ce.retired_at,
                      td.token_kind, td.value_json
               FROM token_detail td
               JOIN catalog_entity ce ON ce.id = td.entity_id
               WHERE ce.kind = 'token' ${retiredFilter} ${kindFilter}
               ORDER BY td.token_kind, ce.slug`,
              params,
            );

            const tokens = result.rows.map((r) => ({
              id: r.id,
              slug: r.slug,
              display_name: r.display_name,
              token_kind: r.token_kind,
              value_json: r.value_json,
              current_published_version_id: r.current_published_version_id,
              retired: r.retired_at != null,
            }));

            return { tokens, total: tokens.length };
          } catch (e) {
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error" as const, message: msg };
          } finally {
            client.release();
          }
        })(tokenListSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

// ── ui_token_publish ─────────────────────────────────────────────────────

const tokenPublishSchema = z.object({
  slug: z.string().describe("Token slug to publish (e.g. 'color-bg-cream')."),
  regen_snapshot: z
    .boolean()
    .optional()
    .default(true)
    .describe("Mark tokens.json snapshot regen required (default true)."),
});

export function registerUiTokenPublish(server: McpServer): void {
  server.registerTool(
    "ui_token_publish",
    {
      description:
        "Publish a token: increments entity_version on catalog_entity and returns new version id. Flags tokens.json snapshot regen required when regen_snapshot=true (default). Returns {slug, prev_version_id, new_version_id, new_version_number, snapshot_regen_required}.",
      inputSchema: tokenPublishSchema,
    },
    async (args) =>
      runWithToolTiming("ui_token_publish", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof tokenPublishSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            // Fetch entity + current published version
            const fetchResult = await client.query(
              `SELECT ce.id AS entity_id, ce.current_published_version_id,
                      ev.version_number AS current_version_number
               FROM catalog_entity ce
               LEFT JOIN entity_version ev ON ev.id = ce.current_published_version_id
               WHERE ce.kind = 'token' AND ce.slug = $1`,
              [input.slug],
            );

            if (fetchResult.rows.length === 0) {
              throw {
                code: "invalid_input" as const,
                message: `Token not found: ${input.slug}`,
              };
            }

            const {
              entity_id: entityId,
              current_published_version_id: prevVersionId,
              current_version_number: currentVersionNum,
            } = fetchResult.rows[0] as {
              entity_id: string;
              current_published_version_id: string | null;
              current_version_number: number | null;
            };

            const nextVersionNumber = (currentVersionNum ?? 0) + 1;

            // Insert new entity_version row
            const insertResult = await client.query(
              `INSERT INTO entity_version (entity_id, version_number, status, parent_version_id, created_at, updated_at)
               VALUES ($1, $2, 'published', $3, NOW(), NOW())
               RETURNING id`,
              [entityId, nextVersionNumber, prevVersionId],
            );

            const newVersionId = (insertResult.rows[0] as { id: string }).id;

            // Update current_published_version_id
            await client.query(
              `UPDATE catalog_entity
               SET current_published_version_id = $1, updated_at = NOW()
               WHERE id = $2`,
              [newVersionId, entityId],
            );

            return {
              slug: input.slug,
              entity_id: entityId,
              prev_version_id: prevVersionId,
              new_version_id: newVersionId,
              new_version_number: nextVersionNumber,
              snapshot_regen_required: input.regen_snapshot,
            };
          } catch (e) {
            if (e && typeof e === "object" && "code" in e) throw e;
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error" as const, message: msg };
          } finally {
            client.release();
          }
        })(tokenPublishSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
