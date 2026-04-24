# grid-asset-visual-registry — Stage 1.3 Plan Digest

Compiled 2026-04-23 from 2 task spec(s).

---

## §Plan Digest

```yaml
mechanicalization_score:
  anchors: ok
  picks: ok
  invariants: ok
  validators: ok
  escalation_enum: ok
  overall: fully_mechanical
```

### §Goal

Land reusable vitest integration test harness under `web/tests/api/catalog/` + one happy-path suite locking seven published-path responses (list, get-by-id, create, patch, retire, preview-diff) and one harness self-test — zero route-handler edits.

### §Acceptance

- [ ] New file `web/tests/api/catalog/_harness.ts` exports `resetCatalogTables()`, `invokeRoute(method, path, body?, params?)`, `seedZoneS()`.
- [ ] New file `web/tests/api/catalog/assets.spec.ts` contains 7 happy-path vitest cases + 1 harness self-test (8 total), all published-only.
- [ ] `npm run validate:web` exits 0 locally (root script chains `npm --prefix web run test` = `vitest run --passWithNoTests`).
- [ ] Harness self-test `harness_db_reset_between_tests_no_leakage` green — second test observes zero carry-over rows.
- [ ] Stable-JSON snapshots committed for `GET /assets/:id` + `POST /preview-diff` via `web/lib/catalog/stable-json-stringify.ts`.
- [ ] Zero edits under `web/app/api/catalog/**` + `web/lib/catalog/**` (except new harness file) — `git diff --stat` shows only `web/tests/api/catalog/**` additions.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `catalog_list_published_default_returns_seven` | `GET /api/catalog/assets` | status 200; `assets.length === 7`; every `status === 'published'`; `next_cursor === null` | vitest direct-invoke |
| `catalog_list_include_draft_returns_non_retired` | `GET /api/catalog/assets?include_draft=1` | status 200; `assets.every(a => a.status !== 'retired')` | vitest direct-invoke |
| `catalog_get_by_id_returns_joined_snapshot` | `GET /api/catalog/assets/{seed_published_id}` | status 200; body matches stable-JSON snapshot pinned in test file | vitest direct-invoke |
| `catalog_post_create_returns_201_composite` | `POST /api/catalog/assets` w/ minimal valid body + one sprite_bind | status 201; response composite has numeric id; DB has row + bind | vitest direct-invoke |
| `catalog_patch_returns_200_composite` | `PATCH /api/catalog/assets/{id}` w/ `{ updated_at, display_name: "patched" }` | status 200; `composite.asset.display_name === "patched"`; `updated_at` advanced | vitest direct-invoke |
| `catalog_retire_returns_200_with_null_replaced_by` | `POST /api/catalog/assets/{id}/retire` w/ `{}` | status 200; `composite.asset.status === 'retired'`; `replaced_by === null` | vitest direct-invoke |
| `catalog_preview_diff_returns_200_stable_json` | `POST /api/catalog/preview-diff` w/ `{ asset_id, patch: { display_name: "Y" } }` | status 200; `stableJsonStringify(body)` identical across two sequential runs | vitest direct-invoke |
| `harness_db_reset_between_tests_no_leakage` | test A inserts row X; test B queries | test B sees zero rows matching X inserted by A | vitest harness self-test |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `GET /api/catalog/assets` (no query) | `200` + `{ assets: CatalogAsset[7], next_cursor: null, limit: 200 }` — only `status='published'` rows | Published-default filter (`parse-list-query.ts`); seven Zone S seeded rows from `0013_zone_s_seed.sql` |
| `GET /api/catalog/assets?include_draft=1` | `200` + `{ assets: [...], next_cursor: ..., limit: 200 }` — draft + published rows | Admin-style listing; excludes retired |
| `GET /api/catalog/assets/{id}` (published) | `200` + `{ asset, economy, sprite_slots }` joined | Snapshot via `stableJsonStringify` to pin key order |
| `POST /api/catalog/assets` (valid) | `201` + joined composite | Body passes `validateCreateBody` |
| `PATCH /api/catalog/assets/{id}` w/ `{ updated_at, display_name }` | `200` + updated composite | Optimistic lock on `updated_at` |
| `POST /api/catalog/assets/{id}/retire` w/ `{}` | `200` + composite w/ `status='retired'`, `replaced_by=null` | Empty body path |
| `POST /api/catalog/preview-diff` w/ `{ asset_id, patch }` | `200` + stable-JSON preview | Read-only |

