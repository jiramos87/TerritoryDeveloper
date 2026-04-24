### Stage 3.3 — Save `asset_id` + `replaced_by` + sprite GC

**Status:** In Progress (tasks filed: TECH-772, TECH-773, TECH-774, TECH-775)

**Objectives:** Persist **`asset_id`** in save; remap retired assets; GC **orphan sprites** safely.

**Exit:**

- Schema bump issue filed if needed; migration respects **`persistence-system`** restore order.
- GC tool or route deletes only **unreferenced** `catalog_sprite` rows.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T3.3.1 | Add save fields | **TECH-772** | Draft | Extend `CellData` / building DTO with **`assetId`**; default **0** means legacy; bump **`GameSaveData.CurrentSchemaVersion`** if required. |
| T3.3.2 | Load-time remap | **TECH-773** | Draft | Walk **`replaced_by`** chain with cycle guard; log telemetry on missing rows. |
| T3.3.3 | GC SQL or admin route | **TECH-774** | Draft | Implement refcount query across **`catalog_asset_sprite`** + **`catalog_pool_member`**; dry-run flag returns candidates. |
| T3.3.4 | Tests for remap + GC | **TECH-775** | Draft | EditMode or server tests cover chain remap + **no delete** when referenced. |

#### §Plan Fix

> plan-review pass 2 — 2 tuples. `out bool remapped` references survive in §Test Blueprint + §Examples after pass-1 §Acceptance fix. Spawn `plan-applier` Mode plan-fix `ia/projects/grid-asset-visual-registry-master-plan.md 3.3`.

```yaml
- operation: replace_section
  target_path: ia/projects/TECH-775.md
  target_anchor: "| `GridAssetRemapTests.StraightChain_ReturnsTerminal`"
  payload: |
    | `GridAssetRemapTests.StraightChain_ReturnsTerminal` | snapshot with `{1→2, 2→3, 3=terminal}`, assetId=1 | `RemapAssetIdOnLoad` returns `3` (int) | unity-batch |
    | `GridAssetRemapTests.CyclicChain_DetectedAndLogged` | snapshot with `{1→2, 2→1}`, assetId=1 | returns `1` (input passthrough); `LogWarning` fired with `cycle` substring | unity-batch |
    | `GridAssetRemapTests.MissingAsset_FallbackNoCrash` | empty snapshot, assetId=99 | returns `99` (identity fallback); no exception thrown | unity-batch |

- operation: replace_section
  target_path: ia/projects/TECH-775.md
  target_anchor: "| EditMode: snapshot `{1→2, 2→3}`, `assetId=1`"
  payload: |
    | EditMode: snapshot `{1→2, 2→3}`, `assetId=1` | `RemapAssetIdOnLoad(1, snap) == 3` | Chain happy path — int return, no `out bool` |
    | EditMode: snapshot `{1→2, 2→1}`, `assetId=1` | `RemapAssetIdOnLoad(1, snap) == 1`; log contains `cycle` | Cycle detection — returns input int |
    | EditMode: empty snapshot, `assetId=99` | `RemapAssetIdOnLoad(99, snap) == 99`; no exception | Missing asset identity fallback |
```

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

<!-- mechanicalization_score: { overall: fully_mechanical, artifact_kind: stage_file_plan, tuples: 4 } -->

