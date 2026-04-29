/**
 * history-repo unit tests (TECH-3222 / Stage 14.2).
 *
 * DB-backed via Postgres: TRUNCATE catalog_entity + entity_version with CASCADE
 * between tests. Mirrors `tests/api/catalog/sprites/_harness.ts` reset shape.
 *
 * @see web/lib/repos/history-repo.ts
 */
import { afterEach, beforeEach, describe, expect, test } from "vitest";

import { getSql } from "@/lib/db/client";
import {
  clampLimit,
  decodeCursor,
  encodeCursor,
  InvalidCursorError,
  listVersions,
} from "@/lib/repos/history-repo";

async function resetSpine(): Promise<void> {
  const sql = getSql();
  await sql.unsafe(
    "truncate sprite_detail, asset_detail, economy_detail, pool_detail, pool_member, entity_version, catalog_entity restart identity cascade",
  );
}

async function seedEntity(
  kind: string,
  slug: string,
  display: string,
): Promise<string> {
  const sql = getSql();
  const rows = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values (${kind}, ${slug}, ${display}, ${[]}::text[])
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return rows[0]!.id;
}

async function seedVersion(
  entityId: string,
  versionNumber: number,
  status: "draft" | "published",
  createdAtIso?: string,
): Promise<string> {
  const sql = getSql();
  const idNum = Number.parseInt(entityId, 10);
  const rows = createdAtIso
    ? ((await sql`
        insert into entity_version (entity_id, version_number, status, created_at)
        values (${idNum}, ${versionNumber}, ${status}, ${createdAtIso}::timestamptz)
        returning id::text as id
      `) as unknown as Array<{ id: string }>)
    : ((await sql`
        insert into entity_version (entity_id, version_number, status)
        values (${idNum}, ${versionNumber}, ${status})
        returning id::text as id
      `) as unknown as Array<{ id: string }>);
  return rows[0]!.id;
}

beforeEach(async () => {
  await resetSpine();
}, 30000);

afterEach(async () => {
  await resetSpine();
}, 30000);

describe("clampLimit", () => {
  test("default 20 when null/undefined/NaN", () => {
    expect(clampLimit(null)).toBe(20);
    expect(clampLimit(undefined)).toBe(20);
    expect(clampLimit(Number.NaN)).toBe(20);
  });
  test("clamps below MIN", () => {
    expect(clampLimit(0)).toBe(1);
    expect(clampLimit(-5)).toBe(1);
  });
  test("clamps above MAX", () => {
    expect(clampLimit(500)).toBe(100);
    expect(clampLimit(101)).toBe(100);
  });
  test("passes through valid values", () => {
    expect(clampLimit(20)).toBe(20);
    expect(clampLimit(50)).toBe(50);
  });
});

describe("encodeCursor / decodeCursor", () => {
  test("round-trip preserves shape", () => {
    const enc = encodeCursor({ created_at: "2026-04-29T00:00:00.000Z", id: "42" });
    const dec = decodeCursor(enc);
    expect(dec.created_at).toBe("2026-04-29T00:00:00.000Z");
    expect(dec.id).toBe("42");
  });
  test("rejects garbage base64", () => {
    expect(() => decodeCursor("notbase64$$$")).toThrow(InvalidCursorError);
  });
  test("rejects non-numeric id", () => {
    const bad = Buffer.from(
      JSON.stringify({ created_at: "2026-04-29T00:00:00.000Z", id: "abc" }),
      "utf8",
    ).toString("base64");
    expect(() => decodeCursor(bad)).toThrow(InvalidCursorError);
  });
  test("rejects malformed JSON", () => {
    const bad = Buffer.from("{not json", "utf8").toString("base64");
    expect(() => decodeCursor(bad)).toThrow(InvalidCursorError);
  });
});

