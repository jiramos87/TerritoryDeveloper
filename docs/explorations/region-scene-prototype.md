---
slug: region-scene-prototype
parent_plan_id: null
target_version: 1
title: RegionScene prototype — shared IsoSceneCore + region-scale evolution
approach_locked: B (Shared IsoSceneCore)
exit_gate: c (Sim-lite — visual + interactive + pop/urban-area evolution + save/load + unlock gate)
arch_decision_slug: DEC-A29
arch_surface_slug: contracts/iso-scene-core-foundation
hub_preservation_rule: true
deferred:
  - db-driven-ui-baking-refactor
  - scene-load-transition-mechanics
  - scale-tiers-above-region
  - region-zone-paint-zone-definitions
  - visual-evolution-overlay
related_master_plans:
  - multi-scale
notes: |
  Hub-preservation rule applies to every stage that touches an existing Inspector-wired hub script
  (GameManager, GridManager, GeographyInitService, UIManager, future RegionManager). New scripts
  under Assets/Scripts/Domains/IsoSceneCore/Services/* and Assets/Scripts/RegionScene/* are
  unconstrained. Companion exploration for scene transitions: docs/explorations/assets/city-scene-loading-research.md.
stages:
  - id: '1.0'
    title: Tracer slice — RegionScene loads + camera pans on placeholder sprite
    exit: |
      Pressing arrow keys on RegionScene moves view; failing test (red) at task 1 demonstrates plumbing; green at last task. RegionScene opens via main menu; placeholder sprite at grid center; camera pans on input.
    red_stage_proof: |
      # tracer_verb red test (red on 1.0.1 stub, green on 1.0.3 wire-up)
      def test_arrow_keys_pan_camera_on_region_scene():
          load_scene("RegionScene")
          root = find_gameobject("RegionRoot")
          assert root.get_component("RegionManager") is not None  # red at 1.0.1
          assert find_sprite_at_grid_center() is not None         # red at 1.0.2
          camera_pos_before = get_main_camera_position()
          press_key(KeyCode.RightArrow, duration_seconds=0.5)
          camera_pos_after = get_main_camera_position()
          assert camera_pos_after.x > camera_pos_before.x          # red at 1.0.3
    red_stage_proof_block:
      red_test_anchor: 'tracer-verb-test:Assets/Scripts/RegionScene/RegionManager.cs::ArrowKeysPanCamera'
      target_kind: tracer_verb
      proof_artifact_id: 'tests/region-scene-prototype/stage1.0-tracer.test.cs'
      proof_status: failed_as_expected
    enriched:
      edge_cases:
        - { input: 'RegionScene loaded without ServiceRegistry GO in scene', state: 'RegionManager.Start() resolves IsoSceneCamera', expected: 'RegionManager logs warning + scene refuses to enter play mode; CI gate catches missing GO' }
        - { input: 'Placeholder sprite asset missing under Assets/Sprites/region/', state: 'RegionManager.Awake() loads sprite via Resources.Load', expected: 'Pink magenta missing-texture sprite renders at grid center; log warning emitted; test still passes on positional assert' }
        - { input: 'Arrow-key input arrives before RegionManager.Start() completes', state: 'Scene mid-load; IsoSceneCamera ref null', expected: 'Update() null-guards on _camera; no NRE; pan event queued or dropped' }
      shared_seams:
        - { name: 'IsoSceneCamera', producer_stage: '1.0', consumer_stages: ['1.1', '2.0', '3.0'], contract: 'Pan/zoom/displacement service consumed by all iso scenes via composition reference; stub at 1.0, fully extracted at 1.1' }
        - { name: 'RegionManager hub facade', producer_stage: '1.0', consumer_stages: ['2.0', '3.0', '4.0', '5.0'], contract: 'Inspector-wired MonoBehaviour holds composition root; never renamed/moved/deleted post-1.0 (hub-preservation rule)' }
        - { name: 'IIsoSceneHost composition contract', producer_stage: '1.0', consumer_stages: ['1.1', '1.2', '3.0', '5.0'], contract: 'RegionManager implements IIsoSceneHost; tool/subtype/click registration flows through it' }
    tasks:
      - id: '1.0.1'
        title: 'Stub RegionScene + RegionManager hub w/ Inspector wiring'
        prefix: TECH
        depends_on: []
        digest_outline: |
          Create RegionScene.unity via unity_bridge_command new_scene. Add RegionRoot GameObject with RegionManager MonoBehaviour stub component. Wire ServiceRegistry GO into scene. Failing test RegionSceneLoadsTest asserts RegionManager present on RegionRoot after scene load (red at task close).
        touched_paths:
          - Assets/Scenes/RegionScene.unity
          - Assets/Scripts/RegionScene/RegionManager.cs
        kind: code
        enriched:
          visual_mockup_svg: |
            <svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
              <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
              <text x="200" y="24" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="11">RegionScene — Stage 1.0.1 stub</text>
              <rect x="20" y="48" width="360" height="160" fill="none" stroke="var(--ds-border-muted, #2e3340)" stroke-width="1" stroke-dasharray="4 3"/>
              <text x="200" y="130" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="13">[RegionRoot GameObject]</text>
              <text x="200" y="150" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="11">RegionManager + ServiceRegistry attached</text>
              <text x="200" y="225" text-anchor="middle" fill="var(--ds-text-danger, #d46a6a)" font-family="monospace" font-size="10">Test red: no sprite yet, no camera pan</text>
            </svg>
          before_after_code:
            path: Assets/Scripts/RegionScene/RegionManager.cs
            before: |
              // file does not exist
            after: |
              using UnityEngine;
              using Territory.IsoSceneCore;

              namespace Territory.RegionScene
              {
                  public sealed class RegionManager : MonoBehaviour
                  {
                      [SerializeField] private Camera mainCamera;

                      void Awake()
                      {
                          // composition root stub — services wired in Stage 1.1
                      }
                  }
              }
          glossary_anchors:
            - { term: 'Host MonoBehaviour', spec_ref: 'DEC-A28 / Assets/Scripts/UI/Hosts/' }
            - { term: 'service registry', spec_ref: 'docs/post-atomization-architecture.md §Service Registry' }
            - { term: 'scene contract', spec_ref: 'docs/asset-pipeline-scene-contract.md' }
          failure_modes:
            - 'Fails if RegionScene.unity created without ServiceRegistry GO — invariant #12 breach'
            - 'Fails if RegionManager partial class declaration places `: MonoBehaviour` in non-stem file — Unity GUID bind error'
            - 'Fails if Inspector ref for mainCamera left unassigned — RegionManager.Awake() NRE at scene load'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
            - { slug: 'DEC-A22', role: 'inherits' }
            - { slug: 'DEC-A23', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scenes/RegionScene.unity', loc: null, kind: 'new', summary: 'New Unity scene file with RegionRoot GameObject + ServiceRegistry GO; created via bridge new_scene mutation' }
            - { path: 'Assets/Scripts/RegionScene/RegionManager.cs', loc: null, kind: 'new', summary: 'Hub MonoBehaviour stub under Territory.RegionScene namespace; Inspector-wired in RegionScene.unity; future composition root' }
      - id: '1.0.2'
        title: 'Place placeholder sprite at region grid center'
        prefix: TECH
        depends_on: ['1.0.1']
        digest_outline: |
          Add placeholder sprite asset under Assets/Sprites/region/placeholder.png. RegionManager.Awake() instantiates a SpriteRenderer GameObject at grid center (cell [31,31] of 64×64). Test grows; second assertion (find_sprite_at_grid_center) now expected to pass at task close.
        touched_paths:
          - Assets/Scripts/RegionScene/RegionManager.cs
          - Assets/Sprites/region/placeholder.png
        kind: code
        enriched:
          visual_mockup_svg: |
            <svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
              <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
              <text x="200" y="24" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="11">RegionScene — Stage 1.0.2 placeholder sprite</text>
              <rect x="20" y="48" width="360" height="160" fill="none" stroke="var(--ds-border-muted, #2e3340)" stroke-width="1" stroke-dasharray="4 3"/>
              <rect x="186" y="120" width="28" height="14" fill="var(--ds-accent-warm, #f4d28a)" opacity="0.8"/>
              <text x="200" y="142" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="9">cell [31,31]</text>
              <text x="200" y="225" text-anchor="middle" fill="var(--ds-text-warning, #e2b14a)" font-family="monospace" font-size="10">Test 2/3 green; pan test still red</text>
            </svg>
          before_after_code:
            path: Assets/Scripts/RegionScene/RegionManager.cs
            before: |
              void Awake() { /* stub */ }
            after: |
              void Awake()
              {
                  var sprite = Resources.Load<Sprite>("region/placeholder");
                  var go = new GameObject("PlaceholderSprite");
                  go.transform.SetParent(transform);
                  go.transform.position = GridCenterWorld(); // [31,31] of 64x64
                  var sr = go.AddComponent<SpriteRenderer>();
                  sr.sprite = sprite;
              }
          glossary_anchors:
            - { term: 'City / Region / Country cell', spec_ref: 'ms §cell-vocab' }
          failure_modes:
            - 'Fails if Sprites/region/placeholder.png missing — Resources.Load returns null, magenta missing-texture renders'
            - 'Fails if GridCenterWorld() computes coords using city-cell scale, not region-cell scale (1 region cell = 32 city cells)'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
            - { slug: 'DEC-A22', role: 'inherits' }
            - { slug: 'DEC-A23', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/RegionManager.cs', loc: 25, kind: 'extend', summary: 'Awake() now loads placeholder sprite and parents SpriteRenderer GO at grid center' }
            - { path: 'Assets/Sprites/region/placeholder.png', loc: null, kind: 'new', summary: 'Placeholder 32x16 isometric tile sprite at grid center; replaced when terrain renderer lands in Stage 2.0' }
      - id: '1.0.3'
        title: 'Wire arrow-keys to stub IsoSceneCamera pan'
        prefix: TECH
        depends_on: ['1.0.2']
        digest_outline: |
          Create IsoSceneCamera service stub under Assets/Scripts/Domains/IsoSceneCore/Services/. Wire arrow-key input via Update() loop; pan main camera transform. RegionManager.Start() resolves IsoSceneCamera from ServiceRegistry (deferred per invariant #12). Stage test fully green at this task close.
        touched_paths:
          - Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneCamera.cs
          - Assets/Scripts/RegionScene/RegionManager.cs
        kind: code
        enriched:
          visual_mockup_svg: |
            <svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
              <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
              <text x="200" y="24" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="11">RegionScene — Stage 1.0.3 camera pan tracer</text>
              <rect x="20" y="48" width="360" height="160" fill="none" stroke="var(--ds-border-muted, #2e3340)" stroke-width="1" stroke-dasharray="4 3"/>
              <rect x="240" y="120" width="28" height="14" fill="var(--ds-accent-warm, #f4d28a)" opacity="0.8"/>
              <path d="M 200 134 L 240 134" stroke="var(--ds-accent-cool, #88c0d0)" stroke-width="2" marker-end="url(#arrow)"/>
              <defs><marker id="arrow" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto"><path d="M0,0 L0,6 L8,3 z" fill="var(--ds-accent-cool, #88c0d0)"/></marker></defs>
              <text x="200" y="200" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="10">RightArrow held 0.5s → sprite drifts right</text>
              <text x="200" y="225" text-anchor="middle" fill="var(--ds-text-success, #8ac28a)" font-family="monospace" font-size="10">Test green: all 3 asserts pass</text>
            </svg>
          before_after_code:
            path: Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneCamera.cs
            before: |
              // file does not exist
            after: |
              using UnityEngine;
              namespace Territory.IsoSceneCore
              {
                  public sealed class IsoSceneCamera
                  {
                      private Camera _cam;
                      private float _panSpeed = 5f;
                      public void Configure(Camera cam, float panSpeed) { _cam = cam; _panSpeed = panSpeed; }
                      public void Tick(float dt)
                      {
                          var v = Vector3.zero;
                          if (Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
                          if (Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
                          if (Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
                          if (Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
                          _cam.transform.position += v * _panSpeed * dt;
                      }
                  }
              }
          glossary_anchors:
            - { term: 'service registry', spec_ref: 'docs/post-atomization-architecture.md §Service Registry' }
            - { term: 'Host MonoBehaviour', spec_ref: 'DEC-A28' }
          failure_modes:
            - 'Fails if RegionManager resolves IsoSceneCamera in Awake() — invariant #12 init-order race'
            - 'Fails if IsoSceneCamera.Configure not called before first Tick — _cam null, NRE'
            - 'Fails if Update() runs while Editor paused — input fires but no scene state visible'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
            - { slug: 'DEC-A22', role: 'inherits' }
            - { slug: 'DEC-A23', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneCamera.cs', loc: null, kind: 'new', summary: 'IsoSceneCore camera pan/zoom service stub; full extraction from GridManager happens in Stage 1.1' }
            - { path: 'Assets/Scripts/RegionScene/RegionManager.cs', loc: 35, kind: 'extend', summary: 'Start() now resolves IsoSceneCamera + calls Configure; Update() delegates Tick to camera service' }
  - id: '1.1'
    title: Extract IsoSceneCore runtime services (camera + culling + tick) from CityScene hubs
    exit: |
      CityScene regression tests pass; GridManager + GameManager file paths + class names + Inspector fields untouched. CityScene visually unchanged; internally pan/zoom/cull/tick flow through new services.
    red_stage_proof: |
      # unit red test (red on 1.1.2 mid-extract, green at 1.1.4 close)
      def test_grid_manager_facade_delegates_to_iso_scene_camera():
          scene = load_scene("CityScene")
          grid_manager = find_component("GridManager")
          assert grid_manager.serialized_field("mainCamera") is not None  # invariant: Inspector untouched
          camera_service = service_registry.resolve("IsoSceneCamera")
          assert camera_service is not None                                # red until 1.1.2
          baseline_pos = camera_service.transform.position
          press_key(KeyCode.RightArrow, duration_seconds=0.3)
          assert camera_service.transform.position.x > baseline_pos.x      # parity with pre-refactor
    red_stage_proof_block:
      red_test_anchor: 'unit-test:Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneCamera.cs::CameraServiceMirrorsGridManagerPan'
      target_kind: unit
      proof_artifact_id: 'tests/region-scene-prototype/stage1.1-iso-core-extract.test.cs'
      proof_status: failed_as_expected
    enriched:
      edge_cases:
        - { input: 'CityScene saved scene file references old GridManager Inspector field that no longer exists', state: 'Stage 1.1 extracts but renames an Inspector field by accident', expected: 'Scene fails to load with missing-script-field warning; CI gate via verify:local catches before merge' }
        - { input: 'Tick fires during scene load before IsoSceneTickBus subscription complete', state: 'GameManager.Start() resolves bus mid-Awake of consumers', expected: 'Bus.Publish guards on subscriber-list-empty; first tick dropped harmlessly' }
        - { input: 'IsoSceneChunkCuller computes visible-set using city-cell bounds when extracted', state: 'Generic culler needs grid-size from owner', expected: 'Culler.Configure(GridSize) called from GridManager.Start; RegionManager passes 64; identical bounds clamping logic' }
      shared_seams:
        - { name: 'IsoSceneCamera', producer_stage: '1.1', consumer_stages: ['2.0', '3.0'], contract: 'Fully extracted service; GridManager + RegionManager both hold composition refs and delegate Update tick' }
        - { name: 'IsoSceneChunkCuller', producer_stage: '1.1', consumer_stages: ['2.0'], contract: 'Visible-cell windowing service subscribed to camera deltas; RegionCellRenderer consumes visible-set' }
        - { name: 'IsoSceneTickBus', producer_stage: '1.1', consumer_stages: ['4.0'], contract: 'TimeManager publishes; RegionEvolutionService subscribes in Start (invariant #12)' }
    tasks:
      - id: '1.1.1'
        title: 'Create Territory.IsoSceneCore namespace + asmdef + folder structure'
        prefix: TECH
        depends_on: ['1.0.3']
        digest_outline: |
          Create Assets/Scripts/Domains/IsoSceneCore/ folder. Add Territory.IsoSceneCore.asmdef declaring assembly; reference UnityEngine + Unity.Mathematics. Subfolders Services/, Contracts/, UI/. .gitkeep placeholders so Unity meta files generate.
        touched_paths:
          - Assets/Scripts/Domains/IsoSceneCore/Territory.IsoSceneCore.asmdef
          - Assets/Scripts/Domains/IsoSceneCore/Services/.gitkeep
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'service registry', spec_ref: 'docs/post-atomization-architecture.md §Service Registry' }
          failure_modes:
            - 'Fails if asmdef references Assembly-CSharp circularly — Unity rejects assembly graph at refresh'
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/Domains/IsoSceneCore/Territory.IsoSceneCore.asmdef', loc: null, kind: 'new', summary: 'Assembly definition for IsoSceneCore; references UnityEngine + Unity.Mathematics' }
            - { path: 'Assets/Scripts/Domains/IsoSceneCore/Services/.gitkeep', loc: null, kind: 'new', summary: 'Folder marker so git + Unity generate meta files' }
      - id: '1.1.2'
        title: 'Extract camera pan/zoom/displacement → IsoSceneCamera; GridManager delegates'
        prefix: TECH
        depends_on: ['1.1.1']
        digest_outline: |
          Move existing camera pan + zoom + displacement code out of GridManager (and any sibling camera scripts) into Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneCamera.cs (extending the stub from 1.0.3). GridManager keeps [SerializeField] mainCamera + panSpeed; Start() resolves IsoSceneCamera and calls Configure(); Update() delegates to camera.Tick(Time.deltaTime). Hub-preservation rule applies — GridManager file path + class name + Inspector untouched.
        touched_paths:
          - Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneCamera.cs
          - Assets/Scripts/GridManager.cs
        kind: code
        enriched:
          before_after_code:
            path: Assets/Scripts/GridManager.cs
            before: |
              void Update()
              {
                  if (Input.GetKey(KeyCode.LeftArrow)) cameraVelocity.x -= panSpeed * Time.deltaTime;
                  // ... 80 lines of pan / zoom / displacement logic ...
              }
            after: |
              private IsoSceneCamera _camera;
              void Start() // Resolve in Start (invariant #12)
              {
                  _camera = ServiceRegistry.Resolve<IsoSceneCamera>();
                  _camera.Configure(mainCamera, panSpeed);
              }
              void Update() => _camera.Tick(Time.deltaTime);
          glossary_anchors:
            - { term: 'service registry', spec_ref: 'docs/post-atomization-architecture.md §Service Registry' }
            - { term: 'Host MonoBehaviour', spec_ref: 'DEC-A28' }
          failure_modes:
            - 'Fails if GridManager.cs class name or file path renamed during extract — hub-preservation rule breach + Inspector script-ref break'
            - 'Fails if Resolve called in Awake — invariant #12 race, _camera null on first Update tick'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneCamera.cs', loc: null, kind: 'extend', summary: 'Full pan/zoom/displacement logic landed; supersedes 1.0.3 stub' }
            - { path: 'Assets/Scripts/GridManager.cs', loc: null, kind: 'extend', summary: 'Hub facade pattern: Inspector serialization untouched; Start() resolves service, Update() delegates Tick' }
      - id: '1.1.3'
        title: 'Extract chunk culling → IsoSceneChunkCuller; GridManager delegates'
        prefix: TECH
        depends_on: ['1.1.2']
        digest_outline: |
          Move visible-cell windowing + chunk-cull logic from GridManager (+ ChunkCullingSystem if present) into Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneChunkCuller.cs. Subscribes to IsoSceneCamera deltas; raises visible-set deltas via event. GridManager.Start() resolves culler + calls Configure(GridSize). Hub-preservation rule applies.
        touched_paths:
          - Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneChunkCuller.cs
          - Assets/Scripts/GridManager.cs
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'service registry', spec_ref: 'docs/post-atomization-architecture.md §Service Registry' }
          failure_modes:
            - 'Fails if culler subscribes to camera before camera.Configure called — null camera ref, NRE'
            - 'Fails if visible-set delta emits during scene unload — listeners on disposed UIDocuments throw'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneChunkCuller.cs', loc: null, kind: 'new', summary: 'Visible-cell windowing service; subscribes to IsoSceneCamera deltas; emits visible-set events' }
            - { path: 'Assets/Scripts/GridManager.cs', loc: null, kind: 'extend', summary: 'GridManager.Start() resolves culler + Configure(GridSize); chunk-cull logic moved out' }
      - id: '1.1.4'
        title: 'Extract global tick subscription → IsoSceneTickBus; GameManager delegates'
        prefix: TECH
        depends_on: ['1.1.3']
        digest_outline: |
          Create Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneTickBus.cs. TimeManager publishes IsoTick to bus once per global tick; subscribers register via Subscribe(IIsoSceneTickHandler, IsoTickKind) in Start (invariant #12). GameManager keeps Inspector wiring untouched; resolves bus in Start; registers TimeManager bridge. Stage 1.1 regression test green at task close.
        touched_paths:
          - Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneTickBus.cs
          - Assets/Scripts/GameManager.cs
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'service registry', spec_ref: 'docs/post-atomization-architecture.md §Service Registry' }
          failure_modes:
            - 'Fails if Subscribe runs in Awake — invariant #12 race; bus producer may not be registered yet'
            - 'Fails if GameManager.cs renamed during extract — hub-preservation breach'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneTickBus.cs', loc: null, kind: 'new', summary: 'Tick multiplexer; producer=TimeManager bridge; consumers=per-scene evolution services' }
            - { path: 'Assets/Scripts/GameManager.cs', loc: null, kind: 'extend', summary: 'Hub facade: Start() resolves bus + registers TimeManager publish bridge; Inspector untouched' }
  - id: '1.2'
    title: Extract IsoSceneCore UI shell (HUD + toolbar + modal host + toast surface + subtype picker)
    exit: |
      Toolbar slot count unchanged; subtype picker still opens; modal/toast surfaces still render. Goldens match. CityScene HUD/toolbar visually identical; same elements rendered by shared UIDocument host.
    red_stage_proof: |
      # visibility_delta red test (red on 1.2.1 slot scaffold; green on 1.2.3 close)
      def test_cityscene_hud_slots_match_baseline_visual():
          load_scene("CityScene")
          shell = find_uidocument("IsoSceneUIShellHost")
          assert shell.query("HudSlot") is not None        # red at 1.2.1
          assert shell.query("ToolbarSlot") is not None    # red at 1.2.1
          baseline = load_golden("cityscene-hud-baseline.png")
          screenshot = capture_screenshot_play_mode()
          assert pixel_diff(screenshot, baseline) < 0.5%   # red until slot-registration refactor complete
    red_stage_proof_block:
      red_test_anchor: 'visibility-delta-test:Assets/Scripts/Domains/IsoSceneCore/UI/IsoSceneUIShellHost.cs::CitySceneHudSlotParityBaseline'
      target_kind: visibility_delta
      proof_artifact_id: 'tests/region-scene-prototype/stage1.2-ui-shell-extract.test.cs'
      proof_status: failed_as_expected
    enriched:
      edge_cases:
        - { input: 'Existing CityScene HUD UIDocument removed before IsoSceneUIShellHost slots exist', state: 'Stage 1.2.2 mid-refactor', expected: 'Scene compiles but HUD missing; golden test red; CI gate catches before merge' }
        - { input: 'UIShellHost.uxml references USS class that does not exist', state: 'Stage 1.2.1 USS file lags UXML', expected: 'Unity logs USS-missing-class warning; visual fallback to unstyled element' }
        - { input: 'Two scenes load IsoSceneUIShellHost simultaneously (CityScene → RegionScene transition)', state: 'Transition mechanic absent in prototype, but bridge test might', expected: 'Scene unload disposes old shell; deferred in this plan, but documented in shared_seams' }
      shared_seams:
        - { name: 'IsoSceneUIShellHost UIDocument', producer_stage: '1.2', consumer_stages: ['3.0', '5.0'], contract: 'Root UIDocument with named slots (hud, toolbar, subtype, modal, toast); per-scene plugins query slot + AddChild' }
        - { name: 'IIsoSceneToolRegistry', producer_stage: '1.2', consumer_stages: ['5.0'], contract: 'Per-scene tool registration into toolbar slot; CityScene registers existing tools; RegionScene registers Found-City tool' }
        - { name: 'IIsoSceneSubtypePicker', producer_stage: '1.2', consumer_stages: ['5.0'], contract: 'Generic picker; scenes register subtype catalogs at Start' }
        - { name: 'IIsoSceneZonePaintHost', producer_stage: '1.2', consumer_stages: [], contract: 'Paint mechanism shell; per-scene zone definitions deferred (region-zone-paint-zone-definitions in deferred list)' }
    tasks:
      - id: '1.2.1'
        title: 'Define IsoSceneUIShellHost UIDocument + UXML + USS w/ slot mechanism'
        prefix: TECH
        depends_on: ['1.1.4']
        digest_outline: |
          Hand-author iso-scene-ui-shell.uxml (named slots: hud-slot, toolbar-slot, subtype-slot, modal-slot, toast-slot) + iso-scene-ui-shell.uss (layout + theme). Create IsoSceneUIShellHost.cs Host MonoBehaviour (DEC-A28 pattern); Awake binds UIDocument rootVisualElement; exposes Slot(name) query API for plugins.
        touched_paths:
          - Assets/Scripts/Domains/IsoSceneCore/UI/IsoSceneUIShellHost.cs
          - Assets/UI/Generated/iso-scene-ui-shell.uxml
          - Assets/UI/Generated/iso-scene-ui-shell.uss
        kind: code
        enriched:
          visual_mockup_svg: |
            <svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
              <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
              <rect x="0" y="0" width="400" height="32" fill="var(--ds-bg-elevated, #23262f)"/>
              <text x="200" y="20" text-anchor="middle" fill="var(--ds-text-primary, #e6e9ef)" font-family="monospace" font-size="10">hud-slot (population, funds)</text>
              <rect x="0" y="208" width="400" height="32" fill="var(--ds-bg-elevated, #23262f)"/>
              <text x="200" y="228" text-anchor="middle" fill="var(--ds-text-primary, #e6e9ef)" font-family="monospace" font-size="10">toolbar-slot (per-scene tools)</text>
              <rect x="120" y="80" width="160" height="80" fill="var(--ds-bg-elevated, #23262f)" stroke="var(--ds-border-muted, #2e3340)"/>
              <text x="200" y="120" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="10">modal-slot / subtype-slot</text>
              <text x="200" y="138" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="9">toast-slot top-right</text>
            </svg>
          glossary_anchors:
            - { term: 'UIDocument', spec_ref: 'Unity UI Toolkit' }
            - { term: 'UXML', spec_ref: 'DEC-A28' }
            - { term: 'Host MonoBehaviour', spec_ref: 'DEC-A28' }
            - { term: 'Subtype picker (RCIS)', spec_ref: 'ui §3.7' }
          failure_modes:
            - 'Fails if UXML slot names diverge from C# Slot(name) string literals — runtime query returns null'
            - 'Fails if USS file path lags UXML reference — Unity logs Style-not-found warning'
          decision_dependencies:
            - { slug: 'DEC-A28', role: 'inherits' }
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/Domains/IsoSceneCore/UI/IsoSceneUIShellHost.cs', loc: null, kind: 'new', summary: 'Host MonoBehaviour rooting the shared UIDocument; exposes Slot(name) plugin API' }
            - { path: 'Assets/UI/Generated/iso-scene-ui-shell.uxml', loc: null, kind: 'new', summary: 'Hand-authored UXML with 5 named slots; hud + toolbar + subtype + modal + toast' }
            - { path: 'Assets/UI/Generated/iso-scene-ui-shell.uss', loc: null, kind: 'new', summary: 'Theme + layout styles for shell slots; ties into ds-* tokens via dark theme' }
      - id: '1.2.2'
        title: 'Refactor existing CityScene HUD/Toolbar/Modal/Toast → register into IsoSceneUIShellHost slots; UIManager hub keeps Inspector binding'
        prefix: TECH
        depends_on: ['1.2.1']
        digest_outline: |
          Move CityScene HUD + Toolbar + Modal + Toast UI registration through IsoSceneUIShellHost slots. UIManager + HudController + ToolbarController stay in current paths/class names; internally they call shell.Slot("hud-slot").Add(uxmlClone) etc. Goldens captured before refactor + diffed at task close — pixel-diff < 0.5%.
        touched_paths:
          - Assets/Scripts/UIManager.cs
          - Assets/Scripts/UI/HudController.cs
          - Assets/Scripts/UI/ToolbarController.cs
        kind: code
        enriched:
          before_after_code:
            path: Assets/Scripts/UIManager.cs
            before: |
              [SerializeField] private UIDocument hudDocument;
              [SerializeField] private UIDocument toolbarDocument;
              void Awake() { /* 2 separate roots */ }
            after: |
              [SerializeField] private UIDocument hudDocument;       // Inspector untouched
              [SerializeField] private UIDocument toolbarDocument;   // Inspector untouched
              private IsoSceneUIShellHost _shell;
              void Start()
              {
                  _shell = ServiceRegistry.Resolve<IsoSceneUIShellHost>();
                  _shell.Slot("hud-slot").Add(hudDocument.rootVisualElement);
                  _shell.Slot("toolbar-slot").Add(toolbarDocument.rootVisualElement);
              }
          glossary_anchors:
            - { term: 'Host MonoBehaviour', spec_ref: 'DEC-A28' }
            - { term: 'ModalCoordinator', spec_ref: 'Assets/Scripts/UI/Modals/ModalCoordinator.cs' }
          failure_modes:
            - 'Fails if UIManager.cs renamed — Inspector script-ref breaks across all scenes'
            - 'Fails if shell.Slot returns null because slot name typo'
          decision_dependencies:
            - { slug: 'DEC-A28', role: 'inherits' }
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/UIManager.cs', loc: null, kind: 'extend', summary: 'Hub facade: Inspector serialization untouched; Start() registers HUD + Toolbar into shell slots' }
            - { path: 'Assets/Scripts/UI/HudController.cs', loc: null, kind: 'extend', summary: 'HUD controller no longer owns root UIDocument lifetime; binds into shell hud-slot' }
            - { path: 'Assets/Scripts/UI/ToolbarController.cs', loc: null, kind: 'extend', summary: 'Toolbar controller registers visual element into toolbar-slot via shell' }
      - id: '1.2.3'
        title: 'Define IIsoSceneToolRegistry + IIsoSceneSubtypePicker + IIsoSceneZonePaintHost interfaces; CityScene tools register'
        prefix: TECH
        depends_on: ['1.2.2']
        digest_outline: |
          Create three interfaces under Assets/Scripts/Domains/IsoSceneCore/Contracts/. CityScene registers existing tools (road, forest, bulldozer) into IIsoSceneToolRegistry; existing subtype picker plugged into IIsoSceneSubtypePicker; zone-paint host stubbed (region zones definitions deferred). Stage 1.2 regression baseline green at task close.
        touched_paths:
          - Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoSceneToolRegistry.cs
          - Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoSceneSubtypePicker.cs
          - Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoSceneZonePaintHost.cs
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'Subtype picker (RCIS)', spec_ref: 'ui §3.7' }
            - { term: 'ZoneSubTypeRegistry', spec_ref: 'econ#zone-sub-type-registry' }
          failure_modes:
            - 'Fails if interface namespace mismatches asmdef root — Unity assembly graph rejects compile'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoSceneToolRegistry.cs', loc: null, kind: 'new', summary: 'Per-scene tool registration into shared toolbar slot' }
            - { path: 'Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoSceneSubtypePicker.cs', loc: null, kind: 'new', summary: 'Generic subtype picker; scenes register catalogs at Start' }
            - { path: 'Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoSceneZonePaintHost.cs', loc: null, kind: 'new', summary: 'Paint mechanism shell; region zone definitions deferred' }
  - id: '2.0'
    title: RegionScene terrain — heightful 64×64 with water + cliff layers
    exit: |
      RegionHeightMap + RegionWaterMap + RegionCliffMap drive RegionCellRenderer; camera pans + chunks cull identical to CityScene. RegionScene grid renders grass + water-slope + cliff cells via shared camera + chunk culling.
    red_stage_proof: |
      # visibility_delta red test (red on 2.0.1; green on 2.0.4 close)
      def test_region_64x64_renders_with_height_water_cliff():
          load_scene("RegionScene")
          seed_region_height_map(deterministic_seed=42)
          renderer = find_component("RegionCellRenderer")
          visible_cells = renderer.visible_cells
          assert len(visible_cells) > 0                                # red at 2.0.1 (no renderer)
          baseline = load_golden("region-64x64-terrain-seed42.png")
          screenshot = capture_screenshot_play_mode()
          assert pixel_diff(screenshot, baseline) < 1.0%               # green at 2.0.4
          assert camera_chunk_cull_matches_cityscene_behavior()        # parity invariant
    red_stage_proof_block:
      red_test_anchor: 'visibility-delta-test:Assets/Scripts/RegionScene/Domains/Terrain/RegionCellRenderer.cs::Region64x64TerrainRendersWithHeightWaterCliff'
      target_kind: visibility_delta
      proof_artifact_id: 'tests/region-scene-prototype/stage2.0-terrain.test.cs'
      proof_status: failed_as_expected
    enriched:
      edge_cases:
        - { input: 'Cell (0,0) has water + cliff flags both true', state: 'RegionHeightMap seeded with edge case', expected: 'RegionWaterMap precedence over CliffMap per existing CityScene cliff rules (invariant #9 south+east faces only)' }
        - { input: 'Camera pans beyond grid edge', state: 'Visible-set query returns cells with x>=64', expected: 'RegionCellRenderer clamps to GridSize bounds; out-of-range cells skipped silently' }
        - { input: 'RegionHeightMap.HeightAt called before terrain seeded', state: 'Scene Start order race', expected: 'Returns 0; renderer draws flat grass; subsequent reseed redraws on next visible-set delta' }
      shared_seams:
        - { name: 'IIsoHeightMap', producer_stage: '2.0', consumer_stages: ['3.0'], contract: 'RegionHeightMap implements; consumed by RegionCellRenderer + RegionCellInspectorPanel for terrain readout' }
    tasks:
      - id: '2.0.1'
        title: 'RegionHeightMap — per-cell elevation, 64×64'
        prefix: TECH
        depends_on: ['1.2.3']
        digest_outline: |
          Create RegionHeightMap.cs (Assets/Scripts/RegionScene/Domains/Terrain/). int[64,64] elevation array; implements IIsoHeightMap (new interface under IsoSceneCore/Contracts/IIsoHeightMap.cs). HeightAt(x,y) / GridSize methods. Procedural seed using simple noise for prototype.
        touched_paths:
          - Assets/Scripts/RegionScene/Domains/Terrain/RegionHeightMap.cs
          - Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoHeightMap.cs
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'Water map data', spec_ref: 'persist §Save, geo §11.5' }
            - { term: 'CellData', spec_ref: 'persist §Save, geo §11.2' }
          failure_modes:
            - 'Fails if HeightMap[x,y] vs Cell.height invariant #1 not honored across region cells when persistence lands in Stage 4.0 — separate region invariant must be added'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/Domains/Terrain/RegionHeightMap.cs', loc: null, kind: 'new', summary: 'Per-cell elevation int array; implements IIsoHeightMap; procedural seed for prototype' }
            - { path: 'Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoHeightMap.cs', loc: null, kind: 'new', summary: 'Abstract height/water/cliff query interface; CityHeightMap + RegionHeightMap implement' }
      - id: '2.0.2'
        title: 'RegionWaterMap — per-cell water/slope flag, drains per HeightMap'
        prefix: TECH
        depends_on: ['2.0.1']
        digest_outline: |
          Create RegionWaterMap.cs. bool[64,64] water flag + slope direction enum. Drainage rule mirrors CityScene shore band (invariant #7); rivers monotonic non-increasing (invariant #8). Procedural water seeding for prototype.
        touched_paths:
          - Assets/Scripts/RegionScene/Domains/Terrain/RegionWaterMap.cs
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'Water map data', spec_ref: 'persist §Save, geo §11.5' }
          failure_modes:
            - 'Fails if shore band rule violated — land cell Moore-adjacent to water with height > min(neighbor S) (invariant #7)'
            - 'Fails if river bed monotonic non-increasing rule violated (invariant #8)'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/Domains/Terrain/RegionWaterMap.cs', loc: null, kind: 'new', summary: 'Per-cell water flag + slope direction; drainage rules mirror CityScene invariants #7 + #8' }
      - id: '2.0.3'
        title: 'RegionCliffMap — per-cell cliff flag, reuses cliff rules'
        prefix: TECH
        depends_on: ['2.0.2']
        digest_outline: |
          Create RegionCliffMap.cs. bool[64,64] cliff flag. Visible faces south+east only (invariant #9). Computed from RegionHeightMap height deltas + RegionWaterMap shore band.
        touched_paths:
          - Assets/Scripts/RegionScene/Domains/Terrain/RegionCliffMap.cs
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'CellData', spec_ref: 'persist §Save, geo §11.2' }
          failure_modes:
            - 'Fails if cliff faces emitted on north or west sides — invariant #9 breach'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/Domains/Terrain/RegionCliffMap.cs', loc: null, kind: 'new', summary: 'Per-cell cliff flag; visible faces south+east only per invariant #9' }
      - id: '2.0.4'
        title: 'RegionCellRenderer — sprite per cell kind; camera/cull integration'
        prefix: TECH
        depends_on: ['2.0.3']
        digest_outline: |
          Create RegionCellRenderer.cs. Subscribes to IsoSceneChunkCuller visible-set delta event; renders sprite per cell kind (grass / water-slope / cliff-south / cliff-east) using RegionHeightMap + RegionWaterMap + RegionCliffMap. Camera pan + culling parity with CityScene. RegionManager.Start() wires renderer.
        touched_paths:
          - Assets/Scripts/RegionScene/Domains/Terrain/RegionCellRenderer.cs
          - Assets/Scripts/RegionScene/RegionManager.cs
        kind: code
        enriched:
          visual_mockup_svg: |
            <svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
              <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
              <text x="200" y="20" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="11">RegionScene — Stage 2.0.4 64×64 terrain</text>
              <g transform="translate(80, 50)">
                <polygon points="120,0 240,60 120,120 0,60" fill="var(--ds-accent-grass, #6a8e4e)"/>
                <polygon points="120,0 200,40 160,60 120,40" fill="var(--ds-accent-cool, #4e6e8e)" opacity="0.8"/>
                <polygon points="240,60 240,80 160,140 120,120" fill="var(--ds-accent-cliff, #5a4a3e)" opacity="0.9"/>
              </g>
              <text x="200" y="225" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="10">grass / water-slope / cliff faces (S+E only)</text>
            </svg>
          glossary_anchors:
            - { term: 'CellData', spec_ref: 'persist §Save, geo §11.2' }
          failure_modes:
            - 'Fails if renderer queries cells outside visible-set — out-of-bounds reads; renderer must consume culler delta as source of truth'
            - 'Fails if sprite sort order vs isometric depth not aligned — back cliffs paint over front grass'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/Domains/Terrain/RegionCellRenderer.cs', loc: null, kind: 'new', summary: 'Sprite-per-cell renderer; subscribes to chunk-culler visible-set; reads Region{Height,Water,Cliff}Map' }
            - { path: 'Assets/Scripts/RegionScene/RegionManager.cs', loc: null, kind: 'extend', summary: 'Start() now wires RegionCellRenderer + binds to IsoSceneChunkCuller delta event' }
  - id: '3.0'
    title: RegionScene UI panels + cell click dispatch
    exit: |
      Three UIDocuments register into IsoSceneCore slots; click dispatcher routes left/right; "Enter City" disabled placeholder. Hover on region cell shows hover panel; left-click shows inspector; right-click shows city summary panel.
    red_stage_proof: |
      # tracer_verb red test (red on 3.0.1; green on 3.0.3 close)
      def test_click_routes_to_inspector_and_hover_panels():
          load_scene("RegionScene")
          hover_cell(GridCoord(10, 10))
          assert find_uidocument("RegionCellHoverPanel").root.is_visible    # red at 3.0.1
          left_click_cell(GridCoord(10, 10))
          assert find_uidocument("RegionCellInspectorPanel").root.is_visible # red at 3.0.2
          city_cell = seed_owned_city_at(GridCoord(31, 31))
          right_click_cell(city_cell)
          summary = find_uidocument("RegionCitySummaryPanel")
          assert summary.root.is_visible                                     # red at 3.0.3
          enter_btn = summary.query("enter-city-btn")
          assert enter_btn.classes.contains("disabled")                      # placeholder
    red_stage_proof_block:
      red_test_anchor: 'tracer-verb-test:Assets/Scripts/RegionScene/UI/RegionCellClickHandler.cs::ClickRoutesToInspectorAndHoverPanels'
      target_kind: tracer_verb
      proof_artifact_id: 'tests/region-scene-prototype/stage3.0-ui-panels.test.cs'
      proof_status: failed_as_expected
    enriched:
      edge_cases:
        - { input: 'Hover panel re-fires every frame the cell is hovered', state: 'Stage 3.0.1 naive impl without debounce', expected: 'Hover-debounce timer carried forward as SUGGESTION-2; non-blocking for prototype but visual jitter possible' }
        - { input: 'Right-click on empty cell (no owning city)', state: 'cellData.HasCity == false', expected: 'No panel opens; early return in RegionCellClickHandler' }
        - { input: 'Click on out-of-bounds cell (camera near grid edge)', state: 'cell coords -1 or >=64', expected: 'InBounds guard at top of OnClick; silent no-op' }
      shared_seams:
        - { name: 'IIsoSceneCellClickDispatcher', producer_stage: '3.0', consumer_stages: ['5.0'], contract: 'Left/right click routing; RegionToolCreateCity also subscribes for cell-placement events' }
    tasks:
      - id: '3.0.1'
        title: 'RegionCellHoverPanel UIDocument'
        prefix: TECH
        depends_on: ['2.0.4']
        digest_outline: |
          Hand-author region-cell-hover.uxml + .uss (terrain kind label + height value + owning-city hint). Create RegionCellHoverPanel.cs Host MonoBehaviour binding UIDocument; registers into IsoSceneUIShellHost modal-slot or dedicated hover-slot. Subscribes to IsoSceneCellHoverDispatcher.
        touched_paths:
          - Assets/Scripts/RegionScene/UI/RegionCellHoverPanel.cs
          - Assets/UI/Generated/region-cell-hover.uxml
          - Assets/UI/Generated/region-cell-hover.uss
        kind: code
        enriched:
          visual_mockup_svg: |
            <svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
              <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
              <rect x="240" y="60" width="140" height="80" fill="var(--ds-bg-elevated, #23262f)" stroke="var(--ds-border-muted, #2e3340)" rx="4"/>
              <text x="310" y="82" text-anchor="middle" fill="var(--ds-text-primary, #e6e9ef)" font-family="monospace" font-size="11">Region cell [10,10]</text>
              <text x="310" y="102" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="10">Terrain: grass</text>
              <text x="310" y="118" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="10">Height: 3</text>
              <text x="310" y="134" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="9">(no city)</text>
            </svg>
          glossary_anchors:
            - { term: 'UIDocument', spec_ref: 'Unity UI Toolkit' }
            - { term: 'UXML', spec_ref: 'DEC-A28' }
            - { term: 'Host MonoBehaviour', spec_ref: 'DEC-A28' }
          failure_modes:
            - 'Fails if hover panel re-binds VisualElement on every Show() call — memory leak from undisposed handlers'
          decision_dependencies:
            - { slug: 'DEC-A28', role: 'inherits' }
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/UI/RegionCellHoverPanel.cs', loc: null, kind: 'new', summary: 'Host MonoBehaviour for hover panel; subscribes to IsoSceneCellHoverDispatcher' }
            - { path: 'Assets/UI/Generated/region-cell-hover.uxml', loc: null, kind: 'new', summary: 'Hand-authored UXML; terrain kind + height + owning-city hint labels' }
            - { path: 'Assets/UI/Generated/region-cell-hover.uss', loc: null, kind: 'new', summary: 'Hover panel styles; ds-* tokens; pinned to mouse position' }
      - id: '3.0.2'
        title: 'RegionCellInspectorPanel + RegionCitySummaryPanel UIDocuments'
        prefix: TECH
        depends_on: ['3.0.1']
        digest_outline: |
          Hand-author region-cell-inspector.uxml + .uss (terrain readout, pop, urban area). Hand-author region-city-summary.uxml + .uss (city name, pop, "Enter City" button disabled). Create RegionCellInspectorPanel.cs + RegionCitySummaryPanel.cs Host MonoBehaviours; register into IsoSceneUIShellHost modal-slot.
        touched_paths:
          - Assets/Scripts/RegionScene/UI/RegionCellInspectorPanel.cs
          - Assets/Scripts/RegionScene/UI/RegionCitySummaryPanel.cs
          - Assets/UI/Generated/region-cell-inspector.uxml
          - Assets/UI/Generated/region-city-summary.uxml
        kind: code
        enriched:
          visual_mockup_svg: |
            <svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
              <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
              <rect x="120" y="60" width="160" height="120" fill="var(--ds-bg-elevated, #23262f)" stroke="var(--ds-border-muted, #2e3340)" rx="6"/>
              <text x="200" y="84" text-anchor="middle" fill="var(--ds-accent-warm, #f4d28a)" font-family="monospace" font-size="13">City of Bacayo</text>
              <text x="200" y="106" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="10">Pop: 2,450</text>
              <text x="200" y="122" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="10">Urban: 4.8 km²</text>
              <rect x="148" y="142" width="104" height="22" fill="var(--ds-bg-muted, #2a2d36)" stroke="var(--ds-border-muted, #2e3340)" opacity="0.4"/>
              <text x="200" y="158" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="10">Enter City (disabled)</text>
            </svg>
          glossary_anchors:
            - { term: 'ModalCoordinator', spec_ref: 'Assets/Scripts/UI/Modals/ModalCoordinator.cs' }
            - { term: 'Host MonoBehaviour', spec_ref: 'DEC-A28' }
          failure_modes:
            - 'Fails if both inspector + summary panels open simultaneously — modal-slot single-child contract'
            - 'Fails if Enter City button missing "disabled" class — placeholder click attempts scene transition (deferred)'
          decision_dependencies:
            - { slug: 'DEC-A28', role: 'inherits' }
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/UI/RegionCellInspectorPanel.cs', loc: null, kind: 'new', summary: 'Host for left-click cell inspector panel; pop + urban-area + terrain readout' }
            - { path: 'Assets/Scripts/RegionScene/UI/RegionCitySummaryPanel.cs', loc: null, kind: 'new', summary: 'Host for right-click city summary panel; Enter City button disabled (transition deferred)' }
            - { path: 'Assets/UI/Generated/region-cell-inspector.uxml', loc: null, kind: 'new', summary: 'Hand-authored UXML; stats grid for cell inspection' }
            - { path: 'Assets/UI/Generated/region-city-summary.uxml', loc: null, kind: 'new', summary: 'Hand-authored UXML; city headline + Enter City placeholder button' }
      - id: '3.0.3'
        title: 'RegionCellClickHandler + IIsoSceneCellClickDispatcher routing; Enter City disabled placeholder'
        prefix: TECH
        depends_on: ['3.0.2']
        digest_outline: |
          Create IIsoSceneCellClickDispatcher.cs interface under Contracts/. Create RegionCellClickHandler.cs implementing IIsoSceneCellClickHandler; subscribes via dispatcher. Left = inspector; Right + HasCity = summary. Stage 3.0 regression test green.
        touched_paths:
          - Assets/Scripts/RegionScene/UI/RegionCellClickHandler.cs
          - Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoSceneCellClickDispatcher.cs
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'service registry', spec_ref: 'docs/post-atomization-architecture.md §Service Registry' }
          failure_modes:
            - 'Fails if dispatcher subscribes handler in Awake — invariant #12 race'
            - 'Fails if click handler does not guard out-of-bounds cell coords from camera-edge mouse projection'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/UI/RegionCellClickHandler.cs', loc: null, kind: 'new', summary: 'Region click handler; routes left=inspector, right+HasCity=summary' }
            - { path: 'Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoSceneCellClickDispatcher.cs', loc: null, kind: 'new', summary: 'Left/right click dispatch contract; per-scene handlers subscribe via Subscribe(handler)' }
  - id: '4.0'
    title: Evolution + save — pop + urban-area per global tick + new FS save file
    exit: |
      RegionEvolutionService subscribes to IsoSceneTickBus; RegionSaveService writes/reads new FS file linking region ↔ cities; RegionUnlockGate flag flows from CityScene. Region cells animate small pop/urban-area changes per tick; save creates new file; reload restores state.
    red_stage_proof: |
      # unit red test (red on 4.0.1; green at 4.0.3 close)
      def test_evolves_pop_and_urban_area_per_tick():
          load_scene("RegionScene")
          seed_city_at(GridCoord(31, 31), pop=1000, urban_area=2.0)
          publish_tick(IsoTickKind.GlobalTick)
          cell = region_data.at(GridCoord(31, 31))
          assert cell.pop > 1000                                          # red at 4.0.1
          assert cell.urban_area > 2.0                                    # red at 4.0.1
          save_path = region_save_service.write_save("test-save")
          assert path_exists(save_path)                                   # red at 4.0.2
          loaded = region_save_service.load_save("test-save")
          assert loaded.cells[31, 31].pop == cell.pop                     # round-trip
          assert game_save_data.region_unlocked == True                   # red at 4.0.3
    red_stage_proof_block:
      red_test_anchor: 'unit-test:Assets/Scripts/RegionScene/Domains/Evolution/RegionEvolutionService.cs::EvolvesPopAndUrbanAreaPerTick'
      target_kind: unit
      proof_artifact_id: 'tests/region-scene-prototype/stage4.0-evolution-save.test.cs'
      proof_status: failed_as_expected
    enriched:
      edge_cases:
        - { input: 'Tick fires before RegionData resolved (scene mid-load)', state: 'RegionEvolutionService.OnTick called early', expected: 'null guard at top of OnTick returns; first ticks dropped harmlessly' }
        - { input: 'Save file exists but schema version older than current', state: 'Player loads legacy save', expected: 'RegionSaveService.MigrateLoadedSaveData bumps schema; missing region data initialized to defaults; unlock=false' }
        - { input: 'Two cities placed at same region cell (race in tool placement)', state: 'Stage 5.0 dependent; this stage tests single-city case', expected: 'Documented; collision handled by tool-side guard in 5.0.3' }
      shared_seams:
        - { name: 'IsoSceneTickBus subscription contract', producer_stage: '1.1', consumer_stages: ['4.0'], contract: 'RegionEvolutionService.Start subscribes; OnTick null-guards on RegionData' }
        - { name: 'RegionSaveService', producer_stage: '4.0', consumer_stages: ['5.0'], contract: 'New FS save file linking region ↔ cities; Stage 5.0 lazy-creates CityData entries into same file' }
    tasks:
      - id: '4.0.1'
        title: 'RegionEvolutionService — pop + urban-area per global tick; subscribes via IsoSceneTickBus'
        prefix: TECH
        depends_on: ['3.0.3']
        digest_outline: |
          Create RegionEvolutionService.cs MonoBehaviour. Start() resolves IsoSceneTickBus + RegionData (Register<RegionData> in RegionManager.Awake). Subscribe(this, IsoTickKind.GlobalTick). OnTick evolves pop + urban_area per owned region cell. RegionCellData POCO carries pop + urban_area + owning_city_id.
        touched_paths:
          - Assets/Scripts/RegionScene/Domains/Evolution/RegionEvolutionService.cs
          - Assets/Scripts/RegionScene/Domains/Evolution/RegionCellData.cs
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'Urban growth rings', spec_ref: 'sim §Rings' }
            - { term: 'CellData', spec_ref: 'persist §Save, geo §11.2' }
          failure_modes:
            - 'Fails if Subscribe in Awake — invariant #12 race'
            - 'Fails if OnTick reads RegionData while null — scene mid-load tick'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/Domains/Evolution/RegionEvolutionService.cs', loc: null, kind: 'new', summary: 'MonoBehaviour subscribes to IsoSceneTickBus; evolves pop + urban-area per region cell' }
            - { path: 'Assets/Scripts/RegionScene/Domains/Evolution/RegionCellData.cs', loc: null, kind: 'new', summary: 'POCO cell record: terrain kind + pop + urban_area + owning_city_id?' }
      - id: '4.0.2'
        title: 'RegionSaveService — new FS save file format linking region ↔ cities; save + load'
        prefix: TECH
        depends_on: ['4.0.1']
        digest_outline: |
          Create RegionSaveService.cs + RegionSaveFile.cs DTO. Writes <save>.region.json alongside GameSaveData; carries region grid + per-cell pop + urban_area + city ownership map. Load path: deserialize + populate RegionData. Schema version field for future migrations (SUGGESTION-1 from review notes).
        touched_paths:
          - Assets/Scripts/RegionScene/Domains/Persistence/RegionSaveService.cs
          - Assets/Scripts/RegionScene/Domains/Persistence/RegionSaveFile.cs
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'Save data', spec_ref: 'persist §Save' }
            - { term: 'Multi-scale save tree', spec_ref: 'ms' }
            - { term: 'Parent region / country id', spec_ref: 'persist §Save' }
          failure_modes:
            - 'Fails if save file written to wrong path — must align with GameSaveData base path convention'
            - 'Fails if load round-trip drops fields — JsonUtility serializer requires public fields on RegionSaveFile DTO'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
            - { slug: 'DEC-A10', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/Domains/Persistence/RegionSaveService.cs', loc: null, kind: 'new', summary: 'FS save/load service; writes <save>.region.json sidecar to GameSaveData' }
            - { path: 'Assets/Scripts/RegionScene/Domains/Persistence/RegionSaveFile.cs', loc: null, kind: 'new', summary: 'Serializable DTO; carries grid + per-cell evolution state + city-ownership map + schema_version' }
      - id: '4.0.3'
        title: 'RegionUnlockGate — CityData flag enables RegionScene access from main menu'
        prefix: TECH
        depends_on: ['4.0.2']
        digest_outline: |
          Create RegionUnlockGate.cs. Reads region_unlocked flag from GameSaveData (new bool field; schema_version bump + migrator). Hardcoded unlock cond for prototype: city pop ≥ 1000 OR cheat flag. CityData carries flag write. MainMenuController checks gate; greys "Open Region" entry until unlocked. Constant lives in single named static (SUGGESTION-3 from review notes).
        touched_paths:
          - Assets/Scripts/RegionScene/RegionUnlockGate.cs
          - Assets/Scripts/CityData.cs
          - Assets/Scripts/UI/MainMenu/MainMenuController.cs
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'Save data', spec_ref: 'persist §Save' }
            - { term: 'Scale switch', spec_ref: 'ms, ms-post' }
          failure_modes:
            - 'Fails if GameSaveData schema bump missing migrator — legacy saves crash on load'
            - 'Fails if unlock cond constant duplicated across files — single named static enforced per SUGGESTION-3'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/RegionUnlockGate.cs', loc: null, kind: 'new', summary: 'Reads region_unlocked flag; hardcoded prototype cond city pop ≥ 1000; single-constant config' }
            - { path: 'Assets/Scripts/CityData.cs', loc: null, kind: 'extend', summary: 'Adds region_unlocked bool field + schema bump; CityData persists flag through GameSaveData' }
            - { path: 'Assets/Scripts/UI/MainMenu/MainMenuController.cs', loc: null, kind: 'extend', summary: 'Main menu checks gate; "Open Region" entry greys when locked' }
  - id: '5.0'
    title: Region city-creation tool — register into shared toolbar, lazy CityData on first entry
    exit: |
      RegionToolCreateCity registers into IIsoSceneToolRegistry; subtype picker integrates; save file shows new CityData link. Region toolbar shows "Found city" tool; clicking empty cell creates lazy CityData entry; "Enter City" stays disabled.
    red_stage_proof: |
      # tracer_verb red test (red on 5.0.1; green at 5.0.3 close)
      def test_places_city_and_creates_lazy_city_data():
          load_scene("RegionScene")
          tool_registry = service_registry.resolve("IIsoSceneToolRegistry")
          assert tool_registry.tools_for_slot(ToolbarSlot.Primary).contains("region.create-city")  # red at 5.0.1
          select_tool("region.create-city")
          left_click_cell(GridCoord(20, 20))
          assert region_data.at(GridCoord(20, 20)).has_city == True
          new_city_id = region_data.at(GridCoord(20, 20)).owning_city_id
          loaded_save = region_save_service.load_save("test-save")
          assert loaded_save.cities.contains(new_city_id)                                          # red at 5.0.3
    red_stage_proof_block:
      red_test_anchor: 'tracer-verb-test:Assets/Scripts/RegionScene/Tools/RegionToolCreateCity.cs::PlacesCityAndCreatesLazyCityData'
      target_kind: tracer_verb
      proof_artifact_id: 'tests/region-scene-prototype/stage5.0-create-city-tool.test.cs'
      proof_status: failed_as_expected
    enriched:
      edge_cases:
        - { input: 'Click on cell already owned by another city', state: 'cellData.has_city == true', expected: 'Tool no-op (early return); future enhancement: toast "Cell occupied"' }
        - { input: 'Click on water-slope or cliff cell', state: 'cellData.terrain_kind in {WaterSlope, Cliff}', expected: 'Tool no-op or terrain-conflict toast; design TBD (open question) but prototype accepts placement' }
        - { input: 'Save fired mid-placement (race)', state: 'RegionSaveService.Write called between LinkCity and CityDataFactory.CreateLazy completion', expected: 'Atomic write contract: LinkCity + CreateLazy + save mutation locked in tool action body' }
      shared_seams:
        - { name: 'IIsoSceneToolRegistry consumption', producer_stage: '1.2', consumer_stages: ['5.0'], contract: 'RegionToolCreateCity registers into ToolbarSlot.Primary; tool surface backed by IsoSceneTool base class' }
    tasks:
      - id: '5.0.1'
        title: 'RegionToolCreateCity — region-level city-placement tool; registers into IIsoSceneToolRegistry'
        prefix: TECH
        depends_on: ['4.0.3']
        digest_outline: |
          Create RegionToolCreateCity.cs extending IsoSceneTool. Slug = "region.create-city"; Slot = ToolbarSlot.Primary; Icon from Resources/Icons/region-found-city. OnCellClicked guards on cellData.has_city + terrain conflict. RegionManager.Start() registers tool via _toolReg.Register(new RegionToolCreateCity()).
        touched_paths:
          - Assets/Scripts/RegionScene/Tools/RegionToolCreateCity.cs
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'Subtype picker (RCIS)', spec_ref: 'ui §3.7' }
            - { term: 'Toolbar family subtype enumeration (MVP)', spec_ref: 'mvp#toolbar-family-subtype-enumeration-mvp-picker-scope' }
          failure_modes:
            - 'Fails if tool registers in Awake instead of Start — invariant #12 race against registry resolve'
            - 'Fails if Resources.Load returns null for icon — toolbar slot renders empty button'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/Tools/RegionToolCreateCity.cs', loc: null, kind: 'new', summary: 'Region city-placement tool; extends IsoSceneTool; OnCellClicked creates lazy CityData + links region cell' }
      - id: '5.0.2'
        title: 'Subtype picker integration — city subtype catalog at region scale'
        prefix: TECH
        depends_on: ['5.0.1']
        digest_outline: |
          Create RegionSubtypeCatalog.cs. Registers into IIsoSceneSubtypePicker at scene Start. Catalog entries: small / medium / large city footprint (placeholder for now; full subtype evolution deferred). Tool consumes selected subtype on placement.
        touched_paths:
          - Assets/Scripts/RegionScene/UI/RegionSubtypeCatalog.cs
        kind: code
        enriched:
          glossary_anchors:
            - { term: 'Subtype picker (RCIS)', spec_ref: 'ui §3.7' }
            - { term: 'Picker universal rule', spec_ref: 'mvp#toolbar-family-subtype-enumeration-mvp-picker-scope' }
            - { term: 'ZoneSubTypeRegistry', spec_ref: 'econ#zone-sub-type-registry' }
          failure_modes:
            - 'Fails if catalog entries lack stable slugs — picker selection persists across scene loads as null'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/UI/RegionSubtypeCatalog.cs', loc: null, kind: 'new', summary: 'Region-scale subtype catalog; small/medium/large city footprint entries' }
      - id: '5.0.3'
        title: 'Lazy CityData create on city placement; new entry in save file'
        prefix: TECH
        depends_on: ['5.0.2']
        digest_outline: |
          Extend RegionSaveService to accept LinkCity(cell, cityId) mutation. CityDataFactory.CreateLazy creates new CityData record (id, name placeholder, pop 0, owning_region_cell). RegionToolCreateCity.OnCellClicked calls LinkCity → CreateLazy → save mutation. Save file shows new city entry; "Enter City" stays disabled placeholder.
        touched_paths:
          - Assets/Scripts/RegionScene/Domains/Persistence/RegionSaveService.cs
          - Assets/Scripts/CityData.cs
        kind: code
        enriched:
          visual_mockup_svg: |
            <svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
              <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
              <text x="200" y="20" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="11">RegionScene — Stage 5.0.3 lazy city placed</text>
              <g transform="translate(80, 50)">
                <polygon points="120,0 240,60 120,120 0,60" fill="var(--ds-accent-grass, #6a8e4e)"/>
                <rect x="100" y="40" width="40" height="20" fill="var(--ds-accent-warm, #f4d28a)" stroke="var(--ds-text-primary, #e6e9ef)"/>
                <text x="120" y="54" text-anchor="middle" fill="var(--ds-text-canvas, #1a1d24)" font-family="monospace" font-size="9">[new]</text>
              </g>
              <text x="200" y="200" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="10">CityData lazy-created; Enter City disabled</text>
              <text x="200" y="225" text-anchor="middle" fill="var(--ds-text-success, #8ac28a)" font-family="monospace" font-size="10">save file: cities += new entry</text>
            </svg>
          glossary_anchors:
            - { term: 'Save data', spec_ref: 'persist §Save' }
            - { term: 'Multi-scale save tree', spec_ref: 'ms' }
            - { term: 'CellData', spec_ref: 'persist §Save, geo §11.2' }
          failure_modes:
            - 'Fails if CityData id collision — must use reserve-id.sh pattern or runtime UUID for lazy ids'
            - 'Fails if save mutation not atomic — partial write leaves CityData entry without RegionData link'
          decision_dependencies:
            - { slug: 'DEC-A29', role: 'inherits' }
            - { slug: 'DEC-A10', role: 'inherits' }
            - { slug: 'DEC-A26', role: 'inherits' }
          touched_paths_with_preview:
            - { path: 'Assets/Scripts/RegionScene/Domains/Persistence/RegionSaveService.cs', loc: null, kind: 'extend', summary: 'LinkCity mutation extends save; atomic write contract with CityDataFactory.CreateLazy' }
            - { path: 'Assets/Scripts/CityData.cs', loc: null, kind: 'extend', summary: 'CityDataFactory.CreateLazy emits new minimal CityData; owning_region_cell field added' }
