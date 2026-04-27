// TECH-1888 / Stage 8.1 — panel list + create + detail PATCH + DELETE round-trip.
//
// Direct-invoke (no dev server); DB-backed; capability gate stubbed via
// session mock. Mirrors `buttons.spec.ts` shape.

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
import {
  PANEL_TEST_USER_ID,
  invokePanelRoute,
  resetPanelTables,
  seedArchetypeEntity,
  seedPanelTestUser,
} from "./_harness";

const mockGetSession = getSessionUser as unknown as ReturnType<typeof vi.fn>;

beforeEach(async () => {
  await seedPanelTestUser();
  await resetPanelTables();
  mockGetSession.mockResolvedValue({
    id: PANEL_TEST_USER_ID,
    email: "panel-tests@example.com",
    role: "admin",
  });
}, 30000);

afterEach(async () => {
  await resetPanelTables();
  vi.clearAllMocks();
}, 30000);

async function postPanel(body: unknown): Promise<Response> {
  const { POST } = await import("@/app/api/catalog/panels/route");
  return invokePanelRoute(POST, "POST", "/api/catalog/panels", { body });
}

async function listPanels(qs = ""): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/panels/route");
  return invokePanelRoute(GET, "GET", `/api/catalog/panels${qs}`);
}

async function getPanel(slug: string): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/panels/[slug]/route");
  return invokePanelRoute(GET, "GET", `/api/catalog/panels/${slug}`, {
    params: { slug },
  });
}

async function patchPanel(slug: string, body: unknown): Promise<Response> {
  const { PATCH } = await import("@/app/api/catalog/panels/[slug]/route");
  return invokePanelRoute(PATCH, "PATCH", `/api/catalog/panels/${slug}`, {
    body,
    params: { slug },
  });
}

async function deletePanel(slug: string, body: unknown): Promise<Response> {
  const { DELETE } = await import("@/app/api/catalog/panels/[slug]/route");
  return invokePanelRoute(DELETE, "DELETE", `/api/catalog/panels/${slug}`, {
    body,
    params: { slug },
  });
}

describe("POST /api/catalog/panels (TECH-1888)", () => {
  test("create_happy: inserts entity + panel_detail in one tx", async () => {
    const res = await postPanel({
      slug: "primary_panel",
      display_name: "Primary",
      tags: ["ui"],
      panel_detail: { layout_template: "vstack", modal: false },
    });
    expect(res.status).toBe(201);
    const body = (await res.json()) as {
      ok: boolean;
      data: { entity_id: string; slug: string };
      audit_id: string | null;
    };
    expect(body.ok).toBe(true);
    expect(body.data.slug).toBe("primary_panel");
    expect(body.audit_id).toMatch(/^\d+$/);
  });

  test("create_validation_bad_slug: rejects invalid slug shape", async () => {
    const res = await postPanel({ slug: "Bad-Slug!", display_name: "Bad" });
    expect(res.status).toBe(400);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("bad_request");
  });

  test("create_duplicate_slug: returns 409 unique_violation", async () => {
    await postPanel({ slug: "dup_panel", display_name: "First" });
    const res = await postPanel({ slug: "dup_panel", display_name: "Second" });
    expect(res.status).toBe(409);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("unique_violation");
  });

  test("create_validation_bad_layout: rejects unknown layout_template", async () => {
    const res = await postPanel({
      slug: "bad_layout",
      display_name: "Bad",
      panel_detail: { layout_template: "spiral" },
    });
    expect(res.status).toBe(400);
  });
});

describe("GET /api/catalog/panels (TECH-1888)", () => {
  test("list_active_filter: returns only non-retired by default", async () => {
    await postPanel({ slug: "active_panel", display_name: "Active" });
    await postPanel({ slug: "retired_panel", display_name: "Retired" });
    const sql = (await import("@/lib/db/client")).getSql();
    await sql`update catalog_entity set retired_at = now() where slug = 'retired_panel'`;

    const res = await listPanels("?status=active");
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { items: Array<{ slug: string }>; next_cursor: string | null };
    };
    expect(body.ok).toBe(true);
    const slugs = body.data.items.map((i) => i.slug);
    expect(slugs).toContain("active_panel");
    expect(slugs).not.toContain("retired_panel");
  });

  test("list_bad_status: 400 for unknown filter", async () => {
    const res = await listPanels("?status=junk");
    expect(res.status).toBe(400);
  });
});

