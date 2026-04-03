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

---

## 1. Purpose and scope

**Audience:** Contributors and IDE agents working in `Assets/Scripts/` and **Unity** scenes.

**Default:** Prefer this spec, [`.cursor/rules/project-overview.mdc`](../rules/project-overview.mdc), [`.cursor/rules/invariants.mdc`](../rules/invariants.mdc), [`.cursor/rules/coding-conventions.mdc`](../rules/coding-conventions.mdc), and **territory-ia** tools (`spec_section`, `glossary_discover`, etc.) before generic **Unity** web documentation.

**When to use external docs:** Version-specific APIs, bugs, or platform details not stated in this repo.

**Out of scope here:** Full **Unity** manual text; authoritative **cell** geometry, **HeightMap**, **road preparation family**, water, cliffs, and **Sorting order** formulas — see [`isometric-geography-system.md`](isometric-geography-system.md) and linked specs.

**Territory-ia and exports:** This file is registered as **`unity-development-context`** (aliases **`unity`**, **`unityctx`**) for **`spec_outline`** / **`spec_section`**. Roadmap items in [`BACKLOG.md`](../../BACKLOG.md) (verify status there): **[TECH-18](../../BACKLOG.md)** may evolve MCP toward a **`unity_context_section`**-style retrieval path over a database; **[TECH-26](../../BACKLOG.md)** plans mechanical repo checks (e.g. **`FindObjectOfType`** in **`Update`**). **Editor** JSON/Markdown exports shipped under **[TECH-28](../../BACKLOG.md)** (completed — see [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md)); **expected menus, prerequisites, and outputs** are defined in **§10** below. Treat output paths and tool names for other issues as **planned** until the corresponding backlog item is completed. If menus or **Sorting** export fail in practice, track **[BUG-53](../../BACKLOG.md)**.

---

## 2. MonoBehaviour lifecycle

Managers and controllers are **scene** `MonoBehaviour` components; they are **not** constructed with `new`. See [`.cursor/rules/invariants.mdc`](../rules/invariants.mdc) — *IF creating a new manager → THEN MonoBehaviour scene component, never `new`*.

**`Awake`:** Use for self-setup and for resolving references that must exist before other scripts’ `Start` when order is guaranteed by **Unity** (same scene) or by **Script Execution Order** (see §6). Example: **`ZoneManager`** builds its zone prefab dictionary in **`Awake`** so lookups are ready before dependent **`Start`** logic ([`ZoneManager.cs`](../../Assets/Scripts/Managers/GameManagers/ZoneManager.cs)).

**`OnEnable`:** Runs when the component becomes enabled (after **`Awake`** on first activation, and again whenever the object is re-enabled). Use for event subscriptions or **`UI`** that must react to enable/disable. This gameplay codebase rarely overrides **`OnEnable`** on managers; prefer **`Awake`** / **`Start`** unless you need enable-cycle behavior.

**`Start`:** Use for logic that depends on other components having finished **`Awake`**, when those components are in the same scene and no custom execution order is set. Some types defer **`FindObjectOfType`** to **`Start`** so sibling **`Awake`** chains can finish first — e.g. **`WaterManager`** assigns **`gridManager`** in **`Start`** if null ([`WaterManager.cs`](../../Assets/Scripts/Managers/GameManagers/WaterManager.cs)). **`ZoneManager.Start`** double-checks prefab initialization after **`Awake`** ([`ZoneManager.cs`](../../Assets/Scripts/Managers/GameManagers/ZoneManager.cs)).

**Coroutines, `Invoke`, and delayed work:** **`GameBootstrap`** yields one frame then runs **New Game** / **Load** intent via **`StartCoroutine(ProcessStartIntent())`** ([`GameBootstrap.cs`](../../Assets/Scripts/Managers/GameManagers/GameBootstrap.cs)). **`UIManager`** uses **`Invoke`** and **`StartCoroutine`** for timed UI (e.g. tooltip hide) ([`UIManager.cs`](../../Assets/Scripts/Managers/GameManagers/UIManager.cs)). **`ForestManager`** defers visual updates with a coroutine ([`ForestManager.cs`](../../Assets/Scripts/Managers/GameManagers/ForestManager.cs)). **`GameNotificationManager`** sequences fades with coroutines ([`GameNotificationManager.cs`](../../Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs)).

**Caching:** Resolve dependencies once during initialization; do not query the scene every frame (§7).

**Where to look next:** Manager responsibilities and dependencies are summarized in [`managers-reference.md`](managers-reference.md) — link there instead of duplicating tables.

---

## 3. Inspector, SerializeField, and dependency resolution

**Target pattern (guardrail):** `[SerializeField] private` fields for dependencies, with `FindObjectOfType<T>()` **fallback** in `Awake` (or a helper called from `Awake`) when the **Inspector** reference is missing. See [`.cursor/rules/invariants.mdc`](../rules/invariants.mdc) guardrail: *IF adding a manager reference → THEN `[SerializeField] private` + `FindObjectOfType` fallback in `Awake`*.

