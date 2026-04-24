---
purpose: "TECH-774 — Sprite GC admin route + refcount."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/grid-asset-visual-registry-master-plan.md"
task_key: "T3.3.3"
---
# TECH-774 — Sprite GC admin route + refcount

> **Issue:** [TECH-774](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

Author admin-only route or SQL migration / job that finds `catalog_sprite` rows with zero references across `catalog_asset_sprite` + `catalog_pool_member`; `dryRun=true` returns candidate ids; commit path deletes only after allowlist + optimistic-lock guards.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Refcount query joins `catalog_sprite` with `catalog_asset_sprite` + `catalog_pool_member`; emits orphans.
2. `dryRun=true` returns candidate rows; `dryRun=false` commits delete within single transaction.
3. Admin caller_agent allowlist gate on mutating path (reuse existing web/ auth allowlist pattern).
4. Telemetry: delete count + candidate count per run.

### 2.2 Non-Goals (Out of Scope)

1. Automatic GC — admin-triggered only.
2. Sprite retirement UI — handled separately.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Admin | I want to identify orphan sprites | `dryRun=true` returns candidate set |
| 2 | Admin | I want safe deletion with audit trail | Allowlist gate + transaction wrapper + telemetry |

## 4. Current State

### 4.1 Domain behavior

No GC tool. Orphan sprites accumulate over time.

### 4.2 Systems map

- `db/migrations/` — no schema change expected (runtime query / route only)
- `web/app/api/catalog/*` — new admin subroute (App Router)
- `web/types/api/catalog*.ts` — hand-written DTOs
- `tools/mcp-ia-server/src/auth/caller-allowlist.ts` — mutation allowlist
- `docs/grid-asset-visual-registry-exploration.md` §8.4 point 11

### 4.3 Implementation investigation notes

Refcount query: LEFT JOIN + HAVING COUNT = 0. Allowlist reuses existing web/ auth pattern.

## 5. Proposed Design

### 5.1 Target behavior (product)

Admin endpoint `POST /api/catalog/sprites/gc` with `{ dryRun }` body. `dryRun=true` returns candidate ids. `dryRun=false` deletes + returns count. Allowlist gate on mutating path.

### 5.2 Architecture / implementation

SQL refcount query (LEFT JOIN + HAVING COUNT = 0). App Router endpoint. DTO under `web/types/api/catalog-gc.ts`. Allowlist gate + transaction wrapper on commit path.

### 5.3 Method / algorithm notes

```sql
SELECT cs.id
FROM catalog_sprite cs
LEFT JOIN catalog_asset_sprite cas ON cs.id = cas.sprite_id
LEFT JOIN catalog_pool_member cpm ON cs.id = cpm.sprite_id
WHERE cas.sprite_id IS NULL AND cpm.sprite_id IS NULL
```

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Admin-triggered only | Safe + auditable | Automatic background job (less control) |
| 2026-04-24 | Allowlist gate reuses web/ pattern | Consistency + single source of truth | New allowlist hardcoded in route (duplication) |

## 7. Implementation Plan

### Phase 1 — Refcount + dry-run

- [ ] Author SQL refcount query (LEFT JOIN + HAVING COUNT = 0).
- [ ] Expose `POST /api/catalog/sprites/gc` with `{ dryRun }` body.
- [ ] DTO under `web/types/api/catalog-gc.ts`.
- [ ] Allowlist gate + transaction wrapper on commit path.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| dryRun returns candidates | Server test | Vitest / Jest fixture | Seeds catalog + sprite + pool rows; queries orphans |
| Referenced row preserved | Server test | Same fixture, refcount path | Orphan deleted, referenced row untouched |
| Allowlist gate enforced | Server test | Auth allowlist mock | Non-admin caller blocked |

## 8. Acceptance Criteria

- [ ] Refcount query joins `catalog_sprite` with `catalog_asset_sprite` + `catalog_pool_member`; emits orphans.
- [ ] `dryRun=true` returns candidate rows; `dryRun=false` commits delete within single transaction.
- [ ] Admin caller_agent allowlist gate on mutating path (reuse existing web/ auth allowlist pattern).
- [ ] Telemetry: delete count + candidate count per run.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- None yet.

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

Ship admin-only `POST /api/catalog/sprites/gc` App Router endpoint that finds `catalog_sprite` rows with zero references in `catalog_asset_sprite` (the ONLY direct FK to `catalog_sprite.id`); `dryRun=true` (default) returns candidate ids; `dryRun=false` deletes inside a single transaction + returns deleted count.

### §Acceptance

- [ ] New route file at `web/app/api/catalog/sprites/gc/route.ts` — exports `POST` handler + `dynamic = "force-dynamic"` (mirrors `web/app/api/catalog/assets/[id]/retire/route.ts`).
- [ ] Request / response DTOs at `web/types/api/catalog-gc.ts` — `CatalogSpritesGcBody { dryRun?: boolean }` + `CatalogSpritesGcDryRunResponse { candidates: string[]; count: number }` + `CatalogSpritesGcCommitResponse { deletedIds: string[]; deletedCount: number }` (ids typed `string` to mirror `CatalogSpriteRow.id`).
- [ ] Refcount query: `SELECT cs.id FROM catalog_sprite cs LEFT JOIN catalog_asset_sprite cas ON cs.id = cas.sprite_id WHERE cas.sprite_id IS NULL` — the ONLY FK into `catalog_sprite.id` is `catalog_asset_sprite.sprite_id` (per `db/migrations/0011_catalog_core.sql`); `catalog_pool_member` references `asset_id`, NOT `sprite_id`, so pool membership is already a transitive asset-level guard — sprite GC needs only the `catalog_asset_sprite` join.
- [ ] `dryRun` default = `true` when request body absent / invalid JSON — matches `retire/route.ts` idiom (`body = {}` on parse error).
- [ ] `dryRun=false` path wraps SELECT + DELETE in a single `sql.begin(...)` transaction (postgres.js pattern); returns `{ deletedIds, deletedCount }`.
- [ ] Auth gate uses existing `web/` admin check (NOT MCP `tools/mcp-ia-server/src/auth/caller-allowlist.ts`); non-admin caller → `catalogJsonError(403, "not_allowed", "Admin required")`. Concrete admin-check symbol resolved at implement time by `grep -rn "admin" web/lib web/app/api`; spec pins the call shape (`catalogJsonError(403, "not_allowed", ...)`), not the symbol.
- [ ] Telemetry via `console.warn` (matching existing web/ api-route logging convention — no new logger): `"[catalog.sprites.gc] dryrun candidates=N"` + `"[catalog.sprites.gc] commit deleted=N"`.
- [ ] Vitest test file at `web/app/api/catalog/sprites/gc/route.test.ts` (colocated per vitest + Next.js convention) — authored by TECH-775.
- [ ] `npm -w web run typecheck` exits 0 after edits.
- [ ] `npm -w web run test` exits 0 with the GC test file present.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `gc_dryRun_default_returnsCandidates` | seed 3 sprites, 2 bound via `catalog_asset_sprite`, body `{}` | `200 { candidates: ["<orphanId>"], count: 1 }` | vitest |
| `gc_dryRun_true_explicit` | same seed, body `{ dryRun: true }` | same as default | vitest |
| `gc_commit_deletesOrphansOnly` | same seed, body `{ dryRun: false }` | `200 { deletedCount: 1, deletedIds: ["<orphanId>"] }`; referenced rows present | vitest |
| `gc_referencedByAssetSprite_preserved` | sprite bound via `catalog_asset_sprite` only | dryRun excludes it; commit does NOT delete it | vitest |
| `gc_poolMemberAssetLinkedSprite_preserved` | sprite bound to asset X; X in `catalog_pool_member` | pool is transitive through `catalog_asset_sprite`; sprite preserved | vitest |
| `gc_nonAdmin_rejected` | auth header missing / non-admin | `403 { error: "not_allowed" }`; no DB query | vitest |
| `gc_commit_rollbackOnError` | injected mid-delete failure | no rows deleted (transaction rolled back); error response 500 | vitest |
| `gc_invalidJsonBody_defaultsDryRun` | malformed body | `200` dry-run response | vitest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| `POST /api/catalog/sprites/gc` (no body, admin) | `200 { candidates: [...], count: N }` | `dryRun` defaults to `true` |
| `POST { dryRun: false }` (admin) | `200 { deletedCount: N, deletedIds: [...] }` | Single transaction |
| `POST { dryRun: true }` (non-admin) | `403 { error: "not_allowed", message: "Admin required" }` | Auth gate fires before DB |
| Seed `catalog_sprite{id:100}` + `catalog_asset_sprite{sprite_id:100}` | dryRun excludes `100` | `catalog_asset_sprite` join hits |
| Seed `catalog_sprite{id:300}` standalone | dryRun returns `["300"]` | True orphan |
| Seed `catalog_sprite{id:200}` + `catalog_asset_sprite{sprite_id:200, asset_id:A}` + `catalog_pool_member{asset_id:A}` | dryRun excludes `200` | Transitive via asset binding |

### §Mechanical Steps

#### Step 1 — Author DTOs at `web/types/api/catalog-gc.ts`

**Goal:** Hand-written request + response DTOs; id types match `CatalogSpriteRow.id` (string, per existing `web/types/api/catalog-sprite.ts`).

**Edits:**
- `web/types/api/catalog-gc.ts` — **operation**: create
  **after** — new file contents:
  ```ts
  /**
   * TECH-774 — Hand-written DTOs for POST /api/catalog/sprites/gc admin route.
   * Sprite GC refcount: only catalog_asset_sprite FKs into catalog_sprite.id.
   */

  export interface CatalogSpritesGcBody {
    dryRun?: boolean;
  }

  export interface CatalogSpritesGcDryRunResponse {
    candidates: string[];
    count: number;
  }

  export interface CatalogSpritesGcCommitResponse {
    deletedIds: string[];
    deletedCount: number;
  }
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm -w web run typecheck`

**Gate:**
```bash
npm -w web run typecheck
```
Expectation: exit 0 (create-target referenced by Step 2 import — typecheck fails if missing or DTO names drift).

**STOP:** File missing after write → re-run Step 1 (Write tool). Drift in DTO name from `CatalogSpritesGc*` → re-open Step 1 and align names before Step 2 imports.

**MCP hints:** `plan_digest_verify_paths`, `plan_digest_resolve_anchor`.

#### Step 2 — Author `POST /api/catalog/sprites/gc` route

**Goal:** Implement the App Router POST handler using the `retire/route.ts` neighbor pattern — body parse, admin gate, refcount query, dry-run / commit branch, telemetry.

**Edits:**
- `web/app/api/catalog/sprites/gc/route.ts` — **operation**: create
  **after** — new file contents:
  ```ts
  import { NextResponse, type NextRequest } from "next/server";
  import { getSql } from "@/lib/db/client";
  import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
  import type {
    CatalogSpritesGcBody,
    CatalogSpritesGcDryRunResponse,
    CatalogSpritesGcCommitResponse,
  } from "@/types/api/catalog-gc";
  import { requireAdmin } from "@/lib/auth/require-admin"; // resolve concrete symbol at implement-time; see STOP.

  export const dynamic = "force-dynamic";

  /**
   * @see ia/projects/TECH-774.md — sprite GC refcount + dry-run + commit.
   * Refcount: only catalog_asset_sprite.sprite_id FKs into catalog_sprite.id
   * (catalog_pool_member is asset-level, transitively covered via catalog_asset_sprite).
   */
  export async function POST(request: NextRequest) {
    const authErr = await requireAdmin(request);
    if (authErr) return authErr; // returns 403 catalogJsonError when non-admin.

    let body: CatalogSpritesGcBody = {};
    try {
      body = (await request.json()) as CatalogSpritesGcBody;
    } catch {
      // malformed body → treat as dry-run default.
    }
    const dryRun = body.dryRun !== false; // default true.

    const sql = getSql();
    try {
      const orphanRows = await sql`
        select cs.id
        from catalog_sprite cs
        left join catalog_asset_sprite cas on cs.id = cas.sprite_id
        where cas.sprite_id is null
        order by cs.id asc
      `;
      const candidates = (orphanRows as unknown as { id: string | number }[])
        .map((r) => String(r.id));

      if (dryRun) {
        console.warn(`[catalog.sprites.gc] dryrun candidates=${candidates.length}`);
        const out: CatalogSpritesGcDryRunResponse = {
          candidates,
          count: candidates.length,
        };
        return NextResponse.json(out, { status: 200 });
      }

      const deletedIds = await sql.begin(async (tx) => {
        if (candidates.length === 0) return [] as string[];
        const deleted = await tx`
          delete from catalog_sprite
          where id = any(${candidates}::bigint[])
          returning id
        `;
        return (deleted as unknown as { id: string | number }[]).map((r) => String(r.id));
      });

      console.warn(`[catalog.sprites.gc] commit deleted=${deletedIds.length}`);
      const out: CatalogSpritesGcCommitResponse = {
        deletedIds,
        deletedCount: deletedIds.length,
      };
      return NextResponse.json(out, { status: 200 });
    } catch (e) {
      if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
        return catalogJsonError(500, "internal", "Database not configured", { logContext: "gc" });
      }
      return responseFromPostgresError(e, "Sprite GC failed");
    }
  }
  ```
- `invariant_touchpoints`:
  - id: `web-backend-error-envelope`
    gate: `rule_content web-backend-logic`
    expected: pass
  - id: `catalog-sprite-fk-graph`
    gate: `grep -n "sprite_id bigint NOT NULL REFERENCES catalog_sprite" db/migrations/0011_catalog_core.sql`
    expected: pass
- `validator_gate`: `npm -w web run typecheck`

**Gate:**
```bash
npm -w web run typecheck
```
Expectation: exit 0.

**STOP:**
- Typecheck fails on `requireAdmin` import → admin-check symbol is implementer-resolved: run `grep -rn "admin" web/lib web/app/api` and wire the discovered admin-check; if NO existing admin gate → file a blocker TECH-XXX (Auth must not be invented); do NOT close Step 2 without a real gate that returns 403 on non-admin.
- Typecheck fails on `sql.begin(...)` — `postgres` driver transaction API; if absent, substitute the driver's transaction primitive (see `web/lib/db/client.ts` for the concrete export) and re-open Step 2.

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_render_literal web/app/api/catalog/assets/[id]/retire/route.ts 1 67`, `backlog_issue TECH-774`, `rule_content web-backend-logic`.

#### Step 3 — Decision Log entries (`replaced_by` auth plane + FK graph)

**Goal:** Record the two spec corrections surfaced in §Plan Author §Findings: (a) admin allowlist lives in `web/`, not MCP; (b) `catalog_sprite` FK graph includes only `catalog_asset_sprite` — `catalog_pool_member` is asset-level.

**Edits:**
- `ia/projects/TECH-774.md` — **operation**: edit
  **before**:
  ```
  | 2026-04-24 | Allowlist gate reuses web/ pattern | Consistency + single source of truth | New allowlist hardcoded in route (duplication) |
  ```
  **after**:
  ```
  | 2026-04-24 | Allowlist gate reuses web/ pattern | Consistency + single source of truth | New allowlist hardcoded in route (duplication) |
  | 2026-04-24 | Admin gate lives in web/ (NOT MCP caller-allowlist) | web/ App Router + MCP are distinct auth planes; stub §4.2 misattribution corrected | Share `tools/mcp-ia-server/src/auth/caller-allowlist.ts` (wrong plane) |
  | 2026-04-24 | GC refcount joins only catalog_asset_sprite | `catalog_sprite.id` has exactly one FK: `catalog_asset_sprite.sprite_id`; `catalog_pool_member` references `asset_id`, transitively covered | Triple join incl. `catalog_pool_member.sprite_id` (column does not exist) |
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** Decision Log pipe count mismatch → re-open Step 3 with `| a | b | c | d |` column parity.

**MCP hints:** `plan_digest_resolve_anchor`.

### §Findings

- Systems-map drift: stub references `tools/mcp-ia-server/src/auth/caller-allowlist.ts` as the allowlist source, but web App Router uses web/ middleware (distinct auth plane). Step 3 records the correction in Decision Log.
- Neighbor pattern confirmed: `web/app/api/catalog/assets/[id]/retire/route.ts` + `web/types/api/catalog-*.ts` establish the route + DTO shape mirrored here.
- FK graph confirmed from `db/migrations/0011_catalog_core.sql:38` + `db/migrations/0012_catalog_spawn_pools.sql:14` — only `catalog_asset_sprite.sprite_id` references `catalog_sprite.id`; `catalog_pool_member` references `asset_id`, not `sprite_id`. GC refcount query simplified accordingly.
- `web/package.json` confirms `vitest` + admin-check symbol NOT yet a known export — Step 2 STOP requires implementer resolution before close.
- Web CLAUDE.md note: Next.js version in this repo may differ from common training data; Step 2 STOP directs the implementer to read `node_modules/next/dist/docs/` for App Router + Route Handler conventions.

## Open Questions (resolve before / during implementation)

- **Glossary candidate:** `Grid asset catalog` — see TECH-772 §Open Questions.
- **Auth plane confirmation:** confirm web/ admin auth middleware path at implement time (stub §4.2 misattributed to MCP allowlist).
- **Transaction isolation level:** `FOR UPDATE` lock vs retry-on-serialization — implementer decision per web/ DB driver conventions.
- **Telemetry sink:** existing web/ logger vs new structured event — reuse repo convention.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor | critical._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
