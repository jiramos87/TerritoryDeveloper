/**
 * Single-version diff route tests (TECH-3301 / Stage 14.3).
 *
 * Covers happy-path (target+parent), root-version (no parent → from=null),
 * 404 missing target, 400 bad-kind, 400 bad-versionId.
 *
 * @see web/app/api/catalog/[kind]/[id]/diff/[versionId]/route.ts
 */
import { afterEach, beforeEach, describe, expect, test } from "vitest";
import { NextRequest } from "next/server";

import { GET } from "@/app/api/catalog/[kind]/[id]/diff/[versionId]/route";
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

async function seedVersion(
  entityId: string,
  n: number,
  paramsJson: Record<string, unknown>,
  parentVersionId: string | null = null,
): Promise<string> {
  const sql = getSql();
  const idNum = Number.parseInt(entityId, 10);
  const parentNum = parentVersionId == null ? null : Number.parseInt(parentVersionId, 10);
  const rows = (await sql`
    insert into entity_version (entity_id, version_number, status, params_json, parent_version_id)
    values (${idNum}, ${n}, 'draft', ${sql.json(paramsJson as Parameters<typeof sql.json>[0])}, ${parentNum})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return rows[0]!.id;
}

async function invokeGet(
  kind: string,
  id: string,
  versionId: string,
): Promise<Response> {
  const url = `http://localhost/api/catalog/${kind}/${id}/diff/${versionId}`;
  const req = new NextRequest(new URL(url), { method: "GET" });
  const ctx = { params: Promise.resolve({ kind, id, versionId }) };
  return GET(req, ctx);
}

beforeEach(async () => {
  await resetSpine();
}, 30000);

afterEach(async () => {
  await resetSpine();
}, 30000);

describe("GET /api/catalog/[kind]/[id]/diff/[versionId] (TECH-3301)", () => {
  test("200 happy path — target + parent → diff envelope", async () => {
    const eid = await seedEntity("sprite", "diff_happy");
    const v1 = await seedVersion(eid, 1, { name: "old", image_path: "a.png" });
    const v2 = await seedVersion(
      eid,
      2,
      { name: "new", image_path: "a.png", tags: ["x"] },
      v1,
    );

    const res = await invokeGet("sprite", eid, v2);
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: {
        from: { id: string; version_number: number } | null;
        to: { id: string; version_number: number };
        diff: {
          added: string[];
          removed: string[];
          changed: Array<{ field: string; before: unknown; after: unknown; hint: string }>;
        };
      };
    };
    expect(body.ok).toBe(true);
    expect(body.data.from).not.toBeNull();
    expect(body.data.from!.version_number).toBe(1);
    expect(body.data.to.version_number).toBe(2);
    expect(body.data.diff.added).toEqual(["tags"]);
    expect(body.data.diff.removed).toEqual([]);
    expect(body.data.diff.changed.length).toBe(1);
    expect(body.data.diff.changed[0]!.field).toBe("name");
    expect(body.data.diff.changed[0]!.hint).toBe("scalar");
  });

  test("200 root version — no parent → from=null, diff against {}", async () => {
    const eid = await seedEntity("sprite", "diff_root");
    const v1 = await seedVersion(eid, 1, { name: "first", image_path: "a.png" });

    const res = await invokeGet("sprite", eid, v1);
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: {
        from: unknown | null;
        to: { version_number: number };
        diff: { added: string[]; removed: string[]; changed: unknown[] };
      };
    };
    expect(body.data.from).toBeNull();
    expect(body.data.to.version_number).toBe(1);
    expect(body.data.diff.added.sort()).toEqual(["image_path", "name"]);
    expect(body.data.diff.removed).toEqual([]);
    expect(body.data.diff.changed).toEqual([]);
  });

  test("404 not_found when target version missing", async () => {
    const res = await invokeGet("sprite", "1", "999999");
    expect(res.status).toBe(404);
    const body = (await res.json()) as { code: string; error: string };
    expect(body.code).toBe("not_found");
  });

  test("404 not_found when versionId belongs to different kind", async () => {
    const eid = await seedEntity("sprite", "diff_kind_mismatch");
    const v1 = await seedVersion(eid, 1, { name: "x" });

    // Query as 'audio' kind — should 404 because entity is sprite-kind.
    const res = await invokeGet("audio", eid, v1);
    expect(res.status).toBe(404);
    const body = (await res.json()) as { code: string };
    expect(body.code).toBe("not_found");
  });

  test("400 bad_request on bogus kind", async () => {
    const res = await invokeGet("bogus", "1", "1");
    expect(res.status).toBe(400);
    const body = (await res.json()) as { code: string; error: string };
    expect(body.code).toBe("bad_request");
    expect(body.error).toMatch(/kind/i);
  });

  test("400 bad_request on non-numeric versionId", async () => {
    const res = await invokeGet("sprite", "1", "abc");
    expect(res.status).toBe(400);
    const body = (await res.json()) as { code: string; error: string };
    expect(body.code).toBe("bad_request");
    expect(body.error).toMatch(/versionId/i);
  });
});
