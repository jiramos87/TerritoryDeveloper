---
purpose: "Reference spec for Unity development context — Territory Developer."
audience: agent
loaded_by: router
slices_via: spec_section
---
# Unity development context — Territory Developer

> First-party **Unity** and **Editor** conventions for this repository: **MonoBehaviour** wiring, **Inspector** usage, dependency resolution, and 2D rendering fields. Does **not** replace [`isometric-geography-system.md`](isometric-geography-system.md) for **Sorting order** math, terrain, or roads.

## Table of contents

1. [Purpose and scope](#1-purpose-and-scope)
2. [MonoBehaviour lifecycle](#2-monobehaviour-lifecycle)
3. [Inspector, SerializeField, and dependency resolution](#3-inspector-serializefield-and-dependency-resolution)
4. [Scenes, prefabs, and renaming](#4-scenes-prefabs-and-renaming)
5. [2D sorting layers, sortingOrder, and Sorting order](#5-2d-sorting-layers-sortingorder-and-sorting-order)
6. [Script Execution Order and initialization](#6-script-execution-order-and-initialization)
7. [Anti-patterns and project guardrails](#7-anti-patterns-and-project-guardrails)
8. [ScriptableObject](#8-scriptableobject)
9. [Glossary alignment](#9-glossary-alignment)
10. [Editor agent diagnostics (machine-readable exports)](#10-editor-agent-diagnostics-machine-readable-exports)
11. [Unity Test Framework — Edit Mode compute parity](#11-unity-test-framework--edit-mode-compute-parity)

---

## 1. Purpose and scope

**Audience:** Contributors and IDE agents working in `Assets/Scripts/` and **Unity** scenes.

**Default:** Prefer this spec, [`ia/rules/project-overview.md`](../rules/project-overview.md), [`ia/rules/invariants.md`](../rules/invariants.md), [`ia/rules/coding-conventions.md`](../rules/coding-conventions.md), and **territory-ia** tools (`spec_section`, `glossary_discover`, etc.) before generic **Unity** web documentation.

**When to use external docs:** Version-specific APIs, bugs, or platform details not stated in this repo.

**Out of scope here:** Full **Unity** manual text; authoritative **cell** geometry, **HeightMap**, **road preparation family**, water, cliffs, and **Sorting order** formulas — see [`isometric-geography-system.md`](isometric-geography-system.md) and linked specs.

**Territory-ia and exports:** This file is registered as **`unity-development-context`** (aliases **`unity`**, **`unityctx`**) for **`spec_outline`** / **`spec_section`**. **Open** roadmap items (DB-backed MCP slices, mechanical repo checks, **Editor** export follow-ups) live in [`BACKLOG.md`](../../BACKLOG.md) — verify status there, not in this spec. **Shipped** **Editor** JSON/Markdown exports are summarized in [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md); **expected menus, prerequisites, and outputs** are defined in **§10** below. Treat output paths and tool names for in-flight work as **planned** until the corresponding **BACKLOG** row ships. If menus or **Sorting** export fail in practice, file or update the relevant **open** row in [`BACKLOG.md`](../../BACKLOG.md).

---

## 2. MonoBehaviour lifecycle

Managers and controllers are **scene** `MonoBehaviour` components; they are **not** constructed with `new`. See [`ia/rules/invariants.md`](../rules/invariants.md) — *IF creating a new manager → THEN MonoBehaviour scene component, never `new`*.

**`Awake`:** Use for self-setup and for resolving references that must exist before other scripts’ `Start` when order is guaranteed by **Unity** (same scene) or by **Script Execution Order** (see §6). Example: **`ZoneManager`** builds its zone prefab dictionary in **`Awake`** so lookups are ready before dependent **`Start`** logic ([`ZoneManager.cs`](../../Assets/Scripts/Managers/GameManagers/ZoneManager.cs)).

**`OnEnable`:** Runs when the component becomes enabled (after **`Awake`** on first activation, and again whenever the object is re-enabled). Use for event subscriptions or **`UI`** that must react to enable/disable. This gameplay codebase rarely overrides **`OnEnable`** on managers; prefer **`Awake`** / **`Start`** unless you need enable-cycle behavior.

**`Start`:** Use for logic that depends on other components having finished **`Awake`**, when those components are in the same scene and no custom execution order is set. Some types defer **`FindObjectOfType`** to **`Start`** so sibling **`Awake`** chains can finish first — e.g. **`WaterManager`** assigns **`gridManager`** in **`Start`** if null ([`WaterManager.cs`](../../Assets/Scripts/Managers/GameManagers/WaterManager.cs)). **`ZoneManager.Start`** double-checks prefab initialization after **`Awake`** ([`ZoneManager.cs`](../../Assets/Scripts/Managers/GameManagers/ZoneManager.cs)).

**Coroutines, `Invoke`, and delayed work:** **`GameBootstrap`** yields one frame then runs **New Game** / **Load** intent via **`StartCoroutine(ProcessStartIntent())`** ([`GameBootstrap.cs`](../../Assets/Scripts/Managers/GameManagers/GameBootstrap.cs)). **`UIManager`** uses **`Invoke`** and **`StartCoroutine`** for timed UI (e.g. tooltip hide) ([`UIManager.cs`](../../Assets/Scripts/Managers/GameManagers/UIManager.cs)). **`ForestManager`** defers visual updates with a coroutine ([`ForestManager.cs`](../../Assets/Scripts/Managers/GameManagers/ForestManager.cs)). **`GameNotificationManager`** sequences fades with coroutines ([`GameNotificationManager.cs`](../../Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs)).

**Caching:** Resolve dependencies once during initialization; do not query the scene every frame (§7).

**Where to look next:** Manager responsibilities and dependencies are summarized in [`managers-reference.md`](managers-reference.md) — link there instead of duplicating tables.

---

## 3. Inspector, SerializeField, and dependency resolution

**Target pattern (guardrail):** `[SerializeField] private` fields for dependencies, with `FindObjectOfType<T>()` **fallback** in `Awake` (or a helper called from `Awake`) when the **Inspector** reference is missing. See [`ia/rules/invariants.md`](../rules/invariants.md) guardrail: *IF adding a manager reference → THEN `[SerializeField] private` + `FindObjectOfType` fallback in `Awake`*.

**In-repo example (recommended shape):** `GameDebugInfoBuilder` keeps optional managers as `[SerializeField] private` and resolves them in `Awake` via `ResolveRefsIfNeeded()` using `FindObjectOfType` when null ([`GameDebugInfoBuilder.cs`](../../Assets/Scripts/Managers/GameManagers/GameDebugInfoBuilder.cs)). A search under `Assets/Scripts/Managers/` currently surfaces that type as the clearest full match for **`[SerializeField] private` manager refs + `FindObjectOfType`**; many other types still use public **Inspector** fields or private fields **without** `[SerializeField]` and resolve peers in **`Start`** (e.g. **`DemandManager`** — [`DemandManager.cs`](../../Assets/Scripts/Managers/GameManagers/DemandManager.cs)).

**Legacy / mixed style:** Many managers still expose `public GridManager gridManager` (and similar) wired in the **Inspector**, with `FindObjectOfType` in `Awake` if null — e.g. `InterstateManager` ([`InterstateManager.cs`](../../Assets/Scripts/Managers/GameManagers/InterstateManager.cs)), `TerraformingService` ([`TerraformingService.cs`](../../Assets/Scripts/Managers/GameManagers/TerraformingService.cs)). Prefer **`SerializeField` private** for **new** fields so encapsulation matches **coding-conventions**; refactor public dependency fields only when touching the type for other reasons.

**Third-party stacks:** Do **not** document **Addressables** (or similar) here unless usage appears under `Assets/Scripts/` — the current snapshot has **no** **Addressables** references there; re-check with search before adding package-specific guidance.

**Invariant:** Never call `FindObjectOfType` from `Update` or other per-frame paths — cache in `Awake` / `Start`. See [`ia/rules/invariants.md`](../rules/invariants.md).

---

## 4. Scenes, prefabs, and renaming

- **Missing references:** After moving or renaming scripts, **prefabs** and scenes can show “Missing (Mono Script)”. Reassign scripts or use **Unity**’s script mapping / metadata repair; agents should not assume GUID stability across branches without checking **YAML** / **meta** if merges broke links.
- **`.meta` and GUIDs:** **Unity** stores script and asset identity in `.meta` files. Duplicating a **prefab** or asset in the file system without letting the **Editor** regenerate **meta** can produce duplicate GUIDs or broken references — prefer duplicate inside the **Editor**, or run a deliberate GUID repair workflow.
- **Scene / prefab YAML:** Serialized scenes and **prefabs** are text **YAML**; merge conflicts can drop component blocks or scramble **fileID** references. Resolve conflicts in the **Editor** when possible; after manual **YAML** edits, open the asset in **Unity** to validate.
- **Renaming types:** Prefer updating `/// <summary>` and **XML docs** on the class when behavior changes ([`ia/rules/coding-conventions.md`](../rules/coding-conventions.md)).
- **Managers in scenes:** Core gameplay types live under `Assets/Scripts/Managers/` per [`ia/rules/project-overview.md`](../rules/project-overview.md); scene wiring is **Inspector**-driven — document new mandatory references on the relevant **Manager** when adding dependencies.

---

## 5. 2D sorting layers, sortingOrder, and Sorting order

**Unity** concepts:

- **Sorting Layer** — Named layer (e.g. Default, UI) used by **SpriteRenderer** / **TilemapRenderer** for coarse grouping.
- **`sortingOrder`** — Integer offset within a layer; higher draws in front.

**Project-specific Sorting order (isometric):** **Cell** visuals and terrain stacks use a **script-driven** formula. Vocabulary such as **`typeOffset`**, **`depthOrder`**, **`heightOrder`**, and **`TERRAIN_BASE_ORDER`** is defined in [`isometric-geography-system.md`](isometric-geography-system.md) section **7. Sorting Order System** and in [`glossary.md`](glossary.md) (**Sorting order**) — read that spec (or **`spec_section`** with **`geo`** + section **`7`**) instead of duplicating formulas here.

**Code entry points:** **`GridManager`** exposes **`SetTileSortingOrder`**, **`SetZoningTileSortingOrder`**, **`SetZoneBuildingSortingOrder`**, and related APIs under `#region Sorting Order` and delegates computation to **`GridSortingOrderService`** ([`GridManager.cs`](../../Assets/Scripts/Managers/GameManagers/GridManager.cs)). Implementation and height-aware assignment live in [`GridSortingOrderService.cs`](../../Assets/Scripts/Managers/GameManagers/GridSortingOrderService.cs) (constructed with a **`GridManager`** reference — see class summary there).

**Rule of thumb:** If a change affects **which** object draws above another on the **grid** for the same **cell** or neighbor relationship, consult geography §7 and [`glossary.md`](glossary.md) (**Sorting order**). If a change is **UI**-only, see [`ui-design-system.md`](ui-design-system.md).

---

## 6. Script Execution Order and initialization

**Unity** runs `Awake` / `OnEnable` / `Start` in a defined order; scripts on the same **GameObject** run in component order unless **Edit → Project Settings → Script Execution Order** overrides.

**Geography and New Game:** **`GeographyManager`** coordinates **terrain**, **water**, **forests**, and related **geography initialization** from **`Start`** ( **`initializeOnStart`**, **`FindObjectOfType`** fallbacks for unassigned refs) ([`GeographyManager.cs`](../../Assets/Scripts/Managers/GameManagers/GeographyManager.cs)). It is the stable high-level entry for **New Game** map build wiring in scenes that include it; detailed phase order and save/load distinctions live in [`isometric-geography-system.md`](isometric-geography-system.md), [`water-terrain-system.md`](water-terrain-system.md), and [`simulation-system.md`](simulation-system.md) — do not invent call order here.

**Initialization races:** If **Manager A** needs **Manager B** in its `Awake`, ensure **B** runs first (execution order), move resolution to `Start`, or use explicit init methods called from a known coordinator. Canonical pattern for per-frame consumers (e.g. **`TimeManager.Update`** tick block reading **`GeographyManager`** state): expose `public bool IsInitialized { get; private set; }` on the producer, flip true at the tail of the init pipeline, guard the consumer's tick block (not the whole `Update`) on the flag so UI/input stays responsive during load. Prefer this explicit gate over **Script Execution Order** for gameplay-adjacent ordering — testable, no hidden Project Settings config.

**Prefer** deterministic setup: **Inspector** wires first; `FindObjectOfType` fallback second; avoid lazy resolution in hot paths. `GameDebugInfoBuilder` re-calls `ResolveRefsIfNeeded()` before building strings so late-instantiated scenes still work — that pattern is for **debug** utilities, not per-frame gameplay.

**Related managers:** Per-type responsibilities remain in [`managers-reference.md`](managers-reference.md); avoid copying its tables into this document.

---

## 7. Anti-patterns and project guardrails

| Do not | Do instead |
|--------|------------|
| New global **singletons** | **Inspector** + `FindObjectOfType` (see §3). **Exception:** `GameNotificationManager.Instance` is the documented single **singleton** ([`GameNotificationManager.cs`](../../Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs)); do **not** add new ones ([`ia/rules/invariants.md`](../rules/invariants.md)). |
| `FindObjectOfType` in `Update` / per-frame loops | Cache references in `Awake` / `Start` |
| `GetComponent<T>()` / `GetComponentInChildren<T>()` in `Update` / per-frame loops | Cache the component in `Awake` / `Start` (or resolve once when the target instance is created); same spirit as the **`FindObjectOfType`** invariant |
| Direct `gridArray` / `cellArray` access | `GridManager.GetCell(x, y)` ([`ia/rules/invariants.md`](../rules/invariants.md)) |
| New responsibilities on `GridManager` | Extract helpers ([`ia/rules/invariants.md`](../rules/invariants.md)) |
| Road placement bypassing preparation pipeline | **Road preparation family** ending in `PathTerraformPlan` + Phase-1 + `Apply` ([`ia/rules/invariants.md`](../rules/invariants.md)) |

---

## 8. ScriptableObject

There is **no** broad use of **`ScriptableObject`** in gameplay scripts under `Assets/Scripts/` in the current codebase snapshot. If a feature introduces **ScriptableObject** assets, follow **Unity** serialization rules and [`ia/rules/coding-conventions.md`](../rules/coding-conventions.md) for naming and **XML** documentation; prefer **Inspector**-friendly, immutable-ish config types.

---

## 9. Glossary alignment

Cross-check these terms in [`glossary.md`](glossary.md) and linked specs when writing or reviewing code:

- **Cell**, **HeightMap**, **GridManager**, **Sorting order**, **WaterMap**, **Geography initialization**, **street** / **interstate**, **road stroke**, **AUTO systems**, **terraform** / **PathTerraformPlan**, **Map border**

For **Unity**-only vocabulary (**MonoBehaviour**, **Inspector**, **SerializeField**, **Prefab**), this document and **Unity** docs suffice; keep **game** terms aligned with the glossary.

---

## 10. Editor agent diagnostics (machine-readable exports)

**Purpose:** Give IDE agents reproducible, **glossary-aligned** snapshots of **Editor** / **Play Mode** context and optional **Sorting order** debugging material without hand-copying **Inspector** values. Player builds **must not** gain these menus (implementation lives under `Assets/Scripts/Editor/`).

**Information architecture (outputs):**

| Artifact | Primary persistence | Role |
|----------|---------------------|------|
| Agent context | Postgres **`editor_export_agent_context`** (`document jsonb`) | `schema_version`, `exported_at_utc`, active scene, selection summary, bounded **grid** sample (**Cell**, **`HeightMap`**, **`WaterMap`**, direct-child **`GameObject`** name hints). **MCP** queue: **`agent_bridge_job`** + **`unity_bridge_command`** / **`unity_bridge_get`** (glossary **IDE agent bridge**); optional **`params.seed_cell`** (`"x,y"` Moore center). **IDE bridge** also writes **`tools/reports/agent-context-bridge-*.json`** (gitignored) for **`artifact_paths`**. |
| Sorting debug | Postgres **`editor_export_sorting_debug`** | Per-**cell** lines derived from **`TerrainManager`** public sorting APIs and sampled **`SpriteRenderer.sortingOrder`**; narrative ties to [`isometric-geography-system.md`](isometric-geography-system.md) **Sorting order** (do not duplicate formula authority here) |
| Cell chunk interchange | Postgres **`editor_export_terrain_cell_chunk`** | **Play Mode**; **`artifact`**: `terrain_cell_chunk` — **Cell** subset + heights ( **`HeightMap`** when available); [`cell-chunk-interchange.v1.schema.json`](../../docs/schemas/cell-chunk-interchange.v1.schema.json) |
| World snapshot dev | Postgres **`editor_export_world_snapshot_dev`** | **Play Mode**; **`artifact`**: `world_snapshot_dev` — **Water map** body-id histogram + optional **HeightMap** raster; not Save data ([`world-snapshot-dev.v1.schema.json`](../../docs/schemas/world-snapshot-dev.v1.schema.json)) |
| Geography init report (dev) | `tools/reports/last-geography-init.json` (stable filename; gitignored) | **Play Mode** after **Geography initialization**; **`artifact`**: `geography_init_report` — master seed, effective toggles, optional string **`interchange_snapshot_json`** when **`geography_init_params`** loaded; **`npm run validate:geography-init-report`**; [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) |
| Agent test mode batch report | `tools/reports/agent-testmode-batch-*.json` (gitignored) | **Batchmode** **Editor** via **`npm run unity:testmode-batch`** — **`GameSaveManager.LoadGame`** on a **fixture** path; optional simulation ticks; optional **`--golden-path`** / **`-testGoldenPath`** (integer **CityStats** golden — mismatch exit **8**); report **`schema_version`** **2** may include **`city_stats`**. **`EditorApplication.Exit`** exit code. **glossary** **Agent test mode batch**; [`tools/fixtures/scenarios/README.md`](../../tools/fixtures/scenarios/README.md). Distinct from **IDE agent bridge** exports. |
| UI inventory (dev) | Postgres **`editor_export_ui_inventory`** | **Edit Mode**; **`artifact`**: `ui_inventory_dev` — bounded **`scenes[]`** (**MainMenu** + **city** scene allowlist), **Canvas** / **RectTransform** / **Graphic** / **Text** / **TMP** sampling; not **Save data** (**`ui-design-system.md`** — **Machine-readable traceability**) |
| UI inventory baseline (committed) | [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) | **Committed** copy of a **`document` jsonb** snapshot (or fresh export) for **`ui-design-system.md`** **as-built** tables; refresh when **UI** scenes change ([`docs/reports/README.md`](../../docs/reports/README.md)) |

**Reports → Postgres-only (registry exports):** **Territory Developer → Reports** items that use **`EditorPostgresExportRegistrar.TryPersistReport`** persist **only** to **`editor_export_*`** tables (**full body** in **`document jsonb`** — glossary **Editor export registry**). There is **no** workspace fallback under `tools/reports/` for **menu-driven** exports: configure **`DATABASE_URL`** (process env, **EditorPrefs**, or **`.env.local`**) and apply migrations (see below). **Exception:** **`export_agent_context`** dispatched by **`AgentBridgeCommandRunner`** mirrors the same JSON to **`tools/reports/agent-context-bridge-*.json`** (gitignored; **`artifact_paths`** in the bridge response) so IDE agents can read the snapshot from disk. **Staging** for **`register-editor-export.mjs`** uses the OS temp directory, not `tools/reports/`. **IDE bridge:** **territory-ia** **`unity_bridge_command`** inserts **`agent_bridge_job`** rows (**`kind`:** **`export_agent_context`**, **`get_console_logs`**, **`capture_screenshot`**, **`enter_play_mode`**, **`exit_play_mode`**, **`get_play_mode_status`**, **`get_compilation_status`**, **`debug_context_bundle`**; **`request` jsonb** carries **`params`** per kind, including optional **`seed_cell`** for **`export_agent_context`** and required **`seed_cell`** for **`debug_context_bundle`**). **`get_compilation_status`** completes synchronously ( **`EditorApplication.isCompiling`**, **`EditorUtility.scriptCompilationFailed`**, recent **`error`** lines from **`AgentBridgeConsoleBuffer`** → **`response.compilation_status`**). Optional MCP **`unity_compile`** enqueues the same **`kind`**. **Unity** **`AgentBridgeCommandRunner`** runs **`agent-bridge-dequeue.mjs`** / **`agent-bridge-complete.mjs`** (migration **`0008_agent_bridge_job.sql`**). **`get_console_logs`** uses **`AgentBridgeConsoleBuffer`** (ring buffer, cleared on script domain reload). **`capture_screenshot`** writes **`tools/reports/bridge-screenshots/*.png`** (**Play Mode**). Default path renders a **Camera** (world / Camera-mode UI); **`params.include_ui`** uses **`ScreenCapture`** for the **Game view** (includes **Screen Space - Overlay** UI; deferred completion via **`EditorApplication.update`** pump). **`debug_context_bundle`** reuses that deferred screenshot pump when **`include_screenshot`** is true (always **Game view** **`ScreenCapture`** for the bundle); combines **`AgentBridgeAnomalyScanner`** Moore rules with export + console in **`response.bundle`**. **`enter_play_mode`** / **`exit_play_mode`** use **`SessionState`** so completion survives **domain reload** when toggling **Play Mode**; **`enter_play_mode`** waits for **`GridManager.isInitialized`**. Use **`unity_bridge_get`** to read a job by **`command_id`**.

**`get_compilation_status` response JSON:** **`UnityEngine.JsonUtility`** serializes the full **`AgentBridgeResponseFileDto`** shape; consumers should treat **`storage`:** **`compilation_status`** and **`compilation_status`** as authoritative. Other top-level fields (e.g. empty **`bundle`**-shaped defaults) may appear and should be ignored for this **`kind`** unless a future Editor change strips them.

**See also:** MCP **Bridge export sugar tools** (**`unity_export_cell_chunk`**, **`unity_export_sorting_debug`**) — [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) (**Bridge export sugar tools**). **`unity_export_cell_chunk`** payload **v2** exposes per-cell **HeightMap** height + **WaterMap**-authoritative water flags (**`cells[].waterMapBodyId`**, **`cells[].waterMapIsWater`**, distinct from **CityCell**-cached **`waterBodyId`** / **`waterBodyType`**) + top-level **`bodies[]`** inventory (**`bodyId`**, **`classification`**, **`surfaceHeight`**, **`cellCount`**) for terrain / water-body debug. Recipe: [`ia/skills/debug-geography-water/SKILL.md`](../skills/debug-geography-water/SKILL.md).

**Postgres registry (dev):** (1) **Bundle** row linking **Agent context** + optional **Sorting debug** paths: **`dev_repro_bundle`** via **`register-dev-repro.mjs`** / **`npm run db:register-repro`** — [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) (**Dev repro bundle registry**); glossary **Dev repro bundle**. (2) **Per-export** rows: tables `editor_export_*` (migrations **`0004_editor_export_tables.sql`**, **`0005_editor_export_document.sql`**, **`0006_editor_export_ui_inventory.sql`**), **`register-editor-export.mjs`** with **`--document-file`** (repo-relative or absolute), **`EditorPostgresExportRegistrar.TryPersistReport`** — [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) (**Editor export registry**); glossary **Editor export registry**. (3) **Agent bridge queue:** **`agent_bridge_job`** — **`0008_agent_bridge_job.sql`**. **`DATABASE_URL`**: process env, **EditorPrefs**, or repo **`.env.local`**. Optional **`node`** path in **EditorPrefs** / **`NODE_BINARY`** when **Unity**’s **PATH** omits **Volta**/**nvm**. Apply **`npm run db:migrate`** before expecting **`editor_export_*`** / **`agent_bridge_job`**. Optional **`backlog_issue_id`** in **EditorPrefs**. **Charter trace** for shipped registry work: [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md); **MCP** staging follow-ups: [`BACKLOG.md`](../../BACKLOG.md).

**Expected Unity Editor behavior:**

1. **Menu location — Reports:** Top bar **Territory Developer → Reports**:
   - **Export Agent Context**
   - **Export Sorting Debug (Markdown)**
   - **Export UI Inventory (JSON)** — [`UiInventoryReportsMenu.cs`](../../Assets/Scripts/Editor/UiInventoryReportsMenu.cs); **Edit Mode**; opens allowlisted **UI** scenes in sequence (**`ui-design-system.md`** baseline)
   - **Validate UI Theme asset** — [`UiThemeValidationMenu.cs`](../../Assets/Scripts/Editor/UiThemeValidationMenu.cs); **Edit Mode**; checks **`Assets/UI/Theme/DefaultUiTheme.asset`**
   - **Export Cell Chunk (Interchange)** — [`InterchangeJsonReportsMenu.cs`](../../Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs); **Play Mode** only
   - **Export World Snapshot (Dev Interchange)** — same; **Play Mode** only
   - **Export Geography Init Report (last-geography-init.json)** — [`GeographyInitReportMenu.cs`](../../Assets/Scripts/Editor/GeographyInitReportMenu.cs); **Play Mode** only
1b. **Menu location — UI:** **Territory Developer → UI → Scaffold UI Prefab Library v0** — [`UiPrefabLibraryScaffoldMenu.cs`](../../Assets/Scripts/Editor/UiPrefabLibraryScaffoldMenu.cs); **Edit Mode**; writes **`Assets/UI/Prefabs/UI_*.prefab`** (overwrites on re-run).
2. **When menus appear:** After the project compiles successfully, **Unity** discovers `[MenuItem]` on `AgentDiagnosticsReportsMenu` ([`AgentDiagnosticsReportsMenu.cs`](../../Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs)), `UiInventoryReportsMenu`, `UiThemeValidationMenu`, `InterchangeJsonReportsMenu`, and `UiPrefabLibraryScaffoldMenu`. If the submenu is missing, check **Console** for script compile errors in **Editor** scripts.
3. **Export Agent Context (JSON):** Must run in **Edit Mode** or **Play Mode** without throwing. If **`GridManager`** is absent or **`isInitialized`** is false, the JSON still validates; the `grid` block documents that state and may omit **cell** samples.
4. **Export Sorting Debug (Markdown):**
   - **Edit Mode:** Writes a **stub** file stating that full **Sorting order** breakdown requires **Play Mode** with an initialized **grid** — this is **expected**, not a failure.
   - **Play Mode:** Requires an initialized **`GridManager`** (`isInitialized`), a non-null **`TerrainManager`**, and valid **`GetCell`** data for sampled coordinates. Output lists **`TerrainManager`** constants (`TERRAIN_BASE_ORDER`, `DEPTH_MULTIPLIER`, `HEIGHT_MULTIPLIER`), per-**cell** computed orders, and a capped list of **`SpriteRenderer`** `sortingOrder` values on the **cell** `GameObject` tree.
5. **Grid reads:** Diagnostics code must use **`GridManager.GetCell(x, y)`** only for **cell** access — no new direct **`gridArray`** / **`cellArray`** use outside **`GridManager`** ([`ia/rules/invariants.md`](../rules/invariants.md)).

**Verification:** If menus do not appear or **Sorting** export does not match the expectations above, use [`BACKLOG.md`](../../BACKLOG.md) for the active bug row and attach **Console** output plus an **Agent context** / **Sorting debug** export when filing details.

---

## 11. Unity Test Framework — Edit Mode compute parity

**Purpose:** **Pure** **C#** **computational** helpers under **`Assets/Scripts/Utilities/Compute/`** are covered by **Unity Test Framework** **Edit Mode** tests without loading **Play Mode** scenes.

**Layout:**

| Item | Path / note |
|------|-------------|
| **Game** **asmdef** | **`TerritoryDeveloper.Game.asmdef`** under **`Assets/Scripts/`** — references **`Assembly-CSharp`** so **Utilities/Compute** types compile in a named assembly |
| **Edit Mode** **tests** | **`Assets/Tests/EditMode/`** — **`TerritoryDeveloper.EditModeTests.asmdef`** references **`TerritoryDeveloper.Game`** |
| **Golden** **parity** | **`ComputeLibParityTests`** loads **`tools/compute-lib/test/fixtures/world-to-grid.json`** — same vectors as **`tools/compute-lib`** **Node** tests (`tools/compute-lib/README.md`) |
| **Committed** **reports** | **`tools/reports/compute-utilities-inventory.md`**, **`tools/reports/compute-utilities-rng-derivation.md`** (expanded **2026-04-04**; **Editor** **geography** **export** **gitignored** under **`tools/reports/*.json`**) — exceptions in **`.gitignore`** |

**Scope note:** **C# compute utilities** include **`IsometricGridMath`**, **`UrbanGrowthRingMath`**, **`GridDistanceMath`**, **`PathfindingCostKernel`**, **Editor** **`last-geography-init.json`** (**`geography_init_report`**), **RNG** doc, **sampler** UTF — **glossary** **C# compute utilities**, **Computational MCP tools**; open follow-ups on [`BACKLOG.md`](../../BACKLOG.md) **§ Compute-lib program**.

---

## Decision Log

| Date | Change |
|------|--------|
| **2026-04-06** | **Territory Developer → Reports** (**Export Agent Context**, **Export Sorting Debug**) verified against §10 expectations (**Play Mode** vs **Edit Mode** stub); no platform caveat recorded. |