### §Mechanical Steps

#### Step 1 — Create harness module

**Goal:** Ship `web/tests/api/catalog/_harness.ts` exposing `resetCatalogTables()`, `invokeRoute()`, `seedZoneS()`. Direct-invoke route handlers via constructed `NextRequest` — no dev server.

**Edits:**
- `web/tests/api/catalog/_harness.ts` — **before**: file does not exist. **after** (verbatim new-file content):
  ```ts
  // Catalog API integration test harness (TECH-755).
  // Direct-invoke route handlers with constructed NextRequest; no dev server.
  // DB isolation: TRUNCATE catalog_* tables RESTART IDENTITY CASCADE (Neon HTTP — no
  // portable per-connection BEGIN/ROLLBACK). Migration 0013 seeds seven Zone S rows
  // and is consumed read-only by seedZoneS().

  import { NextRequest } from "next/server";
  import { getSql } from "@/lib/db/client";

  type RouteHandler = (req: NextRequest, ctx?: { params: Promise<Record<string, string>> }) => Promise<Response>;

  const CATALOG_TABLES = [
    "catalog_asset_sprite",
    "catalog_zone_s_spawn_rule",
    "catalog_zone_s_variant",
    "catalog_economy",
    "catalog_sprite",
    "catalog_asset",
  ] as const;

  export async function resetCatalogTables(): Promise<void> {
    const sql = getSql();
    await sql`truncate ${sql(CATALOG_TABLES as unknown as string[])} restart identity cascade`;
  }

  export async function seedZoneS(): Promise<void> {
    // Re-apply migration 0013 seed shape. db:migrate ran pre-test; this re-seeds
    // after resetCatalogTables() truncates. Keep idempotent (on conflict do nothing).
    const sql = getSql();
    await sql`insert into catalog_sprite (id, path, ppu, pivot_x, pivot_y, provenance)
              values (1, 'zone-s/placeholder', 100, 0.5, 0.5, 'manual')
              on conflict (id) do nothing`;
    // Seven Zone S assets seeded by 0013_zone_s_seed.sql on db:migrate;
    // test setUp() calls db:migrate (or manual psql of 0013) before vitest run.
  }

  export async function invokeRoute(
    handler: RouteHandler,
    method: "GET" | "POST" | "PATCH",
    url: string,
    body?: unknown,
    params?: Record<string, string>,
  ): Promise<Response> {
    const init: RequestInit = { method };
    if (body !== undefined) {
      init.body = JSON.stringify(body);
      init.headers = { "content-type": "application/json" };
    }
    const req = new NextRequest(new URL(url, "http://localhost"), init);
    const ctx = params ? { params: Promise.resolve(params) } : undefined;
    return handler(req, ctx);
  }
  ```

**Gate:**
```bash
cd web && npx tsc --noEmit
```
Expected: exit 0; harness module typechecks under project `tsconfig`.

**STOP:** If `tsc --noEmit` reports error in `_harness.ts` → re-open this step. If `NextRequest` import path differs from shipped routes → align to `next/server` usage in `web/app/api/catalog/assets/route.ts:1` (already the canonical import). Do NOT edit route files.

**MCP hints:** `plan_digest_resolve_anchor` (anchor check on route handler signatures), `plan_digest_verify_paths` (confirm `web/lib/db/client.ts` export), `glossary_lookup` (`catalog asset`).

#### Step 2 — Create happy-path suite

**Goal:** Ship `web/tests/api/catalog/assets.spec.ts` with 7 published-only happy-path cases + 1 harness self-test. Direct-invoke via Step 1 harness.

