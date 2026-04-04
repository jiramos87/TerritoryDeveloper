# TECH-38 — Core computational modules (Unity + tools harnesses)

> **Issue:** [TECH-38](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-03
> **Last updated:** 2026-04-04

**Parent program:** [TECH-36](TECH-36.md) · **Depends on:** **TECH-37** **§ Completed** (**glossary** **territory-compute-lib (TECH-37)** — **`tools/compute-lib/`**, **`isometric_world_to_grid`**, **`verify`** harness) · **Feeds:** [TECH-39](TECH-39.md)

**Program phase:** **Phase B** of [TECH-36](TECH-36.md) **§7** — **behavior-preserving** **C#** extractions; **batchmode** / **golden** **JSON** hooks; **UrbanGrowthRingMath** (or equivalent) **multipolar**-ready; **no** second **pathfinding** authority. **Phase C** (**TECH-39**) consumes this output for **heavy** **MCP** tools.

**Spec pipeline program:** **TECH-60** **§ Completed** — **glossary** **territory-ia spec-pipeline program (TECH-60)** lists **TECH-38** as a **prerequisite** for **pure** **C#** surfaces, **`tools/reports/`** **JSON**, and **UTF** / **batchmode** hooks — [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md).

**territory-ia retrieval:** `router_for_task` / `glossary_discover` / `glossary_lookup` / `spec_section` — keep prose aligned with the umbrella domain table in [TECH-36](TECH-36.md) (§1 table + **§5.1** spec anchors).

| Domain (umbrella) | Canonical terms |
|-------------------|-----------------|
| World ↔ grid | **World ↔ Grid conversion** — **geo** §1.1, §1.3; **Node** **`isometric_world_to_grid`** = planar inverse only |
| Growth bias | **Urban centroid**, **urban growth rings**, **Multipolar urban growth** — **sim** §Rings; product **FEAT-47** / tuning **FEAT-43** |
| Path preview vs commit | **Pathfinding cost model** — **geo** §10; committed **road stroke** → **road preparation family** + **geo** §13 — **roads-system.md**; **wet run**, **bridge lip** — **geo** §14.5 |
| Water / height | **Surface height (S)**, **water body**, **shore band**, **H_bed** — **water-terrain-system** + **geo** §11–§12 |
| Init / interchange | **Geography initialization**, **`geography_init_params`** — **persistence-system**; **JSON program (TECH-21)** **§ Completed** — do not change on-disk **Save data** unless a **FEAT-**/**BUG-** requires it |
| Scoring | **Desirability** — **managers-reference**; **not** interchangeable with **A*** edge costs for **AUTO** unless a **FEAT-** says so ([TECH-36](TECH-36.md) **§3.2**) |

## 1. Summary

Extract and consolidate **pure** **computational** logic from **MonoBehaviour** **managers** into **`Assets/Scripts/Utilities/`** (and focused helpers), add **Edit Mode** / **Play Mode** tests where viable, and add **`tools/`** **batch** / **Node** scripts that consume **JSON** fixtures (**TECH-41** interchange DTOs + **TECH-40** schemas where checked in, **TECH-31**, **TECH-28** archived pattern) to validate **stochastic** **geography initialization** and **graph** algorithms without always launching full Play Mode. Prepare **urban growth rings** / **distance** **math** for **glossary** **Multipolar urban growth** (**FEAT-47**) without changing live **simulation** behavior until that issue executes.

**Authority (same as TECH-36 §1 / §3.4):** **C#** / **Unity** remain authoritative for **grid** truth, **HeightMap** ↔ **Cell.height**, **water map**, and legality of committed **road** work. **`tools/compute-lib/`** stays limited to **verified** **TypeScript** / **Zod** and **golden** vectors; **TECH-38** adds **C#** **pure** modules and **Unity** **batchmode** **JSON** exports **TECH-39** will call for **heavy** previews.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **Inventory** **manager**-embedded **math** / **graph** / **RNG** usage; rank by **TECH-15** / **TECH-16** **profiler** evidence.
2. Extract **at least three** **pure** units (C#) with **tests** or **golden** **JSON** checks: e.g. **World ↔ Grid**, **growth ring** band classification, **pathfinding** **cost** helper slice (read-only).
3. Document **stochastic** parameters for **geography initialization** (**procedural rivers**, **lake** **depression-fill**, **forest** placement seeds) in a single **English** **`docs/`** or **`tools/reports/`** appendix (link from **TECH-15** profiler doc if present).
4. **No** **FindObjectOfType** inside extracted **hot** paths; **no** **gridArray** / **cellArray** reads outside **`GridManager`**.
5. Align **C#** **isometric** **constants** with **`tools/compute-lib`** **golden** files from **TECH-37**.

### 2.2 Non-Goals

1. Implement **FEAT-46** UI or **FEAT-48** **water** **volume** gameplay (helpers only if behind tests and feature-flagged / unused in player).
2. Replace **UrbanCentroidService** **public** API for **multipolar** data — **FEAT-47** owns behavior flip; **TECH-38** only supplies **tested** **building blocks**.
3. Registering new **MCP** tools (**TECH-39**) or expanding **TECH-59** (**Editor** export registry staging) — coordinate **README** / **`docs/mcp-ia-server.md`** only when both lanes touch the same files ([TECH-36](TECH-36.md) **§5.3**).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Performance engineer | I want **ProfilerMarker** spans around extracted helpers to see win/loss. | At least one **marker** added + noted in **Decision Log**. |
| 2 | QA / agent | I want **deterministic** **replay** of **RNG**-driven **geography** slices. | **Seed** + parameter **JSON** documented; one **script** reproduces subset. |
| 3 | Sim developer | I want **ring** math parameterized by **centroid** list for future **FEAT-47**. | **Pure** API `RingIndexForCell(centroids, x, y)` (name illustrative) with tests. |

## 4. Current State

### 4.1 Domain behavior

- **Pathfinding cost model** / **A*** — **geo** §10; **road** legality — **roads-system** + **road preparation family**.
- **Urban growth rings** / **urban centroid** — **simulation-system** **§Rings**; tuning **FEAT-43**; **multipolar** direction **FEAT-47**.
- **HeightMap** / **water map** / **shore band** / **H_bed** — **invariants** + **water-terrain-system** / **geo** §11–§12.
- **Desirability** — **managers-reference** (**Demand**); **scoring** separate from **path** **commit** per [TECH-36](TECH-36.md).

### 4.2 Systems map

| Concentration today | Likely extraction target | Spec / invariant |
|---------------------|-------------------------|------------------|
| **`CoordinateConversionService`** (**GridManager**) | `Territory.Utilities.Compute.IsometricGridMath` + thin service wrapper | **geo** §1 |
| **`GridPathfinder`** | Keep class; extract **pure** **cost** / neighbor iterators if duplicated | **geo** §10 |
| **`ProceduralRiverGenerator`** | Seeded **profile** + step helpers | **geo** §12, **H_bed** monotonicity |
| **`UrbanCentroidService`** | **Ring** classification **pure** function | **sim** §Rings |
| **`TerrainManager`** / **`WaterManager`** | **Smoothing** / **basin** predicates (read-only analysis first) | **shore band**, **HeightMap** sync |
| **`DemandManager`** / **`CityStats`** | Read-only **desirability** **field** sampling for **tools** (no **AUTO** change) | **mgrs** |

## 5. Proposed Design

### 5.1 Target behavior (product)

**Behavior-preserving** refactors unless a **FEAT-**/**BUG-** explicitly changes rules. Any **visible** change → update **reference spec** + backlog item.

### 5.2 Extraction waves

**Wave A — Coordinates (parity with compute-lib)**  
Move **World ↔ Grid** **math** to **`IsometricGridMath`**; **`CoordinateConversionService`** delegates; **golden** parity test **C#** vs **JSON** from **TECH-37**.

**Wave B — Growth rings (multipolar-ready)**  
Extract **`UrbanGrowthRingMath`**: inputs = list of **(cx, cy, weight?)** **urban centroids**, **cell** **(x,y)**, ring radii table from constants; output = **ring index** or **distance** bucket. **Single-centroid** = list of length 1. Unit tests for **Chebyshev** / **Manhattan** choice **must** match **sim** §Rings text after review.

**Wave C — Pathfinding cost (read-only)**  
Extract **edge cost** / **neighbor enumeration** **pure** functions consumed by **`GridPathfinder`** — **no** second **path** **authority**; **InvalidateRoadCache** stays in **road** writers only.

**Wave D — Stochastic geography documentation + harness**  
Script: read **JSON** `{ seed, mapSize, lakeSettings, riverSettings, ... }` → run **subset** of **generation** in **batchmode** (or **precomputed** expected hashes) → write **`tools/reports/geography-init-snapshot.json`**. Align field names with **TECH-41** **`GeographyInitParams`** (and **TECH-39** / **compute-lib** **Zod**).

**Wave E — Terrain analysis helpers (optional)**  
**Slope** / **Moore** **open water** adjacency predicates for future **FEAT-48** — **no** **WaterManager** **state** mutation from helpers.

### 5.3 Testing strategy

| Layer | Tool |
|-------|------|
| **C#** **Edit Mode** | **Unity Test Framework** for **pure** **static** classes |
| **Batchmode** | **`-batchmode -quit`** **executeMethod** with **JSON** **stdin**/**path** arg |
| **Node** | **`tools/compute-lib`** or **`tools/`** script comparing to **golden** |

### 5.4 Desirability vs pathfinding (implementation rule)

- **`DesirabilityFieldSampler`** (illustrative name) may use **grid** distance and **pole** list; **must not** be used as **A*** **cost** unless **FEAT-** explicitly unifies — document in **XML** **`<remarks>`** (charter: [TECH-36](TECH-36.md) **§3.2**).

### 5.5 Cross-cutting invariants (implementers)

Do not violate [`.cursor/rules/invariants.mdc`](../rules/invariants.mdc). For this issue, pay special attention to:

- **`HeightMap[x,y]`** and **`Cell.height`** in sync on every write; **no** **direct** **`gridArray`** / **`cellArray`** access outside **`GridManager`** — use **`GetCell`**.
- **Road** modifications → **`InvalidateRoadCache()`**; **placing** a **road** → **road preparation family** (never **`ComputePathPlan`** alone).
- **Water** placement/removal → **`RefreshShoreTerrainAfterWaterUpdate`** where applicable; **rivers:** **`H_bed`** monotonically non-increasing toward exit (**geo** §12).
- **No new singletons**; **no** **`FindObjectOfType`** in **`Update`** or per-frame loops; **no** new responsibilities **inside** **`GridManager`** — extract helpers (**invariants** + [TECH-36](TECH-36.md) **§6**).

## 6. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-03 | **Multipolar** math extracted before **FEAT-47** behavior | Reduces risk; **FEAT-47** swaps **data** |
| 2026-04-03 | **Desirability** **scoring** ≠ **path** **cost** | User direction + **road preparation** invariant |
| 2026-04-04 | **TECH-37** landed **`tools/compute-lib/test/fixtures/world-to-grid.json`**, **`IsometricGridMath`** stub under **`Assets/Scripts/Utilities/Compute/`** | **Wave A** (§7.2): add **UTF** against fixture; optional dedup with **`GridManager`** / **`CoordinateConversionService`** |
| 2026-04-04 | Spec kickoff: align header, vocabulary table, §5.5 **invariants**, §7b **Test Contracts**, **TECH-39** handoff tool names, post-**TECH-37** preconditions | **TECH-36** umbrella parity + **PROJECT-SPEC-STRUCTURE** §7b |

## 7. Implementation Plan

### 7.0 Preconditions

- [ ] Use **TECH-37** **§ Completed** artifacts (no project spec file): **`tools/compute-lib/test/fixtures/world-to-grid.json`**, **`tools/compute-lib/src/isometric/worldToGrid.ts`** (**Zod** field names), **glossary** **territory-compute-lib (TECH-37)**, existing **`Assets/Scripts/Utilities/Compute/README.md`** + **`IsometricGridMath.cs`** stub.
- [ ] Pull latest **TECH-15** / **TECH-16** **profiler** **JSON** (if available) and list top 10 **C#** **methods** by time under **GeographyManager** / **TerrainManager** / **SimulationManager** / **Auto***.

### 7.1 Inventory phase (documentation-only PR acceptable)

- [ ] **`rg`** / IDE search: `System.Random`, `UnityEngine.Random`, noise, **Perlin**, **depression`, `Pathfinding`, `A*`, `Heuristic`, `desirability`, `centroid`, `ring` across **`Assets/Scripts/Managers/`** and **`Utilities/`**.
- [ ] Produce **`tools/reports/TECH-38-compute-inventory.md`** (English): table **File**, **Symbol**, **Spec term**, **Pure?** (Y/N/Maybe), **Risk**.
- [ ] Review with **sim** §Rings + **geo** §10; mark **blockers** (e.g. hidden **MonoBehaviour** **state**).

### 7.2 Wave A — **IsometricGridMath** (parity with **compute-lib**)

- [ ] **Extend** existing **`Assets/Scripts/Utilities/Compute/IsometricGridMath.cs`** (stub from **TECH-37**): keep **`/// <summary>`** on the type referencing **geo** §1.1 / §1.3; implement **full** **World ↔ Grid** parity with **`CoordinateConversionService`** / **`GridManager`** expectations.
- [ ] Move or delegate **math** from **`CoordinateConversionService`** into **`IsometricGridMath`**; keep **public** call-site API stable (**obsolete** + forward if needed).
- [ ] Add **`IsometricGridMathTests.cs`** (**UTF**) loading **`tools/compute-lib/test/fixtures/world-to-grid.json`** (or embedded **const** vectors) — same cases as **Node** **golden** tests.
- [ ] **ProfilerMarker** on **hot** entry points (optional) if profiling shows regressions vs baseline.

### 7.3 Wave B — **UrbanGrowthRingMath**

- [ ] Add **`UrbanGrowthRingMath.cs`**: `GetRingIndex(int x, int y, IReadOnlyList<Centroid> poles, RingTable table)`.
- [ ] **Centroid** struct: **grid** **x,y** + optional **weight** for future **connurbation** blending (**FEAT-47**).
- [ ] **RingTable**: radii thresholds matching current **`UrbanCentroidService`** constants (single source — move constants here).
- [ ] Tests: **single** **pole** at origin; **two** **poles** — **cell** nearer **pole** A vs B; **boundary** **ties** documented.
- [ ] **Refactor** **`UrbanCentroidService`** to call **pure** **API** **without** changing **observable** **AUTO** output (snapshot test or **Play Mode** **capture** before/after).

### 7.4 Wave C — **Pathfinding** **cost** **slices**

- [ ] Identify duplicated **legality** / **cost** **branches** inside **`GridPathfinder`** vs **RoadManager** **preview**.
- [ ] Extract **`PathfindingCostKernel.cs`** (read-only): **MoveCost**(**cell**, **neighbor**, **context** struct).
- [ ] **Context** struct holds **references** **ids** or **enum** **flags** — **no** **`GridManager`** **inside** **kernel**; caller passes **elevations** / **road** **mask** as **spans** or **readonly** **interfaces** (design in PR).
- [ ] **Regression:** run **existing** **road** / **AUTO** tests; manual **street** draw on **slope** / **wet run** **cells**.

### 7.5 Wave D — **Stochastic** **geography** harness

- [ ] Reuse or extend shipped **`Territory.Persistence.GeographyInitParamsDto`** / **`GeographyInitParamsLoader`** (**TECH-41**) and JSON Schema [`docs/schemas/geography-init-params.v1.schema.json`](../../docs/schemas/geography-init-params.v1.schema.json) — do not fork a second **DTO** shape for the harness.
- [ ] **Editor** menu or **`batchmode`** entry: **`TerritoryTools.ExportGeographyInitReport`** writing **`tools/reports/last-geography-init.json`** (gitignored) + optional committed **golden** for **CI** **smoke**.
- [ ] Document **every** **RNG** **derivation** (master seed → **lake** shuffle → **river** **noise**) in **English** in **`tools/reports/TECH-38-rng-derivation.md`**.
- [ ] Wire **optional** **Node** script **`tools/scripts/validate-geography-init.mjs`** that checks **schema** and **monotonicity** **flags** (e.g. **H_bed** along **river** **polyline** if exported).

### 7.6 Wave E — **Terrain** / **water** **predicates** (optional)

- [ ] **`WaterAdjacency.cs`**: `IsMooreAdjacentToOpenWater(x,y, readonly WaterMapView view)` — **read-only** **interface** implemented by **WaterManager** **adapter** **in** **manager** **assembly** only.
- [ ] **`BasinVolumeMath.cs`**: **placeholder** **static** **methods** **documented** for **FEAT-48** (**volume** ↔ **S**) — **throw** **`NotImplementedException`** or **compile** **only** in **Editor** **test** **asm** until **FEAT-48**.

### 7.7 **Desirability** **sampling** (non-AUTO behavior change)

- [ ] Add **`DesirabilityFieldSampler`** in **`Utilities/`** or **`DemandManager`** **partial** **friend** **helper** **file** — **read-only** **access** to **arrays** **passed** **in**.
- [ ] Unit test: synthetic **grid** **scores** → **top-k** **cells** **match** **brute** **force**.
- [ ] **Do not** wire **AUTO** **zoning** to **new** **API** without **FEAT-** (separate **issue**).

### 7.8 Performance and safety gates

- [ ] After each **wave**, **Unity** **Profiler** **deep** **profile** **New** **Game** and **one** **sim** **tick**; compare **ms** to **baseline** **commit**; **regression** **> 5%** **requires** **Decision** **Log** **entry**.
- [ ] **Run** **TECH-26**-class **grep** (manual): **no** **`FindObjectOfType`** in **new** **Utilities/** **files**.

### 7.9 Handoff to **TECH-39**

- [ ] List **stable** **operations** ready for **MCP**, aligned with [`BACKLOG.md`](../../BACKLOG.md) **TECH-39** **Notes**: **`isometric_world_to_grid`** (**TECH-37** / **compute-lib**), **`growth_ring_classify`**, **`grid_distance`**, **`pathfinding_cost_preview`**, **`geography_init_params_validate`**, **`desirability_top_cells`** (and any **Decision Log** renames).
- [ ] For each, specify **input** **JSON** **shape**, **authority** (**C#** **batchmode** vs **compute-lib** **only**), and **NOT_AVAILABLE** / stub policy until **Wave** **D** / **batchmode** **JSON** exists.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| **§8** inventory markdown | Review | **`tools/reports/TECH-38-compute-inventory.md`** merged | English; links **spec** terms |
| **≥ 3** **pure** modules + tests / **golden** | **Unity** **UTF** + optional **Node** | Test assemblies + **`tools/compute-lib`** fixtures / **`npm test`** under **`tools/compute-lib`** | **Wave A** ↔ **`world-to-grid.json`** |
| **RNG** derivation doc | Review | **`tools/reports/TECH-38-rng-derivation.md`** | **Geography initialization** seed chain |
| **Regression** / **AUTO** parity (**UrbanCentroidService**) | **Play Mode** or snapshot | Document in **Decision Log** | **> 5%** **Profiler** regression → **Decision Log** per §7.8 |
| **IA** / **glossary** / **MCP** index sources touched | **Node** | **`npm run validate:all`** (repo root) | Optional; after doc edits per **project-implementation-validation** |

## 8. Acceptance Criteria

- [ ] **Inventory** markdown merged.
- [ ] **≥ 3** **pure** **modules** with **tests** or **golden** **JSON** **validation**.
- [ ] **UrbanCentroidService** uses **UrbanGrowthRingMath** with **no** **intentional** **AUTO** **output** change (or **Decision** **Log** **records** **approved** **change** **+** **spec** **update**).
- [ ] **RNG** **derivation** doc merged.
- [ ] **No** **new** **singletons**; **invariants** **respected**.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

**N/A for game-logic definitions in this spec** — **TECH-38** is **tooling** / **extraction** scope. Product rules for **Multipolar urban growth**, **geography** authoring UI, and **water body** **volume** / **surface height (S)** remain under **FEAT-47**, **FEAT-46**, **FEAT-48** and [TECH-36](TECH-36.md) **§3** (use **Open Questions** on those issues when they get project specs). If an extraction **would** change observable **AUTO** or **Save** behavior, record the conflict in **Decision Log** or split a **BUG-**/**FEAT-** before shipping.
