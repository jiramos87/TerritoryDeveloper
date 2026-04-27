---
purpose: "Reference spec for Glossary — Territory Developer."
audience: agent
loaded_by: router
slices_via: glossary_lookup
---
# Glossary — Territory Developer

> Domain concepts. Canonical detail lives in the linked spec — spec wins on conflict. Class names, methods, and backlog ID rules live in technical specs and `BACKLOG.md`.

## Abbreviations

Specs (relative to `ia/specs/`): geo=`isometric-geography-system.md`, roads=`roads-system.md`, water=`water-terrain-system.md`, sim=`simulation-system.md`, sim-signals=`simulation-signals.md`, persist=`persistence-system.md`, mgrs=`managers-reference.md`, ui=`ui-design-system.md`, udev=`unity-development-context.md`, arch=`architecture/` (sub-specs: `layers.md`, `data-flows.md`, `interchange.md`, `decisions.md`), audio=`audio-blip.md`, econ=`economy-system.md`.

Rules: pa=`ia/rules/plan-apply-pair-contract.md`, ph=`ia/rules/project-hierarchy.md`, ovs=`ia/rules/orchestrator-vs-spec.md`, inv=`ia/rules/invariants.md`, rs=`ia/rules/runtime-state.md`.

Skills: rr=`ia/skills/release-rollout/SKILL.md`, ss=`ia/skills/ship-stage/SKILL.md`, rrsbl=`ia/skills/release-rollout-skill-bug-log/SKILL.md`, rre=`ia/skills/release-rollout-enumerate/SKILL.md`, mpe=`ia/skills/master-plan-extend/SKILL.md`.

Master plans (DB-backed; render via `master_plan_render({slug})`): ms=`multi-scale`, train=`skill-training`, lifecycle=`lifecycle-refactor`. Sibling docs: ms-post=`docs/multi-scale-post-mvp-expansion.md`.

Docs: schemas=`docs/schemas/README.md`, mcp=`docs/mcp-ia-server.md`, lifecycle-doc=`docs/agent-lifecycle.md`, pg-interchange=`docs/postgres-interchange-patterns.md`, pg-setup=`docs/postgres-ia-dev-setup.md`, avpolicy=`docs/agent-led-verification-policy.md`, planned-ideas=`docs/planned-domain-ideas.md`.

Section shortcuts: mgrs §Zones / §Demand / §World / §Notifications / §MetricsRecorder; geo §14.5 = road stroke + lip + grass + Chebyshev; sim §Rings = centroid + growth rings; persist §Save / §Load pipeline / §Visual restore details.

## Grid & Coordinates

| Term | Definition | Spec |
|------|-----------|------|
| Cell | Smallest addressable map unit — one isometric tile of land, water, or development. MonoBehaviour per grid tile; holds height, terrainSlopeType, cellType, water body id, zone, building ref. | geo §1, §2, §11.2 |
| HeightMap | Terrain elevation field across the map. `int[,]` in `[MIN_HEIGHT, MAX_HEIGHT]`; kept in sync with `Cell.height` on every write. | geo §2 |
| Sorting order | Integer `sortingOrder` from depth + height + per-type offsets; determines front-to-back draw stacking. | geo §7 |
| Tile dimensions | `tileWidth = 1.0`, `tileHeight = 0.5` world units (diamond isometric). | geo §1.1 |
| Direction convention | N/S/E/W use fixed `(Δx, Δy)`; `+x` = North (up-right), `+y` = West (up-left). | geo §1.2 |
| Height offset | `(h - 1) * 0.25` added to world Y per height level above base. | geo §1.1 |
| World ↔ Grid conversion | Diamond projection + inverse using `tileWidth` / `tileHeight` / `heightOffset` + `round`. territory-ia `isometric_world_to_grid` covers planar inverse only (not height-aware). | geo §1.1, §1.3 |
| Moore neighborhood | Eight tiles touching a cell (cardinals + diagonals). | geo §2.4.1 |
| Cardinal neighbor | Four tiles sharing an edge (N/S/E/W steps). Height constraints, water cascades, and many road rules use cardinals only. | geo §2.3, §5.6.2 |
| Grass cell | Default developable land — no street/interstate. Manual pathfinding treats grass + street/interstate as walkable; AUTO adds undeveloped-light per mode. | geo §14.5, §13.9 |

## Height System

| Term | Definition | Spec |
|------|-----------|------|
| Height range | `MIN_HEIGHT`–`MAX_HEIGHT` = 0–5; caps cliffs and sorting. | geo §2.1 |
| Sea level | `SEA_LEVEL = 0` baseline; registered bodies use per-body surface height `S`. | geo §2.1 |
| Height constraint | Cardinal `\|Δh\| ≤ 1` for normal land; larger drops use cliffs and special cases (e.g. lake bowls). | geo §2.3 |
| Height generation | 40×40 designer template; larger maps extend with blended Perlin noise; lakes and rivers stamp afterward. | geo §2.2 |

## Terrain & Slopes

| Term | Definition | Spec |
|------|-----------|------|
| Slope type | `TerrainSlopeType` enum — flat, ramp, diagonal wedge, concave corner — from 8-neighbor height compares (land vs water-shore rules differ). | geo §3–§4 |
| Slope categories | Flat plateau, cardinal ramp, diagonal wedge, corner-up valley. Drives grass vs ramp prefabs and road eligibility. | geo §3.3 |
| Cliff | Vertical drop between two cells, read as rock wall on the drop boundary. S/E-facing stacked meshes when `\|Δh\| ≥ 1` on cardinals; N/W not rendered (camera). | geo §5.7 |
| Shore band | Dry-land ring hugging water with shore art; heights clamped to water surface. Moore neighbors of water with `height ≤ min(S)`. | geo §2.4.1 |
| Surface-height gate | Land-cell gate for water-shore art: `h ≤ V + MAX` with `V = max(MIN_HEIGHT, S − 1)`. Above cap = rim (ordinary terrain). | geo §4.2 |
| Rim | Higher dry ground above the shore band — hills toward water, not a beach ramp. Fails the shore-art gate. | geo §14.1 |
| Bay | Inner-corner shore pattern (cove / notch). Chosen from neighbor water patterns (perpendicular cardinals, rectangle outer corner). | geo §5.9 |
| Cliff face visibility | Only S/E cliff stacks instantiated; `Cell.cliffFaces` may still record N/W for systems like hydrology. | geo §5.7 |
| Cliff suppression | Hides cliff mesh where a shore ramp already shows the transition. One-step suppression toward water/water-shore; rim keeps a segment; `\|Δh\| ≥ 2` still stacks. Toward off-grid void on S/E, water-shore primary skips the duplicate brown cliff. | geo §5.6.1, §5.7 |
| Cut-through corridor | Trench carved through high terrain for a flat path. Terraform flattens to `baseHeight`; cliffs on sides where neighbors stay higher. | geo §5.10 |

