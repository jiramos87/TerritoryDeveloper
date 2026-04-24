# grid-asset-visual-registry — Stage 3.3 Plan Digest

Compiled 2026-04-24 from 4 task spec(s).

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

Persist a stable **`assetId`** (int) on every `CellData` so save → load round-trips preserve `GridAssetCatalog` identity for buildings + terrain; JsonUtility-additive change — `GameSaveData.CurrentSchemaVersion` stays `4` (no binary-layout break); `MigrateLoadedSaveData` stays a no-op for the new field (JSON-absent → default `0` = legacy pre-catalog marker).

### §Acceptance

- [ ] `CellData` class exposes public `int assetId` field (declared next to `sortingOrder` / `height` — existing grid-position block).
- [ ] `CellData.SetDefaults()` sets `this.assetId = 0;`.
- [ ] `CellData.Clone()` copies `assetId`.
- [ ] `GameSaveData.CurrentSchemaVersion` stays `4` (JSON-additive — documented in Decision Log with no-bump rationale).
- [ ] `MigrateLoadedSaveData` does NOT alter `assetId` (JsonUtility defaults absent-field to `0`; matches legacy-marker contract).
- [ ] EditMode test fixture in TECH-775 covers `Save_AssetId_RoundTrip` + `Save_AssetId_LegacyDefaultsZero` + `Migrate_Idempotent` — authored by TECH-775, depends on this Task's DTO shape.
- [ ] `npm run unity:compile-check` exits 0 after edits.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `Save_AssetId_RoundTrip` | `CellData{assetId=7}` save → load | `loaded.assetId == 7` | unity-batch |
| `Save_AssetId_LegacyDefaultsZero` | save JSON with `assetId` field absent | `loaded.assetId == 0` | unity-batch |
| `Migrate_LegacySave_SetsAssetIdZero` | `GameSaveData{schemaVersion=0}` → `MigrateLoadedSaveData` | every `CellData.assetId == 0`; final `schemaVersion == CurrentSchemaVersion` | unity-batch |
| `Migrate_Idempotent` | call `MigrateLoadedSaveData` twice | second call = no-op; `assetId` unchanged | unity-batch |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| Fresh save (`assetId = 7`) | Round-trip: load → resave → `assetId == 7` | Happy path |
| Legacy JSON (no `assetId` field) | `loaded.assetId == 0` | JsonUtility absent-default = 0 = legacy marker |
| Schema 0 → 4 migrate | Migrate runs; `assetId` untouched (already 0 via JsonUtility); final `schemaVersion == 4` | Idempotent |
| Second migrate call | Same `schemaVersion`, same `assetId` | No-op |

### §Mechanical Steps

#### Step 1 — Add `assetId` field to `CellData` DTO

**Goal:** Add public `int assetId` field to `CellData` in the grid-position block (next to `sortingOrder` / `height`); JsonUtility serializes it into save JSON.

**Edits:**
- `Assets/Scripts/Managers/UnitManagers/CellData.cs` — **operation**: edit
  **before**:
  ```csharp
      public int sortingOrder;
      public int height;
  ```
  **after**:
  ```csharp
      public int sortingOrder;
      public int height;

      /// <summary>
      /// Stable <see cref="GridAssetCatalog"/> asset id persisted in save.
      /// 0 = legacy pre-catalog marker (TECH-772). Remap on load handled by TECH-773.
      /// </summary>
      public int assetId;
  ```
- `invariant_touchpoints`:
  - id: `persistence-load-order`
    gate: `rule_content persistence-system § Load pipeline`
    expected: unchanged
  - id: `save-schema-compat`
    gate: `grep -n "CurrentSchemaVersion = 4" Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
    expected: pass
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** Compile fails → re-open Step 1 anchor; field added outside the class body = misplaced. If field position drift breaks `CellData.Clone()` ordering expectations → proceed to Step 3 immediately.

**MCP hints:** `plan_digest_resolve_anchor`, `backlog_issue TECH-772`, `glossary_lookup CellData`.

#### Step 2 — Default `assetId` in `SetDefaults()`

**Goal:** Explicit default `this.assetId = 0;` inside `SetDefaults()` — mirrors symbolic zero for every other numeric field; no throw path in `MigrateLoadedSaveData`.

**Edits:**
- `Assets/Scripts/Managers/UnitManagers/CellData.cs` — **operation**: edit
  **before**:
  ```csharp
          this.closeForestCount = 0;
          this.closeWaterCount = 0;
          this.prefab = null;
  ```
  **after**:
  ```csharp
          this.closeForestCount = 0;
          this.closeWaterCount = 0;
          this.assetId = 0;
          this.prefab = null;
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** Compile fails → re-open Step 2 anchor. If default not emitted in round-trip test → ensure `assetId` declared before `SetDefaults()` call site.

**MCP hints:** `plan_digest_resolve_anchor`.

#### Step 3 — Copy `assetId` in `CellData.Clone()`

