# CityScene runtime loading + seamless scale transition — research, audit, critique, improvement (as of 2026-05)

---

## Findings

### Async scene loading and activation cost

Unity's `SceneManager.LoadSceneAsync` splits scene loading into a background fetch phase and a main-thread activation phase. Community profiling consistently shows that the activation stall — the moment when Unity activates all GameObjects in a scene — is the dominant source of perceived slowness, not the I/O fetch. The activation stall scales linearly with the number of GameObjects: batching static geometry into fewer objects is the single highest-ROI scene optimization. Scenes using URP or HDRP carry an additional 5–6 second black-screen stall at scene activation due to render pipeline setup; Standard pipeline eliminates this delay entirely.

Setting `asyncOperation.allowSceneActivation = false` lets the engine finish I/O (progress reaches 0.9) while keeping the current scene responsive, then flips activation when the game chooses — enabling "load behind a veil" patterns.

- Unity SceneManager.LoadSceneAsync — https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html
- Slow Loading Time - Scene Loading Async - Unity Discussions — https://discussions.unity.com/t/slow-loading-time-scene-loading-async/844813
- Avoiding Hitches When Loading Scenes in Unity - Meta — https://developers.meta.com/horizon/blog/avoiding-hitches-when-loading-scenes-in-unity/

### Addressables and chunk-based world streaming

Unity's Addressables system decouples asset references from build-time inclusion, enabling on-demand load and release by address string or label. The Oculus AssetStreaming sample demonstrates the full pattern: LOD-tiered additive scenes, distance-based load/unload triggers, and reference-counted asset handles that auto-release when all consumers are freed. Each LOD chunk can be a separate addressable scene loaded with `SceneManager.LoadSceneAsync(..., LoadSceneMode.Additive)`.

A real shipping game (Ardenfall) uses 72 additive scenes — 36 full-res terrains + 36 impostor low-res meshes — with a 3×3 cell load radius around the player. Converting from scene-embedded assets to prefabs cut their build time from 3 hours to 3 minutes. Memory pressure is managed by never loading more than the visible buffer.

- How to Choose Runtime Asset Loading Tech - Unity — https://unity.com/resources/runtime-asset-loading-technology-for-rt3d
- Mastering Unity Addressables in 2025 — https://quickunitytips.blogspot.com/2025/11/unity-addressables-guide-2025.html
- Open World Streaming in Unity - Ardenfall — https://ardenfall.com/blog/world-streaming-in-unity
- Unity-AssetStreaming - Oculus Samples — https://github.com/oculus-samples/Unity-AssetStreaming
- Isometric 2d seamless open world scene streaming - Unity Discussions — https://discussions.unity.com/t/isometric-2d-seamless-open-world-scene-streaming/765017

### C# Job System + Burst compiler for terrain generation

Unity's Burst compiler translates a subset of C# (restricted to blittable types and NativeCollections) into LLVM-optimized native machine code with aggressive SIMD vectorization, loop unrolling, and dead-code elimination. The C# Job System parallelizes work across all CPU cores via a worker pool with built-in dependency scheduling.

Benchmarks for procedural terrain specifically:

| Operation | Before | After (Jobs + Burst) | Speedup |
|-----------|--------|----------------------|---------|
| Noise map 256×256 | baseline | 37× faster | 37× |
| Voxel surface generation (16M voxels) | 220 ms / 4.5 FPS | 6.8 ms / 147 FPS | 32× |
| Voxel + caves + ores | ~2,400 ms | ~22 ms | 109× |
| Vector3 addition 1M | baseline | 40× faster | 40× |

The recommended pipeline chains three dependent `IJobParallelFor` jobs per chunk: `NoiseMapJob2D` → `GenerateHeightMapJob` → `Generate3DVolumeJob`. All inter-job data flows through `NativeArray<T>`.

The LogRocket benchmark puts Jobs + Burst at ~6 ms / 80–90 FPS for 1,000 parallel entities versus ~50 ms / 30 FPS for raw `Task`-based async.

- Unity Burst Compiler: Complete Performance Optimization Guide 2025 — https://generalistprogrammer.com/tutorials/unity-burst-compiler-complete-performance-optimization-guide
- Faster Voxel Terrain Generation with Unity Burst Jobs — https://medium.com/@willdavis84/faster-procedural-noise-generation-with-unity-burst-jobs-2bfa0f9aff85
- Performance in Unity: async, await, Tasks vs coroutines, Job System, Burst - LogRocket — https://blog.logrocket.com/performance-unity-async-await-tasks-coroutines-c-job-system-burst-compiler/
- Intro to Jobs/Burst/DoD - Jason Booth — https://medium.com/@jasonbooth_86226/intro-to-jobs-burst-dod-66c6b81c017f

### Frame-spread instantiation and Awaitable

Unity 6 introduces the `Awaitable` class as a zero-allocation coroutine alternative. `Awaitable.NextFrameAsync()` resumes after Update, letting long init loops yield the frame budget without blocking gameplay. `InstantiateAsync` (Unity 2023.2+) offloads object instantiation off the main thread in Unity 6, returning an `AsyncInstantiateOperation<T>` that the caller awaits. Spreading N tile instantiations across frames at a configurable budget-per-frame (e.g. 50 tiles/frame) converts a 30-second sync spike into a 2–3 second animated progressive reveal with no frame drop.

Caution: running thousands of concurrent `Awaitable`-based loops causes scheduling overhead; the right pattern is a single loop that yields once per batch, not one coroutine per object.

- Unity Asynchronous programming with Awaitable — https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html
- Asynchronously Instantiate Objects with InstantiateAsync In Unity — https://giannisakritidis.com/blog/InstantiateAsync/
- Best approach for handling large open world terrain streaming - Unity Discussions — https://discussions.unity.com/t/best-approach-for-handling-large-open-world-terrain-streaming/1710438

### Sprite atlases, tilemap batching, and draw-call reduction

Unity's Tilemap Renderer in Chunk Mode batches entire tile regions into a single draw call. SRP Batcher further merges Tilemap Renderer draw calls with other sprite renderers sharing the same material properties. The key constraint: all sprites in a batch must share one Sprite Atlas. Splitting atlases by usage zone (terrain-only, buildings-only) rather than by type keeps each atlas small enough to avoid loading textures not on screen. Unity 6.3 ships a Sprite Atlas Analyzer that surfaces atlas waste in-Editor.

For extreme scale, GPU-driven tilemap rendering (custom compute shader) can render millions of tiles in a single draw call at 3000 FPS, eliminating per-tile CPU overhead entirely.

- Optimize performance of 2D games with Unity Tilemap — https://unity.com/how-to/optimize-performance-2d-games-unity-tilemap
- Unity Optimize Sprite Atlas usage — https://docs.unity3d.com/6000.2/Documentation/Manual/sprite/atlas/workflow/optimize-sprite-atlas-usage-size-improved-performance.html
- Sprite Atlas Analyzer in Unity 6.3 — https://discussions.unity.com/t/sprite-atlas-analyzer-in-unity-6-3-beta/1683242
- How I render large 3D tilemaps with a single draw call at 3000 FPS — https://blog.paavo.me/gpu-tilemap-rendering/

### ScriptableObject baked data vs JSON deserialization

Unity serializes ScriptableObjects in binary or YAML. Marking large array fields `[PreferBinarySerialization]` stores them as binary assets, which Unity can load in a single IO call at startup — significantly faster than parsing JSON with `JsonUtility` for map-scale data. One mobile developer reported switching from JSON map storage to binary ScriptableObject cut their WebGL load time from several seconds to near-instant. ScriptableObjects are Unity-native assets: they load with the scene or on first reference, with no explicit deserialization step.

