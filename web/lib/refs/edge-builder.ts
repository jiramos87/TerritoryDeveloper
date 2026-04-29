/**
 * Edge-builder library for `catalog_ref_edge` (DEC-A37 + DEC-A42,
 * asset-pipeline Stage 14.1 / TECH-3002).
 *
 * `buildEdgesForVersion(kind, entity_id, version_id, sql)` is the sole entry
 * point. It dispatches to the per-kind walker, which reads detail-table rows
 * (panel_detail / button_detail / asset_detail / pool_detail / archetype
 * params) — matches the existing repo pattern (panel-spine-repo, button-spine-repo,
 * asset-spine-repo). NOT `entity_version.params_json` body — that carries
 * authoring payload, not joined ref ids.
 *
 * Idempotency contract:
 *   - Top-level `DELETE FROM catalog_ref_edge WHERE (src_kind, src_id, src_version_id)`
 *     runs before walker dispatch. Caller wraps in `withAudit` tx so DELETE +
 *     INSERT share a tx (rollback drops both halves).
 *   - Walker performs single batched INSERT (multi-VALUES), no per-edge
 *     round-trips.
 *   - Re-running on the same `(kind, entity_id, version_id)` produces the
 *     same row set.
 *
 * Active walkers (5):
 *   - walkPanel    → emits `panel.token` from panel_detail.{palette,frame_style}
 *                     refs (panel→token edges). Note: spec text mentioned
 *                     panel_child child_kind='token' but `panel_child.child_kind`
 *                     enum has no 'token' value — the only token slots on a
 *                     panel are `palette_entity_id` + `frame_style_entity_id`
 *                     on `panel_detail`. Walker reads detail row, not children.
 *   - walkButton   → emits `button.sprite` from the 6 button_detail sprite slot
 *                     columns. Token slots (4 columns) NOT in the 8-role enum
 *                     and are dropped per spec lock.
 *   - walkAsset    → emits `asset.sprite` from asset_detail.{world,
 *                     button_target, button_pressed, button_disabled,
 *                     button_hover}_sprite_entity_id columns.
 *   - walkPool     → stub returning 0 until pool_detail exposes per-member
 *                     refs in a future stage. `pool_detail` schema today has
 *                     no asset-member columns; `pool_member` table is keyed
 *                     to pool entity (not version), so version-scoped ref
 *                     materialization defers.
 *   - walkArchetype → stub returning 0; archetype params resolution helper
 *                     not yet present per spec deferred-with-stub posture.
 *                     Logs a one-time warn so missing-coverage stays visible.
 *
 * Terminal walkers (3): walkSprite, walkToken, walkAudio — return 0.
 *
 * @see db/migrations/0043_catalog_ref_edge.sql — table + indexes
 * @see web/lib/refs/types.ts — CatalogRefEdge + EdgeRole + CatalogKind
 * @see ia/projects/asset-pipeline/stage-14.1 — TECH-3002 §Plan Digest
 */

import type { Sql } from "postgres";

import type { CatalogKind, EdgeRole } from "@/lib/refs/types";

/**
 * Per-walker emitted edge spec. Top-level dispatch resolves
 * `dst_version_id` via `catalog_entity.current_published_version_id`
 * lookups, then batch-INSERTs.
 */
interface WalkerEdgeSpec {
  dst_kind: CatalogKind;
  dst_id: number;
  edge_role: EdgeRole;
}

let archetypeStubWarned = false;

/**
 * Resolve target `current_published_version_id` for each `(dst_kind, dst_id)`
 * pair in the batch. Returns map keyed `${dst_kind}:${dst_id}` → version id
 * (or null when target unpublished / missing). Caller filters null entries
 * before INSERT — dangling refs are caught by Layer 2 lint (TECH-3003), not
 * the edge builder. If a ref slips past lint (target retired between lint
 * + publish), the edge is silently dropped here.
 */
async function resolveDstVersions(
  edges: WalkerEdgeSpec[],
  sql: Sql,
): Promise<Map<string, number>> {
  const out = new Map<string, number>();
  if (edges.length === 0) return out;
  const ids = Array.from(new Set(edges.map((e) => e.dst_id)));
  const rows = await sql<
    Array<{ id: number; kind: string; current_published_version_id: number | null }>
  >`
    select id, kind, current_published_version_id
    from catalog_entity
    where id in ${sql(ids)}
  `;
  for (const row of rows) {
    if (row.current_published_version_id !== null) {
      out.set(`${row.kind}:${row.id}`, row.current_published_version_id);
    }
  }
  return out;
}

