using UnityEngine;
using Domains.Registry;
using Territory.IsoSceneCore;

namespace Territory.RegionScene
{
    /// <summary>RegionScene composition root hub. Hub-preservation rule: path + class name + Inspector fields must never change post-creation.</summary>
    public sealed class RegionManager : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float panSpeed = 5f;

        private ServiceRegistry _registry;
        private IsoSceneCamera _camera;

        private void Awake()
        {
            _registry = FindObjectOfType<ServiceRegistry>();
            if (_registry == null)
                Debug.LogWarning("[RegionManager] ServiceRegistry not found in scene.");

            SpawnPlaceholderSprite();
        }

        private void Start()
        {
            if (_registry == null) return;
            _camera = new IsoSceneCamera();
            _camera.Configure(mainCamera, panSpeed);
            _registry.Register<IsoSceneCamera>(_camera);
        }

        private void Update()
        {
            _camera?.Tick(Time.deltaTime);
        }

        private void SpawnPlaceholderSprite()
        {
            var sprite = Resources.Load<Sprite>("region/placeholder");
            var go = new GameObject("PlaceholderSprite");
            go.transform.SetParent(transform);
            go.transform.position = GridCenterWorld();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
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
