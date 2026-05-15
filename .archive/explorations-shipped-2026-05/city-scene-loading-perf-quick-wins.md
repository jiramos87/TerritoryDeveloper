---
slug: city-scene-loading-perf-quick-wins
parent_plan_id: null
target_version: 1
stages:
  - id: "1.0"
    title: "Inspector DI + profiler markers"
    exit: "All FindObjectOfType init calls removed from GameBootstrap, GameManager, GridManager. GridInitDependencyBinder validates 13 inspector refs. 6 Profiler.BeginSample/EndSample guards on hot init paths. CityScene.unity inspector slots wired."
    red_stage_proof: |
      binder = GridInitDependencyBinder()
      gm.zoneManager = None  # inspector not wired
      result = binder.validate(gm)
      assert result.missing_count >= 1
      assert find_object_of_type_call_count == 0
      profiler_samples = capture_profiler_samples(lambda: gm.initialize_grid())
      assert "GeographyInitService.CreateGrid" in profiler_samples
    red_stage_proof_block:
      red_test_anchor: inspector-di-test:Assets/Scripts/Domains/Grid/Services/GridInitDependencyBinder.cs::Validate
      target_kind: tracer_verb
      proof_artifact_id: tests/city-scene-loading-perf-quick-wins/stage1-inspector-di.test.mjs
      proof_status: failed_as_expected
    tasks:
      - id: 1.0.1
        title: "Remove FindObjectOfType from GameBootstrap + GameManager"
        prefix: TECH
        depends_on: []
        digest_outline: "Add [SerializeField] private fields to GameBootstrap (gameManager) and GameManager (gridManager, saveManager). Remove all FindObjectOfType calls in Start(). Add null-check Debug.LogWarning guards."
        touched_paths:
          - "Assets/Scripts/Managers/GameManagers/GameBootstrap.cs"
          - "Assets/Scripts/Managers/GameManagers/GameManager.cs"
        kind: code
      - id: 1.0.2
        title: "GridInitDependencyBinder — validate all 13 GridManager inspector refs"
        prefix: TECH
        depends_on: []
        digest_outline: "New service Domains/Grid/Services/GridInitDependencyBinder.cs. Validate() takes GridManager, checks all 13 [SerializeField] fields for null, returns ValidationResult with missing[] list. Call Validate(this) at top of GridManager.InitializeGrid()."
        touched_paths:
          - "Assets/Scripts/Domains/Grid/Services/GridInitDependencyBinder.cs"
          - "Assets/Scripts/Managers/GameManagers/GridManager.cs"
        kind: code
      - id: 1.0.3
        title: "Profiler markers on 6 hot init paths in GeographyInitService"
        prefix: TECH
        depends_on: []
        digest_outline: "Add Profiler.BeginSample/EndSample pairs to GeographyInitService matching existing ApplyInterchangeAtPipelineStart convention. Markers: GeographyInitService.CreateGrid, GeographyInitService.RestoreGrid, GeographyInitService.RunWaterPipeline, GeographyInitService.RunInterstatePipeline, GeographyInitService.InitializeForestMap, GeographyInitService.ReCalculateSortingOrderBasedOnHeight."
        touched_paths:
          - "Assets/Scripts/Managers/GameManagers/GeographyInitService.cs"
        kind: code

  - id: "2.0"
    title: "TilePool pre-warm"
    exit: "TilePool service operational. CreateGrid() and RestoreGrid() pull from pool instead of Instantiate. Pool pre-warmed from GeographyInitService before InitializeGrid(). Zero new GC allocations for tile GOs during grid create/restore cycle."
    red_stage_proof: |
      pool = TilePool(prefab=mock_tile, pre_warm_count=64*64)
      pool.pre_warm()
      initial_alloc = get_gc_alloc_count()
      grid = MockGridManager(tile_pool=pool)
      grid.create_grid(width=64, height=64)
      assert get_gc_alloc_count() - initial_alloc == 0
      assert pool.active_count == 64 * 64
    red_stage_proof_block:
      red_test_anchor: tile-pool-test:Assets/Scripts/Domains/Grid/Services/TilePool.cs::PreWarm
      target_kind: tracer_verb
      proof_artifact_id: tests/city-scene-loading-perf-quick-wins/stage2-tile-pool.test.mjs
      proof_status: failed_as_expected
    tasks:
      - id: 2.0.1
        title: "TilePool service — ObjectPool<GameObject> wrapper"
        prefix: TECH
        depends_on: []
        digest_outline: "New service Domains/Grid/Services/TilePool.cs. Unity ObjectPool<GameObject> wrapper with PreWarm(int count, GameObject prefab), Get(GameObject prefab, Vector3 position), Return(GameObject). Pre-warm allocates count instances inactive under a pool root transform."
        touched_paths:
          - "Assets/Scripts/Domains/Grid/Services/TilePool.cs"
        kind: code
      - id: 2.0.2
        title: "Wire TilePool into GridManager.Impl.cs CreateGrid + RestoreGrid"
        prefix: TECH
        depends_on: []
        digest_outline: "GridManager.Impl.cs: replace Instantiate(tilePrefab) in CreateGrid() with _tilePool.Get(tilePrefab, position). RestoreGrid(): return tile to pool before re-getting. Null-guard: if _tilePool null fallback to Instantiate with Debug.LogWarning."
        touched_paths:
          - "Assets/Scripts/Managers/GameManagers/GridManager.Impl.cs"
          - "Assets/Scripts/Managers/GameManagers/GridManager.cs"
        kind: code
      - id: 2.0.3
        title: "Pre-warm TilePool from GeographyInitService before InitializeGrid"
        prefix: TECH
        depends_on: []
        digest_outline: "GeographyInitService.InitializeGeography(): add _tilePool.PreWarm(width * height, tilePrefab) call before gridManager.InitializeGrid(). Expose TilePool as [SerializeField] on GridManager (inspector-wired). GeographyInitService resolves via GridManager reference."
        touched_paths:
          - "Assets/Scripts/Managers/GameManagers/GeographyInitService.cs"
          - "Assets/Scripts/Managers/GameManagers/GridManager.cs"
        kind: code

  - id: "3.0"
    title: "Loading veil + lazy forest"
    exit: "LoadingVeilController canvas overlay activates at CityScene start, deactivates when GeographyManager.IsInitialized fires. Forest init deferred post-playable via DeferredForestInit coroutine. Veil Canvas prefab in Assets/UI/Prefabs/."
    red_stage_proof: |
      veil = LoadingVeilController()
      geo = MockGeographyManager(is_initialized=False)
      veil.bind(geo)
      assert veil.is_active == True
      geo.is_initialized = True
      veil.on_geography_initialized()
      assert veil.is_active == False
      geo.initialize_geography(forests_on_start=False)
      assert forest_init_call_count == 0
      geo.run_deferred_forest_init()
      assert forest_init_call_count == 1
    red_stage_proof_block:
      red_test_anchor: loading-veil-test:Assets/Scripts/Domains/Geography/Services/LoadingVeilController.cs::OnGeographyInitialized
      target_kind: tracer_verb
      proof_artifact_id: tests/city-scene-loading-perf-quick-wins/stage3-loading-veil.test.mjs
      proof_status: failed_as_expected
    tasks:
      - id: 3.0.1
        title: "LoadingVeilController — canvas overlay service"
        prefix: TECH
        depends_on: []
        digest_outline: "New service Domains/Geography/Services/LoadingVeilController.cs. MonoBehaviour. Awake() activates Canvas overlay (Image, white, full-screen). Listens to GeographyManager.OnGeographyInitialized event; OnGeographyInitialized() deactivates canvas. Exposes float Progress property for optional progress-bar widget."
        touched_paths:
          - "Assets/Scripts/Domains/Geography/Services/LoadingVeilController.cs"
        kind: code
      - id: 3.0.2
        title: "GeographyManager — OnGeographyInitialized event + DeferredForestInit coroutine"
        prefix: TECH
        depends_on: []
        digest_outline: "GeographyManager: add System.Action OnGeographyInitialized event, fire in IsInitialized setter when value transitions false→true. GeographyInitService: add DeferredForestInit() IEnumerator — yield null x2, then forestManager.InitializeForestMap(). When initializeForestsOnStart=false, call StartCoroutine(DeferredForestInit()) after IsInitialized=true instead of inline forest init."
        touched_paths:
          - "Assets/Scripts/Managers/GameManagers/GeographyManager.cs"
          - "Assets/Scripts/Managers/GameManagers/GeographyInitService.cs"
        kind: code
---

# City Scene Loading — Performance Quick Wins

Source exploration: [`docs/explorations/assets/city-scene-loading-research.md`](assets/city-scene-loading-research.md) — Design Expansion § Performance Quick Wins (Approach A).

Approach A bundles proposals #3 (Inspector DI), #4 (TilePool), #11 (profiler markers), #13 (loading veil), #17 (lazy forest). Synchronous only — no async/coroutine init primitives. Hub files extended in-place; new logic in Domains/Grid/Services/ and Domains/Geography/Services/.

**Constraint:** Hub C# files attached via Unity inspector (GridManager, GameBootstrap, GameManager, GeographyInitService, GeographyManager) must not be renamed, moved, or deleted (invariant #13).
