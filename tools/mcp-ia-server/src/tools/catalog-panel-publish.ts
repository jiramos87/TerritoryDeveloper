/**
 * MCP tool: catalog_panel_publish — publish a panel with Layer 1 author-time gates.
 *
 * Gates (all pre-INSERT, inside tx):
 *   T1.0.1  archetype×kind renderer coverage  → archetype_no_renderer
 *   T1.0.2  action-id sink uniqueness          → action_id_sink_collision
 *   T1.0.3  bind-id contract                   → unknown_bind_id
 *   T1.0.4  token reference graph              → dangling_token_ref
 *   T1.0.5  view-slot anchor required-by       → unanchored_view
 *
 * Returns { ok: true, slug, new_version_id } on success.
 * Returns { ok: false, errors[] } on gate failure (no DB mutation occurs).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import {
  validateArchetypeKindCoverage,
  validateActionIdSinkUniqueness,
  registerActionIdSinks,
  validateBindIdContract,
  registerDeclaredBindIds,
  validateTokenReferences,
  validateViewSlotAnchors,
} from "../ia-db/mutations/catalog-panel.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const childSchema = z.object({
  kind: z.string().optional(),
  action_id: z.string().optional(),
  bind_id: z.string().optional(),
  declare_on_publish: z.boolean().optional(),
  params_json: z.unknown().optional(),
  slot_name: z.string().optional(),
  instance_slug: z.string().optional(),
  order_idx: z.number().int().optional(),
});

const catalogPanelPublishSchema = z.object({
  slug: z.string().min(1).describe("Panel slug (e.g. 'hud-bar')."),
  children: z
    .array(childSchema)
    .optional()
    .default([])
    .describe("Panel children — each must pass kind/action-id/bind-id gates."),
  params_json: z.unknown().optional().describe("Top-level panel params; token-* refs validated."),
  views: z
    .array(z.string())
    .optional()
    .default([])
    .describe("Sub-view names (e.g. ['audio', 'video']); each must have a catalog_panel_anchors row."),
  regen_snapshot: z.boolean().optional().default(true),
});

export type CatalogPanelPublishInput = z.infer<typeof catalogPanelPublishSchema>;

export function registerCatalogPanelPublish(server: McpServer): void {
  server.registerTool(
    "catalog_panel_publish",
    {
      description:
        "Publish a panel with Layer 1 author-time DB gates (T1.0.1–T1.0.5). " +
        "Blocks publish when: archetype kind has no renderer (archetype_no_renderer), " +
        "action_id already claimed by another panel (action_id_sink_collision), " +
        "bind_id not in registry without declare_on_publish (unknown_bind_id), " +
        "token-* ref not in ui_design_tokens/ui_token_aliases (dangling_token_ref), " +
        "views[] slot has no catalog_panel_anchors row (unanchored_view). " +
        "Returns {ok:true, slug, new_version_id} or {ok:false, errors:[{code,message,field}]}.",
      inputSchema: catalogPanelPublishSchema,
    },
    async (args) =>
      runWithToolTiming("catalog_panel_publish", async () => {
        let input: CatalogPanelPublishInput;
        try {
          input = catalogPanelPublishSchema.parse(args ?? {});
        } catch (e) {
          const err = { code: "invalid_input", message: e instanceof Error ? e.message : String(e) };
          return jsonResult({ ok: false, error: err, errors: [err] });
        }
        const pool = getIaDatabasePool();
        if (!pool) {
          const ncErr = { code: "db_unconfigured", message: "DATABASE_URL not configured" };
          return jsonResult({ ok: false, error: ncErr, errors: [ncErr] });
        }

        // ── T1.0.1 archetype×kind (synchronous, no DB needed) ──────────────
        const kindResult = validateArchetypeKindCoverage({
          slug: input.slug,
          children: input.children,
        });
        if (!kindResult.ok) {
          return jsonResult({ ok: false, error: kindResult.errors[0], errors: kindResult.errors });
        }

        const client = await pool.connect();
        try {
          await client.query("BEGIN");

          // ── T1.0.2 action-id sink uniqueness ────────────────────────────
          const actionResult = await validateActionIdSinkUniqueness(
            { slug: input.slug, children: input.children },
            client,
          );
          if (!actionResult.ok) {
            await client.query("ROLLBACK");
            return jsonResult({ ok: false, error: actionResult.errors[0], errors: actionResult.errors });
          }

          // ── T1.0.3 bind-id contract ──────────────────────────────────────
          const bindResult = await validateBindIdContract(
            { slug: input.slug, children: input.children },
            client,
          );
          if (!bindResult.ok) {
            await client.query("ROLLBACK");
            return jsonResult({ ok: false, error: bindResult.errors[0], errors: bindResult.errors });
          }

          // ── T1.0.4 token reference graph ─────────────────────────────────
          const tokenResult = await validateTokenReferences(
            { slug: input.slug, params_json: input.params_json, children: input.children },
            client,
          );
          if (!tokenResult.ok) {
            await client.query("ROLLBACK");
            return jsonResult({ ok: false, error: tokenResult.errors[0], errors: tokenResult.errors });
          }

          // ── T1.0.5 view-slot anchor required-by ──────────────────────────
          const anchorResult = await validateViewSlotAnchors(
            { slug: input.slug, views: input.views },
            client,
          );
          if (!anchorResult.ok) {
            await client.query("ROLLBACK");
            return jsonResult({ ok: false, error: anchorResult.errors[0], errors: anchorResult.errors });
          }

          // ── All gates passed — resolve panel entity ────────────────────────
          const entityRes = await client.query<{
            entity_id: string;
            current_published_version_id: string | null;
            current_version_number: number | null;
          }>(
            `SELECT ce.id AS entity_id, ce.current_published_version_id,
                    ev.version_number AS current_version_number
             FROM catalog_entity ce
             LEFT JOIN entity_version ev ON ev.id = ce.current_published_version_id
             WHERE ce.kind = 'panel' AND ce.slug = $1`,
            [input.slug],
          );

          if (entityRes.rows.length === 0) {
            await client.query("ROLLBACK");
            const nfErr = { code: "not_found", message: `Panel '${input.slug}' not found` };
            return jsonResult({ ok: false, error: nfErr, errors: [nfErr] });
          }

          const { entity_id: entityId, current_published_version_id: prevVersionId, current_version_number: currentVersionNum } =
            entityRes.rows[0]!;

          const nextVersionNumber = (currentVersionNum ?? 0) + 1;

          const insertRes = await client.query<{ id: string }>(
            `INSERT INTO entity_version (entity_id, version_number, status, parent_version_id, created_at, updated_at)
             VALUES ($1, $2, 'published', $3, NOW(), NOW())
             RETURNING id::text AS id`,
            [entityId, nextVersionNumber, prevVersionId],
          );
          const newVersionId = insertRes.rows[0]!.id;

          await client.query(
            `UPDATE catalog_entity SET current_published_version_id = $1, updated_at = NOW() WHERE id = $2`,
            [newVersionId, entityId],
          );

          // ── Side-effects: register sinks and declared binds ────────────────
          await registerActionIdSinks({ slug: input.slug, children: input.children }, client);
          await registerDeclaredBindIds({ slug: input.slug, children: input.children }, client);

          await client.query("COMMIT");

          return jsonResult({
            ok: true,
            slug: input.slug,
            entity_id: entityId,
            prev_version_id: prevVersionId,
            new_version_id: newVersionId,
            new_version_number: nextVersionNumber,
            snapshot_regen_required: input.regen_snapshot,
          });
        } catch (e) {
          await client.query("ROLLBACK");
          const rawErr = e as { code?: string; message?: string };
          const errObj = { code: rawErr.code ?? "db_error", message: rawErr.message ?? String(e) };
          return jsonResult({ ok: false, error: errObj, errors: [errObj] });
        } finally {
          client.release();
        }
      }),
  );
}
