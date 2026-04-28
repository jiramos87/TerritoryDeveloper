/**
 * Golden test for `exportSnapshot` (TECH-2673 §Acceptance #6).
 *
 * Seeds 8 catalog kinds × 1 published entity_version each, runs
 * `exportSnapshot` twice over identical DB state, asserts:
 *   1. Per-kind file bytes byte-identical between runs.
 *   2. Manifest hash identical between runs.
 *   3. Both runs insert a `catalog_snapshot` row with the same `hash`
 *      (idempotent re-insert; files overwritten in place).
 *
 * Plus an `includeDrafts` toggle case proving drafts are excluded by default.
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2673 §Plan Digest
 */

import { mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import path from "node:path";
import { afterEach, beforeEach, describe, expect, test } from "vitest";

import { getSql } from "@/lib/db/client";
import {
  exportSnapshot,
  MANIFEST_RELATIVE_PATH,
  SNAPSHOT_DIR_RELATIVE,
} from "@/lib/snapshot/export";
import { SNAPSHOT_KINDS } from "@/lib/snapshot/manifest";

const TEST_USER_ID = "33333333-3333-4333-8333-333333333333";
const FROZEN_NOW = () => new Date("2026-01-01T00:00:00.000Z");

let tmpRoot: string;

async function reset(): Promise<void> {
  const sql = getSql();
  // Truncate per-kind detail + entity tables. CASCADE cleans entity_version + details.
  await sql.unsafe(
    "truncate sprite_detail, asset_detail, button_detail, panel_detail, audio_detail, pool_detail, token_detail, panel_child, pool_member, entity_version, catalog_entity, catalog_snapshot restart identity cascade",
  );
}

async function seedUser(): Promise<void> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (${TEST_USER_ID}::uuid, 'snapshot-tests@example.com', 'snapshot tests', 'admin')
    on conflict (id) do nothing
  `;
}

async function seedFixture(): Promise<void> {
  const sql = getSql();
  // Insert one entity + one published version per kind, plus per-kind detail row.
  // Slugs picked deterministically; values chosen to round-trip canonical bytes.

  // sprite
  const [{ id: spriteEntityId }] = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('sprite', 'snap_sprite_01', 'Snap Sprite 01', '{snap}')
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const [{ id: spriteVerId }] = (await sql`
    insert into entity_version (entity_id, version_number, status, params_json)
    values (${spriteEntityId}::bigint, 1, 'published', '{"a":1}'::jsonb)
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  await sql`
    insert into sprite_detail (entity_id, source_uri, pixels_per_unit, pivot_x, pivot_y, provenance)
    values (${spriteEntityId}::bigint, 'snap/sprite_01', 64, 0.5, 0.5, 'hand')
  `;
  await sql`
    update catalog_entity set current_published_version_id = ${spriteVerId}::bigint
    where id = ${spriteEntityId}::bigint
  `;

  // pool — used as primary_subtype_pool_id by the asset row.
  const [{ id: poolEntityId }] = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('pool', 'snap_pool_01', 'Snap Pool 01', '{snap}')
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  await sql`
    insert into entity_version (entity_id, version_number, status, params_json)
    values (${poolEntityId}::bigint, 1, 'published', '{}'::jsonb)
  `;
  await sql`
    insert into pool_detail (entity_id, primary_subtype, owner_category)
    values (${poolEntityId}::bigint, 'crop', 'building')
  `;

  // asset
  const [{ id: assetEntityId }] = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('asset', 'snap_asset_01', 'Snap Asset 01', '{snap}')
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  await sql`
    insert into entity_version (entity_id, version_number, status, params_json)
    values (${assetEntityId}::bigint, 1, 'published', '{}'::jsonb)
  `;
  await sql`
    insert into asset_detail (entity_id, category, footprint_w, footprint_h, has_button, world_sprite_entity_id, primary_subtype_pool_id)
    values (${assetEntityId}::bigint, 'building', 1, 1, true, ${spriteEntityId}::bigint, ${poolEntityId}::bigint)
  `;

  // button
  const [{ id: buttonEntityId }] = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('button', 'snap_button_01', 'Snap Button 01', '{snap}')
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  await sql`
    insert into entity_version (entity_id, version_number, status, params_json)
    values (${buttonEntityId}::bigint, 1, 'published', '{}'::jsonb)
  `;
  await sql`
    insert into button_detail (entity_id, sprite_idle_entity_id, size_variant, action_id)
    values (${buttonEntityId}::bigint, ${spriteEntityId}::bigint, 'md', 'snap.click')
  `;

  // panel
  const [{ id: panelEntityId }] = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('panel', 'snap_panel_01', 'Snap Panel 01', '{snap}')
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  await sql`
    insert into entity_version (entity_id, version_number, status, params_json)
    values (${panelEntityId}::bigint, 1, 'published', '{}'::jsonb)
  `;
  await sql`
    insert into panel_detail (entity_id, layout_template, modal)
    values (${panelEntityId}::bigint, 'vstack', false)
  `;

  // audio
  const [{ id: audioEntityId }] = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('audio', 'snap_audio_01', 'Snap Audio 01', '{snap}')
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  await sql`
    insert into entity_version (entity_id, version_number, status, params_json)
    values (${audioEntityId}::bigint, 1, 'published', '{}'::jsonb)
  `;
  await sql`
    insert into audio_detail (entity_id, source_uri, duration_ms, sample_rate, channels, fingerprint)
    values (${audioEntityId}::bigint, 'snap/audio_01', 1500, 44100, 2, 'sha256:snap_audio')
  `;

  // token
  const [{ id: tokenEntityId }] = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('token', 'snap_token_01', 'Snap Token 01', '{snap}')
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  await sql`
    insert into entity_version (entity_id, version_number, status, params_json)
    values (${tokenEntityId}::bigint, 1, 'published', '{}'::jsonb)
  `;
  await sql`
    insert into token_detail (entity_id, token_kind, value_json)
    values (${tokenEntityId}::bigint, 'color', '{"hex":"#ff00aa"}'::jsonb)
  `;

  // archetype (no detail table)
  const [{ id: archetypeEntityId }] = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('archetype', 'snap_archetype_01', 'Snap Archetype 01', '{snap}')
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  await sql`
    insert into entity_version (entity_id, version_number, status, params_json)
    values (${archetypeEntityId}::bigint, 1, 'published', '{"slot_count":3}'::jsonb)
  `;
}

