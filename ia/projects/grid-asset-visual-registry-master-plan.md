# Grid asset visual registry — Master Plan (Bucket 12 MVP spine)

> **Status:** In Progress — Step 1 / Stage 1.2
>
> **Scope:** Postgres-backed **grid asset catalog** (identity, sprites, economy, spawn pools) as source of truth; **HTTP + MCP** for agents; **Unity boot snapshot** consumed by **`GridAssetCatalog`** (no new singleton — Inspector + `FindObjectOfType` per `unity-invariants` #4); **Zone S** first consumer via **`ZoneSubTypeRegistry`** convergence; **`PlacementValidator`** owns place-here legality; **`wire_asset_from_catalog`** bridge kind for design-system-safe Control Panel wiring; export + import hygiene + IA scene contract. **Out:** sprite-gen composition logic (Bucket 5), deep sim rules beyond catalog reads, `web/` dashboard product UI (Bucket 9 transport only — this plan adds `/api/catalog/*` on the existing Next app). Post-MVP extensions → recommend `docs/grid-asset-visual-registry-post-mvp-extensions.md` (not authored by this workflow).
>
> **Exploration source:** `docs/grid-asset-visual-registry-exploration.md` (§8 Design Expansion — Chosen approach D, Architecture diagram, Subsystem impact table, Implementation points 1–12, Examples, Review notes; §4 locked decisions; §10 code refs).
>
> **Locked decisions (do not reopen in this plan):**
> - Catalog source of truth = **Postgres** (Drizzle in `web/`, migrations `db/migrations/`); Unity loads **boot-time snapshot**; Resources JSON is **derived**, not authoritative.
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
> - `docs/grid-asset-visual-registry-exploration.md` — full design + §8 ground truth.
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

**Status:** In Progress — Stage 1.2 (remaining Step 1 stages _pending_)

**Backlog state (Step 1):** Stage 1.1 archived (6); Stage 1.2 not filed

**Objectives:** Land the **authoritative catalog** in Postgres with the seven logical tables from exploration §8.1 (core + economy + sprite bind + spawn pools). Expose **CRUD + preview-diff** over **`/api/catalog/*`** with **optimistic locking** and **draft/published** filters. Register thin **`catalog_*`** MCP tools and **`caller_agent`** allowlist hooks so agents mutate data without ad-hoc SQL (raw SQL tool remains escape hatch).

**Exit criteria:**

- `db/migrations/0011_catalog_core.sql` + `db/migrations/0012_catalog_spawn_pools.sql` applied; Zone S **seven rows** seeded via fixture SQL or repeatable seed script.
- Drizzle modules under `web/drizzle/schema/catalog*.ts` match tables; `npm run validate:web` (or project typecheck) green for touched `web/` surfaces.
- Routes implemented: `GET /api/catalog/assets`, `GET /api/catalog/assets/:id`, `POST`, `PATCH` (409 on stale `updated_at`), `POST /api/catalog/assets/:id/retire`, `POST /api/catalog/preview-diff`.
- `tools/mcp-ia-server/` registers **`catalog_list`**, **`catalog_get`**, **`catalog_upsert`**, pool helpers per §8.3; `tools/mcp-ia-server/src/auth/caller-allowlist.ts` updated for mutation classes (coordinate minimal registration-only tasks in mcp-lifecycle plan if required).
- `npm run validate:all` green for IA/MCP edits.

**Art:** None

**Relevant surfaces (load when step opens):**

- `docs/grid-asset-visual-registry-exploration.md` §8.1–§8.4
- `ia/specs/economy-system.md` §Zone sub-type registry (`Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs` cited in glossary)
- Router: economy + persistence domains; **`web/app/api/`** (new routes) — `web-backend-logic` rule on-demand for App Router patterns
- New: `db/migrations/0011_catalog_core.sql`, `db/migrations/0012_catalog_spawn_pools.sql` (paths `(new)` until landed)
- Existing: `db/migrations/0001_ia_tables.sql` … `0010_agent_bridge_lease.sql`, `web/drizzle/`, `tools/mcp-ia-server/src/index.ts`, `tools/mcp-ia-server/src/auth/caller-allowlist.ts`

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
    (`asset_id`, `sprite_id`). Record soft-retire + GC behavior in spec §Implementation so Step 2 Drizzle
    mirrors chosen policy.
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

#### Stage 1.2 — Drizzle schema + shared types

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Mirror SQL in **`web/drizzle/schema/catalog*.ts`** with relations; export types consumed by Next route handlers.

**Exit:**

- Drizzle schema builds; **`npm run validate:web`** (or `web` typecheck) passes.
- No drift vs migrations (column names + types).

**Phases:**

- [ ] Phase 1 — Table defs + relations.
- [ ] Phase 2 — Exported TS types + helper queries.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.2.1 | Drizzle mirror core tables | 1 | _pending_ | _pending_ | Author `catalog*.ts` for asset/sprite/bind/economy matching `0011`; relations for joins used in **`GET /api/catalog/assets/:id`**. |
| T1.2.2 | Drizzle mirror pool tables | 1 | _pending_ | _pending_ | Pool + member tables matching `0012`; export insert helpers for membership tests. |
| T1.2.3 | API DTO alignment | 2 | _pending_ | _pending_ | Shared types for list filters (`status`, `category`), optimistic-lock payload (`updated_at`), preview-diff result shape. |
| T1.2.4 | Repo validation hook | 2 | _pending_ | _pending_ | Wire `package.json` script touch only if `validate:all` requires explicit drizzle check; else document manual **`drizzle-kit`** policy in §Implementation. |

#### Stage 1.3 — Next `/api/catalog/*` routes

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement read/write HTTP surface with **published-default list**, **optimistic PATCH**, **retire**, and **`preview-diff`** for admin/agent plans.

**Exit:**

- Local `curl` / unit test proves CRUD + 409 conflict path.
- Errors return structured JSON consistent with existing `web/app/api/*` patterns.

**Phases:**

- [ ] Phase 1 — Reads (list + get).
- [ ] Phase 2 — Writes + preview-diff + retire.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.3.1 | GET list route + filters | 1 | _pending_ | _pending_ | `GET /api/catalog/assets` with `status=published` default; optional draft flag for admin; pagination if row count grows. |
| T1.3.2 | GET by id joined shape | 1 | _pending_ | _pending_ | `GET /api/catalog/assets/:id` returns asset + economy + sprite slots; stable JSON key naming documented. |
| T1.3.3 | HTTP error contract | 1 | _pending_ | _pending_ | Map DB errors to **400/404/409**; log server-side; no stack traces to client. |
| T1.3.4 | POST create transactional | 2 | _pending_ | _pending_ | Create asset + economy + sprite binds in one transaction; validate slot uniqueness. |
| T1.3.5 | PATCH optimistic lock | 2 | _pending_ | _pending_ | Compare client **`updated_at`**; bump revision; return **`409`** with fresh row on mismatch. |
| T1.3.6 | Retire + preview-diff | 2 | _pending_ | _pending_ | `POST .../retire` sets **`replaced_by`** when provided; `POST /api/catalog/preview-diff` returns human/agent-readable plan without commit. |

#### Stage 1.4 — MCP `catalog_*` tools + allowlist

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Expose catalog operations as **typed MCP tools**; update **`caller-allowlist.ts`** for mutation classes per repo policy.

**Exit:**

- MCP server lists new tools; package tests cover happy + error paths.
- Docs snippet in `docs/mcp-ia-server.md` updated if required by validators.

**Phases:**

- [ ] Phase 1 — Tool implementations.
- [ ] Phase 2 — Tests + allowlist + docs index.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.4.1 | catalog_list + catalog_get | 1 | _pending_ | _pending_ | Thin wrappers over HTTP or shared DB layer; enforce **published** default for agents unless flag set. |
| T1.4.2 | catalog_upsert + pool tools | 1 | _pending_ | _pending_ | Implement **`catalog_upsert`** + minimal **`catalog_pool_*`** per §8.3; validate payloads server-side. |
| T1.4.3 | MCP unit tests | 1 | _pending_ | _pending_ | Extend `tools/mcp-ia-server` tests with fixture DB or mocked fetch; cover dry-run flags if exposed here. |
| T1.4.4 | caller-allowlist updates | 2 | _pending_ | _pending_ | Edit `caller-allowlist.ts` — classify create/update vs delete guarded; follow existing TECH-506 patterns. |
| T1.4.5 | Doc touch + validate:all | 2 | _pending_ | _pending_ | Update human MCP catalog if CI requires; run **`npm run validate:all`** green. |

### Step 2 — Snapshot export + Unity `GridAssetCatalog` + Zone S consumer

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 2):** 0 filed

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

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Deterministic **DB → snapshot** export; **`--check`** mode for CI staleness; embed **`schemaVersion`**.

