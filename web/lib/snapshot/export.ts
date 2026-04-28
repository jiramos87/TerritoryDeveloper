/**
 * exportSnapshot — per-kind catalog JSON export pipeline (DEC-A9, DEC-A21, DEC-A39).
 *
 * Reads the published (or draft+published when `includeDrafts=true`) `entity_version`
 * rows for each of the 8 closed-set kinds, joins to `catalog_entity` + the per-kind
 * detail table, serializes the row arrays via {@link canonicalStringify}, writes
 * `Assets/StreamingAssets/catalog/{kind}.json.tmp` → `fs.rename` atomic, writes
 * `manifest.json` last (also via `.tmp` + rename so Unity's `FileSystemWatcher`
 * cannot observe a torn write), then inserts a `catalog_snapshot` row carrying the
 * manifest hash + per-kind row counts + author.
 *
 * Determinism contract:
 *   - Row arrays sorted by `slug` (stable across runs given identical DB state).
 *   - Object keys sorted via `canonicalStringify` (RFC-8785 shape).
 *   - Manifest hash = sha256 hex over kind-ordered concatenation of bytes
 *     (sprite/asset/button/panel/audio/pool/token/archetype). Re-running over an
 *     unchanged DB produces byte-identical files + identical hash (covered by
 *     {@link "@/lib/snapshot/__tests__/export.test.ts" | golden test}).
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2673 §Plan Digest
 */

import { mkdirSync, promises as fsp } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { getSql } from "@/lib/db/client";
import { canonicalStringify } from "@/lib/json/canonical";
import {
  computeManifestHash,
  emptyEntityCounts,
  SNAPSHOT_KINDS,
  type EntityCounts,
  type Manifest,
  type SnapshotKind,
} from "@/lib/snapshot/manifest";

/**
 * Repo root resolved relative to this file (`web/lib/snapshot/export.ts` →
 * three `..` segments). Centralized so test seeds can override via the
 * `outputRootOverride` opt below.
 */
const HERE_DIR = path.dirname(fileURLToPath(import.meta.url));
const DEFAULT_REPO_ROOT = path.resolve(HERE_DIR, "..", "..", "..");

/** Repo-relative path of the manifest written at the end of every export. */
export const MANIFEST_RELATIVE_PATH =
  "Assets/StreamingAssets/catalog/manifest.json";

/** Repo-relative directory where per-kind files + manifest are written. */
export const SNAPSHOT_DIR_RELATIVE = "Assets/StreamingAssets/catalog";

/** Manifest schemaVersion bumped to 2 for the per-kind v2 export. */
export const MANIFEST_SCHEMA_VERSION = 2;

export type ExportSnapshotResult = {
  snapshotId: string;
  hash: string;
  manifestPath: string;
};

export type ExportSnapshotOptions = {
  /**
   * Include `entity_version.status='draft'` rows in addition to `published`.
   * Default `false` per DEC: drafts admit drift + would break golden hash.
   */
  includeDrafts?: boolean;

  /**
   * Override the repo root used to resolve `Assets/StreamingAssets/catalog/`.
   * Tests pass a temp dir to keep the working tree clean.
   */
  outputRootOverride?: string;

  /**
   * Override `now()` for deterministic `generatedAt` in tests. The DB
   * `created_at` column always uses server-side `now()`; only the manifest +
   * per-row JSON `generatedAt` field consults this hook.
   */
  nowOverride?: () => Date;
};

/**
 * Stable per-row shape exported per kind. Keys are JSON-friendly; nested
 * objects + numbers normalized via `canonicalStringify`. Detail fields
 * inlined onto the row for cheap Unity loader access (no second join).
 */
export type ExportedEntityRow = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  version_id: string;
  version_number: number;
  status: "draft" | "published";
  params_json: unknown;
  /**
   * Per-kind detail map. Exact keys vary per kind (see per-kind queries
   * below). `null` when no detail row exists (archetype: no detail table).
   */
  detail: Record<string, unknown> | null;
};

/** Per-kind file shape: { kind, generatedAt, rows: ExportedEntityRow[] }. */
export type PerKindFile = {
  kind: SnapshotKind;
  generatedAt: string;
  rows: ExportedEntityRow[];
};

/**
 * Main entry point: query DB → serialize → atomic write → row insert.
 * Returns `{ snapshotId, hash, manifestPath }`.
 *
 * Throws on missing `users` row for `authorUserId`, FK violation, or any
 * filesystem error during the write phase. Pre-existing files at
 * `{kind}.json` / `manifest.json` are overwritten atomically (rename).
 */
