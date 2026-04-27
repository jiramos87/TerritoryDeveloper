/**
 * Token repo (TECH-2092 / Stage 10.1).
 *
 * Bridges `catalog_entity` (kind=token) + `token_detail`. Single tx
 * create/update of `token_kind` + `value_json` + optional
 * `semantic_target_entity_id` per DEC-A44. Optimistic concurrency via
 * `catalog_entity.updated_at` fingerprint per DEC-A38.
 *
 * Cycle prevention is API-layer (web/lib/tokens/semantic-cycle-check.ts in
 * TECH-2093) — not enforced here. This repo gates per-kind value_json shape
 * via `validateTokenValueJson` + the schema CHECK enforces semantic XOR.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2092 §Plan Digest
 */
import type { Sql } from "postgres";
import { getSql } from "@/lib/db/client";
import type {
  CatalogTokenCreateBody,
  CatalogTokenDto,
  CatalogTokenKind,
  CatalogTokenPatchBody,
  EntityRefSearchRow,
} from "@/types/api/catalog-api";
import {
  TOKEN_KINDS,
  isTokenKind,
  validateTokenValueJson,
} from "./token-detail-schema";
import { semanticCycleCheck } from "@/lib/tokens/semantic-cycle-check";

export type TokenSpineListItem = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  token_kind: string | null;
  current_published_version_id: string | null;
  updated_at: string;
};

export type TokenSpineListFilter = "active" | "retired" | "all";

export type TokenRepoResult<T> =
  | { ok: "ok"; data: T }
  | { ok: "notfound" }
  | { ok: "conflict"; reason: string; current?: CatalogTokenDto }
  | { ok: "validation"; reason: string };

const SLUG_RE = /^[a-z][a-z0-9_]{2,63}$/;

const ALLOWED_PATCH_TOP = new Set([
  "updated_at",
  "display_name",
  "tags",
  "token_detail",
]);

const ALLOWED_DETAIL_KEYS = new Set([
  "token_kind",
  "value_json",
  "semantic_target_entity_id",
]);

function toIdNumOrNull(v: string | null | undefined): number | null {
  if (v == null) return null;
  const trimmed = String(v).trim();
  if (trimmed === "") return null;
  if (!/^\d+$/.test(trimmed)) return null;
  return Number.parseInt(trimmed, 10);
}

export async function listTokensSpine(opts: {
  filter: TokenSpineListFilter;
  kind?: CatalogTokenKind | "all";
  limit: number;
  cursor: string | null;
}): Promise<{ items: TokenSpineListItem[]; next_cursor: string | null }> {
  const sql = getSql();
  const { filter, kind, limit, cursor } = opts;
  const retiredCond =
    filter === "active"
      ? sql` and e.retired_at is null `
      : filter === "retired"
        ? sql` and e.retired_at is not null `
        : sql``;
  const cursorCond =
    cursor != null && cursor.length > 0
      ? sql` and e.id > ${Number.parseInt(cursor, 10)} `
      : sql``;
  const kindCond =
    kind != null && kind !== "all" ? sql` and d.token_kind = ${kind} ` : sql``;
  const rows = (await sql`
    select
      e.id::text as entity_id,
      e.slug,
      e.display_name,
      e.tags,
      e.retired_at,
      e.current_published_version_id::text as current_published_version_id,
      e.updated_at,
      d.token_kind
    from catalog_entity e
    left join token_detail d on d.entity_id = e.id
    where e.kind = 'token' ${retiredCond} ${kindCond} ${cursorCond}
    order by e.id asc
    limit ${limit}
  `) as unknown as TokenSpineListItem[];
  const next_cursor = rows.length === limit ? rows[rows.length - 1]!.entity_id : null;
  return { items: rows, next_cursor };
}

async function loadResolutionRow(
  sql: Sql,
  id: string | null,
): Promise<EntityRefSearchRow | null> {
  if (id == null) return null;
  if (!/^\d+$/.test(id)) return null;
  const idNum = Number.parseInt(id, 10);
  const rows = (await sql`
    select
      id::text as entity_id,
      slug,
      display_name,
      kind,
      current_published_version_id::text as current_published_version_id,
      retired_at
    from catalog_entity
    where id = ${idNum}
  `) as unknown as EntityRefSearchRow[];
  return rows[0] ?? null;
}