**Edits:**
- `web/tests/api/catalog/assets.spec.ts` — **before**: file does not exist. **after** (verbatim new-file content):
  ```ts
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
      const body = (await res.json()) as { assets: Array<{ status: string }>; next_cursor: string | null };
      expect(body.assets.length).toBe(7);
      expect(body.assets.every((a) => a.status === "published")).toBe(true);
      expect(body.next_cursor).toBeNull();
    });

    test("catalog_list_include_draft_returns_non_retired", async () => {
      const res = await invokeRoute(listGet, "GET", "/api/catalog/assets?include_draft=1");
      expect(res.status).toBe(200);
      const body = (await res.json()) as { assets: Array<{ status: string }> };
      expect(body.assets.every((a) => a.status !== "retired")).toBe(true);
    });

    test("catalog_get_by_id_returns_joined_snapshot", async () => {
      const sql = getSql();
      const [row] = await sql`select id from catalog_asset where status = 'published' order by id asc limit 1`;
      const id = String((row as { id: number | bigint }).id);
      const res = await invokeRoute(byIdGet, "GET", `/api/catalog/assets/${id}`, undefined, { id });
      expect(res.status).toBe(200);
      const body = await res.json();
      expect(stableJsonStringify(body)).toMatchSnapshot();
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
        economy: { base_cost_cents: 0, monthly_upkeep_cents: 0, demolition_refund_pct: 0, construction_ticks: 0 },
        sprite_binds: [{ sprite_id: "1", slot: "default" }],
      };
      const res = await invokeRoute(listPost, "POST", "/api/catalog/assets", body);
      expect(res.status).toBe(201);
      const out = (await res.json()) as { asset: { id: string } };
      expect(out.asset.id).toMatch(/^\d+$/);
    });

    test("catalog_patch_returns_200_composite", async () => {
      const sql = getSql();
      const [row] = await sql`select id, updated_at from catalog_asset where status = 'published' order by id asc limit 1`;
      const id = String((row as { id: number | bigint }).id);
      const updated_at = String((row as { updated_at: string | Date }).updated_at);
      const res = await invokeRoute(
        byIdPatch,
        "PATCH",
        `/api/catalog/assets/${id}`,
        { updated_at, display_name: "patched" },
        { id },
      );
      expect(res.status).toBe(200);
      const out = (await res.json()) as { asset: { display_name: string } };
      expect(out.asset.display_name).toBe("patched");
    });

    test("catalog_retire_returns_200_with_null_replaced_by", async () => {
      const sql = getSql();
      const [row] = await sql`select id from catalog_asset where status = 'published' order by id asc limit 1`;
      const id = String((row as { id: number | bigint }).id);
      const res = await invokeRoute(retirePost, "POST", `/api/catalog/assets/${id}/retire`, {}, { id });
      expect(res.status).toBe(200);
      const out = (await res.json()) as { asset: { status: string; replaced_by: string | null } };
      expect(out.asset.status).toBe("retired");
      expect(out.asset.replaced_by).toBeNull();
    });

    test("catalog_preview_diff_returns_200_stable_json", async () => {
      const sql = getSql();
      const [row] = await sql`select id from catalog_asset where status = 'published' order by id asc limit 1`;
      const asset_id = String((row as { id: number | bigint }).id);
      const res1 = await invokeRoute(previewPost, "POST", "/api/catalog/preview-diff", {
        asset_id,
        patch: { display_name: "Y" },
      });
      expect(res1.status).toBe(200);
      const body1 = await res1.json();
      const res2 = await invokeRoute(previewPost, "POST", "/api/catalog/preview-diff", {
        asset_id,
        patch: { display_name: "Y" },
      });
      const body2 = await res2.json();
      expect(stableJsonStringify(body1)).toBe(stableJsonStringify(body2));
    });
  });

  describe("catalog harness self-test", () => {
    test("harness_db_reset_between_tests_no_leakage", async () => {
      const sql = getSql();
      await sql`insert into catalog_sprite (id, path, ppu, pivot_x, pivot_y, provenance)
                values (9999, 'leak-probe', 100, 0.5, 0.5, 'manual')
                on conflict (id) do nothing`;
      await resetCatalogTables();
      await seedZoneS();
      const rows = await sql`select id from catalog_sprite where id = 9999`;
      expect(rows.length).toBe(0);
    });
  });
  ```

**Gate:**
```bash
cd web && npx vitest run -t "catalog_|harness_db_reset"
```
Expected: exit 0; 8 tests pass; snapshot file generated under `web/tests/api/catalog/__snapshots__/`.

**STOP:** If vitest fails on missing `db:migrate` seed → run `npm run db:migrate` from repo root first; if `fetch-asset-composite.ts` returns `"notfound"` for seeded ids → seed migration did not execute; re-run `db:migrate`. Do NOT edit route handlers to make tests green — any red test exposing Bug 1 / 2 / 3 / 4 / 5 / 6 is out of scope and owned by TECH-756.

**MCP hints:** `plan_digest_resolve_anchor` (verify route handler export names), `backlog_issue` (TECH-756 for scope boundary), `glossary_lookup` (`joined composite`, `preview-diff`).

#### Step 3 — Verify suite picks up via root `validate:web`