export async function exportSnapshot(
  authorUserId: string,
  options: ExportSnapshotOptions = {},
): Promise<ExportSnapshotResult> {
  const includeDrafts = options.includeDrafts ?? false;
  const repoRoot = options.outputRootOverride ?? DEFAULT_REPO_ROOT;
  const now = options.nowOverride ? options.nowOverride() : new Date();
  const generatedAt = now.toISOString();

  const sql = getSql();

  // 1. Per-kind read: serial sequence keeps SQL log + Postgres connection
  //    pressure low. Each fetch returns a sorted ExportedEntityRow[].
  const perKindRows: Record<SnapshotKind, ExportedEntityRow[]> = {
    sprite: await fetchSprites(sql, includeDrafts),
    asset: await fetchAssets(sql, includeDrafts),
    button: await fetchButtons(sql, includeDrafts),
    panel: await fetchPanels(sql, includeDrafts),
    audio: await fetchAudio(sql, includeDrafts),
    pool: await fetchPools(sql, includeDrafts),
    token: await fetchTokens(sql, includeDrafts),
    archetype: await fetchArchetypes(sql, includeDrafts),
  };

  // 2. Serialize each kind to canonical JSON bytes.
  const perKindBytes: Record<SnapshotKind, Buffer> = {} as Record<
    SnapshotKind,
    Buffer
  >;
  const entityCounts: EntityCounts = emptyEntityCounts();
  for (const kind of SNAPSHOT_KINDS) {
    const rows = perKindRows[kind];
    const file: PerKindFile = { kind, generatedAt, rows };
    const text = canonicalStringify(file);
    perKindBytes[kind] = Buffer.from(text, "utf8");
    entityCounts[kind] = rows.length;
  }

  // 3. Compute manifest hash before writing any file (hash is the source of
  //    truth — late filesystem failures must not leave a half-hashed row).
  const hash = computeManifestHash(perKindBytes);

  // 4. Atomic write phase. Per-kind files first (any order — Unity loader
  //    only fires on `manifest.json` change). Manifest last so the watcher
  //    sees a fully-populated directory.
  const outDir = path.join(repoRoot, SNAPSHOT_DIR_RELATIVE);
  mkdirSync(outDir, { recursive: true });

  for (const kind of SNAPSHOT_KINDS) {
    const target = path.join(outDir, `${kind}.json`);
    await atomicWrite(target, perKindBytes[kind]);
  }

  const manifest: Manifest = {
    schemaVersion: MANIFEST_SCHEMA_VERSION,
    generatedAt,
    snapshotHash: hash,
    entityCounts,
  };
  const manifestText = canonicalStringify(manifest);
  const manifestTarget = path.join(outDir, "manifest.json");
  await atomicWrite(manifestTarget, Buffer.from(manifestText, "utf8"));

  // 5. Insert provenance row last; created_by FK rejects unknown user.
  const inserted = (await sql`
    insert into catalog_snapshot (hash, manifest_path, entity_counts_json, schema_version, status, created_by)
    values (
      ${hash},
      ${MANIFEST_RELATIVE_PATH},
      ${sql.json(entityCounts as unknown as Parameters<typeof sql.json>[0])},
      ${MANIFEST_SCHEMA_VERSION},
      'active',
      ${authorUserId}::uuid
    )
    returning id::text as id
  `) as unknown as Array<{ id: string }>;

  if (inserted.length !== 1 || inserted[0] === undefined) {
    throw new Error(
      "exportSnapshot: catalog_snapshot insert returned no row.",
    );
  }

  return {
    snapshotId: inserted[0].id,
    hash,
    manifestPath: MANIFEST_RELATIVE_PATH,
  };
}

/**
 * Atomic-write helper: write `{target}.tmp` then `fs.rename` on top of the
 * final path. POSIX rename is atomic within a single filesystem; Unity's
 * `FileSystemWatcher` will not observe a torn read.
 */
async function atomicWrite(target: string, bytes: Buffer): Promise<void> {
  const tmp = `${target}.tmp`;
  await fsp.writeFile(tmp, bytes);
  await fsp.rename(tmp, target);
}

// -- Per-kind fetchers --------------------------------------------------------
//
// Pattern: select latest `entity_version` (or all matching status set) per
// `catalog_entity` row, left-join the per-kind detail table, return rows
// sorted by `slug`. Determinism:
//   - `entity_version.id` order tie-break inside a single entity (only one
//     `published` row per entity by spec).
//   - `slug` ordering at the outermost level → stable canonical bytes.