**Goal:** Clone semantics must copy `assetId` so in-memory cell duplication (grid hydration, restore flows) preserves asset identity.

**Edits:**
- `Assets/Scripts/Managers/UnitManagers/CellData.cs` — **operation**: edit
  **before**:
  ```csharp
          clone.closeForestCount = closeForestCount;
          clone.closeWaterCount = closeWaterCount;
  ```
  **after**:
  ```csharp
          clone.closeForestCount = closeForestCount;
          clone.closeWaterCount = closeWaterCount;
          clone.assetId = assetId;
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** Compile fails → re-open Step 3 anchor. Clone test in TECH-775 asserts `clone.assetId == orig.assetId`; failure → re-open this step.

**MCP hints:** `plan_digest_resolve_anchor`.

#### Step 4 — Document no-bump rationale in Decision Log

**Goal:** Decision Log records that `CurrentSchemaVersion` stays at `4` — JsonUtility is additive-safe (absent field → type default); no migration branch needed.

**Edits:**
- `ia/projects/TECH-772.md` — **operation**: edit
  **before**:
  ```
  | 2026-04-24 | Default `assetId=0` signals legacy | Avoids null checks; 0 = pre-catalog era | Sentinel value like -1 (less clear) |
  ```
  **after**:
  ```
  | 2026-04-24 | Default `assetId=0` signals legacy | Avoids null checks; 0 = pre-catalog era | Sentinel value like -1 (less clear) |
  | 2026-04-24 | No `CurrentSchemaVersion` bump | JsonUtility defaults absent int field to 0 — legacy JSON loads cleanly; no binary-layout break | Bump to 5 + migrate branch (unnecessary churn) |
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** Validator red on Decision Log table shape → re-open Step 4 with correct pipe-column count.

**MCP hints:** `plan_digest_resolve_anchor`.

### §Findings

- Glossary gap candidate: `Grid asset catalog` (referenced in stub + master plan, no canonical row). Raised in §Open Questions.
- `replaced_by` column confirmed at `Assets/Scripts/Managers/GameManagers/GridAssetCatalog.Dto.cs:86` — consumed by sibling TECH-773.
- JsonUtility additive-safe confirmed: absent int field deserializes to `0`; no migration branch needed in `MigrateLoadedSaveData` for `assetId`.

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

At load time, each `CellData.assetId` walks the `GridAssetCatalog` `replaced_by` chain (visited-set cycle guard + depth cap 100) to the current live id; missing rows emit telemetry + fall back to `0` (legacy marker); hook fires AFTER `MigrateLoadedSaveData` + BEFORE `gridManager.RestoreGrid` so **persistence-system** Load pipeline order is preserved.

### §Acceptance

- [ ] New file `Assets/Scripts/Managers/GameManagers/GridAssetRemap.cs` declares public static helper `int RemapAssetIdOnLoad(int saveAssetId, GridAssetCatalog catalog)` (int / int signature — `catalog_asset.id` parsed from the string DTO at the helper boundary).
- [ ] Helper contract: `assetId == 0` returns `0` early (legacy marker fast path); `catalog == null` returns input `saveAssetId` + emits telemetry `remap.catalog_null`; walks `replaced_by` with visited-set + `MAX_DEPTH = 100`; terminal = first id without a `replaced_by` pointer OR missing row.
- [ ] Cycle detected (visited-set hit) → telemetry `remap.cycle_detected` + returns input `saveAssetId`; no infinite loop.
- [ ] Depth cap exceeded → telemetry `remap.depth_exceeded` + returns input `saveAssetId`.
- [ ] Missing catalog row (`replaced_by` target absent) → telemetry `remap.missing_asset` + returns `0` fallback.
- [ ] `GameSaveManager.LoadGame` invokes `RemapAssetIdsForGrid(saveData.gridData, gridAssetCatalog)` in `GameSaveManager` load path AFTER `MigrateLoadedSaveData(saveData);` and BEFORE `gridManager.RestoreGrid(saveData.gridData);` — persist §Load pipeline step order preserved.
- [ ] `GameSaveManager` holds a serialized `[SerializeField] GridAssetCatalog gridAssetCatalog;` reference; remap skipped with telemetry when null.
- [ ] `npm run unity:compile-check` exits 0 after edits.
- [ ] EditMode fixture in TECH-775 covers 5 cases (straight chain, no-op, cycle, missing, depth cap).

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `Remap_StraightChain_ReturnsTerminal` | catalog `{1→2, 2→3}`, `assetId=1` | returns `3` | unity-batch |
| `Remap_NoReplacedBy_ReturnsSelf` | catalog `{}`, `assetId=5` (row 5 present, no `replaced_by`) | returns `5` | unity-batch |
| `Remap_Cycle_DetectedLoggedReturnsSelf` | catalog `{1→2, 2→1}`, `assetId=1` | returns `1`; telemetry `remap.cycle_detected` logged | unity-batch |
| `Remap_MissingAsset_LogsFallback` | catalog without row `99`, `assetId=99` | returns `0`; telemetry `remap.missing_asset` logged | unity-batch |
| `Remap_DepthCapExceeded_Aborts` | catalog linear chain `1→2→3→…→101`, `assetId=1` | returns `1`; telemetry `remap.depth_exceeded` logged | unity-batch |
| `Remap_LegacyAssetIdZero_Passthrough` | any catalog, `assetId=0` | returns `0` (fast path; no walk) | unity-batch |
| `Remap_CatalogNull_Skips` | catalog null, `assetId=5` | returns `5`; telemetry `remap.catalog_null` logged | unity-batch |
| `LoadPipeline_RemapBeforeRestore` | integration: save with retired `assetId` → load | `saveData.gridData[*].assetId` post-remap = live id; `RestoreGrid` sees live id | unity-batch |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| Chain A(id=`"1"`) → B(id=`"2"`) → C(id=`"3"`), save `assetId=1` | `RemapAssetIdOnLoad(1, catalog) == 3` | Terminal = live asset (no `replaced_by`) |
| Straight live asset (`assetId=5`, row 5 present, `replaced_by == ""`) | `RemapAssetIdOnLoad(5, catalog) == 5` | No-op path |
| Cycle A(`"1"`→`"2"`) → B(`"2"`→`"1"`), save `assetId=1` | Telemetry `remap.cycle_detected` + returns `1` | No infinite loop |
| Missing row (`assetId=99`, `99` absent from catalog) | Telemetry `remap.missing_asset` + returns `0` | Load does NOT crash |
| Legacy save (`assetId=0`) | `RemapAssetIdOnLoad(0, catalog) == 0` | Fast path — no walk |
| Catalog null (Inspector unresolved) | Telemetry `remap.catalog_null` + returns input | Not a throw path |

