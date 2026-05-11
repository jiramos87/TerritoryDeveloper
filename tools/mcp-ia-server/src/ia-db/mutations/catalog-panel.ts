/**
 * catalog-panel.ts — validation helpers for catalog_panel_publish MCP gate.
 *
 * Each helper is called pre-INSERT inside a transaction. Returns { ok, errors[] }
 * shape — caller collects all errors and rejects publish if any present.
 */

import type { PoolClient } from "pg";

export interface GateError {
  code: string;
  message: string;
  field?: string;
}

export interface GateResult {
  ok: boolean;
  errors: GateError[];
}

// ── §T1.0.1 archetype×kind renderer coverage gate ────────────────────────────

/**
 * Every child.kind in a published DB row MUST resolve to a known renderer kind.
 * Known kinds: all values in panel_child.child_kind CHECK constraint.
 */
const KNOWN_RENDERER_KINDS = new Set([
  "button",
  "panel",
  "label",
  "spacer",
  "audio",
  "sprite",
  "label_inline",
  "row",
  "text",
  "confirm-button",
  "view-slot",
]);

export function validateArchetypeKindCoverage(row: {
  slug?: string;
  children?: Array<{ kind?: string; [key: string]: unknown }>;
}): GateResult {
  const errors: GateError[] = [];
  for (const child of row.children ?? []) {
    if (!child.kind || !KNOWN_RENDERER_KINDS.has(child.kind)) {
      errors.push({
        code: "archetype_no_renderer",
        message: `child.kind '${child.kind ?? "(undefined)"}' has no registered renderer. Known: ${[...KNOWN_RENDERER_KINDS].join(", ")}`,
        field: "children[].kind",
      });
    }
  }
  return { ok: errors.length === 0, errors };
}

// ── §T1.0.2 action-id sink uniqueness gate ────────────────────────────────────

/**
 * For each child.action_id, query ia_ui_action_sinks; collision for different
 * owner → error. On success caller must INSERT new rows into ia_ui_action_sinks.
 */
export async function validateActionIdSinkUniqueness(
  row: {
    slug?: string;
    children?: Array<{ action_id?: string; [key: string]: unknown }>;
  },
  tx: PoolClient,
): Promise<GateResult> {
  const errors: GateError[] = [];
  const ownerSlug = row.slug ?? "";

  for (const child of row.children ?? []) {
    if (!child.action_id) continue;
    try {
      const res = await tx.query<{ owner_panel_slug: string }>(
        `SELECT owner_panel_slug FROM ia_ui_action_sinks WHERE action_id = $1 LIMIT 1`,
        [child.action_id],
      );
      if (res.rows.length > 0 && res.rows[0]!.owner_panel_slug !== ownerSlug) {
        errors.push({
          code: "action_id_sink_collision",
          message: `action_id '${child.action_id}' already owned by panel '${res.rows[0]!.owner_panel_slug}'`,
          field: "children[].action_id",
        });
      }
    } catch (e) {
      // ia_ui_action_sinks table not yet created — skip enforcement gracefully
      if ((e as { code?: string }).code === "42P01") break;
      throw e;
    }
  }
  return { ok: errors.length === 0, errors };
}

/**
 * Register all action_id sinks for this panel (called after gates pass).
 */
export async function registerActionIdSinks(
  row: {
    slug?: string;
    children?: Array<{ action_id?: string; [key: string]: unknown }>;
  },
  tx: PoolClient,
): Promise<void> {
  const ownerSlug = row.slug ?? "";
  for (const child of row.children ?? []) {
    if (!child.action_id) continue;
    try {
      await tx.query(
        `INSERT INTO ia_ui_action_sinks (action_id, owner_panel_slug)
         VALUES ($1, $2)
         ON CONFLICT (action_id) DO UPDATE SET owner_panel_slug = EXCLUDED.owner_panel_slug`,
        [child.action_id, ownerSlug],
      );
    } catch (e) {
      if ((e as { code?: string }).code === "42P01") return;
      throw e;
    }
  }
}

// ── §T1.0.3 bind-id contract gate ────────────────────────────────────────────

/**
 * Every child.bind_id must resolve to ia_ui_bind_registry OR carry
 * declare_on_publish=true (auto-registers on publish).
 */
export async function validateBindIdContract(
  row: {
    slug?: string;
    children?: Array<{
      bind_id?: string;
      declare_on_publish?: boolean;
      [key: string]: unknown;
    }>;
  },
  tx: PoolClient,
): Promise<GateResult> {
  const errors: GateError[] = [];

  for (const child of row.children ?? []) {
    if (!child.bind_id) continue;
    try {
      const res = await tx.query<{ bind_id: string }>(
        `SELECT bind_id FROM ia_ui_bind_registry WHERE bind_id = $1 LIMIT 1`,
        [child.bind_id],
      );
      if (res.rows.length === 0 && !child.declare_on_publish) {
        errors.push({
          code: "unknown_bind_id",
          message: `bind_id '${child.bind_id}' not found in ia_ui_bind_registry and declare_on_publish not set`,
          field: "children[].bind_id",
        });
      }
    } catch (e) {
      if ((e as { code?: string }).code === "42P01") break;
      throw e;
    }
  }
  return { ok: errors.length === 0, errors };
}