---

# RegionScene Prototype — Exploration Seed

> **REMINDER — prototype scope + post-prototype expansion.** This plan unblocks scene zoom-transition work + closes MVP gap (region-scale view absent in most genre peers). Stage 1.0–5.0 ship a CityScene-shaped RegionScene (same 64×64 grid, same terrain prefab kinds reskinned, same height/water/cliff logic). Post-prototype expansion (separate plan, not in scope here) replaces region geography wholesale — distinct region-scale terrain prefabs, new geo-feature set, new terrain-height definition, new geo logic (region cliffs/slopes/water/forest scaling rules), plus a full art pass on region-tile sprites. Treat current prototype as the unblock; do NOT over-invest in geo-tile art or geo-logic generality during prototype.

**Status:** Design expansion complete. Ready for `/master-plan-new`.
**Gate:** Run only after `ui-toolkit-migration` plan ships.
**Source:** Derived from `docs/explorations/assets/city-scene-loading-research.md` Design Expansion — RegionScene + Zoom, corrected 2026-05-13.

---

## Problem Statement

The game currently has only one map view: CityScene, a 64×64 isometric grid where the player manages a city. There is no zoomed-out regional view. The RegionScene is a new map layer that shows the broader region the city sits within — other cities, roads, forests, terrain — at a coarser visual scale. The player needs to be able to navigate, terraform, and eventually found new cities from this view.

