// TECH-8608 / Stage 19.1 — Pool list + create + detail PATCH (member edits) round-trip.

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
import {
  POOL_TEST_USER_ID,
  invokePoolRoute,
  resetPoolTables,
  seedAssetForPool,
  seedPoolTestUser,
} from "./_harness";

const mockGetSession = getSessionUser as unknown as ReturnType<typeof vi.fn>;

beforeEach(async () => {
  await seedPoolTestUser();
  await resetPoolTables();
  mockGetSession.mockResolvedValue({
    id: POOL_TEST_USER_ID,
    email: "pool-tests@example.com",
    role: "admin",
  });
}, 30000);

afterEach(async () => {
  await resetPoolTables();
  vi.clearAllMocks();
}, 30000);

async function postPool(body: unknown): Promise<Response> {
  const { POST } = await import("@/app/api/catalog/pools/route");
  return invokePoolRoute(POST, "POST", "/api/catalog/pools", { body });
}

async function listPools(qs = ""): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/pools/route");
  return invokePoolRoute(GET, "GET", `/api/catalog/pools${qs}`);
}

async function getPool(slug: string): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/pools/[slug]/route");
  return invokePoolRoute(GET, "GET", `/api/catalog/pools/${slug}`, {
    params: { slug },
  });
}

async function patchPool(slug: string, body: unknown): Promise<Response> {
  const { PATCH } = await import("@/app/api/catalog/pools/[slug]/route");
  return invokePoolRoute(PATCH, "PATCH", `/api/catalog/pools/${slug}`, {
    body,
    params: { slug },
  });
}

describe("POST /api/catalog/pools (TECH-8608)", () => {
  test("create_happy: inserts entity + pool_detail in one tx", async () => {
    const res = await postPool({
      slug: "house_variants",
      display_name: "House Variants",
      pool_detail: { primary_subtype: "house", owner_category: "residential" },
    });
    expect(res.status).toBe(201);
    const body = (await res.json()) as {
      ok: boolean;
      data: { entity_id: string; slug: string };
      audit_id: string | null;
    };
    expect(body.ok).toBe(true);
    expect(body.data.slug).toBe("house_variants");
    expect(body.audit_id).toMatch(/^\d+$/);
  });

  test("create_validation_bad_slug: rejects invalid slug shape", async () => {
    const res = await postPool({ slug: "Bad-Pool!", display_name: "Bad" });
    expect(res.status).toBe(400);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("bad_request");
  });

  test("create_duplicate_slug: returns 409 unique_violation", async () => {
    await postPool({ slug: "dup_pool", display_name: "First" });
    const res = await postPool({ slug: "dup_pool", display_name: "Second" });
    expect(res.status).toBe(409);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("unique_violation");
  });
});

describe("GET /api/catalog/pools (TECH-8608)", () => {
  test("list_active_filter: returns only non-retired", async () => {
    await postPool({ slug: "active_pool", display_name: "Active" });
    await postPool({ slug: "retired_pool", display_name: "Retired" });
    const sql = (await import("@/lib/db/client")).getSql();
    await sql`update catalog_entity set retired_at = now() where slug = 'retired_pool'`;
    const res = await listPools("?status=active");
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { items: Array<{ slug: string }>; next_cursor: string | null };
    };
    const slugs = body.data.items.map((i) => i.slug);
    expect(slugs).toContain("active_pool");
    expect(slugs).not.toContain("retired_pool");
  });
});

describe("PATCH /api/catalog/pools/[slug] (TECH-8608)", () => {
  test("patch_member_add: round-trips member rows + display_name", async () => {
    const assetA = await seedAssetForPool("house_a", "House A");
    const assetB = await seedAssetForPool("house_b", "House B");
    await postPool({
      slug: "round_trip_pool",
      display_name: "Round Trip",
      pool_detail: { primary_subtype: "house" },
    });
    const getRes = await getPool("round_trip_pool");
    const getBody = (await getRes.json()) as { data: { updated_at: string } };

    const patchRes = await patchPool("round_trip_pool", {
      updated_at: getBody.data.updated_at,
      display_name: "Round Trip Renamed",
      members: [
        { asset_entity_id: assetA, weight: 3 },
        { asset_entity_id: assetB, weight: 1 },
      ],
    });
    expect(patchRes.status).toBe(200);
    const patchBody = (await patchRes.json()) as {
      ok: boolean;
      data: { display_name: string; members: Array<{ asset_entity_id: string; weight: number }> };
      audit_id: string | null;
    };
    expect(patchBody.ok).toBe(true);
    expect(patchBody.data.display_name).toBe("Round Trip Renamed");
    expect(patchBody.data.members.length).toBe(2);
    expect(patchBody.data.members.map((m) => m.asset_entity_id).sort()).toEqual(
      [assetA, assetB].sort(),
    );
  });

  test("patch_member_remove: drops member via removed_member_entity_ids", async () => {
    const assetA = await seedAssetForPool("rm_a", "RemA");
    const assetB = await seedAssetForPool("rm_b", "RemB");
    await postPool({
      slug: "rm_pool",
      display_name: "Remove",
      pool_detail: { primary_subtype: "house" },
    });
    let cur = await getPool("rm_pool");
    let curBody = (await cur.json()) as { data: { updated_at: string } };
    await patchPool("rm_pool", {
      updated_at: curBody.data.updated_at,
      members: [
        { asset_entity_id: assetA, weight: 1 },
        { asset_entity_id: assetB, weight: 1 },
      ],
    });
    cur = await getPool("rm_pool");
    curBody = (await cur.json()) as { data: { updated_at: string } };

    const res = await patchPool("rm_pool", {
      updated_at: curBody.data.updated_at,
      removed_member_entity_ids: [assetA],
    });
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      data: { members: Array<{ asset_entity_id: string }> };
    };
    const ids = body.data.members.map((m) => m.asset_entity_id);
    expect(ids).toEqual([assetB]);
  });

  test("patch_stale_updated_at: returns 409 conflict", async () => {
    await postPool({ slug: "stale_pool", display_name: "Stale" });
    const res = await patchPool("stale_pool", {
      updated_at: "1970-01-01T00:00:00.000Z",
      display_name: "Should Fail",
    });
    expect(res.status).toBe(409);
  });
});