export async function getTokenSpineBySlug(
  slug: string,
  externalTx?: Sql,
): Promise<CatalogTokenDto | null> {
  const sql = externalTx ?? getSql();
  const rows = (await sql`
    select
      e.id::text as entity_id,
      e.slug,
      e.display_name,
      e.tags,
      e.retired_at,
      e.current_published_version_id::text as current_published_version_id,
      e.updated_at,
      d.token_kind,
      d.value_json,
      d.semantic_target_entity_id::text as semantic_target_entity_id
    from catalog_entity e
    left join token_detail d on d.entity_id = e.id
    where e.kind = 'token' and e.slug = ${slug}
    limit 1
  `) as unknown as Array<Record<string, unknown>>;
  if (rows.length === 0) return null;
  const r = rows[0]!;

  const semanticTarget = (r.semantic_target_entity_id as string | null) ?? null;
  const semantic_target_resolution = await loadResolutionRow(sql as Sql, semanticTarget);

  const hasDetail = r.token_kind != null;
  return {
    entity_id: r.entity_id as string,
    slug: r.slug as string,
    display_name: r.display_name as string,
    tags: (r.tags as string[]) ?? [],
    retired_at: (r.retired_at as string | null) ?? null,
    current_published_version_id: (r.current_published_version_id as string | null) ?? null,
    updated_at: r.updated_at as string,
    token_detail: hasDetail
      ? {
          token_kind: r.token_kind as CatalogTokenKind,
          value_json: (r.value_json as Record<string, unknown>) ?? {},
          semantic_target_entity_id: semanticTarget,
        }
      : null,
    semantic_target_resolution,
  };
}

export async function createTokenSpine(
  body: CatalogTokenCreateBody,
  sql: Sql,
): Promise<TokenRepoResult<{ entity_id: string; slug: string }>> {
  if (!SLUG_RE.test(body.slug)) {
    return { ok: "validation", reason: "slug must match ^[a-z][a-z0-9_]{2,63}$" };
  }
  if (typeof body.display_name !== "string" || body.display_name.trim() === "") {
    return { ok: "validation", reason: "display_name required" };
  }
  if (!body.token_detail || typeof body.token_detail !== "object") {
    return { ok: "validation", reason: "token_detail required" };
  }
  const td = body.token_detail;
  if (!isTokenKind(td.token_kind)) {
    return {
      ok: "validation",
      reason: `token_kind must be one of ${TOKEN_KINDS.join("|")}`,
    };
  }
  const valid = validateTokenValueJson(td.token_kind, td.value_json);
  if (!valid.ok) return { ok: "validation", reason: valid.reason };

  const semanticTarget = toIdNumOrNull(td.semantic_target_entity_id ?? null);
  if (td.token_kind === "semantic" && semanticTarget == null) {
    return {
      ok: "validation",
      reason: "semantic kind requires semantic_target_entity_id",
    };
  }
  if (td.token_kind !== "semantic" && semanticTarget != null) {
    return {
      ok: "validation",
      reason: "non-semantic kind must not set semantic_target_entity_id",
    };
  }

  // FK pre-check: target must exist + be kind='token'.
  if (semanticTarget != null) {
    const tgt = (await sql`
      select id, kind from catalog_entity where id = ${semanticTarget} limit 1
    `) as unknown as Array<{ id: string; kind: string }>;
    if (tgt.length === 0 || tgt[0]!.kind !== "token") {
      return {
        ok: "validation",
        reason: "semantic_target_entity_id must reference a kind='token' entity",
      };
    }
  }

  const dup = (await sql`
    select 1 from catalog_entity where kind='token' and slug=${body.slug} limit 1
  `) as unknown as Array<{ "?column?": number }>;
  if (dup.length > 0) return { ok: "conflict", reason: "duplicate_slug" };

  const tags = body.tags ?? [];
  const inserted = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('token', ${body.slug}, ${body.display_name}, ${tags})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const entity_id = inserted[0]!.id;
  const idNum = Number.parseInt(entity_id, 10);

  await sql`
    insert into token_detail (
      entity_id, token_kind, value_json, semantic_target_entity_id
    ) values (
      ${idNum},
      ${td.token_kind},
      ${sql.json(td.value_json as Parameters<typeof sql.json>[0])},
      ${semanticTarget}
    )
  `;
  return { ok: "ok", data: { entity_id, slug: body.slug } };
}

function unknownFieldsOf(body: CatalogTokenPatchBody): string[] {
  const unknown: string[] = [];
  for (const k of Object.keys(body)) if (!ALLOWED_PATCH_TOP.has(k)) unknown.push(k);
  if (body.token_detail) {
    for (const k of Object.keys(body.token_detail)) {
      if (!ALLOWED_DETAIL_KEYS.has(k)) unknown.push(`token_detail.${k}`);
    }
  }
  return unknown;
}