RegionScene must feel like a natural extension of CityScene: same isometric camera, same toolbar/HUD layout, same interaction model — but operating at region scale with its own set of tools and cell types.

---

## Known Design Decisions

### Grid

- RegionScene uses a **64×64 grid of region-cells** — same resolution as CityScene.
- Each region-cell is visually larger than a city-cell (exact world-unit size TBD during grilling).
- Region-cell terrain types: **grass, slopes, water slopes** — human-made prefabs (not reused from CityScene).

### City-to-region mapping rule

- **32×32 city-cells = 1 region-cell.**
- A standard 64×64 city therefore occupies **2×2 region-cells**.
- The player's city footprint is anchored at the **(0,0) corner** of its 2×2 region-cell area.
- Player city starts at the **center** of the 64×64 region grid (approximately cells [31–32, 31–32]).
- Future city sizes (non-64×64) use the same 32×32 chunk rule; transformation undefined beyond 64×64 for now.

### Neighbor regions

- The region grid shows **neighbor regions** surrounding the player's region. How these are generated (procedurally, from seed, pre-authored) is an open question.

### Basic UI (same layout as CityScene)

| Tool | Icon | Prefab |
|---|---|---|
| Road tool | Same icon as CityScene | New human-made region-road prefab |
| Forest tool | Same icon as CityScene | New human-made region-forest prefab |
| Bulldozer tool | Same icon as CityScene | — |
| **Found City** | New human-made icon | New human-made prefab |

