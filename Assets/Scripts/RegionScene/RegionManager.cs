using System;
using UnityEngine;
using Domains.Registry;
using Territory.Core;
using Territory.IsoSceneCore;
using Territory.IsoSceneCore.Contracts;
using Territory.RegionScene.CellRendering;
using Territory.RegionScene.Evolution;
using Territory.RegionScene.Persistence;
using Territory.RegionScene.Terrain;
using Territory.RegionScene.Tools;
using Territory.RegionScene.UI;
using Territory.Services;

namespace Territory.RegionScene
{
    /// <summary>RegionScene composition root hub. Hub-preservation rule: path + class name + Inspector fields must never change post-creation.</summary>
    public sealed class RegionManager : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float panSpeed = 5f;
        [SerializeField] private int terrainSeed = 42;

        // Flat-grass prototype: single terrain sprite (grass everywhere). Post-prototype expansion
        // adds water/cliff/region-specific sprite slots back per the new geo logic.
        [SerializeField] private Sprite grassSprite;

        // Stage 3.0 UI panel hosts (wired in Inspector)
        [SerializeField] private RegionCellClickHandler cellClickHandler;

        private ServiceRegistry _registry;
        private IsoSceneCamera _camera;
        private IsoSceneChunkCuller _culler;
        private RegionHeightMap _heightMap;
        private RegionWaterMap _waterMap;
        private RegionCliffMap _cliffMap;
        private RegionCellRenderer _cellRenderer;
        private RegionData _regionData;
        private RegionToolCreateCity _createCityTool;

        // Stage 8.0 — single-renderer registry slot. Defaults to BrownDiamondCellRenderer;
        // sibling exploration overrides by registering its own IRegionCellRenderer before RegionManager.Start.
        private IRegionCellRenderer _cellRendererPlugin;

        // Stage 8.0 — IsoSceneContextService for event fire guard (only fire in Region context).
        private IsoSceneContextService _contextService;

        /// <summary>Stage 8.0 — fires when GrowthCatchupRunner.Catchup completes for the player city while
        /// IsoSceneContext == Region. Sibling exploration subscribes to re-render the player 2x2 area.
        /// Does NOT fire while IsoSceneContext == City (city self-renders in CityScene).</summary>
        public event Action<PlayerCityState> PlayerCityDataUpdated;

        private void Awake()
        {
            _registry = FindObjectOfType<ServiceRegistry>();
            if (_registry == null)
            {
                Debug.LogWarning("[RegionManager] ServiceRegistry not found in scene.");
                return;
            }

            // Register RegionData in Awake so evolution + save services can resolve in Start (invariant #12).
            _regionData = new RegionData(RegionHeightMap.RegionGridSize);
            _registry.Register<RegionData>(_regionData);

            // Register IsoSceneTickBus so tick subscribers can resolve in Start.
            _registry.Register<IsoSceneTickBus>(new IsoSceneTickBus());
        }

        private void Start()
        {
            if (_registry == null) return;

            // Camera
            _camera = new IsoSceneCamera();
            _camera.Configure(mainCamera, panSpeed);
            _registry.Register<IsoSceneCamera>(_camera);

            // Terrain maps
            _heightMap = new RegionHeightMap();
            _heightMap.Seed(terrainSeed);
            _waterMap = new RegionWaterMap();
            _waterMap.Seed(_heightMap, terrainSeed);
            _cliffMap = new RegionCliffMap();
            _cliffMap.Compute(_heightMap, _waterMap);

            // Chunk culler
            _culler = new IsoSceneChunkCuller();
            _culler.Configure(mainCamera, RegionHeightMap.RegionGridSize, RegionHeightMap.RegionGridSize);
            _registry.Register<IsoSceneChunkCuller>(_culler);

            // Cell renderer — replaces placeholder
            DestroyPlaceholder();
            var rendererGo = new GameObject("RegionCellRenderer");
            rendererGo.transform.SetParent(transform);
            _cellRenderer = rendererGo.AddComponent<RegionCellRenderer>();
            _cellRenderer.Configure(_heightMap, _waterMap, _cliffMap, _culler);
            _cellRenderer.WireSprites(grassSprite);

            // Stage 8.0 — resolve IRegionCellRenderer plugin (sibling override or default).
            _cellRendererPlugin = _registry.Resolve<IRegionCellRenderer>();
            if (_cellRendererPlugin == null)
            {
                var defaultPlugin = new BrownDiamondCellRenderer(rendererGo.transform, grassSprite, RegionHeightMap.RegionGridSize);
                _cellRendererPlugin = defaultPlugin;
                _registry.Register<IRegionCellRenderer>(_cellRendererPlugin);
            }
            // Wire plugin into culler callback so Render() is invoked per visible cell.
            _culler.OnVisibleSetChanged += OnVisibleSetChangedPlugin;

            // Stage 3.0 — wire click handler (Subscribe in Start per invariant #12)
            if (cellClickHandler != null)
                cellClickHandler.Configure(_heightMap, _waterMap, _cliffMap);

            // Stage 8.0 — resolve context service for PlayerCityDataUpdated guard.
            _contextService = FindObjectOfType<IsoSceneContextService>();

            // Stage 5.0 — register city-placement tool into IIsoSceneToolRegistry
            var toolReg   = _registry.Resolve<IIsoSceneToolRegistry>();
            var saveService = _registry.Resolve<RegionSaveService>();
            if (toolReg != null)
            {
                toolReg.Register(RegionToolCreateCity.MakeDescriptor());
                _createCityTool = new RegionToolCreateCity();
                _createCityTool.Configure(_regionData, _heightMap, _waterMap, _cliffMap, cellClickHandler, saveService);
                _registry.Register<RegionToolCreateCity>(_createCityTool);
            }
            else
            {
                Debug.LogWarning("[RegionManager] IIsoSceneToolRegistry not found — create-city tool not registered.");
            }
        }

