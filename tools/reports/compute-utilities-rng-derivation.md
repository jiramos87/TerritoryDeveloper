# RNG derivation for geography initialization

**Status:** Expanded 2026-04-04. Stochastic branches below name their RNG entry and whether output is fixed given the same **master seed** and **GeographyInitParams** interchange (when loaded).

**Related:** **MapGenerationSeed** (`Assets/Scripts/Managers/GameManagers/MapGenerationSeed.cs`), **GeographyManager** (`GeographyManager.cs`), **`geography_init_params`** schema under `docs/schemas/` — do not fork a second DTO shape.

## 1. Master seed

| Step | API | RNG | Deterministic from interchange seed? |
|------|-----|-----|--------------------------------------|
| Session / New Game | `MapGenerationSeed.RollNewMasterSeed` | `UnityEngine.Random.Range` | N — unless `loadGeographyInitParamsFromStreamingAssets` calls `SetSessionMasterSeed(dto.seed)` before terrain/water |
| First Play-in-Editor init | `MapGenerationSeed.EnsureSessionMasterSeed` | defers to `RollNewMasterSeed` if unset | Same as above |
| Interchange load | `GeographyInitParamsLoader` + `SetSessionMasterSeed` | none (assigns integer) | Y for all derived seeds below |

## 2. Derived seeds (`MapGenerationSeed.Derive`)

All use `Derive(masterSeed, salt)` — **deterministic** from `masterSeed` once set.

| Salt (hex) | Method | Consumer |
|------------|--------|----------|
| `0x54455231` (`TER1`) | `GetTerrainProceduralOffsetSeed` | Extended terrain / Perlin-style offsets (`TerrainManager` procedural extension) |
| `0x4D43524F` (`MCRO`) | `GetTerrainMicroLakeNoiseSalt` | Micro-lake roughness noise |
| `0x4D494352` (`MICR`) | micro-lake carve threshold | `GetMicroLakeCarveThreshold` → sparse dip density |
| `0x4C414B45` (`LAKE`) | `GetLakeFillRandomSeed` | **Lake** depression-fill ordering / shuffle in **WaterMap** (`LakeFillSettings.RandomSeed`) |

## 3. Lakes and sea (`WaterManager` / `WaterMap`)

- After terrain builds the height field, `WaterManager.InitializeWaterMap` runs lake generation using **`MapGenerationSeed.GetLakeFillRandomSeed()`** wired into **`LakeFillSettings.RandomSeed`** (see `TerrainManager` / `WaterManager` lake path).
- Lake body ordering, shuffle, and bounded fill use **`System.Random`** seeded from that value — **deterministic** given master seed (and same code path / map size).

## 4. Procedural rivers (FEAT-38)

**File:** `ProceduralRiverGenerator.cs` — **`ProceduralRiverGenerator.Generate(..., System.Random rnd)`**.

**Seeding:** `WaterManager.GenerateProceduralRiversForNewGame` (`WaterManager.cs`):

1. `MapGenerationSeed.EnsureSessionMasterSeed()`
2. `int seed = MapGenerationSeed.GetLakeFillRandomSeed();` (same derived seed as lake fill)
3. `var rnd = new System.Random(seed ^ unchecked((int)0xBADC0DE1));`
4. `ProceduralRiverGenerator.Generate(this, terrainManager, gridManager, rnd);`

So river centerline search, shuffles, and border picks are **deterministic** from master seed **after** XOR with `0xBADC0DE1`, given unchanged terrain/water inputs.

**Monotonicity:** **H_bed** along a river polyline is a **gameplay invariant** (see project invariants / water spec). This document does not duplicate the proof; a future export of centerline + bed heights could be checked by tooling if added to `last-geography-init.json`.

## 5. Forests (`ForestManager`)

**File:** `ForestManager.cs` — **`BuildInitialForestCellsChunkBased`** uses **`UnityEngine.Random.value`** (global Unity RNG) for chunk and cell rolls.

**Determinism:** **Not** solely a function of `MapGenerationSeed` unless the Unity RNG state is explicitly seeded from master seed before forest generation (today it is **not**). Order of prior `Random` calls from terrain/water/Unity internals affects forest placement. Document as **session-dependent** unless/until a FEAT/TECH wires explicit seeding.

## 6. Test river

`WaterManager.GenerateTestRiver` → **`TestRiverGenerator.Generate`** — deterministic geometry from parameters; no extra RNG for the default straight grid path (see `GeographyManager.generateTestRiverOnInit`).

## 7. Harness export

**Editor:** `Territory Developer/Reports/Export Geography Init Report (last-geography-init.json)` → **`GeographyManager.BuildGeographyInitReportJson`** — includes `master_seed`, effective river toggle, forest coverage snapshot, and when interchange load succeeded **`interchange_snapshot_json`** (string: `JsonUtility.ToJson` of **`GeographyInitParamsDto`**, not a nested object). When no interchange was applied, that property is **omitted** from the file (not `""` — `BuildGeographyInitReportJson` strips `JsonUtility`'s empty string for null).

**Validate:** `node tools/scripts/validate-geography-init.mjs` (optional path argument).
