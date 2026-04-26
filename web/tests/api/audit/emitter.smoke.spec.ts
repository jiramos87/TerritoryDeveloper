// TECH-1351 audit emitter + withAudit decorator smoke.
//
// Three IT blocks per §Test Blueprint rows 1-3:
//   1. Happy path → POST /api/catalog/assets emits one audit_log row + envelope { ok, data, audit_id }.
//   2. Validation rejection → 400 envelope { ok:false, error }; no audit_log row written.
//   3. Tx rollback → mock audit() to throw → POST returns 500; catalog_asset count unchanged.
//
// DB-backed: requires DATABASE_URL pointing at a test pg instance with migrations 0013 + 0026 applied.
// Uses the existing harness pattern (resetCatalogTables / seedZoneS) and direct route invocation.

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";
import { resetCatalogTables, seedZoneS } from "../catalog/_harness";

// Mock the auth session — emitter uses `getSessionUser()` for actor; tests run unauthenticated.
vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(async () => null),
}));

// Mock spy for emitter — re-imported lazily inside tests so vi.mock takes effect.
vi.mock("@/lib/audit/emitter", async (importOriginal) => {
  const orig = await importOriginal<typeof import("@/lib/audit/emitter")>();
  return { ...orig, audit: vi.fn(orig.audit) };
});

const VALID_BODY = {
  category: "test",
  slug: "audit-smoke-create",
  display_name: "Audit Smoke Create",
  status: "draft" as const,
  footprint_w: 1,
  footprint_h: 1,
  placement_mode: "tile",
  has_button: false,
  economy: {
    base_cost_cents: 0,
    monthly_upkeep_cents: 0,
    demolition_refund_pct: 0,
    construction_ticks: 0,
  },
  sprite_binds: [{ sprite_id: "1", slot: "world" as const }],
};

async function invokePost(body: unknown): Promise<Response> {
  const { POST } = await import("@/app/api/catalog/assets/route");
  const req = new NextRequest(new URL("/api/catalog/assets", "http://localhost"), {
    method: "POST",
    body: JSON.stringify(body),
    headers: { "content-type": "application/json" },
  });
  return POST(req);
}

beforeEach(async () => {
  await resetCatalogTables();
  await seedZoneS();
  // Truncate audit_log between tests; FK is loose (target_id is text) so independent reset.
  const sql = getSql();
  await sql.unsafe("truncate audit_log restart identity");
  vi.clearAllMocks();
});

afterEach(async () => {
  await resetCatalogTables();
  const sql = getSql();
  await sql.unsafe("truncate audit_log restart identity");
});

describe("audit emitter + withAudit (TECH-1351)", () => {
  it("happy path emits one audit_log row + envelope { ok, data, audit_id }", async () => {
    const res = await invokePost(VALID_BODY);
    expect(res.status).toBe(201);
    const body = (await res.json()) as {
      ok: boolean;
      data: { asset: { id: string } };
      audit_id: string;
    };
    expect(body.ok).toBe(true);
    expect(body.data.asset.id).toMatch(/^\d+$/);
    expect(body.audit_id).toMatch(/^\d+$/);

    const sql = getSql();
    const rows = (await sql`
      select id, action, target_kind, target_id, payload
      from audit_log
      where target_id = ${body.data.asset.id}
    `) as unknown as Array<{
      id: string | number;
      action: string;
      target_kind: string;
      target_id: string;
      payload: Record<string, unknown>;
    }>;
    expect(rows.length).toBe(1);
    expect(rows[0]!.action).toBe("catalog.asset.create");
    expect(rows[0]!.target_kind).toBe("catalog_asset");
    expect(String(rows[0]!.id)).toBe(body.audit_id);
    expect(rows[0]!.payload.slug).toBe(VALID_BODY.slug);
  });

  it("validation rejection returns 400 envelope; no audit_log row written", async () => {
    const sql = getSql();
    const before = (await sql`select count(*)::int as n from audit_log`) as unknown as Array<{
      n: number;
    }>;
    const beforeN = before[0]!.n;

    const bad = { ...VALID_BODY, category: "" }; // category required
    const res = await invokePost(bad);
    expect(res.status).toBe(400);
    const body = (await res.json()) as { code?: string; error?: { code: string } };
    // Outer error envelope (catalogJsonError shape — predates DEC-A48 wrapper).
    expect(body.code ?? body.error?.code).toBe("bad_request");

    const after = (await sql`select count(*)::int as n from audit_log`) as unknown as Array<{
      n: number;
    }>;
    expect(after[0]!.n).toBe(beforeN);
  });

  it("emitter throw rolls back tx; catalog_asset count unchanged", async () => {
    const sql = getSql();
    const before = (await sql`select count(*)::int as n from catalog_asset`) as unknown as Array<{
      n: number;
    }>;
    const beforeN = before[0]!.n;

    const emitter = await import("@/lib/audit/emitter");
    const auditMock = emitter.audit as unknown as ReturnType<typeof vi.fn>;
    auditMock.mockImplementationOnce(async () => {
      throw new Error("forced emitter failure");
    });

    const res = await invokePost(VALID_BODY);
    // Outer try/catch in POST maps unknown errors via responseFromPostgresError → 500.
    expect(res.status).toBeGreaterThanOrEqual(500);

    const after = (await sql`select count(*)::int as n from catalog_asset`) as unknown as Array<{
      n: number;
    }>;
    expect(after[0]!.n).toBe(beforeN);
  });
});
