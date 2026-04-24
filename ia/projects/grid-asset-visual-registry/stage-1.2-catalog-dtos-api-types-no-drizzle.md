### Stage 1.2 — Catalog DTOs + API types (no Drizzle)

**Status:** Final

**Objectives:** Author **hand-written TypeScript DTOs** under **`web/types/api/catalog*.ts`** aligned to `0011` / `0012` (per architecture audit: **no** `drizzle-orm` in `web/`). Add shared list-filter + lock + preview-diff shapes for Stage 1.3 routes. Optional **zod** at route boundary per `docs/architecture-audit-change-list-2026-04-22.md`.

**Exit:**

- DTO modules typecheck; **`npm run validate:web`** (or `web` typecheck) passes.
- No drift vs migrations (column names + nullability) — documented spot-check in §7 / Decision Log.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T1.2.1 | Core catalog DTOs (0011) | **TECH-626** | Done (archived) | Hand-written types for `catalog_asset` / `catalog_sprite` / `catalog_asset_sprite` / `catalog_economy` matching `0011`; shapes for join used in **`GET /api/catalog/assets/:id`**. |
| T1.2.2 | Pool DTOs (0012) | **TECH-627** | Done (archived) | Types for `catalog_spawn_pool` + `catalog_pool_member` matching `0012`; test helpers or documented insert pattern for pool membership. |
| T1.2.3 | API filter + lock + preview DTOs | **TECH-628** | Done (archived) | Shared types for list filters (`status`, `category`), optimistic-lock payload (`updated_at`), preview-diff result shape. |
| T1.2.4 | DTO ↔ migration alignment | **TECH-629** | Done (archived) | Wire **`package.json` script** or **doc checklist** so DTO fields stay aligned with `0011`/`0012` SQL (no `drizzle-kit`; SQL is authoritative). |

#### §Stage Closeout Plan

> Stage 1.2 closeout applied inline with ship-stage Pass 2 — **TECH-626**–**TECH-629** archived to `ia/backlog-archive/`, specs deleted, table flipped **Done (archived)**; no glossary/rule migrations.

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-626"
  title: "Core catalog DTOs (0011)"
  priority: high
  notes: |
    Hand-written TS types in `web/types/api/catalog*.ts` for 0011 tables (`catalog_asset`, `catalog_sprite`,
    `catalog_asset_sprite`, `catalog_economy`) matching `db/migrations/0011_catalog_core.sql`. No Drizzle.
    Shapes for join used by planned `GET /api/catalog/assets/:id`. **Architecture audit 2026-04-22** path.
  depends_on:
    - TECH-612
  related: []
  stub_body:
    summary: |
      Type the four core catalog tables for Next handlers using explicit interfaces/types; column alignment to
      0011 SQL; document FK relationships in JSDoc or narrow helper types (no ORM).
    goals: |
      1. DTO field names and nullability match 0011; no silent drift.
      2. Join / composite type for get-by-id route documented for Stage 1.3.
      3. `npm run validate:web` passes for touched `web/` files.
    systems_map: |
      - `web/types/api/` — `catalog*.ts`
      - `db/migrations/0011_catalog_core.sql`
      - `docs/architecture-audit-handoff-2026-04-22.md` Row 2
    impl_plan_sketch: |
      ### Phase 1 — Core DTOs
      - [ ] Add exported types; barrel file if project uses one.
      - [ ] Spot-check every `0011` column; Decision Log for intentional omissions.
- reserved_id: "TECH-627"
  title: "Pool DTOs (0012)"
  priority: high
  notes: |
    Hand-written types for `catalog_spawn_pool` + `catalog_pool_member` per `0012_catalog_spawn_pools.sql`.
    Test insert pattern or small helper. **Depends on** **TECH-615** (0012 DDL) archive.
  depends_on:
    - TECH-615
  related: []
  stub_body:
    summary: |
      Pool + member row types with `weight` and `catalog_asset` FK; enable typed tests without Drizzle.
    goals: |
      1. Types match 0012.
      2. Documented test insert or factory for membership rows.
      3. `validate:web` green.
    systems_map: |
      - `web/types/api/` — pool DTOs
      - `db/migrations/0012_catalog_spawn_pools.sql`
    impl_plan_sketch: |
      ### Phase 1 — Pool DTOs
      - [ ] Export pool + member interfaces; link to `catalog_asset` id type from TECH-626.
      - [ ] Optional test helper; routes stay in 1.3.
- reserved_id: "TECH-628"
  title: "API filter + lock + preview DTOs"
  priority: medium
  notes: |
    List filters, optimistic-lock bodies, preview-diff JSON types — shared under `web/types/api/`; Stage 1.3
    imports. Aligns with **TECH-612**-era column semantics; no Drizzle inference.
  depends_on:
    - TECH-612
  related: []
  stub_body:
    summary: |
      Stabilize filter / body / response types for CRUD + 409 + preview-diff.
    goals: |
      1. Filter + status vocabulary match exploration.
      2. Preview-diff type JSON-serializable.
      3. No route files in this task.
    systems_map: |
      - `web/types/api/`
    impl_plan_sketch: |
      ### Phase 1 — API DTOs
      - [ ] Add shared request/response + preview types.
      - [ ] Glossary: cents + status enums.
- reserved_id: "TECH-629"
  title: "DTO ↔ migration alignment"
  priority: medium
  notes: |
    Script or doc so `web/types/api/catalog*.ts` stays aligned with `0011`/`0012` SQL. No `drizzle-kit`.
    May add root `package.json` script (e.g. `rg`/AST check) or manual checklist in spec §7 + `web/README.md`.
  depends_on: []
  related: []
  stub_body:
    summary: |
      One owner process for DTO vs migration drift (CI or documented human gate).
    goals: |
      1. Clear validation story without reintroducing Drizzle.
      2. No conflicting npm script names.
    systems_map: |
      - `package.json` (root)
      - `web/package.json`
      - `db/migrations/0011_catalog_core.sql`, `db/migrations/0012_catalog_spawn_pools.sql`
    impl_plan_sketch: |
      ### Phase 1 — Alignment policy
      - [ ] Audit existing `validate:web` / `validate:all`; add script or doc gap-fill.
      - [ ] If doc-only: §7 checklist + `web/README.md` link.
```

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

> **Recheck 2026-04-22 (Stage 1.2):** `TECH-626`–`TECH-629` — §1/§2 vs task **Intent**; §7 phases; §8 acceptance; §Plan Digest; frontmatter `phases:`; DTO / no-Drizzle lock vs `docs/architecture-audit-handoff-2026-04-22.md`. Drift candidates: none.

#### §Stage Audit

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._
