### Stage 1.1 — Migrations + Zone S seed

**Status:** Final

**Objectives:** Create **`catalog_asset`**, **`catalog_sprite`**, **`catalog_asset_sprite`**, **`catalog_economy`**, then pool tables in 0012; enforce uniqueness + cents + FK graph; seed **Zone S** reference rows matching current seven sub-types.

**Exit:**

- `0011_catalog_core.sql` + `0012_catalog_spawn_pools.sql` committed; `npm run db:migrate` (or repo-standard migrate) succeeds on clean DB.
- Zone S seed maps **ids 0–6** to slugs compatible with `Assets/Resources/Economy/zone-sub-types.json` intent.
- Document rollback / one-shot repair note in task §Findings if needed.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T1.1.1 | Author 0011 core DDL | **TECH-612** | Done (archived) | Add `catalog_asset`, `catalog_sprite`, `catalog_asset_sprite`, `catalog_economy` per exploration §8.1; **`(category, slug)`** UNIQUE; money columns **`NOT NULL`** where required; **`updated_at`** trigger or app-managed column. |
| T1.1.2 | Indexes FKs and status filters | **TECH-613** | Done (archived) | Index **`status`**, **`asset_id`** joins, **`sprite_id`** lookups; FK `ON DELETE` policy aligned with soft-retire + GC story (document chosen behavior in §Implementation). |
| T1.1.3 | Migration smoke + idempotency | **TECH-614** | Done (archived) | Run migrate twice / fresh DB; verify no duplicate enum casts; add CI-friendly **`db:migrate`** note or script touch if repo requires. |
| T1.1.4 | Author 0012 pool DDL | **TECH-615** | Done (archived) | `catalog_spawn_pool`, `catalog_pool_member` + **`weight`**; FK to `catalog_asset`. |
| T1.1.5 | Seed seven Zone S assets | **TECH-616** | Done (archived) | SQL seed or `tools/` seed runner inserts seven rows + placeholder sprite bind strategy (nullable until art lands). |
| T1.1.6 | Pool seed smoke optional | **TECH-617** | Done (archived) | Minimal pool row proving **`catalog_pool_member`** write path; optional if MVP defers pools until Step 2 consumer needs it — if deferred, document explicit deferral in §Findings (still land empty tables). |

#### §Stage Closeout Plan

> Stage 1.1 closeout applied 2026-04-22 — archive `ia/backlog/TECH-612`–`TECH-617` → `ia/backlog-archive/`; delete matching `ia/projects/TECH-612`–`TECH-617` specs; task rows → **Done (archived)**; `materialize-backlog.sh` + `validate:all`.

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-612"
  title: "Author 0011 core DDL"
  priority: medium
  notes: |
    Add catalog core migration `db/migrations/0011_catalog_core.sql`: `catalog_asset`, `catalog_sprite`,
    `catalog_asset_sprite`, `catalog_economy` per exploration §8.1. Enforce `(category, slug)` UNIQUE,
    money NOT NULL, `updated_at` + optimistic-lock story. Touches `db/migrations/` only; aligns grid asset
    catalog master plan Step 1 Stage 1.1 Phase 1.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Land `0011_catalog_core.sql` with four core tables, uniqueness on `(category, slug)`, cents columns,
      and revision column for later PATCH 409 behavior.
    goals: |
      1. `catalog_asset` row carries identity, status, category, slug, `updated_at`.
      2. `catalog_sprite` + `catalog_asset_sprite` bind sprites to assets with slot rules.
      3. `catalog_economy` holds Zone S / price fields in integer cents.
      4. DDL is idempotent on fresh DB and matches exploration §8.1 naming.
    systems_map: |
      - `db/migrations/0011_catalog_core.sql` (new)
      - `docs/grid-asset-visual-registry-exploration.md` §8.1
      - `ia/specs/economy-system.md` — Zone S vocabulary
    impl_plan_sketch: |
      ### Phase 1 — Core DDL
      - [ ] Author `0011` with enums/checks, FK stubs, UNIQUE `(category, slug)`, NOT NULL cents.
      - [ ] Document trigger vs app-owned `updated_at` in §Implementation / Decision Log.
