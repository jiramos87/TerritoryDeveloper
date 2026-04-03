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

---

## 1. Purpose and scope

**Audience:** Contributors and IDE agents working in `Assets/Scripts/` and **Unity** scenes.

**Default:** Prefer this spec, [`.cursor/rules/project-overview.mdc`](../rules/project-overview.mdc), [`.cursor/rules/invariants.mdc`](../rules/invariants.mdc), [`.cursor/rules/coding-conventions.mdc`](../rules/coding-conventions.mdc), and **territory-ia** tools (`spec_section`, `glossary_discover`, etc.) before generic **Unity** web documentation.

**When to use external docs:** Version-specific APIs, bugs, or platform details not stated in this repo.

**Out of scope here:** Full **Unity** manual text; authoritative **cell** geometry, **HeightMap**, **road preparation family**, water, cliffs, and **Sorting order** formulas — see [`isometric-geography-system.md`](isometric-geography-system.md) and linked specs.

---

## 2. MonoBehaviour lifecycle

Managers and controllers are **scene** `MonoBehaviour` components; they are **not** constructed with `new`. See [`.cursor/rules/invariants.mdc`](../rules/invariants.mdc) — *IF creating a new manager → THEN MonoBehaviour scene component, never `new`*.

**`Awake`:** Use for self-setup and for resolving references that must exist before other scripts’ `Start` when order is guaranteed by **Unity** (same scene) or by **Script Execution Order** (see §6).

**`Start`:** Use for logic that depends on other components having finished `Awake`, when those components are in the same scene and no custom execution order is set. Some types defer `FindObjectOfType` to `Start` so sibling **Awake** chains can finish first — e.g. `WaterManager` assigns `gridManager` in `Start` if null ([`WaterManager.cs`](../../Assets/Scripts/Managers/GameManagers/WaterManager.cs)).

**Caching:** Resolve dependencies once during initialization; do not query the scene every frame (§7).

---

## 3. Inspector, SerializeField, and dependency resolution

**Target pattern (guardrail):** `[SerializeField] private` fields for dependencies, with `FindObjectOfType<T>()` **fallback** in `Awake` (or a helper called from `Awake`) when the **Inspector** reference is missing. See [`.cursor/rules/invariants.mdc`](../rules/invariants.mdc) guardrail: *IF adding a manager reference → THEN `[SerializeField] private` + `FindObjectOfType` fallback in `Awake`*.

**In-repo example (recommended shape):** `GameDebugInfoBuilder` keeps optional managers as `[SerializeField] private` and resolves them in `Awake` via `ResolveRefsIfNeeded()` using `FindObjectOfType` when null ([`GameDebugInfoBuilder.cs`](../../Assets/Scripts/Managers/GameManagers/GameDebugInfoBuilder.cs)).

**Legacy / mixed style:** Many managers still expose `public GridManager gridManager` (and similar) wired in the **Inspector**, with `FindObjectOfType` in `Awake` if null — e.g. `InterstateManager` ([`InterstateManager.cs`](../../Assets/Scripts/Managers/GameManagers/InterstateManager.cs)), `TerraformingService` ([`TerraformingService.cs`](../../Assets/Scripts/Managers/GameManagers/TerraformingService.cs)). Prefer **`SerializeField` private** for **new** fields so encapsulation matches **coding-conventions**; refactor public dependency fields only when touching the type for other reasons.

**Invariant:** Never call `FindObjectOfType` from `Update` or other per-frame paths — cache in `Awake` / `Start`. See [`.cursor/rules/invariants.mdc`](../rules/invariants.mdc).

---

## 4. Scenes, prefabs, and renaming

- **Missing references:** After moving or renaming scripts, **prefabs** and scenes can show “Missing (Mono Script)”. Reassign scripts or use **Unity**’s script mapping / metadata repair; agents should not assume GUID stability across branches without checking **YAML** / **meta** if merges broke links.
- **Renaming types:** Prefer updating `/// <summary>` and **XML docs** on the class when behavior changes ([`.cursor/rules/coding-conventions.mdc`](../rules/coding-conventions.mdc)).
- **Managers in scenes:** Core gameplay types live under `Assets/Scripts/Managers/` per [`.cursor/rules/project-overview.mdc`](../rules/project-overview.mdc); scene wiring is **Inspector**-driven — document new mandatory references on the relevant **Manager** when adding dependencies.

