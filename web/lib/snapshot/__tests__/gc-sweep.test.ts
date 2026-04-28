/**
 * sweepRetiredSnapshots — coverage for TECH-2676 §Test Blueprint.
 *
 *   1. Age threshold — 5-day retired row stays, 8-day retired row removed.
 *   2. Idempotency — second sweep returns `{ removedCount: 0 }` and performs
 *      no DB or disk mutation.
 *   3. Active-twin guard — when an active row shares the retired row's
 *      `manifest_path`, the DB row is removed but the disk files survive.
 *   4. ENOENT swallow — retired row whose disk file is already missing
 *      sweeps cleanly; no thrown error.
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2676 §Plan Digest
 */

import {
  existsSync,
  mkdirSync,
  mkdtempSync,
  rmSync,
  writeFileSync,
} from "node:fs";
import { tmpdir } from "node:os";
import path from "node:path";

import {
  afterEach,
  beforeEach,
  describe,
  expect,
  test,
} from "vitest";

import { getSql } from "@/lib/db/client";
import { sweepRetiredSnapshots } from "@/lib/snapshot/gc-sweep";
import { SNAPSHOT_KINDS } from "@/lib/snapshot/manifest";

const TEST_USER_ID = "33333333-3333-4333-8333-333333333333";
const NOW = new Date("2026-04-28T00:00:00.000Z");
const FIVE_DAYS_AGO = new Date(NOW.getTime() - 5 * 24 * 60 * 60 * 1000);
const EIGHT_DAYS_AGO = new Date(NOW.getTime() - 8 * 24 * 60 * 60 * 1000);
const MANIFEST_RELATIVE = "Assets/StreamingAssets/catalog/manifest.json";

let tmpRoot: string;
let catalogDir: string;

async function reset(): Promise<void> {
  const sql = getSql();
  await sql.unsafe(
    "truncate sprite_detail, asset_detail, button_detail, panel_detail, audio_detail, pool_detail, token_detail, panel_child, pool_member, entity_version, catalog_entity, catalog_snapshot restart identity cascade",
  );
}

async function seedUser(): Promise<void> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (${TEST_USER_ID}::uuid, 'gc-sweep-tests@example.com', 'gc sweep tests', 'admin')
    on conflict (id) do nothing
  `;
}

async function insertSnapshotRow(
  hash: string,
  status: "active" | "retired",
  retiredAt: Date | null,
  createdAt: Date,
): Promise<string> {
  const sql = getSql();
  const inserted = (await sql`
    insert into catalog_snapshot (hash, manifest_path, entity_counts_json, schema_version, status, created_by, created_at, retired_at)
    values (
      ${hash},
      ${MANIFEST_RELATIVE},
      '{}'::jsonb,
      2,
      ${status},
      ${TEST_USER_ID}::uuid,
      ${createdAt.toISOString()}::timestamptz,
      ${retiredAt ? retiredAt.toISOString() : null}
    )
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return inserted[0]!.id;
}

function writeFakeSnapshotDisk(): void {
  // Write per-kind + manifest stubs so the sweep has files to delete.
  mkdirSync(catalogDir, { recursive: true });
  for (const kind of SNAPSHOT_KINDS) {
    writeFileSync(path.join(catalogDir, `${kind}.json`), `{"kind":"${kind}"}`);
  }
  writeFileSync(path.join(catalogDir, "manifest.json"), "{}");
}

function diskHasManifestAndKinds(): boolean {
  if (!existsSync(path.join(catalogDir, "manifest.json"))) return false;
  for (const kind of SNAPSHOT_KINDS) {
    if (!existsSync(path.join(catalogDir, `${kind}.json`))) return false;
  }
  return true;
}

function diskHasNothing(): boolean {
  if (existsSync(path.join(catalogDir, "manifest.json"))) return false;
  for (const kind of SNAPSHOT_KINDS) {
    if (existsSync(path.join(catalogDir, `${kind}.json`))) return false;
  }
  return true;
}

