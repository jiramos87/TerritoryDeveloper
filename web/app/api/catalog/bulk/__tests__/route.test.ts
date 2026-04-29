/**
 * POST /api/catalog/bulk — route unit tests (TECH-4182 §Test Blueprint).
 * Mocks DB + session; validates request validation, capability gate, idempotency.
 */
import { describe, expect, it, vi, beforeEach } from "vitest";
import { NextRequest } from "next/server";

vi.mock("@/lib/db/client", () => ({
  getSql: vi.fn(),
}));
vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn().mockResolvedValue(null),
}));
vi.mock("@/lib/auth/capabilities", () => ({
  loadCapabilitiesForRole: vi.fn(),
}));
vi.mock("@/lib/catalog/bulk-actions", () => ({
  runBulkRetire: vi.fn().mockResolvedValue({ updated: 1, audit_payloads: [{ entity_id: "1", action: "catalog.entity.retired_bulk", meta: { slug: "s", kind: "sprite", bulk_size: 1 } }] }),
  runBulkRestore: vi.fn().mockResolvedValue({ updated: 0, audit_payloads: [] }),
  runBulkPublish: vi.fn().mockResolvedValue({ updated: 1, audit_payloads: [{ entity_id: "2", action: "catalog.entity.published_bulk", meta: { slug: "t", kind: "audio", bulk_size: 1 } }] }),
}));
vi.mock("@/lib/audit/emitter", () => ({
  audit: vi.fn().mockResolvedValue("123"),
}));

import { POST } from "@/app/api/catalog/bulk/route";
import { getSql } from "@/lib/db/client";

function makeRequest(body: object, headers?: Record<string, string>) {
  return new NextRequest("http://localhost/api/catalog/bulk", {
    method: "POST",
    headers: { "Content-Type": "application/json", ...headers },
    body: JSON.stringify(body),
  });
}

function mockBeginSql() {
  const mockTx = vi.fn().mockResolvedValue([]);
  const mockSql = Object.assign(
    vi.fn().mockResolvedValue([]),
    { begin: vi.fn().mockImplementation(async (fn: (tx: unknown) => unknown) => fn(mockTx)) },
  );
  vi.mocked(getSql).mockReturnValue(mockSql as never);
  return mockSql;
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe("POST /api/catalog/bulk — validation", () => {
  it("400 on missing action", async () => {
    mockBeginSql();
    const res = await POST(makeRequest({ entity_ids: ["1"] }));
    expect(res.status).toBe(400);
    const body = await res.json() as { code: string };
    expect(body.code).toBe("bad_request");
  });

  it("400 on invalid action value", async () => {
    mockBeginSql();
    const res = await POST(makeRequest({ action: "delete", entity_ids: ["1"] }));
    expect(res.status).toBe(400);
  });

  it("400 on empty entity_ids", async () => {
    mockBeginSql();
    const res = await POST(makeRequest({ action: "retire", entity_ids: [] }));
    expect(res.status).toBe(400);
  });

  it("400 on entity_ids exceeding 1000", async () => {
    mockBeginSql();
    const ids = Array.from({ length: 1001 }, (_, i) => String(i));
    const res = await POST(makeRequest({ action: "retire", entity_ids: ids }));
    expect(res.status).toBe(400);
  });

  it("400 on non-string entity_ids", async () => {
    mockBeginSql();
    const res = await POST(makeRequest({ action: "retire", entity_ids: [1, 2] }));
    expect(res.status).toBe(400);
  });
});

describe("POST /api/catalog/bulk — success (no session)", () => {
  it("200 on retire with no session user (proxy-level auth assumed)", async () => {
    mockBeginSql();
    const res = await POST(makeRequest({ action: "retire", entity_ids: ["1"] }));
    expect(res.status).toBe(200);
    const body = await res.json() as { ok: boolean; data: { action: string } };
    expect(body.ok).toBe(true);
    expect(body.data.action).toBe("retire");
  });

  it("200 on publish", async () => {
    mockBeginSql();
    const res = await POST(makeRequest({ action: "publish", entity_ids: ["2"] }));
    expect(res.status).toBe(200);
    const body = await res.json() as { ok: boolean };
    expect(body.ok).toBe(true);
  });
});

describe("POST /api/catalog/bulk — idempotency replay", () => {
  it("returns 200 with idempotent=true when key already seen", async () => {
    const mockSql = Object.assign(
      vi.fn().mockResolvedValue([{ id: "999" }]),
      { begin: vi.fn() },
    );
    vi.mocked(getSql).mockReturnValue(mockSql as never);
    const res = await POST(makeRequest({ action: "retire", entity_ids: ["1"] }, { "Idempotency-Key": "key-abc" }));
    expect(res.status).toBe(200);
    const body = await res.json() as { data: { idempotent?: boolean } };
    expect(body.data.idempotent).toBe(true);
  });
});
