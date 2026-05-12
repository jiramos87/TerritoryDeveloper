/**
 * MCP tools: ui_panel_get + ui_panel_list + ui_panel_publish + panel_detail_update.
 *
 * All write paths delegate to the shared `ia-db/ui-catalog.ts` DAL — the web
 * `asset-pipeline` backend will import the same module, so SQL stays single-sourced.
 *
 * get(slug):    panel_detail row + linked corpus rows.
 * list():       slug + status + rect_json summary.
 * publish(slug): version bump (no gates — use catalog_panel_publish for gated flow).
 * panel_detail_update(slug, patch): scalar overwrite + JSONB shallow-merge.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";
import { resolveRepoRoot } from "../config.js";
import {
  getPanelBundle,
  publishPanel,
  updatePanelDetail,
} from "../ia-db/ui-catalog.js";

const CORPUS_REL = "ia/state/ui-calibration-corpus.jsonl";

interface CorpusRow {
  panel_slug: string;
  [key: string]: unknown;
}

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

function readCorpusForSlug(repoRoot: string, slug: string): CorpusRow[] {
  const filePath = path.join(repoRoot, CORPUS_REL);
  if (!fs.existsSync(filePath)) return [];
  const raw = fs.readFileSync(filePath, "utf8");
  return raw
    .split("\n")
    .filter((line) => line.trim().length > 0)
    .map((line) => JSON.parse(line) as CorpusRow)
    .filter((r) => r.panel_slug === slug);
}

// ── ui_panel_get ─────────────────────────────────────────────────────────

const panelGetSchema = z.object({
  slug: z.string().describe("Panel slug (e.g. 'hud-bar')."),
});

export function registerUiPanelGet(server: McpServer): void {
  server.registerTool(
    "ui_panel_get",
    {
      description:
        "Get one panel by slug: panel_detail row (rect_json, layout, padding_json, gap_px, params_json) joined to catalog_entity (id, kind, display_name, current_published_version_id) plus linked ui-calibration-corpus rows for the slug. Returns null payload when slug not found.",
      inputSchema: panelGetSchema,
    },
    async (args) =>
      runWithToolTiming("ui_panel_get", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof panelGetSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            const bundle = await getPanelBundle(client, input.slug);
            if (bundle == null) return null;

            const repoRoot = resolveRepoRoot();
            const corpusRows = readCorpusForSlug(repoRoot, input.slug);

            // Flatten for response compatibility with prior shape.
            const panel = {
              id: bundle.entity.id,
              slug: bundle.entity.slug,
              kind: bundle.entity.kind,
              display_name: bundle.entity.display_name,
              current_published_version_id: bundle.entity.current_published_version_id,
              tags: bundle.entity.tags,
              created_at: bundle.entity.created_at,
              updated_at: bundle.entity.updated_at,
              rect_json: bundle.detail.rect_json,
              layout: bundle.detail.layout,
              padding_json: bundle.detail.padding_json,
              gap_px: bundle.detail.gap_px,
              params_json: bundle.detail.params_json,
              layout_template: bundle.detail.layout_template,
              modal: bundle.detail.modal,
            };

            return {
              panel,
              corpus_rows: corpusRows,
              corpus_count: corpusRows.length,
            };
          } catch (e) {
            if (e && typeof e === "object" && "code" in e) throw e;
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error" as const, message: msg };
          } finally {
            client.release();
          }
        })(panelGetSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

// ── ui_panel_list ────────────────────────────────────────────────────────

const panelListSchema = z.object({
  include_retired: z
    .boolean()
    .optional()
    .default(false)
    .describe("Include retired/archived panels (default false)."),
});

export function registerUiPanelList(server: McpServer): void {
  server.registerTool(
    "ui_panel_list",
    {
      description:
        "List all panels: slug, display_name, current_published_version_id, rect_json summary (anchor_min, anchor_max, size_delta only). Excludes retired panels by default.",
      inputSchema: panelListSchema,
    },
    async (args) =>
      runWithToolTiming("ui_panel_list", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof panelListSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            const retiredFilter = input.include_retired ? "" : "AND ce.retired_at IS NULL";
            const result = await client.query(
              `SELECT ce.id, ce.slug, ce.display_name,
                      ce.current_published_version_id, ce.retired_at,
                      pd.rect_json
               FROM panel_detail pd
               JOIN catalog_entity ce ON ce.id = pd.entity_id
               WHERE ce.kind = 'panel' ${retiredFilter}
               ORDER BY ce.slug`,
            );

            const panels = result.rows.map((r) => {
              const rect = r.rect_json as Record<string, unknown> | null;
              return {
                id: r.id,
                slug: r.slug,
                display_name: r.display_name,
                current_published_version_id: r.current_published_version_id,
                retired: r.retired_at != null,
                rect_summary: rect
                  ? {
                      anchor_min: rect.anchor_min,
                      anchor_max: rect.anchor_max,
                      size_delta: rect.size_delta,
                    }
                  : null,
              };
            });

            return { panels, total: panels.length };
          } catch (e) {
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error" as const, message: msg };
          } finally {
            client.release();
          }
        })(panelListSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

// ── ui_panel_publish ─────────────────────────────────────────────────────

const panelPublishSchema = z.object({
  slug: z.string().describe("Panel slug to publish (e.g. 'hud-bar')."),
  regen_snapshot: z
    .boolean()
    .optional()
    .default(true)
    .describe("Mark snapshot regen required (default true). Agent should run snapshot export after publish."),
});

export function registerUiPanelPublish(server: McpServer): void {
  server.registerTool(
    "ui_panel_publish",
    {
      description:
        "Publish a panel: increments current_published_version_id on catalog_entity and returns new version id. Flags snapshot regen required when regen_snapshot=true (default). Returns {slug, prev_version_id, new_version_id, snapshot_regen_required}. No author-time gates — use catalog_panel_publish for the gated flow.",
      inputSchema: panelPublishSchema,
    },
    async (args) =>
      runWithToolTiming("ui_panel_publish", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof panelPublishSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            await client.query("BEGIN");
            const result = await publishPanel(client, input.slug);
            await client.query("COMMIT");

            return {
              slug: input.slug,
              entity_id: result.entity_id,
              prev_version_id: result.prev_version_id,
              new_version_id: result.new_version_id,
              new_version_number: result.new_version_number,
              snapshot_regen_required: input.regen_snapshot,
            };
          } catch (e) {
            await client.query("ROLLBACK").catch(() => {});
            if (e && typeof e === "object" && "code" in e) throw e;
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error" as const, message: msg };
          } finally {
            client.release();
          }
        })(panelPublishSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

// ── panel_detail_update ──────────────────────────────────────────────────

const panelDetailUpdateSchema = z.object({
  slug: z.string().describe("Panel slug (e.g. 'stats-panel')."),
  layout_template: z.string().optional(),
  layout: z.string().optional(),
  modal: z.boolean().optional(),
  gap_px: z.number().int().optional(),
  padding_json: z.record(z.string(), z.unknown()).optional()
    .describe("Shallow-merged into existing padding_json. Use json_strategy:'replace' to overwrite."),
  params_json: z.record(z.string(), z.unknown()).optional()
    .describe("Shallow-merged into existing params_json."),
  rect_json: z.record(z.string(), z.unknown()).optional()
    .describe("Shallow-merged into existing rect_json."),
  json_strategy: z.enum(["merge", "replace"]).optional().default("merge"),
});

export function registerPanelDetailUpdate(server: McpServer): void {
  server.registerTool(
    "panel_detail_update",
    {
      description:
        "Patch panel_detail fields for one panel (NO version bump — call ui_panel_publish or catalog_panel_publish after). " +
        "Scalar fields (layout_template, layout, modal, gap_px) overwrite. JSONB fields (padding_json, params_json, rect_json) shallow-merge by default; pass json_strategy:'replace' to overwrite. " +
        "Returns {slug, entity_id, updated_fields:[]}. Throws not_found when slug missing.",
      inputSchema: panelDetailUpdateSchema,
    },
    async (args) =>
      runWithToolTiming("panel_detail_update", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof panelDetailUpdateSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            await client.query("BEGIN");
            const result = await updatePanelDetail(
              client,
              input.slug,
              {
                layout_template: input.layout_template,
                layout: input.layout,
                modal: input.modal,
                gap_px: input.gap_px,
                padding_json: input.padding_json,
                params_json: input.params_json,
                rect_json: input.rect_json,
              },
              { jsonStrategy: input.json_strategy },
            );
            await client.query("COMMIT");
            return {
              slug: input.slug,
              entity_id: result.entity_id,
              updated_fields: result.updated_fields,
            };
          } catch (e) {
            await client.query("ROLLBACK").catch(() => {});
            if (e && typeof e === "object" && "code" in e) throw e;
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error" as const, message: msg };
          } finally {
            client.release();
          }
        })(panelDetailUpdateSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
