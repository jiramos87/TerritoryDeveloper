// TECH-2093 / Stage 10.1 — token ripple-count GET (DEC-A44 banner source).
// TECH-3411 / Stage 14.4 — upgraded to query catalog_ref_edge for real count
// (published incoming edges only; matches listIncomingRefs ripple semantics).
//
// Covers GET /api/catalog/tokens/[slug]/ripple-count:
//   • slug-existence gate (404 vs 200) — Stage 10.1 contract preserved
//   • zero-edge case → data.count === 0
//   • N-edge case (panel.token + button-style) → data.count === N
//   • parity assertion: route count === sum(listIncomingRefs across pages)

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
import { getSql } from "@/lib/db/client";
import { listIncomingRefs } from "@/lib/repos/refs-repo";
import type { CatalogKind, EdgeRole } from "@/lib/refs/types";
import {
  TOKEN_TEST_USER_ID,
  invokeTokenRoute,
  resetTokenTables,
  seedTokenTestUser,
} from "./_harness";

const mockGetSession = getSessionUser as unknown as ReturnType<typeof vi.fn>;

beforeEach(async () => {
  await seedTokenTestUser();
  await resetTokenTables();
  // Stage 14.1 ref-edge graph: harness truncates token tables but not the edge
  // table — wipe explicitly so prior runs don't bleed.
  const sql = getSql();
  await sql.unsafe("truncate catalog_ref_edge restart identity cascade");
  mockGetSession.mockResolvedValue({
    id: TOKEN_TEST_USER_ID,
    email: "token-tests@example.com",
    role: "admin",
  });
}, 30000);

afterEach(async () => {
  const sql = getSql();
  await sql.unsafe("truncate catalog_ref_edge restart identity cascade");
  await resetTokenTables();
  vi.clearAllMocks();
}, 30000);

async function postToken(body: unknown): Promise<Response> {
  const { POST } = await import("@/app/api/catalog/tokens/route");
  return invokeTokenRoute(POST, "POST", "/api/catalog/tokens", { body });
}

async function getRipple(slug: string): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/tokens/[slug]/ripple-count/route");
  return invokeTokenRoute(GET, "GET", `/api/catalog/tokens/${slug}/ripple-count`, {
    params: { slug },
  });
}

/**
 * Token POST creates `catalog_entity` + `token_detail` (no `entity_version`).
 * The ripple-count join filters on the SOURCE side's
 * `current_published_version_id` (Stage 14.4 / DEC-A44 published-only semantics);
 * `catalog_ref_edge.dst_version_id` is unconstrained at the table level so any
 * stable bigint suffices for the seeded edge row.
 */
async function fetchTokenEntity(slug: string): Promise<{ entity_id: string }> {
  const sql = getSql();
  const rows = (await sql`
    select id::text as entity_id
    from catalog_entity
    where kind = 'token' and slug = ${slug}
  `) as unknown as Array<{ entity_id: string }>;
  if (!rows[0]) throw new Error(`Token ${slug} not found`);
  return { entity_id: rows[0].entity_id };
}

/** Stable synthetic dst_version_id used for token edge seeds (no FK enforced). */
const TOKEN_DST_VERSION_ID = "1";