**Exit:**

- `node tools/...` (or `npm run catalog:export`) produces snapshot; second run stable ordering.
- Document inputs to hash key (exploration §7 baker determinism themes).

**Phases:**

- [ ] Phase 1 — Reader + JSON schema.
- [ ] Phase 2 — CI `--check` + docs.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | Export reads published rows | 1 | _pending_ | _pending_ | Query joins asset/sprite/bind/economy; filter **`status=published`** for ship; dev flag includes draft. |
| T2.1.2 | Snapshot JSON schema + version | 1 | _pending_ | _pending_ | Top-level **`schemaVersion`**, **`generatedAt`**, arrays for assets/sprites/bindings; stable sort keys. |
| T2.1.3 | Write to Unity consumable path | 1 | _pending_ | _pending_ | Choose `StreamingAssets` vs `Resources`; document tradeoff; ensure `.meta` policy for generated file. |
| T2.1.4 | Import hygiene hooks | 2 | _pending_ | _pending_ | Emit sidecar list of texture paths for allowlisted **`TextureImporter`** adjustment (or embed PPU per exploration §6). |
| T2.1.5 | Stale check mode | 2 | _pending_ | _pending_ | `catalog:export --check` compares hash vs working tree file; exit non-zero on drift for CI optional gate. |

#### Stage 2.2 — `GridAssetCatalog` runtime loader

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Parse snapshot at boot; in-memory indexes; **dev hot-reload** subscription stub; **missing-asset** policy dev vs ship compile-time symbols or scripting defines.

