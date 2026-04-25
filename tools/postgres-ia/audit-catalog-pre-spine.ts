#!/usr/bin/env tsx
/**
 * audit-catalog-pre-spine.ts
 *
 * Stage 0 (asset-pipeline-stage-0-1-impl.md). Reads existing
 * catalog_* tables and emits a Markdown audit report + JSON twin
 * under docs/audits/. The JSON file is consumed by Stage 1 backfill
 * (0022_catalog_detail_link.sql) for sanity gating.
 *
 * Read-only. Never mutates DB state.
 *
 * Inputs: DATABASE_URL or config/postgres-dev.json (resolveDatabaseUrl).
 * Outputs:
 *   - docs/audits/catalog-pre-spine-{YYYY-MM-DD}.md
 *   - docs/audits/catalog-pre-spine-{YYYY-MM-DD}.json
 *
 * Exit codes:
 *   0 — audit ran end-to-end. Section 9 (manual triage) may still list issues.
 *   1 — DB unreachable or required legacy table missing.
 */

import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Client } from 'pg';
import { resolveDatabaseUrl } from './resolve-database-url.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../..');
const AUDIT_DIR = join(REPO_ROOT, 'docs/audits');

const SLUG_REGEX = /^[a-z][a-z0-9_]{2,63}$/;

type Counts = Record<string, number>;
type IssueList = string[];

interface AuditPayload {
  generated_at: string;
  database_url_redacted: string;
  snapshot_hint: string;
  row_counts: Counts;
  fk_integrity: {
    orphan_asset_sprite_missing_asset: number;
    orphan_asset_sprite_missing_sprite: number;
    orphan_economy_missing_asset: number;
    orphan_pool_member_missing_pool: number;
    orphan_pool_member_missing_asset: number;
    asset_replaced_by_missing: number;
  };
  slug_collisions_across_categories: Array<{
    slug: string;
    count: number;
    categories: string[];
  }>;
  pool_slug_vs_asset_slug_conflicts: Array<{
    slug: string;
    asset_categories: string[];
    pool_owner_categories: string[];
  }>;
  sprite_fingerprint_duplicates: Array<{
    fingerprint: string;
    count: number;
    sprite_ids: number[];
  }>;
  pool_integrity: {
    pools_with_zero_members: Array<{
      pool_id: number;
      slug: string;
      owner_category: string;
      owner_subtype: string | null;
    }>;
    pools_missing_owner_subtype: Array<{
      pool_id: number;
      slug: string;
      owner_category: string;
    }>;
    members_referencing_retired_assets: Array<{
      pool_id: number;
      asset_id: number;
      asset_slug: string;
    }>;
  };
  slug_regex_violations: {
    asset_violations: Array<{ id: number; slug: string }>;
    pool_violations: Array<{ id: number; slug: string }>;
  };
  zone_s_seed_ids: Array<{ id: number; slug: string }>;
  triage_issues: IssueList;
}

function redactUrl(url: string): string {
  try {
    const u = new URL(url);
    if (u.password) u.password = '***';
    return u.toString();
  } catch {
    return '<unparsable>';
  }
}

async function tableExists(client: Client, table: string): Promise<boolean> {
  const r = await client.query(
    `SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = $1`,
    [table],
  );
  return (r.rowCount ?? 0) > 0;
}

async function rowCount(client: Client, table: string): Promise<number> {
  const r = await client.query(`SELECT count(*)::int AS n FROM ${table}`);
  return r.rows[0]?.n ?? 0;
}

async function gatherRowCounts(client: Client): Promise<Counts> {
  const tables = [
    'catalog_asset',
    'catalog_sprite',
    'catalog_asset_sprite',
    'catalog_economy',
    'catalog_spawn_pool',
    'catalog_pool_member',
  ];
  const out: Counts = {};
  for (const t of tables) {
    out[t] = await tableExists(client, t) ? await rowCount(client, t) : -1;
  }
  return out;
}

