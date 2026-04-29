/**
 * refs-repo unit tests (TECH-3408 / Stage 14.4).
 *
 * DB-backed via Postgres: TRUNCATE catalog_entity + entity_version +
 * catalog_ref_edge with CASCADE between tests.
 *
 * @see web/lib/repos/refs-repo.ts
 */
import { afterEach, beforeEach, describe, expect, test } from "vitest";

import { getSql } from "@/lib/db/client";
import {
  clampLimit,
  decodeCursor,
  encodeCursor,
  InvalidCursorError,
  listIncomingRefs,
  listOutgoingRefs,
} from "@/lib/repos/refs-repo";
import type { CatalogKind, EdgeRole } from "@/lib/refs/types";

async function resetSpine(): Promise<void> {
  const sql = getSql();
  await sql.unsafe(
    "truncate catalog_ref_edge, sprite_detail, asset_detail, economy_detail, pool_detail, pool_member, token_detail, button_detail, entity_version, catalog_entity restart identity cascade",
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
  versionNumber: number,
  status: "draft" | "published" = "published",
): Promise<string> {
  const sql = getSql();
  const idNum = Number.parseInt(entityId, 10);
  const rows = (await sql`
    insert into entity_version (entity_id, version_number, status)
    values (${idNum}, ${versionNumber}, ${status})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return rows[0]!.id;
}

async function setCurrentPublished(
  entityId: string,
  versionId: string,
): Promise<void> {
  const sql = getSql();
  const eid = Number.parseInt(entityId, 10);
  const vid = Number.parseInt(versionId, 10);
  await sql`
    update catalog_entity
    set current_published_version_id = ${vid}
    where id = ${eid}
  `;
}

async function seedEdge(args: {
  src_kind: CatalogKind;
  src_id: string;
  src_version_id: string;
  dst_kind: CatalogKind;
  dst_id: string;
  dst_version_id: string;
  edge_role: EdgeRole;
  created_at?: string;
}): Promise<void> {
  const sql = getSql();
  const srcId = Number.parseInt(args.src_id, 10);
  const srcVid = Number.parseInt(args.src_version_id, 10);
  const dstId = Number.parseInt(args.dst_id, 10);
  const dstVid = Number.parseInt(args.dst_version_id, 10);
  if (args.created_at) {
    await sql`
      insert into catalog_ref_edge
        (src_kind, src_id, src_version_id, dst_kind, dst_id, dst_version_id, edge_role, created_at)
      values
        (${args.src_kind}, ${srcId}, ${srcVid}, ${args.dst_kind}, ${dstId}, ${dstVid}, ${args.edge_role}, ${args.created_at}::timestamptz)
    `;
  } else {
    await sql`
      insert into catalog_ref_edge
        (src_kind, src_id, src_version_id, dst_kind, dst_id, dst_version_id, edge_role)
      values
        (${args.src_kind}, ${srcId}, ${srcVid}, ${args.dst_kind}, ${dstId}, ${dstVid}, ${args.edge_role})
    `;
  }
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
    expect(clampLimit(-3)).toBe(1);
  });
  test("clamps above MAX", () => {
    expect(clampLimit(1000)).toBe(100);
    expect(clampLimit(101)).toBe(100);
  });
  test("passes through valid values", () => {
    expect(clampLimit(20)).toBe(20);
    expect(clampLimit(50)).toBe(50);
  });
});

describe("encodeCursor / decodeCursor", () => {
  test("round-trip preserves shape", () => {
    const enc = encodeCursor({
      created_at_us: "1745884800000000",
      src_id: "10",
      dst_id: "20",
    });
    const dec = decodeCursor(enc);
    expect(dec.created_at_us).toBe("1745884800000000");
    expect(dec.src_id).toBe("10");
    expect(dec.dst_id).toBe("20");
  });
  test("rejects garbage base64", () => {
    expect(() => decodeCursor("notbase64$$$")).toThrow(InvalidCursorError);
  });
  test("rejects non-numeric src_id", () => {
    const bad = Buffer.from(
      JSON.stringify({
        created_at_us: "1745884800000000",
        src_id: "abc",
        dst_id: "10",
      }),
      "utf8",
    ).toString("base64");
    expect(() => decodeCursor(bad)).toThrow(InvalidCursorError);
  });
  test("rejects non-numeric dst_id", () => {
    const bad = Buffer.from(
      JSON.stringify({
        created_at_us: "1745884800000000",
        src_id: "10",
        dst_id: "x",
      }),
      "utf8",
    ).toString("base64");
    expect(() => decodeCursor(bad)).toThrow(InvalidCursorError);
  });
  test("rejects non-numeric created_at_us", () => {
    const bad = Buffer.from(
      JSON.stringify({
        created_at_us: "not-a-number",
        src_id: "10",
        dst_id: "20",
      }),
      "utf8",
    ).toString("base64");
    expect(() => decodeCursor(bad)).toThrow(InvalidCursorError);
  });
  test("rejects missing fields", () => {
    const bad = Buffer.from(JSON.stringify({ created_at_us: "1" }), "utf8").toString(
      "base64",
    );
    expect(() => decodeCursor(bad)).toThrow(InvalidCursorError);
  });
  test("rejects malformed JSON", () => {
    const bad = Buffer.from("{not json", "utf8").toString("base64");
    expect(() => decodeCursor(bad)).toThrow(InvalidCursorError);
  });
});

describe("listIncomingRefs DB integration", () => {
  test("non-numeric entityId returns empty page (no error)", async () => {
    const out = await listIncomingRefs("token", "not-a-number", null, 20);
    expect(out.rows).toEqual([]);
    expect(out.nextCursor).toBeNull();
  });

  test("empty entity (no edges) returns empty page", async () => {
    const out = await listIncomingRefs("token", "999999", null, 20);
    expect(out.rows).toEqual([]);
    expect(out.nextCursor).toBeNull();
  });

  test("returns rows DESC by created_at then src_id then dst_id", async () => {
    const tokenId = await seedEntity("token", "tok_inc_desc");
    const tokenVid = await seedVersion(tokenId, 1);
    await setCurrentPublished(tokenId, tokenVid);

    const panelA = await seedEntity("panel", "panel_a");
    const panelAVid = await seedVersion(panelA, 1);
    await setCurrentPublished(panelA, panelAVid);
    const panelB = await seedEntity("panel", "panel_b");
    const panelBVid = await seedVersion(panelB, 1);
    await setCurrentPublished(panelB, panelBVid);

    await seedEdge({
      src_kind: "panel",
      src_id: panelA,
      src_version_id: panelAVid,
      dst_kind: "token",
      dst_id: tokenId,
      dst_version_id: tokenVid,
      edge_role: "panel.token",
      created_at: "2026-04-01T00:00:00.000Z",
    });
    await seedEdge({
      src_kind: "panel",
      src_id: panelB,
      src_version_id: panelBVid,
      dst_kind: "token",
      dst_id: tokenId,
      dst_version_id: tokenVid,
      edge_role: "panel.token",
      created_at: "2026-04-02T00:00:00.000Z",
    });

    const out = await listIncomingRefs("token", tokenId, null, 20);
    expect(out.rows.length).toBe(2);
    // newest first
    expect(out.rows[0]!.src_id).toBe(panelB);
    expect(out.rows[1]!.src_id).toBe(panelA);
    expect(out.nextCursor).toBeNull();
  });

  test("excludes edges where source is not current_published", async () => {
    const tokenId = await seedEntity("token", "tok_inc_filter");
    const tokenVid = await seedVersion(tokenId, 1);
    await setCurrentPublished(tokenId, tokenVid);

    const panelId = await seedEntity("panel", "panel_filter");
    const oldVid = await seedVersion(panelId, 1);
    const newVid = await seedVersion(panelId, 2);
    await setCurrentPublished(panelId, newVid);

    // Edge from old (non-current) panel version — must be excluded.
    await seedEdge({
      src_kind: "panel",
      src_id: panelId,
      src_version_id: oldVid,
      dst_kind: "token",
      dst_id: tokenId,
      dst_version_id: tokenVid,
      edge_role: "panel.token",
    });

    const out = await listIncomingRefs("token", tokenId, null, 20);
    expect(out.rows.length).toBe(0);
  });

  test("cursor pagination — page 1 + page 2 cover all rows in DESC order", async () => {
    const tokenId = await seedEntity("token", "tok_cursor");
    const tokenVid = await seedVersion(tokenId, 1);
    await setCurrentPublished(tokenId, tokenVid);

    const panelIds: string[] = [];
    for (let i = 0; i < 3; i += 1) {
      const pid = await seedEntity("panel", `panel_c_${i}`);
      const pvid = await seedVersion(pid, 1);
      await setCurrentPublished(pid, pvid);
      panelIds.push(pid);
      await seedEdge({
        src_kind: "panel",
        src_id: pid,
        src_version_id: pvid,
        dst_kind: "token",
        dst_id: tokenId,
        dst_version_id: tokenVid,
        edge_role: "panel.token",
        created_at: `2026-04-0${i + 1}T00:00:00.000Z`,
      });
    }

    const page1 = await listIncomingRefs("token", tokenId, null, 2);
    expect(page1.rows.length).toBe(2);
    expect(page1.nextCursor).not.toBeNull();

    const page2 = await listIncomingRefs("token", tokenId, page1.nextCursor, 2);
    expect(page2.rows.length).toBe(1);
    expect(page2.nextCursor).toBeNull();

    const allSrcIds = new Set([
      ...page1.rows.map((r) => r.src_id),
      ...page2.rows.map((r) => r.src_id),
    ]);
    expect(allSrcIds.size).toBe(3);
  });

  test("row shape — bigint columns serialized as strings", async () => {
    const tokenId = await seedEntity("token", "tok_shape");
    const tokenVid = await seedVersion(tokenId, 1);
    await setCurrentPublished(tokenId, tokenVid);

    const panelId = await seedEntity("panel", "panel_shape");
    const panelVid = await seedVersion(panelId, 1);
    await setCurrentPublished(panelId, panelVid);

    await seedEdge({
      src_kind: "panel",
      src_id: panelId,
      src_version_id: panelVid,
      dst_kind: "token",
      dst_id: tokenId,
      dst_version_id: tokenVid,
      edge_role: "panel.token",
    });

    const out = await listIncomingRefs("token", tokenId, null, 20);
    expect(out.rows.length).toBe(1);
    const r = out.rows[0]!;
    expect(typeof r.src_id).toBe("string");
    expect(typeof r.src_version_id).toBe("string");
    expect(typeof r.dst_id).toBe("string");
    expect(typeof r.dst_version_id).toBe("string");
    expect(r.src_kind).toBe("panel");
    expect(r.dst_kind).toBe("token");
    expect(r.edge_role).toBe("panel.token");
    expect(typeof r.created_at).toBe("string");
  });
});

describe("listOutgoingRefs DB integration", () => {
  test("non-numeric entityId returns empty page", async () => {
    const out = await listOutgoingRefs("panel", "abc", null, 20);
    expect(out.rows).toEqual([]);
    expect(out.nextCursor).toBeNull();
  });

  test("returns outbound edges for source entity", async () => {
    const panelId = await seedEntity("panel", "panel_out");
    const panelVid = await seedVersion(panelId, 1);
    await setCurrentPublished(panelId, panelVid);

    const tokenId = await seedEntity("token", "tok_out");
    const tokenVid = await seedVersion(tokenId, 1);
    await setCurrentPublished(tokenId, tokenVid);

    await seedEdge({
      src_kind: "panel",
      src_id: panelId,
      src_version_id: panelVid,
      dst_kind: "token",
      dst_id: tokenId,
      dst_version_id: tokenVid,
      edge_role: "panel.token",
    });

    const out = await listOutgoingRefs("panel", panelId, null, 20);
    expect(out.rows.length).toBe(1);
    expect(out.rows[0]!.dst_id).toBe(tokenId);
    expect(out.rows[0]!.dst_kind).toBe("token");
  });

  test("excludes edges where dst is not current_published", async () => {
    const panelId = await seedEntity("panel", "panel_out_filter");
    const panelVid = await seedVersion(panelId, 1);
    await setCurrentPublished(panelId, panelVid);

    const tokenId = await seedEntity("token", "tok_out_filter");
    const oldVid = await seedVersion(tokenId, 1);
    const newVid = await seedVersion(tokenId, 2);
    await setCurrentPublished(tokenId, newVid);

    // Outbound edge points to old (non-current) token version — excluded.
    await seedEdge({
      src_kind: "panel",
      src_id: panelId,
      src_version_id: panelVid,
      dst_kind: "token",
      dst_id: tokenId,
      dst_version_id: oldVid,
      edge_role: "panel.token",
    });

    const out = await listOutgoingRefs("panel", panelId, null, 20);
    expect(out.rows.length).toBe(0);
  });
});