### §Mechanical Steps

#### Step 1 — Author `GridAssetRemap` helper

**Goal:** Create `Assets/Scripts/Managers/GameManagers/GridAssetRemap.cs` exposing `public static int RemapAssetIdOnLoad(int saveAssetId, GridAssetCatalog catalog)` + `public static void RemapAssetIdsForGrid(List<CellData> cells, GridAssetCatalog catalog)`.

**Edits:**
- `Assets/Scripts/Managers/GameManagers/GridAssetRemap.cs` — **operation**: create
  **after** — new file contents:
  ```csharp
  using System.Collections.Generic;
  using UnityEngine;
  using Territory.Core;

  namespace Territory.GridAsset
  {
      /// <summary>
      /// TECH-773 — Load-time remap of retired <see cref="GridAssetCatalog"/> asset ids to their
      /// current live terminal via <c>replaced_by</c> chain walk.
      /// Visited-set cycle guard + depth cap. Missing rows fall back to 0 (legacy marker).
      /// </summary>
      public static class GridAssetRemap
      {
          public const int MAX_DEPTH = 100;

          public static int RemapAssetIdOnLoad(int saveAssetId, GridAssetCatalog catalog)
          {
              if (saveAssetId == 0) return 0; // legacy pre-catalog marker (TECH-772)
              if (catalog == null)
              {
                  Debug.LogWarning("[GridAssetRemap] remap.catalog_null — catalog unresolved; passthrough.");
                  return saveAssetId;
              }
              var visited = new HashSet<int>();
              int current = saveAssetId;
              int depth = 0;
              while (depth < MAX_DEPTH)
              {
                  if (!visited.Add(current))
                  {
                      Debug.LogWarning($"[GridAssetRemap] remap.cycle_detected assetId={saveAssetId} cycle_node={current}");
                      return saveAssetId;
                  }
                  if (!catalog.TryGetAssetRow(current, out var row))
                  {
                      Debug.LogWarning($"[GridAssetRemap] remap.missing_asset assetId={saveAssetId} missing_node={current}");
                      return 0;
                  }
                  if (string.IsNullOrEmpty(row.replaced_by)) return current; // terminal
                  if (!int.TryParse(row.replaced_by, out int nextId))
                  {
                      Debug.LogWarning($"[GridAssetRemap] remap.replaced_by_parse_error assetId={saveAssetId} value={row.replaced_by}");
                      return current;
                  }
                  current = nextId;
                  depth++;
              }
              Debug.LogWarning($"[GridAssetRemap] remap.depth_exceeded assetId={saveAssetId} depth={depth}");
              return saveAssetId;
          }

          public static void RemapAssetIdsForGrid(List<CellData> cells, GridAssetCatalog catalog)
          {
              if (cells == null) return;
              for (int i = 0; i < cells.Count; i++)
              {
                  var cell = cells[i];
                  if (cell == null) continue;
                  cell.assetId = RemapAssetIdOnLoad(cell.assetId, catalog);
              }
          }
      }
  }
  ```