**In-repo example (recommended shape):** `GameDebugInfoBuilder` keeps optional managers as `[SerializeField] private` and resolves them in `Awake` via `ResolveRefsIfNeeded()` using `FindObjectOfType` when null ([`GameDebugInfoBuilder.cs`](../../Assets/Scripts/Managers/GameManagers/GameDebugInfoBuilder.cs)). A search under `Assets/Scripts/Managers/` currently surfaces that type as the clearest full match for **`[SerializeField] private` manager refs + `FindObjectOfType`**; many other types still use public **Inspector** fields or private fields **without** `[SerializeField]` and resolve peers in **`Start`** (e.g. **`DemandManager`** — [`DemandManager.cs`](../../Assets/Scripts/Managers/GameManagers/DemandManager.cs)).

**Legacy / mixed style:** Many managers still expose `public GridManager gridManager` (and similar) wired in the **Inspector**, with `FindObjectOfType` in `Awake` if null — e.g. `InterstateManager` ([`InterstateManager.cs`](../../Assets/Scripts/Managers/GameManagers/InterstateManager.cs)), `TerraformingService` ([`TerraformingService.cs`](../../Assets/Scripts/Managers/GameManagers/TerraformingService.cs)). Prefer **`SerializeField` private** for **new** fields so encapsulation matches **coding-conventions**; refactor public dependency fields only when touching the type for other reasons.

**Third-party stacks:** Do **not** document **Addressables** (or similar) here unless usage appears under `Assets/Scripts/` — the current snapshot has **no** **Addressables** references there; re-check with search before adding package-specific guidance.

**Invariant:** Never call `FindObjectOfType` from `Update` or other per-frame paths — cache in `Awake` / `Start`. See [`.cursor/rules/invariants.mdc`](../rules/invariants.mdc).

---

## 4. Scenes, prefabs, and renaming

- **Missing references:** After moving or renaming scripts, **prefabs** and scenes can show “Missing (Mono Script)”. Reassign scripts or use **Unity**’s script mapping / metadata repair; agents should not assume GUID stability across branches without checking **YAML** / **meta** if merges broke links.
- **`.meta` and GUIDs:** **Unity** stores script and asset identity in `.meta` files. Duplicating a **prefab** or asset in the file system without letting the **Editor** regenerate **meta** can produce duplicate GUIDs or broken references — prefer duplicate inside the **Editor**, or run a deliberate GUID repair workflow.
- **Scene / prefab YAML:** Serialized scenes and **prefabs** are text **YAML**; merge conflicts can drop component blocks or scramble **fileID** references. Resolve conflicts in the **Editor** when possible; after manual **YAML** edits, open the asset in **Unity** to validate.
- **Renaming types:** Prefer updating `/// <summary>` and **XML docs** on the class when behavior changes ([`.cursor/rules/coding-conventions.mdc`](../rules/coding-conventions.mdc)).
- **Managers in scenes:** Core gameplay types live under `Assets/Scripts/Managers/` per [`.cursor/rules/project-overview.mdc`](../rules/project-overview.mdc); scene wiring is **Inspector**-driven — document new mandatory references on the relevant **Manager** when adding dependencies.

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

**Initialization races:** If **Manager A** needs **Manager B** in its `Awake`, ensure **B** runs first (execution order), move resolution to `Start`, or use explicit init methods called from a known coordinator. **[BUG-16](../../BACKLOG.md)** tracks **geography initialization** vs **`TimeManager`** ordering suspicion — see that issue for context, not for redefining gameplay rules here.

**Prefer** deterministic setup: **Inspector** wires first; `FindObjectOfType` fallback second; avoid lazy resolution in hot paths. `GameDebugInfoBuilder` re-calls `ResolveRefsIfNeeded()` before building strings so late-instantiated scenes still work — that pattern is for **debug** utilities, not per-frame gameplay.

**Related managers:** Per-type responsibilities remain in [`managers-reference.md`](managers-reference.md); avoid copying its tables into this document.

---

## 7. Anti-patterns and project guardrails

