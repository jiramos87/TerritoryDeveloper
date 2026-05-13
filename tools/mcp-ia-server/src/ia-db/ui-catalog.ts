/**
 * ui-catalog.ts — shared DAL for catalog_entity + panel_detail + token_detail.
 *
 * Stats-panel pilot (TECH-extend-design-system) introduced this module to:
 *  1. Centralize DB writes that today live inlined inside MCP tool handlers.
 *  2. Provide a re-usable surface for future web `asset-pipeline` HTTP endpoints
 *     (they import the same module — single source of truth for SQL).
 *
 * Conventions:
 *  - Every function takes a `PoolClient` (caller owns connection + tx control).
 *  - JSONB columns merge shallow-key-wise; pass `{ keys: 'replace' }` to overwrite.
 *  - `publishEntity` does NOT call publish gates — callers that need T1.0.1-T1.0.5
 *    gating use the dedicated `catalog_panel_publish` MCP tool (which wraps these
 *    primitives + validators in one transaction).
 */

import type { PoolClient } from "pg";

// ── shared types ──────────────────────────────────────────────────────────

export type CatalogKind = "panel" | "token" | "component" | "button" | "sprite";

export interface CatalogEntityRow {
  id: string;
  slug: string;
  kind: CatalogKind;
  display_name: string | null;
  current_published_version_id: string | null;
  retired_at: Date | null;
  tags: unknown;
  created_at: Date;
  updated_at: Date;
}

export interface PanelDetailRow {
  entity_id: string;
  layout_template: string | null;
  layout: string | null;
  modal: boolean | null;
  gap_px: number | null;
  padding_json: Record<string, unknown> | null;
  params_json: Record<string, unknown> | null;
  rect_json: Record<string, unknown> | null;
}

export interface TokenDetailRow {
  entity_id: string;
  token_kind: string;
  value_json: Record<string, unknown> | null;
  semantic_target_entity_id: string | null;
}

export interface PanelDetailPatch {
  layout_template?: string;
  layout?: string;
  modal?: boolean;
  gap_px?: number;
  padding_json?: Record<string, unknown>;
  params_json?: Record<string, unknown>;
  rect_json?: Record<string, unknown>;
}

export interface TokenDetailPatch {
  token_kind?: string;
  value_json?: Record<string, unknown>;
  semantic_target_entity_id?: string | null;
}

export interface JsonPatchOptions {
  /** 'merge' (default) = shallow key-wise merge; 'replace' = overwrite full column. */
  jsonStrategy?: "merge" | "replace";
}

export interface PublishResult {
  entity_id: string;
  prev_version_id: string | null;
  new_version_id: string;
  new_version_number: number;
}

// ── read ──────────────────────────────────────────────────────────────────

export async function getPanelBundle(
  client: PoolClient,
  slug: string,
): Promise<{ entity: CatalogEntityRow; detail: PanelDetailRow } | null> {
  const res = await client.query(
    `SELECT ce.id, ce.slug, ce.kind, ce.display_name,
            ce.current_published_version_id, ce.retired_at, ce.tags,
            ce.created_at, ce.updated_at,
            pd.entity_id AS detail_entity_id,
            pd.layout_template, pd.layout, pd.modal, pd.gap_px,
            pd.padding_json, pd.params_json, pd.rect_json
     FROM panel_detail pd
     JOIN catalog_entity ce ON ce.id = pd.entity_id
     WHERE ce.kind = 'panel' AND ce.slug = $1`,
    [slug],
  );
  if (res.rows.length === 0) return null;
  const r = res.rows[0];
  return {
    entity: {
      id: r.id,
      slug: r.slug,
      kind: r.kind,
      display_name: r.display_name,
      current_published_version_id: r.current_published_version_id,
      retired_at: r.retired_at,
      tags: r.tags,
      created_at: r.created_at,
      updated_at: r.updated_at,
    },
    detail: {
      entity_id: r.detail_entity_id,
      layout_template: r.layout_template,
      layout: r.layout,
      modal: r.modal,
      gap_px: r.gap_px,
      padding_json: r.padding_json,
      params_json: r.params_json,
      rect_json: r.rect_json,
    },
  };
}

