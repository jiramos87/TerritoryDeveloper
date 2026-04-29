/**
 * RefsTab container helper tests (TECH-3409 / Stage 14.4).
 *
 * Container fetch / cursor / state plumbing is exposed as pure helpers
 * (`buildRefsUrl`, `unwrapRefsResponse`) for unit coverage.
 *
 * Pure helpers cover:
 *  - URL shape per side (incoming / outgoing) + cursor encoding + custom limit.
 *  - Envelope validation: HTTP not-ok, ok:false, missing data, success.
 *
 * @see web/components/refs/RefsTab.tsx
 */
import { describe, expect, it } from "vitest";

import {
  buildRefsUrl,
  PAGE_SIZE,
  unwrapRefsResponse,
} from "@/components/refs/RefsTab";

describe("buildRefsUrl", () => {
  it("emits limit + side when cursor null (incoming)", () => {
    expect(buildRefsUrl("token", "42", null, "incoming")).toBe(
      `/api/catalog/token/42/refs?limit=${PAGE_SIZE}&side=incoming`,
    );
  });

  it("emits limit + side when cursor null (outgoing)", () => {
    expect(buildRefsUrl("panel", "9", null, "outgoing")).toBe(
      `/api/catalog/panel/9/refs?limit=${PAGE_SIZE}&side=outgoing`,
    );
  });

  it("includes cursor query when non-null", () => {
    expect(buildRefsUrl("asset", "9", "abc123", "incoming")).toBe(
      `/api/catalog/asset/9/refs?limit=${PAGE_SIZE}&side=incoming&cursor=abc123`,
    );
  });

  it("URL-encodes cursor with reserved chars", () => {
    const cursor = "abc=/+";
    const url = buildRefsUrl("pool", "5", cursor, "outgoing");
    expect(url).toContain("cursor=abc%3D%2F%2B");
  });

  it("supports archetype kind path segment", () => {
    expect(buildRefsUrl("archetype", "100", null, "incoming")).toBe(
      `/api/catalog/archetype/100/refs?limit=${PAGE_SIZE}&side=incoming`,
    );
  });

  it("respects custom pageSize override", () => {
    expect(buildRefsUrl("token", "1", null, "outgoing", 50)).toBe(
      "/api/catalog/token/1/refs?limit=50&side=outgoing",
    );
  });
});

describe("unwrapRefsResponse", () => {
  it("returns data on httpOk + ok:true + data present", () => {
    const out = unwrapRefsResponse(true, {
      ok: true,
      data: {
        incoming: { rows: [], nextCursor: null },
        outgoing: { rows: [], nextCursor: null },
      },
    });
    expect(out.ok).toBe(true);
    if (out.ok) {
      expect(out.data.incoming.rows).toEqual([]);
      expect(out.data.outgoing.rows).toEqual([]);
    }
  });

  it("rejects when httpOk false", () => {
    const out = unwrapRefsResponse(false, {
      ok: false,
      error: "Bad request",
    });
    expect(out.ok).toBe(false);
    if (!out.ok) expect(out.error).toBe("Bad request");
  });

  it("rejects when ok:false in envelope", () => {
    const out = unwrapRefsResponse(true, {
      ok: false,
      error: "internal",
    });
    expect(out.ok).toBe(false);
    if (!out.ok) expect(out.error).toBe("internal");
  });

  it("rejects when data missing", () => {
    const out = unwrapRefsResponse(true, { ok: true });
    expect(out.ok).toBe(false);
    if (!out.ok) expect(out.error).toMatch(/Failed to load/);
  });

  it("falls back to default error when no message provided", () => {
    const out = unwrapRefsResponse(false, { ok: false });
    expect(out.ok).toBe(false);
    if (!out.ok) expect(out.error).toMatch(/Failed to load/);
  });
});