- Same HUD bar layout as CityScene.
- Same toolbar layout as CityScene.
- Same picker widget as CityScene.
- Mini-map: same style as CityScene mini-map, but rendering RegionScene grid.

### Code structure

- `RegionGridManager` — new hub MonoBehaviour, inspector-attached in `RegionScene.unity`. Structurally mirrors `GridManager` but operates on region-cells.
- New domain services under `Domains/Geography/Services/` for region-cell rendering, region data.
- Hub constraint (invariant #13): existing hubs not renamed/moved/deleted. `RegionGridManager` = new file, new scene.

---

## Open Questions (to be grilled by design-explore)

### Grid + terrain
1. What is the world-unit size of one region-cell vs one city-cell? (Determines camera orthographic size for RegionScene.)
2. Does region terrain have elevation (height values)? Or is RegionScene flat with terrain types only?
3. How are slopes and water slopes oriented? Same isometric rules as CityScene (south+east cliff faces only)?
4. What does the default procedurally generated RegionScene terrain look like? How much water? Forest density?

### City footprint + neighbors
5. How are neighbor regions generated? Procedurally from a seed? Pre-authored stubs? Empty until player founds cities?
6. Do existing `RegionalMap` + `TerritoryData` data structures survive or get replaced by a new region-cell model?
7. What does a neighboring city look like in RegionScene before the player zooms in? A filled 2×2 block? A sprite?

### Tools
8. **Road tool**: What does a region-road connect? Cities? Can roads span across city footprints?
9. **Forest tool**: Plants forest at region-cell scale — does this affect the CityScene when the player zooms in?
10. **Bulldozer**: Removes roads, forests, or terrain features? Can it affect a city footprint cell?
11. **Found City**: Player selects empty region-cell(s) to place a new city. How many cells does founding require? Is there a minimum distance from existing cities? What happens to the region-cell terrain under the new city?

### UI + HUD
12. What stats does the HUD bar show in RegionScene? (CityScene shows population, funds, etc.) Region-level equivalents?
13. Does the picker widget in RegionScene work identically to CityScene? What categories does it show?
14. Does the mini-map auto-generate from the region grid, or does it require a separate render pass?

### Save + load
15. How is RegionScene state saved? Extension of existing `GameSaveData`? Separate save file?
16. When the player saves in CityScene, does the city footprint in RegionScene update automatically?

### Performance
17. 64×64 region-cells at region scale — does the existing `ChunkCullingSystem` pattern apply, or is a new culling strategy needed?

---

## Approaches

*To be developed during `/design-explore docs/explorations/region-scene-prototype.md` session.*

---

## Notes

- This exploration feeds `region-scene-prototype` master plan.
- `city-region-zoom-transition` plan depends on this plan shipping first.
- Approach D (Addressables + Tilemap migration) is deferred and does not block this prototype.
- Date seeded: 2026-05-13.

---

## Design Expansion

### Architecture Decision

- **Decision id:** DEC-A29
- **Title:** iso-scene-core-shared-foundation
- **Status:** active
- **Surface:** `contracts/iso-scene-core-foundation` (NEW, kind=contract, id=40197, spec_path=`docs/explorations/region-scene-prototype.md`, spec_section=`Design Expansion — Architecture Decision`)
- **Rationale:** RegionScene is the second isometric scale-tier scene. One-off duplication (Approach A) creates two divergent stacks for camera/culling/tick/UI shell. Generic `ScaleTierScene<TCell,TState>` (Approach C) over-engineers before the third tier exists. Extract-then-fork (Approach D) risks fork drift. Shared `IsoSceneCore` (Approach B) extracts ONLY the proven shared seams (camera, chunk culling, tick bus, UI shell, registries, heightmap interface) as composable services + a plugin/registration pattern. CityScene refactors into the same shape via hub facades; RegionScene composes from day one.
- **Alternatives considered:** A one-off duplication / C generic scale-tier base class / D extract then fork. Matrix below.
- **Changelog enqueue:** job_id `9dbef740-ed8d-46dc-ba93-1149750b18e3` (cron drains to `arch_changelog`).
- **Drift scan:** 8 pre-existing affected stages flagged on unrelated surfaces (`data-flows/persistence`, `data-flows/initialization`) — NOT caused by DEC-A29 (new surface). Notable overlap: existing `multi-scale` master plan stages 4.0 + 14.0 — `/master-plan-new` MUST coordinate slug.

#### Hub-preservation rule (HARD, applies to every stage)

Unity Inspector-connected hub scripts (`GameManager`, `GridManager`, `GeographyInitService`, `UIManager`, future `RegionManager`, etc.) NEVER renamed, moved, or deleted during the iso-core refactor.

- Iso-core reusable logic → NEW scripts under `Assets/Scripts/Domains/IsoSceneCore/Services/` (or per-domain subfolder). Namespace `Territory.IsoSceneCore.*`.
- Hub scripts stay in current file path, class name, namespace, Inspector serialization untouched. They become thin facades holding refs to IsoSceneCore services + delegating via composition.
- New RegionScene hubs (e.g. `RegionManager`) are NEW scripts, freely designed at creation; once Inspector-wired into `RegionScene.unity`, the same rule applies — no later rename/move/delete.
- Carve-out for invariant #5 (GridManager grid access): IsoSceneCore services live under `Assets/Scripts/Domains/IsoSceneCore/Services/` and hold a composition reference to their owning hub (GridManager for CityScene; RegionManager for RegionScene); share trust boundary.

### Approach matrix

| Criterion | A — One-off duplicate | **B — Shared IsoSceneCore (LOCKED)** | C — Generic `ScaleTierScene<TCell,TState>` | D — Extract then fork |
|---|---|---|---|---|
| Constraint fit | Drift between scenes; toolbar/HUD inconsistencies appear within weeks. | Camera + culling + tick + UI shell shared by contract; per-scene plugins for terrain/sim/UI/tools. | Solves a 3rd-tier problem we don't have; type parameters force premature shape. | Same as B at start, then forks diverge silently. |
| Effort | Lowest now, highest later (every new tier = new copy). | Medium — one extract pass + composition wiring. | Highest — generic abstraction + 2 concrete instantiations. | Medium — extract once, refactor twice (fork divergence). |
| Output control | High per-scene, low cross-scene parity. | High cross-scene parity via shared seams; per-scene plug-ins keep local control. | Low — generic base resists per-scene exceptions. | High initially, low after first fork divergence. |
| Maintainability | Two copies to fix every bug. | Single source of truth for shared seams; plugins isolate per-scene logic. | Type-parameter churn each time semantics shift. | Forks drift; same as A in 6 months. |
| Dependencies / risk | Low coupling, high duplication. | New `IsoSceneCore` namespace + plugin pattern; refactor risk mitigated by hub-preservation rule. | High — base class refactors ripple across both scenes. | Refactor + fork-merge risk; worst of both worlds. |
| Scalability to future tiers | Linear cost per tier. | Incremental generification when 3rd tier arrives (proven seams only). | Best in theory; depends on getting the generic shape right pre-evidence. | Each fork = new code path. |

**Decision:** B locked. Rationale above. Future country-tier or world-tier scale will incrementally lift proven seams from RegionScene plug-ins back into IsoSceneCore when the 3rd concrete instance lands.

### Components

Namespaces + responsibilities. One line each.

#### `Territory.IsoSceneCore.*` (new — `Assets/Scripts/Domains/IsoSceneCore/`)

- `IIsoSceneHost` — composition contract; scene registers tools, subtype catalogs, inspector factories, click handler, heightmap impl.
- `IsoSceneCamera` — pan/zoom/displacement service; extracted from GridManager + camera code; GridManager remains hub facade.
- `IsoSceneChunkCuller` — visible-cell windowing service; subscribes to camera changes; raises visible-set deltas.
- `IsoSceneTickBus` — global tick subscription multiplexer; producer = `TimeManager` (unchanged); consumers = per-scene evolution services.
- `IIsoHeightMap` — interface; CityHeightMap + RegionHeightMap implement; reads height/water/cliff per cell.
- `IsoSceneUIShellHost` — UIDocument-rooted shell with slots for HUD, Toolbar, Subtype picker, Modal host, Toast surface; hand-authored UXML per parity-recovery patterns.
- `IIsoSceneToolRegistry` — per-scene tool registration into shared toolbar slots.
- `IIsoSceneSubtypePicker` — generic picker; scenes register subtype catalogs.
- `IIsoSceneZonePaintHost` — paint mechanism shell; per-scene zone definitions plug in.
- `IIsoSceneCellClickDispatcher` — left/right-click routing to per-scene handlers.
- `IsoSceneTimeControl` — speed/pause control bound to global tick.
- `IsoSceneCellHoverDispatcher` — hover event routing (companion to click).

#### `Territory.RegionScene.*` (new — `Assets/Scripts/RegionScene/`)

- `RegionManager` — NEW hub MonoBehaviour, Inspector-wired in `RegionScene.unity`. Owns composition root + holds refs to IsoSceneCore services.
- `RegionSceneController` — pure C# orchestrator owned by `RegionManager`; composes IsoSceneCore services + RegionScene services.
- `RegionHeightMap` — height layer data + reader; implements `IIsoHeightMap`.
- `RegionWaterMap` — water layer data + reader.
- `RegionCliffMap` — cliff layer data + reader.
- `RegionCellRenderer` — sprite render per cell (grass / slope / water-slope / cliff face).
- `RegionEvolutionService` — pop + urban-area evolution per global tick.
- `RegionCellData` — POCO (terrain kind, pop, urban_area, owning_city_id?).
- `RegionSaveService` — new FS save file format + load; links region ↔ N cities.
- `RegionToolCreateCity` — region-level city-creation tool; registers into shared toolbar.
- `RegionCellInspectorPanel`, `RegionCellHoverPanel`, `RegionCitySummaryPanel` — UI Toolkit UIDocuments (UXML + USS + C# host) registered into IsoSceneCore slots.
- `RegionCellClickHandler` — routes left/right click through `IIsoSceneCellClickDispatcher`.
- `RegionUnlockGate` — checks unlock flag (from CityScene save) to enable RegionScene access.

#### Refactored CityScene (hub-preservation rule — paths + names + Inspector untouched)

- `GameManager.cs`, `GridManager.cs`, `GeographyInitService.cs`, `UIManager.cs` — stay in place.
- Internally: extract camera + chunk-culling + tick subscription + UI-shell logic into `IsoSceneCore` services; hubs hold refs + delegate.
- Add `CityHeightMap` impl of `IIsoHeightMap` adapting existing `HeightMap` (invariant #1 — HeightMap[x,y] == Cell.height — unchanged).

#### Non-scope (explicit)

- DB-driven UI baking refactor.
- Scene transition mechanics (companion exploration `docs/explorations/assets/city-scene-loading-research.md`).
- Scale tiers above region (country, world, solar).
- Region zone-paint zone definitions (TBD — open question).
- Multi-region world map (single region in prototype).
- Visual evolution overlay / heatmap (post-prototype).
- Return-path scene-load mechanic (post-prototype).

### Data flow

1. NewGame init → global tick scheduler (`TimeManager`) starts → CityScene loads.
2. CityScene composes IsoSceneCore via `GameManager` facade → `GridManager` registers camera + culling + tick + UI shell.
3. Player plays CityScene → unlock condition (hardcoded for prototype, e.g. pop ≥ 1000) triggers `RegionUnlockGate.Enable()` → flag saved in `GameSaveData`.
4. Player loads RegionScene via main menu (transitions deferred). For prototype, RegionScene loaded fresh as separate scene file.
5. `RegionManager.Awake()` instantiates IsoSceneCore services + composes RegionScene services. `RegionEvolutionService.Start()` subscribes to `IsoSceneTickBus` (deferred per invariant #12 — `Resolve<T>` in Start, never Awake).
6. Global tick fires (`TimeManager` → `SimulationManager.ProcessSimulationTick()` continues per `sim §Tick execution order`) → `IsoSceneTickBus.Publish(tick)` → `RegionEvolutionService` evolves pop + urban-area per cell.
7. Player hovers region cell → `IsoSceneCellHoverDispatcher` → `RegionCellHoverPanel` updates.
8. Player left-clicks region cell → `IIsoSceneCellClickDispatcher.Dispatch(cell, Left)` → `RegionCellClickHandler` shows `RegionCellInspectorPanel` (or `RegionCitySummaryPanel` if cell carries `owning_city_id`).
9. Player right-clicks region cell w/ city → `RegionCitySummaryPanel` opens with "Enter City" button (disabled — transition deferred).
10. Save trigger → `RegionSaveService` writes new FS save file (`<save>.region.json`) linking region grid ↔ city ids.

### Interfaces

C# signatures (final shape may iterate during implementation):

```csharp
namespace Territory.IsoSceneCore
{
    public interface IIsoSceneHost
    {
        void RegisterHeightMap(IIsoHeightMap map);
        void RegisterToolRegistry(IIsoSceneToolRegistry registry);
        void RegisterSubtypePicker(IIsoSceneSubtypePicker picker);
        void RegisterCellClickHandler(IIsoSceneCellClickHandler handler);
        void RegisterInspectorPanelFactory(IIsoSceneInspectorPanelFactory factory);
        IsoSceneCamera Camera { get; }
        IsoSceneChunkCuller Culler { get; }
        IsoSceneTickBus TickBus { get; }
        IsoSceneUIShellHost UIShell { get; }
    }

    public interface IIsoHeightMap
    {
        int HeightAt(int x, int y);
        bool WaterAt(int x, int y);
        bool CliffAt(int x, int y);
        int GridSize { get; }
    }

    public interface IIsoSceneToolRegistry
    {
        void Register(IsoSceneTool tool);
        void Unregister(string toolSlug);
        bool IsToolVisible(ToolbarSlot slot, string toolSlug);
        IReadOnlyList<IsoSceneTool> ToolsForSlot(ToolbarSlot slot);
    }

    public interface IIsoSceneCellClickDispatcher
    {
        void Subscribe(IIsoSceneCellClickHandler handler);
        void Dispatch(GridCoord cell, MouseButton button);
    }

    public sealed class IsoSceneTickBus
    {
        public void Subscribe(IIsoSceneTickHandler handler, IsoTickKind kind);
        public void Unsubscribe(IIsoSceneTickHandler handler);
        internal void Publish(IsoTick tick); // called by TimeManager bridge
    }
}
```

### Architecture diagrams

#### Diagram 1 — Component dependency graph

```mermaid
flowchart LR
    subgraph IsoSceneCore["IsoSceneCore (shared)"]
        Cam[IsoSceneCamera]
        Cull[IsoSceneChunkCuller]
        Tick[IsoSceneTickBus]
        Shell[IsoSceneUIShellHost]
        ToolReg[IIsoSceneToolRegistry]
        SubPick[IIsoSceneSubtypePicker]
        ClickDisp[IIsoSceneCellClickDispatcher]
        HMI[IIsoHeightMap]
    end

    subgraph CityScene["CityScene (refactored)"]
        GM[GameManager hub] --> GridM[GridManager hub]
        GridM --> CityCtrl[CitySceneController internal]
        GridM --> CityHM[CityHeightMap]
        CityCtrl --> Cam
        CityCtrl --> Cull
        CityCtrl --> Tick
        CityCtrl --> Shell
        CityHM --> HMI
    end

    subgraph RegionScene["RegionScene (new)"]
        RM[RegionManager hub] --> RegCtrl[RegionSceneController]
        RegCtrl --> RegHM[RegionHeightMap]
        RegCtrl --> RegEvo[RegionEvolutionService]
        RegCtrl --> RegSave[RegionSaveService]
        RegCtrl --> Cam
        RegCtrl --> Cull
        RegCtrl --> Tick
        RegCtrl --> Shell
        RegHM --> HMI
        RegEvo --> Tick
    end

    TimeMgr[TimeManager unchanged] --> Tick
```

#### Diagram 2 — UI plugin / registration topology

```mermaid
flowchart TD
    Shell[IsoSceneUIShellHost UIDocument]
    Shell --> HUDSlot[HUD slot]
    Shell --> ToolbarSlot[Toolbar slot]
    Shell --> SubtypeSlot[Subtype picker slot]
    Shell --> ModalSlot[Modal host slot]
    Shell --> ToastSlot[Toast surface slot]

    subgraph CityRegistration[CityScene plug-ins]
        CityToolReg[CityToolRegistry] --> ToolbarSlot
        CityCatalog[CitySubtypeCatalog] --> SubtypeSlot
        CityInspFactory[CityInspectorPanelFactory] --> ModalSlot
    end

    subgraph RegionRegistration[RegionScene plug-ins]
        RegionToolReg[RegionToolRegistry inc. FoundCity] --> ToolbarSlot
        RegionCatalog[RegionSubtypeCatalog] --> SubtypeSlot
        RegionInspFactory[RegionInspectorPanelFactory] --> ModalSlot
        RegionToast[RegionToastSource] --> ToastSlot
    end
```

#### Diagram 3 — Data flow

```mermaid
flowchart LR
    NewGame[NewGame init] --> TickSched[Global tick scheduler / TimeManager]
    TickSched --> Bus[IsoSceneTickBus]
    Bus --> CityCtrl[CitySceneController]
    Bus --> RegionCtrl[RegionSceneController]
    CityCtrl --> CityEvo[CityScene sim pipeline unchanged]
    RegionCtrl --> RegEvo[RegionEvolutionService]
    CityEvo --> CitySave[CityScene save GameSaveData]
    RegEvo --> RegSave[RegionSaveService new FS file]
    CitySave --> UnlockFlag[UnlockGate flag in GameSaveData]
    UnlockFlag --> RegionGate[RegionUnlockGate read at scene load]
```

### Subsystem impact

Order of impact analysis: glossary terms via `glossary_discover` → `glossary_lookup` (9 terms hit, all multi-scale + simulation + persistence categories). Router → `managers-reference §World features` + `unity-scene-wiring` rule. Spec slices loaded: `managers-reference §manager-responsibilities`, `simulation-system §tick-execution-order`, `persistence-system §save`, `game-overview §scales`. Invariants merged via `invariants_summary` (Unity 1–11 + universal 12–13). Parity-recovery doc grep confirmed UIDocument + UXML + Host pattern.

| Subsystem | Dependency nature | Invariant risk by # | Breaking vs additive | Mitigation |
|---|---|---|---|---|
| **CityScene runtime** | Refactor — camera + culling + tick + UI shell extracted into IsoSceneCore services; hubs become facades. | #4 no new singletons (services attached to scene GO under hubs, not `new`-d); #6 don't add responsibilities to GridManager — extract OK; #5 carve-out (services share trust boundary). | Additive at file-path level; semantically equivalent (refactor). | Hub-preservation rule + golden regression test in Stage 1.1; CityScene plays identically pre/post extract. |
| **HeightMap** | New `IIsoHeightMap` abstracts CityHeightMap + RegionHeightMap. Existing `HeightMap` semantics untouched. | #1 HeightMap[x,y] == Cell.height (CityHeightMap impl must preserve); #7 shore band; #8 river bed monotonic; #9 cliff visible faces south+east only. | Additive — interface, no rewrite of CityScene HeightMap. | CityHeightMap delegates to existing HeightMap; RegionHeightMap is a new instance, separate cell array, same invariants enforced at write. |
| **TickBus / global game tick** | New `IsoSceneTickBus` between `TimeManager` + per-scene services. | #12 ServiceRegistry — `Resolve<T>` only in Start; #3 no FindObjectOfType per frame. | Additive — TimeManager unchanged; bus subscribes once. | Subscription order: `Subscribe` in `Start` (post-Awake); document ordering contract in IsoSceneTickBus XML doc. |
| **UI Toolkit (parity-recovery)** | RegionScene UI panels land inside DEC-A28 strangler migration. Reuses UIDocument + UXML + Host pattern; hand-authored UXML/USS in `Assets/UI/Generated/region-*.uxml` initially, later DB-driven when baking pipeline catches up. | None directly Unity (1–11); universal stack ok. | Additive — new UIDocuments + USS, no breaking change to existing hosts. | Region UIDocs follow `RegisterMigratedPanel` pattern from parity-recovery; staged `SetActive(true)` + `display:none` until host registers. |
| **Save schema** | `RegionSaveService` writes new FS file `<save>.region.json` alongside `GameSaveData`. Adds unlock flag to `GameSaveData`. | #14 monotonic id source (counter via `reserve-id.sh` — applies only when RegionScene needs a backlog id, not at runtime); #13 specs under permanent specs (region-cell schema initially under project spec, graduates later). | Additive — new file + 1 new bool field in `GameSaveData.schemaVersion = N+1` w/ migrator. | Schema bump in `MigrateLoadedSaveData`; legacy saves get unlock=false. RegionSaveService schema documented in project spec stub. |
| **Asset pipeline / catalog** | Out of scope — db-driven UI baking refactor deferred per DEC-A24 / DEC-A28. Region UI panels hand-authored UXML initially. | n/a | n/a | Explicit deferred entry in YAML; future plan bakes region UI when pipeline ready. |
| **Scene wiring** | New `RegionScene.unity` scene file with `RegionManager` hub GO + `ServiceRegistry` GO + `IsoSceneUIShellHost` UIDocument. | #12 ServiceRegistry GO required per scene; agent-led bridge first (universal guardrail). | Additive — new scene. | Stage scene-wiring task uses `unity_bridge_command` mutations (`new_scene` / `attach_component` / `assign_serialized_field`). |

**Deferred / out of scope** confirmed: db-driven UI baking refactor; scene-load transition mechanics; scale tiers above region; region-zone-paint zone definitions; visual evolution overlay; return-path scene-load mechanic.

### Implementation roadmap

Ordered by dependency. Each stage honors hub-preservation rule.

- **Stage 1.0 — Tracer slice (RegionScene loads + camera pans on placeholder sprite).**
  - Add `RegionScene.unity` scene file via `unity_bridge_command new_scene`.
  - Create `RegionManager.cs` hub stub Inspector-wired into the new scene.
  - Compose a minimal `IsoSceneCamera` instance (extracted but still living inside GridManager façade for the moment; full extraction in Stage 1.1).
  - Failing test (red): arrow-key input on RegionScene moves the camera transform; passes (green) after wiring is complete.
  - Visibility delta: RegionScene opens via main menu; placeholder sprite at grid center; camera pans on input.

- **Stage 1.1 — Extract IsoSceneCore runtime services (camera + chunk culling + tick subscription).**
  - Move camera + culling + tick logic out of GridManager into `Assets/Scripts/Domains/IsoSceneCore/Services/`.
  - GridManager + GameManager become facades that hold refs to these services + delegate (hub-preservation rule).
  - Inspector serialization untouched (per invariant #4 + #6).
  - Regression tests: CityScene plays identically (camera pans, culling working, tick firing).

- **Stage 1.2 — Extract IsoSceneCore UI shell (HUD + Toolbar + Subtype picker + Modal host + Toast surface).**
  - Move HUD + Toolbar + Subtype picker UIDocuments into a shared `IsoSceneUIShellHost` UIDocument (hand-authored UXML per parity-recovery patterns).
  - CityScene UI registers into slots (no behavior change).
  - Goldens validate visual parity.

- **Stage 2.0 — RegionScene terrain (heightful 64×64).**
  - Implement `RegionHeightMap` + `RegionWaterMap` + `RegionCliffMap`.
  - `RegionCellRenderer` draws grass + water-slope + cliff cells via shared IsoSceneCamera + IsoSceneChunkCuller.
  - Region terrain procedurally generated for prototype.
  - Visibility delta: full 64×64 region grid renders with height + water + cliff layers; camera + culling work identical to CityScene.

- **Stage 3.0 — RegionScene UI panels + cell click dispatch.**
  - Hand-author `region-cell-hover.uxml/uss`, `region-cell-inspector.uxml/uss`, `region-city-summary.uxml/uss` (3 new UIDocuments).
  - Wire C# Hosts that register into IsoSceneCore slots.
  - `RegionCellClickHandler` registers into `IIsoSceneCellClickDispatcher`.
  - "Enter City" button = disabled placeholder.

- **Stage 4.0 — Evolution + save.**
  - `RegionEvolutionService` subscribes to `IsoSceneTickBus` (kind=GlobalTick); evolves pop + urban-area per cell.
  - `RegionSaveService` writes new FS file `<save>.region.json` linking region grid ↔ N cities.
  - `RegionUnlockGate` flag wired (flag in `GameSaveData`; unlock cond hardcoded for prototype: city pop ≥ 1000 OR cheat flag).

- **Stage 5.0 — Region city-creation tool.**
  - `RegionToolCreateCity` registers into `IIsoSceneToolRegistry` (shared toolbar).
  - Subtype picker integration (icon + tooltip).
  - Lazy `CityData` create on first cell-click; "Enter City" stays disabled (transition deferred).

**Deferred (does not block prototype acceptance):** DB-driven UI baking refactor; scene-load transition mechanics; future scale tiers above region; region-zone-paint zone definitions; visual evolution overlay / heatmap; return-path scene-load mechanic.

### Examples

#### 1. Hub facade delegation pattern (GridManager.cs BEFORE → AFTER)

BEFORE (current — abridged):
```csharp
// Assets/Scripts/Managers/GameManagers/GridManager.cs
public partial class GridManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float panSpeed = 5f;
    private Vector2 cameraVelocity;

    void Update()
    {
        // pan / zoom logic inline here, ~80 lines
        if (Input.GetKey(KeyCode.LeftArrow)) cameraVelocity.x -= panSpeed * Time.deltaTime;
        // ... chunk culling logic ...
    }
}
```

AFTER (Stage 1.1):
```csharp
// Assets/Scripts/Managers/GameManagers/GridManager.cs — SAME PATH, SAME CLASS NAME
public partial class GridManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera; // Inspector field UNCHANGED
    [SerializeField] private float panSpeed = 5f;

    private IsoSceneCamera _camera; // composition ref
    private IsoSceneChunkCuller _culler;

    void Start() // NOTE: Resolve in Start, not Awake (invariant #12)
    {
        _camera = ServiceRegistry.Resolve<IsoSceneCamera>();
        _culler = ServiceRegistry.Resolve<IsoSceneChunkCuller>();
        _camera.Configure(mainCamera, panSpeed); // hand existing Inspector data to service
    }

    void Update() => _camera.Tick(Time.deltaTime); // delegated
}
```

- **Input:** existing CityScene Inspector wiring + arrow-key input.
- **Output:** identical pan behavior; chunk culling identical; no scene wiring changes.
- **Edge case:** `Resolve` returns null if `ServiceRegistry` not present in scene → guard + log warning; CityScene refuses to start without ServiceRegistry GO (per invariant #12).

#### 2. UIDocument + UXML + USS + C# host skeleton (RegionCellInspectorPanel)

UXML (`Assets/UI/Generated/region-cell-inspector.uxml`):
```xml
<UXML xmlns="UnityEngine.UIElements">
  <Style src="project://database/Assets/UI/Themes/dark.tss"/>
  <VisualElement name="root" class="region-inspector-root" style="display:none">
    <Label name="title" text="Region cell" class="inspector-title"/>
    <VisualElement name="stats">
      <Label name="pop-label"/>
      <Label name="urban-area-label"/>
      <Label name="terrain-label"/>
    </VisualElement>
    <Button name="enter-city-btn" text="Enter City" class="disabled"/>
  </VisualElement>
</UXML>
```

USS (`Assets/UI/Generated/region-cell-inspector.uss`):
```css
.region-inspector-root { background: rgba(35,38,47,0.95); padding: 12px; border-radius: 8px; }
.inspector-title { font-size: 16px; color: #f4d28a; }
.disabled { opacity: 0.4; }
```

C# host (`Assets/Scripts/RegionScene/UI/RegionCellInspectorPanel.cs`):
```csharp
public sealed class RegionCellInspectorPanel : MonoBehaviour
{
    [SerializeField] private UIDocument document; // wired in scene
    private VisualElement _root;
    private Label _pop, _urbanArea, _terrain;

    void OnEnable()
    {
        _root = document.rootVisualElement.Q<VisualElement>("root");
        _pop = _root.Q<Label>("pop-label");
        _urbanArea = _root.Q<Label>("urban-area-label");
        _terrain = _root.Q<Label>("terrain-label");

        var coord = ServiceRegistry.Resolve<IIsoSceneHost>();
        coord.RegisterMigratedPanel("region-cell-inspector", _root); // parity-recovery pattern
    }

    public void Show(RegionCellData cell)
    {
        _pop.text = $"Pop: {cell.pop:N0}";
        _urbanArea.text = $"Urban: {cell.urbanArea:F1} km²";
        _terrain.text = $"Terrain: {cell.terrainKind}";
        _root.style.display = DisplayStyle.Flex;
    }
}
```

- **Input:** `RegionCellClickHandler` calls `Show(cell)` on left-click.
- **Output:** panel renders inside IsoSceneCore Modal slot.
- **Edge case:** if `document` unwired in Inspector → `OnEnable` throws; scene wiring stage gates on bridge `find_gameobject` checking UIDocument component attached.

#### 3. Tool registration skeleton (`RegionToolCreateCity`)

```csharp
public sealed class RegionToolCreateCity : IsoSceneTool
{
    public override string Slug => "region.create-city";
    public override ToolbarSlot Slot => ToolbarSlot.Primary;
    public override Sprite Icon => Resources.Load<Sprite>("Icons/region-found-city");

    public override void OnCellClicked(GridCoord cell)
    {
        if (RegionCellHasCity(cell)) return; // edge case
        var newCity = CityDataFactory.CreateLazy(cell);
        RegionData.LinkCity(cell, newCity.id);
        ToastSurface.Push($"Founded city @ {cell}");
    }
}

// registration in RegionSceneController.Start():
_toolReg.Register(new RegionToolCreateCity());
```

- **Input:** player clicks toolbar slot + selects empty cell.
- **Output:** new `CityData` lazy-created, region grid links to it.
- **Edge case:** click on cell already owned by city → no-op (early return); future: show toast + tooltip.

#### 4. Heightmap interface (`IIsoHeightMap` + `RegionHeightMap` + `CityHeightMap` adapter)

```csharp
public sealed class CityHeightMap : IIsoHeightMap
{
    private readonly GridManager _grid;
    public CityHeightMap(GridManager grid) { _grid = grid; }
    public int HeightAt(int x, int y) => _grid.GetCell(x, y).height; // invariant #1 enforced
    public bool WaterAt(int x, int y) => _grid.GetCell(x, y).isWater;
    public bool CliffAt(int x, int y) => _grid.GetCell(x, y).isCliff;
    public int GridSize => _grid.GridSize;
}

public sealed class RegionHeightMap : IIsoHeightMap
{
    private readonly RegionCellData[,] _cells;
    public RegionHeightMap(int size) { _cells = new RegionCellData[size, size]; }
    public int HeightAt(int x, int y) => _cells[x, y].height;
    public bool WaterAt(int x, int y) => _cells[x, y].terrainKind == RegionTerrainKind.WaterSlope;
    public bool CliffAt(int x, int y) => _cells[x, y].terrainKind == RegionTerrainKind.Cliff;
    public int GridSize => _cells.GetLength(0);
}
```

- **Input:** consumer (`RegionCellRenderer`) calls `HeightAt(x,y)`.
- **Output:** integer height value.
- **Edge case:** out-of-bounds (x<0 or x>=GridSize) → caller is renderer; renderer iterates via culler's visible-set + cull bounds clamp visible-set to GridSize. No out-of-bounds reads expected.

#### 5. Tick subscription (`RegionEvolutionService`)

```csharp
public sealed class RegionEvolutionService : MonoBehaviour, IIsoSceneTickHandler
{
    private IsoSceneTickBus _bus;
    private RegionData _data;

    void Start() // Start, not Awake (invariant #12)
    {
        _bus = ServiceRegistry.Resolve<IsoSceneTickBus>();
        _data = ServiceRegistry.Resolve<RegionData>();
        _bus.Subscribe(this, IsoTickKind.GlobalTick);
    }

    void OnDestroy() => _bus?.Unsubscribe(this);

    public void OnTick(IsoTick tick)
    {
        if (_data == null) return; // edge case: tick fires before scene fully loaded
        foreach (var cell in _data.OwnedCityCells)
        {
            cell.pop += GrowthRate(cell);
            cell.urbanArea += UrbanAreaDelta(cell);
        }
    }
}
```

- **Input:** `IsoSceneTickBus.Publish(tick)` from TimeManager bridge.
- **Output:** per-cell pop + urban-area mutated.
- **Edge case:** tick fires before `RegionData` resolved → null guard at top of `OnTick` (Subscribe in Start ensures Awake order, but TimeManager publish may race during scene load).

#### 6. Cell click dispatch

```csharp
public sealed class RegionCellClickHandler : IIsoSceneCellClickHandler
{
    private readonly RegionCellInspectorPanel _inspector;
    private readonly RegionCitySummaryPanel _citySummary;
    private readonly RegionData _data;

    public void OnClick(GridCoord cell, MouseButton button)
    {
        if (!_data.InBounds(cell)) return; // edge case
        var cellData = _data.At(cell);
        if (button == MouseButton.Left) _inspector.Show(cellData);
        else if (button == MouseButton.Right && cellData.HasCity) _citySummary.Show(cellData);
    }
}
```

- **Input:** click event from `IIsoSceneCellClickDispatcher`.
- **Output:** appropriate panel opens.
- **Edge case 1:** right-click on empty cell → no panel opens (early `HasCity` check).
- **Edge case 2:** click on out-of-bounds cell (camera near grid edge) → `InBounds` guard.

### Review notes

#### Resolved BLOCKING (Phase 8 review by `Plan` subagent)

- **BLOCKING-1:** Initial draft did not assert that `RegionData` instance is created/registered before `RegionEvolutionService.Start()` resolves it.
  - **Resolution:** `RegionManager.Awake()` instantiates `RegionData` + `Register<RegionData>` via `ServiceRegistry` (producer side in Awake per invariant #12). `RegionEvolutionService.Start()` then resolves. Order documented in Stage 4.0 implementation roadmap entry.
- **BLOCKING-2:** Stage 1.1 risked breaking invariant #1 (`HeightMap[x,y] == Cell.height`) if `CityHeightMap` were a copy of HeightMap.
  - **Resolution:** `CityHeightMap` is an adapter holding a `GridManager` ref + calls `_grid.GetCell(x,y).height` — single source of truth preserved. Invariant #1 explicit in §Subsystem impact row.

#### Non-blocking + suggestions (carried)

- **SUGGESTION-1:** Consider versioning the `RegionSaveService` schema separately from `GameSaveData.schemaVersion` (e.g. `RegionSaveData.schemaVersion`) to decouple future region-only migrations from CityScene save schema bumps.
- **SUGGESTION-2:** Stage 3.0 click dispatcher could carry a hover-debounce timer (avoid re-firing inspector show on every frame the cell is hovered). Open question for stage authoring.
- **SUGGESTION-3:** `RegionUnlockGate` hardcoded condition (pop ≥ 1000) should live in a single named constant (e.g. `RegionUnlockConfig.MinCityPopulation`) so the gate can be re-tuned without searching the codebase.
- **SUGGESTION-4:** When `multi-scale` master plan re-opens, its Stage 4.0 + 14.0 should explicitly cite DEC-A29 surface + adopt `IsoSceneCore` as the shared substrate rather than re-deriving.
- **NON-BLOCKING:** Region-zone paint zone definitions (open question 8/9 from the seed) remains TBD; revisit after Stage 5.0 user testing.

### Expansion metadata

- **Date:** 2026-05-15
- **Model:** claude-opus-4-7[1m]
- **Approach selected:** B — Shared `IsoSceneCore`
- **Architecture decision:** DEC-A29 (surface `contracts/iso-scene-core-foundation`)
- **Blocking items resolved:** 2
- **Non-blocking + suggestions carried:** 5
- **Drift overlap flagged:** `multi-scale` master plan (stages 4.0, 14.0) — `/master-plan-new` must coordinate.

## Design Expansion — Stage Enrichment

Parallel MD enrichment for ship-plan Phase 3.5 consumption. One block per stage + per task. Heading order fixed per output schema.

### Stage 1.0 — Tracer slice

#### Stage 1.0 — Enriched

##### Edge Cases

- RegionScene loaded without ServiceRegistry GO → RegionManager.Start() warns + scene refuses play mode → CI gate catches missing GO.
- Placeholder sprite asset missing under Assets/Sprites/region/ → Resources.Load returns null → pink magenta missing-texture renders + log warning; positional assert still passes.
- Arrow-key input arrives before RegionManager.Start() completes → IsoSceneCamera ref null → Update() null-guards on _camera; no NRE; pan event queued/dropped.

##### Shared Seams

- **IsoSceneCamera** — producer 1.0; consumers 1.1, 2.0, 3.0. Pan/zoom/displacement service consumed by all iso scenes via composition reference; stub at 1.0, fully extracted at 1.1.
- **RegionManager hub facade** — producer 1.0; consumers 2.0, 3.0, 4.0, 5.0. Inspector-wired MonoBehaviour holds composition root; never renamed/moved/deleted post-1.0 (hub-preservation rule).
- **IIsoSceneHost composition contract** — producer 1.0; consumers 1.1, 1.2, 3.0, 5.0. RegionManager implements IIsoSceneHost; tool/subtype/click registration flows through it.

#### Task 1.0.1 — Enriched

##### Visual Mockup

```svg
<svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
  <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
  <text x="200" y="24" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="11">RegionScene — Stage 1.0.1 stub</text>
  <rect x="20" y="48" width="360" height="160" fill="none" stroke="var(--ds-border-muted, #2e3340)" stroke-width="1" stroke-dasharray="4 3"/>
  <text x="200" y="130" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="13">[RegionRoot GameObject]</text>
  <text x="200" y="150" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="11">RegionManager + ServiceRegistry attached</text>
  <text x="200" y="225" text-anchor="middle" fill="var(--ds-text-danger, #d46a6a)" font-family="monospace" font-size="10">Test red: no sprite yet, no camera pan</text>
</svg>
```

##### Before / After Code

`Assets/Scripts/RegionScene/RegionManager.cs`:

Before:
```csharp
// file does not exist
```

After:
```csharp
using UnityEngine;
using Territory.IsoSceneCore;

namespace Territory.RegionScene
{
    public sealed class RegionManager : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;

        void Awake()
        {
            // composition root stub — services wired in Stage 1.1
        }
    }
}
```

##### Glossary Anchors

- **Host MonoBehaviour** — DEC-A28 / Assets/Scripts/UI/Hosts/
- **service registry** — docs/post-atomization-architecture.md §Service Registry
- **scene contract** — docs/asset-pipeline-scene-contract.md

##### Failure Modes

- Fails if RegionScene.unity created without ServiceRegistry GO — invariant #12 breach.
- Fails if RegionManager partial class declaration places `: MonoBehaviour` in non-stem file — Unity GUID bind error.
- Fails if Inspector ref for mainCamera left unassigned — RegionManager.Awake() NRE at scene load.

##### Decision Dependencies

- DEC-A29 (inherits)
- DEC-A22 (inherits)
- DEC-A23 (inherits)

##### Touched Paths Preview

- `Assets/Scenes/RegionScene.unity` — null LOC, new — New Unity scene file with RegionRoot GameObject + ServiceRegistry GO; created via bridge new_scene mutation.
- `Assets/Scripts/RegionScene/RegionManager.cs` — null LOC, new — Hub MonoBehaviour stub under Territory.RegionScene namespace; Inspector-wired in RegionScene.unity; future composition root.

#### Task 1.0.2 — Enriched

##### Visual Mockup

```svg
<svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
  <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
  <text x="200" y="24" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="11">RegionScene — Stage 1.0.2 placeholder sprite</text>
  <rect x="20" y="48" width="360" height="160" fill="none" stroke="var(--ds-border-muted, #2e3340)" stroke-width="1" stroke-dasharray="4 3"/>
  <rect x="186" y="120" width="28" height="14" fill="var(--ds-accent-warm, #f4d28a)" opacity="0.8"/>
  <text x="200" y="142" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="9">cell [31,31]</text>
  <text x="200" y="225" text-anchor="middle" fill="var(--ds-text-warning, #e2b14a)" font-family="monospace" font-size="10">Test 2/3 green; pan test still red</text>
</svg>
```

##### Before / After Code

`Assets/Scripts/RegionScene/RegionManager.cs`:

Before:
```csharp
void Awake() { /* stub */ }
```

After:
```csharp
void Awake()
{
    var sprite = Resources.Load<Sprite>("region/placeholder");
    var go = new GameObject("PlaceholderSprite");
    go.transform.SetParent(transform);
    go.transform.position = GridCenterWorld(); // [31,31] of 64x64
    var sr = go.AddComponent<SpriteRenderer>();
    sr.sprite = sprite;
}
```

##### Glossary Anchors

- **City / Region / Country cell** — ms §cell-vocab

##### Failure Modes

- Fails if Sprites/region/placeholder.png missing — Resources.Load returns null, magenta missing-texture renders.
- Fails if GridCenterWorld() computes coords using city-cell scale, not region-cell scale (1 region cell = 32 city cells).

##### Decision Dependencies

- DEC-A29 (inherits)
- DEC-A22 (inherits)
- DEC-A23 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/RegionManager.cs` — 25 LOC, extend — Awake() now loads placeholder sprite and parents SpriteRenderer GO at grid center.
- `Assets/Sprites/region/placeholder.png` — null LOC, new — Placeholder 32x16 isometric tile sprite at grid center; replaced when terrain renderer lands in Stage 2.0.

#### Task 1.0.3 — Enriched

##### Visual Mockup

```svg
<svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
  <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
  <text x="200" y="24" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="11">RegionScene — Stage 1.0.3 camera pan tracer</text>
  <rect x="20" y="48" width="360" height="160" fill="none" stroke="var(--ds-border-muted, #2e3340)" stroke-width="1" stroke-dasharray="4 3"/>
  <rect x="240" y="120" width="28" height="14" fill="var(--ds-accent-warm, #f4d28a)" opacity="0.8"/>
  <path d="M 200 134 L 240 134" stroke="var(--ds-accent-cool, #88c0d0)" stroke-width="2" marker-end="url(#arrow)"/>
  <defs><marker id="arrow" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto"><path d="M0,0 L0,6 L8,3 z" fill="var(--ds-accent-cool, #88c0d0)"/></marker></defs>
  <text x="200" y="200" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="10">RightArrow held 0.5s → sprite drifts right</text>
  <text x="200" y="225" text-anchor="middle" fill="var(--ds-text-success, #8ac28a)" font-family="monospace" font-size="10">Test green: all 3 asserts pass</text>
</svg>
```

##### Before / After Code

`Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneCamera.cs`:

Before:
```csharp
// file does not exist
```

After:
```csharp
using UnityEngine;
namespace Territory.IsoSceneCore
{
    public sealed class IsoSceneCamera
    {
        private Camera _cam;
        private float _panSpeed = 5f;
        public void Configure(Camera cam, float panSpeed) { _cam = cam; _panSpeed = panSpeed; }
        public void Tick(float dt)
        {
            var v = Vector3.zero;
            if (Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
            if (Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
            _cam.transform.position += v * _panSpeed * dt;
        }
    }
}
```

##### Glossary Anchors

- **service registry** — docs/post-atomization-architecture.md §Service Registry
- **Host MonoBehaviour** — DEC-A28

##### Failure Modes

- Fails if RegionManager resolves IsoSceneCamera in Awake() — invariant #12 init-order race.
- Fails if IsoSceneCamera.Configure not called before first Tick — _cam null, NRE.
- Fails if Update() runs while Editor paused — input fires but no scene state visible.

##### Decision Dependencies

- DEC-A29 (inherits)
- DEC-A22 (inherits)
- DEC-A23 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneCamera.cs` — null LOC, new — IsoSceneCore camera pan/zoom service stub; full extraction from GridManager happens in Stage 1.1.
- `Assets/Scripts/RegionScene/RegionManager.cs` — 35 LOC, extend — Start() now resolves IsoSceneCamera + calls Configure; Update() delegates Tick to camera service.

### Stage 1.1 — Extract IsoSceneCore runtime services

#### Stage 1.1 — Enriched

##### Edge Cases

- CityScene saved scene references old GridManager Inspector field accidentally renamed → scene fails to load with missing-script-field warning; CI gate via verify:local catches before merge.
- Tick fires during scene load before IsoSceneTickBus subscription complete → Bus.Publish guards on empty subscriber list; first tick dropped harmlessly.
- IsoSceneChunkCuller computes visible-set using city-cell bounds when extracted → Culler.Configure(GridSize) called from GridManager.Start; RegionManager passes 64; identical bounds clamping logic.

##### Shared Seams

- **IsoSceneCamera** — producer 1.1; consumers 2.0, 3.0. Fully extracted service; GridManager + RegionManager hold composition refs and delegate Update tick.
- **IsoSceneChunkCuller** — producer 1.1; consumer 2.0. Visible-cell windowing service subscribed to camera deltas; RegionCellRenderer consumes visible-set.
- **IsoSceneTickBus** — producer 1.1; consumer 4.0. TimeManager publishes; RegionEvolutionService subscribes in Start (invariant #12).

#### Task 1.1.1 — Enriched

##### Glossary Anchors

- **service registry** — docs/post-atomization-architecture.md §Service Registry

##### Failure Modes

- Fails if asmdef references Assembly-CSharp circularly — Unity rejects assembly graph at refresh.

##### Touched Paths Preview

- `Assets/Scripts/Domains/IsoSceneCore/Territory.IsoSceneCore.asmdef` — null LOC, new — Assembly definition for IsoSceneCore; references UnityEngine + Unity.Mathematics.
- `Assets/Scripts/Domains/IsoSceneCore/Services/.gitkeep` — null LOC, new — Folder marker so git + Unity generate meta files.

#### Task 1.1.2 — Enriched

##### Before / After Code

`Assets/Scripts/GridManager.cs`:

Before:
```csharp
void Update()
{
    if (Input.GetKey(KeyCode.LeftArrow)) cameraVelocity.x -= panSpeed * Time.deltaTime;
    // ... 80 lines of pan / zoom / displacement logic ...
}
```

After:
```csharp
private IsoSceneCamera _camera;
void Start() // Resolve in Start (invariant #12)
{
    _camera = ServiceRegistry.Resolve<IsoSceneCamera>();
    _camera.Configure(mainCamera, panSpeed);
}
void Update() => _camera.Tick(Time.deltaTime);
```

##### Glossary Anchors

- **service registry** — docs/post-atomization-architecture.md §Service Registry
- **Host MonoBehaviour** — DEC-A28

##### Failure Modes

- Fails if GridManager.cs class name or file path renamed during extract — hub-preservation rule breach + Inspector script-ref break.
- Fails if Resolve called in Awake — invariant #12 race, _camera null on first Update tick.

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneCamera.cs` — null LOC, extend — Full pan/zoom/displacement logic landed; supersedes 1.0.3 stub.
- `Assets/Scripts/GridManager.cs` — null LOC, extend — Hub facade pattern: Inspector serialization untouched; Start() resolves service, Update() delegates Tick.

#### Task 1.1.3 — Enriched

##### Glossary Anchors

- **service registry** — docs/post-atomization-architecture.md §Service Registry

##### Failure Modes

- Fails if culler subscribes to camera before camera.Configure called — null camera ref, NRE.
- Fails if visible-set delta emits during scene unload — listeners on disposed UIDocuments throw.

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneChunkCuller.cs` — null LOC, new — Visible-cell windowing service; subscribes to IsoSceneCamera deltas; emits visible-set events.
- `Assets/Scripts/GridManager.cs` — null LOC, extend — GridManager.Start() resolves culler + Configure(GridSize); chunk-cull logic moved out.

#### Task 1.1.4 — Enriched

##### Glossary Anchors

- **service registry** — docs/post-atomization-architecture.md §Service Registry

##### Failure Modes

- Fails if Subscribe runs in Awake — invariant #12 race; bus producer may not be registered yet.
- Fails if GameManager.cs renamed during extract — hub-preservation breach.

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneTickBus.cs` — null LOC, new — Tick multiplexer; producer=TimeManager bridge; consumers=per-scene evolution services.
- `Assets/Scripts/GameManager.cs` — null LOC, extend — Hub facade: Start() resolves bus + registers TimeManager publish bridge; Inspector untouched.

### Stage 1.2 — Extract IsoSceneCore UI shell

#### Stage 1.2 — Enriched

##### Edge Cases

- Existing CityScene HUD UIDocument removed before IsoSceneUIShellHost slots exist (Stage 1.2.2 mid-refactor) → scene compiles but HUD missing; golden test red; CI gate catches before merge.
- UIShellHost.uxml references USS class that does not exist (Stage 1.2.1 USS lags UXML) → Unity logs USS-missing-class warning; visual fallback to unstyled element.
- Two scenes load IsoSceneUIShellHost simultaneously (CityScene → RegionScene transition) → scene unload disposes old shell; deferred in this plan but documented in shared_seams.

##### Shared Seams

- **IsoSceneUIShellHost UIDocument** — producer 1.2; consumers 3.0, 5.0. Root UIDocument with named slots (hud, toolbar, subtype, modal, toast); per-scene plugins query slot + AddChild.
- **IIsoSceneToolRegistry** — producer 1.2; consumer 5.0. Per-scene tool registration into toolbar slot.
- **IIsoSceneSubtypePicker** — producer 1.2; consumer 5.0. Generic picker; scenes register subtype catalogs at Start.
- **IIsoSceneZonePaintHost** — producer 1.2; no consumers in this plan. Paint mechanism shell; per-scene zone definitions deferred (region-zone-paint-zone-definitions in deferred list).

#### Task 1.2.1 — Enriched

##### Visual Mockup

```svg
<svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
  <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
  <rect x="0" y="0" width="400" height="32" fill="var(--ds-bg-elevated, #23262f)"/>
  <text x="200" y="20" text-anchor="middle" fill="var(--ds-text-primary, #e6e9ef)" font-family="monospace" font-size="10">hud-slot (population, funds)</text>
  <rect x="0" y="208" width="400" height="32" fill="var(--ds-bg-elevated, #23262f)"/>
  <text x="200" y="228" text-anchor="middle" fill="var(--ds-text-primary, #e6e9ef)" font-family="monospace" font-size="10">toolbar-slot (per-scene tools)</text>
  <rect x="120" y="80" width="160" height="80" fill="var(--ds-bg-elevated, #23262f)" stroke="var(--ds-border-muted, #2e3340)"/>
  <text x="200" y="120" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="10">modal-slot / subtype-slot</text>
  <text x="200" y="138" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="9">toast-slot top-right</text>
</svg>
```

##### Glossary Anchors

- **UIDocument** — Unity UI Toolkit
- **UXML** — DEC-A28
- **Host MonoBehaviour** — DEC-A28
- **Subtype picker (RCIS)** — ui §3.7

##### Failure Modes

- Fails if UXML slot names diverge from C# Slot(name) string literals — runtime query returns null.
- Fails if USS file path lags UXML reference — Unity logs Style-not-found warning.

##### Decision Dependencies

- DEC-A28 (inherits)
- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/Domains/IsoSceneCore/UI/IsoSceneUIShellHost.cs` — null LOC, new — Host MonoBehaviour rooting the shared UIDocument; exposes Slot(name) plugin API.
- `Assets/UI/Generated/iso-scene-ui-shell.uxml` — null LOC, new — Hand-authored UXML with 5 named slots; hud + toolbar + subtype + modal + toast.
- `Assets/UI/Generated/iso-scene-ui-shell.uss` — null LOC, new — Theme + layout styles for shell slots; ties into ds-* tokens via dark theme.

#### Task 1.2.2 — Enriched

##### Before / After Code

`Assets/Scripts/UIManager.cs`:

Before:
```csharp
[SerializeField] private UIDocument hudDocument;
[SerializeField] private UIDocument toolbarDocument;
void Awake() { /* 2 separate roots */ }
```

After:
```csharp
[SerializeField] private UIDocument hudDocument;       // Inspector untouched
[SerializeField] private UIDocument toolbarDocument;   // Inspector untouched
private IsoSceneUIShellHost _shell;
void Start()
{
    _shell = ServiceRegistry.Resolve<IsoSceneUIShellHost>();
    _shell.Slot("hud-slot").Add(hudDocument.rootVisualElement);
    _shell.Slot("toolbar-slot").Add(toolbarDocument.rootVisualElement);
}
```

##### Glossary Anchors

- **Host MonoBehaviour** — DEC-A28
- **ModalCoordinator** — Assets/Scripts/UI/Modals/ModalCoordinator.cs

##### Failure Modes

- Fails if UIManager.cs renamed — Inspector script-ref breaks across all scenes.
- Fails if shell.Slot returns null because slot name typo.

##### Decision Dependencies

- DEC-A28 (inherits)
- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/UIManager.cs` — null LOC, extend — Hub facade: Inspector serialization untouched; Start() registers HUD + Toolbar into shell slots.
- `Assets/Scripts/UI/HudController.cs` — null LOC, extend — HUD controller no longer owns root UIDocument lifetime; binds into shell hud-slot.
- `Assets/Scripts/UI/ToolbarController.cs` — null LOC, extend — Toolbar controller registers visual element into toolbar-slot via shell.

#### Task 1.2.3 — Enriched

##### Glossary Anchors

- **Subtype picker (RCIS)** — ui §3.7
- **ZoneSubTypeRegistry** — econ#zone-sub-type-registry

##### Failure Modes

- Fails if interface namespace mismatches asmdef root — Unity assembly graph rejects compile.

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoSceneToolRegistry.cs` — null LOC, new — Per-scene tool registration into shared toolbar slot.
- `Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoSceneSubtypePicker.cs` — null LOC, new — Generic subtype picker; scenes register catalogs at Start.
- `Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoSceneZonePaintHost.cs` — null LOC, new — Paint mechanism shell; region zone definitions deferred.

### Stage 2.0 — RegionScene terrain

#### Stage 2.0 — Enriched

##### Edge Cases

- Cell (0,0) has water + cliff flags both true → RegionWaterMap precedence over CliffMap per existing CityScene cliff rules (invariant #9 south+east faces only).
- Camera pans beyond grid edge → visible-set query returns cells with x>=64 → RegionCellRenderer clamps to GridSize bounds; out-of-range cells skipped silently.
- RegionHeightMap.HeightAt called before terrain seeded → returns 0; renderer draws flat grass; subsequent reseed redraws on next visible-set delta.

##### Shared Seams

- **IIsoHeightMap** — producer 2.0; consumer 3.0. RegionHeightMap implements; consumed by RegionCellRenderer + RegionCellInspectorPanel for terrain readout.

#### Task 2.0.1 — Enriched

##### Glossary Anchors

- **Water map data** — persist §Save, geo §11.5
- **CellData** — persist §Save, geo §11.2

##### Failure Modes

- Fails if HeightMap[x,y] vs Cell.height invariant #1 not honored across region cells when persistence lands in Stage 4.0 — separate region invariant must be added.

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/Domains/Terrain/RegionHeightMap.cs` — null LOC, new — Per-cell elevation int array; implements IIsoHeightMap; procedural seed for prototype.
- `Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoHeightMap.cs` — null LOC, new — Abstract height/water/cliff query interface; CityHeightMap + RegionHeightMap implement.

#### Task 2.0.2 — Enriched

##### Glossary Anchors

- **Water map data** — persist §Save, geo §11.5

##### Failure Modes

- Fails if shore band rule violated — land cell Moore-adjacent to water with height > min(neighbor S) (invariant #7).
- Fails if river bed monotonic non-increasing rule violated (invariant #8).

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/Domains/Terrain/RegionWaterMap.cs` — null LOC, new — Per-cell water flag + slope direction; drainage rules mirror CityScene invariants #7 + #8.

#### Task 2.0.3 — Enriched

##### Glossary Anchors

- **CellData** — persist §Save, geo §11.2

##### Failure Modes

- Fails if cliff faces emitted on north or west sides — invariant #9 breach.

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/Domains/Terrain/RegionCliffMap.cs` — null LOC, new — Per-cell cliff flag; visible faces south+east only per invariant #9.

#### Task 2.0.4 — Enriched

##### Visual Mockup

```svg
<svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
  <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
  <text x="200" y="20" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="11">RegionScene — Stage 2.0.4 64×64 terrain</text>
  <g transform="translate(80, 50)">
    <polygon points="120,0 240,60 120,120 0,60" fill="var(--ds-accent-grass, #6a8e4e)"/>
    <polygon points="120,0 200,40 160,60 120,40" fill="var(--ds-accent-cool, #4e6e8e)" opacity="0.8"/>
    <polygon points="240,60 240,80 160,140 120,120" fill="var(--ds-accent-cliff, #5a4a3e)" opacity="0.9"/>
  </g>
  <text x="200" y="225" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="10">grass / water-slope / cliff faces (S+E only)</text>
</svg>
```

##### Glossary Anchors

- **CellData** — persist §Save, geo §11.2

##### Failure Modes

- Fails if renderer queries cells outside visible-set — out-of-bounds reads; renderer must consume culler delta as source of truth.
- Fails if sprite sort order vs isometric depth not aligned — back cliffs paint over front grass.

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/Domains/Terrain/RegionCellRenderer.cs` — null LOC, new — Sprite-per-cell renderer; subscribes to chunk-culler visible-set; reads Region{Height,Water,Cliff}Map.
- `Assets/Scripts/RegionScene/RegionManager.cs` — null LOC, extend — Start() now wires RegionCellRenderer + binds to IsoSceneChunkCuller delta event.

### Stage 3.0 — RegionScene UI panels + cell click dispatch

#### Stage 3.0 — Enriched

##### Edge Cases

- Hover panel re-fires every frame the cell is hovered (Stage 3.0.1 naive impl without debounce) → hover-debounce timer carried forward as SUGGESTION-2; non-blocking for prototype but visual jitter possible.
- Right-click on empty cell (no owning city) → no panel opens; early return in RegionCellClickHandler.
- Click on out-of-bounds cell (camera near grid edge) → InBounds guard at top of OnClick; silent no-op.

##### Shared Seams

- **IIsoSceneCellClickDispatcher** — producer 3.0; consumer 5.0. Left/right click routing; RegionToolCreateCity also subscribes for cell-placement events.

#### Task 3.0.1 — Enriched

##### Visual Mockup

```svg
<svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
  <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
  <rect x="240" y="60" width="140" height="80" fill="var(--ds-bg-elevated, #23262f)" stroke="var(--ds-border-muted, #2e3340)" rx="4"/>
  <text x="310" y="82" text-anchor="middle" fill="var(--ds-text-primary, #e6e9ef)" font-family="monospace" font-size="11">Region cell [10,10]</text>
  <text x="310" y="102" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="10">Terrain: grass</text>
  <text x="310" y="118" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="10">Height: 3</text>
  <text x="310" y="134" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="9">(no city)</text>
</svg>
```

##### Glossary Anchors

- **UIDocument** — Unity UI Toolkit
- **UXML** — DEC-A28
- **Host MonoBehaviour** — DEC-A28

##### Failure Modes

- Fails if hover panel re-binds VisualElement on every Show() call — memory leak from undisposed handlers.

##### Decision Dependencies

- DEC-A28 (inherits)
- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/UI/RegionCellHoverPanel.cs` — null LOC, new — Host MonoBehaviour for hover panel; subscribes to IsoSceneCellHoverDispatcher.
- `Assets/UI/Generated/region-cell-hover.uxml` — null LOC, new — Hand-authored UXML; terrain kind + height + owning-city hint labels.
- `Assets/UI/Generated/region-cell-hover.uss` — null LOC, new — Hover panel styles; ds-* tokens; pinned to mouse position.

#### Task 3.0.2 — Enriched

##### Visual Mockup

```svg
<svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
  <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
  <rect x="120" y="60" width="160" height="120" fill="var(--ds-bg-elevated, #23262f)" stroke="var(--ds-border-muted, #2e3340)" rx="6"/>
  <text x="200" y="84" text-anchor="middle" fill="var(--ds-accent-warm, #f4d28a)" font-family="monospace" font-size="13">City of Bacayo</text>
  <text x="200" y="106" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="10">Pop: 2,450</text>
  <text x="200" y="122" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="10">Urban: 4.8 km²</text>
  <rect x="148" y="142" width="104" height="22" fill="var(--ds-bg-muted, #2a2d36)" stroke="var(--ds-border-muted, #2e3340)" opacity="0.4"/>
  <text x="200" y="158" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="10">Enter City (disabled)</text>
</svg>
```

##### Glossary Anchors

- **ModalCoordinator** — Assets/Scripts/UI/Modals/ModalCoordinator.cs
- **Host MonoBehaviour** — DEC-A28

##### Failure Modes

- Fails if both inspector + summary panels open simultaneously — modal-slot single-child contract.
- Fails if Enter City button missing "disabled" class — placeholder click attempts scene transition (deferred).

##### Decision Dependencies

- DEC-A28 (inherits)
- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/UI/RegionCellInspectorPanel.cs` — null LOC, new — Host for left-click cell inspector panel; pop + urban-area + terrain readout.
- `Assets/Scripts/RegionScene/UI/RegionCitySummaryPanel.cs` — null LOC, new — Host for right-click city summary panel; Enter City button disabled (transition deferred).
- `Assets/UI/Generated/region-cell-inspector.uxml` — null LOC, new — Hand-authored UXML; stats grid for cell inspection.
- `Assets/UI/Generated/region-city-summary.uxml` — null LOC, new — Hand-authored UXML; city headline + Enter City placeholder button.

#### Task 3.0.3 — Enriched

##### Glossary Anchors

- **service registry** — docs/post-atomization-architecture.md §Service Registry

##### Failure Modes

- Fails if dispatcher subscribes handler in Awake — invariant #12 race.
- Fails if click handler does not guard out-of-bounds cell coords from camera-edge mouse projection.

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/UI/RegionCellClickHandler.cs` — null LOC, new — Region click handler; routes left=inspector, right+HasCity=summary.
- `Assets/Scripts/Domains/IsoSceneCore/Contracts/IIsoSceneCellClickDispatcher.cs` — null LOC, new — Left/right click dispatch contract; per-scene handlers subscribe via Subscribe(handler).

### Stage 4.0 — Evolution + save

#### Stage 4.0 — Enriched

##### Edge Cases

- Tick fires before RegionData resolved (scene mid-load) → null guard at top of OnTick returns; first ticks dropped harmlessly.
- Save file exists but schema version older than current → RegionSaveService.MigrateLoadedSaveData bumps schema; missing region data initialized to defaults; unlock=false.
- Two cities placed at same region cell (race in tool placement) → Stage 5.0 dependent; collision handled by tool-side guard in 5.0.3.

##### Shared Seams

- **IsoSceneTickBus subscription contract** — producer 1.1; consumer 4.0. RegionEvolutionService.Start subscribes; OnTick null-guards on RegionData.
- **RegionSaveService** — producer 4.0; consumer 5.0. New FS save file linking region ↔ cities; Stage 5.0 lazy-creates CityData entries into same file.

#### Task 4.0.1 — Enriched

##### Glossary Anchors

- **Urban growth rings** — sim §Rings
- **CellData** — persist §Save, geo §11.2

##### Failure Modes

- Fails if Subscribe in Awake — invariant #12 race.
- Fails if OnTick reads RegionData while null — scene mid-load tick.

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/Domains/Evolution/RegionEvolutionService.cs` — null LOC, new — MonoBehaviour subscribes to IsoSceneTickBus; evolves pop + urban-area per region cell.
- `Assets/Scripts/RegionScene/Domains/Evolution/RegionCellData.cs` — null LOC, new — POCO cell record: terrain kind + pop + urban_area + owning_city_id?.

#### Task 4.0.2 — Enriched

##### Glossary Anchors

- **Save data** — persist §Save
- **Multi-scale save tree** — ms
- **Parent region / country id** — persist §Save

##### Failure Modes

- Fails if save file written to wrong path — must align with GameSaveData base path convention.
- Fails if load round-trip drops fields — JsonUtility serializer requires public fields on RegionSaveFile DTO.

##### Decision Dependencies

- DEC-A29 (inherits)
- DEC-A10 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/Domains/Persistence/RegionSaveService.cs` — null LOC, new — FS save/load service; writes <save>.region.json sidecar to GameSaveData.
- `Assets/Scripts/RegionScene/Domains/Persistence/RegionSaveFile.cs` — null LOC, new — Serializable DTO; carries grid + per-cell evolution state + city-ownership map + schema_version.

#### Task 4.0.3 — Enriched

##### Glossary Anchors

- **Save data** — persist §Save
- **Scale switch** — ms, ms-post

##### Failure Modes

- Fails if GameSaveData schema bump missing migrator — legacy saves crash on load.
- Fails if unlock cond constant duplicated across files — single named static enforced per SUGGESTION-3.

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/RegionUnlockGate.cs` — null LOC, new — Reads region_unlocked flag; hardcoded prototype cond city pop ≥ 1000; single-constant config.
- `Assets/Scripts/CityData.cs` — null LOC, extend — Adds region_unlocked bool field + schema bump; CityData persists flag through GameSaveData.
- `Assets/Scripts/UI/MainMenu/MainMenuController.cs` — null LOC, extend — Main menu checks gate; "Open Region" entry greys when locked.

### Stage 5.0 — Region city-creation tool

#### Stage 5.0 — Enriched

##### Edge Cases

- Click on cell already owned by another city → tool no-op (early return); future enhancement: toast "Cell occupied".
- Click on water-slope or cliff cell → tool no-op or terrain-conflict toast; design TBD (open question) but prototype accepts placement.
- Save fired mid-placement (race) → atomic write contract: LinkCity + CreateLazy + save mutation locked in tool action body.

##### Shared Seams

- **IIsoSceneToolRegistry consumption** — producer 1.2; consumer 5.0. RegionToolCreateCity registers into ToolbarSlot.Primary; tool surface backed by IsoSceneTool base class.

#### Task 5.0.1 — Enriched

##### Glossary Anchors

- **Subtype picker (RCIS)** — ui §3.7
- **Toolbar family subtype enumeration (MVP)** — mvp#toolbar-family-subtype-enumeration-mvp-picker-scope

##### Failure Modes

- Fails if tool registers in Awake instead of Start — invariant #12 race against registry resolve.
- Fails if Resources.Load returns null for icon — toolbar slot renders empty button.

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/Tools/RegionToolCreateCity.cs` — null LOC, new — Region city-placement tool; extends IsoSceneTool; OnCellClicked creates lazy CityData + links region cell.

#### Task 5.0.2 — Enriched

##### Glossary Anchors

- **Subtype picker (RCIS)** — ui §3.7
- **Picker universal rule** — mvp#toolbar-family-subtype-enumeration-mvp-picker-scope
- **ZoneSubTypeRegistry** — econ#zone-sub-type-registry

##### Failure Modes

- Fails if catalog entries lack stable slugs — picker selection persists across scene loads as null.

##### Decision Dependencies

- DEC-A29 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/UI/RegionSubtypeCatalog.cs` — null LOC, new — Region-scale subtype catalog; small/medium/large city footprint entries.

#### Task 5.0.3 — Enriched

##### Visual Mockup

```svg
<svg viewBox="0 0 400 240" xmlns="http://www.w3.org/2000/svg">
  <rect width="400" height="240" fill="var(--ds-bg-canvas, #1a1d24)"/>
  <text x="200" y="20" text-anchor="middle" fill="var(--ds-text-secondary, #9aa3b2)" font-family="monospace" font-size="11">RegionScene — Stage 5.0.3 lazy city placed</text>
  <g transform="translate(80, 50)">
    <polygon points="120,0 240,60 120,120 0,60" fill="var(--ds-accent-grass, #6a8e4e)"/>
    <rect x="100" y="40" width="40" height="20" fill="var(--ds-accent-warm, #f4d28a)" stroke="var(--ds-text-primary, #e6e9ef)"/>
    <text x="120" y="54" text-anchor="middle" fill="var(--ds-text-canvas, #1a1d24)" font-family="monospace" font-size="9">[new]</text>
  </g>
  <text x="200" y="200" text-anchor="middle" fill="var(--ds-text-muted, #6c7689)" font-family="monospace" font-size="10">CityData lazy-created; Enter City disabled</text>
  <text x="200" y="225" text-anchor="middle" fill="var(--ds-text-success, #8ac28a)" font-family="monospace" font-size="10">save file: cities += new entry</text>
</svg>
```

##### Glossary Anchors

- **Save data** — persist §Save
- **Multi-scale save tree** — ms
- **CellData** — persist §Save, geo §11.2

##### Failure Modes

- Fails if CityData id collision — must use reserve-id.sh pattern or runtime UUID for lazy ids.
- Fails if save mutation not atomic — partial write leaves CityData entry without RegionData link.

##### Decision Dependencies

- DEC-A29 (inherits)
- DEC-A10 (inherits)
- DEC-A26 (inherits)

##### Touched Paths Preview

- `Assets/Scripts/RegionScene/Domains/Persistence/RegionSaveService.cs` — null LOC, extend — LinkCity mutation extends save; atomic write contract with CityDataFactory.CreateLazy.
- `Assets/Scripts/CityData.cs` — null LOC, extend — CityDataFactory.CreateLazy emits new minimal CityData; owning_region_cell field added.