export async function getTokenBundle(
  client: PoolClient,
  slug: string,
): Promise<{ entity: CatalogEntityRow; detail: TokenDetailRow } | null> {
  const res = await client.query(
    `SELECT ce.id, ce.slug, ce.kind, ce.display_name,
            ce.current_published_version_id, ce.retired_at, ce.tags,
            ce.created_at, ce.updated_at,
            td.entity_id AS detail_entity_id,
            td.token_kind, td.value_json, td.semantic_target_entity_id
     FROM token_detail td
     JOIN catalog_entity ce ON ce.id = td.entity_id
     WHERE ce.kind = 'token' AND ce.slug = $1`,
    [slug],
  );
  if (res.rows.length === 0) return null;
  const r = res.rows[0];
  return {
    entity: {
      id: r.id,
      slug: r.slug,
      kind: r.kind,
      display_name: r.display_name,
      current_published_version_id: r.current_published_version_id,
      retired_at: r.retired_at,
      tags: r.tags,
      created_at: r.created_at,
      updated_at: r.updated_at,
    },
    detail: {
      entity_id: r.detail_entity_id,
      token_kind: r.token_kind,
      value_json: r.value_json,
      semantic_target_entity_id: r.semantic_target_entity_id,
    },
  };
}

// ── write (no version bump) ──────────────────────────────────────────────

/**
 * Patch panel_detail by slug. Scalar fields overwrite; JSONB fields shallow-merge
 * by default (use `jsonStrategy: 'replace'` to overwrite full column).
 *
 * Throws `{ code:'not_found' }` when slug missing.
 */
export async function updatePanelDetail(
  client: PoolClient,
  slug: string,
  patch: PanelDetailPatch,
  opts: JsonPatchOptions = {},
): Promise<{ entity_id: string; updated_fields: string[] }> {
  const strategy = opts.jsonStrategy ?? "merge";
  const fields: string[] = [];
  const params: unknown[] = [];
  let p = 1;

  const fetch = await client.query(
    `SELECT pd.entity_id, pd.padding_json, pd.params_json, pd.rect_json
     FROM panel_detail pd
     JOIN catalog_entity ce ON ce.id = pd.entity_id
     WHERE ce.kind = 'panel' AND ce.slug = $1`,
    [slug],
  );
  if (fetch.rows.length === 0) {
    throw { code: "not_found" as const, message: `Panel not found: ${slug}` };
  }
  const { entity_id, padding_json, params_json, rect_json } = fetch.rows[0] as {
    entity_id: string;
    padding_json: Record<string, unknown> | null;
    params_json: Record<string, unknown> | null;
    rect_json: Record<string, unknown> | null;
  };

  if (patch.layout_template !== undefined) {
    fields.push(`layout_template = $${p++}`);
    params.push(patch.layout_template);
  }
  if (patch.layout !== undefined) {
    fields.push(`layout = $${p++}`);
    params.push(patch.layout);
  }
  if (patch.modal !== undefined) {
    fields.push(`modal = $${p++}`);
    params.push(patch.modal);
  }
  if (patch.gap_px !== undefined) {
    fields.push(`gap_px = $${p++}`);
    params.push(patch.gap_px);
  }
  if (patch.padding_json !== undefined) {
    const next = strategy === "replace"
      ? patch.padding_json
      : { ...(padding_json ?? {}), ...patch.padding_json };
    fields.push(`padding_json = $${p++}::jsonb`);
    params.push(JSON.stringify(next));
  }
  if (patch.params_json !== undefined) {
    const next = strategy === "replace"
      ? patch.params_json
      : { ...(params_json ?? {}), ...patch.params_json };
    fields.push(`params_json = $${p++}::jsonb`);
    params.push(JSON.stringify(next));
  }
  if (patch.rect_json !== undefined) {
    const next = strategy === "replace"
      ? patch.rect_json
      : { ...(rect_json ?? {}), ...patch.rect_json };
    fields.push(`rect_json = $${p++}::jsonb`);
    params.push(JSON.stringify(next));
  }

  const updatedFields = fields.map((f) => f.split(" = ")[0]!);
  if (fields.length === 0) return { entity_id, updated_fields: [] };

  fields.push(`updated_at = NOW()`);
  params.push(entity_id);
  await client.query(
    `UPDATE panel_detail SET ${fields.join(", ")} WHERE entity_id = $${p}`,
    params,
  );

  // Bump catalog_entity.updated_at so listings reflect change time.
  await client.query(
    `UPDATE catalog_entity SET updated_at = NOW() WHERE id = $1`,
    [entity_id],
  );

  return { entity_id, updated_fields: updatedFields };
}

