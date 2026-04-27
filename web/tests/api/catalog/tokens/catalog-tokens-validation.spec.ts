// TECH-2092 / Stage 10.1 — token POST per-kind validation envelope.
//
// Covers POST /api/catalog/tokens for the 5 kinds: each happy path returns 201
// with `ok=true` envelope; each shape mismatch returns 400 with code `validation`
// (mapped to `bad_request` per DEC-A48 envelope keys).

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

describe("POST /api/catalog/tokens — happy paths (TECH-2092)", () => {
  test("color hex 201", async () => {
    const res = await postToken({
      slug: "tok_color_hex",
      display_name: "Color Hex",
      token_detail: { token_kind: "color", value_json: { hex: "#abcdef" } },
    });
    expect(res.status).toBe(201);
    const body = (await res.json()) as { ok: boolean; data: { slug: string } };
    expect(body.ok).toBe(true);
    expect(body.data.slug).toBe("tok_color_hex");
  });

  test("color hsl 201", async () => {
    const res = await postToken({
      slug: "tok_color_hsl",
      display_name: "Color HSL",
      token_detail: { token_kind: "color", value_json: { h: 200, s: 50, l: 60 } },
    });
    expect(res.status).toBe(201);
  });

  test("type-scale 201", async () => {
    const res = await postToken({
      slug: "tok_type",
      display_name: "Type",
      token_detail: {
        token_kind: "type-scale",
        value_json: { font_family: "Inter", size_px: 14, line_height: 1.4 },
      },
    });
    expect(res.status).toBe(201);
  });

  test("motion 201", async () => {
    const res = await postToken({
      slug: "tok_motion",
      display_name: "Motion",
      token_detail: {
        token_kind: "motion",
        value_json: { curve: "ease-out", duration_ms: 150 },
      },
    });
    expect(res.status).toBe(201);
  });

  test("spacing 201", async () => {
    const res = await postToken({
      slug: "tok_spacing",
      display_name: "Spacing",
      token_detail: { token_kind: "spacing", value_json: { px: 12 } },
    });
    expect(res.status).toBe(201);
  });

  test("semantic 201 with target", async () => {
    // First create a base color token to alias.
    await postToken({
      slug: "tok_base_color",
      display_name: "Base",
      token_detail: { token_kind: "color", value_json: { hex: "#001122" } },
    });
    // Read its id back via list endpoint.
    const { GET: ListGET } = await import("@/app/api/catalog/tokens/route");
    const listRes = await invokeTokenRoute(ListGET, "GET", "/api/catalog/tokens");
    const listBody = (await listRes.json()) as {
      data: { items: Array<{ slug: string; entity_id: string }> };
    };
    const target = listBody.data.items.find((i) => i.slug === "tok_base_color");
    expect(target).toBeDefined();

    const res = await postToken({
      slug: "tok_sem",
      display_name: "Semantic",
      token_detail: {
        token_kind: "semantic",
        value_json: { token_role: "primary" },
        semantic_target_entity_id: target!.entity_id,
      },
    });
    expect(res.status).toBe(201);
  });
});

describe("POST /api/catalog/tokens — validation rejects (TECH-2092)", () => {
  test("color bad hex 400", async () => {
    const res = await postToken({
      slug: "tok_bad_color",
      display_name: "Bad",
      token_detail: { token_kind: "color", value_json: { hex: "ZZZ" } },
    });
    expect(res.status).toBe(400);
    const body = (await res.json()) as { code: string };
    expect(body.code).toBe("bad_request");
  });

  test("type-scale missing size_px 400", async () => {
    const res = await postToken({
      slug: "tok_bad_type",
      display_name: "Bad",
      token_detail: {
        token_kind: "type-scale",
        value_json: { font_family: "Inter", line_height: 1.4 },
      },
    });
    expect(res.status).toBe(400);
  });

  test("motion bad curve 400", async () => {
    const res = await postToken({
      slug: "tok_bad_motion",
      display_name: "Bad",
      token_detail: {
        token_kind: "motion",
        value_json: { curve: "wobble", duration_ms: 100 },
      },
    });
    expect(res.status).toBe(400);
  });

  test("spacing negative px 400", async () => {
    const res = await postToken({
      slug: "tok_bad_spacing",
      display_name: "Bad",
      token_detail: { token_kind: "spacing", value_json: { px: -1 } },
    });
    expect(res.status).toBe(400);
  });

  test("semantic without target 400", async () => {
    const res = await postToken({
      slug: "tok_bad_sem",
      display_name: "Bad",
      token_detail: { token_kind: "semantic", value_json: { token_role: "x" } },
    });
    expect(res.status).toBe(400);
  });

  test("non-semantic kind with target 400", async () => {
    const targetId = await seedBareTokenEntity("dummy_target", "Dummy");
    const res = await postToken({
      slug: "tok_bad_color_with_target",
      display_name: "Bad",
      token_detail: {
        token_kind: "color",
        value_json: { hex: "#abcdef" },
        semantic_target_entity_id: targetId,
      },
    });
    expect(res.status).toBe(400);
  });

  test("unknown token_kind 400", async () => {
    const res = await postToken({
      slug: "tok_bad_kind",
      display_name: "Bad",
      token_detail: { token_kind: "shadow", value_json: {} },
    });
    expect(res.status).toBe(400);
  });
});
