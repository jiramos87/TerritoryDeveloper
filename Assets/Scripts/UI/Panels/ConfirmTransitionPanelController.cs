using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using Territory.SceneManagement;

namespace Territory.UI.Panels
{
    /// <summary>Binds ConfirmTransitionPanel.uxml Yes/No buttons to ZoomTransitionController. Invariant #3: cache refs in Awake; never FindObjectOfType in Update.</summary>
    public class ConfirmTransitionPanelController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _backdrop;
        private Button _yesButton;
        private Button _noButton;

        private ZoomTransitionController _transitionController;
        private IsoSceneContext _pendingTarget;
        private CancellationTokenSource _cts;

        void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument != null)
            {
                var root = uiDocument.rootVisualElement;
                _backdrop  = root.Q<VisualElement>("confirm-transition-backdrop");
                _yesButton = root.Q<Button>("confirm-transition-yes");
                _noButton  = root.Q<Button>("confirm-transition-no");
            }

            // Wire buttons in Awake per invariant #3.
            _yesButton?.RegisterCallback<ClickEvent>(_ => OnYes());
            _noButton?.RegisterCallback<ClickEvent>(_ => OnNo());

            HidePanel();
        }

        void Start()
        {
            _transitionController = FindObjectOfType<ZoomTransitionController>();
        }

        /// <summary>Show panel for given target context. Called from HUD Leave-City button handler.</summary>
        public void ShowForTarget(IsoSceneContext target)
        {
            _pendingTarget = target;
            _backdrop?.AddToClassList("confirm-transition__backdrop--visible");
            _backdrop?.RemoveFromClassList("" /* ensure display:none cleared via class */);
            if (_backdrop != null)
                _backdrop.style.display = DisplayStyle.Flex;
        }

        public void HidePanel()
        {
            if (_backdrop != null)
                _backdrop.style.display = DisplayStyle.None;
        }

        private void OnYes()
        {
            HidePanel();
            if (_transitionController == null) return;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _transitionController.AutoConfirm = true;
            _ = _transitionController.RequestTransition(_pendingTarget, _cts.Token);
        }

        private void OnNo()
        {
            HidePanel();
        }

        void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