async function gatherFkIntegrity(client: Client) {
  const orphanAssetSpriteMissingAsset = await client.query(`
    SELECT count(*)::int AS n FROM catalog_asset_sprite cas
    LEFT JOIN catalog_asset a ON a.id = cas.asset_id
    WHERE a.id IS NULL
  `);
  const orphanAssetSpriteMissingSprite = await client.query(`
    SELECT count(*)::int AS n FROM catalog_asset_sprite cas
    LEFT JOIN catalog_sprite s ON s.id = cas.sprite_id
    WHERE s.id IS NULL
  `);
  const orphanEconomyMissingAsset = await client.query(`
    SELECT count(*)::int AS n FROM catalog_economy ce
    LEFT JOIN catalog_asset a ON a.id = ce.asset_id
    WHERE a.id IS NULL
  `);
  const orphanPoolMemberMissingPool = await client.query(`
    SELECT count(*)::int AS n FROM catalog_pool_member m
    LEFT JOIN catalog_spawn_pool p ON p.id = m.pool_id
    WHERE p.id IS NULL
  `);
  const orphanPoolMemberMissingAsset = await client.query(`
    SELECT count(*)::int AS n FROM catalog_pool_member m
    LEFT JOIN catalog_asset a ON a.id = m.asset_id
    WHERE a.id IS NULL
  `);
  const assetReplacedByMissing = await client.query(`
    SELECT count(*)::int AS n FROM catalog_asset a
    WHERE a.replaced_by IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM catalog_asset b WHERE b.id = a.replaced_by)
  `);
  return {
    orphan_asset_sprite_missing_asset: orphanAssetSpriteMissingAsset.rows[0].n,
    orphan_asset_sprite_missing_sprite: orphanAssetSpriteMissingSprite.rows[0].n,
    orphan_economy_missing_asset: orphanEconomyMissingAsset.rows[0].n,
    orphan_pool_member_missing_pool: orphanPoolMemberMissingPool.rows[0].n,
    orphan_pool_member_missing_asset: orphanPoolMemberMissingAsset.rows[0].n,
    asset_replaced_by_missing: assetReplacedByMissing.rows[0].n,
  };
}

async function gatherSlugCollisionsAcrossCategories(client: Client) {
  const r = await client.query(`
    SELECT slug, count(*)::int AS count, array_agg(category ORDER BY category) AS categories
    FROM catalog_asset
    GROUP BY slug
    HAVING count(*) > 1
    ORDER BY slug
  `);
  return r.rows;
}

async function gatherPoolVsAssetSlugConflicts(client: Client) {
  const r = await client.query(`
    SELECT
      a.slug AS slug,
      array_agg(DISTINCT a.category ORDER BY a.category) AS asset_categories,
      array_agg(DISTINCT p.owner_category ORDER BY p.owner_category) AS pool_owner_categories
    FROM catalog_asset a
    JOIN catalog_spawn_pool p ON p.slug = a.slug
    GROUP BY a.slug
    ORDER BY a.slug
  `);
  return r.rows;
}

async function gatherSpriteFingerprintDuplicates(client: Client) {
  const r = await client.query(`
    SELECT
      generator_build_fingerprint AS fingerprint,
      count(*)::int AS count,
      array_agg(id ORDER BY id) AS sprite_ids
    FROM catalog_sprite
    WHERE generator_build_fingerprint IS NOT NULL AND generator_build_fingerprint <> ''
    GROUP BY generator_build_fingerprint
    HAVING count(*) > 1
    ORDER BY generator_build_fingerprint
  `);
  return r.rows;
}

async function gatherPoolIntegrity(client: Client) {
  const zero = await client.query(`
    SELECT p.id AS pool_id, p.slug, p.owner_category, p.owner_subtype
    FROM catalog_spawn_pool p
    LEFT JOIN catalog_pool_member m ON m.pool_id = p.id
    WHERE m.pool_id IS NULL
    ORDER BY p.id
  `);
  const noSubtype = await client.query(`
    SELECT id AS pool_id, slug, owner_category
    FROM catalog_spawn_pool
    WHERE owner_subtype IS NULL OR owner_subtype = ''
    ORDER BY id
  `);
  const retiredRefs = await client.query(`
    SELECT m.pool_id, m.asset_id, a.slug AS asset_slug
    FROM catalog_pool_member m
    JOIN catalog_asset a ON a.id = m.asset_id
    WHERE a.status = 'retired'
    ORDER BY m.pool_id, m.asset_id
  `);
  return {
    pools_with_zero_members: zero.rows,
    pools_missing_owner_subtype: noSubtype.rows,
    members_referencing_retired_assets: retiredRefs.rows,
  };
}