type SqlClient = ReturnType<typeof getSql>;

function statusFilter(includeDrafts: boolean): string[] {
  return includeDrafts ? ["draft", "published"] : ["published"];
}

async function fetchSprites(
  sql: SqlClient,
  includeDrafts: boolean,
): Promise<ExportedEntityRow[]> {
  const statuses = statusFilter(includeDrafts);
  const rows = (await sql`
    select
      e.id::text                  as entity_id,
      e.slug                      as slug,
      e.display_name              as display_name,
      e.tags                      as tags,
      v.id::text                  as version_id,
      v.version_number            as version_number,
      v.status                    as status,
      v.params_json               as params_json,
      d.legacy_sprite_id::text    as legacy_sprite_id,
      d.source_uri                as source_uri,
      d.assets_path               as assets_path,
      d.pixels_per_unit           as pixels_per_unit,
      d.pivot_x                   as pivot_x,
      d.pivot_y                   as pivot_y,
      d.provenance                as provenance,
      d.source_run_id::text       as source_run_id,
      d.source_variant_idx        as source_variant_idx,
      d.build_fingerprint         as build_fingerprint,
      d.palette_hash              as palette_hash
    from catalog_entity e
    join entity_version v on v.entity_id = e.id
    left join sprite_detail d on d.entity_id = e.id
    where e.kind = 'sprite'
      and e.retired_at is null
      and v.status = any(${statuses})
    order by e.slug, v.version_number
  `) as unknown as SpriteRow[];
  return rows.map(toExportRow);
}

async function fetchAssets(
  sql: SqlClient,
  includeDrafts: boolean,
): Promise<ExportedEntityRow[]> {
  const statuses = statusFilter(includeDrafts);
  const rows = (await sql`
    select
      e.id::text  as entity_id,
      e.slug      as slug,
      e.display_name as display_name,
      e.tags      as tags,
      v.id::text  as version_id,
      v.version_number as version_number,
      v.status    as status,
      v.params_json as params_json,
      d.legacy_asset_id::text             as legacy_asset_id,
      d.category                          as category,
      d.footprint_w                       as footprint_w,
      d.footprint_h                       as footprint_h,
      d.placement_mode                    as placement_mode,
      d.unlocks_after                     as unlocks_after,
      d.has_button                        as has_button,
      d.world_sprite_entity_id::text      as world_sprite_entity_id,
      d.button_target_sprite_entity_id::text   as button_target_sprite_entity_id,
      d.button_pressed_sprite_entity_id::text  as button_pressed_sprite_entity_id,
      d.button_disabled_sprite_entity_id::text as button_disabled_sprite_entity_id,
      d.button_hover_sprite_entity_id::text    as button_hover_sprite_entity_id,
      d.primary_subtype_pool_id::text          as primary_subtype_pool_id
    from catalog_entity e
    join entity_version v on v.entity_id = e.id
    left join asset_detail d on d.entity_id = e.id
    where e.kind = 'asset'
      and e.retired_at is null
      and v.status = any(${statuses})
    order by e.slug, v.version_number
  `) as unknown as AssetRow[];
  return rows.map(toExportRow);
}

async function fetchButtons(
  sql: SqlClient,
  includeDrafts: boolean,
): Promise<ExportedEntityRow[]> {
  const statuses = statusFilter(includeDrafts);
  const rows = (await sql`
    select
      e.id::text  as entity_id,
      e.slug      as slug,
      e.display_name as display_name,
      e.tags      as tags,
      v.id::text  as version_id,
      v.version_number as version_number,
      v.status    as status,
      v.params_json as params_json,
      d.sprite_idle_entity_id::text     as sprite_idle_entity_id,
      d.sprite_hover_entity_id::text    as sprite_hover_entity_id,
      d.sprite_pressed_entity_id::text  as sprite_pressed_entity_id,
      d.sprite_disabled_entity_id::text as sprite_disabled_entity_id,
      d.sprite_icon_entity_id::text     as sprite_icon_entity_id,
      d.sprite_badge_entity_id::text    as sprite_badge_entity_id,
      d.token_palette_entity_id::text   as token_palette_entity_id,
      d.token_frame_style_entity_id::text  as token_frame_style_entity_id,
      d.token_font_entity_id::text         as token_font_entity_id,
      d.token_illumination_entity_id::text as token_illumination_entity_id,
      d.size_variant            as size_variant,
      d.action_id               as action_id,
      d.enable_predicate_json   as enable_predicate_json
    from catalog_entity e
    join entity_version v on v.entity_id = e.id
    left join button_detail d on d.entity_id = e.id
    where e.kind = 'button'
      and e.retired_at is null
      and v.status = any(${statuses})
    order by e.slug, v.version_number
  `) as unknown as ButtonRow[];
  return rows.map(toExportRow);
}

