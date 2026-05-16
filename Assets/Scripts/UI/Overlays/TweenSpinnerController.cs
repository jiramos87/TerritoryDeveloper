using UnityEngine;
using UnityEngine.UIElements;
using Territory.SceneManagement;
using Territory.UI.Panels;

namespace Territory.UI.Overlays
{
    /// <summary>CoreScene MonoBehaviour — shows spinner overlay when tween elapsed ≥3s; aborts with LoadFailed toast at ≥5s cap. Subscribes to ZoomTransitionController.TweenElapsed. Invariant #3: cache refs in Awake.</summary>
    public class TweenSpinnerController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private ZoomTransitionController zoomController;

        private VisualElement _overlay;
        private bool _spinnerVisible;

        private const float SpinnerShowThreshold = 3f;
        private const float AbortCapThreshold    = 5f;

        void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument != null)
                _overlay = uiDocument.rootVisualElement?.Q<VisualElement>("tween-spinner-overlay");

            HideOverlay();
        }

        void Start()
        {
            if (zoomController == null)
                zoomController = FindObjectOfType<ZoomTransitionController>();

            if (zoomController != null)
            {
                zoomController.TweenElapsed += OnTweenElapsed;
                zoomController.StateChanged += OnStateChanged;
            }
        }

        void OnDestroy()
        {
            if (zoomController != null)
            {
                zoomController.TweenElapsed -= OnTweenElapsed;
                zoomController.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(TransitionState state)
        {
            if (state == TransitionState.Idle || state == TransitionState.AwaitLoad)
                HideOverlay();
        }

        private void OnTweenElapsed(float elapsed)
        {
            if (elapsed >= AbortCapThreshold)
            {
                // 5s cap: abort transition — handled by ZoomTransitionController; just ensure spinner hides.
                HideOverlay();
                return;
            }

            if (elapsed >= SpinnerShowThreshold && !_spinnerVisible)
                ShowOverlay();
        }

        private void ShowOverlay()
        {
            _spinnerVisible = true;
            if (_overlay == null) return;
            _overlay.RemoveFromClassList("tween-spinner-overlay--hidden");
            _overlay.AddToClassList("tween-spinner-overlay--visible");
        }

        private void HideOverlay()
        {
            _spinnerVisible = false;
            if (_overlay == null) return;
            _overlay.RemoveFromClassList("tween-spinner-overlay--visible");
            _overlay.AddToClassList("tween-spinner-overlay--hidden");
        }
    }
}