- `invariant_touchpoints`:
  - id: `persistence-load-order`
    gate: `rule_content persistence-system § Load pipeline`
    expected: unchanged
  - id: `catalog-replaced-by-type`
    gate: `grep -n "public string replaced_by" Assets/Scripts/Managers/GameManagers/GridAssetCatalog.Dto.cs`
    expected: pass
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** Compile fails on `catalog.TryGetAssetRow` — if `GridAssetCatalog` does NOT expose that method, re-open Step 1 and substitute the existing public lookup API on `GridAssetCatalog` (search `GridAssetCatalog.cs` for `public` methods returning `CatalogAssetRowDto`); method signature is an implementer detail the helper wraps — it MUST NOT be invented. If no lookup exists → file a TECH-XXX to add one (blocker for TECH-773). Do NOT close Step 1 until compile is green.

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_verify_paths`, `glossary_lookup Grid asset catalog`, `backlog_issue TECH-773`, `rule_content persistence-system`.

#### Step 2 — Add `[SerializeField] GridAssetCatalog gridAssetCatalog` to `GameSaveManager`

**Goal:** Wire a catalog reference on `GameSaveManager` so the load path has a snapshot at remap time. Inspector binding — no singleton.

**Edits:**
- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — **operation**: edit
  **before**:
  ```csharp
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            MigrateLoadedSaveData(saveData);
  ```
  **after**:
  ```csharp
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            MigrateLoadedSaveData(saveData);
            // TECH-773: remap retired asset ids via catalog replaced_by chain BEFORE cell restore.
            // Persist §Load pipeline order preserved: migrate → remap → restore.
            Territory.GridAsset.GridAssetRemap.RemapAssetIdsForGrid(saveData.gridData, gridAssetCatalog);
  ```
- `invariant_touchpoints`:
  - id: `persistence-load-order`
    gate: `rule_content persistence-system § Load pipeline`
    expected: unchanged
  - id: `load-order-migrate-before-restore`
    gate: `grep -n "MigrateLoadedSaveData(saveData);" Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
    expected: pass
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** Remap insertion placed AFTER `gridManager.RestoreGrid` (line 245) → re-open Step 2 anchor; the before-string pins the migrate line specifically so the sequence stays `migrate → remap → restore`.

**MCP hints:** `plan_digest_resolve_anchor`, `plan_digest_render_literal Assets/Scripts/Managers/GameManagers/GameSaveManager.cs 195 250`.

#### Step 3 — Declare `gridAssetCatalog` serialized field on `GameSaveManager`

**Goal:** Declare the serialized field referenced by Step 2. Place alongside other manager refs (top of class).

**Edits:**
- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — **operation**: edit
  **before**:
  ```csharp
  public class GameSaveManager : MonoBehaviour
  ```
  **after**:
  ```csharp
  public class GameSaveManager : MonoBehaviour
  ```
  (Note: field insertion point pinned via second anchor below — Step 3.)

  Field declaration to be added inside the class body, immediately after the opening brace:
  ```csharp
      [Header("Catalog Refs (TECH-773)")]
      [SerializeField] private GridAssetCatalog gridAssetCatalog;
  ```

  Authoring shortcut — add via anchor to the first existing `[SerializeField]` or `[Header]` declaration in the class:

  **Authoring anchor (alternate — implementer resolves at edit-time via `plan_digest_resolve_anchor`):**
  - Run `plan_digest_resolve_anchor(Assets/Scripts/Managers/GameManagers/GameSaveManager.cs, "public class GameSaveManager")` → get the first unique post-opening-brace anchor (next serialized-field block or `private GridManager gridManager`); insert new `[SerializeField] private GridAssetCatalog gridAssetCatalog;` above it.
