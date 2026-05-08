/**
 * MCP tool: ui_def_drift_scan — DB ↔ panels.json rect_json drift gate.
 * Wraps validate-ui-def-drift.mjs logic as a structured MCP response.
 * Returns {drifts, total_panels, total_drifts} on any DB+snapshot pair.
 * No input required (optional slug filter for subset scan).
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";
import { resolveRepoRoot } from "../config.js";

const SNAPSHOT_REL = "Assets/UI/Snapshots/panels.json";

interface DriftEntry {
  slug: string;
  field: string;
  db_value: unknown;
  snapshot_value: unknown;
}

interface DriftScanResult {
  drifts: DriftEntry[];
  total_panels: number;
  total_drifts: number;
}

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const inputSchema = z.object({
  slug_filter: z
    .string()
    .optional()
    .describe("Optional panel slug to restrict scan (omit for all panels)."),
});

export function registerUiDefDriftScan(server: McpServer): void {
  server.registerTool(
    "ui_def_drift_scan",
    {
      description:
        "Scan panel_detail DB rows vs Assets/UI/Snapshots/panels.json for rect_json drift. Returns {drifts:[{slug,field,db_value,snapshot_value}], total_panels, total_drifts}. No input required; optional slug_filter to restrict scan. Requires DATABASE_URL / config/postgres-dev.json and panels.json snapshot.",
      inputSchema,
    },
    async (args) =>
      runWithToolTiming("ui_def_drift_scan", async () => {
        const envelope = await wrapTool(
          async (input: z.infer<typeof inputSchema>) => {
            const pool = getIaDatabasePool();
            if (!pool) throw dbUnconfiguredError();

            const repoRoot = resolveRepoRoot();
            const snapshotPath = path.join(repoRoot, SNAPSHOT_REL);

            // ── 1. Snapshot ──────────────────────────────────────────────
            if (!fs.existsSync(snapshotPath)) {
              throw {
                code: "invalid_input" as const,
                message: `panels.json missing at ${SNAPSHOT_REL}. Run snapshot export first.`,
              };
            }

            let snapshotItems: Array<{ slug?: string; fields?: { rect_json?: unknown } }>;
            try {
              const raw = fs.readFileSync(snapshotPath, "utf8");
              const parsed = JSON.parse(raw) as { items?: typeof snapshotItems };
              snapshotItems = parsed.items ?? [];
            } catch (err) {
              const msg = err instanceof Error ? err.message : String(err);
              throw { code: "internal_error" as const, message: `panels.json parse error: ${msg}` };
            }

            const snapshotMap = new Map<string, unknown>();
            for (const item of snapshotItems) {
              const slug = item.slug;
              const rawRect = item.fields?.rect_json;
              if (slug && rawRect !== undefined) {
                let parsed = rawRect;
                if (typeof rawRect === "string") {
                  try {
                    parsed = JSON.parse(rawRect);
                  } catch {
                    parsed = rawRect;
                  }
                }
                snapshotMap.set(slug, parsed);
              }
            }

            // ── 2. DB ────────────────────────────────────────────────────
            const client = await pool.connect();
            let rows: Array<{ slug: string; rect_json: unknown }>;
            try {
              const query = input.slug_filter
                ? `SELECT ce.slug, pd.rect_json
                   FROM panel_detail pd
                   JOIN catalog_entity ce ON ce.id = pd.entity_id
                   WHERE ce.kind = 'panel' AND ce.slug = $1`
                : `SELECT ce.slug, pd.rect_json
                   FROM panel_detail pd
                   JOIN catalog_entity ce ON ce.id = pd.entity_id
                   WHERE ce.kind = 'panel'`;
              const params = input.slug_filter ? [input.slug_filter] : [];
              const result = await client.query(query, params);
              rows = result.rows as typeof rows;
            } catch (e) {
              const msg = e instanceof Error ? e.message : String(e);
              throw { code: "db_error" as const, message: msg };
            } finally {
              client.release();
            }

            // ── 3. Diff ──────────────────────────────────────────────────
            const drifts: DriftEntry[] = [];

            for (const { slug, rect_json: dbRect } of rows) {
              if (!snapshotMap.has(slug)) continue;

              const snapRect = snapshotMap.get(slug);
              const dbObj = typeof dbRect === "string" ? (JSON.parse(dbRect) as Record<string, unknown>) : ((dbRect ?? {}) as Record<string, unknown>);
              const snapObj = typeof snapRect === "string" ? (JSON.parse(snapRect as string) as Record<string, unknown>) : ((snapRect ?? {}) as Record<string, unknown>);

              const allKeys = new Set([...Object.keys(dbObj), ...Object.keys(snapObj)]);
              for (const field of allKeys) {
                const dbVal = JSON.stringify(dbObj[field] ?? null);
                const snapVal = JSON.stringify(snapObj[field] ?? null);
                if (dbVal !== snapVal) {
                  drifts.push({
                    slug,
                    field,
                    db_value: dbObj[field] ?? null,
                    snapshot_value: snapObj[field] ?? null,
                  });
                  break; // one entry per slug
                }
              }
            }

            const result: DriftScanResult = {
              drifts,
              total_panels: rows.length,
              total_drifts: drifts.length,
            };
            return result;
          },
        )(inputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