export async function updateTokenDetail(
  client: PoolClient,
  slug: string,
  patch: TokenDetailPatch,
  opts: JsonPatchOptions = {},
): Promise<{ entity_id: string; updated_fields: string[] }> {
  const strategy = opts.jsonStrategy ?? "merge";
  const fields: string[] = [];
  const params: unknown[] = [];
  let p = 1;

  const fetch = await client.query(
    `SELECT td.entity_id, td.value_json
     FROM token_detail td
     JOIN catalog_entity ce ON ce.id = td.entity_id
     WHERE ce.kind = 'token' AND ce.slug = $1`,
    [slug],
  );
  if (fetch.rows.length === 0) {
    throw { code: "not_found" as const, message: `Token not found: ${slug}` };
  }
  const { entity_id, value_json } = fetch.rows[0] as {
    entity_id: string;
    value_json: Record<string, unknown> | null;
  };

  if (patch.token_kind !== undefined) {
    fields.push(`token_kind = $${p++}`);
    params.push(patch.token_kind);
  }
  if (patch.value_json !== undefined) {
    const next = strategy === "replace"
      ? patch.value_json
      : { ...(value_json ?? {}), ...patch.value_json };
    fields.push(`value_json = $${p++}::jsonb`);
    params.push(JSON.stringify(next));
  }
  if (patch.semantic_target_entity_id !== undefined) {
    fields.push(`semantic_target_entity_id = $${p++}`);
    params.push(patch.semantic_target_entity_id);
  }

  const updatedFields = fields.map((f) => f.split(" = ")[0]!);
  if (fields.length === 0) return { entity_id, updated_fields: [] };

  fields.push(`updated_at = NOW()`);
  params.push(entity_id);
  await client.query(
    `UPDATE token_detail SET ${fields.join(", ")} WHERE entity_id = $${p}`,
    params,
  );
  await client.query(
    `UPDATE catalog_entity SET updated_at = NOW() WHERE id = $1`,
    [entity_id],
  );

  return { entity_id, updated_fields: updatedFields };
}

// ── publish (version bump) ───────────────────────────────────────────────

/**
 * Generic publish: INSERT a new entity_version row + UPDATE catalog_entity
 * current_published_version_id pointer. Does NOT run publish gates.
 *
 * Callers needing T1.0.1-T1.0.5 gating wrap this with the validators in
 * `mutations/catalog-panel.ts` inside the same transaction (see
 * `tools/catalog-panel-publish.ts`).
 */
export async function publishEntity(
  client: PoolClient,
  kind: CatalogKind,
  slug: string,
): Promise<PublishResult> {
  const fetch = await client.query(
    `SELECT ce.id AS entity_id, ce.current_published_version_id,
            ev.version_number AS current_version_number
     FROM catalog_entity ce
     LEFT JOIN entity_version ev ON ev.id = ce.current_published_version_id
     WHERE ce.kind = $1 AND ce.slug = $2`,
    [kind, slug],
  );
  if (fetch.rows.length === 0) {
    throw { code: "not_found" as const, message: `${kind} not found: ${slug}` };
  }
  const {
    entity_id: entityId,
    current_published_version_id: prevVersionId,
    current_version_number: currentVersionNum,
  } = fetch.rows[0] as {
    entity_id: string;
    current_published_version_id: string | null;
    current_version_number: number | null;
  };

  const nextVersionNumber = (currentVersionNum ?? 0) + 1;
  const insertRes = await client.query(
    `INSERT INTO entity_version (entity_id, version_number, status, parent_version_id, created_at, updated_at)
     VALUES ($1, $2, 'published', $3, NOW(), NOW())
     RETURNING id::text AS id`,
    [entityId, nextVersionNumber, prevVersionId],
  );
  const newVersionId = (insertRes.rows[0] as { id: string }).id;
  await client.query(
    `UPDATE catalog_entity SET current_published_version_id = $1, updated_at = NOW() WHERE id = $2`,
    [newVersionId, entityId],
  );

  return {
    entity_id: entityId,
    prev_version_id: prevVersionId,
    new_version_id: newVersionId,
    new_version_number: nextVersionNumber,
  };
}

