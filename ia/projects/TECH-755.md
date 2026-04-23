---
purpose: "TECH-755 — Catalog API test harness + happy-path coverage."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T1.3.1"
phases:
  - "Phase 1 — DB setup + HTTP helper"
  - "Phase 2 — Seed fixture + happy-path tests"
---
# TECH-755 — Catalog API test harness + happy-path coverage

> **Issue:** [TECH-755](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Land reusable integration test harness for catalog API routes so Stage 1.3 Phase 2 bug fixes and
future catalog route work ship with paired regression tests. Harness handles DB lifecycle, HTTP call
ergonomics, and seed fixtures; happy-path suite locks shipped 200/201 responses as snapshots.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Per-test DB isolation (transactional rollback or truncate strategy) — no cross-test leakage.
2. HTTP helper covers GET/POST/PATCH with JSON body + status assertion; matches Next.js App Router handler signatures.
3. Seed fixture produces deterministic seven Zone S rows + minimal sprite/economy bindings.
4. Happy-path suite green: list, get-by-id joined, create, patch, retire, preview-diff.
5. Test script wired so `npm run validate:web` (or documented sibling) picks it up.

### 2.2 Non-Goals (Out of Scope)

1. Behavior fixes — Phase 2 task (TECH-756).
2. MCP tool tests — covered elsewhere (Stage 1.4).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Want reusable test harness for catalog API routes | Harness supports per-test DB isolation, HTTP call semantics, seed fixtures; happy-path suite green |
| 2 | QA | Want regression tests locked to shipped 200/201 responses | All six happy-path routes have snapshot tests wired to `validate:web` |

## 4. Current State

### 4.1 Domain behavior

Shipped `/api/catalog/*` routes (Stage 1.1–1.2: TECH-626–TECH-629, TECH-640–TECH-645) lack integration test coverage and harness. Phase 2 bug fixes (TECH-756) depend on test infra.

### 4.2 Systems map

- New: `web/` test suite under agreed path (e.g. `web/tests/api/catalog/*.test.ts`) per existing test conventions.
- Existing routes: `web/app/api/catalog/assets/route.ts`, `web/app/api/catalog/assets/[id]/route.ts`, `web/app/api/catalog/assets/[id]/retire/route.ts`, `web/app/api/catalog/preview-diff/route.ts`.
- DTOs: `web/types/api/catalog*.ts` (from TECH-626/627/628 archived).
- Migrations: `db/migrations/0011_catalog_core.sql`, `db/migrations/0012_catalog_spawn_pools.sql`.
- Rule: `ia/rules/web-backend-logic.md` (consume as read; Phase 2 task TECH-756 edits it).

### 4.3 Implementation investigation notes

Test framework alignment required (vitest/jest per repo convention). DB isolation strategy confirmed (transactional rollback preferred, truncate fallback). Seed fixture can reuse Stage 1.1 catalog seed SQL or inline TS factory.

## 5. Proposed Design

### 5.1 Target behavior (product)

Integration test harness provides:
- Per-test DB isolation preventing cross-test leakage.
- HTTP helper ergonomic for GET/POST/PATCH with assertion flow.
- Seed fixture producing deterministic seven Zone S rows + minimal sprite/economy bindings.
- Happy-path tests covering list (published-default filter), get-by-id (joined snapshot), create 201, patch 200, retire 200, preview-diff 200.

### 5.2 Architecture / implementation

Test suite structure: `web/tests/api/catalog/` directory housing harness modules + test files.

Harness modules:
- DB lifecycle helper (transactional rollback or truncate per scheme).
- HTTP call wrapper (matches Next.js App Router handler signatures).
- Seed fixture loader (deterministic Zone S asset rows).

Happy-path test file: `assets.test.ts` covering six routes (list, get-by-id, create, patch, retire, preview-diff).

### 5.3 Method / algorithm notes

DB lifecycle: wrap each test in transaction → rollback on teardown (PostgreSQL recommended), fallback truncate catalog tables.

HTTP helper: wrap Next handlers or dev server deterministically; support GET/POST/PATCH body + status assertion.

Seed fixture: reuse or inline Stage 1.1 catalog seed SQL (seven Zone S asset rows + sprite/economy bindings).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-23 | Defer framework + fixture strategy decision | Implementing agent confirms repo test convention in first commit | vitest vs jest; SQL seed vs TS factory |

## 7. Implementation Plan

### Phase 1 — DB setup + HTTP helper

- [ ] Pick test framework matching repo convention (vitest/jest).
- [ ] Implement DB lifecycle helper (transactional rollback preferred; fallback truncate of catalog_* tables).
- [ ] Implement HTTP call helper that wraps Next handlers or hits dev server deterministically.
- [ ] Document test runner integration (e.g. `npm run validate:web`).

### Phase 2 — Seed fixture + happy-path tests

- [ ] Implement seed fixture loader (re-use Stage 1.1 seed SQL or inline factory).
- [ ] Author six happy-path tests (list, get-by-id, create, patch, retire, preview-diff).
- [ ] Wire npm script; document in `web/README.md` if new entry.
- [ ] Verify `npm run validate:web` picks up suite.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Integration test suite green; all happy-path routes covered | Node | `npm run validate:web` | Harness + six happy-path tests; all 200/201 responses locked as snapshots |
| Test runner wired per repo convention | Node | `npm run validate:web` (or documented sibling) | Confirm in first commit message |

## 8. Acceptance Criteria

- [ ] Integration test harness green; all happy-path routes covered.
- [ ] Per-test DB isolation verified (no cross-test leakage).
- [ ] `npm run validate:web` picks up catalog test suite and passes.
- [ ] Six happy-path tests all green (list, get-by-id, create, patch, retire, preview-diff).
- [ ] Test code + harness modules documented in PR.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | Seed ids start at 0; route rejects `idNum < 1`. | `0013_zone_s_seed.sql` uses id sequence starting at 0 (Zone S convention); shipped `loadCatalogAssetById` / `patchCatalogAsset` reject `idNum < 1`. | Test SELECTs filter `and id >= 1` to pick a valid id. Route-level validation left untouched (shipped contract). |
| 2 | `postgres` driver rejects raw `BEGIN;`/`COMMIT;` inside `sql.unsafe()`. | `0013` seed wraps statements in `BEGIN;`/`COMMIT;`; driver reserves `sql.begin()` for managed txns only. | Harness `seedZoneS()` regex-strips `BEGIN;`/`COMMIT;` before `sql.unsafe()`. |
| 3 | PG `timestamptz` stores microseconds; JS `Date.toISOString()` only milliseconds; optimistic-lock string compare fails round-trip. | `patch-asset.ts` compares `updated_at` as raw string (not timestamp); PG `now()` default has µs, ISO has ms. | Harness seed replaces `now()` with `date_trunc('milliseconds', now())`; tests fetch `updated_at` via shipped GET (already ms-truncated JSON) before PATCH. |
| 4 | `catalog_sprite.provenance` CHECK is `IN ('hand','generator')`, not `'manual'`. | Plan digest assumed `'manual'`; migration `0011` allowlist is `hand|generator`. | Harness + tests use `'hand'`. |
| 5 | `catalog_asset_sprite.slot` CHECK excludes `'default'`. | Shipped allowlist is `world|button_target|button_pressed|button_disabled|button_hover`. | POST-create test uses `slot: "world"`. |
| 6 | **Bug 7 (shipped, out of scope):** `patchCatalogAsset` returns stale composite in 200 response. | Post-UPDATE re-read calls `loadCatalogAssetById(idParam)` which uses `getSql()` (fresh pool connection) instead of the enclosing `tx` from `sql.begin()`. PG READ COMMITTED → sees pre-UPDATE snapshot of the uncommitted row. | Happy-path test verifies persistence via subsequent GET round-trip (sidesteps stale composite). Route fix captured as **Bug 7** in TECH-756. |

## 10. Lessons Learned

- …

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

## Open Questions

- Framework choice: align with whatever `web/` already uses — confirm in first PR commit. **Resolved during author pass:** `web/package.json` locks `vitest ^4.1.4`; existing `.spec.ts` files under `web/tests/` — align new suite to that convention.
- Fixture strategy: SQL seed vs TS factory — pick per repo consistency with existing DB tests. **Tentative:** re-use migration `0013_zone_s_seed.sql` seed; harness only truncates between tests.
- Glossary candidates (surface terms not yet in `ia/specs/glossary.md`; author-pass flagged — do NOT edit glossary here): `catalog asset`, `catalog route`, `sprite bind`, `preview-diff`, `joined composite`.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
