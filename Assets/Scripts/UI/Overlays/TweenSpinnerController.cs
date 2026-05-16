using UnityEngine;
using UnityEngine.UIElements;
using Territory.SceneManagement;

namespace Territory.UI.Overlays
{
    /// <summary>CoreScene MonoBehaviour — drives TweenSpinnerOverlay.uxml. Shows spinner when ZoomTransitionController.TweenElapsed ≥ spinnerThreshold. Invariant #3: cache refs in Awake.</summary>
    public class TweenSpinnerController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _overlay;
        private ZoomTransitionController _transitionController;

        const string HiddenClass  = "tween-spinner-overlay--hidden";
        const string VisibleClass = "tween-spinner-overlay--visible";

        void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument != null)
            {
                var root = uiDocument.rootVisualElement;
                _overlay = root?.Q<VisualElement>("tween-spinner-overlay");
            }

            HideImmediate();
        }

        void Start()
        {
            _transitionController = FindObjectOfType<ZoomTransitionController>();
            if (_transitionController != null)
            {
                _transitionController.TweenElapsed     += OnTweenElapsed;
                _transitionController.StateChanged      += OnStateChanged;
            }
        }

        void OnDestroy()
        {
            if (_transitionController != null)
            {
                _transitionController.TweenElapsed     -= OnTweenElapsed;
                _transitionController.StateChanged      -= OnStateChanged;
            }
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void OnTweenElapsed(float elapsed)
        {
            if (elapsed >= 3f)
                ShowSpinner();
        }

        private void OnStateChanged(TransitionState state)
        {
            // Hide spinner when transition resolves or resets.
            if (state == TransitionState.Idle || state == TransitionState.Landing)
                HideImmediate();
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void ShowSpinner()
        {
            if (_overlay == null) return;
            _overlay.RemoveFromClassList(HiddenClass);
            _overlay.AddToClassList(VisibleClass);
        }

        private void HideImmediate()
        {
            if (_overlay == null) return;
            _overlay.RemoveFromClassList(VisibleClass);
            _overlay.AddToClassList(HiddenClass);
        }
    }
}