async function gatherSlugRegexViolations(client: Client) {
  const a = await client.query(`SELECT id, slug FROM catalog_asset ORDER BY id`);
  const p = await client.query(`SELECT id, slug FROM catalog_spawn_pool ORDER BY id`);
  return {
    asset_violations: a.rows.filter((r) => !SLUG_REGEX.test(r.slug)),
    pool_violations: p.rows.filter((r) => !SLUG_REGEX.test(r.slug)),
  };
}

async function gatherZoneSSeedIds(client: Client) {
  const r = await client.query(`
    SELECT id, slug FROM catalog_asset
    WHERE category = 'zone_s' AND id BETWEEN 0 AND 6
    ORDER BY id
  `);
  return r.rows;
}

function deriveTriageIssues(p: AuditPayload): IssueList {
  const issues: IssueList = [];
  const fk = p.fk_integrity;
  if (fk.orphan_asset_sprite_missing_asset > 0)
    issues.push(`catalog_asset_sprite has ${fk.orphan_asset_sprite_missing_asset} rows pointing at missing asset`);
  if (fk.orphan_asset_sprite_missing_sprite > 0)
    issues.push(`catalog_asset_sprite has ${fk.orphan_asset_sprite_missing_sprite} rows pointing at missing sprite`);
  if (fk.orphan_economy_missing_asset > 0)
    issues.push(`catalog_economy has ${fk.orphan_economy_missing_asset} rows missing asset`);
  if (fk.orphan_pool_member_missing_pool > 0)
    issues.push(`catalog_pool_member has ${fk.orphan_pool_member_missing_pool} rows pointing at missing pool`);
  if (fk.orphan_pool_member_missing_asset > 0)
    issues.push(`catalog_pool_member has ${fk.orphan_pool_member_missing_asset} rows pointing at missing asset`);
  if (fk.asset_replaced_by_missing > 0)
    issues.push(`catalog_asset.replaced_by points at missing id in ${fk.asset_replaced_by_missing} rows`);
  if (p.slug_collisions_across_categories.length > 0)
    issues.push(`Slug collides across categories: ${p.slug_collisions_across_categories.length} slug(s). Spine collapses to (kind, slug); manual rename needed pre-migration.`);
  if (p.pool_slug_vs_asset_slug_conflicts.length > 0)
    issues.push(`Pool slug equals an asset slug in ${p.pool_slug_vs_asset_slug_conflicts.length} case(s). Spine kinds differ but verify intent.`);
  if (p.slug_regex_violations.asset_violations.length > 0)
    issues.push(`catalog_asset has ${p.slug_regex_violations.asset_violations.length} slug(s) failing spine regex /^[a-z][a-z0-9_]{2,63}$/`);
  if (p.slug_regex_violations.pool_violations.length > 0)
    issues.push(`catalog_spawn_pool has ${p.slug_regex_violations.pool_violations.length} slug(s) failing spine regex`);
  if (p.pool_integrity.pools_missing_owner_subtype.length > 0)
    issues.push(`${p.pool_integrity.pools_missing_owner_subtype.length} pool(s) missing owner_subtype — pool_detail.primary_subtype will be NULL.`);
  if (p.pool_integrity.members_referencing_retired_assets.length > 0)
    issues.push(`${p.pool_integrity.members_referencing_retired_assets.length} pool member row(s) reference retired assets — DEC-A23 hard block on retired refs requires repointing.`);
  return issues;
}

function rowsAsTable(headers: string[], rows: Array<Record<string, unknown>>): string {
  if (rows.length === 0) return '_(none)_\n';
  const head = `| ${headers.join(' | ')} |`;
  const sep = `| ${headers.map(() => '---').join(' | ')} |`;
  const body = rows
    .map((r) => `| ${headers.map((h) => formatCell(r[h])).join(' | ')} |`)
    .join('\n');
  return `${head}\n${sep}\n${body}\n`;
}