async function fetchPanels(
  sql: SqlClient,
  includeDrafts: boolean,
): Promise<ExportedEntityRow[]> {
  const statuses = statusFilter(includeDrafts);
  const rows = (await sql`
    select
      e.id::text  as entity_id,
      e.slug      as slug,
      e.display_name as display_name,
      e.tags      as tags,
      v.id::text  as version_id,
      v.version_number as version_number,
      v.status    as status,
      v.params_json as params_json,
      d.archetype_entity_id::text         as archetype_entity_id,
      d.background_sprite_entity_id::text as background_sprite_entity_id,
      d.palette_entity_id::text           as palette_entity_id,
      d.frame_style_entity_id::text       as frame_style_entity_id,
      d.layout_template                   as layout_template,
      d.modal                             as modal
    from catalog_entity e
    join entity_version v on v.entity_id = e.id
    left join panel_detail d on d.entity_id = e.id
    where e.kind = 'panel'
      and e.retired_at is null
      and v.status = any(${statuses})
    order by e.slug, v.version_number
  `) as unknown as PanelRow[];
  return rows.map(toExportRow);
}

async function fetchAudio(
  sql: SqlClient,
  includeDrafts: boolean,
): Promise<ExportedEntityRow[]> {
  const statuses = statusFilter(includeDrafts);
  const rows = (await sql`
    select
      e.id::text  as entity_id,
      e.slug      as slug,
      e.display_name as display_name,
      e.tags      as tags,
      v.id::text  as version_id,
      v.version_number as version_number,
      v.status    as status,
      v.params_json as params_json,
      d.source_uri      as source_uri,
      d.assets_path     as assets_path,
      d.duration_ms     as duration_ms,
      d.sample_rate     as sample_rate,
      d.channels        as channels,
      d.loudness_lufs   as loudness_lufs,
      d.peak_db         as peak_db,
      d.fingerprint     as fingerprint
    from catalog_entity e
    join entity_version v on v.entity_id = e.id
    left join audio_detail d on d.entity_id = e.id
    where e.kind = 'audio'
      and e.retired_at is null
      and v.status = any(${statuses})
    order by e.slug, v.version_number
  `) as unknown as AudioRow[];
  return rows.map(toExportRow);
}

async function fetchPools(
  sql: SqlClient,
  includeDrafts: boolean,
): Promise<ExportedEntityRow[]> {
  const statuses = statusFilter(includeDrafts);
  const rows = (await sql`
    select
      e.id::text  as entity_id,
      e.slug      as slug,
      e.display_name as display_name,
      e.tags      as tags,
      v.id::text  as version_id,
      v.version_number as version_number,
      v.status    as status,
      v.params_json as params_json,
      d.legacy_pool_id::text  as legacy_pool_id,
      d.primary_subtype       as primary_subtype,
      d.owner_category        as owner_category
    from catalog_entity e
    join entity_version v on v.entity_id = e.id
    left join pool_detail d on d.entity_id = e.id
    where e.kind = 'pool'
      and e.retired_at is null
      and v.status = any(${statuses})
    order by e.slug, v.version_number
  `) as unknown as PoolRow[];
  return rows.map(toExportRow);
}

async function fetchTokens(
  sql: SqlClient,
  includeDrafts: boolean,
): Promise<ExportedEntityRow[]> {
  const statuses = statusFilter(includeDrafts);
  const rows = (await sql`
    select
      e.id::text  as entity_id,
      e.slug      as slug,
      e.display_name as display_name,
      e.tags      as tags,
      v.id::text  as version_id,
      v.version_number as version_number,
      v.status    as status,
      v.params_json as params_json,
      d.token_kind                         as token_kind,
      d.value_json                         as value_json,
      d.semantic_target_entity_id::text    as semantic_target_entity_id
    from catalog_entity e
    join entity_version v on v.entity_id = e.id
    left join token_detail d on d.entity_id = e.id
    where e.kind = 'token'
      and e.retired_at is null
      and v.status = any(${statuses})
    order by e.slug, v.version_number
  `) as unknown as TokenRow[];
  return rows.map(toExportRow);
}