**Goal:** Confirm root `npm run validate:web` chains into `vitest run` and picks up the new suite — no new npm script needed.

**Edits:** none — this is a validator step only.

**Gate:**
```bash
npm run validate:web
```
Expected: exit 0; output mentions the catalog spec file under `web/tests/api/catalog/` with 8 passed.

**STOP:** If `validate:web` does not include the vitest step → inspect root `package.json` `validate:web` script; chain `npm --prefix web run test` into it in a separate PR (out of scope for TECH-755 — file as a follow-up issue). Do NOT add a new catalog-scoped npm script.

**MCP hints:** `plan_digest_verify_paths` (`package.json` at repo root), `backlog_issue` (file follow-up if chain missing).

---
## §Plan Digest

```yaml
mechanicalization_score:
  anchors: ok
  picks: ok
  invariants: ok
  validators: ok
  escalation_enum: ok
  overall: fully_mechanical
```

### §Goal

Fix six concrete behavior gaps in shipped `/api/catalog/*` routes (each paired with a regression vitest spec via TECH-755 harness), then extend `ia/rules/web-backend-logic.md` with three new `##` sections (Pagination contract / Error-response envelope / Retire idempotency) and retarget JSDoc `@see` refs in four route files from archived project specs to live rule anchors.

### §Acceptance

- [ ] Bug 1: `loadCatalogAssetById` in `web/lib/catalog/fetch-asset-composite.ts` filters `status != 'retired'`; `GET /api/catalog/assets/:id` returns 404 for retired rows.
- [ ] Bug 2: Duplicate sprite_bind slot on `POST /assets` returns 409 `unique_violation` via existing `responseFromPostgresError` path (relies on DB unique index, not new pre-check).
- [ ] Bug 3: `PATCH /assets/:id` rejects unknown body fields with 400 `bad_request` + `details.unknown_fields` listing offending keys.
- [ ] Bug 4: `PATCH /assets/:id` with only `{ updated_at }` returns 400 with message "PATCH body must include at least one field to update" (narrower than current generic message).
- [ ] Bug 5: `web/app/api/catalog/preview-diff/route.ts:37` swaps bare `throw e` for `responseFromPostgresError(e, "Preview diff failed")`.
- [ ] Bug 6: `web/app/api/catalog/assets/[id]/retire/route.ts` returns 409 `conflict` (not 404) when `replaced_by` id missing OR points at a retired asset.
- [ ] 7 new vitest cases in `web/tests/api/catalog/` (one per bug; Bug 6 contributes 2 — missing + retired).
- [ ] `ia/rules/web-backend-logic.md` has three new `##` sections: `## Pagination contract`, `## Error-response envelope`, `## Retire idempotency`.
- [ ] All four route files (`assets/route.ts`, `assets/[id]/route.ts`, `assets/[id]/retire/route.ts`, `preview-diff/route.ts`) have JSDoc `@see` refs retargeted from `ia/backlog-archive/TECH-640.yaml` … `TECH-645.md` to `ia/rules/web-backend-logic.md` section anchors.
- [ ] `npm run validate:all` + `npm run validate:web` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `catalog_get_by_id_retired_returns_404` | seed retired asset; `GET /api/catalog/assets/{retired_id}` | status 404; `body.code === 'not_found'` | vitest (TECH-755 harness) |
| `catalog_post_duplicate_slot_returns_409` | `POST /api/catalog/assets` body w/ two sprite_binds same `slot` | status 409; `body.code === 'unique_violation'` | vitest |
| `catalog_patch_unknown_field_returns_400` | `PATCH /api/catalog/assets/{id}` w/ `{ updated_at, bogus: 1 }` | status 400; `body.code === 'bad_request'`; `body.details.unknown_fields` contains `"bogus"` | vitest |
| `catalog_patch_noop_body_returns_400_narrow_msg` | `PATCH /api/catalog/assets/{id}` w/ only `{ updated_at }` | status 400; `body.error` contains `"at least one field"` | vitest |
| `catalog_preview_diff_runtime_error_returns_500_envelope` | mock `computeCatalogAssetPreview` throwing generic `Error` | status 500; `body.code === 'internal'`; no raw stack in JSON | vitest |
| `catalog_retire_missing_replaced_by_returns_409` | `POST /api/catalog/assets/{id}/retire` w/ `{ replaced_by: 99999 }` | status 409; `body.code === 'conflict'` | vitest |
| `catalog_retire_retired_replaced_by_returns_409` | seed retired asset B; `POST /assets/{A}/retire` w/ `{ replaced_by: B.id }` | status 409; `body.code === 'conflict'`; `body.error` mentions "retired" | vitest |
| `web_backend_logic_rule_has_three_new_sections` | grep `ia/rules/web-backend-logic.md` | all three `## Pagination contract`, `## Error-response envelope`, `## Retire idempotency` present | node (grep) |
| `route_jsdoc_refs_point_at_rule_anchors` | grep `@see` from 4 route files | each `@see` targets `ia/rules/web-backend-logic.md#...`, zero archived-spec refs | node (grep) |

