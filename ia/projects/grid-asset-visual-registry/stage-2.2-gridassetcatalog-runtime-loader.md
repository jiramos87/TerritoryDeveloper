### Stage 2.2 — `GridAssetCatalog` runtime loader

**Status:** Final

**Objectives:** Parse snapshot at boot; in-memory indexes; **dev hot-reload** subscription stub; **missing-asset** policy dev vs ship compile-time symbols or scripting defines.

**Exit:**

- Main scene contains component instance wired via Inspector; **`Awake`** loads snapshot; **`GetAsset`/`TryGet`** APIs documented XML summary.
- **`OnCatalogReloaded`** invoked after reload.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T2.2.1 | DTOs + parser | **TECH-669** | Done (archived) | `JsonUtility`-friendly DTOs or split files if needed; avoid Newtonsoft unless separate issue introduces it. |
| T2.2.2 | Indexes by id and slug | **TECH-670** | Done (archived) | `Dictionary<int, CatalogAssetEntry>` + composite key `(category, slug)`; defensive duplicates log + skip. |
| T2.2.3 | Missing sprite resolution | **TECH-671** | Done (archived) | Dev: loud placeholder material/sprite reference; Ship: mark row unavailable for UI queries. |
| T2.2.4 | Boot load path | **TECH-672** | Done (archived) | `StreamingAssets`/`Resources` load; timing vs `ZoneSubTypeRegistry` init order documented; no singleton pattern. |
| T2.2.5 | Hot-reload signal stub | **TECH-673** | Done (archived) | Editor/dev only: file watcher or bridge ping triggers reload + event; shipped players no-op. |

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

#### §Stage File Plan

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

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.
