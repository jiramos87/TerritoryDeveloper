# Rivers — definitions, metaknowledge, and progress (FEAT-38)

> **Backlog:** [FEAT-38](../../BACKLOG.md) **completed** (2026-03-24) · **Follow-up:** [BUG-41](../../BACKLOG.md) (river shore prefabs + cliffs) · **Parent context:** [`water-system-refactor.md`](water-system-refactor.md) (Phase D — flow & coast; data-driven first)  
> **Language:** All additions to this file and to code must be **English**.

## 1. Purpose

Track **vocabulary**, **design decisions**, **open questions**, and **implementation progress** for **procedural rivers** during geography generation: **static** water bodies laid along **continuous basin paths** on the `HeightMap`, found via **pathfinding** that respects the generated terrain (not forced unnatural basins when natural candidates exist), integrated with **`WaterMap` / `WaterBody`** and **FEAT-37c** save/load.

This file is the **living spec** for the rivers workstream. Update it when decisions are locked or scope changes.

## 2. Scope (this development)

- **New Game / geography init only** — After terrain and the **lake/sea** water pipeline, run a **dedicated river pass** (see **Initialization order** below), then downstream systems (e.g. interstate). The river pipeline: (1) finds viable routes on **flat** (or suitable) sectors using **pathfinding** over the height field, (2) **carves** or **terraforms** **shallow** basins along those connected sectors (see **Carve depth** and **Geometry along the corridor**), (3) places **static** river water with **cardinal** rules and visuals.
- **Initialization order** — Call a **dedicated river generation entry point** from **`GeographyManager`** **after** `WaterManager.InitializeWaterMap()` (lakes/sea) and **before** interstate placement. Same relative order as in `GeographyManager.InitializeGeography()` for terrain → water → **rivers** → interstate → forests (when applicable).
- **Parallel workstream — terrain “basin-prone” generation** — Improvements to **initial terrain generation** (e.g. `TerrainManager` / `HeightMap` noise, smoothing, macro shapes) to make **natural elongated basins** more likely are **in scope** and should proceed **in parallel** with the river pipeline (same milestone / feature; **not** a blocking “phase 0” that must finish before river pathfinding starts). Coordinate so both land in a coherent New Game experience.
- **No runtime fluid physics** — Rivers do not move, merge, or change volume **during play**; they behave like **elongated lakes** along a path. **Interstate-like** analogy: **continuous border-to-border** (or border-to-lake / border-to-sea when applicable) route — but the **route is discovered** on terrain when possible, not imposed without pathfinding.
- **Lake / sea as logical exit** — A river may **terminate at a lake** or **at the sea** as a **logical exit** when those bodies exist. **Procedural sea generation** is **not** required in this pass; framing is reserved for when sea exists on the map.
- **Height-only “physics”** — **Generation-time** **spill / basin** analysis for **candidates** (same family as lake depression-fill) is **in scope**. **Gameplay** spill, floods, and time-stepped discharge are **out of scope** (see §3).
- **Data model** — Per-cell body ids, `WaterBody` with `SurfaceHeight`, `WaterBodyType.River` on `Cell` / `CellData` where applicable.
- **Merging** — `MergeAdjacentBodiesWithSameSurface` merges adjacent bodies at the **same surface height** when allowed: **River** with **River** only; **Lake** with **Lake**; **Sea** with **Sea**; **Lake** with **Sea** (same as pre–FEAT-38). **River never merges with Lake or Sea** (preserves distinct types and avoids modifying lake/sea registration).
- **Existing bodies** — The river pass **does not** add cells to or change **surface height** of bodies created by the lake/sea pipeline. **Logical exit** at a lake or sea ends the river in **River** cells **adjacent** to that body’s perimeter (river does not occupy lake/sea cells).
- **Static classification** — The **`WaterBodyType` assigned at generation** (per cell / body creation) is **fixed for the session** and is **not** recomputed after merge. **Do not** simulate fluid mixing, average surface height after union, or dynamic hydraulics.
- **Forced river** — If **no** viable river candidate is found on generated terrain, **carve** a river basin and place a **forced** river, analogous to **artificial lake** fallback in spirit (must still respect global caps and ordering constraints).
- **River count and tuning (code defaults)** — **1–3** rivers per New Game; **not** exposed in Inspector or Unity UI in this pass.
- **Visuals** — **Cardinal slope water** prefabs (**N / S / E / W**), parallel in spirit to **SlopeRoads**; **cliff** segments may use **cascade / waterfall** prefabs on the appropriate faces. Banks/shores: **BUG-33** and related specs.