- `invariant_touchpoints`:
  - id: `inspector-binding`
    gate: `grep -n "SerializeField.*gridAssetCatalog" Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
    expected: pass
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** Compile fails because `GridAssetCatalog` type not in scope → add `using` for the `GridAssetCatalog` namespace at top of file (same `plan_digest_resolve_anchor` call on existing `using` block). Do NOT close Step 3 until field is declared + compile is green. Scene wiring of this serialized field is Step 5.

**MCP hints:** `plan_digest_resolve_anchor`, `glossary_lookup Grid asset catalog`.

#### Step 4 — Decision Log entry for remap contract

**Goal:** Record the visited-set + MAX_DEPTH=100 + fallback-to-0 contract in TECH-773 §6 Decision Log so downstream readers (TECH-775 test author) see the exact contract.

**Edits:**
- `ia/projects/TECH-773.md` — **operation**: edit
  **before**:
  ```
  | 2026-04-24 | MAX_DEPTH=100 | Prevent infinite loop on data corruption | Higher limits (more permissive but risky) |
  ```
  **after**:
  ```
  | 2026-04-24 | MAX_DEPTH=100 | Prevent infinite loop on data corruption | Higher limits (more permissive but risky) |
  | 2026-04-24 | Missing row → return 0 (legacy marker) | Load does NOT crash; downstream restore treats 0 as pre-catalog | Return input id (stale data risk) |
  | 2026-04-24 | int signature on helper (parse `replaced_by` string at boundary) | Callers already hold `CellData.assetId` (int); parse once at helper edge | string signature (forces callers to stringify) |
  ```
- `invariant_touchpoints`: none (utility)
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0.

**STOP:** Decision Log pipe count mismatch → re-open Step 4 with `| a | b | c | d |` column parity.

**MCP hints:** `plan_digest_resolve_anchor`.

#### Step 5 — Scene Wiring — bind `gridAssetCatalog` on `GameSaveManager` in Main scene

**Goal:** Wire the `GridAssetCatalog` catalog asset into the `[SerializeField] gridAssetCatalog` field on the scene GameObject that hosts `GameSaveManager` (per **scene-wiring** rule — serialized-field without Inspector binding = dead runtime path).

**Edits:** (prefer `unity_bridge_command` kinds; text-edit fallback only if bridge unavailable)
- Step 5.1 — `unity_bridge_command { kind: "open_scene", scene: "Assets/Scenes/Main.unity" }`
- Step 5.2 — `unity_bridge_command { kind: "find_gameobject", path: "GameManagers/GameSaveManager" }` → confirm GameObject hosting `GameSaveManager` component; record full scene path.
- Step 5.3 — `unity_bridge_command { kind: "assign_serialized_field", gameobject_path: "<path from 5.2>", component: "GameSaveManager", field: "gridAssetCatalog", value: { guid: "<guid of GridAssetCatalog asset>", type: "GridAssetCatalog" } }`
- Step 5.4 — `unity_bridge_command { kind: "save_scene", scene: "Assets/Scenes/Main.unity" }`

**Evidence (required in §Acceptance upon completion):**
```yaml
scene: "Assets/Scenes/Main.unity"
parent_object: "<path recorded in 5.2>"
component: "GameSaveManager"
serialized_fields:
  gridAssetCatalog: "<GUID of bound GridAssetCatalog asset>"
