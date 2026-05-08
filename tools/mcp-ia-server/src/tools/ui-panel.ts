/**
 * MCP tools: ui_panel_get + ui_panel_list + ui_panel_publish.
 *
 * Three-sub-tool MCP slice for panel CRUD.
 * get(slug):    panel_detail row + linked corpus rows.
 * list():       slug + status + rect_json summary.
 * publish(slug): increments current_published_version_id + flags snapshot regen.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";
import { resolveRepoRoot } from "../config.js";

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
            const result = await client.query(
              `SELECT ce.id, ce.slug, ce.kind, ce.display_name,
                      ce.current_published_version_id, ce.tags,
                      ce.created_at, ce.updated_at,
                      pd.rect_json, pd.layout, pd.padding_json, pd.gap_px, pd.params_json,
                      pd.layout_template, pd.modal
               FROM panel_detail pd
               JOIN catalog_entity ce ON ce.id = pd.entity_id
               WHERE ce.kind = 'panel' AND ce.slug = $1`,
              [input.slug],
            );

            if (result.rows.length === 0) {
              return null;
            }

            const row = result.rows[0];
            const repoRoot = resolveRepoRoot();
            const corpusRows = readCorpusForSlug(repoRoot, input.slug);

            return {
              panel: row,
              corpus_rows: corpusRows,
              corpus_count: corpusRows.length,
            };
          } catch (e) {
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
        "Publish a panel: increments current_published_version_id on catalog_entity and returns new version id. Flags snapshot regen required when regen_snapshot=true (default). Returns {slug, prev_version_id, new_version_id, snapshot_regen_required}.",
      inputSchema: panelPublishSchema,
    },
    async (args) =>
      runWithToolTiming("ui_panel_publish", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof panelPublishSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            // Fetch entity + current published version row
            const fetchResult = await client.query(
              `SELECT ce.id AS entity_id, ce.current_published_version_id,
                      ev.version_number AS current_version_number
               FROM catalog_entity ce
               LEFT JOIN entity_version ev ON ev.id = ce.current_published_version_id
               WHERE ce.kind = 'panel' AND ce.slug = $1`,
              [input.slug],
            );

            if (fetchResult.rows.length === 0) {
              throw {
                code: "invalid_input" as const,
                message: `Panel not found: ${input.slug}`,
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
        })(panelPublishSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
