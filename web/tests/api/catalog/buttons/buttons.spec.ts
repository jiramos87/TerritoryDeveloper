// TECH-1885 / Stage 8.1 — button list + create + detail PATCH round-trip.
//
// Covers GET /api/catalog/buttons, POST /api/catalog/buttons,
// GET /api/catalog/buttons/[slug], PATCH /api/catalog/buttons/[slug].
// Direct-invoke (no dev server); DB-backed; capability gate stubbed via
// session mock.

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
import {
  BUTTON_TEST_USER_ID,
  invokeButtonRoute,
  resetButtonTables,
  seedButtonTestUser,
  seedSpriteEntity,
  seedTokenEntity,
} from "./_harness";

const mockGetSession = getSessionUser as unknown as ReturnType<typeof vi.fn>;

beforeEach(async () => {
  await seedButtonTestUser();
  await resetButtonTables();
  mockGetSession.mockResolvedValue({
    id: BUTTON_TEST_USER_ID,
    email: "button-tests@example.com",
    role: "admin",
  });
}, 30000);

afterEach(async () => {
  await resetButtonTables();
  vi.clearAllMocks();
}, 30000);

async function postButton(body: unknown): Promise<Response> {
  const { POST } = await import("@/app/api/catalog/buttons/route");
  return invokeButtonRoute(POST, "POST", "/api/catalog/buttons", { body });
}

async function listButtons(qs = ""): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/buttons/route");
  return invokeButtonRoute(GET, "GET", `/api/catalog/buttons${qs}`);
}

async function getButton(slug: string): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/buttons/[slug]/route");
  return invokeButtonRoute(GET, "GET", `/api/catalog/buttons/${slug}`, {
    params: { slug },
  });
}

async function patchButton(slug: string, body: unknown): Promise<Response> {
  const { PATCH } = await import("@/app/api/catalog/buttons/[slug]/route");
  return invokeButtonRoute(PATCH, "PATCH", `/api/catalog/buttons/${slug}`, {
    body,
    params: { slug },
  });
}

describe("POST /api/catalog/buttons (TECH-1885)", () => {
  test("create_happy: inserts entity + button_detail in one tx", async () => {
    const res = await postButton({
      slug: "primary_btn",
      display_name: "Primary",
      tags: ["ui"],
      button_detail: { size_variant: "md", action_id: "submit_form" },
    });
    expect(res.status).toBe(201);
    const body = (await res.json()) as {
      ok: boolean;
      data: { entity_id: string; slug: string };
      audit_id: string | null;
    };
    expect(body.ok).toBe(true);
    expect(body.data.slug).toBe("primary_btn");
    expect(body.audit_id).toMatch(/^\d+$/);
  });

  test("create_validation_bad_slug: rejects invalid slug shape", async () => {
    const res = await postButton({ slug: "Bad-Slug!", display_name: "Bad" });
    expect(res.status).toBe(400);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("bad_request");
  });

  test("create_duplicate_slug: returns 409 unique_violation", async () => {
    await postButton({ slug: "dup_btn", display_name: "First" });
    const res = await postButton({ slug: "dup_btn", display_name: "Second" });
    expect(res.status).toBe(409);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("unique_violation");
  });

  test("create_validation_bad_size: rejects unknown size_variant", async () => {
    const res = await postButton({
      slug: "bad_size_btn",
      display_name: "Bad",
      button_detail: { size_variant: "xl" },
    });
    expect(res.status).toBe(400);
  });
});

describe("GET /api/catalog/buttons (TECH-1885)", () => {
  test("list_active_filter: returns only non-retired by default", async () => {
    await postButton({ slug: "active_btn", display_name: "Active" });
    await postButton({ slug: "retired_btn", display_name: "Retired" });
    const sql = (await import("@/lib/db/client")).getSql();
    await sql`update catalog_entity set retired_at = now() where slug = 'retired_btn'`;

    const res = await listButtons("?status=active");
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { items: Array<{ slug: string }>; next_cursor: string | null };
    };
    expect(body.ok).toBe(true);
    const slugs = body.data.items.map((i) => i.slug);
    expect(slugs).toContain("active_btn");
    expect(slugs).not.toContain("retired_btn");
  });

  test("list_bad_status: 400 for unknown filter", async () => {
    const res = await listButtons("?status=junk");
    expect(res.status).toBe(400);
  });
});

