using UnityEngine;
using UnityEngine.UI;
using Territory.SceneManagement;
using Territory.Services;

namespace Territory.Managers
{
    /// <summary>CoreScene minimap hub. DontDestroyOnLoad persists across scene loads. Hub-preservation rule: path + class name + Inspector fields must never change post-creation. Stage 5.0: supports MinimapMode.City / MinimapMode.Region with per-mode RenderTexture caching.</summary>
    public class MinimapController : MonoBehaviour
    {
        /// <summary>Active rendering mode driven by IsoSceneContext.</summary>
        public enum Mode { City, Region }

        [SerializeField] private RawImage minimapImage;
        [SerializeField] private Camera minimapCamera;

        private Mode _currentMode = Mode.City;
        private RenderTexture _cityCache;
        private RenderTexture _regionCache;
        private IsoSceneContextService _contextService;

        /// <summary>Current minimap mode (read-only externally; set via SetMode).</summary>
        public Mode CurrentMode => _currentMode;

        void Awake()
        {
            // Hub-preservation: always in CoreScene; DontDestroyOnLoad keeps it across scene loads.
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            _contextService = FindObjectOfType<IsoSceneContextService>();
            if (_contextService != null)
                _contextService.ContextChanged += OnContextChanged;

            // Initial render into city cache.
            RenderToCache(Mode.City);
            ApplyCache(Mode.City);
        }

        void OnDestroy()
        {
            if (_contextService != null)
                _contextService.ContextChanged -= OnContextChanged;
            _cityCache?.Release();
            _regionCache?.Release();
        }

        private void OnContextChanged(IsoSceneContextService.SceneContext ctx)
        {
            switch (ctx)
            {
                case IsoSceneContextService.SceneContext.City:
                    SetMode(Mode.City);
                    break;
                case IsoSceneContextService.SceneContext.Region:
                    SetMode(Mode.Region);
                    break;
                // Transition: keep current mode
            }
        }

        /// <summary>Switch minimap mode. Displays cached texture immediately; regen runs async on next Update.</summary>
        public void SetMode(Mode mode)
        {
            if (_currentMode == mode) return;
            _currentMode = mode;

            // Display cached texture to avoid blank-flash during regen.
            ApplyCache(mode);

            // Schedule regen for next frame (background capture).
            _pendingRegen = true;
        }

        /// <summary>Invalidate city cache (call on PlayerCityDataUpdated).</summary>
        public void InvalidateCityCache()
        {
            if (_cityCache != null)
            {
                _cityCache.Release();
                _cityCache = null;
            }
            if (_currentMode == Mode.City)
                _pendingRegen = true;
        }

        /// <summary>Invalidate region cache (call on cell stream-in complete).</summary>
        public void InvalidateRegionCache()
        {
            if (_regionCache != null)
            {
                _regionCache.Release();
                _regionCache = null;
            }
            if (_currentMode == Mode.Region)
                _pendingRegen = true;
        }

        private bool _pendingRegen;

        void Update()
        {
            if (!_pendingRegen) return;
            _pendingRegen = false;
            RenderToCache(_currentMode);
            ApplyCache(_currentMode);
        }

        private void RenderToCache(Mode mode)
        {
            if (minimapCamera == null) return;

            int w = 256;
            int h = 256;

            ref RenderTexture cache = ref (mode == Mode.City ? ref _cityCache : ref _regionCache);
            if (cache == null || !cache.IsCreated())
                cache = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32);

            var prev = minimapCamera.targetTexture;
            minimapCamera.targetTexture = cache;
            minimapCamera.Render();
            minimapCamera.targetTexture = prev;
        }

        private void ApplyCache(Mode mode)
        {
            if (minimapImage == null) return;
            var cache = mode == Mode.City ? _cityCache : _regionCache;
            if (cache != null)
                minimapImage.texture = cache;
        }

    }
}