- reserved_id: "TECH-613"
  title: "Indexes FKs and status filters"
  priority: medium
  notes: |
    Secondary indexes + FK `ON DELETE` policy for core catalog tables: `status` filters, join paths
    (`asset_id`, `sprite_id`). Record soft-retire + GC behavior in spec §Implementation so Stage 1.2
    DTOs + routes mirror chosen policy.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Add indexes and FK actions on `0011` tables so list/get routes and joins stay bounded; align delete
      semantics with soft-retire story.
    goals: |
      1. Index columns used in published/draft filters and FK joins.
      2. `ON DELETE` / `ON UPDATE` choices documented (no silent orphan rows).
      3. Behavior matches exploration retire / `replaced_by` narrative.
    systems_map: |
      - `db/migrations/0011_catalog_core.sql`
      - `docs/grid-asset-visual-registry-exploration.md` §8.1–8.2
    impl_plan_sketch: |
      ### Phase 1 — Indexes + FK policy
      - [ ] Add indexes for `status`, join keys; set FK actions.
      - [ ] Capture policy prose in project spec §7 + §Findings if edge case.
- reserved_id: "TECH-614"
  title: "Migration smoke + idempotency"
  priority: medium
  notes: |
    Prove `npm run db:migrate` on clean DB; re-run migrate (no duplicate errors). Catch enum cast / naming
    drift. Touch `package.json` or docs only if repo requires explicit CI hook for migrate replay.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Verify migration apply + idempotency story for `0011` before pool migration lands.
    goals: |
      1. Clean DB: migrate succeeds.
      2. Second migrate no-op or safe skip per repo pattern.
      3. Any script/doc gap for CI noted in §Findings.
    systems_map: |
      - `db/migrations/0011_catalog_core.sql`
      - `package.json` (optional script touch)
      - `tools/scripts/` migrate entrypoints if cited by repo
    impl_plan_sketch: |
      ### Phase 1 — Smoke
      - [ ] Run migrate twice locally; log commands in §Verification.
      - [ ] File repair note if idempotency gap found.
- reserved_id: "TECH-615"
  title: "Author 0012 pool DDL"
  priority: medium
  notes: |
    Add `db/migrations/0012_catalog_spawn_pools.sql`: `catalog_spawn_pool`, `catalog_pool_member` with
    `weight`, FK to `catalog_asset`. Empty tables acceptable at end of Stage; seeds follow in sibling tasks.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Introduce spawn pool tables and membership graph for weighted random rolls later in Step 1 consumers.
    goals: |
      1. Pool + member tables with `weight` column and FK to assets.
      2. Migration ordering after `0011` enforced in filename chain.
      3. Ready for optional seed smoke in T1.1.6.
    systems_map: |
      - `db/migrations/0012_catalog_spawn_pools.sql` (new)
      - `docs/grid-asset-visual-registry-exploration.md` §8.1 pool bullets
    impl_plan_sketch: |
      ### Phase 1 — Pool DDL
      - [ ] Author `0012` with constraints + indexes for pool lookups.
      - [ ] Note deferral if pools unused until Step 2 (tables still exist).
- reserved_id: "TECH-616"
  title: "Seed seven Zone S assets"
  priority: medium
  notes: |
    SQL seed or `tools/` runner inserts seven Zone S catalog rows; ids 0–6 map to slugs compatible with
    `Assets/Resources/Economy/zone-sub-types.json` intent. Sprite binds nullable until art; document placeholder
    strategy.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Seed reference `catalog_asset` (+ economy rows) for seven Zone S sub-types so Unity / API consumers
      can rely on stable ids 0–6.
    goals: |
      1. Seven rows with correct slugs / categories per economy spec.
      2. Cents / registry fields populated or explicitly defaulted.
      3. Seed is repeatable (fixture SQL or idempotent upsert pattern).
    systems_map: |
      - `db/migrations/` or `tools/` seed artifact (per chosen approach)
      - `Assets/Resources/Economy/zone-sub-types.json`
      - `ia/specs/economy-system.md` — **ZoneSubTypeRegistry**
    impl_plan_sketch: |
      ### Phase 1 — Zone S seed
      - [ ] Author seed SQL or runner; wire into migrate or documented one-shot.
      - [ ] Verify row count + id range in §Verification.
- reserved_id: "TECH-617"
  title: "Pool seed smoke optional"
  priority: low
  notes: |
    Optional minimal `catalog_pool_member` row proving write path; if MVP defers pools, document deferral in
    §Findings while keeping empty pool tables from `0012`.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Prove optional pool membership insert or explicitly defer with recorded rationale.
    goals: |
      1. Either minimal pool+member seed exists OR §Findings states deferral with empty tables OK.
      2. No broken FK references to seeded assets.
    systems_map: |
      - `db/migrations/0012_catalog_spawn_pools.sql`
      - Seed artifact from T1.1.5 if reused
    impl_plan_sketch: |
      ### Phase 1 — Optional pool smoke
      - [ ] Insert minimal pool/member rows OR document deferral.
      - [ ] Note outcome in §Findings for Step 2 consumers.
```

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._