| Do not | Do instead |
|--------|------------|
| New global **singletons** | **Inspector** + `FindObjectOfType` (see §3). **Exception:** `GameNotificationManager.Instance` is the documented single **singleton** ([`GameNotificationManager.cs`](../../Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs)); do **not** add new ones ([`.cursor/rules/invariants.mdc`](../rules/invariants.mdc)). |
| `FindObjectOfType` in `Update` / per-frame loops | Cache references in `Awake` / `Start` |
| `GetComponent<T>()` / `GetComponentInChildren<T>()` in `Update` / per-frame loops | Cache the component in `Awake` / `Start` (or resolve once when the target instance is created); same spirit as the **`FindObjectOfType`** invariant |
| Direct `gridArray` / `cellArray` access | `GridManager.GetCell(x, y)` ([`.cursor/rules/invariants.mdc`](../rules/invariants.mdc)) |
| New responsibilities on `GridManager` | Extract helpers ([`.cursor/rules/invariants.mdc`](../rules/invariants.mdc)) |
| Road placement bypassing preparation pipeline | **Road preparation family** ending in `PathTerraformPlan` + Phase-1 + `Apply` ([`.cursor/rules/invariants.mdc`](../rules/invariants.mdc)) |

---

## 8. ScriptableObject

There is **no** broad use of **`ScriptableObject`** in gameplay scripts under `Assets/Scripts/` in the current codebase snapshot. If a feature introduces **ScriptableObject** assets, follow **Unity** serialization rules and [`.cursor/rules/coding-conventions.mdc`](../rules/coding-conventions.mdc) for naming and **XML** documentation; prefer **Inspector**-friendly, immutable-ish config types.

---

## 9. Glossary alignment

Cross-check these terms in [`glossary.md`](glossary.md) and linked specs when writing or reviewing code:

- **Cell**, **HeightMap**, **GridManager**, **Sorting order**, **WaterMap**, **Geography initialization**, **street** / **interstate**, **road stroke**, **AUTO systems**, **terraform** / **PathTerraformPlan**, **Map border**

For **Unity**-only vocabulary (**MonoBehaviour**, **Inspector**, **SerializeField**, **Prefab**), this document and **Unity** docs suffice; keep **game** terms aligned with the glossary.

---

## 10. Editor agent diagnostics (machine-readable exports)

**Purpose:** Give IDE agents reproducible, **glossary-aligned** snapshots of **Editor** / **Play Mode** context and optional **Sorting order** debugging material without hand-copying **Inspector** values. Player builds **must not** gain these menus (implementation lives under `Assets/Scripts/Editor/`).

**Information architecture (outputs):**

| Artifact | Relative path (repo root) | Role |
|----------|---------------------------|------|
| Agent context | `tools/reports/agent-context-{UTC-timestamp}.json` | `schema_version`, `exported_at_utc`, active scene, selection summary, bounded **grid** sample (**Cell**, **`HeightMap`**, **`WaterMap`** fields) |
| Sorting debug | `tools/reports/sorting-debug-{UTC-timestamp}.md` | Per-**cell** lines derived from **`TerrainManager`** public sorting APIs and sampled **`SpriteRenderer.sortingOrder`**; narrative ties to [`isometric-geography-system.md`](isometric-geography-system.md) **Sorting order** (do not duplicate formula authority here) |

Generated `*.json` / `*.md` under `tools/reports/` are **gitignored** by policy; the folder may contain **`.gitkeep`** only in VCS. Agents should reference exports in prompts with workspace paths (e.g. `@tools/reports/agent-context-….json`).

**Expected Unity Editor behavior:**

1. **Menu location:** Top bar **Territory Developer → Reports** with two sibling items:
   - **Export Agent Context**
   - **Export Sorting Debug (Markdown)**
2. **When menus appear:** After the project compiles successfully, **Unity** discovers `[MenuItem]` on `AgentDiagnosticsReportsMenu` ([`AgentDiagnosticsReportsMenu.cs`](../../Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs)). If the submenu is missing, check **Console** for script compile errors in **Editor** scripts.
3. **Export Agent Context (JSON):** Must run in **Edit Mode** or **Play Mode** without throwing. If **`GridManager`** is absent or **`isInitialized`** is false, the JSON still validates; the `grid` block documents that state and may omit **cell** samples.
4. **Export Sorting Debug (Markdown):**
   - **Edit Mode:** Writes a **stub** file stating that full **Sorting order** breakdown requires **Play Mode** with an initialized **grid** — this is **expected**, not a failure.
   - **Play Mode:** Requires an initialized **`GridManager`** (`isInitialized`), a non-null **`TerrainManager`**, and valid **`GetCell`** data for sampled coordinates. Output lists **`TerrainManager`** constants (`TERRAIN_BASE_ORDER`, `DEPTH_MULTIPLIER`, `HEIGHT_MULTIPLIER`), per-**cell** computed orders, and a capped list of **`SpriteRenderer`** `sortingOrder` values on the **cell** `GameObject` tree.
5. **Grid reads:** Diagnostics code must use **`GridManager.GetCell(x, y)`** only for **cell** access — no new direct **`gridArray`** / **`cellArray`** use outside **`GridManager`** ([`.cursor/rules/invariants.mdc`](../rules/invariants.mdc)).

**Active issue:** **[BUG-53](../../BACKLOG.md)** tracks reports that menus do not appear or that **Sorting** export does not match the expectations above.