## 3. Out of scope (explicit — not now)

Do **not** implement in this FEAT-38 pass unless explicitly moved into scope later:

- **Dynamic** water during play: moving masses, changing levels, **time-stepped** discharge.
- **Hydraulic reinterpretation on merge** — No average surface height, no re-tagging `WaterBodyType` after `WaterMap` merge, no fluid-volume redistribution (merge is **registration-only** adjacency union as today).
- Full **hydrological** simulation during play (infiltration, evaporation, seasonal flow).
- Gameplay/economy **flow rates**.
- **Procedural sea** as a prerequisite for river exit (sea-edge logical exit is allowed when sea **already** exists on the map).

**Clarification — “Spill”:** **Out of scope:** simulated spill/flood **during the game**. **In scope** for generation: **spill-style / basin candidate** analysis on `HeightMap` (rim height, feasible downhill paths) — static analysis only.

## 4. Core concepts (to refine into code)

Definitions are **working** until marked **Locked** in §7.

| Concept | Working definition |
|--------|---------------------|
| **River pathfinding** | Search for a **valid water route** over **generated** terrain (following **natural** patterns rather than terraform-first when candidates exist). Prefer routes through **flat** or **low-gradient** sectors where an **elongated basin** can exist; avoid overly **uniform** rectangular basins when carving (unnatural). |
| **First approach (ordered steps)** | **(1)** Evaluate possible paths on **flat** terrain (or connected flat sectors) that can host a **valid basin** generation. **(2)** **Carve** or **terraform** **shallow** basins along those sectors (**Carve depth**), with extra care at **relief transitions**. **(3)** Create **static** rivers on these prepared basins under the agreed rules (body ids, surface height, visuals). |
| **River corridor** | A **continuous** **cardinal** path from a **map-border entry** toward an **exit** (opposite edge **or** **lake / sea as logical exit**). **Longitudinal** bed/surface rules: **§4.4** (non-increasing from entry toward exit — river never “climbs”). **N/S/E/W** steps for slope water prefabs. **Turns** along the path **add** to total along-channel length **L**; prefer **smooth** orthogonal paths (concatenated segments); **same river** may consist of **horizontal** runs and **vertical** runs on different parts of the path. |
| **Entry / exit (map edge)** | Valid border pairs: **N–S** (or **S–N**) **or** **E–W** (or **W–E**) — **which axis** and **direction** (which side is higher) **depend on map relief**. Entry is **high**; exit on the **opposite** edge is **lower**, **unless** the route **terminates at a lake or sea** (logical exit). |
| **Exit at lake or sea** | If the path reaches a **lake** or **sea**, the river may **terminate** there as the **logical exit** (endpoint). |
| **Carve depth** | Default: **shallow** carve / terraform **up to 2** height steps along the corridor. **Exceptions** allowed where relief changes, cliffs, or waterfalls require a different treatment (e.g. **cascade** prefab instead of deep carve, or documented special-case rules — specify in implementation notes). |
| **“Water source” (minimized)** | Not a gameplay spring. Meaningful endpoints: **edge entry**, **edge exit** **or** **lake/sea termination**. |
| **Flow direction** | **Discrete** next cell along the corridor — **cardinal** for segments. |
| **Channel** | Cells assigned to a **river** `WaterBody`; may include slope tiles and cliff **cascade** segments. |

### 4.1 Geometry along the corridor