/**
 * Insert resolved edges in a single batched INSERT. Filters out edges whose
 * target failed to resolve (target unpublished / missing — should have been
 * caught by lint).
 */
async function insertEdges(
  src_kind: CatalogKind,
  src_id: number,
  src_version_id: number,
  edges: WalkerEdgeSpec[],
  sql: Sql,
): Promise<number> {
  if (edges.length === 0) return 0;
  const dstMap = await resolveDstVersions(edges, sql);
  const rows = edges
    .map((e) => {
      const dvid = dstMap.get(`${e.dst_kind}:${e.dst_id}`);
      if (dvid === undefined) return null;
      return {
        src_kind,
        src_id,
        src_version_id,
        dst_kind: e.dst_kind,
        dst_id: e.dst_id,
        dst_version_id: dvid,
        edge_role: e.edge_role,
      };
    })
    .filter((r): r is NonNullable<typeof r> => r !== null);
  if (rows.length === 0) return 0;
  await sql`
    insert into catalog_ref_edge ${sql(
      rows,
      "src_kind",
      "src_id",
      "src_version_id",
      "dst_kind",
      "dst_id",
      "dst_version_id",
      "edge_role",
    )}
  `;
  return rows.length;
}

// ─── walkers ────────────────────────────────────────────────────────────────

/**
 * Panel → token: read panel_detail's two token slot columns
 * (palette_entity_id + frame_style_entity_id). Both nullable; non-null
 * entries become `panel.token` edges.
 *
 * Note: spec text mentioned `panel_child WHERE child_kind='token'` but the
 * `panel_child.child_kind` CHECK constraint has no 'token' value (enum is
 * button|panel|label|spacer|audio|sprite|label_inline). Locked decision
 * per §Pending Decisions latitude: walker reads detail-table token slots.
 */
async function walkPanel(
  src_id: number,
  src_version_id: number,
  sql: Sql,
): Promise<number> {
  type Row = {
    palette_entity_id: number | null;
    frame_style_entity_id: number | null;
  };
  const rows = await sql<Row[]>`
    select palette_entity_id, frame_style_entity_id
    from panel_detail
    where entity_id = ${src_id}
    limit 1
  `;
  if (rows.length === 0) return 0;
  const detail = rows[0];
  const edges: WalkerEdgeSpec[] = [];
  if (detail.palette_entity_id !== null) {
    edges.push({
      dst_kind: "token",
      dst_id: detail.palette_entity_id,
      edge_role: "panel.token",
    });
  }
  if (detail.frame_style_entity_id !== null) {
    edges.push({
      dst_kind: "token",
      dst_id: detail.frame_style_entity_id,
      edge_role: "panel.token",
    });
  }
  return insertEdges("panel", src_id, src_version_id, edges, sql);
}

/**
 * Button → sprite: read 6 sprite slot columns from button_detail. Token
 * slot columns (4) NOT enumerated in the 8 edge_roles — dropped per spec.
 */
async function walkButton(
  src_id: number,
  src_version_id: number,
  sql: Sql,
): Promise<number> {
  type Row = {
    sprite_idle_entity_id: number | null;
    sprite_hover_entity_id: number | null;
    sprite_pressed_entity_id: number | null;
    sprite_disabled_entity_id: number | null;
    sprite_icon_entity_id: number | null;
    sprite_badge_entity_id: number | null;
  };
  const rows = await sql<Row[]>`
    select sprite_idle_entity_id, sprite_hover_entity_id,
           sprite_pressed_entity_id, sprite_disabled_entity_id,
           sprite_icon_entity_id, sprite_badge_entity_id
    from button_detail
    where entity_id = ${src_id}
    limit 1
  `;
  if (rows.length === 0) return 0;
  const detail = rows[0];
  const slotIds: Array<number | null> = [
    detail.sprite_idle_entity_id,
    detail.sprite_hover_entity_id,
    detail.sprite_pressed_entity_id,
    detail.sprite_disabled_entity_id,
    detail.sprite_icon_entity_id,
    detail.sprite_badge_entity_id,
  ];
  const edges: WalkerEdgeSpec[] = slotIds
    .filter((id): id is number => id !== null)
    .map((id) => ({
      dst_kind: "sprite",
      dst_id: id,
      edge_role: "button.sprite",
    }));
  return insertEdges("button", src_id, src_version_id, edges, sql);
}