## Water

| Term | Definition | Spec |
|------|-----------|------|
| Water body | Connected region sharing one surface height + identity (lake, river reach, sea). `WaterBody`: id, `SurfaceHeight`, cells. | geo §11.2 |
| Water map | Which body id each cell belongs to. `WaterMap`: `int[,]` ids + body list; `0` = dry. | geo §11.2 |
| Open water | Cell registered as water in `WaterMap`. Sorting uses surface height. | geo §11.2, water |
| Water-shore (land) | Dry cell painted with shore transition prefabs toward water. Subject to surface-height gate and shore refresh. | geo §11.2, §4.2, water |
| Surface height (S) | Logical water level for a body. Open water cells use `Cell.height = S`; bed may differ underneath. | geo §11.1 |
| Visual reference (V) | `V = max(MIN_HEIGHT, S − 1)`. Shore-art gate compares land `h` to `V + MAX`, not raw `S`. | geo §2.4.1, §4.2 |
| Water body kind | lake / river / sea — controls merge rules, junction exclusions, carve behavior. | geo §11.2, §12.3 |
| Depression-fill | Lakes forming in natural lows — floods from minima to spill height; merged and validated per lake rules. | geo §11.3 |
| Spill height | Overflow elevation capping a depression fill. | geo §11.3 |
| Junction | Cardinal contact with `S_high > S_low` (subject to lake exclusion). Creates drops, merges, special shore topology. | geo §11.8 |
| Pass A (bed alignment) | Normalizes underwater bed when high water meets low water. Lowers upper-side bed toward lower neighbors; sweeps until stable; ids unchanged. | geo §11.7 |
| Pass B (junction merge) | Reassigns cells so lower surface wins at a multi-body contact. Moves dry/shore on lower plane to lower body; may absorb bank cells. | geo §11.7 |
| Brink | Dry-land role beside a river–river surface step. `UpperBrink` / `LowerBrink` drive cascade shore passes and cliff stacks. | geo §11.8 |
| Lake exclusion | Lake–lake surface steps skip Pass A/B and cascades on that edge. Sea is not treated as lake for this rule. | geo §11.7 |
| Shore refresh | Recomputes shore grass/ramp/cliff art after water changes. Updates Moore (and sometimes wider) rings. | geo §11.6 |
| Cascade | Waterfall between two water tiles at different S (no dry shore between). Cardinal step; S/E cascade prefabs; segment count from `S_high − S_low`. | geo §5.6.2 |
| Corner promotion | Raises inner-corner river bed cells so dry bank stays continuous on bends. Bed cells with two perpendicular neighbors at `H_bed + 1` promoted before water assignment. | geo §12.5 |

## Rivers

| Term | Definition | Spec |
|------|-----------|------|
| River | Fixed water channel carved after lakes — flows toward a border or sink. Static post-init; cardinal path; `H_bed` non-increasing toward exit. | geo §12 |
| River bed (H_bed) | Floor elevation of the channel under the surface. Distinct from surface (`≈ H_bed + 1`) and from dry bank cells. | geo §11.1, §12.4 |
| River bank | Dry strip beside the channel — `H_bank = H_bed + 1` when geometry allows, so channel reads sunken. | geo §12.4 |
| River width | Bed 1–3 cells; total with shores in `{3,4,5}`; width steps limited between segments. | geo §12.4 |
| Forced river | Fallback when no natural path qualifies — generator carves a basin and places a river via constraint relaxation. | geo §12.4 |
| River spacing | Prior corridors dilated (Chebyshev); same-border entries spaced apart. | geo §12.4 |
| Chebyshev distance | `max(\|Δx\|, \|Δy\|)` — king moves on an 8-way grid. Used for corridor dilation and entry spacing. | geo §12.4, §14.5 |

## World generation

| Term | Definition | Spec |
|------|-----------|------|
| Geography initialization | First-time map build on New Game — ordered pipeline heightmap → water → rivers → interstate → forests → desirability → sorting. `GeographyManager` orchestrates; order must match save/load assumptions. Optional Editor diagnostic JSON `geography_init_report` (gitignored). | arch, geo §12.1, mgrs, mcp |

## Roads & Bridges

