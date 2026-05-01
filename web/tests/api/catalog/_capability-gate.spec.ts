// TECH-8608 / Stage 19.1 — Capability gate smoke for new-kind catalog routes.
//
// Confirms the proxy capability gate fires for kinds added in Stage 19.1
// where the proxy route-meta-map already wires them (pools). Archetype +
// assets-spine paths are not yet in the route-meta-map (pre-existing gap —
// not in Stage 19.1 scope per task §Out-of-Scope). Mirrors the
// forbidden-envelope.smoke.spec.ts shape — single 403 envelope assertion per
// representative POST/PATCH surface to prove the gate fires for the new kinds
// that ARE wired, without duplicating the full proxy spec.

import { describe, it, expect, vi, beforeEach } from "vitest";

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

describe("capability gate — Stage 19.1 new kinds (TECH-8608)", () => {
  beforeEach(() => {
    mockGetToken.mockReset();
    mockLoadCaps.mockReset();
    delete process.env.NEXT_PUBLIC_AUTH_DEV_FALLBACK;
  });

  it("blocks POST /api/catalog/pools for viewer with 403 envelope", async () => {
    mockGetToken.mockResolvedValue({ uid: "u1", role: "viewer" });
    mockLoadCaps.mockResolvedValue(new Set(["audit.read"]));
    const res = await proxy(makeRequest("/api/catalog/pools", "POST"));
    expect(res.status).toBe(403);
    const body = await res.json();
    expect(body.ok).toBe(false);
    expect(body.error.code).toBe("forbidden");
    expect(body.error.details.required).toBe("catalog.entity.create");
    expect(body.error.details.role).toBe("viewer");
  });

  it("blocks PATCH /api/catalog/pools/[slug] for viewer with 403 envelope", async () => {
    mockGetToken.mockResolvedValue({ uid: "u1", role: "viewer" });
    mockLoadCaps.mockResolvedValue(new Set(["audit.read"]));
    const res = await proxy(makeRequest("/api/catalog/pools/some_pool", "PATCH"));
    expect(res.status).toBe(403);
    const body = await res.json();
    expect(body.error.details.required).toBe("catalog.entity.edit");
  });
});