describe("PATCH /api/catalog/panels/[slug] (TECH-1888)", () => {
  test("patch_round_trip: archetype + display_name + layout persist", async () => {
    const archetypeId = await seedArchetypeEntity("arc_basic", "Basic", {
      body: { accepts_kind: ["button"], min: 0, max: 4 },
    });

    await postPanel({
      slug: "round_trip_panel",
      display_name: "Round Trip",
      panel_detail: { layout_template: "vstack", modal: false },
    });

    const getRes = await getPanel("round_trip_panel");
    expect(getRes.status).toBe(200);
    const getBody = (await getRes.json()) as { data: { updated_at: string } };
    const startedAt = getBody.data.updated_at;

    const patchRes = await patchPanel("round_trip_panel", {
      updated_at: startedAt,
      display_name: "Round Trip Renamed",
      panel_detail: {
        archetype_entity_id: String(archetypeId),
        layout_template: "hstack",
        modal: true,
      },
    });
    expect(patchRes.status).toBe(200);
    const patchBody = (await patchRes.json()) as {
      ok: boolean;
      data: {
        display_name: string;
        panel_detail: {
          archetype_entity_id: string;
          layout_template: string;
          modal: boolean;
        };
      };
      audit_id: string | null;
    };
    expect(patchBody.ok).toBe(true);
    expect(patchBody.audit_id).toMatch(/^\d+$/);
    expect(patchBody.data.display_name).toBe("Round Trip Renamed");
    expect(patchBody.data.panel_detail.archetype_entity_id).toBe(String(archetypeId));
    expect(patchBody.data.panel_detail.layout_template).toBe("hstack");
    expect(patchBody.data.panel_detail.modal).toBe(true);
  });

  test("patch_stale_updated_at: returns 409 conflict with current row", async () => {
    await postPanel({ slug: "stale_panel", display_name: "Stale" });
    const res = await patchPanel("stale_panel", {
      updated_at: "1970-01-01T00:00:00.000Z",
      display_name: "Should Fail",
    });
    expect(res.status).toBe(409);
    const body = (await res.json()) as { error: string; code: string; current?: unknown };
    expect(body.code).toBe("conflict");
    expect(body.current).toBeDefined();
  });

  test("patch_unknown_field: returns 400 validation", async () => {
    await postPanel({ slug: "unk_panel", display_name: "Unknown" });
    const get = await getPanel("unk_panel");
    const getBody = (await get.json()) as { data: { updated_at: string } };
    const res = await patchPanel("unk_panel", {
      updated_at: getBody.data.updated_at,
      bogus_field: "x",
    });
    expect(res.status).toBe(400);
  });
});

describe("DELETE /api/catalog/panels/[slug] (TECH-1888)", () => {
  test("delete_round_trip: sets retired_at + retains row", async () => {
    await postPanel({ slug: "retire_panel", display_name: "Retire" });
    const get = await getPanel("retire_panel");
    const getBody = (await get.json()) as { data: { updated_at: string } };

    const res = await deletePanel("retire_panel", { updated_at: getBody.data.updated_at });
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { retired_at: string | null };
      audit_id: string | null;
    };
    expect(body.ok).toBe(true);
    expect(body.data.retired_at).toBeTruthy();
    expect(body.audit_id).toMatch(/^\d+$/);
  });

  test("delete_stale_updated_at: returns 409 conflict", async () => {
    await postPanel({ slug: "retire_stale", display_name: "Stale" });
    const res = await deletePanel("retire_stale", {
      updated_at: "1970-01-01T00:00:00.000Z",
    });
    expect(res.status).toBe(409);
  });

  test("delete_unknown: returns 404 not_found", async () => {
    const res = await deletePanel("ghost_panel", {
      updated_at: "1970-01-01T00:00:00.000Z",
    });
    expect(res.status).toBe(404);
  });
});