async function fetchArchetypes(
  sql: SqlClient,
  includeDrafts: boolean,
): Promise<ExportedEntityRow[]> {
  // Archetype has no detail table; the entire shape lives in `params_json`.
  const statuses = statusFilter(includeDrafts);
  const rows = (await sql`
    select
      e.id::text  as entity_id,
      e.slug      as slug,
      e.display_name as display_name,
      e.tags      as tags,
      v.id::text  as version_id,
      v.version_number as version_number,
      v.status    as status,
      v.params_json as params_json
    from catalog_entity e
    join entity_version v on v.entity_id = e.id
    where e.kind = 'archetype'
      and e.retired_at is null
      and v.status = any(${statuses})
    order by e.slug, v.version_number
  `) as unknown as ArchetypeRow[];
  return rows.map((row) => ({
    entity_id: row.entity_id,
    slug: row.slug,
    display_name: row.display_name,
    tags: row.tags ?? [],
    version_id: row.version_id,
    version_number: row.version_number,
    status: row.status,
    params_json: row.params_json,
    detail: null,
  }));
}

// -- Row → ExportedEntityRow --------------------------------------------------

type CommonRow = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[] | null;
  version_id: string;
  version_number: number;
  status: "draft" | "published";
  params_json: unknown;
};

type SpriteRow = CommonRow & {
  legacy_sprite_id: string | null;
  source_uri: string | null;
  assets_path: string | null;
  pixels_per_unit: number;
  pivot_x: number;
  pivot_y: number;
  provenance: string;
  source_run_id: string | null;
  source_variant_idx: number | null;
  build_fingerprint: string | null;
  palette_hash: string | null;
};

type AssetRow = CommonRow & {
  legacy_asset_id: string | null;
  category: string | null;
  footprint_w: number | null;
  footprint_h: number | null;
  placement_mode: string | null;
  unlocks_after: string | null;
  has_button: boolean | null;
  world_sprite_entity_id: string | null;
  button_target_sprite_entity_id: string | null;
  button_pressed_sprite_entity_id: string | null;
  button_disabled_sprite_entity_id: string | null;
  button_hover_sprite_entity_id: string | null;
  primary_subtype_pool_id: string | null;
};

type ButtonRow = CommonRow & {
  sprite_idle_entity_id: string | null;
  sprite_hover_entity_id: string | null;
  sprite_pressed_entity_id: string | null;
  sprite_disabled_entity_id: string | null;
  sprite_icon_entity_id: string | null;
  sprite_badge_entity_id: string | null;
  token_palette_entity_id: string | null;
  token_frame_style_entity_id: string | null;
  token_font_entity_id: string | null;
  token_illumination_entity_id: string | null;
  size_variant: string | null;
  action_id: string | null;
  enable_predicate_json: unknown;
};

type PanelRow = CommonRow & {
  archetype_entity_id: string | null;
  background_sprite_entity_id: string | null;
  palette_entity_id: string | null;
  frame_style_entity_id: string | null;
  layout_template: string | null;
  modal: boolean | null;
};

type AudioRow = CommonRow & {
  source_uri: string | null;
  assets_path: string | null;
  duration_ms: number | null;
  sample_rate: number | null;
  channels: number | null;
  loudness_lufs: number | null;
  peak_db: number | null;
  fingerprint: string | null;
};

type PoolRow = CommonRow & {
  legacy_pool_id: string | null;
  primary_subtype: string | null;
  owner_category: string | null;
};

type TokenRow = CommonRow & {
  token_kind: string | null;
  value_json: unknown;
  semantic_target_entity_id: string | null;
};

type ArchetypeRow = CommonRow;

/**
 * Generic mapper: hoist common fields, fold remaining columns into `detail`.
 * Drops `null` detail columns to keep canonical bytes minimal.
 */
function toExportRow(row: CommonRow & Record<string, unknown>): ExportedEntityRow {
  const detail: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(row)) {
    if (
      key === "entity_id" ||
      key === "slug" ||
      key === "display_name" ||
      key === "tags" ||
      key === "version_id" ||
      key === "version_number" ||
      key === "status" ||
      key === "params_json"
    ) {
      continue;
    }
    if (value === null || value === undefined) continue;
    detail[key] = value;
  }
  return {
    entity_id: row.entity_id,
    slug: row.slug,
    display_name: row.display_name,
    tags: row.tags ?? [],
    version_id: row.version_id,
    version_number: row.version_number,
    status: row.status,
    params_json: row.params_json,
    detail: Object.keys(detail).length === 0 ? null : detail,
  };
}