describe("PATCH /api/catalog/buttons/[slug] (TECH-1885)", () => {
  test("patch_round_trip: 6 sprite refs + 4 token refs + size + action + predicate persist", async () => {
    const spriteIdle = await seedSpriteEntity("sp_idle", "Idle");
    const spriteHover = await seedSpriteEntity("sp_hover", "Hover");
    const spritePressed = await seedSpriteEntity("sp_pressed", "Pressed");
    const spriteDisabled = await seedSpriteEntity("sp_disabled", "Disabled");
    const spriteIcon = await seedSpriteEntity("sp_icon", "Icon");
    const spriteBadge = await seedSpriteEntity("sp_badge", "Badge");
    const tokenPalette = await seedTokenEntity("tok_palette", "Palette");
    const tokenFrame = await seedTokenEntity("tok_frame", "Frame");
    const tokenFont = await seedTokenEntity("tok_font", "Font");
    const tokenIllum = await seedTokenEntity("tok_illum", "Illumination");

    await postButton({
      slug: "round_trip_btn",
      display_name: "Round Trip",
      button_detail: { size_variant: "sm" },
    });

    const getRes = await getButton("round_trip_btn");
    expect(getRes.status).toBe(200);
    const getBody = (await getRes.json()) as {
      ok: boolean;
      data: { updated_at: string };
    };
    const startedAt = getBody.data.updated_at;

    const patchRes = await patchButton("round_trip_btn", {
      updated_at: startedAt,
      display_name: "Round Trip Renamed",
      button_detail: {
        sprite_idle_entity_id: spriteIdle,
        sprite_hover_entity_id: spriteHover,
        sprite_pressed_entity_id: spritePressed,
        sprite_disabled_entity_id: spriteDisabled,
        sprite_icon_entity_id: spriteIcon,
        sprite_badge_entity_id: spriteBadge,
        token_palette_entity_id: tokenPalette,
        token_frame_style_entity_id: tokenFrame,
        token_font_entity_id: tokenFont,
        token_illumination_entity_id: tokenIllum,
        size_variant: "lg",
        action_id: "open_dialog",
        enable_predicate_json: { feat: "x" },
      },
    });
    expect(patchRes.status).toBe(200);
    const patchBody = (await patchRes.json()) as {
      ok: boolean;
      data: {
        display_name: string;
        button_detail: {
          sprite_idle_entity_id: string;
          sprite_badge_entity_id: string;
          token_palette_entity_id: string;
          token_illumination_entity_id: string;
          size_variant: string;
          action_id: string;
          enable_predicate_json: Record<string, unknown>;
        };
      };
      audit_id: string | null;
    };
    expect(patchBody.ok).toBe(true);
    expect(patchBody.audit_id).toMatch(/^\d+$/);
    expect(patchBody.data.display_name).toBe("Round Trip Renamed");
    expect(patchBody.data.button_detail.sprite_idle_entity_id).toBe(spriteIdle);
    expect(patchBody.data.button_detail.sprite_badge_entity_id).toBe(spriteBadge);
    expect(patchBody.data.button_detail.token_palette_entity_id).toBe(tokenPalette);
    expect(patchBody.data.button_detail.token_illumination_entity_id).toBe(tokenIllum);
    expect(patchBody.data.button_detail.size_variant).toBe("lg");
    expect(patchBody.data.button_detail.action_id).toBe("open_dialog");
    expect(patchBody.data.button_detail.enable_predicate_json).toEqual({ feat: "x" });
  });

  test("patch_stale_updated_at: returns 409 conflict with current row", async () => {
    await postButton({ slug: "stale_btn", display_name: "Stale" });
    const res = await patchButton("stale_btn", {
      updated_at: "1970-01-01T00:00:00.000Z",
      display_name: "Should Fail",
    });
    expect(res.status).toBe(409);
    const body = (await res.json()) as { error: string; code: string; current?: unknown };
    expect(body.code).toBe("conflict");
    expect(body.current).toBeDefined();
  });

  test("patch_unknown_field: returns 400 validation", async () => {
    await postButton({ slug: "unk_btn", display_name: "Unknown" });
    const get = await getButton("unk_btn");
    const getBody = (await get.json()) as { data: { updated_at: string } };
    const res = await patchButton("unk_btn", {
      updated_at: getBody.data.updated_at,
      bogus_field: "x",
    });
    expect(res.status).toBe(400);
  });
});
