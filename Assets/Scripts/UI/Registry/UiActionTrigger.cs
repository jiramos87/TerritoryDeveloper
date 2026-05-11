using UnityEngine;
using UnityEngine.EventSystems;
using Territory.UI.StudioControls;

namespace Territory.UI.Registry
{
    /// <summary>
    /// Bake-time wiring component — reads <see cref="IlluminatedButton.OnClick"/> and dispatches
    /// <see cref="UiActionRegistry.Dispatch"/> with the bake-baked action id. UiBakeHandler attaches
    /// this when <c>params_json.action</c> is present, so the rendered button is wired without scene-side
    /// hand-edits. Registry resolution is by scene scan (FindObjectOfType) — main-menu host owns the
    /// single registry instance per Wave A0 (TECH-27059).
    /// </summary>
    [RequireComponent(typeof(IlluminatedButton))]
    public class UiActionTrigger : MonoBehaviour
    {
        [SerializeField] private string _actionId;

        public string ActionId => _actionId;

        /// <summary>Bake-time setter — UiBakeHandler writes via SerializedObject so id persists in prefab.</summary>
        public void SetActionId(string actionId) { _actionId = actionId; }

        private IlluminatedButton _button;
        private UiActionRegistry _registry;

        // Stage 13 hotfix — wire in Awake instead of Start. Baked buttons inside modal
        // panels (pause-menu / budget-panel / stats-panel) get instantiated, then their
        // root is SetActive(false) by ModalCoordinator.RegisterPanel before end of frame,
        // so Start never fires → click listener never attached. Awake fires on Instantiate
        // regardless of subsequent active state.
        private void Awake()
        {
            if (string.IsNullOrEmpty(_actionId)) return;
            _button = GetComponent<IlluminatedButton>();
            if (_button == null) return;
            _button.OnClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.OnClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (string.IsNullOrEmpty(_actionId)) return;
            if (_registry == null) _registry = FindObjectOfType<UiActionRegistry>();
            if (_registry == null)
            {
                Debug.LogWarning($"[UiActionTrigger] no UiActionRegistry in scene (actionId={_actionId})");
                return;
            }
            if (!_registry.Dispatch(_actionId, null))
            {
                Debug.LogWarning($"[UiActionTrigger] action not registered (actionId={_actionId})");
            }

            // Stage 13 hotfix — drop EventSystem selection after dispatch so the
            // button's "selected" highlight (yellow) doesn't linger after click.
            // Without this, the next time the panel reopens the last-clicked
            // button is still highlighted as selected.
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
