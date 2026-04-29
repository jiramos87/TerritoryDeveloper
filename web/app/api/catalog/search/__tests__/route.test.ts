/**
 * Search route unit tests (TECH-4180 §Test Blueprint).
 *
 * Mocks `search-query` and `audit/emitter` to test HTTP edge cases without DB.
 */

import { describe, expect, it, vi, beforeEach } from "vitest";

import { GET } from "@/app/api/catalog/search/route";

vi.mock("@/lib/catalog/search-query", () => ({
  VALID_KINDS: new Set([
    "sprite", "asset", "button", "panel", "pool", "token", "archetype", "audio",
  ]),
  DEFAULT_LIMIT: 20,
  MAX_LIMIT: 100,
  MIN_SCORE: 0.1,
  searchCatalogEntities: vi.fn().mockResolvedValue({ results: [], total: 0 }),
}));

vi.mock("@/lib/audit/emitter", () => ({
  audit: vi.fn().mockResolvedValue("1"),
}));

vi.mock("@/lib/db/client", () => ({
  getSql: vi.fn().mockReturnValue({}),
}));

function makeRequest(search: Record<string, string>): Request {
  const params = new URLSearchParams(search).toString();
  return new Request(`http://localhost/api/catalog/search?${params}`);
}

describe("GET /api/catalog/search", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns 400 when q is missing", async () => {
    const res = await GET(makeRequest({}) as never);
    expect(res.status).toBe(400);
    const body = await res.json();
    expect(body.code).toBe("bad_request");
  });

  it("returns 400 when q is blank", async () => {
    const res = await GET(makeRequest({ q: "   " }) as never);
    expect(res.status).toBe(400);
  });

  it("returns 400 on invalid kind", async () => {
    const res = await GET(makeRequest({ q: "hero", kind: "bad_kind" }) as never);
    expect(res.status).toBe(400);
    const body = await res.json();
    expect((body.details as { kind: string } | undefined)?.kind).toBe("bad_kind");
  });

  it("returns 400 on limit out of range", async () => {
    const res = await GET(makeRequest({ q: "hero", limit: "0" }) as never);
    expect(res.status).toBe(400);
  });

  it("returns 200 with empty results for valid q", async () => {
    const res = await GET(makeRequest({ q: "hero" }) as never);
    expect(res.status).toBe(200);
    const body = await res.json();
    expect(body.ok).toBe(true);
    expect(body.data.results).toEqual([]);
    expect(body.data.total).toBe(0);
  });

  it("accepts valid kind filter", async () => {
    const res = await GET(makeRequest({ q: "hero", kind: "sprite" }) as never);
    expect(res.status).toBe(200);
  });

  it("accepts limit=1", async () => {
    const res = await GET(makeRequest({ q: "hero", limit: "1" }) as never);
    expect(res.status).toBe(200);
  });

  it("returns 400 on limit > MAX_LIMIT", async () => {
    const res = await GET(makeRequest({ q: "hero", limit: "101" }) as never);
    expect(res.status).toBe(400);
  });
});
