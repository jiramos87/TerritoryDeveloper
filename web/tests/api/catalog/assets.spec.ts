// Happy-path integration suite for /api/catalog/* (TECH-755).
// Scope: published-only. Bug 1 (retired-404) + Bug 6 (retire 409) = red tests owned by TECH-756.

import { afterEach, beforeEach, describe, expect, test } from "vitest";
import { GET as listGet, POST as listPost } from "@/app/api/catalog/assets/route";
import { GET as byIdGet, PATCH as byIdPatch } from "@/app/api/catalog/assets/[id]/route";
import { POST as retirePost } from "@/app/api/catalog/assets/[id]/retire/route";
import { POST as previewPost } from "@/app/api/catalog/preview-diff/route";
import { stableJsonStringify } from "@/lib/catalog/stable-json-stringify";
import { getSql } from "@/lib/db/client";
import { resetCatalogTables, seedZoneS, invokeRoute } from "./_harness";

beforeEach(async () => {
  await resetCatalogTables();
  await seedZoneS();
});
afterEach(async () => {
  await resetCatalogTables();
});

describe("catalog api happy path (published only)", () => {
  test("catalog_list_published_default_returns_seven", async () => {
    const res = await invokeRoute(listGet, "GET", "/api/catalog/assets");
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      assets: Array<{ status: string }>;
      next_cursor: string | null;
    };
    expect(body.assets.length).toBe(7);
    expect(body.assets.every((a) => a.status === "published")).toBe(true);
    expect(body.next_cursor).toBeNull();
  });

  test("catalog_list_include_draft_returns_non_retired", async () => {
    const res = await invokeRoute(
      listGet,
      "GET",
      "/api/catalog/assets?include_draft=1",
    );
    expect(res.status).toBe(200);
    const body = (await res.json()) as { assets: Array<{ status: string }> };
    expect(body.assets.every((a) => a.status !== "retired")).toBe(true);
  });

  test("catalog_get_by_id_returns_joined_snapshot", async () => {
    const sql = getSql();
    const [row] =
      await sql`select id from catalog_asset where status = 'published' and id >= 1 order by id asc limit 1`;
    const id = String((row as { id: number | bigint }).id);
    const res = await invokeRoute(
      byIdGet,
      "GET",
      `/api/catalog/assets/${id}`,
      undefined,
      { id },
    );
    expect(res.status).toBe(200);
    const body = await res.json();
    // Normalize volatile `updated_at` — seed uses `now()` so value shifts per run.
    const normalized = JSON.parse(stableJsonStringify(body)) as Record<
      string,
      Record<string, unknown>
    >;
    if (normalized.asset && typeof normalized.asset === "object") {
      normalized.asset.updated_at = "<normalized>";
    }
    expect(stableJsonStringify(normalized)).toMatchSnapshot();
  });

  test("catalog_post_create_returns_201_composite", async () => {
    const body = {
      category: "test",
      slug: "harness-create",
      display_name: "Harness Create",
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
      sprite_binds: [{ sprite_id: "1", slot: "world" }],
    };
    const res = await invokeRoute(listPost, "POST", "/api/catalog/assets", body);
    expect(res.status).toBe(201);
    // TECH-1351: POST envelope now wrapped via `withAudit` → `{ ok, data, audit_id }`.
    const out = (await res.json()) as {
      ok: true;
      data: { asset: { id: string } };
      audit_id: string;
    };
    expect(out.ok).toBe(true);
    expect(out.data.asset.id).toMatch(/^\d+$/);
    expect(out.audit_id).toMatch(/^\d+$/);
  });

  test("catalog_patch_returns_200_composite", async () => {
    const sql = getSql();
    // Use shipped GET-by-id to fetch updated_at — matches real client flow; harness seed
    // truncates to millisecond so the ISO string round-trips through the PATCH route.
    const [row] =
      await sql`select id from catalog_asset where status = 'published' and id >= 1 order by id asc limit 1`;
    const id = String((row as { id: number | bigint }).id);
    const getRes = await invokeRoute(byIdGet, "GET", `/api/catalog/assets/${id}`, undefined, { id });
    const cur = (await getRes.json()) as { asset: { updated_at: string } };
    const updated_at = cur.asset.updated_at;
    const res = await invokeRoute(
      byIdPatch,
      "PATCH",
      `/api/catalog/assets/${id}`,
      { updated_at, display_name: "patched" },
      { id },
    );
    expect(res.status).toBe(200);
    // Bug 7 fixed in TECH-756: PATCH response now reflects the just-applied UPDATE
    // (inline composite build from `tx`, no out-of-txn re-read). Strict regression in
    // bugs.spec.ts → catalog_patch_response_reflects_updated_fields_no_round_trip.
    const out = (await res.json()) as { asset: { display_name: string } };
    expect(out.asset.display_name).toBe("patched");
  });

  test("catalog_retire_returns_200_with_null_replaced_by", async () => {
    const sql = getSql();
    const [row] =
      await sql`select id from catalog_asset where status = 'published' and id >= 1 order by id asc limit 1`;
    const id = String((row as { id: number | bigint }).id);
    const res = await invokeRoute(
      retirePost,
      "POST",
      `/api/catalog/assets/${id}/retire`,
      {},
      { id },
    );
    expect(res.status).toBe(200);
    const out = (await res.json()) as {
      asset: { status: string; replaced_by: string | null };
    };
    expect(out.asset.status).toBe("retired");
    expect(out.asset.replaced_by).toBeNull();
  });

  test("catalog_preview_diff_returns_200_stable_json", async () => {
    const sql = getSql();
    const [row] =
      await sql`select id from catalog_asset where status = 'published' and id >= 1 order by id asc limit 1`;
    const asset_id = String((row as { id: number | bigint }).id);
    const res1 = await invokeRoute(
      previewPost,
      "POST",
      "/api/catalog/preview-diff",
      { asset_id, patch: { display_name: "Y" } },
    );
    expect(res1.status).toBe(200);
    const body1 = await res1.json();
    const res2 = await invokeRoute(
      previewPost,
      "POST",
      "/api/catalog/preview-diff",
      { asset_id, patch: { display_name: "Y" } },
    );
    const body2 = await res2.json();
    expect(stableJsonStringify(body1)).toBe(stableJsonStringify(body2));
  });
});

describe("catalog harness self-test", () => {
  test("harness_db_reset_between_tests_no_leakage", async () => {
    const sql = getSql();
    await sql`insert into catalog_sprite (id, path, ppu, pivot_x, pivot_y, provenance)
              values (9999, 'leak-probe', 100, 0.5, 0.5, 'hand')
              on conflict (id) do nothing`;
    await resetCatalogTables();
    await seedZoneS();
    const rows = await sql`select id from catalog_sprite where id = 9999`;
    expect(rows.length).toBe(0);
  });
});
