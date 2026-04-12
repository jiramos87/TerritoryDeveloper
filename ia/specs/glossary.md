---
purpose: "Reference spec for Glossary — Territory Developer."
audience: agent
loaded_by: router
slices_via: glossary_lookup
---
# Glossary — Territory Developer

> Quick-reference for **domain concepts** (game logic + system behavior). Class names, methods, and backlog ID rules live in technical specs and `BACKLOG.md` — see `ia/specs/managers-reference.md`, `roads-system.md`, etc.
> Canonical detail is always in the linked spec — defer to the spec when they differ.

> **Spec abbreviations:** geo = `isometric-geography-system.md`, roads = `roads-system.md`,
> water = `water-terrain-system.md`, sim = `simulation-system.md`, persist = `persistence-system.md`,
> mgrs = `managers-reference.md` — **§Zones**, **§Demand**, **§World**, **§Notifications**; **geo §14.5** = road stroke, lip, grass, Chebyshev, etc.; **sim §Rings** = centroid + growth rings; ui = `ui-design-system.md`; unity-dev = `unity-development-context.md`; `ARCHITECTURE.md` = layer map and init order (no § numbers).

## Index (quick skim)

| Block | Keywords (search this doc for…) |
|-------|----------------------------------|
| **Grid** | Cell, HeightMap, sorting, tile/world, Moore, cardinal, grass |
| **Height** | range, sea level, Δh, generation |
| **Terrain** | slopes, cliffs, shore, rim, bay, suppression, cut-through |
| **Water** | body, map, open vs shore, S/V, junction, Pass A/B, brink, cascade |
| **Rivers** | H_bed, bank, width, spacing, Chebyshev |
| **World gen** | geography init |
| **Roads** | validation, stroke, street/interstate, bridge, wet run, terraform |
| **Pathfinding** | A*, costs, diagonal steps |
| **Zones** | RCI, density, pivot, footprint, undeveloped light |
| **Simulation** | tick, AUTO, budget, centroid, rings |
| **City** | demand, tax, desirability, happiness, pollution, forest, regional, utility, notification, monthly maintenance |
| **Persistence** | save, CellData, water map data, visual restore, load order |
| **Prefabs** | land/water slopes, sorting formula, type offsets |
| **Documentation** | reference spec, project spec, interchange JSON, geography_init_params, scenario_descriptor_v1, City metrics history, Agent test mode batch, IDE agent bridge |
| **Planned (non-authoritative)** | backlog-backed future terms — [§ Planned terminology](#planned-domain-terms) |

## Grid & Coordinates

| Term | Definition | Spec |
|------|-----------|------|
| **Cell** | Smallest addressable geographic unit on the map — one isometric tile of land, water, or development. `MonoBehaviour` on each grid tile `GameObject`; holds `height`, `terrainSlopeType`, `cellType`, water body id, zone, and building reference. | geo §1, §2, §11.2 |
| **HeightMap** | The terrain elevation field over the whole map; defines hills, basins, and where water sits. `int[,]` in `[MIN_HEIGHT, MAX_HEIGHT]`; must stay in sync with `Cell.height` on every write. | geo §2 |
| **Sorting order** | What draws in front of what on screen so hills, water, roads, and buildings stack believably. Integer `sortingOrder` from depth, height, and per-type offsets (formula in spec). | geo §7 |
| **Tile dimensions** | How wide and tall one map cell is in world space for the diamond isometric layout. `tileWidth` = 1.0, `tileHeight` = 0.5 world units. | geo §1.1 |
| **Direction convention** | How compass directions map to grid steps and on-screen tilt. N/S/E/W use fixed `(Δx, Δy)`; `+x` reads as North (up-right), `+y` as West (up-left). | geo §1.2 |
| **Height offset** | Vertical lift of the whole cell in world space so higher terrain reads above lower terrain. `(h - 1) * 0.25` added to world Y per height level above base. | geo §1.1 |
| **World ↔ Grid conversion** | Turning mouse/world positions into cell indices and back for picking and placement. Diamond projection and inverse use `tileWidth` / `tileHeight` / `heightOffset` and `round` for inverse. **territory-ia** **`isometric_world_to_grid`** covers the **planar** inverse only — not height-aware picking. | geo §1.1, §1.3 |
| **Moore neighborhood** | The eight tiles touching a cell (cardinals + diagonals); used for slopes, shores, and many adjacency rules. Same as Moore-adjacent cells in geography and water specs. | geo §2.4.1 |
| **Cardinal neighbor** | The four tiles sharing an edge (N/S/E/W steps only) — not diagonals. Height difference limits, water–water cascades, and many road rules use cardinals only. | geo §2.3, §5.6.2 |
| **Grass cell** | Default developable land — empty of **street**/**interstate** cells, typically open for zoning, forests, and placement tools. Manual pathfinding treats grass plus **street**/**interstate** cells as walkable; AUTO adds undeveloped light zoning per mode. | geo §14.5, geo §13.9 |

## Height System

| Term | Definition | Spec |
|------|-----------|------|
| **Height range** | How low and high land can be in the simulation. `MIN_HEIGHT`–`MAX_HEIGHT` (0–5); higher steps cliffs and sorting, not infinite scale. | geo §2.1 |
| **Sea level** | The baseline height band where open sea and low water bodies live. `SEA_LEVEL = 0`; registered bodies still use per-body surface height `S`. | geo §2.1 |
| **Height constraint** | Rule that neighbors cannot jump more than one step except where designed (e.g. lake bowls). Cardinal `\|Δh\| ≤ 1` for “normal” land; larger drops use cliffs and special cases. | geo §2.3 |
| **Height generation** | How the initial landform is created before play. 40×40 designer template; larger maps extend with blended Perlin noise; lakes and rivers stamp afterward. | geo §2.2 |

## Terrain & Slopes

| Term | Definition | Spec |
|------|-----------|------|
| **Slope type** | Which way the ground tilts on a cell — flat, ramp, diagonal wedge, or concave corner. `TerrainSlopeType` enum from 8-neighbor height compares (land vs water-shore rules differ). | geo §3–§4 |
| **Slope categories** | Grouping of slope outcomes: flat plateau, cardinal ramp, diagonal wedge, corner-up valley. Drives grass vs ramp prefabs and which tiles allow roads. | geo §3.3 |
| **Cliff** | A vertical drop between two cells — reads as a rock wall along the **drop boundary** (the lower cell’s side of the step). S/E-facing stacked meshes when `\|Δh\| ≥ 1` on cardinals; N/W faces not rendered (camera). Not the same as **map border** (play-area boundary). | geo §5.7 |
| **Shore band** | The ring of dry land hugging water where special shore art applies and heights are clamped to the water surface. Moore neighbors of water with `height ≤ min(S)` among adjacent water. | geo §2.4.1 |
| **Surface-height gate** | Whether a land tile is allowed to use water-shore visuals vs normal land + cliffs. Tests `h ≤ V + MAX` with `V = max(MIN_HEIGHT, S − 1)`; rim above cap uses ordinary terrain. | geo §4.2 |
| **Rim** | Higher dry ground just above the shore band — looks like normal hills toward water, not a beach ramp. Uses slopes + cliff stacks; fails the shore-art gate. | geo §14.1 |
| **Bay** | An inner-corner shore pattern where water wraps around land (a cove or notch). Chosen from neighbor water patterns (perpendicular cardinals, rectangle outer corner, etc.). | geo §5.9 |
| **Cliff face visibility** | Which cliff meshes the player actually sees. Only **south** and **east** cliff stacks instantiated; `Cell.cliffFaces` may still record N/W for systems like hydrology. | geo §5.7 |
| **Cliff suppression** | Hiding a cliff mesh when a shore ramp already shows the transition. One-step suppression toward water/water-shore on eligible shore cells; rim keeps a segment; `\|Δh\| ≥ 2` still stacks. Toward **off-grid** void on **S**/**E**, **water-shore** primary cells skip duplicate brown **cliff** (same face-ownership rule). | geo §5.6.1, §5.7 |
| **Cut-through corridor** | A trench carved through high terrain so a path can run flat — reads as a notch in a hill. Terraform flattens to `baseHeight`; cliffs on sides where neighbors stay higher. | geo §5.10 |

## Water

| Term | Definition | Spec |
|------|-----------|------|
| **Water body** | A connected region of water sharing one surface height and identity (lake, river reach, sea). `WaterBody`: id, `SurfaceHeight`, cells; drives rendering and shore affiliation. | geo §11.2 |
| **Water map** | Which body id each cell belongs to, if any. `WaterMap`: `int[,]` ids + body list; `0` = dry; used for placement, save, and junction logic. | geo §11.2 |
| **Open water** | A cell that is registered as water in `WaterMap` — the main water surface tile for a body. Contrasts with dry **water-shore** art on land; sorting uses surface height. | geo §11.2, water |
| **Water-shore (land)** | A **dry** cell painted with shore transition prefabs toward water — beach/ramp art, not the water tile itself. Subject to surface-height gate and shore refresh; distinct from **rim** above the cap. | geo §11.2, §4.2, water |
| **Surface height (S)** | The logical water level of a body — “how high” the water is for rules and sorting. Open water cells use `Cell.height = S` for visuals; bed may differ underneath. | geo §11.1 |
| **Visual reference (V)** | Index used for shore-art eligibility vs land height: `V = max(MIN_HEIGHT, S − 1)`. The gate compares land `h` to `V + MAX`, not raw `S` alone. | geo §2.4.1, §4.2 |
| **Water body kind** | Classification of a body — **lake**, **river**, or **sea** — controlling merge rules, junction exclusions, and carve behavior (e.g. river–river vs lake–lake contact). | geo §11.2, §12.3 |
| **Depression-fill** | Lakes forming in natural lows on the terrain — water fills until it would spill out. Algorithm floods from minima to spill height; bodies merged and validated per lake rules. | geo §11.3 |
| **Spill height** | The overflow elevation that caps a depression fill — like the rim of a basin. Sets max surface during depression-fill before accept/reject and merging. | geo §11.3 |
| **Junction** | Where two water surfaces at different heights meet on an edge — creates drops, merges, and special shore topology. Cardinal contact with `S_high > S_low` (subject to lake exclusion rules). | geo §11.8 |
| **Pass A (bed alignment)** | Normalizing underwater terrain when high water meets low water so beds line up before placement. Lowers upper-side bed toward lower neighbors; sweeps until stable; ids unchanged. | geo §11.7 |
| **Pass B (junction merge)** | Reassigning cells so the lower surface “wins” at a multi-body contact. Moves dry/shore on lower plane to lower body; may absorb bank cells; contact-bed reassignment. | geo §11.7 |
| **Brink** | Special dry land roles next to a river–river surface step — upper vs lower pool sides of a drop. **UpperBrink** / **LowerBrink** drive cascade shore passes and cliff stacks. | geo §11.8 |
| **Lake exclusion** | Rule that lake–lake surface steps do not get water–water cascades or junction merge passes on that edge. Pass A/B skip; sea is not treated as lake for this rule. | geo §11.7 |
| **Shore refresh** | Recomputing shore grass/ramp/cliff art after water changes. Updates Moore (and sometimes wider) rings around new water so land matches the new shoreline. | geo §11.6 |
| **Cascade** | A waterfall between two water tiles at different surface heights — no dry shore between. Cardinal step; S/E cascade prefabs; segment count from `S_high − S_low`. | geo §5.6.2 |
| **Corner promotion** | Raising inner corner river bed cells so the dry bank stays continuous around bends. Bed cells with two perpendicular neighbors at `H_bed + 1` promoted before water assignment. | geo §12.5 |

## Rivers

| Term | Definition | Spec |
|------|-----------|------|
| **River** | A fixed water channel carved after lakes — flows across the map toward a border or sink. Static after init; cardinal path; `H_bed` non-increasing toward exit; banks and width rules in spec. | geo §12 |
| **River bed (H_bed)** | The floor elevation of the river channel under the water surface — may vary within a segment but follows monotonic rules toward the exit. Distinct from **surface** `≈ H_bed + 1` and from dry **bank** cells. | geo §11.1, §12.4 |
| **River bank** | The dry strip beside the channel — one step above the bed so the channel reads sunken. Symmetric rule `H_bank = H_bed + 1` when geometry allows. | geo §12.4 |
| **River width** | How wide the wet channel and its shoulders are. Bed 1–3 cells; total with shores in `{3,4,5}`; width steps limited between segments. | geo §12.4 |
| **Forced river** | A fallback when no natural river path qualifies — the generator still guarantees a channel. Carves a basin and places a river by constraint relaxation. | geo §12.4 |
| **River spacing** | Keeping separate river corridors from overlapping or crowding entries. Prior corridors dilated (Chebyshev); same-border entries spaced apart. | geo §12.4 |
| **Chebyshev distance** | Grid metric `max(|Δx|,|Δy|)` — “king moves” on an 8-way grid. Used to dilate river corridors and measure spacing between river entries without diagonal double-counting. | geo §12.4, geo §14.5 |

## World generation

| Term | Definition | Spec |
|------|-----------|------|
| **Geography initialization** | First-time map build on **New Game** — ordered pipeline from heightmap through water, rivers, interstate, forests, desirability, and sorting before play. `GeographyManager` orchestrates; order must stay consistent with save/load assumptions. Optional **Editor** diagnostic JSON **`geography_init_report`** (gitignored). | `ARCHITECTURE.md`, geo §12.1, mgrs, [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) |

## Roads & Bridges

| Term | Definition | Spec |
|------|-----------|------|
| **Terraform plan** | The authoritative description of how terrain under a proposed road changes (or does not). `PathTerraformPlan`: per-cell actions, heights, `postTerraformSlopeType`; `Apply` / `Revert`. | geo §8 |
| **Road validation pipeline** | The required gate before any **street** or **interstate** placement is committed — same checks for manual **street** draw, **interstate**, and AUTO **streets**. Preparation (`TryPrepareRoadPlacementPlan`, longest-prefix, optional locked deck-span) → Phase-1 heights → `Apply` → prefab resolve — not raw `ComputePathPlan` alone. | geo §13.1, roads |
| **Road stroke** | The ordered path of cells for a road placement attempt — player drag or AUTO route — before filtering, truncation, and plan build. Same logical “stroke” drives preview and commit when valid. | geo §14.5, roads |
| **Phase-1 validation** | Height and neighbor consistency check on a terraform plan **before** terrain writes are committed (`TryValidatePhase1Heights`). Fails fast if the plan would break `\|Δh\|` or edge rules. | geo §13.1 |
| **Interstate** | Limited-access highway linking the city grid to the **map border** — long straight preference, full-path validation, **cut-through forbidden**. Distinct from ordinary streets and bridges. | geo §13.5, §13.6, mgrs |
| **Street (ordinary road)** | Player or AUTO **non-interstate** road: local network using the same validation family as manual draw (prefix, deck-span, terraform). Contrasts with border **interstate**. | geo §14.5, geo §13.1, mgrs |
| **Street or interstate** | Umbrella when the same rule applies to **street (ordinary road)** and **interstate**: both must pass the **road validation pipeline** and commit via **`PathTerraformPlan`**. Prefer **street** or **interstate** alone when the distinction matters (e.g. **map border** endpoints, **cut-through** bans). | geo §13.1, roads |
| **Map border** | The outer boundary of the playable grid (`x`/`y` min/max). **South**/**east** brown **cliff** stacks toward **off-grid** void use **`MIN_HEIGHT`** as the foot so the mesh fills to the terrain base; interstate endpoints and some water rules also reference the border. Do not use informal “map edge” for this — see [REFERENCE-SPEC-STRUCTURE.md](REFERENCE-SPEC-STRUCTURE.md) deprecated → canonical table. Not a generic **cell** edge or **Moore**/**cardinal neighbor** face unless that neighbor lies on the border. | geo §14.5, §5.7, §13.5 |
| **Water-slope tile** | Coastal **land** cell using water-slope shore prefabs (`IsWaterSlopeCell`). Behaves like impassable or high-cost terrain for normal street routing; bridges use separate rules. | geo §5.8, §10, roads |
| **Road cache invalidation** | After **any** change to road topology, cached road queries must be rebuilt so pathfinding and neighbors see the new network. Project invariant: call invalidate after modifications. | roads, `ia/rules/invariants.md` |
| **Cut-through** | Flattening terrain along a **street**/**interstate** path when slopes are too steep to “ride.” `Flatten` to `baseHeight`; rejected when `maxHeight - baseHeight > 1` (**interstate** forbids entirely). | geo §8.3, §13.6 |
| **baseHeight (terraform)** | Reference elevation a cut-through plan writes along the path so the road sits flat through high ground. Chosen from path context; too deep a cut vs surrounding max height fails validation. | geo §8.3, geo §14.5 |
| **Scale-with-slopes** | Letting a road follow natural ramps when every step is gentle. No height writes; plan records `TerraformAction.None` and slope type per cell. | geo §8.2 |
| **Deck span** | A bridge segment over water — straight, no corners on water, one deck height. Axis-aligned; uniform `waterBridgeDeckDisplayHeight` across the wet run. | geo §13.4 |
| **Bridge lip** | Last firm **dry** land cell at the water’s edge where a deck span begins — the anchor for deck height and locked chord geometry. | geo §13.4, geo §14.5 |
| **Wet run** | Contiguous **water and/or water-slope** cells along a stroke that a bridge crosses in one straight segment. Truncation rules keep wet runs intact for bridge / interstate validation (see **roads** spec). | geo §13.4, geo §14.5, roads |
| **Locked chord** | A fixed straight line from dry bank through water to dry land used for manual bridge preview/commit. Cardinal chord from lip through wet to far dry at matching bridge height. | geo §13.4 |
| **Longest valid prefix** | Truncating a stroke to the longest part that still passes all rules. Manual/AUTO use when tail would invalidate; silent when stroke starts on blocked slope (see roads spec). | geo §13.2, roads |
| **Land slope eligibility** | Which ground tiles may carry a road stroke. **Flat** and **cardinal ramps** only; pure diagonals and corner-up land slopes disallowed for strokes and A* walkability. | geo §13.10, roads |
| **Resolver rules** | Prefab choice invariants for topology and approach. Elbow degree, exit alignment, terraform-over-live-slope, hill avoidance, interstate straight preference, bridge approach orthogonality (A–F). | geo §13.7, roads |
| **Road reservation** | Cells AUTO zoning must leave empty so future **street** alignment stays possible. Axial strips from `GetRoadExtensionCells` / `GetRoadAxialCorridorCells` excluded from auto-zone each tick. | geo §13.9, sim |

## Pathfinding

| Term | Definition | Spec |
|------|-----------|------|
| **Pathfinding cost model** | How the game scores candidate street routes — prefers flat, penalizes slopes and water-shore. A* costs: flat cheap, slopes expensive, water-slope very high, `\|Δh\|>1` impossible. | geo §10 |
| **A* search** | Best-first shortest-path search on the road grid using the cost table in the geography spec; explores cheaper cells first until the goal is reached. Separate entry points for manual vs AUTO simulation walkability. | geo §10, roads |
| **Interstate cost model** | Stronger penalties for kinks and detours on highway generation. Scales slope costs, adds turn/zigzag/away penalties and straight bonus (see table). | geo §10 |
| **Diagonal step expansion** | Roads move in cardinal steps only, even if the sketch is diagonal. Planner splits diagonal moves into two orthogonal steps for prefab compatibility. | geo §8.4 |

## Zones & Buildings

| Term | Definition | Spec |
|------|-----------|------|
| **RCI** | The three land-use families — housing, shops/offices, and factories — that drive demand and tax base. Residential / Commercial / Industrial; each has its own zone tiles and building sets. | mgrs §Zones |
| **Zone** | A parcel designated for a type of development before a building appears. `Zone` component on cell: type, density tier, level, building ref; lifecycle in managers spec. | mgrs §Zones |
| **Zone density** | How intense development is allowed on a zoned tile — light, medium, heavy. Selects building size/tier; **undeveloped light** interacts with AUTO roads (see below). | mgrs §Zones |
| **Pivot cell** | The anchor tile of a multi-cell building for save, sort, and demolish. Other footprint cells point at the pivot’s building instance. | mgrs §Zones |
| **Building footprint** | All grid cells covered by one building (1×1 or multi-cell). Sorting, save data, bulldoze, and growth operate on the footprint as a unit tied to the pivot. | mgrs §Zones |
| **Undeveloped light zoning** | A zoned tile still empty of a building, at light density — treated as passable for AUTO **street** planning only. Walkability via `AutoSimulationRoadRules` + simulation pathfinder; medium/heavy and built tiles differ. | mgrs §Zones, geo §13.9 |
| **Building** | The visible structure on a zoned or service tile — RCI houses/shops/factories or **utility** plants. Spawned by zone/growth/resource rules; may be 1×1 or multi-cell; level tracks growth stage. | mgrs §Zones, mgrs §World |

## Simulation & Growth

| Term | Definition | Spec |
|------|-----------|------|
| **Simulation tick** | One automatic city update — roads extend, zones spread, utilities plan when time advances. Driven by `TimeManager` → `SimulationManager.ProcessSimulationTick()`. | sim |
| **AUTO systems** | The three autopilots that grow the city each tick — streets, zoning, utilities. `AutoRoadBuilder`, `AutoZoningManager`, `AutoResourcePlanner` in fixed order after centroid/budget. | sim |
| **Tick execution order** | Strict sequence inside a tick so later systems see fresh data. Budget valid → centroid recompute → roads → zoning → resource planner (see simulation spec list). | sim |
| **Growth budget** | Per-category cap on how much AUTO may spend or place each tick. Prevents runaway sprawl; total pool comes from `GrowthBudgetManager` using projected net monthly cash flow (**tax base** income minus **monthly maintenance**) when positive, otherwise treasury (`CityStats`). | sim, mgrs §Demand |
| **Urban centroid** | A statistical “center of mass” of development used to bias growth rings. `UrbanCentroidService` computes centroid and ring metrics for road/zoning targeting. | sim, sim §Rings |
| **Urban growth rings** | Distance bands from the urban centroid — AUTO uses them to weight where roads and zones expand (typically denser near core). Recalculated each tick before AUTO systems run. | sim §Rings |
| **Urbanization proposal** | **OBSOLETE** — legacy expansion proposal UI and manager; **never re-enable** (see **invariants**). Not part of `UrbanCentroidService` / ring AUTO. | sim |

## City systems

| Term | Definition | Spec |
|------|-----------|------|
| **Demand (R / C / I)** | How much the city “wants” each zone type to grow this cycle — pressure from jobs, population, forests, **per-sector tax** pressure, and a **happiness**-target multiplier. Drives the demand bar and AUTO zoning targets. Refreshed each in-game day after **happiness** is updated. | mgrs §Demand |
| **Tax base** | Economic capacity tied to zoned development and population that **tax rates** apply to — income flows through `EconomyManager` / `CityStats`, while rates feed back via the **highest** of the three **tax** rates into **happiness** (above a comfort band) and via **per-sector** scaling into each R/C/I **demand** channel. | mgrs §Demand |
| **Monthly maintenance** | Recurring **city** expense on the first **simulation** calendar day of each month: **street** upkeep from `CityStats.roadCount` (road cells) and **utility building** upkeep from registered **power plant** count; collected after **tax base** income for that day. Debits use `EconomyManager.SpendMoney`; insufficient funds skip the charge with a **game notification**. HUD net **money** hint uses projected tax minus projected maintenance. | mgrs §Demand |
| **Desirability** | How attractive a tile is for growth based on nearby terrain (water, forest, etc.), computed after geography init. Biases AUTO roads/zoning toward nicer locations. | mgrs §Demand, `ARCHITECTURE.md` |
| **Forest (coverage)** | Tree cover on land — **sparse**, **medium**, or **dense** — affecting demand and player forest tools. | mgrs §World |
| **Regional map** | The broader region with **neighboring cities**; context for regional systems and UI. | mgrs §World |
| **Utility building** | Non-RCI service structure (water, power, etc.), often multi-cell; placed manually or by AUTO resource planning. | mgrs §World |
| **Happiness** | City-wide citizen satisfaction score, normalized 0–100. Recalculated each in-game **day** (and when **tax** rates change from the UI) from weighted factors: employment rate, **highest** of the three **tax** rates vs a comfort band, **service coverage**, **forest** bonus, development base, and **pollution** penalty. Converges smoothly toward a target (lerp). Feeds back into **demand (R / C / I)** via a multiplier derived from that **target** on the same day. | mgrs §Demand |
| **Pollution** | City-wide environmental degradation score. Sources: **industrial** **buildings** (heavy > medium > light), polluting **utility buildings** (power plants — nuclear = medium, fossil = high). Sinks: **forest** coverage (trees absorb pollution), future parks. Base pollution may later be affected by geographic and climatic factors. Influences **happiness** as a negative factor. | mgrs §World |
| **Game notification** | Player-facing message (money, errors, hints) shown as a toast or alert. Only the notification singleton may enqueue UI messages. | mgrs §Notifications |

## Persistence

| Term | Definition | Spec |
|------|-----------|------|
| **Save data** | The on-disk snapshot of the whole city state. `GameSaveData`: `List<CellData>` plus `WaterMapData` from `WaterMap.GetSerializableData()`. | persist §Save |
| **CellData** | Serializable DTO mirroring runtime `Cell` fields for persistence — height, prefabs, `sortingOrder`, zone, water ids, building refs. Written and read by save/load; not a scene `MonoBehaviour`. | persist §Save, geo §11.2 |
| **Water map data** | Serialized water bodies and per-cell ids for reload. `WaterMapData` nested in `WaterMap.cs`; v2 format with legacy fallback when absent. | persist §Save, geo §11.5 |
| **Legacy save** | Older save file without `waterMapData` — load uses fallback path to reconstruct water from height/legacy flags; still supported. | persist §Load pipeline, persist §Visual restore details |
| **Visual restore** | Reloading exactly what the player saw — no full regen of slopes/sort from scratch. Load applies saved prefabs and `sortingOrder`; building post-pass; geography spec §7.4 details. | persist §Visual restore details, geo §7.4 |
| **Load pipeline order** | Mandatory restore sequence so references resolve. Heightmap → water map (or legacy) → grid cells → sync shore/body ids — do not reorder. | persist §Load pipeline |

## Prefabs & Visual Layer

| Term | Definition | Spec |
|------|-----------|------|
| **Land slope prefabs** | The twelve terrain ramp meshes for hills — four cardinals, four diagonals, four corner-ups. Named `*SlopePrefab` per facing; used by terrain builder. | geo §6.1 |
| **Water slope prefabs** | Shore ramps where land meets water — same topology set as land but water-tinted. `*SlopeWaterPrefab` / `*UpslopeWaterPrefab` variants. | geo §6.2 |
| **Slope variant naming** | How building sprites pick a ramp-compatible mesh. Pattern `{flatPrefab}_{slopeCode}Slope`; `GetSlopeVariant` resolves by constructed name. | geo §6.4 |
| **Infrastructure prefabs** | Shared world props — sea tile, cliff wall pieces (S/E visible; N/W reserved), bay corners. Listed in prefab inventory table. | geo §6.3 |
| **Sorting formula** | How a cell’s draw order is computed from position and height. `TERRAIN_BASE_ORDER + depthOrder + heightOrder + typeOffset` with `depthOrder = -(x+y)*DEPTH_MULTIPLIER`. | geo §7.1 |
| **Sorting components** | The four additive pieces: **TERRAIN_BASE_ORDER** (base), **depthOrder** (isometric depth), **heightOrder** (elevation), **typeOffset** (layer kind: terrain, road, building, etc.). Together they enforce global draw order rules. | geo §7.1, §7.2 |
| **DEPTH_MULTIPLIER** | How strongly “farther on the map” pushes sprites back. Set so depth beats max height contribution (100 vs 10×max height). | geo §7.1, §7.3 |
| **HEIGHT_MULTIPLIER** | Per-level boost so taller tiles sort above neighbors at the same depth. Used inside `heightOrder`. | geo §7.1 |
| **Type offsets** | Extra bias per object kind so **street**/**interstate** tiles sit above grass, buildings above roads, etc. Terrain 0, slopes +1, road +5, utility +8, building +10, effect +30. | geo §7.2 |

## Documentation

| Term | Definition | Spec |
|------|-----------|------|
| **Reference spec** | Permanent Markdown under `ia/specs/` defining domain behavior and vocabulary. Contrasts with **project spec** (temporary, issue-scoped). | [REFERENCE-SPEC-STRUCTURE.md](REFERENCE-SPEC-STRUCTURE.md), `AGENTS.md` §4 |
| **Project spec** | Temporary Markdown under `ia/projects/{ISSUE_ID}.md` for an active backlog item. Deleted after verified completion once normative content migrates to **reference specs** / **glossary** / `docs/`. | [PROJECT-SPEC-STRUCTURE.md](../projects/PROJECT-SPEC-STRUCTURE.md), `AGENTS.md` §4 |
| **Interchange JSON (artifact)** | Tooling and config JSON distinct from player **Save data**. Payloads carry `artifact` id and optional `schema_version`. Not part of **Load pipeline**. | `ARCHITECTURE.md` §Interchange JSON, [`docs/schemas/README.md`](../../docs/schemas/README.md), persist |
| **geography_init_params** | Interchange artifact for declarative **Geography initialization** (seed, map, water/rivers/forest). Not **Save data**. | persist, [`docs/schemas/README.md`](../../docs/schemas/README.md) |
| **scenario_descriptor_v1** | Interchange artifact for assembling test-mode saves from structured intent (map, terrain, water, **road stroke** lists). Not **Save data**. | persist §Load pipeline, [`docs/schemas/README.md`](../../docs/schemas/README.md), [`tools/fixtures/scenarios/BUILDER.md`](../../tools/fixtures/scenarios/BUILDER.md) |
| **City metrics history** | Optional Postgres time-series of per-**simulation tick** city aggregates (population, happiness, R/C/I demand, etc.). Not **Save data**. | mgrs §MetricsRecorder, [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) |
| **Agent test mode batch** | Headless Unity Editor path for committed scenarios: loads a save, optionally runs **simulation tick**s and golden-path assertions. Requires project lock release. | unity-dev §10, [`docs/agent-led-verification-policy.md`](../../docs/agent-led-verification-policy.md) |
| **IDE agent bridge** | Postgres job queue letting IDE agents control the Unity Editor: console logs, screenshots, Play Mode, compilation status, debug context bundles. | unity-dev §10, [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) |

<a id="planned-domain-terms"></a>

## Planned terminology (backlog-backed, non-authoritative)

> **Not canonical gameplay.** Rows here name **product directions** tracked as **open rows** in [`BACKLOG.md`](../../BACKLOG.md). They are **not** implemented rules until **reference specs** are updated. For current simulation and water behavior, use **Urban centroid**, **Urban growth rings**, **Surface height (S)**, etc. in the tables above. **If a reference spec is updated for a feature, the spec wins** over this section.
>
> **Full narrative:** [`docs/planned-domain-ideas.md`](../../docs/planned-domain-ideas.md). **Completed** rows and historical ids live only in [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md).

| Term | Working definition (intent only) | Trace |
|------|-----------------------------------|--------|
| **Geography authoring** | In-game or Editor flow to design **territory** / **urban** area **maps** with a **parameter dashboard** (e.g. **map** size, **water** / **forest** / **height** mix, **sea** / **river** / **lake** proportions). Intended to drive **geography initialization** and to reuse the same parameter model for **player** **terraform**, **basin** / **elevation** tools, **water body** placement in **depressions**, and **AUTO**-driven tools. | Open [`BACKLOG.md`](../../BACKLOG.md) (**gameplay / simulation** sections); [`docs/postgres-interchange-patterns.md`](../../docs/postgres-interchange-patterns.md) |
| **Urban pole** (working name) | A growth anchor for **AUTO** systems — e.g. employment or **desirability** hotspot — used to weight nearby **cells** for **streets** and **zoning**. Distinct from **committed** **pathfinding**: **road** segments still follow **road preparation** and **geo** §10. | Open [`BACKLOG.md`](../../BACKLOG.md) |
| **Multipolar urban growth** | Evolution from a single **urban centroid** to **multiple** **urban poles**, each with its own **urban growth rings** (or equivalent distance field), while preserving coherent regional patterns on one **map**; long-term **connurbation** between urban masses. | Open [`BACKLOG.md`](../../BACKLOG.md); coordinates **Urban growth rings** tuning rows there |
| **Connurbation** | Planned concept: two or more distinct urban areas on the same **map** recognized as coexisting under shared regional rules (exact criteria TBD). | Open [`BACKLOG.md`](../../BACKLOG.md) |
| **Water volume budget** | Planned **not** full 3D **fluid** simulation: a **water body** (or connected component) holds a **volume** constraint so expanding **basin** capacity can **lower** **surface height (S)** to conserve mass; conversely, constrained **basins** may raise **S**. **Rendering** updates **water** prefab height; optional directional **fill** **animation** for player feedback. | Open [`BACKLOG.md`](../../BACKLOG.md); related **water** / **terraform** rows there |
| **Moore-adjacent excavation fill** | Planned **gameplay**: excavating a **dry** **cell** **Moore**-adjacent to **open water** allows that **depression** to **fill** from the adjacent **water body** (rules TBD). | Open [`BACKLOG.md`](../../BACKLOG.md) |
