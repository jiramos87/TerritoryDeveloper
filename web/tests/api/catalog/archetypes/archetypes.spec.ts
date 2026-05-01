// TECH-8608 / Stage 19.1 — Archetype list + create + detail PATCH +
// version publish + retire round-trip.

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
import {
  ARCHETYPE_TEST_USER_ID,
  invokeArchetypeRoute,
  resetArchetypeTables,
  seedArchetypeTestUser,
} from "./_harness";

const mockGetSession = getSessionUser as unknown as ReturnType<typeof vi.fn>;

beforeEach(async () => {
  await seedArchetypeTestUser();
  await resetArchetypeTables();
  mockGetSession.mockResolvedValue({
    id: ARCHETYPE_TEST_USER_ID,
    email: "archetype-tests@example.com",
    role: "admin",
  });
}, 30000);

afterEach(async () => {
  await resetArchetypeTables();
  vi.clearAllMocks();
}, 30000);

async function postArchetype(body: unknown): Promise<Response> {
  const { POST } = await import("@/app/api/catalog/archetypes/route");
  return invokeArchetypeRoute(POST, "POST", "/api/catalog/archetypes", { body });
}

async function listArchetypes(qs = ""): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/archetypes/route");
  return invokeArchetypeRoute(GET, "GET", `/api/catalog/archetypes${qs}`);
}

async function getArchetype(slug: string): Promise<Response> {
  const { GET } = await import("@/app/api/catalog/archetypes/[slug]/route");
  return invokeArchetypeRoute(GET, "GET", `/api/catalog/archetypes/${slug}`, {
    params: { slug },
  });
}

async function patchArchetype(slug: string, body: unknown): Promise<Response> {
  const { PATCH } = await import("@/app/api/catalog/archetypes/[slug]/route");
  return invokeArchetypeRoute(PATCH, "PATCH", `/api/catalog/archetypes/${slug}`, {
    body,
    params: { slug },
  });
}

async function listVersions(slug: string): Promise<Response> {
  const { GET } = await import(
    "@/app/api/catalog/archetypes/[slug]/versions/route"
  );
  return invokeArchetypeRoute(
    GET,
    "GET",
    `/api/catalog/archetypes/${slug}/versions`,
    { params: { slug } },
  );
}

async function publishVersion(slug: string, versionId: string): Promise<Response> {
  const { POST } = await import(
    "@/app/api/catalog/archetypes/[slug]/versions/[versionId]/publish/route"
  );
  return invokeArchetypeRoute(
    POST,
    "POST",
    `/api/catalog/archetypes/${slug}/versions/${versionId}/publish`,
    { params: { slug, versionId } },
  );
}

async function retireArchetype(slug: string): Promise<Response> {
  const { POST } = await import(
    "@/app/api/catalog/archetypes/[slug]/retire/route"
  );
  return invokeArchetypeRoute(
    POST,
    "POST",
    `/api/catalog/archetypes/${slug}/retire`,
    { params: { slug } },
  );
}

describe("POST /api/catalog/archetypes (TECH-8608)", () => {
  test("create_happy: inserts entity + initial draft version", async () => {
    const res = await postArchetype({
      slug: "house_arch",
      display_name: "House Archetype",
      kind_tag: "house",
      initial_params: { type: "object", properties: {} },
    });
    expect(res.status).toBe(201);
    const body = (await res.json()) as {
      ok: boolean;
      data: { entity_id: string; slug: string };
      audit_id: string | null;
    };
    expect(body.ok).toBe(true);
    expect(body.data.slug).toBe("house_arch");
    expect(body.audit_id).toMatch(/^\d+$/);
  });

  test("create_validation_bad_slug: rejects invalid slug shape", async () => {
    const res = await postArchetype({
      slug: "Bad-Arch!",
      display_name: "Bad",
    });
    expect(res.status).toBe(400);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("bad_request");
  });

  test("create_duplicate_slug: returns 409 unique_violation", async () => {
    await postArchetype({ slug: "dup_arch", display_name: "First" });
    const res = await postArchetype({ slug: "dup_arch", display_name: "Second" });
    expect(res.status).toBe(409);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("unique_violation");
  });
});

describe("GET /api/catalog/archetypes (TECH-8608)", () => {
  test("list_active_filter: returns only non-retired", async () => {
    await postArchetype({ slug: "active_arch", display_name: "Active" });
    await postArchetype({ slug: "retired_arch", display_name: "Retired" });
    const sql = (await import("@/lib/db/client")).getSql();
    await sql`update catalog_entity set retired_at = now() where slug = 'retired_arch'`;
    const res = await listArchetypes("?status=active");
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { items: Array<{ slug: string }>; next_cursor: string | null };
    };
    const slugs = body.data.items.map((i) => i.slug);
    expect(slugs).toContain("active_arch");
    expect(slugs).not.toContain("retired_arch");
  });
});

describe("PATCH /api/catalog/archetypes/[slug] (TECH-8608)", () => {
  test("patch_display_name: round-trips display_name", async () => {
    await postArchetype({
      slug: "rename_arch",
      display_name: "Original",
    });
    const cur = await getArchetype("rename_arch");
    const curBody = (await cur.json()) as { data: { updated_at: string } };
    const res = await patchArchetype("rename_arch", {
      updated_at: curBody.data.updated_at,
      display_name: "Renamed",
    });
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { display_name: string };
      audit_id: string | null;
    };
    expect(body.ok).toBe(true);
    expect(body.data.display_name).toBe("Renamed");
  });

  test("patch_stale_updated_at: returns 409 conflict", async () => {
    await postArchetype({ slug: "stale_arch", display_name: "Stale" });
    const res = await patchArchetype("stale_arch", {
      updated_at: "1970-01-01T00:00:00.000Z",
      display_name: "Should Fail",
    });
    expect(res.status).toBe(409);
  });
});

describe("Version publish + retire (TECH-8608)", () => {
  test("publish_first_version: flips draft -> published", async () => {
    await postArchetype({
      slug: "pub_arch",
      display_name: "Pub",
      initial_params: { type: "object", properties: {} },
    });
    const versionsRes = await listVersions("pub_arch");
    expect(versionsRes.status).toBe(200);
    const versionsBody = (await versionsRes.json()) as {
      data: { items: Array<{ version_id: string; status: string }> };
    };
    expect(versionsBody.data.items.length).toBeGreaterThan(0);
    const draft = versionsBody.data.items.find((v) => v.status === "draft");
    expect(draft).toBeDefined();
    const pubRes = await publishVersion("pub_arch", draft!.version_id);
    expect(pubRes.status).toBe(200);
    const pubBody = (await pubRes.json()) as {
      ok: boolean;
      data: { version: { status: string } };
    };
    expect(pubBody.ok).toBe(true);
    expect(pubBody.data.version.status).toBe("published");
  });

  test("retire_archetype: soft-retires entity", async () => {
    await postArchetype({ slug: "retire_arch", display_name: "Retire" });
    const res = await retireArchetype("retire_arch");
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { retired_at: string | null };
    };
    expect(body.ok).toBe(true);
    expect(body.data.retired_at).not.toBeNull();
  });
});