/**
 * Auto-register bind_ids with declare_on_publish=true (called after gates pass).
 */
export async function registerDeclaredBindIds(
  row: {
    slug?: string;
    children?: Array<{
      bind_id?: string;
      declare_on_publish?: boolean;
      [key: string]: unknown;
    }>;
  },
  tx: PoolClient,
): Promise<void> {
  for (const child of row.children ?? []) {
    if (!child.bind_id || !child.declare_on_publish) continue;
    try {
      await tx.query(
        `INSERT INTO ia_ui_bind_registry (bind_id, owner_panel_slug, declared_at)
         VALUES ($1, $2, now())
         ON CONFLICT (bind_id) DO NOTHING`,
        [child.bind_id, row.slug ?? ""],
      );
    } catch (e) {
      if ((e as { code?: string }).code === "42P01") return;
      throw e;
    }
  }
}

// ── §T1.0.4 token reference graph gate ───────────────────────────────────────

/** Regex to extract token-* references from any string value. */
const TOKEN_REF_RE = /token-[a-z0-9_-]+/g;

function extractTokenRefs(obj: unknown): string[] {
  if (typeof obj === "string") return obj.match(TOKEN_REF_RE) ?? [];
  if (Array.isArray(obj)) return obj.flatMap(extractTokenRefs);
  if (obj && typeof obj === "object") {
    return Object.values(obj as Record<string, unknown>).flatMap(extractTokenRefs);
  }
  return [];
}

/**
 * All token-* refs in params_json must resolve to a published token entity
 * in catalog_entity (kind='token') OR in ui_design_tokens / ui_token_aliases
 * when those tables exist. Closes cityscene Stage 8.0 root cause (stale token
 * refs silently fell through to defaults).
 */
export async function validateTokenReferences(
  row: {
    slug?: string;
    params_json?: unknown;
    children?: Array<{ params_json?: unknown; [key: string]: unknown }>;
  },
  tx: PoolClient,
): Promise<GateResult> {
  const errors: GateError[] = [];
  const allRefs = new Set<string>([
    ...extractTokenRefs(row.params_json),
    ...(row.children ?? []).flatMap((c) => extractTokenRefs(c.params_json)),
  ]);

  for (const tokenId of allRefs) {
    try {
      // Primary: check catalog_entity token spine (always present)
      // token slug convention: token-* ref strips leading "token-" prefix to get slug
      const tokenSlug = tokenId.replace(/^token-/, "");
      const res = await tx.query<{ token_id: string }>(
        `SELECT ce.slug AS token_id
         FROM catalog_entity ce
         WHERE ce.kind = 'token' AND (ce.slug = $1 OR ce.slug = $2) AND ce.retired_at IS NULL
         LIMIT 1`,
        [tokenId, tokenSlug],
      );
      if (res.rows.length === 0) {
        errors.push({
          code: "dangling_token_ref",
          message: `token ref '${tokenId}' not found in catalog_entity (kind=token). Ensure token entity is published before referencing it.`,
          field: "params_json",
        });
      }
    } catch (e) {
      if ((e as { code?: string }).code === "42P01") break;
      throw e;
    }
  }
  return { ok: errors.length === 0, errors };
}

// ── §T1.0.5 view-slot anchor required-by gate ────────────────────────────────

/**
 * Panels declaring views[] must have matching catalog_panel_anchors row for
 * each view (slot_name).
 */
export async function validateViewSlotAnchors(
  row: {
    slug?: string;
    views?: string[];
  },
  tx: PoolClient,
): Promise<GateResult> {
  const errors: GateError[] = [];
  const panelSlug = row.slug ?? "";

  for (const view of row.views ?? []) {
    try {
      const res = await tx.query<{ slot_name: string }>(
        `SELECT slot_name FROM catalog_panel_anchors WHERE panel_slug = $1 AND slot_name = $2 LIMIT 1`,
        [panelSlug, view],
      );
      if (res.rows.length === 0) {
        errors.push({
          code: "unanchored_view",
          message: `view '${view}' in panel '${panelSlug}' has no catalog_panel_anchors row. Insert anchor row before publish.`,
          field: "views[]",
        });
      }
    } catch (e) {
      if ((e as { code?: string }).code === "42P01") break;
      throw e;
    }
  }
  return { ok: errors.length === 0, errors };
}
