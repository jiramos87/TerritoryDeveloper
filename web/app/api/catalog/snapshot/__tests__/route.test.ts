/**
 * Snapshot route — POST + GET integration tests (TECH-2674 §Test Blueprint).
 *
 *   POST: invokes handler → asserts returned hash equals sha256 of disk
 *         `manifest.json`; asserts a `catalog_snapshot` row was inserted.
 *   GET:  seeds N rows + asserts cursor pagination round-trips (page 1
 *         returns `limit` items + `nextCursor`; final page returns null).
 *
 * `parseListQuery` pure-helper coverage included.
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2674 §Plan Digest
 */

import { mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import path from "node:path";

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

import { GET, POST, parseListQuery } from "@/app/api/catalog/snapshot/route";
import { getSql } from "@/lib/db/client";
import { SNAPSHOT_DIR_RELATIVE } from "@/lib/snapshot/export";

const TEST_USER_ID = "33333333-3333-4333-8333-333333333333";

let tmpRoot: string;

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: async () => ({
    id: TEST_USER_ID,
    email: "snapshot-route@example.com",
    role: "admin",
  }),
}));

vi.mock("@/lib/snapshot/export", async (importOriginal) => {
  const orig = await importOriginal<typeof import("@/lib/snapshot/export")>();
  return {
    ...orig,
    exportSnapshot: async (
      authorUserId: string,
      options?: { includeDrafts?: boolean },
    ) => {
      // Wrap real exporter but pin output to per-test tmpRoot so the tree
      // stays clean. `tmpRoot` is set in beforeEach.
      return orig.exportSnapshot(authorUserId, {
        ...(options ?? {}),
        outputRootOverride: tmpRoot,
        nowOverride: () => new Date("2026-01-01T00:00:00.000Z"),
      });
    },
  };
});

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
    values (${TEST_USER_ID}::uuid, 'snapshot-route@example.com', 'snapshot route', 'admin')
    on conflict (id) do nothing
  `;
}

async function seedMinimalFixture(): Promise<void> {
  const sql = getSql();
  // One published sprite suffices for exporter to produce a valid manifest.
  const [{ id: spriteEntityId }] = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values ('sprite', 'snap_post_01', 'Snap Post 01', '{snap}')
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  await sql`
    insert into entity_version (entity_id, version_number, status, params_json)
    values (${spriteEntityId}::bigint, 1, 'published', '{}'::jsonb)
  `;
  await sql`
    insert into sprite_detail (entity_id, source_uri, pixels_per_unit, pivot_x, pivot_y, provenance)
    values (${spriteEntityId}::bigint, 'snap/post_01', 64, 0.5, 0.5, 'hand')
  `;
}

async function seedSnapshotRows(count: number): Promise<void> {
  const sql = getSql();
  // Seed `count` rows with monotonically increasing created_at (1 second apart)
  // so cursor pagination has deterministic ordering.
  for (let i = 0; i < count; i++) {
    const ts = new Date(2026, 0, 1, 0, 0, i).toISOString();
    await sql`
      insert into catalog_snapshot (hash, manifest_path, entity_counts_json, schema_version, status, created_by, created_at)
      values (
        ${`hash_${i.toString().padStart(2, "0")}`},
        'Assets/StreamingAssets/catalog/manifest.json',
        '{}'::jsonb,
        2,
        'active',
        ${TEST_USER_ID}::uuid,
        ${ts}::timestamptz
      )
    `;
  }
}

beforeEach(async () => {
  tmpRoot = mkdtempSync(path.join(tmpdir(), "snapshot-route-"));
  await seedUser();
  await reset();
}, 30000);

afterEach(() => {
  rmSync(tmpRoot, { recursive: true, force: true });
}, 30000);

describe("POST /api/catalog/snapshot (TECH-2674)", () => {
  test("returns hash matching manifest.json on disk + inserts catalog_snapshot row", async () => {
    await seedMinimalFixture();

    const req = new Request("http://localhost/api/catalog/snapshot", {
      method: "POST",
    });
    const res = await POST(req as unknown as Parameters<typeof POST>[0]);
    expect(res.status).toBe(200);

    const body = (await res.json()) as {
      ok: true;
      data: { snapshot_id: string; hash: string; manifest_path: string };
    };
    expect(body.ok).toBe(true);
    expect(body.data.hash).toMatch(/^[0-9a-f]{64}$/);

    // Read manifest.json from tmpRoot and assert snapshotHash matches.
    const manifestRaw = readFileSync(
      path.join(tmpRoot, SNAPSHOT_DIR_RELATIVE, "manifest.json"),
      "utf8",
    );
    const manifest = JSON.parse(manifestRaw) as { snapshotHash: string };
    expect(manifest.snapshotHash).toBe(body.data.hash);

    // Assert one catalog_snapshot row inserted with that hash.
    const sql = getSql();
    const rows = (await sql`
      select id::text as id, hash, status::text as status
      from catalog_snapshot
    `) as unknown as Array<{ id: string; hash: string; status: string }>;
    expect(rows.length).toBe(1);
    expect(rows[0]!.hash).toBe(body.data.hash);
    expect(rows[0]!.status).toBe("active");
    expect(rows[0]!.id).toBe(body.data.snapshot_id);
  });
});

describe("GET /api/catalog/snapshot (TECH-2674)", () => {
  test("first page returns `limit` items + nextCursor; final page returns null", async () => {
    await seedSnapshotRows(5);

    // Page 1: limit=3 → 3 items + nextCursor.
    const req1 = new Request(
      "http://localhost/api/catalog/snapshot?limit=3",
      { method: "GET" },
    );
    const res1 = await GET(req1 as unknown as Parameters<typeof GET>[0]);
    expect(res1.status).toBe(200);
    const body1 = (await res1.json()) as {
      ok: true;
      data: {
        items: Array<{ hash: string; created_at: string }>;
        nextCursor: string | null;
      };
    };
    expect(body1.data.items.length).toBe(3);
    expect(body1.data.nextCursor).not.toBeNull();
    // DESC ordering — newest seeded row first (hash_04).
    expect(body1.data.items[0]!.hash).toBe("hash_04");
    expect(body1.data.items[2]!.hash).toBe("hash_02");

    // Page 2: pass cursor → 2 items + nextCursor=null.
    const cursor = encodeURIComponent(body1.data.nextCursor!);
    const req2 = new Request(
      `http://localhost/api/catalog/snapshot?limit=3&cursor=${cursor}`,
      { method: "GET" },
    );
    const res2 = await GET(req2 as unknown as Parameters<typeof GET>[0]);
    const body2 = (await res2.json()) as {
      ok: true;
      data: {
        items: Array<{ hash: string }>;
        nextCursor: string | null;
      };
    };
    expect(body2.data.items.length).toBe(2);
    expect(body2.data.items[0]!.hash).toBe("hash_01");
    expect(body2.data.items[1]!.hash).toBe("hash_00");
    expect(body2.data.nextCursor).toBeNull();
  });

  test("empty table → empty items + null cursor", async () => {
    const req = new Request("http://localhost/api/catalog/snapshot", {
      method: "GET",
    });
    const res = await GET(req as unknown as Parameters<typeof GET>[0]);
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: true;
      data: { items: unknown[]; nextCursor: string | null };
    };
    expect(body.data.items.length).toBe(0);
    expect(body.data.nextCursor).toBeNull();
  });

  test("rejects invalid limit", async () => {
    const req = new Request(
      "http://localhost/api/catalog/snapshot?limit=999",
      { method: "GET" },
    );
    const res = await GET(req as unknown as Parameters<typeof GET>[0]);
    expect(res.status).toBe(400);
  });
});

describe("parseListQuery — pure helper", () => {
  test("defaults when both params missing", () => {
    const r = parseListQuery(null, null);
    expect(r.ok).toBe(true);
    if (r.ok) {
      expect(r.limit).toBe(20);
      expect(r.cursor).toBeNull();
    }
  });

  test("parses cursor as Date", () => {
    const r = parseListQuery(null, "2026-01-01T00:00:00.000Z");
    expect(r.ok).toBe(true);
    if (r.ok && r.cursor !== null) {
      expect(r.cursor.toISOString()).toBe("2026-01-01T00:00:00.000Z");
    }
  });

  test("rejects non-numeric limit", () => {
    const r = parseListQuery("abc", null);
    expect(r.ok).toBe(false);
  });

  test("rejects out-of-range limit", () => {
    const r = parseListQuery("9999", null);
    expect(r.ok).toBe(false);
  });

  test("rejects malformed cursor", () => {
    const r = parseListQuery(null, "not-a-date");
    expect(r.ok).toBe(false);
  });
});