| Concept | Definition |
|--------|------------|
| **Along-channel length L** | **L** is the **sum of cells traversed along the local axis of the channel** (each cell step along the path counts). If a step uses a **rectangular** footprint (depth **k** along flow), add **k** to **L** (not “one logical strip” unless **k = 1**). **Maximum** **L** = **1.5 ×** the relevant map extent for the traced axis: on **square** maps, **width** and **height** are equal — use either. **Future (non-square maps):** cap **L** relative to the **bounding-box extent** of the river path along the dominant layout axis (document precisely when non-square maps ship). |
| **Transverse strips** | Along the path, each step places a **cross-section** perpendicular to **local** flow. Strips may be **unicellular** along flow or **rectangular**; avoid **overly invariant** (perfectly regular) basins. **Orthogonal grid** algorithms may produce **curved-looking** paths from concatenated cardinal segments. |
| **Width Wₙ** | **Bed (lecho)** width **1–3** cells; **total** cross-stream width **Wₙ = bed + 2** (one shore strip per side) → **Wₙ ∈ {3, 4, 5}**. **|Wₙ₊₁ − Wₙ| ≤ 1** between consecutive cross-sections along the path. **Minimum run length** before changing width (unless terrain forces otherwise): default **≥ 4** steps (tunable). |
| **River bed (lecho)** | In each cross-section, the **contiguous lowest** bed cell(s); **both** lateral shores are **strictly higher** than the bed. The bed may **shift laterally** (meander in plan) between steps so the full basin is not a rigid rectangle. |
| **Turns** | **Increase L**. Avoid **sharp** bends; approximate smooth meanders with **concatenated cardinal** sections; direction may switch from **horizontal** to **vertical** composition within the **same** river. |

### 4.2 Border margin and edge placement (implementation)

- **`RiverBorderMargin`** (code default **2**): pathfinding and forced centerlines only consider **entry/exit** on map edges **inside** a band `[margin, size − 1 − margin]` along the active edge (north/south or east/west), so rivers do not **hug** the **lateral** borders (no long “moat” along east/west for **N–S** flow, or along north/south for **E–W** flow).
- **N–S** routes: centerline may use **north** row only at the **start** and **south** row only at the **end**; **east** and **west** columns are forbidden for the channel footprint except at the **first** / **last** segment where the path meets those edges for **entry** / **exit** (see `ProceduralRiverGenerator`).
- **E–W** routes: symmetric — **west** / **east** only at start/end; **north** / **south** rows are not used as a continuous lateral border run.
- **Tiny maps:** if `width` / `height` cannot satisfy margin + a valid band, the river pass may **skip** procedural rivers (`CanPlaceRiversWithMargin`).

### 4.3 Transverse coherence and surface segments (implementation — Locked)

These rules prevent floating banks, black holes, and mismatched water heights within one cross-section. **`ProceduralRiverGenerator`** applies them at generation time.

| Rule | Statement |
|------|-----------|
| **Symmetric banks** | For each cross-section index along the centerline, the **two** dry shore cells (one per side of the bed) use the **same** terrain height: `H_bank = H_bed + 1` (one step above the shared bed floor `H_bed`), unless `H_bed` is already at max height. |
| **Single bed height per section** | All **bed** (water) cells in that cross-section share one carved **`H_bed`**, derived from the **minimum** pre-carve terrain height among those bed cells and the usual shallow carve (≤2 steps). |
| **Water surface along the path** | Logical **surface** height for a section is `surface = H_bed + 1` (clamped). **Consecutive** sections with the **same** `surface` share one **`WaterBody`** id; when `surface` **changes** (relief step along the river), a **new** river body is created so adjacent merge still respects same-surface rules. |
| **Interior vs edge water** | All river water cells in a section use the **same** body surface for that segment; `WaterManager.PlaceWater` uses `GetSurfaceHeightAt` per cell — bodies must therefore not mix incompatible surfaces within one section (enforced by the single `H_bed` per section). |
| **Terrain refresh** | Land tiles must not be placed on registered water cells during `TerrainManager.UpdateTileElevation`; water visuals are refreshed via **`WaterManager.PlaceWater`** only. |

**Lateral cliffs:** Rim / cliff stacks belong on **land** cells outside the water strip; they should appear only when height rules in `TerrainManager` place cliffs toward lower neighbors, not as a substitute for symmetric bank height inside the corridor.

### 4.4 Longitudinal monotonicity (entry → exit — Locked)

