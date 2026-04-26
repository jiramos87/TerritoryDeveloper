import { describe, it, expect, vi } from "vitest";
import { NextResponse, type NextRequest } from "next/server";

import {
  formatFingerprint,
  parseFingerprint,
  withOptimisticConcurrency,
  type AuditEmitter,
} from "@/lib/optimistic-concurrency";

/**
 * Minimal NextRequest stub. We only exercise the surface the decorator reads:
 * `headers.get`, `clone`, and `json`. Casting to `NextRequest` keeps the
 * decorator type contract intact without spinning up the full edge runtime.
 */
type StubInit = {
  headers?: Record<string, string>;
  body?: unknown;
};

function stubRequest(init: StubInit = {}): NextRequest {
  const headers = new Map(Object.entries(init.headers ?? {}).map(([k, v]) => [k.toLowerCase(), v]));
  const bodyText = init.body !== undefined ? JSON.stringify(init.body) : null;

  const req = {
    headers: {
      get(name: string) {
        return headers.get(name.toLowerCase()) ?? null;
      },
    },
    clone() {
      return req;
    },
    async json() {
      if (bodyText === null) throw new Error("no body");
      return JSON.parse(bodyText);
    },
  } as unknown as NextRequest;

  return req;
}

const ISO = "2026-04-26T12:00:00.000Z";
const ISO_OTHER = "2026-04-26T12:00:00.001Z";

describe("parseFingerprint / formatFingerprint", () => {
  it("round-trips an ISO 8601 timestamp", () => {
    const d = new Date(ISO);
    const formatted = formatFingerprint(d);
    expect(formatted).toBe(ISO);
    expect(parseFingerprint(formatted)?.toISOString()).toBe(ISO);
  });

  it("strips ETag-style surrounding quotes", () => {
    expect(parseFingerprint(`"${ISO}"`)?.toISOString()).toBe(ISO);
  });

  it("returns null for empty / invalid input", () => {
    expect(parseFingerprint(null)).toBeNull();
    expect(parseFingerprint("")).toBeNull();
    expect(parseFingerprint("not a date")).toBeNull();
  });
});

describe("withOptimisticConcurrency", () => {
  it("invokes the wrapped handler when If-Match matches loadCurrent.updated_at", async () => {
    const handler = vi.fn(async () => NextResponse.json({ ok: true, data: { id: "x" } }));
    const decorated = withOptimisticConcurrency(handler, {
      loadCurrent: async () => ({ updated_at: new Date(ISO), payload: { id: "x" } }),
    });
    const res = await decorated(stubRequest({ headers: { "if-match": ISO } }));
    expect(handler).toHaveBeenCalledOnce();
    expect(res.status).toBe(200);
  });

  it("returns 409 stale envelope when If-Match mismatches", async () => {
    const handler = vi.fn();
    const decorated = withOptimisticConcurrency(handler as unknown as Parameters<typeof withOptimisticConcurrency>[0], {
      loadCurrent: async () => ({ updated_at: new Date(ISO_OTHER), payload: { id: "x", v: 2 } }),
    });
    const res = await decorated(stubRequest({ headers: { "if-match": ISO } }));
    expect(handler).not.toHaveBeenCalled();
    expect(res.status).toBe(409);
    const body = (await res.json()) as { ok: boolean; error: { code: string; details?: unknown } };
    expect(body.ok).toBe(false);
    expect(body.error.code).toBe("stale");
    expect(body.error.details).toMatchObject({
      current_payload: { id: "x", v: 2 },
      current_updated_at: ISO_OTHER,
    });
  });

  it("returns 400 validation envelope when both If-Match header and expected_updated_at body field are absent", async () => {
    const handler = vi.fn();
    const decorated = withOptimisticConcurrency(handler as unknown as Parameters<typeof withOptimisticConcurrency>[0], {
      loadCurrent: async () => ({ updated_at: new Date(ISO), payload: {} }),
    });
    const res = await decorated(stubRequest());
    expect(handler).not.toHaveBeenCalled();
    expect(res.status).toBe(400);
    const body = (await res.json()) as { ok: boolean; error: { code: string } };
    expect(body.error.code).toBe("validation");
  });

  it("falls back to expected_updated_at body field when If-Match header missing", async () => {
    const handler = vi.fn(async () => NextResponse.json({ ok: true, data: {} }));
    const decorated = withOptimisticConcurrency(handler, {
      loadCurrent: async () => ({ updated_at: new Date(ISO), payload: {} }),
    });
    const res = await decorated(
      stubRequest({
        headers: { "content-type": "application/json" },
        body: { expected_updated_at: ISO, payload: { name: "x" } },
      }),
    );
    expect(handler).toHaveBeenCalledOnce();
    expect(res.status).toBe(200);
  });

  it("emits catalog.entity.save_conflict audit on 409 path", async () => {
    const auditEmitter: ReturnType<typeof vi.fn<AuditEmitter>> = vi.fn(async () => {});
    const handler = vi.fn();
    const decorated = withOptimisticConcurrency(handler as unknown as Parameters<typeof withOptimisticConcurrency>[0], {
      loadCurrent: async () => ({ updated_at: new Date(ISO_OTHER), payload: {} }),
      auditEmitter,
      targetKind: "asset",
      resolveTargetId: () => "asset:42",
    });
    await decorated(stubRequest({ headers: { "if-match": ISO } }));
    expect(auditEmitter).toHaveBeenCalledOnce();
    const firstCall = auditEmitter.mock.calls[0];
    expect(firstCall).toBeDefined();
    const arg = firstCall![0]!;
    expect(arg.action).toBe("catalog.entity.save_conflict");
    expect(arg.target_kind).toBe("asset");
    expect(arg.target_id).toBe("asset:42");
    expect(arg.payload).toMatchObject({
      provided_updated_at: ISO,
      current_updated_at: ISO_OTHER,
    });
  });

  it("returns 404 not_found when loadCurrent returns null", async () => {
    const handler = vi.fn();
    const decorated = withOptimisticConcurrency(handler as unknown as Parameters<typeof withOptimisticConcurrency>[0], {
      loadCurrent: async () => null,
    });
    const res = await decorated(stubRequest({ headers: { "if-match": ISO } }));
    expect(handler).not.toHaveBeenCalled();
    expect(res.status).toBe(404);
    const body = (await res.json()) as { ok: boolean; error: { code: string } };
    expect(body.error.code).toBe("not_found");
  });
});