function formatCell(v: unknown): string {
  if (v === null || v === undefined) return '';
  if (Array.isArray(v)) return v.map((x) => String(x)).join(', ');
  return String(v);
}

function renderMarkdown(p: AuditPayload): string {
  const lines: string[] = [];
  lines.push(`# Catalog Pre-Spine Audit — ${p.generated_at}`);
  lines.push('');
  lines.push(`Database: \`${p.database_url_redacted}\``);
  lines.push(`Snapshot: ${p.snapshot_hint}`);
  lines.push('');
  lines.push('## 1. Row counts');
  lines.push('');
  lines.push('| table | rows |');
  lines.push('| --- | --- |');
  for (const [t, n] of Object.entries(p.row_counts)) {
    lines.push(`| ${t} | ${n === -1 ? '_(table missing)_' : n} |`);
  }
  lines.push('');
  lines.push('## 2. FK integrity');
  lines.push('');
  lines.push(`- Orphan catalog_asset_sprite (asset missing): ${p.fk_integrity.orphan_asset_sprite_missing_asset}`);
  lines.push(`- Orphan catalog_asset_sprite (sprite missing): ${p.fk_integrity.orphan_asset_sprite_missing_sprite}`);
  lines.push(`- Orphan catalog_economy (asset missing): ${p.fk_integrity.orphan_economy_missing_asset}`);
  lines.push(`- Orphan catalog_pool_member (pool missing): ${p.fk_integrity.orphan_pool_member_missing_pool}`);
  lines.push(`- Orphan catalog_pool_member (asset missing): ${p.fk_integrity.orphan_pool_member_missing_asset}`);
  lines.push(`- Asset.replaced_by pointing at missing id: ${p.fk_integrity.asset_replaced_by_missing}`);
  lines.push('');
  lines.push('## 3. Slug collisions across categories');
  lines.push('');
  lines.push('Spine collapses (category, slug) UNIQUE → (kind, slug) UNIQUE. A slug shared across categories will conflict on migration.');
  lines.push('');
  lines.push(rowsAsTable(['slug', 'count', 'categories'], p.slug_collisions_across_categories));
  lines.push('## 4. Spawn pool slugs vs asset slugs');
  lines.push('');
  lines.push('Spine separates pool / asset by `kind`, so a shared slug is technically permitted. Listed for human review.');
  lines.push('');
  lines.push(rowsAsTable(['slug', 'asset_categories', 'pool_owner_categories'], p.pool_slug_vs_asset_slug_conflicts));
  lines.push('## 5. Sprite duplicate fingerprints');
  lines.push('');
  lines.push(rowsAsTable(['fingerprint', 'count', 'sprite_ids'], p.sprite_fingerprint_duplicates));
  lines.push('## 6. Slug regex violations');
  lines.push('');
  lines.push('Spine enforces `^[a-z][a-z0-9_]{2,63}$` via CHECK. Any non-conforming legacy slug must be renamed before backfill.');
  lines.push('');
  lines.push('### Assets');
  lines.push(rowsAsTable(['id', 'slug'], p.slug_regex_violations.asset_violations));
  lines.push('### Pools');
  lines.push(rowsAsTable(['id', 'slug'], p.slug_regex_violations.pool_violations));
  lines.push('## 7. Pool integrity');
  lines.push('');
  lines.push('### Pools with zero members');
  lines.push(rowsAsTable(['pool_id', 'slug', 'owner_category', 'owner_subtype'], p.pool_integrity.pools_with_zero_members));
  lines.push('### Pools missing owner_subtype');
  lines.push(rowsAsTable(['pool_id', 'slug', 'owner_category'], p.pool_integrity.pools_missing_owner_subtype));
  lines.push('### Members referencing retired assets');
  lines.push(rowsAsTable(['pool_id', 'asset_id', 'asset_slug'], p.pool_integrity.members_referencing_retired_assets));
  lines.push('## 8. Field census (drives Stage 1 mapping)');
  lines.push('');
  lines.push('See [`docs/asset-pipeline-stage-0-1-impl.md`](../asset-pipeline-stage-0-1-impl.md) §8 — frozen reference, not regenerated per audit.');
  lines.push('');
  lines.push('## 9. Issues requiring manual triage before migration');
  lines.push('');
  if (p.triage_issues.length === 0) {
    lines.push('_(none — preconditions satisfied)_');
  } else {
    for (const issue of p.triage_issues) lines.push(`- ${issue}`);
  }
  lines.push('');
  lines.push('## 10. Zone S seed preservation gate');
  lines.push('');
  lines.push('Stage 1 backfill must keep Unity-visible ids stable for `ZoneSubTypeRegistry` (per `0013_zone_s_seed.sql`). Current rows:');
  lines.push('');
  lines.push(rowsAsTable(['id', 'slug'], p.zone_s_seed_ids));
  lines.push('Backfill writes these legacy ids to `asset_detail.legacy_asset_id`. `catalog_asset_compat` view exposes them as `id`.');
  lines.push('');
  lines.push('## Sign-off');
  lines.push('');
  lines.push('Auto-generated by `tools/scripts/audit-catalog-pre-spine.ts`. Migrations 0021/0022/0023 may proceed iff section 9 is empty OR each item carries a noted resolution in this report (commit-time edit acceptable).');
  return lines.join('\n');
}

