using UnityEngine;
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

        private void Start()
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
        }
    }
}
