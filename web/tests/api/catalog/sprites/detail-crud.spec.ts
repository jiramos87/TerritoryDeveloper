// TECH-1675 — sprite detail GET / PATCH / DELETE.
//
// Covers GET by slug, PATCH with frozen-version 409, DELETE with all three
// modes (retire / restore / delete-draft including the published-version
// guard).

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

async function getDetail(slug: string): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/sprites/[slug]/route");
  return invokeSpriteRoute(GET, "GET", `/api/catalog/sprites/${slug}`, { params: { slug } });
}

async function patchDetail(slug: string, body: unknown): Promise<Response> {
  const { PATCH } = await import("@/app/api/catalog/sprites/[slug]/route");
  return invokeSpriteRoute(PATCH, "PATCH", `/api/catalog/sprites/${slug}`, { body, params: { slug } });
}

async function deleteDetail(slug: string, mode?: string): Promise<Response> {
  const { DELETE } = await import("@/app/api/catalog/sprites/[slug]/route");
  const url = mode ? `/api/catalog/sprites/${slug}?mode=${mode}` : `/api/catalog/sprites/${slug}`;
  return invokeSpriteRoute(DELETE, "DELETE", url, { params: { slug } });
}

describe("GET /api/catalog/sprites/[slug] (TECH-1675)", () => {
  test("get_happy: returns full DTO including detail", async () => {
    await postSprite({
      slug: "detail_one",
      display_name: "Detail One",
      sprite_detail: { pixels_per_unit: 50 },
    });
    const res = await getDetail("detail_one");
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { slug: string; pixels_per_unit: number | null; active_version_status: string | null };
    };
    expect(body.data.slug).toBe("detail_one");
    expect(body.data.pixels_per_unit).toBe(50);
    expect(body.data.active_version_status).toBe("draft");
  });

  test("get_unknown_404: returns not_found envelope", async () => {
    const res = await getDetail("nope_nope");
    expect(res.status).toBe(404);
    const body = (await res.json()) as { code: string };
    expect(body.code).toBe("not_found");
  });
});

describe("PATCH /api/catalog/sprites/[slug] (TECH-1675)", () => {
  test("patch_draft_happy: updates display_name + pivot", async () => {
    await postSprite({ slug: "patch_one", display_name: "Old" });
    const res = await patchDetail("patch_one", {
      display_name: "New",
      sprite_detail: { pivot_x: 0.25 },
    });
    expect(res.status).toBe(200);
    const sql = getSql();
    const ent = (await sql`
      select display_name from catalog_entity where slug = 'patch_one'
    `) as unknown as Array<{ display_name: string }>;
    expect(ent[0]!.display_name).toBe("New");
    const det = (await sql`
      select pivot_x from sprite_detail
        join catalog_entity e on e.id = sprite_detail.entity_id
       where e.slug = 'patch_one'
    `) as unknown as Array<{ pivot_x: number }>;
    expect(det[0]!.pivot_x).toBeCloseTo(0.25, 5);
  });

  test("patch_frozen_version: rejects with 409 frozen_version", async () => {
    await postSprite({ slug: "frozen_one", display_name: "F" });
    const sql = getSql();
    // Promote draft to published manually.
    await sql`
      update entity_version set status = 'published'
       where entity_id = (select id from catalog_entity where slug = 'frozen_one')
    `;
    const res = await patchDetail("frozen_one", { display_name: "Should-Fail" });
    expect(res.status).toBe(409);
    const body = (await res.json()) as { code: string };
    expect(body.code).toBe("frozen_version");
  });

  test("patch_unknown_404", async () => {
    const res = await patchDetail("missing_one", { display_name: "X" });
    expect(res.status).toBe(404);
  });
});

describe("DELETE /api/catalog/sprites/[slug] (TECH-1675)", () => {
  test("delete_retire_then_restore: round-trips retired_at", async () => {
    await postSprite({ slug: "retire_one", display_name: "R" });
    const r1 = await deleteDetail("retire_one", "retire");
    expect(r1.status).toBe(200);
    const sql = getSql();
    const after1 = (await sql`
      select retired_at from catalog_entity where slug = 'retire_one'
    `) as unknown as Array<{ retired_at: string | null }>;
    expect(after1[0]!.retired_at).not.toBeNull();

    const r2 = await deleteDetail("retire_one", "restore");
    expect(r2.status).toBe(200);
    const after2 = (await sql`
      select retired_at from catalog_entity where slug = 'retire_one'
    `) as unknown as Array<{ retired_at: string | null }>;
    expect(after2[0]!.retired_at).toBeNull();
  });

  test("delete_draft_happy: removes entity when version is draft", async () => {
    await postSprite({ slug: "draft_one", display_name: "D" });
    const res = await deleteDetail("draft_one", "delete-draft");
    expect(res.status).toBe(200);
    const sql = getSql();
    const ex = (await sql`
      select 1 as ok from catalog_entity where slug = 'draft_one'
    `) as unknown as Array<{ ok: number }>;
    expect(ex.length).toBe(0);
  });

  test("delete_draft_blocked_when_published: 409 published_version", async () => {
    await postSprite({ slug: "pub_one", display_name: "P" });
    const sql = getSql();
    await sql`
      update entity_version set status = 'published'
       where entity_id = (select id from catalog_entity where slug = 'pub_one')
    `;
    const res = await deleteDetail("pub_one", "delete-draft");
    expect(res.status).toBe(409);
    const body = (await res.json()) as { code: string };
    expect(body.code).toBe("published_version");
  });

  test("delete_bad_mode: 400", async () => {
    const res = await deleteDetail("any_one", "junk");
    expect(res.status).toBe(400);
  });
});