beforeEach(async () => {
  tmpRoot = mkdtempSync(path.join(tmpdir(), "gc-sweep-"));
  catalogDir = path.join(tmpRoot, "Assets/StreamingAssets/catalog");
  await seedUser();
  await reset();
}, 30000);

afterEach(() => {
  rmSync(tmpRoot, { recursive: true, force: true });
}, 30000);

describe("sweepRetiredSnapshots (TECH-2676)", () => {
  test("removes 8-day retired row + keeps 5-day retired row", async () => {
    const eightDayId = await insertSnapshotRow(
      "hash_8day",
      "retired",
      EIGHT_DAYS_AGO,
      EIGHT_DAYS_AGO,
    );
    const fiveDayId = await insertSnapshotRow(
      "hash_5day",
      "retired",
      FIVE_DAYS_AGO,
      FIVE_DAYS_AGO,
    );
    writeFakeSnapshotDisk();

    const result = await sweepRetiredSnapshots(NOW, 7, {
      repoRootOverride: tmpRoot,
    });

    expect(result.removedCount).toBe(1);
    expect(result.removedIds).toEqual([eightDayId]);

    const sql = getSql();
    const remaining = (await sql`
      select id::text as id from catalog_snapshot order by created_at asc
    `) as unknown as Array<{ id: string }>;
    expect(remaining.map((r) => r.id)).toEqual([fiveDayId]);

    // 8-day was the sole retired row → no active twin → disk wiped.
    expect(diskHasNothing()).toBe(true);
  });

  test("idempotent: second sweep is a no-op", async () => {
    await insertSnapshotRow(
      "hash_8day",
      "retired",
      EIGHT_DAYS_AGO,
      EIGHT_DAYS_AGO,
    );
    writeFakeSnapshotDisk();

    const first = await sweepRetiredSnapshots(NOW, 7, {
      repoRootOverride: tmpRoot,
    });
    expect(first.removedCount).toBe(1);

    const second = await sweepRetiredSnapshots(NOW, 7, {
      repoRootOverride: tmpRoot,
    });
    expect(second.removedCount).toBe(0);
    expect(second.removedIds).toEqual([]);

    const sql = getSql();
    const count = (await sql`
      select count(*)::int as n from catalog_snapshot
    `) as unknown as Array<{ n: number }>;
    expect(count[0]!.n).toBe(0);
  });

  test("active-twin guard: disk kept when active row shares manifest_path", async () => {
    const eightDayId = await insertSnapshotRow(
      "hash_8day",
      "retired",
      EIGHT_DAYS_AGO,
      EIGHT_DAYS_AGO,
    );
    const activeId = await insertSnapshotRow(
      "hash_active",
      "active",
      null,
      NOW,
    );
    writeFakeSnapshotDisk();

    const result = await sweepRetiredSnapshots(NOW, 7, {
      repoRootOverride: tmpRoot,
    });

    expect(result.removedCount).toBe(1);
    expect(result.removedIds).toEqual([eightDayId]);

    const sql = getSql();
    const remaining = (await sql`
      select id::text as id from catalog_snapshot
    `) as unknown as Array<{ id: string }>;
    expect(remaining.map((r) => r.id)).toEqual([activeId]);

    // Active twin → disk untouched.
    expect(diskHasManifestAndKinds()).toBe(true);
  });

  test("ENOENT swallow: missing disk files do not throw", async () => {
    const eightDayId = await insertSnapshotRow(
      "hash_8day",
      "retired",
      EIGHT_DAYS_AGO,
      EIGHT_DAYS_AGO,
    );
    // Intentionally do NOT write disk — simulate prior cleanup.

    const result = await sweepRetiredSnapshots(NOW, 7, {
      repoRootOverride: tmpRoot,
    });

    expect(result.removedCount).toBe(1);
    expect(result.removedIds).toEqual([eightDayId]);

    const sql = getSql();
    const count = (await sql`
      select count(*)::int as n from catalog_snapshot
    `) as unknown as Array<{ n: number }>;
    expect(count[0]!.n).toBe(0);
  });
});