        private void Update()
        {
            _camera?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_culler != null)
                _culler.OnVisibleSetChanged -= OnVisibleSetChangedPlugin;
        }

        /// <summary>Stage 8.0 — invoke IRegionCellRenderer.Render per visible cell chunk range.</summary>
        private void OnVisibleSetChangedPlugin(int minCX, int maxCX, int minCY, int maxCY)
        {
            if (_cellRendererPlugin == null || _heightMap == null) return;
            int chunkSize = 16;
            int xStart = Mathf.Max(0, minCX * chunkSize);
            int xEnd   = Mathf.Min(RegionHeightMap.RegionGridSize - 1, (maxCX + 1) * chunkSize - 1);
            int yStart = Mathf.Max(0, minCY * chunkSize);
            int yEnd   = Mathf.Min(RegionHeightMap.RegionGridSize - 1, (maxCY + 1) * chunkSize - 1);

            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    var regionCell = new RegionCell(x, y, _regionData?.GetCell(x, y)?.owningCityId);
                    _cellRendererPlugin.Render(regionCell, null);
                }
            }
        }

        private void DestroyPlaceholder()
        {
            var placeholder = transform.Find("PlaceholderSprite");
            if (placeholder != null) Destroy(placeholder.gameObject);
        }

        /// <summary>World position of region grid center cell [31,31] in a 64x64 grid. Region cell scale = 1 unit per cell.</summary>
        private static Vector3 GridCenterWorld()
        {
            // Region grid: 64x64, center at cell [31,31].
            // Isometric: x = (col - row) * 0.5, y = (col + row) * 0.25
            const int col = 31;
            const int row = 31;
            float wx = (col - row) * 0.5f;
            float wy = (col + row) * 0.25f;
            return new Vector3(wx, wy, 0f);
        }

        // ArrowKeysPanCamera — tracer anchor for stage1.0 test
        public bool ArrowKeysPanCamera => _camera != null;

        /// <summary>Stage 8.0 — active IRegionCellRenderer plugin. Used by contract test to assert Render() call count.</summary>
        public IRegionCellRenderer CellRendererPlugin => _cellRendererPlugin;

        /// <summary>Stage 8.0 — called by growth-catchup pipeline when city evolution completes while in Region context.
        /// Fires PlayerCityDataUpdated and re-renders the player 2x2 area via the active IRegionCellRenderer.
        /// NO-OP when IsoSceneContext == City (city self-renders).</summary>
        public void NotifyCityEvolved(PlayerCityState state)
        {
            if (state == null) return;
            // Guard: only fire and re-render in Region context.
            if (_contextService != null && _contextService.Context != IsoSceneContextService.SceneContext.Region)
                return;

            PlayerCityDataUpdated?.Invoke(state);

            // Re-render player 2x2 footprint with fresh state.
            if (_cellRendererPlugin != null)
            {
                for (int dx = 0; dx < 2; dx++)
                {
                    for (int dy = 0; dy < 2; dy++)
                    {
                        int cx = state.AnchorX + dx;
                        int cy = state.AnchorY + dy;
                        if (cx < 0 || cx >= RegionHeightMap.RegionGridSize) continue;
                        if (cy < 0 || cy >= RegionHeightMap.RegionGridSize) continue;
                        var cell = new RegionCell(cx, cy, _regionData?.GetCell(cx, cy)?.owningCityId);
                        _cellRendererPlugin.Render(cell, state);
                    }
                }
            }
        }
    }
}