### §Examples

| Input | Expected output | Bug |
|-------|-----------------|-----|
| `GET /api/catalog/assets/{retired_id}` | `404` + `{ error, code: 'not_found' }` | 1 |
| `POST /assets` w/ duplicate `sprite_bind.slot` | `409` + `{ error, code: 'unique_violation' }` | 2 |
| `PATCH /assets/{id}` w/ `{ updated_at, unknown_field: "x" }` | `400` + `{ error, code: 'bad_request', details: { unknown_fields: ["unknown_field"] } }` | 3 |
| `PATCH /assets/{id}` w/ only `{ updated_at }` | `400` + `{ error: "PATCH body must include at least one field to update", code: 'bad_request' }` | 4 |
| `POST /preview-diff` that hits non-DATABASE_URL runtime error | `500` + `{ error, code: 'internal' }` (via `responseFromPostgresError`) | 5 |
| `POST /assets/{id}/retire` w/ `{ replaced_by: 99999 }` | `409` + `{ error, code: 'conflict' }` | 6 |
| `POST /assets/{id}/retire` w/ `{ replaced_by: {retired_id} }` | `409` + `{ error, code: 'conflict' }` w/ `error` mentioning "retired" | 6 |

### §Mechanical Steps

#### Step 1 — Bug 1: filter retired in `loadCatalogAssetById`

**Goal:** Add `status != 'retired'` to the primary asset SELECT so `GET /api/catalog/assets/:id` returns 404 for retired rows. Keep existing "notfound" path as-is (now covers retired too).

**Edits:**
- `web/lib/catalog/fetch-asset-composite.ts` — **before**:
  ```
  const ar = await sql`select * from catalog_asset where id = ${idNum} limit 1`;
  ```
  **after**:
  ```
  const ar = await sql`select * from catalog_asset where id = ${idNum} and status != 'retired' limit 1`;
  ```

**Gate:**
```bash
cd web && npx vitest run -t "catalog_get_by_id_retired_returns_404"
```
Expected: exit 0; test green.

**STOP:** If happy-path `catalog_get_by_id_returns_joined_snapshot` (TECH-755) turns red due to seed drift → revert this edit, re-verify seed has a published row, then re-apply. Do NOT move the filter into `[id]/route.ts` handler — `loadCatalogAssetById` is the one consumer of this SELECT and also feeds `preview-diff` which must also exclude retired.

**MCP hints:** `plan_digest_resolve_anchor` (confirm anchor is still unique after edit), `glossary_lookup` (`catalog asset`).

#### Step 2 — Bug 2: verify DB unique constraint covers duplicate slot

**Goal:** Confirm `catalog_asset_sprite` has a unique index on `(asset_id, slot)` in `0011_catalog_core.sql` so existing `responseFromPostgresError` path converts `23505` → 409 `unique_violation`. No handler edit if constraint exists.

**Edits:**
- none — constraint verification + regression test only.

**Gate:**
```bash
cd web && npx vitest run -t "catalog_post_duplicate_slot_returns_409"
```
Expected: vitest exits 0; regression test green via existing DB unique constraint.

**STOP:** If vitest test fails with 500 or missing-constraint symptom → grep `0011_catalog_core.sql` under `db/migrations/` for `unique.*catalog_asset_sprite`; if constraint missing, file a follow-up migration in a separate commit adding `unique (asset_id, slot)`, re-run gate. Do NOT add a handler pre-`SELECT` (race window, duplicates DB uniqueness).

**MCP hints:** `plan_digest_verify_paths` (migration path under `db/migrations/`), `glossary_lookup` (`slot uniqueness`).

#### Step 3 — Bug 3+4: strict-schema + narrow no-op msg in `patch-asset.ts`

