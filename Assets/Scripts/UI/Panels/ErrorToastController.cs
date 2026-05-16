using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Panels
{
    /// <summary>Toast kind discriminator for error surface.</summary>
    public enum ToastKind
    {
        SaveFailed,
        LoadFailed
    }

    /// <summary>CoreScene MonoBehaviour — drives ErrorToastSurface.uxml. Show(ToastKind) API; 6s auto-fade; dismiss button. Invariant #3: cache refs in Awake.</summary>
    public class ErrorToastController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _banner;
        private Label _titleLabel;
        private Label _bodyLabel;
        private Button _dismissButton;

        private CancellationTokenSource _fadeCts;
        private bool _isVisible;

        const float AutoFadeSeconds = 6f;

        void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument != null)
            {
                var root = uiDocument.rootVisualElement;
                _banner       = root?.Q<VisualElement>("error-toast-banner");
                _titleLabel   = root?.Q<Label>("error-toast-title");
                _bodyLabel    = root?.Q<Label>("error-toast-body");
                _dismissButton = root?.Q<Button>("error-toast-dismiss");
            }

            _dismissButton?.RegisterCallback<ClickEvent>(_ => Hide());
            HideBannerImmediate();
        }

        /// <summary>Show toast with kind-specific copy. Auto-fades after 6s.</summary>
        public void Show(ToastKind kind)
        {
            if (_banner == null) return;

            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = new CancellationTokenSource();

            ApplyCopy(kind);

            _banner.RemoveFromClassList("error-toast__banner--fading");
            _banner.AddToClassList("error-toast__banner--visible");
            _isVisible = true;

            // Schedule auto-fade after 6s using UI Toolkit scheduler.
            long delayMs = (long)(AutoFadeSeconds * 1000f);
            _banner.schedule.Execute(() =>
            {
                if (_isVisible) StartFade();
            }).StartingIn(delayMs);
        }

        /// <summary>Hide immediately (dismiss path).</summary>
        public void Hide()
        {
            _fadeCts?.Cancel();
            HideBannerImmediate();
        }

        // ── Private ──────────────────────────────────────────────────────────

        void ApplyCopy(ToastKind kind)
        {
            if (kind == ToastKind.SaveFailed)
            {
                if (_titleLabel != null) _titleLabel.text = "Save Failed";
                if (_bodyLabel != null)  _bodyLabel.text  = "Your progress could not be saved. Please try again.";
            }
            else
            {
                if (_titleLabel != null) _titleLabel.text = "Load Failed";
                if (_bodyLabel != null)  _bodyLabel.text  = "Save data could not be loaded. Please restart the game.";
            }
        }

        void StartFade()
        {
            if (_banner == null) return;
            _banner.AddToClassList("error-toast__banner--fading");
            // After transition-duration (0.4s) hide completely.
            _banner.schedule.Execute(() => HideBannerImmediate()).StartingIn(450);
        }

        void HideBannerImmediate()
        {
            if (_banner == null) return;
            _banner.RemoveFromClassList("error-toast__banner--visible");
            _banner.RemoveFromClassList("error-toast__banner--fading");
            _isVisible = false;
        }

        void OnDestroy()
        {
            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
        }
    }
}