- Loading Data in Unity: JSON vs ScriptableObject — https://unity3dperformance.com/index.php/2024/10/30/json-vs-scriptableobjects/

### Object pooling pre-warm strategy

Pre-warming object pools during a loading screen amortizes instantiation cost before the player gains control. Unity's built-in `ObjectPool<T>` (added in Unity 2021) provides a ref-counted pool with configurable pre-allocation. The pattern for grid-scale games: allocate NxN tile GameObjects into the pool at scene load, then pull from pool during `RestoreGrid` rather than calling `Instantiate` per cell. This converts the restore-loop from an O(N²) allocation spike into an O(N²) pool assignment with no GC pressure.

- Unity Object Pool tutorial — https://learn.unity.com/tutorial/introduction-to-object-pooling
- Why does my Unity game take forever to load - Toxigon — https://toxigon.com/improving-unity-loading-times
- Optimizing Scene Loading Performance In Unity Games — https://gamedevfaqs.com/optimizing-scene-loading-performance-in-unity-games/

### Seamless scale-transition UX design

Game designer Radek Koncewicz (Gamedeveloper.com) formalizes the "segue" concept: a transition is seamless when the player's control is never removed and the world appears continuous. The key failures of non-seamless transitions are disorientation (sudden context switch forcing rapid information re-parsing) and helplessness (loss of agency during loading). Modern hardware enables dynamic streaming and shared level geometry that eliminates explicit cut points. Successful examples include Spy Hunter's car-to-boat transformation and Lost Odyssey's cinematic-to-gameplay bridge — both maintain spatial coherence across the boundary.

Applied to city-to-region zoom: the transition is a segue when the RegionScene geometry is already visible behind the camera zoom-out and the city appears as one block within that geometry, never disappearing.

- Smooth Transitions - Game Developer — https://www.gamedeveloper.com/design/smooth-transitions
- Experience Seamless Stunning Scene Transitions - Unity Discussions — https://discussions.unity.com/t/experience-seamless-stunning-scene-transitions/1601209

### Render Texture thumbnail capture for world preview

Unity's `RenderTexture` class combined with a dedicated secondary camera renders any scene view to a texture at any resolution. Setting the secondary camera's `orthographicSize` determines zoom level. At CityScene resolution (zoomed out), a RenderTexture capture of the full city grid can be used as an impostor — a flat 2D thumbnail placed as one "cell" in the RegionScene grid while the actual CityScene content streams in behind it. Runtime preview generators (e.g. `yasirkula/UnityRuntimePreviewGenerator`) extend this to arbitrary prefab thumbnails without scene changes.

- Unity Render a camera view to a Render Texture — https://docs.unity3d.com/6000.3/Documentation/Manual/output-to-render-texture.html
- UnityRuntimePreviewGenerator — https://github.com/yasirkula/UnityRuntimePreviewGenerator

### FindObjectOfType and inspector-wired dependency injection

`FindObjectOfType<T>()` traverses the full scene hierarchy and is documented as expensive. Multiple calls at startup in different `Start()` methods accumulate: if 10 managers each call `FindObjectOfType` once, the total cost is 10 full scene scans. Industry recommendations: wire dependencies via Inspector `[SerializeField]` references set at authoring time, or use a lightweight service locator / ScriptableObject-based injection. Avoiding runtime scene searches entirely removes a non-trivial fraction of init overhead on large scenes.

- Unity FindObjectOfType performance tip - Medium — https://medium.com/@djolexv/unity-tip-avoiding-inefficiency-with-findobjectoftype-02961e4a8a85

### Tween libraries for camera zoom animation

Allocation-free tween libraries (PrimeTween, LeanTween, DOTween) animate `Camera.orthographicSize` with a single line of code and zero GC allocations. PrimeTween benchmarks show 2–5× fewer allocations than DOTween and 0 allocations for standard tween types. Coroutine-based zoom tweens are also common and sufficient for non-critical paths. The camera orthographic-size zoom from city level to region level is a natural animation hook: the zoom-out gesture *is* the transition, with the RegionScene loading additively behind it during the tween duration.

- PrimeTween - high-performance tween library — https://github.com/KyryloKuzyk/PrimeTween

### Cross-cutting observations

- **Dominant:** Addressables + additive scene streaming is the standard pattern for open-world Unity games in 2025–2026. Single-scene architectures are considered legacy for any grid larger than ~200×200.
- **Emerging:** Jobs + Burst for procedural world generation is now the baseline expectation; coroutine-based generators are considered a perf liability. Unity 6's `InstantiateAsync` begins to bridge this gap for instantiation specifically.
- **Emerging:** The "segue" design pattern (transition as gameplay, not pause) is gaining traction as hardware enables background streaming that was previously impossible.
- **Declining:** Loading screens as neutral pauses are falling out of favor; players now expect background loading with progressive reveal or transition-as-gameplay.
- **Recency anchor:** 2026-05. All vendor doc links resolve to Unity 6 / 6.3 documentation.

---

## Audit — current implementation in repo

### Entry points

- `GameBootstrap` (`Assets/Scripts/Managers/GameManagers/GameBootstrap.cs`) — `MonoBehaviour`, runs in `CityScene`. `Start()` calls `FindObjectOfType<GameManager>()`, then launches `ProcessStartIntent()` coroutine. Coroutine yields one frame, reads `GameStartInfo.Mode` (NewGame or Load), invokes `gameManager.CreateNewGame()` or `gameManager.LoadGame(path)`.
- `GameManager` (`Assets/Scripts/Managers/GameManagers/GameManager.cs`) — `MonoBehaviour`, wires `GridManager` + `GameSaveManager` via `FindObjectOfType` in `Start()`. `CreateNewGame()` delegates to `saveManager.NewGame()`; `LoadGame()` delegates to `saveManager.LoadGame(path)`.
- `GeographyManager` (`Assets/Scripts/Managers/GameManagers/GeographyManager.cs`) — `MonoBehaviour`, hub for all geography subsystems. Inspector-wired fields: `terrainManager`, `waterManager`, `forestManager`, `gridManager`, `zoneManager`, `interstateManager`, `regionalMapManager`. Bool toggles control which subsystems fire on init: `generateStandardWaterBodies`, `generateProceduralRiversOnInit`, `generateTestRiverOnInit`, `initializeForestsOnStart`, `loadGeographyInitParamsFromStreamingAssets`.
- `GeographyInitService` (`Assets/Scripts/Managers/GameManagers/GeographyInitService.cs`) — plain C# class. `InitializeGeography()` orchestrates the full synchronous pipeline: interchange load → `RegionalMapManager.InitializeRegionalMap()` → `GridManager.InitializeGrid()` → water pipeline → interstate pipeline (3 placement attempts + deterministic fallback) → forest init → desirability calc → sorting-order recalc → border signs → minimap notify.
- `GridManager` (`Assets/Scripts/Managers/GameManagers/GridManager.cs` + `GridManager.Impl.cs`) — `[DefaultExecutionOrder(-100)]` `MonoBehaviour`. `InitializeGrid()` calls `FindObjectOfType` for 10+ dependencies, then `CreateGrid()` synchronously instantiates all `width × height` cell GameObjects + tile prefabs in a nested double loop. Default grid is 64×64 = 4,096 cells; each cell = 1 `new GameObject` + 1 `Instantiate(tilePrefab)` = 8,192 allocations minimum.
- `TerrainManager` (`Assets/Scripts/Managers/GameManagers/TerrainManager.cs`) — `MonoBehaviour`, holds `HeightMap`. `InitializeHeightMap()` runs terrain generation synchronously.
- `GameBootstrap` → `GameManager` → `GameSaveManager.NewGame()` / `LoadGame()` — load path deserializes a JSON save file with `JsonUtility.FromJson`, then calls `GridManager.RestoreGrid(savedGridData)` which iterates all cells and instantiates prefabs synchronously.

