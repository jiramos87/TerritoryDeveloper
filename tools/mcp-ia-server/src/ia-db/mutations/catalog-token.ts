/**
 * catalog-token.ts — shared token resolution helper (TECH-28359 §T1.0.4).
 *
 * resolveTokenOrAlias: looks up token_id in catalog_entity (kind=token)
 * and falls back to ui_token_aliases when that table exists.
 * Shared between catalog_panel_publish gate and ui_token read tools.
 */

import type { PoolClient } from "pg";

/**
 * Resolves a token-* reference. Returns the canonical slug when found, null otherwise.
 * Checks catalog_entity (kind=token) using both the full token-* ref and the
 * slug-without-prefix form. Gracefully skips ui_token_aliases if table absent (42P01).
 */
export async function resolveTokenOrAlias(
  tokenId: string,
  tx: PoolClient,
): Promise<string | null> {
  // Strip leading "token-" to match slug convention
  const tokenSlug = tokenId.replace(/^token-/, "");

  try {
    const res = await tx.query<{ slug: string }>(
      `SELECT ce.slug
       FROM catalog_entity ce
       WHERE ce.kind = 'token'
         AND (ce.slug = $1 OR ce.slug = $2)
         AND ce.retired_at IS NULL
       LIMIT 1`,
      [tokenId, tokenSlug],
    );
    if (res.rows.length > 0) return res.rows[0]!.slug;
  } catch (e) {
    if ((e as { code?: string }).code === "42P01") return null;
    throw e;
  }

  // Secondary: ui_token_aliases (optional table — skip if absent)
  try {
    const aliasRes = await tx.query<{ alias_id: string }>(
      `SELECT alias_id FROM ui_token_aliases WHERE alias_id = $1 LIMIT 1`,
      [tokenId],
    );
    if (aliasRes.rows.length > 0) return aliasRes.rows[0]!.alias_id;
  } catch (e) {
    if ((e as { code?: string }).code === "42P01") return null;
    throw e;
  }

  return null;
}