async function seedSourceEntity(
  kind: CatalogKind,
  slug: string,
): Promise<{ entity_id: string; version_id: string }> {
  const sql = getSql();
  const entityRows = (await sql`
    insert into catalog_entity (kind, slug, display_name, tags)
    values (${kind}, ${slug}, ${slug}, ${[]}::text[])
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const entityId = entityRows[0]!.id;
  const idNum = Number.parseInt(entityId, 10);
  const versionRows = (await sql`
    insert into entity_version (entity_id, version_number, status)
    values (${idNum}, 1, 'published')
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const versionId = versionRows[0]!.id;
  const vidNum = Number.parseInt(versionId, 10);
  await sql`
    update catalog_entity
    set current_published_version_id = ${vidNum}
    where id = ${idNum}
  `;
  return { entity_id: entityId, version_id: versionId };
}

async function seedEdge(args: {
  src_kind: CatalogKind;
  src_id: string;
  src_version_id: string;
  dst_kind: CatalogKind;
  dst_id: string;
  dst_version_id: string;
  edge_role: EdgeRole;
}): Promise<void> {
  const sql = getSql();
  await sql`
    insert into catalog_ref_edge
      (src_kind, src_id, src_version_id, dst_kind, dst_id, dst_version_id, edge_role)
    values
      (${args.src_kind}, ${Number.parseInt(args.src_id, 10)}, ${Number.parseInt(args.src_version_id, 10)},
       ${args.dst_kind}, ${Number.parseInt(args.dst_id, 10)}, ${Number.parseInt(args.dst_version_id, 10)},
       ${args.edge_role})
  `;
}

describe("GET /api/catalog/tokens/[slug]/ripple-count — TECH-2093 / TECH-3411", () => {
  test("returns 200 with count=0 when token exists with no incoming edges", async () => {
    const create = await postToken({
      slug: "tok_ripple_zero",
      display_name: "Ripple Zero",
      token_detail: { token_kind: "color", value_json: { hex: "#112233" } },
    });
    expect(create.status).toBe(201);

    const res = await getRipple("tok_ripple_zero");
    expect(res.status).toBe(200);
    const body = (await res.json()) as { ok: boolean; data: { count: number } };
    expect(body.ok).toBe(true);
    expect(body.data.count).toBe(0);
  });

  test("returns 404 when token does not exist", async () => {
    const res = await getRipple("tok_does_not_exist");
    expect(res.status).toBe(404);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("not_found");
  });

  test("returns 200 with count=N when N published incoming edges exist", async () => {
    const create = await postToken({
      slug: "tok_ripple_n",
      display_name: "Ripple N",
      token_detail: { token_kind: "color", value_json: { hex: "#445566" } },
    });
    expect(create.status).toBe(201);

    const token = await fetchTokenEntity("tok_ripple_n");

    const panelA = await seedSourceEntity("panel", "panel_rip_a");
    const panelB = await seedSourceEntity("panel", "panel_rip_b");
    const panelC = await seedSourceEntity("panel", "panel_rip_c");
    for (const p of [panelA, panelB, panelC]) {
      await seedEdge({
        src_kind: "panel",
        src_id: p.entity_id,
        src_version_id: p.version_id,
        dst_kind: "token",
        dst_id: token.entity_id,
        dst_version_id: TOKEN_DST_VERSION_ID,
        edge_role: "panel.token",
      });
    }

    const res = await getRipple("tok_ripple_n");
    expect(res.status).toBe(200);
    const body = (await res.json()) as { ok: boolean; data: { count: number } };
    expect(body.ok).toBe(true);
    expect(body.data.count).toBe(3);
  });

  test("excludes edges from non-current_published source versions", async () => {
    const create = await postToken({
      slug: "tok_ripple_filter",
      display_name: "Ripple Filter",
      token_detail: { token_kind: "color", value_json: { hex: "#778899" } },
    });
    expect(create.status).toBe(201);

    const token = await fetchTokenEntity("tok_ripple_filter");

    // Panel with two versions; current_published = newer. Edge from older
    // version must NOT be counted (DEC-A44 published-only semantics).
    const sql = getSql();
    const panelRows = (await sql`
      insert into catalog_entity (kind, slug, display_name, tags)
      values ('panel', 'panel_filter', 'panel_filter', ${[]}::text[])
      returning id::text as id
    `) as unknown as Array<{ id: string }>;
    const panelId = panelRows[0]!.id;
    const panelIdNum = Number.parseInt(panelId, 10);
    const oldVRows = (await sql`
      insert into entity_version (entity_id, version_number, status)
      values (${panelIdNum}, 1, 'published')
      returning id::text as id
    `) as unknown as Array<{ id: string }>;
    const oldVid = oldVRows[0]!.id;
    const newVRows = (await sql`
      insert into entity_version (entity_id, version_number, status)
      values (${panelIdNum}, 2, 'published')
      returning id::text as id
    `) as unknown as Array<{ id: string }>;
    const newVid = newVRows[0]!.id;
    await sql`
      update catalog_entity
      set current_published_version_id = ${Number.parseInt(newVid, 10)}
      where id = ${panelIdNum}
    `;

    await seedEdge({
      src_kind: "panel",
      src_id: panelId,
      src_version_id: oldVid,
      dst_kind: "token",
      dst_id: token.entity_id,
      dst_version_id: TOKEN_DST_VERSION_ID,
      edge_role: "panel.token",
    });

    const res = await getRipple("tok_ripple_filter");
    expect(res.status).toBe(200);
    const body = (await res.json()) as { ok: boolean; data: { count: number } };
    expect(body.data.count).toBe(0);
  });

  test("parity — route count equals listIncomingRefs total across pages", async () => {
    const create = await postToken({
      slug: "tok_ripple_parity",
      display_name: "Ripple Parity",
      token_detail: { token_kind: "color", value_json: { hex: "#aabbcc" } },
    });
    expect(create.status).toBe(201);

    const token = await fetchTokenEntity("tok_ripple_parity");

    // Seed 5 panel→token edges to force at least one cursor page boundary.
    for (let i = 0; i < 5; i += 1) {
      const panel = await seedSourceEntity("panel", `panel_par_${i}`);
      await seedEdge({
        src_kind: "panel",
        src_id: panel.entity_id,
        src_version_id: panel.version_id,
        dst_kind: "token",
        dst_id: token.entity_id,
        dst_version_id: TOKEN_DST_VERSION_ID,
        edge_role: "panel.token",
      });
    }

    const res = await getRipple("tok_ripple_parity");
    expect(res.status).toBe(200);
    const body = (await res.json()) as { ok: boolean; data: { count: number } };
    const routeCount = body.data.count;
    expect(routeCount).toBe(5);

    // Paginate listIncomingRefs (page size 2) until exhausted; sum rows.
    let cursor: string | null = null;
    let total = 0;
    let pageGuard = 0;
    do {
      const page = await listIncomingRefs("token", token.entity_id, cursor, 2);
      total += page.rows.length;
      cursor = page.nextCursor;
      pageGuard += 1;
      if (pageGuard > 10) throw new Error("pagination loop guard tripped");
    } while (cursor !== null);

    expect(total).toBe(routeCount);
  });
});