### Data flow

**New game path:**
1. `GameBootstrap.Start()` — 1 frame yield, then → `GameManager.CreateNewGame()`
2. `GameSaveManager.NewGame()` → `GeographyManager.ReinitializeGeographyForNewGame()` → `GeographyInitService.ReinitializeGeographyForNewGame()`
3. `GeographyInitService`: RegionalMap init (sync) → `TerrainManager.InitializeHeightMap()` (sync, HeightMap generated) → water pipeline (sync) → interstate placement loop up to 3×retry + deterministic fallback (sync) → forest init (sync) → sorting-order recalc (full grid scan, sync) → border signs (sync) → minimap notify (sync)
4. `GridManager.InitializeGrid()` called from within `GeographyInitService.InitializeGeography()`: full double-loop over `width × height`, instantiates `new GameObject` per cell + `Instantiate(tilePrefab)` per cell — **all synchronous, all main thread**.

**Load game path:**
1. `GameBootstrap` → `GameManager.LoadGame(path)` → `GameSaveManager.LoadGame(path)`
2. `JsonUtility.FromJson<GameSaveData>(json)` — deserializes full save payload
3. `GridManager.RestoreGrid(savedGridData)` — iterates saved cells, calls `Instantiate` per changed prefab + sets sorting orders
4. `GeographyInitService.LoadGeography(data)` — HeightMap restore → WaterMap restore → Forest restore → desirability → sorting-order recalc → minimap notify

**Load order spec** (`ia/specs/persistence-system.md §Load pipeline`): HeightMap → WaterMap → Grid cells → shore membership sync. Sorting recalc and slope restoration do NOT re-run on load (saved prefabs + `sortingOrder` applied directly from snapshot).

### Constraints

- `GridManager` is `[DefaultExecutionOrder(-100)]` — executes `Awake` before all other managers. No explicit scene-load async ordering.
- Invariant #1 (from `GeographyInitService` doc comment): HeightMap/Cell.height must remain in sync across all write paths.
- Invariant #5 (from `GridManager.Impl.cs` comment): raw `cellArray` access preserved in Impl for save/restore performance paths — enforces that `cellArray` is a flat 2D array, not a container type.
- `GameBootstrap` uses `SceneManagement` but only for reading current scene name; no `LoadSceneAsync` or additive loading present anywhere in the codebase.
- `RegionalMap` (`Assets/Scripts/Core/Geography/RegionalMap.cs`) exists as a 5×5 grid of `TerritoryData`. Player is at center (2,2). No RegionScene exists; `RegionalMapManager` manages border signs and interstate routing within `CityScene` only.
- No `Addressables`, no asset bundles, no async loading primitives in any manager. All asset loading is synchronous via direct `Instantiate` calls.
- `ChunkCullingSystem` (`Assets/Scripts/Managers/GameManagers/ChunkCullingSystem.cs`) — runs in `LateUpdate`, toggles chunk `GameObject` visibility by camera frustum. Chunk size = `chunkSize` (default 16). Culling is runtime-only and does not affect init time.

### Coverage

