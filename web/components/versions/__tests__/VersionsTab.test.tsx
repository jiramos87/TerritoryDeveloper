/**
 * VersionsTab container helper tests (TECH-3223 / Stage 14.2).
 *
 * Container fetch / cursor / state plumbing is exposed as pure helpers
 * (`buildVersionsUrl`, `unwrapVersionsResponse`) for unit coverage — Vitest
 * runs in node env (no jsdom), so React-side useEffect / useState behavior is
 * exercised end-to-end via the integration page-wire tests + dev runtime.
 *
 * Pure helpers cover the load-bearing edges:
 *  - URL shape (initial vs. cursor pages, encoded cursor).
 *  - Envelope validation: HTTP not-ok, ok:false, missing data, success.
 *
 * @see web/components/versions/VersionsTab.tsx
 */
import { describe, expect, it } from "vitest";

import {
  buildVersionsUrl,
  PAGE_SIZE,
  unwrapVersionsResponse,
} from "@/components/versions/VersionsTab";

describe("buildVersionsUrl", () => {
  it("emits limit only when cursor null", () => {
    expect(buildVersionsUrl("sprite", "42", null)).toBe(
      `/api/catalog/sprite/42/versions?limit=${PAGE_SIZE}`,
    );
  });
  it("includes cursor query when non-null", () => {
    expect(buildVersionsUrl("asset", "9", "abc123")).toBe(
      `/api/catalog/asset/9/versions?limit=${PAGE_SIZE}&cursor=abc123`,
    );
  });
  it("URL-encodes cursor with reserved chars", () => {
    const cursor = "abc=/+";
    const url = buildVersionsUrl("pool", "5", cursor);
    expect(url).toContain("cursor=abc%3D%2F%2B");
  });
  it("supports archetype kind path segment", () => {
    expect(buildVersionsUrl("archetype", "100", null)).toBe(
      `/api/catalog/archetype/100/versions?limit=${PAGE_SIZE}`,
    );
  });
  it("respects custom pageSize override", () => {
    expect(buildVersionsUrl("sprite", "1", null, 50)).toBe(
      "/api/catalog/sprite/1/versions?limit=50",
    );
  });
});

describe("unwrapVersionsResponse", () => {
  it("returns data on httpOk + ok:true + data present", () => {
    const out = unwrapVersionsResponse(true, {
      ok: true,
      data: { rows: [], nextCursor: null },
    });
    expect(out.ok).toBe(true);
    if (out.ok) {
      expect(out.data.rows).toEqual([]);
      expect(out.data.nextCursor).toBeNull();
    }
  });

  it("rejects when httpOk false", () => {
    const out = unwrapVersionsResponse(false, {
      ok: false,
      error: "Bad request",
    });
    expect(out.ok).toBe(false);
    if (!out.ok) expect(out.error).toBe("Bad request");
  });

  it("rejects when ok:false in envelope", () => {
    const out = unwrapVersionsResponse(true, {
      ok: false,
      error: "internal",
    });
    expect(out.ok).toBe(false);
    if (!out.ok) expect(out.error).toBe("internal");
  });

  it("rejects when data missing", () => {
    const out = unwrapVersionsResponse(true, { ok: true });
    expect(out.ok).toBe(false);
    if (!out.ok) expect(out.error).toMatch(/Failed to load/);
  });

  it("falls back to default error when no message provided", () => {
    const out = unwrapVersionsResponse(false, { ok: false });
    expect(out.ok).toBe(false);
    if (!out.ok) expect(out.error).toMatch(/Failed to load/);
  });
});
