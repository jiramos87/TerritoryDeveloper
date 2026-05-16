---
slug: cityscene-perf-large-grids
target_version: 1
parent_plan_id: null
targets:
  - id: cityscene-load-time-grids
    label: CityScene loading time on 64×64 / 128×128 / 256×256 grids
  - id: cityscene-runtime-fps-large-maps
    label: CityScene runtime FPS on large maps
decisions_compatible:
  - DEC-A1
  - DEC-A2
  - DEC-A9
  - DEC-A25
  - DEC-A29
stages:
  - id: "1"
    title: "Async additive scene load + skeleton screen + time-sliced bootstrap (Units 1+2 merged)"
    exit: "CityScene loads via Addressables additive + skeleton panel renders progress + GridManager.CreateGrid yields cells in 1024-cell batches; FullFlowSmokeTest passes load-time + bootstrap-time budgets at 64/128/256 grid dims."
    red_stage_proof: "Assets/Scripts/Tests/PlayMode/FullFlowSmokeTest.cs::TestCityScene_AsyncLoad_BootstrapTimeWithinBudget — failing assertion on combined async handle PercentComplete + GridManager.Progress hitting 1f under budget at 256×256; green when async loader + coroutine bootstrap ship together."
    status: pending
    target_refs:
      - cityscene-load-time-grids
    red_stage_proof_block:
      red_test_anchor: "Assets/Scripts/Tests/PlayMode/FullFlowSmokeTest.cs::TestCityScene_AsyncLoad_BootstrapTimeWithinBudget"
      target_kind: visibility_delta
      proof_artifact_id: "pending"
      proof_status: failed_as_expected
    tasks:
      - id: "1.1"
        title: "Add CitySceneAsyncLoader wrapping Addressables additive scene load"
        prefix: TECH
        depends_on: []
        digest_outline: "Add CitySceneAsyncLoader MonoBehaviour wrapping Addressables.LoadSceneAsync(cityKey, Additive, activateOnLoad:false); expose AsyncOperationHandle.PercentComplete."
        touched_paths:
          - Assets/Scripts/Domains/IsoSceneCore/Services/CitySceneAsyncLoader.cs
        kind: code
      - id: "1.2"
        title: "Author CitySceneLoadingPanel prefab + controller"
        prefix: TECH
        depends_on: ["1.1"]
        digest_outline: "Author CitySceneLoadingPanel prefab — skeleton placeholders, goal-gradient progress bar, rotating tip copy."
        touched_paths:
          - Assets/Prefabs/UI/CitySceneLoadingPanel.prefab
          - Assets/Scripts/UI/CitySceneLoadingPanelController.cs
        kind: code
      - id: "1.3"
        title: "Convert GridManager.CreateGrid to time-sliced coroutine"
        prefix: TECH
        depends_on: ["1.1"]
        digest_outline: "Convert GridManager.CreateGrid into IEnumerator with BATCH=1024 cells/frame + yield return null between batches; expose float Progress."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/GridManager.Impl.cs
        kind: code
      - id: "1.4"
        title: "Move terrain + water pipeline behind yield boundary in GeographyInitService"
        prefix: TECH
        depends_on: ["1.3"]
        digest_outline: "Move terrainManager.InitializeHeightMap + RunWaterPipeline behind same yield boundary in GeographyInitService; flip isInitialized after final batch."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/GeographyInitService.cs
        kind: code
      - id: "1.5"
        title: "Swap 8 SceneManager.LoadScene call sites to CitySceneAsyncLoader.LoadAsync"
        prefix: TECH
        depends_on: ["1.1", "1.2", "1.4"]
        digest_outline: "Swap 8 SceneManager.LoadScene(CitySceneBuildIndex) call sites to CitySceneAsyncLoader.LoadAsync; bind panel progress to combined async handle + bootstrap Progress."
        touched_paths:
          - Assets/Scripts/UI/MainMenu/MainMenuHost.cs
          - Assets/Scripts/UI/MainMenu/MainMenuController.cs
          - Assets/Scripts/UI/PauseMenu/PauseMenuHost.cs
          - Assets/Scripts/UI/SaveLoad/SaveLoadScreenDataAdapter.cs
          - Assets/Scripts/UI/SaveLoad/SaveLoadViewHost.cs
        kind: code
      - id: "1.6"
        title: "Add headless-skip flag + load-time assertions to FullFlowSmokeTest"
        prefix: TECH
        depends_on: ["1.5"]
        digest_outline: "Add headless-skip flag for AgentTestModeBatchRunner; add load-time + bootstrap-time assertions to FullFlowSmokeTest at 64/128/256 grid dims."
        touched_paths:
          - Assets/Scripts/Tests/PlayMode/FullFlowSmokeTest.cs
          - Assets/Scripts/Editor/Bridge/AgentTestModeBatchRunner.cs
        kind: code
  - id: "2"
    title: "ServiceRegistry sweep replacing FindObjectOfType (Unit 3)"
    exit: "ServiceRegistry.Resolve<T>() throws MissingServiceException on missing service; 13 InitializeGrid manager refs resolved O(1) via Resolve<T> in Start (Invariant #12); Wave 2 sweep of remaining 503 sites documented per topic cluster."
    red_stage_proof: "Assets/Scripts/Tests/PlayMode/FullFlowSmokeTest.cs::TestInitializeGrid_LoadTimeDelta_AfterResolveMigration — failing assertion on InitializeGrid wall-clock load-time delta until 13 FindObjectOfType calls flip to ServiceRegistry.Resolve<T>."
    status: pending
    target_refs:
      - cityscene-load-time-grids
      - cityscene-runtime-fps-large-maps
    red_stage_proof_block:
      red_test_anchor: "Assets/Scripts/Tests/PlayMode/FullFlowSmokeTest.cs::TestInitializeGrid_LoadTimeDelta_AfterResolveMigration"
      target_kind: visibility_delta
      proof_artifact_id: "pending"
      proof_status: failed_as_expected
    tasks:
      - id: "2.1"
        title: "Add ServiceRegistry.Resolve<T>() guard helper"
        prefix: TECH
        depends_on: []
        digest_outline: "Add ServiceRegistry.Resolve<T>() guard helper that throws clear missing-service error instead of returning null."
        touched_paths:
          - Assets/Scripts/Domains/_Registry/ServiceRegistry.cs
        kind: code
      - id: "2.2"
        title: "Register 13 InitializeGrid manager refs in GeographyInitService.Awake"
        prefix: TECH
        depends_on: ["2.1"]
        digest_outline: "Central registration of 13 InitializeGrid manager refs in GeographyInitService Awake (producer per Invariant #12)."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/GeographyInitService.cs
        kind: code
      - id: "2.3"
        title: "Wave 1 — migrate 13 FindObjectOfType<T> in GridManager.InitializeGrid"
        prefix: TECH
        depends_on: ["2.2"]
        digest_outline: "Wave 1 — migrate the 13 FindObjectOfType<T>() calls inside GridManager.InitializeGrid to ServiceRegistry.Resolve<T>() in Start path."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/GridManager.Impl.cs
        kind: code
      - id: "2.4"
        title: "Wave 2 — sweep remaining 503 FindObjectOfType call sites"
        prefix: TECH
        depends_on: ["2.3"]
        digest_outline: "Wave 2 — sweep remaining 503 FindObjectOfType call sites in topic clusters (managers → systems → UI); document opt-outs."
        touched_paths:
          - Assets/Scripts/
        kind: code
      - id: "2.5"
        title: "Add InitializeGrid load-time regression assertion"
        prefix: TECH
        depends_on: ["2.3"]
        digest_outline: "Add load-time regression assertion for InitializeGrid (Wave 1 expected delta) under FullFlowSmokeTest perf harness."
        touched_paths:
          - Assets/Scripts/Tests/PlayMode/FullFlowSmokeTest.cs
        kind: code
  - id: "3"
    title: "Cap + early-exit lake-depression carving (Unit 4)"
    exit: "EnsureGuaranteedLakeDepressions exits early when delta==0 or cap=min(50, w*h/100) reached; HeightMap≡Cell.height parity preserved (Invariant #1); pathological seed test asserts cap hit + load time within budget."
    red_stage_proof: "Assets/Scripts/Tests/EditMode/TerrainLakeCarveCapTest.cs::TestPathologicalSeed_LakeCarveCap_HitsCapAndPreservesParity — failing assertion on round count + load time + HeightMap parity until cap and delta tracking land."
    status: pending
    target_refs:
      - cityscene-load-time-grids
    red_stage_proof_block:
      red_test_anchor: "Assets/Scripts/Tests/EditMode/TerrainLakeCarveCapTest.cs::TestPathologicalSeed_LakeCarveCap_HitsCapAndPreservesParity"
      target_kind: visibility_delta
      proof_artifact_id: "pending"
      proof_status: failed_as_expected
    tasks:
      - id: "3.1"
        title: "Hard-cap rounds in EnsureGuaranteedLakeDepressions"
        prefix: TECH
        depends_on: []
        digest_outline: "Add hard-cap rounds = min(50, w*h/100) inside EnsureGuaranteedLakeDepressions; log one-line warning when cap hit."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/TerrainManager.Impl.cs
        kind: code
      - id: "3.2"
        title: "Replace full CountSpillPassingCells scan with delta tracking"
        prefix: TECH
        depends_on: ["3.1"]
        digest_outline: "Replace per-round full CountSpillPassingCells with delta tracking: each carve subtracts/adds only affected cells."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/TerrainManager.Impl.cs
        kind: code
      - id: "3.3"
        title: "Add EditMode pathological-seed cap test"
        prefix: TECH
        depends_on: ["3.1"]
        digest_outline: "Add EditMode test fixture for pathological terrain seed → assert cap hit + load time within budget; assert HeightMap≡Cell.height parity preserved."
        touched_paths:
          - Assets/Scripts/Tests/EditMode/TerrainLakeCarveCapTest.cs
        kind: code
  - id: "4"
    title: "Zero-allocation hot loops sweep (Unit 5)"
    exit: "Update/LateUpdate hot paths produce GC.Alloc per simulation-only frame ≈ 0 at 256×256; LINQ replaced with for loops; Debug.Log* wrapped in [Conditional(\"VERBOSE_LOG\")]; pooled NativeArray<T> owned by manager."
    red_stage_proof: "Assets/Scripts/Tests/PlayMode/HotPathAllocSmokeTest.cs::TestHotPath_GCAllocPerFrame_NearZero_256x256 — failing assertion on per-frame GC.Alloc bytes until alloc audit + stackalloc/NativeArray rewrites + LINQ removal land."
    status: pending
    target_refs:
      - cityscene-runtime-fps-large-maps
    red_stage_proof_block:
      red_test_anchor: "Assets/Scripts/Tests/PlayMode/HotPathAllocSmokeTest.cs::TestHotPath_GCAllocPerFrame_NearZero_256x256"
      target_kind: visibility_delta
      proof_artifact_id: "pending"
      proof_status: failed_as_expected
    tasks:
      - id: "4.1"
        title: "Audit Update/LateUpdate hot paths for hidden allocations"
        prefix: TECH
        depends_on: []
        digest_outline: "Audit Update/LateUpdate hot paths for hidden allocations (LINQ, foreach over List<T>, lambda capture, string interpolation in logs)."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/GridManager.Impl.cs
          - Assets/Scripts/Managers/GameManagers/ChunkCullingSystem.cs
          - Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneChunkCuller.cs
        kind: code
      - id: "4.2"
        title: "Replace transient buffers with stackalloc + pooled NativeArray<T>"
        prefix: TECH
        depends_on: ["4.1"]
        digest_outline: "Replace small fixed-size buffers with stackalloc; convert reusable larger buffers to pooled NativeArray<T> owned by the manager."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/
        kind: code
      - id: "4.3"
        title: "Wrap hot-path Debug.Log* + convert LINQ to for loops"
        prefix: TECH
        depends_on: ["4.1"]
        digest_outline: "Wrap Debug.Log* on hot paths in [Conditional(\"VERBOSE_LOG\")] helpers; convert LINQ hot-path uses to plain for loops."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/
        kind: code
      - id: "4.4"
        title: "Add PlayMode GC.Alloc-per-frame smoke at 256×256"
        prefix: TECH
        depends_on: ["4.2", "4.3"]
        digest_outline: "Add PlayMode FPS smoke that records GC.Alloc per frame on 256×256 simulation-only frames; assert near-zero."
        touched_paths:
          - Assets/Scripts/Tests/PlayMode/HotPathAllocSmokeTest.cs
        kind: code
  - id: "5"
    title: "Distance-gated frustum culling with tighter bounds (Unit 6)"
    exit: "ChunkCullingSystem skips chunks beyond maxRenderRadius before AABB test; tight height-aware AABB replaces generic bbox; IsoSceneChunkCuller becomes single visibility-delta emitter (DEC-A29 reuse); 256×256 FPS smoke shows dramatic zoom-in vs zoom-out delta."
    red_stage_proof: "Assets/Scripts/Tests/PlayMode/CullingFpsSmokeTest.cs::TestCulling_256x256_FpsDeltaWithDistanceGate — failing assertion on FPS delta between zoomed-in and zoomed-out until distance gate + tighter AABB + single emitter land."
    status: pending
    target_refs:
      - cityscene-runtime-fps-large-maps
    red_stage_proof_block:
      red_test_anchor: "Assets/Scripts/Tests/PlayMode/CullingFpsSmokeTest.cs::TestCulling_256x256_FpsDeltaWithDistanceGate"
      target_kind: visibility_delta
      proof_artifact_id: "pending"
      proof_status: failed_as_expected
    tasks:
      - id: "5.1"
        title: "Add distance gate at top of ChunkCullingSystem.UpdateVisibility"
        prefix: TECH
        depends_on: []
        digest_outline: "Add distance gate at top of ChunkCullingSystem.UpdateVisibility: skip chunk if center > maxRenderRadius before AABB test."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/ChunkCullingSystem.cs
        kind: code
      - id: "5.2"
        title: "Tighten chunk AABB to height-map-aware occupied volume"
        prefix: TECH
        depends_on: ["5.1"]
        digest_outline: "Tighten chunk AABB to height-map-aware actual occupied volume (replace generic chunk bbox)."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/ChunkCullingSystem.cs
        kind: code
      - id: "5.3"
        title: "Pick IsoSceneChunkCuller as single visibility-delta emitter"
        prefix: TECH
        depends_on: ["5.2"]
        digest_outline: "Pick single visibility-delta emitter (IsoSceneChunkCuller per DEC-A29 reuse) + retire ChunkCullingSystem update loop; preserve SetActive owner."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/ChunkCullingSystem.cs
          - Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneChunkCuller.cs
          - Assets/Scripts/Managers/GameManagers/GridManager.Impl.cs
        kind: code
      - id: "5.4"
        title: "Expose maxRenderRadius Inspector field"
        prefix: TECH
        depends_on: ["5.1"]
        digest_outline: "Expose maxRenderRadius as Inspector field tunable per platform; default tuned for 256×256."
        touched_paths:
          - Assets/Scripts/Managers/GameManagers/ChunkCullingSystem.cs
        kind: code
      - id: "5.5"
        title: "Add PlayMode FPS smoke 256×256 zoomed-in vs zoomed-out"
        prefix: TECH
        depends_on: ["5.3", "5.4"]
        digest_outline: "Add PlayMode FPS smoke at 256×256 zoomed-in vs zoomed-out; assert dramatic delta with distance gate enabled."
        touched_paths:
          - Assets/Scripts/Tests/PlayMode/CullingFpsSmokeTest.cs
        kind: code
notes: |
  Canonical-schema rewrite of docs/research/cityscene-loading-runtime-perf-large-grids.md frontmatter for ship-plan Phase A handoff validator. Original `plan_slug` / `plan_shape` / `stage_id` / kind=test|refactor coerced to `slug` / dropped / `id` / `kind: code` (refactor/test rolled into single code kind per validator enum). Stage `exit` + `red_stage_proof` synthesized from Design Expansion §Implementation Points exit-criteria + chosen red-test anchors per stage red_stage_proof_block.
---