**Goal:** Reject unknown PATCH body fields (Bug 3) with 400 `bad_request` + `details.unknown_fields`; narrow no-op-body error to `"PATCH body must include at least one field to update"` (Bug 4). Land via shared strict-schema allowlist inside `patchCatalogAsset`.

**Edits:**
- `web/lib/catalog/patch-asset.ts` — **before**:
  ```
  const { updated_at: _v, ...rest } = body;
  if (Object.keys(rest).length === 0) {
    return { ok: "badid" };
  }
  ```
  **after**:
  ```
  const ALLOWED_PATCH_KEYS = new Set([
    "updated_at",
    "display_name",
    "status",
    "replaced_by",
    "footprint_w",
    "footprint_h",
    "placement_mode",
    "unlocks_after",
    "has_button",
    "economy",
  ]);
  const unknownFields = Object.keys(body).filter((k) => !ALLOWED_PATCH_KEYS.has(k));
  if (unknownFields.length > 0) {
    return { ok: "unknown_fields", unknownFields };
  }
  const { updated_at: _v, ...rest } = body;
  if (Object.keys(rest).length === 0) {
    return { ok: "no_fields" };
  }
  ```
- `web/lib/catalog/patch-asset.ts` (PatchOutcome type) — **before**:
  ```
  type PatchOutcome =
    | PatchOk
    | { ok: "notfound" }
    | { ok: "badid" }
    | {
        ok: "conflict";
        current: NonNullable<Exclude<Awaited<ReturnType<typeof loadCatalogAssetById>>, "notfound" | "badid">>;
      };
  ```
  **after**:
  ```
  type PatchOutcome =
    | PatchOk
    | { ok: "notfound" }
    | { ok: "badid" }
    | { ok: "no_fields" }
    | { ok: "unknown_fields"; unknownFields: string[] }
    | {
        ok: "conflict";
        current: NonNullable<Exclude<Awaited<ReturnType<typeof loadCatalogAssetById>>, "notfound" | "badid">>;
      };
  ```
- `web/app/api/catalog/assets/[id]/route.ts` — **before**:
  ```
      if (out.ok === "badid") {
        return catalogJsonError(400, "bad_request", "Invalid id or body (need updated_at + one field to patch)");
      }
  ```
  **after**:
  ```
      if (out.ok === "badid") {
        return catalogJsonError(400, "bad_request", "Invalid id or updated_at (need valid numeric id + ISO updated_at)");
      }
      if (out.ok === "unknown_fields") {
        return catalogJsonError(400, "bad_request", "PATCH body contains unknown fields", {
          details: { unknown_fields: out.unknownFields },
        });
      }
      if (out.ok === "no_fields") {
        return catalogJsonError(400, "bad_request", "PATCH body must include at least one field to update");
      }
  ```

**Gate:**
```bash
cd web && npx vitest run -t "catalog_patch_unknown_field_returns_400|catalog_patch_noop_body_returns_400_narrow_msg"
```
Expected: exit 0; both tests green.

**STOP:** If TECH-755 `catalog_patch_returns_200_composite` turns red → check that happy-path body includes only allowlisted keys (`updated_at` + `display_name`) and retry. Do NOT widen `ALLOWED_PATCH_KEYS` beyond the 10 fields in `CatalogPatchAssetBody`. If `economy` sub-object needs its own strict-schema pass → defer to follow-up issue; scope Bug 3 to top-level keys only.

**MCP hints:** `plan_digest_resolve_anchor` (verify `"Invalid id or body"` anchor remains unique), `glossary_lookup` (`optimistic lock`).

#### Step 4 — Bug 5: preview-diff envelope swap

**Goal:** Replace bare `throw e` with `responseFromPostgresError` call for consistent error envelope.

**Edits:**
- `web/app/api/catalog/preview-diff/route.ts` — **before**:
  ```
      throw e;
  ```
  **after**:
  ```
      return responseFromPostgresError(e, "Preview diff failed");
  ```
- `web/app/api/catalog/preview-diff/route.ts` (import line) — **before**:
  ```
  import { catalogJsonError } from "@/lib/catalog/catalog-api-errors";
  ```
  **after**:
  ```
  import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
  ```

**Gate:**
```bash
cd web && npx vitest run -t "catalog_preview_diff_runtime_error_returns_500_envelope"
```
Expected: exit 0; test green; response `body.code === 'internal'`.

**STOP:** If `responseFromPostgresError` import typechecks red → verify export list in `web/lib/catalog/catalog-api-errors.ts:56` (`export function responseFromPostgresError`). If still red → revert import addition + re-open step. Do NOT catch the error inside `computeCatalogAssetPreview` — envelope belongs at the handler boundary.

