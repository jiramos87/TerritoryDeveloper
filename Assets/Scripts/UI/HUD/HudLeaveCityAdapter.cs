using UnityEngine;
using UnityEngine.UIElements;
using Territory.SceneManagement;
using Territory.UI.Panels;

namespace Territory.UI.HUD
{
    /// <summary>Wires hud-leave-city button → ConfirmTransitionPanelController. Invariant #3: cache refs in Awake.</summary>
    public class HudLeaveCityAdapter : MonoBehaviour
    {
        [SerializeField] private UIDocument hudDocument;
        [SerializeField] private ConfirmTransitionPanelController confirmPanel;

        private Button _leaveCityButton;

        void Awake()
        {
            if (hudDocument == null)
                hudDocument = GetComponent<UIDocument>();

            if (hudDocument != null)
                _leaveCityButton = hudDocument.rootVisualElement.Q<Button>("hud-leave-city");

            _leaveCityButton?.RegisterCallback<ClickEvent>(_ => OnLeaveCityClicked());
        }

        void Start()
        {
            if (confirmPanel == null)
                confirmPanel = FindObjectOfType<ConfirmTransitionPanelController>();
        }

        private void OnLeaveCityClicked()
        {
            confirmPanel?.ShowForTarget(IsoSceneContext.Region);
        }
    }
}
