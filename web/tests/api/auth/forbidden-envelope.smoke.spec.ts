import { describe, it, expect, vi, beforeEach } from "vitest";
import { forbiddenEnvelope } from "@/lib/auth/route-meta";

// Helper-shape unit (DEC-A48 canonical envelope).
describe("forbidden envelope (DEC-A48)", () => {
  it("matches the canonical shape", () => {
    const env = forbiddenEnvelope("catalog.entity.create", "viewer");
    expect(env.ok).toBe(false);
    expect(env.error.code).toBe("forbidden");
    expect(env.error.details.required).toBe("catalog.entity.create");
    expect(env.error.details.role).toBe("viewer");
    expect(typeof env.error.message).toBe("string");
  });
});

// Behavior smoke — proxy gate (Pass A surface).
vi.mock("next-auth/jwt", () => ({ getToken: vi.fn() }));
vi.mock("@/lib/auth/capabilities", () => ({
  loadCapabilitiesForRole: vi.fn(),
}));
vi.mock("@/lib/db/client", () => ({
  getSql: vi.fn(),
}));

import { getToken } from "next-auth/jwt";
import { loadCapabilitiesForRole } from "@/lib/auth/capabilities";
import { proxy } from "@/proxy";
import { NextRequest } from "next/server";

const mockGetToken = getToken as unknown as ReturnType<typeof vi.fn>;
const mockLoadCaps = loadCapabilitiesForRole as unknown as ReturnType<typeof vi.fn>;

function makeRequest(pathname: string, method: string): NextRequest {
  return new NextRequest(`http://localhost:4000${pathname}`, { method });
}

describe("proxy capability gate", () => {
  beforeEach(() => {
    mockGetToken.mockReset();
    mockLoadCaps.mockReset();
    delete process.env.NEXT_PUBLIC_AUTH_DEV_FALLBACK;
  });

  it("returns 401 when no session + dev fallback off", async () => {
    mockGetToken.mockResolvedValue(null);
    const res = await proxy(makeRequest("/api/catalog/assets", "GET"));
    expect(res.status).toBe(401);
    const body = await res.json();
    expect(body.ok).toBe(false);
    expect(body.error.code).toBe("forbidden");
    expect(body.error.details.required).toBe("catalog.entity.create");
    expect(body.error.details.role).toBe("<none>");
  });

  it("returns 403 with DEC-A48 envelope when role lacks capability", async () => {
    mockGetToken.mockResolvedValue({ uid: "u1", role: "viewer" });
    mockLoadCaps.mockResolvedValue(new Set(["audit.read"]));
    const res = await proxy(makeRequest("/api/catalog/assets", "POST"));
    expect(res.status).toBe(403);
    const body = await res.json();
    expect(body).toEqual({
      ok: false,
      error: {
        code: "forbidden",
        message: "Capability 'catalog.entity.create' required",
        details: { required: "catalog.entity.create", role: "viewer" },
      },
    });
  });

  it("passes through (NextResponse.next) when role has capability", async () => {
    mockGetToken.mockResolvedValue({ uid: "u1", role: "admin" });
    mockLoadCaps.mockResolvedValue(new Set(["catalog.entity.create"]));
    const res = await proxy(makeRequest("/api/catalog/assets", "POST"));
    expect(res.status).toBe(200);
  });

  it("matches dynamic [id] segments correctly", async () => {
    mockGetToken.mockResolvedValue({ uid: "u1", role: "viewer" });
    mockLoadCaps.mockResolvedValue(new Set(["audit.read"]));
    const res = await proxy(makeRequest("/api/catalog/assets/42/retire", "POST"));
    expect(res.status).toBe(403);
    const body = await res.json();
    expect(body.error.details.required).toBe("catalog.entity.retire");
  });

  it("denies routes without routeMeta entry (fail-closed)", async () => {
    mockGetToken.mockResolvedValue({ uid: "u1", role: "admin" });
    const res = await proxy(makeRequest("/api/catalog/unmapped", "GET"));
    expect(res.status).toBe(403);
    const body = await res.json();
    expect(body.error.details.required).toBe("<unknown>");
  });
});