async function main() {
  const dbUrl = resolveDatabaseUrl(REPO_ROOT);
  if (!dbUrl) {
    console.error('audit-catalog-pre-spine: no DATABASE_URL or config/postgres-dev.json');
    process.exit(1);
  }

  const date = new Date();
  const yyyy = date.getUTCFullYear();
  const mm = String(date.getUTCMonth() + 1).padStart(2, '0');
  const dd = String(date.getUTCDate()).padStart(2, '0');
  const generatedAt = `${yyyy}-${mm}-${dd}`;

  const client = new Client({ connectionString: dbUrl });
  await client.connect();

  try {
    for (const t of [
      'catalog_asset',
      'catalog_sprite',
      'catalog_asset_sprite',
      'catalog_economy',
      'catalog_spawn_pool',
      'catalog_pool_member',
    ]) {
      if (!(await tableExists(client, t))) {
        console.error(`audit-catalog-pre-spine: required table missing: ${t}`);
        process.exit(1);
      }
    }

    const payload: AuditPayload = {
      generated_at: generatedAt,
      database_url_redacted: redactUrl(dbUrl),
      snapshot_hint: `var/db-snapshots/pre-spine-${generatedAt}.dump (run db:snapshot:freeze first)`,
      row_counts: await gatherRowCounts(client),
      fk_integrity: await gatherFkIntegrity(client),
      slug_collisions_across_categories: await gatherSlugCollisionsAcrossCategories(client),
      pool_slug_vs_asset_slug_conflicts: await gatherPoolVsAssetSlugConflicts(client),
      sprite_fingerprint_duplicates: await gatherSpriteFingerprintDuplicates(client),
      pool_integrity: await gatherPoolIntegrity(client),
      slug_regex_violations: await gatherSlugRegexViolations(client),
      zone_s_seed_ids: await gatherZoneSSeedIds(client),
      triage_issues: [],
    };
    payload.triage_issues = deriveTriageIssues(payload);

    await mkdir(AUDIT_DIR, { recursive: true });
    const mdPath = join(AUDIT_DIR, `catalog-pre-spine-${generatedAt}.md`);
    const jsonPath = join(AUDIT_DIR, `catalog-pre-spine-${generatedAt}.json`);
    await writeFile(mdPath, renderMarkdown(payload), 'utf8');
    await writeFile(jsonPath, JSON.stringify(payload, null, 2) + '\n', 'utf8');

    console.log(`audit-catalog-pre-spine: wrote ${mdPath}`);
    console.log(`audit-catalog-pre-spine: wrote ${jsonPath}`);
    if (payload.triage_issues.length > 0) {
      console.log(`audit-catalog-pre-spine: ${payload.triage_issues.length} issue(s) require manual triage (see §9).`);
    } else {
      console.log('audit-catalog-pre-spine: no manual triage items detected.');
    }
  } finally {
    await client.end();
  }
}

main().catch((err) => {
  console.error('audit-catalog-pre-spine: fatal', err);
  process.exit(1);
});