- `tests/` directory not scanned; no test files were returned for loading, scene transition, or init pipeline.
- `Assets/Scripts/Domains/Testing/Services/HeightIntegrityService.cs` — integrity check for HeightMap sync (Invariant #1).
- `Assets/Scripts/Editor/GeographyInitReportMenu.cs` — Editor menu to run and report geography init; not a runtime test.
- No profiling instrumentation visible in init paths beyond the `Profiler.BeginSample("GeographyInitParams.Load")` guard in `GeographyInitService.ApplyInterchangeAtPipelineStart()`.

---

## Critique — strengths and weaknesses

### Strengths

- **Chunk culling active at runtime.** `ChunkCullingSystem` toggles off-screen chunks in `LateUpdate` every frame, reducing draw calls after init. System is already extracted from `GridManager` as a separate class — well-isolated.
- **Geography pipeline cleanly extracted.** `GeographyInitService` separates the pipeline orchestration from the `GeographyManager` hub, giving a single entry-point class per init path (new game, load, reinit). Modifying init ordering does not require touching the hub.
- **Load-path skips redundant work.** The load spec (`persistence-system.md §Load pipeline`) explicitly skips slope restoration and sorting recalc, restoring directly from snapshot values — partial mitigation of the per-cell overhead.
- **RegionalMap data model exists.** `RegionalMap` + `TerritoryData` (5×5 grid, player at center) gives the data skeleton for a RegionScene without new domain modeling.
- **Service-level atomization underway.** Multiple hub managers already thinned (`GridManager`, `TerrainManager`). Service layer (`Domains/`) established. New async patterns can be introduced at service level without hub changes.

### Weaknesses

- **All init is synchronous and main-thread-blocked.** `GeographyInitService.InitializeGeography()` runs the entire pipeline — grid creation, height generation, water pipeline, forest init, interstate placement, sorting recalc — without a single `yield` or Job dispatch. On a 64×64 grid this produces the observed ~30-second stall.
- **`CreateGrid()` is a double-loop of `Instantiate` calls with no yield.** `GridManager.Impl.cs` line 90–119: nested `for(x)` / `for(y)` loop instantiates `width × height` GameObjects + `width × height` tile prefabs synchronously. For a 64×64 grid = 8,192 `Instantiate` calls in one frame. Any larger grid multiplies the stall proportionally. §Audit · Entry points · GridManager.Impl.cs
- **Multiple `FindObjectOfType` calls at init time.** `GameBootstrap.Start()`, `GameManager.Start()`, and `GridManager.InitializeGrid()` (10+ calls) each traverse the full scene hierarchy. On a scene with thousands of GameObjects, these scans accumulate. §Audit · Entry points · GameBootstrap / GameManager / GridManager.Impl.cs
- **No async loading primitives.** No `LoadSceneAsync`, no Addressables, no additive scenes, no `InstantiateAsync`, no Job dispatch. The architecture assumes a single synchronous scene with all content instantiated at startup. §Audit · Constraints
- **RegionScene does not exist.** No scene, no scene-loading code, no streaming budget, no RegionScene prefab layout. The `RegionalMap` data model exists but has no rendering surface. Transition to region scale requires building the scene and all navigation code from scratch. §Audit · Constraints
- **No profiling instrumentation in hot init paths.** The `Profiler.BeginSample` guard exists only in `ApplyInterchangeAtPipelineStart()`. The actual bottleneck paths — `CreateGrid()`, `InitializeHeightMap()`, water pipeline, `ReCalculateSortingOrderBasedOnHeight()` — carry no profiler markers, making bottleneck measurement manual. §Audit · Coverage
- **Interstate placement has retry loop with no async escape.** `RunInterstatePipeline()` attempts placement up to 3 times + a deterministic fallback, all synchronously. On pathological maps this loop runs 4× full-graph traversals before yielding. §Audit · Data flow

---

## Exploration — 18 ways to improve

1. **Frame-Spread Grid Instantiation via Awaitable.NextFrameAsync.** Addresses §Critique · Weaknesses · "`CreateGrid()` is a double-loop of `Instantiate` calls with no yield." Convert `GridManager.CreateGrid()` from a single nested loop to a coroutine or `async` method that batches N tiles per frame (configurable budget, e.g. 64 tiles/frame), calling `await Awaitable.NextFrameAsync()` between batches. A 64×64 grid at 64 tiles/frame completes in 64 frames (~1 second at 60 FPS) while the scene remains interactive. A loading bar driven by `tilesCreated / totalTiles` gives progress feedback. Source: §Findings · Frame-spread instantiation and Awaitable — https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html

2. **Jobs + Burst Parallel HeightMap Generation.** Addresses §Critique · Weaknesses · "All init is synchronous and main-thread-blocked." Port `TerrainManager.InitializeHeightMap()` to a two-job pipeline: `NoiseMapJob` (`IJobParallelFor` over grid cells, fills `NativeArray<float>`) → `HeightQuantizeJob` (`IJobParallelFor`, converts noise to integer height levels). Schedule from main thread; complete before `CreateGrid()` reads height values. Benchmarks show 32–109× speedup for comparable noise+height pipelines; a 64×64 HeightMap currently consuming several seconds would finish in <100 ms. Source: §Findings · C# Job System + Burst compiler for terrain generation — https://medium.com/@willdavis84/faster-procedural-noise-generation-with-unity-burst-jobs-2bfa0f9aff85

3. **Inspector-Wired Dependency Injection to Replace FindObjectOfType Scans.** Addresses §Critique · Weaknesses · "Multiple `FindObjectOfType` calls at init time." Assign all `[SerializeField]` references in `GameBootstrap`, `GameManager`, and `GridManager.InitializeGrid()` via the Unity Inspector at scene authoring time. Remove all runtime `FindObjectOfType` calls that fire during init. At scene sizes of thousands of GameObjects, eliminating 10+ full-scene traversals removes a non-trivial latency cluster from frame 1. Source: §Findings · FindObjectOfType and inspector-wired dependency injection — https://medium.com/@djolexv/unity-tip-avoiding-inefficiency-with-findobjectoftype-02961e4a8a85

4. **Pre-Warmed Object Pool for Tile GameObjects.** Addresses §Critique · Weaknesses · "`CreateGrid()` is a double-loop of `Instantiate` calls with no yield." Introduce a `TilePool` that pre-allocates a fixed set of tile `GameObject` instances during the loading phase (before player control is granted), using Unity's built-in `ObjectPool<T>`. `CreateGrid()` and `RestoreGrid()` pull from pool instead of calling `Instantiate`. Pool pre-warm can itself be frame-spread via a coroutine. Eliminates GC pressure from repeated alloc/dealloc during grid resets. Source: §Findings · Object pooling pre-warm strategy — https://learn.unity.com/tutorial/introduction-to-object-pooling

5. **Binary ScriptableObject Baked Map Cache with PreferBinarySerialization.** Addresses §Critique · Weaknesses · "No async loading primitives." Introduce a `BakedCityMap` ScriptableObject carrying `[PreferBinarySerialization]` arrays for HeightMap, initial tile types, and water membership. At new-game creation, bake and cache these arrays. On subsequent loads, read from the ScriptableObject asset instead of re-running procedural generation. Unity loads binary ScriptableObjects via a single file read with no deserialization step — reported as near-instant on mobile versus multi-second JSON parsing. Source: §Findings · ScriptableObject baked data vs JSON deserialization — https://unity3dperformance.com/index.php/2024/10/30/json-vs-scriptableobjects/

6. **Additive RegionScene Pre-Load Behind the Zoom Tween.** Addresses §Critique · Weaknesses · "RegionScene does not exist." Create `RegionScene.unity` as a stub with a `RegionGridManager` that uses an `N×M` coarse grid (1 cell = 1 city territory). On player zoom-out trigger, immediately begin `SceneManager.LoadSceneAsync("RegionScene", LoadSceneMode.Additive)` with `allowSceneActivation = false`. During the camera zoom-out animation (1–2 second tween), the engine fetches scene assets in the background. Activate the scene at `asyncOp.progress >= 0.9`. Player never sees a loading screen. Source: §Findings · Async scene loading and activation cost — https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html

7. **Orthographic Zoom Tween as the Transition Segue.** Addresses §Critique · Weaknesses · "RegionScene does not exist" (UX dimension). Implement zoom-out as an animated `Camera.orthographicSize` tween using PrimeTween (0 GC allocations) from city-level zoom to region-level zoom over 1.5–2.0 seconds. During the tween, the additive RegionScene loads behind the camera. The zoom animation *is* the transition — no cut, no fade, no loading screen. At tween end, `CityScene` content is replaced by the city-as-block impostor and unloaded. Source: §Findings · Tween libraries for camera zoom animation + Seamless scale-transition UX design — https://github.com/KyryloKuzyk/PrimeTween and https://www.gamedeveloper.com/design/smooth-transitions

8. **RenderTexture City Thumbnail as RegionScene Impostor.** Addresses the transition coherence gap created by the zoom-out segue. At zoom-out trigger (before the tween starts), capture a `RenderTexture` of the city viewed from above at the target region-scale orthographic size. Place this texture as a sprite on the player's RegionScene cell. During the zoom tween, the city appears to shrink into that cell. Dispose the `RenderTexture` after RegionScene is fully active. This gives the illusion that the region "was always there" — the city tile in the region map matches the actual city layout. Source: §Findings · Render Texture thumbnail capture for world preview — https://docs.unity3d.com/6000.3/Documentation/Manual/output-to-render-texture.html

9. **Chunk-Based Additive Streaming for RegionScene Cells.** Addresses §Critique · Weaknesses · "No async loading primitives" (at region scale). Model each RegionScene cell as a separate addressable scene. As the camera pans through the region, load the 3×3 neighborhood additively and unload cells that leave a configurable unload radius. The Ardenfall pattern (72 additive scenes, 3×3 load radius) is directly applicable to a 5×5 `RegionalMap`. Each neighbor-city cell can stream its low-res impostor independently. Source: §Findings · Addressables and chunk-based world streaming — https://ardenfall.com/blog/world-streaming-in-unity

10. **Sprite Atlas Partitioned by Terrain Zone for Draw-Call Collapse.** Addresses §Critique · Weaknesses · "All init is synchronous and main-thread-blocked" (texture cost dimension). Create zone-partitioned Sprite Atlases: `Atlas_Terrain` (grass, cliff, slope tiles), `Atlas_Water` (sea, river tiles), `Atlas_Buildings` (residential, commercial, industrial). Tilemap Renderer Chunk Mode + SRP Batcher can then batch each zone's draw calls to 1–3 total draw calls, reducing GPU-side init and runtime overhead. Use the Unity 6.3 Sprite Atlas Analyzer to identify atlas waste before and after. Source: §Findings · Sprite atlases, tilemap batching, and draw-call reduction — https://docs.unity3d.com/6000.2/Documentation/Manual/sprite/atlas/workflow/optimize-sprite-atlas-usage-size-improved-performance.html

11. **Profiler Marker Instrumentation on All Init Hot Paths.** Addresses §Critique · Weaknesses · "No profiling instrumentation in hot init paths." Add `Profiler.BeginSample` / `EndSample` guards (matching the existing pattern in `ApplyInterchangeAtPipelineStart`) to: `CreateGrid()`, `InitializeHeightMap()`, `InitializeWaterMap()`, `InitializeForestMap()`, `ReCalculateSortingOrderBasedOnHeight()`, `RestoreGrid()`. Once markers are present, Unity Profiler pinpoints which subsystem drives the 30-second stall. Without this data, all other optimizations are prioritized by assumption rather than measurement. Source: §Findings · Async scene loading and activation cost (Meta hitch analysis) — https://developers.meta.com/horizon/blog/avoiding-hitches-when-loading-scenes-in-unity/

12. **Async Interstate Placement with Job-Based Pathfinding.** Addresses §Critique · Weaknesses · "Interstate placement has retry loop with no async escape." Port `GridPathfinder` used by `InterstateManager` to a `IJob` + Burst-compiled BFS/A* over a `NativeArray<bool>` passability map. Schedule the job off-frame; `GeographyInitService.RunInterstatePipeline()` becomes a coroutine that awaits job completion. Eliminates up to 4 synchronous full-graph traversals from the main thread. Source: §Findings · C# Job System + Burst compiler for terrain generation — https://blog.logrocket.com/performance-unity-async-await-tasks-coroutines-c-job-system-burst-compiler/

13. **Progressive Reveal Loading Veil.** Addresses §Critique · Weaknesses · "All init is synchronous and main-thread-blocked" (UX dimension). Introduce a `LoadingVeil` canvas overlay (simple animated image or progress bar) that activates at scene start and deactivates when `GeographyManager.IsInitialized` is true. Frame-spread instantiation (Improvement 1) makes the veil optional after a few seconds; the veil hides the partial grid while it builds. Combined with Improvement 1, the player sees: loading veil → progressive tile pop-in (veil fades) → playable game. Source: §Findings · Async scene loading and activation cost — https://toxigon.com/improving-unity-loading-times

14. **Water Pipeline Async Refactor with Coroutine Yield Points.** Addresses §Critique · Weaknesses · "All init is synchronous and main-thread-blocked." `RunWaterPipeline()` calls `InitializeWaterMap()`, then optionally `GenerateProceduralRiversForNewGame()`. River generation (`ProceduralRiverGenerator.cs`) is likely the slowest water step. Refactor `RunWaterPipeline()` as a coroutine, yielding between `InitializeWaterMap()` and river generation. River generation itself can yield per river segment. Keeps the main thread responsive during what is likely a multi-second waterway construction loop. Source: §Findings · Frame-spread instantiation and Awaitable — https://docs.unity3d.com/6000.3/Documentation/Manual/async-await-support.html

15. **RegionScene Neighbor-City Procedural Stub Rendering.** Addresses the "region was always there" vision. For each `TerritoryData` in the `RegionalMap` (not the player city), generate a low-fidelity isometric stub: a flat 1×1 cell with a pre-baked sprite representing a generic city silhouette keyed by territory type. During zoom-out, these stubs are already placed in the RegionScene grid — the region looks populated before any real city data loads. Uses `UnityRuntimePreviewGenerator` or a Burst-generated thumbnail from procedural noise seeded by `TerritoryData.seed`. Source: §Findings · Render Texture thumbnail capture for world preview — https://github.com/yasirkula/UnityRuntimePreviewGenerator

16. **allowSceneActivation Gate for Zero-Pop Transition.** Addresses the visible "pop" at scene activation during zoom-out. When loading `RegionScene` additively, set `asyncOp.allowSceneActivation = false`. Activate only when: (a) `asyncOp.progress >= 0.9`, AND (b) the camera zoom tween is at the point where the city scale matches the region-cell scale. This ensures RegionScene content appears exactly at the visual moment the city-as-thumbnail fills the region-cell slot — no pop, no black flash. Source: §Findings · Async scene loading and activation cost — https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html

17. **Forest Init Lazy-Load After Playable State.** Addresses §Critique · Weaknesses · "All init is synchronous and main-thread-blocked." `initializeForestsOnStart` toggle on `GeographyManager` already exists. If false, forests are omitted from startup. Extend: defer forest init to a post-playable coroutine that runs after `GridManager.isInitialized` is true, placing forest tiles frame-spread (N trees per frame) while the player already has control. Forests are cosmetic at game start — visibility is not gated on their presence. Source: §Findings · Frame-spread instantiation and Awaitable — https://giannisakritidis.com/blog/InstantiateAsync/

18. **Tilemap Renderer Migration for Terrain Base Layer.** Addresses §Critique · Weaknesses · "`CreateGrid()` is a double-loop of `Instantiate` calls with no yield" (long-term arch). The current approach instantiates a `GameObject` per cell. Unity's `Tilemap` component manages N×M tiles as data entries in a single component, rendered in Chunk Mode with a single draw call per atlas. Migrating the base terrain layer (grass, cliff, slope tiles) to a `Tilemap` eliminates `width × height` GameObjects from the scene hierarchy — replacing 4,096+ scene objects with tile data entries. Reduces both init time and runtime `Update()` overhead. Source: §Findings · Sprite atlases, tilemap batching, and draw-call reduction — https://unity.com/how-to/optimize-performance-2d-games-unity-tilemap

---

## Approaches

### Approach A — Quick Wins (no architecture change)

Bundles proposals: 3, 4, 11, 13, 17.

Core strategy: remove init drag with zero structural risk. Wire deps via Inspector (replaces `FindObjectOfType`), pre-warm tile pool, add profiler markers to all hot paths, add a loading veil, defer forest init post-playable. All changes are contained within existing classes; no new systems, no new scenes, no async primitives.

- Proposals: #3 (Inspector DI), #4 (TilePool), #11 (profiler markers), #13 (loading veil), #17 (lazy forest).
- Effort: ~1 sprint.
- Outcome: faster perceived startup, instrumented hot paths for data-driven follow-up.

### Approach B — Async Init Pipeline (single-scene, no RegionScene)

Bundles proposals: 1, 2, 5, 12, 14.

Core strategy: convert the synchronous `GeographyInitService` pipeline to a frame-yielding async pipeline — no new scenes required. Frame-spread `CreateGrid()` via `Awaitable.NextFrameAsync`, port HeightMap generation to Jobs+Burst, async water+interstate pipelines, binary ScriptableObject baked cache on repeat loads. Eliminates the ~30-second main-thread stall. Supersedes Approach A (includes its profiler markers as a prerequisite).

- Proposals: #1 (frame-spread grid), #2 (Jobs+Burst HeightMap), #5 (binary SO cache), #12 (async interstate), #14 (async water pipeline).
- Effort: ~2–3 sprints.
- Outcome: init stall drops to <5 seconds; load path near-instant on repeat.

### Approach C — Seamless Zoom Transition + RegionScene (new scene, no streaming)

Bundles proposals: 6, 7, 8, 15, 16.

Core strategy: build `RegionScene.unity` and wire the zoom-out camera tween as the transition segue. Additive async load of RegionScene behind the zoom animation; `allowSceneActivation` gate eliminates pop. RenderTexture thumbnail placed as city impostor in region cell. Neighbor stubs pre-placed for populated look. Depends on Approach B (async init) so CityScene startup is no longer blocking before zoom-out becomes meaningful. Requires RegionScene authoring from scratch.

- Proposals: #6 (additive RegionScene load), #7 (orthographic zoom tween), #8 (RenderTexture impostor), #15 (neighbor stubs), #16 (allowSceneActivation gate).
- Effort: ~2–3 sprints (after B).
- Outcome: seamless city→region transition with no loading screen.

### Approach D — Full Streaming Architecture (Addressables + Tilemap migration)

Bundles proposals: 9, 10, 18.

Core strategy: long-term architectural refactor. Migrate terrain base layer to `Tilemap` (eliminates per-cell GameObjects), partition Sprite Atlases by zone for draw-call collapse, adopt Addressables + additive-scene streaming for the RegionScene 5×5 grid. Builds on B + C; replaces the current per-GameObject scene representation entirely. Highest ROI at scale but highest structural risk.

- Proposals: #9 (Addressables chunk streaming), #10 (zone-partitioned sprite atlases), #18 (Tilemap migration).
- Effort: ~3–4 sprints (after B + C).
- Outcome: scalable beyond 64×64; region streaming with memory budget; GPU-side draw-call floor.

---

## Recommendation

TBD — see design expansion below.

---

### Conflicts with locked decisions

No active `arch_decisions` rows were returned by `arch_decision_list`. No conflicts detected against the 18 proposals.

---

## Design Expansion — Performance Quick Wins (Approach A)

### Chosen Approach

Approach A — Quick Wins. Proposals: #3 (Inspector DI), #4 (TilePool pre-warm), #11 (profiler markers), #13 (loading veil), #17 (lazy forest). ~1 sprint. Synchronous only — no async/coroutine primitives added in this plan. Hub files (inspector-attached MonoBehaviours) extended in-place only; all new logic in `Domains/{X}/Services/` or `Managers/GameManagers/` helper classes.

### Architecture

**New services / classes:**

| Class | Path | Role |
|---|---|---|
| `GridInitDependencyBinder` | `Domains/Grid/Services/GridInitDependencyBinder.cs` | Validates inspector-wired refs on `GridManager` at init; warns for any still-null slots before `InitializeGrid()` runs. No FindObjectOfType calls. |
| `TilePool` | `Domains/Grid/Services/TilePool.cs` | `ObjectPool<GameObject>` wrapper. Pre-warms `width × height` tile instances before `CreateGrid()`. `CreateGrid` pulls from pool; `RestoreGrid` returns to pool before re-pulling. |
| `LoadingVeilController` | `Domains/Geography/Services/LoadingVeilController.cs` | Canvas overlay activated at `CityScene` start, deactivated when `GeographyManager.IsInitialized = true`. Optional progress float exposed for future progress-bar widget. |

**Hub extensions (in-place, no rename/move):**

| Hub | Extension |
|---|---|
| `GameBootstrap` | Swap `FindObjectOfType<GameManager>()` → `[SerializeField] GameManager gameManager` inspector field. Null-check with editor warning kept. |
| `GameManager` | Swap both `FindObjectOfType` calls → `[SerializeField]` inspector fields for `GridManager` + `GameSaveManager`. |
| `GridManager` / `GridManager.Impl.cs` | 13 `FindObjectOfType` guards → all converted to null-check only (Inspector wires the refs). `GridInitDependencyBinder.Validate(this)` call added at top of `InitializeGrid()`. `CreateGrid()` calls `TilePool.Get()` instead of `Instantiate`. `Profiler.BeginSample` / `EndSample` added to `CreateGrid`, `RestoreGrid`, `SetTileSortingOrder` hot loops. |
| `GeographyInitService` | `Profiler.BeginSample` guards added to `InitializeGrid()` call site, `RunWaterPipeline()`, `RunInterstatePipeline()`, `InitializeForestMap()`, `ReCalculateSortingOrderBasedOnHeight()`. `initializeForestsOnStart = false` path extended: defer forest init via `GeographyManager.StartCoroutine(DeferredForestInit())` after `IsInitialized = true`. |

**Profiler marker names (match existing `ApplyInterchangeAtPipelineStart` convention):**

```
GeographyInitService.CreateGrid
GeographyInitService.RestoreGrid
GeographyInitService.RunWaterPipeline
GeographyInitService.RunInterstatePipeline
GeographyInitService.InitializeForestMap
GeographyInitService.ReCalculateSortingOrderBasedOnHeight
```

### Red-Stage Proof — Stage 1 (Inspector DI + Profiler Markers)

```python
# Arrange
gm = MockGridManager()
gm.zoneManager = None   # inspector not wired
gm.uiManager = None

# Act
binder = GridInitDependencyBinder()
result = binder.validate(gm)

# Assert — null inspector refs logged, no FindObjectOfType called
assert result.missing_count == 2
assert "zoneManager" in result.missing
assert find_object_of_type_call_count == 0

# Profiler marker present in output
profiler_samples = capture_profiler_samples(lambda: gm.initialize_grid())
assert "GeographyInitService.CreateGrid" in profiler_samples
```

### Red-Stage Proof — Stage 2 (TilePool)

```python
# Arrange
pool = TilePool(prefab=mock_tile, pre_warm_count=64*64)
pool.pre_warm()

initial_alloc_count = get_gc_alloc_count()

# Act — CreateGrid pulls from pool, not Instantiate
grid = MockGridManager(tile_pool=pool)
grid.create_grid(width=64, height=64)

post_alloc_count = get_gc_alloc_count()

# Assert — zero new allocations for tile GOs beyond pool bootstrap
assert post_alloc_count - initial_alloc_count == 0
assert pool.active_count == 64 * 64
```

### Red-Stage Proof — Stage 3 (Loading Veil + Lazy Forest)

```python
# Arrange
veil = LoadingVeilController()
geo_manager = MockGeographyManager(is_initialized=False)
veil.bind(geo_manager)

# Act — veil active at start
assert veil.is_active == True

# Simulate init complete
geo_manager.is_initialized = True
veil.on_geography_initialized()

assert veil.is_active == False

# Forest deferred: not called during InitializeGeography
geo_manager.initialize_geography(forests_on_start=False)
assert forest_init_call_count == 0

# Forest fires post-init coroutine
geo_manager.run_deferred_forest_init()
assert forest_init_call_count == 1
```

### Subsystem Impact

| Subsystem | Impact | Invariants |
|---|---|---|
| `GridManager` / `GridManager.Impl.cs` | Inspector DI + TilePool integration + profiler markers. Hub extended in-place. | Inv #5 (cellArray carve-out preserved), Inv #6 (new logic in services, not hub body). |
| `GameBootstrap` | 1 field: `[SerializeField] GameManager`. FindObjectOfType removed. | Hub not renamed/moved. |
| `GameManager` | 2 fields: `[SerializeField] GridManager`, `GameSaveManager`. FindObjectOfType removed. | Hub not renamed/moved. |
| `GeographyInitService` | Profiler guards + deferred forest path. No structural change. | Inv #1 preserved (no height write paths touched). |
| CityScene.unity | Inspector slot fills required for `GameBootstrap.gameManager`, `GameManager.gridManager`, `GameManager.saveManager`. Existing hub slots already wired for `GridManager` deps — validation only pass. | scene contract |
| `TilePool` | New service under `Domains/Grid/Services/`. Zero existing code paths modified beyond `CreateGrid`. | — |
| `LoadingVeilController` | New service under `Domains/Geography/Services/`. Requires UI Canvas GameObject in CityScene. | — |

**Invariants flagged:** #1 (HeightMap sync — not touched, preserved), #5 (cellArray carve-out — preserved in Impl), #6 (no new responsibilities added to GridManager hub body).

### Implementation Points

**Stage 1 — Inspector DI + profiler markers (days 1–2):**
1. Add `[SerializeField]` fields to `GameBootstrap` + `GameManager`. Remove `FindObjectOfType` calls.
2. `GridInitDependencyBinder.cs` — validates all 13 `GridManager` inspector refs; `Debug.LogWarning` per null slot.
3. Call `binder.Validate(this)` at top of `GridManager.InitializeGrid()` (before null-guard pattern).
4. Add 6 `Profiler.BeginSample/EndSample` pairs to `GeographyInitService`.
5. Wire Inspector slots in `CityScene.unity` for newly serialized fields (Unity Editor step — note in task).
6. `npm run unity:compile-check` after each file edit.

**Stage 2 — TilePool (days 3–4):**
1. `TilePool.cs` — `ObjectPool<GameObject>` wrapper, `PreWarm(int count, GameObject prefab)`, `Get()`, `Return(GameObject)`.
2. `CreateGrid()` in `GridManager.Impl.cs` — replace `Instantiate(tilePrefab, ...)` with `tilePool.Get(tilePrefab)`. Set position/parent after get.
3. `RestoreGrid()` — `tilePool.Return(existingTile)` before re-getting. Match on `cellArray[x,y]` tile child.
4. Pool pre-warm triggered from `GeographyInitService.InitializeGeography()` before `gridManager.InitializeGrid()`.
5. Null-guard: if pool not pre-warmed, fallback to `Instantiate` with `Debug.LogWarning`.

**Stage 3 — Loading veil + lazy forest (days 5):**
1. `LoadingVeilController.cs` — `MonoBehaviour`, auto-activates Canvas overlay on `Awake`, listens to `GeographyManager.OnInitialized` event (or polls `IsInitialized` in `Update` at minimal cost).
2. `GeographyManager` — expose `System.Action OnGeographyInitialized` event, fire in setter of `IsInitialized = true`.
3. `DeferredForestInit()` coroutine in `GeographyInitService`: `yield return null` × 2, then `forestManager.InitializeForestMap()`.
4. `LoadingVeilController` added to `CityScene.unity` as child Canvas.

### Examples

**Before (GameBootstrap):**
```csharp
void Start() {
    gameManager = FindObjectOfType<GameManager>(); // full scene scan
    StartCoroutine(ProcessStartIntent());
}
```

**After (GameBootstrap):**
```csharp
[SerializeField] private GameManager gameManager; // wired in Inspector

void Start() {
    if (gameManager == null) Debug.LogWarning("[GameBootstrap] gameManager not wired");
    StartCoroutine(ProcessStartIntent());
}
```

**TilePool pull in CreateGrid:**
```csharp
// Before
GameObject zoneTile = Instantiate(tilePrefab, gridCell.transform.position, Quaternion.identity);

// After
GameObject zoneTile = _tilePool != null
    ? _tilePool.Get(tilePrefab, gridCell.transform.position)
    : Instantiate(tilePrefab, gridCell.transform.position, Quaternion.identity);
```

### Review Notes

NON-BLOCKING:
- `ChunkCullingSystem` already extracted; no changes needed.
- `GridManager.Impl.cs` has `// long-file-allowed` annotation — confirm annotation remains after edits.
- CityScene.unity Inspector wiring is a manual Editor step, not automatable by agent; must be noted in task descriptions.
- `LoadingVeilController` needs a Canvas prefab stub in `Assets/UI/Prefabs/` — create minimal stub (one Image component, white overlay, alpha fades).

### Expansion metadata

- Date: 2026-05-13
- Model: claude-sonnet-4-6
- Approach selected: A — Quick Wins (Proposals #3, #4, #11, #13, #17)
- Blocking items resolved: 0

---

## Design Expansion — RegionScene + Zoom (Approach C + D Deferred)

### Chosen Approach

Two plans derived from this expansion:

1. **`region-scene-prototype`** — New plan. Stage 1 = design-explore seed + UI grill. Gated on `ui-toolkit-migration` completion (UI Toolkit must be stable before RegionScene UI is authored). Scope: `RegionScene.unity` with 64×64 region-cell grid (same resolution as CityScene), `RegionGridManager` hub, city footprint mapping (32×32 city-cells = 1 region-cell, so 64×64 city = 2×2 region-cells anchored at player city center), neighbor regions, basic UI (Road/Forest/Bulldozer tools + new Found City tool, HUD bar, toolbar, picker, region mini-map), human-made region-cell prefabs (grass, slopes, water slopes). Full gameplay + UI grilling required before implementation. Seed doc: `docs/explorations/region-scene-prototype.md`.

2. **`city-region-zoom-transition`** — New plan. Approach C (Proposals #6, #7, #8, #15, #16). Gated on `region-scene-prototype` done. Scope: orthographic zoom tween as segue, additive scene load behind tween, `allowSceneActivation` gate, RenderTexture impostor, neighbor stubs.

**Approach D (Proposals #9, #10, #18) — Deferred.** Addressables chunk streaming + Tilemap migration = long-term arch refactor; requires Approach B (async init) complete first. Not planned now.

**Constraint inherited:** Hub C# files (inspector-attached) not renamed/moved/deleted. `RegionGridManager` = new hub, new file, new inspector attachment. No existing hubs modified beyond extension points.

### Architecture — region-scene-prototype

> ⚠️ **Superseded by user corrections (2026-05-13).** Full architecture defined in `docs/explorations/region-scene-prototype.md` (seed doc). Summary of corrections:
> - Grid is 64×64 region-cells, NOT 5×5 territories.
> - Mapping rule: 32×32 city-cells = 1 region-cell. 64×64 city → 2×2 region-cells, anchor at (0,0) of that 2×2 area.
> - Player city spans multiple cells; neighbors are full regions, not stubs.
> - UI: Road tool, Forest tool, Bulldozer, Found City (new), HUD bar, toolbar, picker, region mini-map.
> - Region-cells use human-made prefabs (grass, slopes, water slopes) — not procedural sprites.
> - Full gameplay + UI grilling by design-explore agent required before architecture is locked.

See `docs/explorations/region-scene-prototype.md` for the authoritative seed.

### Architecture — city-region-zoom-transition

**New components:**

| Class | Path | Role |
|---|---|---|
| `ZoomTransitionController` | `Domains/Camera/Services/ZoomTransitionController.cs` | Orchestrates full city→region transition. Triggers: (1) `CityThumbnailCapture.Capture()`, (2) start `LoadSceneAsync("RegionScene", Additive)` with `allowSceneActivation = false`, (3) tween `Camera.orthographicSize` city→region via PrimeTween, (4) gate activation at `asyncOp.progress >= 0.9` AND tween at region-cell scale, (5) activate RegionScene, (6) unload CityScene async. |
| `RegionToCityTransitionController` | `Domains/Camera/Services/RegionToCityTransitionController.cs` | Reverse path: zoom-in from region cell back to CityScene. Loads CityScene additively, tween zoom-in, gate on progress, activate, unload RegionScene. |

**Zoom tween parameters:**

- Library: PrimeTween (`com.kyrylokuzyk.primetween`) — 0 GC alloc standard tweens.
- `orthographicSize` from: city-level value (e.g. 8–12 units) → region-level value (e.g. 30–50 units).
- Duration: 1.8 seconds (configurable in `ZoomTransitionController` inspector).
- Easing: `Ease.InOutCubic`.
- `allowSceneActivation = false` set immediately after `LoadSceneAsync` call; activation fires when both tween progress ≥ 0.8 AND `asyncOp.progress >= 0.9`.

### Red-Stage Proof — Stage 1 (RegionScene Stub + RegionGridManager)

```python
# Arrange
region_scene = load_scene("RegionScene")
rgm = find_component(region_scene, "RegionGridManager")

# Assert — hub present, 5x5 grid cells rendered
assert rgm is not None
assert rgm.cell_count == 25   # 5 × 5
assert rgm.city_cell_slot_empty == True   # [2,2] awaiting thumbnail

# CityThumbnailCapture
capture = CityThumbnailCapture(city_camera=mock_cam)
sprite = capture.capture(orthographic_size=40)
assert sprite is not None
assert sprite.texture.width > 0
```

### Red-Stage Proof — Stage 2 (Zoom Transition)

```python
# Arrange
controller = ZoomTransitionController()
controller.city_ortho_size = 10
controller.region_ortho_size = 40
controller.tween_duration = 1.8

load_op = MockAsyncOperation(progress=0.0)

# Act — trigger transition
controller.begin_zoom_out(load_op)
simulate_time(1.0)   # mid-tween

assert camera.orthographic_size > 10  # zoom growing
assert load_op.allow_activation == False  # gate still closed

simulate_time(0.9)   # asyncOp hits 0.9 + tween at 0.8+
assert load_op.allow_activation == True  # gate opens
```

### Red-Stage Proof — Stage 3 (RegionScene Neighbor Stubs)

```python
# Arrange
map_data = RegionSceneStub(territory_data=mock_5x5_grid())
renderer = RegionCellRenderer(map_data=map_data)

# Act
renderer.render_all()

# Assert — 24 neighbor cells have stub sprites, city slot empty
stubs = [c for c in renderer.cells if c.position != (2,2)]
assert len(stubs) == 24
assert all(c.sprite is not None for c in stubs)
city_cell = renderer.get_cell(2, 2)
assert city_cell.sprite is None   # awaiting thumbnail
```

### Subsystem Impact

| Subsystem | Impact | Invariants |
|---|---|---|
| `RegionGridManager` | New hub file. New inspector attachment in `RegionScene.unity`. Does not modify any existing hub. | Hub constraint: new file, new scene — no existing inspector wiring touched. |
| `ZoomTransitionController` | New service in `Domains/Camera/Services/`. Calls `Camera.orthographicSize` setter only (no CameraController hub modification). | Hub not renamed/moved. `CameraController` not modified. |
| `CameraController` (existing hub) | Extended only: expose `public float OrthographicSize` property (get/set passthrough to `Camera.main.orthographicSize`) for `ZoomTransitionController` consumption. | Hub extended, not renamed/moved. |
| `RegionCellRenderer`, `CityThumbnailCapture` | New services in `Domains/Geography/Services/`. Pure domain logic, no hub modifications. | — |
| `RegionScene.unity` | New scene file. No modifications to `CityScene.unity`. | scene contract |
| PrimeTween package | New dependency. Add via Package Manager. No existing code modified. | — |
| `RegionalMapManager` (hub) | Extended: expose `RegionalMap GetRegionalMapData()` read-only accessor for `ZoomTransitionController`. No rename/move. | Hub extended only. |

**Invariants flagged:** #1 (HeightMap — not touched), #5 (cellArray — not touched), #6 (no new responsibilities added to existing hubs).

**Architecture decision note:** `RegionScene` is a new scene + new hub MonoBehaviour. This is additive — no existing scene structure modified. Approach D (Addressables + Tilemap) deferred; streaming architecture decision deferred to post-C plan.

### Implementation Points — region-scene-prototype

**Stage 1 — RegionScene.unity + RegionGridManager (days 1–2):**
1. Create `Assets/Scenes/RegionScene.unity` — minimal, no terrain. Add root `RegionGridManager` GameObject.
2. `RegionGridManager.cs` — new hub MonoBehaviour. Fields: `[SerializeField] RegionCellRenderer cellRenderer`, `[SerializeField] ScriptableObject mapData`. `Start()` calls `cellRenderer.Initialize(mapData)`.
3. `RegionCellRenderer.cs` — `Initialize(data)` iterates 5×5, places `SpriteRenderer` GameObjects at isometric offsets matching region scale. City cell `[2,2]` left blank.
4. Stub sprites for neighbor cells: reuse existing territory tile sprites or create 1 placeholder sprite.
5. `npm run unity:compile-check`.

**Stage 2 — CityThumbnailCapture (days 3–4):**
1. `CityThumbnailCapture.cs` — `Capture(float targetOrthoSize)` → create `RenderTexture(512, 256)`, spin up secondary `Camera`, set `orthographicSize = targetOrthoSize`, render to RT, `Texture2D.ReadPixels`, create `Sprite`, dispose RT + Camera.
2. Integration test: trigger capture manually via `GeographyInitReportMenu` Editor menu to validate output sprite.

**Stage 3 — `RegionSceneStub` ScriptableObject (day 5):**
1. `RegionSceneStub.cs` — `[CreateAssetMenu]` ScriptableObject, `TerritoryData[,] territories`.
2. `RegionalMapManager` extended: `GetRegionalMapData()` accessor builds `RegionSceneStub` snapshot.

### Implementation Points — city-region-zoom-transition

**Stage 1 — PrimeTween + ZoomTransitionController (days 1–3):**
1. Add PrimeTween via Package Manager: `com.kyrylokuzyk.primetween`.
2. `ZoomTransitionController.cs` — fields: `[SerializeField] float cityOrthoSize`, `[SerializeField] float regionOrthoSize`, `[SerializeField] float tweenDuration = 1.8f`. `BeginZoomOut()`: capture thumbnail → `LoadSceneAsync` with `allowSceneActivation=false` → `PrimeTween.Tween(camera, v => camera.orthographicSize = v, regionOrthoSize, tweenDuration, Ease.InOutCubic)` → activation gate check in `Update()`.
3. Extend `CameraController`: add `OrthographicSize` property (2 lines).
4. Wire `ZoomTransitionController` to a GameObject in `CityScene.unity` (new GameObject "TransitionController").

**Stage 2 — allowSceneActivation gate + RegionScene activation (days 4–5):**
1. `Update()` loop in `ZoomTransitionController`: when `asyncOp != null && asyncOp.progress >= 0.9 && tweenProgress >= 0.8` → `asyncOp.allowSceneActivation = true` → place thumbnail sprite on `RegionGridManager.CityCell[2,2]`.
2. After activation: `SceneManager.UnloadSceneAsync("CityScene")`.
3. `RegionToCityTransitionController.cs` — mirror logic for zoom-in (stub implementation, full impl deferred).

### Examples

**ZoomTransitionController.BeginZoomOut sketch:**
```csharp
public void BeginZoomOut() {
    _thumbnail = _thumbnailCapture.Capture(regionOrthoSize);
    _loadOp = SceneManager.LoadSceneAsync("RegionScene", LoadSceneMode.Additive);
    _loadOp.allowSceneActivation = false;
    Tween.Custom(cityOrthoSize, regionOrthoSize, tweenDuration,
        v => _camera.orthographicSize = v, Ease.InOutCubic);
}
// Update: gate activation when both conditions met
```

**CityThumbnailCapture sketch:**
```csharp
public Sprite Capture(float orthoSize) {
    var rt = new RenderTexture(512, 256, 0);
    var cam = new GameObject("ThumbnailCam").AddComponent<Camera>();
    cam.orthographicSize = orthoSize;
    cam.targetTexture = rt;
    cam.Render();
    var tex = new Texture2D(512, 256);
    RenderTexture.active = rt;
    tex.ReadPixels(new Rect(0,0,512,256), 0, 0);
    tex.Apply();
    Object.Destroy(cam.gameObject);
    rt.Release();
    return Sprite.Create(tex, new Rect(0,0,512,256), Vector2.one*0.5f);
}
```

### Review Notes

NON-BLOCKING:
- `region-scene-prototype` gate: must confirm `ui-toolkit-migration` plan is closed (status = `shipped`) before starting Stage 1 Inspector wiring for RegionScene UI panels.
- PrimeTween license: MIT, free. Confirm Unity version compatibility (requires Unity 2021.3+, project is Unity 6 — compatible).
- `RegionToCityTransitionController` (zoom-in path) is a stub in the city-region-zoom-transition plan — full impl is a follow-on task or later stage.
- RegionScene cell scale (isometric offsets) must be validated against `CityScene` camera to ensure visual continuity at the tween boundary. Parameter tuning expected.
- `CityThumbnailCapture` RT disposal must handle edge case where scene unloads before `ReadPixels` completes — add null-check on RT in dispose path.
- Approach D (Addressables + Tilemap migration) deferred. Add as BACKLOG TECH item after city-region-zoom-transition ships.

### Expansion metadata

- Date: 2026-05-13
- Model: claude-sonnet-4-6
- Approaches selected: C (city-region-zoom-transition) + region-scene-prototype new plan; D deferred
- Blocking items resolved: 0