/**
 * Asset → sprite: read 5 sprite slot columns from asset_detail
 * (world + button_target/pressed/disabled/hover).
 */
async function walkAsset(
  src_id: number,
  src_version_id: number,
  sql: Sql,
): Promise<number> {
  type Row = {
    world_sprite_entity_id: number | null;
    button_target_sprite_entity_id: number | null;
    button_pressed_sprite_entity_id: number | null;
    button_disabled_sprite_entity_id: number | null;
    button_hover_sprite_entity_id: number | null;
  };
  const rows = await sql<Row[]>`
    select world_sprite_entity_id, button_target_sprite_entity_id,
           button_pressed_sprite_entity_id, button_disabled_sprite_entity_id,
           button_hover_sprite_entity_id
    from asset_detail
    where entity_id = ${src_id}
    limit 1
  `;
  if (rows.length === 0) return 0;
  const detail = rows[0];
  const slotIds: Array<number | null> = [
    detail.world_sprite_entity_id,
    detail.button_target_sprite_entity_id,
    detail.button_pressed_sprite_entity_id,
    detail.button_disabled_sprite_entity_id,
    detail.button_hover_sprite_entity_id,
  ];
  const edges: WalkerEdgeSpec[] = slotIds
    .filter((id): id is number => id !== null)
    .map((id) => ({
      dst_kind: "sprite",
      dst_id: id,
      edge_role: "asset.sprite",
    }));
  return insertEdges("asset", src_id, src_version_id, edges, sql);
}

/**
 * Pool → asset: stub. `pool_detail` schema does not yet expose per-member
 * asset refs in a version-scoped way; `pool_member` table is keyed to the
 * pool entity, not the version. Materialization defers to a future stage.
 */
async function walkPool(): Promise<number> {
  return 0;
}

/**
 * Archetype → asset/sprite/token/audio: stub. Archetype params are stored
 * in `entity_version.params_json` (no detail table); the param-ref
 * resolution helper is deferred per spec. Walker emits 0 + logs a single
 * warn so missing-coverage stays visible without spamming logs.
 */
async function walkArchetype(): Promise<number> {
  if (!archetypeStubWarned) {
    archetypeStubWarned = true;
    console.warn(
      "[edge-builder] archetype walker is a stub (Stage 14.1 deferred-with-stub posture); zero edges emitted",
    );
  }
  return 0;
}

/**
 * Terminal walker — sprite is a leaf kind (no outbound refs in the 8-role
 * enum).
 */
async function walkSprite(): Promise<number> {
  return 0;
}

/** Terminal walker — token is a leaf kind. */
async function walkToken(): Promise<number> {
  return 0;
}

/** Terminal walker — audio is a leaf kind. */
async function walkAudio(): Promise<number> {
  return 0;
}

// ─── dispatch ───────────────────────────────────────────────────────────────

type WalkerFn = (
  src_id: number,
  src_version_id: number,
  sql: Sql,
) => Promise<number>;

const WALKERS: Record<CatalogKind, WalkerFn> = {
  panel: walkPanel,
  button: walkButton,
  asset: walkAsset,
  pool: () => walkPool(),
  archetype: () => walkArchetype(),
  sprite: () => walkSprite(),
  token: () => walkToken(),
  audio: () => walkAudio(),
};

/**
 * Materialize `catalog_ref_edge` rows for one published `(kind, entity_id,
 * version_id)`. Idempotent — DELETEs existing edges for that triple before
 * dispatching to the walker.
 *
 * @param kind        Source entity kind (matches `catalog_entity.kind`).
 * @param entity_id   Source `catalog_entity.id`.
 * @param version_id  Source `entity_version.id`.
 * @param sql         Postgres client; caller wraps in `withAudit` tx so
 *                    DELETE + INSERT share rollback scope.
 * @returns Total edges inserted (may be 0 for terminal/stub walkers).
 */
export async function buildEdgesForVersion(
  kind: CatalogKind,
  entity_id: number,
  version_id: number,
  sql: Sql,
): Promise<number> {
  const walker = WALKERS[kind];
  if (walker === undefined) {
    throw new Error(`[edge-builder] unknown CatalogKind: ${String(kind)}`);
  }
  await sql`
    delete from catalog_ref_edge
    where src_kind       = ${kind}
      and src_id         = ${entity_id}
      and src_version_id = ${version_id}
  `;
  return walker(entity_id, version_id, sql);
}