**Exit:**

- Main scene contains component instance wired via Inspector; **`Awake`** loads snapshot; **`GetAsset`/`TryGet`** APIs documented XML summary.
- **`OnCatalogReloaded`** invoked after reload.

**Phases:**

- [ ] Phase 1 — Parse + index.
- [ ] Phase 2 — Boot + reload hook.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | DTOs + parser | 1 | _pending_ | _pending_ | `JsonUtility`-friendly DTOs or split files if needed; avoid Newtonsoft unless separate issue introduces it. |
| T2.2.2 | Indexes by id and slug | 1 | _pending_ | _pending_ | `Dictionary<int, CatalogAssetEntry>` + composite key `(category, slug)`; defensive duplicates log + skip. |
| T2.2.3 | Missing sprite resolution | 1 | _pending_ | _pending_ | Dev: loud pink placeholder material/sprite reference; Ship: mark row unavailable for UI queries. |
| T2.2.4 | Boot load path | 2 | _pending_ | _pending_ | `StreamingAssets`/`Resources` load; timing vs `ZoneSubTypeRegistry` init order documented; no singleton pattern. |
| T2.2.5 | Hot-reload signal stub | 2 | _pending_ | _pending_ | Editor/dev only: file watcher or bridge ping triggers reload + event; shipped players no-op. |

#### Stage 2.3 — Zone S consumer migration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** **`ZoneSubTypeRegistry`** reads **`GridAssetCatalog`** for costs, names, sprite paths; retain JSON fallback behind define only if needed for one-stage rollback (prefer single source).

**Exit:**

- `SubTypePickerModal`, `BudgetAllocationService`, `ZoneSService` compile against new lookup APIs.
- EditMode tests cover seven ids resolution.

**Phases:**

- [ ] Phase 1 — Registry refactor.
- [ ] Phase 2 — Call-site smoke + tests.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.3.1 | Wire registry to catalog | 1 | _pending_ | _pending_ | Inject `[SerializeField] GridAssetCatalog catalog` + fallback `FindObjectOfType` in `Awake` on `ZoneSubTypeRegistry` GameObject. |
| T2.3.2 | Map subTypeId to asset_id | 1 | _pending_ | _pending_ | Stable mapping table (`0..6` → catalog PK) from seed; document migration from JSON-only era. |
| T2.3.3 | Update callers | 2 | _pending_ | _pending_ | Adjust `UIManager` / modals to use registry APIs without breaking envelope logic. |
| T2.3.4 | EditMode tests | 2 | _pending_ | _pending_ | Tests load snapshot fixture under `Assets/Tests/EditMode/...`; assert costs + display names. |

### Step 3 — Placement validator + save semantics + sprite GC

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 3):** 0 filed

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

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Deterministic **legality** answers: footprint placeholder (1×1 MVP), zoning channel match, unlock stub, affordability hook via **`EconomyManager`** / treasury services.

**Exit:**

- Public method returns **`bool`** + **`PlacementFailReason`** enum + optional detail string.
- Unit tests table-driven for core cases.

**Phases:**

- [ ] Phase 1 — Types + zoning match.
- [ ] Phase 2 — Economy + unlock stubs.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.1.1 | Author PlacementValidator type | 1 | _pending_ | _pending_ | New class file; serialized refs to **`GridManager`**, **`GridAssetCatalog`**, **`EconomyManager`** per guardrails. |
| T3.1.2 | Reason codes + result struct | 1 | _pending_ | _pending_ | Structured enum covers footprint, zoning, locked, unaffordable, occupied; XML docs on public API. |
| T3.1.3 | Zoning channel match MVP | 1 | _pending_ | _pending_ | Zone S manual placement path consults validator before commit; keep **`GridManager`** extraction — no new `GridManager` methods unless unavoidable (justify in §Findings). |
| T3.1.4 | Affordability gate | 2 | _pending_ | _pending_ | Query **`baseCost`** cents from catalog economy snapshot; delegate to existing spend/try APIs. |
| T3.1.5 | Unlock gate stub | 2 | _pending_ | _pending_ | Read **`unlocks_after`** string; integrate with existing tech stub or return **Allowed** if not implemented — document. |

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