Macro flow follows the **centerline order** from **entry** (first index at the map edge) toward **exit**. Cardinal **turns** are allowed; height constraints apply **along that ordered path**, not as a single fixed compass direction.

| Rule | Statement |
|------|-----------|
| **Entry as highest reference** | Let `surface(i) = H_bed(i) + 1` (clamped) for cross-section index `i` along the centerline. The **first** section with a valid bed fixes the **maximum** logical surface along that river: for all `j` with valid bed, `surface(j) ≤ surface(0)` (equivalently `H_bed(j) ≤ H_bed(0)`). |
| **Non-increasing downstream** | For each consecutive pair `i → i+1` with valid carved sections, **`H_bed(i+1) ≤ H_bed(i)`** so the river **never** gains elevation along its path. Logical water surface is therefore **non-increasing** along the centerline: `surface(i+1) ≤ surface(i)`. |
| **No climbing hills** | After per-section shallow carve (§4.3), **`ProceduralRiverGenerator`** applies **`H_bed(i) = min(candidate(i), H_bed(i−1))`** forward from entry so terrain under the channel cannot rise when moving toward the exit. |
| **Orphans** | Isolated water in depressions **not** connected to the chosen centerline path is **out of scope** for this pass unless explicitly generated as a separate body elsewhere. |

## 5. Code and doc touchpoints (read before designing)

| Area | Role |
|------|------|
| `GeographyManager.InitializeGeography()` | Order: terrain → water (lakes/sea) → **dedicated river pass** → **interstate** → forests (if enabled) → desirability, sorting, etc. |
| `TerrainManager` / `HeightMap` | **Basin-prone** terrain tweaks (**parallel** track); **carve** / terraform for river channels; slope + cascade prefabs; align with `RoadPrefabResolver` / **SlopeRoads** patterns for cardinals. |
| `WaterManager` / `WaterMap` / `WaterBody` | Register rivers **after** lake init; merge same-surface **within** the same `WaterBody` classification (see §2). |
| `Cell` / `CellData` / `GameSaveManager` | **FEAT-37c** — river cells **round-trip** via **`WaterMapData` + cell water fields** (preferred); document if regen is ever used. |
| `WaterBodyType` | **`River`** — static elongated body along a prepared corridor; **not** re-derived after merge. |

Related specs: [`isometric-geography-system.md`](isometric-geography-system.md), [`bugs/cliff-water-shore-sorting.md`](bugs/cliff-water-shore-sorting.md), `ARCHITECTURE.md` (Terrain / Geography).

## 6. Progress checklist

- [x] Vocabulary locked (§4, §4.1) + **Locked** rows in §7
- [ ] **Parallel:** terrain “basin-prone” tuning + river pipeline coordinated (same feature; not sequential gate)
- [x] `GeographyManager`: river pass **after** lakes, **before** interstate
- [x] Pathfinding + geometry (**L**, **Wₙ**, strips, forced fallback) + carve (≤2 with exceptions) + river placement sequence in code (verify in Unity)
- [x] **Merge** behavior: river isolated from lake/sea; static `WaterBodyType` after generation
- [ ] Save/load (`WaterMapData` / `CellData`) verified for rivers in Unity
- [ ] Cardinal **slope water** + **cascade** prefabs (assets + placement) — uses existing water tiles until dedicated slope-water art ships
- [x] `BACKLOG.md` **FEAT-38** — completed (2026-03-24)
- [ ] **BUG-41** — river shore prefabs + cliff stacks on river corridors (in progress)

## 7. Decisions and open questions

_Add rows as the team resolves them. Mark **Status:** Draft | Locked | Superseded._