export const publishPanel = (client: PoolClient, slug: string) =>
  publishEntity(client, "panel", slug);

export const publishToken = (client: PoolClient, slug: string) =>
  publishEntity(client, "token", slug);

// ── panel_child DAL ───────────────────────────────────────────────────────

export interface PanelChildNode {
  slot: string;
  ord: number;
  kind: string;
  slug: string | null;
  child_entity_id: string | null;
  params_json: Record<string, unknown>;
  resolved?: { display_name: string | null; role: string | null } | null;
  children?: PanelChildNode[];
}

export interface GetPanelChildrenOpts {
  maxDepth?: number;
  pin?: "live" | "frozen";
}

/**
 * Recursive panel_child resolver with cycle guard + depth limit.
 * Returns tree of PanelChildNode rooted at entityId.
 */
export async function getPanelChildren(
  client: PoolClient,
  entityId: string,
  opts: GetPanelChildrenOpts = {},
  _visited: Set<string> = new Set(),
): Promise<PanelChildNode[]> {
  const maxDepth = opts.maxDepth ?? 2;
  const depth = _visited.size;
  if (depth >= maxDepth) return [];

  const res = await client.query(
    `SELECT pc.slot_name, pc.order_idx, pc.child_kind,
            pc.params_json, pc.child_entity_id::text AS child_entity_id,
            ce.slug AS child_slug, ce.display_name AS child_display_name
     FROM panel_child pc
     LEFT JOIN catalog_entity ce ON ce.id = pc.child_entity_id
     WHERE pc.panel_entity_id = $1::bigint
     ORDER BY pc.order_idx, pc.slot_name`,
    [entityId],
  );

  const nodes: PanelChildNode[] = [];
  for (const r of res.rows) {
    const childEntityId: string | null = r.child_entity_id ?? null;
    const childSlug: string | null = r.child_slug ?? null;

    // Cycle guard: skip if child_entity_id already on current branch path
    if (childEntityId !== null && _visited.has(childEntityId)) continue;

    const node: PanelChildNode = {
      slot: r.slot_name as string,
      ord: r.order_idx as number,
      kind: r.child_kind as string,
      slug: childSlug,
      child_entity_id: childEntityId,
      params_json: (r.params_json ?? {}) as Record<string, unknown>,
      resolved: childSlug != null
        ? { display_name: (r.child_display_name as string | null) ?? null, role: null }
        : null,
    };

    // Recurse into child panels
    if (childEntityId !== null && depth + 1 < maxDepth) {
      const nextVisited = new Set(_visited);
      nextVisited.add(childEntityId);
      const children = await getPanelChildren(client, childEntityId, opts, nextVisited);
      if (children.length > 0) node.children = children;
    }

    nodes.push(node);
  }
  return nodes;
}

/**
 * Direct panel_child reverse-join: returns panel slugs that have entitySlug as a child.
 */
export async function getPanelConsumersDirect(
  client: PoolClient,
  entitySlug: string,
): Promise<string[]> {
  const res = await client.query(
    `SELECT DISTINCT panel_ce.slug
     FROM panel_child pc
     JOIN catalog_entity panel_ce ON panel_ce.id = pc.panel_entity_id
     WHERE pc.child_entity_id = (
       SELECT id FROM catalog_entity WHERE slug = $1 LIMIT 1
     )`,
    [entitySlug],
  );
  return res.rows.map((r: { slug: string }) => r.slug);
}