export async function patchTokenSpine(
  slug: string,
  body: CatalogTokenPatchBody,
  sql: Sql,
): Promise<TokenRepoResult<CatalogTokenDto>> {
  if (typeof body !== "object" || body == null) {
    return { ok: "validation", reason: "body must be object" };
  }
  if (typeof body.updated_at !== "string") {
    return { ok: "validation", reason: "updated_at required" };
  }
  const unknown = unknownFieldsOf(body);
  if (unknown.length > 0) {
    return { ok: "validation", reason: `unknown fields: ${unknown.join(", ")}` };
  }

  const locked = (await sql`
    select id, updated_at
      from catalog_entity
     where kind = 'token' and slug = ${slug}
     for update
  `) as unknown as Array<{ id: string; updated_at: string }>;
  if (locked.length === 0) return { ok: "notfound" };
  const cur = locked[0]!;
  if (new Date(cur.updated_at).toISOString() !== new Date(body.updated_at).toISOString()) {
    const current = await getTokenSpineBySlug(slug, sql);
    return { ok: "conflict", reason: "stale_updated_at", current: current ?? undefined };
  }
  const idNum = Number.parseInt(cur.id, 10);

  if (body.display_name !== undefined) {
    await sql`update catalog_entity set display_name=${body.display_name} where id=${idNum}`;
  }
  if (body.tags !== undefined) {
    await sql`update catalog_entity set tags=${body.tags} where id=${idNum}`;
  }

  if (body.token_detail) {
    const d = body.token_detail;

    // Resolve effective kind + value for validation: pull current row when not patched.
    const cur2 = (await sql`
      select token_kind, value_json, semantic_target_entity_id::text as semantic_target_entity_id
        from token_detail where entity_id = ${idNum}
    `) as unknown as Array<{
      token_kind: CatalogTokenKind;
      value_json: Record<string, unknown>;
      semantic_target_entity_id: string | null;
    }>;
    if (cur2.length === 0) return { ok: "notfound" };
    const effectiveKind = (d.token_kind ?? cur2[0]!.token_kind) as CatalogTokenKind;
    if (d.token_kind !== undefined && !isTokenKind(d.token_kind)) {
      return {
        ok: "validation",
        reason: `token_kind must be one of ${TOKEN_KINDS.join("|")}`,
      };
    }
    const effectiveValue = (d.value_json ?? cur2[0]!.value_json) as Record<string, unknown>;
    const valid = validateTokenValueJson(effectiveKind, effectiveValue);
    if (!valid.ok) return { ok: "validation", reason: valid.reason };

    const effectiveTarget =
      d.semantic_target_entity_id !== undefined
        ? toIdNumOrNull(d.semantic_target_entity_id)
        : cur2[0]!.semantic_target_entity_id != null
          ? Number.parseInt(cur2[0]!.semantic_target_entity_id, 10)
          : null;

    if (effectiveKind === "semantic" && effectiveTarget == null) {
      return {
        ok: "validation",
        reason: "semantic kind requires semantic_target_entity_id",
      };
    }
    if (effectiveKind !== "semantic" && effectiveTarget != null) {
      return {
        ok: "validation",
        reason: "non-semantic kind must not set semantic_target_entity_id",
      };
    }
    if (effectiveTarget != null) {
      const tgt = (await sql`
        select id, kind from catalog_entity where id = ${effectiveTarget} limit 1
      `) as unknown as Array<{ id: string; kind: string }>;
      if (tgt.length === 0 || tgt[0]!.kind !== "token") {
        return {
          ok: "validation",
          reason: "semantic_target_entity_id must reference a kind='token' entity",
        };
      }
      // DEC-A44 cycle gate (server side; client also checks for fast UX feedback).
      const cycle = await semanticCycleCheck(
        idNum,
        effectiveTarget,
        async (id: number): Promise<number | null> => {
          const row = (await sql`
            select semantic_target_entity_id::text as t
              from token_detail where entity_id = ${id} limit 1
          `) as unknown as Array<{ t: string | null }>;
          if (row.length === 0) return null;
          const t = row[0]!.t;
          return t == null ? null : Number.parseInt(t, 10);
        },
      );
      if (cycle.cycle) {
        return {
          ok: "validation",
          reason: `semantic alias creates a cycle: ${cycle.path.join(" → ")}`,
        };
      }
    }

    if (d.token_kind !== undefined) {
      await sql`update token_detail set token_kind=${effectiveKind} where entity_id=${idNum}`;
    }
    if (d.value_json !== undefined) {
      const json = effectiveValue as Parameters<typeof sql.json>[0];
      await sql`update token_detail set value_json=${sql.json(json)} where entity_id=${idNum}`;
    }
    if (d.semantic_target_entity_id !== undefined) {
      await sql`update token_detail set semantic_target_entity_id=${effectiveTarget} where entity_id=${idNum}`;
    }
  }

  await sql`update catalog_entity set updated_at = now() where id = ${idNum}`;
  const after = await getTokenSpineBySlug(slug, sql);
  if (after == null) return { ok: "notfound" };
  return { ok: "ok", data: after };
}
