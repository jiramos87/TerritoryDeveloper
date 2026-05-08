/**
 * MCP tools: ui_component_get + ui_component_list + ui_component_publish.
 *
 * Three-sub-tool MCP slice for component CRUD.
 * get(slug):    component_detail row + spine (catalog_entity) + linked panel consumers.
 * list():       kind=component rows with status filter.
 * publish(slug): increments entity_version + flags components.json regen.
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

// ── ui_component_get ──────────────────────────────────────────────────────

const componentGetSchema = z.object({
  slug: z.string().describe("Component slug (e.g. 'icon-button')."),
});

export function registerUiComponentGet(server: McpServer): void {
  server.registerTool(
    "ui_component_get",
    {
      description:
        "Get one component by slug: catalog_entity spine (id, slug, kind, display_name, current_published_version_id) joined to component_detail (role, default_props_json, variants_json) plus linked panel consumers (panel slugs whose params_json references this component slug). Returns null payload when slug not found.",
      inputSchema: componentGetSchema,
    },
    async (args) =>
      runWithToolTiming("ui_component_get", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof componentGetSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            // Fetch component entity + detail
            const result = await client.query(
              `SELECT ce.id, ce.slug, ce.kind, ce.display_name,
                      ce.current_published_version_id, ce.tags,
                      ce.created_at, ce.updated_at,
                      cd.role, cd.default_props_json, cd.variants_json
               FROM component_detail cd
               JOIN catalog_entity ce ON ce.id = cd.entity_id
               WHERE ce.kind = 'component' AND ce.slug = $1`,
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
              component: row,
              panel_consumers: consumers,
              consumer_count: consumers.length,
            };
          } catch (e) {
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error" as const, message: msg };
          } finally {
            client.release();
          }
        })(componentGetSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

// ── ui_component_list ─────────────────────────────────────────────────────

const componentListSchema = z.object({
  include_retired: z
    .boolean()
    .optional()
    .default(false)
    .describe("Include retired/archived components (default false)."),
});

export function registerUiComponentList(server: McpServer): void {
  server.registerTool(
    "ui_component_list",
    {
      description:
        "List all components: slug, display_name, role, variants_json, current_published_version_id. Excludes retired components by default.",
      inputSchema: componentListSchema,
    },
    async (args) =>
      runWithToolTiming("ui_component_list", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof componentListSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            const retiredFilter = input.include_retired ? "" : "AND ce.retired_at IS NULL";
            const result = await client.query(
              `SELECT ce.id, ce.slug, ce.display_name,
                      ce.current_published_version_id, ce.retired_at,
                      cd.role, cd.variants_json
               FROM component_detail cd
               JOIN catalog_entity ce ON ce.id = cd.entity_id
               WHERE ce.kind = 'component' ${retiredFilter}
               ORDER BY ce.slug`,
            );

            const components = result.rows.map((r) => ({
              id: r.id,
              slug: r.slug,
              display_name: r.display_name,
              role: r.role,
              variants_json: r.variants_json,
              current_published_version_id: r.current_published_version_id,
              retired: r.retired_at != null,
            }));

            return { components, total: components.length };
          } catch (e) {
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error" as const, message: msg };
          } finally {
            client.release();
          }
        })(componentListSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

// ── ui_component_publish ─────────────────────────────────────────────────

const componentPublishSchema = z.object({
  slug: z.string().describe("Component slug to publish (e.g. 'icon-button')."),
  regen_snapshot: z
    .boolean()
    .optional()
    .default(true)
    .describe("Mark components.json snapshot regen required (default true)."),
});

export function registerUiComponentPublish(server: McpServer): void {
  server.registerTool(
    "ui_component_publish",
    {
      description:
        "Publish a component: increments entity_version on catalog_entity and returns new version id. Flags components.json snapshot regen required when regen_snapshot=true (default). Returns {slug, prev_version_id, new_version_id, new_version_number, snapshot_regen_required}.",
      inputSchema: componentPublishSchema,
    },
    async (args) =>
      runWithToolTiming("ui_component_publish", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof componentPublishSchema>) => {
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
               WHERE ce.kind = 'component' AND ce.slug = $1`,
              [input.slug],
            );

            if (fetchResult.rows.length === 0) {
              throw {
                code: "invalid_input" as const,
                message: `Component not found: ${input.slug}`,
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
        })(componentPublishSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
