using UnityEngine;
using Domains.Registry;
using Territory.IsoSceneCore;
using Territory.RegionScene.Terrain;

namespace Territory.RegionScene
{
    /// <summary>RegionScene composition root hub. Hub-preservation rule: path + class name + Inspector fields must never change post-creation.</summary>
    public sealed class RegionManager : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float panSpeed = 5f;
        [SerializeField] private int terrainSeed = 42;

        private ServiceRegistry _registry;
        private IsoSceneCamera _camera;
        private IsoSceneChunkCuller _culler;
        private RegionHeightMap _heightMap;
        private RegionWaterMap _waterMap;
        private RegionCliffMap _cliffMap;
        private RegionCellRenderer _cellRenderer;

        private void Awake()
        {
            _registry = FindObjectOfType<ServiceRegistry>();
            if (_registry == null)
                Debug.LogWarning("[RegionManager] ServiceRegistry not found in scene.");
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
        }

        private void Update()
        {
            _camera?.Tick(Time.deltaTime);
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
    }
}