| Term | Definition | Spec |
|------|-----------|------|
| Terraform plan | Authoritative description of terrain changes under a proposed road. `PathTerraformPlan`: per-cell actions, heights, `postTerraformSlopeType`; `Apply` / `Revert`. | geo §8 |
| Road validation pipeline | Required gate before any street/interstate commit. Preparation (`TryPrepareRoadPlacementPlan`, longest-prefix, optional locked deck-span) → Phase-1 heights → `Apply` → prefab resolve. | geo §13.1, roads |
| Road stroke | Ordered path of cells for a placement attempt (player drag or AUTO route) before filtering, truncation, plan build. Same stroke drives preview and commit. | geo §14.5, roads |
| Phase-1 validation | Height + neighbor consistency check on a terraform plan before terrain writes commit (`TryValidatePhase1Heights`). Fails fast on `\|Δh\|` or edge-rule breaks. | geo §13.1 |
| Interstate | Limited-access highway linking the city grid to the map border. Long-straight preference, full-path validation, cut-through forbidden. | geo §13.5, §13.6, mgrs |
| Interstate border | Outward-facing face of an interstate at the map border — cell+side pair where an exit leaves the grid into a neighbor-city stub. Recorded by `NeighborCityBinding` on road-build. | geo §13.5 |
| Street (ordinary road) | Player or AUTO non-interstate road. Same validation family as manual draw (prefix, deck-span, terraform). | geo §14.5, §13.1, mgrs |
| Street or interstate | Umbrella when the same rule applies to both. Both must pass the road validation pipeline and commit via `PathTerraformPlan`. Use specific term when distinction matters (border endpoints, cut-through bans). | geo §13.1, roads |
| Map border | Outer boundary of the playable grid (`x`/`y` min/max). S/E brown cliff stacks toward off-grid void use `MIN_HEIGHT` as foot. Interstate endpoints and some water rules reference the border. | geo §14.5, §5.7, §13.5 |
| Water-slope tile | Coastal land cell using water-slope shore prefabs (`IsWaterSlopeCell`). Treated as impassable/high-cost for normal street routing; bridges use separate rules. | geo §5.8, §10, roads |
| Road cache invalidation | After any road topology change, cached road queries must be rebuilt. Project invariant: call invalidate after modifications. | roads, inv |
| Cut-through | Flatten terrain along a street/interstate path when slopes are too steep. `Flatten` to `baseHeight`; rejected when `maxHeight - baseHeight > 1` (interstate forbids). | geo §8.3, §13.6 |
| baseHeight (terraform) | Reference elevation a cut-through writes along the path so the road sits flat through high ground. | geo §8.3, §14.5 |
| Scale-with-slopes | Road follows natural ramps when every step is gentle. No height writes; plan records `TerraformAction.None` + slope type per cell. | geo §8.2 |
| Deck span | Straight bridge segment over water, no corners on water, one deck height. Axis-aligned; uniform `waterBridgeDeckDisplayHeight`. | geo §13.4 |
| Bridge lip | Last firm dry-land cell at the water's edge where a deck span begins — anchor for deck height and locked chord. | geo §13.4, §14.5 |
| Wet run | Contiguous water and/or water-slope cells along a stroke that a bridge crosses in one straight segment. Truncation rules keep wet runs intact. | geo §13.4, §14.5, roads |
| Locked chord | Fixed straight line from dry bank through water to dry land for manual bridge preview/commit. Cardinal; matching bridge height. | geo §13.4 |
| Longest valid prefix | Truncates a stroke to the longest part that still passes all rules. Silent when stroke starts on blocked slope. | geo §13.2, roads |
| Land slope eligibility | Flat and cardinal ramps only carry strokes and A* walkability. Pure diagonals and corner-up slopes disallowed. | geo §13.10, roads |
| Resolver rules | Prefab choice invariants: elbow degree, exit alignment, terraform-over-live-slope, hill avoidance, interstate straight preference, bridge approach orthogonality (A–F). | geo §13.7, roads |
| Road reservation | Cells AUTO zoning must leave empty so future street alignment stays possible. Axial strips from `GetRoadExtensionCells` / `GetRoadAxialCorridorCells` excluded from auto-zone each tick. | geo §13.9, sim |

## Pathfinding

| Term | Definition | Spec |
|------|-----------|------|
| Pathfinding cost model | A* costs: flat cheap, slopes expensive, water-slope very high, `\|Δh\| > 1` impossible. | geo §10 |
| A* search | Best-first shortest-path on the road grid using the geo cost table. Separate entry points for manual vs AUTO simulation walkability. | geo §10, roads |
| Interstate cost model | Stronger penalties for kinks and detours. Scales slope costs; adds turn/zigzag/away penalties and straight bonus. | geo §10 |
| Diagonal step expansion | Roads move cardinal only. Planner splits diagonal moves into two orthogonal steps for prefab compat. | geo §8.4 |

## Zones & Buildings

