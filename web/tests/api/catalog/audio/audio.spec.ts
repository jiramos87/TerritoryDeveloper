// TECH-8608 / Stage 19.1 — Audio list + detail integration suite.
//
// Audio surface: GET /api/catalog/audio (list), GET /api/catalog/audio/[slug]
// (detail). POST /promote scoped out of this suite (lint-rule + filesystem
// dependency lives in Stage 9.1 promote fixture). Pre-seed audio_detail rows
// directly via SQL (no POST endpoint exists for create).

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
import {
  AUDIO_TEST_USER_ID,
  invokeAudioRoute,
  resetAudioTables,
  seedAudioEntity,
  seedAudioTestUser,
} from "./_harness";

const mockGetSession = getSessionUser as unknown as ReturnType<typeof vi.fn>;

beforeEach(async () => {
  await seedAudioTestUser();
  await resetAudioTables();
  mockGetSession.mockResolvedValue({
    id: AUDIO_TEST_USER_ID,
    email: "audio-tests@example.com",
    role: "admin",
  });
}, 30000);

afterEach(async () => {
  await resetAudioTables();
  vi.clearAllMocks();
}, 30000);

async function listAudio(qs = ""): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/audio/route");
  return invokeAudioRoute(GET, "GET", `/api/catalog/audio${qs}`);
}

async function getAudio(slug: string): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/audio/[slug]/route");
  return invokeAudioRoute(GET, "GET", `/api/catalog/audio/${slug}`, {
    params: { slug },
  });
}

describe("GET /api/catalog/audio (TECH-8608)", () => {
  test("list_active_filter: returns only non-retired by default", async () => {
    await seedAudioEntity({ slug: "a_active", display_name: "Active" });
    await seedAudioEntity({ slug: "a_retired", display_name: "Retired", retired: true });
    const res = await listAudio("?status=active");
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { items: Array<{ slug: string }>; next_cursor: string | null };
    };
    expect(body.ok).toBe(true);
    const slugs = body.data.items.map((i) => i.slug);
    expect(slugs).toContain("a_active");
    expect(slugs).not.toContain("a_retired");
  });

  test("list_retired_filter: returns retired only", async () => {
    await seedAudioEntity({ slug: "a_keep", display_name: "Keep" });
    await seedAudioEntity({ slug: "a_gone", display_name: "Gone", retired: true });
    const res = await listAudio("?status=retired");
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { items: Array<{ slug: string }> };
    };
    const slugs = body.data.items.map((i) => i.slug);
    expect(slugs).toEqual(["a_gone"]);
  });

  test("list_bad_status: 400 for unknown filter", async () => {
    const res = await listAudio("?status=junk");
    expect(res.status).toBe(400);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("bad_request");
  });
});

describe("GET /api/catalog/audio/[slug] (TECH-8608)", () => {
  test("detail_happy: returns audio_detail joined fields", async () => {
    await seedAudioEntity({
      slug: "a_detail",
      display_name: "Detail",
      duration_ms: 2400,
      sample_rate: 48000,
      channels: 2,
      loudness_lufs: -16.5,
      peak_db: -2.1,
    });
    const res = await getAudio("a_detail");
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: {
        slug: string;
        duration_ms: number;
        loudness_lufs: number | null;
        peak_db: number | null;
      };
    };
    expect(body.ok).toBe(true);
    expect(body.data.slug).toBe("a_detail");
    expect(body.data.duration_ms).toBe(2400);
    expect(body.data.loudness_lufs).toBeCloseTo(-16.5, 1);
  });

  test("detail_not_found: returns 404 for missing slug", async () => {
    const res = await getAudio("missing_audio_slug");
    expect(res.status).toBe(404);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("not_found");
  });
});