**MCP hints:** `plan_digest_resolve_anchor` (`throw e;` anchor — confirm exactly 1 hit before edit), `glossary_lookup` (`error envelope`).

#### Step 5 — Bug 6: retire 409 on invalid `replaced_by`

**Goal:** Change retire handler to return 409 `conflict` (not 404) when `replaced_by` id missing OR points at a retired asset. Keep 404 reserved for the primary retire target (`id` not found).

**Edits:**
- `web/app/api/catalog/assets/[id]/retire/route.ts` — **before**:
  ```
      if (rep != null) {
        const ex = await sql`select 1 from catalog_asset where id = ${rep} limit 1`;
        if (ex.length === 0) {
          return catalogJsonError(404, "not_found", "replaced_by asset not found");
        }
      }
  ```
  **after**:
  ```
      if (rep != null) {
        const ex = await sql`select status from catalog_asset where id = ${rep} limit 1`;
        if (ex.length === 0) {
          return catalogJsonError(409, "conflict", "replaced_by asset not found");
        }
        if ((ex[0] as { status: string }).status === "retired") {
          return catalogJsonError(409, "conflict", "replaced_by asset is retired");
        }
      }
  ```

**Gate:**
```bash
cd web && npx vitest run -t "catalog_retire_missing_replaced_by_returns_409|catalog_retire_retired_replaced_by_returns_409"
```
Expected: exit 0; both tests green.

**STOP:** If TECH-755 `catalog_retire_returns_200_with_null_replaced_by` turns red → the `if (rep != null)` branch must still short-circuit for `replaced_by == null`; verify by re-reading surrounding lines at `retire/route.ts:37-42`. Do NOT change the 404 path on primary asset not-found (line 50 inside the update branch).

**MCP hints:** `plan_digest_resolve_anchor` (anchor on full 4-line before block — confirm unique hit).

#### Step 6 — Rule doc: three new sections in `web-backend-logic.md`

**Goal:** Append three canonical sections documenting shipped backend contracts (pagination / error envelope / retire idempotency) just above `## Relation to other rules` so JSDoc `@see` refs land on stable anchors.

