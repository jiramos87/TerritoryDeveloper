// TECH-2093 / Stage 10.1 — POST→PATCH token roundtrip per editor kind.
//
// Covers full create-then-edit path through public REST surface:
//   POST  /api/catalog/tokens             — create kind row
//   PATCH /api/catalog/tokens/[slug]      — value_json mutation per kind
//   GET   /api/catalog/tokens/[slug]      — readback verifies persistence
//
// Acts as the integration counterpart to the per-kind editor unit tests.

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
import {
  TOKEN_TEST_USER_ID,
  invokeTokenRoute,
  resetTokenTables,
  seedBareTokenEntity,
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

async function patchToken(slug: string, body: unknown): Promise<Response> {
  const { PATCH } = await import("@/app/api/catalog/tokens/[slug]/route");
  return invokeTokenRoute(PATCH, "PATCH", `/api/catalog/tokens/${slug}`, {
    body,
    params: { slug },
  });
}

async function getToken(slug: string): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/tokens/[slug]/route");
  return invokeTokenRoute(GET, "GET", `/api/catalog/tokens/${slug}`, {
    params: { slug },
  });
}

type TokenEnvelope = {
  ok: boolean;
  data: {
    slug: string;
    entity_id?: string;
    updated_at: string;
    token_detail: {
      token_kind: string;
      value_json: Record<string, unknown>;
      semantic_target_entity_id: string | null;
    } | null;
  };
};

async function readEnvelope(res: Response): Promise<TokenEnvelope> {
  return (await res.json()) as TokenEnvelope;
}

/**
 * POST returns minimal `{entity_id, slug}`; GET fetches the full row to grab
 * `updated_at` for the subsequent PATCH (DEC-A38 optimistic-concurrency token).
 */
async function postThenGet(body: {
  slug: string;
  display_name: string;
  token_detail: Record<string, unknown>;
}): Promise<TokenEnvelope["data"]> {
  const post = await postToken(body);
  if (post.status !== 201) {
    const err = await post.json();
    throw new Error(`POST failed ${post.status}: ${JSON.stringify(err)}`);
  }
  const get = await getToken(body.slug);
  const env = await readEnvelope(get);
  return env.data;
}

describe("POST→PATCH→GET roundtrip per token kind (TECH-2093)", () => {
  test("color: hex value_json mutates", async () => {
    const created = await postThenGet({
      slug: "rt_color",
      display_name: "Roundtrip color",
      token_detail: { token_kind: "color", value_json: { hex: "#aabbcc" } },
    });

    const patch = await patchToken("rt_color", {
      updated_at: created.updated_at,
      token_detail: { value_json: { hex: "#112233" } },
    });
    expect(patch.status).toBe(200);

    const get = await getToken("rt_color");
    const env = await readEnvelope(get);
    expect(env.data.token_detail!.value_json).toMatchObject({ hex: "#112233" });
  });

  test("type-scale: size_px mutates", async () => {
    const created = await postThenGet({
      slug: "rt_scale",
      display_name: "Roundtrip scale",
      token_detail: {
        token_kind: "type-scale",
        value_json: { font_family: "Inter", size_px: 14, line_height: 1.5 },
      },
    });
    const patch = await patchToken("rt_scale", {
      updated_at: created.updated_at,
      token_detail: { value_json: { font_family: "Inter", size_px: 18, line_height: 1.4 } },
    });
    expect(patch.status).toBe(200);
    const get = await getToken("rt_scale");
    const env = await readEnvelope(get);
    expect(env.data.token_detail!.value_json).toMatchObject({ size_px: 18, line_height: 1.4 });
  });

  test("motion: cubic-bezier curve mutates", async () => {
    const created = await postThenGet({
      slug: "rt_motion",
      display_name: "Roundtrip motion",
      token_detail: { token_kind: "motion", value_json: { curve: "linear", duration_ms: 200 } },
    });
    const patch = await patchToken("rt_motion", {
      updated_at: created.updated_at,
      token_detail: {
        value_json: { curve: "cubic-bezier", duration_ms: 300, cubic_bezier: [0.2, 0.0, 0.4, 1.0] },
      },
    });
    expect(patch.status).toBe(200);
    const get = await getToken("rt_motion");
    const env = await readEnvelope(get);
    expect(env.data.token_detail!.value_json).toMatchObject({
      curve: "cubic-bezier",
      duration_ms: 300,
    });
  });

  test("spacing: px mutates", async () => {
    const created = await postThenGet({
      slug: "rt_spacing",
      display_name: "Roundtrip spacing",
      token_detail: { token_kind: "spacing", value_json: { px: 8 } },
    });
    const patch = await patchToken("rt_spacing", {
      updated_at: created.updated_at,
      token_detail: { value_json: { px: 16 } },
    });
    expect(patch.status).toBe(200);
    const get = await getToken("rt_spacing");
    const env = await readEnvelope(get);
    expect(env.data.token_detail!.value_json).toMatchObject({ px: 16 });
  });

  test("semantic: cycle is rejected as validation error", async () => {
    const targetId = await seedBareTokenEntity("rt_target", "Target token");
    void targetId;

    const created = await postThenGet({
      slug: "rt_alias",
      display_name: "Roundtrip alias",
      token_detail: {
        token_kind: "semantic",
        value_json: { token_role: "surface" },
        semantic_target_entity_id: String(targetId),
      },
    });
    const aliasId = created.entity_id!;

    // Self-alias attempt → cycle gate rejects with 400.
    const patch = await patchToken("rt_alias", {
      updated_at: created.updated_at,
      token_detail: { semantic_target_entity_id: aliasId },
    });
    expect(patch.status).toBe(400);
    const body = (await patch.json()) as { error: string; code: string };
    expect(body.code).toBe("bad_request");
    expect(body.error).toMatch(/cycle/i);
  });
});