async function seedDraftSprite(): Promise<void> {
  const sql = getSql();
  const [{ id: entityId }] = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('sprite', 'snap_sprite_draft', 'Snap Sprite Draft', '{snap}')
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  await sql`
    insert into entity_version (entity_id, version_number, status, params_json)
    values (${entityId}::bigint, 1, 'draft', '{}'::jsonb)
  `;
  await sql`
    insert into sprite_detail (entity_id, source_uri, pixels_per_unit, pivot_x, pivot_y, provenance)
    values (${entityId}::bigint, 'snap/sprite_draft', 64, 0.5, 0.5, 'hand')
  `;
}

beforeEach(async () => {
  tmpRoot = mkdtempSync(path.join(tmpdir(), "snapshot-export-"));
  await seedUser();
  await reset();
  await seedFixture();
}, 30000);

afterEach(() => {
  rmSync(tmpRoot, { recursive: true, force: true });
}, 30000);

describe("exportSnapshot — golden two-run determinism (TECH-2673)", () => {
  test("two runs over identical DB → byte-identical files + identical hash", async () => {
    const first = await exportSnapshot(TEST_USER_ID, {
      outputRootOverride: tmpRoot,
      nowOverride: FROZEN_NOW,
    });

    // Snapshot per-kind bytes after run 1.
    const outDir1 = path.join(tmpRoot, SNAPSHOT_DIR_RELATIVE);
    const bytes1: Record<string, Buffer> = {};
    for (const kind of SNAPSHOT_KINDS) {
      bytes1[kind] = readFileSync(path.join(outDir1, `${kind}.json`));
    }
    const manifestBytes1 = readFileSync(path.join(outDir1, "manifest.json"));

    const second = await exportSnapshot(TEST_USER_ID, {
      outputRootOverride: tmpRoot,
      nowOverride: FROZEN_NOW,
    });

    const outDir2 = path.join(tmpRoot, SNAPSHOT_DIR_RELATIVE);
    for (const kind of SNAPSHOT_KINDS) {
      const bytes2 = readFileSync(path.join(outDir2, `${kind}.json`));
      expect(bytes2.equals(bytes1[kind]!)).toBe(true);
    }
    const manifestBytes2 = readFileSync(path.join(outDir2, "manifest.json"));
    expect(manifestBytes2.equals(manifestBytes1)).toBe(true);

    expect(second.hash).toBe(first.hash);
    expect(second.snapshotId).not.toBe(first.snapshotId);

    // Two distinct catalog_snapshot rows, both with same hash.
    const sql = getSql();
    const rows = (await sql`
      select hash, status::text from catalog_snapshot order by created_at
    `) as unknown as Array<{ hash: string; status: string }>;
    expect(rows.length).toBe(2);
    expect(rows[0]!.hash).toBe(first.hash);
    expect(rows[1]!.hash).toBe(first.hash);
    expect(rows[0]!.status).toBe("active");
    expect(rows[1]!.status).toBe("active");

    expect(first.manifestPath).toBe(MANIFEST_RELATIVE_PATH);
  });

  test("includeDrafts=false (default) excludes draft entity_version rows", async () => {
    await seedDraftSprite();

    const out = await exportSnapshot(TEST_USER_ID, {
      outputRootOverride: tmpRoot,
      nowOverride: FROZEN_NOW,
    });

    const outDir = path.join(tmpRoot, SNAPSHOT_DIR_RELATIVE);
    const spriteRaw = readFileSync(
      path.join(outDir, "sprite.json"),
      "utf8",
    );
    expect(spriteRaw).not.toContain("snap_sprite_draft");
    expect(spriteRaw).toContain("snap_sprite_01");
    expect(out.hash).toMatch(/^[0-9a-f]{64}$/);
  });

  test("includeDrafts=true folds draft + published rows", async () => {
    await seedDraftSprite();

    await exportSnapshot(TEST_USER_ID, {
      outputRootOverride: tmpRoot,
      nowOverride: FROZEN_NOW,
      includeDrafts: true,
    });

    const outDir = path.join(tmpRoot, SNAPSHOT_DIR_RELATIVE);
    const spriteRaw = readFileSync(
      path.join(outDir, "sprite.json"),
      "utf8",
    );
    expect(spriteRaw).toContain("snap_sprite_draft");
    expect(spriteRaw).toContain("snap_sprite_01");
  });

  test("manifest carries schemaVersion=2 + entityCounts for 8 kinds", async () => {
    await exportSnapshot(TEST_USER_ID, {
      outputRootOverride: tmpRoot,
      nowOverride: FROZEN_NOW,
    });
    const manifestRaw = readFileSync(
      path.join(tmpRoot, SNAPSHOT_DIR_RELATIVE, "manifest.json"),
      "utf8",
    );
    const manifest = JSON.parse(manifestRaw) as {
      schemaVersion: number;
      generatedAt: string;
      snapshotHash: string;
      entityCounts: Record<string, number>;
    };
    expect(manifest.schemaVersion).toBe(2);
    expect(manifest.snapshotHash).toMatch(/^[0-9a-f]{64}$/);
    for (const kind of SNAPSHOT_KINDS) {
      expect(manifest.entityCounts[kind]).toBe(1);
    }
  });
});