**Edits:**
- `ia/rules/web-backend-logic.md` — **before**:
  ```
  ## Relation to other rules
  ```
  **after**:
  ```
  ## Pagination contract

  `GET /api/catalog/assets` uses keyset (cursor) pagination on `bigserial id`, ascending:

  - Query params: `limit` (default 200, range 1–500), `cursor` (numeric id string; scans `id > cursor`).
  - Response: `{ assets: CatalogAsset[], next_cursor: string | null, limit: number }`.
  - `next_cursor` is the last row id when `assets.length === limit`, else `null` (exhausted).
  - See `web/lib/catalog/parse-list-query.ts` + `web/app/api/catalog/assets/route.ts`.

  ## Error-response envelope

  All `/api/catalog/*` routes emit errors via `web/lib/catalog/catalog-api-errors.ts` helpers:

  - `catalogJsonError(status, code, message, { details?, current?, logContext? })`.
  - `responseFromPostgresError(e, fallback)` maps `23505 → 409 unique_violation`, `23503 → 400 foreign_key_violation`, `22P02 → 400 bad_request`, else `500 internal`.
  - Response body: `{ error: string, code: CatalogErrorCode, details?: unknown, current?: unknown }` — no stack traces in body.
  - `CatalogErrorCode` enum: `bad_request | not_found | conflict | internal | unique_violation | foreign_key_violation`.

  ## Retire idempotency

  `POST /api/catalog/assets/:id/retire`:

  - Empty body valid: retires asset with `replaced_by = null`.
  - `replaced_by` missing id → 409 `conflict` (not 404 — 404 reserved for the primary `:id`).
  - `replaced_by` references a retired asset → 409 `conflict`.
  - `replaced_by === :id` (self) → 400 `bad_request`.
  - Re-retiring an already-retired asset returns 200 with the current composite (idempotent).

  ## Relation to other rules
  ```

**Gate:**
```bash
cd web && npx vitest run -t "web_backend_logic_rule_has_three_new_sections"
```
Expected: exit 0; rule-doc guard test green — three new H2 headings present.

**STOP:** If guard test fails → re-open this step and re-apply the insertion at the anchor. Do NOT re-order existing sections; append only.

**MCP hints:** `plan_digest_resolve_anchor` (`## Relation to other rules` must stay unique), `glossary_lookup` (`retire idempotency`, `error envelope`).

#### Step 7 — JSDoc `@see` ref retargeting (4 route files)

**Goal:** Retarget `@see` refs from archived `ia/backlog-archive/TECH-640.yaml` … `TECH-645.md` to live rule anchors in `ia/rules/web-backend-logic.md` (landed in Step 6).

**Edits:**
- `web/app/api/catalog/assets/route.ts` — **before**:
  ```
  /**
   * @see `ia/backlog-archive/TECH-640.yaml` — `GET /api/catalog/assets`
   */
  ```
  **after**:
  ```
  /**
   * @see `ia/rules/web-backend-logic.md#pagination-contract` — `GET /api/catalog/assets`
   */
  ```
- `web/app/api/catalog/assets/route.ts` — **before**:
  ```
  /**
   * @see `ia/backlog-archive/TECH-643.yaml` — `POST /api/catalog/assets`
   */
  ```
  **after**:
  ```
  /**
   * @see `ia/rules/web-backend-logic.md#error-response-envelope` — `POST /api/catalog/assets`
   */
  ```
- `web/app/api/catalog/assets/[id]/route.ts` — **before**:
  ```
  /**
   * @see `ia/backlog-archive/TECH-641.yaml` — `GET /api/catalog/assets/:id`
   */
  ```
  **after**:
  ```
  /**
   * @see `ia/rules/web-backend-logic.md#error-response-envelope` — `GET /api/catalog/assets/:id`
   */
  ```
- `web/app/api/catalog/assets/[id]/route.ts` — **before**:
  ```
  /**
   * @see `ia/backlog-archive/TECH-644.yaml` — `PATCH /api/catalog/assets/:id`
   */
  ```
  **after**:
  ```
  /**
   * @see `ia/rules/web-backend-logic.md#error-response-envelope` — `PATCH /api/catalog/assets/:id`
   */
  ```
- `web/app/api/catalog/assets/[id]/retire/route.ts` — **before**:
  ```
  /**
   * @see `ia/backlog-archive/TECH-645.yaml` — `POST /api/catalog/assets/:id/retire`
   */
  ```
  **after**:
  ```
  /**
   * @see `ia/rules/web-backend-logic.md#retire-idempotency` — `POST /api/catalog/assets/:id/retire`
   */
  ```
- `web/app/api/catalog/preview-diff/route.ts` — **before**:
  ```
  /**
   * @see `ia/backlog-archive/TECH-645.yaml` — `POST /api/catalog/preview-diff` (read-only; no `INSERT`/`UPDATE`).
   */
  ```
  **after**:
  ```
  /**
   * @see `ia/rules/web-backend-logic.md#error-response-envelope` — `POST /api/catalog/preview-diff` (read-only; no `INSERT`/`UPDATE`).
   */
  ```

**Gate:**
```bash
cd web && npx vitest run -t "route_jsdoc_refs_point_at_rule_anchors"
```
Expected: exit 0; JSDoc-ref guard test green — zero archived-spec refs, six rule-anchor refs.

**STOP:** If guard test fails → revisit the before/after blocks in this step; re-apply. Do NOT leave a dual-citation (`@see A; @see B`) — rule anchor is the canonical surface now.

**MCP hints:** `plan_digest_resolve_anchor` (confirm each before block unique), `glossary_lookup` (`error envelope`).

#### Step 8 — Final validator sweep

**Goal:** Confirm both root validators exit 0 after Steps 1–7 land.

**Edits:** none.

**Gate:**
```bash
npm run validate:all && npm run validate:web
```
Expected: both exit 0; vitest suite reports 7 new Bug-fix cases + 8 TECH-755 happy-path cases = 15 passing (or more if harness self-test added).

**STOP:** If `validate:all` fails on `validate:frontmatter` due to rule-doc changes → verify `ia/rules/web-backend-logic.md` frontmatter unchanged (still `loaded_by: on-demand`). If `validate:web` fails on typecheck after Step 3 (`PatchOutcome` widening) → re-open Step 3, confirm new variants destructured in the handler switch at `assets/[id]/route.ts`. Do NOT suppress lint errors with `// eslint-disable`.

**MCP hints:** `backlog_issue` (`TECH-756` for scope), `plan_digest_verify_paths` (root `package.json`).


## Final gate

```bash
npm run validate:all
```
