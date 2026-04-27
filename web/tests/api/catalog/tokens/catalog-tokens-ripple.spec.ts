// TECH-2093 / Stage 10.1 — token ripple-count GET (DEC-A44 banner source).
//
// Covers GET /api/catalog/tokens/[slug]/ripple-count — Stage 10.1 baseline
// returns `count: 0` (catalog_ref_edge ships in Stage 14.1) but contract still
// gates on slug existence (404 vs 200).

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
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
  mockGetSession.mockResolvedValue({
    id: TOKEN_TEST_USER_ID,
    email: "token-tests@example.com",
    role: "admin",
  });
}, 30000);

afterEach(async () => {
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

describe("GET /api/catalog/tokens/[slug]/ripple-count — TECH-2093", () => {
  test("returns 200 with count=0 when token exists", async () => {
    const create = await postToken({
      slug: "tok_ripple_a",
      display_name: "Ripple A",
      token_detail: { token_kind: "color", value_json: { hex: "#112233" } },
    });
    expect(create.status).toBe(201);

    const res = await getRipple("tok_ripple_a");
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
});
