/**
 * Generic refs list route tests (TECH-3408 / Stage 14.4).
 *
 * Covers GET /api/catalog/[kind]/[id]/refs:
 *   - 200 happy path + envelope shape `{ok, data: {incoming, outgoing}}`
 *   - 400 invalid kind / id / cursor / side
 *   - cursor pagination (page1 nextCursor consumes page2)
 *   - side=incoming / side=outgoing scoping
 *
 * @see web/app/api/catalog/[kind]/[id]/refs/route.ts
 * @see web/lib/repos/refs-repo.ts
 */
import { afterEach, beforeEach, describe, expect, test } from "vitest";
import { NextRequest } from "next/server";

import { GET } from "@/app/api/catalog/[kind]/[id]/refs/route";
import { getSql } from "@/lib/db/client";
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
): Promise<string> {
  const sql = getSql();
  const idNum = Number.parseInt(entityId, 10);
  const rows = (await sql`
    insert into entity_version (entity_id, version_number, status)
    values (${idNum}, ${versionNumber}, 'published')
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

async function invokeGet(
  kind: string,
  id: string,
  qs = "",
): Promise<Response> {
  const url = `http://localhost/api/catalog/${kind}/${id}/refs${qs}`;
  const req = new NextRequest(new URL(url), { method: "GET" });
  const ctx = { params: Promise.resolve({ kind, id }) };
  return GET(req, ctx);
}

interface RefsBody {
  ok: boolean;
  data: {
    incoming: { rows: Array<{ src_id: string; dst_id: string }>; nextCursor: string | null };
    outgoing: { rows: Array<{ src_id: string; dst_id: string }>; nextCursor: string | null };
  };
}

beforeEach(async () => {
  await resetSpine();
}, 30000);

afterEach(async () => {
  await resetSpine();
}, 30000);

describe("GET /api/catalog/[kind]/[id]/refs (TECH-3408)", () => {
  test("200 empty entity returns {ok, data:{incoming:{rows:[],nextCursor:null}, outgoing:{rows:[],nextCursor:null}}}", async () => {
    const res = await invokeGet("token", "999999");
    expect(res.status).toBe(200);
    const body = (await res.json()) as RefsBody;
    expect(body.ok).toBe(true);
    expect(body.data.incoming.rows).toEqual([]);
    expect(body.data.incoming.nextCursor).toBeNull();
    expect(body.data.outgoing.rows).toEqual([]);
    expect(body.data.outgoing.nextCursor).toBeNull();
  });

  test("200 happy path returns incoming + outgoing rows", async () => {
    const tokenId = await seedEntity("token", "happy_token");
    const tokenVid = await seedVersion(tokenId, 1);
    await setCurrentPublished(tokenId, tokenVid);

    const panelId = await seedEntity("panel", "happy_panel");
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

    const incRes = await invokeGet("token", tokenId);
    expect(incRes.status).toBe(200);
    const incBody = (await incRes.json()) as RefsBody;
    expect(incBody.ok).toBe(true);
    expect(incBody.data.incoming.rows.length).toBe(1);
    expect(incBody.data.incoming.rows[0]!.src_id).toBe(panelId);
    expect(incBody.data.outgoing.rows.length).toBe(0);

    const outRes = await invokeGet("panel", panelId);
    expect(outRes.status).toBe(200);
    const outBody = (await outRes.json()) as RefsBody;
    expect(outBody.data.outgoing.rows.length).toBe(1);
    expect(outBody.data.outgoing.rows[0]!.dst_id).toBe(tokenId);
    expect(outBody.data.incoming.rows.length).toBe(0);
  });

  test("400 invalid kind on bogus kind", async () => {
    const res = await invokeGet("bogus", "123");
    expect(res.status).toBe(400);
    const body = (await res.json()) as { code: string; error: string };
    expect(body.code).toBe("bad_request");
    expect(body.error).toMatch(/kind/i);
  });

  test("400 invalid id on non-numeric id", async () => {
    const res = await invokeGet("token", "abc");
    expect(res.status).toBe(400);
    const body = (await res.json()) as { code: string; error: string };
    expect(body.code).toBe("bad_request");
    expect(body.error).toMatch(/id/i);
  });

  test("400 invalid cursor on malformed cursor", async () => {
    const tokenId = await seedEntity("token", "bad_cursor_token");
    const tokenVid = await seedVersion(tokenId, 1);
    await setCurrentPublished(tokenId, tokenVid);
    const res = await invokeGet("token", tokenId, "?cursor=garbage$$$");
    expect(res.status).toBe(400);
    const body = (await res.json()) as { code: string; error: string };
    expect(body.code).toBe("bad_request");
    expect(body.error).toMatch(/cursor/i);
  });

  test("400 invalid side on bogus side value", async () => {
    const res = await invokeGet("token", "1", "?side=both");
    expect(res.status).toBe(400);
    const body = (await res.json()) as { code: string; error: string };
    expect(body.code).toBe("bad_request");
    expect(body.error).toMatch(/side/i);
  });

  test("side=incoming scopes to incoming only", async () => {
    const tokenId = await seedEntity("token", "side_in_token");
    const tokenVid = await seedVersion(tokenId, 1);
    await setCurrentPublished(tokenId, tokenVid);

    const panelId = await seedEntity("panel", "side_in_panel");
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

    const res = await invokeGet("token", tokenId, "?side=incoming");
    expect(res.status).toBe(200);
    const body = (await res.json()) as RefsBody;
    expect(body.data.incoming.rows.length).toBe(1);
    expect(body.data.outgoing.rows).toEqual([]);
    expect(body.data.outgoing.nextCursor).toBeNull();
  });

  test("side=outgoing scopes to outgoing only", async () => {
    const tokenId = await seedEntity("token", "side_out_token");
    const tokenVid = await seedVersion(tokenId, 1);
    await setCurrentPublished(tokenId, tokenVid);

    const panelId = await seedEntity("panel", "side_out_panel");
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

    const res = await invokeGet("panel", panelId, "?side=outgoing");
    expect(res.status).toBe(200);
    const body = (await res.json()) as RefsBody;
    expect(body.data.outgoing.rows.length).toBe(1);
    expect(body.data.incoming.rows).toEqual([]);
    expect(body.data.incoming.nextCursor).toBeNull();
  });

  test("cursor pagination — page 1 nextCursor consumes page 2 (incoming side)", async () => {
    const tokenId = await seedEntity("token", "page_token");
    const tokenVid = await seedVersion(tokenId, 1);
    await setCurrentPublished(tokenId, tokenVid);

    for (let i = 0; i < 3; i += 1) {
      const pid = await seedEntity("panel", `page_panel_${i}`);
      const pvid = await seedVersion(pid, 1);
      await setCurrentPublished(pid, pvid);
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

    const r1 = await invokeGet("token", tokenId, "?side=incoming&limit=2");
    const b1 = (await r1.json()) as RefsBody;
    expect(b1.data.incoming.rows.length).toBe(2);
    expect(b1.data.incoming.nextCursor).not.toBeNull();

    const r2 = await invokeGet(
      "token",
      tokenId,
      `?side=incoming&limit=2&cursor=${encodeURIComponent(b1.data.incoming.nextCursor!)}`,
    );
    const b2 = (await r2.json()) as RefsBody;
    expect(b2.data.incoming.rows.length).toBe(1);
    expect(b2.data.incoming.nextCursor).toBeNull();

    const allSrcIds = new Set([
      ...b1.data.incoming.rows.map((r) => r.src_id),
      ...b2.data.incoming.rows.map((r) => r.src_id),
    ]);
    expect(allSrcIds.size).toBe(3);
  });
});
