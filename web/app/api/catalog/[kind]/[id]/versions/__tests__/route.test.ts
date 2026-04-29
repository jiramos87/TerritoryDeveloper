/**
 * Generic versions list route tests (TECH-3222 / Stage 14.2).
 *
 * Covers GET /api/catalog/[kind]/[id]/versions: 200 happy path + envelope
 * shape, 400 invalid_kind / invalid_id / invalid_cursor.
 *
 * @see web/app/api/catalog/[kind]/[id]/versions/route.ts
 */
import { afterEach, beforeEach, describe, expect, test } from "vitest";
import { NextRequest } from "next/server";

import { GET } from "@/app/api/catalog/[kind]/[id]/versions/route";
import { getSql } from "@/lib/db/client";

async function resetSpine(): Promise<void> {
  const sql = getSql();
  await sql.unsafe(
    "truncate sprite_detail, asset_detail, economy_detail, pool_detail, pool_member, entity_version, catalog_entity restart identity cascade",
  );
}

async function seedEntity(kind: string, slug: string): Promise<string> {
  const sql = getSql();
  const rows = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values (${kind}, ${slug}, ${slug}, ${[]}::text[])
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return rows[0]!.id;
}

async function seedVersion(entityId: string, n: number): Promise<void> {
  const sql = getSql();
  const idNum = Number.parseInt(entityId, 10);
  await sql`
    insert into entity_version (entity_id, version_number, status)
    values (${idNum}, ${n}, 'draft')
  `;
}

async function invokeGet(
  kind: string,
  id: string,
  qs = "",
): Promise<Response> {
  const url = `http://localhost/api/catalog/${kind}/${id}/versions${qs}`;
  const req = new NextRequest(new URL(url), { method: "GET" });
  const ctx = { params: Promise.resolve({ kind, id }) };
  return GET(req, ctx);
}

beforeEach(async () => {
  await resetSpine();
}, 30000);

afterEach(async () => {
  await resetSpine();
}, 30000);

describe("GET /api/catalog/[kind]/[id]/versions (TECH-3222)", () => {
  test("200 happy path returns {ok: true, data: {rows, nextCursor}} envelope", async () => {
    const eid = await seedEntity("sprite", "route_happy");
    await seedVersion(eid, 1);
    await seedVersion(eid, 2);

    const res = await invokeGet("sprite", eid);
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { rows: unknown[]; nextCursor: string | null };
    };
    expect(body.ok).toBe(true);
    expect(Array.isArray(body.data.rows)).toBe(true);
    expect(body.data.rows.length).toBe(2);
    expect(body.data.nextCursor).toBeNull();
  });

  test("200 empty page for unknown entity id (no 404)", async () => {
    const res = await invokeGet("sprite", "999999");
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { rows: unknown[]; nextCursor: string | null };
    };
    expect(body.ok).toBe(true);
    expect(body.data.rows).toEqual([]);
    expect(body.data.nextCursor).toBeNull();
  });

  test("400 invalid_kind on bogus kind", async () => {
    const res = await invokeGet("bogus", "123");
    expect(res.status).toBe(400);
    const body = (await res.json()) as { code: string; error: string };
    expect(body.code).toBe("bad_request");
    expect(body.error).toMatch(/kind/i);
  });

  test("400 invalid_id on non-numeric id", async () => {
    const res = await invokeGet("sprite", "abc");
    expect(res.status).toBe(400);
    const body = (await res.json()) as { code: string; error: string };
    expect(body.code).toBe("bad_request");
    expect(body.error).toMatch(/id/i);
  });

  test("400 invalid_cursor on malformed cursor", async () => {
    const eid = await seedEntity("sprite", "route_bad_cursor");
    await seedVersion(eid, 1);
    const res = await invokeGet("sprite", eid, "?cursor=garbage$$$");
    expect(res.status).toBe(400);
    const body = (await res.json()) as { code: string; error: string };
    expect(body.code).toBe("bad_request");
    expect(body.error).toMatch(/cursor/i);
  });

  test("cursor pagination — page 1 nextCursor consumes page 2", async () => {
    const eid = await seedEntity("sprite", "route_pagination");
    await seedVersion(eid, 1);
    await seedVersion(eid, 2);
    await seedVersion(eid, 3);

    const r1 = await invokeGet("sprite", eid, "?limit=2");
    const b1 = (await r1.json()) as {
      ok: boolean;
      data: { rows: Array<{ id: string }>; nextCursor: string | null };
    };
    expect(b1.data.rows.length).toBe(2);
    expect(b1.data.nextCursor).not.toBeNull();

    const r2 = await invokeGet(
      "sprite",
      eid,
      `?limit=2&cursor=${encodeURIComponent(b1.data.nextCursor!)}`,
    );
    const b2 = (await r2.json()) as {
      ok: boolean;
      data: { rows: Array<{ id: string }>; nextCursor: string | null };
    };
    expect(b2.data.rows.length).toBe(1);
    expect(b2.data.nextCursor).toBeNull();

    const allIds = [...b1.data.rows.map((r) => r.id), ...b2.data.rows.map((r) => r.id)];
    const uniqueIds = new Set(allIds);
    expect(uniqueIds.size).toBe(3);
  });
});