| Topic | Decision / question | Status |
|-------|---------------------|--------|
| Static vs dynamic | Rivers **static** after generation; no dynamic water movement in play. | Locked |
| Pathfinding | **Required** when searching natural candidates; **forced** basin if none. | Locked |
| First approach | (1) Path / sector analysis → (2) carve/terraform shallow basin (≤2 with **exceptions** at relief) → (3) place river bodies. | Locked |
| Terrain generation | **Basin-prone** terrain changes run **in parallel** with the river pipeline (same milestone; not a blocking prerequisite phase). | Locked |
| Carve depth | **Up to 2** logical steps by default; **exceptions** at relief transitions / cliffs / cascades (document per implementation). | Locked |
| Lake / sea as exit | Route may **terminate at a lake** or **sea** as **logical exit**; procedural sea **not** required in this pass. | Locked |
| Valid map edges | **N–S** or **E–W** **opposite** pairs only; **orientation** (high entry vs low exit) from **relief**. | Locked |
| Border footprint | **Margin band** on edges; no **lateral** border “moats” (N–S: no full-length E/W hug; E–W: no full-length N/S hug); entry/exit only in the allowed strips — see **§4.2**. | Locked |
| Transverse coherence | **§4.3** — symmetric bank height per section, single `H_bed` per section, surface segments / split bodies when `surface` changes; no grass-on-water in `UpdateTileElevation`. | Locked |
| Longitudinal monotonicity | **§4.4** — `H_bed` and logical `surface` non-increasing along centerline from entry; entry has highest surface along the river; `min(candidate, previous H_bed)` clamp in `ProceduralRiverGenerator`. | Locked |
| Merging | **Superseded** — see **Merge (rivers vs lakes)**. | Superseded |
| Merge (rivers vs lakes) | **Superseded** — see **Merge by classification**. | Superseded |
| Merge by classification | `MergeAdjacentBodiesWithSameSurface` merges when **surface heights match** and merge is allowed: same classification, or **Lake with Sea** (FEAT-37 compatibility). **River** merges **only** with **River** — never with Lake or Sea. | Locked |
| River pass vs existing water | River generation **does not** add cells to or alter surfaces of lake/sea bodies from the prior init. Logical lake/sea exit = river channel ends in **River** cells next to that body (see §2). | Locked |
| Static `WaterBodyType` | Type set at **generation**; **not** updated after merge; no fluid-volume simulation on merge. | Locked |
| Spill wording | Generation-time spill/basin analysis **in scope**; gameplay spill/flood **out of scope**. | Locked |
| Visuals — slopes | Water-on-slope prefabs **N, S, E, W** (SlopeRoads-like). | Locked |
| Visuals — cliffs | **Cascade / waterfall** prefabs on appropriate faces where needed. | Locked |
| Initialization order | **Dedicated river pass** from `GeographyManager` **after** `InitializeWaterMap`, **before** interstate. | Locked |
| River count | **1–3** per New Game; **not** Inspector/UI in this pass. | Locked |
| Along-channel length L | Sum of cells along **local channel axis**; **max L** = **1.5 ×** map dimension on relevant axis (square: width or height); **future non-square:** tie-break via **bounding-box extent** (spec detail when maps are non-square). | Locked |
| Cross-section width Wₙ | **Bed width 1–3** cells; **Wₙ = bed + 2** shores → **{3, 4, 5}** total; **\|Wₙ₊₁ − Wₙ\| ≤ 1**; default **≥ 4** steps between width changes unless terrain forces. | Locked |
| Bed / lecho + meander | Shores higher than bed; bed may **shift laterally** between steps. | Locked |
| Forced river | If no candidate, **carve** basin + **forced** river (analogous to artificial lake fallback). | Locked |
| Exception cases for carve | Exact rules when carve depth exceeds two steps vs cascade-only (corner cases) | Draft |
| Save format | Prefer full **`WaterMapData` + `CellData`** round-trip per **FEAT-37c**. | Locked |

## 8. Future (post–MVP procedural rivers)

**FEAT-38** is closed; near-term **visual / prefab** polish for river shores and cliffs is **[BUG-41](../../BACKLOG.md)**.

Animated flow, flow-rate scalars, gameplay spill/flood, tides, full drainage networks, **procedural sea** as standard neighbor, optional **Inspector** tuning for river count/length, non-square map **L** cap refinement.

---

**Last updated:** 2026-03-24 — FEAT-38 completed; **BUG-41** follow-up; §4.4 longitudinal monotonicity; §4.3 transverse coherence; §4.2 border margin; merge by classification; **L** / **Wₙ**; init order rivers before interstate.
