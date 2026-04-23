// Regression suite for TECH-756 bug fixes in /api/catalog/* routes.
// Each bug gets one test; Bug 6 contributes two (missing + retired).
// Plus two guard tests for doc/ref reconciliation (rule sections + JSDoc refs).

import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { GET as listGet, POST as listPost } from "@/app/api/catalog/assets/route";
import { PATCH as byIdPatch } from "@/app/api/catalog/assets/[id]/route";
import { GET as byIdGet } from "@/app/api/catalog/assets/[id]/route";
import { POST as retirePost } from "@/app/api/catalog/assets/[id]/retire/route";
import { POST as previewPost } from "@/app/api/catalog/preview-diff/route";
import { getSql } from "@/lib/db/client";
import { resetCatalogTables, seedZoneS, invokeRoute } from "./_harness";

const HERE = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(HERE, "../../../..");

beforeEach(async () => {
  await resetCatalogTables();
  await seedZoneS();
});
afterEach(async () => {
  await resetCatalogTables();
  vi.restoreAllMocks();
});

async function getFirstPublishedId(): Promise<string> {
  const sql = getSql();
  const [row] =
    await sql`select id from catalog_asset where status = 'published' and id >= 1 order by id asc limit 1`;
  return String((row as { id: number | bigint }).id);
}

async function retireById(id: string): Promise<void> {
  const sql = getSql();
  await sql`update catalog_asset set status = 'retired', updated_at = now() where id = ${Number(id)}`;
}

describe("TECH-756 bug fixes", () => {
  test("catalog_get_by_id_retired_returns_404", async () => {
    const id = await getFirstPublishedId();
    await retireById(id);
    const res = await invokeRoute(
      byIdGet,
      "GET",
      `/api/catalog/assets/${id}`,
      undefined,
      { id },
    );
    expect(res.status).toBe(404);
    const body = (await res.json()) as { code: string };
    expect(body.code).toBe("not_found");
  });

  test("catalog_post_duplicate_slot_returns_409", async () => {
    const body = {
      category: "test",
      slug: "dup-slot",
      display_name: "Dup Slot",
      status: "draft",
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
      sprite_binds: [
        { sprite_id: "1", slot: "world" },
        { sprite_id: "1", slot: "world" },
      ],
    };
    const res = await invokeRoute(listPost, "POST", "/api/catalog/assets", body);
    expect(res.status).toBe(409);
    const out = (await res.json()) as { code: string };
    expect(out.code).toBe("unique_violation");
  });

  test("catalog_patch_unknown_field_returns_400", async () => {
    const id = await getFirstPublishedId();
    const getRes = await invokeRoute(byIdGet, "GET", `/api/catalog/assets/${id}`, undefined, { id });
    const cur = (await getRes.json()) as { asset: { updated_at: string } };
    const updated_at = cur.asset.updated_at;
    const res = await invokeRoute(
      byIdPatch,
      "PATCH",
      `/api/catalog/assets/${id}`,
      { updated_at, bogus: 1 },
      { id },
    );
    expect(res.status).toBe(400);
    const body = (await res.json()) as {
      code: string;
      details?: { unknown_fields?: string[] };
    };
    expect(body.code).toBe("bad_request");
    expect(body.details?.unknown_fields).toContain("bogus");
  });

  test("catalog_patch_noop_body_returns_400_narrow_msg", async () => {
    const id = await getFirstPublishedId();
    const getRes = await invokeRoute(byIdGet, "GET", `/api/catalog/assets/${id}`, undefined, { id });
    const cur = (await getRes.json()) as { asset: { updated_at: string } };
    const updated_at = cur.asset.updated_at;
    const res = await invokeRoute(
      byIdPatch,
      "PATCH",
      `/api/catalog/assets/${id}`,
      { updated_at },
      { id },
    );
    expect(res.status).toBe(400);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("bad_request");
    expect(body.error).toContain("at least one field");
  });

  test("catalog_preview_diff_runtime_error_returns_500_envelope", async () => {
    // Mock `computeCatalogAssetPreview` to throw a generic Error so we hit the swapped
    // `responseFromPostgresError` path (not a PG error → non-pg → 500 internal envelope).
    const mod = await import("@/lib/catalog/preview-diff");
    vi.spyOn(mod, "computeCatalogAssetPreview").mockImplementation(() => {
      throw new Error("boom");
    });
    const id = await getFirstPublishedId();
    const res = await invokeRoute(previewPost, "POST", "/api/catalog/preview-diff", {
      asset_id: id,
      patch: { display_name: "Y" },
    });
    expect(res.status).toBe(500);
    const body = (await res.json()) as { code: string; error: string };
    expect(body.code).toBe("internal");
    // No stack trace leaked in JSON body.
    expect(body.error).not.toMatch(/at .+:[0-9]+:[0-9]+/);
  });

  test("catalog_retire_missing_replaced_by_returns_409", async () => {
    const id = await getFirstPublishedId();
    const res = await invokeRoute(
      retirePost,
      "POST",
      `/api/catalog/assets/${id}/retire`,
      { replaced_by: 99999 },
      { id },
    );
    expect(res.status).toBe(409);
    const body = (await res.json()) as { code: string };
    expect(body.code).toBe("conflict");
  });

  test("catalog_patch_response_reflects_updated_fields_no_round_trip", async () => {
    // Bug 7: post-PATCH composite must reflect the just-applied UPDATE — not stale snapshot.
    // Pre-fix: `patchCatalogAsset` re-read via `loadCatalogAssetById` (sibling pool conn,
    // outside the txn) → READ COMMITTED returned pre-COMMIT row. Fix: inline build via `tx`.
    const id = await getFirstPublishedId();
    const getRes = await invokeRoute(byIdGet, "GET", `/api/catalog/assets/${id}`, undefined, { id });
    const cur = (await getRes.json()) as { asset: { updated_at: string; display_name: string } };
    const updated_at = cur.asset.updated_at;
    const before = cur.asset.display_name;
    const next = `${before} (patched ${Date.now()})`;
    const res = await invokeRoute(
      byIdPatch,
      "PATCH",
      `/api/catalog/assets/${id}`,
      { updated_at, display_name: next },
      { id },
    );
    expect(res.status).toBe(200);
    const out = (await res.json()) as { asset: { display_name: string } };
    expect(out.asset.display_name).toBe(next);
  });

  test("catalog_retire_retired_replaced_by_returns_409", async () => {
    const sql = getSql();
    // Pick two published ids; retire the second; try to retire the first pointing at it.
    const rows =
      (await sql`select id from catalog_asset where status = 'published' and id >= 1 order by id asc limit 2`) as unknown as Array<{
        id: number | bigint;
      }>;
    const aId = String(rows[0].id);
    const bId = String(rows[1].id);
    await retireById(bId);
    const res = await invokeRoute(
      retirePost,
      "POST",
      `/api/catalog/assets/${aId}/retire`,
      { replaced_by: Number(bId) },
      { id: aId },
    );
    expect(res.status).toBe(409);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("conflict");
    expect(body.error).toMatch(/retired/i);
  });
});