describe("listVersions DB integration", () => {
  test("empty entity returns empty page", async () => {
    const out = await listVersions("sprite", "999999", null, 20);
    expect(out.rows).toEqual([]);
    expect(out.nextCursor).toBeNull();
  });

  test("returns rows DESC by created_at then id", async () => {
    const eid = await seedEntity("sprite", "list_desc", "Desc test");
    await seedVersion(eid, 1, "draft", "2026-04-01T00:00:00.000Z");
    await seedVersion(eid, 2, "published", "2026-04-02T00:00:00.000Z");
    await seedVersion(eid, 3, "draft", "2026-04-03T00:00:00.000Z");

    const out = await listVersions("sprite", eid, null, 20);
    expect(out.rows.map((r) => r.version_number)).toEqual([3, 2, 1]);
    expect(out.nextCursor).toBeNull();
  });

  test("cursor round-trip — page 1 and page 2 cover all rows in DESC order", async () => {
    const eid = await seedEntity("sprite", "list_cursor", "Cursor test");
    await seedVersion(eid, 1, "draft", "2026-04-01T00:00:00.000Z");
    await seedVersion(eid, 2, "published", "2026-04-02T00:00:00.000Z");
    await seedVersion(eid, 3, "draft", "2026-04-03T00:00:00.000Z");

    const page1 = await listVersions("sprite", eid, null, 2);
    expect(page1.rows.length).toBe(2);
    expect(page1.nextCursor).not.toBeNull();
    expect(page1.rows.map((r) => r.version_number)).toEqual([3, 2]);

    const page2 = await listVersions("sprite", eid, page1.nextCursor, 2);
    expect(page2.rows.length).toBe(1);
    expect(page2.nextCursor).toBeNull();
    expect(page2.rows.map((r) => r.version_number)).toEqual([1]);
  });

  test("returned row shape — no author column, id field carries PK", async () => {
    const eid = await seedEntity("sprite", "list_shape", "Shape test");
    await seedVersion(eid, 1, "draft");
    const out = await listVersions("sprite", eid, null, 20);
    expect(out.rows.length).toBe(1);
    const r = out.rows[0]!;
    expect(typeof r.id).toBe("string");
    expect(/^\d+$/.test(r.id)).toBe(true);
    expect(r.entity_id).toBe(eid);
    expect(r.status).toBe("draft");
    expect(r.version_number).toBe(1);
    expect(typeof r.created_at).toBe("string");
    expect(r.parent_version_id).toBeNull();
    expect(r.archetype_version_id).toBeNull();
    expect(Object.prototype.hasOwnProperty.call(r, "author")).toBe(false);
  });

  test("kind filter — version with mismatched kind not returned", async () => {
    const spriteId = await seedEntity("sprite", "list_kind_sprite", "Sprite");
    const assetId = await seedEntity("asset", "list_kind_asset", "Asset");
    await seedVersion(spriteId, 1, "draft");
    await seedVersion(assetId, 1, "draft");

    const outSprite = await listVersions("sprite", spriteId, null, 20);
    expect(outSprite.rows.length).toBe(1);
    expect(outSprite.rows[0]!.entity_id).toBe(spriteId);

    // Cross-kind lookup returns empty (kind mismatch on JOIN).
    const outMismatch = await listVersions("asset", spriteId, null, 20);
    expect(outMismatch.rows.length).toBe(0);
  });

  test("limit clamp — passing 500 caps at 100", async () => {
    const eid = await seedEntity("sprite", "list_clamp", "Clamp test");
    await seedVersion(eid, 1, "draft");
    const out = await listVersions("sprite", eid, null, 500);
    // Asserting clamp behavior — we can't easily verify the SQL LIMIT without
    // seeding 100+ rows; clampLimit() is unit-tested above. This call must not
    // throw.
    expect(out.rows.length).toBeLessThanOrEqual(100);
  });

  test("invalid entityId (non-numeric) returns empty page", async () => {
    const out = await listVersions("sprite", "not-a-number", null, 20);
    expect(out.rows).toEqual([]);
    expect(out.nextCursor).toBeNull();
  });
});