```yaml
- reserved_id: "TECH-772"
  title: "Add save fields"
  priority: high
  issue_type: tech
  notes: |
    Extend CellData / building DTO with assetId (int, default 0 = legacy pre-catalog marker); bump
    GameSaveData.schemaVersion + wire MigrateLoadedSaveData when field added. Respect persistence-system
    Load pipeline order — no reorder. Stage 3.3 Phase 1 — save plumbing foundation for remap + GC.
  depends_on: []
  related:
    - "TECH-773"
    - "TECH-774"
    - "TECH-775"
  stub_body:
    summary: |
      Add assetId field to CellData (and/or building DTO) so saves persist stable catalog asset_id;
      bump GameSaveData.schemaVersion when schema changes; migrate legacy saves (assetId == 0) in
      MigrateLoadedSaveData before any restore step.
    goals: |
      - CellData carries assetId (int, default 0) serialized into save.
      - GameSaveData.schemaVersion bumped iff field addition changes binary compatibility.
      - MigrateLoadedSaveData handles legacy saves deterministically (default assetId = 0 = legacy pre-catalog).
      - Save round-trip test covers v0 → v{new} upgrade path.
    systems_map: |
      - Assets/Scripts/Managers/GameManagers/GameSaveManager.cs (CellData DTO, MigrateLoadedSaveData)
      - Assets/Scripts/Managers/GameManagers/GridManager.cs (CellData write/read sites)
      - ia/specs/persistence-system.md §Save + §Load pipeline (restore order unchanged)
      - glossary: Save data, CellData, Legacy save
    impl_plan_sketch: |
      ### Phase 1 — DTO + schema bump
      - [ ] Add assetId field to CellData (default 0).
      - [ ] Bump GameSaveData.schemaVersion if field touches binary layout; else document no-bump rationale.
      - [ ] Extend MigrateLoadedSaveData to treat missing assetId as 0 (legacy marker).
      - [ ] Round-trip test: save v_prev → load → resave matches.
- reserved_id: "TECH-773"
  title: "Load-time replaced_by remap"
  priority: high
  issue_type: tech
  notes: |
    Walk catalog replaced_by chain with cycle guard during load (post-Migrate, pre-restore); log telemetry
    on missing rows (dev = loud placeholder, ship = hide + telemetry per locked decisions). Consumes
    assetId introduced in sibling Add-save-fields task.
  depends_on: []
  related:
    - "TECH-772"
    - "TECH-774"
    - "TECH-775"
  stub_body:
    summary: |
      At load time, each CellData.assetId walks the catalog replaced_by chain (with cycle guard +
      depth cap) to its current live id; missing rows emit telemetry + follow missing-asset policy
      (dev loud placeholder / ship hide).
    goals: |
      - Remap utility consumes GridAssetCatalog snapshot; walks replaced_by with visited-set cycle guard.
      - Missing asset row → telemetry log + fallback per locked decision; load does NOT crash.
      - Integrate remap between MigrateLoadedSaveData and cell restore step (preserves persistence-system order).
      - Unit / EditMode test: chain A→B→C remaps A to C; cyclic chain detected + reported.
    systems_map: |
      - GameSaveManager.cs (load path, post-migrate hook)
      - GridAssetCatalog (catalog snapshot consumer — Inspector ref, no new singleton)
      - ia/specs/persistence-system.md §Load pipeline
      - docs/grid-asset-visual-registry-exploration.md §8.4 point 11 (missing-asset policy)
    impl_plan_sketch: |
      ### Phase 1 — Remap + telemetry
      - [ ] Author RemapAssetIdsOnLoad helper (catalog + cycle guard + depth cap).
      - [ ] Hook between MigrateLoadedSaveData and grid cell restore.
      - [ ] Telemetry log on missing id (category = loud placeholder dev / hide ship).
      - [ ] Tests: straight chain remap, cyclic chain abort, missing row fallback.
- reserved_id: "TECH-774"
  title: "Sprite GC admin route + refcount"
  priority: medium
  issue_type: tech
  notes: |
    GC endpoint or SQL job deletes only unreferenced catalog_sprite rows; refcount walks
    catalog_asset_sprite + catalog_pool_member; dry-run flag returns candidate set without mutation.
    Stage 3.3 Phase 2 — server-side hygiene, no Unity runtime touch.
  depends_on: []
  related:
    - "TECH-772"
    - "TECH-773"
    - "TECH-775"
  stub_body:
    summary: |
      Author admin-only route or SQL migration / job that finds catalog_sprite rows with zero
      references across catalog_asset_sprite + catalog_pool_member; dry-run returns candidate ids;
      commit path deletes only after allowlist + optimistic-lock guards.
    goals: |
      - Refcount query joins catalog_sprite with catalog_asset_sprite + catalog_pool_member; emits orphans.
      - dryRun=true returns candidate rows; dryRun=false commits delete within single transaction.
      - Admin caller_agent allowlist gate on mutating path (reuse existing web/ auth allowlist pattern).
      - Telemetry: delete count + candidate count per run.
    systems_map: |
      - db/migrations/ (no schema change expected — runtime query / route only)
      - web/app/api/catalog/* (new admin subroute, App Router)
      - web/types/api/catalog*.ts (hand-written DTOs — no Drizzle per locked decisions)
      - tools/mcp-ia-server/src/auth/caller-allowlist.ts (mutation allowlist)
      - docs/grid-asset-visual-registry-exploration.md §8.4 point 11
    impl_plan_sketch: |
      ### Phase 1 — Refcount + dry-run
      - [ ] Author SQL refcount query (LEFT JOIN + HAVING COUNT = 0).
      - [ ] Expose POST /api/catalog/sprites/gc with { dryRun } body.
      - [ ] DTO under web/types/api/catalog-gc.ts.
      - [ ] Allowlist gate + transaction wrapper on commit path.
- reserved_id: "TECH-775"
  title: "Tests for remap + GC"
  priority: medium
  issue_type: tech
  notes: |
    EditMode tests cover replaced_by chain remap (straight + cyclic + missing); server-side tests
    (Vitest or existing web test pattern) cover GC refcount — orphan row deleted, referenced row
    preserved. Stage 3.3 Phase 2 — closes Exit criteria.
  depends_on: []
  related:
    - "TECH-772"
    - "TECH-773"
    - "TECH-774"
  stub_body:
    summary: |
      Regression tests for Stage 3.3 remap + GC paths — Unity EditMode for load-time remap helper,
      server-side tests for GC refcount + no-delete-when-referenced invariant.
    goals: |
      - EditMode test asserts A→B→C chain remap result id + missing-row fallback does not crash.
      - Server test asserts GC candidate set excludes any catalog_sprite referenced by catalog_asset_sprite
        OR catalog_pool_member.
      - dry-run vs commit paths both covered.
      - Tests green under npm run validate:all / relevant test runner.
    systems_map: |
      - Assets/Tests/EditMode/ (remap helper tests — match existing pattern)
      - web/ test harness (Vitest / Jest — whichever the repo uses for api routes)
      - GameSaveManager.cs remap hook
      - web/app/api/catalog/sprites/gc route
    impl_plan_sketch: |
      ### Phase 1 — Test coverage
      - [ ] EditMode fixture for RemapAssetIdsOnLoad (3 cases: chain, cycle, missing).
      - [ ] Server test fixture seeds catalog + sprite + pool rows; asserts GC orphan filter.
      - [ ] dryRun path returns candidates without mutation; commit path deletes + row count drops.
      - [ ] Wire into CI / verify:local chain.
```

#### §Stage Audit

_pending — populated by `/audit ia/projects/grid-asset-visual-registry-master-plan.md Stage 3.3` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

_pending — populated by `/closeout ia/projects/grid-asset-visual-registry-master-plan.md Stage 3.3` planner pass when all Tasks reach `Done`._
