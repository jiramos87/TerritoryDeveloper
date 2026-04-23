# Grid asset visual registry — Master Plan (Bucket 12 MVP spine)

> **Status:** In Progress — Step 3 / Stage 3.1
>
> **Scope:** Postgres-backed **grid asset catalog** (identity, sprites, economy, spawn pools) as source of truth; **HTTP + MCP** for agents; **Unity boot snapshot** consumed by **`GridAssetCatalog`** (no new singleton — Inspector + `FindObjectOfType` per `unity-invariants` #4); **Zone S** first consumer via **`ZoneSubTypeRegistry`** convergence; **`PlacementValidator`** owns place-here legality; **`wire_asset_from_catalog`** bridge kind for design-system-safe Control Panel wiring; export + import hygiene + IA scene contract. **Out:** sprite-gen composition logic (Bucket 5), deep sim rules beyond catalog reads, `web/` dashboard product UI (Bucket 9 transport only — this plan adds `/api/catalog/*` on the existing Next app). Post-MVP extensions → recommend `docs/grid-asset-visual-registry-post-mvp-extensions.md` (not authored by this workflow).
>
> **Exploration source:** `docs/grid-asset-visual-registry-exploration.md` (§8 Design Expansion — Chosen approach D, Architecture diagram, Subsystem impact table, Implementation points 1–12, Examples, Review notes; §4 locked decisions; §10 code refs).
>
> **Locked decisions (do not reopen in this plan):**
> - Catalog source of truth = **Postgres**; **`db/migrations/*.sql` is authoritative**. **`web/`** has **no Drizzle** (removed 2026-04-22 per `docs/architecture-audit-handoff-2026-04-22.md` Row 2); route/API typing uses **hand-written DTOs** under **`web/types/api/catalog*.ts`**. Unity loads **boot-time snapshot**; Resources JSON is **derived**, not authoritative.
> - **Sprite-first** authoring in DB rows; export step enforces **PPU / pivot** hygiene for allowlisted paths; **no collider** on baked world tiles under current **`GridManager`** hit-test contract.
> - Money in DB/API = **integer cents**; saves store stable **`asset_id`** (numeric PK); **`replaced_by`** soft-remap on load.
> - **Draft / published / retired** visibility; list defaults **published**; **`(category, slug)`** unique.
> - **Missing-asset policy:** dev = loud placeholder; ship = hide row + telemetry (per exploration §8.2).
> - **Concurrency:** optimistic **`updated_at`** on writes; conflicting PATCH returns retriable error.
> - **Bucket 12** child under `ia/projects/full-game-mvp-master-plan.md` (umbrella edit is a **separate** follow-up task, not auto-applied here).
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Coordination:** **`ia/projects/ui-polish-master-plan.md`** owns widget/visual contracts; this plan owns **catalog + bridge recipes**. **`ia/projects/sprite-gen-master-plan.md`** feeds **`generator_archetype_id`** + paths. **`ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`** / **`ia/projects/session-token-latency-master-plan.md`** = registration-only follow-ups when new MCP kinds ship.
>
> **Read first if landing cold:**
> - `docs/grid-asset-visual-registry-exploration.md` — full design + §8 ground truth (amended 2026-04-22: **no Drizzle in `web/`**; DTOs in `web/types/api/`).
> - `docs/architecture-audit-handoff-2026-04-22.md` — **Pick 7** (Drizzle drop) + `docs/db-boundaries.md` when present.
> - `ia/specs/economy-system.md` §Zone sub-type registry (`lineStart` 28) + Zone S — **`ZoneSubTypeRegistry`** vocabulary.
> - `ia/specs/ui-design-system.md` §1 Foundations + §2 Components — **`UiTheme`**, **`IlluminatedButton`**, Control Panel paths (appendix lands Step 4).
> - `ia/specs/persistence-system.md` — Load pipeline order (`lineStart` 24) before mutating save fields.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — cardinality (≥2 tasks/phase).
> - `ia/rules/invariants.md` — #1 (specs vs `ia/projects/`), #2 (`reserve-id.sh`), #3 (MCP-first retrieval).
> - `ia/rules/unity-invariants.md` — #3 (no `FindObjectOfType` in hot loops), #4 (no new singletons — **`GridAssetCatalog`** is scene **`MonoBehaviour`**), #5 (no direct `cellArray` — **`PlacementValidator`** consumes **`GridManager`** API), #6 (do not grow **`GridManager`** — extract helpers).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | Skeleton | Planned | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`; `Skeleton` + `Planned` authored by `master-plan-new` / `stage-decompose`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file-plan` + `stage-file-apply` → task rows gain `Issue` id + `Draft` status; `stage-file-apply` also flips Stage header `Draft/Planned → In Progress` (R2) and plan top Status `Draft → In Progress — Step {N} / Stage {N.M}` on first task ever filed (R1); `stage-decompose` → Step header `Skeleton → Draft (tasks _pending_)` (R7); `/author` → `In Review`; `/implement` → `In Progress`; `/closeout` (Stage-scoped) → `Done (archived)` + phase box when last task of phase closes + stage `Final` + step rollup; `master-plan-extend` → plan top Status `Final → In Progress — Step {N_new} / Stage {N_new}.1` when new Steps appended to a Final plan (R6).

### Step 1 — Postgres catalog + HTTP API + MCP tools

**Status:** In Progress — Stage 1.3 (remaining Step 1 stages _pending_)

**Backlog state (Step 1):** Stage 1.1 archived (6); Stage 1.2 archived (**TECH-626**–**TECH-629**)

**Objectives:** Land the **authoritative catalog** in Postgres with the seven logical tables from exploration §8.1 (core + economy + sprite bind + spawn pools). Expose **CRUD + preview-diff** over **`/api/catalog/*`** with **optimistic locking** and **draft/published** filters. Register thin **`catalog_*`** MCP tools and **`caller_agent`** allowlist hooks so agents mutate data without ad-hoc SQL (raw SQL tool remains escape hatch).

**Exit criteria:**

- `db/migrations/0011_catalog_core.sql` + `db/migrations/0012_catalog_spawn_pools.sql` applied; Zone S **seven rows** seeded via fixture SQL or repeatable seed script.
- Hand-written **DTO modules** under `web/types/api/catalog*.ts` match migration columns; `npm run validate:web` (or project typecheck) green for touched `web/` surfaces.
- Routes implemented: `GET /api/catalog/assets`, `GET /api/catalog/assets/:id`, `POST`, `PATCH` (409 on stale `updated_at`), `POST /api/catalog/assets/:id/retire`, `POST /api/catalog/preview-diff`.
- `tools/mcp-ia-server/` registers **`catalog_list`**, **`catalog_get`**, **`catalog_upsert`**, pool helpers per §8.3; `tools/mcp-ia-server/src/auth/caller-allowlist.ts` updated for mutation classes (coordinate minimal registration-only tasks in mcp-lifecycle plan if required).
- `npm run validate:all` green for IA/MCP edits.

**Art:** None

**Relevant surfaces (load when step opens):**

- `docs/grid-asset-visual-registry-exploration.md` §8.1–§8.4
- `ia/specs/economy-system.md` §Zone sub-type registry (`Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs` cited in glossary)
- Router: economy + persistence domains; **`web/app/api/`** (new routes) — `web-backend-logic` rule on-demand for App Router patterns
- New: `db/migrations/0011_catalog_core.sql`, `db/migrations/0012_catalog_spawn_pools.sql` (paths `(new)` until landed)
- Existing: `db/migrations/0001_ia_tables.sql` … `0010_agent_bridge_lease.sql`, `web/lib/db/`, `web/types/api/` (catalog DTOs — Stage 1.2), `tools/mcp-ia-server/src/index.ts`, `tools/mcp-ia-server/src/auth/caller-allowlist.ts`

#### Stage 1.1 — Migrations + Zone S seed

**Status:** Final

**Objectives:** Create **`catalog_asset`**, **`catalog_sprite`**, **`catalog_asset_sprite`**, **`catalog_economy`**, then pool tables in 0012; enforce uniqueness + cents + FK graph; seed **Zone S** reference rows matching current seven sub-types.

**Exit:**

- `0011_catalog_core.sql` + `0012_catalog_spawn_pools.sql` committed; `npm run db:migrate` (or repo-standard migrate) succeeds on clean DB.
- Zone S seed maps **ids 0–6** to slugs compatible with `Assets/Resources/Economy/zone-sub-types.json` intent.
- Document rollback / one-shot repair note in task §Findings if needed.

**Phases:**

- [x] Phase 1 — Core tables + constraints + indexes.
- [x] Phase 2 — Pool tables + membership + seed fixture.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.1.1 | Author 0011 core DDL | 1 | **TECH-612** | Done (archived) | Add `catalog_asset`, `catalog_sprite`, `catalog_asset_sprite`, `catalog_economy` per exploration §8.1; **`(category, slug)`** UNIQUE; money columns **`NOT NULL`** where required; **`updated_at`** trigger or app-managed column. |
| T1.1.2 | Indexes FKs and status filters | 1 | **TECH-613** | Done (archived) | Index **`status`**, **`asset_id`** joins, **`sprite_id`** lookups; FK `ON DELETE` policy aligned with soft-retire + GC story (document chosen behavior in §Implementation). |
| T1.1.3 | Migration smoke + idempotency | 1 | **TECH-614** | Done (archived) | Run migrate twice / fresh DB; verify no duplicate enum casts; add CI-friendly **`db:migrate`** note or script touch if repo requires. |
| T1.1.4 | Author 0012 pool DDL | 2 | **TECH-615** | Done (archived) | `catalog_spawn_pool`, `catalog_pool_member` + **`weight`**; FK to `catalog_asset`. |
| T1.1.5 | Seed seven Zone S assets | 2 | **TECH-616** | Done (archived) | SQL seed or `tools/` seed runner inserts seven rows + placeholder sprite bind strategy (nullable until art lands). |
| T1.1.6 | Pool seed smoke optional | 2 | **TECH-617** | Done (archived) | Minimal pool row proving **`catalog_pool_member`** write path; optional if MVP defers pools until Step 2 consumer needs it — if deferred, document explicit deferral in §Findings (still land empty tables). |

#### §Stage Closeout Plan

> Stage 1.1 closeout applied 2026-04-22 — archive `ia/backlog/TECH-612`–`TECH-617` → `ia/backlog-archive/`; delete matching `ia/projects/TECH-612`–`TECH-617` specs; task rows → **Done (archived)**; `materialize-backlog.sh` + `validate:all`.

### §Stage File Plan

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

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### Stage 1.2 — Catalog DTOs + API types (no Drizzle)

**Status:** Final

**Objectives:** Author **hand-written TypeScript DTOs** under **`web/types/api/catalog*.ts`** aligned to `0011` / `0012` (per architecture audit: **no** `drizzle-orm` in `web/`). Add shared list-filter + lock + preview-diff shapes for Stage 1.3 routes. Optional **zod** at route boundary per `docs/architecture-audit-change-list-2026-04-22.md`.

**Exit:**

- DTO modules typecheck; **`npm run validate:web`** (or `web` typecheck) passes.
- No drift vs migrations (column names + nullability) — documented spot-check in §7 / Decision Log.

**Phases:**

- [x] Phase 1 — Core + pool row DTOs (0011 / 0012).
- [x] Phase 2 — List/preview/lock DTOs + validation policy (script or doc).

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.2.1 | Core catalog DTOs (0011) | 1 | **TECH-626** | Done (archived) | Hand-written types for `catalog_asset` / `catalog_sprite` / `catalog_asset_sprite` / `catalog_economy` matching `0011`; shapes for join used in **`GET /api/catalog/assets/:id`**. |
| T1.2.2 | Pool DTOs (0012) | 1 | **TECH-627** | Done (archived) | Types for `catalog_spawn_pool` + `catalog_pool_member` matching `0012`; test helpers or documented insert pattern for pool membership. |
| T1.2.3 | API filter + lock + preview DTOs | 2 | **TECH-628** | Done (archived) | Shared types for list filters (`status`, `category`), optimistic-lock payload (`updated_at`), preview-diff result shape. |
| T1.2.4 | DTO ↔ migration alignment | 2 | **TECH-629** | Done (archived) | Wire **`package.json` script** or **doc checklist** so DTO fields stay aligned with `0011`/`0012` SQL (no `drizzle-kit`; SQL is authoritative). |

#### §Stage Closeout Plan

> Stage 1.2 closeout applied inline with ship-stage Pass 2 — **TECH-626**–**TECH-629** archived to `ia/backlog-archive/`, specs deleted, table flipped **Done (archived)**; no glossary/rule migrations.

### §Stage File Plan

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

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

> **Recheck 2026-04-22 (Stage 1.2):** `TECH-626`–`TECH-629` — §1/§2 vs task **Intent**; §7 phases; §8 acceptance; §Plan Digest; frontmatter `phases:`; DTO / no-Drizzle lock vs `docs/architecture-audit-handoff-2026-04-22.md`. Drift candidates: none.

#### Stage 1.3 — Catalog API gap-patch: test harness + behavior fixes

**Status:** In Progress

**Objectives:** Patch concrete gaps found in shipped `/api/catalog/*` routes (TECH-640..645 surface): build integration test harness; fix 6 behavior bugs; reconcile doc/refs.

**Exit:**

- Integration test suite green; all happy-path routes covered.
- 6 behavior gaps fixed; each with paired regression test.
- `ia/rules/web-backend-logic.md` updated (pagination + error contract + retire idempotency); JSDoc `@see` refs reconciled.
- `npm run validate:all` + `npm run validate:web` green.

**Phases:**

- [ ] Phase 1 — Integration test harness + happy-path coverage.
- [ ] Phase 2 — Behavior gaps (6 bug fixes) + paired regression tests + doc/ref reconciliation.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.3.1 | Catalog API test harness + happy-path coverage | 1 | **TECH-755** | Draft | Integration test infra for `/api/catalog/*`: DB setup/teardown per test, transactional rollback, HTTP helper, seed fixture. Happy-path tests: GET list published-default, GET by id joined shape snapshot, POST create 201, PATCH 200, retire 200, preview-diff 200 stable JSON. |
| T1.3.2 | Catalog API behavior gaps + doc/ref reconciliation | 2 | **TECH-756** | Draft | Six bug fixes with paired regression tests: GET-by-id retired-asset 404 filter; POST slot uniqueness pre-check; PATCH unknown-field reject; PATCH no-op-body reject; preview-diff swap `throw e` → `catalogJsonError`; retire 409 on invalid `replaced_by`. Doc: pagination + error contract + retire idempotency in `web-backend-logic.md`; fix JSDoc `@see` refs in 4 route files. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
mechanicalization_score:
  anchors: ok
  picks: ok
  invariants: ok
  validators: ok
  escalation_enum: ok
  overall: fully_mechanical

# Decision Log — cardinality:
# Stage 1.3 has 2 phases × 1 task each (sub-≥2/phase rule). Justified:
# post-rewrite intentional grouping — T1.3.1 bundles harness+happy-path as one
# coherent unit; T1.3.2 bundles 6 small bug fixes + doc/ref reconciliation as
# one coherent follow-up. Splitting further creates artificial fragmentation
# (single-file test setup / single-PR bug batch). User confirmed Stage 1.3
# rewrite intent. Proceed with 2-task emission.

- reserved_id: "TECH-755"
  title: "Catalog API test harness + happy-path coverage"
  priority: high
  issue_type: "infrastructure / web"
  notes: |
    Build integration test harness for `/api/catalog/*` routes under `web/`. Per-test DB setup/teardown w/
    transactional rollback, HTTP test client helper, seed fixture matching Zone S seven-row shape.
    Happy-path coverage: `GET /api/catalog/assets` (published-default filter), `GET /api/catalog/assets/:id`
    (joined shape snapshot), `POST` create 201, `PATCH` 200, `POST /:id/retire` 200, `POST /preview-diff`
    200 with stable JSON ordering. Test runner wired into `npm run validate:web` or sibling script per repo
    pattern. No behavior changes to routes themselves — harness-only.
    Aligns grid-asset-visual-registry master plan Step 1 Stage 1.3 Phase 1.
  depends_on: []
  related:
    - TECH-626
    - TECH-627
    - TECH-628
  stub_body:
    summary: |
      Land reusable integration test harness for catalog API routes so Stage 1.3 Phase 2 bug fixes and
      future catalog route work ship with paired regression tests. Harness handles DB lifecycle, HTTP call
      ergonomics, and seed fixtures; happy-path suite locks shipped 200/201 responses as snapshots.
    goals: |
      1. Per-test DB isolation (transactional rollback or truncate strategy) — no cross-test leakage.
      2. HTTP helper covers GET/POST/PATCH with JSON body + status assertion; matches Next.js App Router handler signatures.
      3. Seed fixture produces deterministic seven Zone S rows + minimal sprite/economy bindings.
      4. Happy-path suite green: list, get-by-id joined, create, patch, retire, preview-diff.
      5. Test script wired so `npm run validate:web` (or documented sibling) picks it up.
    systems_map: |
      - New: `web/` test suite under agreed path (e.g. `web/tests/api/catalog/*.test.ts`) per existing test conventions.
      - Existing routes: `web/app/api/catalog/assets/route.ts`, `web/app/api/catalog/assets/[id]/route.ts`, `web/app/api/catalog/assets/[id]/retire/route.ts`, `web/app/api/catalog/preview-diff/route.ts`.
      - DTOs: `web/types/api/catalog*.ts` (from TECH-626/627/628 archived).
      - Migrations: `db/migrations/0011_catalog_core.sql`, `db/migrations/0012_catalog_spawn_pools.sql`.
      - Rule: `ia/rules/web-backend-logic.md` (consume as read; Phase 2 task TECH-756 edits it).
    impl_plan_sketch: |
      ### Phase 1 — Harness + happy-path
      - [ ] Pick test framework matching repo convention (vitest/jest) + discover existing web test setup if any.
      - [ ] Implement DB lifecycle helper (transactional rollback preferred; fallback truncate of catalog_* tables).
      - [ ] Implement HTTP call helper that wraps Next handlers or hits dev server deterministically.
      - [ ] Implement seed fixture loader (re-use Stage 1.1 seed SQL or inline factory).
      - [ ] Author six happy-path tests (list, get-by-id, create, patch, retire, preview-diff).
      - [ ] Wire npm script; document in `web/README.md` if new entry.
    open_questions: |
      - Framework choice: align with whatever `web/` already uses — confirm in first PR commit.
      - Fixture strategy: SQL seed vs TS factory — pick per repo consistency with existing DB tests.

- reserved_id: "TECH-756"
  title: "Catalog API behavior gaps + doc/ref reconciliation"
  priority: high
  issue_type: "bug / web"
  notes: |
    Six discrete bug fixes in shipped `/api/catalog/*` routes, each paired with regression test using TECH-755
    harness:
      1. `GET /assets/:id` — filter out retired assets (currently returns 200 for retired rows); return 404.
      2. `POST /assets` — pre-check slot uniqueness against `catalog_asset_sprite` to avoid 500 on dup.
      3. `PATCH /assets/:id` — reject unknown fields (strict body validation).
      4. `PATCH /assets/:id` — reject empty/no-op body (400 not 200 with no-op).
      5. `POST /preview-diff` — swap bare `throw e` for `catalogJsonError` helper (consistent error envelope).
      6. `POST /assets/:id/retire` — return 409 when `replaced_by` references non-existent or retired asset.
    Doc pass: extend `ia/rules/web-backend-logic.md` with pagination contract + error-response contract +
    retire idempotency semantics. Fix JSDoc `@see` refs in 4 route files pointing at stale spec sections.
    Depends on TECH-755 harness availability.
    Aligns grid-asset-visual-registry master plan Step 1 Stage 1.3 Phase 2.
  depends_on:
    - TECH-755
  related:
    - TECH-626
    - TECH-627
    - TECH-628
  stub_body:
    summary: |
      Fix six concrete behavior gaps in shipped catalog routes, each with paired regression test, then
      reconcile rule doc + JSDoc refs so route behavior is canonically documented.
    goals: |
      1. All six bugs fixed with matching regression test (Red → Green pattern).
      2. Error envelope consistent across all catalog routes (`catalogJsonError` helper, not raw throws).
      3. `ia/rules/web-backend-logic.md` updated w/ pagination + error contract + retire idempotency sections.
      4. JSDoc `@see` refs in four route files point at live spec sections (no 404 links).
      5. `npm run validate:all` + `npm run validate:web` green; full harness suite green.
    systems_map: |
      - Routes: `web/app/api/catalog/assets/route.ts`, `web/app/api/catalog/assets/[id]/route.ts`, `web/app/api/catalog/assets/[id]/retire/route.ts`, `web/app/api/catalog/preview-diff/route.ts`.
      - Helper: `web/lib/catalog/*` (error helper location per existing pattern).
      - DTOs: `web/types/api/catalog*.ts`.
      - Rule: `ia/rules/web-backend-logic.md` (edit target).
      - Harness: Phase 1 suite from TECH-755.
      - Migrations context: `db/migrations/0011_catalog_core.sql` (retired status + replaced_by).
    impl_plan_sketch: |
      ### Phase 1 — Bug fixes (6 × test-paired)
      - [ ] Bug 1: GET-by-id retired 404 — filter on `status`; regression test.
      - [ ] Bug 2: POST slot uniqueness pre-check — lookup + 409; regression test.
      - [ ] Bug 3: PATCH unknown-field reject — strict schema; regression test.
      - [ ] Bug 4: PATCH no-op-body reject — 400; regression test.
      - [ ] Bug 5: preview-diff `catalogJsonError` swap — consistent envelope; regression test.
      - [ ] Bug 6: retire 409 on invalid `replaced_by` — FK/existence check; regression test.
      ### Phase 2 — Doc/ref reconciliation
      - [ ] `web-backend-logic.md`: add pagination contract section.
      - [ ] `web-backend-logic.md`: add error-response envelope section.
      - [ ] `web-backend-logic.md`: add retire idempotency section.
      - [ ] Fix JSDoc `@see` in 4 route files (verify each link resolves).
      - [ ] Run `validate:all` + `validate:web`; attach logs to §Verification.
    open_questions: |
      - `catalogJsonError` helper location — confirm whether it exists already or needs extraction in Bug 5.
      - Pagination contract: does shipped list route already implement cursor vs offset? Doc the as-built.
```

### §Plan Fix — PENDING

> stage-file-plan authored — awaiting stage-file-apply tail.

#### Stage 1.4 — MCP `catalog_*` tools + allowlist

**Status:** Final

**Objectives:** Expose catalog operations as **typed MCP tools**; update **`caller-allowlist.ts`** for mutation classes per repo policy.

**Exit:**

- MCP server lists new tools; package tests cover happy + error paths.
- Docs snippet in `docs/mcp-ia-server.md` updated if required by validators.

**Phases:**

- [x] Phase 1 — Tool implementations.
- [x] Phase 2 — Tests + allowlist + docs index.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.4.1 | catalog_list + catalog_get | 1 | **TECH-650** | Done | Thin wrappers over HTTP or shared DB layer; enforce **published** default for agents unless flag set. |
| T1.4.2 | catalog_upsert + pool tools | 1 | **TECH-651** | Done | Implement **`catalog_upsert`** + minimal **`catalog_pool_*`** per §8.3; validate payloads server-side. |
| T1.4.3 | MCP unit tests | 1 | **TECH-652** | Done | Extend `tools/mcp-ia-server` tests with fixture DB or mocked fetch; cover dry-run flags if exposed here. |
| T1.4.4 | caller-allowlist updates | 2 | **TECH-653** | Done | Edit `caller-allowlist.ts` — classify create/update vs delete guarded; follow existing TECH-506 patterns. |
| T1.4.5 | Doc touch + validate:all | 2 | **TECH-654** | Done | Update human MCP catalog if CI requires; run **`npm run validate:all`** green. |

### Step 2 — Snapshot export + Unity `GridAssetCatalog` + Zone S consumer

**Status:** Final

**Backlog state (Step 2):** Stage 2.1 closed (archived **TECH-662**–**TECH-666**); Stage 2.2 closed (archived **TECH-669**–**TECH-673**); Stage 2.3 closed (archived **TECH-684**–**TECH-687**)

**Objectives:** Add **`tools/`** export that dumps **published** catalog to a **versioned snapshot file** Unity loads at boot. Implement **`GridAssetCatalog`** as scene **`MonoBehaviour`** (serialized refs + `FindObjectOfType` fallback) exposing queries by **`asset_id`** and **`(category, slug)`**. Migrate **`ZoneSubTypeRegistry`** read path to **`GridAssetCatalog`** for Zone S while preserving envelope/upkeep callers.

**Exit criteria:**

- Export script writes snapshot under agreed path (e.g. `Assets/StreamingAssets/...` or `Assets/Resources/...`) + documents **hot-reload** dev signal hook (stub OK if broadcast channel not ready).
- `GridAssetCatalog.cs` at `Assets/Scripts/Managers/GameManagers/GridAssetCatalog.cs` parses snapshot; fires **`OnCatalogReloaded`**; **no** `FindObjectOfType` in per-frame paths.
- `ZoneSubTypeRegistry` consumes catalog rows for the seven sub-types; `npm run unity:compile-check` green.
- Import hygiene: export embeds **sprite paths + PPU/pivot** policy fields for allowlisted textures (or references manifest sidecar).

**Art:** Optional placeholder sprites for dev missing-asset policy; can remain pink-square stub.

**Relevant surfaces:**

- `docs/grid-asset-visual-registry-exploration.md` §5–§6, §8.2 snapshot lifecycle
- `Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs` (existing)
- `Assets/Scripts/Managers/GameManagers/GridManager.cs` (read-only contract — no new responsibilities)
- New: `Assets/Scripts/Managers/GameManagers/GridAssetCatalog.cs` `(new)`
- New: `tools/catalog-export/` or `tools/scripts/catalog-export.*` `(new)`
- Existing: `Assets/Scripts/Managers/GameManagers/BudgetAllocationService.cs`, `UIManager` toolbar bindings (read when wiring)

#### Stage 2.1 — Export CLI + snapshot schema

**Status:** Done

**Objectives:** Deterministic **DB → snapshot** export; **`--check`** mode for CI staleness; embed **`schemaVersion`**.

**Exit:**

- `node tools/...` (or `npm run catalog:export`) produces snapshot; second run stable ordering.
- Document inputs to hash key (exploration §7 baker determinism themes).

**Phases:**

- [x] Phase 1 — Reader + JSON schema.
- [x] Phase 2 — CI `--check` + docs.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | Export reads published rows | 1 | **TECH-662** | Done | Query joins asset/sprite/bind/economy; filter **`status=published`** for ship; dev flag includes draft. |
| T2.1.2 | Snapshot JSON schema + version | 1 | **TECH-663** | Done | Top-level **`schemaVersion`**, **`generatedAt`**, arrays for assets/sprites/bindings; stable sort keys. |
| T2.1.3 | Write to Unity consumable path | 1 | **TECH-664** | Done | Choose `StreamingAssets` vs `Resources`; document tradeoff; ensure `.meta` policy for generated file. |
| T2.1.4 | Import hygiene hooks | 2 | **TECH-665** | Done | Emit sidecar list of texture paths for allowlisted **`TextureImporter`** adjustment (or embed PPU per exploration §6). |
| T2.1.5 | Stale check mode | 2 | **TECH-666** | Done | `catalog:export --check` compares hash vs working tree file; exit non-zero on drift for CI optional gate. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: TECH-662
  title: "Export reads published rows"
  priority: medium
  notes: |
    Postgres catalog export reader: join catalog_asset, catalog_sprite, catalog_asset_sprite, catalog_economy via web/lib/db pool.
    Default filter status=published; dev flag includes draft. Deterministic ORDER BY for stable snapshots. Aligns with Stage 2.1 Exit + exploration §8 snapshot lifecycle.
  depends_on: []
  related:
    - TECH-663
    - TECH-664
    - TECH-665
    - TECH-666
  stub_body:
    summary: |
      Implement Node/TS export path that queries joined catalog tables through existing DB access layer, emits in-memory row set suitable for snapshot serialization, default published-only with optional draft inclusion for dev.
    goals: |
      1. Published rows default; explicit dev mode for drafts.
      2. Deterministic ordering (stable sort keys documented).
      3. Column coverage matches asset/sprite/bind/economy contract from migrations 0011/0012.
    systems_map: |
      web/lib/db/, db/migrations/0011_catalog_core.sql + 0012_catalog_spawn_pools.sql, web/types/api/catalog*.ts DTOs, new tools/catalog-export or tools/scripts entry, package.json npm script alias catalog:export (stub OK until wired).
    impl_plan_sketch: |
      Phase 1 — Reader: implement SQL or Drizzle-free query module, unit/integration smoke against fixture DB or mocked pool; document connection env (DATABASE_URL).

- reserved_id: TECH-663
  title: "Snapshot JSON schema + version"
  priority: medium
  notes: |
    Versioned snapshot envelope: schemaVersion, generatedAt, ordered arrays for assets/sprites/bindings/economy. Stable key ordering. Contract doc for Unity GridAssetCatalog (Stage 2.2).
  depends_on: []
  related:
    - TECH-662
    - TECH-664
    - TECH-665
    - TECH-666
  stub_body:
    summary: |
      Define canonical snapshot JSON shape consumed by Unity loader: top-level metadata plus arrays; enforce stable sort; bump schemaVersion when breaking.
    goals: |
      1. Top-level schemaVersion + generatedAt ISO-8601.
      2. Arrays for assets, sprites, bindings, economy with stable sort keys.
      3. Human-readable schema note or JSON Schema file under tools/docs for agents.
    systems_map: |
      tools/catalog-export (serializer), docs/grid-asset-visual-registry-exploration.md §8.2, web/types/api/catalog*.ts field parity.
    impl_plan_sketch: |
      Phase 1 — Types + serializer: TypeScript interfaces matching DTOs; JSON.stringify with ordered keys; golden fixture test for sort stability.

- reserved_id: TECH-664
  title: "Write to Unity consumable path"
  priority: medium
  notes: |
    Choose StreamingAssets vs Resources; write generated JSON; document .meta policy and hot-reload dev note per master-plan Step 2 Objectives.
  depends_on: []
  related:
    - TECH-662
    - TECH-663
    - TECH-665
    - TECH-666
  stub_body:
    summary: |
      Wire export CLI to emit file under agreed Unity path (e.g. Assets/StreamingAssets/catalog/catalog-snapshot.json); document tradeoffs and generated asset policy.
    goals: |
      1. Single authoritative output path documented in repo.
      2. Idempotent write + mkdir -p behavior.
      3. README or exploration pointer for Unity load contract.
    systems_map: |
      tools/catalog-export writer, Assets/StreamingAssets or Assets/Resources target, .gitignore/.meta conventions per team policy.
    impl_plan_sketch: |
      Phase 1 — File writer: fs write + path resolve from repo root; document in Stage 2.1 Exit / exploration cross-link.

- reserved_id: TECH-665
  title: "Import hygiene hooks"
  priority: medium
  notes: |
    Sidecar or embedded list of texture paths + PPU/pivot hints for allowlisted TextureImporter adjustments (exploration §6). No Unity C# in this task—data for later pipeline.
  depends_on: []
  related:
    - TECH-662
    - TECH-663
    - TECH-664
    - TECH-666
  stub_body:
    summary: |
      Extend snapshot or sibling manifest with texture path hygiene fields so bake/import tooling can enforce PPU/pivot policy on allowlisted assets.
    goals: |
      1. Emit path list aligned with catalog_sprite allowlist rules.
      2. Embed or reference PPU/pivot per exploration §6.
      3. Document consumer (editor script vs manual) as stub if not automated yet.
    systems_map: |
      tools/catalog-export manifest emitter, ia/specs/coding-conventions.md TextureImporter notes, exploration §6.
    impl_plan_sketch: |
      Phase 2 — Hygiene manifest: additional JSON section or sidecar file; validate against sample rows.

- reserved_id: TECH-666
  title: "Stale check mode"
  priority: medium
  notes: |
    catalog:export --check: hash inputs + snapshot bytes vs working tree; non-zero exit on drift for optional CI gate. Tie to exploration §7 baker determinism themes.
  depends_on: []
  related:
    - TECH-662
    - TECH-663
    - TECH-664
    - TECH-665
  stub_body:
    summary: |
      Add CLI mode that recomputes export and compares fingerprint to committed artifact; fails when developers forget to refresh snapshot.
    goals: |
      1. Deterministic hash of inputs (connection string excluded; schema + published rows + export version).
      2. Exit code 0 match, non-zero drift.
      3. Document optional CI wiring (non-blocking advisory acceptable).
    systems_map: |
      tools/catalog-export CLI argv parsing, crypto.createHash or stable stringify, CI doc snippet in task §Findings or README.
    impl_plan_sketch: |
      Phase 2 — --check flag: parse args, run export in memory, diff vs on-disk file, stderr message on mismatch.
```

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### Stage 2.2 — `GridAssetCatalog` runtime loader

**Status:** Final

**Objectives:** Parse snapshot at boot; in-memory indexes; **dev hot-reload** subscription stub; **missing-asset** policy dev vs ship compile-time symbols or scripting defines.

**Exit:**

- Main scene contains component instance wired via Inspector; **`Awake`** loads snapshot; **`GetAsset`/`TryGet`** APIs documented XML summary.
- **`OnCatalogReloaded`** invoked after reload.

**Phases:**

- [x] Phase 1 — Parse + index.
- [x] Phase 2 — Boot + reload hook.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | DTOs + parser | 1 | **TECH-669** | Done (archived) | `JsonUtility`-friendly DTOs or split files if needed; avoid Newtonsoft unless separate issue introduces it. |
| T2.2.2 | Indexes by id and slug | 1 | **TECH-670** | Done (archived) | `Dictionary<int, CatalogAssetEntry>` + composite key `(category, slug)`; defensive duplicates log + skip. |
| T2.2.3 | Missing sprite resolution | 1 | **TECH-671** | Done (archived) | Dev: loud placeholder material/sprite reference; Ship: mark row unavailable for UI queries. |
| T2.2.4 | Boot load path | 2 | **TECH-672** | Done (archived) | `StreamingAssets`/`Resources` load; timing vs `ZoneSubTypeRegistry` init order documented; no singleton pattern. |
| T2.2.5 | Hot-reload signal stub | 2 | **TECH-673** | Done (archived) | Editor/dev only: file watcher or bridge ping triggers reload + event; shipped players no-op. |

#### §Stage Audit

> Post-ship aggregate — task `ia/projects/TECH-669`–`TECH-673` specs removed at closeout; this block replaces per-spec **§Audit** (opus-audit / ship-stage Pass 2).

- **TECH-669:** `GridAssetSnapshotRoot` + row DTOs match export keys (`web/lib/catalog/build-catalog-snapshot.ts`); `GridAssetCatalog.TryParseSnapshotJson` validates `schemaVersion >= 1` and normalizes null arrays; **EditMode** `GridAssetCatalogParseTests` + `min_snapshot.json` lock parse; no Newtonsoft; no `FindObjectOfType` on parse path.
- **TECH-670:** `RebuildIndexes` fills `Dictionary<int, CatalogAssetRowDto>` and composite `(category,slug)` map; duplicate id or key → English `LogWarning` + first row wins; `TryGetAsset` / `TryGetAssetByCategorySlug` on `GridAssetCatalog`.
- **TECH-671:** `TryResolveSpriteFromRow` — `Resources.Load` then dev placeholder (`UnityEditor` / `DEVELOPMENT_BUILD`) or release path logs + unusable; optional `[SerializeField]` dev placeholder sprite.
- **TECH-672:** `GridAssetCatalog` `Awake` → private `LoadInternal` — `File.ReadAllText` under `Application.streamingAssetsPath` + default relative `catalog/grid-asset-catalog-snapshot.json`; `RebuildIndexes` then `OnCatalogReloaded` **UnityEvent**; XML summaries on public surface where authored.
- **TECH-673:** `ReloadFromDisk` calls `LoadInternal`; `Assets/Scripts/Editor/GridAssetCatalogMenu.cs` **Territory Developer → Catalog** menu in Play Mode; no `FileSystemWatcher` in non-Editor (stub satisfied by menu path).

**Verification (Stage):** `npm run validate:all` green; `npm run unity:compile-check` green (batchmode after editor fully quit; log under `tools/reports/unity-compile-check-*.log`).

#### §Stage Closeout Plan

> Stage 2.2 closeout applied 2026-04-22 (after compile gate) — **TECH-669**–**TECH-673** `status: closed` in `ia/backlog-archive/{id}.yaml` (source open rows removed); `ia/projects/TECH-669`–`TECH-673` deleted; table **Done (archived)**; `BACKLOG.md` / `BACKLOG-ARCHIVE.md` via `materialize-backlog.sh` + `validate:all` green; `docs/implementation/grid-asset-visual-registry-stage-2.2-plan.md` task index points at archive, not deleted specs; no glossary or MCP `catalog_*` change in this stage (Unity runtime only).

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-669"
  title: "DTOs + parser"
  priority: medium
  notes: |
    C# DTOs match TECH-663 snapshot top-level + row shapes. Parse JSON text w/ `JsonUtility`. No `Newtonsoft` here; split extra types into sibling partial class files if one file is unwieldy. Places `GridAssetCatalog` DTOs under `Assets/Scripts/...` with XML summaries on public fields used by the loader. Aligns w/ grid-asset master plan Step 2 Stage 2.2 + exploration §8 snapshot envelope.
  depends_on:
    - TECH-663
  related:
    - TECH-670
    - TECH-671
    - TECH-672
    - TECH-673
  stub_body:
    summary: |
      Define `JsonUtility`-serializable DTOs for the catalog snapshot: root envelope matching TECH-663 schema, nested asset/sprite/economy rows, plus one `TryParse` path from raw `string` to populated root object (errors surfaced for boot logging).
    goals: |
      1. DTO public fields line up 1:1 w/ `schemaVersion` + ordered arrays the export emits.
      2. `JsonUtility` parse passes on a fixture string copied from `catalog:export` output (document fixture path in §Findings).
      3. No Newtonsoft dependency; no `FindObjectOfType` in parse path.
    systems_map: |
      New: `Assets/Scripts/Managers/GameManagers/GridAssetCatalog*.cs` (parser + DTOs). Ref: `docs/grid-asset-visual-registry-exploration.md` §8; archived TECH-663 for schema. `ia/rules/unity-invariants.md` #4.
    impl_plan_sketch: |
      Phase 1 — Author DTO structs/classes + static parse helper; wire minimal unit or EditMode test that loads a tiny JSON fixture string.

- reserved_id: "TECH-670"
  title: "Indexes by id and slug"
  priority: medium
  notes: |
    Build in-memory `Dictionary<int, T>` for PK lookups + `Dictionary` or nested map for `(category, slug)` after TECH-669 parse output is available. Log + skip on duplicate key insert; no throw in prod path. `GridAssetCatalog` holds indexes; `GetAsset`/`TryGet` land in a follow method task or same PR if co-located. Stage 2.2 Exit requires documented APIs; coordinate method names w/ T2.2.1 output types.
  depends_on:
    - TECH-663
  related:
    - TECH-669
    - TECH-671
    - TECH-672
    - TECH-673
  stub_body:
    summary: |
      From parsed snapshot DTOs, build O(1) `asset_id` index + unique `(category, slug)` index; document defensive duplicate policy (`Debug.LogWarning` + first win).
    goals: |
      1. `Dictionary<int, CatalogAssetEntry>` (or project row struct) for primary key.
      2. Second index key composite string or tuple w/ clear collision handling.
      3. Rebuild method callable from load + reload.
    systems_map: |
      `GridAssetCatalog` private fields; logging via `Debug.LogWarning` in English. `ZoneSubTypeRegistry` consumes in Stage 2.3 — not this task.
    impl_plan_sketch: |
      Phase 1 — Add index builder called from load path after parse; unit-test duplicate slug scenario.

- reserved_id: "TECH-671"
  title: "Missing sprite resolution"
  priority: medium
  notes: |
    Implement dev vs ship policy from master plan + exploration §8.2: `DEVELOPMENT` build or scripting define = bright placeholder sprite/material ref assigned on missing bind; `RELEASE` = hide/flag row unavailable. No silent null refs in `GetAsset` return path. Expose `bool` or sentinel so UI can skip.
  depends_on:
    - TECH-663
  related:
    - TECH-669
    - TECH-670
    - TECH-672
    - TECH-673
  stub_body:
    summary: |
      When sprite bind or resource load fails, resolve a dev-only loud placeholder; ship build marks entry unusable w/ log line + explicit query API behavior.
    goals: |
      1. Compile-time or scripting define switch dev vs release behavior per repo convention.
      2. `Debug.LogError` or `LogWarning` in English on dev placeholder path; telemetry hook stub ok.
      3. `TryGetSprite`-style surface returns `false` in ship when row unusable.
    systems_map: |
      `GridAssetCatalog` + optional `Resources` / addressables placeholder; `Assets/` pink square or existing dev stub if present. Exploration §8.2.
    impl_plan_sketch: |
      Phase 1 — One resolver method invoked during index build or lazy load; cover both code paths w/ `Conditional` or `#if` blocks.

- reserved_id: "TECH-672"
  title: "Boot load path"
  priority: medium
  notes: |
    Load JSON bytes/string from `StreamingAssets` and/or `Resources` per TECH-664 decision path; `Awake` on scene `GridAssetCatalog` `MonoBehaviour` calls parse+index+missing resolution; document init order w.r.t. `ZoneSubTypeRegistry` (no singleton: serialized ref + `FindObjectOfType` once in `Awake` if needed). Fire `OnCatalogReloaded` after first successful load.
  depends_on:
    - TECH-663
    - TECH-664
  related:
    - TECH-669
    - TECH-670
    - TECH-671
    - TECH-673
  stub_body:
    summary: |
      Scene `GridAssetCatalog` loads snapshot file at boot, populates DTOs + indexes, subscribes optional reload, raises `OnCatalogReloaded` after load.
    goals: |
      1. `TextAsset` / `File.ReadAllText` path per documented repo location; works in Editor + Player where applicable.
      2. `Awake` only for load orchestration; no per-frame `FindObjectOfType` in `Update`/`LateUpdate`.
      3. XML `///` on public `GetAsset`/`TryGet` + `Awake` behavior.
    systems_map: |
      `Assets/Scripts/Managers/GameManagers/GridAssetCatalog.cs` (or split partial). `ZoneSubTypeRegistry` init note in spec §4. `unity-invariants` #3/#4.
    impl_plan_sketch: |
      Phase 1 — `Awake` load chain; Phase 2 — `UnityEvent` or C# `event` for `OnCatalogReloaded` field.

- reserved_id: "TECH-673"
  title: "Hot-reload signal stub"
  priority: medium
  notes: |
    Editor + dev build only: optional `FileSystemWatcher` on snapshot path or dev menu ping that calls same reload pipeline as boot; `OnCatalogReloaded` fires. Strip or no-op in release player w/ `UNITY_EDITOR` / dev defines. Shipped `RELEASE` = zero filesystem watchers.
  depends_on:
    - TECH-663
    - TECH-664
  related:
    - TECH-669
    - TECH-670
    - TECH-671
    - TECH-672
  stub_body:
    summary: |
      Stub hook: dev-only file watch or menu item triggers re-parse; production builds skip registration entirely.
    goals: |
      1. Single entry `ReloadFromDisk()` reused by boot + hot path.
      2. No watcher allocated in non-editor/non-dev.
      3. Callsite documented for bridge ping future work.
    systems_map: |
      `#if UNITY_EDITOR` blocks; `GridAssetCatalog`. Exploration snapshot refresh story.
    impl_plan_sketch: |
      Phase 2 — Wrap watcher init in editor conditional; `Update` not used; manual refresh ok for stub.
```

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### Stage 2.3 — Zone S consumer migration

**Status:** Final

**Objectives:** **`ZoneSubTypeRegistry`** reads **`GridAssetCatalog`** for costs, names, sprite paths; retain JSON fallback behind define only if needed for one-stage rollback (prefer single source).

**Exit:**

- `SubTypePickerModal`, `BudgetAllocationService`, `ZoneSService` compile against new lookup APIs.
- EditMode tests cover seven ids resolution.

**Phases:**

- [x] Phase 1 — Registry refactor.
- [x] Phase 2 — Call-site smoke + tests.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.3.1 | Wire registry to catalog | 1 | **TECH-684** | Done (archived) | Inject `[SerializeField] GridAssetCatalog catalog` + fallback `FindObjectOfType` in `Awake` on `ZoneSubTypeRegistry` GameObject. |
| T2.3.2 | Map subTypeId to asset_id | 1 | **TECH-685** | Done (archived) | Stable mapping table (`0..6` → catalog PK) from seed; document migration from JSON-only era. |
| T2.3.3 | Update callers | 2 | **TECH-686** | Done (archived) | Adjust `UIManager` / modals to use registry APIs without breaking envelope logic. |
| T2.3.4 | EditMode tests | 2 | **TECH-687** | Done (archived) | Tests load snapshot fixture under `Assets/Tests/EditMode/...`; assert costs + display names. |

#### §Stage Audit

> Post-ship aggregate — task specs **TECH-684**–**TECH-687** removed at closeout.

- **TECH-684:** `ZoneSubTypeRegistry` requires scene `GridAssetCatalog`; `Awake` resolves ref once; exposes internal `Catalog` getter.
- **TECH-685:** Identity map 0..6 → catalog PKs per Zone S seed; `TryGetAssetIdForSubType`.
- **TECH-686:** `GridAssetCatalog` indexes `catalog_economy` by `asset_id`; registry façade for picker labels + placement cost sim units (`base_cost_cents / 100`); `SubTypePickerModal` + `ZoneSService` updated.
- **TECH-687:** Fragment JSON fixture + `ZoneSubTypeRegistryCatalogBackedTests` for seven ids.

**Verification (Stage):** `npm run validate:all` green; `npm run unity:compile-check` green; `npm run unity:testmode-batch -- --quit-editor-first` exit 0; `npm run db:bridge-playmode-smoke` exit 0 (after `unity:ensure-editor`).

#### §Stage Closeout Plan

> **Applied 2026-04-22 (ship-stage-main-session):** archived **TECH-684**…**TECH-687** to `ia/backlog-archive/` (`status: closed`, `completed: "2026-04-22"`); removed temporary `ia/projects/TECH-684`…`TECH-687` specs; flipped Stage 2.3 task table to **Done (archived)** and Stage **Status** to **Final**; ran `materialize-backlog.sh` + `validate:all`.

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-684"
  title: "Wire registry to catalog"
  priority: medium
  notes: |
    `ZoneSubTypeRegistry` on same scene as `GridAssetCatalog`: add `[SerializeField] GridAssetCatalog catalog` + single `FindObjectOfType<GridAssetCatalog>()` fallback in `Awake` if unset.
    `Awake` must run after or tolerate catalog `Awake` order per TECH-672; no hot-loop `FindObjectOfType` per `unity-invariants` #3.
  depends_on:
    - TECH-672
  related:
    - TECH-685
    - TECH-686
    - TECH-687
  stub_body:
    summary: |
      Serialize catalog ref on `ZoneSubTypeRegistry`; resolve at `Awake` so later tasks read costs/sprites from the same `GridAssetCatalog` instance as boot snapshot.
    goals: |
      1. `[SerializeField] GridAssetCatalog catalog` on component.
      2. One-time resolution when null: `FindObjectOfType<GridAssetCatalog>()` in `Awake` only.
      3. Defensive `LogError` in English if still null after resolution.
    systems_map: |
      `Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs` — `GridAssetCatalog` in `Assets/Scripts/Managers/GameManagers/`. `ia/rules/unity-invariants.md` #3–4.
    impl_plan_sketch: |
      Phase 1 — Add field + `Awake` resolution + null guard; note init order in spec §4.

- reserved_id: "TECH-685"
  title: "Map subTypeId to asset_id"
  priority: medium
  notes: |
    Stable map from legacy `ZoneSubTypeEntry.id` values `0..6` to catalog `asset_id` (PK) matching seeded Zone S rows. Document JSON-only pre-catalog era vs snapshot-driven era in spec Decision Log. No runtime file I/O here beyond what catalog already provides.
  depends_on:
    - TECH-672
  related:
    - TECH-684
    - TECH-686
    - TECH-687
  stub_body:
    summary: |
      Author a small static table or `readonly` map `int subTypeId → int asset_id` aligned with `db` seed + snapshot export; used whenever registry needs catalog row identity.
    goals: |
      1. One authoritative map type (class or nested struct) colocated with `ZoneSubTypeRegistry` or adjacent partial file.
      2. Values match seven Zone S assets in published snapshot; mismatch covered by tests in T2.3.4.
      3. `///` comment block documenting migration from `Resources/.../zone-sub-types` JSON.
    systems_map: |
      `ZoneSubTypeRegistry` + `GridAssetCatalog` TryGet APIs; `Resources/Economy/zone-sub-types` legacy path reference only in comments where needed.
    impl_plan_sketch: |
      Phase 1 — Define map + accessor `TryResolveAssetId(int subTypeId, out int assetId)`.

- reserved_id: "TECH-686"
  title: "Update callers"
  priority: medium
  notes: |
    `UIManager` / `SubTypePickerModal` / `BudgetAllocationService` / `ZoneSService` switch to registry+asset_id path for display names, cent costs, icon/prefab resolution via catalog-backed data; preserve Zone S money envelope invariants; no new singletons.
  depends_on:
    - TECH-672
  related:
    - TECH-684
    - TECH-685
    - TECH-687
  stub_body:
    summary: |
      All Zone S UI and services query `ZoneSubTypeRegistry` for display + cost data sourced through catalog mapping; JSON `baseCost` path retired or gated behind single define.
    goals: |
      1. `SubTypePickerModal` shows names/costs consistent with catalog rows.
      2. `BudgetAllocationService` + `ZoneSService` use same cent values as `GridAssetCatalog` economy fields.
      3. Compile clean: no dead references to old JSON-only cost path unless `#if` rollback define explicitly documented.
    systems_map: |
      Grep for `ZoneSubTypeRegistry`, `SubTypePickerModal`, `BudgetAllocationService`, `ZoneSService`, `UIManager` under `Assets/Scripts/`.
    impl_plan_sketch: |
      Phase 1 — Thread catalog-backed lookups through modal + services; Phase 2 — remove or ifdef legacy JSON field reads from entries.

- reserved_id: "TECH-687"
  title: "EditMode tests"
  priority: medium
  notes: |
    `Assets/Tests/EditMode/...` loads min snapshot or fixture `TextAsset` matching export shape; drives `GridAssetCatalog` + `ZoneSubTypeRegistry` in isolation or via test scene setup; asserts seven ids resolve expected display strings + cent costs. English assertion messages.
  depends_on:
    - TECH-672
  related:
    - TECH-684
    - TECH-685
    - TECH-686
  stub_body:
    summary: |
      EditMode tests lock seven subtype ids → catalog-backed costs + display names; fail with clear message if map or snapshot drifts.
    goals: |
      1. Test fixture path documented in spec §7b.
      2. One test per concern or one table-driven test with seven cases.
      3. `npm run unity:compile-check` green after tests land.
    systems_map: |
      `Assets/Tests/EditMode/Economy/ZoneSubTypeRegistryTests.cs` (extend or mirror); `GridAssetCatalog` test patterns from prior Stage 2.2.
    impl_plan_sketch: |
      Phase 1 — Add fixture + tests calling public registry surface only; no Play Mode.
```

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

### Step 3 — Placement validator + save semantics + sprite GC

**Status:** In Progress (Stage 3.1 filed; Stages 3.2–3.3 still _pending_)

**Backlog state (Step 3):** Stage 3.1 filed (**TECH-688**..**TECH-692**); Stages 3.2–3.3 still _pending_

**Objectives:** Introduce **`PlacementValidator`** (new type) as **single owner** of **`CanPlace(assetId, cell, rotation)`** with structured reason codes for UX + ghosts. Extend **save DTO** to store **`asset_id`** and implement **`replaced_by`** remap on load. Add **sprite GC** janitor endpoint or SQL job per exploration §8.4 point 11.

**Exit criteria:**

- `PlacementValidator.cs` under `Assets/Scripts/Managers/GameManagers/` (or `Services/` sibling) — **does not** touch `grid.cellArray` directly; uses **`GridManager`** public API only.
- Ghost tint + tooltip consumers read validator output (stub rotation if always zero in MVP).
- `GameSaveManager` + `CellData` (or parallel structure) persists **`asset_id`**; load applies **`replaced_by`** chain safely.
- Admin/agent **`catalog_sprite` GC** removes unreferenced rows per policy.

**Art:** None

**Relevant surfaces:**

- `docs/grid-asset-visual-registry-exploration.md` §8.3 **`PlacementValidator`**, **`GameSaveManager`**
- `ia/specs/persistence-system.md` Load pipeline (`lineStart` 24)
- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`, `CellData` definition sites
- `Assets/Scripts/Managers/GameManagers/CursorManager.cs`, `ZoneManager.cs` (integration hooks — extract if needed)
- New: `Assets/Scripts/Managers/GameManagers/PlacementValidator.cs` `(new)`

#### Stage 3.1 — `PlacementValidator` core API

**Status:** In Progress — 5 tasks filed (**TECH-688**..**TECH-692**, all Draft)

**Objectives:** Deterministic **legality** answers: footprint placeholder (1×1 MVP), zoning channel match, unlock stub, affordability hook via **`EconomyManager`** / treasury services.

**Exit:**

- Public method returns **`PlacementResult`** (allowed + **`PlacementFailReason`** + optional detail string).
- Unit tests table-driven for core cases.

**Phases:**

- [ ] Phase 1 — Types + zoning match.
- [ ] Phase 2 — Economy + unlock stubs.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.1.1 | Author PlacementValidator type | 1 | **TECH-688** | Draft | New class file; serialized refs to **`GridManager`**, **`GridAssetCatalog`**, **`EconomyManager`** per guardrails. |
| T3.1.2 | Reason codes + result struct | 1 | **TECH-689** | Draft | Structured enum covers footprint, zoning, locked, unaffordable, occupied; XML docs on public API. |
| T3.1.3 | Zoning channel match MVP | 1 | **TECH-690** | Draft | Zone S manual placement path consults validator before commit; keep **`GridManager`** extraction — no new `GridManager` methods unless unavoidable (justify in §Findings). |
| T3.1.4 | Affordability gate | 2 | **TECH-691** | Draft | Query **`baseCost`** cents from catalog economy snapshot; delegate to existing spend/try APIs. |
| T3.1.5 | Unlock gate stub | 2 | **TECH-692** | Draft | Read **`unlocks_after`** string; integrate with existing tech stub or return **Allowed** if not implemented — document. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "Author PlacementValidator type"
  priority: high
  notes: |
    New MonoBehaviour or plain C# service under GameManagers/Services; serialized GridManager,
    GridAssetCatalog, EconomyManager refs per unity-invariants Inspector pattern. No direct grid.cellArray.
    Stage 3.1 Phase 1 — types foundation for CanPlace MVP.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Add PlacementValidator type with serialized manager refs and a stub CanPlace API surface so later
      tasks can attach reason codes, zoning, economy, and unlock gates without reshaping the type.
    goals: |
      - PlacementValidator lives under Assets/Scripts/Managers/GameManagers/ (or Services sibling).
      - SerializeField refs: GridManager, GridAssetCatalog, EconomyManager; Awake resolves via FindObjectOfType fallback where pattern already exists in codebase.
      - Public CanPlace signature reserved (bool + detail) even if body returns placeholder true until T3.1.2 lands.
    systems_map: |
      - New: Assets/Scripts/Managers/GameManagers/PlacementValidator.cs
      - Existing: GridManager, GridAssetCatalog, ZoneSubTypeRegistry / catalog wiring, EconomyManager
      - Ref: docs/grid-asset-visual-registry-exploration.md §8.3 PlacementValidator
    impl_plan_sketch: |
      ### Phase 1 — Type scaffold
      - [ ] Create class file; add SerializeField trio + XML summary on class.
      - [ ] Stub CanPlace(assetId, cell, rotation) returning true with TODO hook for fail reasons.
- reserved_id: ""
  title: "Reason codes + result struct"
  priority: high
  notes: |
    PlacementFailReason enum + structured result (bool, reason, optional message) with XML docs on public API.
    Table-driven EditMode tests per Stage Exit. Consumes PlacementValidator from TECH-688.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Replace placeholder CanPlace return with PlacementResult carrying bool, PlacementFailReason, and optional
      detail string; document enum values for footprint, zoning, locked, unaffordable, occupied.
    goals: |
      - Enum covers footprint, zoning, locked, unaffordable, occupied (+ None/Ok as appropriate).
      - XML documentation on public CanPlace / result types.
      - EditMode tests: table-driven cases for at least one pass and one fail path per reason category (stubbed deps).
    systems_map: |
      - PlacementValidator.cs (expand)
      - Tests under Assets/Tests/ or existing EditMode test assembly pattern
    impl_plan_sketch: |
      ### Phase 1 — Result + tests
      - [ ] Define PlacementFailReason + PlacementResult structs/classes.
      - [ ] Wire CanPlace to return structured failures (stubs OK for deps not yet implemented).
      - [ ] Add EditMode test fixture with [TestCase] matrix.
- reserved_id: ""
  title: "Zoning channel match MVP"
  priority: high
  notes: |
    Zone S manual placement consults validator before commit; use GridManager public API only; avoid new
    GridManager methods unless justified in §Findings. Integrates with TECH-689 result shape.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Hook Zone S (or ZoneManager manual path) so placement attempts call PlacementValidator.CanPlace before
      committing building spawn; surface fail reason for downstream UX (Stage 3.2).
    goals: |
      - Manual placement path blocks illegal zoning channel mismatches using validator output.
      - No direct grid.cellArray access from validator; GridManager extraction only.
      - Document any unavoidable GridManager API addition in §Findings.
    systems_map: |
      - PlacementValidator.cs
      - ZoneManager.cs, CursorManager.cs (integration hooks per master plan surfaces)
      - GridManager public surface
    impl_plan_sketch: |
      ### Phase 1 — Integration
      - [ ] Locate Zone S commit point; insert CanPlace guard; abort commit on false.
      - [ ] Map PlacementFailReason.Zoning (or equivalent) for channel mismatch.
      - [ ] Manual smoke: place allowed vs disallowed asset in Editor.
- reserved_id: ""
  title: "Affordability gate"
  priority: medium
  notes: |
    Read baseCost cents from GridAssetCatalog economy snapshot; delegate to existing EconomyManager try/spend
    APIs. Unaffordable → PlacementFailReason.Unaffordable.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Extend validator so CanPlace rejects when treasury cannot afford catalog baseCost for assetId using
      existing economy APIs (no new economy subsystem).
    goals: |
      - Query catalog snapshot for baseCost; align with economy spec cents.
      - Call existing spend/affordability check pattern used elsewhere for buildings.
      - Unit or EditMode test: affordable vs unaffordable paths.
    systems_map: |
      - PlacementValidator.cs
      - EconomyManager, GridAssetCatalog economy fields
      - ia/specs/economy-system.md (treasury / spend patterns)
    impl_plan_sketch: |
      ### Phase 1 — Economy gate
      - [ ] Resolve baseCost for assetId from catalog.
      - [ ] Integrate EconomyManager affordability probe before allow.
      - [ ] Tests for can/cannot afford.
- reserved_id: ""
  title: "Unlock gate stub"
  priority: medium
  notes: |
    Read unlocks_after from catalog row; integrate tech unlock stub if present else return Allowed and document
    behavior in spec §Open Questions / Decision Log.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Validator reads unlocks_after string; if no tech unlock system wired, document and return allowed; if stub
      exists, map locked assets to PlacementFailReason.Locked.
    goals: |
      - Catalog field unlocks_after consulted in CanPlace path.
      - Documented fallback when tech tree not implemented.
      - Test or explicit manual checklist for locked vs unlocked asset.
    systems_map: |
      - PlacementValidator.cs
      - GridAssetCatalog asset rows / DTO
      - Existing tech unlock stub (if any)
    impl_plan_sketch: |
      ### Phase 1 — Unlock stub
      - [ ] Parse unlocks_after in validator; branch to Locked or Allowed per integration state.
      - [ ] Document integration gap in Decision Log if returning Allowed by default.
      - [ ] Minimal test or §8 manual step.
```

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### Stage 3.2 — Ghost + tooltip integration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Preview flows set **green/red** tint from validator; tooltips show **reason** string; **`GridManager`** hit-test contract unchanged.

**Exit:**

- Play Mode manual smoke documented; no **`Collider2D`** added to world tiles.

**Phases:**

- [ ] Phase 1 — Cursor / preview hook.
- [ ] Phase 2 — Tooltip + UX polish pass.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.2.1 | Wire ghost preview to validator | 1 | _pending_ | _pending_ | `CursorManager` or dedicated preview helper calls **`CanPlace`** each move; throttle if needed (no per-frame `FindObjectOfType`). |
| T3.2.2 | Valid tint path | 1 | _pending_ | _pending_ | Reuse existing sprite tint utilities; ensure **sortingOrder** unaffected. |
| T3.2.3 | Invalid tint path | 1 | _pending_ | _pending_ | Red tint + reason propagation to UI layer. |
| T3.2.4 | Tooltip reason string | 2 | _pending_ | _pending_ | `UIManager` or local tooltip controller shows **human-readable** mapping from enum. |
| T3.2.5 | Play Mode smoke checklist | 2 | _pending_ | _pending_ | Document scenario steps for verify-loop; no automated Play test required if policy says manual — state explicitly. |

#### Stage 3.3 — Save `asset_id` + `replaced_by` + sprite GC

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Persist **`asset_id`** in save; remap retired assets; GC **orphan sprites** safely.

**Exit:**

- Schema bump issue filed if needed; migration respects **`persistence-system`** restore order.
- GC tool or route deletes only **unreferenced** `catalog_sprite` rows.

**Phases:**

- [ ] Phase 1 — Save/load plumbing.
- [ ] Phase 2 — GC endpoint + safety checks.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.3.1 | Add save fields | 1 | _pending_ | _pending_ | Extend `CellData` / building DTO with **`assetId`**; default **0** means legacy; bump **`GameSaveData.CurrentSchemaVersion`** if required. |
| T3.3.2 | Load-time remap | 1 | _pending_ | _pending_ | Walk **`replaced_by`** chain with cycle guard; log telemetry on missing rows. |
| T3.3.3 | GC SQL or admin route | 2 | _pending_ | _pending_ | Implement refcount query across **`catalog_asset_sprite`** + **`catalog_pool_member`**; dry-run flag returns candidates. |
| T3.3.4 | Tests for remap + GC | 2 | _pending_ | _pending_ | EditMode or server tests cover chain remap + **no delete** when referenced. |

### Step 4 — `wire_asset_from_catalog` + dry-run + scene contract IA

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 4):** 0 filed

**Objectives:** Implement composite **`unity_bridge_command`** kind **`wire_asset_from_catalog`** in `Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs` (partial class pattern): **snapshot → instantiate → parent → bind `UiTheme` / `IlluminatedButton` → hook onClick → save scene** with **rollback** on failure. Add **`ia/specs/ui-design-system.md` appendix** listing **canonical scene paths** for toolbar/HUD/modal host. Document **verify-loop** recipe for agent-driven UI wiring.

**Exit criteria:**

- New bridge kind registered end-to-end (MCP DTO + switch dispatch + tests if pattern exists).
- **`dry_run: true`** returns structured plan JSON without mutating scene.
- UI spec appendix merged; glossary rows **Grid asset catalog** / **Grid asset baker** / **Art manifest (grid)** land per exploration §9.5 in a Stage task here or umbrella IA task.
- `npm run unity:compile-check` green after Editor partial changes.

**Art:** None (uses existing UI prefabs)

**Relevant surfaces:**

- `docs/grid-asset-visual-registry-exploration.md` §1.4, §8.3–§8.4 points 8–10
- `ia/specs/ui-design-system.md` — appendix `(new)` section
- `Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs` (existing partial)
- `docs/agent-led-verification-policy.md` — UI wiring evidence notes `(touch)`
- `ia/specs/glossary.md` — new rows `(touch)`
- `docs/mcp-ia-server.md` — kind catalog `(touch)`

#### Stage 4.1 — Bridge composite implementation

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** **`wire_asset_from_catalog`** executes deterministic steps; respects **scene contract** paths from spec appendix (once landed, temporary constants OK in Stage 4.1 with TODO removed in 4.3).

**Exit:**

- Edit Mode run creates toolbar button wired to existing **`UIManager`** entry stub.
- Logs each sub-step for agent observability.

**Phases:**

- [ ] Phase 1 — DTO + dispatch wiring.
- [ ] Phase 2 — Scene graph mutations + save.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.1.1 | Kind enum + DTO fields | 1 | _pending_ | _pending_ | Extend mutation DTO with **`wire_asset_from_catalog`** payload: `assetId`, `dryRun`, `parentPath`, `uiThemeRef` strategy. |
| T4.1.2 | Dispatch switch case | 1 | _pending_ | _pending_ | Route in `AgentBridgeCommandRunner.Mutations.cs` per **bridge tooling patterns** (`unity-invariants` doc). |
| T4.1.3 | Resolve catalog row | 1 | _pending_ | _pending_ | Editor-only read of snapshot or DB bridge — choose one deterministic source for Edit Mode (document). |
| T4.1.4 | Instantiate + parent + bind | 2 | _pending_ | _pending_ | Reuse `instantiate_prefab`, `set_gameobject_parent`, `assign_serialized_field` primitives internally. |
| T4.1.5 | onClick wire + save_scene | 2 | _pending_ | _pending_ | Hook to existing inspector-exposed handler; call **`save_scene`**; return structured success object. |

#### Stage 4.2 — Transactional snapshot + dry-run

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** **Snapshot scene** before composite; **rollback** restores prior state on any failure; **`dry_run`** prints plan only.

**Exit:**

- Failed run leaves scene unchanged (Edit Mode test).
- Telemetry fields include **`recipe_id`** + **`caller_agent`** passthrough if available.

**Phases:**

- [ ] Phase 1 — Snapshot/restore mechanism.
- [ ] Phase 2 — Dry-run path + tests.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.2.1 | Pre-snapshot hook | 1 | _pending_ | _pending_ | Serialize relevant `GameObject` hierarchy or use Unity `Undo` stack if compatible with bridge — pick one pattern and document limits. |
| T4.2.2 | Rollback on failure | 1 | _pending_ | _pending_ | Ensure exceptions in any sub-step trigger restore; return `partial` metadata for agents. |
| T4.2.3 | dry_run plan JSON | 2 | _pending_ | _pending_ | No prefab instance persists; output lists intended creates + property sets. |
| T4.2.4 | EditMode bridge tests | 2 | _pending_ | _pending_ | If repo has Editor test asmdef, cover success + rollback; else document **`verify-loop`** manual path. |

#### Stage 4.3 — IA scene contract + verification docs + glossary

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Publish **canonical paths** + **glossary** terms; align verification policy for **UI wiring via bridge**.

**Exit:**

- `ia/specs/ui-design-system.md` contains **Scene contract (agents)** appendix.
- `ia/specs/glossary.md` rows added; `npm run validate:all` green.

**Phases:**

- [ ] Phase 1 — Spec appendix.
- [ ] Phase 2 — Glossary + policy doc touch.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.3.1 | Author UI scene contract appendix | 1 | _pending_ | _pending_ | Document toolbar root, HUD strip, modal host, Control Panel mount paths with **MainScene** examples. |
| T4.3.2 | Cross-link bridge doc | 1 | _pending_ | _pending_ | Update `docs/mcp-ia-server.md` **`unity_bridge_command`** section listing **`wire_asset_from_catalog`** fields. |
| T4.3.3 | Glossary rows | 2 | _pending_ | _pending_ | Add **Grid asset catalog**, **Grid asset baker**, **Art manifest (grid)** per exploration §9.5 with links to specs. |
| T4.3.4 | Verification policy note | 2 | _pending_ | _pending_ | `docs/agent-led-verification-policy.md` short subsection: agent UI wiring evidence expectations (Edit Mode scene diff + optional Play smoke). |

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` (Stage-scoped pair) runs.
- Run `claude-personal "/stage-file ia/projects/grid-asset-visual-registry-master-plan.md Stage {N}.{M}"` (routes to `stage-file-plan` + `stage-file-apply` pair) to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to exploration doc + scope-boundary doc.
- Keep this orchestrator synced with umbrella **`full-game-mvp-master-plan.md`** when Bucket 12 row lands — separate PR/task from this file's author time.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only terminal step work flips step headers; the file stays.
- Silently promote post-MVP items into MVP stages — park them in `docs/grid-asset-visual-registry-post-mvp-extensions.md` once authored.
- Merge partial stage state — every stage must land on a green bar.
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Introduce new **singletons** for **`GridAssetCatalog`** — violates `unity-invariants` #4.

---

## Changelog

| Date | Note |
|------|------|
| 2026-04-21 | Orchestrator authored from `docs/grid-asset-visual-registry-exploration.md` §8 via `master-plan-new`. |