| Term | Definition | Spec |
|------|-----------|------|
| RCI | Residential / Commercial / Industrial — three land-use families driving demand and tax base. | mgrs §Zones |
| Zone | Parcel designated for a type of development before a building appears. `Zone` component: type, density tier, level, building ref. | mgrs §Zones |
| Zone density | Light / medium / heavy — selects building size/tier. Undeveloped-light interacts with AUTO roads. | mgrs §Zones |
| Pivot cell | Anchor tile of a multi-cell building for save, sort, demolish. Other footprint cells point at the pivot. | mgrs §Zones |
| Building footprint | All grid cells covered by one building (1×1 or multi-cell). Sorting, save, bulldoze, growth operate on the footprint as a unit. | mgrs §Zones |
| Undeveloped light zoning | Zoned but empty tile at light density — passable for AUTO street planning only. Walkability via `AutoSimulationRoadRules` + sim pathfinder; medium/heavy and built tiles differ. | mgrs §Zones, geo §13.9 |
| Building | Visible structure on a zoned or service tile — RCI or utility. Spawned by zone/growth/resource rules; 1×1 or multi-cell; level tracks growth stage. | mgrs §Zones, §World |
| Zone S | 4th zone channel alongside RCI — state-owned buildings, 7 sub-types × 3 density tiers. Manual placement only in MVP; budget-gated via envelope allocator. | [econ#zone-s](economy-system.md#zone-s) |
| ZoneSubTypeRegistry | ScriptableObject cataloging 7 Zone S sub-types (police, fire, education, health, parks, public housing, public offices). Per-entry: id, displayName, prefab, baseCost, monthlyUpkeep, icon. | [econ#zone-sub-type-registry](economy-system.md#zone-sub-type-registry) |
| ZoneSService | MonoBehaviour orchestrating Zone S placement: `ZoneSubTypeRegistry` baseCost → `BudgetAllocationService.TryDraw` → `ZoneManager` placement with subTypeId. Grid access only via `GridManager.GetCell` (inv #5). | [econ#zonesservice-placement](economy-system.md#zonesservice-placement) |

## Simulation & Growth

| Term | Definition | Spec |
|------|-----------|------|
| Simulation tick | One automatic city update — roads extend, zones spread, utilities plan. Driven by `TimeManager` → `SimulationManager.ProcessSimulationTick()`. | sim |
| AUTO systems | Three autopilots growing the city each tick — streets, zoning, utilities. `AutoRoadBuilder`, `AutoZoningManager`, `AutoResourcePlanner` in fixed order after centroid/budget. | sim |
| Tick execution order | Budget valid → centroid recompute → roads → zoning → resource planner. | sim |
| Growth budget | Per-category cap on AUTO spend per tick. Pool from `GrowthBudgetManager` using projected net monthly cash flow (tax base − monthly maintenance) when positive, else treasury (`CityStats`). | sim, mgrs §Demand |
| Urban centroid | Statistical center of mass of development, biases growth rings. `UrbanCentroidService` computes centroid + ring metrics. | sim, sim §Rings |
| Urban growth rings | Distance bands from the centroid — AUTO weights where roads and zones expand (denser near core). Recalculated each tick before AUTO runs. | sim §Rings |
| SimulationSignal | Locked 12-entry enum for city-sim depth: pollution (air / land / water), crime, services (police, fire, education, health, parks), traffic, waste pressure, land value. Ordinal-stable; indexes `SignalMetadataRegistry` entries. | sim-signals |
| SignalField | Per-signal `float[,]` grid sized to `GridManager` dims; clamp-floor-0 invariant on every write; `Snapshot()` returns deep-copy buffer for diffusion ping-pong. | sim-signals |
| SignalFieldRegistry | Per-scene MonoBehaviour owning one `SignalField` per `SimulationSignal`; allocates in `Awake` from `GridManager.width/height`; `ResizeForMap` reallocates on map reload. | sim-signals |
| SignalMetadataRegistry | ScriptableObject keyed by `SimulationSignal` ordinal — per-signal `diffusionRadius`, `decayPerStep`, `Vector2 anisotropy`, `RollupRule`. | sim-signals |
| DiffusionKernel | Stage 1.2 system applying separable horizontal + vertical Gaussian (per-axis sigma from anisotropy) plus `decayPerStep` to each `SignalField` once per tick. | sim-signals |
| SignalTickScheduler | Stage 1.2 driver running the signal phase: producers → diffusion → district rollup → consumers, once per `SimulationManager.ProcessSimulationTick`. | sim-signals |
| DistrictSignalCache | Stage 1.3 district aggregator keyed by signal × district; populated per `RollupRule`. Stage 1.1 ships a placeholder class only. | sim-signals |
| Rollup rule | Per-signal aggregation in `DistrictSignalCache` — `Mean` for steady-exposure signals, `P90` for hot-spot signals (`Crime`, `TrafficLevel`). | sim-signals |

## City systems

| Term | Definition | Spec |
|------|-----------|------|
| Demand (R / C / I) | Per-zone growth pressure — jobs, population, forests, per-sector tax, happiness-target multiplier. Refreshed each in-game day after happiness. | mgrs §Demand |
| Tax base | Economic capacity tied to zoned development and population. Income flows through `EconomyManager` / `CityStats`; highest of three tax rates feeds back into happiness (above comfort band) and per-sector scaling into R/C/I demand. | mgrs §Demand |
| Monthly maintenance | Recurring city expense on the first sim calendar day of each month: street upkeep from `CityStats.roadCount` + utility upkeep from registered power-plant count. Collected after tax income. Debits via `EconomyManager.SpendMoney`; insufficient funds skip the charge with a game notification. HUD net-money hint uses projected tax − projected maintenance. | mgrs §Demand |
| envelope (budget sense) | Per-sub-type monthly spending allowance for Zone S. Global S monthly cap split 7 ways via pct sliders (sum=100%). `TryDraw` blocks spend when remaining < amount even if treasury has funds. | [econ#budget-envelope](economy-system.md#budget-envelope) |
| TreasuryFloorClampService | Helper extracted from `EconomyManager` (inv #6) enforcing a non-negative treasury floor. API: `CanAfford(int)`, `TrySpend(int, string)`, `CurrentBalance`. `TrySpend` calls `CityStats.RemoveMoney` on success; on failure posts insufficient-funds notification. Only authorised treasury-mutation site. | [econ#treasury-floor-clamp](economy-system.md#treasury-floor-clamp) |
| BudgetAllocationService | Helper extracted from `EconomyManager` (inv #6) owning per-Zone-S-sub-type monthly envelope. API: `TryDraw`, `GetMonthlyEnvelope`, `SetEnvelopePct`, `SetEnvelopePctsBatch`, `MonthlyReset`. Composes `TreasuryFloorClampService`. Save round-trip via `CaptureSaveData` / `RestoreFromSaveData` (schema v4). | [econ#budget-envelope](economy-system.md#budget-envelope) |
| IBudgetAllocator | Interface contract for `BudgetAllocationService`. Decouples call sites; enables test stubs. Same API as the impl. | [econ#budget-envelope](economy-system.md#budget-envelope) |
| BondLedgerService | Helper extracted from `EconomyManager` (inv #6) implementing single-bond-per-scale-tier ledger. Proactive treasury injection. API: `TryIssueBond`, `GetActiveBond`, `ProcessMonthlyRepayment`, `ProcessAllMonthlyRepayments`. Repayment routes through `TreasuryFloorClampService.TrySpend`; failure flags `arrears` (HUD only). Save round-trip via schema v4. | [econ#bond-ledger](economy-system.md#bond-ledger) |
| IBondLedger | Interface contract for `BondLedgerService`. Methods: `TryIssueBond`, `GetActiveBond`, `ProcessMonthlyRepayment`. | [econ#bond-ledger](economy-system.md#bond-ledger) |
| IMaintenanceContributor | Contract for monthly-maintenance registry. Methods: `GetMonthlyMaintenance() → int`, `GetContributorId() → string` (deterministic sort key), `GetSubTypeId() → int` (−1 = general pool, 0..6 = Zone S sub-type). `EconomyManager` iterates sorted by contributorId (ordinal) in `ProcessMonthlyMaintenance`. Built-in adapters: `RoadMaintenanceContributor`, `PowerPlantMaintenanceContributor`. | [econ#maintenance-contributor-registry](economy-system.md#maintenance-contributor-registry) |
| Desirability | Per-tile growth attractiveness from nearby terrain (water, forest, etc.), computed after geography init. Biases AUTO. | mgrs §Demand, arch |
| Forest (coverage) | Tree cover — sparse / medium / dense — affecting demand and player forest tools. | mgrs §World |
| Regional map | Broader region with neighboring cities; context for regional systems and UI. | mgrs §World |
| Utility building | Non-RCI service structure (water, power, etc.), often multi-cell. Placed manually or by AUTO resource planning. | mgrs §World |
| Happiness | City-wide satisfaction score 0–100. Recalculated each in-game day (and on tax-rate UI changes) from: employment, highest tax rate vs comfort band, service coverage, forest bonus, development base, pollution penalty. Converges smoothly toward target (lerp). Feeds demand via a target-derived multiplier. | mgrs §Demand |
| Pollution | City-wide environmental degradation. Sources: industrial buildings (heavy > medium > light), polluting utilities (nuclear = medium, fossil = high). Sinks: forest coverage, future parks. Negative factor in happiness. | mgrs §World |
| Game notification | Player-facing toast/alert (money, errors, hints). Only the notification singleton enqueues UI messages. | mgrs §Notifications |

## Persistence

| Term | Definition | Spec |
|------|-----------|------|
| Save data | On-disk snapshot of city state. `GameSaveData`: `List<CellData>` + `WaterMapData` from `WaterMap.GetSerializableData()`. | persist §Save |
| CellData | Serializable DTO mirroring runtime `Cell` fields — height, prefabs, sortingOrder, zone, water ids, building refs. | persist §Save, geo §11.2 |
| Water map data | Serialized water bodies and per-cell ids. `WaterMapData` nested in `WaterMap.cs`; v2 format with legacy fallback. | persist §Save, geo §11.5 |
| Legacy save | Older save without `waterMapData` — load uses fallback path to reconstruct water from height/legacy flags. | persist §Load pipeline, §Visual restore details |
| Visual restore | Reloads saved prefabs and `sortingOrder` without regenerating slopes/sort. Building post-pass; details in geo §7.4. | persist §Visual restore details, geo §7.4 |
| Load pipeline order | Heightmap → water map (or legacy) → grid cells → sync shore/body ids. Do not reorder. | persist §Load pipeline |

## Audio

| Term | Definition | Spec |
|------|-----------|------|
| Bake-to-clip | On-demand render of `BlipPatchFlat` → `AudioClip` via `BlipBaker.BakeOrGet`. LRU-cached by `(patchHash, variantIndex)` under 4 MB. | audio §5.1, §7 |
| Biquad band-pass | DF-II transposed BP filter (`BlipFilterKind.BandPass`); RBJ constant-skirt-gain form. Coefficients `a1n / a2n / b0n` pre-computed once per `BlipVoice.Render` from `cutoffHz` + `resonanceQ`; per-sample kernel reads/writes `state.biquadZ1 / biquadZ2`. | audio §3.2, §4.2 |
| Blip bootstrap | Persistent GameObject at `MainMenu.unity` root; `DontDestroyOnLoad` on Awake. Hosts Catalog / Player / MixerRouter / Cooldown. Scene-load suppression: `BlipEngine.Play` returns early until `BlipCatalog.Awake` sets ready flag. Boot-time reads `SfxMutedKey` (`PlayerPrefs.GetInt`) and clamps dB to −80 if muted. `BlipVolumeController` (on `OptionsPanel`) primes slider/toggle from PlayerPrefs on `OnEnable`. | audio §5.1, §5.2 |
| Blip cooldown | Minimum ms between same-BlipId plays; per-patch `cooldownMs` enforced by `BlipCooldownRegistry` queried from `BlipEngine` before dispatch. | audio §5.5 |
| Blip delay pool | `BlipDelayPool` `ArrayPool<float>`-backed delay-buffer lessor owned by `BlipCatalog`; per-call `Lease(sampleRate, maxDelayMs)` used by Comb / Allpass / Chorus / Flanger FX kernels; pre-leased outside the NoAlloc measurement window. | audio §5.1 |
| Blip FX chain | Up to 4 `BlipFxSlot` slots per `BlipPatch` (BitCrush / RingMod / SoftClip / DcBlocker / Comb / Allpass / Chorus / Flanger); switch-dispatched in `BlipFxChain.ProcessFx` per sample. Slot-N state (`dcZ1_N`, `dcY1_N`, `ringModPhase_N`, `delayWritePos_N`) lives on `BlipVoiceState`. | audio §3.2, §5.1 |
| Blip LFO | `BlipLfoKind` waveform oscillator (Off / Sine / Triangle / Square / SampleAndHold) per `BlipPatch`; sample-rate run, scaled by depth, routed via `BlipLfoRoute`; output smoothed by `SmoothOnePole` (τ ≈ 20 ms). | audio §4.1 |
| Blip LUT pool | `BlipLutPool` plain-class `ArrayPool<float>` stub owned by `BlipCatalog`; reserved for wavetable LUT caching; no kernel wired yet. | audio §5.1 |
| Blip mixer group | One of three routing groups (`Blip-UI`, `Blip-World`, `Blip-Ambient`) on `BlipMixer.mixer`. Master exposes `SfxVolume` dB param. | audio §5.4 |
| Blip patch | `BlipPatch` ScriptableObject — oscillators (0..3), AHDSR envelope, one-pole filter, jitter triplet, voice management, bake params. Inspector-authored; flattened to `BlipPatchFlat` for runtime DSP. | audio §4.1 |
| Blip patch flat | `BlipPatchFlat` blittable struct, no managed refs. Produced by `BlipPatchFlat.FromSO`. Input to `BlipBaker` and voice kernels. `mixerGroup` excluded (routed by `BlipMixerRouter`). | audio §2, §3.1 |
| Blip variant | Per-patch randomized sound selection `0..variantCount-1`; round-robin or jitter on `BlipEngine.Play` (fixed 0 when `deterministic`). | audio §4.1 |
| Param smoothing | 1-pole IIR `SmoothOnePole(ref float z, float target, float coef)` at 50 Hz cutoff (τ ≈ 20 ms); applied per LFO route target in `BlipVoice.Render` to kill zipper noise on Square / S&H. `coef = 1 − exp(−2π × 50 / sampleRate)`. | audio §3.2 |
| Patch flatten | `BlipPatch` SO → `BlipPatchFlat` struct conversion on `BlipCatalog.Awake`; strips managed refs (`AudioMixerGroup`, `AnimationCurve`) for audio-thread safety. | audio §2, §4.1 |
| patch hash | FNV-1a 32-bit content hash (`BlipPatchHash.Compute`) over canonical scalar fields in frozen order. `[SerializeField] private int patchHash`; recomputed on `OnValidate`; verified on `Awake`/`OnEnable` (warn-only). `BlipBaker` LRU key. | audio §4.3 |

## Prefabs & Visual Layer

| Term | Definition | Spec |
|------|-----------|------|
| Land slope prefabs | Twelve terrain ramp meshes — 4 cardinals, 4 diagonals, 4 corner-ups. Named `*SlopePrefab` per facing. | geo §6.1 |
| Water slope prefabs | Shore ramps at land↔water edges. `*SlopeWaterPrefab` / `*UpslopeWaterPrefab` variants. | geo §6.2 |
| Slope variant naming | Pattern `{flatPrefab}_{slopeCode}Slope`; `GetSlopeVariant` resolves by constructed name. | geo §6.4 |
| Infrastructure prefabs | Shared props — sea tile, cliff wall pieces (S/E visible, N/W reserved), bay corners. | geo §6.3 |
| Sorting formula | `TERRAIN_BASE_ORDER + depthOrder + heightOrder + typeOffset` with `depthOrder = -(x+y) * DEPTH_MULTIPLIER`. | geo §7.1 |
| Sorting components | Four additive pieces: `TERRAIN_BASE_ORDER` (base), depthOrder (isometric depth), heightOrder (elevation), typeOffset (layer kind). | geo §7.1, §7.2 |
| DEPTH_MULTIPLIER | Depth beats max-height contribution (100 vs 10 × max height). | geo §7.1, §7.3 |
| HEIGHT_MULTIPLIER | Per-level boost inside `heightOrder` so taller tiles sort above neighbors at same depth. | geo §7.1 |
| Type offsets | Terrain 0, slopes +1, road +5, utility +8, building +10, effect +30. | geo §7.2 |

## Documentation

| Term | Definition | Spec |
|------|-----------|------|
| Backlog record | Canonical per-issue YAML at `ia/backlog/{ISSUE_ID}.yaml` (open) or `ia/backlog-archive/{ISSUE_ID}.yaml` (closed). Single source for id, type, title, status, section, spec path. Never hand-edited; mutated only by `reserve-id.sh`, `stage-file`, `project-new`, and the stage-scoped `/closeout` pair. | [AGENTS §7](../../AGENTS.md), [reserve-id.sh](../../tools/scripts/reserve-id.sh), [materialize-backlog.sh](../../tools/scripts/materialize-backlog.sh) |
| Backlog view | Generated Markdown `BACKLOG.md` (open) and `BACKLOG-ARCHIVE.md` (closed) produced by `materialize-backlog.sh`. Never hand-edited; regenerated after yaml writes. | [AGENTS §7](../../AGENTS.md) |
| Reference spec | Permanent Markdown under `ia/specs/` defining domain behavior and vocabulary. | [REFERENCE-SPEC-STRUCTURE.md](REFERENCE-SPEC-STRUCTURE.md) |
| Project spec | Temporary Markdown at `ia/projects/{ISSUE_ID}.md` for an active backlog item. Deleted after verified completion once content migrates to reference specs / glossary / `docs/`. | [PROJECT-SPEC-STRUCTURE.md](../../docs/PROJECT-SPEC-STRUCTURE.md) |
| Interchange JSON (artifact) | Tooling/config JSON distinct from player Save data. Payloads carry `artifact` id and optional `schema_version`. Not part of Load pipeline. | arch, schemas, persist |
| geography_init_params | Interchange artifact for declarative Geography initialization (seed, map, water/rivers/forest). | persist, schemas |
| scenario_descriptor_v1 | Interchange artifact for assembling test-mode saves from structured intent (map, terrain, water, road-stroke lists). | persist §Load pipeline, schemas, [BUILDER](../../tools/fixtures/scenarios/BUILDER.md) |
| City metrics history | Optional Postgres time-series of per-tick city aggregates (population, happiness, R/C/I demand). | mgrs §MetricsRecorder, pg-setup |
| Agent test mode batch | Headless Unity Editor path for committed scenarios: loads a save, optionally runs ticks + golden-path assertions. Requires project lock release. | udev §10, avpolicy |
| IDE agent bridge | Postgres job queue letting IDE agents control the Unity Editor: console logs, screenshots, Play Mode, compilation, debug bundles. | udev §10, mcp |
| runtime-state | Per-clone JSON at `ia/state/runtime-state.json` (gitignored) holding last `verify:local` exit, last `db:bridge-preflight` exit, queued test-mode scenario id. Read/write via MCP `runtime_state` or `tools/scripts/runtime-state-write.sh`. | rs, mcp |
| Orchestrator document | Permanent coordination Markdown under `ia/projects/` tracking a multi-step plan (master plan, step- or stage-level orchestrators). Not closeable via stage-scoped `/closeout` pair. | ovs, ph |
| Project hierarchy | Two-level execution structure: Stage > Task. Stages authored at master-plan-new; Tasks materialize lazily via `stage-file-apply`; specs ephemeral. Cardinality gate: ≥2 Tasks per Stage (hard), ≤6 (soft). | ph |
| Stage | Parent-of-Task execution unit. Shippable compilable increment; authored as `### Stage N.M` block with Exit + Tasks subsections. Status: `Draft → In Review → In Progress → Final`. Verified by `/ship-stage` (Path A per task + batched Path B at stage end). | ph |
| Plan-Apply pair | Lifecycle pattern where Opus pair-head writes a `§Plan` payload (ordered `{operation, target_path, target_anchor, payload}` tuples); Sonnet pair-tail reads + applies verbatim. Five seams: plan-review↔plan-fix-apply, stage-file-plan↔stage-file-apply, project-new-plan↔project-new-apply, code-review↔code-fix-apply, audit↔closeout-apply. Idempotent re-runs; ambiguous anchors escalate to Opus. | pa |
| plan review | Opus pair-head that reads all Tasks of a Stage + master-plan header + invariants; emits fix tuples into `§Plan Fix` of the Stage block. Seam #1; tail = plan-fix apply. | pa |
| plan-fix apply | Sonnet tail that reads `§Plan Fix` tuples and applies verbatim. Validation gate: `validate:master-plan-status` + `validate:backlog-yaml`. | pa |
| spec enrichment | Sonnet stage pulling glossary anchors + tightening spec terminology against this file. | pa |
| Opus audit | Opus pair-head post-verify that reads spec → impl diff → verify findings → outputs `§Audit` + `§Closeout Plan` tuples into project spec. Seam #5; tail = closeout apply. | pa |
| Opus code review | Opus pair-head reading diff vs spec + invariants + glossary; emits PASS, minor notes, or `§Code Fix Plan` tuples. Seam #4; tail = code-fix apply. | pa |
| code-fix apply | Sonnet tail reading `§Code Fix Plan` tuples, applying fixes to source, re-entering `/verify-loop`. Validation gate: `verify:local`. | pa |
| closeout apply | Sonnet tail reading `§Closeout Plan` tuples + migrating canonical knowledge to glossary / specs / rules / docs + archiving BACKLOG row + deleting spec + persisting journal. Validation gate: `validate:all`. | pa |
| Rollout tracker | Living tracker doc `docs/{umbrella-slug}-rollout-tracker.md` pairing with an umbrella DB-backed master plan (slug) that has ≥3 child plans / buckets. Tracks each child through the rollout lifecycle in a matrix row. Seeded once by release-rollout-enumerate; advanced row-by-row via `/release-rollout`. | rr, rre |
| Rollout lifecycle | 7-column matrix per child master-plan: (a) enumerate → (b) explore → (c) plan → (d) stage-present → (e) stage-decomposed → (f) task-filed → (g) align. Handoff target to single-issue flow = column (f) ≥1 task filed. Cell glyphs: `✓` done / `◐` partial / `—` not started / `❓` ambiguous / `⚠️` disagreement w/ umbrella. | rr |
| Alignment gate | Column (g) of rollout lifecycle. Per new domain entity requires: glossary row present + `ia/specs/*.md` section anchor + MCP (`router_for_task` / `spec_section`) resolves. Gates only (e) → (f) handoff on new-entity introduction; does not block (a)–(d) / (f) for pre-aligned rows. Failure → (g) marked `—` + skill-bug entry, never silent skip. | rr |
| Skill Iteration Log | Aggregator section `## Skill Iteration Log` in a rollout tracker. One row per skill-bug; cross-references per-skill `## Changelog` anchor. Dual-written by release-rollout-skill-bug-log with the owning skill's changelog entry. | rrsbl |
| Per-skill Changelog | Tail section `## Changelog` inside `ia/skills/{name}/SKILL.md` tracking dated fix audits (`### YYYY-MM-DD — {summary}` → bullets: symptom / fix / location / status-applied `{commit}` or `pending`). Source of truth for skill bugs. | mpe |
| Ship-stage dispatcher | Slash command `/ship-stage {MASTER_PLAN_PATH} {STAGE_ID}` + ship-stage Opus chain orchestrator + ship-stage skill. Chains `plan-author → spec-implementer → verify-loop (--skip-path-b)` per task + stage-scoped `/closeout` pair at stage end. MCP context loaded once via `domain-context-load`; per-task Path A compile gate; one batched Path B at stage end on cumulative delta. Emits chain-level stage digest + next-stage handoff. Between `/ship` and `/release-rollout` in dispatch hierarchy. | ss, lifecycle-doc |
| Chain-level stage digest | Structured report emitted by ship-stage dispatcher at chain end (after all tasks closed + batched Path B). Fenced JSON header + caveman summary + `chain:` block `{tasks[], aggregate_lessons[], aggregate_decisions[], verify_iterations_total}`. Aggregates cross-task lessons + decisions across the stage run. `stage_verify`: `passed` / `failed` / `skipped`. | ss |
| Stage tail (open / incomplete) | After every task row in a Stage is Done-like, ship-stage Pass 2 bulk (verify-loop → code-review → opus-audit → stage closeout) may still be pending. Machine signal: any filed id has open `ia/backlog/{ISSUE_ID}.yaml`. Same check as `validate:master-plan-status` R6. `/ship-stage` re-entry sets `PASS2_ONLY` — skips Pass 1, runs Pass 2 through closeout. Stage-level idempotent when validators green. | ss, [validate-master-plan-status](../../tools/validate-master-plan-status.mjs) |
| skill self-report | Structured JSON at Phase-N-tail of a lifecycle skill when friction fires (`guardrail_hits > 0 OR phase_deviations > 0 OR missing_inputs > 0`). Schema: `{skill, run_date, schema_version, friction_types[], guardrail_hits[], phase_deviations[], missing_inputs[], severity}`. Appended to target's Per-skill Changelog as `source: self-report`; consumed by skill-train. Clean runs stay silent. | train |
| skill training | Retrospective Changelog-driven loop: lifecycle skills emit skill self-report entries; skill-train aggregates recurring friction (≥2 occurrences) into a patch proposal for user review. No auto-apply. | train |
| patch proposal (skill) | Unified-diff proposal authored by skill-train against a target SKILL.md (Phase sequence / Guardrails / Seed prompt), stored as `ia/skills/{name}/train-proposal-{YYYY-MM-DD}.md`. Never auto-applied. | train |
| skill-train | Opus consumer + `/skill-train` command. On demand, reads target skill's Per-skill Changelog since last `source: train-proposed` entry, aggregates recurring friction, writes patch proposal. Input: skill self-report entries. | train |

## Multi-scale simulation

| Term | Definition | Spec |
|------|-----------|------|
| Simulation scale | Named level of the sim stack (`CITY`, `REGION`, `COUNTRY`). Enum + `ISimulationModel` contract. | ms |
| Active scale | The single scale currently running its full tick loop. | ms |
| Dormant scale | Any non-active scale. Holds a snapshot + evolution parameters. Does not tick. Evolution applied by its parent-scale entity, not by itself. | ms |
| Child-scale entity | Representation of a dormant child inside its parent. Region holds one per dormant city; country holds one per dormant region. Carries a pending evolution delta layered over the child's last-materialized snapshot + `last_active_at` calendar stamp. | ms |
| Evolution algorithm | Pure function `evolve(snapshot, Δt, params) → snapshot'` fast-forwarding a dormant scale at scale-switch time. Deterministic in MVP. Scale-specific: city, region, country. | ms |
| Evolution parameters | Tunable inputs per scale node: growth coefficients, policy multipliers, RNG seed, and (at region/country) player-authored parameters from the parent scale's UI. | ms |
| Evolution-invariant | State evolution must preserve verbatim. For the city: everything the player actively touched (main road backbone, landmarks, districts, player-assigned budgets, explicit zoning). Evolution may additively create new main roads or density but may not overwrite a player-touched surface. | ms |
| Evolution-mutable | State the algorithm may rewrite: default-generated density, untouched cells, population mix, zoning not explicitly chosen. | ms |
| Parity budget | Max allowed divergence between algorithmic projection and live-sim re-run over the same interval. Measured empirically via playtest. | ms |
| Reconstruction | Materializing a playable live scale state from snapshot + parent entity's pending evolution delta up to "now". Happens at scale-switch time. | ms |
| Procedural scale generation | Creation of a never-visited scale node (city, region) from parent-scale parameters + deterministic seed. | ms |
| Scale switch | Player-driven transition from one active scale to another. Steps: (a) save leaving scale, (b) apply entering scale's pending evolution delta, (c) load entering scale into playable form. MVP UX: semantic zoom + procedural fog mask + per-scale `ScaleToolProvider`. | ms, ms-post |
| Multi-scale save tree | Relational save: main `game_save` table + per-scale tables (`city_nodes`, `region_nodes`, `country_nodes`), each with JSON column for cell data + typed FK columns + `evolution_params jsonb` + `pending_delta jsonb` + `last_active_at`. | ms |
| Neighbor-city stub | Minimum representation of a neighbor city at an interstate exit on the map border. Schema-only `[Serializable]` `NeighborCityStub` — `id` GUID, `displayName`, `borderSide`. No behavior. Paired with `NeighborCityBinding` records tying a stub to the grid exit cell. Part of the parent-scale stub set for the city MVP. | ms |
| City / Region / Country cell | Scale-specific refinements of the generic Cell. Same isometric primitive, sized and semantically typed per scale. | ms |
| Parent-scale stub | Minimum parent-scale representation inside the city MVP: `region_id` + `country_id` references + at least one neighbor-city stub + interstate-border data admitting a region-facing interpretation. | ms |
| Parent region / country id | GUID (string-serialized) on `GameSaveData` identifying the region / country that owns this city. Allocated at new-game init or first legacy-save load via `MigrateLoadedSaveData`. Non-null after migration. Read surface: `GridManager.ParentRegionId` / `ParentCountryId` (read-only, hydrated via `HydrateParentIds`). No runtime consumer in MVP. | persist §Save |
| Scale-switch event bubble-up / constraint push-down | Event and parameter transport across scales, applied at switch time (not continuously). MVP ships both as thin hooks. | ms |
| Player-authored dormant control | At region scale, player sets budget allocation per dormant child city. At country scale, per dormant region. Extended surface is post-MVP. | ms |

## Retired terms — do not use

| Term | Replacement | Note |
|------|-------------|------|
| Urbanization proposal | Urban centroid / Urban growth rings AUTO | Invariants forbid re-enabling. |
| Phase | Stage | 4-level hierarchy (Step > Stage > Phase > Task) collapsed to 2-level per `lifecycle`. |
| Gate | Stage exit criteria | Folded into the Exit subsection of each `### Stage N.M` block. |

## Do not confuse

| A | B | Key difference |
|---|---|----------------|
| Cliff | Map border | Cliff = per-cell vertical drop; map border = outer grid boundary. |
| Rim | Water-shore (land) | Rim fails the surface-height gate; water-shore passes it. |
| Open water | Water-shore (land) | Open water = registered in `WaterMap` (id ≠ 0); water-shore = dry land with shore art. |
| Lake | Sea | Sea is not treated as lake in exclusion rules. |
| Interchange JSON | Save data | Interchange is tooling/config, not part of Load pipeline. |
| Orchestrator document | Project spec | Orchestrator is permanent; project spec is temporary/issue-scoped. |
| skill-train | release-rollout-skill-bug-log | skill-train = self-reported friction; skill-bug-log = user-logged bugs. |

<a id="planned-domain-terms"></a>

## Planned terminology (backlog-backed, non-authoritative)

> Not canonical gameplay. Rows name product directions tracked as open rows in [BACKLOG.md](../../BACKLOG.md). If a reference spec is updated for a feature, the spec wins over this section. Full narrative: [planned-domain-ideas.md](../../docs/planned-domain-ideas.md). Completed rows live in [BACKLOG-ARCHIVE.md](../../BACKLOG-ARCHIVE.md).

| Term | Working definition (intent only) | Trace |
|------|-----------------------------------|-------|
| Geography authoring | In-game or Editor flow to design territory / urban maps with a parameter dashboard (map size, water / forest / height mix, sea / river / lake proportions). Drives geography initialization and reuses the same parameter model for player terraform, basin / elevation tools, water body placement in depressions, and AUTO-driven tools. | Open BACKLOG (gameplay/sim); pg-interchange |
| Urban pole (working name) | A growth anchor for AUTO systems — e.g. employment or desirability hotspot — weighting nearby cells for streets and zoning. Distinct from committed pathfinding: road segments still follow road preparation and geo §10. | Open BACKLOG |
| Multipolar urban growth | Evolution from single urban centroid to multiple urban poles, each with its own growth rings (or equivalent distance field), preserving coherent regional patterns on one map; long-term connurbation between urban masses. | Open BACKLOG |
| Connurbation | Two or more distinct urban areas on the same map recognized as coexisting under shared regional rules (exact criteria TBD). | Open BACKLOG |
| Water volume budget | Not full 3D fluid simulation: a water body holds a volume constraint so expanding basin capacity can lower `S` to conserve mass; constrained basins may raise `S`. Rendering updates water prefab height; optional directional fill animation. | Open BACKLOG |
| Moore-adjacent excavation fill | Excavating a dry cell Moore-adjacent to open water allows that depression to fill from the adjacent water body (rules TBD). | Open BACKLOG |