describe("TECH-756 doc/ref reconciliation", () => {
  test("web_backend_logic_rule_has_three_new_sections", () => {
    const rulePath = resolve(REPO_ROOT, "ia/rules/web-backend-logic.md");
    const text = readFileSync(rulePath, "utf8");
    expect(text).toMatch(/^##\s+Pagination contract\s*$/m);
    expect(text).toMatch(/^##\s+Error-response envelope\s*$/m);
    expect(text).toMatch(/^##\s+Retire idempotency\s*$/m);
  });

  test("route_jsdoc_refs_point_at_rule_anchors", () => {
    const routeFiles = [
      "web/app/api/catalog/assets/route.ts",
      "web/app/api/catalog/assets/[id]/route.ts",
      "web/app/api/catalog/assets/[id]/retire/route.ts",
      "web/app/api/catalog/preview-diff/route.ts",
    ].map((p) => resolve(REPO_ROOT, p));
    for (const f of routeFiles) {
      const text = readFileSync(f, "utf8");
      // Every @see ref inside this file must target the rule doc, not an archived project spec.
      const atSeeLines = text
        .split("\n")
        .filter((l) => /@see\b/.test(l));
      expect(atSeeLines.length).toBeGreaterThan(0);
      for (const line of atSeeLines) {
        expect(line).toMatch(/ia\/rules\/web-backend-logic\.md#/);
        expect(line).not.toMatch(/ia\/projects\/TECH-6[0-9]{2}\.md/);
      }
    }
  });
});