---

## 5. 2D sorting layers, sortingOrder, and Sorting order

**Unity** concepts:

- **Sorting Layer** — Named layer (e.g. Default, UI) used by **SpriteRenderer** / **TilemapRenderer** for coarse grouping.
- **`sortingOrder`** — Integer offset within a layer; higher draws in front.

**Project-specific Sorting order (isometric):** **Cell** visuals and terrain stacks use a **script-driven** formula and type offsets (**TERRAIN_BASE_ORDER**, **depthOrder**, **heightOrder**, **typeOffset**). That logic is defined in [`isometric-geography-system.md`](isometric-geography-system.md) section **7. Sorting Order System** — read that spec (or `spec_section` with `geo` + section `7`) instead of duplicating formulas here.

**Rule of thumb:** If a change affects **which** object draws above another on the **grid** for the same **cell** or neighbor relationship, consult geography §7 and [`glossary.md`](glossary.md) (**Sorting order**). If a change is **UI**-only, see [`ui-design-system.md`](ui-design-system.md).

---

## 6. Script Execution Order and initialization

**Unity** runs `Awake` / `OnEnable` / `Start` in a defined order; scripts on the same **GameObject** run in component order unless **Edit → Project Settings → Script Execution Order** overrides.

**Initialization races:** If **Manager A** needs **Manager B** in its `Awake`, ensure **B** runs first (execution order), move resolution to `Start`, or use explicit init methods called from a known coordinator (e.g. geography / game bootstrap). **BUG-16**-class issues stem from order assumptions without one of these fixes.

**Prefer** deterministic setup: **Inspector** wires first; `FindObjectOfType` fallback second; avoid lazy resolution in hot paths. `GameDebugInfoBuilder` re-calls `ResolveRefsIfNeeded()` before building strings so late-instantiated scenes still work — that pattern is for **debug** utilities, not per-frame gameplay.

---

## 7. Anti-patterns and project guardrails

| Do not | Do instead |
|--------|------------|
| New global **singletons** | **Inspector** + `FindObjectOfType` (see §3). **Exception:** `GameNotificationManager.Instance` is the documented single **singleton** ([`GameNotificationManager.cs`](../../Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs)); do **not** add new ones ([`.cursor/rules/invariants.mdc`](../rules/invariants.mdc)). |
| `FindObjectOfType` in `Update` / per-frame loops | Cache references in `Awake` / `Start` |
| Direct `gridArray` / `cellArray` access | `GridManager.GetCell(x, y)` ([`.cursor/rules/invariants.mdc`](../rules/invariants.mdc)) |
| New responsibilities on `GridManager` | Extract helpers ([`.cursor/rules/invariants.mdc`](../rules/invariants.mdc)) |
| Road placement bypassing preparation pipeline | **Road preparation family** ending in `PathTerraformPlan` + Phase-1 + `Apply` ([`.cursor/rules/invariants.mdc`](../rules/invariants.mdc)) |

---

## 8. ScriptableObject

There is **no** broad use of **`ScriptableObject`** in gameplay scripts under `Assets/Scripts/` in the current codebase snapshot. If a feature introduces **ScriptableObject** assets, follow **Unity** serialization rules and [`.cursor/rules/coding-conventions.mdc`](../rules/coding-conventions.mdc) for naming and **XML** documentation; prefer **Inspector**-friendly, immutable-ish config types.

---

## 9. Glossary alignment

Cross-check these terms in [`glossary.md`](glossary.md) and linked specs when writing or reviewing code:

- **Cell**, **HeightMap**, **GridManager**, **Sorting order**, **WaterMap**, **street** / **interstate**, **road stroke**, **AUTO systems**, **terraform** / **PathTerraformPlan**, **Map border**

For **Unity**-only vocabulary (**MonoBehaviour**, **Inspector**, **SerializeField**, **Prefab**), this document and **Unity** docs suffice; keep **game** terms aligned with the glossary.
