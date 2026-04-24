### Stage 2.3 — Zone S consumer migration

**Status:** Final

**Objectives:** **`ZoneSubTypeRegistry`** reads **`GridAssetCatalog`** for costs, names, sprite paths; retain JSON fallback behind define only if needed for one-stage rollback (prefer single source).

**Exit:**

- `SubTypePickerModal`, `BudgetAllocationService`, `ZoneSService` compile against new lookup APIs.
- EditMode tests cover seven ids resolution.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T2.3.1 | Wire registry to catalog | **TECH-684** | Done (archived) | Inject `[SerializeField] GridAssetCatalog catalog` + fallback `FindObjectOfType` in `Awake` on `ZoneSubTypeRegistry` GameObject. |
| T2.3.2 | Map subTypeId to asset_id | **TECH-685** | Done (archived) | Stable mapping table (`0..6` → catalog PK) from seed; document migration from JSON-only era. |
| T2.3.3 | Update callers | **TECH-686** | Done (archived) | Adjust `UIManager` / modals to use registry APIs without breaking envelope logic. |
| T2.3.4 | EditMode tests | **TECH-687** | Done (archived) | Tests load snapshot fixture under `Assets/Tests/EditMode/...`; assert costs + display names. |

#### §Stage Audit

> Post-ship aggregate — task specs **TECH-684**–**TECH-687** removed at closeout.

- **TECH-684:** `ZoneSubTypeRegistry` requires scene `GridAssetCatalog`; `Awake` resolves ref once; exposes internal `Catalog` getter.
- **TECH-685:** Identity map 0..6 → catalog PKs per Zone S seed; `TryGetAssetIdForSubType`.
- **TECH-686:** `GridAssetCatalog` indexes `catalog_economy` by `asset_id`; registry façade for picker labels + placement cost sim units (`base_cost_cents / 100`); `SubTypePickerModal` + `ZoneSService` updated.
- **TECH-687:** Fragment JSON fixture + `ZoneSubTypeRegistryCatalogBackedTests` for seven ids.

**Verification (Stage):** `npm run validate:all` green; `npm run unity:compile-check` green; `npm run unity:testmode-batch -- --quit-editor-first` exit 0; `npm run db:bridge-playmode-smoke` exit 0 (after `unity:ensure-editor`).

#### §Stage Closeout Plan

> **Applied 2026-04-22 (ship-stage-main-session):** archived **TECH-684**…**TECH-687** to `ia/backlog-archive/` (`status: closed`, `completed: "2026-04-22"`); removed temporary `ia/projects/TECH-684`…`TECH-687` specs; flipped Stage 2.3 task table to **Done (archived)** and Stage **Status** to **Final**; ran `materialize-backlog.sh` + `validate:all`.

#### §Stage File Plan

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

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.