unity_events: []
compile_check: "npm run unity:compile-check exits 0"
```

- `invariant_touchpoints`:
  - id: `scene-wiring-serialized-field`
    gate: `rule_content unity-scene-wiring`
    expected: pass
  - id: `scene-file-diff-present`
    gate: `git diff --name-only | grep Main.unity`
    expected: pass
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0.

**STOP:** `git diff` does NOT include `Assets/Scenes/Main.unity` after Step 5.4 → re-open Step 5 (save_scene step did not flush). If `find_gameobject` returns no match → the `GameSaveManager` scene object is named differently; resolve path first via `unity_bridge_command { kind: "find_component", component: "GameSaveManager" }` before retrying 5.2–5.4. Do NOT close Task until scene diff is visible + `npm run unity:compile-check` green.

**MCP hints:** `unity_bridge_command`, `find_gameobject`, `find_component`, `get_compilation_status`, `rule_content unity-scene-wiring`.

### §Findings

- `replaced_by` DTO field confirmed at `Assets/Scripts/Managers/GameManagers/GridAssetCatalog.Dto.cs:86` — type `string`, points to another asset `id` (also `string`); remap helper parses at boundary so callers stay int-typed (matches `CellData.assetId`).
- Persistence invariant: `ia/specs/persistence-system.md` §Load pipeline — 4 restore steps, order locked; remap insertion between migrate + cell restore preserves that order.
- Load hook exact sites in `GameSaveManager.cs`: `MigrateLoadedSaveData(saveData);` at line 200 + `gridManager.RestoreGrid(saveData.gridData);` at line 245.

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

Author regression tests covering Stage 3.3 load-time remap (TECH-773) + sprite GC route (TECH-774): Unity EditMode fixture for `GridAssetRemap.RemapAssetId(...)` (chain / cycle / missing) + colocated Vitest file for `POST /api/catalog/sprites/gc` (dry-run / commit / admin gate / reference protection).

### §Acceptance

- [ ] New EditMode test class at `Assets/Tests/EditMode/GridAssetCatalog/GridAssetRemapTests.cs` — 3 cases (straight chain, cycle, missing) under `namespace Territory.Tests.EditMode.GridAsset`, matching sibling `GridAssetCatalogParseTests.cs` attribute shape (`[Test]`, no explicit `[TestFixture]`).
- [ ] New colocated Vitest file at `web/app/api/catalog/sprites/gc/route.test.ts` — 6 cases (dry-run default, dry-run explicit, commit deletes orphans only, referenced-by-asset-sprite preserved, pool-member asset preserved, non-admin rejected) per TECH-774 §Test Blueprint.
- [ ] Vitest file uses colocated `*.test.ts` pattern (mirrors `web/lib/catalog/stable-json-stringify.test.ts`) — NOT `__tests__/` subdir (repo has both; route tests stay colocated).
- [ ] EditMode uses NUnit `[Test]` + `Assert.*`, not `TestCaseSource`, matching sibling style.
- [ ] Tests assert on TECH-773 public API signature — `GridAssetRemap.RemapAssetId(int assetId, GridAssetSnapshotRoot catalog, out bool remapped)` (resolve exact shape at implement-time from TECH-773 Step 1; spec pins behavior not symbol).
- [ ] `npm -w web run test` exits 0 with GC test file present.
- [ ] `npm run unity:testmode-batch` exits 0 with GridAssetRemapTests present.
- [ ] `npm run validate:all` green after test additions.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| `GridAssetRemapTests.StraightChain_ReturnsTerminal` | snapshot with `{1→2, 2→3, 3=terminal}`, assetId=1 | `RemapAssetId` returns 3; `remapped=true` | unity-batch |
| `GridAssetRemapTests.CyclicChain_DetectedAndLogged` | snapshot with `{1→2, 2→1}`, assetId=1 | returns original id (1) OR sentinel; `remapped=false`; `LogWarning` fired with `cycle` substring | unity-batch |
| `GridAssetRemapTests.MissingAsset_FallbackNoCrash` | empty snapshot, assetId=99 | returns 99 (identity fallback); `remapped=false`; no exception thrown | unity-batch |
| `route.test.ts: gc_dryRun_default_returnsCandidates` | seed 3 sprites, 2 via `catalog_asset_sprite`, body `{}` | `200 { candidates: ["<orphanId>"], count: 1 }`; DB unchanged | vitest |
| `route.test.ts: gc_dryRun_true_explicit` | same seed, body `{ dryRun: true }` | same as default | vitest |
| `route.test.ts: gc_commit_deletesOrphansOnly` | same seed, body `{ dryRun: false }` | `200 { deletedCount: 1, deletedIds: ["<orphanId>"] }`; referenced rows present | vitest |
| `route.test.ts: gc_referencedByAssetSprite_preserved` | sprite bound via `catalog_asset_sprite` only | dryRun excludes it; commit does NOT delete it | vitest |
| `route.test.ts: gc_poolMemberAssetLinkedSprite_preserved` | sprite bound to asset X; X in `catalog_pool_member` | sprite preserved (transitive via `catalog_asset_sprite`) | vitest |
| `route.test.ts: gc_nonAdmin_rejected` | auth header missing / non-admin | `403 { error: "not_allowed" }`; no DB query | vitest |

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| EditMode: snapshot `{1→2, 2→3}`, `assetId=1` | `RemapAssetId(1, snap, out remapped) == 3`; `remapped == true` | Chain happy path |
| EditMode: snapshot `{1→2, 2→1}`, `assetId=1` | return = 1 (fallback); `remapped == false`; log contains `cycle` | Cycle detection |
| EditMode: empty snapshot, `assetId=99` | return = 99; `remapped == false`; no exception | Missing asset identity fallback |
| Vitest: seed orphan `catalog_sprite{id:100}`, `dryRun=true` | `candidates` includes `100`; row count unchanged | GC dry-run |
| Vitest: same seed, `dryRun=false` | `deletedCount == 1`; row gone | GC commit |
| Vitest: seed `catalog_sprite{id:200}` + `catalog_asset_sprite{sprite_id:200}` | dryRun excludes `200`; commit preserves | `catalog_asset_sprite` join hit |
| Vitest: non-admin POST | `403 { error: "not_allowed" }`; no DB query fires | Auth gate precedes DB |

### §Mechanical Steps

#### Step 1 — Author EditMode test `GridAssetRemapTests.cs`

**Goal:** New NUnit EditMode fixture under `Assets/Tests/EditMode/GridAssetCatalog/` asserts behavior of TECH-773 `GridAssetRemap.RemapAssetId` for the 3 canonical cases (chain, cycle, missing). Matches sibling attribute style (`[Test]` + `Assert.*`, plain public class, no `[TestFixture]`).

**Edits:**
- `Assets/Tests/EditMode/GridAssetCatalog/GridAssetRemapTests.cs` — **operation**: create
  **after** — new file contents:
  ```csharp
  using NUnit.Framework;
  using UnityEngine;

  namespace Territory.Tests.EditMode.GridAsset
  {
      /// <summary>TECH-775 — regression tests for TECH-773 load-time remap helper.</summary>
      public class GridAssetRemapTests
      {
          [Test]
          public void RemapAssetId_StraightChain_ReturnsTerminal()
          {
              // Snapshot: assetId 1 → 2, 2 → 3, 3 terminal.
              // Assert: RemapAssetId(1, snap, out remapped) returns 3; remapped == true.
              Assert.Fail("TECH-775: author fixture + assertion against TECH-773 GridAssetRemap API.");
          }

          [Test]
          public void RemapAssetId_CyclicChain_DetectedAndFallsBack()
          {
              // Snapshot: 1 → 2, 2 → 1 (cycle).
              // Assert: returns original (1); remapped == false; LogWarning fired w/ "cycle" substring (LogAssert.Expect).
              Assert.Fail("TECH-775: author cycle fixture + LogAssert.Expect call.");
          }

          [Test]
          public void RemapAssetId_MissingAsset_IdentityFallbackNoCrash()
          {
              // Snapshot: empty assets[].
              // Assert: returns 99 (identity); remapped == false; no exception.
              Assert.Fail("TECH-775: author missing-asset fixture.");
          }
      }
  }
  ```
- `invariant_touchpoints`:
  - id: `editmode-test-dir`
    gate: `test -d Assets/Tests/EditMode/GridAssetCatalog`
    expected: pass
- `validator_gate`: `npm run unity:compile-check`

**Gate:**
```bash
npm run unity:compile-check
```
Expectation: exit 0 (new EditMode fixture compiles under `Assets/Tests/EditMode/GridAssetCatalog/`).

**STOP:** Namespace drift → re-run Step 1 (Write tool) matching `Territory.Tests.EditMode.GridAsset` namespace exactly (per sibling `GridAssetCatalogParseTests.cs`). Test class MUST be public + name ending in `Tests` for Unity Test Runner discovery. Do NOT add `[TestFixture]` attribute — siblings rely on NUnit default.

**MCP hints:** `plan_digest_verify_paths`, `plan_digest_resolve_anchor`.

#### Step 2 — Author Vitest colocated file `route.test.ts`

**Goal:** New Vitest file colocated with the GC route asserts dry-run / commit / admin-gate / reference-protection paths per TECH-774 §Test Blueprint. Uses Vitest `describe` / `it` / `expect` API (NOT `node:test`). Seeds DB fixtures via existing web/ test helpers (resolved at implement-time).

**Edits:**
- `web/app/api/catalog/sprites/gc/route.test.ts` — **operation**: create
  **after** — new file contents:
  ```ts
  /**
   * TECH-775 — Regression tests for TECH-774 POST /api/catalog/sprites/gc.
   * Colocated with route.ts per web/ Vitest convention.
   */
  import { describe, it, expect, beforeEach } from "vitest";

  // NOTE: concrete imports resolved at implement-time.
  // Expected neighbors:
  //   - POST handler from "./route"
  //   - DB seed helpers: resolve per existing neighbor tests under web/tests/api/catalog/
  //   - Admin mock: resolve via Vitest module-mock of the concrete admin-check symbol chosen in TECH-774 Step 2

  describe("POST /api/catalog/sprites/gc", () => {
    beforeEach(async () => {
      // TECH-775: reset test DB + seed baseline sprites/assets/asset_sprite/pool rows.
    });

    it("dryRun default returns orphan candidates without mutation", async () => {
      expect.fail("TECH-775: seed 3 sprites (1 orphan, 2 referenced); POST {}; assert candidates=[orphanId], count=1; DB row count unchanged.");
    });

    it("dryRun=true explicit behaves identically to default", async () => {
      expect.fail("TECH-775: same seed; POST { dryRun: true }; assert response matches default.");
    });

    it("commit path deletes orphans only", async () => {
      expect.fail("TECH-775: same seed; POST { dryRun: false }; assert deletedCount=1; referenced rows still present.");
    });

    it("sprite referenced via catalog_asset_sprite is preserved", async () => {
      expect.fail("TECH-775: seed sprite w/ catalog_asset_sprite row; dryRun candidates exclude it; commit does NOT delete it.");
    });

    it("sprite whose asset is in catalog_pool_member is preserved (transitive)", async () => {
      expect.fail("TECH-775: seed sprite + asset_sprite + pool_member(asset_id); preserved in both paths.");
    });

    it("non-admin caller receives 403 not_allowed with no DB query", async () => {
      expect.fail("TECH-775: stub requireAdmin → reject; assert 403 { error: 'not_allowed' } + no DB mutation observed.");
    });
  });
  ```
- `invariant_touchpoints`:
  - id: `vitest-route-parent-dir-exists`
    gate: `test -d web/app/api/catalog`
    expected: pass
  - id: `vitest-runner-wired`
    gate: `grep -n '"test": "vitest run' web/package.json`
    expected: pass
- `validator_gate`: `npm -w web run typecheck`

**Gate:**
```bash
npm -w web run typecheck
```
Expectation: exit 0 (colocated Vitest file typechecks against TECH-774 route + DTO landing).

**STOP:** Typecheck drift → re-run Step 2. If implementer drifts into `node:test` idioms (per `web/lib/catalog/stable-json-stringify.test.ts`) instead of Vitest `describe`/`it` → re-open Step 2 and align: Vitest is the script runner per `web/package.json:16` (`"test": "vitest run --passWithNoTests"`). Non-admin stub MUST fire BEFORE any DB call — assertion required to catch auth-gate regression.

**MCP hints:** `plan_digest_verify_paths`, `plan_digest_resolve_anchor`.

#### Step 3 — Flesh test bodies against TECH-773 + TECH-774 public API (post-landing)

**Goal:** Replace `Assert.Fail` / `expect.fail` stubs in Steps 1–2 with real fixtures + assertions once TECH-773 (`GridAssetRemap`) + TECH-774 (route + DTOs) have landed on the branch. Stage 3.3 Phase 2 Exit criteria gate this step — sequence must be TECH-772 → TECH-773 → TECH-774 → TECH-775.

**Edits:**
- `Assets/Tests/EditMode/GridAssetCatalog/GridAssetRemapTests.cs` — **operation**: edit
  **before**:
  ```
  Assert.Fail("TECH-775: author fixture + assertion against TECH-773 GridAssetRemap API.");
  ```
  **after**:
  ```
  // Snapshot JSON built inline (per GridAssetCatalogParseTests.cs MinFixture pattern):
  //   "assets": [ { "id": 1, "replaced_by": "2" }, { "id": 2, "replaced_by": "3" }, { "id": 3, "replaced_by": null } ]
  // GridAssetCatalog.TryParseSnapshotJson → snapshot root; pass to GridAssetRemap.RemapAssetId(1, root, out bool remapped).
  // Assert.AreEqual(3, remapped_id); Assert.IsTrue(remapped);
  ```
- `web/app/api/catalog/sprites/gc/route.test.ts` — **operation**: edit
  **before**:
  ```
  expect.fail("TECH-775: seed 3 sprites (1 orphan, 2 referenced); POST {}; assert candidates=[orphanId], count=1; DB row count unchanged.");
  ```
  **after**:
  ```
  // Seed via web/ test DB helpers; invoke POST handler directly (App Router pattern: `await POST(new NextRequest(...))`).
  // Assert response.status === 200; body.candidates to equal [orphanId]; body.count === 1.
  // Post-commit SELECT count confirms DB unchanged.
  ```
- `invariant_touchpoints`:
  - id: `sequencing-tech-773-774-landed`
    gate: `backlog_issue TECH-773` + `backlog_issue TECH-774` (both Status=Done before Step 3 executes)
    expected: pass
  - id: `validate-all-green`
    gate: `npm run validate:all`
    expected: pass
- `validator_gate`: `npm run validate:all`

**Gate:**
```bash
npm run validate:all
```
Expectation: exit 0. EditMode batch green + Vitest green + no other chain regressions.

**STOP:** `Assert.Fail` / `expect.fail` still present after Step 3 → re-open Step 3; tests are gated stubs, not acceptance. If TECH-773 or TECH-774 not merged yet → Step 3 blocked — re-sequence per Stage 3.3 Phase 2 Exit (do NOT merge Step 3 ahead of its dependencies). If `GridAssetRemap` public signature differs from spec (e.g. helper returns struct instead of `out bool`) → update the §Acceptance signature line + Step 3 assertions in the same commit.

**MCP hints:** `plan_digest_resolve_anchor`, `backlog_issue TECH-773`, `backlog_issue TECH-774`.

#### Step 4 — Decision Log entries

**Goal:** Record the two concrete test-layout choices made during digest authoring so downstream readers do not re-debate.

**Edits:**
- `ia/projects/TECH-775.md` — **operation**: edit
  **before**:
  ```
  | 2026-04-24 | EditMode + server separate | Domain separation (load vs server) | Single fixture (less clear boundaries) |
  | 2026-04-24 | Standard test patterns | Consistency with repo test suite | Custom test framework (overhead) |
  ```
  **after**:
  ```
  | 2026-04-24 | EditMode + server separate | Domain separation (load vs server) | Single fixture (less clear boundaries) |
  | 2026-04-24 | Standard test patterns | Consistency with repo test suite | Custom test framework (overhead) |
  | 2026-04-24 | Vitest colocated `route.test.ts` (not `__tests__/`) | Matches `web/lib/catalog/stable-json-stringify.test.ts` + Next.js App Router colocated convention | Nested `__tests__/` (used by `web/lib/__tests__/` but not by route tests) |
  | 2026-04-24 | EditMode test dir = `Assets/Tests/EditMode/GridAssetCatalog/` | Sibling convention (`GridAssetCatalogParseTests.cs`, `CursorPlacementPreviewTests.cs`, `PlacementReasonTooltipTests.cs`) + shared `min_snapshot.json` fixture | Flat `Assets/Tests/EditMode/` (stub §5.2 draft; rejected as inconsistent) |
  ```
- `invariant_touchpoints`:
  - id: `decision-log-column-parity`
    gate: `grep -c "^| 2026-04-24" ia/projects/TECH-775.md`
    expected: pass
- `validator_gate`: `npm run validate:frontmatter`

**Gate:**
```bash
npm run validate:frontmatter
```
Expectation: exit 0.

**STOP:** Decision Log pipe count mismatch → re-open Step 4 aligning `| a | b | c | d |` column parity.

**MCP hints:** `plan_digest_resolve_anchor`.


## Final gate

```bash
npm run validate:all
```
