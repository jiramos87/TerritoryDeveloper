// TECH-1675 — sprite list + create.
//
// Covers GET /api/catalog/sprites (active|retired|all + cursor pagination)
// and POST /api/catalog/sprites (create-tx + duplicate-slug 409).
// Direct-invoke (no dev server); DB-backed; capability gate stubbed via
// session mock (mirrors render harness).

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
import { getSql } from "@/lib/db/client";
import {
  SPRITE_TEST_USER_ID,
  invokeSpriteRoute,
  resetSpriteTables,
  seedSpriteTestUser,
} from "./_harness";

const mockGetSession = getSessionUser as unknown as ReturnType<typeof vi.fn>;

beforeEach(async () => {
  await seedSpriteTestUser();
  await resetSpriteTables();
  mockGetSession.mockResolvedValue({
    id: SPRITE_TEST_USER_ID,
    email: "sprite-tests@example.com",
    role: "admin",
  });
}, 30000);

afterEach(async () => {
  await resetSpriteTables();
  vi.clearAllMocks();
}, 30000);

async function postSprite(body: unknown): Promise<Response> {
  const { POST } = await import("@/app/api/catalog/sprites/route");
  return invokeSpriteRoute(POST, "POST", "/api/catalog/sprites", { body });
}

async function listSprites(qs = ""): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/sprites/route");
  return invokeSpriteRoute(GET, "GET", `/api/catalog/sprites${qs}`);
}

describe("POST /api/catalog/sprites (TECH-1675)", () => {
  test("create_happy: inserts entity + version + detail in one tx", async () => {
    const res = await postSprite({
      slug: "alpha_sprite",
      display_name: "Alpha Sprite",
      tags: ["test"],
      sprite_detail: { pixels_per_unit: 64, pivot_x: 0.5, pivot_y: 0.5, provenance: "hand" },
    });
    expect(res.status).toBe(201);
    const body = (await res.json()) as {
      ok: boolean;
      data: { entity_id: string; slug: string };
      audit_id: string | null;
    };
    expect(body.ok).toBe(true);
    expect(body.data.slug).toBe("alpha_sprite");
    expect(body.audit_id).toMatch(/^\d+$/);

    const sql = getSql();
    const eRows = (await sql`
      select id::text as id, slug, display_name, tags from catalog_entity where slug = 'alpha_sprite'
    `) as unknown as Array<{ id: string; slug: string; display_name: string; tags: string[] }>;
    expect(eRows.length).toBe(1);
    expect(eRows[0]!.display_name).toBe("Alpha Sprite");
    expect(eRows[0]!.tags).toEqual(["test"]);

    const vRows = (await sql`
      select status, version_number from entity_version where entity_id = ${eRows[0]!.id}::bigint
    `) as unknown as Array<{ status: string; version_number: number }>;
    expect(vRows.length).toBe(1);
    expect(vRows[0]!.status).toBe("draft");
    expect(vRows[0]!.version_number).toBe(1);

    const dRows = (await sql`
      select pixels_per_unit, pivot_x, pivot_y, provenance from sprite_detail where entity_id = ${eRows[0]!.id}::bigint
    `) as unknown as Array<{ pixels_per_unit: number; pivot_x: number; pivot_y: number; provenance: string }>;
    expect(dRows.length).toBe(1);
    expect(dRows[0]!.pixels_per_unit).toBe(64);
    expect(dRows[0]!.provenance).toBe("hand");
  });

  test("create_validation_bad_slug: rejects invalid slug shape", async () => {
    const res = await postSprite({ slug: "Bad-Slug!", display_name: "Bad" });
    expect(res.status).toBe(400);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("bad_request");
  });

  test("create_duplicate_slug: returns 409", async () => {
    await postSprite({ slug: "dup_one", display_name: "First" });
    const res = await postSprite({ slug: "dup_one", display_name: "Second" });
    expect(res.status).toBe(409);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("unique_violation");
  });
});

describe("GET /api/catalog/sprites (TECH-1675)", () => {
  test("list_active_filter: returns only non-retired by default", async () => {
    await postSprite({ slug: "active_one", display_name: "Active 1" });
    await postSprite({ slug: "active_two", display_name: "Active 2" });
    await postSprite({ slug: "retired_one", display_name: "Retired" });
    // Manually retire third sprite.
    const sql = getSql();
    await sql`update catalog_entity set retired_at = now() where slug = 'retired_one'`;

    const res = await listSprites("?status=active");
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { items: Array<{ slug: string }>; next_cursor: string | null };
    };
    expect(body.ok).toBe(true);
    const slugs = body.data.items.map((i) => i.slug);
    expect(slugs).toContain("active_one");
    expect(slugs).toContain("active_two");
    expect(slugs).not.toContain("retired_one");

    const all = await listSprites("?status=all");
    const allBody = (await all.json()) as { data: { items: Array<{ slug: string }> } };
    expect(allBody.data.items.map((i) => i.slug)).toContain("retired_one");

    const ret = await listSprites("?status=retired");
    const retBody = (await ret.json()) as { data: { items: Array<{ slug: string }> } };
    expect(retBody.data.items.map((i) => i.slug)).toEqual(["retired_one"]);
  });

  test("list_pagination: cursor round-trips", async () => {
    for (let i = 0; i < 5; i++) {
      await postSprite({ slug: `page_${i}`, display_name: `P${i}` });
    }
    const r1 = await listSprites("?status=active&limit=2");
    const b1 = (await r1.json()) as {
      data: { items: Array<{ entity_id: string; slug: string }>; next_cursor: string | null };
    };
    expect(b1.data.items.length).toBe(2);
    expect(b1.data.next_cursor).not.toBeNull();

    const r2 = await listSprites(`?status=active&limit=2&cursor=${b1.data.next_cursor}`);
    const b2 = (await r2.json()) as {
      data: { items: Array<{ slug: string }>; next_cursor: string | null };
    };
    expect(b2.data.items.length).toBe(2);
    // Distinct from first page.
    expect(b2.data.items[0]!.slug).not.toBe(b1.data.items[0]!.slug);
  });

  test("list_bad_status: 400 for unknown filter", async () => {
    const res = await listSprites("?status=junk");
    expect(res.status).toBe(400);
  });
});
