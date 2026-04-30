/**
 * Catalog entity MCP tools — read surface (TECH-5121) and mutate surface
 * (TECH-5122). Targets catalog_entity spine + per-kind detail tables.
 *
 * Tool naming: catalog_{kind}_{op} (read: list/get/get_version/refs/search;
 * mutate: create/update/retire/restore/publish) plus catalog_bulk_action.
 *
 * Envelope: { ok: true, data: ... } per DEC-A48. No wrapTool — new tools build
 * response directly to match REST GET envelope shape exactly.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";

const CATALOG_KINDS = [
  "sprite",
  "asset",
  "button",
  "panel",
  "audio",
  "pool",
  "token",
  "archetype",
] as const;
type CatalogKind = (typeof CATALOG_KINDS)[number];

// ── helpers ──────────────────────────────────────────────────────────────────

function jsonResult(data: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify({ ok: true, data }, null, 2) }],
  };
}

function errResult(code: string, message: string) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify({ ok: false, error: { code, message } }, null, 2) }],
  };
}

function getPool() {
  const pool = getIaDatabasePool();
  if (!pool) throw Object.assign(new Error("DATABASE_URL not configured"), { code: "db_unconfigured" });
  return pool;
}

// Resolve entity numeric id from (kind, slug).
async function resolveEntityId(kind: string, slug: string): Promise<number | null> {
  const pool = getPool();
  const res = await pool.query<{ id: number }>(
    `SELECT id FROM catalog_entity WHERE kind = $1 AND slug = $2 LIMIT 1`,
    [kind, slug],
  );
  return res.rows[0]?.id ?? null;
}

// ── per-kind SQL config ───────────────────────────────────────────────────────

type KindConfig = {
  detailJoin: string;
  listDetailCols: string;
  getDetailCols: string;
};

const KIND_CONFIGS: Record<CatalogKind, KindConfig> = {
  sprite: {
    detailJoin: "LEFT JOIN sprite_detail d ON d.entity_id = e.id",
    listDetailCols: ", d.assets_path, d.pixels_per_unit, d.pivot_x, d.pivot_y, d.provenance",
    getDetailCols:
      ", d.assets_path, d.pixels_per_unit, d.pivot_x, d.pivot_y, d.provenance" +
      ", d.source_uri, d.source_run_id, d.source_variant_idx",
  },
  asset: {
    detailJoin: "LEFT JOIN asset_detail d ON d.entity_id = e.id",
    listDetailCols: ", d.category",
    getDetailCols: ", d.category",
  },
  button: {
    detailJoin: "LEFT JOIN button_detail d ON d.entity_id = e.id",
    listDetailCols: ", d.size_variant, d.action_id",
    getDetailCols: ", d.size_variant, d.action_id",
  },
  panel: {
    detailJoin: "LEFT JOIN panel_detail d ON d.entity_id = e.id",
    listDetailCols: ", d.archetype_entity_id::text AS archetype_entity_id",
    getDetailCols:
      ", d.archetype_entity_id::text AS archetype_entity_id" +
      ", d.background_sprite_entity_id::text AS background_sprite_entity_id" +
      ", d.palette_entity_id::text AS palette_entity_id" +
      ", d.frame_style_entity_id::text AS frame_style_entity_id" +
      ", d.layout_template, d.modal",
  },
  audio: {
    detailJoin: "LEFT JOIN audio_detail d ON d.entity_id = e.id",
    listDetailCols:
      ", d.assets_path, d.source_uri, d.duration_ms, d.sample_rate" +
      ", d.channels, d.loudness_lufs, d.peak_db, d.fingerprint",
    getDetailCols:
      ", d.assets_path, d.source_uri, d.duration_ms, d.sample_rate" +
      ", d.channels, d.loudness_lufs, d.peak_db, d.fingerprint",
  },
  pool: {
    detailJoin: "LEFT JOIN pool_detail d ON d.entity_id = e.id",
    listDetailCols: ", d.owner_category",
    getDetailCols: ", d.owner_category, d.primary_subtype",
  },
  token: {
    detailJoin: "LEFT JOIN token_detail d ON d.entity_id = e.id",
    listDetailCols: ", d.token_kind",
    getDetailCols: ", d.token_kind, d.value_json, d.semantic_target_entity_id::text AS semantic_target_entity_id",
  },
  archetype: {
    detailJoin: "LEFT JOIN entity_version v ON v.id = e.current_published_version_id",
    listDetailCols: ", v.params_json",
    getDetailCols: ", v.params_json, v.id::text AS active_version_id, v.version_number, v.status AS version_status",
  },
};

const BASE_ENTITY_COLS = `
  e.id::text AS entity_id,
  e.slug,
  e.display_name,
  e.tags,
  e.retired_at,
  e.current_published_version_id::text AS current_published_version_id,
  e.updated_at`;

// ── LIST ──────────────────────────────────────────────────────────────────────

const listInputSchema = z.object({
  filter: z.enum(["active", "retired", "all"]).optional().default("active"),
  limit: z.coerce.number().int().min(1).max(500).optional().default(50),
  cursor: z.string().optional(),
});

function registerListTool(server: McpServer, kind: CatalogKind): void {
  const cfg = KIND_CONFIGS[kind];
  server.registerTool(
    `catalog_${kind}_list`,
    {
      description: `List ${kind} catalog entities (kind=${kind}) with keyset cursor pagination. filter: active|retired|all. Returns {items, next_cursor}.`,
      inputSchema: listInputSchema,
    },
    async (args) =>
      runWithToolTiming(`catalog_${kind}_list`, async () => {
        try {
          const pool = getPool();
          const input = listInputSchema.parse(args ?? {});
          const whereParts: string[] = [`e.kind = $1`];
          const params: unknown[] = [kind];

          if (input.filter === "active") whereParts.push(`e.retired_at IS NULL`);
          else if (input.filter === "retired") whereParts.push(`e.retired_at IS NOT NULL`);

          if (input.cursor && /^\d+$/.test(input.cursor)) {
            params.push(Number.parseInt(input.cursor, 10));
            whereParts.push(`e.id > $${params.length}`);
          }
          params.push(input.limit);
          const limitPlaceholder = `$${params.length}`;

          const query = `
            SELECT ${BASE_ENTITY_COLS}${cfg.listDetailCols}
            FROM catalog_entity e ${cfg.detailJoin}
            WHERE ${whereParts.join(" AND ")}
            ORDER BY e.id ASC
            LIMIT ${limitPlaceholder}`;
          const { rows } = await pool.query(query, params);
          const next_cursor =
            rows.length === input.limit
              ? ((rows[rows.length - 1] as Record<string, unknown>)?.entity_id as string) ?? null
              : null;
          return jsonResult({ items: rows, next_cursor });
        } catch (e) {
          const err = e as { code?: string; message?: string };
          return errResult(err.code ?? "db_error", err.message ?? String(e));
        }
      }),
  );
}

// ── GET ───────────────────────────────────────────────────────────────────────

const getInputSchema = z.object({
  slug: z.string().min(1),
});

function registerGetTool(server: McpServer, kind: CatalogKind): void {
  const cfg = KIND_CONFIGS[kind];
  server.registerTool(
    `catalog_${kind}_get`,
    {
      description: `Get a single ${kind} catalog entity by slug. Returns full detail row or not_found error.`,
      inputSchema: getInputSchema,
    },
    async (args) =>
      runWithToolTiming(`catalog_${kind}_get`, async () => {
        try {
          const pool = getPool();
          const input = getInputSchema.parse(args ?? {});
          const query = `
            SELECT ${BASE_ENTITY_COLS}${cfg.getDetailCols}
            FROM catalog_entity e ${cfg.detailJoin}
            WHERE e.kind = $1 AND e.slug = $2
            LIMIT 1`;
          const { rows } = await pool.query(query, [kind, input.slug]);
          if (rows.length === 0) return errResult("not_found", `${kind} '${input.slug}' not found`);
          return jsonResult(rows[0]);
        } catch (e) {
          const err = e as { code?: string; message?: string };
          return errResult(err.code ?? "db_error", err.message ?? String(e));
        }
      }),
  );
}

// ── GET_VERSION ───────────────────────────────────────────────────────────────

const getVersionInputSchema = z.object({
  slug: z.string().min(1),
  version_id: z.string().optional(),
  limit: z.coerce.number().int().min(1).max(100).optional().default(20),
  cursor: z.string().optional(),
});

function registerGetVersionTool(server: McpServer, kind: CatalogKind): void {
  server.registerTool(
    `catalog_${kind}_get_version`,
    {
      description: `Get entity_version rows for a ${kind} entity by slug. Omit version_id for full history list (newest first, cursor paginated). Supply version_id to fetch one version.`,
      inputSchema: getVersionInputSchema,
    },
    async (args) =>
      runWithToolTiming(`catalog_${kind}_get_version`, async () => {
        try {
          const pool = getPool();
          const input = getVersionInputSchema.parse(args ?? {});
          const entityId = await resolveEntityId(kind, input.slug);
          if (entityId == null) return errResult("not_found", `${kind} '${input.slug}' not found`);

          if (input.version_id) {
            const { rows } = await pool.query(
              `SELECT ev.id::text AS version_id, ev.version_number, ev.status, ev.params_json, ev.created_at::text AS created_at
               FROM entity_version ev
               WHERE ev.entity_id = $1 AND ev.id = $2
               LIMIT 1`,
              [entityId, Number.parseInt(input.version_id, 10)],
            );
            if (rows.length === 0) return errResult("not_found", `Version ${input.version_id} not found`);
            return jsonResult(rows[0]);
          }

          const whereParts = [`ev.entity_id = $1`];
          const params: unknown[] = [entityId];

          if (input.cursor && /^\d+$/.test(input.cursor)) {
            params.push(Number.parseInt(input.cursor, 10));
            whereParts.push(`ev.version_number < $${params.length}`);
          }
          params.push(input.limit);
          const { rows } = await pool.query(
            `SELECT ev.id::text AS version_id, ev.version_number, ev.status, ev.params_json, ev.created_at::text AS created_at
             FROM entity_version ev
             WHERE ${whereParts.join(" AND ")}
             ORDER BY ev.version_number DESC
             LIMIT $${params.length}`,
            params,
          );
          const next_cursor =
            rows.length === input.limit
              ? String((rows[rows.length - 1] as Record<string, unknown>)?.version_number ?? "")
              : null;
          return jsonResult({ rows, next_cursor });
        } catch (e) {
          const err = e as { code?: string; message?: string };
          return errResult(err.code ?? "db_error", err.message ?? String(e));
        }
      }),
  );
}

// ── REFS ──────────────────────────────────────────────────────────────────────

const refsInputSchema = z.object({
  slug: z.string().min(1),
  direction: z.enum(["incoming", "outgoing", "both"]).optional().default("both"),
  limit: z.coerce.number().int().min(1).max(100).optional().default(20),
});

function registerRefsTool(server: McpServer, kind: CatalogKind): void {
  server.registerTool(
    `catalog_${kind}_refs`,
    {
      description: `List incoming and/or outgoing catalog_ref_edge rows for a ${kind} entity. direction: incoming|outgoing|both. Returns {incoming, outgoing} arrays.`,
      inputSchema: refsInputSchema,
    },
    async (args) =>
      runWithToolTiming(`catalog_${kind}_refs`, async () => {
        try {
          const pool = getPool();
          const input = refsInputSchema.parse(args ?? {});
          const entityId = await resolveEntityId(kind, input.slug);
          if (entityId == null) return errResult("not_found", `${kind} '${input.slug}' not found`);

          const REF_COLS = `
            ce.src_kind,
            ce.src_id::text AS src_id,
            ce.src_version_id::text AS src_version_id,
            ce.dst_kind,
            ce.dst_id::text AS dst_id,
            ce.dst_version_id::text AS dst_version_id,
            ce.edge_role,
            ce.created_at::text AS created_at`;

          let incoming: unknown[] = [];
          let outgoing: unknown[] = [];

          if (input.direction === "incoming" || input.direction === "both") {
            const { rows } = await pool.query(
              `SELECT ${REF_COLS}
               FROM catalog_ref_edge ce
               JOIN catalog_entity src ON src.id = ce.src_id AND src.current_published_version_id = ce.src_version_id
               WHERE ce.dst_kind = $1 AND ce.dst_id = $2
               ORDER BY ce.created_at DESC, ce.src_id DESC, ce.dst_id DESC
               LIMIT $3`,
              [kind, entityId, input.limit],
            );
            incoming = rows;
          }

          if (input.direction === "outgoing" || input.direction === "both") {
            const { rows } = await pool.query(
              `SELECT ${REF_COLS}
               FROM catalog_ref_edge ce
               JOIN catalog_entity dst ON dst.id = ce.dst_id AND dst.current_published_version_id = ce.dst_version_id
               WHERE ce.src_kind = $1 AND ce.src_id = $2
               ORDER BY ce.created_at DESC, ce.src_id DESC, ce.dst_id DESC
               LIMIT $3`,
              [kind, entityId, input.limit],
            );
            outgoing = rows;
          }

          return jsonResult({ incoming, outgoing });
        } catch (e) {
          const err = e as { code?: string; message?: string };
          return errResult(err.code ?? "db_error", err.message ?? String(e));
        }
      }),
  );
}

// ── SEARCH ────────────────────────────────────────────────────────────────────

const searchInputSchema = z.object({
  q: z.string().min(1),
  kind: z.enum(CATALOG_KINDS).optional(),
  limit: z.coerce.number().int().min(1).max(100).optional().default(20),
});

function registerSearchTool(server: McpServer, kind: CatalogKind): void {
  server.registerTool(
    `catalog_${kind}_search`,
    {
      description: `Full-text / trigram similarity search over ${kind} catalog entities (slug + display_name). Returns {results, total}.`,
      inputSchema: searchInputSchema,
    },
    async (args) =>
      runWithToolTiming(`catalog_${kind}_search`, async () => {
        try {
          const pool = getPool();
          const input = searchInputSchema.parse(args ?? {});
          const effectiveKind = input.kind ?? kind;
          const params: unknown[] = [input.q, effectiveKind, input.limit];
          const { rows } = await pool.query(
            `SELECT e.id::text AS entity_id, e.slug, e.display_name, e.kind, e.retired_at,
                    e.current_published_version_id::text AS current_published_version_id,
                    similarity(e.slug || ' ' || e.display_name, $1) AS score
             FROM catalog_entity e
             WHERE e.retired_at IS NULL
               AND e.kind = $2
               AND (
                 similarity(e.slug || ' ' || e.display_name, $1) > 0.05
                 OR e.display_name ILIKE '%' || $1 || '%'
                 OR e.slug ILIKE '%' || $1 || '%'
               )
             ORDER BY score DESC, e.id ASC
             LIMIT $3`,
            params,
          );
          return jsonResult({ results: rows, total: rows.length });
        } catch (e) {
          const err = e as { code?: string; message?: string };
          return errResult(err.code ?? "db_error", err.message ?? String(e));
        }
      }),
  );
}

// ── MUTATE: CREATE ────────────────────────────────────────────────────────────

const createInputSchema = z.object({
  kind: z.enum(CATALOG_KINDS),
  slug: z.string().regex(/^[a-z][a-z0-9_]{2,63}$/, "slug must match ^[a-z][a-z0-9_]{2,63}$"),
  display_name: z.string().min(1),
  tags: z.array(z.string()).optional().default([]),
  detail: z.record(z.string(), z.unknown()).optional().default({}),
  actor_user_id: z.string().optional(),
});

type DetailInsertFn = (
  pool: import("pg").Pool,
  entityId: number,
  detail: Record<string, unknown>,
) => Promise<void>;

const DETAIL_INSERT: Record<CatalogKind, DetailInsertFn | null> = {
  sprite: async (pool, id, d) => {
    await pool.query(
      `INSERT INTO sprite_detail (entity_id, assets_path, pixels_per_unit, pivot_x, pivot_y, provenance, source_uri, source_run_id, source_variant_idx)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
       ON CONFLICT (entity_id) DO NOTHING`,
      [
        id,
        d.assets_path ?? null,
        d.pixels_per_unit ?? null,
        d.pivot_x ?? null,
        d.pivot_y ?? null,
        d.provenance ?? null,
        d.source_uri ?? null,
        d.source_run_id ?? null,
        d.source_variant_idx ?? null,
      ],
    );
  },
  asset: async (pool, id, d) => {
    await pool.query(
      `INSERT INTO asset_detail (entity_id, category) VALUES ($1, $2) ON CONFLICT (entity_id) DO NOTHING`,
      [id, d.category ?? null],
    );
  },
  button: async (pool, id, d) => {
    await pool.query(
      `INSERT INTO button_detail (entity_id, size_variant, action_id) VALUES ($1, $2, $3) ON CONFLICT (entity_id) DO NOTHING`,
      [id, d.size_variant ?? null, d.action_id ?? null],
    );
  },
  panel: async (pool, id, d) => {
    await pool.query(
      `INSERT INTO panel_detail (entity_id, archetype_entity_id, background_sprite_entity_id, palette_entity_id, frame_style_entity_id, layout_template, modal)
       VALUES ($1, $2, $3, $4, $5, $6, $7) ON CONFLICT (entity_id) DO NOTHING`,
      [
        id,
        d.archetype_entity_id ? Number.parseInt(String(d.archetype_entity_id), 10) : null,
        d.background_sprite_entity_id ? Number.parseInt(String(d.background_sprite_entity_id), 10) : null,
        d.palette_entity_id ? Number.parseInt(String(d.palette_entity_id), 10) : null,
        d.frame_style_entity_id ? Number.parseInt(String(d.frame_style_entity_id), 10) : null,
        d.layout_template ?? "vstack",
        d.modal ?? false,
      ],
    );
  },
  audio: async (pool, id, d) => {
    await pool.query(
      `INSERT INTO audio_detail (entity_id, assets_path, source_uri, duration_ms, sample_rate, channels, loudness_lufs, peak_db, fingerprint)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9) ON CONFLICT (entity_id) DO NOTHING`,
      [
        id,
        d.assets_path ?? null,
        d.source_uri ?? null,
        d.duration_ms ?? null,
        d.sample_rate ?? null,
        d.channels ?? null,
        d.loudness_lufs ?? null,
        d.peak_db ?? null,
        d.fingerprint ?? null,
      ],
    );
  },
  pool: async (pool, id, d) => {
    await pool.query(
      `INSERT INTO pool_detail (entity_id, owner_category, primary_subtype) VALUES ($1, $2, $3) ON CONFLICT (entity_id) DO NOTHING`,
      [id, d.owner_category ?? null, d.primary_subtype ?? null],
    );
  },
  token: async (pool, id, d) => {
    await pool.query(
      `INSERT INTO token_detail (entity_id, token_kind, value_json, semantic_target_entity_id)
       VALUES ($1, $2, $3, $4) ON CONFLICT (entity_id) DO NOTHING`,
      [
        id,
        d.token_kind ?? null,
        d.value_json ? JSON.stringify(d.value_json) : null,
        d.semantic_target_entity_id ? Number.parseInt(String(d.semantic_target_entity_id), 10) : null,
      ],
    );
  },
  archetype: null, // archetype entity_version handled separately
};

function registerCreateTool(server: McpServer, kind: CatalogKind): void {
  server.registerTool(
    `catalog_${kind}_create`,
    {
      description: `Create a new ${kind} catalog entity. slug must match ^[a-z][a-z0-9_]{2,63}$. Returns {entity_id, slug}.`,
      inputSchema: createInputSchema,
    },
    async (args) =>
      runWithToolTiming(`catalog_${kind}_create`, async () => {
        try {
          const pool = getPool();
          const input = createInputSchema.parse({ ...(args ?? {}), kind });

          // Duplicate slug check
          const dupCheck = await pool.query(
            `SELECT 1 FROM catalog_entity WHERE kind = $1 AND slug = $2 LIMIT 1`,
            [kind, input.slug],
          );
          if (dupCheck.rows.length > 0) return errResult("duplicate_slug", `slug '${input.slug}' already exists for kind ${kind}`);

          const insertRes = await pool.query<{ id: string }>(
            `INSERT INTO catalog_entity (kind, slug, display_name, tags)
             VALUES ($1, $2, $3, $4)
             RETURNING id::text AS id`,
            [kind, input.slug, input.display_name, input.tags],
          );
          const entityIdStr = insertRes.rows[0]!.id;
          const entityId = Number.parseInt(entityIdStr, 10);

          const detailFn = DETAIL_INSERT[kind];
          if (detailFn) await detailFn(pool, entityId, input.detail);

          // Audit log
          if (input.actor_user_id) {
            await pool.query(
              `INSERT INTO audit_log (actor_user_id, action, target_kind, target_id)
               VALUES ($1, 'create', 'catalog_entity', $2)`,
              [input.actor_user_id, entityId],
            );
          }

          return jsonResult({ entity_id: entityIdStr, slug: input.slug });
        } catch (e) {
          const err = e as { code?: string; message?: string };
          return errResult(err.code ?? "db_error", err.message ?? String(e));
        }
      }),
  );
}

// ── MUTATE: UPDATE ────────────────────────────────────────────────────────────

const updateInputSchema = z.object({
  slug: z.string().min(1),
  updated_at: z.string().min(1).describe("Optimistic concurrency fingerprint (DEC-A38). Must match current updated_at."),
  display_name: z.string().min(1).optional(),
  tags: z.array(z.string()).optional(),
  detail: z.record(z.string(), z.unknown()).optional(),
  actor_user_id: z.string().optional(),
});

function registerUpdateTool(server: McpServer, kind: CatalogKind): void {
  server.registerTool(
    `catalog_${kind}_update`,
    {
      description: `Update ${kind} catalog entity fields. updated_at fingerprint required (DEC-A38 optimistic concurrency). Returns updated entity or concurrency_error.`,
      inputSchema: updateInputSchema,
    },
    async (args) =>
      runWithToolTiming(`catalog_${kind}_update`, async () => {
        try {
          const pool = getPool();
          const input = updateInputSchema.parse(args ?? {});

          // Lock row + fingerprint check
          const locked = await pool.query<{ id: string; updated_at: string }>(
            `SELECT id, updated_at FROM catalog_entity WHERE kind = $1 AND slug = $2 FOR UPDATE`,
            [kind, input.slug],
          );
          if (locked.rows.length === 0) return errResult("not_found", `${kind} '${input.slug}' not found`);
          const cur = locked.rows[0]!;
          if (new Date(cur.updated_at).toISOString() !== new Date(input.updated_at).toISOString()) {
            return errResult("concurrency_error", "stale updated_at fingerprint — fetch current entity and retry");
          }
          const entityId = Number.parseInt(cur.id, 10);

          if (input.display_name !== undefined) {
            await pool.query(`UPDATE catalog_entity SET display_name = $1 WHERE id = $2`, [input.display_name, entityId]);
          }
          if (input.tags !== undefined) {
            await pool.query(`UPDATE catalog_entity SET tags = $1 WHERE id = $2`, [input.tags, entityId]);
          }
          if (input.detail && Object.keys(input.detail).length > 0) {
            const d = input.detail;
            const kindCfg = kind;
            // Build per-kind update
            if (kindCfg === "sprite") {
              const sets: string[] = [];
              const vals: unknown[] = [];
              for (const col of ["assets_path", "pixels_per_unit", "pivot_x", "pivot_y", "provenance", "source_uri", "source_run_id", "source_variant_idx"] as const) {
                if (col in d) { sets.push(`${col} = $${vals.length + 1}`); vals.push(d[col]); }
              }
              if (sets.length > 0) {
                vals.push(entityId);
                await pool.query(`UPDATE sprite_detail SET ${sets.join(", ")} WHERE entity_id = $${vals.length}`, vals);
              }
            } else if (kindCfg === "asset") {
              if ("category" in d) await pool.query(`UPDATE asset_detail SET category = $1 WHERE entity_id = $2`, [d.category, entityId]);
            } else if (kindCfg === "button") {
              const sets: string[] = [];
              const vals: unknown[] = [];
              for (const col of ["size_variant", "action_id"] as const) {
                if (col in d) { sets.push(`${col} = $${vals.length + 1}`); vals.push(d[col]); }
              }
              if (sets.length > 0) {
                vals.push(entityId);
                await pool.query(`UPDATE button_detail SET ${sets.join(", ")} WHERE entity_id = $${vals.length}`, vals);
              }
            } else if (kindCfg === "panel") {
              const refCols = ["archetype_entity_id", "background_sprite_entity_id", "palette_entity_id", "frame_style_entity_id"];
              const sets: string[] = [];
              const vals: unknown[] = [];
              for (const col of refCols) {
                if (col in d) {
                  sets.push(`${col} = $${vals.length + 1}`);
                  vals.push(d[col] ? Number.parseInt(String(d[col]), 10) : null);
                }
              }
              for (const col of ["layout_template", "modal"] as const) {
                if (col in d) { sets.push(`${col} = $${vals.length + 1}`); vals.push(d[col]); }
              }
              if (sets.length > 0) {
                vals.push(entityId);
                await pool.query(`UPDATE panel_detail SET ${sets.join(", ")} WHERE entity_id = $${vals.length}`, vals);
              }
            } else if (kindCfg === "audio") {
              const sets: string[] = [];
              const vals: unknown[] = [];
              for (const col of ["assets_path", "source_uri", "duration_ms", "sample_rate", "channels", "loudness_lufs", "peak_db", "fingerprint"] as const) {
                if (col in d) { sets.push(`${col} = $${vals.length + 1}`); vals.push(d[col]); }
              }
              if (sets.length > 0) {
                vals.push(entityId);
                await pool.query(`UPDATE audio_detail SET ${sets.join(", ")} WHERE entity_id = $${vals.length}`, vals);
              }
            } else if (kindCfg === "pool") {
              const sets: string[] = [];
              const vals: unknown[] = [];
              for (const col of ["owner_category", "primary_subtype"] as const) {
                if (col in d) { sets.push(`${col} = $${vals.length + 1}`); vals.push(d[col]); }
              }
              if (sets.length > 0) {
                vals.push(entityId);
                await pool.query(`UPDATE pool_detail SET ${sets.join(", ")} WHERE entity_id = $${vals.length}`, vals);
              }
            } else if (kindCfg === "token") {
              const sets: string[] = [];
              const vals: unknown[] = [];
              for (const col of ["token_kind", "value_json", "semantic_target_entity_id"] as const) {
                if (col in d) {
                  sets.push(`${col} = $${vals.length + 1}`);
                  vals.push(col === "value_json" && d[col] != null ? JSON.stringify(d[col]) :
                            col === "semantic_target_entity_id" && d[col] ? Number.parseInt(String(d[col]), 10) : d[col]);
                }
              }
              if (sets.length > 0) {
                vals.push(entityId);
                await pool.query(`UPDATE token_detail SET ${sets.join(", ")} WHERE entity_id = $${vals.length}`, vals);
              }
            }
          }

          await pool.query(`UPDATE catalog_entity SET updated_at = now() WHERE id = $1`, [entityId]);

          if (input.actor_user_id) {
            await pool.query(
              `INSERT INTO audit_log (actor_user_id, action, target_kind, target_id) VALUES ($1, 'update', 'catalog_entity', $2)`,
              [input.actor_user_id, entityId],
            );
          }

          const after = await pool.query(
            `SELECT ${BASE_ENTITY_COLS} FROM catalog_entity e WHERE e.id = $1`,
            [entityId],
          );
          return jsonResult(after.rows[0]);
        } catch (e) {
          const err = e as { code?: string; message?: string };
          return errResult(err.code ?? "db_error", err.message ?? String(e));
        }
      }),
  );
}

// ── MUTATE: RETIRE ────────────────────────────────────────────────────────────

const retireInputSchema = z.object({
  slug: z.string().min(1),
  updated_at: z.string().min(1),
  actor_user_id: z.string().optional(),
});

function registerRetireTool(server: McpServer, kind: CatalogKind): void {
  server.registerTool(
    `catalog_${kind}_retire`,
    {
      description: `Soft-retire a ${kind} entity (DEC-A23). Sets retired_at = now(). updated_at fingerprint required. Returns updated entity.`,
      inputSchema: retireInputSchema,
    },
    async (args) =>
      runWithToolTiming(`catalog_${kind}_retire`, async () => {
        try {
          const pool = getPool();
          const input = retireInputSchema.parse(args ?? {});
          const locked = await pool.query<{ id: string; updated_at: string }>(
            `SELECT id, updated_at FROM catalog_entity WHERE kind = $1 AND slug = $2 FOR UPDATE`,
            [kind, input.slug],
          );
          if (locked.rows.length === 0) return errResult("not_found", `${kind} '${input.slug}' not found`);
          const cur = locked.rows[0]!;
          if (new Date(cur.updated_at).toISOString() !== new Date(input.updated_at).toISOString()) {
            return errResult("concurrency_error", "stale updated_at fingerprint");
          }
          const entityId = Number.parseInt(cur.id, 10);
          await pool.query(
            `UPDATE catalog_entity SET retired_at = now(), updated_at = now() WHERE id = $1`,
            [entityId],
          );
          if (input.actor_user_id) {
            await pool.query(
              `INSERT INTO audit_log (actor_user_id, action, target_kind, target_id) VALUES ($1, 'retire', 'catalog_entity', $2)`,
              [input.actor_user_id, entityId],
            );
          }
          const after = await pool.query(`SELECT ${BASE_ENTITY_COLS} FROM catalog_entity e WHERE e.id = $1`, [entityId]);
          return jsonResult(after.rows[0]);
        } catch (e) {
          const err = e as { code?: string; message?: string };
          return errResult(err.code ?? "db_error", err.message ?? String(e));
        }
      }),
  );
}

// ── MUTATE: RESTORE ───────────────────────────────────────────────────────────

const restoreInputSchema = z.object({
  slug: z.string().min(1),
  actor_user_id: z.string().optional(),
});

function registerRestoreTool(server: McpServer, kind: CatalogKind): void {
  server.registerTool(
    `catalog_${kind}_restore`,
    {
      description: `Restore a retired ${kind} entity (DEC-A23). Clears retired_at. Returns updated entity or not_found.`,
      inputSchema: restoreInputSchema,
    },
    async (args) =>
      runWithToolTiming(`catalog_${kind}_restore`, async () => {
        try {
          const pool = getPool();
          const input = restoreInputSchema.parse(args ?? {});
          const res = await pool.query<{ id: string }>(
            `UPDATE catalog_entity SET retired_at = NULL, updated_at = now()
             WHERE kind = $1 AND slug = $2
             RETURNING id::text AS id`,
            [kind, input.slug],
          );
          if (res.rows.length === 0) return errResult("not_found", `${kind} '${input.slug}' not found`);
          const entityId = Number.parseInt(res.rows[0]!.id, 10);
          if (input.actor_user_id) {
            await pool.query(
              `INSERT INTO audit_log (actor_user_id, action, target_kind, target_id) VALUES ($1, 'restore', 'catalog_entity', $2)`,
              [input.actor_user_id, entityId],
            );
          }
          const after = await pool.query(`SELECT ${BASE_ENTITY_COLS} FROM catalog_entity e WHERE e.id = $1`, [entityId]);
          return jsonResult(after.rows[0]);
        } catch (e) {
          const err = e as { code?: string; message?: string };
          return errResult(err.code ?? "db_error", err.message ?? String(e));
        }
      }),
  );
}

// ── MUTATE: PUBLISH ───────────────────────────────────────────────────────────

const publishInputSchema = z.object({
  slug: z.string().min(1),
  version_id: z.string().min(1).describe("entity_version id to publish (numeric string)."),
  actor_user_id: z.string().optional(),
});

function registerPublishTool(server: McpServer, kind: CatalogKind): void {
  server.registerTool(
    `catalog_${kind}_publish`,
    {
      description: `Publish a specific entity_version for a ${kind} entity. Sets current_published_version_id. Lenient — null outbound refs do not block publish (DEC-A22). Returns {entity_id, published_version_id}.`,
      inputSchema: publishInputSchema,
    },
    async (args) =>
      runWithToolTiming(`catalog_${kind}_publish`, async () => {
        try {
          const pool = getPool();
          const input = publishInputSchema.parse(args ?? {});
          const entityId = await resolveEntityId(kind, input.slug);
          if (entityId == null) return errResult("not_found", `${kind} '${input.slug}' not found`);
          const versionId = Number.parseInt(input.version_id, 10);

          // Verify version belongs to this entity
          const versionCheck = await pool.query<{ id: number }>(
            `SELECT id FROM entity_version WHERE id = $1 AND entity_id = $2 LIMIT 1`,
            [versionId, entityId],
          );
          if (versionCheck.rows.length === 0) {
            return errResult("not_found", `Version ${input.version_id} not found for this entity`);
          }

          await pool.query(
            `UPDATE catalog_entity SET current_published_version_id = $1, updated_at = now() WHERE id = $2`,
            [versionId, entityId],
          );
          await pool.query(
            `UPDATE entity_version SET status = 'published' WHERE id = $1`,
            [versionId],
          );

          if (input.actor_user_id) {
            await pool.query(
              `INSERT INTO audit_log (actor_user_id, action, target_kind, target_id) VALUES ($1, 'publish', 'catalog_entity', $2)`,
              [input.actor_user_id, entityId],
            );
          }

          return jsonResult({ entity_id: String(entityId), published_version_id: input.version_id });
        } catch (e) {
          const err = e as { code?: string; message?: string };
          return errResult(err.code ?? "db_error", err.message ?? String(e));
        }
      }),
  );
}

// ── MUTATE: BULK ACTION ───────────────────────────────────────────────────────

const bulkActionInputSchema = z.object({
  actions: z.array(
    z.object({
      kind: z.enum(CATALOG_KINDS),
      op: z.enum(["retire", "restore"]),
      slug: z.string().min(1),
      updated_at: z.string().optional(),
      actor_user_id: z.string().optional(),
    }),
  ).min(1).max(50),
});

export function registerCatalogBulkAction(server: McpServer): void {
  server.registerTool(
    "catalog_bulk_action",
    {
      description: "Apply a batch of retire/restore operations across any catalog kinds. Partial failure returns per-action results. Returns {results: [{slug, kind, op, ok, error?}]}.",
      inputSchema: bulkActionInputSchema,
    },
    async (args) =>
      runWithToolTiming("catalog_bulk_action", async () => {
        try {
          const pool = getPool();
          const input = bulkActionInputSchema.parse(args ?? {});
          const results: unknown[] = [];
          for (const action of input.actions) {
            try {
              if (action.op === "retire") {
                if (!action.updated_at) {
                  results.push({ kind: action.kind, slug: action.slug, op: action.op, ok: false, error: "updated_at required for retire" });
                  continue;
                }
                const locked = await pool.query<{ id: string; updated_at: string }>(
                  `SELECT id, updated_at FROM catalog_entity WHERE kind = $1 AND slug = $2 FOR UPDATE`,
                  [action.kind, action.slug],
                );
                if (locked.rows.length === 0) {
                  results.push({ kind: action.kind, slug: action.slug, op: action.op, ok: false, error: "not_found" });
                  continue;
                }
                const cur = locked.rows[0]!;
                if (new Date(cur.updated_at).toISOString() !== new Date(action.updated_at).toISOString()) {
                  results.push({ kind: action.kind, slug: action.slug, op: action.op, ok: false, error: "concurrency_error" });
                  continue;
                }
                const entityId = Number.parseInt(cur.id, 10);
                await pool.query(
                  `UPDATE catalog_entity SET retired_at = now(), updated_at = now() WHERE id = $1`,
                  [entityId],
                );
                if (action.actor_user_id) {
                  await pool.query(
                    `INSERT INTO audit_log (actor_user_id, action, target_kind, target_id) VALUES ($1, 'retire', 'catalog_entity', $2)`,
                    [action.actor_user_id, entityId],
                  );
                }
                results.push({ kind: action.kind, slug: action.slug, op: action.op, ok: true });
              } else if (action.op === "restore") {
                const res = await pool.query<{ id: string }>(
                  `UPDATE catalog_entity SET retired_at = NULL, updated_at = now()
                   WHERE kind = $1 AND slug = $2
                   RETURNING id::text AS id`,
                  [action.kind, action.slug],
                );
                if (res.rows.length === 0) {
                  results.push({ kind: action.kind, slug: action.slug, op: action.op, ok: false, error: "not_found" });
                  continue;
                }
                const entityId = Number.parseInt(res.rows[0]!.id, 10);
                if (action.actor_user_id) {
                  await pool.query(
                    `INSERT INTO audit_log (actor_user_id, action, target_kind, target_id) VALUES ($1, 'restore', 'catalog_entity', $2)`,
                    [action.actor_user_id, entityId],
                  );
                }
                results.push({ kind: action.kind, slug: action.slug, op: action.op, ok: true });
              }
            } catch (e) {
              const err = e as { message?: string };
              results.push({ kind: action.kind, slug: action.slug, op: action.op, ok: false, error: err.message ?? String(e) });
            }
          }
          return jsonResult({ results });
        } catch (e) {
          const err = e as { code?: string; message?: string };
          return errResult(err.code ?? "db_error", err.message ?? String(e));
        }
      }),
  );
}

// ── REGISTER HELPERS ──────────────────────────────────────────────────────────

export function registerCatalogReadTools(server: McpServer): void {
  for (const kind of CATALOG_KINDS) {
    registerListTool(server, kind);
    registerGetTool(server, kind);
    registerGetVersionTool(server, kind);
    registerRefsTool(server, kind);
    registerSearchTool(server, kind);
  }
}

export function registerCatalogMutateTools(server: McpServer): void {
  for (const kind of CATALOG_KINDS) {
    registerCreateTool(server, kind);
    registerUpdateTool(server, kind);
    registerRetireTool(server, kind);
    registerPublishTool(server, kind);
    registerRestoreTool(server, kind);
  }
  registerCatalogBulkAction(server);
}

/** Flat list of all catalog entity tool names (read + mutate + bulk). Used by coverage validator. */
export const CATALOG_ENTITY_TOOL_NAMES: ReadonlyArray<string> = [
  ...CATALOG_KINDS.flatMap((kind) => [
    `catalog_${kind}_list`,
    `catalog_${kind}_get`,
    `catalog_${kind}_get_version`,
    `catalog_${kind}_refs`,
    `catalog_${kind}_search`,
    `catalog_${kind}_create`,
    `catalog_${kind}_update`,
    `catalog_${kind}_retire`,
    `catalog_${kind}_restore`,
    `catalog_${kind}_publish`,
  ]),
  "catalog_bulk_action",
];
