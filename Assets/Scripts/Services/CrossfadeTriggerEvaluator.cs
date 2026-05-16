using UnityEngine;
using PrimeTween;
using Territory.SceneManagement;

namespace Territory.Services
{
    /// <summary>Interface — allows test injection.</summary>
    public interface ICrossfadeTriggerEvaluator
    {
        bool ShouldFireRegionFade(float cameraOrthoSize, Rect cityFootprintWorld, Rect regionAnchorWorld);
        void ResetForNewTransition();
    }

    /// <summary>CoreScene service — evaluates geometric crossfade trigger during TweeningOut. Fires region-layer alpha tween once when city footprint is contained in 2x2 anchor rect. Wire RegionLayerRoot in inspector.</summary>
    public class CrossfadeTriggerEvaluator : MonoBehaviour, ICrossfadeTriggerEvaluator
    {
        [SerializeField] private CanvasGroup regionLayerGroup;
        private const float FadeDuration = 0.5f;

        private bool _hasFiredRegionFade;
        private ZoomTransitionController _zoomController;

        void Start()
        {
            _zoomController = FindObjectOfType<ZoomTransitionController>();
            if (_zoomController != null)
                _zoomController.StateChanged += OnStateChanged;
        }

        void OnDestroy()
        {
            if (_zoomController != null)
                _zoomController.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(TransitionState state)
        {
            if (state == TransitionState.Idle)
                ResetForNewTransition();
        }

        /// <summary>Returns true (once per transition) when city footprint is geometrically contained in the 2×2 region anchor world rect.</summary>
        public bool ShouldFireRegionFade(float cameraOrthoSize, Rect cityFootprintWorld, Rect regionAnchorWorld)
        {
            if (_hasFiredRegionFade) return false;
            bool contained = regionAnchorWorld.Contains(new Vector2(cityFootprintWorld.xMin, cityFootprintWorld.yMin))
                          && regionAnchorWorld.Contains(new Vector2(cityFootprintWorld.xMax, cityFootprintWorld.yMax));
            if (contained)
            {
                _hasFiredRegionFade = true;
                FireRegionFade();
                return true;
            }
            return false;
        }

        /// <summary>Reset latch — called on Idle entry to prepare for next transition.</summary>
        public void ResetForNewTransition()
        {
            _hasFiredRegionFade = false;
        }

        private void FireRegionFade()
        {
            if (regionLayerGroup == null) return;
            regionLayerGroup.alpha = 0f;
            Tween.Alpha(regionLayerGroup, endValue: 1f, duration: FadeDuration);
        }
    }
}
