# AI agent prompt — FEAT-38 rivers

> **Status (2026-03-24):** **FEAT-38** is **completed** in `BACKLOG.md`. Use this file for **historical context** or small follow-ups. **Water shores + cliffs (lakes + rivers), waterfalls, water-cliff walls:** **[BUG-42](../../BACKLOG.md)** (in progress; merged **BUG-33** + **BUG-41**).

**Mode:** Follow the task the user gives in the same thread (analysis, planning, or implementation). For **analysis-only** runs, do **not** write production code until the user asks for an implementation plan or code.

## Goal

Implement or analyze **`BACKLOG.md` [FEAT-38](../../BACKLOG.md)** — **procedural static rivers** on **New Game**, **after** the lake/sea water pipeline and **before** interstate. Rivers are **fixed for the session**. The **authoritative spec** is **[`.cursor/specs/rivers.md`](rivers.md)** (§2 init order, §3 out-of-scope, §4 / §4.1 geometry, §7 decisions).

## Must read (in order)

1. **[`.cursor/specs/rivers.md`](rivers.md)** — Full rules: **dedicated river pass** from `GeographyManager` **after** `WaterManager.InitializeWaterMap()`, **before** interstate; **L** and **Wₙ**; merge **same as lakes**; static `WaterBodyType`; **forced** basin if no candidate; cardinal corridor + smooth orthogonal turns.
2. **`.cursor/specs/water-system-refactor.md`** — Phase **D** context; **§9** `WaterMap` / merge / save notes.
3. **`BACKLOG.md`** — FEAT-38 (completed); **BUG-42** (shores/cliffs/waterfalls — merged **BUG-33** + **BUG-41**).
4. **`ARCHITECTURE.md`** — Geography init, `WaterManager` dependencies.
5. **Code skim:** `GeographyManager.cs` (insert river pass), `WaterManager.cs`, `WaterMap.cs` (`MergeAdjacentBodiesWithSameSurface`, lake init order), `TerrainManager.cs` / `HeightMap` (carve), `RoadPrefabResolver` / slope roads for **cardinal** water; `Cell` / `CellData` / `GameSaveManager` for save.

## Product rules (from `rivers.md` — do not reinterpret)

- **Init** — Rivers run in a **dedicated method** invoked from **`GeographyManager`** **after** lakes/sea init, **before** interstate.
- **Path** — Discover routes on **generated** terrain when possible; if **no** candidate, **carve** a **forced** river basin (spirit: artificial-lake-style fallback).
- **Pipeline** — (1) analyze path / sectors → (2) carve/terraform (≤2 with relief **exceptions**) → (3) place **static** `WaterBodyType.River` + visuals.
- **Geometry** — **L** = sum of cells along **local channel axis** (rectangular steps add their along-flow depth); **max L** = **1.5 ×** relevant map dimension (square maps: width = height). **Future non-square:** cap vs **bounding-box extent** (see `rivers.md` §7).
- **Width** — **Wₙ ∈ {3,4,5}** (2 shores + 1–3 bed); **|Wₙ₊₁ − Wₙ| ≤ 1**; default **≥ 4** steps between width changes unless terrain forces; bed may **shift laterally**; avoid **overly uniform** carved basins.
- **Edges** — **N–S** or **E–W** opposite border pairs; high/low from **relief**; **lake or sea** as **logical exit** when applicable (procedural sea **not** a prerequisite for this pass).
- **Merge** — **Same as lakes** (`MergeAdjacentBodiesWithSameSurface`); **no** special “no-merge for rivers.” **No** fluid sim or type recomputation after merge.
- **Counts** — **1–3** rivers; **not** Inspector/UI in this pass.
- **Static** — No gameplay fluid sim; generation-time basin/spill analysis only where `rivers.md` allows.

## Hard constraints

- **Not** Navier–Stokes or per-tick volume; **not** gameplay spill/flood/drainage/tides.
- Reuse **`WaterMap` / `WaterBody`**; `WaterBodyType.River`; **FEAT-37c**-style save for water.
- **Inspector + `FindObjectOfType`**; **no new singletons**.

## Deliverables when asked for analysis / planning

1. **Risks** — save/load, merge + static typing, carve vs cliffs, **BUG-42** (shore/cliff polish), perf (path + carve + refresh).
2. **Work phases** — Parallel **basin-prone terrain** vs **river pass**; internal order **analyze → carve → place water**.
3. **Gaps** — Narrow **Draft** rows in `rivers.md` §7 (e.g. exception carve rules).
4. **Blockers** — Call out anything still ambiguous for coding.

## Deliverables when asked for implementation

- Follow `rivers.md` and project rules (`AGENTS.md`, `.cursor/rules/`).
- Prefer **small, reviewable** changes; **invalidate** caches where the project requires after terrain/water edits.
- Update **`rivers.md` §6 / §7** and **`BACKLOG.md`** FEAT-38 per project workflow when the feature is verified.

Do **not** paste huge diffs into chat unless requested; **file:symbol** references are enough for planning.
